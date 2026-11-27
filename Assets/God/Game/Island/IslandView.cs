using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P4a, the organic island look. The sim still deals 19 hexes; this view hides them: one continuous ground and sea
    /// (<see cref="IslandGroundMesh"/>), terrains that blend along wandering borders and an irregular coast
    /// (<see cref="IslandField"/>), and props scattered over each hex's warped region (<see cref="IslandDressing"/>),
    /// combined into one mesh per hex. Where a building, flag or road stands (read from <c>GodState.Territory</c>) the
    /// props round it are left out, and inside build circles only a light share stays, so the free spots read. A
    /// producing hex glows softly over its own region (<see cref="Pulse(int)"/>). It reads the sim and never changes it.
    /// </summary>
    public sealed class IslandView : MonoBehaviour
    {
        public static IslandView Instance { get; private set; }

        public IslandField Field { get; private set; }
        public IslandGroundMesh Ground { get; private set; }
        public Board Board { get; private set; }
        /// <summary>Every scattered item, in a fixed order.</summary>
        public List<IslandItem> Items { get; private set; }
        /// <summary>Items generated per hex (before any culling).</summary>
        public int[] ItemCounts { get; private set; }
        /// <summary>Order- and value-sensitive hash of the placement (seed in, same hash out).</summary>
        public long PlacementHash { get; private set; }
        /// <summary>Times the culling ran (for tests).</summary>
        public int Refreshes { get; private set; }

        private bool[] _hidden;
        private GodSim _sim;
        private int _signature = int.MinValue;
        private Material _mat;
        private GameObject[] _hexProps;
        private List<int>[] _hexItems;
        // Bucket grid of items for the culling.
        private const float Bucket = 10f;
        private int _bn; private float _b0;
        private List<int>[] _buckets;

        /// <summary>Builds the island for a board and seed under a new object. A sim, when given, drives the culling.</summary>
        public static IslandView Create(Board board, ulong seed, GodSim sim, Transform parent)
        {
            var go = new GameObject("IslandView");
            if (parent != null) go.transform.SetParent(parent, false);
            var v = go.AddComponent<IslandView>();
            v._sim = sim;
            v.Build(board, seed);
            Instance = v;
            if (sim != null) v.RefreshNow();
            return v;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Build(Board board, ulong seed)
        {
            Board = board;
            Field = new IslandField(board, seed);
            _mat = MaterialFactory.CreateVertexColored(Color.white, "Island");

            Ground = new IslandGroundMesh(Field);
            var groundRoot = new GameObject("Ground");
            groundRoot.transform.SetParent(transform, false);
            Ground.Build(groundRoot.transform, _mat);
            var sea = new GameObject("Far sea");
            sea.transform.SetParent(transform, false);
            sea.AddComponent<MeshFilter>().sharedMesh = IslandGroundMesh.FarSea(Ground.Extent * 0.98f, new Color(0.62f, 0.78f, 0.90f));
            var sr = sea.AddComponent<MeshRenderer>();
            sr.sharedMaterial = _mat; sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Items = IslandDressing.Scatter(Field);
            PlacementHash = IslandDressing.PlacementHash(Items);
            ItemCounts = new int[Field.HexCount];
            _hexItems = new List<int>[Field.HexCount];
            for (int h = 0; h < Field.HexCount; h++) _hexItems[h] = new List<int>();
            for (int i = 0; i < Items.Count; i++) { ItemCounts[Items[i].Hex]++; _hexItems[Items[i].Hex].Add(i); }
            _hidden = new bool[Items.Count];

            _b0 = -Ground.Extent; _bn = Mathf.CeilToInt(Ground.Extent * 2f / Bucket) + 1;
            _buckets = new List<int>[_bn * _bn];
            for (int i = 0; i < Items.Count; i++)
            {
                int k = BucketOf(Items[i].Pos.x, Items[i].Pos.z);
                (_buckets[k] ?? (_buckets[k] = new List<int>())).Add(i);
            }

            _hexProps = new GameObject[Field.HexCount];
            for (int h = 0; h < Field.HexCount; h++) BuildHex(h);
        }

        private int BucketOf(float x, float z)
        {
            int i = Mathf.Clamp(Mathf.FloorToInt((x - _b0) / Bucket), 0, _bn - 1), j = Mathf.Clamp(Mathf.FloorToInt((z - _b0) / Bucket), 0, _bn - 1);
            return i * _bn + j;
        }

        private void BuildHex(int h)
        {
            if (_hexProps[h] != null) { DestroyObj(_hexProps[h].GetComponent<MeshFilter>().sharedMesh); DestroyObj(_hexProps[h]); _hexProps[h] = null; }
            var mb = new MeshBuilder();
            foreach (int i in _hexItems[h]) if (!_hidden[i]) IslandDressing.Emit(mb, Items[i]);
            var go = mb.ToObject("Dressing " + h + " " + Board.HexTerrain[h], transform, _mat);
            _hexProps[h] = go;
        }

        private static void DestroyObj(Object o) { if (o == null) return; if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }

        /// <summary>Items visible now.</summary>
        public int VisibleCount { get { int n = 0; for (int i = 0; i < _hidden.Length; i++) if (!_hidden[i]) n++; return n; } }
        public bool IsHidden(int item) => _hidden[item];

        /// <summary>Ground height of the island mesh at a world point (for views that stand things on it).</summary>
        public float HeightAt(Vector3 p) => Ground.HeightAt(p.x, p.z);

        // ------------------------------------------------------------------ culling round the territory

        private void LateUpdate()
        {
            if (_sim != null && _sim.State.Territory != null && Signature(_sim.State.Territory) != _signature) RefreshNow();
        }

        private static int Signature(TerritoryState t)
        {
            unchecked { return t.NextId * 31 + t.Hqs.Count * 7 + t.Buildings.Count * 13 + t.Flags.Count * 17 + t.Roads.Count * 19; }
        }

        /// <summary>Binds a sim (the culling follows its territory).</summary>
        public void Bind(GodSim sim) { _sim = sim; _signature = int.MinValue; }

        /// <summary>Recomputes which items are hidden by buildings, flags, roads and build circles, and rebuilds the hexes that changed.</summary>
        public void RefreshNow()
        {
            if (_sim == null || _sim.State.Territory == null) return;
            var t = _sim.State.Territory;
            _signature = Signature(t);
            Refreshes++;
            var g = t.Grid;
            var hide = new bool[Items.Count];

            // Build circles: light dressing inside (a soft ramp, no hard ring), nothing bulky.
            foreach (var hq in t.Hqs)
            {
                var c = t.CircleOf(hq);
                float R = (float)c.Radius;
                Visit((float)c.X, (float)c.Z, R + 8f, (i, d) =>
                {
                    var it = Items[i];
                    if (it.Bulky && d < R + 2f + it.Radius * 0.5f) return true;
                    float keep = Mathf.Lerp(0.28f, 1f, Mathf.SmoothStep(0f, 1f, (d - (R - 8f)) / 14f));
                    if (it.Kind == IslandItem.Kinds.Crop) keep = Mathf.Max(keep, 0.55f);
                    return it.Keep > keep;
                }, hide);
            }
            // Occupied nodes.
            foreach (var b in t.Buildings)
            {
                float r = b.Size == BuildingSize.Hq ? 8f : b.Size == BuildingSize.Medium ? 5.5f : 3.8f;
                Clear((float)g.X(b.Node), (float)g.Z(b.Node), r, hide);
            }
            foreach (var fl in t.Flags) Clear((float)g.X(fl.Node), (float)g.Z(fl.Node), 2.2f, hide);
            foreach (var rd in t.Roads)
                for (int k = 0; k < rd.Nodes.Length; k++)
                {
                    float x = (float)g.X(rd.Nodes[k]), z = (float)g.Z(rd.Nodes[k]);
                    Clear(x, z, 2.4f, hide);
                    if (k + 1 < rd.Nodes.Length) Clear((x + (float)g.X(rd.Nodes[k + 1])) * 0.5f, (z + (float)g.Z(rd.Nodes[k + 1])) * 0.5f, 2.4f, hide);
                }

            var dirty = new bool[Field.HexCount];
            for (int i = 0; i < hide.Length; i++)
                if (hide[i] != _hidden[i]) { dirty[Items[i].Hex] = true; _hidden[i] = hide[i]; }
            for (int h = 0; h < dirty.Length; h++) if (dirty[h]) BuildHex(h);
        }

        private void Clear(float x, float z, float r, bool[] hide)
            => Visit(x, z, r + 4f, (i, d) => d < r + Items[i].Radius * 0.6f, hide);

        private void Visit(float x, float z, float reach, System.Func<int, float, bool> hideIf, bool[] hide)
        {
            int i0 = Mathf.FloorToInt((x - reach - _b0) / Bucket), i1 = Mathf.FloorToInt((x + reach - _b0) / Bucket);
            int j0 = Mathf.FloorToInt((z - reach - _b0) / Bucket), j1 = Mathf.FloorToInt((z + reach - _b0) / Bucket);
            for (int i = Mathf.Max(0, i0); i <= Mathf.Min(_bn - 1, i1); i++)
                for (int j = Mathf.Max(0, j0); j <= Mathf.Min(_bn - 1, j1); j++)
                {
                    var list = _buckets[i * _bn + j];
                    if (list == null) continue;
                    foreach (int k in list)
                    {
                        if (hide[k]) continue;
                        float dx = Items[k].Pos.x - x, dz = Items[k].Pos.z - z;
                        float d = Mathf.Sqrt(dx * dx + dz * dz);
                        if (d <= reach && hideIf(k, d)) hide[k] = true;
                    }
                }
        }

        // ------------------------------------------------------------------ pulse

        public const float PulseSeconds = 1.6f;
        private sealed class Pulsing { public float T; public GameObject Glow; }
        private readonly Dictionary<int, Pulsing> _pulses = new Dictionary<int, Pulsing>();
        private Material _glowMat;
        private MaterialPropertyBlock _mpb;

        /// <summary>Hexes pulsing now (for tests and the demo).</summary>
        public IReadOnlyCollection<int> PulsingNow
        {
            get { var l = new List<int>(); foreach (var kv in _pulses) if (kv.Value.T > 0f) l.Add(kv.Key); return l; }
        }

        /// <summary>Softly lights each hex's warped region and makes its dressing hop. Ids out of range are ignored.</summary>
        public void Pulse(IEnumerable<int> hexes)
        {
            if (hexes == null) return;
            foreach (int h in hexes) Pulse(h);
        }

        public void Pulse(int h)
        {
            if (Field == null || h < 0 || h >= Field.HexCount) return;
            if (!_pulses.TryGetValue(h, out var p) || p.Glow == null)
            {
                if (_glowMat == null) _glowMat = MaterialFactory.CreateGlow(Color.white, "Island pulse");
                p = new Pulsing { Glow = new GameObject("Pulse glow " + h) };
                p.Glow.transform.SetParent(transform, false);
                p.Glow.AddComponent<MeshFilter>().sharedMesh = GlowMesh(h);
                var r = p.Glow.AddComponent<MeshRenderer>();
                r.sharedMaterial = _glowMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _pulses[h] = p;
            }
            p.T = PulseSeconds;
            p.Glow.SetActive(true);
        }

        /// <summary>A soft glow sheet over a hex's warped region: brightest inside, fading to nothing at its wandering border.</summary>
        private Mesh GlowMesh(int h)
        {
            var c = Field.Centre(h);
            const float reach = 95f, step = 3f;
            int n = Mathf.CeilToInt(reach * 2f / step) + 1;
            var verts = new List<Vector3>(n * n); var cols = new List<Color>(n * n); var tris = new List<int>();
            var a = new float[n * n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    float x = c.x - reach + i * step, z = c.y - reach + j * step;
                    int o = Field.Owner(x, z, out _, out float margin);
                    float inside = o == h ? margin : -margin;
                    float k = Mathf.SmoothStep(0f, 1f, (inside + 2f) / 14f);
                    if (Field.Coast(x, z) < 0f) k = 0f;
                    a[i * n + j] = k;
                    verts.Add(new Vector3(x, Ground.HeightAt(x, z) + 0.35f, z));
                    cols.Add(new Color(k, k, k, 1f));
                }
            for (int i = 0; i < n - 1; i++)
                for (int j = 0; j < n - 1; j++)
                {
                    int k00 = i * n + j, k10 = (i + 1) * n + j, k01 = i * n + j + 1, k11 = (i + 1) * n + j + 1;
                    if (a[k00] + a[k10] + a[k01] + a[k11] <= 0.001f) continue;
                    tris.Add(k00); tris.Add(k01); tris.Add(k11); tris.Add(k00); tris.Add(k11); tris.Add(k10);
                }
            var m = new Mesh { name = "Pulse " + h };
            m.SetVertices(verts); m.SetColors(cols); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        private void Update()
        {
            if (_pulses.Count == 0) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            foreach (var kv in _pulses)
            {
                var p = kv.Value;
                var props = _hexProps[kv.Key];
                if (p.Glow == null || !p.Glow.activeSelf) continue;
                p.T -= Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(p.T / PulseSeconds);
                if (p.T <= 0f)
                {
                    p.Glow.SetActive(false);
                    if (props != null) props.transform.localPosition = Vector3.zero;
                    continue;
                }
                float fade = Mathf.Sin(k * Mathf.PI);
                _mpb.SetColor("_Color", new Color(1.0f, 0.80f, 0.35f) * (0.45f * fade));
                p.Glow.GetComponent<MeshRenderer>().SetPropertyBlock(_mpb);
                if (props != null) props.transform.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * 1.2f * (1f - k), 0);
            }
        }
    }
}
