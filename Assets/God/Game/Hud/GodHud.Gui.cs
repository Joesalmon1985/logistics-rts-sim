using System.Collections.Generic;
using System.Text;
using UnityEngine;
using God.Sim;

namespace God.Game
{
    /// <summary>The drawing half of the HUD: the cloud strip, the cards, the toast, the help line and the end cards (IMGUI).</summary>
    public sealed partial class GodHud
    {
        private const int WinBuilding = 101, WinEnemy = 102, WinGround = 103, WinEnd = 104;
        private static readonly Ware[] StockWares =
            { Ware.Log, Ware.Clay, Ware.Grain, Ware.Stone, Ware.Ore, Ware.Plank, Ware.Kindling, Ware.Brick, Ware.Iron };
        private static readonly string[] PrioNames = { "Stop", "Low", "Norm", "High" };

        private readonly List<Rect> _ui = new List<Rect>();
        private Rect _buildingRect, _enemyRect, _groundRect, _endRect;
        private GUIStyle _tiny, _pill, _bold;
        private float _scale = 1f;

        /// <summary>False while the start card or the menu holds the game: HUD controls show but take no clicks.</summary>
        private bool HudOn => !(Boot != null && Boot.Menu != null && Boot.Menu.Blocking);

        /// <summary>HUD scale: 1 at 900 px high (and at least 1500 virtual units wide), so a small window still fits the strip.</summary>
        public float Scale => Mathf.Clamp(Mathf.Min(Screen.height / 900f, Screen.width / 1500f), 0.5f, 3f);

        /// <summary>Panel rectangles from the last repaint, in HUD units (top-left origin).</summary>
        public IReadOnlyList<Rect> PanelRects => _ui;

        /// <summary>True when a screen point (pixels, origin bottom-left) is over a HUD panel.</summary>
        public bool IsOverUi(Vector2 screen)
        {
            if (Boot != null && Boot.Menu != null && Boot.Menu.Blocking) return true;
            float s = Scale;
            var p = new Vector2(screen.x / s, (Screen.height - screen.y) / s);
            foreach (var r in _ui) if (r.Contains(p)) return true;
            return false;
        }

        private Vector2 ToHud(Vector3 world)
        {
            var sp = Cam.WorldToScreenPoint(world);
            return new Vector2(sp.x / _scale, (Screen.height - sp.y) / _scale);
        }

        private void OnGUI()
        {
            if (Boot == null) return;
            var st = HudStyle.Get();
            if (_tiny == null)
            {
                _tiny = new GUIStyle(st.Small) { fontSize = 9, wordWrap = false };
                _bold = new GUIStyle(st.Label) { fontStyle = FontStyle.Bold, wordWrap = false };
                _pill = new GUIStyle(st.Window) { padding = new RectOffset(14, 14, 5, 7), border = new RectOffset(24, 24, 24, 24) };
            }
            _scale = Scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(_scale, _scale, 1f));
            // While the start card or the menu holds the game, the HUD shows but takes no clicks (windows keep this state).
            GUI.enabled = HudOn;
            float vw = Screen.width / _scale, vh = Screen.height / _scale;
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) _ui.Clear();

            float stripH = vh * 0.13f;
            DrawStrip(st, vw, stripH, repaint);
            DrawArmy(st, stripH, repaint);
            DrawToastAndHelp(st, vw, vh, stripH, repaint);
            if (Outcome == GameOutcome.Playing) { DrawCards(st, vw, vh, stripH, repaint); DrawTrade(st, vw, vh, stripH, repaint); }
            else DrawEnd(st, vw, vh, repaint);
            DrawTooltip(st, vw, vh);
        }

        // ---------------------------------------------------------------- strip

        private struct Tool { public string Label; public string Tip; public HudMode Mode; public string Kind; public Color Col; public Color Dot; }

        private static readonly List<Tool> Tools = BuildTools();

        private static List<Tool> BuildTools()
        {
            var l = new List<Tool>();
            string[] kinds = { BuildingKinds.Woodcutter, BuildingKinds.ClayPit, BuildingKinds.Quarry, BuildingKinds.Mine, BuildingKinds.Farm,
                BuildingKinds.Sawmill, BuildingKinds.Kiln, BuildingKinds.Smelter, BuildingKinds.Barracks, BuildingKinds.Axesmith };
            string[] labels = { "Woodcutter", "Clay pit", "Quarry", "Mine", "Farm", "Sawmill", "Kiln", "Smelter", "Barracks", "Axesmith" };
            for (int i = 0; i < kinds.Length; i++)
            {
                var d = EconomyDefs.Get(kinds[i]);
                Ware dot = d.Role == BuildingRole.Harvester ? d.HarvestWare : FirstOut(d);
                l.Add(new Tool
                {
                    Label = labels[i], Tip = KindTip(d), Mode = HudMode.Place, Kind = kinds[i],
                    Col = EconomyView.KindColour(kinds[i]), Dot = dot == Ware.None ? Color.white : EconomyView.WareColour(dot),
                });
            }
            var hq = EconomyDefs.Hq;
            l.Add(new Tool { Label = "HQ", Tip = "HQ\nCost: " + CostText(hq) + "\nGrows your land: put it on a legal corner two corners from one of yours.", Mode = HudMode.PlaceHq, Col = new Color(0.86f, 0.70f, 0.36f), Dot = Color.white });
            l.Add(new Tool { Label = "Road", Tip = "Road\nClick one of your flags, then the ground or another flag. Click a flag, or the end again, to finish. Click a road to put a flag on it.", Mode = HudMode.Road, Col = new Color(0.86f, 0.72f, 0.42f), Dot = Color.white });
            l.Add(new Tool { Label = "Demolish", Tip = "Demolish\nClick one of your buildings or roads to remove it. The HQ stays.", Mode = HudMode.Demolish, Col = new Color(0.85f, 0.35f, 0.30f), Dot = Color.white });
            return l;
        }

        private static Ware FirstOut(BuildingDef d)
        {
            for (int w = 1; w < Wares.Count; w++) if (d.Out[w] > 0) return (Ware)w;
            return Ware.None;
        }

        /// <summary>"2 plank, 1 stone".</summary>
        public static string CostText(BuildingDef d) => WareList(d.Cost);

        private static string WareList(int[] counts)
        {
            var sb = new StringBuilder();
            for (int w = 1; w < Wares.Count; w++)
                if (counts[w] > 0) { if (sb.Length > 0) sb.Append(", "); sb.Append(counts[w]).Append(' ').Append(Wares.Name((Ware)w)); }
            return sb.Length == 0 ? "nothing" : sb.ToString();
        }

        /// <summary>The tooltip of a building button: cost, what it takes and what it gives.</summary>
        public static string KindTip(BuildingDef d)
        {
            string takes, gives;
            switch (d.Role)
            {
                case BuildingRole.Harvester: takes = "piles of " + Wares.Name(Wares.OfRaw(d.Harvests)) + " from its " + TerrainWord(d.Harvests); gives = Wares.Name(d.HarvestWare); break;
                case BuildingRole.Factory: takes = "1 bread and a weapon; a shield (not with a bow), helmet and body armour when it has them"; gives = "a soldier in that kit"; break;
                default: takes = WareList(d.In); gives = WareList(d.Out); break;
            }
            return d.Label + "\nCost: " + CostText(d) + "\nTakes: " + takes + "\nGives: " + gives;
        }

        private static string TerrainWord(RawKind k)
        {
            switch (k)
            {
                case RawKind.Log: return "forest";
                case RawKind.Clay: return "hills";
                case RawKind.Grain: return "fields";
                default: return "mountain";
            }
        }

        public static string KindLabel(string kind)
        {
            if (kind == TerritoryTuning.HqKind) return "HQ";
            return EconomyDefs.Get(kind)?.Label ?? "Building";
        }

        private void DrawStrip(HudStyle st, float vw, float stripH, bool repaint)
        {
            var strip = new Rect(0, 0, vw, stripH);
            if (repaint) { GUI.DrawTexture(strip, st.Strip, ScaleMode.StretchToFill, true); _ui.Add(new Rect(0, 0, vw, stripH * 0.78f)); }
            float y = 8f, h = stripH * 0.60f - 6f, w = 64f, x = 18f;
            bool playing = CanAct;
            GUI.enabled = HudOn && (playing);
            // Watch mode: no build tools, no stock of yours; just the clock controls.
            if (Watching)
            {
                GUI.enabled = HudOn;
                GUI.Label(new Rect(x, y + 12f, 190f, 24f), "Watching six AIs play", _bold);
                x += 200f;
                DrawWatchJumps(st, x, y, repaint);
                x += 6 * 40f + 70f;
                DrawClock(st, x, y);
                return;
            }
            foreach (var tool in Tools)
            {
                var r = new Rect(x, y, w, h);
                bool sel = Mode == tool.Mode && PlaceKind == tool.Kind;
                if (GUI.Button(r, new GUIContent(tool.Label, tool.Tip), sel ? st.TileSel : st.Tile)) ToggleTool(tool.Mode, tool.Kind);
                if (repaint)
                {
                    var sw = new Rect(r.x + 9, r.y + 6, r.width - 18, r.height * 0.40f);
                    GUI.DrawTexture(sw, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, tool.Col, Vector4.zero, new Vector4(7, 7, 7, 7));
                    float dd = sw.height * 0.5f;
                    GUI.DrawTexture(new Rect(sw.center.x - dd * 0.5f, sw.center.y - dd * 0.5f, dd, dd), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, tool.Dot, Vector4.zero, new Vector4(dd, dd, dd, dd));
                }
                x += w + 5f;
            }
            x = DrawTradeTile(st, x, y, w, h, repaint);
            GUI.enabled = HudOn;

            // Stock of your wares: small counts with a colour swatch.
            x += 18f;
            var eco = Sim.State.Economy;
            for (int i = 0; i < StockWares.Length; i++)
            {
                float cx = x + (i % 5) * 58f, cy = y + 4f + (i / 5) * (h * 0.5f);
                var ware = StockWares[i];
                if (repaint) GUI.DrawTexture(new Rect(cx, cy + 3f, 11f, 11f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, EconomyView.WareColour(ware), Vector4.zero, new Vector4(4, 4, 4, 4));
                GUI.Label(new Rect(cx + 15f, cy - 3f, 40f, 18f), eco.TotalStock(Me, ware).ToString(), _bold);
                GUI.Label(new Rect(cx + 15f, cy + 11f, 44f, 12f), Wares.Name(ware), _tiny);
            }
            x += 5 * 58f + 22f;
            DrawClock(st, x, y);
        }

        /// <summary>Speed, pause, and the dice tick.</summary>
        private void DrawClock(HudStyle st, float x, float y)
        {
            bool repaint = Event.current.type == EventType.Repaint;
            var pr = new Rect(x, y + 8f, 54f, 30f);
            if (GUI.Button(pr, new GUIContent(Paused ? "Play" : "Pause", "Space"), Paused ? st.BtnSel : st.Btn)) TogglePause();
            x += 58f;
            float[] speeds = Speeds;
            foreach (float sp in speeds)
            {
                bool on = !Paused && Mathf.Approximately(Boot.Speed, sp);
                if (GUI.Button(new Rect(x, y + 8f, 38f, 30f), new GUIContent(sp + "x", "Speed: + and -"), on ? st.BtnSel : st.Btn)) SetSpeed(sp);
                x += 40f;
            }
            x += 12f;
            float k = Mathf.Clamp01(1f - (Time.unscaledTime - _rollTime) / 0.9f);
            float size = 12f + 12f * k;
            if (repaint)
                GUI.DrawTexture(new Rect(x + 12f - size * 0.5f, y + 23f - size * 0.5f, size, size), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f,
                    new Color(1f, 0.80f, 0.25f, 0.45f + 0.55f * k), Vector4.zero, new Vector4(size * 0.3f, size * 0.3f, size * 0.3f, size * 0.3f));
        }

        // ---------------------------------------------------------------- readout, toast, help

        /// <summary>The army line: your soldiers, wagons and guards, and who you are at war with.</summary>
        public string ArmyText()
        {
            var m = Sim.State.Military;
            if (Watching) return "Watch mode: units " + m.Units.Count;
            if (!MeAlive) return "Your faction is out of the game";
            int inf = 0, veh = 0, guards = 0;
            foreach (var u in m.Units) if (u.Faction == Me) { if (u.Kind == UnitKind.Vehicle) veh++; else inf++; }
            foreach (var f in m.Defends) if (f.Faction == Me) guards += f.Guards;
            var foes = Sim.State.Factions.Enemies(Me);
            string war = foes.Count == 0 ? "at peace" : "at war with " + string.Join(", ", foes.ConvertAll(FactionLook.Name));
            return "Infantry " + inf + "    Vehicles " + veh + "    Guards " + guards + "    " + war;
        }

        /// <summary>The standings line: VP per faction, most first (minimal; P10 styles it).</summary>
        public string StandingsText()
        {
            var sb = new StringBuilder();
            foreach (var st in FactionRules.Standings(Sim.State))
            {
                if (sb.Length > 0) sb.Append("   ");
                sb.Append(FactionLook.Name(st.Faction)).Append(' ').Append(st.Eliminated ? "out" : st.VictoryPoints + " VP");
            }
            return sb.ToString();
        }

        private void DrawArmy(HudStyle st, float stripH, bool repaint)
        {
            var c = new GUIContent(ArmyText());
            var size = _bold.CalcSize(c);
            var r = new Rect(14f, stripH + 2f, size.x + 30f, size.y + 12f);
            GUI.Box(r, c, _pill);
            if (repaint) _ui.Add(r);
            DrawStandings(stripH + 2f, repaint);
        }

        // (DrawStandings and the watch-mode jump buttons are in GodHud.Standings.cs)

        private void DrawToastAndHelp(HudStyle st, float vw, float vh, float stripH, bool repaint)
        {
            if (ToastActive)
            {
                float a = Mathf.Clamp01((_toastUntil - Time.unscaledTime) / 0.6f);
                var c = new GUIContent(ToastText);
                float wd = Mathf.Min(560f, st.Label.CalcSize(c).x + 40f);
                var r = new Rect((vw - wd) * 0.5f, stripH + 6f, wd, st.Label.CalcHeight(c, wd - 36f) + 16f);
                var old = GUI.color; GUI.color = new Color(1, 1, 1, a);
                GUI.Box(r, c, _pill);
                GUI.color = old;
            }
            if (HelpVisible && Outcome == GameOutcome.Playing && !Watching)
            {
                const string text = "Pick a building above and click your land. Right-drag pans, the wheel zooms, Space pauses.";
                float a = Mathf.Clamp01((_helpUntil - Time.unscaledTime) / 2f);
                var c = new GUIContent(text);
                float wd = st.Label.CalcSize(c).x + 80f;
                var r = new Rect((vw - wd) * 0.5f, vh - 56f, wd, 38f);
                var old = GUI.color; GUI.color = new Color(1, 1, 1, a);
                GUI.Box(r, "", _pill);
                GUI.Label(new Rect(r.x + 16f, r.y + 8f, wd - 50f, 24f), c, st.Label);
                if (GUI.Button(new Rect(r.xMax - 34f, r.y + 6f, 24f, 24f), "x", st.Btn)) DismissHelp();
                GUI.color = old;
                if (repaint) _ui.Add(r);
            }
        }

        private void DrawTooltip(HudStyle st, float vw, float vh)
        {
            if (string.IsNullOrEmpty(GUI.tooltip)) return;
            var c = new GUIContent(GUI.tooltip);
            const float wd = 270f;
            float ht = st.TipBox.CalcHeight(c, wd);
            var m = Event.current.mousePosition;
            var r = new Rect(Mathf.Min(m.x + 12f, vw - wd - 6f), m.y + 22f, wd, ht);
            if (r.yMax > vh) r.y = m.y - ht - 8f;
            GUI.Box(r, c, st.TipBox);
        }

        // ---------------------------------------------------------------- cards

        private Rect CardRect(Vector3 world, Rect last, float width, float stripH, float vw, float vh)
        {
            var p = ToHud(world);
            float h = last.height > 1f ? last.height : 160f;
            float x = Mathf.Clamp(p.x + 40f, 8f, vw - width - 8f);
            float y = Mathf.Clamp(p.y - h * 0.4f, stripH + 40f, Mathf.Max(stripH + 40f, vh - h - 8f));
            return new Rect(x, y, width, 0f);
        }

        private void DrawCards(HudStyle st, float vw, float vh, float stripH, bool repaint)
        {
            var t = Sim.State.Territory; var g = t.Grid;
            if (SelectedBuilding != 0)
            {
                var b = t.BuildingById(SelectedBuilding);
                if (b != null)
                {
                    var pos = new Vector3((float)g.X(b.Node), 3f, (float)g.Z(b.Node));
                    if (b.Faction == Me && !Watching)
                    {
                        var r = CardRect(pos, _buildingRect, 262f, stripH, vw, vh);
                        _buildingRect = GUILayout.Window(WinBuilding, r, id => BuildingCard(st, b), GUIContent.none, st.Window, GUILayout.Width(262f));
                        if (repaint) _ui.Add(_buildingRect);
                    }
                    else
                    {
                        var r = CardRect(pos, _enemyRect, 230f, stripH, vw, vh);
                        _enemyRect = GUILayout.Window(WinEnemy, r, id => EnemyCard(st, b), GUIContent.none, st.Window, GUILayout.Width(230f));
                        if (repaint) _ui.Add(_enemyRect);
                    }
                }
            }
            else if (SelectedNode >= 0)
            {
                var pos = new Vector3((float)g.X(SelectedNode), 0.5f, (float)g.Z(SelectedNode));
                var r = CardRect(pos, _groundRect, 230f, stripH, vw, vh);
                _groundRect = GUILayout.Window(WinGround, r, id => GroundCard(st), GUIContent.none, st.Window, GUILayout.Width(230f));
                if (repaint) _ui.Add(_groundRect);
            }
        }

        private void CardHead(HudStyle st, string title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, st.Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("x", st.Btn, GUILayout.Width(24f), GUILayout.Height(22f))) ClearSelection();
            GUILayout.EndHorizontal();
        }

        private static void Swatch(Color c, float size = 11f)
        {
            var r = GUILayoutUtility.GetRect(size, 22f, GUILayout.Width(size), GUILayout.Height(22f));
            r.y += (22f - size) * 0.5f; r.height = size;
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, new Vector4(4, 4, 4, 4));
        }

        public static string StatusText(EcoBuilding e)
        {
            if (e.Paused && e.Built) return "Paused";
            switch (e.Status)
            {
                case BuildStatus.SiteWaitingWares: return "Waiting for wares";
                case BuildStatus.SiteNoRoad: return "No road to it";
                case BuildStatus.SiteWaitingBuilder: return "Waiting for a builder";
                case BuildStatus.SiteBuilding: return "Being built";
                case BuildStatus.Idle: return "Idle";
                case BuildStatus.Working: return "Working";
                case BuildStatus.Paused: return "Paused";
                case BuildStatus.MissingInputs: return "Missing inputs";
                case BuildStatus.OutputFull: return "Output full";
                case BuildStatus.NoPiles: return "No goods nearby";
                case BuildStatus.WrongTerrain: return "Wrong land";
                default: return e.Status.ToString();
            }
        }

        private void WareLine(string head, int[] have, int[] cap)
        {
            bool any = false;
            for (int w = 1; w < Wares.Count; w++) if ((cap != null ? cap[w] : have[w]) > 0) any = true;
            if (!any) return;
            GUILayout.BeginHorizontal(GUILayout.Height(22f));
            GUILayout.Label(head, HudStyle.Get().RowSmall, GUILayout.Width(42f), GUILayout.Height(22f));
            for (int w = 1; w < Wares.Count; w++)
            {
                if ((cap != null ? cap[w] : have[w]) <= 0) continue;
                Swatch(EconomyView.WareColour((Ware)w));
                GUILayout.Label(cap != null ? have[w] + "/" + cap[w] : have[w].ToString(), HudStyle.Get().Row, GUILayout.ExpandWidth(false), GUILayout.Height(22f));
                GUILayout.Space(4f);
            }
            GUILayout.EndHorizontal();
        }

        private void BuildingCard(HudStyle st, Building b)
        {
            var e = Sim.State.Economy.Find(b.Id);
            var def = e?.Def;
            string name = KindLabel(b.Kind);
            CardHead(st, e != null && !e.Built && !e.IsHq ? name + " (site)" : name);
            if (e != null && !e.IsHq)
            {
                var old = GUI.contentColor;
                GUILayout.Label(StatusText(e) + (e.Built ? "" : "  " + (Sim.State.Economy.SiteProgressPermille(b.Id) / 10) + "%"), st.Small);
                GUI.contentColor = old;
                if (!e.Built)
                {
                    var bar = GUILayoutUtility.GetRect(10f, 8f, GUILayout.ExpandWidth(true));
                    if (Event.current.type == EventType.Repaint)
                    {
                        GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.27f, 0.19f, 0.09f, 0.18f), Vector4.zero, new Vector4(4, 4, 4, 4));
                        var f = new Rect(bar.x, bar.y, Mathf.Max(8f, bar.width * Sim.State.Economy.SiteProgressPermille(b.Id) / 1000f), bar.height);
                        GUI.DrawTexture(f, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, HudStyle.Good, Vector4.zero, new Vector4(4, 4, 4, 4));
                    }
                    WareLine("Wares", e.In, def.Cost);
                }
                else if (def.Role == BuildingRole.Harvester)
                {
                    var o = new int[Wares.Count]; o[(int)def.HarvestWare] = e.Out[(int)def.HarvestWare]; WareLine("Out", o, null);
                    HarvesterLevelRows(st, e, b.Id);
                }
                else
                {
                    // The barracks lists every piece of gear as an input: show only what it holds.
                    WareLine("In", e.In, def.Role == BuildingRole.Factory ? null : def.In);
                    WareLine("Out", e.Out, null);
                    if (def.Role == BuildingRole.Factory)
                        GUILayout.Label(e.LastKit.IsArmed ? "Last soldier: " + e.LastKit : "No soldier yet: needs 1 bread and a weapon", st.Small);
                }

                // Priority of each input ware: what the site still wants, or what the workshop consumes (not a harvester's upgrade wares).
                var wanted = e.Built && e.Upgrading ? new int[Wares.Count] : e.Wanted;
                for (int w = 1; w < Wares.Count; w++)
                {
                    if (wanted[w] <= 0) continue;
                    GUILayout.BeginHorizontal();
                    Swatch(EconomyView.WareColour((Ware)w));
                    GUILayout.Label(Wares.Name((Ware)w), st.Row, GUILayout.Width(62f), GUILayout.Height(22f));
                    for (int p = 0; p < 4; p++)
                    {
                        bool on = (int)e.Prio[w] == p;
                        if (GUILayout.Button(PrioNames[p], on ? st.BtnSel : st.Btn, GUILayout.Width(38f), GUILayout.Height(22f)) && !on) SetPriority(b.Id, (Ware)w, (Priority)p);
                    }
                    GUILayout.EndHorizontal();
                }
                if (e.Built && GUILayout.Button(e.Paused ? "Paused  (resume)" : "Pause", e.Paused ? st.BtnSel : st.Btn, GUILayout.Height(24f))) TogglePaused(b.Id);
            }
            else
            {
                var hq = Sim.State.Territory.HqOf(b.Id);
                bool site = hq != null && hq.Site;
                GUILayout.Label(site ? "Your new HQ (site)" : hq != null && hq.Citadel ? "Your Citadel (2 VP)" : "Your headquarters (1 VP)", st.Small);
                if (e != null && e.Upgrading) GUILayout.Label("Becoming a Citadel  " + (Sim.State.Economy.UpgradeProgressPermille(b.Id) / 10) + "%", st.Small);
                else if (hq != null && !site && !hq.Citadel && GUILayout.Button(new GUIContent("Upgrade to Citadel", "Cost: " + CostText(EconomyDefs.Citadel) + ". Worth 2 VP, 3000 health."), st.Btn, GUILayout.Height(24f)))
                    UpgradeCitadel(b.Id);
            }

            // Defend flag and guards.
            var flag = Sim.State.Military.DefendAtBuilding(b.Id);
            int cur = flag?.Guards ?? 0;
            GUILayout.Label(flag == null ? "Defend here" : "Defend here  (" + Sim.State.Military.GuardCount(flag.Id) + " on guard)", st.Small);
            GUILayout.BeginHorizontal();
            for (int n = 0; n <= MilitaryTuning.MaxGuards; n++)
                if (GUILayout.Button(n.ToString(), cur == n ? st.BtnSel : st.Btn, GUILayout.Width(34f), GUILayout.Height(22f)) && cur != n) SetGuards(b.Id, n);
            GUILayout.EndHorizontal();

            if (b.Size != BuildingSize.Hq && GUILayout.Button("Demolish", st.Btn, GUILayout.Height(24f))) DemolishBuilding(b.Id);
        }

        /// <summary>Stage 3: a harvester's level, and its upgrade button (with the cost) or the upgrade under way.</summary>
        private void HarvesterLevelRows(HudStyle st, EcoBuilding e, int id)
        {
            int per = EconomyTuning.WaresPerPileAt(e.Level);
            GUILayout.Label("Level " + e.Level + ": " + per + " wares per pile" + (e.Level >= EconomyTuning.MaxHarvesterLevel ? ", never idle" : ""), st.Small);
            var up = e.UpgradeDef;
            if (up == null) return;
            if (e.Upgrading)
            {
                GUILayout.Label("Upgrading to level " + (e.Level + 1) + "  " + (Sim.State.Economy.UpgradeProgressPermille(id) / 10) + "%", st.Small);
                WareLine("Wares", e.In, up.Cost);
            }
            else if (GUILayout.Button(new GUIContent("Upgrade (level " + e.Level + "→" + (e.Level + 1) + "): " + CostText(up),
                         "Level " + (e.Level + 1) + ": " + EconomyTuning.WaresPerPileAt(e.Level + 1) + " wares per pile, faster gathering"
                         + (e.Level + 1 >= EconomyTuning.MaxHarvesterLevel ? ", and 1 ware every 40 s when its hex has no pile" : "") + "."),
                         st.Btn, GUILayout.Height(24f)))
                UpgradeHarvester(id);
        }

        private void EnemyCard(HudStyle st, Building b)
        {
            var m = Sim.State.Military; var fs = Sim.State.Factions;
            var hqOf = Sim.State.Territory.HqOf(b.Id);
            string kind = hqOf != null && hqOf.Citadel ? "Citadel" : KindLabel(b.Kind);
            CardHead(st, FactionLook.Name(b.Faction) + " " + kind);
            var rec = m.BuildingById(b.Id);
            if (rec != null)
            {
                GUILayout.Label("Health " + rec.Health + " / " + rec.MaxHealth, st.Small);
                var bar = GUILayoutUtility.GetRect(10f, 8f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.27f, 0.19f, 0.09f, 0.18f), Vector4.zero, new Vector4(4, 4, 4, 4));
                    GUI.DrawTexture(new Rect(bar.x, bar.y, Mathf.Max(6f, bar.width * rec.HealthFraction), bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, HudStyle.Bad, Vector4.zero, new Vector4(4, 4, 4, 4));
                }
            }
            if (Watching || !MeAlive) { GUILayout.Label(FactionRules.VictoryPoints(Sim.State, b.Faction) + " VP", st.Small); return; }
            bool war = fs.AtWar(Me, b.Faction);
            int order = m.MarkOrder(Me, b.Id);
            if (war)
            {
                GUILayout.Label(order > 0 ? "At war. Target number " + order : "At war. Not a target", order > 0 ? st.Label : st.Small);
                if (GUILayout.Button(order > 0 ? "Untarget" : "Target", order > 0 ? st.BtnSel : st.Btn, GUILayout.Height(26f))) ToggleTarget(b.Id);
                bool offered = fs.HasOfferedPeace(Me, b.Faction), theyOffer = fs.HasOfferedPeace(b.Faction, Me);
                string label = offered ? "Peace offered" : theyOffer ? "Accept their peace" : "Offer peace";
                GUI.enabled = HudOn && (!offered);
                if (GUILayout.Button(label, st.Btn, GUILayout.Height(24f))) OfferPeaceTo(b.Faction);
                GUI.enabled = HudOn;
            }
            else
            {
                GUILayout.Label("At peace: declare war to target it.", st.Small);
                if (GUILayout.Button("Declare war", st.Btn, GUILayout.Height(26f))) DeclareWarOn(b.Faction);
            }
        }

        private void GroundCard(HudStyle st)
        {
            var t = Sim.State.Territory; var m = Sim.State.Military;
            bool road = t.NodeRoad[SelectedNode] != 0;
            CardHead(st, road ? "Road" : "Ground");
            if (Watching || !MeAlive) { GUILayout.Label("Watching", st.Small); return; }
            var flag = m.DefendAtNode(Me, SelectedNode);
            int cur = flag?.Guards ?? 0;
            GUILayout.Label(flag == null ? "Defend here" : "Defend here  (" + m.GuardCount(flag.Id) + " on guard)", st.Small);
            GUILayout.BeginHorizontal();
            for (int n = 0; n <= MilitaryTuning.MaxGuards; n++)
                if (GUILayout.Button(n == 0 ? "Off" : n.ToString(), cur == n ? st.BtnSel : st.Btn, GUILayout.Width(n == 0 ? 38f : 30f), GUILayout.Height(22f)) && cur != n) SetGuardsAt(SelectedNode, n);
            GUILayout.EndHorizontal();
            if (road)
            {
                if (Sim.Check(new PlaceFlag(Me, SelectedNode)).Ok && GUILayout.Button("Place a flag here", st.Btn, GUILayout.Height(24f))) PlaceFlagAt(SelectedNode);
                if (GUILayout.Button("Remove this road", st.Btn, GUILayout.Height(24f))) { RemoveRoadAt(SelectedNode); ClearSelection(); }
            }
        }

        // ---------------------------------------------------------------- win and lose

        public Rect EndCardRect => _endRect;

        private void DrawEnd(HudStyle st, float vw, float vh, bool repaint)
        {
            GUI.DrawTexture(new Rect(0, 0, vw, vh), st.Dim);
            float h = _endRect.height > 1f ? _endRect.height : 300f;
            var r = new Rect((vw - 360f) * 0.5f, Mathf.Max(10f, (vh - h) * 0.5f), 360f, 0f);
            _endRect = GUILayout.Window(WinEnd, r, id => EndCard(st), GUIContent.none, st.Window, GUILayout.Width(360f));
            if (repaint) _ui.Add(new Rect(0, 0, vw, vh));
        }

        /// <summary>The headline of the end card: "Victory", "Defeat" or, in watch mode, who won.</summary>
        public string EndTitle()
        {
            var fs = Sim.State.Factions;
            string winner = fs.Winner >= 0 ? FactionLook.Name((Faction)fs.Winner) : "Nobody";
            return Watching ? winner + " wins" : Outcome == GameOutcome.Won ? "Victory" : "Defeat";
        }

        /// <summary>Why the game ended, in a sentence.</summary>
        public string EndReasonText()
        {
            var fs = Sim.State.Factions;
            string winner = fs.Winner >= 0 ? FactionLook.Name((Faction)fs.Winner) : "Nobody";
            return fs.EndReason == GameEndReason.VictoryPoints ? winner + " reached " + FactionTuning.VictoryPoints + " victory points."
                : fs.Winner >= 0 ? winner + " is the last faction standing." : "Every faction fell.";
        }

        private void EndCard(HudStyle st)
        {
            var fs = Sim.State.Factions;
            GUILayout.Label(EndTitle(), new GUIStyle(st.Title) { fontSize = 26, alignment = TextAnchor.MiddleCenter });
            GUILayout.Label(EndReasonText(), st.Centered);
            GUILayout.Space(6f);
            foreach (var stg in fs.FinalStandings)
            {
                GUILayout.BeginHorizontal();
                var sw = GUILayoutUtility.GetRect(14f, 18f, GUILayout.Width(14f));
                if (Event.current.type == EventType.Repaint)
                    GUI.DrawTexture(new Rect(sw.x, sw.y + 3f, 12f, 12f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, FactionLook.Colour(stg.Faction), Vector4.zero, new Vector4(4, 4, 4, 4));
                string line = stg.Rank + ". " + FactionLook.Name(stg.Faction) + "   " + stg.VictoryPoints + " VP   "
                    + stg.Hqs + (stg.Hqs == 1 ? " HQ" : " HQs") + (stg.Citadels > 0 ? ", " + stg.Citadels + (stg.Citadels == 1 ? " Citadel" : " Citadels") : "") + (stg.Eliminated ? "   out" : "");
                var old = GUI.color; if (stg.Eliminated) GUI.color = new Color(1, 1, 1, 0.5f);
                GUILayout.Label(line, st.Row);
                GUI.color = old;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Watching ? "Watch again" : "Play again", st.BtnSel, GUILayout.Height(34f))) Restart(false);
            if (GUILayout.Button("New island", st.Btn, GUILayout.Height(34f))) Restart(true);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Watching) { if (GUILayout.Button("Play yourself", st.Btn, GUILayout.Height(30f))) GodBootstrap.StartNew(Boot.Seed, false); }
            else if (GUILayout.Button("Watch", st.Btn, GUILayout.Height(30f))) GodBootstrap.StartNew(Boot.Seed, true);
            if (GUILayout.Button("Quit", st.Btn, GUILayout.Height(30f))) Boot.Menu?.Quit();
            GUILayout.EndHorizontal();
        }
    }
}
