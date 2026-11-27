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
            // D1 (contracts 2, G1): one god per initial faction, GodId = faction + 1, seats from GameSetup.HumanGod
            s.Divinity.Gods = Gods.Create(s);
            s.Divinity.Interventions = new InterventionRegistry();
        }

        /// <summary>Registers handlers and systems (D1: the configuration handlers and <see cref="DivinitySystem"/> at 230).</summary>
        public static void Install(GodSim sim)
        {
            if (sim.State.Divinity == null) return; // Enabled = 0: no handlers, no system (pin P1)
            Gods.Install(sim);
            sim.AddSystem(DivinitySystem.SystemOrder, new DivinitySystem());
            // flagged additive edit (D2): the packages' handler slots, in package order
            InstallD2(sim);
            InstallD3(sim);
            InstallD4(sim);
            InstallD5(sim);
            InstallD6(sim);
            InstallD7(sim);
        }
        static partial void InstallD2(GodSim sim);
        static partial void InstallD3(GodSim sim);
        static partial void InstallD4(GodSim sim);
        static partial void InstallD5(GodSim sim);
        static partial void InstallD6(GodSim sim);
        static partial void InstallD7(GodSim sim);

        /// <summary>A new mind: its part of this module (none in F1: the mind slot stays null until D1 fills it).</summary>
        public static void NewMind(GodState s, Npc npc) { }

        /// <summary>A person died (called by the hooks' <c>OnDeath</c> only while the state exists).</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc) { }

        /// <summary>The module's invariants (D1): G1 gods one per faction, contiguous, never shrinking; G3 no god owner; M1 0 &lt;= Mythic &lt;= max.</summary>
        public static void Check(GodState s, List<string> bad)
        {
            var reg = s.Divinity?.Gods;
            if (s.Divinity == null) return;
            if (reg == null) { bad.Add("G1: divinity state without a god registry"); return; }
            if (reg.Gods.Count != s.FactionCount) bad.Add("G1: " + reg.Gods.Count + " gods for " + s.FactionCount + " factions");
            if (reg.Days.Count != reg.Gods.Count) bad.Add("G1: day records " + reg.Days.Count + " for " + reg.Gods.Count + " gods");
            for (int i = 0; i < reg.Gods.Count; i++)
            {
                var g = reg.Gods[i];
                if (g.Id != i + 1) bad.Add("G1: god at " + i + " has id " + g.Id);
                if (g.Mythic < 0 || g.Mythic > g.MythicMax) bad.Add("M1: god " + g.Id + " Mythic " + g.Mythic + " outside 0.." + g.MythicMax);
                if (g.CooldownUntil == null || g.CooldownUntil.Length != Gods.KindCount) bad.Add("G1: god " + g.Id + " cooldown array");
            }
            // G3: OwnerKind has no God value, so nothing can be owned by a god (checked by the type: contracts 2)
            if (System.Enum.IsDefined(typeof(OwnerKind), "God")) bad.Add("G3: OwnerKind.God exists");
        }

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

        /// <summary>D1: the seat is <see cref="GodRecord.Seat"/> once the registry exists (1 Human, 0 anything else).</summary>
        static partial void HumanSeatD1(GodState s, int god, ref int seat)
        {
            var reg = s.Divinity?.Gods;
            if (reg == null) return;
            var g = reg.Of(god);
            seat = g != null && g.Seat == GodSeat.Human ? 1 : 0;
        }
    }
}
