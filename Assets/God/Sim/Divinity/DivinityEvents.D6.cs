namespace God.Sim.Divinity
{
    /// <summary>
    /// D6's rows of the event catalogue (contracts 15.1, codes 860-879, domains Commandment 65 and Religion 66). OWNER: D6.
    /// <c>VariantMask</c> fields are analytics truth (never read by a rule or a policy). 870 PrayerMade and 871 WorshipHeld stay
    /// unallocated until prayer and worship land (D6b remainder); 872-879 spare.
    /// </summary>
    public static partial class DivinityEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> D6 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(CommandmentIssued), 860, 1, TelemetryDomain.Commandment, "fact_commandment", note: "one per revealed commandment: template, importance, rules, recipients"),
            new EventInfo(typeof(CommandmentRevised), 861, 1, TelemetryDomain.Commandment, "fact_commandment_revision", note: "one per revision (id kept, Revision bumped)"),
            new EventInfo(typeof(CommandmentRevoked), 862, 1, TelemetryDomain.Commandment, "fact_commandment_revision", note: "one per revocation (knowledge is never erased)"),
            new EventInfo(typeof(CommandmentLearned), 863, 1, TelemetryDomain.Commandment, "fact_commandment_knowledge", note: "a person gained a known entry or a newer revision of one (source, from whom; VariantMask is analytics truth)"),
            new EventInfo(typeof(CommandmentStanceChanged), 864, 1, TelemetryDomain.Commandment, "fact_commandment_knowledge", note: "a known entry moved between Heard, Learned, Adopted and Rejected"),
            new EventInfo(typeof(CommandmentForgotten), 865, 1, TelemetryDomain.Commandment, "fact_commandment_knowledge", note: "a known entry left the mind (salience 0 or evicted)"),
            new EventInfo(typeof(CommandmentChoiceMade), 866, 1, TelemetryDomain.Commandment, "fact_commandment_choice", true, note: "a person started an action its held interpretation weighed (the term it carried)"),
            new EventInfo(typeof(ReligionFounded), 867, 1, TelemetryDomain.Religion, "fact_religion", note: "one per religion (Tier 2: one founding religion per god)"),
            new EventInfo(typeof(AffiliationChanged), 868, 1, TelemetryDomain.Religion, "fact_affiliation", note: "an affiliation spell opened or closed; citizenship and community carried unchanged"),
            new EventInfo(typeof(BeliefChanged), 869, 1, TelemetryDomain.Religion, "fact_god_belief", true, note: "one person's five fields about one god moved, with the perceived cause"));
    }

    /// <summary>860: a god revealed a commandment.</summary>
    public sealed class CommandmentIssued : DomainEvent
    {
        public int Intervention, Commandment, God, Template, Importance, Rules;
        public DivineTargetKind TargetKind;
        public int TargetId, TargetNode, TargetRadius;
        public int Recipients, NotReached;
        /// <summary>The recipient named by a person target (0 otherwise) and its node, for the HUD's cue.</summary>
        public int Npc, Node;
    }

    /// <summary>861: a god revised a commandment (it keeps its id).</summary>
    public sealed class CommandmentRevised : DomainEvent
    {
        public int Intervention, Commandment, God, Revision, Importance, Rules, Recipients;
    }

    /// <summary>862: a god revoked a commandment.</summary>
    public sealed class CommandmentRevoked : DomainEvent
    {
        public int Intervention, Commandment, God, Revision, Recipients;
    }

    /// <summary>863: a person learned a commandment (a new entry, or a newer revision of one it held).</summary>
    public sealed class CommandmentLearned : DomainEvent
    {
        public int Npc, Commandment, Revision, God;
        public CommandmentSource Source;
        public int FromNpc;
        public int Salience, Adoption;
        /// <summary>The revelation's intervention (0 when told by a person).</summary>
        public int Intervention;
        /// <summary>True when it replaced an older revision it held.</summary>
        public bool Update;
        /// <summary>Analytics truth: the fields of the interpretation that differ from the revision as issued (bit 0 modality, 1 verb, 2 object, 3 applies, 4 condition, 5 scope).</summary>
        public int VariantMask;
    }

    /// <summary>864: a known entry changed state.</summary>
    public sealed class CommandmentStanceChanged : DomainEvent
    {
        public int Npc, Commandment;
        public CommandmentStance From, To;
        public int Adoption, Salience;
        /// <summary>What moved it: 1 day pass (beliefs), 2 exposure, 3 experience, 4 told of a revocation, 5 learned.</summary>
        public int Cause;
    }

    /// <summary>865: a known entry left a person's mind.</summary>
    public sealed class CommandmentForgotten : DomainEvent
    {
        public int Npc, Commandment, Revision;
        /// <summary>True when a newer entry took its slot (cap 8); false when its salience reached 0.</summary>
        public bool Evicted;
    }

    /// <summary>866 (HV): a person started an action its held interpretation of a commandment weighed.</summary>
    public sealed class CommandmentChoiceMade : DomainEvent
    {
        public int Npc, Commandment;
        public Modality Modality;
        public byte Verb;
        public int TargetId;
        /// <summary>The commandment part of the divine term the chosen candidate carried (vp).</summary>
        public int Term;
    }

    /// <summary>867: a religion was founded.</summary>
    public sealed class ReligionFounded : DomainEvent
    {
        public int Religion, God, Parent;
    }

    /// <summary>868: an affiliation spell opened (<see cref="Opened"/>) or closed. Citizenship and community are carried to show they did not change.</summary>
    public sealed class AffiliationChanged : DomainEvent
    {
        public int Npc, Religion, God;
        public bool Opened;
        /// <summary>1 founder, 2 converted, 3 lapsed, 4 left on conversion.</summary>
        public int Reason;
        /// <summary>The person's community of origin (<c>Npc.Faction</c>) and citizenship stand-in (the faction until C5) at the change: unchanged by it.</summary>
        public int Community, Citizenship;
    }

    /// <summary>869 (HV): one person's beliefs about one god moved, from something it perceived.</summary>
    public sealed class BeliefChanged : DomainEvent
    {
        public int Npc, God;
        /// <summary>1 instruction, 2 healed, 3 smitten, 4 given goods, 5 sign, 6 smite witnessed, 7 heal witnessed, 8 founder seed, 9 daily decay.</summary>
        public int Cause;
        /// <summary>The intervention (or event) behind it, 0 none.</summary>
        public int CauseRef;
        public int DBelief, DFaith, DTrust, DFear, DLoyalty;
        public int Belief, Faith, Trust, Fear, Loyalty;
    }
}
