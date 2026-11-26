using System;
using System.Collections.Generic;
using God.Recording;
using God.Sim;

namespace God.Runner
{
    /// <summary>
    /// One sim as the runner drives it: built through <see cref="SimBuilder"/> (as the Unity player builds it) with the
    /// scenario's policies, fed its external commands before the tick they apply at (from the scenario schedule or a replay
    /// log). Every tick is an ordinary tick (M1 removed the held ticks of a paused game, so a replay is only commands and
    /// checkpoints). The run loop, verify and bisect all step sims through this, so they cannot disagree on how a tick is
    /// driven.
    /// </summary>
    public sealed class SimSession
    {
        public readonly GodSim Sim;
        public readonly AiDirector Director;
        private readonly List<(long tick, Command cmd, string origin)> _commands;
        private int _next;
        private readonly List<SimEvent> _scratch = new List<SimEvent>();

        /// <summary>Extra systems added after the build (test-only hooks: injected faults, exceptions).</summary>
        public SimSession(Scenario scenario, ulong seed, SaveFile fromSave, ReplayFile replay, IEnumerable<Func<ISimSystem>> testSystems = null)
        {
            BuiltSim built = fromSave != null ? SimBuilder.FromSave(fromSave, true, scenario.Policies) : SimBuilder.Create(scenario.ToSetup(seed), scenario.ToTuning(), true, scenario.Policies);
            Sim = built.Sim; Director = built.Director;
            if (testSystems != null) foreach (var f in testSystems) Sim.AddSystem(5000, f());
            _commands = new List<(long, Command, string)>();
            long from = Sim.State.Tick;
            if (replay != null)
            {
                foreach (var e in replay.Entries)
                {
                    if (e.Tick <= from) continue;
                    if (e.Kind == ReplayEntryKind.Command) _commands.Add((e.Tick, e.ToCommand(), e.Origin ?? "replay"));
                }
            }
            else
            {
                foreach (var (tick, cmd) in scenario.BuildCommands())
                    if (tick > from) _commands.Add((tick, cmd, "schedule"));
            }
        }

        public long Tick => Sim.State.Tick;

        /// <summary>Set while the session submits a command, so a replay recorder can log its origin.</summary>
        public string CurrentOrigin { get; private set; } = "external";

        /// <summary>Scheduled commands that the sim refused at submission (a scenario mistake, or a replay that diverged).</summary>
        public readonly List<string> Refused = new List<string>();

        /// <summary>Submits the commands due for the next tick, then steps once.</summary>
        public void Step()
        {
            long next = Sim.State.Tick + 1;
            while (_next < _commands.Count && _commands[_next].tick <= next)
            {
                var (tick, cmd, origin) = _commands[_next++];
                if (tick < next) continue; // its tick passed while the game was over or before a load
                CurrentOrigin = origin;
                var r = Sim.Submit(cmd);
                CurrentOrigin = "external";
                if (!r.Ok) Refused.Add("tick " + tick + " " + cmd.GetType().Name + ": " + r);
            }
            Sim.Step();
        }

        /// <summary>Steps and throws the events away (bisect, fast-forward).</summary>
        public void StepQuiet()
        {
            Step();
            _scratch.Clear();
            Sim.DrainInto(_scratch);
            _scratch.Clear();
        }
    }
}
