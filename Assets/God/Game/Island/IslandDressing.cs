using System.Collections.Generic;
using UnityEngine;
using Terrain = God.Sim.Terrain;

namespace God.Game
{
    /// <summary>One scattered prop of the island (a tree, a crop tuft, a peak...). Placement only; the mesh is made from it.</summary>
    public struct IslandItem
    {
        public Vector3 Pos;
        public float Yaw;
        public float Scale;
        /// <summary>Footprint radius (m), for culling round buildings and roads.</summary>
        public float Radius;
        /// <summary>0..1: inside a build circle only the items with a low Keep stay, so the dressing goes light there.</summary>
        public float Keep;
        public IslandItem.Kinds Kind;
        public byte Variant;
        public short Hex;
        /// <summary>Tall items (peaks, mounds) that never stand inside a build circle.</summary>
        public bool Bulky;
        public int Seed;

        public enum Kinds : byte { Conifer, Broadleaf, Bush, Crop, Hedge, Haystack, Mound, ClayPit, Peak, Rock, Scrub, DryGrass, DeadTree }
    }

    /// <summary>
    /// P4a: scatters the props of every terrain across its warped region of the <see cref="IslandField"/> (no centred
    /// circles or rings) and turns them into meshes. Richer hexes (more dice pips) get denser and bigger dressing. A
    /// ragged clearing is left at each hex centre for the dice piles. Deterministic from the field (board + seed).
    /// </summary>
    public static class IslandDressing
    {
        public const float Spacing = 2f;

        /// <summary>All items of the island, in a fixed order (row by row of the candidate grid).</summary>
        public static List<IslandItem> Scatter(IslandField f)
        {
            var items = new List<IslandItem>();
            var scratch = new float[f.HexCount];
            float ext = BoardSpace.BoardRadius + 60f;
            int n = Mathf.CeilToInt(ext * 2f / Spacing);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    float x = -ext + (i + f.Hash01(i * 4099 + j, 11)) * Spacing;
                    float z = -ext + (j + f.Hash01(i * 4099 + j, 12)) * Spacing;
                    float sdf = f.BoardSdf(x, z);
                    if (sdf > 45f) continue;
                    float coast = f.Coast(x, z);
                    if (coast < f.BeachWidth(x, z) + 2.5f) continue;
                    int h = f.Owner(x, z, out int second, out float margin);
                    var t = f.TerrainOf(h);
                    float fade = t == f.TerrainOf(second) ? 1f : Mathf.SmoothStep(0f, 1f, (margin - 1f) / 9f);
                    float rim = 1f - Mathf.SmoothStep(0f, 1f, sdf / 38f);
                    float cd = f.ClearingDistance(h, x, z, out float cr);
                    if (cd < cr) continue;
                    float clear = Mathf.SmoothStep(0f, 1f, (cd - cr) / 7f);
                    int pips = f.PipsOf(h);
                    float dens = t == Terrain.Desert ? 0.75f : 0.38f + 0.17f * pips;
                    float size = t == Terrain.Desert ? 0.9f : 0.70f + 0.10f * pips;
                    float r = f.Hash01(i * 4099 + j, 13);
                    float s = f.Hash01(i * 4099 + j, 14);
                    float k = fade * rim * clear;
                    var it = new IslandItem { Hex = (short)h, Yaw = s * 360f, Keep = f.Hash01(i * 4099 + j, 15), Seed = i * 4099 + j };
                    bool add = false;
                    switch (t)
                    {
                        case Terrain.Forest:
                        {
                            float cluster = Mathf.SmoothStep(0f, 1f, (f.Fbm(x, z, 8, 1f / 45f) - 0.36f) / 0.24f);
                            float p = 0.30f * dens * cluster * k;
                            if (r < p) { it.Kind = s < 0.68f ? IslandItem.Kinds.Conifer : IslandItem.Kinds.Broadleaf; it.Scale = size * (0.8f + 0.45f * f.Hash01(it.Seed, 16)); it.Radius = 2.2f * it.Scale; add = true; }
                            else if (r < p + 0.012f * dens * k) { it.Kind = IslandItem.Kinds.Bush; it.Scale = 0.8f + 0.5f * s; it.Radius = 1.2f; add = true; }
                            break;
                        }
                        case Terrain.Fields:
                        {
                            int patch = f.FieldPatch(h, x, z, out float edge, out float yaw);
                            if (edge < 1.1f)
                            {
                                if (f.Noise(x, z, 5, 1f / 28f) > 0.42f && r < 0.65f * k) { it.Kind = IslandItem.Kinds.Hedge; it.Scale = 0.8f + 0.5f * s; it.Radius = 1.2f; it.Yaw = yaw; add = true; }
                            }
                            else if (edge > 1.8f && (patch == 0 || patch == 1 || patch == 3))
                            {
                                if (r < 0.60f * dens * k) { it.Kind = IslandItem.Kinds.Crop; it.Variant = (byte)patch; it.Scale = size * (0.85f + 0.3f * s); it.Yaw = yaw; it.Radius = 0.9f; add = true; }
                            }
                            else if (patch == 4 && r < 0.004f * k) { it.Kind = IslandItem.Kinds.Haystack; it.Scale = 0.9f + 0.4f * s; it.Radius = 1.6f; add = true; }
                            else if (patch == 4 && r < 0.010f * dens * k) { it.Kind = IslandItem.Kinds.Bush; it.Scale = 0.7f + 0.4f * s; it.Radius = 1.0f; add = true; }
                            break;
                        }
                        case Terrain.Hills:
                        {
                            if (r < 0.013f * dens * k * Mathf.SmoothStep(0f, 1f, (cd - cr) / 20f)) { it.Kind = IslandItem.Kinds.Mound; it.Scale = size * (0.7f + 0.6f * s); it.Radius = 5f * it.Scale; it.Bulky = true; add = true; }
                            else if (r < 0.022f * dens * k && cd > cr + 6f) { it.Kind = IslandItem.Kinds.ClayPit; it.Scale = size * (0.8f + 0.5f * s); it.Radius = 5f * it.Scale; add = true; }
                            else if (r < 0.045f * dens * k) { it.Kind = IslandItem.Kinds.Bush; it.Scale = 0.7f + 0.5f * s; it.Radius = 1.1f; add = true; }
                            else if (r < 0.051f * dens * k) { it.Kind = IslandItem.Kinds.Rock; it.Scale = 0.7f + 0.6f * s; it.Radius = 1f; add = true; }
                            break;
                        }
                        case Terrain.Mountain:
                        {
                            float ridge = f.Ridge(x, z);
                            // The piles sit in a pass that runs along the ranges, open at both ends, so the peaks never ring them.
                            float lobe = f.Noise(Mathf.Atan2(z - f.Centre(h).y, x - f.Centre(h).x) * 9f + h * 31f, h * 17f, 6, 1f / 6f);
                            float pass = f.PassDistance(h, x, z);
                            float valley = Mathf.SmoothStep(0f, 1f, (pass - 7f - 6f * lobe) / 14f) * Mathf.SmoothStep(0f, 1f, (cd - cr * 0.7f) / 8f);
                            float pr = (0.025f + 0.09f * Mathf.SmoothStep(0f, 1f, (ridge - 0.10f) / 0.6f)) * dens * dens * fade * rim * valley; // squared: peaks carry the richness strongly
                            if (r < pr) { it.Kind = IslandItem.Kinds.Peak; it.Scale = size * (0.65f + 0.6f * s) * Mathf.Lerp(0.55f, 1f, Mathf.SmoothStep(0f, 1f, (cd - cr) / 30f)); it.Radius = 6f * it.Scale; it.Bulky = true; add = true; }
                            else if (r < pr + 0.035f * dens * k) { it.Kind = IslandItem.Kinds.Rock; it.Scale = 0.8f + 1.0f * s; it.Radius = 1.2f; add = true; }
                            else if (r < pr + 0.030f * dens * k * (1f - ridge)) { it.Kind = IslandItem.Kinds.Conifer; it.Scale = 0.7f + 0.3f * s; it.Radius = 1.6f; add = true; }
                            break;
                        }
                        case Terrain.Desert:
                        {
                            if (r < 0.020f * k) { it.Kind = IslandItem.Kinds.Scrub; it.Scale = 0.7f + 0.5f * s; it.Radius = 1f; add = true; }
                            else if (r < 0.045f * k) { it.Kind = IslandItem.Kinds.DryGrass; it.Scale = 0.7f + 0.5f * s; it.Radius = 0.6f; add = true; }
                            else if (r < 0.050f * k) { it.Kind = IslandItem.Kinds.Rock; it.Scale = 0.6f + 0.6f * s; it.Radius = 0.9f; add = true; }
                            else if (r < 0.052f * k) { it.Kind = IslandItem.Kinds.DeadTree; it.Scale = 0.8f + 0.4f * s; it.Radius = 1.2f; add = true; }
                            break;
                        }
                    }
                    if (!add) continue;
                    it.Pos = new Vector3(x, f.Height(x, z, scratch), z);
                    items.Add(it);
                }
            return items;
        }

        /// <summary>An order- and value-sensitive hash of the placement (for the determinism test).</summary>
        public static long PlacementHash(List<IslandItem> items)
        {
            unchecked
            {
                long h = 1469598103934665603L;
                foreach (var it in items)
                {
                    h = (h ^ Mathf.RoundToInt(it.Pos.x * 100f)) * 1099511628211L;
                    h = (h ^ Mathf.RoundToInt(it.Pos.y * 100f)) * 1099511628211L;
                    h = (h ^ Mathf.RoundToInt(it.Pos.z * 100f)) * 1099511628211L;
                    h = (h ^ (int)it.Kind) * 1099511628211L;
                    h = (h ^ Mathf.RoundToInt(it.Scale * 1000f)) * 1099511628211L;
                }
                return h;
            }
        }

        // ------------------------------------------------------------------ meshes

        public static void Emit(MeshBuilder mb, in IslandItem it)
        {
            var rnd = new PropRandom(it.Seed * 7 + (int)it.Kind);
            var p = it.Pos; float s = it.Scale;
            switch (it.Kind)
            {
                case IslandItem.Kinds.Conifer:
                {
                    mb.Cylinder(p - Vector3.up * 0.3f, 0.28f * s, 0.2f * s, 2.0f * s, 5, new Color(0.36f, 0.25f, 0.15f), false);
                    var g = Tint(new Color(0.14f, 0.33f, 0.15f), rnd, 0.15f);
                    float w = 2.1f * s * rnd.Range(0.85f, 1.15f);
                    mb.Cone(p + Vector3.up * 1.3f * s, w, 3.6f * s, 6, g);
                    mb.Cone(p + Vector3.up * 3.2f * s, w * 0.72f, 3.4f * s, 6, g * 1.12f);
                    break;
                }
                case IslandItem.Kinds.Broadleaf:
                {
                    mb.Cylinder(p - Vector3.up * 0.3f, 0.32f * s, 0.24f * s, 2.6f * s, 5, new Color(0.40f, 0.28f, 0.17f), false);
                    var g = Tint(new Color(0.26f, 0.44f, 0.18f), rnd, 0.15f);
                    Blob(mb, p + Vector3.up * 3.6f * s, new Vector3(2.4f, 2.0f, 2.4f) * s, g, rnd, it.Yaw);
                    break;
                }
                case IslandItem.Kinds.Bush:
                    Blob(mb, p + Vector3.up * 0.5f * s, new Vector3(1.1f, 0.8f, 1.1f) * s, Tint(new Color(0.24f, 0.40f, 0.17f), rnd, 0.15f), rnd, it.Yaw);
                    break;
                case IslandItem.Kinds.Hedge:
                    Blob(mb, p + Vector3.up * 0.55f * s, new Vector3(1.0f, 0.75f, 1.5f) * s, Tint(new Color(0.20f, 0.36f, 0.15f), rnd, 0.1f), rnd, it.Yaw);
                    break;
                case IslandItem.Kinds.Crop:
                {
                    Color c = it.Variant == 0 ? new Color(0.88f, 0.72f, 0.28f) : it.Variant == 1 ? new Color(0.40f, 0.62f, 0.22f) : new Color(0.95f, 0.85f, 0.25f);
                    c = Tint(c, rnd, 0.08f);
                    float hgt = (it.Variant == 1 ? 0.55f : 0.9f) * s;
                    mb.Box(p + Vector3.up * (hgt * 0.5f - 0.1f), new Vector3(0.7f, hgt, 1.5f), c, it.Yaw);
                    break;
                }
                case IslandItem.Kinds.Haystack:
                    mb.Cylinder(p - Vector3.up * 0.2f, 1.2f * s, 1.0f * s, 1.3f * s, 7, new Color(0.84f, 0.72f, 0.36f));
                    mb.Cone(p + Vector3.up * 1.1f * s, 1.05f * s, 1.0f * s, 7, new Color(0.78f, 0.66f, 0.32f));
                    break;
                case IslandItem.Kinds.Mound:
                {
                    Mound(mb, p, s, it.Yaw, rnd);
                    break;
                }
                case IslandItem.Kinds.ClayPit:
                    ClayPit(mb, p, s, it.Yaw, rnd);
                    break;
                case IslandItem.Kinds.Peak:
                    Peak(mb, p, s, it.Yaw, rnd);
                    break;
                case IslandItem.Kinds.Rock:
                    mb.Rock(p - Vector3.up * 0.2f, 0.9f * s, Tint(new Color(0.55f, 0.53f, 0.50f), rnd, 0.1f), rnd.Next);
                    break;
                case IslandItem.Kinds.Scrub:
                    Blob(mb, p + Vector3.up * 0.4f * s, new Vector3(0.9f, 0.6f, 0.9f) * s, Tint(new Color(0.50f, 0.50f, 0.28f), rnd, 0.12f), rnd, it.Yaw);
                    break;
                case IslandItem.Kinds.DryGrass:
                {
                    var c = Tint(new Color(0.74f, 0.64f, 0.38f), rnd, 0.1f);
                    for (int q = 0; q < 3; q++)
                    {
                        var o = new Vector3(rnd.Range(-0.5f, 0.5f), 0, rnd.Range(-0.5f, 0.5f)) * s;
                        mb.Cone(p + o - Vector3.up * 0.1f, 0.25f * s, rnd.Range(0.7f, 1.2f) * s, 4, c);
                    }
                    break;
                }
                case IslandItem.Kinds.DeadTree:
                {
                    var c = new Color(0.45f, 0.38f, 0.30f);
                    mb.Cylinder(p - Vector3.up * 0.2f, 0.25f * s, 0.12f * s, 3.2f * s, 5, c, false);
                    var q = Quaternion.Euler(0, it.Yaw, 0);
                    mb.Box(p + Vector3.up * 2.2f * s + q * Vector3.right * 0.6f * s, new Vector3(1.2f, 0.18f, 0.18f) * s, c, it.Yaw);
                    mb.Box(p + Vector3.up * 2.7f * s - q * Vector3.right * 0.5f * s, new Vector3(1.0f, 0.16f, 0.16f) * s, c, it.Yaw);
                    break;
                }
            }
        }

        private static Color Tint(Color c, PropRandom rnd, float amount)
        {
            c *= 1f + (rnd.Next() - 0.5f) * 2f * amount; c.a = 1f; return c;
        }

        /// <summary>A rough low-poly lump (ellipsoid with jittered points): canopies, bushes, mounds.</summary>
        public static void Blob(MeshBuilder mb, Vector3 c, Vector3 half, Color col, PropRandom rnd, float yaw)
        {
            const int N = 7;
            var q = Quaternion.Euler(0, yaw, 0);
            var up = new Vector3[N]; var lo = new Vector3[N];
            for (int k = 0; k < N; k++)
            {
                float a = (k + rnd.Range(-0.2f, 0.2f)) * Mathf.PI * 2f / N;
                float r1 = rnd.Range(0.85f, 1.1f), r2 = rnd.Range(0.85f, 1.05f);
                up[k] = c + q * new Vector3(Mathf.Cos(a) * half.x * r1 * 0.8f, half.y * 0.45f, Mathf.Sin(a) * half.z * r1 * 0.8f);
                lo[k] = c + q * new Vector3(Mathf.Cos(a + 0.45f) * half.x * r2, -half.y * 0.25f, Mathf.Sin(a + 0.45f) * half.z * r2);
            }
            var top = c + new Vector3(rnd.Range(-0.15f, 0.15f) * half.x, half.y, rnd.Range(-0.15f, 0.15f) * half.z);
            var bot = c - Vector3.up * half.y * 0.8f;
            for (int k = 0; k < N; k++)
            {
                int k1 = (k + 1) % N;
                mb.Tri(top, up[k1], up[k], col * 1.08f);
                mb.Tri(up[k], up[k1], lo[k], col);
                mb.Tri(up[k1], lo[k1], lo[k], col * 0.95f);
                mb.Tri(lo[k], lo[k1], bot, col * 0.8f);
            }
        }

        /// <summary>A jagged rock peak: a jittered base ring, a narrower shoulder ring and an off-centre summit; lighter rock up high.</summary>
        private static void Peak(MeshBuilder mb, Vector3 p, float s, float yaw, PropRandom rnd)
        {
            int N = 5 + (int)(rnd.Next() * 3f);
            float R = 6.5f * s, H = R * rnd.Range(1.1f, 1.7f);
            var baseC = p - Vector3.up * 1.2f;
            var b = new Vector3[N]; var m = new Vector3[N];
            float stretch = rnd.Range(1.0f, 1.6f);
            var q = Quaternion.Euler(0, yaw, 0);
            for (int k = 0; k < N; k++)
            {
                float a = (k + rnd.Range(-0.3f, 0.3f)) * Mathf.PI * 2f / N;
                float r = R * rnd.Range(0.75f, 1.15f);
                b[k] = baseC + q * new Vector3(Mathf.Cos(a) * r * stretch, 0, Mathf.Sin(a) * r);
                float a2 = a + rnd.Range(-0.2f, 0.2f);
                m[k] = baseC + q * new Vector3(Mathf.Cos(a2) * r * stretch * 0.5f, H * rnd.Range(0.45f, 0.6f), Mathf.Sin(a2) * r * 0.5f);
            }
            var apex = baseC + q * new Vector3(rnd.Range(-0.25f, 0.25f) * R, H, rnd.Range(-0.2f, 0.2f) * R);
            var lowCol = new Color(0.47f, 0.45f, 0.42f) * rnd.Range(0.9f, 1.1f);
            var highCol = new Color(0.66f, 0.64f, 0.61f) * rnd.Range(0.92f, 1.06f);
            lowCol.a = highCol.a = 1f;
            for (int k = 0; k < N; k++)
            {
                int k1 = (k + 1) % N;
                float shade = rnd.Range(0.85f, 1.1f);
                mb.Quad(b[k], m[k], m[k1], b[k1], lowCol * shade);
                mb.Tri(m[k], apex, m[k1], Color.Lerp(lowCol, highCol, 0.8f) * shade);
            }
        }

        /// <summary>A low grassy hillock: a gentle jittered dome whose foot sinks into the ground, grass in the ground's own tone.</summary>
        private static void Mound(MeshBuilder mb, Vector3 p, float s, float yaw, PropRandom rnd)
        {
            const int N = 11;
            float[] rr = { 1.05f, 0.78f, 0.48f, 0.2f };
            float[] hh = { -0.35f, 0.42f, 0.82f, 1f };
            float R = 6.5f * s, H = 1.7f * s * rnd.Range(0.8f, 1.2f), stretch = rnd.Range(1.0f, 1.5f);
            var q = Quaternion.Euler(0, yaw, 0);
            var ring = new Vector3[rr.Length, N];
            for (int k = 0; k < N; k++)
            {
                float a = (k + rnd.Range(-0.25f, 0.25f)) * Mathf.PI * 2f / N;
                float j = rnd.Range(0.85f, 1.15f);
                for (int l = 0; l < rr.Length; l++)
                    ring[l, k] = p + q * new Vector3(Mathf.Cos(a) * R * rr[l] * j * stretch, H * hh[l], Mathf.Sin(a) * R * rr[l] * j);
            }
            var top = p + Vector3.up * H * 1.04f;
            var g = new Color(0.35f, 0.47f, 0.22f) * rnd.Range(0.95f, 1.05f); g.a = 1f;
            for (int k = 0; k < N; k++)
            {
                int k1 = (k + 1) % N;
                for (int l = 0; l < rr.Length - 1; l++)
                    mb.Quad(ring[l, k], ring[l + 1, k], ring[l + 1, k1], ring[l, k1], g * (0.94f + 0.03f * l));
                mb.Tri(ring[rr.Length - 1, k], top, ring[rr.Length - 1, k1], g * 1.04f);
            }
        }

        /// <summary>A small irregular clay pit with a spoil bank and a few heaps of dug clay.</summary>
        private static void ClayPit(MeshBuilder mb, Vector3 p, float s, float yaw, PropRandom rnd)
        {
            const int N = 9;
            var ring = new List<Vector3>(N); var inner = new List<Vector3>(N);
            float R = 3.4f * s, stretch = rnd.Range(1.1f, 1.7f);
            var q = Quaternion.Euler(0, yaw, 0);
            var off = q * new Vector3(rnd.Range(-0.3f, 0.3f), 0, rnd.Range(-0.3f, 0.3f)) * R;
            for (int k = 0; k < N; k++)
            {
                float a = (k + rnd.Range(-0.3f, 0.3f)) * Mathf.PI * 2f / N;
                float r = R * rnd.Range(0.7f, 1.15f);
                ring.Add(p + Vector3.up * 0.12f + q * new Vector3(Mathf.Cos(a) * r * stretch, 0, Mathf.Sin(a) * r));
                inner.Add(p + off + Vector3.up * 0.16f + q * new Vector3(Mathf.Cos(a) * r * stretch * rnd.Range(0.35f, 0.55f), 0, Mathf.Sin(a) * r * rnd.Range(0.35f, 0.55f)));
            }
            mb.FanUp(p + Vector3.up * 0.12f, ring, new Color(0.70f, 0.42f, 0.25f));
            mb.FanUp(p + off + Vector3.up * 0.16f, inner, new Color(0.58f, 0.32f, 0.20f));
            // Spoil bank on one side and a few heaps.
            int heaps = 2 + (int)(rnd.Next() * 3f);
            for (int k = 0; k < heaps; k++)
            {
                float a = yaw * Mathf.Deg2Rad + rnd.Range(-1.2f, 1.2f);
                var hp = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * R * stretch * rnd.Range(1.1f, 1.5f);
                Blob(mb, hp, new Vector3(1.2f, 0.9f, 1.0f) * s * rnd.Range(0.7f, 1.2f), new Color(0.74f, 0.45f, 0.27f) * rnd.Range(0.9f, 1.1f), rnd, rnd.Range(0f, 360f));
            }
        }
    }
}
