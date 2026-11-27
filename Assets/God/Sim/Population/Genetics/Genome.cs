using System;
using System.Collections.Generic;

namespace God.Sim.Population.Genetics
{
    /// <summary>The traits of the gene map (BIOLOGICAL_INHERITANCE 3): physical, appearance, 12 personality, 4 instinct gains, 3 learning.</summary>
    public static class GeneTrait
    {
        public const int Strength = 0, Endurance = 1, Size = 2, Health = 3, Fertility = 4, Lifespan = 5, Perception = 6,
            Features = 7, Complexion = 8, Build = 9,
            /// <summary>The 12 personality traits are <c>Personality0 + Trait.*</c> (aggression ... kin weight).</summary>
            Personality0 = 10,
            HungerGain = 22, FearGain = 23, SocialGain = 24, NurtureGain = 25,
            LearnRate = 26, ForgetRate = 27, Curiosity = 28;
        public const int Count = 29;

        public static bool IsPersonality(int t) => t >= Personality0 && t < Personality0 + Trait.Count;
    }

    /// <summary>One row of the gene map (data): the trait it adds to, its weight, dominance and mutation bias.</summary>
    public readonly struct LocusDef
    {
        public readonly byte Trait;
        public readonly byte Weight;
        /// <summary>500 additive; 1000 the higher allele dominant; 0 the lower allele dominant.</summary>
        public readonly short DominancePermille;
        /// <summary>The chance a mutation goes down (500 unbiased).</summary>
        public readonly short DownBiasPermille;

        public LocusDef(int trait, int weight, int dominance, int downBias)
        {
            Trait = (byte)trait; Weight = (byte)weight; DominancePermille = (short)dominance; DownBiasPermille = (short)downBias;
        }
    }

    /// <summary>
    /// The default gene map (BIOLOGICAL_INHERITANCE 3, 64 loci on 8 chromosomes of 8): 3 loci for each physical trait, 2 for
    /// each appearance and personality trait, 1 for each instinct gain, 3 for each learning trait. Loci are spread over the
    /// chromosomes (slot k sits at locus 23k mod 64) so a trait's loci are only partly linked. Health, Fertility and Lifespan
    /// loci have the higher allele dominant and mutations biased down (the source of inbreeding depression, 4.7: M4
    /// calibration, the default 750 of the design gave about 10 permille at full-sibling mating, below M4.13's bound).
    /// </summary>
    public static class GeneMap
    {
        public const int Loci = 64, Chromosomes = 8, LociPerChromosome = 8;

        public static readonly ReadOnlyArray<LocusDef> Default = Build(1000, 600, 500);

        /// <summary>A map with the given dominance and down bias on the health loci and the given down bias elsewhere (tests).</summary>
        public static ReadOnlyArray<LocusDef> Build(int healthDominance, int healthDownBias, int otherDownBias)
        {
            var slots = new List<int>();
            for (int t = 0; t < GeneTrait.Count; t++)
            {
                int n = t <= GeneTrait.Perception ? 3 : t <= GeneTrait.Build ? 2 : GeneTrait.IsPersonality(t) ? 2 : t <= GeneTrait.NurtureGain ? 1 : 3;
                for (int k = 0; k < n; k++) slots.Add(t);
            }
            if (slots.Count != Loci) throw new InvalidOperationException("gene map has " + slots.Count + " loci");
            var map = new LocusDef[Loci];
            for (int k = 0; k < Loci; k++)
            {
                int t = slots[k];
                bool health = t == GeneTrait.Health || t == GeneTrait.Fertility || t == GeneTrait.Lifespan;
                map[k * 23 % Loci] = new LocusDef(t, 1, health ? healthDominance : 500, health ? healthDownBias : otherDownBias);
            }
            return new ReadOnlyArray<LocusDef>(map);
        }

        /// <summary>The default half-width of a trait's developmental noise (permille of the 0..1000 genotypic scale), 4.3.</summary>
        public static int NoiseOf(int trait)
        {
            // half-widths for the h2 of 4.3, given each trait's loci (the genotypic sd is about 59 permille for 3 loci, 72 for 2, 102 for 1)
            if (trait >= GeneTrait.Features && trait <= GeneTrait.Build) return 65;          // appearance, 2 loci: h2 about 0.8
            if (trait == GeneTrait.Health || trait == GeneTrait.Fertility) return 110;      // h2 0.35-0.5
            if (trait == GeneTrait.Lifespan) return 130;                                     // h2 0.3-0.4
            if (trait <= GeneTrait.Perception) return 80;                                    // other physical, 3 loci: 0.5-0.65
            if (GeneTrait.IsPersonality(trait)) return 125;                                  // personality, 2 loci: 0.4-0.55
            if (trait >= GeneTrait.HungerGain && trait <= GeneTrait.NurtureGain) return 177; // instinct gains, 1 locus: 0.4-0.55
            return 100;                                                                      // learning, 3 loci: 0.4-0.55
        }
    }

    /// <summary>One mutation in a gamete (for <c>GeneMutated</c>): locus, allele before and after.</summary>
    public struct Mutation
    {
        public int Locus, Old, New;
    }

    /// <summary>
    /// Genome operations (BIOLOGICAL_INHERITANCE 4): a genome is <c>byte[128]</c>, <c>[2l]</c> the maternal and <c>[2l+1]</c> the
    /// paternal copy of locus l. Only this class, <see cref="PhenotypeBuilder"/>, reproduction and the genetics metrics read
    /// alleles (G4.7). <see cref="Gamete"/> takes a genome and a stream and nothing else, so nothing learned, owned or taught
    /// can reach a child's genes (M4.11).
    /// </summary>
    public static class GenomeOps
    {
        public const int Length = GeneMap.Loci * 2;

        /// <summary>A founder or immigrant genome: each allele the sum of 4 draws of NextRange(0, spread), locus 0 to 63, maternal then paternal.</summary>
        public static byte[] Founder(Rng r, int spread)
        {
            var g = new byte[Length];
            for (int i = 0; i < Length; i++)
            {
                int a = 0;
                for (int k = 0; k < 4; k++) a += r.NextRange(0, Math.Max(1, spread));
                g[i] = (byte)Math.Min(255, a);
            }
            return g;
        }

        /// <summary>
        /// One gamete (4.5): chromosomes ascending, a random starting copy, a crossover between neighbouring loci with
        /// <c>CrossoverPerGapPermille</c>, then a mutation per copy with <c>MutationPer10k</c> (down with the locus bias, a step of
        /// 1..MutationStepMax). <paramref name="mutations"/> (may be null) receives the mutations; returns the crossovers made.
        /// </summary>
        public static int Gamete(byte[] g, Rng rng, GeneticsTuning t, ReadOnlyArray<LocusDef> map, byte[] into, List<Mutation> mutations)
        {
            int crossovers = 0;
            for (int c = 0; c < GeneMap.Chromosomes; c++)
            {
                int copy = rng.NextInt(2);
                for (int i = 0; i < GeneMap.LociPerChromosome; i++)
                {
                    if (i > 0 && rng.Chance(t.CrossoverPerGapPermille, 1000)) { copy ^= 1; crossovers++; }
                    int l = c * GeneMap.LociPerChromosome + i;
                    int a = g[2 * l + copy];
                    if (rng.Chance(t.MutationPer10k, 10000))
                    {
                        bool down = rng.Chance(map[l].DownBiasPermille, 1000);
                        int step = 1 + rng.NextInt(Math.Max(1, t.MutationStepMax));
                        int b = Math.Max(0, Math.Min(255, a + (down ? -step : step)));
                        mutations?.Add(new Mutation { Locus = l, Old = a, New = b });
                        a = b;
                    }
                    into[l] = (byte)a;
                }
            }
            return crossovers;
        }

        /// <summary>A child's genome from a maternal and a paternal gamete.</summary>
        public static byte[] Combine(byte[] mat, byte[] pat)
        {
            var g = new byte[Length];
            for (int l = 0; l < GeneMap.Loci; l++) { g[2 * l] = mat[l]; g[2 * l + 1] = pat[l]; }
            return g;
        }

        /// <summary>
        /// The genotypic value of a trait (4.1), permille: per locus <c>lo + (hi - lo) Dominance / 1000</c>, weighted mean over the
        /// trait's loci, times 1000 / 255. Deterministic.
        /// </summary>
        public static int Genotypic(byte[] g, int trait, ReadOnlyArray<LocusDef> map)
        {
            long sum = 0; int w = 0;
            for (int l = 0; l < GeneMap.Loci; l++)
            {
                var d = map[l];
                if (d.Trait != trait) continue;
                int a = g[2 * l], b = g[2 * l + 1];
                int lo = Math.Min(a, b), hi = Math.Max(a, b);
                sum += (long)d.Weight * (lo * 1000 + (hi - lo) * d.DominancePermille);
                w += d.Weight;
            }
            if (w == 0) return 500;
            return (int)(sum / w / 255);
        }

        /// <summary>A 64-bit FNV hash of the alleles (the <c>NpcBorn</c> genome hash).</summary>
        public static long Hash(byte[] g)
        {
            if (g == null) return 0;
            var h = new StateHasher();
            foreach (byte b in g) h.Add(b);
            return (long)h.Value;
        }
    }

    /// <summary>The phenotype (BIOLOGICAL_INHERITANCE 3), all integers: physical 0..1000, appearance 0..1000, instinct and learning gains 500..1500.</summary>
    public struct Phenotype
    {
        public int Strength, Endurance, Size, Health, Fertility, Lifespan, Perception;
        public int Features, Complexion, Build;
        public int HungerGain, FearGain, SocialGain, NurtureGain;
        public int LearnRate, ForgetRate, Curiosity;
    }

    /// <summary>
    /// Builds the phenotype from a genome (4.3): <c>clamp(GP + env + noise)</c>, noise = the trait's half-width times a centred sum
    /// of 4 draws from <c>Rng.Fork("develop", npcId)</c> (separate from conception, so noise never shifts genomes). The birth
    /// part (personality, instincts, learning, appearance) is fixed at birth; the physical part at adulthood, with the
    /// childhood nutrition term on Size, Strength and Health (-150..0).
    /// </summary>
    public static class PhenotypeBuilder
    {
        /// <summary>The noise draws for each trait in a fixed order (so the birth and adult parts share one stream).</summary>
        public static int[] Noise(Rng develop, int scalePermille)
        {
            var n = new int[GeneTrait.Count];
            for (int t = 0; t < GeneTrait.Count; t++)
            {
                int d = GeneMap.NoiseOf(t) * scalePermille / 1000;
                int s = 0;
                for (int k = 0; k < 4; k++) s += develop.NextRange(-d, d + 1);
                n[t] = s / 2;
            }
            return n;
        }

        /// <summary>The birth part: appearance, personality (-100..100, into <paramref name="traits"/>), instinct gains and learning rates.</summary>
        public static void Birth(byte[] g, ReadOnlyArray<LocusDef> map, int[] noise, ref Phenotype p, int[] traits)
        {
            int V(int t) => GenomeOps.Genotypic(g, t, map) + noise[t];
            p.Features = Clamp(V(GeneTrait.Features), 0, 1000);
            p.Complexion = Clamp(V(GeneTrait.Complexion), 0, 1000);
            p.Build = Clamp(V(GeneTrait.Build), 0, 1000);
            for (int k = 0; k < Trait.Count; k++) traits[k] = Clamp((V(GeneTrait.Personality0 + k) - 500) / 2, -100, 100);
            p.HungerGain = Clamp(500 + V(GeneTrait.HungerGain), 500, 1500);
            p.FearGain = Clamp(500 + V(GeneTrait.FearGain), 500, 1500);
            p.SocialGain = Clamp(500 + V(GeneTrait.SocialGain), 500, 1500);
            p.NurtureGain = Clamp(500 + V(GeneTrait.NurtureGain), 500, 1500);
            p.LearnRate = Clamp(500 + V(GeneTrait.LearnRate), 500, 1500);
            p.ForgetRate = Clamp(500 + V(GeneTrait.ForgetRate), 500, 1500);
            p.Curiosity = Clamp(500 + V(GeneTrait.Curiosity), 500, 1500);
            // the physical part is provisional until adulthood (a newborn's health uses it)
            Adult(g, map, noise, 1000, ref p);
        }

        /// <summary>The physical part, fixed once at adulthood from the childhood nutrition mean (0..1000; 1000 = never hungry).</summary>
        public static void Adult(byte[] g, ReadOnlyArray<LocusDef> map, int[] noise, int childNutrition, ref Phenotype p)
        {
            int env = -150 * (1000 - Clamp(childNutrition, 0, 1000)) / 1000;
            int V(int t) => GenomeOps.Genotypic(g, t, map) + noise[t];
            p.Strength = Clamp(V(GeneTrait.Strength) + env, 0, 1000);
            p.Endurance = Clamp(V(GeneTrait.Endurance), 0, 1000);
            p.Size = Clamp((V(GeneTrait.Size) + GenomeOps.Genotypic(g, GeneTrait.Build, map)) / 2 + env, 0, 1000);
            p.Health = Clamp(V(GeneTrait.Health) + env, 0, 1000);
            p.Fertility = Clamp(V(GeneTrait.Fertility), 0, 1000);
            p.Lifespan = Clamp(V(GeneTrait.Lifespan), 0, 1000);
            p.Perception = Clamp(V(GeneTrait.Perception), 0, 1000);
        }

        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
    }

    /// <summary>The true pedigree of one person (BIOLOGICAL 3): kept for everyone ever born or arrived. Telemetry, tests and the lab only (G4.2).</summary>
    public struct PedigreeRecord
    {
        public int Id, Mother, Father;
        public long BornTick;
    }

    /// <summary>
    /// True relatedness from the pedigree (4.6), for measurement and tests only: the kinship coefficient phi in 1/1024 units,
    /// <c>phi(a, a) = (1 + F_a)/2</c>, for a younger a: <c>phi(a, b) = (phi(mother_a, b) + phi(father_a, b)) / 2</c>; founders and
    /// immigrants unrelated. r = 2 phi (in permille: r * 1000 / 1024). The search stops at the depth.
    /// </summary>
    public static class Relatedness
    {
        public const int One = 1024;

        public static int IndexOf(List<PedigreeRecord> ped, int id)
        {
            int lo = 0, hi = ped.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = ped[mid].Id;
                if (v == id) return mid;
                if (v < id) lo = mid + 1; else hi = mid - 1;
            }
            return -1;
        }

        /// <summary>The kinship coefficient phi(a, b) in 1/1024 (0 when either is unknown).</summary>
        public static int Phi(List<PedigreeRecord> ped, int a, int b, int depth)
        {
            if (a <= 0 || b <= 0 || depth < 0) return 0;
            if (a == b)
            {
                int i = IndexOf(ped, a);
                int f = i < 0 ? 0 : Phi(ped, ped[i].Mother, ped[i].Father, depth - 1);
                return (One + f) / 2;
            }
            if (a < b) { int t = a; a = b; b = t; } // the younger (higher id) first
            int k = IndexOf(ped, a);
            if (k < 0) return 0;
            var r = ped[k];
            if (r.Mother == 0 && r.Father == 0) return 0;
            return (Phi(ped, r.Mother, b, depth - 1) + Phi(ped, r.Father, b, depth - 1)) / 2;
        }

        /// <summary>The inbreeding coefficient F of a person (1/1024): phi of its parents.</summary>
        public static int F(List<PedigreeRecord> ped, int id, int depth)
        {
            int i = IndexOf(ped, id);
            return i < 0 ? 0 : Phi(ped, ped[i].Mother, ped[i].Father, depth);
        }

        /// <summary>True relatedness r in permille (0..1000+): 2 phi.</summary>
        public static int RPermille(List<PedigreeRecord> ped, int a, int b, int depth) => Phi(ped, a, b, depth) * 2 * 1000 / One;
    }

    /// <summary>The resemblance cue (4.6): a score, never a belief, over the visible traits only.</summary>
    public static class Resemblance
    {
        public static int Score(in Phenotype a, in Phenotype b, int visibleSpread)
        {
            int d = Math.Abs(a.Features - b.Features) + Math.Abs(a.Complexion - b.Complexion) + Math.Abs(a.Build - b.Build);
            return PhenotypeBuilder.Clamp(1000 - 500 * d / (3 * Math.Max(1, visibleSpread)), 0, 1000);
        }
    }
}
