using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN1 (contracts 23.7, spec 5.4): the handlers of <see cref="EffectKind.Forage"/> and <see cref="EffectKind.DrinkWash"/>. Both are
    /// stateless; both fail only for physical reasons (reach, room, the target's kind). The time a collection takes is the matched row's
    /// <c>Ticks</c> (the executor, UN2, counts it; a fixture counts it the same way); the place's own <c>Ticks</c> are what the generator
    /// wrote into the row. Forage never changes a place and needs no building, tool or dice.
    /// </summary>
    public static partial class EffectRegistry
    {
        static partial void RegisterUN1(EffectRegistrar r)
        {
            r.Register(EffectKind.Forage, new ForageHandler());
            r.Register(EffectKind.DrinkWash, new DrinkWashHandler());
        }
    }

    /// <summary>Forage: <c>USE</c> empty hand on a <see cref="ForagePlace"/> produces one lot (reason Foraged) into the actor's carry.</summary>
    public sealed class ForageHandler : IEffectHandler
    {
        static int ReachOf(in EffectCall c) => c.Row != null ? c.Row.Reach : Reach.Touch;

        public MotorStatus Validate(GodState s, in EffectCall c)
        {
            var npc = s.Population.Find(c.Actor);
            if (npc == null || !npc.Alive) return MotorStatus.Dead;
            if (c.Target.Kind != TargetKind.ForagePlace) return MotorStatus.Invalid;
            var place = World.ForagePlaceById(s.Unification, c.Target.Id);
            if (place == null) return MotorStatus.Invalid;
            if (c.SourceLot != 0) return MotorStatus.NoInteraction; // forage is done with an empty hand
            if (s.Territory.Grid.Distance(npc.Node, place.Node) > ReachOf(in c)) return MotorStatus.OutOfReach;
            if (PopulationModule.Room(s, npc, (Ware)place.Ware) <= 0) return MotorStatus.NoRoom;
            return MotorStatus.Accepted;
        }

        public MotorStatus Apply(GodState s, EventBuffer ev, in EffectCall c)
        {
            var st = Validate(s, in c);
            if (st != MotorStatus.Accepted) return st;
            var npc = s.Population.Find(c.Actor);
            var u = s.Unification;
            var place = World.ForagePlaceById(u, c.Target.Id);
            var ware = (Ware)place.Ware;
            int idx = HexTypes.ForageIndex(ware);
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            int days = idx >= 0 ? s.Tuning.Unification.UN1.ForageSpoilDays[idx] : 3;
            int lot = Goods.Produce(s, ev, Holder.Carry(npc.Id), ware, 1, day + days, 0, Owner.None, ProduceReason.Foraged, npc.Id, place.Id);
            if (lot == 0) return MotorStatus.NoRoom; // never dropped silently
            UnifiedGoods.SetSource(s, lot, OriginSource.ForagePlace, place.Id);
            if (idx >= 0) u.ForagedEver[idx]++;
            ev?.Add(new ForageCollected { Actor = npc.Id, Place = place.Id, Node = place.Node, Ware = ware, Ticks = c.Row != null ? c.Row.Ticks : place.Ticks, Origin = (byte)c.Origin });
            return MotorStatus.Accepted;
        }
    }

    /// <summary>
    /// DrinkWash: <c>USE</c> empty hand on a <see cref="WaterPoint"/>. <c>Effect.A</c> 0 = drink (needs a fresh point; lowers Thirst), 1 = wash
    /// (fresh or salt; lowers Hygiene). No lot is created and no container is needed (G3).
    /// </summary>
    public sealed class DrinkWashHandler : IEffectHandler
    {
        public const int Drink = 0, Wash = 1;

        static int ReachOf(in EffectCall c) => c.Row != null ? c.Row.Reach : Reach.Touch;

        public MotorStatus Validate(GodState s, in EffectCall c)
        {
            var npc = s.Population.Find(c.Actor);
            if (npc == null || !npc.Alive) return MotorStatus.Dead;
            if (c.Target.Kind != TargetKind.WaterPoint) return MotorStatus.Invalid;
            var w = World.WaterPointById(s.Unification, c.Target.Id);
            if (w == null) return MotorStatus.Invalid;
            if (c.SourceLot != 0) return MotorStatus.NoInteraction;
            if (c.Effect.A != Drink && c.Effect.A != Wash) return MotorStatus.Invalid;
            if (c.Effect.A == Drink && !w.Fresh) return MotorStatus.NoInteraction; // salt water: wash only
            if (s.Territory.Grid.Distance(npc.Node, w.Node) > ReachOf(in c)) return MotorStatus.OutOfReach;
            return MotorStatus.Accepted;
        }

        public MotorStatus Apply(GodState s, EventBuffer ev, in EffectCall c)
        {
            var st = Validate(s, in c);
            if (st != MotorStatus.Accepted) return st;
            var npc = s.Population.Find(c.Actor);
            var w = World.WaterPointById(s.Unification, c.Target.Id);
            var t = s.Tuning.Unification.UN1;
            if (c.Effect.A == Drink) Needs.Apply(s, npc, NeedChannel.Thirst, -t.DrinkRelief);
            else Needs.Apply(s, npc, NeedChannel.Hygiene, -t.WashRelief);
            ev?.Add(new WaterUsed { Actor = npc.Id, Point = w.Id, Fresh = w.Fresh, Slot = (byte)c.Effect.A, Origin = (byte)c.Origin });
            return MotorStatus.Accepted;
        }
    }
}
