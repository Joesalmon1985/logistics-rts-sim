using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Population.Households;
using God.Sim.Social;

namespace God.Sim.Population
{
    /// <summary>
    /// The extension dispatch of the population system (SHARED_CONTRACTS 0.2), frozen by F0: sim code, may read state.
    /// M4 first, then M5; for the try-calls the first that handles it wins.
    /// </summary>
    public static class PopulationExtensions
    {
        /// <summary>The episode key of a decision M3 does not key (0: none).</summary>
        public static int EpisodeKey(Npc npc, in Candidate c)
        {
            int k = HouseholdHooks.EpisodeKey(npc, in c);
            return k != 0 ? k : SocialHooks.EpisodeKey(npc, in c);
        }

        public static bool TryStart(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day) =>
            HouseholdHooks.TryStart(s, ev, npc, in c, day) || SocialHooks.TryStart(s, ev, npc, in c, day);

        public static bool TryAct(GodState s, EventBuffer ev, Npc npc, int day) =>
            HouseholdHooks.TryAct(s, ev, npc, day) || SocialHooks.TryAct(s, ev, npc, day);

        /// <summary>The time-use category of a verb M3 does not know (<see cref="Activity.Other"/> when neither knows it).</summary>
        public static int ActivityOf(byte verb)
        {
            int a = HouseholdHooks.ActivityOf(verb);
            return a != Activity.Other ? a : SocialHooks.ActivityOf(verb);
        }

        /// <summary>The fields of a seen person its owners fill (M4: stage displays, household; M5: gestures).</summary>
        public static void FillPerson(GodState s, Npc observer, Npc seen, ref SeenPerson p)
        {
            HouseholdHooks.FillPerson(s, observer, seen, ref p);
            SocialHooks.FillPerson(s, observer, seen, ref p);
        }

        /// <summary>
        /// I-BS amendment (SHARED_CONTRACTS 12.32), the Sense seam for bins: the bins in sight of <paramref name="npc"/> its
        /// owner perceives (M4: its household's store, estate goods), appended to the sensed things. Returns the new count.
        /// </summary>
        public static int SenseBins(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, int n) => HouseholdHooks.SenseBins(s, npc, sight, g, near, n);

        // ---- module entry points ----
        public static void StartGame(GodState s) { HouseholdHooks.StartGame(s); SocialHooks.StartGame(s); }
        public static void Install(GodSim sim) { HouseholdHooks.Install(sim); SocialHooks.Install(sim); }
        public static void NewMind(GodState s, Npc npc) { HouseholdHooks.NewMind(s, npc); SocialHooks.NewMind(s, npc); }
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc) { HouseholdHooks.OnDeath(s, ev, npc); SocialHooks.OnDeath(s, ev, npc); }
        public static void Check(GodState s, List<string> bad) { HouseholdHooks.Check(s, bad); SocialHooks.Check(s, bad); }
    }
}
