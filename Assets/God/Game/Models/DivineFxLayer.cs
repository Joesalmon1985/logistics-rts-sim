using System.Collections.Generic;
using God.Sim;
using God.Sim.Divinity;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// U3b: draws the divine effects (VISUAL_WORLD 9, 11). Each sim event goes through the pure <see cref="DivineFxPlanner"/>;
    /// every record becomes one drawn effect (a procedural mesh in the effect's family colour, with a core in the god's own
    /// colour), placed on the real person, hut, source or lot the event names. Nothing is drawn without an event; persistent
    /// effects (tether, possession rim, avatar, consecration) stay until their closing event. Over the budget of 24 the rest
    /// are small markers. View only: reads events and positions, writes nothing to the sim.
    /// </summary>
    public sealed class DivineFxLayer : MonoBehaviour
    {
        public static DivineFxLayer Instance { get; private set; }

        private sealed class Live
        {
            public FxRecord R; public GameObject Go; public float Born, ClosedAt = -1f; public LineRenderer Line; public Vector3 Pos; public bool Placed;
        }

        private readonly DivineFxPlanner _planner = new DivineFxPlanner();
        private readonly List<Live> _live = new List<Live>();
        private GodBootstrap _boot;
        private bool _subscribed;

        /// <summary>Every record planned (tests, capped).</summary>
        public readonly List<FxRecord> Records = new List<FxRecord>();
        public int ActiveCount => _live.Count;
        public int DetailedActive { get { int n = 0; foreach (var l in _live) if (l.R.Detailed) n++; return n; } }
        public IEnumerable<FxRecord> ActiveRecords { get { foreach (var l in _live) yield return l.R; } }
        public GameObject ObjectOf(int recordId) { foreach (var l in _live) if (l.R.Id == recordId) return l.Go; return null; }

        public static DivineFxLayer Create(GodBootstrap boot, Transform parent)
        {
            var go = new GameObject("DivineFxLayer");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<DivineFxLayer>();
            l._boot = boot;
            if (boot != null) { boot.SimEventRaised += l.OnEvent; l._subscribed = true; }
            Instance = l;
            return l;
        }

        private void OnDestroy()
        {
            if (_subscribed && _boot != null) _boot.SimEventRaised -= OnEvent;
            if (Instance == this) Instance = null;
        }

        /// <summary>Feeds a sim event (the bootstrap pump; tests call it directly).</summary>
        public void OnEvent(SimEvent e)
        {
            foreach (var r in _planner.Plan(e)) Spawn(r);
        }

        /// <summary>The same for an event with no type yet (D4 and D6 codes).</summary>
        public void OnCode(ushort code, long tick, int god, int npc, int node)
        {
            foreach (var r in _planner.PlanCode(code, tick, 0, god, npc, node)) Spawn(r);
        }

        public static Color ColourOf(FxColour c)
        {
            switch (c)
            {
                case FxColour.Pale: return new Color(0.95f, 0.95f, 0.85f);
                case FxColour.Gold: return new Color(1f, 0.82f, 0.25f);
                case FxColour.Violet: return new Color(0.58f, 0.30f, 0.92f);
                case FxColour.Teal: return new Color(0.15f, 0.85f, 0.80f);
                case FxColour.White: return new Color(1f, 1f, 1f);
                case FxColour.Grey: return new Color(0.55f, 0.57f, 0.60f);
                case FxColour.Green: return new Color(0.30f, 0.90f, 0.40f);
                case FxColour.Blue: return new Color(0.30f, 0.55f, 1f);
                case FxColour.Red: return new Color(0.95f, 0.18f, 0.12f);
                case FxColour.Amber: return new Color(1f, 0.62f, 0.10f);
                case FxColour.Brown: return new Color(0.50f, 0.36f, 0.22f);
                default: return new Color(1f, 0.45f, 0.10f);
            }
        }

        private void Spawn(FxRecord r)
        {
            if (Records.Count < 5000) Records.Add(r);
            var go = new GameObject("Fx " + r.Name + " " + r.Id);
            go.transform.SetParent(transform, false);
            var lv = new Live { R = r, Go = go, Born = Time.time };
            var col = ColourOf(r.Colour);
            var mat = MaterialFactory.SharedUnlit(col);
            Mesh mesh = r.Detailed ? ShapeMesh(r.Shape) : ShapeMesh(FxShape.Spark);
            var main = ModelKit.Part("Body", go.transform, mesh, mat);
            if (r.Detailed && r.Shape != FxShape.Figure)
            {
                var g = new Color(r.GodTint.R, r.GodTint.G, r.GodTint.B);
                ModelKit.Part("GodCore", go.transform, CoreMesh(r.Sigil), MaterialFactory.SharedUnlit(g)).transform.localPosition = new Vector3(0, 2.2f, 0);
            }
            if (r.Detailed && r.Shape == FxShape.Tether)
            {
                var lgo = new GameObject("Tether"); lgo.transform.SetParent(go.transform, false);
                lv.Line = lgo.AddComponent<LineRenderer>(); lv.Line.useWorldSpace = true; lv.Line.positionCount = 2; lv.Line.widthMultiplier = 0.09f;
                lv.Line.sharedMaterial = mat; lv.Line.startColor = col; lv.Line.endColor = col;
            }
            if (r.Shape == FxShape.Collapse && FxLayer.Instance != null) lv.Pos = Vector3.zero;
            _live.Add(lv);
            Place(lv);
        }

        private bool Locate(FxRecord r, out Vector3 p)
        {
            p = default;
            var b = _boot != null ? _boot : GodBootstrap.Instance;
            if (b == null) return false;
            if (r.Npc != 0 && b.People != null && b.People.TryPosition(r.Npc, out p)) return true;
            var pop = b.Sim.State.Population;
            int node = r.Node;
            if (pop != null)
            {
                if (r.ThingKind == MiracleThing.Hut) foreach (var h in pop.Huts) if (h.Id == r.ThingId) { node = h.Node; break; }
                if (r.ThingKind == MiracleThing.Source) foreach (var s in pop.Sources) if (s.Id == r.ThingId) { node = s.Node; break; }
                if (r.Lot != 0) foreach (var l in pop.Lots) if (l.Lot.Id == r.Lot) { node = l.Node; break; }
            }
            if (node < 0) return false;
            p = ModelKit.Node(b.Sim.State.Territory.Grid, node);
            return true;
        }

        private void Place(Live l)
        {
            if (Locate(l.R, out var p)) { l.Go.transform.position = p; l.Pos = p; l.Placed = true; l.Go.SetActive(true); }
            else l.Go.SetActive(false);
        }

        private static float Seconds(FxPersist p) => p == FxPersist.Flash ? 2f : p == FxPersist.Short ? 8f : p == FxPersist.Sustained ? 30f : 1e9f;

        private void Update()
        {
            var b = _boot != null ? _boot : GodBootstrap.Instance;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var l = _live[i];
                bool persistent = l.R.Persist == FxPersist.Persistent;
                if (persistent && l.R.EndTick != long.MaxValue && l.ClosedAt < 0f) l.ClosedAt = Time.time;
                float age = Time.time - l.Born, life = Seconds(l.R.Persist);
                if (persistent) { age = l.ClosedAt < 0f ? 0f : Time.time - l.ClosedAt; life = 1.2f; }
                if (age >= life) { Destroy(l.Go); _live.RemoveAt(i); continue; }
                if (l.R.Npc != 0 || !l.Placed) Place(l);
                if (!l.Go.activeSelf) continue;
                float k = age / life;
                float fade = persistent && l.ClosedAt < 0f ? 1f : Mathf.Clamp01((1f - k) * 4f);
                var t = l.Go.transform;
                float s = Mathf.Max(0.02f, fade) * (l.R.Detailed ? 1f : 0.5f);
                switch (l.R.Shape)
                {
                    case FxShape.Implode: s *= 1f - k; break;
                    case FxShape.Burst: case FxShape.Snap: s *= 0.5f + 1.5f * Mathf.Clamp01(k * 3f); break;
                    case FxShape.Collapse: s *= 0.6f + 0.8f * Mathf.Clamp01(k * 2f); break;
                }
                t.localScale = Vector3.one * s;
                float yo = 0f;
                if (l.R.Shape == FxShape.Motes || l.R.Shape == FxShape.HealSpiral) yo = k * 1.5f;
                if (l.R.Shape == FxShape.Dissolve) yo = (1f - k) * 1.5f;
                t.position = l.Pos + Vector3.up * yo;
                if (l.R.Shape == FxShape.Halo || l.R.Shape == FxShape.Rim || l.R.Shape == FxShape.Ring || l.R.Shape == FxShape.Glyph || l.R.Shape == FxShape.HealSpiral || l.R.Shape == FxShape.Aura)
                    t.rotation = Quaternion.Euler(0, Time.time * 60f, 0);
                if (l.Line != null && b != null)
                {
                    var from = l.Pos + Vector3.up * 1.6f;
                    var to = from + Vector3.up * 6f;
                    int node = l.R.Node;
                    if (node >= 0) to = ModelKit.Node(b.Sim.State.Territory.Grid, node, 1.2f);
                    l.Line.SetPosition(0, from); l.Line.SetPosition(1, to);
                }
            }
        }

        // ---------------- meshes ----------------

        private static Mesh CoreMesh(int sigil) => ModelKit.Cached("divfx/core" + sigil, mb =>
        {
            // a small sigil per god: 1 + sigil pointed boxes around a core
            int n = 3 + sigil;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                mb.Box(new Vector3(Mathf.Cos(a) * 0.22f, 0, Mathf.Sin(a) * 0.22f), new Vector3(0.1f, 0.1f, 0.1f), Color.white, -a * Mathf.Rad2Deg);
            }
            mb.Box(Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), Color.white, 45f);
        });

        private static void Ring(MeshBuilder mb, float r, float y, int n, float w, float h)
        {
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                mb.Box(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), new Vector3(w * 2f, h, w), Color.white, -a * Mathf.Rad2Deg);
            }
        }

        public static Mesh ShapeMesh(FxShape s) => ModelKit.Cached("divfx/" + s, mb =>
        {
            var w = Color.white;
            switch (s)
            {
                case FxShape.Halo: Ring(mb, 0.8f, 3.0f, 14, 0.07f, 0.07f); break;
                case FxShape.Chevron:
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 4.2f - i * 0.5f;
                        ModelKit.Beam(mb, new Vector3(-0.5f, y + 0.3f, 0), new Vector3(0, y, 0), 0.07f, w);
                        ModelKit.Beam(mb, new Vector3(0.5f, y + 0.3f, 0), new Vector3(0, y, 0), 0.07f, w);
                    }
                    break;
                case FxShape.Tether: Ring(mb, 0.55f, 1.6f, 10, 0.06f, 0.18f); break;
                case FxShape.Rim:
                    Ring(mb, 1.1f, 0.08f, 16, 0.07f, 0.07f);
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; ModelKit.Beam(mb, new Vector3(Mathf.Cos(a) * 1.1f, 0.1f, Mathf.Sin(a) * 1.1f), new Vector3(Mathf.Cos(a) * 1.1f, 2.4f, Mathf.Sin(a) * 1.1f), 0.03f, w); }
                    break;
                case FxShape.Column: mb.Cylinder(Vector3.zero, 0.7f, 0.4f, 16f, 10, w); break;
                case FxShape.Figure: AvatarModel.Build(mb); break;
                case FxShape.Implode: Ring(mb, 1.6f, 0.2f, 12, 0.1f, 0.1f); Ring(mb, 0.9f, 1.8f, 10, 0.08f, 0.08f); break;
                case FxShape.HealSpiral:
                    for (int i = 0; i < 18; i++) { float a = i * 0.7f; mb.Box(new Vector3(Mathf.Cos(a) * 0.7f, 0.15f + i * 0.14f, Mathf.Sin(a) * 0.7f), Vector3.one * 0.13f, w, a * 57f); }
                    break;
                case FxShape.Aura:
                    Ring(mb, 1.2f, 0.06f, 14, 0.07f, 0.06f);
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; mb.Box(new Vector3(Mathf.Cos(a) * 1.2f, 0.9f, Mathf.Sin(a) * 1.2f), new Vector3(0.05f, 1.8f, 0.05f), w); }
                    break;
                case FxShape.Bolt:
                {
                    var p = new Vector3(0, 16f, 0);
                    for (int i = 0; i < 8; i++)
                    {
                        var q = new Vector3(((i % 2) * 2 - 1) * 0.9f, 16f - (i + 1) * 2f, ((i % 3) - 1) * 0.5f);
                        if (i == 7) q = new Vector3(0, 0, 0);
                        ModelKit.Beam(mb, p, q, 0.14f, w); p = q;
                    }
                    break;
                }
                case FxShape.Burst:
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; ModelKit.Beam(mb, new Vector3(Mathf.Cos(a) * 0.3f, 1.2f, Mathf.Sin(a) * 0.3f), new Vector3(Mathf.Cos(a) * 1.6f, 1.6f, Mathf.Sin(a) * 1.6f), 0.07f, w); }
                    break;
                case FxShape.Collapse:
                    for (int i = 0; i < 12; i++) { float a = i * 0.9f; mb.Box(new Vector3(Mathf.Cos(a) * (0.8f + i * 0.12f), 0.3f + (i % 3) * 0.3f, Mathf.Sin(a) * (0.8f + i * 0.12f)), Vector3.one * (0.5f - i * 0.02f), w, a * 40f); }
                    break;
                case FxShape.Spark:
                    for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2f; ModelKit.Beam(mb, new Vector3(0, 1.8f, 0), new Vector3(Mathf.Cos(a) * 0.4f, 2.2f, Mathf.Sin(a) * 0.4f), 0.04f, w); }
                    break;
                case FxShape.Motes:
                    for (int i = 0; i < 10; i++) mb.Box(new Vector3(Mathf.Sin(i * 2.3f) * 0.5f, 0.2f + i * 0.2f, Mathf.Cos(i * 1.7f) * 0.5f), Vector3.one * 0.1f, w, i * 30f);
                    break;
                case FxShape.Dissolve:
                    for (int i = 0; i < 10; i++) mb.Box(new Vector3(Mathf.Sin(i * 1.9f) * 0.6f, 0.2f + i * 0.15f, Mathf.Cos(i * 2.1f) * 0.6f), Vector3.one * 0.12f, w, i * 20f);
                    break;
                case FxShape.Ring: Ring(mb, 3f, 0.07f, 28, 0.09f, 0.07f); Ring(mb, 2.7f, 0.07f, 24, 0.05f, 0.05f); break;
                case FxShape.Glyph:
                    mb.Box(new Vector3(0, 3f, 0), new Vector3(0.4f, 0.8f, 0.08f), w, 0f);
                    mb.Box(new Vector3(0, 3f, 0), new Vector3(0.8f, 0.4f, 0.08f), w, 0f);
                    break;
                case FxShape.Thread: ModelKit.Beam(mb, new Vector3(0, 1.8f, 0), new Vector3(0, 14f, 0), 0.03f, w); break;
                case FxShape.Snap: for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3f; ModelKit.Beam(mb, new Vector3(Mathf.Cos(a) * 0.3f, 1.4f, Mathf.Sin(a) * 0.3f), new Vector3(Mathf.Cos(a) * 0.9f, 1.4f, Mathf.Sin(a) * 0.9f), 0.04f, w); } break;
                case FxShape.Cross:
                    ModelKit.Beam(mb, new Vector3(-0.4f, 2.7f, 0), new Vector3(0.4f, 3.5f, 0), 0.06f, w);
                    ModelKit.Beam(mb, new Vector3(-0.4f, 3.5f, 0), new Vector3(0.4f, 2.7f, 0), 0.06f, w);
                    break;
            }
        });
    }

    /// <summary>The avatar's own model: a tall robed figure with a halo, never the villager figure (U3-T13).</summary>
    public static class AvatarModel
    {
        public static void Build(MeshBuilder mb)
        {
            var robe = new Color(0.96f, 0.95f, 0.90f); var gold = new Color(1f, 0.85f, 0.35f);
            mb.Cone(Vector3.zero, 0.95f, 2.6f, 10, robe);
            mb.Cylinder(new Vector3(0, 2.4f, 0), 0.45f, 0.35f, 0.5f, 8, robe);
            mb.Box(new Vector3(0, 3.1f, 0), new Vector3(0.5f, 0.55f, 0.5f), ModelKit.Skin);
            ModelKit.Beam(mb, new Vector3(-0.55f, 2.6f, 0), new Vector3(-1.0f, 1.8f, 0.4f), 0.1f, robe);
            ModelKit.Beam(mb, new Vector3(0.55f, 2.6f, 0), new Vector3(1.0f, 1.8f, 0.4f), 0.1f, robe);
            for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6f; mb.Box(new Vector3(Mathf.Cos(a) * 0.55f, 3.75f, Mathf.Sin(a) * 0.55f), new Vector3(0.18f, 0.06f, 0.06f), gold, -a * Mathf.Rad2Deg); }
            mb.Box(new Vector3(0, 1.6f, 0.9f), new Vector3(0.9f, 0.06f, 0.12f), gold);
        }
    }
}
