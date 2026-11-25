using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Stage 3 building models: the seven new processors (bakery, mortar mill, bronze foundry, pottery, steel forge, tannery,
    /// weaver), the ten armouries (one house with a sign of its gear on a pole, a roof in the gear's colour, an anvil and
    /// hearth for the metal ones, a bench for the rest) and the upgrade levels of the five harvesters (an annexe, a bigger
    /// stockpile, a coloured roof and gate posts). Same procedural low-poly style as the others; view code only.
    /// A model kind with a level is written "woodcutter:2".
    /// </summary>
    public static partial class BuildingModels
    {
        /// <summary>Model kind of a harvester at a level (level 1 is the plain kind).</summary>
        public static string KindAtLevel(string kind, int level) => level >= 2 && System.Array.IndexOf(BuildingKinds.Harvesters, kind) >= 0 ? kind + ":" + Mathf.Min(level, 3) : kind;

        /// <summary>Every kind of the stage 3 groups (harvesters, processors, armouries).</summary>
        public static IEnumerable<string> Stage3Kinds()
        {
            foreach (var k in BuildingKinds.Harvesters) yield return k;
            foreach (var k in BuildingKinds.Processors) yield return k;
            foreach (var k in BuildingKinds.Armouries) yield return k;
        }

        private static bool BuildStage3(MeshBuilder mb, string kind, ModelStyle st, BuildingRig rig)
        {
            switch (kind)
            {
                case BuildingKinds.Bakery: Bakery(mb, st, rig); return true;
                case BuildingKinds.MortarMill: MortarMill(mb, st, rig); return true;
                case BuildingKinds.Foundry: Foundry(mb, st, rig); return true;
                case BuildingKinds.Pottery: PotteryShop(mb, st, rig); return true;
                case BuildingKinds.Forge: SteelForge(mb, st, rig); return true;
                case BuildingKinds.Tannery: Tannery(mb, st, rig); return true;
                case BuildingKinds.Weaver: Weaver(mb, st, rig); return true;
            }
            if (System.Array.IndexOf(BuildingKinds.Armouries, kind) >= 0) { Armoury(mb, kind, st, rig); return true; }
            return false;
        }

        // ---------------------------------------------------------------- helpers

        private static ModelStyle Reroof(ModelStyle st, Color roof, Color edge) => new ModelStyle
        {
            Stone = st.Stone, Wall = st.Wall, Frame = st.Frame, Roof = roof, RoofEdge = edge, Plinth = st.Plinth,
            Door = st.Door, Accent = st.Accent, AccentDark = st.AccentDark, Cloth = st.Cloth,
        };

        /// <summary>A flat disc in the XY plane at depth z, facing +Z (and -Z).</summary>
        private static void Disc(MeshBuilder mb, Vector3 c, float r, Color col, int n = 14)
        {
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                ModelKit.T2(mb, c, c + new Vector3(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0), c + new Vector3(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0), col);
            }
        }

        private static Color Mix(Color a, Color b, float k) { var c = Color.Lerp(a, b, k); c.a = 1f; return c; }

        private static void Sack(MeshBuilder mb, Vector3 p, Color col, float s = 1f)
        {
            mb.Cylinder(p, 0.42f * s, 0.34f * s, 0.55f * s, 7, col);
            mb.Cylinder(p + Vector3.up * 0.5f * s, 0.34f * s, 0.18f * s, 0.3f * s, 7, col * 0.9f);
        }

        private static void Anvil(MeshBuilder mb, Vector3 p, float yaw = 0f)
        {
            mb.Box(p + Vector3.up * 0.3f, new Vector3(0.5f, 0.6f, 0.5f), ModelKit.DarkWood, yaw);
            mb.Box(p + Vector3.up * 0.72f, new Vector3(0.62f, 0.24f, 1.0f), ModelKit.Iron * 0.9f, yaw);
            mb.Box(p + Vector3.up * 0.9f, new Vector3(0.78f, 0.16f, 1.2f), ModelKit.Metal * 0.85f, yaw);
            mb.Cone(p + Vector3.up * 0.9f + ModelKit.Dir(yaw) * 0.75f, 0.14f, 0.5f, 4, ModelKit.Metal * 0.85f);
        }

        private static void Bench(MeshBuilder mb, Vector3 p, float len = 2.0f, float yaw = 0f)
        {
            mb.Box(p + Vector3.up * 0.8f, new Vector3(len, 0.16f, 0.8f), ModelKit.Plank * 0.9f, yaw);
            foreach (float s in new[] { -0.42f, 0.42f })
                foreach (float t in new[] { -0.28f, 0.28f })
                    mb.Box(p + ModelKit.Dir(yaw + 90f) * (s * len * 0.5f * 2f) + ModelKit.Dir(yaw) * t + Vector3.up * 0.4f, new Vector3(0.14f, 0.8f, 0.14f), ModelKit.DarkWood, yaw);
        }

        private static void Dome(MeshBuilder mb, Vector3 k, float r, Color body)
        {
            mb.Cylinder(k + Vector3.down * 0.4f, r, r * 0.96f, 1.2f, 12, body * 0.92f);
            mb.Cylinder(k + Vector3.up * 0.8f, r * 0.96f, r * 0.82f, 0.7f, 12, body);
            mb.Cylinder(k + Vector3.up * 1.5f, r * 0.82f, r * 0.56f, 0.7f, 12, body * 1.04f);
            mb.Cylinder(k + Vector3.up * 2.2f, r * 0.56f, r * 0.24f, 0.55f, 12, body * 1.08f);
        }

        /// <summary>The common back house of a processor, banner and sizes.</summary>
        private static void ProcessorBase(MeshBuilder mb, ModelStyle st, BuildingRig rig, float hx = -2.8f, float hz = -2.0f)
        {
            ModelKit.House(mb, new Vector3(hx, 0, hz), 3.0f, 2.7f, 2.3f, 2.0f, st);
            Banner(mb, rig, new Vector3(-4.6f, 0, -0.1f), 5.4f, st);
            rig.Height = 6.0f; rig.Radius = 5.0f;
        }

        // ---------------------------------------------------------------- processors

        private static void Bakery(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ProcessorBase(mb, st, rig);
            // A whitewashed bread oven: a low dome with a wide mouth and a chimney, and a baker's peel leaning on it.
            var o = new Vector3(1.4f, 0, -0.8f);
            var wash = st.Stone ? new Color(0.62f, 0.56f, 0.50f) : new Color(0.90f, 0.82f, 0.66f);
            Dome(mb, o, 2.1f, wash);
            mb.Cylinder(o + Vector3.up * 0.9f, 2.15f, 2.15f, 0.12f, 12, st.Accent, false);
            ModelKit.Chimney(mb, o + new Vector3(0.9f, 2.0f, -0.6f), 1.8f, 0.4f, wash * 0.8f);
            rig.Smoke.Add(o + new Vector3(0.9f, 4.1f, -0.6f));
            rig.SmokeColour = new Color(0.86f, 0.84f, 0.80f);
            mb.Box(o + new Vector3(0, 0.7f, 2.0f), new Vector3(1.3f, 1.2f, 0.3f), ModelKit.Soot);
            mb.Box(o + new Vector3(0, 1.4f, 2.05f), new Vector3(1.6f, 0.22f, 0.3f), wash * 0.7f);
            rig.HasGlow = true; rig.Glow = o + new Vector3(0, 0.65f, 2.17f); rig.GlowSize = new Vector3(0.95f, 0.8f, 0.1f);
            ModelKit.Beam(mb, o + new Vector3(1.6f, 0.1f, 2.2f), o + new Vector3(1.9f, 2.0f, 1.4f), 0.06f, ModelKit.Wood);
            mb.Box(o + new Vector3(1.55f, 0.12f, 2.25f), new Vector3(0.6f, 0.05f, 0.5f), ModelKit.Wood, 20f);
            // A table with loaves, flour sacks and a woodpile.
            var tb = new Vector3(-2.6f, 0, 1.8f);
            Bench(mb, tb, 2.2f, 0f);
            var crust = new Color(0.78f, 0.52f, 0.25f);
            for (int i = 0; i < 5; i++) mb.Cylinder(tb + new Vector3(-0.8f + i * 0.4f, 0.88f, (i % 2) * 0.12f), 0.19f, 0.15f, 0.2f, 6, crust * (0.92f + 0.05f * (i % 3)));
            Sack(mb, new Vector3(3.8f, 0, 1.6f), new Color(0.92f, 0.88f, 0.78f));
            Sack(mb, new Vector3(4.5f, 0, 1.0f), new Color(0.90f, 0.85f, 0.74f), 0.9f);
            Sack(mb, new Vector3(4.1f, 0, 0.3f), new Color(0.92f, 0.88f, 0.78f), 0.8f);
            ModelKit.LogStack(mb, new Vector3(3.9f, 0, -3.2f), 1.8f, 2, 0f, 0.2f);
        }

        private static void MortarMill(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ProcessorBase(mb, st, rig);
            // The millstone: a heavy round stone on a stone drum, a wooden arm and hopper, driven by a water wheel at the side.
            var m = new Vector3(1.0f, 0, -0.8f);
            var grey = new Color(0.70f, 0.69f, 0.66f);
            mb.Cylinder(m + Vector3.down * 0.4f, 2.0f, 1.9f, 1.4f, 10, grey * 0.85f);
            mb.Cylinder(m + Vector3.up * 1.0f, 1.8f, 1.8f, 0.5f, 10, grey);
            mb.Cylinder(m + Vector3.up * 1.5f, 1.6f, 1.5f, 0.18f, 10, grey * 0.8f);
            mb.Cylinder(m + Vector3.up * 1.0f, 1.82f, 1.82f, 0.08f, 10, st.Accent, false);
            ModelKit.Beam(mb, m + new Vector3(0, 1.8f, 0), m + new Vector3(1.9f, 1.8f, 0), 0.1f, ModelKit.DarkWood);
            mb.Box(m + new Vector3(0, 2.6f, 0), new Vector3(0.18f, 1.6f, 0.18f), ModelKit.DarkWood);
            // Hopper above the stone.
            mb.Cylinder(m + new Vector3(0, 2.2f, 1.1f), 0.2f, 0.65f, 0.9f, 4, ModelKit.Wood);
            rig.HasWheel = true; rig.Wheel = m + new Vector3(2.7f, 1.5f, 0); rig.WheelR = 1.4f; rig.WheelSpeed = 70f;
            mb.Box(m + new Vector3(2.7f, 0.6f, 0), new Vector3(0.4f, 1.3f, 0.4f), st.Frame);
            mb.Box(m + new Vector3(2.7f, 0.06f, 0), new Vector3(1.0f, 0.12f, 3.0f), st.Frame);
            // Mortar sacks, a heap of lime and blocks.
            Sack(mb, new Vector3(-2.7f, 0, 1.9f), new Color(0.66f, 0.64f, 0.58f));
            Sack(mb, new Vector3(-2.0f, 0, 2.4f), new Color(0.68f, 0.66f, 0.60f), 0.9f);
            ModelKit.Lump(mb, new Vector3(-3.5f, 0, 1.3f), 0.8f, 0.6f, new Color(0.84f, 0.82f, 0.74f));
            ModelKit.BlockStack(mb, new Vector3(3.6f, 0, 2.4f), 3, 0.6f, ModelKit.StoneCol, 10f);
            rig.Radius = 5.5f;
        }

        private static void Foundry(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ProcessorBase(mb, st, rig);
            // A wide furnace on a casting floor: two short stacks, a tilting crucible, a row of ingot moulds with bronze ingots.
            var f = new Vector3(1.4f, 0, -0.9f);
            var brickc = st.Stone ? new Color(0.40f, 0.33f, 0.30f) : new Color(0.62f, 0.38f, 0.26f);
            mb.Box(f + new Vector3(0, 0.9f, 0), new Vector3(3.6f, 2.2f, 2.4f), brickc);
            mb.Box(f + new Vector3(0, 2.1f, 0), new Vector3(3.9f, 0.25f, 2.7f), st.Accent);
            ModelKit.Chimney(mb, f + new Vector3(-1.0f, 2.2f, -0.4f), 2.0f, 0.45f, brickc * 0.85f);
            ModelKit.Chimney(mb, f + new Vector3(1.0f, 2.2f, -0.4f), 1.4f, 0.4f, brickc * 0.85f);
            rig.Smoke.Add(f + new Vector3(-1.0f, 4.4f, -0.4f)); rig.Smoke.Add(f + new Vector3(1.0f, 3.8f, -0.4f));
            rig.SmokeColour = new Color(0.40f, 0.38f, 0.38f);
            mb.Box(f + new Vector3(0, 0.75f, 1.25f), new Vector3(1.5f, 1.1f, 0.3f), ModelKit.Soot);
            rig.HasGlow = true; rig.Glow = f + new Vector3(0, 0.7f, 1.42f); rig.GlowSize = new Vector3(1.1f, 0.8f, 0.1f);
            var bronze = EconomyView.WareColour(Ware.Bronze);
            // Crucible on a stand.
            var cr = f + new Vector3(2.9f, 0, 0.6f);
            ModelKit.Beam(mb, cr + new Vector3(-0.5f, 0, 0), cr + new Vector3(0, 1.3f, 0), 0.07f, ModelKit.DarkWood);
            ModelKit.Beam(mb, cr + new Vector3(0.5f, 0, 0), cr + new Vector3(0, 1.3f, 0), 0.07f, ModelKit.DarkWood);
            mb.Cylinder(cr + Vector3.up * 1.0f, 0.3f, 0.5f, 0.7f, 8, ModelKit.Soot * 1.4f);
            mb.Cylinder(cr + Vector3.up * 1.68f, 0.46f, 0.46f, 0.04f, 8, bronze * 1.15f);
            // Moulds and ingots.
            for (int i = 0; i < 4; i++)
            {
                mb.Box(new Vector3(-2.8f + i * 0.75f, 0.1f, 2.3f), new Vector3(0.6f, 0.2f, 1.0f), ModelKit.Soot * 1.5f);
                mb.Box(new Vector3(-2.8f + i * 0.75f, 0.24f, 2.3f), new Vector3(0.46f, 0.1f, 0.84f), bronze * (0.95f + 0.05f * (i % 2)));
            }
            for (int l = 0; l < 3; l++) for (int i = 0; i < 3 - l; i++)
                mb.Box(new Vector3(3.2f + i * 0.5f + l * 0.25f, 0.1f + l * 0.18f, 2.5f), new Vector3(0.44f, 0.16f, 0.9f), bronze * (0.9f + 0.06f * ((i + l) % 2)));
            Anvil(mb, new Vector3(-0.4f, 0, 2.4f), 90f);
            rig.Height = 6.6f;
        }

        private static void PotteryShop(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ProcessorBase(mb, st, rig);
            // A beehive kiln dome in terracotta with its chimney and a glowing mouth.
            var k = new Vector3(1.6f, 0, -0.8f);
            var body = st.Stone ? new Color(0.46f, 0.34f, 0.30f) : new Color(0.86f, 0.55f, 0.40f);
            Dome(mb, k, 2.0f, body);
            mb.Cylinder(k + Vector3.up * 0.85f, 2.05f, 2.05f, 0.1f, 12, st.Accent, false);
            mb.Cone(k + Vector3.up * 2.7f, 0.5f, 0.55f, 8, body * 0.8f);
            ModelKit.Chimney(mb, k + new Vector3(0, 3.1f, 0), 1.2f, 0.32f, body * 0.85f);
            rig.Smoke.Add(k + new Vector3(0, 4.5f, 0));
            rig.SmokeColour = new Color(0.78f, 0.76f, 0.74f);
            mb.Box(k + new Vector3(0, 0.7f, 1.95f), new Vector3(1.1f, 1.2f, 0.3f), ModelKit.Soot);
            rig.HasGlow = true; rig.Glow = k + new Vector3(0, 0.65f, 2.12f); rig.GlowSize = new Vector3(0.8f, 0.8f, 0.1f);
            // A potter's wheel with a pot on it, a drying shelf with rows of pots and big amphorae.
            var pc = EconomyView.WareColour(Ware.Pottery);
            var wh = new Vector3(-2.4f, 0, 1.8f);
            mb.Cylinder(wh, 0.3f, 0.3f, 0.7f, 8, ModelKit.DarkWood);
            mb.Cylinder(wh + Vector3.up * 0.7f, 0.5f, 0.5f, 0.1f, 10, ModelKit.Wood);
            mb.Cylinder(wh + Vector3.up * 0.8f, 0.3f, 0.2f, 0.45f, 8, pc);
            var sh = new Vector3(-0.2f, 0, 3.0f);
            for (int l = 0; l < 2; l++)
            {
                mb.Box(sh + new Vector3(0, 0.5f + l * 0.9f, 0), new Vector3(3.2f, 0.1f, 0.7f), ModelKit.Plank);
                for (int i = 0; i < 5; i++) mb.Cylinder(sh + new Vector3(-1.2f + i * 0.6f, 0.55f + l * 0.9f, 0), 0.18f, 0.12f, 0.42f, 7, pc * (0.9f + 0.05f * ((i + l) % 3)));
            }
            foreach (float x in new[] { -1.6f, 1.6f }) mb.Box(sh + new Vector3(x, 0.9f, 0), new Vector3(0.12f, 1.8f, 0.12f), ModelKit.DarkWood);
            for (int i = 0; i < 3; i++)
            {
                var a = new Vector3(4.0f + (i % 2) * 0.2f, 0, 1.0f + i * 0.9f);
                mb.Cylinder(a, 0.3f, 0.5f, 0.7f, 8, pc * (0.88f + 0.06f * i));
                mb.Cylinder(a + Vector3.up * 0.7f, 0.5f, 0.2f, 0.5f, 8, pc * (0.88f + 0.06f * i));
            }
            ModelKit.Lump(mb, new Vector3(-3.9f, 0, 0.8f), 0.8f, 0.7f, ModelKit.ClayCol);
        }

        private static void SteelForge(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ProcessorBase(mb, st, rig);
            // An open forge shed with a big stone hearth and tall square chimney, an anvil, bellows and a quench trough.
            var f = new Vector3(1.5f, 0, -0.9f);
            var stone = st.Stone ? new Color(0.34f, 0.33f, 0.34f) : new Color(0.52f, 0.52f, 0.54f);
            foreach (var o in new[] { new Vector3(-2.0f, 0, -1.4f), new Vector3(2.0f, 0, -1.4f), new Vector3(2.0f, 0, 1.4f), new Vector3(-2.0f, 0, 1.4f) })
                mb.Box(f + o + Vector3.up * 1.5f, new Vector3(0.26f, 3.0f, 0.26f), st.Frame);
            ModelKit.GableX(mb, f + Vector3.up * 3.0f, 4.8f, 3.6f, 1.5f, st.Roof, st.RoofEdge, st.Accent, st.Stone ? 0.15f : 0.3f);
            mb.Box(f + new Vector3(0, 0.6f, -0.9f), new Vector3(2.4f, 1.2f, 1.5f), stone);
            mb.Box(f + new Vector3(0, 3.0f, -1.2f), new Vector3(1.4f, 4.4f, 1.0f), stone * 0.95f);
            mb.Box(f + new Vector3(0, 5.3f, -1.2f), new Vector3(1.7f, 0.3f, 1.3f), st.Accent);
            rig.Smoke.Add(f + new Vector3(0, 5.7f, -1.2f));
            rig.SmokeColour = new Color(0.36f, 0.35f, 0.36f);
            mb.Box(f + new Vector3(0, 1.0f, -0.12f), new Vector3(1.4f, 0.6f, 0.2f), ModelKit.Soot);
            rig.HasGlow = true; rig.Glow = f + new Vector3(0, 1.0f, 0.0f); rig.GlowSize = new Vector3(1.1f, 0.45f, 0.1f);
            Anvil(mb, f + new Vector3(0.1f, 0, 1.0f), 0f);
            var steel = EconomyView.WareColour(Ware.Steel);
            mb.Box(f + new Vector3(-1.7f, 0.7f, 0.4f), new Vector3(0.9f, 0.5f, 1.4f), new Color(0.45f, 0.30f, 0.18f), 15f);
            // Quench trough and steel bars.
            mb.Box(new Vector3(-2.9f, 0.3f, 2.4f), new Vector3(1.8f, 0.6f, 0.9f), ModelKit.Wood);
            mb.Box(new Vector3(-2.9f, 0.58f, 2.4f), new Vector3(1.5f, 0.04f, 0.6f), new Color(0.22f, 0.34f, 0.44f));
            for (int l = 0; l < 3; l++) for (int i = 0; i < 4 - l; i++)
                mb.Box(new Vector3(3.6f + i * 0.3f + l * 0.15f, 0.08f + l * 0.14f, 2.0f), new Vector3(0.24f, 0.13f, 1.4f), steel * (1f + 0.07f * ((i + l) % 2)));
            rig.Height = 7.0f;
        }

        private static void Tannery(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ProcessorBase(mb, st, rig);
            // Drying racks with hides hung out, and round tanning vats of dark liquor.
            var hide = EconomyView.WareColour(Ware.Leather);
            for (int r = 0; r < 2; r++)
            {
                var rk = new Vector3(0.3f + r * 3.2f, 0, -1.0f);
                foreach (float x in new[] { -1.2f, 1.2f }) mb.Box(rk + new Vector3(x, 1.3f, 0), new Vector3(0.16f, 2.6f, 0.16f), ModelKit.DarkWood);
                mb.Box(rk + new Vector3(0, 2.55f, 0), new Vector3(2.7f, 0.14f, 0.16f), ModelKit.DarkWood);
                mb.Box(rk + new Vector3(0, 1.2f, 0), new Vector3(2.5f, 0.1f, 0.12f), ModelKit.DarkWood);
                for (int i = 0; i < 3; i++)
                {
                    var hc = hide * (0.85f + 0.12f * ((i + r) % 3));
                    mb.Box(rk + new Vector3(-0.8f + i * 0.8f, 1.7f, 0.05f), new Vector3(0.62f, 1.6f, 0.06f), hc);
                    mb.Box(rk + new Vector3(-0.8f + i * 0.8f, 0.85f, 0.05f), new Vector3(0.38f, 0.4f, 0.06f), hc);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                var v = new Vector3(-2.6f + i * 1.7f, 0, 2.5f);
                mb.Cylinder(v, 0.7f, 0.75f, 0.9f, 9, ModelKit.Wood * 0.95f);
                mb.Cylinder(v + Vector3.up * 0.82f, 0.62f, 0.62f, 0.06f, 9, new Color(0.30f, 0.20f, 0.12f));
                mb.Cylinder(v + Vector3.up * 0.55f, 0.76f, 0.76f, 0.08f, 9, ModelKit.Iron, false);
            }
            // A folded stack of finished leather and a scraping beam.
            for (int l = 0; l < 3; l++) mb.Box(new Vector3(3.8f, 0.1f + l * 0.14f, 2.6f), new Vector3(1.0f, 0.12f, 0.7f), hide * (0.95f + 0.05f * l), l * 12f);
            ModelKit.Beam(mb, new Vector3(-3.9f, 0.1f, 1.0f), new Vector3(-3.2f, 1.2f, 1.4f), 0.12f, ModelKit.Wood);
            rig.HasWorker = true; rig.Worker = new Vector3(1.2f, 0, 1.5f); rig.WorkerYaw = 180f; rig.WorkerTool = ToolKind.Hammer;
        }

        private static void Weaver(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ProcessorBase(mb, st, rig);
            // A tall upright loom: two posts, a top beam, rows of warp threads and a woven panel in the cloth colour.
            var cloth = EconomyView.WareColour(Ware.Cloth);
            var l0 = new Vector3(1.4f, 0, -0.4f);
            foreach (float x in new[] { -1.4f, 1.4f }) mb.Box(l0 + new Vector3(x, 1.7f, 0), new Vector3(0.22f, 3.4f, 0.22f), ModelKit.DarkWood);
            mb.Box(l0 + new Vector3(0, 3.4f, 0), new Vector3(3.2f, 0.22f, 0.3f), ModelKit.Wood);
            mb.Box(l0 + new Vector3(0, 0.5f, 0), new Vector3(3.0f, 0.18f, 0.3f), ModelKit.Wood);
            mb.Box(l0 + new Vector3(0, 0.1f, 0.4f), new Vector3(3.4f, 0.2f, 1.2f), ModelKit.Plank * 0.9f);
            for (int i = 0; i < 11; i++) mb.Box(l0 + new Vector3(-1.2f + i * 0.24f, 1.95f, 0), new Vector3(0.03f, 2.9f, 0.03f), new Color(0.92f, 0.90f, 0.82f));
            for (int i = 0; i < 6; i++)
                mb.Box(l0 + new Vector3(0, 0.75f + i * 0.17f, 0.02f), new Vector3(2.7f, 0.15f, 0.06f), cloth * (i % 2 == 0 ? 1.0f : 0.84f));
            mb.Box(l0 + new Vector3(0, 1.9f, 0.34f), new Vector3(2.6f, 0.1f, 0.1f), ModelKit.Wood);
            // Spools of thread and bolts of finished cloth.
            for (int i = 0; i < 4; i++)
            {
                var c = Mix(cloth, i % 2 == 0 ? st.Accent : new Color(0.92f, 0.84f, 0.60f), 0.5f);
                mb.Cylinder(new Vector3(-3.4f + i * 0.55f, 0, 2.2f), 0.2f, 0.2f, 0.6f, 6, c);
                mb.Cylinder(new Vector3(-3.4f + i * 0.55f, 0.6f, 2.2f), 0.2f, 0.06f, 0.1f, 6, ModelKit.Wood);
            }
            for (int l = 0; l < 3; l++) mb.Box(new Vector3(3.5f, 0.2f + l * 0.38f, 2.4f), new Vector3(1.6f, 0.34f, 0.9f), cloth * (0.82f + 0.1f * l), l * 8f);
            // Grain sheaves hanging to dry: the weaver spins from stalk fibre and is fed grain.
            ModelKit.Sheaf(mb, new Vector3(-2.2f, 0, 3.4f), 1.2f);
            rig.HasWorker = true; rig.Worker = l0 + new Vector3(0, 0, 1.6f); rig.WorkerYaw = 180f; rig.WorkerTool = ToolKind.Hammer;
            rig.Height = 6.0f;
        }

        // ---------------------------------------------------------------- armouries

        private static Ware GearOf(string kind)
        {
            switch (kind)
            {
                case BuildingKinds.Swordsmith: return Ware.Sword;
                case BuildingKinds.Spearmaker: return Ware.Spear;
                case BuildingKinds.Axesmith: return Ware.Axe;
                case BuildingKinds.Bowyer: return Ware.Bow;
                case BuildingKinds.Slingmaker: return Ware.Sling;
                case BuildingKinds.Shieldwright: return Ware.BigShield;
                case BuildingKinds.Helmsmith: return Ware.Helmet;
                case BuildingKinds.Bucklersmith: return Ware.SmallShield;
                case BuildingKinds.Armourer: return Ware.Breastplate;
                default: return Ware.LightArmour;
            }
        }

        private static bool IsSmithy(Ware gear) => gear == Ware.Sword || gear == Ware.Spear || gear == Ware.Axe || gear == Ware.Helmet || gear == Ware.SmallShield || gear == Ware.Breastplate;

        private readonly struct Fr
        {
            private readonly Vector3 _c; private readonly float _s;
            public Fr(Vector3 c, float side) { _c = c; _s = side; }
            public Vector3 P(float x, float y, float z = 0f) => new Vector3(_c.x + x * _s, _c.y + y, _c.z + z * _s);
        }

        private static void Armoury(MeshBuilder mb, string kind, ModelStyle st, BuildingRig rig)
        {
            var gear = GearOf(kind);
            var gc = EconomyView.WareColour(gear);
            var st2 = Reroof(st, Mix(st.Roof, gc, 0.55f), Mix(st.RoofEdge, gc, 0.4f) * 0.85f);
            ModelKit.House(mb, new Vector3(-1.9f, 0, -2.3f), 4.6f, 3.2f, 2.5f, 2.0f, st2, true, 1.2f);
            mb.Box(new Vector3(-1.9f, 2.55f, -0.64f), new Vector3(4.7f, 0.22f, 0.12f), gc); // a band of the gear's colour along the eave
            Banner(mb, rig, new Vector3(-4.7f, 0, -0.3f), 5.6f, st);
            rig.Height = 6.2f; rig.Radius = 5.0f;

            bool smith = IsSmithy(gear);
            if (smith)
            {
                ModelKit.Chimney(mb, new Vector3(-3.3f, 3.0f, -3.0f), 1.9f, 0.4f, st.Stone ? st.Wall * 0.9f : ModelKit.Rock);
                rig.Smoke.Add(new Vector3(-3.3f, 5.0f, -3.0f));
                rig.SmokeColour = new Color(0.46f, 0.44f, 0.44f);
                var hearth = new Vector3(1.9f, 0, -2.6f);
                mb.Box(hearth + new Vector3(0, 0.55f, 0), new Vector3(1.6f, 1.1f, 1.3f), ModelKit.StoneCol * 0.8f);
                mb.Box(hearth + new Vector3(0, 1.15f, 0), new Vector3(1.8f, 0.14f, 1.5f), st.Accent);
                mb.Box(hearth + new Vector3(0, 0.65f, 0.67f), new Vector3(0.9f, 0.6f, 0.1f), ModelKit.Soot);
                rig.HasGlow = true; rig.Glow = hearth + new Vector3(0, 0.62f, 0.76f); rig.GlowSize = new Vector3(0.7f, 0.5f, 0.1f);
                Anvil(mb, new Vector3(0.7f, 0, 0.2f), 90f);
                mb.Box(new Vector3(0.7f, 0.04f, 1.3f), new Vector3(0.9f, 0.4f, 0.5f), new Color(0.22f, 0.34f, 0.44f)); // quench bucket
            }
            else
            {
                Bench(mb, new Vector3(0.9f, 0, 0.2f), 2.4f, 90f);
                for (int i = 0; i < 3; i++) mb.Box(new Vector3(0.8f + i * 0.28f - 0.3f, 1.0f, 0.2f + (i - 1) * 0.5f), new Vector3(0.12f, 0.14f, 0.5f), gc * 0.9f, i * 25f);
                ModelKit.Lump(mb, new Vector3(3.2f, 0, -1.8f), 0.7f, 0.5f, ModelKit.Wood);
                mb.Cylinder(new Vector3(3.2f, 0, -0.6f), 0.4f, 0.4f, 0.5f, 8, ModelKit.Plank);
            }
            // The sign on a pole, double sided: a wooden board with the gear on it, in the gear's colours.
            var pole = new Vector3(3.7f, 0, 1.4f);
            mb.Box(pole + new Vector3(0, 1.8f, 0), new Vector3(0.2f, 3.6f, 0.2f), ModelKit.DarkWood);
            mb.Box(pole + new Vector3(0, 2.6f, 0), new Vector3(2.4f, 2.4f, 0.14f), ModelKit.Plank * 0.85f);
            mb.Box(pole + new Vector3(0, 3.9f, 0), new Vector3(2.6f, 0.2f, 0.2f), st.Accent);
            foreach (float side in new[] { 1f, -1f })
                GearIcon(mb, gear, new Fr(pole + new Vector3(0, 2.6f, 0.1f * side), side), gc, st);
            // The finished gear stacked by the door.
            mb.Box(new Vector3(-0.2f, 0.25f, 1.1f), new Vector3(0.9f, 0.5f, 0.9f), ModelKit.Wood);
            mb.Box(new Vector3(-0.2f, 0.58f, 1.1f), new Vector3(0.7f, 0.16f, 0.7f), gc * 0.95f, 15f);
        }

        /// <summary>A large picture of the gear in the XY plane (about 2 units across) on a sign.</summary>
        private static void GearIcon(MeshBuilder mb, Ware gear, Fr f, Color gc, ModelStyle st)
        {
            var steel = new Color(0.82f, 0.85f, 0.92f);
            var wood = ModelKit.Wood; var dark = ModelKit.DarkWood;
            switch (gear)
            {
                case Ware.Sword:
                    ModelKit.Beam(mb, f.P(0, -0.35f), f.P(0, 0.75f), 0.12f, steel);
                    mb.Cone(f.P(0, 0.75f), 0.16f, 0.4f, 4, steel);
                    mb.Box(f.P(0, -0.38f), new Vector3(0.9f, 0.13f, 0.14f), gc);
                    ModelKit.Beam(mb, f.P(0, -0.45f), f.P(0, -0.85f), 0.08f, dark);
                    mb.Box(f.P(0, -0.92f), new Vector3(0.2f, 0.16f, 0.16f), gc);
                    break;
                case Ware.Spear:
                    ModelKit.Beam(mb, f.P(-0.15f, -1.0f), f.P(0.15f, 0.7f), 0.06f, wood);
                    mb.Cone(f.P(0.15f, 0.7f), 0.2f, 0.5f, 4, steel);
                    mb.Box(f.P(0.14f, 0.62f), new Vector3(0.34f, 0.06f, 0.1f), gc);
                    break;
                case Ware.Axe:
                    ModelKit.Beam(mb, f.P(-0.3f, -0.95f), f.P(0.25f, 0.75f), 0.07f, wood);
                    mb.Box(f.P(0.55f, 0.5f), new Vector3(0.8f, 0.7f, 0.12f), Mix(steel, gc, 0.25f));
                    mb.Box(f.P(0.98f, 0.5f), new Vector3(0.14f, 0.95f, 0.12f), steel);
                    mb.Box(f.P(0.2f, 0.7f), new Vector3(0.3f, 0.35f, 0.14f), dark);
                    break;
                case Ware.Bow:
                    {
                        Vector3 prev = f.P(0, 0);
                        for (int i = 0; i <= 8; i++)
                        {
                            float t = Mathf.Lerp(-1.1f, 1.1f, i / 8f);
                            var p = f.P(0.5f * Mathf.Cos(t) - 0.2f, 1.0f * Mathf.Sin(t));
                            if (i > 0) ModelKit.Beam(mb, prev, p, 0.07f, wood * 1.1f);
                            prev = p;
                        }
                        ModelKit.Beam(mb, f.P(0.5f * Mathf.Cos(-1.1f) - 0.2f, -0.89f), f.P(0.5f * Mathf.Cos(1.1f) - 0.2f, 0.89f), 0.02f, new Color(0.9f, 0.88f, 0.8f));
                        ModelKit.Beam(mb, f.P(-0.55f, -0.1f), f.P(0.55f, -0.1f), 0.03f, wood);
                        mb.Cone(f.P(0.55f, -0.1f), 0.08f, 0.0f + 0.2f, 4, steel);
                    }
                    break;
                case Ware.Sling:
                    ModelKit.Beam(mb, f.P(-0.5f, 0.8f), f.P(0f, -0.2f), 0.05f, gc);
                    ModelKit.Beam(mb, f.P(0.5f, 0.8f), f.P(0f, -0.2f), 0.05f, gc);
                    Disc(mb, f.P(0, -0.4f), 0.3f, gc * 0.85f, 10);
                    Disc(mb, f.P(0, -0.4f, 0.03f), 0.15f, new Color(0.7f, 0.7f, 0.72f), 8);
                    ModelKit.Beam(mb, f.P(0f, -0.65f), f.P(0.2f, -1.05f), 0.04f, gc);
                    break;
                case Ware.BigShield:
                    Disc(mb, f.P(0, 0), 0.95f, ModelKit.Iron, 16);
                    Disc(mb, f.P(0, 0, 0.04f), 0.8f, gc, 16);
                    mb.Box(f.P(0, 0, 0.08f), new Vector3(1.4f, 0.2f, 0.06f), Mix(gc, Color.white, 0.5f));
                    mb.Box(f.P(0, 0, 0.08f), new Vector3(0.2f, 1.4f, 0.06f), Mix(gc, Color.white, 0.5f));
                    Disc(mb, f.P(0, 0, 0.12f), 0.22f, steel, 8);
                    break;
                case Ware.SmallShield:
                    Disc(mb, f.P(0, 0), 0.62f, ModelKit.Iron, 14);
                    Disc(mb, f.P(0, 0, 0.04f), 0.5f, gc, 14);
                    Disc(mb, f.P(0, 0, 0.1f), 0.17f, steel, 8);
                    break;
                case Ware.Helmet:
                    mb.Cylinder(f.P(0, -0.7f), 0.62f, 0.56f, 0.8f, 10, steel * 0.92f);
                    mb.Cone(f.P(0, 0.1f), 0.56f, 0.6f, 10, steel);
                    mb.Cylinder(f.P(0, -0.74f), 0.74f, 0.74f, 0.1f, 10, Mix(steel, gc, 0.4f), false);
                    mb.Box(f.P(0, -0.35f, 0.5f), new Vector3(0.14f, 0.7f, 0.1f), steel * 0.8f);
                    mb.Box(f.P(0, 0.75f), new Vector3(0.14f, 0.5f, 0.5f), st.Accent);
                    break;
                case Ware.Breastplate:
                    mb.Box(f.P(0, 0, 0), new Vector3(1.1f, 1.3f, 0.2f), Mix(steel, gc, 0.3f));
                    mb.Box(f.P(0, -0.55f), new Vector3(1.2f, 0.18f, 0.24f), gc);
                    foreach (float x in new[] { -0.55f, 0.55f }) mb.Box(f.P(x, 0.7f), new Vector3(0.38f, 0.28f, 0.24f), steel);
                    mb.Box(f.P(0, 0.05f, 0.12f), new Vector3(0.1f, 1.0f, 0.06f), steel * 0.8f);
                    break;
                default: // light armour: a leather jerkin with sleeves and lacing
                    mb.Box(f.P(0, -0.05f), new Vector3(0.9f, 1.2f, 0.18f), gc);
                    foreach (float x in new[] { -0.62f, 0.62f }) mb.Box(f.P(x, 0.35f), new Vector3(0.36f, 0.5f, 0.18f), gc * 0.9f);
                    mb.Box(f.P(0, 0.62f), new Vector3(0.36f, 0.14f, 0.2f), ModelKit.Skin);
                    for (int i = 0; i < 4; i++) ModelKit.Beam(mb, f.P(-0.12f, 0.35f - i * 0.28f, 0.1f), f.P(0.12f, 0.2f - i * 0.28f, 0.1f), 0.025f, new Color(0.9f, 0.84f, 0.66f));
                    mb.Box(f.P(0, -0.5f), new Vector3(1.0f, 0.12f, 0.22f), ModelKit.DarkWood);
                    break;
            }
        }

        // ---------------------------------------------------------------- harvester levels

        private static readonly Color Level2Roof = new Color(0.64f, 0.30f, 0.22f), Level2Edge = new Color(0.46f, 0.21f, 0.15f);
        private static readonly Color Level3Roof = new Color(0.27f, 0.36f, 0.56f), Level3Edge = new Color(0.20f, 0.26f, 0.42f);
        private static readonly Color Gold = new Color(0.95f, 0.78f, 0.28f);

        /// <summary>Adds what a level 2 or 3 harvester has over level 1: a roofed annexe, a bigger stockpile and, at level 3, gold-capped gate posts and a second annexe.</summary>
        private static void HarvesterLevel(MeshBuilder mb, string kind, ModelStyle st, BuildingRig rig, int level)
        {
            var roof = level >= 3 ? Level3Roof : Level2Roof; var edge = level >= 3 ? Level3Edge : Level2Edge;
            var st2 = Reroof(st, roof, edge);
            bool medium = kind == BuildingKinds.Farm;
            float back = medium ? -5.2f : -4.6f;
            ModelKit.House(mb, new Vector3(medium ? -2.6f : 0.2f, 0, back), 3.4f, 2.4f, 1.9f, 1.7f, st2, false);
            mb.Box(new Vector3(medium ? -2.6f : 0.2f, 1.95f, back + 1.2f), new Vector3(3.5f, 0.16f, 0.1f), Gold * (level >= 3 ? 1f : 0.7f));

            // The stockpile for what it gathers.
            var p = medium ? new Vector3(-6.0f, 0, 1.6f) : new Vector3(4.3f, 0, -1.2f);
            switch (kind)
            {
                case BuildingKinds.Woodcutter:
                    ModelKit.LogStack(mb, p, 2.8f, level >= 3 ? 4 : 3, 0f);
                    if (level >= 3) ModelKit.PlankStack(mb, p + new Vector3(0, 0, 2.2f), 2.6f, 4, 0f);
                    break;
                case BuildingKinds.ClayPit:
                    ModelKit.BrickStack(mb, p, level >= 3 ? 6 : 4, 0f);
                    ModelKit.BrickStack(mb, p + new Vector3(0, 0, 1.2f), 3, 90f);
                    for (int i = 0; i < level + 1; i++) ModelKit.Lump(mb, p + new Vector3(1.0f + i * 0.5f, 0, -0.4f), 0.5f, 0.45f, ModelKit.ClayCol, 5, i);
                    break;
                case BuildingKinds.Quarry:
                    ModelKit.BlockStack(mb, p, level >= 3 ? 6 : 3, 0.7f, ModelKit.StoneCol, 5f);
                    ModelKit.BlockStack(mb, p + new Vector3(0, 0, 1.6f), 3, 0.7f, ModelKit.StoneCol * 0.95f, -8f);
                    break;
                case BuildingKinds.Mine:
                    p = new Vector3(-3.6f, 0, -0.4f);
                    for (int i = 0; i < 3 + level * 2; i++) ModelKit.Lump(mb, p + new Vector3((i % 3) * 0.6f, 0, (i / 3) * 0.55f), 0.45f, 0.5f, ModelKit.OreCol * (0.9f + 0.05f * (i % 3)), 5, i);
                    // A timber headframe with a pulley over the shaft.
                    ModelKit.Beam(mb, new Vector3(-1.4f, 0, 2.6f), new Vector3(-1.0f, 3.2f, 3.0f), 0.1f, st.Frame);
                    ModelKit.Beam(mb, new Vector3(-0.6f, 0, 2.6f), new Vector3(-1.0f, 3.2f, 3.0f), 0.1f, st.Frame);
                    mb.Cylinder(new Vector3(-1.0f, 3.0f, 3.0f), 0.3f, 0.3f, 0.2f, 8, ModelKit.Iron);
                    break;
                default: // farm: hay bales and sacks of grain
                    for (int i = 0; i < 2 + level; i++) mb.Box(p + new Vector3((i % 3) * 1.1f, 0.4f + (i / 3) * 0.8f, 0), new Vector3(1.0f, 0.8f, 0.8f), ModelKit.Straw * (0.9f + 0.05f * (i % 3)), i * 6f);
                    Sack(mb, p + new Vector3(0, 0, 1.6f), new Color(0.88f, 0.78f, 0.52f));
                    Sack(mb, p + new Vector3(0.9f, 0, 1.8f), new Color(0.88f, 0.78f, 0.52f), 0.9f);
                    break;
            }

            if (level >= 3)
            {
                // Gate posts capped in gold, a second small annexe and a gold finial: the richest harvester.
                foreach (float x in new[] { -2.0f, 2.0f })
                {
                    mb.Box(new Vector3(x, 0.8f, 4.0f), new Vector3(0.3f, 1.6f, 0.3f), st.Frame);
                    mb.Cone(new Vector3(x, 1.6f, 4.0f), 0.28f, 0.45f, 6, Gold);
                }
                var sp = medium ? new Vector3(2.6f, 0, -5.4f) : new Vector3(-4.6f, 0, -3.4f);
                ModelKit.House(mb, sp, 2.6f, 2.0f, 1.6f, 1.4f, st2, false);
                rig.Height += 0.9f;
            }
            else rig.Height += 0.4f;
            rig.Radius += level >= 3 ? 1.6f : 0.9f;
        }
    }
}
