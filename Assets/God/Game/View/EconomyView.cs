using System.Collections.Generic;
using System.Text;
using UnityEngine;
using God.Sim;

namespace God.Game
{
    /// <summary>
    /// P2 debug view of the economy, drawn over <see cref="TerritoryView"/>: a coloured block per building kind, sites that
    /// grow with progress, carriers (capsules) with a coloured cube for the ware, builders and harvester walkers, kind and
    /// status labels, and a tiny text list of the HQ stock. It only reads <see cref="EconomyState"/>; it never mutates the sim.
    /// </summary>
    public sealed class EconomyView : MonoBehaviour
    {
        private const float WalkerY = 1.3f;

        private sealed class Marker { public GameObject Root, Body, Cargo, Block, Status; public Renderer CargoR; }

        private GodSim _sim;
        private readonly Dictionary<int, Marker> _walkers = new Dictionary<int, Marker>();
        private readonly Dictionary<int, Marker> _buildings = new Dictionary<int, Marker>();
        private readonly HashSet<int> _alive = new HashSet<int>();
        private GUIStyle _label, _stock;
        private readonly StringBuilder _sb = new StringBuilder();

        public GodSim Sim => _sim;
        public int WalkerCount => _walkers.Count;
        public int BuildingMarkerCount => _buildings.Count;

        public static EconomyView Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("EconomyView");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<EconomyView>();
            v._sim = sim;
            return v;
        }

        public static Color WareColour(Ware w)
        {
            switch (w)
            {
                case Ware.Log: return new Color(0.50f, 0.33f, 0.16f);
                case Ware.Clay: return new Color(0.80f, 0.40f, 0.22f);
                case Ware.Grain: return new Color(0.92f, 0.80f, 0.25f);
                case Ware.Rock: return new Color(0.60f, 0.62f, 0.66f);
                case Ware.Stone: return new Color(0.78f, 0.80f, 0.85f);
                case Ware.Ore: return new Color(0.45f, 0.25f, 0.55f);
                case Ware.Plank: return new Color(0.86f, 0.68f, 0.40f);
                case Ware.Kindling: return new Color(0.98f, 0.55f, 0.12f);
                case Ware.Brick: return new Color(0.70f, 0.20f, 0.18f);
                case Ware.Iron: return new Color(0.30f, 0.45f, 0.62f);
                default: return Color.magenta;
            }
        }

        public static Color KindColour(string kind)
        {
            switch (kind)
            {
                case BuildingKinds.Woodcutter: return new Color(0.35f, 0.62f, 0.25f);
                case BuildingKinds.ClayPit: return new Color(0.80f, 0.45f, 0.30f);
                case BuildingKinds.Quarry: return new Color(0.70f, 0.72f, 0.76f);
                case BuildingKinds.Mine: return new Color(0.40f, 0.30f, 0.55f);
                case BuildingKinds.Farm: return new Color(0.95f, 0.85f, 0.30f);
                case BuildingKinds.Sawmill: return new Color(0.75f, 0.55f, 0.30f);
                case BuildingKinds.Kiln: return new Color(0.78f, 0.25f, 0.20f);
                case BuildingKinds.Smelter: return new Color(0.30f, 0.45f, 0.70f);
                case BuildingKinds.Barracks: return new Color(0.20f, 0.60f, 0.45f);
                case BuildingKinds.VehicleWorkshop: return new Color(0.15f, 0.75f, 0.85f);
                default: return Color.white;
            }
        }

        public static Color StatusColour(BuildStatus s)
        {
            switch (s)
            {
                case BuildStatus.Working: return new Color(0.2f, 0.85f, 0.3f);
                case BuildStatus.Idle: return new Color(0.7f, 0.7f, 0.7f);
                case BuildStatus.MissingInputs:
                case BuildStatus.NoPiles: return new Color(0.95f, 0.8f, 0.2f);
                case BuildStatus.OutputFull:
                case BuildStatus.WrongTerrain:
                case BuildStatus.SiteNoRoad: return new Color(0.9f, 0.25f, 0.2f);
                case BuildStatus.Paused: return new Color(0.3f, 0.5f, 0.95f);
                default: return new Color(0.9f, 0.6f, 0.2f);
            }
        }

        private GameObject Prim(PrimitiveType type, Transform parent, Color c, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = MaterialFactory.Shared(c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static Vector3 P(NodeGrid g, int node, float y) => new Vector3((float)g.X(node), y, (float)g.Z(node));

        private void LateUpdate()
        {
            var s = _sim.State; var e = s.Economy;
            if (e == null || s.Territory == null) return;
            var g = s.Territory.Grid;
            UpdateBuildings(s, e, g);
            UpdateWalkers(e, g);
        }

        private void UpdateBuildings(GodState s, EconomyState e, NodeGrid g)
        {
            _alive.Clear();
            foreach (var b in e.Buildings)
            {
                if (b.IsHq && b.Built) continue; // the HQ block is drawn by the territory view
                _alive.Add(b.Id);
                var def = b.Def;
                if (!_buildings.TryGetValue(b.Id, out var m))
                {
                    m = new Marker { Root = new GameObject("Eco " + b.Kind + " " + b.Id) };
                    m.Root.transform.SetParent(transform, false);
                    m.Root.transform.position = P(g, b.Node, 0f);
                    _buildings[b.Id] = m;
                }
                float size = b.IsHq ? 9f : b.Built ? (def.Size == BuildingSize.Medium ? 6.5f : 3.6f) : (def.Size == BuildingSize.Medium ? 6.5f : def.Size == BuildingSize.Hq ? 9f : 3.6f);
                var colour = b.IsHq ? new Color(0.8f, 0.8f, 0.85f) : KindColour(b.Kind);
                bool isBuilt = b.Built;
                if (!isBuilt)
                {
                    // A site: a block that rises with progress. It is rebuilt when the stage changes.
                    float k = Mathf.Clamp01(e.SiteProgressPermille(b.Id) / 1000f);
                    float h = Mathf.Lerp(0.4f, b.IsHq ? 7f : def.Size == BuildingSize.Medium ? 4.8f : 3.4f, k);
                    if (m.Block == null) m.Block = Prim(PrimitiveType.Cube, m.Root.transform, Color.Lerp(colour, new Color(0.45f, 0.40f, 0.33f), 0.5f), Vector3.one);
                    m.Block.transform.localScale = new Vector3(size, h, size);
                    m.Block.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
                    if (m.Status == null) m.Status = Prim(PrimitiveType.Cube, m.Root.transform, Color.white, new Vector3(1.2f, 1.2f, 1.2f));
                }
                else
                {
                    if (m.Block != null) { Destroy(m.Block); m.Block = null; }
                    if (m.Body == null)
                    {
                        float roofY = def.Size == BuildingSize.Medium ? 5.6f : 3.9f;
                        m.Body = Prim(PrimitiveType.Cube, m.Root.transform, colour, new Vector3(size * 0.7f, 1.4f, size * 0.7f));
                        m.Body.transform.localPosition = new Vector3(0f, roofY, 0f);
                        if (m.Status == null) m.Status = Prim(PrimitiveType.Cube, m.Root.transform, Color.white, new Vector3(1.2f, 1.2f, 1.2f));
                    }
                }
                if (m.Status != null)
                {
                    m.Status.transform.localPosition = new Vector3(0f, (isBuilt ? (def.Size == BuildingSize.Medium ? 7.6f : 5.9f) : 8f), 0f);
                    m.Status.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Shared(StatusColour(b.Status));
                }
            }
            List<int> gone = null;
            foreach (var kv in _buildings)
                if (!_alive.Contains(kv.Key)) (gone ?? (gone = new List<int>())).Add(kv.Key);
            if (gone != null) foreach (int id in gone) { Destroy(_buildings[id].Root); _buildings.Remove(id); }
        }

        private void UpdateWalkers(EconomyState e, NodeGrid g)
        {
            _alive.Clear();
            foreach (var w in e.Walkers)
            {
                _alive.Add(w.Id);
                if (!_walkers.TryGetValue(w.Id, out var m))
                {
                    m = new Marker { Root = new GameObject("Walker " + w.Kind + " " + w.Id) };
                    m.Root.transform.SetParent(transform, false);
                    var col = w.Kind == WalkerKind.Carrier ? new Color(0.96f, 0.96f, 0.92f) : w.Kind == WalkerKind.Builder ? new Color(0.95f, 0.55f, 0.15f) : new Color(0.35f, 0.75f, 0.35f);
                    float scale = w.Kind == WalkerKind.Carrier ? 0.8f : 1.0f;
                    m.Body = Prim(PrimitiveType.Capsule, m.Root.transform, col, new Vector3(scale, scale * 1.1f, scale));
                    m.Cargo = Prim(PrimitiveType.Cube, m.Root.transform, Color.white, new Vector3(1.0f, 1.0f, 1.0f));
                    m.Cargo.transform.localPosition = new Vector3(0f, 1.9f, 0f);
                    m.CargoR = m.Cargo.GetComponent<Renderer>();
                    _walkers[w.Id] = m;
                }
                int a = w.CurrentNode, b = w.NextNode;
                if (a >= 0)
                {
                    var pa = P(g, a, WalkerY); var pb = P(g, b, WalkerY);
                    m.Root.transform.position = Vector3.Lerp(pa, pb, w.StepPermille / 1000f);
                    var dir = pb - pa;
                    if (dir.sqrMagnitude > 0.01f) m.Root.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
                bool carrying = w.Cargo != Ware.None;
                m.Cargo.SetActive(carrying);
                if (carrying) m.CargoR.sharedMaterial = MaterialFactory.Shared(WareColour(w.Cargo));
            }
            List<int> gone = null;
            foreach (var kv in _walkers)
                if (!_alive.Contains(kv.Key)) (gone ?? (gone = new List<int>())).Add(kv.Key);
            if (gone != null) foreach (int id in gone) { Destroy(_walkers[id].Root); _walkers.Remove(id); }
        }

        private void OnGUI()
        {
            var s = _sim.State; var e = s.Economy;
            if (e == null) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
                _stock = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = new Color(1f, 1f, 0.9f) } };
            }
            var cam = Camera.main;
            if (cam != null)
            {
                var g = s.Territory.Grid;
                foreach (var b in e.Buildings)
                {
                    if (b.IsHq && b.Built) continue;
                    var sp = cam.WorldToScreenPoint(P(g, b.Node, 9f));
                    if (sp.z < 0) continue;
                    string text = b.Built ? b.Def.Label + "\n" + b.Status
                        : b.Def.Label + " " + (e.SiteProgressPermille(b.Id) / 10) + "%\n" + b.Status;
                    GUI.Label(new Rect(sp.x - 60f, Screen.height - sp.y - 14f, 120f, 28f), text, _label);
                }
            }
            _sb.Length = 0;
            foreach (var hq in e.HqsOf(Faction.Player))
            {
                _sb.Append("HQ ").Append(hq.Id).Append("  carriers ").Append(hq.IdleCarriers).Append('/').Append(EconomyTuning.CarriersPerHq).Append('\n');
                for (int w = 1; w < Wares.Count; w++)
                    if (hq.Stock[w] > 0 && (Ware)w != Ware.Rock) _sb.Append("  ").Append(Wares.Name((Ware)w)).Append(' ').Append(hq.Stock[w]).Append('\n');
            }
            _sb.Append("units ready: ").Append(s.UnitQueue.Count);
            var rect = new Rect(14f, 52f, 200f, 24f + 14f * (_sb.ToString().Split('\n').Length));
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width, rect.height), _sb.ToString(), _stock);
        }
    }
}
