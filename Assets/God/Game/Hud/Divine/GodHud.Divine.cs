using System.Collections.Generic;
using God.Sim;
using God.Sim.Divinity;
using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>What a send did: how many commands the gate accepted, how many it refused (and why), and the chip that will show the answers.</summary>
    public sealed class DivineSendResult
    {
        public int Sent, Refused;
        public string Reason, Message;
        public InstructionChip Chip;
        public bool Ok => Sent > 0 && Refused == 0;
    }

    /// <summary>The ghost of an armed power on the hovered target: the gate's answer and the estimated cost (U2-T3).</summary>
    public struct GhostState
    {
        public bool Active, Valid, Affordable;
        public string Reason, Message;
        public long CostMilli;
        public int Person, Node;
    }

    /// <summary>
    /// U2a: the divine HUD (god-only mode). It reads the world through the human god's <see cref="IDivineView"/> and the inspection cards,
    /// the events through the <see cref="DivineHost"/>'s feed, and writes only through <c>PlayerCommandBuilder</c> and the gate; it never
    /// touches the state and names no divine command type (the factory does). Content is added through the registration hooks of R9:
    /// panels and a person-card tab, registered from <see cref="GodHud.Created"/>. This file: the state, arming, sending, the ghost, the gating.
    /// </summary>
    public sealed partial class GodHud
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void HookDivine() { Created -= InitDivineHud; Created += InitDivineHud; }

        private static void InitDivineHud(GodHud h) { h.InitDivine(); }

        /// <summary>True in a god-only game that has gods: the divine interface is on.</summary>
        public bool DivineOn => Boot != null && Boot.GodOnly && Boot.Divine != null && Boot.Divine.Available;

        /// <summary>
        /// Blocker 6: false in the god-only game, where the sim refuses faction commands anyway (<c>god_only</c>); true only where a human
        /// faction plays (stage 3). Every faction control (build bar, HQ, roads, war and peace, defend, trade, citadel, standings, army line,
        /// tutorial pointers) is drawn and reachable only when this is true.
        /// </summary>
        public bool FactionControlsOn => !Boot.GodOnly && !Watching;

        private void InitDivine()
        {
            if (!DivineOn) return;
            RegisterPanel("divine.meter", 100, DrawMeterPanel);
            RegisterPanel("divine.toolbar", 110, DrawToolbarPanel);
            RegisterPanel("divine.popover", 120, DrawPopoverPanel);
            RegisterPanel("divine.chips", 130, DrawChipsPanel);
            RegisterPanel("divine.alerts", 140, DrawAlertsPanel);
            RegisterPersonTab("divine", "Divine", 30, DrawDivineTab, c => DivineOn);
        }

        // ---------------------------------------------------------------- view of the god (refreshed once per tick)

        private DivineGodView _god;
        private bool _godOk;
        private long _godTick = long.MinValue;

        /// <summary>The human god as the divine view shows it (a cached copy, refreshed when the tick moves).</summary>
        public bool TryGod(out DivineGodView g)
        {
            var v = Boot.Divine?.View;
            if (v == null) { g = default; return false; }
            if (v.AsOfTick != _godTick) { _godTick = v.AsOfTick; _godOk = v.TryGod(v.God, out _god); }
            g = _god;
            return _godOk;
        }

        /// <summary>The tick of the divine view (the HUD never reads the sim's tick directly).</summary>
        public long DivineTick => Boot.Divine?.View?.AsOfTick ?? 0;

        /// <summary>The Mythic mode of the game (Standard when there is no god view).</summary>
        public DivinityMode MythicMode => Boot.Divine?.View?.Mode ?? DivinityMode.Standard;

        // ---------------------------------------------------------------- arming

        /// <summary>The power in hand (null: none). Works like the tool modes: click a target to use it, Esc or right-click puts it down.</summary>
        public PowerSlot Armed { get; private set; }

        /// <summary>The slot ids shown for the current Mythic mode.</summary>
        public List<PowerSlot> VisibleSlots()
        {
            _visible.Clear();
            DivineCatalog.Visible(MythicMode, _visible);
            return _visible;
        }

        private readonly List<PowerSlot> _visible = new List<PowerSlot>();

        public bool Arm(string slotId)
        {
            var s = DivineCatalog.Find(slotId);
            if (s == null || !DivineOn) return false;
            if (s.NeedsPower && MythicMode == DivinityMode.Disabled) return false;
            if (Armed == s) { CancelArmed(); return true; }
            Armed = s; PopoverOpen = false; TemplateOpen = false; Ghost = default;
            if (s.Family == SlotFamily.Inspect) Armed = null;
            else if (s.Family == SlotFamily.Commandment) TemplateOpen = true;
            return true;
        }

        /// <summary>Puts the power down (and closes the forms). True when there was something to put down.</summary>
        public bool CancelArmed()
        {
            bool any = Armed != null || PopoverOpen || TemplateOpen;
            Armed = null; PopoverOpen = false; TemplateOpen = false; Ghost = default;
            return any;
        }

        private PlayerController _hooked;

        private void HookPlayer()
        {
            var p = Boot.Player;
            if (p == null || _hooked == p) return;
            _hooked = p;
            p.Gestured += OnDivineGesture;
            p.SecondaryClicked += _ => CancelArmed();
        }

        /// <summary>A left click that the controller has already turned into a selection: with a power in hand, act on it.</summary>
        private void OnDivineGesture(PointerGesture g, Vector2 pos)
        {
            if (!DivineOn || Armed == null || (g != PointerGesture.Click && g != PointerGesture.DoubleClick && g != PointerGesture.BoxSelect)) return;
            UseArmedOnSelection();
        }

        /// <summary>Uses the armed power on the current selection: a miracle acts at once, an instruction opens the popover, the law opens the picker.</summary>
        public DivineSendResult UseArmedOnSelection()
        {
            var slot = Armed;
            if (slot == null) return null;
            var sel = Boot.Selection.Snapshot();
            if (sel == null || sel.Kind == SelectionKind.None) return null;
            if (slot.Family == SlotFamily.Miracle)
            {
                var req = PlayerCommandBuilder.Request(Boot.HumanGod, DivineCatalog.Intent(slot, DivineAct.None, 0), sel);
                var r = SendRequest(req, slot.Label);
                return r;
            }
            if (slot.Family == SlotFamily.Commandment) { TemplateOpen = true; return null; }
            PopoverOpen = true; _popSlot = slot;
            _popScope = ScopeOfSelection(sel);
            _popAct = slot.Family == SlotFamily.Compel ? DivineCatalog.CompelActs[0].Act : DivineCatalog.InstructActs[0].Act;
            _popExpiryMul = 1;
            return null;
        }

        private static TargetScope ScopeOfSelection(SelectionSnapshot sel)
        {
            switch (sel.Kind)
            {
                case SelectionKind.Group: return TargetScope.Group;
                case SelectionKind.Household: return TargetScope.Household;
                case SelectionKind.Area: return TargetScope.Area;
                case SelectionKind.Location: return TargetScope.Location;
                default: return TargetScope.Person;
            }
        }

        // ---------------------------------------------------------------- sending

        /// <summary>The instruction request of a slot, an act, a scope and an expiry on the current selection (null when the scope does not fit it).</summary>
        public DivineRequest InstructRequest(PowerSlot slot, DivineAct act, TargetScope scope, int expiryTicks)
        {
            var sel = Boot.Selection.Snapshot();
            if (sel == null || sel.Kind == SelectionKind.None) return null;
            var intent = DivineCatalog.Intent(slot, act, expiryTicks);
            int primary = sel.Primary;
            switch (scope)
            {
                case TargetScope.Person:
                    if (primary == 0) return null;
                    return PlayerCommandBuilder.Request(Boot.HumanGod, intent, PersonSnapshot(primary));
                case TargetScope.Group:
                    if (sel.Kind != SelectionKind.Group && sel.Kind != SelectionKind.Person) return null;
                    return PlayerCommandBuilder.Request(Boot.HumanGod, intent, sel);
                case TargetScope.Household:
                    {
                        int hh = sel.Kind == SelectionKind.Household ? sel.Ids.Length > 0 ? sel.Ids[0] : 0 : HouseholdOf(primary);
                        if (hh == 0) return null;
                        var s2 = new SelectionSnapshot { Kind = SelectionKind.Household, Ids = new[] { hh }, Primary = primary };
                        return PlayerCommandBuilder.Request(Boot.HumanGod, intent, s2);
                    }
                case TargetScope.Area:
                    {
                        if (sel.Kind == SelectionKind.Area) return PlayerCommandBuilder.Request(Boot.HumanGod, intent, sel);
                        int node = NodeOfPerson(primary);
                        if (node < 0) return null;
                        return PlayerCommandBuilder.Request(Boot.HumanGod, intent, new SelectionSnapshot { Kind = SelectionKind.Area, Area = new AreaSpec(node, 4) });
                    }
                case TargetScope.Location:
                    {
                        int node = sel.Kind == SelectionKind.Location ? sel.Node : NodeOfPerson(primary);
                        if (node < 0) return null;
                        return PlayerCommandBuilder.Request(Boot.HumanGod, intent, new SelectionSnapshot { Kind = SelectionKind.Location, Node = node });
                    }
            }
            return null;
        }

        private static SelectionSnapshot PersonSnapshot(int id) => new SelectionSnapshot { Kind = SelectionKind.Person, Ids = new[] { id }, Primary = id };

        private int NodeOfPerson(int id)
        {
            var v = Boot.Divine?.View;
            return v != null && id != 0 && v.TryPerson(id, out var p) ? p.Node : -1;
        }

        /// <summary>
        /// Sends a request through the builder (checked with the gate, then queued for the next tick that runs). A group becomes one command per
        /// person sharing one chip. A refusal is kept and shown, never swallowed.
        /// </summary>
        public DivineSendResult SendRequest(DivineRequest req, string label, PowerSlot slot = null)
        {
            var res = new DivineSendResult();
            var feed = Boot.Divine.Feed;
            var b = Boot.Player.Builder;
            if (req == null) { res.Refused = 1; res.Reason = "no_selection"; res.Message = "Select someone or somewhere first."; NoteRefusal(label, res.Message); return res; }
            slot = slot ?? SlotOf(req);
            foreach (var one in PlayerCommandBuilder.Expand(req))
            {
                var ir = b.Issue(one);
                if (ir.Submitted)
                {
                    res.Sent++;
                    res.Chip = feed.Sent(ChipLabel(label, req), slot != null ? slot.Kind : InterventionKind.None, ActOf(req), Boot.HumanGod, DivineTick, res.Chip);
                }
                else { res.Refused++; if (res.Reason == null) { res.Reason = ir.Result.Reason; res.Message = ir.Result.Message; } }
            }
            if (res.Refused > 0) NoteRefusal(label, res.Message ?? res.Reason);
            else { RefusalText = null; }
            return res;
        }

        private static PowerSlot SlotOf(DivineRequest r)
        {
            foreach (var s in DivineCatalog.Slots)
                if (s.Act == r.Act && (s.Family != SlotFamily.Miracle || s.Arg == r.Arg)) return s;
            return null;
        }

        private static DivineAct ActOf(DivineRequest r) => r.Act == DivineActs.Miracle || r.Act == DivineActs.Reveal || r.Act == DivineActs.SetMythicMode ? DivineAct.None : (DivineAct)r.Arg;

        private static string ChipLabel(string label, DivineRequest r) => r.Act == DivineActs.Miracle || r.Act == DivineActs.Reveal || r.Act == DivineActs.SetMythicMode ? label : label + " " + (DivineAct)r.Arg;

        /// <summary>The last refusal as the player reads it (null when the last send went through).</summary>
        public string RefusalText { get; private set; }
        private float _refusalUntil;

        private void NoteRefusal(string label, string message)
        {
            RefusalText = label + ": " + (string.IsNullOrEmpty(message) ? "refused" : message);
            _refusalUntil = Time.unscaledTime + 7f;
        }

        /// <summary>Sets the Mythic mode (sandbox games): sends <c>SetMythicMode</c> through the builder.</summary>
        public DivineSendResult SetMythicMode(DivinityMode mode)
        {
            var req = new DivineRequest { God = Boot.HumanGod, Act = DivineActs.SetMythicMode, Arg = (int)mode, Node = -1 };
            return SendRequest(req, "Mythic mode " + mode);
        }

        /// <summary>Casts a miracle at once on the current selection (the tray; also what clicking with a miracle armed does).</summary>
        public DivineSendResult CastOnSelection(string slotId)
        {
            var slot = DivineCatalog.Find(slotId);
            if (slot == null || slot.Family != SlotFamily.Miracle) return null;
            var req = PlayerCommandBuilder.Request(Boot.HumanGod, DivineCatalog.Intent(slot, DivineAct.None, 0), Boot.Selection.Snapshot());
            return SendRequest(req, slot.Label, slot);
        }

        // ---------------------------------------------------------------- ghost (armed power on the hovered target)

        public GhostState Ghost { get; private set; }

        /// <summary>The ghost of a slot over a hovered person or node: the gate's answer, never a rule of its own.</summary>
        public GhostState EvaluateGhost(PowerSlot slot, int person, int node)
        {
            var g = new GhostState { Person = person, Node = node };
            if (slot == null || (person == 0 && node < 0)) return g;
            g.Active = true;
            var intent = DivineCatalog.Intent(slot, slot.Family == SlotFamily.Compel ? DivineCatalog.CompelActs[0].Act : DivineCatalog.InstructActs[0].Act,
                DivineCatalog.DefaultExpiry(slot.Family, Boot.Divine.TicksPerDay));
            SelectionSnapshot sel = person != 0 ? PersonSnapshot(person) : new SelectionSnapshot { Kind = SelectionKind.Location, Node = node };
            var req = PlayerCommandBuilder.Request(Boot.HumanGod, intent, sel);
            var check = DivineCatalog.Check(Boot.Player.Builder, req);
            g.Valid = check.Valid; g.Reason = check.Reason; g.Message = check.Message;
            g.CostMilli = DivineCatalog.CostOf(Boot.Divine.Costs, slot, 1);
            g.Affordable = MythicMode != DivinityMode.Standard || (TryGod(out var god) && god.Mythic >= g.CostMilli);
            return g;
        }

        private void UpdateGhost()
        {
            if (Armed == null) { Ghost = default; return; }
            var mouse = DebugCursor ?? (Vector2)Input.mousePosition;
            if (IsOverUi(mouse)) { Ghost = default; return; }
            int person = PickNpc(mouse);
            Ghost = EvaluateGhost(Armed, person, person == 0 ? HoverNode : -1);
        }

        // ---------------------------------------------------------------- per frame (called from DrawDynamic, flagged hook)

        private void DivineUpdate(HudOverlay.Soft d)
        {
            if (!DivineOn) return;
            HookPlayer();
            var h = Boot.Divine;
            long tick = h.View != null ? h.View.AsOfTick : 0;
            h.Alerts.Advance(tick);
            h.Cues.Prune(Time.unscaledTime);
            if (_refusalUntil > 0f && Time.unscaledTime > _refusalUntil) { RefusalText = null; _refusalUntil = 0f; }
            if (MythicMode == DivinityMode.Disabled && Armed != null && Armed.NeedsPower) CancelArmed();
            UpdateGhost();
            DrawDivineCues(d);
        }
    }
}
