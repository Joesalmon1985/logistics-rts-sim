using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace God.Sim
{
    /// <summary>Per-tick system. Must be deterministic: no wall clock, no unordered enumeration.</summary>
    public interface ISimSystem
    {
        void Tick(GodState s, EventBuffer ev);
    }

    /// <summary>
    /// The simulation: state, ordered systems, command handlers, command gate and event buffer, ticking at
    /// <see cref="TicksPerSecond"/> in integers. Submit validates immediately and queues; queued commands apply at the
    /// start of the next Step. Views read <see cref="State"/> and send commands; they never mutate it.
    /// </summary>
    public sealed class GodSim : ICommandGate
    {
        public const int TicksPerSecond = 10;

        /// <summary>
        /// Names of <see cref="StateHashParts"/>, in their fixed order. Stage 4 modules append theirs at the end as they land
        /// (HEADLESS_SIMULATION 4.4). <c>core</c> is the tick, seed, root random stream, setup (calendar, victory list) and
        /// tuning; <c>pending</c> the commands queued for the next tick.
        /// </summary>
        public static readonly ReadOnlyArray<string> HashPartNames = new ReadOnlyArray<string>(
            "territory", "economy", "military", "factions", "trade", "core", "pending");

        /// <summary>
        /// Every part name including the modules that exist only in some games: M3's population, cognition and learning parts
        /// follow <see cref="HashPartNames"/> when the setup has a population, and are absent otherwise, so a stage 3 game's
        /// parts and hash are exactly as before.
        /// </summary>
        public static readonly ReadOnlyArray<string> AllHashPartNames = new ReadOnlyArray<string>(
            "territory", "economy", "military", "factions", "trade", "core", "pending", "population", "cognition", "learning", "household", "social");

        /// <summary>
        /// The part names of this state, in the order of <see cref="PartsOf"/> (F0.12): the stage 3 parts; the M3 parts with a
        /// population; then "household" and "social", each only while its state is non-null.
        /// </summary>
        public static string[] PartNamesOf(GodState s)
        {
            var l = new List<string>();
            int n = s.Population == null ? HashPartNames.Count : 10;
            for (int i = 0; i < n; i++) l.Add(AllHashPartNames[i]);
            if (s.Population != null && s.Households != null) l.Add("household");
            if (s.Population != null && s.Social != null) l.Add("social");
            return l.ToArray();
        }

        public GodState State { get; }

        private readonly List<(int order, int index, ISimSystem system)> _systems = new List<(int, int, ISimSystem)>();
        private readonly Dictionary<Type, ICommandHandler> _handlers = new Dictionary<Type, ICommandHandler>();
        private readonly List<Command> _pending = new List<Command>();
        private readonly EventBuffer _events = new EventBuffer();

        /// <summary>
        /// True while <see cref="Step()"/> runs. A command submitted then comes from inside the sim (an AI brain, a system);
        /// one submitted while it is false is external (the HUD, a scenario schedule, a server), which replays log.
        /// </summary>
        public bool InStep { get; private set; }

        /// <summary>A host setting (not state): emit one <c>Learning.DecisionMade</c> per chosen intent (Full recording only).</summary>
        public bool TraceDecisions { get => _events.TraceDecisions; set => _events.TraceDecisions = value; }

        /// <summary>
        /// Raised for every command accepted by <see cref="Submit"/> while <see cref="InStep"/> is false: what a replay
        /// recorder logs. A host hook; it must never touch state.
        /// </summary>
        public event Action<Command> ExternalCommandAccepted;

        public GodSim(GodState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>A new six-faction game from a seed, faction 0 human (see <see cref="Create(GameSetup, SimTuning)"/>).</summary>
        public static GodSim Create(ulong seed) => Create(new GameSetup(seed));

        /// <summary>
        /// A new game: the 30-hex island dealt from the setup's seed, one HQ with the start stock per faction on spread-out
        /// start corners, everyone at peace, every module installed. <paramref name="tuning"/> is copied (default: the
        /// stage 3 numbers), so the caller's object is never shared with the game.
        /// </summary>
        public static GodSim Create(GameSetup setup, SimTuning tuning = null)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (setup.FactionCount < 2 || setup.FactionCount > FactionInfo.Max) throw new ArgumentOutOfRangeException(nameof(setup), "2 to 6 factions.");
            if (setup.HasHuman && !setup.InGame(setup.Human)) throw new ArgumentOutOfRangeException(nameof(setup), "The human faction is not in the game.");
            if (setup.TicksPerDay <= 0 || setup.DaysPerYear <= 0 || setup.LifeScale <= 0) throw new ArgumentOutOfRangeException(nameof(setup), "The calendar needs positive TicksPerDay, DaysPerYear and LifeScale.");
            ulong seed = setup.Seed;
            var s = new GodState { Seed = seed, Rng = new Rng(seed).Fork("sim"), Setup = setup, Tuning = tuning != null ? tuning.Clone() : new SimTuning() };
            s.Board = BoardGenerator.CreateIslandBoard(seed);
            FactionsModule.StartGame(s);
            TerritoryModule.StartGame(s);
            MilitaryModule.StartGame(s);
            EconomyModule.StartGame(s);
            TradeModule.StartGame(s);
            Population.PopulationModule.StartGame(s);
            if (s.Population != null) Population.PopulationExtensions.StartGame(s); // F0.13: M4, then M5
            var sim = new GodSim(s);
            InstallModules(sim);
            return sim;
        }

        /// <summary>Registers every module's handlers and systems. Shared by <see cref="Create(GameSetup, SimTuning)"/> and <see cref="FromState"/>.</summary>
        private static void InstallModules(GodSim sim)
        {
            TerritoryModule.Install(sim);
            EconomyModule.Install(sim);
            MilitaryModule.Install(sim);
            TradeModule.Install(sim);
            FactionsModule.Install(sim);
            Population.PopulationModule.Install(sim);
            if (sim.State.Population != null) Population.PopulationExtensions.Install(sim); // F0.13: M4, then M5
        }

        /// <summary>
        /// A working sim over a state that already exists (a loaded save): handlers and systems installed, nothing dealt.
        /// Caches (board topology, node grid, health index, route cache) are rebuilt from the state, never stored.
        /// </summary>
        public static GodSim FromState(GodState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.Tuning == null) state.Tuning = new SimTuning();
            var sim = new GodSim(state);
            InstallModules(sim);
            state.Military?.IndexBuildings();
            return sim;
        }

        /// <summary>Puts a command back in the queue without validating it (a load restores what was queued).</summary>
        internal void RestorePending(Command c) { _pending.Add(c); }

        // ---------------- hashing ----------------

        /// <summary>
        /// One hash over the whole state the modules drive and the queued commands: the FNV fold of
        /// <see cref="StateHashParts"/>. Equal for equal setup, tuning and commands.
        /// </summary>
        public ulong StateHash() => Fold(StateHashParts());

        /// <summary>
        /// One hash per module, in the order of <see cref="HashPartNames"/>, so a mismatch names its module
        /// (HEADLESS_SIMULATION 4.4). Includes the pending command queue (M0).
        /// </summary>
        public ulong[] StateHashParts() => PartsOf(State, _pending);

        /// <summary>The hash of a state alone (no command queue: as if it were empty).</summary>
        public static ulong StateHashOf(GodState s) => Fold(PartsOf(s, null));

        public static ulong[] PartsOf(GodState s, IReadOnlyList<Command> pending)
        {
            if (s.Population == null)
                return new[]
                {
                    TerritoryModule.Hash(s),
                    EconomyModule.StateHash(s),
                    s.Military.Hash(s),
                    FactionsModule.Hash(s),
                    TradeModule.Hash(s),
                    CoreHash(s),
                    PendingHash(pending),
                };
            var parts = new List<ulong>
            {
                TerritoryModule.Hash(s),
                EconomyModule.StateHash(s),
                s.Military.Hash(s),
                FactionsModule.Hash(s),
                TradeModule.Hash(s),
                CoreHash(s),
                PendingHash(pending),
                Population.PopulationModule.HashPopulation(s),
                Population.PopulationModule.HashCognition(s),
                Population.PopulationModule.HashLearning(s),
            };
            // F0.12: the later modules' parts, each only while its state exists (so M3 games hash exactly as before)
            if (s.Households != null) { var h = new StateHasher(); s.Households.AddTo(ref h); parts.Add(h.Value); }
            if (s.Social != null) { var h = new StateHasher(); s.Social.AddTo(ref h); parts.Add(h.Value); }
            return parts.ToArray();
        }

        /// <summary>FNV-1a over the parts in order.</summary>
        public static ulong Fold(ulong[] parts)
        {
            var h = new StateHasher();
            foreach (var p in parts) h.Add((long)p);
            return h.Value;
        }

        /// <summary>Tick, seed, the root random stream, the setup (calendar, life scale, victory list...) and the tuning.</summary>
        private static ulong CoreHash(GodState s)
        {
            var h = new StateHasher();
            h.Add(s.Tick); h.Add((long)s.Seed); h.Add((long)(s.Rng?.State ?? 0));
            h.Add(s.Setup != null ? JsonConvert.SerializeObject(s.Setup, SaveFile.Settings) : "");
            h.Add(s.Tuning != null ? s.Tuning.ToJson() : "");
            return h.Value;
        }

        /// <summary>The queued commands, in order: type and fields (the save's form of each command).</summary>
        public static ulong PendingHash(IReadOnlyList<Command> pending)
        {
            var h = new StateHasher();
            int n = pending?.Count ?? 0;
            h.Add(n);
            for (int i = 0; i < n; i++)
            {
                var c = pending[i];
                h.Add(c.GetType().FullName);
                h.Add(JsonConvert.SerializeObject(c, SaveFile.Settings));
            }
            return h.Value;
        }

        // ---------------- systems and commands ----------------

        public IReadOnlyList<Command> PendingCommands => _pending;

        public IReadOnlyList<ISimSystem> Systems
        {
            get
            {
                var list = new List<ISimSystem>(_systems.Count);
                foreach (var s in _systems) list.Add(s.system);
                return list;
            }
        }

        /// <summary>Systems run in ascending order; equal orders run in registration order.</summary>
        public void AddSystem(int order, ISimSystem system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            _systems.Add((order, _systems.Count, system));
            _systems.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : a.index.CompareTo(b.index));
        }

        public void RegisterHandler(ICommandHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_handlers.ContainsKey(handler.CommandType))
                throw new InvalidOperationException("Handler already registered for " + handler.CommandType.Name);
            _handlers.Add(handler.CommandType, handler);
        }

        public bool HasHandler(Type commandType) => _handlers.ContainsKey(commandType);

        /// <summary>Validate without queuing (for UI previews). Never mutates state. Everything is refused once the game is over.</summary>
        public CommandResult Check(Command c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (State.IsGameOver) return CommandResult.Reject(MilitaryReasons.GameOver, "The game is over.");
            if (!_handlers.TryGetValue(c.GetType(), out var h))
                return CommandResult.Reject(Reasons.Disabled, "That action is not available.");
            return h.Validate(State, c);
        }

        /// <summary>Validate now; if accepted, queue for the next tick boundary.</summary>
        public CommandResult Submit(Command c)
        {
            var r = Check(c);
            if (r.Ok)
            {
                _pending.Add(c);
                if (!InStep) ExternalCommandAccepted?.Invoke(c);
            }
            return r;
        }

        /// <summary>
        /// One tick (none once the game is over): Tick += 1, apply queued commands in submission order (each re-checked), then run systems in
        /// order. Events are stamped with the new Tick value.
        /// </summary>
        public void Step()
        {
            // The game is over: the sim stops (no tick, no commands, no systems).
            if (State.IsGameOver) { _pending.Clear(); return; }
            InStep = true;
            try
            {
                State.Tick++;
                _events.CurrentTick = State.Tick;

                if (_pending.Count > 0)
                {
                    var batch = new List<Command>(_pending);
                    _pending.Clear();
                    foreach (var c in batch)
                    {
                        var h = _handlers[c.GetType()];
                        var check = h.Validate(State, c);
                        var r = check.Ok ? h.Apply(State, c, _events) : check;
                        if (r.Ok) _events.Add(new CommandApplied { Command = c });
                        else _events.Add(new CommandRejected { Command = c, Result = r });
                    }
                }

                foreach (var s in _systems) s.system.Tick(State, _events);
            }
            finally { InStep = false; }
        }

        public void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++) Step();
        }

        /// <summary>Events since the last drain, in emission order.</summary>
        public IReadOnlyList<SimEvent> DrainEvents() => _events.Drain();

        /// <summary>Moves the events since the last drain into <paramref name="into"/> (appended), without allocating.</summary>
        public void DrainInto(List<SimEvent> into) => _events.DrainInto(into);
    }

    /// <summary>FNV-1a over 64-bit values and strings: the hash every module's part uses.</summary>
    public struct StateHasher
    {
        private ulong _h;
        private bool _started;

        public ulong Value => _started ? _h : 14695981039346656037UL;

        public void Add(long v)
        {
            if (!_started) { _h = 14695981039346656037UL; _started = true; }
            unchecked { for (int i = 0; i < 8; i++) { _h ^= (ulong)((v >> (i * 8)) & 0xFF); _h *= 1099511628211UL; } }
        }

        public void Add(string s)
        {
            if (!_started) { _h = 14695981039346656037UL; _started = true; }
            if (s == null) { Add(-1); return; }
            Add(s.Length);
            unchecked { foreach (char c in s) { _h ^= c; _h *= 1099511628211UL; _h ^= (ulong)(c >> 8); _h *= 1099511628211UL; } }
        }
    }
}
