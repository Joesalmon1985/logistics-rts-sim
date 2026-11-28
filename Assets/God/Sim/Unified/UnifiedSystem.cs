namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (LEAD_RULINGS R85, UN0 spec 5.5): the unified world's tick, installed only in a unified game at
    /// <see cref="UnifiedModule.SystemOrder"/> (251: after the population's person ticks, before miracles). It calls the packages'
    /// ordered slots through <see cref="UnifiedHooks.Tick"/>: world (UN1: dice clock, materialisation, forage), motor (UN2: action
    /// progress, ascending actor id), interactions (UN3b: reservation expiry, workstations), sites (UN4), exchange (UN7: hold-out
    /// lapses), conflict (UN8a), settlements (UB). Each slot is a no-op until its owner fills it.
    /// </summary>
    public sealed class UnifiedSystem : ISimSystem
    {
        public void Tick(GodState s, EventBuffer ev) => UnifiedHooks.Tick(s, ev);
    }
}
