using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Social
{
    /// <summary>
    /// M5's sim-side hooks of the extension dispatch (SHARED_CONTRACTS 0.2), landed by F0 as no-op stubs. OWNER: M5.
    /// The call sites and their order are frozen (M3, then M4, then M5); only the bodies change. These are sim code and may
    /// read and write state; the scratch-only hooks Cognition and Policy call are on <see cref="SocialMindHooks"/> (gate G4.1).
    /// </summary>
    public static class SocialHooks
    {
        // ---------------- module entry points ----------------

        public static void StartGame(GodState s) { }
        public static void Install(GodSim sim) { }
        public static void NewMind(GodState s, Npc npc) { }
        /// <summary>From <c>PopulationModule.Kill</c>, after M4's <c>OnDeath</c>.</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc) { }
        public static void Check(GodState s, List<string> bad) { }

        /// <summary>Fills M5's fields of a seen person (a visible gesture). Sim side: may read state.</summary>
        public static void FillPerson(GodState s, Npc observer, Npc seen, ref SeenPerson p) { }

        // ---------------- sim side ----------------

        public static int EpisodeKey(Npc npc, in Candidate c) => 0;
        public static bool TryStart(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day) => false;
        public static bool TryAct(GodState s, EventBuffer ev, Npc npc, int day) => false;
        public static int ActivityOf(byte verb) => Activity.Other;

        /// <summary>A person-to-person ownership transfer by hand, observed by both parties (called by <c>Goods.Move</c>).</summary>
        public static void OnHandOver(GodState s, EventBuffer ev, in HandOver h) { }

        /// <summary>Two claimants of one ownerless asset in one tick (M4 calls it): the winner. Stub: the lower id, nothing emitted (13.2).</summary>
        public static int Contest(GodState s, EventBuffer ev, int a, int b, byte assetKind, int assetId) => a < b ? a : b;
    }

    /// <summary>
    /// M5's mind-side hooks (F0 stubs; OWNER: M5): the data rows the learning tables concatenate and the
    /// hooks Cognition and Policy call. They receive the think scratch only, never <see cref="GodState"/> (gate G4.1), so they
    /// live apart from the sim-side <see cref="SocialHooks"/>.
    /// </summary>
    public static class SocialMindHooks
    {
        // ---------------- data rows (concatenated after M3's and M4's in F0.11) ----------------

        /// <summary>M5's learning verbs (48-59, and Say(act) = 64 + act for 64-95).</summary>
        public static readonly ReadOnlyArray<VerbInfo> Verbs = new ReadOnlyArray<VerbInfo>();

        /// <summary>M5's instinct rules (appended after M4's).</summary>
        public static readonly ReadOnlyArray<Instinct> Instincts = new ReadOnlyArray<Instinct>();

        // ---------------- perception and cognition (scratch only) ----------------

        /// <summary>Who the observer takes a seen person to be (recognition). Default: perfect.</summary>
        public static int Recognise(ThinkScratch sc, int trueId) => trueId;

        /// <summary>Fills <c>sc.Heard</c> with the messages this mind decodes now. Default: none.</summary>
        public static void FillHeard(ThinkScratch sc) { }

        /// <summary>M5's candidates (quota 4, after M4's).</summary>
        public static void AddCandidates(ThinkScratch sc) { }

        /// <summary>The social term of candidate <paramref name="cand"/> (trust, obligations, affection), clamped by the dispatch to ±2000.</summary>
        public static int SocialTerm(ThinkScratch sc, int cand) => 0;

        public static int Personality(byte verb, int[] traits) => 0;
        public static string Describe(uint f) => null;

        /// <summary>The relationship class M5 gives a seen person (<see cref="NpcRelClass"/>), 0 to leave it to the kin fallback.</summary>
        public static byte RelationClass(ThinkScratch sc, in SeenPerson p) => 0;
    }
}
