namespace God.Sim.Population.Genetics
{
    /// <summary>
    /// <c>SimTuning.Genetics</c> (F0.12): M4's inheritance numbers (BIOLOGICAL_INHERITANCE 3). Written only when changed. OWNER: M4.
    /// <see cref="MutationPer10k"/> is the owner's Q4 default (40 per 10,000 allele copies, deliberately accelerated; 0 and
    /// lower rates are supported, M4.30).
    /// </summary>
    public sealed class GeneticsTuning : IOmitWhenDefault
    {
        /// <summary>Mutation chance per allele copy per gamete, per 10,000.</summary>
        public int MutationPer10k = 40;
        /// <summary>A mutation steps the allele by 1..MutationStepMax.</summary>
        public int MutationStepMax = 24;
        /// <summary>Crossover chance between neighbouring loci of a chromosome (permille): about 1.05 per chromosome.</summary>
        public int CrossoverPerGapPermille = 150;
        /// <summary>A founder allele is the sum of 4 draws of NextRange(0, FounderSpread): mean about 126, sd about 37.</summary>
        public int FounderSpread = 64;
        /// <summary>Generations of pedigree the relatedness search walks.</summary>
        public int InbreedingCheckDepth = 4;
        /// <summary>1: every child copies a fixed clone genome (BIOLOGICAL 4.5 "GameSetup.Genetics = Fixed"; a tuning path, 12.13).</summary>
        public int Fixed;
        /// <summary>
        /// Scale of the developmental noise (permille of each group's default half-width, BIOLOGICAL 4.3): 1000 default, 0 for
        /// genes only (tests of the genotypic value).
        /// </summary>
        public int NoisePermille = 1000;
        /// <summary>The visible-trait spread the resemblance cue divides by (a gene-map constant, never the true population).</summary>
        public int VisibleSpread = 120;
    }
}
