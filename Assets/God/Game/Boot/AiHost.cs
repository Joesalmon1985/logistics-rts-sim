using System.Collections.Generic;
using God.Sim;

namespace God.Game
{
    /// <summary>
    /// The one entry point the game uses for the faction AI (P9). <see cref="Start"/> attaches an <see cref="AiDirector"/>
    /// to the sim as a sim system, so the brains think on sim ticks inside <see cref="GodSim.Step()"/>, whatever steps the
    /// sim (the bootstrap at any speed, the smoke run, a test) and never on frames. The view still only sends commands
    /// through the brains; it never changes sim state. Pass <c>-godnoai</c> to start a game without brains.
    /// </summary>
    public static class AiHost
    {
        /// <summary>The director of the last game started (null before, or with <c>-godnoai</c>).</summary>
        public static AiDirector Director { get; private set; }

        /// <summary>
        /// Gives each listed faction a brain that trades (<see cref="AiTrader"/> unless another trader is given). Call once
        /// per sim (a loaded game included: brains rebuild from its state). It also makes the brains the game's trade judge,
        /// which a save does not keep, so offers reaching an AI faction are answered by its brain; offers to the human
        /// faction still wait for the HUD offer card.
        /// </summary>
        public static AiDirector Start(GodSim sim, IEnumerable<Faction> factions, IAiTrader trader = null)
        {
            // M0: the same construction as the headless runner (SimBuilder), so the player and the runner play the same game.
            Director = SimBuilder.AttachAi(sim, factions, trader);
            return Director;
        }

        /// <summary>A brain for every faction but the human one (all six in watch mode), unless <c>-godnoai</c> is given.</summary>
        public static AiDirector StartForGame(GodSim sim)
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-godnoai") >= 0) { Director = null; return null; }
            return Start(sim, AiDirector.NonHuman(sim.State));
        }

        /// <summary>True when a brain of the current director plays this faction of this sim.</summary>
        public static bool Drives(GodSim sim, Faction f) =>
            Director != null && Director.Enabled && Director.Sim == sim && Director.BrainOf(f) != null;
    }
}
