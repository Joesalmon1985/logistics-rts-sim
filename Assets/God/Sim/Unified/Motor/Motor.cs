using System;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.5, UN0 spec 5.8): the signatures of the one primitive executor, declared so every package compiles against
    /// them. OWNER UN2: it replaces these bodies (this file is UN2's). Until then every call throws: nothing in a UN0 game calls
    /// them (the possession seam answers <see cref="MotorStatus.Disabled"/> instead).
    /// <para>Rules (23.5): one action in progress per body; durations in ticks from data; every submission's result is an observable
    /// input for the actor's next decision; origin changes nothing physical and is recorded on events; the sim never paths for an NPC.</para>
    /// </summary>
    public static partial class Motor
    {
        public static MotorStatus Submit(GodState s, EventBuffer ev, in PrimitiveControl c, ControlOrigin origin) => throw new NotImplementedException("Motor.Submit: UN2");

        /// <summary>Advances action progress, ascending actor id.</summary>
        public static void Tick(GodState s, EventBuffer ev) => throw new NotImplementedException("Motor.Tick: UN2");

        public static bool Busy(GodState s, int actor) => s.Unification?.PersonOf(actor)?.Busy ?? false;

        /// <summary>Pain, collapse, strike, death (<see cref="InterruptReason"/>).</summary>
        public static void Interrupt(GodState s, EventBuffer ev, int actor, byte reason) => throw new NotImplementedException("Motor.Interrupt: UN2");

        /// <summary>Physical legality only.</summary>
        public static void Legal(GodState s, int actor, in Percept p, ref ActMask mask) => throw new NotImplementedException("Motor.Legal: UN2");
    }
}
