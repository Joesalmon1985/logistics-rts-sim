namespace God.Sim.Unified
{
    /// <summary>1200 (HV): a forager took one lot from a place (<c>Goods.Produce</c> reason Foraged); <see cref="Ticks"/> the place's collect time.</summary>
    public sealed class ForageCollected : DomainEvent
    {
        public int Actor, Place, Node;
        public Ware Ware;
        public int Ticks;
        public byte Origin;
    }

    /// <summary>1201: a dice roll matched a hex and added <see cref="Amount"/> to its pending allowance (<see cref="Pending"/> after).</summary>
    public sealed class AllowanceGranted : DomainEvent
    {
        public int Hex, Roll, Amount, Pending;
    }

    /// <summary>1202: allowance beyond the cap was wasted.</summary>
    public sealed class AllowanceWasted : DomainEvent
    {
        public int Hex, Roll, Amount;
    }

    /// <summary>1203 (HV): <see cref="Units"/> moved from a hex's pending allowance to a raw node (<see cref="Stock"/> after).</summary>
    public sealed class RawMaterialised : DomainEvent
    {
        public int Hex, Node, Units, Stock;
        public Ware Ware;
    }

    /// <summary>1204 (HV): a person drank (<see cref="Slot"/> 0) or washed (1) at a water point; no lot exists.</summary>
    public sealed class WaterUsed : DomainEvent
    {
        public int Actor, Point;
        public bool Fresh;
        public byte Slot, Origin;
    }

    /// <summary>1205: the daily environment books: day, living persons, allowance pending and raw stock summed over all hexes, foods foraged so far by forage index.</summary>
    public sealed class UnifiedDay : DomainEvent
    {
        public int Day, Alive, Pending, Stock;
        public int[] Foraged;
    }

    /// <summary>UN1's rows of the event catalogue (contracts 23.12, block 1200-1219). OWNER: UN1.</summary>
    public static partial class UnifiedEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> UN1 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(ForageCollected), 1200, 1, TelemetryDomain.UnifiedWorld, EventCatalog.Generic, true),
            new EventInfo(typeof(AllowanceGranted), 1201, 1, TelemetryDomain.UnifiedWorld, EventCatalog.Generic),
            new EventInfo(typeof(AllowanceWasted), 1202, 1, TelemetryDomain.UnifiedWorld, EventCatalog.Generic),
            new EventInfo(typeof(RawMaterialised), 1203, 1, TelemetryDomain.UnifiedWorld, EventCatalog.Generic, true),
            new EventInfo(typeof(WaterUsed), 1204, 1, TelemetryDomain.UnifiedWorld, EventCatalog.Generic, true),
            new EventInfo(typeof(UnifiedDay), 1205, 1, TelemetryDomain.UnifiedWorld, EventCatalog.Generic));
    }
}
