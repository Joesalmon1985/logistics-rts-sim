using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN3b's effect handlers, slice-1 subset: only <see cref="EffectKind.Eat"/>, so a person can relieve hunger through the one
    /// executor (USE a carried food on self). The physics are those of <c>unity/un3b</c> 1792da9's <c>EatHandler</c>; the
    /// <c>NeedRelieved</c> event (UN3b's event block) is not emitted here. <b>At UN3b's merge its full file replaces this one</b>
    /// (same path; take UN3b's version).
    /// </summary>
    public static partial class EffectRegistry
    {
        static partial void RegisterUN3b(EffectRegistrar r)
        {
            r.Register(EffectKind.Eat, new EatHandler());
        }
    }

    /// <summary>Eat (3): <c>USE</c> a carried food on self consumes one unit (<c>Eaten</c>) and lowers the channel (A, Hunger) by the object's <c>Nutrition</c>.</summary>
    public sealed class EatHandler : IEffectHandler
    {
        private static Npc Actor(GodState s, in EffectCall c) { var n = s.Population.Find(c.Actor); return n != null && n.Alive ? n : null; }

        private static bool Carried(Npc npc, int lot, out ItemLot l)
        {
            l = default;
            if (lot == 0 || npc.Carry == null) return false;
            foreach (var x in npc.Carry) if (!x.Empty && x.Id == lot) { l = x; return true; }
            return false;
        }

        public MotorStatus Validate(GodState s, in EffectCall c)
        {
            var npc = Actor(s, in c);
            if (npc == null) return MotorStatus.Dead;
            if (!Carried(npc, c.SourceLot, out var l)) return MotorStatus.Invalid;
            if (c.Effect.A < 0 || c.Effect.A >= NeedChannels.Count) return MotorStatus.Invalid;
            var o = s.Unification.Book.OfWare(l.Ware);
            if (o == null || o.Nutrition <= 0) return MotorStatus.NoInteraction; // raw fish and raw meat have nutrition 0 (G2)
            return MotorStatus.Accepted;
        }

        public MotorStatus Apply(GodState s, EventBuffer ev, in EffectCall c)
        {
            var st = Validate(s, in c);
            if (st != MotorStatus.Accepted) return st;
            var npc = Actor(s, in c);
            Carried(npc, c.SourceLot, out var l);
            var o = s.Unification.Book.OfWare(l.Ware);
            if (!Goods.Consume(s, ev, Holder.Carry(npc.Id), l.Id, 1, ConsumeReason.Eaten, npc.Id)) return MotorStatus.NotEnough;
            var u = s.Unification.PersonOf(npc.Id);
            if (u != null && u.Equipped == l.Id && Motor.CarrySlotOf(npc, l.Id) < 0) u.Equipped = 0;
            Needs.Apply(s, npc, (NeedChannel)c.Effect.A, -o.Nutrition);
            return MotorStatus.Accepted;
        }
    }
}
