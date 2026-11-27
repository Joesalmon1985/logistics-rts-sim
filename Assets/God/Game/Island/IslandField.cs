using System.Collections.Generic;
using God.Sim;
using UnityEngine;
using Terrain = God.Sim.Terrain;

namespace God.Game
{
    /// <summary>
    /// P4a: the organic island as a set of pure functions of (board, seed, x, z). The sim keeps its 19 hexes; this only
    /// decides how the land LOOKS. Every ground point belongs to the hex whose centre is nearest to a domain-warped copy
    /// of the point (so the borders between terrains wander and never run straight), blends softly with the runner-up,
    /// and lies on land when it is inside the hex union pushed in or out by low noise (bays and headlands). Heights are
    /// gentle and flat where build circles can be (every corner shared by three land hexes) and round each hex centre,
    /// where the dice piles collect. Same board and seed give the same values, bit for bit. View only.
    /// </summary>
    public sealed class IslandField
    {
        public readonly Board Board;
        public readonly ulong Seed;
        public readonly BoardTopology Topo;
        public readonly int HexCount;
        private readonly float[] _cx, _cz;
        private readonly Terrain[] _terrain;
        private readonly int[] _pips;
        private readonly float[] _ox = new float[12], _oz = new float[12];
        private readonly float[] _fieldAngle;
        private readonly Vector2[] _hqCorners;

        /// <summary>How far (m) the terrain lookup point is pushed by noise.</summary>
        public const float WarpAmp = 26f;
        /// <summary>Width (m) of the colour blend between two terrains.</summary>
        public const float BlendWidth = 7f;
        /// <summary>Base level of the sea surface (m).</summary>
        public const float SeaY = -0.35f;
        /// <summary>Radius (m) round a corner that stays flat (a build circle is 30 m).</summary>
        public const float CornerFlat = 32f;
        /// <summary>Centre of the pile clearing relative to the hex centre: the piles stand a little south of it.</summary>
        public static readonly Vector2 ClearingOffset = new Vector2(0f, -6f);
        public const float ClearingRadius = 16f;

        public IslandField(Board board, ulong seed)
        {
            Board = board; Seed = seed; Topo = board.Topology; HexCount = Topo.HexCount;
            _cx = new float[HexCount]; _cz = new float[HexCount];
            _terrain = new Terrain[HexCount]; _pips = new int[HexCount]; _fieldAngle = new float[HexCount];
            for (int h = 0; h < HexCount; h++)
            {
                _cx[h] = (float)Topo.HexX(h); _cz[h] = (float)Topo.HexZ(h);
                _terrain[h] = board.HexTerrain[h];
                _pips[h] = BoardGenerator.Pips(board.HexNumber[h]);
                _fieldAngle[h] = Hash01(h, 991) * 180f;
            }
            for (int i = 0; i < _ox.Length; i++) { _ox[i] = 13f + Hash01(i, 17) * 600f; _oz[i] = 29f + Hash01(i, 53) * 600f; }
            var hq = new List<Vector2>();
            for (int c = 0; c < Topo.CornerCount; c++)
                if (Topo.VertexHexes(c).Count == 3) hq.Add(new Vector2((float)Topo.VertexX(c), (float)Topo.VertexZ(c)));
            _hqCorners = hq.ToArray();
        }

        public Terrain TerrainOf(int h) => _terrain[h];
        /// <summary>Richness 0..5 (dice pips of the hidden number). Shown only as dressing density and size.</summary>
        public int PipsOf(int h) => _pips[h];
        public Vector2 Centre(int h) => new Vector2(_cx[h], _cz[h]);
        public float FieldAngle(int h) => _fieldAngle[h];

        // ------------------------------------------------------------------ hashing and noise

        /// <summary>Deterministic 0..1 hash of two ints mixed with the seed.</summary>
        public float Hash01(int a, int b)
        {
            unchecked
            {
                ulong x = Seed * 0x9E3779B97F4A7C15UL ^ (ulong)(uint)a * 0xBF58476D1CE4E5B9UL ^ (ulong)(uint)b * 0x94D049BB133111EBUL;
                x ^= x >> 31; x *= 0xD6E8FEB86DA3B5C9UL; x ^= x >> 29; x *= 0x9E3779B97F4A7C15UL; x ^= x >> 32;
                return (x & 0xFFFFFF) / (float)0x1000000;
            }
        }

        /// <summary>Seeded Perlin noise 0..1 on channel ch.</summary>
        public float Noise(float x, float z, int ch, float freq) => Mathf.PerlinNoise(x * freq + _ox[ch], z * freq + _oz[ch]);

        /// <summary>Two-octave noise 0..1.</summary>
        public float Fbm(float x, float z, int ch, float freq)
            => Noise(x, z, ch, freq) * 0.68f + Noise(x + 71.3f, z - 13.1f, ch, freq * 2.7f) * 0.32f;

        /// <summary>The domain warp: the point whose nearest hex centre decides the terrain.</summary>
        public void Warp(float x, float z, out float wx, out float wz)
        {
            wx = x + (Fbm(x, z, 0, 1f / 85f) * 2f - 1f) * WarpAmp + (Noise(x, z, 2, 1f / 34f) * 2f - 1f) * 17f + (Noise(x, z, 2, 1f / 13f) * 2f - 1f) * 5f;
            wz = z + (Fbm(x, z, 1, 1f / 85f) * 2f - 1f) * WarpAmp + (Noise(x, z, 3, 1f / 34f) * 2f - 1f) * 17f + (Noise(x, z, 3, 1f / 13f) * 2f - 1f) * 5f;
        }

        // ------------------------------------------------------------------ regions

        /// <summary>
        /// The hex that owns a ground point (nearest centre to the warped point), the runner-up and the margin (m, d2 - d1):
        /// 0 on the wandering border, larger deeper inside.
        /// </summary>
        public int Owner(float x, float z, out int second, out float margin)
        {
            Warp(x, z, out float wx, out float wz);
            float d1 = float.MaxValue, d2 = float.MaxValue; int h1 = 0, h2 = 0;
            for (int h = 0; h < HexCount; h++)
            {
                float dx = _cx[h] - wx, dz = _cz[h] - wz;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < d1) { d2 = d1; h2 = h1; d1 = d; h1 = h; }
                else if (d < d2) { d2 = d; h2 = h; }
            }
            second = h2; margin = d2 - d1;
            return h1;
        }

        /// <summary>Soft weights of every hex at a point (sum 1). Only hexes within a few blend widths of the nearest get weight.</summary>
        public void Weights(float x, float z, float[] w)
        {
            Warp(x, z, out float wx, out float wz);
            float d1 = float.MaxValue;
            for (int h = 0; h < HexCount; h++)
            {
                float dx = _cx[h] - wx, dz = _cz[h] - wz;
                w[h] = Mathf.Sqrt(dx * dx + dz * dz);
                if (w[h] < d1) d1 = w[h];
            }
            float sum = 0f;
            for (int h = 0; h < HexCount; h++)
            {
                float e = (w[h] - d1) / BlendWidth;
                w[h] = e > 6f ? 0f : Mathf.Exp(-e * e);
                sum += w[h];
            }
            for (int h = 0; h < HexCount; h++) w[h] /= sum;
        }

        // ------------------------------------------------------------------ coast

        /// <summary>
        /// Smooth signed distance (m) to the board: negative inside. Each hex counts as the disc through its corners, and the
        /// discs merge with a wide smooth minimum, so the notches between the outer hexes fill in and no hex outline or
        /// zigzag shows. Every point of every board hex lies inside (at most about 2 m out at an outer corner tip).
        /// </summary>
        public float BoardSdf(float x, float z)
        {
            float r = BoardSpace.HexSize + 2f, best = float.MaxValue;
            for (int h = 0; h < HexCount; h++)
            {
                float dx = x - _cx[h], dz = z - _cz[h];
                best = SMin(best, Mathf.Sqrt(dx * dx + dz * dz) - r, 55f);
            }
            return best;
        }

        private static float SMin(float a, float b, float k)
        {
            if (a > 1e6f) return b;
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        /// <summary>
        /// Distance (m) inland from the coast: positive on land, negative at sea. The coast is the board edge pushed out
        /// by 0 to about 50 m of low noise, never more than a few metres into a board hex.
        /// </summary>
        public float Coast(float x, float z)
        {
            float wx = x + (Noise(x, z, 0, 1f / 120f) - 0.5f) * 30f, wz = z + (Noise(x, z, 1, 1f / 120f) - 0.5f) * 30f;
            float push = Mathf.Lerp(-2f, 44f, Mathf.SmoothStep(0f, 1f, (Fbm(wx, wz, 4, 1f / 140f) - 0.28f) / 0.46f))
                       + (Noise(x, z, 5, 1f / 36f) - 0.5f) * 12f;
            // Never bite into the board itself: a hex point is always at least a few metres inland.
            return Mathf.Max(push - BoardSdf(wx, wz), -BoardSdf(x, z) - 8f + Mathf.Min(push, 0f));
        }

        /// <summary>Width (m) of the beach strip at a point: from almost none to a broad sandy bay.</summary>
        public float BeachWidth(float x, float z) => 3f + 13f * Mathf.SmoothStep(0f, 1f, (Noise(x, z, 6, 1f / 70f) - 0.3f) / 0.45f);

        // ------------------------------------------------------------------ flat zones

        /// <summary>Distance (m) to the nearest corner where an HQ build circle can sit.</summary>
        public float CornerDistance(float x, float z)
        {
            float best = float.MaxValue;
            for (int i = 0; i < _hqCorners.Length; i++)
            {
                float dx = _hqCorners[i].x - x, dz = _hqCorners[i].y - z, d = dx * dx + dz * dz;
                if (d < best) best = d;
            }
            return Mathf.Sqrt(best);
        }

        /// <summary>Distance (m) from a point to the pile clearing of a hex (0 at its middle), and the clearing's ragged radius there.</summary>
        public float ClearingDistance(int h, float x, float z, out float radius)
        {
            float dx = x - (_cx[h] + ClearingOffset.x), dz = z - (_cz[h] + ClearingOffset.y);
            float a = Mathf.Atan2(dz, dx);
            radius = ClearingRadius + (Noise(Mathf.Cos(a) * 30f + h * 97f, Mathf.Sin(a) * 30f, 7, 1f / 18f) - 0.5f) * 9f;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>0 where the ground must stay flat (build circles, the pile clearing), 1 where it may roll freely.</summary>
        public float ReliefMask(int owner, float x, float z)
        {
            float corner = Mathf.SmoothStep(0f, 1f, (CornerDistance(x, z) - CornerFlat) / 14f);
            float cd = ClearingDistance(owner, x, z, out float cr);
            float centre = Mathf.SmoothStep(0f, 1f, (cd - cr) / 12f);
            return corner * centre;
        }

        // ------------------------------------------------------------------ heights

        /// <summary>
        /// Ridge value 0..1 of the mountain ranges at a point (1 on a crest): noise stretched along a slowly turning
        /// direction, so the high ground forms long irregular ranges rather than blobs or closed loops.
        /// </summary>
        public float Ridge(float x, float z)
        {
            float a = RidgeAngle(x, z);
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float u = x * ca + z * sa, v = (-x * sa + z * ca) * 2.6f;
            float n = Noise(u, v, 8, 1f / 75f) * 0.7f + Noise(u, v, 10, 1f / 28f) * 0.3f;
            return Mathf.SmoothStep(0f, 1f, (n - 0.40f) / 0.30f);
        }

        /// <summary>Direction (radians) the mountain ranges run at a point.</summary>
        public float RidgeAngle(float x, float z) => Hash01(5, 77) * Mathf.PI + (Noise(x, z, 7, 1f / 220f) - 0.5f) * 2.2f;

        /// <summary>
        /// Distance (m) from a point to the pass through a mountain hex: a valley along the ranges' direction through the
        /// pile clearing, so the piles sit in an open valley instead of a ring of peaks.
        /// </summary>
        public float PassDistance(int h, float x, float z)
        {
            float cx = _cx[h] + ClearingOffset.x, cz = _cz[h] + ClearingOffset.y;
            float a = RidgeAngle(cx, cz);
            return Mathf.Abs((x - cx) * -Mathf.Sin(a) + (z - cz) * Mathf.Cos(a));
        }

        /// <summary>Dune ripple 0..1 of the desert.</summary>
        public float Dune(float x, float z)
        {
            float wx = x + (Noise(x, z, 9, 1f / 40f) - 0.5f) * 30f;
            float wz = z + (Noise(x, z, 10, 1f / 40f) - 0.5f) * 30f;
            float s = Mathf.Sin((wx * 0.8f + wz * 0.6f) / 9f);
            return s * s * (0.5f + Noise(x, z, 11, 1f / 60f));
        }

        /// <summary>Terrain-only height (m) of a hex's relief at a point, before the flat-zone mask.</summary>
        public float TerrainHeight(Terrain t, float x, float z)
        {
            switch (t)
            {
                case Terrain.Hills: return Fbm(x, z, 9, 1f / 34f) * 4.2f;
                case Terrain.Mountain: return Ridge(x, z) * 7.5f + Fbm(x, z, 10, 1f / 25f) * 1.5f;
                case Terrain.Forest: return Fbm(x, z, 11, 1f / 40f) * 1.6f;
                case Terrain.Desert: return Dune(x, z) * 2.2f;
                case Terrain.Fields: return Fbm(x, z, 11, 1f / 70f) * 0.6f;
                default: return Fbm(x, z, 11, 1f / 50f) * 1.2f;
            }
        }

        /// <summary>Ground height (m) at a point: the sea level offshore, a beach ramp, then gentle relief inland.</summary>
        public float Height(float x, float z, float[] scratch)
        {
            float c = Coast(x, z);
            if (c <= 0f) return SeaY;
            Weights(x, z, scratch);
            int owner = Owner(x, z, out _, out _);
            float relief = 0f;
            for (int h = 0; h < HexCount; h++) if (scratch[h] > 0.001f) relief += scratch[h] * TerrainHeight(_terrain[h], x, z);
            // Off the board (the rim beyond the hexes) the nearest terrain carries on, gently.
            float mask = ReliefMask(owner, x, z);
            float beach = Mathf.SmoothStep(0f, 1f, c / Mathf.Max(1f, BeachWidth(x, z) + 6f));
            return Mathf.Lerp(SeaY + 0.05f, 0f, beach) + relief * mask * beach;
        }

        // ------------------------------------------------------------------ fields

        /// <summary>
        /// The patchwork of a fields hex at a point: patch kind (0 wheat, 1 green crop, 2 ploughed, 3 yellow flower,
        /// 4 meadow), distance (m) to the patch's edge and the row direction (degrees). Patches are warped rectangles at
        /// a seeded angle, so no two hexes line up.
        /// </summary>
        public int FieldPatch(int h, float x, float z, out float edge, out float yaw)
        {
            float a = _fieldAngle[h] * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float wx = x + (Noise(x, z, 9, 1f / 30f) - 0.5f) * 12f, wz = z + (Noise(x, z, 10, 1f / 30f) - 0.5f) * 12f;
            float u = wx * ca + wz * sa, v = -wx * sa + wz * ca;
            const float CU = 19f, CV = 12f;
            // Stagger alternate rows of patches so the seams never form a grid.
            float row = Mathf.Floor(v / CV);
            u += Hash01((int)row, h * 7 + 3) * CU;
            float col = Mathf.Floor(u / CU);
            float fu = u - col * CU, fv = v - row * CV;
            edge = Mathf.Min(Mathf.Min(fu, CU - fu), Mathf.Min(fv, CV - fv));
            float r = Hash01((int)col * 131 + (int)row, h * 17 + 1);
            float cultivated = 0.35f + 0.13f * _pips[h];
            yaw = _fieldAngle[h] + (Hash01((int)col, (int)row * 3 + h) < 0.5f ? 0f : 90f);
            if (r > cultivated) return r > cultivated + (1f - cultivated) * 0.5f ? 4 : 2;
            float q = r / cultivated;
            return q < 0.5f ? 0 : q < 0.8f ? 1 : 3;
        }

        public static Color PatchColour(int kind)
        {
            switch (kind)
            {
                case 0: return new Color(0.80f, 0.66f, 0.28f);
                case 1: return new Color(0.42f, 0.58f, 0.22f);
                case 2: return new Color(0.50f, 0.38f, 0.24f);
                case 3: return new Color(0.86f, 0.78f, 0.30f);
                default: return new Color(0.46f, 0.60f, 0.28f);
            }
        }

        // ------------------------------------------------------------------ colour

        public static readonly Color Sand = new Color(0.86f, 0.80f, 0.60f), WetSand = new Color(0.70f, 0.66f, 0.52f);
        public static readonly Color Foam = new Color(0.50f, 0.74f, 0.72f), Shallow = new Color(0.20f, 0.52f, 0.60f);
        public static readonly Color Deep = new Color(0.10f, 0.30f, 0.48f);

        /// <summary>Muted ground colour of a terrain at a point (with its own patches of variation).</summary>
        public Color TerrainColour(int h, float x, float z, float height)
        {
            float n = Noise(x, z, 11, 1f / 26f);
            switch (_terrain[h])
            {
                case Terrain.Forest:
                    return Color.Lerp(new Color(0.18f, 0.33f, 0.14f), new Color(0.28f, 0.42f, 0.18f), n);
                case Terrain.Fields:
                {
                    int k = FieldPatch(h, x, z, out float edge, out _);
                    var pc = Color.Lerp(new Color(0.46f, 0.56f, 0.27f), PatchColour(k), 0.8f);
                    var hedge = new Color(0.36f, 0.50f, 0.22f);
                    return Color.Lerp(hedge, pc, Mathf.SmoothStep(0f, 1f, (edge - 0.3f) / 3f));
                }
                case Terrain.Hills:
                {
                    var grass = Color.Lerp(new Color(0.33f, 0.52f, 0.22f), new Color(0.44f, 0.56f, 0.26f), n);
                    float clay = Mathf.SmoothStep(0.55f, 0.75f, Noise(x, z, 7, 1f / 30f));
                    return Color.Lerp(grass, new Color(0.62f, 0.46f, 0.28f), clay * 0.45f);
                }
                case Terrain.Mountain:
                {
                    var low = Color.Lerp(new Color(0.36f, 0.42f, 0.27f), new Color(0.46f, 0.43f, 0.36f), n);
                    float scree = Mathf.SmoothStep(0.45f, 0.8f, Ridge(x, z));
                    return Color.Lerp(low, new Color(0.52f, 0.50f, 0.46f), Mathf.Max(scree * 0.7f, Mathf.Clamp01(height / 6f)));
                }
                case Terrain.Desert:
                    return Color.Lerp(new Color(0.72f, 0.56f, 0.34f), new Color(0.86f, 0.70f, 0.45f), n * 0.4f + Dune(x, z) * 0.6f);
                default:
                    return Color.Lerp(new Color(0.34f, 0.50f, 0.24f), new Color(0.42f, 0.56f, 0.27f), n);
            }
        }

        /// <summary>Ground colour at a point: blended terrains, the beach, then shallows fading to deep sea.</summary>
        public Color ColourAt(float x, float z, float height, float[] scratch)
        {
            float c = Coast(x, z);
            if (c <= 0f)
            {
                float d = -c * (0.8f + 0.5f * Noise(x, z, 6, 1f / 50f));
                if (d < 3f) return Color.Lerp(Foam, Shallow, d / 3f);
                if (d < 38f) return Color.Lerp(Shallow, Deep, Mathf.SmoothStep(0f, 1f, (d - 3f) / 35f));
                return Deep;
            }
            Weights(x, z, scratch);
            float r = 0f, g = 0f, b = 0f;
            for (int h = 0; h < HexCount; h++)
            {
                if (scratch[h] < 0.001f) continue;
                var col = TerrainColour(h, x, z, height);
                r += col.r * scratch[h]; g += col.g * scratch[h]; b += col.b * scratch[h];
            }
            var land = new Color(r, g, b);
            float m = Noise(x, z, 3, 1f / 9f);
            land *= 0.94f + 0.12f * m;
            float bw = BeachWidth(x, z);
            if (c < bw + 4f)
            {
                var sand = Color.Lerp(WetSand, Sand, Mathf.SmoothStep(0f, 1f, c / 2.5f));
                land = Color.Lerp(sand, land, Mathf.SmoothStep(0f, 1f, (c - bw) / 4f));
            }
            land.a = 1f;
            return land;
        }

        /// <summary>Shading-only small bumps added to the shape normal, so flat ground is never one flat tone.</summary>
        public Vector3 Bump(float x, float z, float amp)
        {
            const float e = 1.5f;
            float hx = (Noise(x + e, z, 3, 1f / 9f) - Noise(x - e, z, 3, 1f / 9f)) * amp / (2f * e);
            float hz = (Noise(x, z + e, 3, 1f / 9f) - Noise(x, z - e, 3, 1f / 9f)) * amp / (2f * e);
            return new Vector3(-hx, 0f, -hz);
        }
    }
}
