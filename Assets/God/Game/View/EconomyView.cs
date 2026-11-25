using System.Text;
using UnityEngine;
using God.Sim;

namespace God.Game
{
    /// <summary>
    /// The economy view. Since P4b the world is drawn by models: <see cref="BuildingLayer"/> (every building and site,
    /// working state, damage, collapse and rubble) and <see cref="WalkerLayer"/> (carriers, builders and harvesters).
    /// This class keeps the debug readouts: a tiny text list of the HQ stock, and per-building kind and status labels
    /// when <see cref="ShowLabels"/> is on. It only reads <see cref="EconomyState"/>; it never mutates the sim.
    /// </summary>
    public sealed class EconomyView : MonoBehaviour
    {
        private GodSim _sim;
        private BuildingLayer _buildings;
        private WalkerLayer _walkers;
        private EnvoyLayer _envoys;
        private GUIStyle _label, _stock;
        private readonly StringBuilder _sb = new StringBuilder();

        /// <summary>Debug: kind, progress and status labels over every building. Off by default.</summary>
        public bool ShowLabels;
        /// <summary>Debug: the HQ stock list in the top-left corner.</summary>
        public bool ShowStock = true;

        /// <summary>The HUD's switch (P5) for this view's debug text: the stock list and the labels. Its strip and cards say it better.</summary>
        public bool ShowDebugText
        {
            get => ShowStock || ShowLabels;
            set { ShowStock = value; if (!value) ShowLabels = false; }
        }

        public GodSim Sim => _sim;
        public BuildingLayer Buildings => _buildings;
        public WalkerLayer Walkers => _walkers;
        /// <summary>The trade envoys (P8): a herald leading a pack donkey.</summary>
        public EnvoyLayer Envoys => _envoys;
        public int WalkerCount => _walkers.Count;
        public int BuildingMarkerCount => _buildings.Count;

        public static EconomyView Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("EconomyView");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<EconomyView>();
            v._sim = sim;
            v._buildings = BuildingLayer.Create(sim, go.transform);
            v._walkers = WalkerLayer.Create(sim, go.transform);
            v._envoys = EnvoyLayer.Create(sim, go.transform);
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
                // processed, stage 3: earthy for food and building goods, cool for metals, soft for cloth
                case Ware.Bread: return new Color(0.74f, 0.46f, 0.22f);
                case Ware.Mortar: return new Color(0.66f, 0.64f, 0.55f);
                case Ware.Bronze: return new Color(0.80f, 0.55f, 0.22f);
                case Ware.Pottery: return new Color(0.90f, 0.62f, 0.50f);
                case Ware.Steel: return new Color(0.60f, 0.68f, 0.78f);
                case Ware.Leather: return new Color(0.55f, 0.32f, 0.20f);
                case Ware.Cloth: return new Color(0.55f, 0.75f, 0.78f);
                // gear: attack in warm and green tones, defence in cool blues and greys
                case Ware.Sword: return new Color(0.82f, 0.84f, 0.90f);
                case Ware.Spear: return new Color(0.62f, 0.72f, 0.45f);
                case Ware.Axe: return new Color(0.80f, 0.35f, 0.28f);
                case Ware.Bow: return new Color(0.66f, 0.48f, 0.26f);
                case Ware.Sling: return new Color(0.78f, 0.68f, 0.45f);
                case Ware.BigShield: return new Color(0.35f, 0.50f, 0.80f);
                case Ware.Helmet: return new Color(0.55f, 0.58f, 0.64f);
                case Ware.SmallShield: return new Color(0.30f, 0.65f, 0.70f);
                case Ware.Breastplate: return new Color(0.45f, 0.45f, 0.62f);
                case Ware.LightArmour: return new Color(0.62f, 0.54f, 0.40f);
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
                default:
                    {
                        // The new processors and armouries take the colour of what they make, a little darker.
                        var w = KindOutput(kind);
                        return w == Ware.None ? Color.white : Color.Lerp(WareColour(w), new Color(0.25f, 0.2f, 0.15f), 0.18f);
                    }
            }
        }

        /// <summary>The ware a building kind is known for (what its button shows as a dot), by kind alone so it works before defs exist.</summary>
        public static Ware KindOutput(string kind)
        {
            switch (kind)
            {
                case BuildingKinds.Woodcutter: return Ware.Log;
                case BuildingKinds.ClayPit: return Ware.Clay;
                case BuildingKinds.Farm: return Ware.Grain;
                case BuildingKinds.Quarry: return Ware.Stone;
                case BuildingKinds.Mine: return Ware.Ore;
                case BuildingKinds.Sawmill: return Ware.Plank;
                case BuildingKinds.Kiln: return Ware.Brick;
                case BuildingKinds.Smelter: return Ware.Iron;
                case BuildingKinds.Bakery: return Ware.Bread;
                case BuildingKinds.MortarMill: return Ware.Mortar;
                case BuildingKinds.Foundry: return Ware.Bronze;
                case BuildingKinds.Pottery: return Ware.Pottery;
                case BuildingKinds.Forge: return Ware.Steel;
                case BuildingKinds.Tannery: return Ware.Leather;
                case BuildingKinds.Weaver: return Ware.Cloth;
                case BuildingKinds.Swordsmith: return Ware.Sword;
                case BuildingKinds.Spearmaker: return Ware.Spear;
                case BuildingKinds.Axesmith: return Ware.Axe;
                case BuildingKinds.Bowyer: return Ware.Bow;
                case BuildingKinds.Slingmaker: return Ware.Sling;
                case BuildingKinds.Shieldwright: return Ware.BigShield;
                case BuildingKinds.Helmsmith: return Ware.Helmet;
                case BuildingKinds.Bucklersmith: return Ware.SmallShield;
                case BuildingKinds.Armourer: return Ware.Breastplate;
                case BuildingKinds.Leatherworker: return Ware.LightArmour;
                default: return Ware.None;
            }
        }

        public static Color KindDot(string kind)
        {
            var w = KindOutput(kind);
            return w == Ware.None ? Color.white : WareColour(w);
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

        private static Vector3 P(NodeGrid g, int node, float y) => new Vector3((float)g.X(node), y, (float)g.Z(node));

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
            if (cam != null && ShowLabels)
            {
                var g = s.Territory.Grid;
                foreach (var b in e.Buildings)
                {
                    if (b.IsHq && b.Built) continue;
                    var sp = cam.WorldToScreenPoint(P(g, b.Node, 9f + ModelKit.GroundY((float)g.X(b.Node), (float)g.Z(b.Node))));
                    if (sp.z < 0) continue;
                    string text = b.Built ? b.Def.Label + "\n" + b.Status
                        : b.Def.Label + " " + (e.SiteProgressPermille(b.Id) / 10) + "%\n" + b.Status;
                    GUI.Label(new Rect(sp.x - 60f, Screen.height - sp.y - 14f, 120f, 28f), text, _label);
                }
            }
            if (!ShowStock) return;
            _sb.Length = 0;
            foreach (var hq in e.HqsOf(FactionLook.Viewer(s)))
            {
                _sb.Append("HQ ").Append(hq.Id).Append("  carriers ").Append(hq.IdleCarriers).Append('/').Append(EconomyTuning.CarriersPerHq).Append('\n');
                for (int w = 1; w < Wares.Count; w++)
                    if (hq.Stock[w] > 0 && (Ware)w != Ware.Rock) _sb.Append("  ").Append(Wares.Name((Ware)w)).Append(' ').Append(hq.Stock[w]).Append('\n');
            }
            _sb.Append("units made: ").Append(s.Economy.ProducedLog.Count);
            var rect = new Rect(14f, 52f, 200f, 24f + 14f * (_sb.ToString().Split('\n').Length));
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width, rect.height), _sb.ToString(), _stock);
        }
    }
}
