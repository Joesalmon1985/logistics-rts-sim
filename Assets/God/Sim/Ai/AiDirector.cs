using System;
using System.Collections.Generic;
using God.Sim.Information;
using God.Sim.Policy;

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

        /// <summary>One faction played by a policy: the policy and the faction's knowledge view (M1).</summary>
        public sealed class Seat
        {
            public Faction Side;
            public IFactionPolicy Policy;
            public FactionKnowledgeView View;
        }

        private readonly List<Seat> _seats = new List<Seat>();
        private readonly List<AiBrain> _brains = new List<AiBrain>();
        private readonly List<Command> _out = new List<Command>();
        /// <summary>The factions played, ascending, each with its policy.</summary>
        public IReadOnlyList<Seat> Seats => _seats;
        /// <summary>The stage 3 brains among the seats (the <c>AiBrain</c> policy), ascending by faction.</summary>
        public IReadOnlyList<AiBrain> Brains => _brains;

        /// <summary>False stops every policy (a director cannot be removed from a sim, so <see cref="Detach"/> clears this).</summary>
        public bool Enabled = true;

        /// <summary>Thinks run so far (for tests and logs; not state).</summary>
        public long Thinks;
        /// <summary>Commands a policy returned that a faction policy may not send (another faction's, or not a faction command); dropped.</summary>
        public long Inadmissible;

        private AiDirector(GodSim sim, AiTuning tuning)
        {
            Sim = sim; Tuning = tuning ?? AiTuning.Default;
        }

        /// <summary>
        /// The stage 3 AI for each listed faction: an <c>AiBrain</c> behind <see cref="AiBrainFactionPolicy"/>. Factions not
        /// in the game are skipped; duplicates are ignored. Optional trader (default <see cref="NoTrade"/>); any other trader
        /// also becomes the game's trade judge (<see cref="AiTradeJudge"/>), and an <see cref="AiTrader"/> is bound to this
        /// director.
        /// </summary>
        public static AiDirector Attach(GodSim sim, IEnumerable<Faction> factions, AiTuning tuning = null, IAiTrader trader = null) =>
            Attach(sim, factions, null, null, tuning, trader);

        /// <summary>
        /// Seats each listed faction with the faction policy its <paramref name="assignment"/> names (default
        /// <c>AiBrain</c>), built by <paramref name="registry"/> (default <see cref="StandardPolicies.Registry"/>), in
        /// ascending faction order. A faction assigned <c>none</c> gets no seat. Unknown names throw
        /// <see cref="PolicyException"/> before anything is installed.
        /// </summary>
        public static AiDirector Attach(GodSim sim, IEnumerable<Faction> factions, PolicyAssignment assignment, PolicyRegistry registry,
            AiTuning tuning = null, IAiTrader trader = null)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            var d = new AiDirector(sim, tuning);
            registry = registry ?? StandardPolicies.Registry(sim, d.Tuning, trader);
            var seen = new SortedSet<int>();
            if (factions != null) foreach (var f in factions) if (sim.State.Setup.InGame((int)f)) seen.Add((int)f);
            foreach (int f in seen)
            {
                string name = assignment?.NameFor(PolicyLevel.Faction, f) ?? PolicyAssignment.StageThreeFaction;
                var policy = registry.CreateFaction(name, (Faction)f);
                if (policy == null) continue;
                d._seats.Add(new Seat { Side = (Faction)f, Policy = policy, View = ViewFiller.For(sim.State, (Faction)f) });
                if (policy is AiBrainFactionPolicy ab) d._brains.Add(ab.Brain);
            }
            sim.AddSystem(SystemOrder, d);
            // P11: an AI trader answers offers to its brains through the trade judge (not saved: set again on every start).
            if (trader is AiTrader at) at.Director = d;
            if (trader != null && !(trader is NoTrade) && sim.State.Trade != null) sim.State.Trade.Judge = new AiTradeJudge(d, trader);
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

        /// <summary>The policy playing a faction, or null.</summary>
        public IFactionPolicy PolicyOf(Faction f) { foreach (var s in _seats) if (s.Side == f) return s.Policy; return null; }

        /// <summary>Stops every brain for good.</summary>
        public void Detach() => Enabled = false;

        /// <summary>The tick offset of a faction within the think interval: factions spread evenly over it.</summary>
        public int OffsetOf(Faction f) => (int)f * Tuning.ThinkIntervalTicks / FactionInfo.Max;

        /// <summary>
        /// The due seats think, ascending by faction, at most <see cref="AiTuning.MaxThinksPerTick"/> per tick. Each gets its
        /// own view and the think stream <c>Fork("ai.F.T")</c> of the game seed, returns commands, and the director submits
        /// them in order (inside the step, so they apply at the start of the next tick, like a person's). A command a faction
        /// policy may not send is dropped and counted in <see cref="Inadmissible"/>.
        /// </summary>
        public void Tick(GodState s, EventBuffer ev)
        {
            if (!Enabled || s.IsGameOver) return;
            int interval = Math.Max(1, Tuning.ThinkIntervalTicks), budget = Tuning.MaxThinksPerTick;
            foreach (var seat in _seats)
            {
                if (budget <= 0) break;
                if ((s.Tick + OffsetOf(seat.Side)) % interval != 0) continue;
                if (s.Factions.IsEliminated(seat.Side)) continue;
                var rng = AiBrain.ThinkStream(s.Seed, seat.Side, s.Tick);
                _out.Clear();
                seat.Policy.Decide(seat.View, ref rng, _out);
                foreach (var c in _out)
                {
                    if (!FactionCommandSet.Admissible(c, seat.Side)) { Inadmissible++; continue; }
                    Sim.Submit(c);
                }
                _out.Clear();
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
