using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// M4's sim-side hooks of the extension dispatch (SHARED_CONTRACTS 0.2), landed by F0 as no-op stubs. OWNER: M4.
    /// The call sites and their order are frozen (M3, then M4, then M5); only the bodies change. These are sim code and may
    /// read and write state; the scratch-only hooks Cognition and Policy call are on <see cref="HouseholdMindHooks"/> (gate G4.1).
    /// </summary>
    public static class HouseholdHooks
    {
        // ---------------- module entry points ----------------

        /// <summary>From <c>GodSim.Create</c> after the population started (founder families, households, kin cues).</summary>
        public static void StartGame(GodState s) { }

        /// <summary>From <c>GodSim</c>'s module installation (new game and loaded save): M4 adds its systems here (order 240, 255).</summary>
        public static void Install(GodSim sim) { }

        /// <summary>From <c>PopulationModule.AddNpc</c> after the NPC and its mind exist (stage, kin mind).</summary>
        public static void NewMind(GodState s, Npc npc) { }

        /// <summary>From <c>PopulationModule.Kill</c> after <c>NpcDied</c> (carry into the estate, leave the household).</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc) { }

        /// <summary>From <c>SimInvariants.Check</c>: M4's invariants, one line per broken rule.</summary>
        public static void Check(GodState s, List<string> bad) { }

        /// <summary>Fills M4's fields of a seen person (stage displays, same household). Sim side: may read state.</summary>
        public static void FillPerson(GodState s, Npc observer, Npc seen, ref SeenPerson p) { }

        // ---------------- sim side ----------------

        /// <summary>The episode key of an M4 decision (<see cref="EpisodeKey.Make"/> with kinds 1-7), 0 for none.</summary>
        public static int EpisodeKey(Npc npc, in Candidate c) => 0;

        /// <summary>Starts an M4 intent; true when handled.</summary>
        public static bool TryStart(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day) => false;

        /// <summary>One tick of an M4 intent; true when handled.</summary>
        public static bool TryAct(GodState s, EventBuffer ev, Npc npc, int day) => false;

        /// <summary>The time-use category of an M4 verb.</summary>
        public static int ActivityOf(byte verb) => Activity.Other;
    }

    /// <summary>
    /// M4's mind-side hooks (F0 stubs; OWNER: M4): the data rows the learning tables concatenate and the
    /// hooks Cognition and Policy call. They receive the think scratch only, never <see cref="GodState"/> (gate G4.1), so they
    /// live apart from the sim-side <see cref="HouseholdHooks"/>.
    /// </summary>
    public static class HouseholdMindHooks
    {
        // ---------------- data rows (concatenated after M3's in F0.11) ----------------

        /// <summary>M4's learning verbs (32-47, plus rows for 17 Store and 18 TakeFromStore).</summary>
        public static readonly ReadOnlyArray<VerbInfo> Verbs = new ReadOnlyArray<VerbInfo>();

        /// <summary>M4's instinct rules (appended to the default table).</summary>
        public static readonly ReadOnlyArray<Instinct> Instincts = new ReadOnlyArray<Instinct>();

        // ---------------- perception and cognition (scratch only) ----------------

        /// <summary>At most one base feature key, while the base has room (dispatch checks the count).</summary>
        public static void AddBase(ThinkScratch sc) { }

        /// <summary>M4's candidates (quota 4, after M3's 16), through <see cref="Thinker.AddCandidate"/>.</summary>
        public static void AddCandidates(ThinkScratch sc) { }

        /// <summary>The kin term of candidate <paramref name="cand"/> (Hamilton: KinWeight trait x r̂), clamped by the dispatch to ±2000.</summary>
        public static int KinTerm(ThinkScratch sc, int cand) => 0;

        /// <summary>The genome's instinct gain on a verb's prior.</summary>
        public static int InstinctGain(ThinkScratch sc, byte verb, int prior) => prior;

        /// <summary>Personality bias of an M4 verb.</summary>
        public static int Personality(byte verb, int[] traits) => 0;

        /// <summary>Words for an M4 feature value, or null.</summary>
        public static string Describe(uint f) => null;
    }
}
