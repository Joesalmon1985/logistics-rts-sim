namespace God.Sim.Population.Households
{
    /// <summary>
    /// <c>SimTuning.Households</c> (F0.12): M4's numbers. Written only when changed. OWNER: M4. <see cref="Enabled"/> 0 turns
    /// off ageing, births, households, kin cues, care and estates (SHARED_CONTRACTS 7.3); <see cref="Immigration"/> replaces a
    /// scenario key (12.13).
    /// </summary>
    public sealed class HouseholdTuning : IOmitWhenDefault
    {
        public int Enabled = 1;
        public int Immigration;
    }
}
