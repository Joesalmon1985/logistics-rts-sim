using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>A mesh with a colour per vertex (soft blends: road edges, glows). Smooth, up-facing normals.</summary>
    public sealed class VMesh
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<Color> C = new List<Color>();
        public readonly List<int> T = new List<int>();

        public int Add(Vector3 p, Color c) { V.Add(p); C.Add(c); return V.Count - 1; }
        public void Tri(int a, int b, int c) { T.Add(a); T.Add(b); T.Add(c); }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (V.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            var n = new List<Vector3>(V.Count);
            for (int i = 0; i < V.Count; i++) n.Add(Vector3.up);
            m.SetVertices(V); m.SetColors(C); m.SetNormals(n); m.SetTriangles(T, 0);
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>
    /// P4b ground details over the island: worn dirt roads that follow the ground (soft edges blended into the ground
    /// colour, a little lighter than the grass), flags as small poles with a faction pennant, natural heaps for the raw
    /// piles at hex centres, and, only when asked (placing mode), a soft glow on buildable spots. Rebuilt when the
    /// territory changes; reads the sim, never changes it.
    /// </summary>
    public sealed class GroundLayer : MonoBehaviour
    {
        private GodSim _sim;
        private GameObject _roads, _flags, _piles, _glow;
        private int _roadSig = int.MinValue, _pileSig = int.MinValue, _glowSig = int.MinValue;
        private bool _showBuildable;
        private Faction _buildableFor = Faction.Player;
        private float[] _scratch;

        public int RoadRebuilds { get; private set; }
        public int PileHeaps { get; private set; }
        public int FlagCount { get; private set; }
        public int GlowSpots { get; private set; }
        public bool BuildableShown => _showBuildable;

        public static GroundLayer Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("GroundLayer");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<GroundLayer>();
            l._sim = sim;
            return l;
        }

        public void ShowBuildable(bool on, Faction f = Faction.Player)
        {
            if (on == _showBuildable && f == _buildableFor) return;
            _showBuildable = on; _buildableFor = f; _glowSig = int.MinValue;
            if (!on && _glow != null) _glow.SetActive(false);
        }

        private void LateUpdate()
        {
            var s = _sim.State; var t = s.Territory;
            if (t == null) return;
            int built = 0;
            if (s.Economy != null) foreach (var b in s.Economy.Buildings) if (b.Built) built++;
            int rs; unchecked { rs = t.NextId * 31 + t.Roads.Count * 7 + t.Flags.Count * 13 + t.Hqs.Count; }
            if (rs != _roadSig) { _roadSig = rs; RebuildRoads(t); }
            int ps; unchecked { ps = 17; for (int i = 0; i < t.Piles.Length; i++) ps = ps * 31 + t.Piles[i]; }
            if (ps != _pileSig) { _pileSig = ps; RebuildPiles(s); }
            if (_showBuildable)
            {
                int gs; unchecked { gs = rs * 31 + built * 7 + (int)_buildableFor; }
                if (gs != _glowSig) { _glowSig = gs; RebuildGlow(t); }
            }
        }

        private Color GroundColour(float x, float z, float y)
        {
            var iv = IslandView.Instance;
            if (iv == null || iv.Field == null) return new Color(0.45f, 0.58f, 0.30f);
            if (_scratch == null || _scratch.Length != iv.Field.HexCount) _scratch = new float[iv.Field.HexCount];
            return iv.Field.ColourAt(x, z, y, _scratch);
        }

        private static void Replace(ref GameObject slot, GameObject next)
        {
            if (slot != null) { var mf = slot.GetComponent<MeshFilter>(); if (mf != null) Destroy(mf.sharedMesh); Destroy(slot); }
            slot = next;
        }

        // ---------------------------------------------------------------- roads and flags

        private static readonly Color Dirt = new Color(0.74f, 0.62f, 0.43f);

        private void RebuildRoads(TerritoryState t)
        {
            RoadRebuilds++;
            var g = t.Grid;
            var vm = new VMesh();
            foreach (var r in t.Roads)
            {
                var pts = new List<Vector3>(r.Nodes.Length);
                foreach (int n in r.Nodes) pts.Add(new Vector3((float)g.X(n), 0, (float)g.Z(n)));
                pts = Chaikin(Chaikin(pts));
                Track(vm, Resample(pts, 1.4f), r.Id);
            }
            // A worn patch round every flag so roads meet softly.
            foreach (var f in t.Flags) Patch(vm, (float)g.X(f.Node), (float)g.Z(f.Node), 1.8f, 0.4f);
            Replace(ref _roads, Obj("Roads", vm.ToMesh("Roads"), ModelKit.Lit));

            var mb = new MeshBuilder();
            FlagCount = 0;
            foreach (var f in t.Flags)
            {
                if (f.BuildingId != 0) { var b = t.BuildingById(f.BuildingId); if (b != null && b.Size == BuildingSize.Hq && b.Node == f.Node) continue; }
                var p = ModelKit.Node(g, f.Node);
                var top = p + Vector3.up * 2.7f;
                mb.Cylinder(p + Vector3.down * 0.2f, 0.32f, 0.26f, 0.35f, 6, ModelKit.Rock * 0.9f);
                mb.Cylinder(p, 0.08f, 0.07f, 2.8f, 5, ModelKit.DarkWood);
                var col = ModelKit.Accent(ModelKit.FactionColour(f.Faction));
                var d = ModelKit.Dir(65f + (f.Id * 37) % 40);
                ModelKit.T2(mb, top, top + Vector3.down * 0.8f, top + d * 1.3f + Vector3.down * 0.35f, col);
                FlagCount++;
            }
            Replace(ref _flags, Obj("Flags", mb.ToMesh("Flags"), ModelKit.Lit));
        }

        private static List<Vector3> Chaikin(List<Vector3> p)
        {
            if (p.Count < 3) return p;
            var o = new List<Vector3>(p.Count * 2) { p[0] };
            for (int i = 0; i < p.Count - 1; i++)
            {
                o.Add(Vector3.Lerp(p[i], p[i + 1], 0.25f));
                o.Add(Vector3.Lerp(p[i], p[i + 1], 0.75f));
            }
            o.Add(p[p.Count - 1]);
            return o;
        }

        private static List<Vector3> Resample(List<Vector3> p, float step)
        {
            var o = new List<Vector3> { p[0] };
            for (int i = 1; i < p.Count; i++)
            {
                float len = (p[i] - p[i - 1]).magnitude;
                int n = Mathf.Max(1, Mathf.CeilToInt(len / step));
                for (int k = 1; k <= n; k++) o.Add(Vector3.Lerp(p[i - 1], p[i], k / (float)n));
            }
            return o;
        }

        private void Track(VMesh vm, List<Vector3> pts, int seed)
        {
            if (pts.Count < 2) return;
            var rnd = new PropRandom(seed * 71 + 9);
            float[] off = { -1.9f, -1.0f, -0.35f, 0.35f, 1.0f, 1.9f };
            float[] blend = { 0f, 0.7f, 0.9f, 0.9f, 0.7f, 0f };
            int cols = off.Length;
            int start = vm.V.Count;
            float run = 0f, phase = rnd.Range(0f, 50f);
            for (int i = 0; i < pts.Count; i++)
            {
                var a = pts[Mathf.Max(0, i - 1)]; var b = pts[Mathf.Min(pts.Count - 1, i + 1)];
                var d = b - a; d.y = 0; d = d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward;
                var side = new Vector3(d.z, 0, -d.x);
                // A gentle meander (none at the ends, so roads still meet at their flags).
                if (i > 0) run += (pts[i] - pts[i - 1]).magnitude;
                float ends = Mathf.Clamp01(Mathf.Min(i, pts.Count - 1 - i) / 4f);
                var centre = pts[i] + side * ((Mathf.PerlinNoise(run * 0.09f, phase) - 0.5f) * 1.6f * ends);
                float wob = rnd.Range(0.88f, 1.12f), wear = rnd.Range(-0.08f, 0.06f);
                for (int k = 0; k < cols; k++)
                {
                    var q = centre + side * off[k] * wob;
                    float y = ModelKit.GroundY(q.x, q.z);
                    var gc = GroundColour(q.x, q.z, y);
                    float bl = Mathf.Clamp01(blend[k] + (k > 0 && k < cols - 1 ? wear : 0f));
                    var c = Color.Lerp(gc, Color.Lerp(gc, Dirt, 0.8f), bl);
                    vm.Add(new Vector3(q.x, y + 0.16f + (k > 0 && k < cols - 1 ? 0.03f : 0f), q.z), c);
                }
            }
            for (int i = 0; i < pts.Count - 1; i++)
                for (int k = 0; k < cols - 1; k++)
                {
                    int a = start + i * cols + k, b = a + 1, c = a + cols, e = c + 1;
                    vm.Tri(a, c, e); vm.Tri(a, e, b);
                    vm.Tri(a, e, c); vm.Tri(a, b, e);
                }
        }

        private void Patch(VMesh vm, float x, float z, float r, float strength)
        {
            float cy = ModelKit.GroundY(x, z);
            var gc0 = GroundColour(x, z, cy);
            int c0 = vm.Add(new Vector3(x, cy + 0.2f, z), Color.Lerp(gc0, Color.Lerp(gc0, Dirt, 0.62f), strength));
            const int n = 10;
            int first = vm.V.Count;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                float px = x + Mathf.Cos(a) * r, pz = z + Mathf.Sin(a) * r;
                float py = ModelKit.GroundY(px, pz);
                vm.Add(new Vector3(px, py + 0.15f, pz), GroundColour(px, pz, py));
            }
            for (int i = 0; i < n; i++) { vm.Tri(c0, first + (i + 1) % n, first + i); vm.Tri(c0, first + i, first + (i + 1) % n); }
        }

        // ---------------------------------------------------------------- piles

        /// <summary>World spot of pile slot i (0..2) on a hex, beside its centre.</summary>
        public static Vector3 PileSpot(NodeGrid g, int hex, int i)
        {
            float x = (float)g.Topology.HexX(hex) + (i - 1) * 4.2f + (i == 1 ? 0f : 0.6f), z = (float)g.Topology.HexZ(hex) - 10f + (i == 1 ? 1.6f : 0f);
            return ModelKit.OnGround(x, z);
        }

        private void RebuildPiles(GodState s)
        {
            var t = s.Territory; var g = t.Grid;
            var mb = new MeshBuilder();
            PileHeaps = 0;
            for (int h = 0; h < t.Piles.Length; h++)
            {
                var kind = TerritoryState.PileKind(s.Board, h);
                for (int i = 0; i < t.Piles[h]; i++) { PropModels.Pile(mb, PileSpot(g, h, i), kind, h * 3 + i); PileHeaps++; }
            }
            Replace(ref _piles, Obj("Piles", mb.ToMesh("Piles"), ModelKit.Lit));
        }

        // ---------------------------------------------------------------- buildable glow (placing mode)

        private void RebuildGlow(TerritoryState t)
        {
            var g = t.Grid;
            var vm = new VMesh();
            GlowSpots = 0;
            var warm = new Color(0.30f, 0.27f, 0.16f);
            foreach (int n in t.BuildableNodes(_buildableFor))
            {
                if (t.NodeBuilding[n] != 0 || t.NodeFlag[n] != 0 || t.NodeRoad[n] != 0) continue;
                float x = (float)g.X(n), z = (float)g.Z(n);
                float y = ModelKit.GroundY(x, z) + 0.3f;
                int c0 = vm.Add(new Vector3(x, y, z), warm);
                const int k = 10; const float r = 2.3f;
                int first = vm.V.Count;
                for (int i = 0; i < k; i++)
                {
                    float a = i * Mathf.PI * 2f / k;
                    float px = x + Mathf.Cos(a) * r, pz = z + Mathf.Sin(a) * r;
                    vm.Add(new Vector3(px, ModelKit.GroundY(px, pz) + 0.3f, pz), Color.black);
                }
                for (int i = 0; i < k; i++) vm.Tri(c0, first + (i + 1) % k, first + i);
                GlowSpots++;
            }
            Replace(ref _glow, Obj("Buildable glow", vm.ToMesh("Buildable glow"), ModelKit.Glow));
            _glow.SetActive(_showBuildable);
        }

        private GameObject Obj(string name, Mesh m, Material mat) => ModelKit.Part(name, transform, m, mat);
    }
}
