namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.5, D9.2): the twelve primitive controls of the workbook <c>NPC Actions</c> tab, the only way a unified
    /// body acts. Values are frozen (append-only: a new value is an amendment). Executor: <c>Motor</c> (UN2).
    /// </summary>
    public enum Prim : byte
    {
        None = 0, TurnLeft = 1, TurnRight = 2, StepForward = 3, StepBack = 4, StepLeft = 5, StepRight = 6,
        Take = 7, Drop = 8, Use = 9, Gesture = 10, Sleep = 11, Wait = 12,
    }

    /// <summary>UN0 (contracts 23.5): what a control acts on. <see cref="Placement"/>: Id = node, the design in <see cref="PrimitiveControl.Design"/>.</summary>
    public enum TargetKind : byte
    {
        None = 0, Self = 1, Npc = 2, Lot = 3, Node = 4, Facing = 5, Building = 6, Site = 7,
        Fixture = 8, ForagePlace = 9, RawNode = 10, WaterPoint = 11, Placement = 12,
    }

    /// <summary>UN0 (contracts 23.5): a typed target.</summary>
    public struct TargetRef
    {
        public TargetKind Kind;
        public int Id;

        public TargetRef(TargetKind kind, int id) { Kind = kind; Id = id; }
        public static TargetRef None => default;
        public static TargetRef Self => new TargetRef(TargetKind.Self, 0);
        public void AddTo(ref StateHasher h) { h.Add((int)Kind); h.Add(Id); }
    }

    /// <summary>The kinds of <see cref="Referent"/>: a ware, a lot, a person, a place (node).</summary>
    public static class ReferentKind
    {
        public const byte None = 0, Ware = 1, Lot = 2, Person = 3, Place = 4;
    }

    /// <summary>UN0 (contracts 23.5, 23.9): what a gesture refers to (<see cref="ReferentKind"/>).</summary>
    public struct Referent
    {
        public byte Kind;
        public int Id;
        public short Ware;

        public void AddTo(ref StateHasher h) { h.Add(Kind | (Ware << 8)); h.Add(Id); }
    }

    /// <summary>
    /// UN0 (contracts 23.5): one submitted control. <see cref="SourceLot"/> 0 = empty hand, else a lot the actor carries or has
    /// equipped; <see cref="Slot"/> the interaction slot 0-3 (0 = first matching row); <see cref="Design"/> the <see cref="CatId"/>
    /// of a known design (USE placement); gesture fields for GESTURE (quantity 0-999); <see cref="Corr"/> an optional correlation id
    /// (exchange) that nothing ever enforces.
    /// </summary>
    public struct PrimitiveControl
    {
        public Prim Control;
        public int Actor;
        public int SourceLot;
        public TargetRef Target;
        public byte Slot;
        public ushort Design;
        public byte Gesture;
        public Referent R1, R2;
        public short Quantity;
        public uint Corr;

        public const short MaxQuantity = 999;

        public void AddTo(ref StateHasher h)
        {
            h.Add((int)Control | (Slot << 8) | (Gesture << 16)); h.Add(Actor); h.Add(SourceLot); Target.AddTo(ref h);
            h.Add(Design); h.Add(Quantity); R1.AddTo(ref h); R2.AddTo(ref h); h.Add(Corr);
        }
    }

    /// <summary>UN0 (contracts 23.5): who issued a control. Recorded on events; never changes a physical result (QA-01).</summary>
    public enum ControlOrigin : byte { Policy = 0, Possessed = 1, Compelled = 2, Avatar = 3, Fixture = 4, Teacher = 5 }

    /// <summary>
    /// UN0 (contracts 23.5): the result of a submission, an observable proprioceptive input for the actor's next decision
    /// (failures emit <c>ControlFailed</c> 1221, UN2).
    /// </summary>
    public enum MotorStatus : byte
    {
        Accepted = 0, Busy, Invalid, Dead, Asleep, OutOfReach, Blocked, NoRoom, NotEnough,
        NoInteraction, NotKnown, BadSite, Occupied, Full, Resisted, Disabled,
    }

    /// <summary>UN0 (contracts 23.5): why an action in progress was interrupted (<c>Motor.Interrupt</c>, UN2).</summary>
    public static class InterruptReason
    {
        public const byte None = 0, Pain = 1, Collapse = 2, Strike = 3, Death = 4, Possession = 5, Divine = 6;
    }
}
