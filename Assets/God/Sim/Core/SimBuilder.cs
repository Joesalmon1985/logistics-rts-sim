using System;
using System.Collections.Generic;
using God.Sim.Policy;

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
        /// <summary>The policy name of the stage 3 faction AI (<see cref="PolicyAssignment.StageThreeFaction"/>).</summary>
        public const string StageThreeAi = PolicyAssignment.StageThreeFaction;

        /// <summary>
        /// A new game; with <paramref name="attachAi"/>, the faction policies of <paramref name="policies"/> (default: the
        /// stage 3 AI brain) for every faction but the human one, and its military policies.
        /// </summary>
        public static BuiltSim Create(GameSetup setup, SimTuning tuning = null, bool attachAi = true, PolicyAssignment policies = null)
        {
            var sim = GodSim.Create(setup, tuning);
            return new BuiltSim { Sim = sim, Director = attachAi ? AttachPolicies(sim, policies) : null };
        }

        /// <summary>A loaded game, with the policies re-attached (brains rebuild from state; the trade judge is bound again).</summary>
        public static BuiltSim FromSave(SaveFile file, bool attachAi = true, PolicyAssignment policies = null)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            var sim = file.ToSim();
            return new BuiltSim { Sim = sim, Director = attachAi ? AttachPolicies(sim, policies) : null };
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

        /// <summary>
        /// M1: the scenario's policy assignment through the registry (<see cref="StandardPolicies"/>). The military policy of
        /// every faction is set on the military system; every non-human faction gets the faction policy it is assigned. When
        /// no faction gets one (all <c>none</c>), no director is attached at all, exactly as before M1. Unknown names throw
        /// <see cref="PolicyException"/> before the sim is touched.
        /// </summary>
        public static AiDirector AttachPolicies(GodSim sim, PolicyAssignment policies = null, IAiTrader trader = null)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            policies = policies ?? PolicyAssignment.Stage3Default();
            trader = trader ?? new AiTrader();
            var registry = StandardPolicies.Registry(sim, AiTuning.Default, trader);
            policies.Validate(registry);
            if (policies.Defaults[(int)PolicyLevel.Institution] != PolicyRegistry.None || policies.Scoped[(int)PolicyLevel.Institution].Count > 0)
                throw new PolicyException("policies.institution: no institutions exist before M12");
            if (policies.Defaults[(int)PolicyLevel.Civilian] != PolicyRegistry.None || policies.Scoped[(int)PolicyLevel.Civilian].Count > 0)
                throw new PolicyException("policies.civilian: no NPCs exist before M3");

            var mil = MilitaryOf(sim);
            foreach (var f in sim.State.Setup.Factions)
            {
                string name = policies.NameFor(PolicyLevel.Military, (int)f);
                if (name == PolicyRegistry.None) throw new PolicyException("policies.military: soldiers must have a policy ('none' is not allowed)");
                if (mil != null && name != PolicyAssignment.StageThreeMilitary) mil.SetPolicy(f, registry.CreateMilitary(name, (int)f));
            }

            var seated = new List<Faction>();
            foreach (var f in AiDirector.NonHuman(sim.State))
                if (policies.NameFor(PolicyLevel.Faction, (int)f) != PolicyRegistry.None) seated.Add(f);
            if (seated.Count == 0) return null;
            return AiDirector.Attach(sim, seated, policies, registry, AiTuning.Default, trader);
        }

        /// <summary>The sim's military system (null when a test sim has none).</summary>
        public static MilitarySystem MilitaryOf(GodSim sim)
        {
            foreach (var sys in sim.Systems) if (sys is MilitarySystem m) return m;
            return null;
        }
    }
}
