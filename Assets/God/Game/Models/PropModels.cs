using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P4b small models: construction sites (a frame and scaffold that fill in with progress), ware heaps at sites, raw
    /// piles at hex centres, rubble, and the target and defend standards. Local space, feet at y = 0. View code only.
    /// </summary>
    public static class PropModels
    {
        public const int SiteStages = 10;

        /// <summary>Half width and depth of a site's frame by size.</summary>
        public static Vector2 SiteHalf(BuildingSize s) => s == BuildingSize.Hq ? new Vector2(4.6f, 3.2f) : s == BuildingSize.Medium ? new Vector2(3.2f, 2.4f) : new Vector2(1.8f, 1.5f);
        public static float SiteWall(BuildingSize s) => s == BuildingSize.Hq ? 3.8f : s == BuildingSize.Medium ? 2.9f : 2.3f;

        public static Mesh SiteMesh(BuildingSize size, Faction f, int stage)
            => ModelKit.Cached("site/" + (int)size + "/" + (int)f + "/" + stage, mb => Site(mb, size, ModelStyle.Of(f), stage / (float)SiteStages));

        /// <summary>
        /// A construction site at progress k (0..1): pegs and a stone footing first; then the timber frame rises, wall
        /// panels fill in from the bottom, the scaffold climbs alongside; at the end the rafters and the first roof courses.
        /// </summary>
        public static void Site(MeshBuilder mb, BuildingSize size, ModelStyle st, float k)
        {
            var h = SiteHalf(size); float wall = SiteWall(size);
            float hw = h.x, hd = h.y;
            var raw = new Color(0.80f, 0.66f, 0.44f);
            // Dug earth inside, a course of rough footing stones round the edge.
            var rnd = new PropRandom((int)size * 31 + (st.Stone ? 7 : 3));
            mb.Box(new Vector3(0, -0.45f, 0), new Vector3(hw * 2f + 0.2f, 1.0f, hd * 2f + 0.2f), new Color(0.40f, 0.31f, 0.21f));
            var corners = new[] { new Vector3(-hw, 0, -hd), new Vector3(hw, 0, -hd), new Vector3(hw, 0, hd), new Vector3(-hw, 0, hd) };
            for (int i = 0; i < 4; i++)
            {
                var a = corners[i]; var b = corners[(i + 1) % 4];
                int n = Mathf.Max(2, Mathf.RoundToInt((b - a).magnitude / 0.75f));
                for (int j = 0; j < n; j++)
                {
                    var p = Vector3.Lerp(a, b, (j + 0.5f) / n);
                    mb.Box(p + Vector3.up * 0.12f, new Vector3(0.7f, 0.42f, 0.55f) * rnd.Range(0.85f, 1.1f), st.Plinth * rnd.Range(0.9f, 1.15f), ModelKit.Yaw(b - a) + rnd.Range(-8f, 8f));
                }
            }
            // Corner pegs with a string line.
            foreach (var c in corners) mb.Box(c + Vector3.up * 0.6f, new Vector3(0.14f, 1.2f, 0.14f), raw);
            for (int i = 0; i < 4; i++) ModelKit.Beam(mb, corners[i] + Vector3.up * 1.0f, corners[(i + 1) % 4] + Vector3.up * 1.0f, 0.025f, new Color(0.95f, 0.92f, 0.85f));
            // A few timbers laid out ready on the floor.
            for (int i = 0; i < 3; i++) mb.Box(new Vector3(-hw * 0.4f + i * 0.45f, 0.18f, -hd * 0.2f), new Vector3(0.3f, 0.22f, hd * 1.2f), raw * (0.9f + 0.05f * i), 6f * i);

            float post = Mathf.Clamp01((k - 0.12f) / 0.35f) * wall;
            if (post > 0.05f)
            {
                // Frame posts: corners and mid posts on the long sides.
                foreach (var c in corners) mb.Box(c + Vector3.up * post * 0.5f, new Vector3(0.26f, post, 0.26f), raw);
                foreach (float z in new[] { -hd, hd }) mb.Box(new Vector3(0, post * 0.5f, z), new Vector3(0.22f, post, 0.22f), raw);
                if (post >= wall - 0.01f)
                {
                    for (int i = 0; i < 4; i++) ModelKit.Beam(mb, corners[i] + Vector3.up * wall, corners[(i + 1) % 4] + Vector3.up * wall, 0.12f, raw * 0.95f);
                }
                // Diagonal braces.
                ModelKit.Beam(mb, corners[0] + Vector3.up * 0.2f, new Vector3(0, post * 0.9f, -hd), 0.07f, raw * 0.9f);
                ModelKit.Beam(mb, corners[1] + Vector3.up * 0.2f, new Vector3(0, post * 0.9f, -hd), 0.07f, raw * 0.9f);
            }
            // Wall panels fill in from the bottom (left open at the front so the inside reads).
            float fill = Mathf.Clamp01((k - 0.4f) / 0.4f) * wall;
            if (fill > 0.05f)
            {
                mb.Box(new Vector3(0, fill * 0.5f, -hd), new Vector3(hw * 2f, fill, 0.18f), st.Wall);
                mb.Box(new Vector3(-hw, fill * 0.5f, 0), new Vector3(0.18f, fill, hd * 2f), st.Wall * 0.95f);
                mb.Box(new Vector3(hw, fill * 0.5f, 0), new Vector3(0.18f, fill, hd * 2f), st.Wall * 0.95f);
                float front = Mathf.Min(fill, wall * 0.45f);
                mb.Box(new Vector3(-hw * 0.6f, front * 0.5f, hd), new Vector3(hw * 0.8f, front, 0.18f), st.Wall);
                mb.Box(new Vector3(hw * 0.6f, front * 0.5f, hd), new Vector3(hw * 0.8f, front, 0.18f), st.Wall);
            }
            // Scaffold poles and boards alongside the front, climbing with the work.
            float scaf = Mathf.Max(post, fill) + 0.8f;
            if (post > 0.05f)
            {
                for (int i = 0; i < 3; i++)
                {
                    var p = new Vector3(-hw + i * hw, 0, hd + 0.8f);
                    mb.Box(p + Vector3.up * scaf * 0.5f, new Vector3(0.12f, scaf, 0.12f), raw * 0.85f);
                }
                for (float y = 1.0f; y < scaf - 0.2f; y += 1.2f)
                    mb.Box(new Vector3(0, y, hd + 0.75f), new Vector3(hw * 2f + 0.3f, 0.08f, 0.5f), ModelKit.Plank * 0.9f);
                ModelKit.Beam(mb, new Vector3(-hw, 0.1f, hd + 0.8f), new Vector3(0, scaf - 0.3f, hd + 0.8f), 0.05f, raw * 0.85f);
            }
            // Rafters, then the first roof courses.
            float roofK = Mathf.Clamp01((k - 0.8f) / 0.2f);
            if (roofK > 0f)
            {
                float rh = wall * 0.8f;
                int n = Mathf.Max(2, Mathf.RoundToInt(hw * 2f / 1.2f));
                for (int i = 0; i <= n; i++)
                {
                    float x = -hw + i * hw * 2f / n;
                    ModelKit.Beam(mb, new Vector3(x, wall, -hd - 0.3f), new Vector3(x, wall + rh, 0), 0.08f, raw);
                    ModelKit.Beam(mb, new Vector3(x, wall, hd + 0.3f), new Vector3(x, wall + rh, 0), 0.08f, raw);
                }
                ModelKit.Beam(mb, new Vector3(-hw - 0.2f, wall + rh, 0), new Vector3(hw + 0.2f, wall + rh, 0), 0.1f, raw);
                if (roofK > 0.4f)
                {
                    float c = (roofK - 0.4f) / 0.6f * 0.55f;
                    var e0 = new Vector3(-hw - 0.4f, wall, -hd - 0.4f); var e1 = new Vector3(hw + 0.4f, wall, -hd - 0.4f);
                    var r0 = new Vector3(-hw - 0.4f, wall + rh, 0); var r1 = new Vector3(hw + 0.4f, wall + rh, 0);
                    ModelKit.Q2(mb, e0, Vector3.Lerp(e0, r0, c), Vector3.Lerp(e1, r1, c), e1, st.Roof);
                }
            }
            // A little sign post in the faction colour at the front corner.
            mb.Box(new Vector3(hw + 0.6f, 0.7f, hd + 0.2f), new Vector3(0.12f, 1.4f, 0.12f), ModelKit.DarkWood);
            mb.Box(new Vector3(hw + 0.6f, 1.35f, hd + 0.26f), new Vector3(0.7f, 0.42f, 0.06f), st.Accent);
        }

        /// <summary>The delivered cost wares heaped beside a site: planks, stone blocks, bricks.</summary>
        public static void SiteWares(MeshBuilder mb, Vector3 p, int planks, int stone, int bricks)
        {
            if (planks > 0) ModelKit.PlankStack(mb, p, 2.0f, Mathf.Min(planks, 6), 90f);
            if (stone > 0) ModelKit.BlockStack(mb, p + new Vector3(-1.6f, 0, 0.3f), Mathf.Min(stone, 6), 0.48f, ModelKit.StoneCol, 20f);
            if (bricks > 0) ModelKit.BrickStack(mb, p + new Vector3(-0.6f, 0, 1.2f), Mathf.Min(bricks, 5), 0f);
        }

        // ---------------------------------------------------------------- piles of raw goods

        /// <summary>One natural heap of a raw pile: logs, clay lumps, rocks or grain sheaves. Seeded by the pile slot.</summary>
        public static void Pile(MeshBuilder mb, Vector3 p, RawKind kind, int seed)
        {
            var rnd = new PropRandom(seed * 131 + 17);
            float yaw = rnd.Range(0f, 180f);
            switch (kind)
            {
                case RawKind.Log:
                    ModelKit.LogStack(mb, p, rnd.Range(2.6f, 3.2f), 3, yaw, 0.32f);
                    break;
                case RawKind.Clay:
                    for (int i = 0; i < 7; i++)
                    {
                        var o = ModelKit.Dir(i * 51f + yaw) * (i == 0 ? 0f : rnd.Range(0.5f, 1.1f));
                        ModelKit.Lump(mb, p + o + Vector3.up * (i == 0 ? 0.25f : 0f), rnd.Range(0.45f, 0.65f), rnd.Range(0.5f, 0.75f), ModelKit.ClayCol * rnd.Range(0.88f, 1.06f), 6, rnd.Next() * 3f);
                    }
                    break;
                case RawKind.Rock:
                    for (int i = 0; i < 6; i++)
                    {
                        var o = ModelKit.Dir(i * 60f + yaw) * (i == 0 ? 0f : rnd.Range(0.6f, 1.2f));
                        mb.Rock(p + o, i == 0 ? 0.9f : rnd.Range(0.45f, 0.7f), ModelKit.Rock * rnd.Range(0.9f, 1.12f), rnd.Next);
                    }
                    break;
                case RawKind.Grain:
                    // A stook: sheaves leaning together, and one lying.
                    for (int i = 0; i < 5; i++) ModelKit.Sheaf(mb, p + ModelKit.Dir(i * 72f + yaw) * 0.45f, rnd.Range(1.5f, 1.8f));
                    ModelKit.Sheaf(mb, p, 2.0f);
                    break;
            }
        }

        // ---------------------------------------------------------------- rubble

        public static Mesh RubbleMesh(BuildingSize size, Faction f, int seed)
            => ModelKit.Cached("rubble/" + (int)size + "/" + (int)f + "/" + (seed % 4), mb => Rubble(mb, size, ModelStyle.Of(f), seed % 4));

        /// <summary>A collapsed heap: broken wall stones, charred beams, a few roof pieces, soot on the ground.</summary>
        public static void Rubble(MeshBuilder mb, BuildingSize size, ModelStyle st, int seed)
        {
            var rnd = new PropRandom(seed * 977 + 5);
            float r = size == BuildingSize.Hq ? 5.5f : size == BuildingSize.Medium ? 3.8f : 2.4f;
            int n = size == BuildingSize.Hq ? 26 : size == BuildingSize.Medium ? 18 : 11;
            var ring = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < 12; i++) ring.Add(new Vector3(Mathf.Cos(i * Mathf.PI / 6f), 0, Mathf.Sin(i * Mathf.PI / 6f)) * r * rnd.Range(1.0f, 1.25f) + Vector3.up * 0.1f);
            mb.FanUp(Vector3.up * 0.12f, ring, new Color(0.26f, 0.23f, 0.20f));
            for (int i = 0; i < n; i++)
            {
                var o = ModelKit.Dir(rnd.Range(0f, 360f)) * Mathf.Sqrt(rnd.Next()) * r * 0.9f;
                float s = rnd.Range(0.4f, 1.0f) * (size == BuildingSize.Small ? 0.8f : 1f);
                if (i % 3 == 0) mb.Rock(o, s, st.Wall * rnd.Range(0.75f, 1.0f), rnd.Next);
                else mb.Box(o + Vector3.up * s * 0.25f, new Vector3(s, s * 0.5f, s * 0.8f), (st.Stone ? st.Wall : ModelKit.Rock) * rnd.Range(0.7f, 1.0f), rnd.Range(0f, 90f));
            }
            for (int i = 0; i < n / 3; i++)
            {
                var a = ModelKit.Dir(rnd.Range(0f, 360f)) * rnd.Range(0f, r * 0.7f);
                var b = a + ModelKit.Dir(rnd.Range(0f, 360f)) * rnd.Range(1.4f, 2.6f) + Vector3.up * rnd.Range(0.2f, 1.2f);
                ModelKit.Beam(mb, a + Vector3.up * 0.2f, b, 0.13f, ModelKit.Soot * rnd.Range(1.0f, 1.6f));
            }
            for (int i = 0; i < 3; i++)
            {
                var o = ModelKit.Dir(rnd.Range(0f, 360f)) * rnd.Range(0f, r * 0.6f) + Vector3.up * 0.4f;
                mb.Box(o, new Vector3(rnd.Range(1.2f, 2f), 0.15f, rnd.Range(1f, 1.6f)), st.Roof * 0.8f, rnd.Range(0f, 90f));
            }
        }

        // ---------------------------------------------------------------- standards

        /// <summary>The war standard on a marked enemy building: a tall pole, crossbar and a long banner in the marking faction's colour with a pale disc where the number goes.</summary>
        public static Mesh TargetStandard(Faction by)
            => ModelKit.Cached("standard/target/" + (int)by, mb =>
            {
                var col = ModelKit.Accent(FactionLook.Colour(by));
                mb.Cylinder(Vector3.zero, 0.16f, 0.12f, 7.2f, 6, ModelKit.DarkWood);
                mb.Cone(new Vector3(0, 7.2f, 0), 0.22f, 0.6f, 4, ModelKit.Metal);
                mb.Box(new Vector3(0, 6.9f, 0), new Vector3(2.6f, 0.14f, 0.14f), ModelKit.DarkWood);
                var a = new Vector3(-1.15f, 6.85f, -0.08f); var b = new Vector3(1.15f, 6.85f, -0.08f);
                var c = new Vector3(1.15f, 3.9f, -0.08f); var d = new Vector3(-1.15f, 3.9f, -0.08f);
                ModelKit.Q2(mb, d, a, b, c, col);
                ModelKit.T2(mb, d, c, new Vector3(0, 3.2f, -0.08f), col * 0.9f);
                // A pale disc for the number, and two dark tassels.
                var disc = new System.Collections.Generic.List<Vector3>();
                for (int i = 0; i < 10; i++) disc.Add(new Vector3(Mathf.Cos(i * Mathf.PI / 5f) * 0.8f, 5.35f + Mathf.Sin(i * Mathf.PI / 5f) * 0.8f, -0.12f));
                for (int i = 0; i < 10; i++) ModelKit.T2(mb, new Vector3(0, 5.35f, -0.12f), disc[i], disc[(i + 1) % 10], new Color(0.96f, 0.92f, 0.80f));
                mb.Box(new Vector3(-1.25f, 6.4f, -0.05f), new Vector3(0.12f, 0.8f, 0.12f), ModelKit.Straw);
                mb.Box(new Vector3(1.25f, 6.4f, -0.05f), new Vector3(0.12f, 0.8f, 0.12f), ModelKit.Straw);
            });

        /// <summary>The defend standard: a pole with a square banner in the faction's colour bearing a shield, stones round its foot.</summary>
        public static Mesh DefendStandard(Faction by)
            => ModelKit.Cached("standard/defend/" + (int)by, mb =>
            {
                var col = ModelKit.Accent(FactionLook.Colour(by));
                for (int i = 0; i < 6; i++) mb.Rock(ModelKit.Dir(i * 60f) * 0.55f, 0.3f, ModelKit.Rock, () => 0.5f);
                mb.Cylinder(Vector3.zero, 0.14f, 0.11f, 5.4f, 6, ModelKit.DarkWood);
                mb.Cone(new Vector3(0, 5.4f, 0), 0.22f, 0.45f, 6, ModelKit.Straw);
                mb.Box(new Vector3(0, 5.15f, 0), new Vector3(2.0f, 0.12f, 0.12f), ModelKit.DarkWood);
                var a = new Vector3(-0.95f, 5.1f, -0.08f); var b = new Vector3(0.95f, 5.1f, -0.08f);
                var c = new Vector3(0.95f, 3.3f, -0.08f); var d = new Vector3(-0.95f, 3.3f, -0.08f);
                ModelKit.Q2(mb, d, a, b, c, col * 0.85f);
                // The shield emblem: a pale kite shape.
                var s0 = new Vector3(0, 4.85f, -0.12f); var s1 = new Vector3(0.55f, 4.6f, -0.12f); var s2 = new Vector3(0, 3.55f, -0.12f); var s3 = new Vector3(-0.55f, 4.6f, -0.12f);
                var pale = new Color(0.95f, 0.90f, 0.76f);
                mb.Tri(s0, s1, s2, pale); mb.Tri(s0, s2, s3, pale);
                mb.Tri(s0, s2, s1, pale); mb.Tri(s0, s3, s2, pale);
            });
    }
}
