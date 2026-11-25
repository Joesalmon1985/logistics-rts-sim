using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>Where a building model's moving parts sit (local space, front +Z).</summary>
    public sealed class BuildingRig
    {
        /// <summary>Top of the banner pole: the cloth hangs from here (lowered when paused).</summary>
        public Vector3 BannerTop;
        /// <summary>Chimneys: working smoke rises from these.</summary>
        public readonly List<Vector3> Smoke = new List<Vector3>();
        /// <summary>Dark or light working smoke.</summary>
        public Color SmokeColour = new Color(0.82f, 0.82f, 0.80f);
        /// <summary>HQ hearth: a thin smoke even when idle.</summary>
        public bool AlwaysSmoke;
        public bool HasWheel; public Vector3 Wheel; public float WheelR; public float WheelSpeed = 90f;
        public bool HasWorker; public Vector3 Worker; public float WorkerYaw; public ToolKind WorkerTool;
        public bool HasGlow; public Vector3 Glow; public Vector3 GlowSize;
        /// <summary>Height of the tallest part (bars and markers go above it).</summary>
        public float Height;
        /// <summary>Footprint radius (rubble, collapse dust, damage smoke spread).</summary>
        public float Radius;
    }

    /// <summary>
    /// P4b: one procedural low-poly model per building kind, in two styles (player timber and thatch, enemy stone and
    /// slate), each recognisable by its silhouette and a prop: log pile and axe stump, clay pit, stone blocks and hoist,
    /// mine mouth in a mound, barn and field, saw shed with a wheel, dome kiln, furnace, barracks yard with a spear rack,
    /// wheelwright's shed with a big door. The faction colour shows only on the ridge cap, eave band and banner.
    /// Local space, feet at y = 0, the door (+Z) faces the building's flag. View code only.
    /// </summary>
    public static partial class BuildingModels
    {
        /// <summary>Kinds with their own model (the HQ uses <see cref="TerritoryTuning.HqKind"/>).</summary>
        public static readonly string[] Kinds =
        {
            TerritoryTuning.HqKind, BuildingKinds.Woodcutter, BuildingKinds.ClayPit, BuildingKinds.Quarry, BuildingKinds.Mine,
            BuildingKinds.Farm, BuildingKinds.Sawmill, BuildingKinds.Kiln, BuildingKinds.Smelter, BuildingKinds.Barracks,
            BuildingKinds.VehicleWorkshop,
        };

        private static readonly Dictionary<string, BuildingRig> Rigs = new Dictionary<string, BuildingRig>();

        public static string Key(string kind, Faction f) => "bld/" + (kind ?? "placeholder") + "/" + (int)f;

        /// <summary>The shared static mesh of a kind for a faction.</summary>
        public static Mesh MeshOf(string kind, Faction f)
        {
            string key = Key(kind, f);
            return ModelKit.Cached(key, mb => { var rig = new BuildingRig(); Build(mb, kind, ModelStyle.Of(f), rig); Rigs[key] = rig; });
        }

        public static BuildingRig RigOf(string kind, Faction f)
        {
            string key = Key(kind, f);
            if (!Rigs.TryGetValue(key, out var r)) { MeshOf(kind, f); r = Rigs[key]; }
            return r;
        }

        /// <summary>The banner cloth of a faction (a separate part so it can be lowered).</summary>
        public static Mesh BannerMesh(Faction f) => ModelKit.Cached("banner/" + (int)f, mb => ModelKit.BannerCloth(mb, ModelKit.FactionColour(f)));

        public static Mesh WheelMesh(string kind, Faction f, float r)
            => ModelKit.Cached("wheel/" + kind + "/" + (int)f, mb =>
            {
                var st = ModelStyle.Of(f);
                if (kind == BuildingKinds.Sawmill && st.Stone) { ModelKit.Wheel(mb, r, 0.3f, ModelKit.Iron, ModelKit.Iron * 0.8f, 8); return; }
                ModelKit.Wheel(mb, r, 0.26f, ModelKit.Wood, ModelKit.DarkWood, kind == BuildingKinds.Sawmill ? 8 : 6);
                if (kind == BuildingKinds.Sawmill)
                    for (int i = 0; i < 8; i++) // paddles
                    {
                        float a = i * Mathf.PI * 2f / 8f;
                        mb.Box(new Vector3(0, Mathf.Sin(a) * r, Mathf.Cos(a) * r), new Vector3(0.5f, 0.1f, 0.35f), ModelKit.Wood * 1.1f);
                    }
            });

        public static Mesh GlowMesh(Vector3 size)
            => ModelKit.Cached("glow/" + size.x.ToString("0.00") + size.y.ToString("0.00"), mb =>
            {
                mb.Box(Vector3.zero, size, new Color(1f, 0.62f, 0.2f));
                mb.Box(new Vector3(0, -size.y * 0.25f, 0.02f), new Vector3(size.x * 0.7f, size.y * 0.4f, size.z), new Color(1f, 0.85f, 0.4f));
            });

        public static void Build(MeshBuilder mb, string kind, ModelStyle st, BuildingRig rig)
        {
            switch (kind)
            {
                case TerritoryTuning.HqKind: Hq(mb, st, rig); break;
                case CitadelKind: Citadel(mb, st, rig); break;
                case BuildingKinds.Woodcutter: Woodcutter(mb, st, rig); break;
                case BuildingKinds.ClayPit: ClayPit(mb, st, rig); break;
                case BuildingKinds.Quarry: Quarry(mb, st, rig); break;
                case BuildingKinds.Mine: Mine(mb, st, rig); break;
                case BuildingKinds.Farm: Farm(mb, st, rig); break;
                case BuildingKinds.Sawmill: Sawmill(mb, st, rig); break;
                case BuildingKinds.Kiln: Kiln(mb, st, rig); break;
                case BuildingKinds.Smelter: Smelter(mb, st, rig); break;
                case BuildingKinds.Barracks: Barracks(mb, st, rig); break;
                case BuildingKinds.VehicleWorkshop: Workshop(mb, st, rig); break;
                default: Cottage(mb, st, rig); break;
            }
        }

        private static void Banner(MeshBuilder mb, BuildingRig rig, Vector3 p, float h, ModelStyle st)
        {
            ModelKit.Pole(mb, p, h, st);
            rig.BannerTop = p + Vector3.up * (h - 0.25f);
        }

        // ---------------------------------------------------------------- HQ / warehouse

        private static void Hq(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ModelKit.House(mb, new Vector3(-1.2f, 0, -1.4f), 9f, 6f, 3.8f, 3.6f, st, true, 2.0f);
            var tower = new Vector3(4.6f, 0, -2.6f);
            if (!st.Stone)
            {
                // A timber lookout: four posts, a planked room, a thatch cone.
                foreach (var o in new[] { new Vector3(-1.2f, 0, -1.2f), new Vector3(1.2f, 0, -1.2f), new Vector3(1.2f, 0, 1.2f), new Vector3(-1.2f, 0, 1.2f) })
                    mb.Cylinder(tower + o + Vector3.down * 0.5f, 0.24f, 0.2f, 7.4f, 6, st.Frame);
                ModelKit.Beam(mb, tower + new Vector3(-1.2f, 0.5f, 1.2f), tower + new Vector3(1.2f, 4f, 1.2f), 0.09f, st.Frame);
                ModelKit.Beam(mb, tower + new Vector3(1.2f, 0.5f, 1.2f), tower + new Vector3(-1.2f, 4f, 1.2f), 0.09f, st.Frame);
                mb.Box(tower + Vector3.up * 5.6f, new Vector3(3.1f, 1.8f, 3.1f), st.Wall);
                mb.Box(tower + Vector3.up * 4.75f, new Vector3(3.3f, 0.25f, 3.3f), st.Frame);
                mb.Box(tower + Vector3.up * 6.5f, new Vector3(3.5f, 0.25f, 3.5f), st.Accent);
                mb.Cone(tower + Vector3.up * 6.6f, 2.7f, 3.2f, 8, st.Roof);
                Banner(mb, rig, tower + Vector3.up * 9.2f, 4.2f, st);
                rig.Height = 13.4f;
            }
            else
            {
                // A square keep with battlements and a slate cap.
                mb.Box(tower + Vector3.up * 3.6f, new Vector3(3.4f, 8.4f, 3.4f), st.Wall);
                for (int k = 0; k < 4; k++) mb.Box(tower + Vector3.up * (1f + k * 2.1f), new Vector3(3.5f, 0.2f, 3.5f), st.Wall * 0.8f);
                for (int i = 0; i < 4; i++)
                {
                    var d = ModelKit.Dir(45f + i * 90f) * 1.6f;
                    mb.Box(tower + d + Vector3.up * 8.2f, new Vector3(0.8f, 0.9f, 0.8f), st.Wall * 1.1f);
                }
                mb.Box(tower + Vector3.up * 7.75f, new Vector3(3.7f, 0.3f, 3.7f), st.Accent);
                mb.Box(tower + new Vector3(0, 5.2f, 1.72f), new Vector3(0.5f, 1.0f, 0.1f), ModelKit.Soot);
                Banner(mb, rig, tower + Vector3.up * 8.0f, 4.2f, st);
                rig.Height = 12.5f;
            }
            ModelKit.Chimney(mb, new Vector3(-3.6f, 5.2f, -2.4f), 1.6f, 0.4f, st.Stone ? st.Wall * 0.9f : ModelKit.Rock);
            rig.Smoke.Add(new Vector3(-3.6f, 7.1f, -2.4f));
            rig.AlwaysSmoke = true;
            // The yard: crates, barrels, a log stack, kept clear of the flag in front.
            ModelKit.Crate(mb, new Vector3(-4.6f, 0, 3.0f), 0.9f, 10f);
            ModelKit.Crate(mb, new Vector3(-3.7f, 0, 3.4f), 0.8f, -15f);
            ModelKit.Crate(mb, new Vector3(-4.2f, 0.9f, 3.2f), 0.7f, 5f);
            ModelKit.Barrel(mb, new Vector3(-2.6f, 0, 3.4f));
            ModelKit.Barrel(mb, new Vector3(-2.0f, 0, 2.8f));
            ModelKit.LogStack(mb, new Vector3(3.4f, 0, 2.6f), 2.6f, 2, 90f);
            ModelKit.PlankStack(mb, new Vector3(5.4f, 0, 1.2f), 2.4f, 3, 0f);
            rig.Radius = 7.5f;
        }

        // ---------------------------------------------------------------- small harvesters

        private static void Woodcutter(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ModelKit.House(mb, new Vector3(-0.7f, 0, -0.9f), 3.4f, 2.8f, 2.3f, 2.0f, st);
            ModelKit.LogStack(mb, new Vector3(2.0f, 0, -0.6f), 2.8f, 2, 0f);
            // The axe stump with the axe stuck in it, and chips round it.
            var stump = new Vector3(1.5f, 0, 2.0f);
            mb.Cylinder(stump, 0.55f, 0.48f, 0.7f, 8, ModelKit.LogBark);
            mb.Cylinder(stump + Vector3.up * 0.7f, 0.48f, 0.48f, 0.02f, 8, ModelKit.LogEnd);
            ModelKit.Beam(mb, stump + new Vector3(-0.05f, 0.75f, 0f), stump + new Vector3(-0.6f, 1.6f, 0.2f), 0.05f, ModelKit.Wood);
            mb.Box(stump + new Vector3(0.0f, 0.82f, 0f), new Vector3(0.36f, 0.26f, 0.08f), ModelKit.Metal, 30f);
            for (int i = 0; i < 6; i++) mb.Box(stump + ModelKit.Dir(i * 60f + 10f) * 0.9f + Vector3.up * 0.05f, new Vector3(0.22f, 0.08f, 0.12f), ModelKit.LogEnd, i * 37f);
            ModelKit.Log(mb, new Vector3(-2.4f, 0.25f, 1.4f), new Vector3(-0.6f, 0.25f, 2.0f), 0.25f, ModelKit.LogBark, ModelKit.LogEnd);
            Banner(mb, rig, new Vector3(-2.4f, 0, -2.4f), 4.6f, st);
            rig.HasWorker = true; rig.Worker = stump + new Vector3(-1.0f, 0, 0.1f); rig.WorkerYaw = 90f; rig.WorkerTool = ToolKind.Axe;
            rig.Height = 5.0f; rig.Radius = 3.2f;
        }

        private static void ClayPit(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ModelKit.Hut(mb, new Vector3(-1.7f, 0, -1.5f), 1.35f, 1.7f, 1.6f, st);
            // The pit: a ring of dug earth, wet clay inside, lumps and a plank ramp.
            var pit = new Vector3(1.1f, 0, 0.6f);
            var ring = new List<Vector3>();
            for (int i = 0; i < 12; i++) ring.Add(pit + ModelKit.Dir(i * 30f) * (1.5f + 0.15f * ((i * 7) % 3)) + Vector3.up * 0.12f);
            ring.Reverse();
            mb.FanUp(pit + Vector3.up * 0.08f, ring, new Color(0.55f, 0.28f, 0.17f));
            for (int i = 0; i < 12; i++)
                ModelKit.Lump(mb, pit + ModelKit.Dir(i * 30f + 8f) * 1.85f, 0.55f, 0.45f + 0.1f * (i % 3), ModelKit.Earth * (0.9f + 0.05f * (i % 3)), 5, i);
            for (int i = 0; i < 3; i++) ModelKit.Lump(mb, pit + ModelKit.Dir(i * 120f + 40f) * 0.7f + Vector3.up * 0.05f, 0.3f, 0.3f, ModelKit.ClayCol, 5, i);
            mb.Box(pit + new Vector3(-1.3f, 0.3f, 1.3f), new Vector3(0.6f, 0.08f, 1.8f), ModelKit.Plank, 45f);
            // A barrow of clay lumps.
            var barrow = new Vector3(-1.9f, 0, 1.6f);
            mb.Box(barrow + Vector3.up * 0.55f, new Vector3(0.9f, 0.4f, 1.2f), ModelKit.Wood);
            ModelKit.Beam(mb, barrow + new Vector3(-0.35f, 0.5f, -0.5f), barrow + new Vector3(-0.35f, 0.4f, -1.4f), 0.05f, ModelKit.Wood);
            ModelKit.Beam(mb, barrow + new Vector3(0.35f, 0.5f, -0.5f), barrow + new Vector3(0.35f, 0.4f, -1.4f), 0.05f, ModelKit.Wood);
            mb.Cylinder(barrow + new Vector3(0, 0.25f, 0.65f), 0.25f, 0.25f, 0.12f, 8, ModelKit.DarkWood);
            ModelKit.Lump(mb, barrow + Vector3.up * 0.7f, 0.4f, 0.45f, ModelKit.ClayCol);
            Banner(mb, rig, new Vector3(-0.4f, 0, -2.6f), 4.4f, st);
            rig.HasWorker = true; rig.Worker = pit + new Vector3(0.1f, -0.1f, 0); rig.WorkerYaw = -40f; rig.WorkerTool = ToolKind.Shovel;
            rig.Height = 4.6f; rig.Radius = 3.0f;
        }

        private static void Quarry(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            var rnd = new PropRandom(7);
            // The rock face behind, cut stone blocks in front, a hoist.
            for (int i = 0; i < 5; i++)
                mb.Rock(new Vector3(-1.2f + i * 0.9f, -0.1f, -2.0f + (i % 2) * 0.3f), 1.0f + 0.35f * (i % 3), ModelKit.Rock * (0.9f + 0.05f * i), rnd.Next);
            mb.Rock(new Vector3(0.2f, 1.0f, -2.3f), 1.0f, ModelKit.Rock * 1.05f, rnd.Next);
            ModelKit.BlockStack(mb, new Vector3(-1.8f, 0, 1.2f), 6, 0.62f, ModelKit.StoneCol, 15f);
            mb.Box(new Vector3(1.9f, 0.35f, 1.9f), new Vector3(0.9f, 0.7f, 0.7f), ModelKit.StoneCol * 0.95f, -10f);
            var hoist = new Vector3(1.4f, 0, 0.2f);
            ModelKit.Beam(mb, hoist + new Vector3(-0.9f, 0, -0.6f), hoist + new Vector3(0, 3.4f, 0), 0.09f, st.Frame);
            ModelKit.Beam(mb, hoist + new Vector3(0.9f, 0, -0.6f), hoist + new Vector3(0, 3.4f, 0), 0.09f, st.Frame);
            ModelKit.Beam(mb, hoist + new Vector3(0, 0, 1.0f), hoist + new Vector3(0, 3.4f, 0), 0.09f, st.Frame);
            ModelKit.Beam(mb, hoist + new Vector3(0, 3.35f, 0), hoist + new Vector3(0, 1.4f, 0), 0.025f, ModelKit.Straw * 0.8f);
            mb.Box(hoist + new Vector3(0, 1.1f, 0), new Vector3(0.6f, 0.5f, 0.6f), ModelKit.StoneCol);
            // A small lean-to for tools.
            var shed = new Vector3(-2.0f, 0, -0.6f);
            foreach (var o in new[] { new Vector3(-0.7f, 0, -0.6f), new Vector3(0.7f, 0, -0.6f), new Vector3(-0.7f, 0, 0.6f), new Vector3(0.7f, 0, 0.6f) })
                mb.Box(shed + o + Vector3.up * 1.0f, new Vector3(0.16f, 2.0f, 0.16f), st.Frame);
            ModelKit.GableX(mb, shed + Vector3.up * 2.0f, 1.9f, 1.8f, 0.9f, st.Roof, st.RoofEdge, st.Accent, 0.15f);
            Banner(mb, rig, new Vector3(-2.6f, 0, 0.6f), 4.4f, st);
            rig.HasWorker = true; rig.Worker = new Vector3(0.2f, 0, -0.6f); rig.WorkerYaw = 180f; rig.WorkerTool = ToolKind.Pick;
            rig.Height = 4.6f; rig.Radius = 3.0f;
        }

        private static void Mine(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            var rnd = new PropRandom(11);
            // A grassy, rocky mound with a timbered mouth.
            var mound = new Vector3(0, -0.3f, -1.0f);
            var earth = new Color(0.46f, 0.42f, 0.34f);
            mb.Cylinder(mound, 3.2f, 2.6f, 1.2f, 9, earth);
            mb.Cylinder(mound + Vector3.up * 1.2f, 2.6f, 1.7f, 1.1f, 9, earth * 1.05f);
            mb.Cylinder(mound + Vector3.up * 2.3f, 1.7f, 0.7f, 0.8f, 9, new Color(0.44f, 0.52f, 0.30f));
            for (int i = 0; i < 4; i++) mb.Rock(mound + ModelKit.Dir(100f + i * 50f) * 2.2f + Vector3.up * 0.9f, 0.55f, ModelKit.Rock, rnd.Next);
            var mouth = new Vector3(0, 0, 1.4f);
            mb.Box(mouth + new Vector3(0, 0.85f, -0.2f), new Vector3(1.4f, 1.7f, 0.6f), new Color(0.06f, 0.05f, 0.05f));
            mb.Box(mouth + new Vector3(-0.85f, 1.0f, 0), new Vector3(0.28f, 2.0f, 0.28f), st.Frame);
            mb.Box(mouth + new Vector3(0.85f, 1.0f, 0), new Vector3(0.28f, 2.0f, 0.28f), st.Frame);
            mb.Box(mouth + new Vector3(0, 2.05f, 0), new Vector3(2.3f, 0.3f, 0.36f), st.Frame);
            mb.Box(mouth + new Vector3(0, 2.25f, 0.05f), new Vector3(1.0f, 0.12f, 0.3f), st.Accent);
            // Rails and an ore cart.
            ModelKit.Beam(mb, mouth + new Vector3(-0.35f, 0.06f, 0), mouth + new Vector3(-0.35f, 0.06f, 2.2f), 0.05f, ModelKit.Iron);
            ModelKit.Beam(mb, mouth + new Vector3(0.35f, 0.06f, 0), mouth + new Vector3(0.35f, 0.06f, 2.2f), 0.05f, ModelKit.Iron);
            for (int i = 0; i < 5; i++) mb.Box(mouth + new Vector3(0, 0.04f, 0.2f + i * 0.5f), new Vector3(1.0f, 0.08f, 0.16f), ModelKit.Wood);
            var cart = mouth + new Vector3(0, 0, 1.6f);
            mb.Cylinder(cart + new Vector3(0, 0.3f, 0), 0.5f, 0.62f, 0.55f, 4, ModelKit.Wood * 0.9f);
            ModelKit.Lump(mb, cart + Vector3.up * 0.8f, 0.42f, 0.4f, ModelKit.OreCol);
            for (int i = 0; i < 4; i++) ModelKit.Lump(mb, new Vector3(2.1f + (i % 2) * 0.5f, 0, 1.3f + (i / 2) * 0.5f), 0.4f, 0.45f, ModelKit.OreCol * (0.9f + 0.1f * i), 5, i);
            Banner(mb, rig, new Vector3(-1.9f, 0, 1.0f), 4.0f, st);
            rig.HasWorker = true; rig.Worker = mouth + new Vector3(0.9f, 0, 1.0f); rig.WorkerYaw = 180f; rig.WorkerTool = ToolKind.Pick;
            rig.Height = 4.2f; rig.Radius = 3.2f;
        }

        // ---------------------------------------------------------------- medium

        private static void Farm(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            // The barn: tall roof, a wide door.
            ModelKit.House(mb, new Vector3(-2.6f, 0, -1.9f), 4.4f, 3.8f, 3.0f, 3.0f, st, true, 1.9f);
            // A small field of grain in rows, fenced on two sides.
            var f0 = new Vector3(0.9f, 0, -2.8f);
            float fw = 4.4f, fd = 5.6f;
            var soil = new Color(0.50f, 0.37f, 0.22f);
            mb.Quad(f0 + new Vector3(0, 0.06f, 0), f0 + new Vector3(0, 0.06f, fd), f0 + new Vector3(fw, 0.06f, fd), f0 + new Vector3(fw, 0.06f, 0), soil);
            mb.Box(f0 + new Vector3(fw * 0.5f, -0.3f, fd * 0.5f), new Vector3(fw, 0.7f, fd), soil * 0.9f);
            for (int r = 0; r < 6; r++)
            {
                float x = 0.4f + r * 0.72f;
                var grain = r % 2 == 0 ? ModelKit.Straw : ModelKit.Straw * 0.93f;
                mb.Box(f0 + new Vector3(x, 0.45f, fd * 0.5f), new Vector3(0.42f, 0.8f, fd - 0.5f), grain);
                for (int k = 0; k < 6; k++) mb.Box(f0 + new Vector3(x, 0.9f, 0.6f + k * 0.85f), new Vector3(0.36f, 0.18f, 0.3f), grain * 1.08f, 20f * k);
            }
            ModelKit.Fence(mb, f0 + new Vector3(-0.2f, 0, -0.2f), f0 + new Vector3(fw + 0.2f, 0, -0.2f), ModelKit.Wood);
            ModelKit.Fence(mb, f0 + new Vector3(fw + 0.2f, 0, -0.2f), f0 + new Vector3(fw + 0.2f, 0, fd + 0.2f), ModelKit.Wood);
            for (int i = 0; i < 3; i++) ModelKit.Sheaf(mb, new Vector3(-3.6f + i * 0.7f, 0, 1.3f + (i % 2) * 0.5f), 1.3f);
            ModelKit.Barrel(mb, new Vector3(-0.6f, 0, 0.6f), 0.4f, 0.9f);
            Banner(mb, rig, new Vector3(-4.6f, 0, -0.2f), 5.6f, st);
            rig.HasWorker = true; rig.Worker = f0 + new Vector3(2.2f, 0, fd + 0.6f); rig.WorkerYaw = 180f; rig.WorkerTool = ToolKind.Sickle;
            rig.Height = 6.8f; rig.Radius = 5.0f;
        }

        private static void Sawmill(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ModelKit.House(mb, new Vector3(-2.7f, 0, -2.0f), 3.4f, 3.0f, 2.5f, 2.2f, st);
            // The open saw shed: posts, a roof, the saw bench with a log on it.
            var shed = new Vector3(1.4f, 0, -0.6f);
            foreach (var o in new[] { new Vector3(-2.0f, 0, -1.5f), new Vector3(2.0f, 0, -1.5f), new Vector3(2.0f, 0, 1.5f), new Vector3(-2.0f, 0, 1.5f) })
                mb.Box(shed + o + Vector3.up * 1.5f, new Vector3(0.26f, 3.0f, 0.26f), st.Frame);
            mb.Box(shed + new Vector3(0, -0.3f, 0), new Vector3(4.6f, 0.7f, 3.6f), st.Plinth);
            ModelKit.GableX(mb, shed + Vector3.up * 3.0f, 5.0f, 3.9f, 1.8f, st.Roof, st.RoofEdge, st.Accent, st.Stone ? 0.15f : 0.3f);
            mb.Box(shed + new Vector3(0, 0.55f, 0), new Vector3(3.6f, 0.3f, 0.9f), ModelKit.Wood);
            foreach (float x in new[] { -1.5f, 1.5f }) foreach (float z in new[] { -0.35f, 0.35f })
                mb.Box(shed + new Vector3(x, 0.25f, z), new Vector3(0.14f, 0.5f, 0.14f), ModelKit.DarkWood);
            ModelKit.Log(mb, shed + new Vector3(-1.9f, 0.98f, 0), shed + new Vector3(1.3f, 0.98f, 0), 0.3f, ModelKit.LogBark, ModelKit.LogEnd);
            mb.Box(shed + new Vector3(0.8f, 1.1f, 0), new Vector3(0.05f, 0.9f, 0.9f), ModelKit.Metal);
            // Plank stack and logs waiting.
            ModelKit.PlankStack(mb, new Vector3(-2.6f, 0, 1.6f), 2.6f, 4, 90f);
            ModelKit.LogStack(mb, new Vector3(1.6f, 0, 2.5f), 2.6f, 2, 90f);
            ModelKit.Lump(mb, new Vector3(-0.6f, 0, 0.7f), 0.6f, 0.35f, ModelKit.LogEnd * 1.05f);
            Banner(mb, rig, new Vector3(-4.6f, 0, -0.2f), 5.4f, st);
            rig.HasWheel = true; rig.Wheel = shed + new Vector3(2.55f, 1.6f, 0); rig.WheelR = 1.4f; rig.WheelSpeed = 120f;
            rig.Height = 6.0f; rig.Radius = 5.0f;
        }

        private static void Kiln(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ModelKit.House(mb, new Vector3(-2.8f, 0, -2.0f), 3.0f, 2.7f, 2.3f, 2.0f, st);
            // The dome kiln with its chimney and a glowing mouth.
            var k = new Vector3(1.4f, 0, -0.6f);
            var body = st.Stone ? new Color(0.40f, 0.33f, 0.30f) : new Color(0.72f, 0.44f, 0.28f);
            mb.Cylinder(k + Vector3.down * 0.4f, 2.5f, 2.4f, 1.4f, 12, body * 0.92f);
            mb.Cylinder(k + Vector3.up * 1.0f, 2.4f, 2.05f, 0.8f, 12, body);
            mb.Cylinder(k + Vector3.up * 1.8f, 2.05f, 1.4f, 0.8f, 12, body * 1.04f);
            mb.Cylinder(k + Vector3.up * 2.6f, 1.4f, 0.6f, 0.6f, 12, body * 1.08f);
            mb.Cylinder(k + Vector3.up * 0.95f, 2.45f, 2.45f, 0.14f, 12, st.Accent, false);
            ModelKit.Chimney(mb, k + Vector3.up * 3.0f, 1.8f, 0.45f, body * 0.85f);
            rig.Smoke.Add(k + Vector3.up * 5.0f);
            mb.Box(k + new Vector3(0, 0.75f, 2.32f), new Vector3(1.2f, 1.3f, 0.3f), ModelKit.Soot);
            mb.Box(k + new Vector3(0, 1.45f, 2.38f), new Vector3(1.5f, 0.25f, 0.3f), body * 0.75f);
            rig.HasGlow = true; rig.Glow = k + new Vector3(0, 0.7f, 2.45f); rig.GlowSize = new Vector3(0.9f, 1.0f, 0.1f);
            // Bricks drying and a clay heap.
            ModelKit.BrickStack(mb, new Vector3(-2.6f, 0, 1.6f), 4, 0f);
            ModelKit.BrickStack(mb, new Vector3(-1.6f, 0, 2.2f), 3, 90f);
            ModelKit.Lump(mb, new Vector3(3.6f, 0, 2.0f), 0.8f, 0.7f, ModelKit.ClayCol);
            ModelKit.LogStack(mb, new Vector3(3.9f, 0, -2.4f), 1.6f, 1, 0f, 0.2f);
            Banner(mb, rig, new Vector3(-4.6f, 0, -0.1f), 5.4f, st);
            rig.SmokeColour = new Color(0.78f, 0.76f, 0.74f);
            rig.Height = 6.0f; rig.Radius = 5.0f;
        }

        private static void Smelter(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ModelKit.House(mb, new Vector3(-2.9f, 0, -2.1f), 2.9f, 2.7f, 2.3f, 2.0f, st);
            // The furnace: a squat stone stack, a tall chimney, a glowing mouth, leather bellows.
            var f = new Vector3(1.3f, 0, -0.8f);
            var stone = st.Stone ? new Color(0.34f, 0.33f, 0.33f) : new Color(0.58f, 0.55f, 0.50f);
            mb.Cylinder(f + Vector3.down * 0.4f, 2.0f, 1.9f, 1.2f, 6, stone * 0.9f);
            mb.Cylinder(f + Vector3.up * 0.8f, 1.9f, 1.3f, 2.8f, 6, stone);
            mb.Cylinder(f + Vector3.up * 3.6f, 1.4f, 1.4f, 0.3f, 6, st.Accent);
            ModelKit.Chimney(mb, f + Vector3.up * 3.9f, 2.6f, 0.55f, stone * 0.8f);
            rig.Smoke.Add(f + Vector3.up * 6.7f);
            rig.SmokeColour = new Color(0.42f, 0.40f, 0.40f);
            mb.Box(f + new Vector3(0, 0.7f, 1.75f), new Vector3(1.1f, 1.1f, 0.3f), ModelKit.Soot);
            rig.HasGlow = true; rig.Glow = f + new Vector3(0, 0.65f, 1.9f); rig.GlowSize = new Vector3(0.85f, 0.85f, 0.1f);
            var bellows = f + new Vector3(-2.0f, 0, 0.6f);
            mb.Box(bellows + new Vector3(0, 0.6f, 0), new Vector3(0.9f, 0.5f, 1.4f), new Color(0.45f, 0.30f, 0.18f), 20f);
            ModelKit.Beam(mb, bellows + new Vector3(0, 0.6f, 0), f + new Vector3(-1.2f, 0.7f, 0.4f), 0.1f, ModelKit.DarkWood);
            // Iron bars and an ore heap.
            for (int l = 0; l < 3; l++) for (int i = 0; i < 4 - l; i++)
                mb.Box(new Vector3(-2.9f + i * 0.3f + l * 0.15f, 0.08f + l * 0.14f, 1.8f), new Vector3(0.24f, 0.13f, 1.2f), ModelKit.Iron * (1f + 0.08f * ((i + l) % 2)));
            for (int i = 0; i < 4; i++) ModelKit.Lump(mb, new Vector3(3.5f + (i % 2) * 0.55f, 0, 1.6f + (i / 2) * 0.5f), 0.45f, 0.5f, ModelKit.OreCol * (0.9f + 0.08f * i), 5, i);
            Banner(mb, rig, new Vector3(-4.6f, 0, -0.1f), 5.4f, st);
            rig.Height = 7.0f; rig.Radius = 5.0f;
        }

        private static void Barracks(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ModelKit.House(mb, new Vector3(0, 0, -2.7f), 7.2f, 3.2f, 2.8f, 2.4f, st, true, 1.4f);
            // The yard: packed earth, a fence each side, a spear rack and a straw dummy.
            var yard = new List<Vector3>();
            for (int i = 0; i < 14; i++) yard.Add(new Vector3(0, 0.07f, 1.0f) + new Vector3(Mathf.Cos(i * Mathf.PI * 2f / 14f) * 3.6f, 0, Mathf.Sin(i * Mathf.PI * 2f / 14f) * 2.4f));
            mb.FanUp(new Vector3(0, 0.07f, 1.0f), yard, new Color(0.62f, 0.52f, 0.36f));
            var fence = st.Stone ? st.Wall * 0.9f : ModelKit.Wood;
            ModelKit.Fence(mb, new Vector3(-3.9f, 0, -0.9f), new Vector3(-3.9f, 0, 3.4f), fence);
            ModelKit.Fence(mb, new Vector3(3.9f, 0, -0.9f), new Vector3(3.9f, 0, 3.4f), fence);
            var rack = new Vector3(-2.4f, 0, 0.0f);
            mb.Box(rack + new Vector3(-0.9f, 0.7f, 0), new Vector3(0.16f, 1.4f, 0.16f), ModelKit.DarkWood);
            mb.Box(rack + new Vector3(0.9f, 0.7f, 0), new Vector3(0.16f, 1.4f, 0.16f), ModelKit.DarkWood);
            mb.Box(rack + new Vector3(0, 1.3f, 0), new Vector3(2.0f, 0.14f, 0.16f), ModelKit.DarkWood);
            for (int i = 0; i < 5; i++)
            {
                var b0 = rack + new Vector3(-0.7f + i * 0.35f, 0.05f, 0.35f);
                var t0 = rack + new Vector3(-0.7f + i * 0.35f, 2.5f, -0.1f);
                ModelKit.Beam(mb, b0, t0, 0.04f, ModelKit.Wood);
                mb.Cone(t0, 0.08f, 0.35f, 4, ModelKit.Metal);
            }
            var dummy = new Vector3(2.2f, 0, 1.5f);
            mb.Box(dummy + Vector3.up * 0.9f, new Vector3(0.14f, 1.8f, 0.14f), ModelKit.DarkWood);
            mb.Box(dummy + Vector3.up * 1.4f, new Vector3(1.3f, 0.12f, 0.12f), ModelKit.DarkWood);
            mb.Cylinder(dummy + Vector3.up * 0.9f, 0.32f, 0.3f, 0.9f, 6, ModelKit.Straw);
            ModelKit.Lump(mb, dummy + Vector3.up * 1.8f, 0.25f, 0.45f, ModelKit.Straw);
            mb.Cylinder(dummy + new Vector3(0, 1.15f, 0.32f), 0.42f, 0.42f, 0.06f, 8, st.Accent);
            Banner(mb, rig, new Vector3(3.4f, 0, -1.2f), 6.4f, st);
            rig.HasWorker = true; rig.Worker = dummy + new Vector3(-1.3f, 0, -0.1f); rig.WorkerYaw = 90f; rig.WorkerTool = ToolKind.Spear;
            rig.Height = 6.6f; rig.Radius = 5.0f;
        }

        private static void Workshop(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            // A tall shed with a big double door standing open.
            var c = new Vector3(-0.6f, 0, -1.6f);
            ModelKit.House(mb, c, 6.4f, 4.2f, 3.6f, 2.6f, st, false);
            var doorC = c + new Vector3(0, 0, 2.12f);
            mb.Box(doorC + new Vector3(0, 1.4f, 0), new Vector3(3.0f, 2.8f, 0.12f), new Color(0.07f, 0.06f, 0.05f));
            mb.Box(doorC + new Vector3(0, 2.95f, 0.05f), new Vector3(3.5f, 0.3f, 0.2f), st.Frame);
            foreach (int side in new[] { -1, 1 })
            {
                var hinge = doorC + new Vector3(side * 1.5f, 0, 0.05f);
                var leaf = ModelKit.Dir(side * 60f) * 0.75f;
                mb.Box(hinge + leaf + Vector3.up * 1.4f, new Vector3(0.14f, 2.7f, 1.5f), ModelKit.Plank * 0.8f, side * 60f + 90f);
            }
            // A war-wagon frame outside, a spare wheel, planks.
            var cart = new Vector3(-3.4f, 0, 1.9f);
            mb.Box(cart + Vector3.up * 0.9f, new Vector3(1.4f, 0.25f, 2.4f), ModelKit.Wood);
            ModelKit.Beam(mb, cart + new Vector3(0, 0.9f, 1.2f), cart + new Vector3(0, 0.3f, 2.6f), 0.06f, ModelKit.Wood);
            mb.Box(cart + new Vector3(0.72f, 0.5f, -0.6f), new Vector3(0.12f, 1.0f, 0.12f), ModelKit.DarkWood);
            ModelKit.PlankStack(mb, new Vector3(2.4f, 0, 2.2f), 2.4f, 3, 0f);
            Banner(mb, rig, new Vector3(3.9f, 0, -3.4f), 6.0f, st);
            rig.HasWheel = true; rig.Wheel = new Vector3(3.6f, 1.25f, 0.6f); rig.WheelR = 1.05f; rig.WheelSpeed = 70f;
            // The wheelwright's stand under the wheel.
            mb.Box(new Vector3(3.6f, 0.6f, 0.6f), new Vector3(0.3f, 1.2f, 0.3f), st.Frame);
            mb.Box(new Vector3(3.6f, 0.06f, 0.6f), new Vector3(0.9f, 0.12f, 1.2f), st.Frame);
            rig.HasWorker = true; rig.Worker = new Vector3(2.7f, 0, 1.4f); rig.WorkerYaw = 60f; rig.WorkerTool = ToolKind.Hammer;
            rig.Height = 7.0f; rig.Radius = 5.0f;
        }

        private static void Cottage(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            ModelKit.House(mb, Vector3.zero, 3.6f, 3.0f, 2.4f, 2.1f, st);
            ModelKit.Barrel(mb, new Vector3(2.2f, 0, 1.0f));
            Banner(mb, rig, new Vector3(-2.2f, 0, -1.6f), 4.6f, st);
            rig.Height = 5.0f; rig.Radius = 3.0f;
        }
    }
}
