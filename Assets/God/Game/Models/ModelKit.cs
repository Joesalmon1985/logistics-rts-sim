using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// The two building styles, warm timber and thatch or dark stone and slate, each in a faction's colour. Factions
    /// alternate between them by id for now (<see cref="FactionLook.Stone"/>).
    /// </summary>
    public sealed class ModelStyle
    {
        public bool Stone;
        public Color Wall, Frame, Roof, RoofEdge, Plinth, Door, Accent, AccentDark, Cloth;

        public static readonly ModelStyle Timber = new ModelStyle
        {
            Stone = false,
            Wall = new Color(0.84f, 0.76f, 0.62f), Frame = new Color(0.36f, 0.24f, 0.14f),
            Roof = new Color(0.66f, 0.51f, 0.31f), RoofEdge = new Color(0.50f, 0.37f, 0.22f),
            Plinth = new Color(0.55f, 0.52f, 0.47f), Door = new Color(0.30f, 0.20f, 0.12f),
            Accent = ModelKit.Accent(FactionLook.Colour(Faction.F0)), AccentDark = ModelKit.Accent(FactionLook.Colour(Faction.F0)) * 0.7f,
            Cloth = new Color(0.62f, 0.50f, 0.36f),
        };

        public static readonly ModelStyle Slate = new ModelStyle
        {
            Stone = true,
            Wall = new Color(0.44f, 0.42f, 0.41f), Frame = new Color(0.24f, 0.21f, 0.19f),
            Roof = new Color(0.22f, 0.23f, 0.27f), RoofEdge = new Color(0.16f, 0.17f, 0.20f),
            Plinth = new Color(0.30f, 0.29f, 0.28f), Door = new Color(0.16f, 0.12f, 0.10f),
            Accent = ModelKit.Accent(FactionLook.Colour(Faction.F1)), AccentDark = ModelKit.Accent(FactionLook.Colour(Faction.F1)) * 0.7f,
            Cloth = new Color(0.30f, 0.27f, 0.25f),
        };

        private static readonly ModelStyle[] ByFaction = new ModelStyle[FactionInfo.Max];

        /// <summary>The style of a faction: its family (timber or stone) with the faction colour as the accent.</summary>
        public static ModelStyle Of(Faction f)
        {
            int i = (int)f;
            if (i < 0 || i >= ByFaction.Length) return Timber;
            if (ByFaction[i] != null) return ByFaction[i];
            var b = FactionLook.Stone(f) ? Slate : Timber;
            var acc = ModelKit.Accent(FactionLook.Colour(f));
            return ByFaction[i] = new ModelStyle
            {
                Stone = b.Stone, Wall = b.Wall, Frame = b.Frame, Roof = b.Roof, RoofEdge = b.RoofEdge, Plinth = b.Plinth, Door = b.Door,
                Accent = acc, AccentDark = acc * 0.7f, Cloth = b.Cloth,
            };
        }
    }

    /// <summary>
    /// P4b shared model helpers: colours, ground height, oriented beams and logs, gabled roofs, houses, banners and a
    /// mesh cache. Everything is original procedural geometry built from <see cref="MeshBuilder"/> primitives, in local
    /// space (feet at y = 0, front towards +Z). View code only.
    /// </summary>
    public static class ModelKit
    {
        public static readonly Color Wood = new Color(0.47f, 0.33f, 0.19f), DarkWood = new Color(0.28f, 0.19f, 0.12f);
        public static readonly Color LogBark = new Color(0.40f, 0.28f, 0.17f), LogEnd = new Color(0.84f, 0.68f, 0.44f);
        public static readonly Color Plank = new Color(0.84f, 0.67f, 0.42f), Straw = new Color(0.90f, 0.78f, 0.36f);
        public static readonly Color ClayCol = new Color(0.74f, 0.40f, 0.24f), Brick = new Color(0.66f, 0.27f, 0.20f);
        public static readonly Color StoneCol = new Color(0.72f, 0.72f, 0.70f), Rock = new Color(0.56f, 0.56f, 0.55f);
        public static readonly Color OreCol = new Color(0.36f, 0.30f, 0.38f), Iron = new Color(0.34f, 0.38f, 0.44f);
        public static readonly Color Soil = new Color(0.46f, 0.34f, 0.22f), Earth = new Color(0.54f, 0.44f, 0.30f);
        public static readonly Color Skin = new Color(0.90f, 0.70f, 0.52f), Hair = new Color(0.32f, 0.22f, 0.14f);
        public static readonly Color Ember = new Color(1f, 0.55f, 0.16f), Soot = new Color(0.20f, 0.19f, 0.18f);
        public static readonly Color Metal = new Color(0.58f, 0.60f, 0.64f);

        /// <summary>The faction colour as an accent: slightly richer so it reads against thatch and stone.</summary>
        public static Color Accent(Color c) { var a = Color.Lerp(c, c * 1.12f, 0.5f); a.a = 1f; return a; }

        public static Color FactionColour(Faction f) => FactionLook.Colour(f);

        /// <summary>Ground height of the island at a world point (0 without the organic island).</summary>
        public static float GroundY(float x, float z)
        {
            var iv = IslandView.Instance;
            return iv != null && iv.Ground != null ? iv.Ground.HeightAt(x, z) : 0f;
        }

        public static Vector3 OnGround(float x, float z, float lift = 0f) => new Vector3(x, GroundY(x, z) + lift, z);

        public static Vector3 Node(NodeGrid g, int node, float lift = 0f) => OnGround((float)g.X(node), (float)g.Z(node), lift);

        /// <summary>Yaw (degrees) that turns local +Z towards a world direction.</summary>
        public static float Yaw(Vector3 dir) => Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        public static Vector3 Dir(float yawDeg) => new Vector3(Mathf.Sin(yawDeg * Mathf.Deg2Rad), 0f, Mathf.Cos(yawDeg * Mathf.Deg2Rad));

        // ---------------------------------------------------------------- mesh cache

        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        /// <summary>One shared mesh per key, built once (buildings of a kind and faction share their mesh).</summary>
        public static Mesh Cached(string key, System.Action<MeshBuilder> build)
        {
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            var mb = new MeshBuilder();
            build(mb);
            m = mb.ToMesh(key);
            Cache[key] = m;
            return m;
        }

        private static Material _lit, _unlit, _glow;
        public static Material Lit => _lit != null ? _lit : (_lit = MaterialFactory.CreateVertexColored(Color.white, "Models lit"));
        public static Material Unlit => _unlit != null ? _unlit : (_unlit = MaterialFactory.CreateUnlit(Color.white, "Models unlit"));
        public static Material Glow => _glow != null ? _glow : (_glow = MaterialFactory.CreateGlow(Color.white, "Models glow"));

        /// <summary>A child object drawing a mesh (no collider, no shadows).</summary>
        public static GameObject Part(string name, Transform parent, Mesh mesh, Material mat = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat != null ? mat : Lit;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        // ---------------------------------------------------------------- shapes

        public static void Q2(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col) { mb.Quad(a, b, c, d, col); mb.Quad(a, d, c, b, col * 0.85f); }
        public static void T2(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Color col) { mb.Tri(a, b, c, col); mb.Tri(a, c, b, col * 0.85f); }

        /// <summary>A square beam between two points.</summary>
        public static void Beam(MeshBuilder mb, Vector3 a, Vector3 b, float half, Color c)
        {
            var d = b - a; float len = d.magnitude;
            if (len < 1e-4f) return;
            var q = Quaternion.FromToRotation(Vector3.up, d / len);
            Vector3 P(float x, float y, float z) => a + q * new Vector3(x, y, z);
            float w = half;
            var p0 = P(-w, 0, -w); var p1 = P(w, 0, -w); var p2 = P(w, 0, w); var p3 = P(-w, 0, w);
            var t0 = P(-w, len, -w); var t1 = P(w, len, -w); var t2 = P(w, len, w); var t3 = P(-w, len, w);
            mb.Quad(p0, t0, t1, p1, c); mb.Quad(p1, t1, t2, p2, c * 0.9f); mb.Quad(p2, t2, t3, p3, c); mb.Quad(p3, t3, t0, p0, c * 0.9f);
            mb.Quad(t0, t3, t2, t1, c * 1.05f); mb.Quad(p0, p1, p2, p3, c * 0.8f);
        }

        /// <summary>A round log (or any cylinder) between two points, with light end caps.</summary>
        public static void Log(MeshBuilder mb, Vector3 a, Vector3 b, float r, Color bark, Color end, int sides = 7)
        {
            var d = b - a; float len = d.magnitude;
            if (len < 1e-4f) return;
            var axis = d / len;
            var q = Quaternion.FromToRotation(Vector3.up, axis);
            var ring0 = new Vector3[sides]; var ring1 = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float ang = i * Mathf.PI * 2f / sides;
                var o = q * new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                ring0[i] = a + o; ring1[i] = b + o;
            }
            for (int i = 0; i < sides; i++)
            {
                int k = (i + 1) % sides;
                mb.Quad(ring0[i], ring1[i], ring1[k], ring0[k], bark * (0.88f + 0.12f * ((i & 1) == 0 ? 1f : 0.6f)));
                mb.Tri(b, ring1[k], ring1[i], end);
                mb.Tri(a, ring0[i], ring0[k], end * 0.92f);
            }
        }

        /// <summary>A low-poly rounded lump (puff, clay lump, sheaf head): a squashed double cone.</summary>
        public static void Lump(MeshBuilder mb, Vector3 c, float r, float h, Color col, int sides = 6, float twist = 0f)
        {
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = twist + i * Mathf.PI * 2f / sides;
                ring[i] = c + new Vector3(Mathf.Cos(a) * r, h * 0.45f, Mathf.Sin(a) * r);
            }
            var top = c + Vector3.up * h; var bot = c;
            for (int i = 0; i < sides; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % sides];
                mb.Tri(top, b, a, col);
                mb.Tri(bot, a, b, col * 0.75f);
            }
        }

        /// <summary>A gabled roof with the ridge along local X: two slopes, two gable ends, a thick eave lip and a ridge cap.</summary>
        public static void GableX(MeshBuilder mb, Vector3 b, float W, float D, float h, Color roof, Color edge, Color ridgeCap, float lip = 0.35f)
        {
            float hw = W * 0.5f, hd = D * 0.5f;
            var e0 = b + new Vector3(-hw, 0, -hd); var e1 = b + new Vector3(hw, 0, -hd);
            var e2 = b + new Vector3(hw, 0, hd); var e3 = b + new Vector3(-hw, 0, hd);
            var r0 = b + new Vector3(-hw, h, 0); var r1 = b + new Vector3(hw, h, 0);
            mb.Quad(e0, r0, r1, e1, roof * 0.92f);  // front -Z slope
            mb.Quad(e2, r1, r0, e3, roof);         // back +Z slope (faces up and back)
            mb.Quad(e3, r0, r1, e2, roof);         // (front-facing copy for the +Z side seen from the camera)
            mb.Quad(e1, r1, r0, e0, roof * 0.92f);
            T2(mb, e0, e3, r0, edge);
            T2(mb, e1, r1, e2, edge);
            if (lip > 0f)
            {
                // Thick eave edges along both long sides.
                Beam(mb, e0 + Vector3.down * lip * 0.5f, e1 + Vector3.down * lip * 0.5f, lip * 0.5f, edge);
                Beam(mb, e3 + Vector3.down * lip * 0.5f, e2 + Vector3.down * lip * 0.5f, lip * 0.5f, edge);
            }
            Beam(mb, r0 + Vector3.up * 0.05f + Vector3.left * 0.1f, r1 + Vector3.up * 0.05f + Vector3.right * 0.1f, 0.22f, ridgeCap);
        }

        /// <summary>
        /// A house: plinth, walls (timber framing for timber, quoins for stone), a door in the +Z wall, a gabled roof
        /// along X with the ridge cap in the faction accent. Footprint W (x) by D (z), centred on c.
        /// </summary>
        public static void House(MeshBuilder mb, Vector3 c, float W, float D, float wallH, float roofH, ModelStyle st, bool door = true, float doorW = 1.1f)
        {
            mb.Box(c + new Vector3(0, -0.6f, 0), new Vector3(W + 0.5f, 1.6f, D + 0.5f), st.Plinth);
            mb.Box(c + new Vector3(0, wallH * 0.5f, 0), new Vector3(W, wallH, D), st.Wall);
            float hw = W * 0.5f, hd = D * 0.5f;
            if (!st.Stone)
            {
                // Timber frame: corner posts, a sill, a mid rail and diagonal braces on the front.
                foreach (var o in new[] { new Vector3(-hw, 0, -hd), new Vector3(hw, 0, -hd), new Vector3(hw, 0, hd), new Vector3(-hw, 0, hd) })
                    mb.Box(c + o + Vector3.up * wallH * 0.5f, new Vector3(0.28f, wallH, 0.28f), st.Frame);
                mb.Box(c + new Vector3(0, wallH - 0.12f, hd + 0.02f), new Vector3(W + 0.1f, 0.22f, 0.12f), st.Frame);
                mb.Box(c + new Vector3(0, wallH * 0.5f, hd + 0.02f), new Vector3(W, 0.16f, 0.1f), st.Frame);
                mb.Box(c + new Vector3(0, wallH - 0.12f, -hd - 0.02f), new Vector3(W + 0.1f, 0.22f, 0.12f), st.Frame);
                Beam(mb, c + new Vector3(-hw + 0.2f, 0.1f, hd + 0.05f), c + new Vector3(-hw * 0.45f, wallH * 0.5f, hd + 0.05f), 0.07f, st.Frame);
                Beam(mb, c + new Vector3(hw - 0.2f, 0.1f, hd + 0.05f), c + new Vector3(hw * 0.45f, wallH * 0.5f, hd + 0.05f), 0.07f, st.Frame);
            }
            else
            {
                // Stone: lighter corner quoins and a dark base course.
                foreach (var o in new[] { new Vector3(-hw, 0, -hd), new Vector3(hw, 0, -hd), new Vector3(hw, 0, hd), new Vector3(-hw, 0, hd) })
                    for (int k = 0; k < 3; k++)
                        mb.Box(c + o + Vector3.up * (0.4f + k * wallH / 3f), new Vector3(0.45f, 0.38f, 0.45f), st.Wall * 1.25f);
                mb.Box(c + new Vector3(0, 0.25f, 0), new Vector3(W + 0.12f, 0.5f, D + 0.12f), st.Plinth);
            }
            if (door)
            {
                mb.Box(c + new Vector3(0, 0.85f, hd + 0.04f), new Vector3(doorW, 1.7f, 0.14f), st.Door);
                mb.Box(c + new Vector3(0, 1.75f, hd + 0.08f), new Vector3(doorW + 0.3f, 0.18f, 0.16f), st.Frame);
            }
            // Small windows on the front.
            if (W > 3.5f)
                foreach (float x in new[] { -hw * 0.6f, hw * 0.6f })
                    mb.Box(c + new Vector3(x, wallH * 0.62f, hd + 0.03f), new Vector3(0.5f, 0.5f, 0.1f), st.Door * 0.8f);
            float steep = st.Stone ? 1.15f : 1f;
            GableX(mb, c + Vector3.up * wallH, W + 0.9f, D + 1.1f, roofH * steep, st.Roof, st.RoofEdge, st.Accent, st.Stone ? 0.18f : 0.38f);
        }

        /// <summary>A round hut: wall, accent eave band, cone roof, door towards +Z.</summary>
        public static void Hut(MeshBuilder mb, Vector3 p, float r, float wall, float roof, ModelStyle st)
        {
            mb.Cylinder(p + Vector3.down * 0.4f, r + 0.2f, r + 0.2f, 0.6f, 10, st.Plinth);
            mb.Cylinder(p, r, r * 0.96f, wall, 10, st.Wall);
            mb.Cylinder(p + Vector3.up * wall, r * 1.3f, r * 1.18f, 0.22f, 10, st.Accent, false);
            mb.Cone(p + Vector3.up * (wall + 0.22f), r * 1.18f, roof, 10, st.Roof);
            mb.Box(p + new Vector3(0, 0.8f, r * 0.97f), new Vector3(0.9f, 1.6f, 0.2f), st.Door);
        }

        /// <summary>A pole; the cloth is drawn by <see cref="BannerCloth"/> (so it can be lowered).</summary>
        public static void Pole(MeshBuilder mb, Vector3 p, float h, ModelStyle st)
        {
            mb.Cylinder(p, 0.13f, 0.1f, h, 6, ModelKit.DarkWood);
            mb.Cone(p + Vector3.up * h, 0.2f, 0.4f, 6, st.Stone ? Metal : Straw);
        }

        /// <summary>A hanging banner cloth from the pole top (local origin): a crossbar and a tall cloth with a pale band and a pointed foot.</summary>
        public static void BannerCloth(MeshBuilder mb, Color col, float w = 1.3f, float h = 2.2f)
        {
            var c = Accent(col);
            mb.Box(new Vector3(w * 0.5f, 0f, 0f), new Vector3(w + 0.3f, 0.12f, 0.12f), DarkWood);
            var a = new Vector3(0.1f, -0.05f, 0); var b = new Vector3(w + 0.1f, -0.05f, 0);
            var d = new Vector3(0.1f, -h, 0); var e = new Vector3(w + 0.1f, -h, 0); var tip = new Vector3(w * 0.5f + 0.1f, -h - 0.6f, 0);
            Q2(mb, a, d, e, b, c);
            T2(mb, d, tip, e, c * 0.9f);
            var band = Color.Lerp(c, Color.white, 0.55f);
            Q2(mb, a + new Vector3(0, -h * 0.28f, -0.02f), a + new Vector3(0, -h * 0.4f, -0.02f), b + new Vector3(0, -h * 0.4f, -0.02f), b + new Vector3(0, -h * 0.28f, -0.02f), band);
        }

        /// <summary>A small triangular pennant from the pole top (local origin), streaming towards +X.</summary>
        public static void Pennant(MeshBuilder mb, Color col, float len = 1.4f, float h = 0.8f)
        {
            var c = Accent(col);
            T2(mb, new Vector3(0, 0, 0), new Vector3(0, -h, 0), new Vector3(len, -h * 0.45f, 0.05f), c);
        }

        // ---------------------------------------------------------------- small props

        public static void Crate(MeshBuilder mb, Vector3 p, float s, float yaw = 0f)
        {
            mb.Box(p + Vector3.up * s * 0.5f, new Vector3(s, s, s), Plank * 0.85f, yaw);
            mb.Box(p + Vector3.up * s * 0.5f, new Vector3(s * 1.04f, s * 0.15f, s * 1.04f), Wood, yaw);
        }

        public static void Barrel(MeshBuilder mb, Vector3 p, float r = 0.45f, float h = 1.0f)
        {
            mb.Cylinder(p, r * 0.85f, r, h * 0.5f, 8, Wood);
            mb.Cylinder(p + Vector3.up * h * 0.5f, r, r * 0.85f, h * 0.5f, 8, Wood);
            mb.Cylinder(p + Vector3.up * h * 0.45f, r * 1.02f, r * 1.02f, 0.1f, 8, Iron, false);
        }

        public static void LogStack(MeshBuilder mb, Vector3 p, float len, int rows, float yaw, float r = 0.28f)
        {
            var along = Dir(yaw); var side = Dir(yaw + 90f);
            for (int row = 0; row < rows; row++)
            {
                int n = rows - row + 1;
                for (int i = 0; i < n; i++)
                {
                    var c = p + side * ((i - (n - 1) * 0.5f) * r * 2.05f) + Vector3.up * (r + row * r * 1.75f);
                    Log(mb, c - along * len * 0.5f, c + along * len * 0.5f, r, LogBark * (0.9f + 0.05f * ((i + row) % 3)), LogEnd);
                }
            }
        }

        public static void PlankStack(MeshBuilder mb, Vector3 p, float len, int layers, float yaw)
        {
            for (int l = 0; l < layers; l++)
                for (int i = 0; i < 3; i++)
                    mb.Box(p + Dir(yaw + 90f) * ((i - 1) * 0.42f) + Vector3.up * (0.1f + l * 0.16f), new Vector3(0.38f, 0.13f, len), Plank * (0.92f + 0.04f * ((i + l) % 3)), yaw + (l % 2) * 2f);
        }

        public static void BlockStack(MeshBuilder mb, Vector3 p, int n, float s, Color col, float yaw)
        {
            int k = 0;
            for (int layer = 0; k < n; layer++)
                for (int i = 0; i < 3 - layer && k < n; i++, k++)
                    mb.Box(p + Dir(yaw + 90f) * ((i - (2 - layer) * 0.5f) * s * 1.05f) + Vector3.up * (s * 0.4f + layer * s * 0.8f), new Vector3(s, s * 0.8f, s * 1.2f), col * (0.9f + 0.05f * (k % 3)), yaw);
        }

        public static void BrickStack(MeshBuilder mb, Vector3 p, int layers, float yaw)
        {
            for (int l = 0; l < layers; l++)
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++)
                        mb.Box(p + Dir(yaw) * ((i - 0.5f) * 0.52f) + Dir(yaw + 90f) * ((j - 0.5f) * 0.3f) + Vector3.up * (0.08f + l * 0.16f), new Vector3(0.26f, 0.14f, 0.5f), Brick * (0.9f + 0.06f * ((i + j + l) % 3)), yaw + (l % 2) * 90f);
        }

        public static void Sheaf(MeshBuilder mb, Vector3 p, float h, float tilt = 0f)
        {
            mb.Cylinder(p, 0.32f, 0.2f, h * 0.55f, 6, Straw * 0.92f);
            mb.Cylinder(p + Vector3.up * h * 0.5f, 0.2f, 0.18f, 0.1f, 6, Wood);
            mb.Cylinder(p + Vector3.up * h * 0.6f, 0.2f, 0.36f, h * 0.25f, 6, Straw);
            mb.Cone(p + Vector3.up * h * 0.85f, 0.36f, h * 0.25f, 6, Straw * 1.05f);
        }

        public static void Fence(MeshBuilder mb, Vector3 a, Vector3 b, Color c)
        {
            float len = (b - a).magnitude; int posts = Mathf.Max(2, Mathf.RoundToInt(len / 1.4f) + 1);
            for (int i = 0; i < posts; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)(posts - 1));
                mb.Box(p + Vector3.up * 0.55f, new Vector3(0.16f, 1.1f, 0.16f), c);
            }
            Beam(mb, a + Vector3.up * 0.8f, b + Vector3.up * 0.8f, 0.06f, c * 1.1f);
            Beam(mb, a + Vector3.up * 0.4f, b + Vector3.up * 0.4f, 0.06f, c * 1.1f);
        }

        public static void Chimney(MeshBuilder mb, Vector3 p, float h, float r, Color col)
        {
            mb.Cylinder(p, r * 1.15f, r, h, 6, col);
            mb.Cylinder(p + Vector3.up * h, r * 1.2f, r * 1.2f, 0.2f, 6, col * 0.8f);
            mb.Cylinder(p + Vector3.up * (h + 0.2f), r * 0.75f, r * 0.75f, 0.02f, 6, Soot);
        }

        /// <summary>A spoked wheel in the X = 0 plane (axis along X), centre at the origin.</summary>
        public static void Wheel(MeshBuilder mb, float r, float w, Color rim, Color spoke, int spokes = 6)
        {
            int sides = 12;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                var p0 = new Vector3(0, Mathf.Sin(a0) * r, Mathf.Cos(a0) * r); var p1 = new Vector3(0, Mathf.Sin(a1) * r, Mathf.Cos(a1) * r);
                Beam(mb, p0, p1, w * 0.5f, rim);
            }
            for (int i = 0; i < spokes; i++)
            {
                float a = i * Mathf.PI * 2f / spokes;
                Beam(mb, Vector3.zero, new Vector3(0, Mathf.Sin(a) * r, Mathf.Cos(a) * r), w * 0.22f, spoke);
            }
            Log(mb, new Vector3(-w * 0.8f, 0, 0), new Vector3(w * 0.8f, 0, 0), r * 0.18f, spoke, spoke * 0.8f, 6);
        }
    }
}
