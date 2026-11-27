namespace God.Sim.Orgs
{
    /// <summary>
    /// C7's rows of the event catalogue (contracts 15.1 block 520-539, Health 32 and Education 33). 520-523 are the contract's;
    /// 524 is appended from C7's spares (spec 10). Fields marked "truth" are analytics-only (never read by a rule, policy or
    /// mind). OWNER: C7.
    /// </summary>
    public static partial class OrgEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> C7 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(TreatmentStarted), 520, 1, TelemetryDomain.Health, "fact_health_encounter", note: "a healer took a patient (informal Org 0, or a practice's request): skill, remedy lot, its diagnosis"),
            new EventInfo(typeof(TreatmentEnded), 521, 1, TelemetryDomain.Health, "fact_health_encounter", note: "the treatment finished or stopped (before/after: truth)"),
            new EventInfo(typeof(TeachingSession), 522, 1, TelemetryDomain.Education, "fact_education_enrolment", note: "a teacher and a learner held a session (Teach, Demonstrate, Apprentice)"),
            new EventInfo(typeof(KnowledgeTransferred), 523, 1, TelemetryDomain.Education, "fact_education_assessment", note: "associations copied (Origin.Imitated or Vicarious) and the learner's skill before and after"),
            new EventInfo(typeof(HealerSkillChanged), 524, 1, TelemetryDomain.Education, "fact_education_assessment", note: "a person's skill changed (Skill: the SkillClass; by treating, a recovery, or being taught)"));
    }

    /// <summary>520. SeenNeed: the healer's diagnosis (0-3, from its perception and skill); Control: divine control of the healer.</summary>
    public sealed class TreatmentStarted : DomainEvent
    {
        public int Request, Org, Healer, Patient, Skill, RemedyLot, SeenNeed;
        public byte Control;
    }

    /// <summary>521. Outcome: 1 completed, 2 not needed any more, 3 patient gone or dead, 4 healer stopped. Before/after fields: truth.</summary>
    public sealed class TreatmentEnded : DomainEvent
    {
        public const byte Completed = 1, NotNeeded = 2, PatientGone = 3, Stopped = 4;
        public int Request, Healer, Patient, Ticks;
        public int SeverityBefore, SeverityAfter, InjuryBefore, InjuryAfter, HealthBefore, HealthAfter;
        public byte Outcome;
        /// <summary>The treated multipliers applied (permille): severity loss and injury recovery (0 unless completed).</summary>
        public int SeverityPermille, InjuryPermille;
        public bool Remedy;
    }

    /// <summary>522. Kind 1 Teach, 2 Demonstrate, 3 Apprentice.</summary>
    public sealed class TeachingSession : DomainEvent
    {
        public const byte Teach = 1, Demonstrate = 2, Apprentice = 3;
        public int Teacher, Learner, Topic, Ticks, Org;
        public byte Kind, Control;
    }

    /// <summary>523. Fidelity permille; SkillBefore/After exact.</summary>
    public sealed class KnowledgeTransferred : DomainEvent
    {
        public int Teacher, Learner, Topic, EntriesCopied, Fidelity, SkillBefore, SkillAfter;
        public byte Kind;
    }

    /// <summary>524. Cause 1 treated, 2 a patient recovered, 3 taught, 4 supervised practice.</summary>
    public sealed class HealerSkillChanged : DomainEvent
    {
        public const byte Treated = 1, Recovered = 2, Taught = 3, Supervised = 4;
        public int Npc, Skill, From, To;
        public byte Cause;
    }
}
