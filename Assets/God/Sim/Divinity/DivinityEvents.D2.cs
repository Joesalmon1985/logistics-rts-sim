namespace God.Sim.Divinity
{
    /// <summary>
    /// D2's rows of the event catalogue (contracts 15.1, codes 810-824, domain Divine 60). OWNER: D2. Every effect event
    /// carries <c>Intervention</c>. <c>Mismatch</c> fields are analytics truth only (never read by a rule or a policy).
    /// </summary>
    public static partial class DivinityEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> D2 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(DivineSuggestionIssued), 810, 1, TelemetryDomain.Divine, "fact_divine_command", note: "one per accepted suggestion: act, target, scope, recipients"),
            new EventInfo(typeof(DivineCommandIssued), 811, 1, TelemetryDomain.Divine, "fact_divine_command", note: "one per accepted command: act, target, scope, recipients"),
            new EventInfo(typeof(InstructionRelayed), 812, 1, TelemetryDomain.Divine, "fact_divine_command_target", note: "one per recipient an organisation-scope instruction is relayed to (faction stand-in until C6)"),
            new EventInfo(typeof(CommandPerceived), 813, 1, TelemetryDomain.Divine, "fact_command_perception", true, note: "one per perceiving recipient (decoded act and topic; Mismatch is analytics truth)"),
            new EventInfo(typeof(CommandResponded), 814, 1, TelemetryDomain.Divine, "fact_command_response", note: "exactly one per perceiver: Followed, Ignored, Refused, Lapsed, Misread"),
            new EventInfo(typeof(CompulsionStarted), 815, 1, TelemetryDomain.Divine, "fact_compulsion", note: "a compulsion's control spell opened (Control.TryAcquire)"),
            new EventInfo(typeof(CompulsionEnded), 816, 1, TelemetryDomain.Divine, "fact_compulsion", note: "once per spell: Completed, Timeout, Cancelled, Death, MythicExhausted, Superseded"),
            new EventInfo(typeof(CompulsionBlocked), 817, 1, TelemetryDomain.Divine, "fact_compulsion", note: "the compelled act is physically impossible at a think (each transition into Blocked)"),
            new EventInfo(typeof(DivineSignPerceived), 819, 1, TelemetryDomain.Divine, "fact_command_perception", true, note: "a witness within gesture range attending a command's recipient saw the sign"));
    }

    /// <summary>810: a suggestion was accepted and delivered.</summary>
    public sealed class DivineSuggestionIssued : DomainEvent
    {
        public int Intervention, God;
        public DivineAct Act;
        public DivineTargetKind TargetKind;
        public int TargetId, TargetNode, TargetRadius;
        public DivineScope Scope;
        public int Recipients, NotReached;
        public long Expires;
        /// <summary>The person named by an individual target (0 otherwise) and the target's node (for the HUD's cue).</summary>
        public int Npc, Node;
    }

    /// <summary>811: a command was accepted and delivered.</summary>
    public sealed class DivineCommandIssued : DomainEvent
    {
        public int Intervention, God;
        public DivineAct Act;
        public DivineTargetKind TargetKind;
        public int TargetId, TargetNode, TargetRadius;
        public DivineScope Scope;
        public int Recipients, NotReached;
        public long Expires;
        /// <summary>The person named by an individual target (0 otherwise) and the target's node (for the HUD's cue).</summary>
        public int Npc, Node;
    }

    /// <summary>812: one recipient an organisation-scope instruction was relayed to.</summary>
    public sealed class InstructionRelayed : DomainEvent
    {
        public int Intervention, God, Org, Npc;
    }

    /// <summary>813 (HV): a recipient perceived an instruction and decoded it into its inbox.</summary>
    public sealed class CommandPerceived : DomainEvent
    {
        public int Intervention, Npc, God;
        public InterventionKind Kind;
        public DivineAct DecodedAct;
        public Comms.TopicKind TopicKind;
        public int TopicId;
        public byte Clarity;
        /// <summary>Perceived on waking (a dream or vision).</summary>
        public bool OnWaking;
        /// <summary>The ids an evicted older entry had (0 none).</summary>
        public int Evicted;
        /// <summary>Analytics truth only: the decoded act or topic differs from the god's.</summary>
        public bool Mismatch;
        /// <summary>What it heard, in words (the decoded act and topic; the person's belief).</summary>
        public string Heard;
    }

    /// <summary>814: the one response of a perceiver.</summary>
    public sealed class CommandResponded : DomainEvent
    {
        public int Intervention, Npc, God;
        public InterventionKind Kind;
        public CommandResponse Response;
        /// <summary>The learning verb it chose at that think (0: none, or at expiry).</summary>
        public byte Verb;
        /// <summary>The chosen decision's divine term (vp).</summary>
        public int Divine;
        public long DecidedTick;
        /// <summary>The act as this person understood it (its belief), when it differs from the god's (null otherwise).</summary>
        public string Understood;
    }

    /// <summary>815: a compulsion's control spell opened.</summary>
    public sealed class CompulsionStarted : DomainEvent
    {
        public int Intervention, God, Npc;
        public DivineAct Act;
        public DivineTargetKind TargetKind;
        public int TargetId, TargetNode, TargetRadius;
        public long Expires;
    }

    /// <summary>816: a compulsion's spell ended (exactly once per spell, CT1).</summary>
    public sealed class CompulsionEnded : DomainEvent
    {
        public int Intervention, God, Npc;
        public ControlEndReason Reason;
        /// <summary>The compelled intent had started at least once.</summary>
        public bool Started;
        public long SinceTick;
    }

    /// <summary>817: the compelled act could not be built at a think (physically impossible now): it retries at each think.</summary>
    public sealed class CompulsionBlocked : DomainEvent
    {
        public int Intervention, God, Npc;
        public DivineAct Act;
        /// <summary>Why, in words (the act cannot be built from what the body holds, knows and can carry now).</summary>
        public string Reason;
    }

    /// <summary>819 (HV): a witness saw a command's sign at its recipient.</summary>
    public sealed class DivineSignPerceived : DomainEvent
    {
        public int Intervention, Witness, Recipient, Node;
    }
}
