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

        /// <summary>A new game: the island dealt from the seed, no systems registered yet.</summary>
        public static GodSim Create(ulong seed)
        {
            var s = new GodState { Seed = seed, Rng = new Rng(seed).Fork("sim") };
            s.Board = BoardGenerator.CreateIslandBoard(seed);
            TerritoryModule.StartGame(s);
            // Military first: it pre-places the enemy base, which the economy then records as already built.
            MilitaryModule.StartGame(s);
            EconomyModule.StartGame(s);
            foreach (var b in s.Territory.Buildings.ToArray())
                if (b.Faction == Faction.Enemy && b.Size != BuildingSize.Hq) EconomyModule.CompleteInstantly(s, b.Id);
            var sim = new GodSim(s);
            TerritoryModule.Install(sim);
            EconomyModule.Install(sim);
            MilitaryModule.Install(sim);
            return sim;
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

        /// <summary>Validate without queuing (for UI previews). Never mutates state.</summary>
        public CommandResult Check(Command c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
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
        /// One tick: Tick += 1, apply queued commands in submission order (each re-checked), then run systems in
        /// order. Events are stamped with the new Tick value.
        /// </summary>
        public void Step()
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

        public void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++) Step();
        }

        /// <summary>Events since the last drain, in emission order.</summary>
        public IReadOnlyList<SimEvent> DrainEvents() => _events.Drain();
    }
}
