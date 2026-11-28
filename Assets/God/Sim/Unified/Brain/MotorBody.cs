using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN6: the game's <see cref="IBrainBody"/>, over UN2's executor and encoder (reconciled at the UN2+UN6 merge, slice 1): sense is
    /// <c>Percept.Encode</c> + <c>Percept.ToVector</c> (Obs v1) + <c>Motor.Legal</c> (physical legality only), a choice decodes through
    /// <c>Percept.ControlOf</c> (Act v1), submission is <c>Motor.Submit</c> with the origin. Persons near a body come from the world's
    /// <see cref="ISpatialIndex"/> (never all-pairs). No path, no truth query.
    /// </summary>
    public sealed class MotorBody : IBrainBody
    {
        private readonly SpatialHit[] _hits = new SpatialHit[ObsSchema.Entities + 8];

        public bool Available(GodState s) => Unified.On(s) && s.Population != null && s.Unification?.Index != null;

        public void Sense(GodState s, Npc npc, ref Percept p, short[] obs, ref ActMask m)
        {
            Percept.Encode(s, npc.Id, ref p);
            Percept.ToVector(in p, obs);
            m = default;
            Motor.Legal(s, npc.Id, in p, ref m);
        }

        public PrimitiveControl Decode(GodState s, in Percept p, in BrainChoice c) =>
            Percept.ControlOf(s, in p, c.Control, c.Target, c.Source, c.Slot, c.Design, c.Gesture, c.R1, c.R2, c.Quantity);

        public MotorStatus Submit(GodState s, EventBuffer ev, in PrimitiveControl c, ControlOrigin origin) => Motor.Submit(s, ev, in c, origin);

        public bool Entity(in Percept p, int slot, out TargetKind kind, out int id, out int node)
        {
            if (slot < 0 || slot >= p.EntityCount || p.EntityKind == null) { kind = TargetKind.None; id = 0; node = -1; return false; }
            kind = Percept.TargetKindOf(p.EntityKind[slot]); id = p.EntityId[slot]; node = p.EntityNode[slot];
            return true;
        }

        public int PersonsNear(GodState s, int actor, int radius, int[] into, int[] distance)
        {
            var a = s.Population?.Find(actor);
            var ix = s.Unification?.Index;
            if (a == null || ix == null) return 0;
            int h = ix.Nearest(s, a.Node, radius, SpatialKind.Npc, _hits);
            int n = 0;
            for (int i = 0; i < h && n < into.Length; i++)
            {
                if (_hits[i].Id == actor) continue;
                var o = s.Population.Find(_hits[i].Id);
                if (o == null || !Bodies.IsLivingPerson(o)) continue;
                into[n] = _hits[i].Id; distance[n] = _hits[i].Distance; n++;
            }
            return n;
        }
    }
}
