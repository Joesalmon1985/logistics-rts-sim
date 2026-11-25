using System;
using System.Collections.Generic;
using System.Linq;

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
    public sealed class GodSim
    {
        public const int TicksPerSecond = 10;

        public GodState State { get; }

        private readonly List<(int order, int index, ISimSystem system)> _systems = new List<(int, int, ISimSystem)>();
        private readonly Dictionary<Type, ICommandHandler> _handlers = new Dictionary<Type, ICommandHandler>();
        private readonly List<Command> _pending = new List<Command>();
        private readonly EventBuffer _events = new EventBuffer();

        public GodSim(GodState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>A new six-faction game from a seed, faction 0 human (see <see cref="Create(GameSetup)"/>).</summary>
        public static GodSim Create(ulong seed) => Create(new GameSetup(seed));

        /// <summary>
        /// A new game: the 30-hex island dealt from the setup's seed, one HQ with the start stock per faction on spread-out
        /// start corners, everyone at peace, every module installed.
        /// </summary>
        public static GodSim Create(GameSetup setup)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (setup.FactionCount < 2 || setup.FactionCount > FactionInfo.Max) throw new ArgumentOutOfRangeException(nameof(setup), "2 to 6 factions.");
            if (setup.HasHuman && !setup.InGame(setup.Human)) throw new ArgumentOutOfRangeException(nameof(setup), "The human faction is not in the game.");
            ulong seed = setup.Seed;
            var s = new GodState { Seed = seed, Rng = new Rng(seed).Fork("sim"), Setup = setup };
            s.Board = BoardGenerator.CreateIslandBoard(seed);
            FactionsModule.StartGame(s);
            TerritoryModule.StartGame(s);
            MilitaryModule.StartGame(s);
            EconomyModule.StartGame(s);
            var sim = new GodSim(s);
            TerritoryModule.Install(sim);
            EconomyModule.Install(sim);
            MilitaryModule.Install(sim);
            FactionsModule.Install(sim);
            return sim;
        }

        /// <summary>One hash over the whole state the modules drive (territory, economy, military, factions). Equal for equal setup and commands.</summary>
        public ulong StateHash() => StateHashOf(State);

        public static ulong StateHashOf(GodState s)
        {
            unchecked
            {
                ulong h = EconomyModule.StateHash(s);
                h = h * 1099511628211UL ^ s.Military.Hash(s);
                h = h * 1099511628211UL ^ FactionsModule.Hash(s);
                return h;
            }
        }

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
            if (r.Ok) _pending.Add(c);
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

        public void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++) Step();
        }

        /// <summary>Events since the last drain, in emission order.</summary>
        public IReadOnlyList<SimEvent> DrainEvents() => _events.Drain();
    }
}
