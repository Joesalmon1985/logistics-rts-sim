using System.Collections.Generic;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// M3: the population drawn in the world (a world view: it reads the sim, never changes it). Each living NPC is a small
    /// procedural figure in its faction's colour, walking between lattice nodes, with poses for carrying (the load on its
    /// head, an axe in hand), eating (hand to mouth), working (chopping, hammering), resting and sleeping (lying down; in a
    /// hut it is hidden inside). Wild food sources (berry bushes, root patches, fishing spots, nightshade), wood stands,
    /// huts and hut sites, and goods lying on the ground are drawn too; bushes shrink as their stock falls.
    /// </summary>
    public sealed class NpcLayer : MonoBehaviour
    {
        public const float Scale = 1.1f;

        private sealed class Fig
        {
            public GameObject Root; public Transform Body, LegL, LegR, ArmL, ArmR;
            public MeshFilter Cargo, Tool, BodyF;
            public FigureParts P;
            public Vector3 Pos; public float Yaw, Phase; public bool Placed;
            public Ware Shown = (Ware)(-1); public bool ShownAxe; public Faction Faction = (Faction)(-1);
        }

        private GodSim _sim;
        private readonly Dictionary<int, Fig> _figs = new Dictionary<int, Fig>();
        private readonly Stack<Fig> _pool = new Stack<Fig>();
        private readonly HashSet<int> _alive = new HashSet<int>();
        private readonly List<int> _gone = new List<int>();
        private readonly Dictionary<long, GameObject> _things = new Dictionary<long, GameObject>();
        private readonly HashSet<long> _seen = new HashSet<long>();
        private readonly List<long> _drop = new List<long>();
        private float _time;

        /// <summary>Figures drawn now, and how many are asleep or carrying (tests).</summary>
        public int Count => _figs.Count;
        public int SleepingNow { get; private set; }
        public int CarryingNow { get; private set; }
        public int ThingsNow => _things.Count;

        public static NpcLayer Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("NpcLayer");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<NpcLayer>();
            l._sim = sim;
            return l;
        }

        /// <summary>The parts of a drawn person's figure (tests and the inspector); null when not drawn.</summary>
        public FigureParts PartsOf(int npcId) => _figs.TryGetValue(npcId, out var f) ? f.P : null;

        /// <summary>Where a figure stands now (world), for pickers and cameras; false when not drawn.</summary>
        public bool TryPosition(int npcId, out Vector3 p)
        {
            if (_figs.TryGetValue(npcId, out var f) && f.Root.activeSelf) { p = f.Pos; return true; }
            p = default; return false;
        }

        // ---------------- meshes ----------------

        private static Mesh BodyMesh(Faction f) => ModelKit.Cached("npc/body2/" + (int)f,
            mb => PeopleModels.TorsoOnly(mb, ModelKit.Accent(FactionLook.Colour(f)) * 0.9f));
        public static Mesh HeadMesh(Faction f, bool grey) => ModelKit.Cached("npc/head/" + (int)f + "/" + grey,
            mb => PeopleModels.Head(mb, ModelKit.Accent(FactionLook.Colour(f)) * 0.7f, grey));
        public static Mesh FingerMesh(int n) => ModelKit.Cached("npc/fingers/" + n, mb => PeopleModels.Fingers(mb, n));
        public static Mesh BellyMesh(Faction f) => ModelKit.Cached("npc/belly/" + (int)f, mb => PeopleModels.Belly(mb, ModelKit.Accent(FactionLook.Colour(f)) * 0.9f));
        public static Mesh WrapMesh() => ModelKit.Cached("npc/wrap", mb => PeopleModels.Wrap(mb, -0.4f, 0.2f));
        public static Mesh LegWrapMesh() => ModelKit.Cached("npc/wrapleg", mb => PeopleModels.Wrap(mb, -0.45f, 0.24f));
        public static Mesh SlingMesh() => ModelKit.Cached("npc/sling", mb => PeopleModels.Sling(mb));
        public static Mesh AuraMesh() => ModelKit.Cached("npc/aura", mb => PeopleModels.AuraRing(mb));
        private static Material _ill;
        public static Material IllMaterial => _ill != null ? _ill : (_ill = MaterialFactory.CreateVertexColored(new Color(0.72f, 0.92f, 0.66f), "Ill tint"));
        private static Mesh LegMesh() => ModelKit.Cached("npc/leg", mb => PeopleModels.Leg(mb, new Color(0.34f, 0.28f, 0.22f)));
        private static Mesh ArmMesh(Faction f) => ModelKit.Cached("npc/arm/" + (int)f, mb => PeopleModels.Arm(mb, ModelKit.Accent(FactionLook.Colour(f)) * 0.85f));
        private static Mesh AxeMesh() => ModelKit.Cached("npc/axe", mb => PeopleModels.Tool(mb, ToolKind.Axe));

        public static Mesh LoadMesh(Ware w) => ModelKit.Cached("npc/load/" + (int)w, mb =>
        {
            if (Wares.IsFood(w))
            {
                var b = new Vector3(0, PeopleModels.HeadTop, 0);
                mb.Cylinder(b, 0.3f, 0.38f, 0.28f, 8, new Color(0.62f, 0.48f, 0.28f));
                ModelKit.Lump(mb, b + new Vector3(0, 0.28f, 0), 0.26f, 0.22f, FoodColour(w));
            }
            else PeopleModels.Load(mb, w);
        });

        public static Color FoodColour(Ware w)
        {
            switch (w)
            {
                case Ware.Berries: return new Color(0.78f, 0.12f, 0.20f);
                case Ware.Roots: return new Color(0.62f, 0.44f, 0.24f);
                case Ware.Fish: return new Color(0.70f, 0.76f, 0.82f);
                case Ware.Nightshade: return new Color(0.34f, 0.14f, 0.42f);
                default: return Color.gray;
            }
        }

        private static Mesh SourceMesh(SourceKind k) => ModelKit.Cached("npc/src/" + (int)k, mb =>
        {
            switch (k)
            {
                case SourceKind.Berries:
                case SourceKind.Nightshade:
                {
                    var leaf = k == SourceKind.Berries ? new Color(0.20f, 0.46f, 0.18f) : new Color(0.16f, 0.26f, 0.18f);
                    var fruit = k == SourceKind.Berries ? new Color(0.84f, 0.12f, 0.20f) : new Color(0.38f, 0.12f, 0.48f);
                    ModelKit.Lump(mb, new Vector3(0, 0, 0), 1.3f, 1.2f, leaf, 7);
                    ModelKit.Lump(mb, new Vector3(0.6f, 0, 0.4f), 0.9f, 0.9f, leaf * 1.1f, 6);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i * 0.7f;
                        mb.Box(new Vector3(Mathf.Cos(a) * 0.9f, 0.7f + (i % 3) * 0.25f, Mathf.Sin(a) * 0.9f), Vector3.one * 0.18f, fruit);
                    }
                    break;
                }
                case SourceKind.Roots:
                    ModelKit.Lump(mb, Vector3.zero, 1.1f, 0.35f, new Color(0.44f, 0.32f, 0.20f), 7);
                    for (int i = 0; i < 5; i++) ModelKit.Beam(mb, new Vector3((i - 2) * 0.35f, 0.2f, 0), new Vector3((i - 2) * 0.4f, 0.9f, 0.1f), 0.05f, new Color(0.30f, 0.58f, 0.22f));
                    break;
                case SourceKind.Fish:
                    mb.Cylinder(Vector3.zero, 1.4f, 1.4f, 0.06f, 12, new Color(0.30f, 0.52f, 0.70f));
                    mb.Box(new Vector3(0.3f, 0.12f, 0), new Vector3(0.6f, 0.14f, 0.22f), new Color(0.72f, 0.78f, 0.84f));
                    mb.Box(new Vector3(-0.4f, 0.12f, 0.3f), new Vector3(0.5f, 0.12f, 0.2f), new Color(0.72f, 0.78f, 0.84f));
                    break;
                case SourceKind.Wood:
                    ModelKit.LogStack(mb, Vector3.zero, 1.8f, 2, 0f, 0.22f);
                    ModelKit.Beam(mb, new Vector3(1.2f, 0, 0.2f), new Vector3(1.2f, 0.5f, 0.2f), 0.25f, ModelKit.Wood);
                    break;
            }
        });

        private static Mesh HutMesh(Faction f, bool built) => ModelKit.Cached("npc/hut/" + (int)f + "/" + built, mb =>
        {
            var st = ModelStyle.Of(f);
            if (built) ModelKit.Hut(mb, Vector3.zero, 2.6f, 1.6f, 2.0f, st);
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    ModelKit.Beam(mb, new Vector3(Mathf.Cos(a) * 2.4f, 0, Mathf.Sin(a) * 2.4f), new Vector3(0, 3.2f, 0), 0.08f, ModelKit.Wood);
                }
                ModelKit.LogStack(mb, new Vector3(2.8f, 0, 1.6f), 1.6f, 1, 30f, 0.2f);
            }
        });

        private static Mesh LotMesh(Ware w) => ModelKit.Cached("npc/lot/" + (int)w, mb =>
        {
            if (w == Ware.Axe) { PeopleModels.Tool(mb, ToolKind.Axe); return; }
            if (w == Ware.Log) { ModelKit.Log(mb, new Vector3(-0.8f, 0.22f, 0), new Vector3(0.8f, 0.22f, 0), 0.22f, ModelKit.LogBark, ModelKit.LogEnd); return; }
            ModelKit.Lump(mb, Vector3.zero, 0.35f, 0.25f, FoodColour(w));
        });

        // ---------------- figures ----------------

        private Fig Take(Faction f)
        {
            var fig = _pool.Count > 0 ? _pool.Pop() : Build();
            fig.Placed = false; fig.Shown = (Ware)(-1); fig.ShownAxe = false;
            if (fig.Faction != f)
            {
                fig.Faction = f;
                fig.BodyF.sharedMesh = BodyMesh(f);
                fig.ArmL.GetComponent<MeshFilter>().sharedMesh = ArmMesh(f);
                fig.ArmR.GetComponent<MeshFilter>().sharedMesh = ArmMesh(f);
                fig.P.HeadKey = -1; // LifeStageLayer sets the head (faction cap, grey hair)
                fig.P.HeadF.sharedMesh = HeadMesh(f, false);
                fig.P.Belly.GetComponent<MeshFilter>().sharedMesh = BellyMesh(f);
            }
            fig.Cargo.sharedMesh = null; fig.Tool.sharedMesh = null;
            fig.Root.SetActive(true);
            return fig;
        }

        private Fig Build()
        {
            var f = new Fig { Root = new GameObject("Npc") };
            f.Root.transform.SetParent(transform, false);
            f.Root.transform.localScale = Vector3.one * Scale;
            var p = FigureParts.Build(f.Root.transform, LegMesh());
            f.P = p; f.Body = p.Body; f.BodyF = p.BodyF; f.LegL = p.LegL; f.LegR = p.LegR; f.ArmL = p.ArmL; f.ArmR = p.ArmR; f.Cargo = p.Cargo; f.Tool = p.Tool;
            p.BandageArm.GetComponent<MeshFilter>().sharedMesh = WrapMesh();
            p.BandageLeg.GetComponent<MeshFilter>().sharedMesh = LegWrapMesh();
            p.Sling.GetComponent<MeshFilter>().sharedMesh = SlingMesh();
            p.AuraF.sharedMesh = AuraMesh();
            return f;
        }

        private void LateUpdate()
        {
            var p = _sim?.State.Population;
            if (p == null) return;
            var g = _sim.State.Territory.Grid;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            _time += dt;
            _alive.Clear();
            SleepingNow = 0; CarryingNow = 0;
            foreach (var n in p.Npcs)
            {
                if (!n.Alive) continue;
                _alive.Add(n.Id);
                if (!_figs.TryGetValue(n.Id, out var f)) { f = Take(n.Faction); _figs[n.Id] = f; }
                Animate(f, n, g, dt);
                LifeStageLayer.Apply(f.P, n, _sim.State); // F0.15: M4 (size and look by life stage, displays)
                GestureLayer.Apply(f.Root.transform, n, _sim.State, _time); // F0.15: M5 (gestures)
                PoseLayer.Apply(f.P, n, _sim.State, _time, this); // U3b: the gesture vocabulary over the walk and work poses
            }
            _gone.Clear();
            foreach (var kv in _figs) if (!_alive.Contains(kv.Key)) _gone.Add(kv.Key);
            foreach (int id in _gone) { var f = _figs[id]; f.Root.SetActive(false); _pool.Push(f); _figs.Remove(id); }
            DrawThings(p, g);
        }

        private void Animate(Fig f, Npc n, NodeGrid g, float dt)
        {
            var a = n.Action;
            // where it is: between its node and the next path node
            int from = n.Node, to = n.Path != null && n.PathIdx < n.Path.Length ? n.Path[n.PathIdx] : n.Node;
            int stepTicks = _sim.State.Territory.NodeRoad[to] != 0 ? _sim.State.Tuning.Population.RoadStepTicks : _sim.State.Tuning.Population.StepTicks;
            float k = to == from ? 0f : Mathf.Clamp01(n.StepTicks / (float)stepTicks);
            var flat = new Vector3(Mathf.Lerp((float)g.X(from), (float)g.X(to), k), 0, Mathf.Lerp((float)g.Z(from), (float)g.Z(to), k));
            // spread people standing on one node a little, by id
            flat += new Vector3(((n.Id * 37) % 7 - 3) * 0.35f, 0, ((n.Id * 53) % 7 - 3) * 0.35f);
            if (!f.Placed || (flat - new Vector3(f.Pos.x, 0, f.Pos.z)).sqrMagnitude > 144f) { f.Pos = flat; f.Placed = true; }
            var prev = f.Pos;
            f.Pos = Vector3.Lerp(new Vector3(f.Pos.x, 0, f.Pos.z), flat, 1f - Mathf.Exp(-dt * 10f));
            f.Pos.y = ModelKit.GroundY(f.Pos.x, f.Pos.z);
            var moved = f.Pos - prev; moved.y = 0;
            float speed = moved.magnitude / Mathf.Max(dt, 1e-4f);
            if (moved.sqrMagnitude > 1e-5f) f.Yaw = Mathf.LerpAngle(f.Yaw, ModelKit.Yaw(moved), 1f - Mathf.Exp(-dt * 10f));

            bool asleep = a.Asleep;
            bool inHut = asleep && a.SleepHut != 0;
            f.Root.SetActive(!inHut); // a sleeper in a hut is inside it
            if (inHut) { SleepingNow++; return; }

            // what it holds: the first non-tool lot on its head, the axe in its hand
            Ware load = Ware.None; bool axe = false;
            foreach (var l in n.Carry) { if (l.Empty) continue; if (l.Ware == Ware.Axe) axe = true; else if (load == Ware.None) load = l.Ware; }
            if (load != Ware.None) CarryingNow++;
            if (load != f.Shown) { f.Shown = load; f.Cargo.sharedMesh = load == Ware.None ? null : LoadMesh(load); }
            if (axe != f.ShownAxe) { f.ShownAxe = axe; f.Tool.sharedMesh = axe ? AxeMesh() : null; }

            if (asleep)
            {
                SleepingNow++;
                // lying on its side on the ground, breathing
                f.Root.transform.SetPositionAndRotation(f.Pos + Vector3.up * 0.25f, Quaternion.Euler(0, f.Yaw, 0) * Quaternion.Euler(-88f, 0, 0));
                f.Body.localPosition = Vector3.up * (Mathf.Sin(_time * 1.6f + n.Id) * 0.03f);
                f.LegL.localRotation = Quaternion.identity; f.LegR.localRotation = Quaternion.Euler(8f, 0, 0);
                f.ArmL.localRotation = Quaternion.Euler(-20f, 0, 0); f.ArmR.localRotation = Quaternion.Euler(-150f, 0, 0);
                f.Body.localRotation = Quaternion.identity;
                return;
            }
            f.Root.transform.SetPositionAndRotation(f.Pos, Quaternion.Euler(0, f.Yaw, 0));

            bool eating = a.Verb == Verb.Eat && a.BusyUntil > 0;
            bool working = !eating && speed < 0.3f && a.Progress > 0 && (a.Verb == Verb.Gather || a.Verb == Verb.Build || a.Verb == Verb.ForageAt);
            float walk = Mathf.Clamp01(speed / 2.5f);
            f.Phase += speed * dt * 2.4f;
            float swing = Mathf.Sin(f.Phase) * 32f * walk;
            f.LegL.localRotation = Quaternion.Euler(swing, 0, 0);
            f.LegR.localRotation = Quaternion.Euler(-swing, 0, 0);
            f.Body.localPosition = Vector3.up * (Mathf.Abs(Mathf.Cos(f.Phase)) * 0.07f * walk);
            if (eating)
            {
                // hand to mouth, again and again
                float ph = _time * 5f + n.Id;
                f.ArmR.localRotation = Quaternion.Euler(-150f + Mathf.Sin(ph) * 20f, 0, 0);
                f.ArmL.localRotation = Quaternion.Euler(-25f, 0, 0);
                f.Body.localRotation = Quaternion.Euler(6f, 0, 0);
            }
            else if (working)
            {
                float ph = _time * 7f + n.Id;
                f.ArmR.localRotation = Quaternion.Euler(-60f + Mathf.Sin(ph) * 55f, 0, 0);
                f.ArmL.localRotation = Quaternion.Euler(-30f, 0, 0);
                f.Body.localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(ph)) * 0.08f);
                f.Body.localRotation = Quaternion.Euler(12f + Mathf.Sin(ph) * 6f, 0, 0);
            }
            else if (load != Ware.None)
            {
                f.ArmL.localRotation = Quaternion.Euler(180f, 0, -12f);
                f.ArmR.localRotation = Quaternion.Euler(axe ? -35f : 180f, 0, 12f);
                f.Body.localRotation = Quaternion.identity;
            }
            else
            {
                f.ArmL.localRotation = Quaternion.Euler(-swing, 0, 0);
                f.ArmR.localRotation = Quaternion.Euler(axe ? -35f + swing * 0.3f : swing, 0, 0);
                // resting: a slight slump
                f.Body.localRotation = a.Verb == Verb.Rest ? Quaternion.Euler(8f, 0, 0) : Quaternion.identity;
            }
        }

        // ---------------- sources, huts, lots ----------------

        private void DrawThings(PopulationState p, NodeGrid g)
        {
            _seen.Clear();
            foreach (var src in p.Sources)
            {
                long key = 1L << 40 | (uint)src.Id;
                _seen.Add(key);
                var go = Thing(key, "Source" + src.Id, SourceMesh(src.Kind), g, src.Node, src.Id * 47f);
                float fill = src.Capacity <= 0 ? 0f : Mathf.Clamp01(src.Stock / (float)src.Capacity);
                float s = src.Kind == SourceKind.Wood || src.Kind == SourceKind.Fish ? 1f : 0.35f + 0.65f * fill;
                go.transform.localScale = Vector3.one * s;
            }
            foreach (var h in p.Huts)
            {
                long key = 2L << 40 | (uint)h.Id;
                _seen.Add(key);
                var go = Thing(key, "Hut" + h.Id, HutMesh(h.Faction, h.Built), g, h.Node, h.Id * 61f);
                var mf = go.GetComponent<MeshFilter>();
                var want = HutMesh(h.Faction, h.Built);
                if (mf.sharedMesh != want) mf.sharedMesh = want;
            }
            foreach (var l in p.Lots)
            {
                long key = 3L << 40 | (uint)l.Lot.Id;
                _seen.Add(key);
                Thing(key, "Lot" + l.Lot.Id, LotMesh(l.Lot.Ware), g, l.Node, l.Lot.Id * 29f);
            }
            _drop.Clear();
            foreach (var kv in _things) if (!_seen.Contains(kv.Key)) _drop.Add(kv.Key);
            foreach (long k in _drop) { Destroy(_things[k]); _things.Remove(k); }
        }

        private GameObject Thing(long key, string name, Mesh mesh, NodeGrid g, int node, float yaw)
        {
            if (!_things.TryGetValue(key, out var go))
            {
                go = ModelKit.Part(name, transform, mesh);
                go.transform.SetPositionAndRotation(ModelKit.Node(g, node), Quaternion.Euler(0, yaw % 360f, 0));
                _things[key] = go;
            }
            return go;
        }
    }
}
