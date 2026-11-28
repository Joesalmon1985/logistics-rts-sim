namespace God.Sim.Unified
{
    /// <summary>UN2's rows of the event catalogue (contracts 23.12, block 1220-1239, domain Motor). OWNER: UN2.</summary>
    public static partial class UnifiedEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        // Table "generic" until UN11a lands fact_control in migration 0044 (the intended columns are in the DEV_LOG UN2 record).
        private static ReadOnlyArray<EventInfo> UN2 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(ControlApplied), 1220, 1, TelemetryDomain.Motor, EventCatalog.Generic, true,
                "a control accepted by the motor (Full recording; intended table fact_control)"),
            new EventInfo(typeof(ControlFailed), 1221, 1, TelemetryDomain.Motor, EventCatalog.Generic, true,
                "a control refused by a physical check, or a step blocked at completion (intended table fact_control)"),
            new EventInfo(typeof(ActionInterrupted), 1222, 1, TelemetryDomain.Motor, EventCatalog.Generic),
            new EventInfo(typeof(Equipped), 1223, 1, TelemetryDomain.Motor, EventCatalog.Generic, false, "emitted by UN8a's Equip effect"),
            new EventInfo(typeof(Unequipped), 1224, 1, TelemetryDomain.Motor, EventCatalog.Generic, false, "emitted by UN8a's Equip effect"));
    }

    /// <summary>1220: the motor accepted a control (contracts 23.5). <see cref="Origin"/> is recorded, never a physical input.</summary>
    public sealed class ControlApplied : DomainEvent
    {
        public int Actor;
        public Prim Control;
        public ControlOrigin Origin;
        public MotorStatus Status;
        public TargetKind TargetKind;
        public int TargetId;
        /// <summary>The ware of the source or moved lot (0 none).</summary>
        public short Ware;
    }

    /// <summary>1221: the motor refused a control, or an action failed at completion; <see cref="Status"/> is the physical reason.</summary>
    public sealed class ControlFailed : DomainEvent
    {
        public int Actor;
        public Prim Control;
        public ControlOrigin Origin;
        public MotorStatus Status;
    }

    /// <summary>1222: an action in progress was cut short (<see cref="InterruptReason"/>); an unfinished USE applied nothing.</summary>
    public sealed class ActionInterrupted : DomainEvent
    {
        public int Actor;
        public Prim Control;
        public byte Reason;
        public int TicksDone;
        public ControlOrigin Origin;
    }

    /// <summary>1223: a carried lot was equipped (emitted by UN8a's Equip effect).</summary>
    public sealed class Equipped : DomainEvent
    {
        public int Actor;
        public int Lot;
        public short Ware;
        public ControlOrigin Origin;
    }

    /// <summary>1224: an equipped lot was set aside (emitted by UN8a).</summary>
    public sealed class Unequipped : DomainEvent
    {
        public int Actor;
        public int Lot;
        public short Ware;
        public ControlOrigin Origin;
    }
}
