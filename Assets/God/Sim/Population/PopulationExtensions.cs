using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Population.Households;
using God.Sim.Social;
using God.Sim.Civ;
using God.Sim.Orgs;
using God.Sim.Divinity;

namespace God.Sim.Population
{
    /// <summary>
    /// The extension dispatch of the population system (SHARED_CONTRACTS 0.2), frozen by F0: sim code, may read state.
    /// M4 first, then M5; for the try-calls the first that handles it wins. F1 (contracts 0.1 F1.6, 14): then Civ, Orgs and
    /// Divinity everywhere (<see cref="CivHooks"/>, <see cref="OrgHooks"/>, <see cref="DivinityHooks"/>), plus the F1 sites
    /// <see cref="OverrideIntent"/>, <see cref="ForceThink"/>, <see cref="ActionEnded"/>, <see cref="SenseExtra"/> and <see cref="ArchiveMind"/>.
    /// Every new hook returns its default while its switch is off.
    /// </summary>
    public static class PopulationExtensions
    {
        /// <summary>The episode key of a decision M3 does not key (0: none).</summary>
        public static int EpisodeKey(Npc npc, in Candidate c)
        {
            int k = HouseholdHooks.EpisodeKey(npc, in c);
            if (k == 0) k = SocialHooks.EpisodeKey(npc, in c);
            if (k == 0) k = CivHooks.EpisodeKey(npc, in c);
            if (k == 0) k = OrgHooks.EpisodeKey(npc, in c);
            if (k == 0) k = DivinityHooks.EpisodeKey(npc, in c);
            return k;
        }

        public static bool TryStart(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day) =>
            HouseholdHooks.TryStart(s, ev, npc, in c, day) || SocialHooks.TryStart(s, ev, npc, in c, day)
            || CivHooks.TryStart(s, ev, npc, in c, day) || OrgHooks.TryStart(s, ev, npc, in c, day) || DivinityHooks.TryStart(s, ev, npc, in c, day);

        public static bool TryAct(GodState s, EventBuffer ev, Npc npc, int day) =>
            HouseholdHooks.TryAct(s, ev, npc, day) || SocialHooks.TryAct(s, ev, npc, day)
            || CivHooks.TryAct(s, ev, npc, day) || OrgHooks.TryAct(s, ev, npc, day) || DivinityHooks.TryAct(s, ev, npc, day);

        /// <summary>The time-use category of a verb M3 does not know (<see cref="Activity.Other"/> when neither knows it).</summary>
        public static int ActivityOf(byte verb)
        {
            int a = HouseholdHooks.ActivityOf(verb);
            if (a == Activity.Other) a = SocialHooks.ActivityOf(verb);
            if (a == Activity.Other) a = CivHooks.ActivityOf(verb);
            if (a == Activity.Other) a = OrgHooks.ActivityOf(verb);
            if (a == Activity.Other) a = DivinityHooks.ActivityOf(verb);
            return a;
        }

        /// <summary>The fields of a seen person its owners fill (M4: stage displays, household; M5: gestures).</summary>
        public static void FillPerson(GodState s, Npc observer, Npc seen, ref SeenPerson p)
        {
            HouseholdHooks.FillPerson(s, observer, seen, ref p);
            SocialHooks.FillPerson(s, observer, seen, ref p);
            CivHooks.FillPerson(s, observer, seen, ref p);
            OrgHooks.FillPerson(s, observer, seen, ref p);
            DivinityHooks.FillPerson(s, observer, seen, ref p);
        }

        /// <summary>
        /// I-BS amendment (SHARED_CONTRACTS 12.32), the Sense seam for bins: the bins in sight of <paramref name="npc"/> its
        /// owner perceives (M4: its household's store, estate goods), appended to the sensed things. Returns the new count.
        /// </summary>
        public static int SenseBins(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, int n) => HouseholdHooks.SenseBins(s, npc, sight, g, near, n);

        /// <summary>
        /// F1 (contracts 6): the Sense seam generalised. M4's bins (only while bins exist, as the I-BS site did), then Civ
        /// (assets, C1), Orgs (org buildings and stores, C6), Divinity (shrines, D3). Returns the new count.
        /// </summary>
        public static int SenseExtra(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, int n)
        {
            if (s.Population.Bins.Count > 0) n = HouseholdHooks.SenseBins(s, npc, sight, g, near, n);
            n = CivHooks.SenseExtra(s, npc, sight, g, near, n);
            n = OrgHooks.SenseExtra(s, npc, sight, g, near, n);
            n = DivinityHooks.SenseExtra(s, npc, sight, g, near, n);
            return n;
        }

        // ---- F1: the control seam (contracts 11.3; bodies in Divinity/Control.cs) ----

        /// <summary>True when the body must think this tick although its cadence says not (a new control input or spell). False by default.</summary>
        public static bool ForceThink(GodState s, Npc npc) => DivinityHooks.ForceThink(s, npc);

        /// <summary>
        /// After the policy step: the control authority over the body may replace the chosen intent (compulsion) and stamps
        /// <c>why.Control</c>. Possession and avatar bodies never reach the policy (<see cref="SkipsPolicy"/>). No-op by default.
        /// </summary>
        public static void OverrideIntent(GodState s, Npc npc, ThinkScratch sc, ref Policy.NpcIntent intent, ref DecisionRecord why) => DivinityHooks.OverrideIntent(s, npc, sc, ref intent, ref why);

        /// <summary>F1.8: true when a god controls the body now (an avatar body runs the tick only then).</summary>
        public static bool IsControlled(GodState s, Npc npc) => s.Divinity != null && Control.Of(s, npc.Id) != ControlKind.Self;

        /// <summary>True when the body's policy must not run at this think (possessed or avatar: no policy draw). False by default.</summary>
        public static bool SkipsPolicy(GodState s, Npc npc) => DivinityHooks.SkipsPolicy(s, npc);

        /// <summary>The control intent of a body whose policy is skipped (the input candidate, the running action, or idle).</summary>
        public static Policy.NpcIntent ControlIntent(GodState s, Npc npc, ThinkScratch sc, ref DecisionRecord why) => DivinityHooks.ControlIntent(s, npc, sc, ref why);

        /// <summary>The control entry over the body, copied into the think scratch before Think (default: Self).</summary>
        public static ControlEntry ControlOf(GodState s, Npc npc) => DivinityHooks.ControlOf(s, npc);

        /// <summary>A running action ended (done, failed or replaced): compulsion completion and the action events of controlled bodies. No-op by default.</summary>
        public static void ActionEnded(GodState s, EventBuffer ev, Npc npc, byte verb, bool done) => DivinityHooks.ActionEnded(s, ev, npc, verb, done);

        /// <summary>Contracts 4.4: just before a dead person's mind is released (D7's archive; nothing by default).</summary>
        public static void ArchiveMind(GodState s, Npc npc) => DivinityHooks.ArchiveMind(s, npc);

        // ---- module entry points ----
        public static void StartGame(GodState s) { HouseholdHooks.StartGame(s); SocialHooks.StartGame(s); }
        public static void Install(GodSim sim) { HouseholdHooks.Install(sim); SocialHooks.Install(sim); }
        public static void NewMind(GodState s, Npc npc)
        {
            HouseholdHooks.NewMind(s, npc); SocialHooks.NewMind(s, npc);
            CivModule.NewMind(s, npc); OrgModule.NewMind(s, npc); DivinityModule.NewMind(s, npc); // F1.5
        }
        /// <summary>Kill order (contracts 11.4): M4, M5, then Civ, Orgs, Divinity (which releases control first).</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            HouseholdHooks.OnDeath(s, ev, npc); SocialHooks.OnDeath(s, ev, npc);
            CivHooks.OnDeath(s, ev, npc); OrgHooks.OnDeath(s, ev, npc); DivinityHooks.OnDeath(s, ev, npc); // F1.6
        }
        public static void Check(GodState s, List<string> bad) { HouseholdHooks.Check(s, bad); SocialHooks.Check(s, bad); }
    }
}
