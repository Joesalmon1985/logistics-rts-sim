using God.Sim.Policy;

namespace God.Sim
{
    /// <summary>
    /// The policies a normal game can name (HEADLESS_SIMULATION "policies"): one registry per sim, so the stage 3 brain's
    /// factory can close over that sim. Names:
    /// <list type="bullet">
    /// <item>faction: <c>AiBrain</c> (the stage 3 AI behind <see cref="AiBrainFactionPolicy"/>), <c>none</c>;</item>
    /// <item>military: <c>Stage3MilitaryPolicy</c> (the stage 3 soldier rules), <c>none</c> is refused by the military system (soldiers must decide);</item>
    /// <item>civilian (M3): <c>AssociativeNpcPolicy</c>; institution: only <c>none</c> until M12.</item>
    /// </list>
    /// </summary>
    public static class StandardPolicies
    {
        public static PolicyRegistry Registry(GodSim sim, AiTuning tuning = null, IAiTrader trader = null)
        {
            var r = new PolicyRegistry();
            r.Register(PolicyLevel.Faction, PolicyAssignment.StageThreeFaction,
                ctx => new AiBrainFactionPolicy(new AiBrain(sim, (Faction)ctx.Scope, tuning ?? AiTuning.Default, trader)),
                "The stage 3 faction AI (P9-P11), unchanged behind the M1 seam. Reads true state internally until M14.");
            r.Register(PolicyLevel.Military, PolicyAssignment.StageThreeMilitary, ctx => new Stage3MilitaryPolicy(),
                "The stage 3 soldier rules 1-4: nearest enemy in sight, guard, march on the first target, wait at the base.");
            r.Register(PolicyLevel.Civilian, Population.Policies.Associative, ctx => new AssociativeNpcPolicy(),
                "M3: instincts plus the NPC's own learned associations, personality, perceived cost and seeded exploration (COMMUNICATION 4.7).");
            GodPolicies.Register(r); // F1.11: IdleGod
            return r;
        }

        /// <summary>A registry without a sim, for validating scenario files (the factories are never called).</summary>
        public static PolicyRegistry ForValidation() => Registry(null);
    }
}
