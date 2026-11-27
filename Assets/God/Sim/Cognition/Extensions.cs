using System;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Households;
using God.Sim.Social;

namespace God.Sim.Cognition
{
    /// <summary>
    /// The verb ranges of the learning store (SHARED_CONTRACTS 6.1): which milestone a verb belongs to, for the dispatch of
    /// per-verb hooks (personality, time use). Frozen by F0.
    /// </summary>
    public static class VerbRanges
    {
        /// <summary>M4: 32-47, plus 17 <c>Store</c> and 18 <c>TakeFromStore</c>.</summary>
        public static bool IsHousehold(byte v) => v >= 32 && v <= 47 || v == Verb.Store || v == Verb.TakeFromStore;

        /// <summary>M5: 48-59 and <c>Say(act)</c> 64-95.</summary>
        public static bool IsSocial(byte v) => v >= 48 && v <= 59 || v >= 64 && v <= 95;
    }

    /// <summary>
    /// The extension dispatch of cognition (SHARED_CONTRACTS 0.2), frozen by F0: each call site in M3 code calls one method
    /// here, which calls M4's hook, then M5's, in that order. Hooks here see the think scratch only (gate G4.1).
    /// </summary>
    public static class CognitionExtensions
    {
        /// <summary>Candidates each later module may add after M3's 16.</summary>
        public const int CandidateQuota = 4;

        /// <summary>After attention: each seen person gets the identity the observer takes it to be (M5; perfect by default).</summary>
        public static void Recognise(ThinkScratch sc)
        {
            for (int i = 0; i < sc.PeopleCount; i++) sc.People[i].As = SocialMindHooks.Recognise(sc, sc.People[i].Id);
        }

        /// <summary>Before the candidates: the messages this mind decodes now (M5; none by default).</summary>
        public static void FillHeard(ThinkScratch sc)
        {
            sc.HeardCount = 0;
            SocialMindHooks.FillHeard(sc);
            if (sc.HeardCount < 0 || sc.HeardCount > ThinkScratch.MaxHeard) throw new InvalidOperationException("FillHeard: HeardCount out of range");
        }

        /// <summary>At the end of the base context, before its sort: M4 may add one key while the base has room. M5 adds none.</summary>
        public static void AddBase(ThinkScratch sc)
        {
            if (sc.BaseCount >= ThinkScratch.MaxBase) return;
            int before = sc.BaseCount;
            HouseholdMindHooks.AddBase(sc);
            if (sc.BaseCount > before + 1) throw new InvalidOperationException("AddBase: M4 may add at most one base key");
        }

        /// <summary>After M3's candidates, before costs: M4, then M5, each at most <see cref="CandidateQuota"/>.</summary>
        public static void AddCandidates(ThinkScratch sc)
        {
            sc.CandLimit = Math.Min(sc.CandCount + CandidateQuota, ThinkScratch.MaxCand);
            HouseholdMindHooks.AddCandidates(sc);
            sc.CandLimit = Math.Min(sc.CandCount + CandidateQuota, ThinkScratch.MaxCand);
            SocialMindHooks.AddCandidates(sc);
            sc.CandLimit = ThinkScratch.M3Cand;
        }

        /// <summary>The Social score term: <c>clamp(KinTerm, ±2000) + clamp(SocialTerm, ±2000)</c>.</summary>
        public static int SocialTerm(ThinkScratch sc, int cand) =>
            Fx.Clamp(HouseholdMindHooks.KinTerm(sc, cand), -2000, 2000) + Fx.Clamp(SocialMindHooks.SocialTerm(sc, cand), -2000, 2000);

        /// <summary>The instinct prior after M4's genome gain.</summary>
        public static int InstinctGain(ThinkScratch sc, byte verb, int prior) => HouseholdMindHooks.InstinctGain(sc, verb, prior);

        /// <summary>Personality of a verb M3 does not know, by verb range.</summary>
        public static int Personality(byte verb, int[] traits)
        {
            if (VerbRanges.IsHousehold(verb)) return HouseholdMindHooks.Personality(verb, traits);
            if (VerbRanges.IsSocial(verb)) return SocialMindHooks.Personality(verb, traits);
            return 0;
        }

        /// <summary>
        /// I-BS amendment (SHARED_CONTRACTS 12.31): whether the mind remembers a person out of sight, by the handle it would
        /// name in an intent: M4's believed kin, then M5's social memory. Used by <see cref="Thinker.Known"/> for person targets.
        /// </summary>
        public static bool Remembers(ThinkScratch sc, int person) => HouseholdMindHooks.Remembers(sc, person) || SocialMindHooks.Remembers(sc, person);

        /// <summary>
        /// F0 amendment (SHARED_CONTRACTS 12.37): whether a node target is one of the mind's own headings beyond M3's explore
        /// directions (M4's ranging walk, D4 M4.43). Used by <see cref="Thinker.Known"/> for node targets.
        /// </summary>
        public static bool OwnHeading(ThinkScratch sc, int node) => HouseholdMindHooks.OwnHeading(sc, node);

        /// <summary>Words for a feature M3 does not describe (null: none).</summary>
        public static string Describe(uint f) => HouseholdMindHooks.Describe(f) ?? SocialMindHooks.Describe(f);
    }

    /// <summary>The relationship class of a seen person (SHARED_CONTRACTS 4.4), frozen by F0.</summary>
    public static class Relations
    {
        /// <summary>
        /// M5's class when it gives one; otherwise the kin fallback: Partner if the observer believes it its partner,
        /// HouseholdMate if it shares the household, Kin if r̂ is at least 120, else Stranger.
        /// </summary>
        public static NpcRelClass ClassOf(ThinkScratch sc, in SeenPerson p)
        {
            byte m5 = SocialMindHooks.RelationClass(sc, in p);
            if (m5 != 0) return (NpcRelClass)m5;
            if (KinData.RoleOf(sc.Mind, p.As) == KinRole.Partner) return NpcRelClass.Partner;
            if (p.SameHousehold) return NpcRelClass.HouseholdMate;
            if (KinData.RHat(sc.Mind, p.As) >= 120) return NpcRelClass.Kin;
            return NpcRelClass.Stranger;
        }
    }
}
