using System.Collections.Generic;
using UnityEngine;
using God.Sim;

namespace God.Game
{
    public enum HudMode { Normal, Place, PlaceHq, Road, Demolish }

    /// <summary>
    /// The god HUD and input (P5; six factions in P7), logic half. It acts for the human faction of the game setup
    /// (<see cref="Me"/>); in watch mode (no human) the build tools are off and the cards only inform. The tool the player holds (place a building, place an HQ, lay a road,
    /// demolish), what is selected, and every action as a public method that only sends sim commands (it never changes
    /// sim state). The mouse and keyboard drive these methods from <see cref="Update"/>; the drawing is in GodHud.Gui.cs;
    /// tests call the methods directly.
    /// </summary>
    public sealed partial class GodHud : MonoBehaviour
    {
        public GodBootstrap Boot { get; private set; }
        public GodSim Sim => Boot.Sim;
        public Camera Cam { get; private set; }
        public GodCameraRig Rig { get; private set; }
        public HudOverlay Overlay { get; private set; }

        public HudMode Mode { get; private set; } = HudMode.Normal;
        /// <summary>The building kind being placed (<see cref="BuildingKinds"/>), in <see cref="HudMode.Place"/>.</summary>
        public string PlaceKind { get; private set; }
        /// <summary>Lattice node under the cursor, -1 when none (water, or over a HUD panel).</summary>
        public int HoverNode { get; private set; } = -1;
        /// <summary>Own or enemy building whose card is open, 0 for none.</summary>
        public int SelectedBuilding { get; private set; }
        /// <summary>Ground node whose card is open, -1 for none.</summary>
        public int SelectedNode { get; private set; } = -1;
        /// <summary>The road being laid: nodes from its first flag to its current end.</summary>
        public List<int> RoadNodes { get; } = new List<int>();
        public int RoadEnd => RoadNodes.Count == 0 ? -1 : RoadNodes[RoadNodes.Count - 1];
        public string ToastText { get; private set; }
        public bool HelpVisible { get; private set; } = true;
        /// <summary>Legal spots (or corners) currently marked, for tests.</summary>
        public int LegalSpotCount { get; private set; }

        /// <summary>Tests and screenshots: a cursor position (pixels) used instead of the mouse.</summary>
        public Vector2? DebugCursor;

        public bool Placing => Mode == HudMode.Place || Mode == HudMode.PlaceHq;
        public bool Paused => !Boot.Running;

        /// <summary>The faction the HUD acts for: the human one (F0 stands in, inactive, in watch mode).</summary>
        public Faction Me => FactionLook.Viewer(Sim.State);
        /// <summary>True in watch mode: no human faction, so no build tools and no orders.</summary>
        public bool Watching => !FactionLook.Human(Sim.State).HasValue;
        /// <summary>True while the human faction is still in the game.</summary>
        public bool MeAlive => !Watching && !Sim.State.Factions.IsEliminated(Me);

        /// <summary>Playing until the game ends; then Won if the human faction won, else Lost (always Lost in watch mode).</summary>
        public GameOutcome Outcome => Sim.State.Factions == null ? GameOutcome.Playing
            : Watching ? (Sim.State.IsGameOver ? GameOutcome.Lost : GameOutcome.Playing) : Sim.State.Factions.OutcomeFor(Me);

        /// <summary>True when the build tools and orders may be used: a living human faction and a game still on.</summary>
        public bool CanAct => Outcome == GameOutcome.Playing && MeAlive;

        private float _toastUntil, _helpUntil;
        private float _rollTime = -10f;
        private long _seenRoll;
        private int _spotSig = int.MinValue;
        private string _spotKey;

        public static GodHud Create(GodBootstrap boot, GodCameraRig rig)
        {
            var go = new GameObject("GodHud");
            go.transform.SetParent(boot.transform, false);
            var h = go.AddComponent<GodHud>();
            h.Boot = boot; h.Rig = rig; h.Cam = rig.Cam;
            h.Overlay = HudOverlay.Create(go.transform);
            h._helpUntil = Time.unscaledTime + 14f;
            h._seenRoll = boot.Sim.State.Territory.LastRollTick;
            rig.OverUi = h.IsOverUi;
            if (boot.Territory != null) boot.Territory.ShowDiceTick = false;
            if (boot.Economy != null) boot.Economy.ShowDebugText = false;
            return h;
        }

        // ---------------------------------------------------------------- game loop

        public void TogglePause() { Boot.Running = !Boot.Running; }

        /// <summary>1x, 2x or 4x. Choosing a speed also resumes a paused game.</summary>
        public void SetSpeed(float speed) { Boot.Speed = speed; Boot.Running = true; }

        public void StepSpeed(int dir)
        {
            float[] s = { 1f, 2f, 4f };
            int i = 0;
            for (int k = 0; k < s.Length; k++) if (Boot.Speed >= s[k] - 0.01f) i = k;
            SetSpeed(s[Mathf.Clamp(i + dir, 0, s.Length - 1)]);
        }

        public void Restart(bool newIsland) { Boot.Restart(newIsland); }

        public void DismissHelp() { HelpVisible = false; }

        public void Toast(string text) { ToastText = text; _toastUntil = Time.unscaledTime + 3.2f; }
        public void ClearToast() { ToastText = null; }
        public bool ToastActive => !string.IsNullOrEmpty(ToastText) && Time.unscaledTime < _toastUntil;

        // ---------------------------------------------------------------- tools

        public void BeginPlace(string kind)
        {
            if (!CanAct || EconomyDefs.Get(kind) == null) return;
            ClearSelection(); ClearRoad();
            Mode = HudMode.Place; PlaceKind = kind;
        }

        public void BeginHq()
        {
            if (!CanAct) return;
            ClearSelection(); ClearRoad();
            Mode = HudMode.PlaceHq; PlaceKind = null;
        }

        public void BeginRoad()
        {
            if (!CanAct) return;
            ClearSelection(); ClearRoad();
            Mode = HudMode.Road; PlaceKind = null;
        }

        public void BeginDemolish()
        {
            if (!CanAct) return;
            ClearSelection(); ClearRoad();
            Mode = HudMode.Demolish; PlaceKind = null;
        }

        /// <summary>Clicking the active tool's button again puts it down.</summary>
        public void ToggleTool(HudMode mode, string kind = null)
        {
            if (Mode == mode && PlaceKind == kind) { Cancel(); return; }
            switch (mode)
            {
                case HudMode.Place: BeginPlace(kind); break;
                case HudMode.PlaceHq: BeginHq(); break;
                case HudMode.Road: BeginRoad(); break;
                case HudMode.Demolish: BeginDemolish(); break;
            }
        }

        /// <summary>Esc or right-click: undo the road in progress, else leave the tool, else close the card.</summary>
        public void Cancel()
        {
            if (Mode == HudMode.Road && RoadNodes.Count > 0) { ClearRoad(); return; }
            if (Mode != HudMode.Normal) { Mode = HudMode.Normal; PlaceKind = null; ClearRoad(); return; }
            if (SelectedEnvoy != 0) { ClearEnvoy(); return; }
            if (TradeOpen) { CloseTrade(); return; }
            ClearSelection();
        }

        public void ClearSelection() { SelectedBuilding = 0; SelectedNode = -1; }
        private void ClearRoad() { RoadNodes.Clear(); }

        // ---------------------------------------------------------------- world clicks

        /// <summary>
        /// A left click on the world: the node under the cursor and the building drawn under it (0 for none).
        /// Does what the current tool does. Returns the command result when a command was tried.
        /// </summary>
        public CommandResult WorldClick(int node, int buildingId, bool keepTool = false)
        {
            if (Outcome != GameOutcome.Playing) return CommandResult.Accept();
            if (!CanAct && Mode != HudMode.Normal) { Mode = HudMode.Normal; PlaceKind = null; }
            switch (Mode)
            {
                case HudMode.Place: return node < 0 ? Refuse(Reasons.NotBuildable) : ClickPlace(node, keepTool);
                case HudMode.PlaceHq: return ClickHq(node);
                case HudMode.Road: return node < 0 && buildingId == 0 ? Refuse(Reasons.BadRoad, "A road runs on land.") : ClickRoad(node, buildingId);
                case HudMode.Demolish: return ClickDemolish(node, buildingId);
                default:
                    if (buildingId != 0) SelectBuilding(buildingId);
                    else if (node >= 0) { SelectedBuilding = 0; SelectedNode = node; }
                    else ClearSelection();
                    return CommandResult.Accept();
            }
        }

        public void SelectBuilding(int id) { SelectedBuilding = id; SelectedNode = -1; }

        private CommandResult Refuse(string reason, params object[] args)
        {
            var r = CommandResult.RejectDefault(reason, args);
            Toast(r.Message);
            return r;
        }

        private CommandResult Send(Command c)
        {
            var r = Sim.Submit(c);
            if (!r.Ok) Toast(r.Message);
            return r;
        }

        /// <summary>Places the chosen building kind on a node. The tool is put down after a success unless <paramref name="keepTool"/> (shift).</summary>
        public CommandResult ClickPlace(int node, bool keepTool = false)
        {
            var r = Send(new PlaceBuilding(Me, node, PlaceKind));
            if (r.Ok && !keepTool) { Mode = HudMode.Normal; PlaceKind = null; }
            return r;
        }

        /// <summary>The corner nearest a node that an HQ may stand on (else the nearest corner at all), or -1.</summary>
        public int CornerNear(int node, int maxSteps = 5)
        {
            if (node < 0) return -1;
            var t = Sim.State.Territory; var g = t.Grid; var topo = t.Topology;
            int best = -1, bestD = int.MaxValue; bool bestLegal = false;
            for (int c = 0; c < topo.CornerCount; c++)
            {
                int d = g.Distance(g.CornerNode(c), node);
                if (d > maxSteps) continue;
                bool legal = Sim.Check(new PlaceHq(Me, c)).Ok;
                if ((legal && !bestLegal) || (legal == bestLegal && d < bestD)) { best = c; bestD = d; bestLegal = legal; }
            }
            return best;
        }

        public CommandResult ClickHq(int node)
        {
            int corner = CornerNear(node);
            if (corner < 0) return Refuse(Reasons.NotLandCorner);
            var r = Send(new PlaceHq(Me, corner));
            if (r.Ok) { Mode = HudMode.Normal; PlaceKind = null; }
            return r;
        }

        private int FlagNear(int node)
        {
            if (node < 0) return -1;
            var t = Sim.State.Territory; var g = t.Grid;
            if (t.NodeFlag[node] != 0) return node;
            int best = -1;
            for (int d = 0; d < 6; d++)
            {
                int nb = g.Neighbour(node, d);
                if (nb >= 0 && t.NodeFlag[nb] != 0) { best = nb; break; }
            }
            return best;
        }

        private int RoadNodeNear(int node)
        {
            if (node < 0) return -1;
            var t = Sim.State.Territory; var g = t.Grid;
            if (t.NodeRoad[node] != 0) return node;
            for (int d = 0; d < 6; d++)
            {
                int nb = g.Neighbour(node, d);
                if (nb >= 0 && t.NodeRoad[nb] != 0) return nb;
            }
            return -1;
        }

        /// <summary>The node a road click means: a building's flag, a flag at or beside the cursor, else the node.</summary>
        public int RoadTarget(int node, int buildingId)
        {
            var t = Sim.State.Territory;
            if (buildingId != 0)
            {
                var b = t.BuildingById(buildingId);
                if (b != null && b.Faction == Me) return b.FlagNode;
            }
            int f = FlagNear(node);
            return f >= 0 ? f : node;
        }

        /// <summary>
        /// One click of the road tool. First click: one of your flags (or a road node, which gets a flag). Each next click
        /// extends the road by the shortest way, around the part already laid. A click on a flag finishes the road; so does
        /// a second click on the free end, which then gets a flag.
        /// </summary>
        public CommandResult ClickRoad(int node, int buildingId)
        {
            var t = Sim.State.Territory;
            int target = RoadTarget(node, buildingId);
            if (target < 0) return Refuse(Reasons.BadRoad, "A road runs on land.");

            if (RoadNodes.Count == 0)
            {
                var f = t.FlagAtNode(target);
                if (f != null && f.Faction == Me) { RoadNodes.Add(target); return CommandResult.Accept(); }
                int rn = RoadNodeNear(node);
                if (rn >= 0) return Send(new PlaceFlag(Me, rn));
                return Refuse(Reasons.BadRoad, "A road starts at one of your flags.");
            }

            if (target == RoadEnd) return RoadNodes.Count >= 2 ? FinishRoad() : CommandResult.Accept();
            if (RoadNodes.Contains(target)) return Refuse(Reasons.BadRoad, "A road cannot cross itself.");
            var path = t.FindRoadPath(RoadEnd, target, RoadNodes);
            if (path == null) return Refuse(Reasons.BadRoad, "There is no way there.");
            for (int i = 1; i < path.Length; i++) RoadNodes.Add(path[i]);
            return t.FlagAtNode(target) != null ? FinishRoad() : CommandResult.Accept();
        }

        /// <summary>Sends the road laid so far. A refusal keeps it, so it can be mended with another click.</summary>
        public CommandResult FinishRoad()
        {
            var r = Send(new BuildRoad(Me, RoadNodes.ToArray()));
            if (r.Ok) ClearRoad();
            return r;
        }

        /// <summary>The route the next click would add (for the preview), or null.</summary>
        public int[] RoadPreview(int hoverNode)
        {
            if (RoadNodes.Count == 0 || hoverNode < 0) return null;
            int target = RoadTarget(hoverNode, 0);
            if (target == RoadEnd || RoadNodes.Contains(target)) return null;
            return Sim.State.Territory.FindRoadPath(RoadEnd, target, RoadNodes);
        }

        public CommandResult ClickDemolish(int node, int buildingId)
        {
            var t = Sim.State.Territory;
            if (buildingId != 0) return DemolishBuilding(buildingId);
            var b = node >= 0 ? t.BuildingAtNode(node) : null;
            if (b != null) return DemolishBuilding(b.Id);
            int rn = RoadNodeNear(node);
            if (rn >= 0) return Send(new RemoveRoad(Me, t.NodeRoad[rn]));
            return Refuse(Reasons.NoSuchThing);
        }

        // ---------------------------------------------------------------- card actions

        public CommandResult DemolishBuilding(int id)
        {
            var r = Send(new RemoveBuilding(Me, id));
            if (r.Ok && SelectedBuilding == id) ClearSelection();
            return r;
        }

        public CommandResult SetPriority(int buildingId, Ware ware, Priority p) => Send(new SetInputPriority(Me, buildingId, ware, p));

        public CommandResult TogglePaused(int buildingId)
        {
            var e = Sim.State.Economy.Find(buildingId);
            return Send(new SetPaused(Me, buildingId, e == null || !e.Paused));
        }

        /// <summary>Guards 0..5 on one of your buildings: 0 takes the flag away.</summary>
        public CommandResult SetGuards(int buildingId, int guards)
        {
            var f = Sim.State.Military.DefendAtBuilding(buildingId);
            if (guards <= 0) return f == null ? CommandResult.Accept() : Send(new RemoveDefend(Me, f.Id));
            return Send(SetDefend.OnBuilding(Me, buildingId, guards));
        }

        /// <summary>Guards 0..5 on a bare ground node ("Defend here").</summary>
        public CommandResult SetGuardsAt(int node, int guards)
        {
            var f = Sim.State.Military.DefendAtNode(Me, node);
            if (guards <= 0) return f == null ? CommandResult.Accept() : Send(new RemoveDefend(Me, f.Id));
            return Send(SetDefend.OnNode(Me, node, guards));
        }

        /// <summary>Marks or unmarks a building of another faction. Marking needs war with its owner.</summary>
        public CommandResult ToggleTarget(int buildingId)
        {
            bool marked = Sim.State.Military.IsMarked(Me, buildingId);
            return Send(marked ? (Command)new UnmarkTarget(Me, buildingId) : new MarkTarget(Me, buildingId));
        }

        /// <summary>Declares war on another faction (at once).</summary>
        public CommandResult DeclareWarOn(Faction other) => Send(new DeclareWar(Me, other));

        /// <summary>Offers peace to a faction at war with you; peace follows once they offer too.</summary>
        public CommandResult OfferPeaceTo(Faction other) => Send(new OfferPeace(Me, other));

        /// <summary>Starts the Citadel upgrade of one of your HQs.</summary>
        public CommandResult UpgradeCitadel(int hqId) => Send(new UpgradeToCitadel(Me, hqId));

        public CommandResult PlaceFlagAt(int node) => Send(new PlaceFlag(Me, node));

        public CommandResult RemoveRoadAt(int node)
        {
            var t = Sim.State.Territory;
            if (node < 0 || t.NodeRoad[node] == 0) return Refuse(Reasons.NoSuchThing);
            return Send(new RemoveRoad(Me, t.NodeRoad[node]));
        }

        // ---------------------------------------------------------------- input

        private void Update()
        {
            if (Boot == null) return;
            var mouse = DebugCursor ?? (Vector2)Input.mousePosition;
            if (Time.unscaledTime > _helpUntil) HelpVisible = false;

            if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus)) StepSpeed(1);
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) StepSpeed(-1);
            if (Input.GetKeyDown(KeyCode.Escape)) Cancel();

            bool over = IsOverUi(mouse);
            HoverNode = -1;
            if (!over && Outcome == GameOutcome.Playing && Rig.GroundPoint(mouse, out var gp)) HoverNode = NodeAt(gp);

            if (Outcome == GameOutcome.Playing)
            {
                if (Input.GetMouseButtonDown(0) && !over)
                {
                    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    int b = Mode == HudMode.Place || Mode == HudMode.PlaceHq ? 0 : PickBuilding(mouse);
                    int envoy = Mode == HudMode.Normal ? PickEnvoy(mouse) : 0;
                    if (envoy != 0) SelectEnvoy(envoy);
                    else { ClearEnvoy(); WorldClick(HoverNode, b, shift); }
                }
                if (Input.GetMouseButtonUp(1) && !Rig.Dragged && !over) Cancel();
            }
        }

        /// <summary>Nearest lattice node (land only) to a ground point, or -1.</summary>
        public int NodeAt(Vector3 ground)
        {
            var g = Sim.State.Territory.Grid;
            Lattice.CellOf(ground.x, ground.z, out int i, out int j);
            int n = g.Id(i, j);
            return n >= 0 && g.IsLand(n) ? n : -1;
        }

        /// <summary>The building drawn nearest the cursor (own or enemy), judged on screen, or 0.</summary>
        public int PickBuilding(Vector2 mouse)
        {
            var t = Sim.State.Territory; var g = t.Grid;
            int best = 0; float bestD = float.MaxValue;
            foreach (var b in t.Buildings)
            {
                float h = b.Size == BuildingSize.Hq ? 4f : b.Size == BuildingSize.Medium ? 2.5f : 1.8f;
                var sp = Cam.WorldToScreenPoint(new Vector3((float)g.X(b.Node), h, (float)g.Z(b.Node)));
                if (sp.z <= 0f) continue;
                float rad = Screen.height * (b.Size == BuildingSize.Hq ? 0.05f : b.Size == BuildingSize.Medium ? 0.038f : 0.03f);
                float d = Vector2.Distance(mouse, new Vector2(sp.x, sp.y));
                if (d < rad && d < bestD) { bestD = d; best = b.Id; }
            }
            return best;
        }

        // ---------------------------------------------------------------- frame update of the world markers

        private bool _overlayWasPlacing;

        private void LateUpdate()
        {
            if (Boot == null) return;
            var t = Sim.State.Territory;
            if (t.LastRollTick != _seenRoll) { _seenRoll = t.LastRollTick; _rollTime = Time.unscaledTime; }
            if (SelectedBuilding != 0 && t.BuildingById(SelectedBuilding) == null) SelectedBuilding = 0;
            if (!CanAct && Mode != HudMode.Normal) Cancel();
            // Only on a change, so other callers (tests, dev tools) may show the glow too.
            if (Boot.Territory != null && Placing != _overlayWasPlacing) { _overlayWasPlacing = Placing; Boot.Territory.OverlayVisible = Placing; }
            RefreshSpots(t);
            RefreshTradeLog();
            DrawDynamic(t);
        }

        private float Y(float x, float z) => Rig.GroundY(x, z) + 0.5f;

        private Vector3 NodePos(int node, float lift = 0f)
        {
            var g = Sim.State.Territory.Grid;
            float x = (float)g.X(node), z = (float)g.Z(node);
            return new Vector3(x, Y(x, z) + lift, z);
        }

        private static int Signature(TerritoryState t)
        {
            unchecked
            {
                int h = t.NextId * 31 + t.Hqs.Count * 7 + t.Buildings.Count * 13 + t.Flags.Count * 17 + t.Roads.Count * 19;
                foreach (var hq in t.Hqs) h = h * 31 + (hq.Site ? 1 : 2);
                return h;
            }
        }

        /// <summary>The soft markers on every legal spot, asked of <c>Sim.Check</c> so they match the rules exactly.</summary>
        private void RefreshSpots(TerritoryState t)
        {
            if (!Placing)
            {
                if (_spotKey != null) { _spotKey = null; _spotSig = int.MinValue; LegalSpotCount = 0; Overlay.Spots.Clear(); Overlay.CommitSpots(); }
                return;
            }
            string key = Mode + ":" + PlaceKind;
            int sig = Signature(t);
            if (key == _spotKey && sig == _spotSig) return;
            _spotKey = key; _spotSig = sig;
            var soft = Overlay.Spots; soft.Clear();
            int n = 0;
            if (Mode == HudMode.Place)
            {
                var inner = new Color(1f, 0.93f, 0.62f, 0.85f); var edge = new Color(1f, 0.82f, 0.40f, 0.10f);
                foreach (int node in t.BuildableNodes(Me))
                {
                    if (!Sim.Check(new PlaceBuilding(Me, node, PlaceKind)).Ok) continue;
                    soft.Disc(NodePos(node), 1.6f, inner, edge);
                    n++;
                }
            }
            else
            {
                var inner = new Color(1f, 0.93f, 0.62f, 0.9f); var edge = new Color(1f, 0.82f, 0.40f, 0.12f);
                var g = t.Grid;
                for (int c = 0; c < t.Topology.CornerCount; c++)
                {
                    if (!Sim.Check(new PlaceHq(Me, c)).Ok) continue;
                    var p = NodePos(g.CornerNode(c));
                    soft.Disc(p, 4.5f, inner, edge, 20);
                    soft.Ring(p, 4.6f, 0.5f, new Color(1f, 0.78f, 0.30f, 0.8f), 24);
                    n++;
                }
            }
            LegalSpotCount = n;
            Overlay.CommitSpots();
        }

        private void DrawDynamic(TerritoryState t)
        {
            var d = Overlay.Dynamic; d.Clear();
            var g = t.Grid;
            var gold = new Color(1f, 0.82f, 0.36f, 0.85f);
            var green = new Color(0.45f, 0.85f, 0.40f, 0.8f);
            var red = new Color(0.92f, 0.38f, 0.30f, 0.8f);

            // The open card's subject.
            if (SelectedBuilding != 0)
            {
                var b = t.BuildingById(SelectedBuilding);
                if (b != null) d.Ring(NodePos(b.Node), b.Size == BuildingSize.Hq ? 7.5f : b.Size == BuildingSize.Medium ? 5.2f : 3.6f, 0.6f, gold);
            }
            if (SelectedNode >= 0) d.Ring(NodePos(SelectedNode), 2.4f, 0.5f, gold);

            int hover = HoverNode;
            if (hover >= 0)
            {
                switch (Mode)
                {
                    case HudMode.Place:
                        {
                            var def = EconomyDefs.Get(PlaceKind);
                            bool ok = Sim.Check(new PlaceBuilding(Me, hover, PlaceKind)).Ok;
                            float s = def.Size == BuildingSize.Medium ? 6.5f : 3.6f, hgt = def.Size == BuildingSize.Medium ? 4.4f : 3.2f;
                            var col = Color.Lerp(EconomyView.KindColour(PlaceKind), ok ? green : red, 0.65f); col.a = 0.7f;
                            d.Box(NodePos(hover, hgt * 0.5f), new Vector3(s, hgt, s), col);
                            int fn = g.FlagNodeOf(hover);
                            if (fn >= 0) d.Disc(NodePos(fn), 1.3f, ok ? green : red, new Color(1, 1, 1, 0.1f));
                            break;
                        }
                    case HudMode.PlaceHq:
                        {
                            int c = CornerNear(hover);
                            if (c >= 0)
                            {
                                bool ok = Sim.Check(new PlaceHq(Me, c)).Ok;
                                var p = NodePos(g.CornerNode(c));
                                var col = Color.Lerp(FactionLook.Colour(Me), ok ? green : red, 0.5f); col.a = 0.6f;
                                d.Box(p + Vector3.up * 3f, new Vector3(9f, 6f, 9f), col);
                                if (ok) d.Ring(p, (float)g.CircleRadius, 0.8f, new Color(1f, 0.82f, 0.36f, 0.7f), 60);
                            }
                            break;
                        }
                    case HudMode.Road:
                        {
                            int target = RoadTarget(hover, 0);
                            d.Ring(NodePos(target), 2.0f, 0.5f, gold, 16);
                            break;
                        }
                    default:
                        d.Ring(NodePos(hover), 2.2f, 0.45f, new Color(1f, 0.88f, 0.5f, 0.6f), 20);
                        break;
                }
            }

            // The road being laid: gold for what is laid so far, then the route the next click adds.
            if (Mode == HudMode.Road && RoadNodes.Count > 0)
            {
                for (int i = 1; i < RoadNodes.Count; i++) d.Line(NodePos(RoadNodes[i - 1]), NodePos(RoadNodes[i]), 1.3f, gold);
                d.Disc(NodePos(RoadNodes[0]), 2f, gold, new Color(1, 1, 1, 0.1f));
                var pre = RoadPreview(hover);
                if (pre != null)
                {
                    var full = new List<int>(RoadNodes);
                    for (int i = 1; i < pre.Length; i++) full.Add(pre[i]);
                    bool ok = Sim.Check(new BuildRoad(Me, full.ToArray())).Ok;
                    var col = ok ? green : new Color(0.95f, 0.62f, 0.45f, 0.75f);
                    for (int i = 1; i < pre.Length; i++) d.Line(NodePos(pre[i - 1]), NodePos(pre[i]), 1.1f, col);
                }
            }
            Overlay.CommitDynamic();
        }
    }
}
