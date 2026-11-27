namespace God.Sim.Divinity
{
    /// <summary>
    /// D1's rows of the event catalogue (contracts 15.1, codes 800-809, domain Divine 60). OWNER: D1. A changed field
    /// meaning takes a new code; an added field bumps the version. Mythic amounts are milli-Mythic; <c>Amount</c> is always
    /// what moved the balance (so <c>MythicTrace</c> reconstructs it in every mode), <c>Nominal</c> what it would have cost.
    /// </summary>
    public static partial class DivinityEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> D1 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(GodCreated), 800, 1, TelemetryDomain.Divine, "dim_god", note: "one per god on the first divinity tick (G4)"),
            new EventInfo(typeof(GodAssociated), 801, 1, TelemetryDomain.Divine, "fact_god_faction", note: "a god's association with a faction opens (its origin faction at creation)"),
            new EventInfo(typeof(GodPolicyChanged), 802, 1, TelemetryDomain.Divine, "dim_god_history", note: "seat, policy or observation profile of a god changed by a configuration command"),
            new EventInfo(typeof(MythicConfigChanged), 803, 1, TelemetryDomain.Divine, "dim_god_history", note: "SetMythicMode: closes every god's history row and opens one with the new mode"),
            new EventInfo(typeof(MythicCredited), 804, 1, TelemetryDomain.Divine, "fact_mythic_ledger", note: "daily regeneration (or the top-up to max when Unlimited starts); capped excess reported"),
            new EventInfo(typeof(MythicSpent), 805, 1, TelemetryDomain.Divine, "fact_mythic_ledger", note: "one spend: nominal cost, charged amount (0 when waived), balance after"),
            new EventInfo(typeof(MythicUpkeepCharged), 806, 1, TelemetryDomain.Divine, "fact_mythic_ledger", true, note: "daily aggregate per god and spell kind"),
            new EventInfo(typeof(DivineActionAccepted), 807, 1, TelemetryDomain.Divine, "fact_divine_action", note: "one per intervention (Interventions.Open)"),
            new EventInfo(typeof(DivineObservationAnswered), 808, 1, TelemetryDomain.Divine, "fact_divine_observation", note: "Perceived/Reported answers (Tier 3): never emitted in this run (Omniscient only)"),
            new EventInfo(typeof(MythicBalanceDay), 809, 1, TelemetryDomain.Divine, "fact_mythic_balance_day", note: "one per god per finished day, at the next day start"));
    }

    /// <summary>800: a god exists (emitted once, on the first divinity tick).</summary>
    public sealed class GodCreated : DomainEvent
    {
        public int God;
        public string Name;
        public Faction OriginFaction;
        public GodSeat Seat;
        public string Policy;
        public long Mythic, MythicMax;
        public DivinityMode Mode;
    }

    /// <summary>801: a god's association with a faction opens.</summary>
    public sealed class GodAssociated : DomainEvent
    {
        public int God;
        public Faction Faction;
    }

    /// <summary>802: a configuration command changed who drives a god or how it observes.</summary>
    public sealed class GodPolicyChanged : DomainEvent
    {
        public int God;
        public GodSeat Seat, PreviousSeat;
        public string Policy, PreviousPolicy;
        public Information.DivinePerception Profile;
        /// <summary>The god whose seat issued the command (0: none).</summary>
        public int By;
    }

    /// <summary>803: the Mythic mode changed.</summary>
    public sealed class MythicConfigChanged : DomainEvent
    {
        public DivinityMode Mode, Previous;
        public int By;
    }

    /// <summary>804: Mythic credited to a god (<c>Source</c> 0 daily regeneration, 1 the top-up when Unlimited starts).</summary>
    public sealed class MythicCredited : DomainEvent
    {
        public int God;
        public long Amount, Capped, Balance;
        public long Day;
        public byte Source;
    }

    /// <summary>805: one spend through <see cref="Mythic.TrySpend"/>.</summary>
    public sealed class MythicSpent : DomainEvent
    {
        public int God, Intervention;
        public InterventionKind Kind;
        /// <summary>What was charged (0 when <see cref="Waived"/>).</summary>
        public long Amount;
        public long Nominal;
        public bool Waived;
        public long Balance;
    }

    /// <summary>806 (HV): one god's upkeep of one spell kind over one day.</summary>
    public sealed class MythicUpkeepCharged : DomainEvent
    {
        public int God;
        public InterventionKind Kind;
        public long Day;
        public int Ticks;
        public long Amount, Waived;
    }

    /// <summary>807: an accepted divine action (one per InterventionId).</summary>
    public sealed class DivineActionAccepted : DomainEvent
    {
        public int Intervention, God;
        public InterventionKind Kind;
        public DivineAct Act;
        public DivineTargetKind TargetKind;
        public int TargetId, TargetNode, TargetRadius;
        public DivineScope Scope;
        public long Cost, Charged;
    }

    /// <summary>808 (Full): a non-omniscient observation answered (reserved: not emitted in this run).</summary>
    public sealed class DivineObservationAnswered : DomainEvent
    {
        public int God;
        public byte Domain, Channel;
        public int Subject;
    }

    /// <summary>809: one god's Mythic over one finished day.</summary>
    public sealed class MythicBalanceDay : DomainEvent
    {
        public int God;
        public long Day;
        public long Balance, Max, Credited, Capped, Spent, Waived, Upkeep;
        public int Actions;
        public DivinityMode Mode;
    }
}
