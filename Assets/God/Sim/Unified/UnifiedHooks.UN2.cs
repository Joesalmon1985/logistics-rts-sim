using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN2's slots of the unified dispatch (contracts 23.5, R85): the motor's world tick, the body's own tick (waking, collapse), the
    /// possession adapter (a <c>ControlInputKind.Primitive</c> input is checked and submitted by the same <see cref="Motor"/> as every
    /// origin, labelled Possessed or Avatar; the sim never paths for it) and the end of a dead body's action. OWNER: UN2.
    /// </summary>
    public static partial class UnifiedHooks
    {
        static partial void TickMotor(GodState s, EventBuffer ev) => Motor.Tick(s, ev);

        static partial void PersonTickUN2(GodState s, EventBuffer ev, Npc npc) => Motor.PersonTick(s, ev, npc);

        static partial void CheckPossessedUN2(GodState s, int body, in PrimitiveControl c, ref MotorStatus status) => status = Motor.Check(s, in c);

        static partial void SubmitPossessedUN2(GodState s, EventBuffer ev, int body, ControlOrigin origin, in PrimitiveControl c, ref MotorStatus status)
        {
            if (c.Actor != body) { status = MotorStatus.Invalid; return; }
            status = Motor.Submit(s, ev, in c, origin);
        }

        static partial void OnDeathUN2(GodState s, EventBuffer ev, Npc npc) => Motor.Interrupt(s, ev, npc.Id, InterruptReason.Death);
    }
}
