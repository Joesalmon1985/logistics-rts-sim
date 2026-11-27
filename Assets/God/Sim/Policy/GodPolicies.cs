using System.Collections.Generic;
using God.Sim.Divinity;
using God.Sim.Information;

namespace God.Sim.Policy
{
    /// <summary>
    /// Contracts 12: the god-level policies. <see cref="IdleGod"/> never acts (the control of pin P2, F1);
    /// <see cref="ScheduleGod"/> issues a fixed schedule from scenario tuning (the lead's default test god: an <c>Ai</c>-seated
    /// god acting from inside the sim, LEAD_RULINGS R1). Every policy reads only its <see cref="IDivineView"/>. OWNER: D1.
    /// </summary>
    public static class GodPolicies
    {
        public const string Idle = "IdleGod";
        public const string Schedule = "ScheduleGod";

        /// <summary>The god policy names a <c>SetGodPolicy</c> command may name (the standard registry's god level).</summary>
        public static bool IsKnown(string name) => name == Idle || name == Schedule;

        /// <summary>Adds the god-level policies to a registry (called by <c>StandardPolicies.Registry</c>).</summary>
        public static void Register(PolicyRegistry r)
        {
            r.Register(PolicyLevel.God, Idle, ctx => new IdleGod(), "F1: a god that never acts (the control for P2).");
            r.Register(PolicyLevel.God, Schedule, ctx => new ScheduleGod(),
                "D1: issues the fixed schedule of Divinity.Mythic.Schedule (tick, command template) for its own god; the scripted test god.");
        }
    }

    /// <summary>A god policy that never issues a command.</summary>
    public sealed class IdleGod : IGodPolicy
    {
        public string Name => GodPolicies.Idle;
        public PolicyLevel Level => PolicyLevel.God;
        public void Decide(in IDivineView view, ref Rng rng, List<Command> outCommands) { }
    }

    /// <summary>
    /// D1 (spec 6): a scripted god. At each think it issues, for its own god, every schedule slot that fires in the ticks
    /// since its previous think (<c>(AsOfTick - ThinkTicks, AsOfTick]</c>), in slot order then tick order. Stateless (the
    /// window is a function of the tick), so a loaded game continues exactly. The schedule and the think interval are given
    /// by the god director from the game's tuning (<see cref="Configure"/>); it never reads state.
    /// </summary>
    public sealed class ScheduleGod : IGodPolicy
    {
        public string Name => GodPolicies.Schedule;
        public PolicyLevel Level => PolicyLevel.God;

        private ScheduleSlot[] _slots = new ScheduleSlot[0];
        private int _think = 1;

        /// <summary>Sets the slots and the think interval (copies the slots).</summary>
        public void Configure(GodSchedule schedule, int thinkTicks)
        {
            var src = schedule?.Slots() ?? new ScheduleSlot[0];
            _slots = new ScheduleSlot[src.Length];
            for (int i = 0; i < src.Length; i++)
                _slots[i] = new ScheduleSlot { Tick = src[i].Tick, Every = src[i].Every, Until = src[i].Until, God = src[i].God, Template = src[i].Template, A = src[i].A, B = src[i].B, C = src[i].C };
            _think = thinkTicks < 1 ? 1 : thinkTicks;
        }

        public void Decide(in IDivineView view, ref Rng rng, List<Command> outCommands)
        {
            long now = view.AsOfTick;
            int god = view.God;
            foreach (var slot in _slots)
            {
                if (slot.God != 0 && slot.God != god) continue;
                for (long t = now - _think + 1; t <= now; t++)
                    if (slot.FiresAt(t)) { var c = Build(slot, god, view); if (c != null) outCommands.Add(c); }
            }
        }

        /// <summary>The command a template stands for, in the god's name (null for <c>None</c>).</summary>
        public static Command Build(ScheduleSlot slot, int god)
        {
            var who = new DivineTarget(DivineTargetKind.Npc, slot.B);
            switch (slot.Template)
            {
                case ScheduleTemplate.TestSpend: return new DivineTestSpend { God = god, Kind = (InterventionKind)slot.A, Cost = slot.B };
                case ScheduleTemplate.Suggestion: return new IssueSuggestion { God = god, Act = (DivineAct)slot.A, Target = who, Scope = DivineScope.Individual, ExpireTicks = slot.C };
                case ScheduleTemplate.Command: return new IssueCommand { God = god, Act = (DivineAct)slot.A, Target = who, Scope = DivineScope.Individual, ExpireTicks = slot.C };
                case ScheduleTemplate.Miracle: return new CastMiracle { God = god, Kind = (MiracleKind)slot.A, Target = who, Magnitude = slot.C };
                case ScheduleTemplate.Compulsion: return new IssueCompulsion { God = god, Act = (DivineAct)slot.A, Target = who, ExpireTicks = slot.C };
                case ScheduleTemplate.Possess: return new BeginPossession { God = god, Npc = slot.B };
                case ScheduleTemplate.EndPossession: return new EndPossession { God = god };
                case ScheduleTemplate.Dismiss: return new DismissAvatar { God = god };
                default: return null;
            }
        }

        /// <summary>
        /// Flagged additive edit (D4): the templates that need the god's view (the controlled body, a person's node); the others
        /// as <see cref="Build(ScheduleSlot, int)"/>. Null when the body does not exist (nothing to steer).
        /// </summary>
        public static Command Build(ScheduleSlot slot, int god, IDivineView view)
        {
            switch (slot.Template)
            {
                case ScheduleTemplate.ControlHeading: case ScheduleTemplate.ControlMoveTo: case ScheduleTemplate.ControlAction:
                {
                    int body = 0;
                    if (view != null && view.TryPossession(out var pv)) body = pv.Npc;
                    else if (view != null && view.TryAvatar(out var av) && av.Alive) body = av.Body;
                    if (body == 0) return null;
                    var c = new DivineControlInput { God = god, Body = body };
                    if (slot.Template == ScheduleTemplate.ControlHeading) { c.Kind = ControlInputKind.Heading; c.Heading = (byte)slot.A; c.Gait = (byte)(slot.C <= 0 ? 1 : slot.C); }
                    else if (slot.Template == ScheduleTemplate.ControlMoveTo) { c.Kind = ControlInputKind.MoveTo; c.Node = slot.B; }
                    else { c.Kind = ControlInputKind.Action; c.Act = (DivineAct)slot.A; c.Target = ActionTarget((DivineAct)slot.A, slot.B, slot.C); }
                    return c;
                }
                case ScheduleTemplate.Manifest:
                {
                    int node = slot.B;
                    if (node == 0 && slot.A != 0 && view != null && view.TryPerson(slot.A, out var p)) node = p.Node;
                    return new ManifestAvatar { God = god, Node = node };
                }
                default: return Build(slot, god);
            }
        }

        /// <summary>The object of a scheduled action input (D4's input target convention, <c>Embodiment</c>).</summary>
        public static DivineTarget ActionTarget(DivineAct act, int obj, int extra)
        {
            switch (act)
            {
                case DivineAct.Take: case DivineAct.Drop: case DivineAct.UseTool:
                    return obj == 0 ? default : new DivineTarget(DivineTargetKind.Lot, obj);
                case DivineAct.Give: return new DivineTarget(DivineTargetKind.Npc, obj, extra);
                case DivineAct.Approach: return new DivineTarget(DivineTargetKind.Npc, obj);
                case DivineAct.Communicate: return new DivineTarget(DivineTargetKind.Npc, obj, extra);
                case DivineAct.Sleep: return obj == 0 ? default : new DivineTarget(DivineTargetKind.Hut, obj);
                default: return obj == 0 ? default : new DivineTarget(DivineTargetKind.Location, 0, obj);
            }
        }
    }
}
