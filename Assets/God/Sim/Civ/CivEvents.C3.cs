using God.Sim.Population;

namespace God.Sim.Civ
{
    /// <summary>
    /// C3's rows of the event catalogue (contracts 15.1, block 250-269, domain Employment). 250-253 are the contract's;
    /// 254-256 are appended from C3's spares (spec 10; owner Q19 for the suspension pair). 257-269 spare. OWNER: C3.
    /// </summary>
    public static partial class CivEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> C3 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(EmploymentStarted), 250, 1, TelemetryDomain.Employment, "fact_employment", note: "an offer of work accepted by gesture: the contract opens its spell and the worker's Work right on the asset"),
            new EventInfo(typeof(WagePaid), 251, 1, TelemetryDomain.Employment, "fact_employment_event", note: "the agreed pay handed over within reach (also the ordinary ItemMoved HandOver and OwnershipChanged with basis Wage)"),
            new EventInfo(typeof(EmploymentEnded), 252, 1, TelemetryDomain.Employment, "fact_employment", note: "a contract closed: resigned, dismissed, died, asset gone, abandoned"),
            new EventInfo(typeof(JobDay), 253, 1, TelemetryDomain.Employment, "fact_job_day", note: "one per open contract per day: work ticks, output, paid and unpaid units"),
            new EventInfo(typeof(ContractSuspended), 254, 1, TelemetryDomain.Employment, "fact_employment_event", note: "owner Q19: a walk-out suspends the contract (no work, no pay due, slot kept)"),
            new EventInfo(typeof(ContractResumed), 255, 1, TelemetryDomain.Employment, "fact_employment_event", note: "a suspended contract taken up again by agreement"),
            new EventInfo(typeof(WageUnpaid), 256, 1, TelemetryDomain.Employment, "fact_employment_event", note: "a pay unit past its grace without pay: arrears grow (a counter; a formal obligation once C2 is merged)"));
    }

    /// <summary>250. Per: 1 shift, 2 output unit, 3 task. Control: the worker's control at the acceptance (0 own).</summary>
    public sealed class EmploymentStarted : DomainEvent
    {
        public int Contract;
        public OwnerKind EmployerKind;
        public int EmployerId;
        public int Agent, Worker, Asset;
        public PayUnitKind PayUnit;
        public int PayRef, PayAmount;
        public PayPer Per;
        public int ShiftTicks;
        public byte Control;
    }

    /// <summary>251: <see cref="Employment.Pay"/> with purpose Wage. ForUnits: how many pay units the hand-over settles.</summary>
    public sealed class WagePaid : DomainEvent
    {
        public int Contract, Payer, Worker;
        public Ware Ware;
        public int Count, ForUnits, Lot;
        public byte Control;
    }

    /// <summary>252. Reason: 1 resigned, 2 dismissed, 3 died, 4 asset gone, 5 abandoned.</summary>
    public sealed class EmploymentEnded : DomainEvent
    {
        public int Contract, Worker;
        public EndReason Reason;
        public byte By;
        public int ArrearsUnits, UnitsEarned, UnitsPaid;
    }

    /// <summary>253: the day <see cref="Day"/> of one contract open on that day.</summary>
    public sealed class JobDay : DomainEvent
    {
        public int Contract, Worker, Day;
        public int WorkTicks, OutputCount, PaidUnits, UnpaidUnits;
        public ContractStatus Status;
    }

    /// <summary>254. By: 1 the worker (a walk-out), 2 the employer. Reason: 1 unpaid, 2 other.</summary>
    public sealed class ContractSuspended : DomainEvent
    {
        public const byte Unpaid = 1, Other = 2;
        public int Contract;
        public byte By, Reason;
        public int ArrearsUnits;
    }

    /// <summary>255. By: who took it up again (the worker accepting the old or renegotiated terms).</summary>
    public sealed class ContractResumed : DomainEvent
    {
        public int Contract;
        public byte By;
        public int PayAmount;
    }

    /// <summary>256: Units newly unpaid, Arrears the total now, Obligation the C2 record (0: C2 absent, a counter only).</summary>
    public sealed class WageUnpaid : DomainEvent
    {
        public int Contract, Worker, Units, Arrears, Obligation;
    }
}
