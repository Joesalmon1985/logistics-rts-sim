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
                    if (slot.FiresAt(t)) { var c = Build(slot, god); if (c != null) outCommands.Add(c); }
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
                default: return null;
            }
        }
    }
}
