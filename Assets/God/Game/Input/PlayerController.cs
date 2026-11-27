using System.Collections.Generic;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Turns device frames into <see cref="PlayerIntent"/> and applies each intent to the camera director, the selection, the
    /// time controls and the command builder (CC-1). It runs in god-only mode (the stage 3 faction mode keeps its old input
    /// paths). It reads the world only through the HUD's view-side pickers and the NPC layer's drawn positions, and sends
    /// commands only through <see cref="PlayerCommandBuilder"/>; it never mutates the sim.
    /// <para>Controls: wheel zoom to the cursor, right-drag grab and pan, middle-drag orbit, Alt+wheel tilt, WASD or arrows pan,
    /// Q/E rotate, R/T or PgUp/PgDn tilt, Home whole island, Z zoom to the person, F follow, H household (or home), left click
    /// select, Shift or Ctrl click toggle, left drag box select, Alt+left drag area brush (wheel sets the radius), double-click a
    /// person to focus and select their household, Tab roster, Space pause, + and - speed, . step, ] accelerated, Esc.</para>
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        public GodBootstrap Boot { get; private set; }
        public GodHud Hud => Boot.Hud;
        public CameraDirector Director { get; private set; }
        public SelectionModel Selection { get; private set; }
        public UiExtra Ui { get; private set; }
        public PlayerCommandBuilder Builder { get; private set; }
        public IInputSource Source;
        public ControlTuning Tuning => Director.Tuning;

        /// <summary>False: nothing is read from the source in Update (tests call <see cref="Step"/> themselves).</summary>
        public bool InputEnabled = true;
        /// <summary>The area brush radius in metres (wheel while the brush is down).</summary>
        public float BrushMetres = 20f;
        /// <summary>Raised on a right click that was not a drag: where U2's radial menu opens.</summary>
        public event System.Action<Vector2> SecondaryClicked;
        /// <summary>Raised when the player chose something to act on, for U2's popover (kind and screen point).</summary>
        public event System.Action<PointerGesture, Vector2> Gestured;

        // pointer tracking
        private bool _leftHeld, _leftDragged, _brush, _rightHeld, _rightDragged, _middleHeld;
        private Vector2 _leftStart, _rightStart, _lastPointer;
        private float _clock, _lastClickTime = -10f;
        private Vector2 _lastClickPos;
        private int _lastClickPerson;
        private int _rosterIndex = -1;
        private readonly List<int> _ids = new List<int>();

        public static PlayerController Create(GodBootstrap boot, CameraDirector director, SelectionModel selection, UiExtra ui)
        {
            var go = new GameObject("PlayerController");
            go.transform.SetParent(boot.transform, false);
            var p = go.AddComponent<PlayerController>();
            p.Boot = boot; p.Director = director; p.Selection = selection; p.Ui = ui;
            p.BrushMetres = director.Tuning.BrushDefaultMetres;
            p.Source = new UnityInputSource { TextFocus = () => p.Boot.Hud != null && p.Boot.Hud.TypingText };
            p.Builder = new PlayerCommandBuilder(() => boot.HumanGod, boot.Gate);
            director.Follower = id => boot.People != null ? new NpcFollowTarget(boot.People, id) : null;
            selection.Changed += p.OnSelectionChanged;
            ui.CaptureCamera = () => director.Pose;
            ui.CaptureSelection = () => selection;
            ui.Loaded += p.ApplyLoadedUi;
            return p;
        }

        private void OnDestroy()
        {
            if (Selection != null) Selection.Changed -= OnSelectionChanged;
            if (Ui != null) Ui.Loaded -= ApplyLoadedUi;
        }

        /// <summary>After a load: the saved camera pose and selection come back (the sim hash is untouched by either).</summary>
        public void ApplyLoadedUi()
        {
            var s = Ui.State;
            if (s.Camera.HasValue && Director.Driving) Director.RestorePose(s.Camera.Value);
            UiExtra.Restore(s.Selection, Selection);
        }

        private bool Active => Boot != null && Boot.GodOnly && Boot.Hud != null && (Boot.Menu == null || !Boot.Menu.Blocking);

        private void Update()
        {
            if (!InputEnabled || !Active || Source == null) return;
            Step(Time.unscaledDeltaTime);
        }

        /// <summary>One frame: poll the device, interpret, apply. Tests call it with a <see cref="ScriptedInput"/>.</summary>
        public void Step(float dt)
        {
            var frame = Source.Poll(dt);
            Apply(Interpret(frame));
        }

        // ---------------------------------------------------------------- frame -> intent

        public PlayerIntent Interpret(PlayerInputFrame f)
        {
            _clock += f.DeltaTime;
            var i = new PlayerIntent { Dt = f.DeltaTime, Pan = f.Move, Rotate = f.Rotate, Tilt = f.Tilt };
            bool over = Hud.IsOverUi(f.Pointer);
            float slop = Tuning.DragSlopPixels;
            i.Additive = f.Shift || f.Ctrl;

            // wheel: brush radius while painting, Alt tilts, otherwise zoom (not over a panel)
            if (!over && Mathf.Abs(f.Wheel) > 0.01f)
            {
                if (_brush) i.BrushWheelTicks = f.Wheel;
                else if (f.Alt) i.TiltWheelTicks = f.Wheel;
                else { i.ZoomTicks = f.Wheel; i.ZoomAt = f.Pointer; }
            }

            // right button: grab and pan; a press that never moved is a secondary click
            if (f.Right.Pressed && !over) { _rightHeld = true; _rightDragged = false; _rightStart = f.Pointer; i.Grab = GrabPhase.Begin; i.GrabAt = f.Pointer; }
            else if (_rightHeld && f.Right.Held)
            {
                if (!_rightDragged && (f.Pointer - _rightStart).magnitude > slop) _rightDragged = true;
                if (_rightDragged) { i.Grab = GrabPhase.Move; i.GrabAt = f.Pointer; }
            }
            if (_rightHeld && !f.Right.Held)
            {
                _rightHeld = false;
                i.Grab = GrabPhase.End;
                if (!_rightDragged) { i.Gesture = PointerGesture.SecondaryClick; i.GesturePos = f.Pointer; }
            }

            // middle button: orbit
            if (f.Middle.Pressed && !over) { _middleHeld = true; _lastPointer = f.Pointer; }
            if (_middleHeld)
            {
                if (f.Middle.Held) i.OrbitPixels = f.Pointer - _lastPointer;
                else _middleHeld = false;
            }
            _lastPointer = f.Pointer;

            // left button: click, double click, box select, area brush (Alt)
            if (f.Left.Pressed && !over)
            {
                _leftHeld = true; _leftDragged = false; _leftStart = f.Pointer;
                if (f.Alt) { _brush = true; i.Gesture = PointerGesture.BrushBegin; i.GesturePos = f.Pointer; }
            }
            else if (_leftHeld && f.Left.Held)
            {
                if (_brush) { i.Gesture = PointerGesture.BrushMove; i.GesturePos = f.Pointer; }
                else if ((f.Pointer - _leftStart).magnitude > slop) { _leftDragged = true; Hud.RubberBand = Corners(_leftStart, f.Pointer); }
            }
            if (_leftHeld && !f.Left.Held)
            {
                _leftHeld = false;
                Hud.RubberBand = null;
                if (_brush) { _brush = false; i.Gesture = PointerGesture.BrushEnd; i.GesturePos = f.Pointer; }
                else if (_leftDragged) { i.Gesture = PointerGesture.BoxSelect; i.Box = Corners(_leftStart, f.Pointer); i.GesturePos = f.Pointer; }
                else
                {
                    bool dbl = _clock - _lastClickTime <= Tuning.DoubleClickSeconds && (f.Pointer - _lastClickPos).magnitude <= slop * 2f;
                    i.Gesture = dbl ? PointerGesture.DoubleClick : PointerGesture.Click;
                    i.GesturePos = f.Pointer;
                    _lastClickTime = dbl ? -10f : _clock; _lastClickPos = f.Pointer;
                }
            }

            if (f.Has(PlayerKeys.Home)) i.Home = true;
            if (f.Has(PlayerKeys.Follow)) i.ToggleFollow = true;
            if (f.Has(PlayerKeys.ZoomToIndividual)) i.ZoomToIndividual = true;
            if (f.Has(PlayerKeys.HouseholdOrHome)) i.HouseholdOrHome = true;
            if (f.Has(PlayerKeys.Escape)) i.Escape = true;
            if (f.Has(PlayerKeys.RosterNext)) i.RosterNext = true;
            if (f.Has(PlayerKeys.RosterPrev)) i.RosterPrev = true;
            if (f.Has(PlayerKeys.TogglePause)) i.TogglePause = true;
            if (f.Has(PlayerKeys.ToggleAccelerated)) i.ToggleAccelerated = true;
            if (f.Has(PlayerKeys.SpeedUp)) i.SpeedStep += 1;
            if (f.Has(PlayerKeys.SpeedDown)) i.SpeedStep -= 1;
            if (f.Has(PlayerKeys.StepTick)) i.StepTicks = f.Shift ? 10 : 1;
            return i;
        }

        private static Rect Corners(Vector2 a, Vector2 b) => Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));

        // ---------------------------------------------------------------- intent -> subsystems

        public void Apply(PlayerIntent i)
        {
            var d = Director; var t = d.Tuning;
            // ---- time (UI only: never a command)
            if (i.TogglePause) Hud.TogglePause();
            if (i.SpeedStep != 0) Hud.StepSpeed(i.SpeedStep);
            if (i.StepTicks > 0) Hud.StepTicks(i.StepTicks);
            if (i.ToggleAccelerated) Hud.ToggleAccelerated();

            // ---- camera
            if (i.ZoomTicks != 0f) d.Zoom(i.ZoomTicks, i.ZoomAt);
            if (i.TiltWheelTicks != 0f) d.Orbit(0f, i.TiltWheelTicks * t.WheelTiltDegrees);
            if (i.Grab == GrabPhase.Begin) d.BeginGrab(i.GrabAt);
            else if (i.Grab == GrabPhase.Move) d.GrabTo(i.GrabAt);
            else if (i.Grab == GrabPhase.End) d.EndGrab();
            if (i.OrbitPixels != Vector2.zero) d.Orbit(i.OrbitPixels.x * t.OrbitDegreesPerPixel, -i.OrbitPixels.y * t.OrbitDegreesPerPixel);
            if (i.Rotate != 0f || i.Tilt != 0f) d.Orbit(i.Rotate * t.KeyRotateDegreesPerSecond * i.Dt, i.Tilt * t.KeyTiltDegreesPerSecond * i.Dt);
            d.Pan(i.Pan, i.Dt);
            if (i.Home) d.Home();
            if (i.ZoomToIndividual) ZoomToPerson();
            if (i.ToggleFollow) ToggleFollow();
            if (i.RosterNext) CycleRoster(1);
            if (i.RosterPrev) CycleRoster(-1);

            // ---- selection
            if (i.BrushWheelTicks != 0f) BrushMetres = Mathf.Clamp(BrushMetres * Mathf.Pow(t.BrushWheelFactor, i.BrushWheelTicks), t.BrushMinMetres, t.BrushMaxMetres);
            switch (i.Gesture)
            {
                case PointerGesture.Click: Click(i.GesturePos, i.Additive); break;
                case PointerGesture.DoubleClick: DoubleClick(i.GesturePos); break;
                case PointerGesture.BoxSelect: BoxSelect(i.Box, i.Additive); break;
                case PointerGesture.BrushBegin:
                case PointerGesture.BrushMove:
                case PointerGesture.BrushEnd: Brush(i.GesturePos); break;
                case PointerGesture.SecondaryClick: SecondaryClicked?.Invoke(i.GesturePos); break;
            }
            if (i.Gesture != PointerGesture.None) Gestured?.Invoke(i.Gesture, i.GesturePos);
            if (i.HouseholdOrHome) { if (!SelectHouseholdOfPrimary()) d.Home(); }
            if (i.Escape) { if (!Selection.IsEmpty) Selection.Clear(); else Hud.PressEscape(); }
        }

        // ---------------------------------------------------------------- selection actions (public: tests and U2 call them too)

        public void Click(Vector2 screen, bool additive)
        {
            int person = Hud.PickNpc(screen);
            if (person != 0)
            {
                if (additive) Selection.Toggle(person); else Selection.SetPerson(person);
                return;
            }
            int b = Hud.PickBuilding(screen);
            Hud.ClearNpc();
            Vector3 gp = default;
            bool ground = Hud.Rig.GroundPoint(screen, out gp);
            int node = ground ? Hud.NodeAt(gp) : -1;
            if (b != 0) { Hud.WorldClick(node, b); Selection.SetObject(ObjectKind.Building, b); }
            else if (node >= 0) { Hud.WorldClick(node, 0); Selection.SetLocation(node, gp.x, gp.z); }
            else { Hud.ClearSelection(); Selection.Clear(); }
        }

        public void DoubleClick(Vector2 screen)
        {
            int person = Hud.PickNpc(screen);
            if (person == 0) { Click(screen, false); return; }
            Selection.SetPerson(person);
            Director.FocusOnPerson(person);
            int h = Hud.HouseholdOf(person);
            if (h != 0) Selection.SetHousehold(h, person);
        }

        public void BoxSelect(Rect screenRect, bool additive)
        {
            Hud.BoxSelectIds(screenRect, _ids);
            if (additive) foreach (int id in Selection.HasPersons ? Selection.Ids : (IReadOnlyList<int>)System.Array.Empty<int>()) if (!_ids.Contains(id)) _ids.Add(id);
            Selection.SetGroup(_ids);
        }

        /// <summary>Paints the area brush at a screen point: the circle on the ground, radius in lattice steps.</summary>
        public void Brush(Vector2 screen)
        {
            if (!Hud.Rig.GroundPoint(screen, out var gp)) return;
            int node = Hud.NodeAt(gp);
            if (node < 0) return;
            Selection.SetArea(new AreaSpec(node, AreaSpec.StepsOf(BrushMetres)), gp.x, gp.z);
        }

        public bool SelectHouseholdOfPrimary()
        {
            if (!(Selection.Kind == SelectionKind.Person || Selection.Kind == SelectionKind.Group || Selection.Kind == SelectionKind.Household) || Selection.Primary == 0) return false;
            int h = Hud.HouseholdOf(Selection.Primary);
            if (h == 0) return false;
            Selection.SetHousehold(h, Selection.Primary);
            return true;
        }

        public void ToggleFollow()
        {
            if (Director.Following) { Director.Unfollow(); return; }
            int id = PrimaryPerson();
            if (id != 0) Director.Follow(id);
        }

        public void ZoomToPerson()
        {
            int id = PrimaryPerson();
            if (id != 0 && Director.FocusOnPerson(id)) { Director.ZoomToBand(ZoomBand.Individual); return; }
            Director.ZoomToBand(ZoomBand.Individual);
        }

        private int PrimaryPerson() => Selection.Kind == SelectionKind.Person || Selection.Kind == SelectionKind.Group || Selection.Kind == SelectionKind.Household ? Selection.Primary : 0;

        /// <summary>Click-to-jump of a roster chip: focus on the person and select them (a followed person is followed by the camera).</summary>
        public bool JumpTo(int personId)
        {
            if (!Director.FocusOnPerson(personId)) return false;
            Selection.SetPerson(personId);
            if (Ui.IsFollowed(personId)) Director.Follow(personId);
            return true;
        }

        public void CycleRoster(int dir)
        {
            var r = Ui.Roster();
            if (r.Count == 0) return;
            _rosterIndex = ((_rosterIndex + dir) % r.Count + r.Count) % r.Count;
            JumpTo(r[_rosterIndex]);
        }

        // ---------------------------------------------------------------- selection -> HUD cards

        private void OnSelectionChanged()
        {
            switch (Selection.Kind)
            {
                case SelectionKind.Person:
                case SelectionKind.Group:
                case SelectionKind.Household:
                    if (Selection.Primary != 0 && Hud.SelectedNpc != Selection.Primary) Hud.SelectNpc(Selection.Primary);
                    break;
                default:
                    if (Hud.SelectedNpc != 0) Hud.ClearNpc();
                    break;
            }
        }

        // ---------------------------------------------------------------- commands

        /// <summary>Issues a divine act on the current selection through the builder (checked, then queued for the next tick that runs).</summary>
        public IssueResult Issue(string act, int arg = 0) => Builder.Issue(new DivineIntent(act, arg), Selection.Snapshot());
    }
}
