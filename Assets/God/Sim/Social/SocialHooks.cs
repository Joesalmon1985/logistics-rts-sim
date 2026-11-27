using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Social
{
    /// <summary>
    /// M5's sim-side hooks of the extension dispatch (SHARED_CONTRACTS 0.2). OWNER: M5. The call sites and their order are
    /// frozen (M3, then M4, then M5); the bodies are M5's (<see cref="SocialSim"/>). These are sim code and may read and
    /// write state; the scratch-only hooks Cognition and Policy call are on <see cref="SocialMindHooks"/> (gate G4.1).
    /// With <c>Social.Enabled = 0</c> the state stays null and every hook returns its default.
    /// </summary>
    public static class SocialHooks
    {
        // ---------------- module entry points ----------------

        public static void StartGame(GodState s) => SocialSim.StartGame(s);
        public static void Install(GodSim sim) => SocialSim.Install(sim);
        public static void NewMind(GodState s, Npc npc) => SocialSim.NewMind(s, npc);
        /// <summary>From <c>PopulationModule.Kill</c>, after M4's <c>OnDeath</c>.</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc) { if (s.Social != null) SocialSim.OnDeath(s, ev, npc); }
        public static void Check(GodState s, List<string> bad) => SocialSim.Check(s, bad);

        /// <summary>Fills M5's fields of a seen person (recognition, a visible gesture). Sim side: may read state.</summary>
        public static void FillPerson(GodState s, Npc observer, Npc seen, ref SeenPerson p) { if (s.Social != null) SocialSim.FillPerson(s, observer, seen, ref p); }

        // ---------------- sim side ----------------

        public static int EpisodeKey(Npc npc, in Candidate c) => SocialSim.EpisodeKeyOf(npc, in c);
        public static bool TryStart(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day) => s.Social != null && SocialSim.TryStart(s, ev, npc, in c, day);
        public static bool TryAct(GodState s, EventBuffer ev, Npc npc, int day) => s.Social != null && SocialSim.TryAct(s, ev, npc, day);
        public static int ActivityOf(byte verb) => SocialSim.ActivityOf(verb);

        /// <summary>A person-to-person hand-over, observed by both parties (called by <c>Goods.Move</c>, A3).</summary>
        public static void OnHandOver(GodState s, EventBuffer ev, in HandOver h) => SocialSim.OnHandOver(s, ev, in h);

        /// <summary>Two claimants of one ownerless asset in one tick (M4 calls it): the winner. M5 keeps the stub rule (lower id, nothing emitted, 13.2): island contests are not built in M5.</summary>
        public static int Contest(GodState s, EventBuffer ev, int a, int b, byte assetKind, int assetId) => a < b ? a : b;
    }

    /// <summary>
    /// M5's mind-side hooks (OWNER: M5): the data rows the learning tables concatenate and the hooks Cognition and Policy
    /// call. They receive the think scratch only, never <see cref="GodState"/> (gate G4.1): what they read is the mind's
    /// own social memory and the scratch's view of itself, the people it sees and what it heard.
    /// </summary>
    public static class SocialMindHooks
    {
        // ---------------- data rows (concatenated after M3's and M4's in F0.11) ----------------

        /// <summary>M5's learning verbs (48-59, and Say(act) = 64 + act for 64-95).</summary>
        public static readonly ReadOnlyArray<VerbInfo> Verbs = SocialRows.Verbs();

        /// <summary>M5's instinct rules (appended after M4's).</summary>
        public static readonly ReadOnlyArray<Instinct> Instincts = SocialRows.Instincts();

        // ---------------- perception and cognition (scratch only) ----------------

        /// <summary>Who the observer takes a seen person to be (recognition, drawn by the sim side before the think). Default: perfect.</summary>
        public static int Recognise(ThinkScratch sc, int trueId)
        {
            var m = sc.Mind?.Social;
            if (m == null || m.RecTick != sc.Self.Tick) return trueId;
            int a = m.RecognisedAs(trueId);
            return a >= 0 ? a : trueId;
        }

        /// <summary>
        /// Fills <c>sc.Heard</c> with the messages this mind has read and not yet answered (newest first, at most 4): only
        /// those heard before this tick (SHARED_CONTRACTS 7.1 rule 3). None with gestures off.
        /// </summary>
        public static void FillHeard(ThinkScratch sc)
        {
            var m = sc.Mind?.Social;
            if (m == null || m.Tuning == null || m.Tuning.Enabled == 0 || m.Tuning.Gestures == 0) return;
            for (int i = m.InboxCount - 1; i >= 0 && sc.HeardCount < ThinkScratch.MaxHeard; i--)
            {
                var e = m.Inbox[i];
                if (!e.Read || e.Answered || e.Msg.Tick >= sc.Self.Tick) continue;
                sc.Heard[sc.HeardCount++] = e.Msg;
            }
        }

        /// <summary>I-BS amendment (12.31): a person with a record in its social memory is remembered (a person target out of sight is then known).</summary>
        public static bool Remembers(ThinkScratch sc, int person)
        {
            var m = sc.Mind?.Social;
            return m != null && person != 0 && m.Find(person) >= 0;
        }

        /// <summary>M5's candidates (quota 4, after M4's).</summary>
        public static void AddCandidates(ThinkScratch sc) => SocialCandidates.Build(sc);

        /// <summary>
        /// The social term of a candidate (clamped by the dispatch to ±2000): trust, grudge, affection and the believed
        /// surplus of a deal, from this mind's own records about the person as it recognises them (SOCIAL 5, ECONOMY 4.5
        /// acceptance rule: surplus minus the risk trust leaves). Kin is never read here (M4's KinTerm carries it, 4.2).
        /// </summary>
        public static int SocialTerm(ThinkScratch sc, int cand)
        {
            var m = sc.Mind?.Social;
            if (m == null || m.Tuning == null || m.Tuning.Term == 0) return 0;
            int pi = m.PendingOf(cand);
            if (pi < 0) return 0;
            var p = m.Pending[pi];
            int ri = m.Find(p.As);
            int trust = ri >= 0 ? m.Rel[ri].Trust : 0, aff = ri >= 0 ? m.Rel[ri].Affection : 0, grudge = ri >= 0 ? m.Rel[ri].Grudge : 0;
            bool mate = ri >= 0 && (m.Rel[ri].Flags & RelFlags.HouseholdMate) != 0;
            switch (p.Verb)
            {
                case SocialVerb.SayAccept:
                {
                    // risk: what I may lose if I go first and they do not, discounted by trust
                    int di = m.FindDeal(p.DealId);
                    int atRisk = di >= 0 && m.Deals[di].MyWare != Ware.None ? m.Deals[di].MyCount * Valuation.PriceOf(m.Price, m.Deals[di].MyWare) : 0;
                    int risk = atRisk * (1000 - Fx.Clamp(trust, -1000, 1000)) / 2000;
                    return p.Surplus - risk - grudge / 2;
                }
                case SocialVerb.SayRefuse: return -p.Surplus / 2 + grudge / 4;
                case SocialVerb.SayOffer: return p.Surplus / 2 + trust / 5 - grudge / 2;
                case SocialVerb.HandOver: return trust / 3 - grudge / 2 + (p.Surplus > 0 ? 200 : 0);
                case SocialVerb.Give: return aff / 4 + (mate ? 150 : 0) - grudge / 2 + (ri >= 0 ? Fx.Clamp(m.Rel[ri].IOwe / 2, 0, 300) : 0);
                case SocialVerb.SayCooperate: return aff / 4 + trust / 5 + (mate ? 100 : 0) - grudge / 2;
                case SocialVerb.SayNeedFood: case SocialVerb.SayNeedHelp: return trust / 4 + aff / 4 - grudge / 2;
                default: return 0;
            }
        }

        /// <summary>Trait biases of M5's verbs: generosity gives, honesty hands over its side, trustfulness accepts.</summary>
        public static int Personality(byte verb, int[] traits)
        {
            switch (verb)
            {
                case SocialVerb.Give: return traits[Trait.Generosity];
                case SocialVerb.SayCooperate: return traits[Trait.Generosity] / 2;
                case SocialVerb.HandOver: return traits[Trait.Honesty];
                case SocialVerb.SayAccept: return traits[Trait.Trustfulness] / 2;
                case SocialVerb.SayRefuse: return -traits[Trait.Trustfulness] / 2;
                case SocialVerb.SayWarn: return traits[Trait.Boldness] / 2;
                default: return 0;
            }
        }

        /// <summary>Plain words for M5's feature values (the inspector).</summary>
        public static string Describe(uint f)
        {
            int v = Feature.ValueOf(f);
            switch (Feature.KindOf(f))
            {
                case FeatureKind.NpcRel: return "with " + Article((NpcRelClass)v);
                case FeatureKind.NpcRef: return "with person #" + v;
                case FeatureKind.NpcGroup: return v == NpcGroupValue.OwnFaction ? "with my own people" : v == NpcGroupValue.OtherFaction ? "with outsiders" : "group " + v;
                case FeatureKind.MsgAct: return "told: " + ((GestureAct)v).ToString().ToLowerInvariant();
                default: return null;
            }
        }

        private static string Article(NpcRelClass c)
        {
            switch (c)
            {
                case NpcRelClass.Stranger: return "a stranger";
                case NpcRelClass.Acquaintance: return "an acquaintance";
                case NpcRelClass.Friend: return "a friend";
                case NpcRelClass.Kin: return "kin";
                case NpcRelClass.Partner: return "my partner";
                case NpcRelClass.HouseholdMate: return "a household mate";
                case NpcRelClass.Enemy: return "an enemy";
                case NpcRelClass.Rival: return "a rival";
                case NpcRelClass.Debtor: return "someone who owes me";
                case NpcRelClass.Creditor: return "someone I owe";
                default: return c.ToString().ToLowerInvariant();
            }
        }

        /// <summary>The relationship class M5 gives a seen person (<see cref="NpcRelClass"/>), 0 to leave it to the kin fallback (no record).</summary>
        public static byte RelationClass(ThinkScratch sc, in SeenPerson p)
        {
            var m = sc.Mind?.Social;
            if (m == null) return 0;
            if (p.As == 0) return (byte)NpcRelClass.Stranger; // not recognised: a stranger now
            int ri = m.Find(p.As);
            if (ri < 0) return 0;
            var c = SocialMemory.ClassOf(m, m.Rel[ri]);
            if (c == NpcRelClass.Stranger && p.SameHousehold) return (byte)NpcRelClass.HouseholdMate;
            return (byte)c;
        }
    }
}
