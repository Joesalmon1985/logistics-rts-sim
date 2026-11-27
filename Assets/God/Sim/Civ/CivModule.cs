using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Civ
{
    /// <summary>
    /// F1.5 (contracts 0.1): the module entry points. Call sites (frozen): <c>GodSim.Create</c> (after
    /// <c>PopulationExtensions.StartGame</c>), <c>GodSim.InstallModules</c>, <c>PopulationModule.AddNpc</c> (through
    /// <c>PopulationExtensions.NewMind</c>), <c>Kill</c> (through the hooks' <c>OnDeath</c>) and <c>SimInvariants.Check</c>.
    /// OWNER of the bodies: C1. In F1 every body is a no-op except that <see cref="StartGame"/> creates the empty state when
    /// <c>Civ.Enabled = 1</c> and the game has a population.
    /// </summary>
    public static partial class CivModule
    {
        /// <summary>Before the first tick: the empty state when the switch is on and the game has a population.</summary>
        public static void StartGame(GodState s)
        {
            if (s.Population == null || s.Tuning.Civ.Enabled == 0) return;
            s.Civ = new CivState();
        }

        /// <summary>Registers handlers and systems (none in F1).</summary>
        public static void Install(GodSim sim) { }

        /// <summary>A new mind: its part of this module (none in F1: the mind slot stays null until C1 fills it).</summary>
        public static void NewMind(GodState s, Npc npc) { }

        /// <summary>A person died (called by the hooks' <c>OnDeath</c> only while the state exists).</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc) { }

        /// <summary>The module's invariants (none in F1).</summary>
        public static void Check(GodState s, List<string> bad) { }
    }
}
