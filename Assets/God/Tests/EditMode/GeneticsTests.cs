using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Genetics;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// M4's biological inheritance (BIOLOGICAL_INHERITANCE 10; ACCEPTANCE M4.8-M4.11, M4.30): gametes, crossover and mutation
    /// rates, offspring statistics, relatedness from a hand-made pedigree, the resemblance cue, and the separation of the
    /// four inheritances. Fixed seeds; every threshold is the acceptance table's.
    /// </summary>
    public class GeneticsTests
    {
        private static GeneticsTuning T(int mut = 40) => new GeneticsTuning { MutationPer10k = mut };

        private static byte[] Founder(ulong seed) => GenomeOps.Founder(new Rng(seed), 64);

        [Test]
        public void Gamete_IsDeterministic()
        {
            var g = Founder(1);
            var a = new byte[GeneMap.Loci]; var b = new byte[GeneMap.Loci];
            GenomeOps.Gamete(g, new Rng(77), T(), GeneMap.Default, a, null);
            GenomeOps.Gamete(g, new Rng(77), T(), GeneMap.Default, b, null);
            CollectionAssert.AreEqual(a, b);
        }

        /// <summary>Mutations per 100,000 allele copies at a rate (1,563 gametes of 64 copies); and the down-bias share.</summary>
        private static (int events, int down) Mutations(int per10k, ReadOnlyArray<LocusDef> map)
        {
            int events = 0, down = 0;
            var into = new byte[GeneMap.Loci];
            var muts = new List<Mutation>();
            for (int i = 0; i < 1563; i++)
            {
                var g = new byte[GenomeOps.Length];
                for (int k = 0; k < g.Length; k++) g[k] = 128; // mid alleles: a step never hits a bound
                muts.Clear();
                GenomeOps.Gamete(g, new Rng((ulong)(1000 + i)), T(per10k), map, into, muts);
                events += muts.Count;
                down += muts.Count(m => m.New < m.Old);
            }
            return (events, down);
        }

        [Test]
        public void Mutation_RateAndBias_M4_8()
        {
            var (e40, _) = Mutations(40, GeneMap.Default);
            TestContext.WriteLine("mutations per 100,000 copies at 40: " + e40);
            Assert.That(e40, Is.InRange(340, 460), "400 +- 60 per 100,000 copies at MutationPer10k 40");
            Assert.AreEqual(0, Mutations(0, GeneMap.Default).events, "0 gives exactly 0");
            var biased = GeneMap.Build(1000, 800, 800);
            var (e, down) = Mutations(40, biased);
            double share = (double)down / e;
            TestContext.WriteLine($"down share at bias 800: {share:F3} of {e}");
            Assert.That(share, Is.InRange(0.76, 0.84), "down-bias share 0.80 +- 0.04");
        }

        [Test]
        public void Mutation_RateConfigurable_M4_30()
        {
            var (e10, _) = Mutations(10, GeneMap.Default);
            TestContext.WriteLine("mutations per 100,000 copies at 10: " + e10);
            Assert.That(e10, Is.InRange(70, 130), "100 +- 30 per 100,000 copies at MutationPer10k 10");
            Assert.AreEqual(0, Mutations(0, GeneMap.Default).events);
            // per-scenario values are honoured: a game's own tuning, never a static (two games side by side)
            var a = HouseholdFixtures.Empty(1, t => t.Genetics.MutationPer10k = 0);
            var b = HouseholdFixtures.Empty(1, t => t.Genetics.MutationPer10k = 400);
            Assert.AreEqual(0, a.State.Tuning.Genetics.MutationPer10k);
            Assert.AreEqual(400, b.State.Tuning.Genetics.MutationPer10k);
            Assert.AreEqual(40, new SimTuning().Genetics.MutationPer10k, "the default is the owner's Q4 value");
        }

        [Test]
        public void Crossover_And_Segregation_M4_8()
        {
            // a genome whose maternal copies are 0 and paternal 255: each gamete allele shows which copy it came from
            var g = new byte[GenomeOps.Length];
            for (int l = 0; l < GeneMap.Loci; l++) { g[2 * l] = 0; g[2 * l + 1] = 255; }
            long crossovers = 0, maternal = 0, total = 0;
            var into = new byte[GeneMap.Loci];
            const int n = 4000;
            for (int i = 0; i < n; i++)
            {
                crossovers += GenomeOps.Gamete(g, new Rng((ulong)(50000 + i)), T(0), GeneMap.Default, into, null);
                foreach (byte a in into) { total++; if (a == 0) maternal++; }
            }
            double perChromosome = (double)crossovers / (n * GeneMap.Chromosomes);
            double share = (double)maternal / total;
            TestContext.WriteLine($"crossovers per chromosome {perChromosome:F3}; maternal share {share:F4}");
            Assert.That(perChromosome, Is.InRange(0.97, 1.13), "1.05 +- 0.08 crossovers per chromosome");
            Assert.That(share, Is.InRange(0.49, 0.51), "each parental allele 50% +- 1%");
        }

        /// <summary>A child of two genomes (the reproduction path: one gamete each, then combined).</summary>
        private static byte[] Child(byte[] m, byte[] f, ulong seed, int mut = 40)
        {
            var r = new Rng(seed);
            var a = new byte[GeneMap.Loci]; var b = new byte[GeneMap.Loci];
            GenomeOps.Gamete(m, r, T(mut), GeneMap.Default, a, null);
            GenomeOps.Gamete(f, r, T(mut), GeneMap.Default, b, null);
            return GenomeOps.Combine(a, b);
        }

        private static double Slope(IList<double> x, IList<double> y)
        {
            double mx = x.Average(), my = y.Average(), sxy = 0, sxx = 0;
            for (int i = 0; i < x.Count; i++) { sxy += (x[i] - mx) * (y[i] - my); sxx += (x[i] - mx) * (x[i] - mx); }
            return sxy / sxx;
        }

        private static double Var(IEnumerable<double> v) { var l = v.ToList(); double m = l.Average(); return l.Sum(x => (x - m) * (x - m)) / (l.Count - 1); }

        [Test]
        public void Offspring_FollowMidParent_M4_9()
        {
            const int families = 2000;
            int trait = GeneTrait.Personality0 + Trait.Generosity; // an additive trait
            var mid = new List<double>(); var kid = new List<double>(); var popG = new List<double>();
            var midP = new List<double>(); var kidP = new List<double>();
            var midH = new List<double>(); var kidH = new List<double>();
            for (int i = 0; i < families; i++)
            {
                var m = Founder((ulong)(10 * i + 1)); var f = Founder((ulong)(10 * i + 2));
                var c = Child(m, f, (ulong)(10 * i + 3), mut: 0);
                double gm = GenomeOps.Genotypic(m, trait, GeneMap.Default), gf = GenomeOps.Genotypic(f, trait, GeneMap.Default), gc = GenomeOps.Genotypic(c, trait, GeneMap.Default);
                mid.Add((gm + gf) / 2); kid.Add(gc); popG.Add(gm); popG.Add(gf);
                // phenotypes: genotypic value plus each person's own developmental noise
                double P(byte[] g, ulong s) => GenomeOps.Genotypic(g, trait, GeneMap.Default) + PhenotypeBuilder.Noise(new Rng(s), 1000)[trait];
                midP.Add((P(m, (ulong)(900000 + 3 * i)) + P(f, (ulong)(900001 + 3 * i))) / 2); kidP.Add(P(c, (ulong)(900002 + 3 * i)));
                int h = GeneTrait.Health;
                midH.Add((GenomeOps.Genotypic(m, h, GeneMap.Default) + GenomeOps.Genotypic(f, h, GeneMap.Default)) / 2.0); kidH.Add(GenomeOps.Genotypic(c, h, GeneMap.Default));
            }
            double meanDiff = kid.Zip(mid, (k, m) => k - m).Average();
            double ratio = Var(kid.Zip(mid, (k, m) => k - m)) / Var(popG);
            double gSlope = Slope(mid, kid), pSlope = Slope(midP, kidP), hSlope = Slope(midH, kidH);
            TestContext.WriteLine($"mean child - mid-parent {meanDiff:F2} permille; segregation variance / population variance {ratio:F3}; genotypic slope {gSlope:F3}; phenotypic slope {pSlope:F3}; Health (dominant) genotypic slope {hSlope:F3}");
            Assert.That(Math.Abs(meanDiff), Is.LessThanOrEqualTo(5), "mean (child - mid-parent) within +-5 permille");
            Assert.That(ratio, Is.InRange(0.42, 0.58), "variance about the mid-parent 0.5 x the population variance");
            Assert.That(gSlope, Is.InRange(0.93, 1.07), "genotypic slope 1.0 +- 0.07");
            Assert.That(pSlope, Is.InRange(0.40, 0.60), "phenotypic slope 0.50 +- 0.10");
            Assert.That(gSlope - hSlope, Is.GreaterThanOrEqualTo(0.05), "dominance lowers the narrow-sense slope by at least 0.05");
        }

        [Test]
        public void Personality_FromGenome_ChildrenNearMidParent_M4_9()
        {
            var sim = HouseholdFixtures.Empty(3);
            var s = sim.State;
            var node = LearningFixtures.Glade(sim);
            double sumChild = 0, sumMid = 0; int n = 0;
            for (int i = 0; i < 500; i++)
            {
                var m = Founder((ulong)(7000 + 2 * i)); var f = Founder((ulong)(7001 + 2 * i));
                var c = Child(m, f, (ulong)(80000 + i));
                var tm = new int[Trait.Count]; var tf = new int[Trait.Count]; var tc = new int[Trait.Count];
                var p = default(Phenotype);
                PhenotypeBuilder.Birth(m, GeneMap.Default, PhenotypeBuilder.Noise(new Rng((ulong)(3 * i + 1)), 1000), ref p, tm);
                PhenotypeBuilder.Birth(f, GeneMap.Default, PhenotypeBuilder.Noise(new Rng((ulong)(3 * i + 2)), 1000), ref p, tf);
                PhenotypeBuilder.Birth(c, GeneMap.Default, PhenotypeBuilder.Noise(new Rng((ulong)(3 * i + 3)), 1000), ref p, tc);
                for (int t = 0; t < Trait.Count; t++) { sumChild += tc[t]; sumMid += (tm[t] + tf[t]) / 2.0; n++; }
                // personality equals the genome mapping plus noise (no noise: exactly the mapping)
                var exact = new int[Trait.Count];
                PhenotypeBuilder.Birth(c, GeneMap.Default, new int[GeneTrait.Count], ref p, exact);
                for (int t = 0; t < Trait.Count; t++) Assert.AreEqual(Math.Max(-100, Math.Min(100, (GenomeOps.Genotypic(c, GeneTrait.Personality0 + t, GeneMap.Default) - 500) / 2)), exact[t]);
            }
            double diff = Math.Abs(sumChild / n - sumMid / n);
            TestContext.WriteLine($"children's mean trait {sumChild / n:F2}, mid-parent mean {sumMid / n:F2}");
            Assert.LessOrEqual(diff, 10, "children's mean trait within 10 of the mid-parent mean over 500 families");
        }

        [Test]
        public void Relatedness_HandMadePedigree_M4_10()
        {
            // founders 1,2 (a couple), 3,4 (another); children 5,6 of 1x2; 7 of 3x4; 8 of 5 and an unrelated 9; 10 of 6x7; 11 of 8x(12, unrelated); 13 of 10x14
            var ped = new List<PedigreeRecord>();
            void Add(int id, int m, int f) => ped.Add(new PedigreeRecord { Id = id, Mother = m, Father = f });
            Add(1, 0, 0); Add(2, 0, 0); Add(3, 0, 0); Add(4, 0, 0); Add(5, 1, 2); Add(6, 1, 2); Add(7, 3, 4); Add(8, 5, 9); Add(9, 0, 0);
            Add(10, 6, 7); Add(11, 8, 12); Add(12, 0, 0); Add(13, 10, 14); Add(14, 0, 0); Add(15, 5, 6);
            int R(int a, int b) => Relatedness.Phi(ped, a, b, 6) * 2;
            Assert.AreEqual(1024, R(1, 1) / 1, "self 1.0");
            Assert.AreEqual(512, R(5, 1), "parent and child 0.5");
            Assert.AreEqual(512, R(5, 6), "full siblings 0.5");
            Assert.AreEqual(256, R(8, 1), "grandparent 0.25");
            Assert.AreEqual(256, R(8, 6), "aunt or uncle and nephew or niece 0.25");
            Assert.AreEqual(128, R(8, 10), "first cousins 0.125");
            Assert.AreEqual(32, R(11, 13), "second cousins 0.03125 (depth 4 or more)");
            Assert.AreEqual(0, R(1, 3), "founders are unrelated");
            Assert.AreEqual(256, Relatedness.F(ped, 15, 6), "a child of full siblings has F = 0.25");
        }

        [Test]
        public void Resemblance_SiblingsVersusStrangers_AUC_M4_10()
        {
            const int pairs = 2000;
            var sib = new int[pairs]; var unrel = new int[pairs];
            int spread = new GeneticsTuning().VisibleSpread;
            Phenotype P(byte[] g, ulong s) { var p = default(Phenotype); PhenotypeBuilder.Birth(g, GeneMap.Default, PhenotypeBuilder.Noise(new Rng(s), 1000), ref p, new int[Trait.Count]); return p; }
            for (int i = 0; i < pairs; i++)
            {
                var m = Founder((ulong)(20000 + 2 * i)); var f = Founder((ulong)(20001 + 2 * i));
                var a = Child(m, f, (ulong)(30000 + 2 * i)); var b = Child(m, f, (ulong)(30001 + 2 * i));
                sib[i] = Resemblance.Score(P(a, (ulong)(40000 + 4 * i)), P(b, (ulong)(40001 + 4 * i)), spread);
                var x = Founder((ulong)(60000 + 2 * i)); var y = Founder((ulong)(60001 + 2 * i));
                unrel[i] = Resemblance.Score(P(x, (ulong)(40002 + 4 * i)), P(y, (ulong)(40003 + 4 * i)), spread);
            }
            // AUC: the chance a sibling pair resembles more than an unrelated pair (ties count half)
            Array.Sort(unrel);
            double wins = 0;
            foreach (int v in sib)
            {
                int lo = Array.BinarySearch(unrel, v); if (lo < 0) lo = ~lo; while (lo > 0 && unrel[lo - 1] == v) lo--;
                int hi = lo; while (hi < unrel.Length && unrel[hi] == v) hi++;
                wins += lo + 0.5 * (hi - lo);
            }
            double auc = wins / ((double)pairs * pairs);
            TestContext.WriteLine($"resemblance AUC {auc:F3} (VisibleSpread {spread})");
            Assert.That(auc, Is.InRange(0.60, 0.80), "resemblance cue AUC 0.60-0.80");
        }

        /// <summary>
        /// M4.11: the four inheritances stay separate. The same birth in two copies of a game, one where the parents' learned
        /// weights, skills and goods differ before the conception: the child's genome and phenotype are identical; and no mind,
        /// policy, learning or social type references the pedigree, the genome or the relatedness function.
        /// </summary>
        [Test]
        public void FourInheritances_Separate_M4_11()
        {
            (long genome, int pheno, int traits, int store) Birth(bool tamper)
            {
                var sim = HouseholdFixtures.Empty(5);
                var s = sim.State;
                int node = LearningFixtures.Glade(sim);
                var fam = HouseholdFixtures.Family(sim, node, 2, Array.Empty<int>(), false);
                var mother = s.Population.Find(fam.Adults[0]); var father = s.Population.Find(fam.Adults[1]);
                if (tamper)
                {
                    // learned weights, goods and personality traits of the parents (the culture store does not exist before M11)
                    for (int k = 0; k < 20; k++) { Learner.AddReward(mother.Mind.Store, 500); Learner.AddReward(father.Mind.Store, -500); }
                    mother.Mind.Store.Mode = LearningMode.Frozen;
                    HouseholdFixtures.Give(sim, mother, Ware.Roots, 3);
                    HouseholdFixtures.Give(sim, father, Ware.Axe, 1);
                    mother.Traits[Trait.Generosity] = 100; father.Traits[Trait.Boldness] = -100;
                }
                mother.Life.PregFather = father.Id; mother.Life.PregStart = s.Tick; mother.Life.PregDue = s.Tick; // test-only: a due pregnancy
                var ev = new EventBuffer();
                var child = Reproduction.Birth(s, ev, mother, 0);
                var p = child.Life.Pheno;
                int ph = p.Strength ^ p.Size * 3 ^ p.Health * 7 ^ p.Features * 11 ^ p.NurtureGain * 13 ^ p.LearnRate * 17;
                int tr = 0; foreach (int t in child.Traits) tr = tr * 31 + t;
                return (GenomeOps.Hash(child.Life.Genome), ph, tr, child.Mind.Store.Count);
            }
            var a = Birth(false); var b = Birth(true);
            Assert.AreEqual(a.genome, b.genome, "the child's genome does not see the parents' learning, goods or traits");
            Assert.AreEqual(a.pheno, b.pheno, "nor does its phenotype");
            Assert.AreEqual(a.traits, b.traits, "nor its personality");
            Assert.AreEqual(0, b.store, "a newborn's learning store is empty (nothing learned is inherited)");
            // reflection: no mind, policy, learning, social or comms type reads the pedigree, the genome or r (G4.2)
            var forbidden = new HashSet<Type> { typeof(PedigreeRecord), typeof(GenomeOps), typeof(Relatedness), typeof(Resemblance), typeof(PhenotypeBuilder), typeof(NpcLife) };
            var asm = typeof(GodSim).Assembly;
            var bad = new List<string>();
            foreach (var t in asm.GetTypes())
            {
                bool mind = t.Namespace == "God.Sim.Policy" || t.Namespace == "God.Sim.Cognition" || t.Namespace == "God.Sim.Learning" || t.Namespace == "God.Sim.Social" || t.Namespace == "God.Sim.Comms"
                    || t == typeof(HouseholdMindHooks) || t == typeof(KinData) || t == typeof(KinMind);
                if (!mind) continue;
                bad.AddRange(TypeReferenceScanner.Forbidden(t, forbidden).Select(r => r.ToString()));
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void EveryKinBelief_HasASource_M4_11()
        {
            var sim = HouseholdFixtures.Empty(6);
            int node = LearningFixtures.Glade(sim);
            HouseholdFixtures.Family(sim, node, 2, new[] { 300, 700 }, true, (Ware.Roots, 6));
            for (int i = 0; i < 1200 * 3; i++) { sim.Step(); sim.DrainEvents(); }
            int beliefs = 0;
            foreach (var n in sim.State.Population.Npcs)
                if (n.Mind?.Kin != null)
                    foreach (var b in n.Mind.Kin.Beliefs) { beliefs++; Assert.AreNotEqual(KinSource.None, b.Source, "npc " + n.Id + " about " + b.Other); }
            Assert.Greater(beliefs, 4);
        }
    }
}
