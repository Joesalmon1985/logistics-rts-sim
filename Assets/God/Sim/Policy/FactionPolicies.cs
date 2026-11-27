using System;
using System.Collections.Generic;
using God.Sim.Information;

namespace God.Sim.Policy
{
    /// <summary>
    /// The stage 3 faction AI as an <see cref="IFactionPolicy"/> (registry name <c>AiBrain</c>): the M1 seam around
    /// <see cref="AiBrain"/>, with its behaviour unchanged (M1.4: stage 3 seeds 1-12 replay with M0's hashes). The
    /// director gives it the faction's view and the think stream and submits what it returns.
    /// <para>TEMPORARY (DEV_LOG "Temporary mechanisms", removal M14): inside, <see cref="AiBrain"/> and its
    /// <see cref="AiTrader"/> still read the true state, including what Q21 forbids a faction to know (partners' stock
    /// and needs when trading). This wrapper only checks that the view is the faction's own. The reflection test allows
    /// exactly that one reference (and nothing else in the policy namespaces reads state).</para>
    /// </summary>
    public sealed class AiBrainFactionPolicy : IFactionPolicy
    {
        private readonly AiBrain _brain;

        public AiBrainFactionPolicy(AiBrain brain) { _brain = brain ?? throw new ArgumentNullException(nameof(brain)); }

        public string Name => PolicyAssignment.StageThreeFaction;
        public PolicyLevel Level => PolicyLevel.Faction;
        public Faction Side => _brain.Side;
        /// <summary>The wrapped brain (the trade judge and tests use it).</summary>
        public AiBrain Brain => _brain;

        public void Decide(in FactionKnowledgeView view, ref Rng rng, List<Command> outCommands)
        {
            if (view == null || view.Faction != _brain.Side) throw new PolicyException("AiBrain for " + FactionInfo.Name(_brain.Side) + " was given another faction's view");
            _brain.Think(rng, outCommands);
        }
    }

    /// <summary>A faction that decides nothing (registry name <c>none</c> behaves alike; this is the explicit object for tests and tools).</summary>
    public sealed class IdleFactionPolicy : IFactionPolicy
    {
        public string Name => "idle";
        public PolicyLevel Level => PolicyLevel.Faction;
        public void Decide(in FactionKnowledgeView view, ref Rng rng, List<Command> outCommands) { }
    }
}
