using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P4b carriers, builders and harvesters as tiny villagers walking the roads: legs and arms swing with the distance
    /// walked, the cap shows the owner, carriers hold their ware on the head, builders carry a hammer, harvesters a tool
    /// of their trade out to the hex centre and the raw goods back. Positions follow the sim's integer path steps,
    /// smoothed between ticks; figures are pooled. Reads the sim only.
    /// </summary>
    public sealed class WalkerLayer : MonoBehaviour
    {
        public const float Scale = 1.15f;

        private sealed class Fig
        {
            public GameObject Root; public Transform Body, LegL, LegR, ArmL, ArmR;
            public MeshFilter Cargo, Tool; public MeshFilter BodyF, LegLF, LegRF, ArmLF, ArmRF;
            public Vector3 Pos; public float Yaw, Phase; public bool Placed;
            public Ware Shown = (Ware)(-1); public ToolKind ShownTool = (ToolKind)(-1);
            public WalkerKind Kind; public Faction Faction;
        }

        private GodSim _sim;
        private readonly Dictionary<int, Fig> _figs = new Dictionary<int, Fig>();
        private readonly Stack<Fig> _pool = new Stack<Fig>();
        private readonly HashSet<int> _alive = new HashSet<int>();
        private readonly List<int> _gone = new List<int>();
        private float _time;

        public int Count => _figs.Count;
        public int CarryingNow { get; private set; }
        public int WithToolNow { get; private set; }

        public static WalkerLayer Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("WalkerLayer");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<WalkerLayer>();
            l._sim = sim;
            return l;
        }

        private static Mesh BodyMesh(WalkerKind k, Faction f) => ModelKit.Cached("walker/body/" + (int)k + "/" + (int)f,
            mb => PeopleModels.Torso(mb, PeopleModels.Tunic(k, f), ModelStyle.Of(f).Accent, k == WalkerKind.Builder));
        private static Mesh LegMesh() => ModelKit.Cached("walker/leg", mb => PeopleModels.Leg(mb, new Color(0.36f, 0.30f, 0.24f)));
        private static Mesh ArmMesh(WalkerKind k, Faction f) => ModelKit.Cached("walker/arm/" + (int)k + "/" + (int)f, mb => PeopleModels.Arm(mb, PeopleModels.Tunic(k, f) * 0.95f));
        private static Mesh LoadMesh(Ware w) => ModelKit.Cached("walker/load/" + (int)w, mb => PeopleModels.Load(mb, w));
        private static Mesh ToolMesh(ToolKind t) => ModelKit.Cached("walker/tool/" + t, mb => PeopleModels.Tool(mb, t));

        private Fig Take(WalkerKind k, Faction f)
        {
            var fig = _pool.Count > 0 ? _pool.Pop() : Build();
            fig.Kind = k; fig.Faction = f; fig.Placed = false; fig.Shown = (Ware)(-1); fig.ShownTool = (ToolKind)(-1);
            fig.BodyF.sharedMesh = BodyMesh(k, f);
            fig.ArmLF.sharedMesh = ArmMesh(k, f); fig.ArmRF.sharedMesh = ArmMesh(k, f);
            fig.Root.SetActive(true);
            return fig;
        }

        private Fig Build()
        {
            var f = new Fig { Root = new GameObject("Walker") };
            f.Root.transform.SetParent(transform, false);
            f.Root.transform.localScale = Vector3.one * Scale;
            var body = new GameObject("Body"); body.transform.SetParent(f.Root.transform, false); f.Body = body.transform;
            var bodyMesh = ModelKit.Part("Torso", f.Body, null); f.BodyF = bodyMesh.GetComponent<MeshFilter>();
            f.LegL = ModelKit.Part("LegL", f.Body, LegMesh()).transform; f.LegL.localPosition = new Vector3(-PeopleModels.LegX, PeopleModels.Hip, 0);
            f.LegR = ModelKit.Part("LegR", f.Body, LegMesh()).transform; f.LegR.localPosition = new Vector3(PeopleModels.LegX, PeopleModels.Hip, 0);
            f.ArmL = ModelKit.Part("ArmL", f.Body, null).transform; f.ArmL.localPosition = new Vector3(-PeopleModels.ShoulderX, PeopleModels.Shoulder, 0);
            f.ArmR = ModelKit.Part("ArmR", f.Body, null).transform; f.ArmR.localPosition = new Vector3(PeopleModels.ShoulderX, PeopleModels.Shoulder, 0);
            f.ArmLF = f.ArmL.GetComponent<MeshFilter>(); f.ArmRF = f.ArmR.GetComponent<MeshFilter>();
            f.Cargo = ModelKit.Part("Cargo", f.Body, null).GetComponent<MeshFilter>();
            f.Tool = ModelKit.Part("Tool", f.ArmR, null).GetComponent<MeshFilter>();
            return f;
        }

        private static ToolKind ToolOf(string kind)
        {
            switch (kind)
            {
                case BuildingKinds.Woodcutter: return ToolKind.Axe;
                case BuildingKinds.ClayPit: return ToolKind.Shovel;
                case BuildingKinds.Quarry:
                case BuildingKinds.Mine: return ToolKind.Pick;
                case BuildingKinds.Farm: return ToolKind.Sickle;
                default: return ToolKind.None;
            }
        }

        private void LateUpdate()
        {
            var s = _sim.State; var e = s.Economy; var t = s.Territory;
            if (e == null || t == null) return;
            var g = t.Grid;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            _time += dt;
            _alive.Clear();
            CarryingNow = 0; WithToolNow = 0;
            foreach (var w in e.Walkers)
            {
                _alive.Add(w.Id);
                if (!_figs.TryGetValue(w.Id, out var f)) { f = Take(w.Kind, w.Faction); _figs[w.Id] = f; }
                Animate(f, w, e, g, dt);
            }
            _gone.Clear();
            foreach (var kv in _figs) if (!_alive.Contains(kv.Key)) _gone.Add(kv.Key);
            foreach (int id in _gone) { var f = _figs[id]; f.Root.SetActive(false); _pool.Push(f); _figs.Remove(id); }
        }

        private void Animate(Fig f, Walker w, EconomyState e, NodeGrid g, float dt)
        {
            int a = w.CurrentNode, b = w.NextNode;
            if (a < 0) { f.Root.SetActive(false); return; }
            if (!f.Root.activeSelf) f.Root.SetActive(true);
            float k = w.StepPermille / 1000f;
            float ax = (float)g.X(a), az = (float)g.Z(a), bx = (float)g.X(b), bz = (float)g.Z(b);
            var flat = new Vector3(Mathf.Lerp(ax, bx, k), 0, Mathf.Lerp(az, bz, k));
            var dir = new Vector3(bx - ax, 0, bz - az);
            // Keep to the right of the track so walkers going opposite ways pass each other.
            if (dir.sqrMagnitude > 0.01f) flat += new Vector3(dir.z, 0, -dir.x).normalized * 0.6f;
            if (!f.Placed || (flat - new Vector3(f.Pos.x, 0, f.Pos.z)).sqrMagnitude > 144f)
            {
                f.Pos = flat; f.Placed = true;
            }
            var prev = f.Pos;
            f.Pos = Vector3.Lerp(new Vector3(f.Pos.x, 0, f.Pos.z), flat, 1f - Mathf.Exp(-dt * 12f));
            f.Pos.y = ModelKit.GroundY(f.Pos.x, f.Pos.z);
            var moved = f.Pos - prev; moved.y = 0;
            float speed = moved.magnitude / Mathf.Max(dt, 1e-4f);
            if (moved.sqrMagnitude > 1e-5f) f.Yaw = Mathf.LerpAngle(f.Yaw, ModelKit.Yaw(moved), 1f - Mathf.Exp(-dt * 10f));
            f.Root.transform.SetPositionAndRotation(f.Pos, Quaternion.Euler(0, f.Yaw, 0));

            // What it holds.
            Ware load = w.Cargo;
            ToolKind tool = ToolKind.None;
            if (w.Kind == WalkerKind.Builder) tool = ToolKind.Hammer;
            else if (w.Kind == WalkerKind.Harvester)
            {
                var home = e.Find(w.Home);
                if (w.Phase == WalkerPhase.Returning && load == Ware.None && home != null && home.Def != null) load = home.Def.HarvestWare;
                else if (home != null) tool = ToolOf(home.Kind);
            }
            if (load != Ware.None) { tool = ToolKind.None; CarryingNow++; }
            if (tool != ToolKind.None) WithToolNow++;
            if (load != f.Shown) { f.Shown = load; f.Cargo.sharedMesh = load == Ware.None ? null : LoadMesh(load); }
            if (tool != f.ShownTool) { f.ShownTool = tool; f.Tool.sharedMesh = tool == ToolKind.None ? null : ToolMesh(tool); }

            bool working = w.Phase == WalkerPhase.Working;
            float walk = Mathf.Clamp01(speed / 2.5f);
            f.Phase += speed * dt * 2.4f;
            float swing = Mathf.Sin(f.Phase) * 32f * walk;
            f.LegL.localRotation = Quaternion.Euler(swing, 0, 0);
            f.LegR.localRotation = Quaternion.Euler(-swing, 0, 0);
            f.Body.localPosition = Vector3.up * (Mathf.Abs(Mathf.Cos(f.Phase)) * 0.07f * walk);
            if (working)
            {
                // Hammering or digging: the tool arm chops, the body bobs.
                float ph = _time * 7f + w.Id;
                f.ArmR.localRotation = Quaternion.Euler(-60f + Mathf.Sin(ph) * 55f, 0, 0);
                f.ArmL.localRotation = Quaternion.Euler(-30f, 0, 0);
                f.Body.localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(ph)) * 0.08f);
                f.Body.localRotation = Quaternion.Euler(10f + Mathf.Sin(ph) * 6f, 0, 0);
            }
            else if (load != Ware.None)
            {
                // Both hands up to steady the load on the head.
                f.ArmL.localRotation = Quaternion.Euler(180f, 0, -12f);
                f.ArmR.localRotation = Quaternion.Euler(180f, 0, 12f);
                f.Body.localRotation = Quaternion.identity;
            }
            else
            {
                f.ArmL.localRotation = Quaternion.Euler(-swing, 0, 0);
                f.ArmR.localRotation = Quaternion.Euler(tool != ToolKind.None ? -35f + swing * 0.3f : swing, 0, 0);
                f.Body.localRotation = Quaternion.identity;
            }
        }
    }
}
