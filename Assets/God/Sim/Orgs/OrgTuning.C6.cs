namespace God.Sim.Orgs
{
    /// <summary>
    /// C6's numbers (<c>Orgs.C6</c>, OWNER C6): the organisation framework. Written only when changed (<see cref="IOmitWhenDefault"/>),
    /// so a game that never sets them hashes as before. The per-template numbers (founders, offices, charter) are rows of
    /// <see cref="OrgTemplates"/> (data), not tuning.
    /// </summary>
    public sealed partial class OrgC6Tuning
    {
        /// <summary>
        /// The practices (bit per <see cref="OrgTemplates"/> id) every adult of the starting population knows (its culture at the
        /// start, set by a scenario). 0: nobody knows any practice; people still learn one by being told or by seeing an organisation.
        /// It makes a founding candidate possible; it creates nothing (ORG-INV-6).
        /// </summary>
        public int StartPractices;
        /// <summary>Need evidence (signal days, own experience) at which a founding candidate exists (spec 4.1).</summary>
        public int NeedThreshold = 3;
        /// <summary>Days without a need signal after which one unit of evidence is forgotten.</summary>
        public int EvidenceDecayDays = 3;
        /// <summary>A believed supporter needs at least this trust in the believer's own social memory (ORGANISATIONS 4.1, default 300).</summary>
        public int SupportTrust = 300;
        /// <summary>Ticks before the same supporter is asked again by the same proposer.</summary>
        public int ProposeCooldownTicks = 400;
        /// <summary>Ticks an acceptance of a proposal stays valid (ProposalWindowDays in ticks).</summary>
        public int ProposalWindowTicks = 2400;
        /// <summary>Ticks a founding meeting stays open for the founders' second acceptance.</summary>
        public int MeetingTicks = 60;
        /// <summary>Days with no living office-holder with Propose before an organisation is Dormant (ORGANISATIONS 4.13).</summary>
        public int DormantDays = 5;
        /// <summary>Ticks a waiting service request lives before it expires.</summary>
        public int RequestExpireTicks = 600;
        /// <summary>Days a MajorityPresent proposal waits for a meeting with its proposer present before it lapses.</summary>
        public int ProposalWindowDays = 3;
        /// <summary>The recorder writes at most this many witnessed entries per act.</summary>
        public int RecordsPerAct = 4;
    }
}
