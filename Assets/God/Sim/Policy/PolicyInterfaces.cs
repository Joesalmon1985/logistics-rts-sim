using System.Collections.Generic;
using God.Sim.Information;

namespace God.Sim.Policy
{
    /// <summary>The four policy levels (DESIGN_CONVENTIONS §7). Scenario files name them in lower case.</summary>
    public enum PolicyLevel { Civilian = 0, Military = 1, Institution = 2, Faction = 3 }

    /// <summary>
    /// What every policy has: a registry name and its level. A policy decides only from what its level is given (an
    /// observation, its own mind, a view) and acts only through intents or commands. No policy type may reference
    /// <see cref="GodState"/>, a module state or the true-state overlay (reflection test <c>Policies_DoNotReferenceGodState</c>).
    /// </summary>
    public interface IPolicy
    {
        /// <summary>The <see cref="PolicyRegistry"/> name it was created under.</summary>
        string Name { get; }
        PolicyLevel Level { get; }
    }

    /// <summary>One civilian NPC (M3 onwards). One intent per think.</summary>
    public interface INpcPolicy : IPolicy
    {
        NpcIntent Decide(in NpcObservation obs, MindView mind, ref Rng rng);
    }

    /// <summary>One NPC on military duty: combat decisions only (MILITARY_DESIGN "IMilitaryPolicy"). Returns a combat-subset intent.</summary>
    public interface IMilitaryPolicy : IPolicy
    {
        NpcIntent Decide(in SoldierObservation obs, MindView mind, ref Rng rng);
    }

    /// <summary>One institution's management (M12 onwards): zero or more institution commands per think.</summary>
    public interface IInstitutionPolicy : IPolicy
    {
        void Decide(in InstitutionView view, ref Rng rng, List<Command> outCommands);
    }

    /// <summary>
    /// A faction's state, AI or human: zero or more state commands per think, the same commands the HUD sends
    /// (<see cref="FactionCommandSet"/>). It reads only its <see cref="FactionKnowledgeView"/>.
    /// </summary>
    public interface IFactionPolicy : IPolicy
    {
        void Decide(in FactionKnowledgeView view, ref Rng rng, List<Command> outCommands);
    }
}
