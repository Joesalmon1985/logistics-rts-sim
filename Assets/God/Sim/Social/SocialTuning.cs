namespace God.Sim.Social
{
    /// <summary>
    /// <c>SimTuning.Social</c> (F0.12): M5's numbers. Written to JSON only when changed, so stage 3 and M3 hashes stay.
    /// OWNER: M5. Switches (SHARED_CONTRACTS 7.3): <see cref="Enabled"/> 0 turns social memory, recognition, gestures,
    /// barter and gifts off; <see cref="Gestures"/> 0 turns off Say candidates and decoding.
    /// </summary>
    public sealed class SocialTuning : IOmitWhenDefault
    {
        public int Enabled = 1;
        public int Gestures = 1;
    }
}
