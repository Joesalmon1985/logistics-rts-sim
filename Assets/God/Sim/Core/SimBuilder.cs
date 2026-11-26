using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>A sim as a host gets it from <see cref="SimBuilder"/>: the sim and the AI director playing its bot factions (or null).</summary>
    public sealed class BuiltSim
    {
        public GodSim Sim;
        public AiDirector Director;
    }

    /// <summary>
    /// The one way both hosts build a game (HEADLESS_SIMULATION 9.2): the Unity player (<c>GodBootstrap</c>, <c>AiHost</c>,
    /// <c>SmokeRun</c>) and the headless runner. A new game is <see cref="GodSim.Create(GameSetup, SimTuning)"/> plus, for
    /// every faction without a human, an AI brain with <see cref="AiTuning.Default"/> and an <see cref="AiTrader"/> (which
    /// also becomes the game's trade judge). Because both hosts call this, the player and the runner give the same hash for
    /// the same setup, seed and external commands.
    /// </summary>
    public static class SimBuilder
    {
        /// <summary>The policy name of the stage 3 faction AI (the only faction policy before M1's registry).</summary>
        public const string StageThreeAi = "AiBrain";

        /// <summary>A new game; with <paramref name="attachAi"/>, AI brains for every faction but the human one.</summary>
        public static BuiltSim Create(GameSetup setup, SimTuning tuning = null, bool attachAi = true)
        {
            var sim = GodSim.Create(setup, tuning);
            return new BuiltSim { Sim = sim, Director = attachAi ? AttachAi(sim) : null };
        }

        /// <summary>A loaded game, with the AI re-attached (brains rebuild from state; the trade judge is bound again).</summary>
        public static BuiltSim FromSave(SaveFile file, bool attachAi = true)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            var sim = file.ToSim();
            return new BuiltSim { Sim = sim, Director = attachAi ? AttachAi(sim) : null };
        }

        /// <summary>
        /// Gives each listed faction (default: every faction but the human one) a brain that trades. Call once per sim. Returns
        /// the director (a sim system, so brains think on sim ticks whatever steps the sim).
        /// </summary>
        public static AiDirector AttachAi(GodSim sim, IEnumerable<Faction> factions = null, IAiTrader trader = null)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            return AiDirector.Attach(sim, factions ?? AiDirector.NonHuman(sim.State), AiTuning.Default, trader ?? new AiTrader());
        }
    }
}
