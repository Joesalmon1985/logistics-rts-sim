using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN2 (contracts 23.5, D9.1 b, D9.4 a): the one primitive executor. Every origin (trained policy, possession, avatar, compulsion,
    /// fixtures, the teacher) submits the same <see cref="PrimitiveControl"/> here; the origin is recorded on the events and on the body
    /// (saved, never hashed) and never changes a physical result (QA-01). Only physical checks refuse: existence, reach, room, weight,
    /// count, passability, occupancy, busy, asleep, dead and a known design. No title, no permission, no high-level verb, no pather.
    /// <para>Timing: turns and steps take effect when they complete (<see cref="Tick"/>); TAKE, DROP, GESTURE and SLEEP take effect when
    /// submitted and then hold the body for their ticks; USE validates when submitted and applies only at completion. One action per
    /// body; the result of every submission is stored in <see cref="UnifiedNpc.LastResult"/> (an input of the body's next decision).</para>
    /// </summary>
    public static partial class Motor
    {
        public static MotorStatus Submit(GodState s, EventBuffer ev, in PrimitiveControl c, ControlOrigin origin) => Run(s, ev, in c, origin, true);

        /// <summary>The checks of <see cref="Submit"/> without any effect (possession validation, legality probes): the same status.</summary>
        public static MotorStatus Check(GodState s, in PrimitiveControl c) => Run(s, null, in c, ControlOrigin.Policy, false);

        private static MotorStatus Run(GodState s, EventBuffer ev, in PrimitiveControl c, ControlOrigin origin, bool apply)
        {
            if (!Unified.On(s) || s.Population == null) return MotorStatus.Disabled;
            var npc = s.Population.Find(c.Actor);
            var u = s.Unification.PersonOf(c.Actor);
            if (npc == null || u == null || !Bodies.IsPerson(npc))
            {
                if (apply) ev?.Add(new ControlFailed { Actor = c.Actor, Control = c.Control, Origin = origin, Status = MotorStatus.Invalid });
                return MotorStatus.Invalid;
            }
            var st = Precheck(s, npc, u, in c);
            if (st == MotorStatus.Accepted)
            {
                switch (c.Control)
                {
                    case Prim.TurnLeft: case Prim.TurnRight: st = Turn(s, npc, u, in c, origin, apply); break;
                    case Prim.StepForward: case Prim.StepBack: case Prim.StepLeft: case Prim.StepRight: st = Step(s, npc, u, in c, origin, apply); break;
                    case Prim.Take: st = Take(s, ev, npc, u, in c, origin, apply); break;
                    case Prim.Drop: st = Drop(s, ev, npc, u, in c, origin, apply); break;
                    case Prim.Use: st = Resolver.Start(s, ev, npc, u, in c, origin, apply); break;
                    case Prim.Gesture: st = Gesture(s, ev, npc, u, in c, origin, apply); break;
                    case Prim.Sleep: st = Sleep(s, ev, npc, u, origin, apply, false); break;
                    case Prim.Wait: st = Start(s, u, Prim.Wait, s.Tuning.Unification.UN2.WaitTicks, TargetRef.Self, origin, apply); break;
                    default: st = MotorStatus.Invalid; break;
                }
            }
            if (!apply) return st;
            u.LastResult = st;
            if (st == MotorStatus.Accepted)
                ev?.Add(new ControlApplied
                {
                    Actor = c.Actor, Control = c.Control, Origin = origin, Status = st, TargetKind = c.Target.Kind, TargetId = c.Target.Id,
                    Ware = (short)WareOfLot(s, npc, c.SourceLot),
                });
            else ev?.Add(new ControlFailed { Actor = c.Actor, Control = c.Control, Origin = origin, Status = st });
            return st;
        }

        /// <summary>What every control needs: a well-formed record, a living, awake, idle body.</summary>
        private static MotorStatus Precheck(GodState s, Npc npc, UnifiedNpc u, in PrimitiveControl c)
        {
            if (c.Control == Prim.None || c.Control > Prim.Wait) return MotorStatus.Invalid;
            if (c.Slot > 3 || c.Quantity < 0 || c.Quantity > PrimitiveControl.MaxQuantity) return MotorStatus.Invalid;
            if (!npc.Alive) return MotorStatus.Dead;
            if (npc.Action.Asleep) return MotorStatus.Asleep;
            if (u.Busy) return MotorStatus.Busy;
            return MotorStatus.Accepted;
        }

        /// <summary>Begins a timed action (apply only).</summary>
        internal static MotorStatus Start(GodState s, UnifiedNpc u, Prim kind, int ticks, TargetRef target, ControlOrigin origin, bool apply,
            int source = 0, byte slot = 0)
        {
            if (!apply) return MotorStatus.Accepted;
            u.ClearAction();
            u.ActionKind = kind;
            u.ActionTicksLeft = ticks < 1 ? 1 : ticks;
            u.ActionTotal = u.ActionTicksLeft;
            u.ActionTarget = target;
            u.ActionSource = source;
            u.ActionSlot = slot;
            u.ActionOrigin = origin;
            return MotorStatus.Accepted;
        }

        /// <summary>Advances every action in progress by one tick (ascending actor id); completes those that run out.</summary>
        public static void Tick(GodState s, EventBuffer ev)
        {
            var u = s.Unification;
            if (u == null || s.Population == null) return;
            var people = u.People;
            for (int i = 0; i < people.Count; i++)
            {
                var r = people[i];
                if (r.ActionKind == Prim.None) continue;
                var npc = s.Population.Find(r.Npc);
                if (npc == null || !npc.Alive) { r.ClearAction(); continue; }
                if (r.ActionKind == Prim.Sleep)
                {
                    // waking completes SLEEP here, with every other completion: the body decides at its next tick, never the tick it woke
                    if (npc.Action.Asleep && WakesNow(s, npc)) Wake(s, ev, npc, r);
                    continue;
                }
                if (r.ActionTicksLeft > 0) r.ActionTicksLeft--;
                if (r.ActionTicksLeft > 0) continue;
                Complete(s, ev, npc, r);
            }
        }

        private static void Complete(GodState s, EventBuffer ev, Npc npc, UnifiedNpc r)
        {
            var kind = r.ActionKind;
            var origin = r.ActionOrigin;
            var st = MotorStatus.Accepted;
            switch (kind)
            {
                case Prim.TurnLeft: r.Facing = (byte)((r.Facing + 5) % 6); break;
                case Prim.TurnRight: r.Facing = (byte)((r.Facing + 1) % 6); break;
                case Prim.StepForward: case Prim.StepBack: case Prim.StepLeft: case Prim.StepRight: st = FinishStep(s, npc, r); break;
                case Prim.Use: st = Resolver.Complete(s, ev, npc, r); break;
            }
            r.ClearAction();
            if (st != MotorStatus.Accepted)
            {
                r.LastResult = st;
                ev?.Add(new ControlFailed { Actor = npc.Id, Control = kind, Origin = origin, Status = st });
            }
        }

        public static bool Busy(GodState s, int actor) => s.Unification?.PersonOf(actor)?.Busy ?? false;

        /// <summary>
        /// Pain, collapse, strike, death, possession or a divine act (<see cref="InterruptReason"/>): the action in progress ends now with
        /// <see cref="ActionInterrupted"/>; an unfinished USE applies nothing (its completed earlier units stay). A sleeper is woken by
        /// anything but collapse and death.
        /// </summary>
        public static void Interrupt(GodState s, EventBuffer ev, int actor, byte reason)
        {
            var u = s.Unification?.PersonOf(actor);
            var npc = s.Population?.Find(actor);
            if (u == null || npc == null) return;
            if (npc.Action.Asleep)
            {
                if (reason == InterruptReason.Collapse) return;
                ev?.Add(new ActionInterrupted { Actor = actor, Control = Prim.Sleep, Reason = reason, TicksDone = (int)(s.Tick - npc.Action.Since), Origin = u.ActionOrigin });
                if (reason == InterruptReason.Death) { u.ClearAction(); return; }
                Wake(s, ev, npc, u);
                return;
            }
            if (!u.Busy) return;
            ev?.Add(new ActionInterrupted { Actor = actor, Control = u.ActionKind, Reason = reason, TicksDone = u.ActionTotal - u.ActionTicksLeft, Origin = u.ActionOrigin });
            u.ClearAction();
        }

        // ---------------- turning, sleeping, waking ----------------

        private static MotorStatus Turn(GodState s, Npc npc, UnifiedNpc u, in PrimitiveControl c, ControlOrigin origin, bool apply) =>
            Start(s, u, c.Control, s.Tuning.Unification.UN2.TurnTicks, TargetRef.Self, origin, apply);

        /// <summary>
        /// SLEEP where the body stands: the legacy sleep state (<c>Npc.Action.Asleep</c>), so <c>PopulationSystem.Body()</c> recovers
        /// fatigue exactly as for a legacy sleeper; waking follows the legacy waking conditions (<see cref="PersonTick"/>).
        /// </summary>
        internal static MotorStatus Sleep(GodState s, EventBuffer ev, Npc npc, UnifiedNpc u, ControlOrigin origin, bool apply, bool collapsed)
        {
            if (!apply) return MotorStatus.Accepted;
            Start(s, u, Prim.Sleep, 1, TargetRef.Self, origin, true);
            npc.Action = new NpcAction { Verb = Verb.Sleep, TargetKind = IntentTarget.Node, TargetNode = npc.Node, Asleep = true, Since = s.Tick };
            s.Population.Stats.Sleeps++;
            ev?.Add(new Slept { Npc = npc.Id, Node = npc.Node, Hut = 0, Collapsed = collapsed, Fatigue = npc.DriveOf(Drive.Fatigue) });
            return MotorStatus.Accepted;
        }

        /// <summary>The legacy waking conditions: rested, or hungry once somewhat rested.</summary>
        private static bool WakesNow(GodState s, Npc npc)
        {
            var tu = s.Tuning.Population;
            int f = npc.DriveOf(Drive.Fatigue);
            return f <= tu.WakeFatigue || npc.DriveOf(Drive.Hunger) >= tu.WakeHunger && f < tu.SleepFrom;
        }

        private static void Wake(GodState s, EventBuffer ev, Npc npc, UnifiedNpc u)
        {
            npc.Action = new NpcAction { Since = s.Tick };
            u.ClearAction();
            ev?.Add(new Slept { Npc = npc.Id, Node = npc.Node, Hut = 0, Woke = true, Fatigue = npc.DriveOf(Drive.Fatigue) });
        }

        /// <summary>
        /// The body's own tick (from <c>UnifiedHooks.PersonTick</c>, after <c>Body()</c>): a sleeper wakes on the legacy conditions
        /// (rested, or hungry once somewhat rested); an awake body at top fatigue collapses asleep (interrupting what it was doing).
        /// </summary>
        public static void PersonTick(GodState s, EventBuffer ev, Npc npc)
        {
            var u = s.Unification?.PersonOf(npc.Id);
            if (u == null || !npc.Alive) return;
            var tu = s.Tuning.Population;
            int f = npc.DriveOf(Drive.Fatigue);
            if (npc.Action.Asleep)
            {
                if (u.ActionKind != Prim.Sleep) Start(s, u, Prim.Sleep, 1, TargetRef.Self, u.ActionOrigin, true);
                return; // waking is checked by Tick (after every person's body), so a sleeper never decides in the tick it wakes

            }
            if (u.ActionKind == Prim.Sleep) u.ClearAction();
            if (f >= tu.CollapseFatigue)
            {
                Interrupt(s, ev, npc.Id, InterruptReason.Collapse);
                Sleep(s, ev, npc, u, u.ActionOrigin, true, true);
            }
        }

        // ---------------- helpers ----------------

        internal static int WareOfLot(GodState s, Npc npc, int lot)
        {
            if (lot == 0 || npc.Carry == null) return 0;
            foreach (var l in npc.Carry) if (!l.Empty && l.Id == lot) return (int)l.Ware;
            return 0;
        }

        internal static int CarrySlotOf(Npc npc, int lot)
        {
            if (lot == 0 || npc.Carry == null) return -1;
            for (int i = 0; i < npc.Carry.Length; i++) if (!npc.Carry[i].Empty && npc.Carry[i].Id == lot) return i;
            return -1;
        }
    }
}
