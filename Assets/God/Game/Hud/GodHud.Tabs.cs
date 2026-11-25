using System.Collections.Generic;
using UnityEngine;
using God.Sim;

namespace God.Game
{
    /// <summary>The tabs of the build bar.</summary>
    public enum BuildTab { Gather = 0, Process = 1, Arms = 2, Army = 3, Land = 4 }

    /// <summary>One button of the build bar.</summary>
    public struct BuildTool
    {
        public string Label; public string Key; public HudMode Mode; public string Kind; public BuildTab Tab;
        public Color Col; public Color Dot;
    }

    /// <summary>
    /// The tabbed build bar and the grouped stock bar (stage 3). Tabs: Gather (the five harvesters), Process (ten processors),
    /// Arms (ten armouries), Army (the barracks) and Land (HQ, road, demolish, trade). A kind whose def does not exist yet
    /// shows a disabled button with a "coming" tooltip and starts working as soon as the def appears. Keys: Tab and
    /// Shift+Tab cycle the tabs, 1 to 5 pick one (watch mode keeps 1 to 6 and Tab for its own jumps).
    /// </summary>
    public sealed partial class GodHud
    {
        public static readonly BuildTab[] AllTabs = { BuildTab.Gather, BuildTab.Process, BuildTab.Arms, BuildTab.Army, BuildTab.Land };
        public static string TabName(BuildTab t) => t.ToString();

        /// <summary>The tab showing now.</summary>
        public BuildTab Tab { get; private set; } = BuildTab.Gather;

        /// <summary>The wares of the stock bar's three groups: raws, processed goods and gear.</summary>
        public static readonly Ware[] StockRaws = Wares.Raws;
        public static readonly Ware[] StockProcessed = Wares.Processed;
        public static readonly Ware[] StockGear = BuildGear();

        private static Ware[] BuildGear()
        {
            var l = new List<Ware>(Gear.Attack); l.AddRange(Gear.Defence);
            return l.ToArray();
        }

        private static readonly Dictionary<string, string> ToolLabels = new Dictionary<string, string>
        {
            { BuildingKinds.Woodcutter, "Wood-\ncutter" }, { BuildingKinds.ClayPit, "Clay pit" }, { BuildingKinds.Farm, "Farm" },
            { BuildingKinds.Quarry, "Quarry" }, { BuildingKinds.Mine, "Mine" },
            { BuildingKinds.Sawmill, "Sawmill" }, { BuildingKinds.Kiln, "Brick\nkiln" }, { BuildingKinds.Smelter, "Smelter" },
            { BuildingKinds.Bakery, "Bakery" }, { BuildingKinds.MortarMill, "Mortar\nmill" }, { BuildingKinds.Foundry, "Bronze\nfoundry" },
            { BuildingKinds.Pottery, "Pottery" }, { BuildingKinds.Forge, "Steel\nforge" }, { BuildingKinds.Tannery, "Tannery" },
            { BuildingKinds.Weaver, "Weaver" },
            { BuildingKinds.Swordsmith, "Swords-\nmith" }, { BuildingKinds.Spearmaker, "Spear-\nmaker" }, { BuildingKinds.Axesmith, "Axesmith" },
            { BuildingKinds.Bowyer, "Bowyer" }, { BuildingKinds.Slingmaker, "Sling-\nmaker" }, { BuildingKinds.Shieldwright, "Shield-\nwright" },
            { BuildingKinds.Helmsmith, "Helm-\nsmith" }, { BuildingKinds.Bucklersmith, "Buckler-\nsmith" }, { BuildingKinds.Armourer, "Armourer" },
            { BuildingKinds.Leatherworker, "Leather-\nworker" }, { BuildingKinds.Barracks, "Barracks" },
        };

        private static readonly List<BuildTool> AllTools = BuildAllTools();

        private static List<BuildTool> BuildAllTools()
        {
            var l = new List<BuildTool>();
            void Group(string[] kinds, BuildTab tab)
            {
                foreach (var k in kinds)
                    l.Add(new BuildTool { Label = ToolLabels.TryGetValue(k, out var s) ? s : k, Key = k, Mode = HudMode.Place, Kind = k, Tab = tab, Col = EconomyView.KindColour(k), Dot = EconomyView.KindDot(k) });
            }
            Group(BuildingKinds.Harvesters, BuildTab.Gather);
            Group(BuildingKinds.Processors, BuildTab.Process);
            Group(BuildingKinds.Armouries, BuildTab.Arms);
            Group(new[] { BuildingKinds.Barracks }, BuildTab.Army);
            l.Add(new BuildTool { Label = "HQ", Key = "hq", Mode = HudMode.PlaceHq, Tab = BuildTab.Land, Col = new Color(0.86f, 0.70f, 0.36f), Dot = Color.white });
            l.Add(new BuildTool { Label = "Road", Key = "road", Mode = HudMode.Road, Tab = BuildTab.Land, Col = new Color(0.86f, 0.72f, 0.42f), Dot = Color.white });
            l.Add(new BuildTool { Label = "Demolish", Key = "demolish", Mode = HudMode.Demolish, Tab = BuildTab.Land, Col = new Color(0.85f, 0.35f, 0.30f), Dot = Color.white });
            l.Add(new BuildTool { Label = "Trade", Key = "trade", Mode = HudMode.Normal, Tab = BuildTab.Land, Col = new Color(0.92f, 0.78f, 0.34f), Dot = new Color(0.55f, 0.36f, 0.18f) });
            return l;
        }

        /// <summary>Every button of the build bar, in order.</summary>
        public static IReadOnlyList<BuildTool> BuildTools => AllTools;

        /// <summary>The buttons of one tab.</summary>
        public static List<BuildTool> ToolsOf(BuildTab tab)
        {
            var l = new List<BuildTool>();
            foreach (var t in AllTools) if (t.Tab == tab) l.Add(t);
            return l;
        }

        /// <summary>The tab a button (building kind, or "hq", "road", "demolish", "trade") lives on.</summary>
        public static BuildTab TabOf(string key)
        {
            foreach (var t in AllTools) if (t.Key == key) return t.Tab;
            return BuildTab.Gather;
        }

        public void SetTab(BuildTab tab) { Tab = tab; }

        public void CycleTab(int dir)
        {
            int n = AllTabs.Length;
            Tab = AllTabs[((int)Tab + dir % n + n) % n];
        }

        /// <summary>True when a button can be used: its building def exists (always for the Land tools).</summary>
        public static bool ToolAvailable(BuildTool t) => t.Mode != HudMode.Place || EconomyDefs.Get(t.Kind) != null;

        /// <summary>The tooltip of a button: cost and recipe from the def, or a "coming" note while the def is missing.</summary>
        public static string ToolTip(BuildTool t)
        {
            switch (t.Key)
            {
                case "hq": return "HQ\nCost: " + CostText(EconomyDefs.Hq) + "\nGrows your land: put it on a legal corner two corners from one of yours.";
                case "road": return "Road\nClick one of your flags, then the ground or another flag. Click a flag, or the end again, to finish. Click a road to put a flag on it.";
                case "demolish": return "Demolish\nClick one of your buildings or roads to remove it. The HQ stays.";
                case "trade": return "Trade\nSend an envoy with goods to another faction. If they accept, the goods are exchanged. Envoys can be killed by enemy soldiers.";
            }
            var d = EconomyDefs.Get(t.Kind);
            if (d == null) return t.Label.Replace("-\n", "").Replace("\n", " ") + "\nComing soon: this building is not in the game yet.";
            return KindTip(d);
        }

        /// <summary>Tab and number keys. Not in watch mode, which uses 1 to 6 and Tab to jump about.</summary>
        private void HandleTabKeys()
        {
            if (Watching || !CanAct) return;
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                CycleTab(shift ? -1 : 1);
            }
            for (int i = 0; i < AllTabs.Length; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) SetTab(AllTabs[i]);
        }

        // ---------------------------------------------------------------- drawing

        private string _manualTip;
        private GUIStyle _tile10, _tile10Sel, _tab, _tabSel;
        private const float ToolW = 62f, ToolGap = 4f, ToolsLeft = 18f;

        /// <summary>Where the strip's stock bar starts: after the widest tab (ten buttons), so nothing shifts when the tab changes.</summary>
        private static float StockLeft => ToolsLeft + 10 * (ToolW + ToolGap) + 18f;

        private void MakeTabStyles(HudStyle st)
        {
            if (_tile10 != null) return;
            _tile10 = new GUIStyle(st.Tile) { fontSize = 10, wordWrap = false, padding = new RectOffset(1, 1, 2, 3) };
            _tile10Sel = new GUIStyle(st.TileSel) { fontSize = 10, wordWrap = false, padding = new RectOffset(1, 1, 2, 3) };
            _tab = new GUIStyle(st.Btn) { fontSize = 11, padding = new RectOffset(2, 2, 1, 2) };
            _tabSel = new GUIStyle(st.BtnSel) { fontSize = 11, fontStyle = FontStyle.Bold, padding = new RectOffset(2, 2, 1, 2) };
        }

        /// <summary>Tab headers and the buttons of the current tab. Returns nothing; positions are fixed.</summary>
        private void DrawBuildBar(HudStyle st, float y, float h, bool repaint)
        {
            MakeTabStyles(st);
            _manualTip = null;
            bool act = CanAct;
            float x = ToolsLeft;
            const float tabH = 19f;
            for (int i = 0; i < AllTabs.Length; i++)
            {
                var tab = AllTabs[i];
                var r = new Rect(x, y - 2f, 74f, tabH);
                if (GUI.Button(r, new GUIContent(TabName(tab), (i + 1) + ": " + TabName(tab) + " (Tab cycles)"), tab == Tab ? _tabSel : _tab)) SetTab(tab);
                x += 78f;
            }
            float ty = y + tabH + 1f, th = h - tabH - 1f;
            x = ToolsLeft;
            foreach (var tool in AllTools)
            {
                if (tool.Tab != Tab) continue;
                var r = new Rect(x, ty, ToolW, th);
                bool avail = ToolAvailable(tool);
                bool sel = tool.Key == "trade" ? TradeOpen : Mode == tool.Mode && PlaceKind == tool.Kind;
                string tip = ToolTip(tool);
                GUI.enabled = HudOn && act && avail;
                bool clicked = GUI.Button(r, new GUIContent(tool.Label, tip), sel ? _tile10Sel : _tile10);
                GUI.enabled = HudOn;
                if (!avail && r.Contains(Event.current.mousePosition)) _manualTip = tip;
                if (clicked)
                {
                    if (tool.Key == "trade") ToggleTrade(); else ToggleTool(tool.Mode, tool.Kind);
                }
                if (repaint)
                {
                    var tint = avail ? Color.white : new Color(1, 1, 1, 0.4f);
                    var sw = new Rect(r.x + 9, r.y + 5, r.width - 18, r.height * 0.34f);
                    GUI.DrawTexture(sw, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, tool.Col * tint, Vector4.zero, new Vector4(7, 7, 7, 7));
                    float dd = sw.height * 0.5f;
                    GUI.DrawTexture(new Rect(sw.center.x - dd * 0.5f, sw.center.y - dd * 0.5f, dd, dd), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, tool.Dot * tint, Vector4.zero, new Vector4(dd, dd, dd, dd));
                    if (tool.Key == "trade" && PendingOffers().Count > 0)
                        GUI.DrawTexture(new Rect(r.xMax - 14f, r.y - 3f, 16f, 16f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, HudStyle.Bad, Vector4.zero, new Vector4(8, 8, 8, 8));
                    DrawTutorialButtonGlow(tool.Key, r, tab: false);
                }
                x += ToolW + ToolGap;
            }
            if (repaint)
                for (int i = 0; i < AllTabs.Length; i++)
                    if (TutorialPointsAtTab(AllTabs[i])) DrawTutorialButtonGlow(null, new Rect(ToolsLeft + i * 78f, y - 2f, 74f, tabH), tab: true);
        }

        /// <summary>The stock bar: raws, processed goods and gear in three compact groups, a swatch and a count each (the name is in the tooltip).</summary>
        private void DrawStockBar(float x, float y, float h, bool repaint)
        {
            var eco = Sim.State.Economy;
            float cell = 33f, rowH = Mathf.Max(20f, (h + 14f) * 0.5f);
            x = DrawStockGroup(eco, StockRaws, x, y, 3, cell, rowH, repaint) + 12f;
            x = DrawStockGroup(eco, StockProcessed, x, y, 5, cell, rowH, repaint) + 12f;
            DrawStockGroup(eco, StockGear, x, y, 5, cell, rowH, repaint);
        }

        /// <summary>Width of the stock bar (for the layout of what follows).</summary>
        private static float StockBarWidth => (3 + 5 + 5) * 33f + 24f;

        private float DrawStockGroup(EconomyState eco, Ware[] wares, float x, float y, int cols, float cell, float rowH, bool repaint)
        {
            for (int i = 0; i < wares.Length; i++)
            {
                float cx = x + (i % cols) * cell, cy = y + (i / cols) * rowH;
                var w = wares[i];
                int n = eco.TotalStock(Me, w);
                var cr = new Rect(cx, cy, cell, rowH);
                if (repaint)
                {
                    var col = EconomyView.WareColour(w);
                    GUI.DrawTexture(new Rect(cx + 1f, cy + 4f, 10f, 10f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, n > 0 ? col : col * new Color(1, 1, 1, 0.45f), Vector4.zero, new Vector4(4, 4, 4, 4));
                }
                GUI.Label(new Rect(cx + 12f, cy, cell - 12f, 20f), n.ToString(), n > 0 ? _bold : _tiny);
                GUI.Label(cr, new GUIContent("", Wares.Name(w) + ": " + n));
            }
            return x + cols * cell;
        }
    }
}
