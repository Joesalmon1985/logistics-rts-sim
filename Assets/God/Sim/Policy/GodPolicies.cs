using System.Collections.Generic;
using God.Sim.Information;

namespace God.Sim.Policy
{
    /// <summary>
    /// F1.11 (contracts 12): the god-level policies. <see cref="IdleGod"/> never acts (the control of pin P2). OWNER: D1
    /// (<c>StewardGod</c> follows in D1 Tier 2).
    /// </summary>
    public static class GodPolicies
    {
        public const string Idle = "IdleGod";

        /// <summary>Adds the god-level policies to a registry (called by <c>StandardPolicies.Registry</c>).</summary>
        public static void Register(PolicyRegistry r)
        {
            r.Register(PolicyLevel.God, Idle, ctx => new IdleGod(), "F1: a god that never acts (the control for P2).");
        }
    }

    /// <summary>A god policy that never issues a command.</summary>
    public sealed class IdleGod : IGodPolicy
    {
        public string Name => GodPolicies.Idle;
        public PolicyLevel Level => PolicyLevel.God;
        public void Decide(in IDivineView view, ref Rng rng, List<Command> outCommands) { }
    }
}
