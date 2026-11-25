using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P4b buildings as models: one object per territory building (HQs included) drawing the shared mesh of its kind and
    /// faction, standing on the island ground with its door towards its flag. Construction sites show a frame that fills
    /// in with progress and a heap of the delivered wares. Working buildings smoke, turn their wheel, glow or show a
    /// bobbing worker; a paused one lowers its banner; one missing inputs shows a small empty-basket icon. Damage brings
    /// smoke (more as health drops) and a bar; a destroyed building collapses into rubble, which sinks away when the sim's
    /// rubble time runs out. Reads the sim only.
    /// </summary>
    public sealed class BuildingLayer : MonoBehaviour
    {
        private sealed class BView
        {
            public int Id, Node; public string Kind; public Faction Faction; public BuildingSize Size; public bool Citadel;
            public GameObject Root, Model, Site, SiteWares, Worker, Glow, Icon;
            public Transform Banner, Wheel;
            public BuildingRig Rig;
            public bool Built; public int SiteStage = -1, WareSig = -1;
            public float BannerDrop, SmokeT, DamageT, Seed;
            public float CollapseT = -1f; public Vector3 BasePos;
        }

        private sealed class RView { public int Id; public GameObject Go; public float Age; public Vector3 Base; public BuildingSize Size; }

        private GodSim _sim;
        private readonly Dictionary<int, BView> _views = new Dictionary<int, BView>();
        private readonly List<BView> _collapsing = new List<BView>();
        private readonly Dictionary<int, RView> _rubble = new Dictionary<int, RView>();
        private readonly HashSet<int> _alive = new HashSet<int>();
        private readonly List<int> _gone = new List<int>();
        private float _time;

        public int Count => _views.Count;
        public int SiteCount { get { int n = 0; foreach (var v in _views.Values) if (!v.Built) n++; return n; } }
        public int RubbleCount => _rubble.Count;
        public int CollapsedSoFar { get; private set; }
        /// <summary>Finished buildings drawn now, by kind (HQ is "hq").</summary>
        public int CountBuilt(string kind, Faction? f = null)
        {
            int n = 0;
            foreach (var v in _views.Values) if (v.Built && v.Kind == kind && (f == null || v.Faction == f.Value) && v.Model != null && v.Model.activeSelf) n++;
            return n;
        }
        public int WorkingNow { get; private set; }
        /// <summary>Views rebuilt because their building changed hands (a capture) or became a Citadel.</summary>
        public int Restyled { get; private set; }

        /// <summary>The faction a building is drawn for right now (null when it has no view), for tests.</summary>
        public Faction? DrawnFaction(int buildingId) => _views.TryGetValue(buildingId, out var v) ? v.Faction : (Faction?)null;

        /// <summary>The model object of a building (null while it is a site or has no view), for tests.</summary>
        public GameObject ModelOf(int buildingId) => _views.TryGetValue(buildingId, out var v) ? v.Model : null;

        public static BuildingLayer Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("BuildingLayer");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<BuildingLayer>();
            l._sim = sim;
            FxLayer.Ensure(parent);
            return l;
        }

        /// <summary>The model a view draws: the Citadel model for an upgraded HQ, else the model of its kind.</summary>
        private static string ModelKindOf(BView v) => v.Citadel ? BuildingModels.CitadelKind : v.Kind;

        private static string KindOf(Building b) => b.Size == BuildingSize.Hq ? TerritoryTuning.HqKind : b.Kind;

        private void LateUpdate()
        {
            var s = _sim.State; var t = s.Territory;
            if (t == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            _time += dt;
            var g = t.Grid; var e = s.Economy; var m = s.Military; var fx = FxLayer.Instance;
            _alive.Clear();
            WorkingNow = 0;
            foreach (var b in t.Buildings)
            {
                _alive.Add(b.Id);
                bool citadel = b.Size == BuildingSize.Hq && t.HqOf(b.Id)?.Citadel == true;
                if (_views.TryGetValue(b.Id, out var v) && (v.Faction != b.Faction || v.Citadel != citadel))
                {
                    // Captured (or upgraded): rebuild in the new owner's style and colour.
                    Destroy(v.Root); _views.Remove(b.Id); v = null; Restyled++;
                }
                if (v == null) v = Make(b, g);
                var eco = e?.Find(b.Id);
                bool built;
                if (eco != null) built = eco.Built;
                else { var hq = t.HqOf(b.Id); built = hq == null || !hq.Site; }
                if (built != v.Built || v.Model == null && v.Site == null) SetBuilt(v, built);
                if (!built) UpdateSite(v, e, eco);
                else UpdateBuilding(v, eco, dt, fx);
                var rec = m?.BuildingById(b.Id);
                if (rec != null && rec.Damaged) Damage(v, rec.HealthFraction, dt, fx);
            }

            _gone.Clear();
            foreach (var kv in _views) if (!_alive.Contains(kv.Key)) _gone.Add(kv.Key);
            foreach (int id in _gone)
            {
                var v = _views[id];
                _views.Remove(id);
                bool razed = false;
                if (m != null) foreach (var r in m.Rubbles) if (r.Node == v.Node) { razed = true; break; }
                if (razed && v.Built) StartCollapse(v, fx);
                else Destroy(v.Root);
            }
            UpdateCollapses(dt, fx);
            UpdateRubble(m, g, dt, fx);
        }

        // ---------------------------------------------------------------- make

        private BView Make(Building b, NodeGrid g)
        {
            var v = new BView { Id = b.Id, Node = b.Node, Kind = KindOf(b), Faction = b.Faction, Size = b.Size, Seed = (b.Id * 0.618f) % 1f };
            v.Citadel = b.Size == BuildingSize.Hq && _sim.State.Territory.HqOf(b.Id)?.Citadel == true;
            v.Root = new GameObject("Building " + v.Kind + " " + b.Id);
            v.Root.transform.SetParent(transform, false);
            var p = ModelKit.Node(g, b.Node);
            int fnode = b.FlagNode >= 0 && b.FlagNode != b.Node ? b.FlagNode : g.FlagNodeOf(b.Node);
            var fp = ModelKit.Node(g, fnode);
            v.BasePos = p;
            v.Root.transform.position = p;
            v.Root.transform.rotation = Quaternion.Euler(0, ModelKit.Yaw(fp - p), 0);
            v.Rig = BuildingModels.RigOf(ModelKindOf(v), v.Faction);
            _views[b.Id] = v;
            return v;
        }

        private void SetBuilt(BView v, bool built)
        {
            v.Built = built;
            if (built)
            {
                if (v.Site != null) { Destroy(v.Site); v.Site = null; }
                if (v.SiteWares != null) { Destroy(v.SiteWares.GetComponent<MeshFilter>().sharedMesh); Destroy(v.SiteWares); v.SiteWares = null; }
                if (v.Model == null) BuildModel(v);
                // A finished building settles with a little puff of dust.
                FxLayer.Instance?.Dust(v.BasePos, v.Rig.Radius * 0.6f, 6, new Color(0.75f, 0.68f, 0.55f));
            }
            else if (v.Model != null) { Destroy(v.Model); v.Model = null; }
        }

        private void BuildModel(BView v)
        {
            var rig = v.Rig; var tr = v.Root.transform;
            v.Model = ModelKit.Part("Model", tr, BuildingModels.MeshOf(ModelKindOf(v), v.Faction));
            var mt = v.Model.transform;
            var banner = ModelKit.Part("Banner", mt, BuildingModels.BannerMesh(v.Faction));
            banner.transform.localPosition = rig.BannerTop;
            banner.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            v.Banner = banner.transform;
            if (rig.HasWheel)
            {
                var w = ModelKit.Part("Wheel", mt, BuildingModels.WheelMesh(v.Kind, v.Faction, rig.WheelR));
                w.transform.localPosition = rig.Wheel;
                v.Wheel = w.transform;
            }
            if (rig.HasWorker)
            {
                var st = ModelStyle.Of(v.Faction);
                v.Worker = ModelKit.Part("Worker", mt, ModelKit.Cached("worker/" + rig.WorkerTool + "/" + (int)v.Faction,
                    mb => PeopleModels.Worker(mb, PeopleModels.Tunic(WalkerKind.Harvester, v.Faction), st.Accent, rig.WorkerTool)));
                v.Worker.transform.localPosition = rig.Worker;
                v.Worker.transform.localRotation = Quaternion.Euler(0, rig.WorkerYaw, 0);
                v.Worker.transform.localScale = Vector3.one * 1.1f;
                v.Worker.SetActive(false);
            }
            if (rig.HasGlow)
            {
                v.Glow = ModelKit.Part("Glow", mt, BuildingModels.GlowMesh(rig.GlowSize), ModelKit.Unlit);
                v.Glow.transform.localPosition = rig.Glow;
                v.Glow.SetActive(false);
            }
            v.Icon = ModelKit.Part("Missing", mt, ModelKit.Cached("icon/missing", IconMesh), ModelKit.Unlit);
            v.Icon.transform.localPosition = new Vector3(0, rig.Height + 1.4f, 0);
            v.Icon.transform.localScale = Vector3.one * 0.75f;
            v.Icon.SetActive(false);
        }

        /// <summary>The "missing inputs" icon: a small empty basket on a pale round sign.</summary>
        private static void IconMesh(MeshBuilder mb)
        {
            var pale = new Color(0.96f, 0.92f, 0.80f);
            for (int i = 0; i < 12; i++)
            {
                float a0 = i * Mathf.PI / 6f, a1 = (i + 1) * Mathf.PI / 6f;
                ModelKit.T2(mb, Vector3.zero, new Vector3(Mathf.Cos(a0) * 0.75f, Mathf.Sin(a0) * 0.75f, 0), new Vector3(Mathf.Cos(a1) * 0.75f, Mathf.Sin(a1) * 0.75f, 0), pale);
            }
            var basket = new Color(0.55f, 0.38f, 0.18f);
            mb.Quad(new Vector3(-0.42f, 0.2f, -0.03f), new Vector3(0.42f, 0.2f, -0.03f), new Vector3(0.3f, -0.35f, -0.03f), new Vector3(-0.3f, -0.35f, -0.03f), basket);
            mb.Quad(new Vector3(-0.42f, 0.2f, -0.03f), new Vector3(-0.3f, -0.35f, -0.03f), new Vector3(0.3f, -0.35f, -0.03f), new Vector3(0.42f, 0.2f, -0.03f), basket);
            mb.Box(new Vector3(0, 0.32f, -0.04f), new Vector3(0.7f, 0.08f, 0.04f), basket * 0.8f);
        }

        // ---------------------------------------------------------------- per frame

        private void UpdateSite(BView v, EconomyState e, EcoBuilding eco)
        {
            float k = e != null && eco != null ? e.SiteProgressPermille(v.Id) / 1000f : 0f;
            int stage = Mathf.Clamp(Mathf.FloorToInt(k * PropModels.SiteStages), 0, PropModels.SiteStages);
            if (stage != v.SiteStage)
            {
                v.SiteStage = stage;
                if (v.Site != null) Destroy(v.Site);
                v.Site = ModelKit.Part("Site " + stage, v.Root.transform, PropModels.SiteMesh(v.Size, v.Faction, stage));
            }
            int planks = 0, stone = 0, brick = 0;
            if (eco != null)
            {
                // Wares delivered but not yet built in: shrink the heap as the work goes on.
                float left = 1f - Mathf.Clamp01((k - 0.4f) / 0.6f);
                planks = Mathf.CeilToInt(eco.In[(int)Ware.Plank] * left);
                stone = Mathf.CeilToInt(eco.In[(int)Ware.Stone] * left);
                brick = Mathf.CeilToInt(eco.In[(int)Ware.Brick] * left);
            }
            int sig = planks * 10000 + stone * 100 + brick;
            if (sig != v.WareSig)
            {
                v.WareSig = sig;
                if (v.SiteWares != null) { Destroy(v.SiteWares.GetComponent<MeshFilter>().sharedMesh); Destroy(v.SiteWares); }
                var mb = new MeshBuilder();
                var half = PropModels.SiteHalf(v.Size);
                PropModels.SiteWares(mb, new Vector3(-half.x - 1.3f, 0, half.y + 0.4f), planks, stone, brick);
                v.SiteWares = ModelKit.Part("Site wares", v.Root.transform, mb.ToMesh("Site wares"));
            }
        }

        private void UpdateBuilding(BView v, EcoBuilding eco, float dt, FxLayer fx)
        {
            var rig = v.Rig; var tr = v.Model.transform;
            bool working = eco != null && (eco.Working || eco.Status == BuildStatus.Working);
            bool paused = eco != null && (eco.Paused || eco.Status == BuildStatus.Paused);
            bool missing = eco != null && eco.Status == BuildStatus.MissingInputs && FactionLook.IsHuman(_sim.State, v.Faction);
            if (working) WorkingNow++;

            // Banner: lowered to half mast when paused; a gentle sway.
            v.BannerDrop = Mathf.MoveTowards(v.BannerDrop, paused ? 2.3f : 0f, dt * 3f);
            v.Banner.localPosition = rig.BannerTop + Vector3.down * v.BannerDrop;
            // The cloth turns to face the camera (a flat cloth seen edge-on would vanish), swaying a little.
            var cam = Camera.main;
            float camYaw = cam != null ? cam.transform.eulerAngles.y : 0f;
            v.Banner.rotation = Quaternion.Euler(0, camYaw + 20f + Mathf.Sin(_time * 1.3f + v.Seed * 6f) * 10f, 0);

            if (v.Wheel != null && working) v.Wheel.localRotation *= Quaternion.Euler(rig.WheelSpeed * dt, 0, 0);
            if (v.Worker != null)
            {
                if (v.Worker.activeSelf != working) v.Worker.SetActive(working);
                if (working)
                {
                    float ph = _time * 5.5f + v.Seed * 6f;
                    v.Worker.transform.localPosition = rig.Worker + Vector3.up * Mathf.Abs(Mathf.Sin(ph)) * 0.12f;
                    v.Worker.transform.localRotation = Quaternion.Euler(0, rig.WorkerYaw, 0) * Quaternion.Euler(Mathf.Sin(ph) * 22f, 0, 0);
                }
            }
            if (v.Glow != null)
            {
                if (v.Glow.activeSelf != working) v.Glow.SetActive(working);
                if (working) v.Glow.transform.localScale = Vector3.one * (0.9f + 0.12f * Mathf.Sin(_time * 7f + v.Seed * 9f));
            }
            if (v.Icon.activeSelf != missing) v.Icon.SetActive(missing);
            if (missing)
            {
                v.Icon.transform.localPosition = new Vector3(0, rig.Height + 1.4f + Mathf.Sin(_time * 2f + v.Seed) * 0.2f, 0);
                if (cam != null) v.Icon.transform.rotation = cam.transform.rotation;
            }

            // Chimney smoke while working (the HQ hearth always smokes a little).
            if (fx != null && rig.Smoke.Count > 0 && (working || rig.AlwaysSmoke))
            {
                v.SmokeT -= dt;
                if (v.SmokeT <= 0f)
                {
                    v.SmokeT = working ? 0.32f : 0.9f;
                    foreach (var sp in rig.Smoke) fx.Smoke(tr.TransformPoint(sp), rig.SmokeColour, working ? 0.75f : 0.5f, 1.5f, working ? 2.8f : 2.0f);
                }
            }
        }

        private void Damage(BView v, float frac, float dt, FxLayer fx)
        {
            if (fx == null) return;
            float top = v.Rig.Height;
            fx.HealthBar(v.BasePos + Vector3.up * (top + 1.2f), v.Size == BuildingSize.Hq ? 6f : v.Size == BuildingSize.Medium ? 4.4f : 3.2f, frac);
            if (frac > 0.9f) return;
            float hurt = 1f - frac;
            v.DamageT -= dt;
            if (v.DamageT > 0f) return;
            v.DamageT = Mathf.Lerp(0.7f, 0.08f, hurt);
            var r = v.Rig.Radius * 0.45f;
            var p = v.BasePos + new Vector3(fx.Rand(-r, r), top * fx.Rand(0.35f, 0.7f), fx.Rand(-r, r));
            fx.Smoke(p, Color.Lerp(new Color(0.55f, 0.53f, 0.50f), new Color(0.16f, 0.15f, 0.15f), hurt), 0.8f + hurt * 1.2f, 2.2f, 3.0f);
            if (frac < 0.45f && fx.Rand(0f, 1f) < 0.6f) fx.Spark(p + Vector3.down * 0.6f, 0.7f + hurt, new Color(1f, 0.55f, 0.15f), 0.25f);
        }

        // ---------------------------------------------------------------- destruction

        private void StartCollapse(BView v, FxLayer fx)
        {
            v.CollapseT = 0f;
            CollapsedSoFar++;
            if (v.Banner != null) v.Banner.gameObject.SetActive(false);
            if (v.Worker != null) v.Worker.SetActive(false);
            if (v.Glow != null) v.Glow.SetActive(false);
            if (v.Icon != null) v.Icon.SetActive(false);
            _collapsing.Add(v);
            fx?.Dust(v.BasePos + Vector3.up * 0.5f, v.Rig.Radius, 14, new Color(0.50f, 0.46f, 0.41f));
        }

        private void UpdateCollapses(float dt, FxLayer fx)
        {
            for (int i = _collapsing.Count - 1; i >= 0; i--)
            {
                var v = _collapsing[i];
                v.CollapseT += dt;
                float k = Mathf.Clamp01(v.CollapseT / 1.3f);
                float h = v.Rig.Height;
                v.Root.transform.position = v.BasePos + Vector3.down * (h * k * k);
                v.Root.transform.localScale = new Vector3(1f + 0.15f * k, 1f - 0.5f * k, 1f + 0.15f * k);
                if (fx != null && fx.Rand(0f, 1f) < 0.35f) fx.Dust(v.BasePos, v.Rig.Radius * 0.8f, 1, new Color(0.48f, 0.44f, 0.40f));
                if (k >= 1f) { Destroy(v.Root); _collapsing.RemoveAt(i); }
            }
        }

        private void UpdateRubble(MilitaryState m, NodeGrid g, float dt, FxLayer fx)
        {
            _alive.Clear();
            if (m != null)
                foreach (var r in m.Rubbles)
                {
                    _alive.Add(r.Id);
                    if (!_rubble.TryGetValue(r.Id, out var rv))
                    {
                        rv = new RView { Id = r.Id, Size = r.Size, Base = ModelKit.OnGround(r.X / 1000f, r.Z / 1000f) };
                        rv.Go = ModelKit.Part("Rubble " + r.Id, transform, PropModels.RubbleMesh(r.Size, r.Faction, r.Id));
                        rv.Go.transform.position = rv.Base;
                        rv.Go.transform.rotation = Quaternion.Euler(0, r.Id * 47f, 0);
                        _rubble[r.Id] = rv;
                    }
                    rv.Age += dt;
                    // Rises in as the building falls; sinks away over the last seconds of the sim's rubble time.
                    float grow = Mathf.Clamp01(rv.Age / 1.2f);
                    float sink = Mathf.Clamp01(1f - r.TicksLeft / 40f);
                    rv.Go.transform.localScale = new Vector3(1f, Mathf.Max(0.05f, grow * (1f - sink)), 1f);
                    rv.Go.transform.position = rv.Base + Vector3.down * (sink * 0.6f);
                    if (fx != null && r.TicksLeft > 60 && fx.Rand(0f, 1f) < dt * 1.5f)
                        fx.Smoke(rv.Base + new Vector3(fx.Rand(-1.5f, 1.5f), 0.6f, fx.Rand(-1.5f, 1.5f)), new Color(0.32f, 0.30f, 0.29f), 0.6f, 1.2f, 2.2f);
                }
            _gone.Clear();
            foreach (var kv in _rubble) if (!_alive.Contains(kv.Key)) _gone.Add(kv.Key);
            foreach (int id in _gone) { Destroy(_rubble[id].Go); _rubble.Remove(id); }
        }
    }
}
