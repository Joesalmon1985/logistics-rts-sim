namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.6): a brain: from the observation, the brain state and the legality mask, one control. Deterministic: the
    /// only randomness is <paramref name="rng"/>, the person's keyed policy stream. Implementations: UN6 (Untrained, Trained, labelled
    /// Fallback); the teacher exists only in <c>DotNet/God.Train</c> and test fixtures.
    /// </summary>
    public interface IPrimitivePolicy
    {
        PrimitiveControl Decide(in Percept p, short[] obs, ref BrainState b, in ActMask m, ref Rng rng);
    }
}
