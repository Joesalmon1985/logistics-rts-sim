namespace God.Sim.Orgs
{
    /// <summary>
    /// C7's numbers (<c>Orgs.C7</c>, OWNER C7; the contracts call the healthcare part <c>Orgs.Health</c>): healthcare
    /// (INSTITUTIONAL_SERVICES 5.1) and education (5.2). Written only when changed (<see cref="IOmitWhenDefault"/>), so a game
    /// that never sets them hashes as before. With <see cref="StartHealers"/> 0 and nobody taught, nobody knows the healing
    /// practice and C7 never acts (the pins stay neutral).
    /// </summary>
    public sealed partial class OrgC7Tuning
    {
        // ---- the start culture (a scenario sets it; it creates no organisation) ----
        /// <summary>Adults per faction (lowest ids) who start knowing the healing practice with <see cref="StartHealerSkill"/>; everyone of their faction believes them healers.</summary>
        public int StartHealers;
        public int StartHealerSkill = 300;

        // ---- the skill store (R42: CivState) ----
        /// <summary>At most this many skill classes per person.</summary>
        public int SkillSlots = 4;

        // ---- treatment (SERVICES 5.1) ----
        /// <summary>Ticks within <c>Reach.Touch</c> a treatment takes (M4's <c>TendTicks</c> by default).</summary>
        public int TreatTicks = 150;
        /// <summary>Severity term: the tended multiplier is scaled by <c>(1000 - Skill x SkillGain/1000 - Remedy)/1000</c>.</summary>
        public int SkillGain = 500;
        /// <summary>The treated severity multiplier never falls below this (effectiveness never 100%).</summary>
        public int TreatFloorPermille = 300;
        /// <summary>Injury term: recovery on a treated day is multiplied by <c>(InjuryBase + Skill x InjuryGain/1000 + Remedy)</c> permille.</summary>
        public int InjuryBasePermille = 2000;
        public int InjuryGain = 1000;
        /// <summary>The remedy term (one lot of herbs used).</summary>
        public int RemedyPermille = 150;
        /// <summary>Diagnosis noise at skill 0 (display-level units, +/-); falls linearly to 0 at skill 1000.</summary>
        public int DiagnoseNoise = 2;
        /// <summary>Healing skill per completed treatment, and extra when the patient recovers within <see cref="RecoveryDays"/>.</summary>
        public int SkillPerTreatment = 20, SkillPerRecovery = 30, RecoveryDays = 3;
        /// <summary>Reward (value points) to the healer for a completed treatment (M4's <c>TendRewardVp</c> by default).</summary>
        public int TreatRewardVp = 80;
        /// <summary>A patient asks the same believed healer again only after this many ticks.</summary>
        public int AskCooldownTicks = 300;
        /// <summary>After asking, the patient waits for the healer at most this long (its own act; its thinks may drop it).</summary>
        public int AskWaitTicks = 300;
        /// <summary>Ticks an informal request lives in the provider's mind.</summary>
        public int RequestTicks = 900;

        // ---- herbs (R22: gathered at Roots sources) ----
        public int HerbTicks = 40;
        public int HerbSpoilDays = 30;
        /// <summary>A healer gathers herbs while it carries fewer than this.</summary>
        public int HerbKeep = 2;

        // ---- the healer's practice (template HealerPractice, SVC-ACC-4) ----
        /// <summary>Informal treatments given before a healer may found a practice of its own.</summary>
        public int FoundAfterTreatments = 3;
        /// <summary>... and at least this healing skill.</summary>
        public int FoundMinSkill = 300;

        // ---- education (SERVICES 5.2, C7b) ----
        /// <summary>Teacher and learner within gesture range for this many ticks.</summary>
        public int TeachTicks = 60;
        /// <summary>Skill rise = <c>TeachSkillGain x fidelity / 1000</c>, capped below the teacher's.</summary>
        public int TeachSkillGain = 300;
        /// <summary>At most this many associations copied per session.</summary>
        public int TeachMaxEntries = 8;
        public int MaxLearnersPerSession = 4;
        /// <summary>A teacher offers again to the same learner only after this many ticks.</summary>
        public int TeachCooldownTicks = 1200;
        /// <summary>A teacher teaches a topic only with at least this skill in it.</summary>
        public int TeachMinSkill = 200;
        /// <summary>An offer to teach stays open this long in the learner's mind.</summary>
        public int TeachOfferTicks = 300;
        /// <summary>Demonstration: the observer's vicarious value is the demonstrator's value x this / 1000.</summary>
        public int DemonstratePermille = 500;
        /// <summary>Apprenticeship: supervision bonus (skill per completed work cycle of the topic while the master is in sight).</summary>
        public int SupervisionGain = 5;
    }
}
