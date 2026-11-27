using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// F1.5 (contracts 0.1): the module entry points. Call sites (frozen): <c>GodSim.Create</c> (after
    /// <c>PopulationExtensions.StartGame</c>), <c>GodSim.InstallModules</c>, <c>PopulationModule.AddNpc</c> (through
    /// <c>PopulationExtensions.NewMind</c>), <c>Kill</c> (through the hooks' <c>OnDeath</c>) and <c>SimInvariants.Check</c>.
    /// OWNER of the bodies: D1. In F1 every body is a no-op except that <see cref="StartGame"/> creates the empty state when
    /// <c>Divinity.Enabled = 1</c> and the game has a population.
    /// </summary>
    public static partial class DivinityModule
    {
        /// <summary>Before the first tick: the empty state when the switch is on and the game has a population.</summary>
        public static void StartGame(GodState s)
        {
            if (s.Population == null || s.Tuning.Divinity.Enabled == 0) return;
            s.Divinity = new DivinityState();
        }

        /// <summary>Registers handlers and systems (none in F1).</summary>
        public static void Install(GodSim sim) { }

        /// <summary>A new mind: its part of this module (none in F1: the mind slot stays null until D1 fills it).</summary>
        public static void NewMind(GodState s, Npc npc) { }

        /// <summary>A person died (called by the hooks' <c>OnDeath</c> only while the state exists).</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc) { }

        /// <summary>The module's invariants (none in F1).</summary>
        public static void Check(GodState s, List<string> bad) { }

        /// <summary>
        /// The authorship gate's question (contracts 10.1): does <paramref name="god"/> hold the <c>Human</c> seat now? The seat is
        /// sim state (<c>GodRecord.Seat</c>, D1): D1 answers through <see cref="HumanSeatD1"/> (1 yes, 0 no). Until it does (no
        /// registry yet) the answer is the setup's <see cref="GameSetup.HumanGod"/>.
        /// </summary>
        public static bool HoldsHumanSeat(GodState s, int god)
        {
            int seat = -1;
            HumanSeatD1(s, god, ref seat);
            if (seat >= 0) return seat == 1;
            return god != 0 && s.Setup != null && s.Setup.HumanGod == god;
        }
        static partial void HumanSeatD1(GodState s, int god, ref int seat);
    }
}
