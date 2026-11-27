using System;
using System.Collections.Generic;
using God.Sim.Information;
using God.Sim.Policy;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 10.1, 12: which commands a god policy may send. A god policy sends the divine action commands (every concrete
    /// <see cref="DivineCommand"/> except the four configuration commands, which are external only) in its own god's name.
    /// The human god sends the same action set (plus configuration in sandbox); there is no second command vocabulary.
    /// </summary>
    public static class DivineCommandSet
    {
        /// <summary>The configuration commands (external only, sandbox only).</summary>
        public static bool IsConfiguration(Type t) => t == typeof(SwitchGod) || t == typeof(SetGodPolicy) || t == typeof(SetMythicMode) || t == typeof(SetDivineProfile);

        /// <summary>The action set, by full name, sorted (built on each call: no static state).</summary>
        public static List<Type> Types()
        {
            var l = new List<Type>();
            foreach (var t in typeof(DivineCommand).Assembly.GetTypes())
                if (!t.IsAbstract && typeof(DivineCommand).IsAssignableFrom(t) && !IsConfiguration(t)) l.Add(t);
            l.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
            return l;
        }

        /// <summary>True when the policy of <paramref name="god"/> may send this command: a divine action command in its own name.</summary>
        public static bool Admissible(Command c, int god) => c is DivineCommand dc && dc.God == god && god != 0 && !IsConfiguration(c.GetType());
    }

    /// <summary>
    /// Contracts 11.4/12 (order 910, after <c>AiDirector</c> 900 and <c>InstitutionDirector</c> 905): seats every god whose
    /// <see cref="GodRecord.Seat"/> is <c>Ai</c> with the policy named by its <see cref="GodRecord.PolicyName"/>, read from
    /// state at every think (so seats switched by <c>SwitchGod</c> or loaded from a save take effect at once). Each AI god
    /// thinks every <c>Divinity.Mythic.GodThinkTicks</c>, staggered by god id, with the stream <c>god-policy(god, tick)</c>,
    /// and its commands are submitted inside <c>Step</c> (recomputed in replay, never logged). A command that is not the
    /// policy's own god's action command is dropped and counted (<see cref="Inadmissible"/>), never applied. Host-attached
    /// like <c>AiDirector</c>; holds no state of its own (policies are rebuilt by name). OWNER: D1.
    /// </summary>
    public sealed class GodDirector : ISimSystem
    {
        public const int SystemOrder = 910;

        public GodSim Sim { get; }
        private readonly PolicyRegistry _registry;
        private readonly List<Command> _out = new List<Command>();
        // per god (index god - 1): the policy built for the god's current PolicyName, and its view
        private readonly List<IGodPolicy> _policies = new List<IGodPolicy>();
        private readonly List<string> _names = new List<string>();
        private readonly List<IDivineView> _views = new List<IDivineView>();

        public bool Enabled = true;
        /// <summary>Thinks so far (not state).</summary>
        public long Thinks;
        /// <summary>Commands dropped: another god's, a configuration command, or not a divine command.</summary>
        public long Inadmissible;
        /// <summary>Submitted commands the gate or handler refused at submission (for example <c>divinity_disabled</c>).</summary>
        public long Refused;
        /// <summary>PolicyName values the registry does not know (the god idles).</summary>
        public long UnknownPolicies;
        /// <summary>A host/test hook: every command a policy submitted, with the tick and the result. Must never touch state.</summary>
        public event Action<int, long, Command, CommandResult> Submitted;

        private GodDirector(GodSim sim, PolicyRegistry registry) { Sim = sim; _registry = registry; }

        /// <summary>
        /// Attaches the director to a game with divinity state (returns null without). On a fresh game (tick 0) every god's
        /// <see cref="GodRecord.PolicyName"/> is set from <paramref name="assignment"/>'s <c>policies.god</c> (scope = GodId;
        /// <c>none</c> means <c>IdleGod</c>), the way <c>AttachPolicies</c> names the founders' civilian policy; a loaded game
        /// keeps the names in its state.
        /// </summary>
        public static GodDirector Attach(GodSim sim, PolicyAssignment assignment = null, PolicyRegistry registry = null)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            var reg = sim.State.Divinity?.Gods;
            if (reg == null) return null;
            registry = registry ?? StandardPolicies.Registry(sim);
            if (assignment != null && sim.State.Tick == 0)
                foreach (var g in reg.Gods)
                {
                    string name = assignment.NameFor(PolicyLevel.God, g.Id);
                    g.PolicyName = name == PolicyRegistry.None || string.IsNullOrEmpty(name) ? GodPolicies.Idle : name;
                }
            var d = new GodDirector(sim, registry);
            sim.AddSystem(SystemOrder, d);
            return d;
        }

        /// <summary>The tick offset of a god within the think interval.</summary>
        public static int OffsetOf(int god, int interval) => (god - 1) * interval / FactionInfo.Max;

        /// <summary>The policy now driving a god (null for a Human-seated god or before its first think).</summary>
        public IGodPolicy PolicyOf(int god) => god >= 1 && god <= _policies.Count ? _policies[god - 1] : null;

        public void Tick(GodState s, EventBuffer ev)
        {
            var reg = s.Divinity?.Gods;
            if (!Enabled || reg == null || s.IsGameOver) return;
            int interval = Math.Max(1, s.Tuning.Divinity.Mythic.GodThinkTicks);
            while (_policies.Count < reg.Gods.Count) { _policies.Add(null); _names.Add(null); _views.Add(null); }
            foreach (var g in reg.Gods)
            {
                if (g.Seat != GodSeat.Ai) continue;
                if ((s.Tick + OffsetOf(g.Id, interval)) % interval != 0) continue;
                var policy = Seat(s, g, interval);
                if (policy == null) continue;
                var rng = ThinkStream(s.Seed, g.Id, s.Tick);
                _out.Clear();
                policy.Decide(_views[g.Id - 1], ref rng, _out);
                foreach (var c in _out)
                {
                    if (!DivineCommandSet.Admissible(c, g.Id)) { Inadmissible++; continue; }
                    var r = Sim.Submit(c);
                    if (!r.Ok) Refused++;
                    Submitted?.Invoke(g.Id, s.Tick, c, r);
                }
                _out.Clear();
                Thinks++;
            }
        }

        /// <summary>The think stream of a god: <c>god-policy(god, tick)</c> of the game seed.</summary>
        public static Rng ThinkStream(ulong seed, int god, long tick) => new Rng(seed).Fork("god-policy", god, tick);

        private IGodPolicy Seat(GodState s, GodRecord g, int interval)
        {
            int i = g.Id - 1;
            if (_names[i] == g.PolicyName && _policies[i] != null) return _policies[i];
            IGodPolicy p = null;
            if (!string.IsNullOrEmpty(g.PolicyName) && _registry.Has(PolicyLevel.God, g.PolicyName) && g.PolicyName != PolicyRegistry.None)
                p = _registry.Create(PolicyLevel.God, g.PolicyName, g.Id, allowTestOnly: true) as IGodPolicy;
            if (p == null) { UnknownPolicies++; p = new IdleGod(); }
            if (p is ScheduleGod sg) sg.Configure(s.Tuning.Divinity.Mythic.Schedule, interval);
            _policies[i] = p; _names[i] = g.PolicyName;
            if (_views[i] == null) _views[i] = DivineViewFiller.For(s, g.Id);
            return p;
        }
    }
}
