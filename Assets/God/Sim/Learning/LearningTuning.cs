namespace God.Sim.Learning
{
    /// <summary>
    /// Every number of the learning core (NPC_LEARNING_DESIGN, defaults of sections 3-4), on <c>SimTuning.Learning</c>:
    /// saved and hashed with the game, settable from scenarios (<c>Learning.TraceShift</c>, <c>Learning.DecayScalePermille</c>).
    /// Decay is per sim day and independent of <c>LifeScale</c> (owner decision Q1).
    /// </summary>
    /// <summary>The credit rule of the learning core (see <see cref="Learner.Step"/>).</summary>
    public enum CreditRule : byte { TdLambda = 0, MonteCarlo = 1 }

    public sealed class LearningTuning : IOmitWhenDefault
    {
        // ---- caps (3.3): 256 entries, 64 conjunctions, 16 traces, 8 episodes, 32 day moments ----
        public int Entries = 256;
        public int Conjunctions = 64;
        public int Traces = 16;
        public int Episodes = 8;
        public int Moments = 32;

        /// <summary>The mode every NPC starts in (a scenario may set single NPCs otherwise).</summary>
        public LearningMode DefaultMode = LearningMode.On;

        // ---- credit (4.5) ----
        /// <summary>Eligibility falls by <c>E &gt;&gt; TraceShift</c> per think (2: x0.75). 16 disables traces (credit only to the decision just made).</summary>
        public int TraceShift = 2;
        /// <summary>The shift for patient NPCs (Patience at least <see cref="PatientFrom"/>).</summary>
        public int PatientTraceShift = 3;
        public int PatientFrom = 40;
        /// <summary>A trace whose eligibility falls below this closes and updates the store.</summary>
        public int CloseBelowE = 32;
        /// <summary>Episodes (4.7) are on; 0 closes a deferred trace like any other (the "episodes off" arm of the tests).</summary>
        public bool EpisodesOn = true;
        public int EpisodeExpireDays = 10;

        // ---- the update rule (4.6) ----
        public int AlphaShift = 2;
        /// <summary>How reward reaches the traces: TD(lambda) errors (default) or the raw reward with the error taken at close (Monte Carlo).</summary>
        public CreditRule Credit = CreditRule.TdLambda;
        /// <summary>The per-think discount of the next decision's value in the TD error (970 = a horizon of about 33 thinks).</summary>
        public int GammaPermille = 0;
        /// <summary>The TD error bootstraps from the best candidate's learned value (Q-learning style, default) or the chosen one's (SARSA).</summary>
        public bool BootstrapBest = true;
        /// <summary>The learned term's gain in the score (1024 = x1).</summary>
        public int LearnGain = 1024;
        public int MomentThreshold = 300;
        /// <summary>Rescorla-Wagner cue salience of the update: drive features and conjunctions, target features, the rest. All 1: the equal split of 4.6.</summary>
        public int SalienceDrive = 4, SalienceTarget = 2, SalienceBase = 1;
        /// <summary>No entry is inserted for an update smaller than this (noise must not fill the store).</summary>
        public int MinInsertDelta = 4;

        // ---- consolidation and forgetting (4.8, 4.11, 4.12) ----
        /// <summary>Scales the nightly decay of untouched entries (1000 = about 3% a day; 0 stops forgetting). Not scaled by LifeScale (Q1).</summary>
        public int DecayScalePermille = 1000;
        /// <summary>Entries lose <c>Use &gt;&gt; UseDecayShift</c> each night.</summary>
        public int UseDecayShift = 3;
        /// <summary>Conjunction slots lose <c>Strength &gt;&gt; ConjDecayShift</c> (at least 1) each night.</summary>
        public int ConjDecayShift = 3;
        public int RecruitPerNight = 4;
        /// <summary>Recruitment weighs a pair of a drive and a target feature (what was acted on) this many times a drive and a base feature.</summary>
        public int RecruitTargetWeight = 2;
        public int ConjStartStrength = 256;
        public int ConjReplaceBelow = 128;
        public int ConjGain = 64;
        /// <summary>Eviction keeps this much headroom after sleep.</summary>
        public int EvictHeadroom = 16;
        public int RecencyDays = 30;

        // ---- exploration (4.2) ----
        public int ExploreBase = 150;
        public int ExploreMax = 600;
        public int NoveltyBonus = 200;

        // ---- reward channels (4.4), value points ----
        public int WHunger = 1000;
        public int WFatigue = 700;
        public int WCold = 700;
        public int WFear = 900;
        public int WPain = 1200;
        public int Frustration = -50;
        public int BedGained = 400;
        public int BedLost = -400;
        public int Injury = -300;

        // ---- telemetry ----
        /// <summary><c>AssociationUpdated</c> is emitted for every update with |delta| at least 1000, and for the NPC-days with <c>hash(npc, day) % SampleEvery == 0</c>.</summary>
        public int SampleEvery = 16;

        public LearningCaps Caps => new LearningCaps(Entries, Conjunctions, Traces, Episodes, Moments);

        public int W(int drive)
        {
            switch (drive)
            {
                case Drive.Hunger: return WHunger;
                case Drive.Fatigue: return WFatigue;
                case Drive.Cold: return WCold;
                case Drive.Fear: return WFear;
                case Drive.Pain: return WPain;
                default: return 0;
            }
        }
    }
}
