using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// Ticks the AI brains of a set of factions inside the sim, as a sim system that runs after every other system
    /// (<see cref="SystemOrder"/>). So bots think on sim ticks, never on frames, whatever loop steps the sim (the game at
    /// any speed, the smoke run, tests). Each brain thinks every <see cref="AiTuning.ThinkIntervalTicks"/> ticks, staggered
    /// by faction so they rarely share a tick; commands a brain submits apply at the start of the next tick, like a
    /// person's.
    ///
    /// <para><b>CPU budget.</b> Counted in work, not wall time (wall time would break determinism): at most
    /// <see cref="AiTuning.MaxThinksPerTick"/> brains think in one tick, and one think spends at most
    /// <see cref="AiTuning.MaxChecksPerThink"/> rule checks, <see cref="AiTuning.BuildsPerThink"/> structural actions and
    /// path searches bounded by <see cref="AiTuning.MaxRoadSteps"/>.</para>
    ///
    /// <para><b>Restart.</b> Brains keep no memory between thinks: every think derives everything from
    /// <see cref="GodState"/> (state, tick, seed), so a director attached to a loaded game continues it exactly as the
    /// original would have. <see cref="SaveState"/> and <see cref="LoadState"/> exist for the save format and carry
    /// nothing but a version tag.</para>
    /// </summary>
    public sealed class AiDirector : ISimSystem
    {
        /// <summary>After the faction system (400): the brains see the finished tick.</summary>
        public const int SystemOrder = 900;
        public const string SaveTag = "ai-stateless-1";

        public GodSim Sim { get; }
        public AiTuning Tuning { get; }
        private readonly List<AiBrain> _brains = new List<AiBrain>();
        public IReadOnlyList<AiBrain> Brains => _brains;

        /// <summary>False stops every brain (a director cannot be removed from a sim, so <see cref="Detach"/> clears this).</summary>
        public bool Enabled = true;

        /// <summary>
        /// True holds every brain without detaching: ticks pass with no thinking. The game holds the brains for the single
        /// step it takes while paused to apply a person's command, so a paused game stays paused.
        /// </summary>
        public bool Held;

        /// <summary>Thinks run so far (for tests and logs; not state).</summary>
        public long Thinks;

        private AiDirector(GodSim sim, AiTuning tuning)
        {
            Sim = sim; Tuning = tuning ?? AiTuning.Default;
        }

        /// <summary>
        /// Gives each listed faction a brain and installs the director as a sim system. Factions not in the game are skipped;
        /// duplicates are ignored. Optional trader (default <see cref="NoTrade"/>).
        /// </summary>
        public static AiDirector Attach(GodSim sim, IEnumerable<Faction> factions, AiTuning tuning = null, IAiTrader trader = null)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            var d = new AiDirector(sim, tuning);
            var seen = new SortedSet<int>();
            if (factions != null) foreach (var f in factions) if (sim.State.Setup.InGame((int)f)) seen.Add((int)f);
            foreach (int f in seen) d._brains.Add(new AiBrain(sim, (Faction)f, d.Tuning, trader));
            sim.AddSystem(SystemOrder, d);
            return d;
        }

        /// <summary>Every faction of the game except the human one (all six in watch mode).</summary>
        public static List<Faction> NonHuman(GodState s)
        {
            var l = new List<Faction>();
            foreach (var f in s.Setup.Factions) if (!s.Setup.HasHuman || f != s.Setup.HumanFaction) l.Add(f);
            return l;
        }

        public AiBrain BrainOf(Faction f) => _brains.Find(b => b.Side == f);

        /// <summary>Stops every brain for good.</summary>
        public void Detach() => Enabled = false;

        /// <summary>The tick offset of a faction within the think interval: factions spread evenly over it.</summary>
        public int OffsetOf(Faction f) => (int)f * Tuning.ThinkIntervalTicks / FactionInfo.Max;

        public void Tick(GodState s, EventBuffer ev)
        {
            if (!Enabled || Held || s.IsGameOver) return;
            int interval = Math.Max(1, Tuning.ThinkIntervalTicks), budget = Tuning.MaxThinksPerTick;
            foreach (var b in _brains)
            {
                if (budget <= 0) break;
                if ((s.Tick + OffsetOf(b.Side)) % interval != 0) continue;
                if (s.Factions.IsEliminated(b.Side)) continue;
                b.Think();
                Thinks++;
                budget--;
            }
        }

        /// <summary>Brains are rebuilt from the game state alone, so the save needs only this tag.</summary>
        public string SaveState() => SaveTag;

        /// <summary>Accepts what <see cref="SaveState"/> wrote (or nothing); there is no memory to restore.</summary>
        public void LoadState(string data)
        {
            if (!string.IsNullOrEmpty(data) && data != SaveTag) throw new FormatException("Unknown AI save data: " + data);
        }
    }
}
