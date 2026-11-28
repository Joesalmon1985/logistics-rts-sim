using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN6: the brain's only contact with the world, through UN2's interface (contracts 23.5, 23.6): sense (UN2's <c>Percept.Encode</c>,
    /// <c>Percept.ToVector</c> and <c>Motor.Legal</c>: local perception and physical legality only), turn a factorised choice back into a
    /// <see cref="PrimitiveControl"/> through the percept's slots, submit to <c>Motor.Submit</c> with an origin, and name the perceived
    /// entities and the persons who can perceive a body (through <see cref="ISpatialIndex"/>, never all-pairs). <see cref="MotorBody"/>
    /// is the game's implementation; tests substitute a fixture body. A body never paths, never answers a truth query.
    /// </summary>
    public interface IBrainBody
    {
        /// <summary>False when the world cannot be sensed (no motor yet): the person does not decide.</summary>
        bool Available(GodState s);

        /// <summary>Encodes the actor's perception into <paramref name="p"/> and <paramref name="obs"/> and its legality into <paramref name="m"/>.</summary>
        void Sense(GodState s, Npc npc, ref Percept p, short[] obs, ref ActMask m);

        /// <summary>The control a choice names (target slot to <see cref="TargetRef"/>, source slot to a carried lot ...).</summary>
        PrimitiveControl Decode(GodState s, in Percept p, in BrainChoice c);

        MotorStatus Submit(GodState s, EventBuffer ev, in PrimitiveControl c, ControlOrigin origin);

        /// <summary>The perceived entity in vector slot <paramref name="slot"/>: its target kind, id and node (false past the last).</summary>
        bool Entity(in Percept p, int slot, out TargetKind kind, out int id, out int node);

        /// <summary>The living persons within <paramref name="radius"/> of <paramref name="actor"/>'s node, nearest first, at most <c>into.Length</c>.</summary>
        int PersonsNear(GodState s, int actor, int radius, int[] into, int[] distance);
    }
}
