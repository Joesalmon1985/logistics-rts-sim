using God.Sim.Learning;

namespace God.Sim.Culture
{
    /// <summary>
    /// CULTURAL_TRANSMISSION 4.3: the adoption test of one exposure, one shared pure function (LEAD_RULINGS R6: owned by D6;
    /// C10's cultural pass calls the same function). <c>odds = BaseRate x Susceptibility x (s_M + 32) / 288 x (500 + Esteem) / 500
    /// x (1000 + Synergy + FrequencyBias) / 1000 x TraitCostFactor</c>, clamped to 0..950 per mille; the caller draws against it
    /// with its own keyed stream. Integers only. OWNER: D6.
    /// </summary>
    public static class AdoptionTest
    {
        /// <param name="baseRatePermille">BaseRate(channel), per mille.</param>
        /// <param name="susceptibilityPermille">Susceptibility(L), per mille (1000 = x1).</param>
        /// <param name="modelStrength">The model's strength of the trait, 0..255.</param>
        /// <param name="esteem">The learner's esteem of the model, 0..255.</param>
        /// <param name="synergyPermille">Synergy + frequency bias, per mille (0 = x1).</param>
        /// <param name="costFactorPermille">TraitCostFactor, per mille (1000 = x1).</param>
        public static int Odds(int baseRatePermille, int susceptibilityPermille, int modelStrength, int esteem, int synergyPermille, int costFactorPermille)
        {
            long o = baseRatePermille;
            o = o * susceptibilityPermille / 1000;
            o = o * (Fx.Clamp(modelStrength, 0, 255) + 32) / 288;
            o = o * (500 + Fx.Clamp(esteem, 0, 255)) / 500;
            o = o * (1000 + synergyPermille) / 1000;
            o = o * costFactorPermille / 1000;
            return (int)System.Math.Max(0, System.Math.Min(950, o));
        }
    }
}
