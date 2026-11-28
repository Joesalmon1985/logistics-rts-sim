using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN6: the game's <see cref="IBrainBody"/>, over UN2's executor and encoder. <b>Until UN2 merges</b> (its <c>Percept.Encode</c>,
    /// <c>Percept.ToVector</c>, <c>Percept.TargetOf</c>/<c>SourceOf</c>, <c>Motor.Legal</c> and <c>Motor.Submit</c>) the body is not
    /// <see cref="Available"/> and no person decides in a game (the foundation's motor stubs throw). This file is the one place UN6
    /// reconciles with UN2 at merge.
    /// </summary>
    public sealed class MotorBody : IBrainBody
    {
        public bool Available(GodState s) => false;

        public void Sense(GodState s, Npc npc, ref Percept p, short[] obs, ref ActMask m) { }

        public PrimitiveControl Decode(GodState s, in Percept p, in BrainChoice c) => new PrimitiveControl { Control = (Prim)c.Control, Actor = p.Actor };

        public MotorStatus Submit(GodState s, EventBuffer ev, in PrimitiveControl c, ControlOrigin origin) => MotorStatus.Disabled;

        public bool Entity(in Percept p, int slot, out TargetKind kind, out int id, out int node) { kind = TargetKind.None; id = 0; node = -1; return false; }

        public int PersonsNear(GodState s, int actor, int radius, int[] into, int[] distance) => 0;
    }
}
