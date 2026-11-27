using System.Collections.Generic;
using UnityEngine;
using God.Sim;
using Terrain = God.Sim.Terrain;

namespace God.Game
{
    /// <summary>
    /// The island's ground as one continuous surface (owner brief 21 Nov, package W): every hex tile is a fine lattice
    /// whose vertex colours come from <see cref="ColourAt"/>, a soft blend of the neighbouring biomes (Gaussian weights
    /// round each hex centre, the sample point warped by low noise so the borders wander), so no tile shows a hard
    /// outline or a flat colour. Gentle relief is given by the vertex normals only (<see cref="Relief"/>): the board
    /// stays flat at <see cref="BoardSpace.GroundY"/> for everything that stands or walks on it. The rim beyond the walk
    /// bounds may rise (<see cref="RimHeight"/>). Pure view code; colours and shapes are original.
    /// </summary>
    public static class IslandGround
    {
        /// <summary>Lattice subdivisions per hex triangle (centre, corner, next corner).</summary>
        public const int Subdiv = 10;
        /// <summary>Width (as a share of the hex circumradius) of the Gaussian that weights a hex's colour.</summary>
        public const float BlendSigma = 0.62f;
        /// <summary>How far (m) the colour lookup point is pushed by noise: the biome borders wander by about this much.</summary>
        public const float Warp = 14f;

        /// <summary>Muted ground colour of a biome. Strong colour is left to the centrepieces (crops, flocks, pits, quarries).</summary>
        public static Color Biome(Terrain t)
        {
            switch (t)
            {
                case Terrain.Forest: return new Color(0.21f, 0.37f, 0.17f);
                case Terrain.Pasture: return new Color(0.38f, 0.56f, 0.24f);
                case Terrain.Fields: return new Color(0.50f, 0.52f, 0.27f);
                case Terrain.Hills: return new Color(0.54f, 0.44f, 0.28f);
                case Terrain.Mountain: return new Color(0.49f, 0.47f, 0.43f);
                case Terrain.Desert: return new Color(0.74f, 0.66f, 0.43f);
                default: return new Color(0.39f, 0.53f, 0.26f); // Meadow
            }
        }

        /// <summary>The grass of the island rim beyond the board.</summary>
        public static readonly Color Rim = new Color(0.33f, 0.49f, 0.24f);
        public static readonly Color Sand = new Color(0.84f, 0.78f, 0.58f), WetSand = new Color(0.64f, 0.60f, 0.46f);

        /// <summary>Relief amplitude (m, shading only) of a biome.</summary>
        private static float Amp(Terrain t)
        {
            switch (t)
            {
                case Terrain.Hills: return 7f;
                case Terrain.Mountain: return 9f;
                case Terrain.Forest: return 3f;
                case Terrain.Fields: return 1.2f;
                default: return 2f;
            }
        }
        private const float RimAmp = 4f, RimWeight = 0.005f;

        private static float Fbm(float x, float z)
        {
            return Mathf.PerlinNoise(x * 0.012f + 17.3f, z * 0.012f + 5.1f) * 0.6f
                 + Mathf.PerlinNoise(x * 0.031f + 3.7f, z * 0.031f + 41.9f) * 0.3f
                 + Mathf.PerlinNoise(x * 0.083f + 61.1f, z * 0.083f + 8.3f) * 0.1f;
        }

        /// <summary>Blend weights at a ground point: colour, relief amplitude and the board share (1 deep in the board, 0 out on the rim).</summary>
        public static void Sample(Board board, float x, float z, out Color col, out float amp, out float boardShare)
        {
            var t = board.Topology;
            float R = BoardSpace.HexSize, sig2 = BlendSigma * R * BlendSigma * R;
            float wx = x + (Mathf.PerlinNoise(x * 0.011f + 3.1f, z * 0.011f + 7.7f) - 0.5f) * 2f * Warp;
            float wz = z + (Mathf.PerlinNoise(x * 0.011f + 11.3f, z * 0.011f + 1.9f) - 0.5f) * 2f * Warp;
            float sw = 0f, r = 0f, g = 0f, b = 0f, a = 0f;
            float reach2 = 4f * R * R;
            for (int h = 0; h < t.HexCount; h++)
            {
                float dx = (float)t.HexX(h) - wx, dz = (float)t.HexZ(h) - wz;
                float d2 = dx * dx + dz * dz;
                if (d2 > reach2) continue;
                float w = Mathf.Exp(-d2 / sig2);
                var c = Biome(board.HexTerrain[h]);
                r += c.r * w; g += c.g * w; b += c.b * w; a += Amp(board.HexTerrain[h]) * w; sw += w;
            }
            boardShare = sw / (sw + RimWeight);
            r += Rim.r * RimWeight; g += Rim.g * RimWeight; b += Rim.b * RimWeight; a += RimAmp * RimWeight; sw += RimWeight;
            col = new Color(r / sw, g / sw, b / sw);
            amp = a / sw;
            // Low-frequency light and dark patches so a biome is never one flat colour.
            float n = Mathf.PerlinNoise(x * 0.027f + 9.9f, z * 0.027f + 2.2f);
            float m = Mathf.PerlinNoise(x * 0.09f + 1.3f, z * 0.09f + 4.4f);
            col *= 0.90f + 0.16f * n + 0.06f * (m - 0.5f);
            col.a = 1f;
        }

        /// <summary>Blended ground colour at a ground point (board hexes and the rim).</summary>
        public static Color ColourAt(Board board, float x, float z) { Sample(board, x, z, out var c, out _, out _); return c; }

        /// <summary>Shading-only relief normal at a point with a given relief amplitude (m).</summary>
        public static Vector3 Relief(float x, float z, float amp)
        {
            const float e = 2.5f;
            float hx = (Fbm(x + e, z) - Fbm(x - e, z)) * amp / (2f * e);
            float hz = (Fbm(x, z + e) - Fbm(x, z - e)) * amp / (2f * e);
            return new Vector3(-hx, 1f, -hz).normalized;
        }

        // ---------------------------------------------------------------- coast and rim

        /// <summary>Radius of the coast line at an angle (radians, counter-clockwise from +X): the island's ragged edge.</summary>
        public static float CoastRadius(float angle)
        {
            float u = angle / (Mathf.PI * 2f) * 120f; // same wobble as the old 120-segment coast
            return BoardSpace.IslandRadius * (1f + 0.035f * Mathf.Sin(u * 0.37f) + 0.02f * Mathf.Sin(u * 1.3f + 1f));
        }

        /// <summary>Circles (x, z, radius) where the rim stays level at its height at the circle's centre: lakes, rivers, landmark feet. Set by <see cref="IslandLandmarks"/> before the island mesh is built.</summary>
        public static readonly List<Vector3> FlatZones = new List<Vector3>();
        private static readonly List<float> FlatBase = new List<float>();

        /// <summary>Adds a level zone (keeps its raw height at the centre).</summary>
        public static void AddFlatZone(Vector3 circle) { FlatZones.Add(circle); FlatBase.Add(RimHeightRaw(circle.x, circle.y)); }
        /// <summary>Adds a level zone at a given height (jetties: the board level).</summary>
        public static void AddFlatZone(Vector3 circle, float height) { FlatZones.Add(circle); FlatBase.Add(height); }
        public static void ClearFlatZones() { FlatZones.Clear(); FlatBase.Clear(); RiverMouthAngle = float.NaN; BeachAngles.Clear(); }

        /// <summary>Angles (radians) where the coast must be a beach (port jetties run out there).</summary>
        public static readonly List<float> BeachAngles = new List<float>();

        /// <summary>Angle (radians) where the lake's river leaves the island: the coast there is always a cliff (the waterfall).</summary>
        public static float RiverMouthAngle = float.NaN;
        /// <summary>Height (m) of the coastal cliffs above the board level where the coast is cliff.</summary>
        public const float CliffTop = 14f;

        /// <summary>0 for a sandy beach, 1 for a cliff coast, at an angle (radians); smooth and periodic.</summary>
        public static float Cliffiness(float angle)
        {
            float n = Mathf.PerlinNoise(Mathf.Cos(angle) * 1.6f + 20f, Mathf.Sin(angle) * 1.6f + 20f);
            float c = Mathf.SmoothStep(0f, 1f, (n - 0.42f) / 0.16f);
            if (!float.IsNaN(RiverMouthAngle))
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, RiverMouthAngle * Mathf.Rad2Deg));
                c = Mathf.Max(c, 1f - Mathf.SmoothStep(0f, 1f, (d - 6f) / 8f));
            }
            foreach (float b in BeachAngles)
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, b * Mathf.Rad2Deg));
                c = Mathf.Min(c, Mathf.SmoothStep(0f, 1f, (d - 7f) / 8f));
            }
            return c;
        }

        /// <summary>Raw rim height without the level zones: rolling ground plus the rise to the cliff tops.</summary>
        public static float RimHeightRaw(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float inner = BoardSpace.BoardRadius + 10f;
            if (r <= inner) return 0f;
            float ang = Mathf.Atan2(z, x);
            float coast = CoastRadius(ang);
            float up = Mathf.SmoothStep(0f, 1f, (r - inner) / 30f);
            float down = Mathf.SmoothStep(0f, 1f, (coast - 14f - r) / 18f);
            float cl = Cliffiness(ang);
            float rise = cl * CliffTop * Mathf.SmoothStep(0f, 1f, (r - inner) / 40f);
            return Fbm(x * 1.3f, z * 1.3f) * 7f * up * Mathf.Lerp(Mathf.Clamp01(down), 1f, cl) + rise;
        }

        /// <summary>Real height of the rim (m): zero inside the walk bounds, rolling ground and cliff tops beyond, level in the flat zones.</summary>
        public static float RimHeight(float x, float z)
        {
            float h = RimHeightRaw(x, z);
            for (int i = 0; i < FlatZones.Count; i++)
            {
                var f = FlatZones[i];
                float dx = x - f.x, dz = z - f.y;
                float d = Mathf.Sqrt(dx * dx + dz * dz) - f.z;
                if (d < 20f) h = Mathf.Lerp(FlatBase[i], h, Mathf.SmoothStep(0f, 1f, d / 20f));
            }
            return h;
        }

        // ---------------------------------------------------------------- meshes

        /// <summary>
        /// The ground tile of hex h: six triangles (centre, corner, next corner), each a lattice of
        /// <see cref="Subdiv"/> rows with per-vertex blended colour and relief normals, plus a short skirt. Flat at y = 0.
        /// </summary>
        public static Mesh HexTile(Board board, int h, string name)
        {
            var t = board.Topology;
            var centre = BoardSpace.Hex(t, h);
            var corners = new List<Vector3>();
            foreach (int v in t.HexCorners(h)) corners.Add(BoardSpace.Vertex(t, v));
            corners.Sort((p, q) => Mathf.Atan2(p.z - centre.z, p.x - centre.x).CompareTo(Mathf.Atan2(q.z - centre.z, q.x - centre.x)));
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var cols = new List<Color>(); var tris = new List<int>();
            int N = Subdiv;
            for (int k = 0; k < corners.Count; k++)
            {
                var A = centre; var B = corners[k]; var C = corners[(k + 1) % corners.Count];
                int baseIdx = verts.Count;
                // Row i from the centre (0) to the edge (N); row i has i + 1 points from B side to C side.
                for (int i = 0; i <= N; i++)
                    for (int j = 0; j <= i; j++)
                    {
                        float u = i / (float)N, w = i == 0 ? 0f : j / (float)i;
                        var p = A + (B - A) * u * (1f - w) + (C - A) * u * w;
                        Sample(board, p.x, p.z, out var col, out float amp, out _);
                        verts.Add(new Vector3(p.x, BoardSpace.GroundY, p.z));
                        norms.Add(Relief(p.x, p.z, amp));
                        cols.Add(col);
                    }
                int Idx(int i, int j) => baseIdx + i * (i + 1) / 2 + j;
                for (int i = 0; i < N; i++)
                    for (int j = 0; j <= i; j++)
                    {
                        // Counter-clockwise from above is (centre, C, B) in the old fan: keep that winding so faces point up.
                        AddUp(verts, tris, Idx(i, j), Idx(i + 1, j), Idx(i + 1, j + 1));
                        if (j < i) AddUp(verts, tris, Idx(i, j), Idx(i + 1, j + 1), Idx(i, j + 1));
                    }
                // Skirt below the outer edge (only seen at the board rim).
                int s0 = verts.Count;
                var b0 = verts[Idx(N, 0)]; var c0 = verts[Idx(N, N)];
                var dark = cols[Idx(N, 0)] * 0.7f; dark.a = 1f;
                verts.Add(b0); verts.Add(c0); verts.Add(c0 + Vector3.down * 0.4f); verts.Add(b0 + Vector3.down * 0.4f);
                var outN = ((b0 + c0) * 0.5f - centre); outN.y = 0; outN.Normalize();
                for (int q = 0; q < 4; q++) { norms.Add(outN); cols.Add(dark); }
                bool outward = Vector3.Dot(Vector3.Cross(verts[s0 + 1] - verts[s0], verts[s0 + 2] - verts[s0]), outN) >= 0f;
                if (outward) { tris.Add(s0); tris.Add(s0 + 1); tris.Add(s0 + 2); tris.Add(s0); tris.Add(s0 + 2); tris.Add(s0 + 3); }
                else { tris.Add(s0); tris.Add(s0 + 2); tris.Add(s0 + 1); tris.Add(s0); tris.Add(s0 + 3); tris.Add(s0 + 2); }
            }
            var m = new Mesh { name = name };
            m.SetVertices(verts); m.SetNormals(norms); m.SetColors(cols); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>True when a point and six points round it at margin (m) all lie on board hexes.</summary>
        private static bool DeepInBoard(BoardTopology t, Vector3 p, float margin)
        {
            if (t.HexAt(p.x, p.z) < 0) return false;
            for (int k = 0; k < 6; k++)
            {
                var q = p + BoardSpace.DirOf(k * 60f + 30f) * margin;
                if (t.HexAt(q.x, q.z) < 0) return false;
            }
            return true;
        }

        /// <summary>Adds a triangle so that it faces up (+Y).</summary>
        private static void AddUp(List<Vector3> v, List<int> tris, int a, int b, int c)
        {
            var n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (n.y >= 0f) { tris.Add(a); tris.Add(b); tris.Add(c); }
            else { tris.Add(a); tris.Add(c); tris.Add(b); }
        }

        public static readonly Color CliffRock = new Color(0.55f, 0.49f, 0.42f), Shallows = new Color(0.30f, 0.46f, 0.42f);
        /// <summary>Distances (m) from the coast line of the rings outside the land: beach or cliff top, the cliff face, the sea floor.</summary>
        private static readonly float[] Outer = { -11f, -6f, -3f, -1.5f, 2f, 6f, 12f, 24f };

        /// <summary>
        /// The island ground round the board: a polar grid from inside the board edge out past the coast, coloured with
        /// the same blend (so the outer tiles fade into the rim). The coast is a sandy beach or a rock cliff by angle
        /// (<see cref="Cliffiness"/>); the rim beyond the walk bounds rolls and rises to the cliff tops.
        /// </summary>
        public static Mesh IslandMesh(Board board, string name)
        {
            const int seg = 200;
            float inner = BoardSpace.BoardRadius * 0.78f;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var cols = new List<Color>(); var tris = new List<int>();
            var land = new List<bool>();
            // Centre vertex under the board (hidden by the tiles).
            verts.Add(new Vector3(0, -0.8f, 0)); norms.Add(Vector3.up); cols.Add(Rim); land.Add(true);
            const int landRings = 20;
            int rings = landRings + Outer.Length;
            for (int i = 0; i < seg; i++)
            {
                float ang = i * Mathf.PI * 2f / seg;
                float coast = CoastRadius(ang);
                float cl = Cliffiness(ang);
                var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                for (int k = 0; k < rings; k++)
                {
                    float r = k < landRings ? Mathf.Lerp(inner, coast - 16f, k / (float)(landRings - 1)) : coast + Outer[k - landRings];
                    var p = dir * r;
                    float y; Color col; float amp = RimAmp;
                    if (k < landRings)
                    {
                        Sample(board, p.x, p.z, out col, out amp, out _);
                        // Under the board the ground drops out of sight (no depth fight with the tiles from the god camera).
                        // Only well inside it, so no triangle crossing the board's edge dips under the sea.
                        y = DeepInBoard(board.Topology, p, 26f) ? -0.8f : board.Topology.HexAt(p.x, p.z) >= 0 ? -0.22f : RimHeight(p.x, p.z) - 0.05f;
                        land.Add(true);
                    }
                    else
                    {
                        var q = dir * Mathf.Min(r, coast - 3f);
                        float top = RimHeight(q.x, q.z);
                        Sample(board, q.x, q.z, out var grass, out amp, out _);
                        // Beach profile: down to the waterline at the coast, then the sea floor.
                        float yb = r <= coast - 3f ? Mathf.Lerp(top, 0.05f, Mathf.InverseLerp(coast - 16f, coast - 3f, r)) : Mathf.Lerp(0.05f, -5f, Mathf.InverseLerp(coast - 3f, coast + 24f, r));
                        Color cb = r < coast - 8f ? Color.Lerp(grass, Sand, Mathf.InverseLerp(coast - 16f, coast - 8f, r))
                                 : r <= coast ? Sand : r <= coast + 6f ? WetSand : Shallows;
                        // Cliff profile: the land holds its height to the edge, then a rock face into the sea.
                        float yc = r <= coast - 1.5f ? top : r <= coast + 2f ? -2.5f : Mathf.Lerp(-2.5f, -6f, Mathf.InverseLerp(coast + 2f, coast + 24f, r));
                        Color cc = r <= coast - 3f ? grass : r <= coast - 1.5f ? Color.Lerp(grass, CliffRock, 0.6f) : CliffRock * (r <= coast + 2f ? 0.85f : 0.6f);
                        y = Mathf.Lerp(yb, yc, cl);
                        col = Color.Lerp(cb, cc, cl);
                        amp = 1.5f;
                        land.Add(false);
                    }
                    col.a = 1f;
                    verts.Add(new Vector3(p.x, y, p.z)); norms.Add(Relief(p.x, p.z, amp)); cols.Add(col);
                }
            }
            int V(int i, int k) => 1 + (i % seg) * rings + k;
            for (int i = 0; i < seg; i++)
            {
                AddUp(verts, tris, 0, V(i, 0), V(i + 1, 0));
                for (int k = 0; k < rings - 1; k++)
                {
                    AddUp(verts, tris, V(i, k), V(i + 1, k), V(i + 1, k + 1));
                    AddUp(verts, tris, V(i, k), V(i + 1, k + 1), V(i, k + 1));
                }
            }
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(verts); m.SetColors(cols); m.SetTriangles(tris, 0);
            // Shape normals (slopes, cliff faces) with the shading-only relief on top.
            m.RecalculateNormals();
            var shape = new List<Vector3>(); m.GetNormals(shape);
            for (int i = 0; i < shape.Count; i++) shape[i] = (shape[i] + norms[i] - Vector3.up).normalized;
            m.SetNormals(shape);
            m.RecalculateBounds();
            return m;
        }

        // ---------------------------------------------------------------- the sea and the clouds

        /// <summary>Haze colour the far sea fades into (the board camera's background should match it).</summary>
        public static readonly Color Haze = new Color(0.70f, 0.79f, 0.86f);
        public static readonly Color DeepSea = new Color(0.12f, 0.32f, 0.50f), NearSea = new Color(0.18f, 0.46f, 0.60f);
        public const float SeaY = -0.3f, SeaRadius = 9000f;

        /// <summary>
        /// A round sea out to <see cref="SeaRadius"/>: turquoise round the island, deep blue, then haze towards the
        /// horizon, so no plane edge shows from an angled camera. Unlit vertex colour (it dims at night with the world tint).
        /// </summary>
        public static Mesh SeaMesh(string name)
        {
            float[] radii = { 0f, BoardSpace.IslandRadius + 10f, BoardSpace.IslandRadius + 60f, BoardSpace.IslandRadius + 220f, 1100f, 1800f, 2600f, 3600f, SeaRadius };
            Color[] cs = { NearSea, NearSea, Color.Lerp(NearSea, DeepSea, 0.5f), DeepSea, DeepSea, Color.Lerp(DeepSea, Haze, 0.35f), Color.Lerp(DeepSea, Haze, 0.7f), Haze, Haze };
            const int seg = 96;
            var verts = new List<Vector3>(); var cols = new List<Color>(); var tris = new List<int>(); var norms = new List<Vector3>();
            for (int k = 0; k < radii.Length; k++)
                for (int i = 0; i < seg; i++)
                {
                    float a = i * Mathf.PI * 2f / seg;
                    verts.Add(new Vector3(Mathf.Cos(a) * radii[k], SeaY, Mathf.Sin(a) * radii[k]));
                    cols.Add(cs[k]); norms.Add(Vector3.up);
                }
            for (int k = 0; k < radii.Length - 1; k++)
                for (int i = 0; i < seg; i++)
                {
                    int a = k * seg + i, b = k * seg + (i + 1) % seg, c = (k + 1) * seg + (i + 1) % seg, d = (k + 1) * seg + i;
                    AddUp(verts, tris, a, b, c); AddUp(verts, tris, a, c, d);
                }
            var m = new Mesh { name = name };
            m.SetVertices(verts); m.SetNormals(norms); m.SetColors(cols); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
