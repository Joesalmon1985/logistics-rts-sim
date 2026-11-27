using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// C6's rows of the event catalogue (contracts 15.1, block 500-519, domains Organisation 45 and Service 46; LEAD_RULINGS
    /// R23/R36: 520-539 are C7's, 540-549 C2's). 500-512 are the contract's; 513-517 are appended by C6 from its spares (spec 10).
    /// Fields marked "truth" are analytics-only (never read by a rule, policy or mind). OWNER: C6.
    /// </summary>
    public static partial class OrgEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> C6 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(OrgFounded), 500, 1, TelemetryDomain.Organisation, "dim_org", note: "founded by individuals at a meeting (founders present, each accepted twice); opens dim_org and the founders' membership spells"),
            new EventInfo(typeof(OrgDissolved), 501, 1, TelemetryDomain.Organisation, "dim_org", note: "dissolved by its rule (last member gone, or a decision); its goods re-owned in place by the dissolution rule"),
            new EventInfo(typeof(OrgHeadChanged), 502, 1, TelemetryDomain.Organisation, "fact_org_membership", note: "an office changed holder (Office names which; the head office is the one flagged IsHead)"),
            new EventInfo(typeof(MemberJoined), 503, 1, TelemetryDomain.Organisation, "fact_org_membership", note: "a role taken: opens a membership spell"),
            new EventInfo(typeof(MemberLeft), 504, 1, TelemetryDomain.Organisation, "fact_org_membership", note: "a role ended (left, dismissed, died, dissolved): closes the spell"),
            new EventInfo(typeof(OrgSpent), 505, 1, TelemetryDomain.Organisation, "fact_org_expenditure", note: "an organisation's goods went out by an actor within reach (also the ordinary ItemMoved or Consumed)"),
            new EventInfo(typeof(ServiceRequested), 506, 1, TelemetryDomain.Service, "fact_service_request", note: "a request opened by the role-holder who perceived it (TrueNeed: truth)"),
            new EventInfo(typeof(ServiceAccepted), 507, 1, TelemetryDomain.Service, "fact_service_request", note: "the provider took the request"),
            new EventInfo(typeof(ServiceDelivered), 508, 1, TelemetryDomain.Service, "fact_service_delivery", note: "the provider's activity changed real state (OutcomeEvent: the code of that event)"),
            new EventInfo(typeof(ServiceOutcomeRecorded), 509, 1, TelemetryDomain.Service, "fact_service_outcome", note: "a recorder wrote the outcome as it knew it (TrueOutcome: truth)"),
            new EventInfo(typeof(ServiceRefused), 510, 1, TelemetryDomain.Service, "fact_service_request", note: "the provider refused for the reason it saw"),
            new EventInfo(typeof(OrgOpportunityAvailable), 511, 1, TelemetryDomain.Organisation, EventCatalog.Generic, true, note: "Full only: a person's own need evidence and practice made a founding candidate possible (creates nothing)"),
            new EventInfo(typeof(OrgDay), 512, 1, TelemetryDomain.Organisation, "fact_org_day", note: "the daily snapshot of one organisation (TrueUnmet: truth)"),
            new EventInfo(typeof(OrgDecisionTaken), 513, 1, TelemetryDomain.Organisation, "fact_org_decision", note: "a decision by a living decider or counted votes, with the counts as the counter saw them"),
            new EventInfo(typeof(OrgPolicySet), 514, 1, TelemetryDomain.Organisation, EventCatalog.Generic, note: "a policy record set by a decision (Decision 0: the founding charter)"),
            new EventInfo(typeof(RecordWritten), 515, 1, TelemetryDomain.Organisation, EventCatalog.Generic, note: "a role-holder wrote a record at the place (Falsified: truth)"),
            new EventInfo(typeof(OrgStatusChanged), 516, 1, TelemetryDomain.Organisation, "dim_org_history", note: "operating, dormant, dissolved"),
            new EventInfo(typeof(ServiceExpired), 517, 1, TelemetryDomain.Service, "fact_service_request", note: "a waiting request outlived its time"));
    }

    /// <summary>500.</summary>
    public sealed class OrgFounded : DomainEvent
    {
        public int Org;
        public OrgType Type;
        public OwnershipModel Ownership;
        public int Template;
        public int[] Founders;
        public int Place;
        public int OriginOpportunity;
        public int Store;
    }

    /// <summary>501. Reason: 1 last member gone, 2 decision.</summary>
    public sealed class OrgDissolved : DomainEvent
    {
        public const byte LastMemberGone = 1, ByDecision = 2;
        public int Org;
        public byte Reason;
        public DissolutionRule Rule;
    }

    /// <summary>502: an office changed holder (From/To NpcIds, 0 vacant).</summary>
    public sealed class OrgHeadChanged : DomainEvent
    {
        public int Org, Office;
        public OfficeKind Kind;
        public bool IsHead;
        public int From, To;
        public OfficeFilling Filling;
    }

    /// <summary>503.</summary>
    public sealed class MemberJoined : DomainEvent
    {
        public int Org, Npc;
        public RoleKind Role;
        public RoleBasis Basis;
    }

    /// <summary>504. Reason: 1 left, 2 dismissed, 3 died, 4 dissolved.</summary>
    public sealed class MemberLeft : DomainEvent
    {
        public const byte Left = 1, Dismissed = 2, Died = 3, Dissolved = 4;
        public int Org, Npc;
        public RoleKind Role;
        public byte Reason;
    }

    /// <summary>505.</summary>
    public sealed class OrgSpent : DomainEvent
    {
        public int Org, Actor;
        public LedgerCause Category;
        public Ware Ware;
        public int Count;
        public int Payee;
        public int Request;
        public byte Control;
    }

    /// <summary>506. TrueNeed: truth (the subject's real need when opened).</summary>
    public sealed class ServiceRequested : DomainEvent
    {
        public int Request, Org;
        public ServiceKind Kind;
        public int Requester;
        public SubjectKind SubjectKind;
        public int Subject, Place;
        public byte Control;
        public int ReceivedBy;
        public short TrueNeed;
    }

    /// <summary>507.</summary>
    public sealed class ServiceAccepted : DomainEvent
    {
        public int Request, Org, ProviderNpc;
    }

    /// <summary>508. OutcomeEvent: the catalogue code of the state-change event the activity produced (551 for a ration).</summary>
    public sealed class ServiceDelivered : DomainEvent
    {
        public int Request, Org, ProviderNpc, Ticks, GoodsUsed, OutcomeEvent;
        public byte Control;
    }

    /// <summary>509. TrueOutcome: truth.</summary>
    public sealed class ServiceOutcomeRecorded : DomainEvent
    {
        public int Request, Org, Recorder, RecordedOutcome, TrueOutcome;
    }

    /// <summary>510. ReasonSeen: 1 not a member as the provider saw it, 2 store empty as seen, 3 not in need as seen, 4 no provider.</summary>
    public sealed class ServiceRefused : DomainEvent
    {
        public const byte NotMember = 1, StoreEmpty = 2, NotInNeed = 3, NoProvider = 4;
        public int Request, Org, By;
        public byte ReasonSeen;
    }

    /// <summary>511 (Full only).</summary>
    public sealed class OrgOpportunityAvailable : DomainEvent
    {
        public int Npc, Template, NeedEvidence;
    }

    /// <summary>512: the daily snapshot (the day that ended). StoreWares/StoreCounts: the true contents of its stores per ware. TrueUnmet: truth.</summary>
    public sealed class OrgDay : DomainEvent
    {
        public int Org, Day;
        public OrgStatus Status;
        public int Members, Staff, OfficesVacant;
        public int[] StoreWares, StoreCounts;
        public int RequestsOpen, Served, Refused, Expired, Deposits, Records, Decisions;
        public int TrueUnmet;
    }

    /// <summary>513: Decision is the OrgDecision id; Outcome as counted; Accept, Refuse, Absent as the counter saw them.</summary>
    public sealed class OrgDecisionTaken : DomainEvent
    {
        public int Org, Decision, Proposer;
        public DecisionSource Source;
        public int SourceRef;
        public DecisionWhat What;
        public int A0, A1;
        public DecisionOutcome Outcome;
        public DecisionKind Rule;
        public int Accept, Refuse, Absent;
        public int Counter;
    }

    /// <summary>514.</summary>
    public sealed class OrgPolicySet : DomainEvent
    {
        public int Org;
        public OrgPolicyKey Key;
        public int Value, Before, Decision;
    }

    /// <summary>515. Falsified: truth.</summary>
    public sealed class RecordWritten : DomainEvent
    {
        public int Org, Recorder;
        public RecordKind Kind;
        public int Ref;
        public Ware Ware;
        public int Count;
        public bool Falsified;
    }

    /// <summary>516.</summary>
    public sealed class OrgStatusChanged : DomainEvent
    {
        public int Org;
        public OrgStatus From, To;
    }

    /// <summary>517.</summary>
    public sealed class ServiceExpired : DomainEvent
    {
        public int Request, Org;
    }
}
