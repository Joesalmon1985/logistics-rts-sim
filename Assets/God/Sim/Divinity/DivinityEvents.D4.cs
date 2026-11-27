namespace God.Sim.Divinity
{
    /// <summary>
    /// D4's rows of the event catalogue (contracts 15.1, codes 845-849, domain Possession 61). OWNER: D4. Every event carries
    /// <c>Intervention</c>. A possession's start and end pair exactly once (CT1).
    /// </summary>
    public static partial class DivinityEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> D4 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(PossessionStarted), 845, 1, TelemetryDomain.Possession, "fact_possession", note: "a possession's control spell opened (Control.TryAcquire)"),
            new EventInfo(typeof(PossessionEnded), 846, 1, TelemetryDomain.Possession, "fact_possession", note: "once per spell: Cancelled, Death, MythicExhausted, Superseded"),
            new EventInfo(typeof(PossessedActionStarted), 847, 1, TelemetryDomain.Possession, "fact_possession", true, note: "a possessed body started an act chosen by the god (its decision record carries Control = Possessed)"));
    }

    /// <summary>845: a god took control of a living person.</summary>
    public sealed class PossessionStarted : DomainEvent
    {
        public int Intervention, God, Npc, Faction, Node;
        /// <summary>The compulsion on the body this possession ended (<c>Superseded</c>), 0 none.</summary>
        public int SupersededCompulsion;
        /// <summary>The god's avatar manifestation this possession dismissed (<c>Superseded</c>), 0 none.</summary>
        public int SupersededAvatar;
    }

    /// <summary>846: a possession ended (exactly once per spell). The person is the same individual, id and record.</summary>
    public sealed class PossessionEnded : DomainEvent
    {
        public int Intervention, God, Npc, Node;
        public ControlEndReason Reason;
        public long StartTick, Ticks;
        public int InputsApplied, InputsRefused, Actions;
    }

    /// <summary>847 (HV): a possessed body started an act at the god's input (<see cref="ActionTick"/>: the think that started it).</summary>
    public sealed class PossessedActionStarted : DomainEvent
    {
        public int Intervention, Npc;
        public byte Verb;
        public Policy.IntentTarget TargetKind;
        public int TargetId, TargetNode;
        public long ActionTick;
    }
}
