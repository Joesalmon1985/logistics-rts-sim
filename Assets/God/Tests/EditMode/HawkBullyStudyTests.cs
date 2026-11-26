using System.Linq;
using God.Sim.Lab;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// The Hawk/Bully/Retaliator investigation (docs/stage4/research/HAWK_BULLY_RETALIATOR_INVESTIGATION.md): payoff
    /// matrices, exact invasion analysis, replicator fixed points, and the matched agent comparison. Analysis only; L2
    /// itself is tested in LabRunnerTests and its golden hash is unchanged.
    /// </summary>
    public class HawkBullyStudyTests
    {
        private static readonly string[] N4 = { "H", "D", "R", "B" };
        private static readonly string[] N5 = { "H", "D", "R", "B", "P" };

        private static long[,] L2() => ContestStudy.Named("l2", ContestRules.Lab);
        private static long[,] Msp4() => ContestStudy.Named("msp4", ContestRules.Lab);

        private static Candidate Find(System.Collections.Generic.List<Candidate> l, params long[] numDen)
        {
            // numDen: num0, num1, ..., den (shares as integers over a common denominator)
            int k = numDen.Length - 1;
            long den = numDen[k];
            return l.Single(c => Enumerable.Range(0, k).All(i => c.X[i].Equals(Rational.Of(numDen[i], den))));
        }

        private static InvasionCell Cell(Candidate c, int j) => c.Mutants.Single(m => m.Mutant == j);

        // ---------------- payoff matrices ----------------

        [Test]
        public void TurnRuleMatrix_HDRBP_ExactValues()
        {
            var m = ContestStudy.Named("l2p", ContestRules.Lab);
            var want = new long[,]
            {
                { -25000, 50000, -25000, 40000, -25000 },
                { 0, 15000, 15000, 0, 13500 },
                { -25000, 15000, 15000, 40000, 11000 },
                { -10000, 50000, -10000, 15000, -10000 },
                { -25000, 18500, 11000, 40000, 7400 },
            };
            CollectionAssert.AreEqual(want, m);
            CollectionAssert.AreEqual(ContestStudy.Block(want, 4), L2());
        }

        [Test]
        public void Msp1973Table1_Transcription_ReorderedToHDRBP()
        {
            var m = ContestStudy.Msp1973X1000();
            // paper: "contests between Mouse and Hawk ... 19.5 to Mouse and 80.0 to Hawk"
            Assert.AreEqual(80000, m[0, 1]);
            Assert.AreEqual(19500, m[1, 0]);
            Assert.AreEqual(-19500, m[0, 0]);
            Assert.AreEqual(29000, m[1, 1]);
            Assert.AreEqual(29000, m[2, 2]);
            Assert.AreEqual(29000, m[1, 2], "Mouse ties with Retaliator against Retaliator");
            Assert.AreEqual(57100, m[2, 3]);
            Assert.AreEqual(11900, m[3, 2]);
            Assert.AreEqual(41500, m[3, 3]);
            Assert.AreEqual(21900, m[4, 4]);
            long sum = 0;
            foreach (var v in m) sum += v;
            Assert.AreEqual(622500, sum, "sum of the 25 published cells (x1000)");
        }

        [TestCase(0, 100, 10, 50)]
        [TestCase(5, 100, 10, 50)]
        [TestCase(10, 100, 10, 50)]
        [TestCase(20, 200, 0, 80)]
        public void TurnRules_RetaliatorIsPayoffEquivalentToHawk_AgainstHawkAndBully(int s, int i, int t, int v)
        {
            // The structural cause: under the 4.2 turn rules a Retaliator's opening display against an escalation costs
            // nothing, so R earns exactly what H earns against H and against B, for any V, I, T, S.
            var r = new ContestRules(v, i, t, s, 8, 100);
            var m = ContestStudy.TurnRuleX1000(4, r);
            Assert.AreEqual(m[0, 0], m[2, 0], "R vs H = H vs H");
            Assert.AreEqual(m[0, 3], m[2, 3], "R vs B = H vs B");
            // In Maynard Smith and Price's table the Retaliator pays for its first move (it takes the first blow)
            var p = ContestStudy.Msp1973X1000();
            Assert.Less(p[2, 0], p[0, 0]);
            Assert.Less(p[2, 3], p[0, 3]);
        }

        // ---------------- exact invasion analysis ----------------

        [Test]
        public void L2_HawkBullyMix_IsInvadedByDoveAndRetaliator()
        {
            var hb = Find(ContestStudy.RestPoints(L2(), N4), 5, 0, 0, 3, 8);
            Assert.IsTrue(hb.IsRestPoint);
            Assert.AreEqual(Rational.Of(-5, 8), hb.Value);
            Assert.AreEqual(Rational.Of(5, 8), Cell(hb, 1).First, "Dove earns 0 against the mix, the mix earns -0.625");
            Assert.AreEqual(InvasionVerdict.Invades, Cell(hb, 1).Verdict);
            Assert.IsTrue(Cell(hb, 2).First.IsZero, "Retaliator ties at first order");
            Assert.AreEqual(Rational.Of(275, 8), Cell(hb, 2).Second, "and wins at second order (15 against itself vs -19.375)");
            Assert.AreEqual(InvasionVerdict.Invades, Cell(hb, 2).Verdict);
        }

        [Test]
        public void L2_HawkDoveBully_IsTheOnlyHawkBullyEss_AndItsMarginIsSmall()
        {
            var all = ContestStudy.RestPoints(L2(), N4);
            var ess = all.Where(c => c.Verdict(N4).StartsWith("ESS")).ToList();
            Assert.AreEqual(1, ess.Count, string.Join("; ", all.Select(c => c.Label + ": " + c.Verdict(N4))));
            var hdb = ess[0];
            Assert.AreEqual(Rational.Of(35, 57), hdb.X[0]);
            Assert.AreEqual(Rational.Of(1, 57), hdb.X[1]);
            Assert.IsTrue(hdb.X[2].IsZero);
            Assert.AreEqual(Rational.Of(21, 57), hdb.X[3]);
            Assert.AreEqual(Rational.Of(15, 57), hdb.Value);
            Assert.AreEqual(Rational.Of(-35, 57), Cell(hdb, 2).First, "R loses only through the 1/57 Doves (35 x 1/57)");
            // the basin boundary: an interior saddle 2.4% Retaliator away
            var saddle = all.Single(c => c.X.All(x => x.Sign > 0));
            Assert.AreEqual("not an ESS: unstable inside its support", saddle.Verdict(N4));
            Assert.AreEqual("0.0243", saddle.X[2].Format(4));
        }

        [Test]
        public void L2_PureRetaliator_IsNeutrallyStable_DoveDrifts()
        {
            var r = ContestStudy.Analyse(L2(), ContestStudy.StatePermille(0, 0, 1000, 0), "R");
            Assert.AreEqual("neutrally stable only: D drift in", r.Verdict(N4));
            Assert.AreEqual(Rational.Of(-40), Cell(r, 0).First);
            Assert.AreEqual(Rational.Of(-25), Cell(r, 3).First);
            Assert.AreEqual(InvasionVerdict.Neutral, Cell(r, 1).Verdict);
            // the Retaliator/Dove edge: Bully invades once d > 25/60
            Assert.AreEqual(InvasionVerdict.Resists, Cell(ContestStudy.Analyse(L2(), ContestStudy.StatePermille(0, 400, 600, 0), "rd"), 3).Verdict);
            Assert.AreEqual(InvasionVerdict.Invades, Cell(ContestStudy.Analyse(L2(), ContestStudy.StatePermille(0, 450, 550, 0), "rd"), 3).Verdict);
            // with the Prober: P resists at -4 (amended cells), so pure R is still only neutrally stable
            var r5 = ContestStudy.Analyse(ContestStudy.Named("l2p", ContestRules.Lab), ContestStudy.StatePermille(0, 0, 1000, 0, 0), "R");
            Assert.AreEqual(Rational.Of(-4), Cell(r5, 4).First);
            Assert.AreEqual("neutrally stable only: D drift in", r5.Verdict(N5));
        }

        [Test]
        public void Msp_HawkBullyMix_IsAStrictEss_AndRetaliatorIsNeutrallyStable()
        {
            foreach (var (m, names) in new[] { (Msp4(), N4), (ContestStudy.Msp1973X1000(), N5) })
            {
                var all = ContestStudy.RestPoints(m, names);
                var hb = all.Single(c => c.X[0].Sign > 0 && c.X[3].Sign > 0 && c.X.Count(x => x.Sign > 0) == 2);
                Assert.AreEqual(Rational.Of(331, 575), hb.X[0], "h = (74.6 - 41.5) / (74.6 - 41.5 + 4.9 + 19.5)");
                Assert.AreEqual("ESS (every mutant resists)", hb.Verdict(names));
                Assert.AreEqual("-0.931", Cell(hb, 1).First.Format(3));
                Assert.AreEqual("-9.038", Cell(hb, 2).First.Format(3));
                if (names.Length == 5) Assert.AreEqual("-6.795", Cell(hb, 4).First.Format(3));
                var r = ContestStudy.Analyse(m, ContestStudy.StatePermille(names.Length == 5 ? new[] { 0, 0, 1000, 0, 0 } : new[] { 0, 0, 1000, 0 }), "R");
                Assert.AreEqual("neutrally stable only: D drift in", r.Verdict(names));
                Assert.AreEqual(1, all.Count(c => c.Verdict(names).StartsWith("ESS")), "the Hawk/Bully mix is the only strict ESS");
            }
        }

        // ---------------- replicator fixed points and invasion runs ----------------

        private static long Fx3(int permille) => Fx.FromPermille(permille);

        [Test]
        public void Replicator_L2_NoMutation_IsBistable()
        {
            var end = ContestStudy.RunReplicator(L2(), Replicator.SharesPermille(ContestStudy.StartPermille("hawk", 4)), 20000, 500000, 0);
            var target = new[] { Rational.Of(35, 57), Rational.Of(1, 57), Rational.Zero, Rational.Of(21, 57) };
            for (int i = 0; i < 4; i++) Assert.Less(System.Math.Abs(end[i] - target[i].ToQ30()), Fx3(1), "Hawk-heavy start ends at the H/D/B ESS, share " + i);
            foreach (var s in new[] { "uniform", "ret", "hb" })
            {
                var e = ContestStudy.RunReplicator(L2(), Replicator.SharesPermille(ContestStudy.StartPermille(s, 4)), 20000, 500000, 0);
                Assert.AreEqual("R", ContestStudy.Region(e), s);
                Assert.Less(e[0] + e[3], Fx3(1), s + ": Hawk and Bully gone");
            }
        }

        [Test]
        public void Replicator_L2_WithMutation_HasOneAttractor_RetaliatorDominant()
        {
            long[] first = null;
            foreach (var s in new[] { "uniform", "hawk", "ret", "hb" })
            {
                var e = ContestStudy.RunReplicator(L2(), Replicator.SharesPermille(ContestStudy.StartPermille(s, 4)), 20000, 500000, 2);
                Assert.AreEqual("R", ContestStudy.Region(e), s);
                Assert.That(e[2], Is.InRange(Fx3(700), Fx3(740)), s);
                if (first == null) first = e;
                else for (int i = 0; i < 4; i++) Assert.Less(System.Math.Abs(e[i] - first[i]), Fx3(2), s + " share " + i);
            }
        }

        [Test]
        public void Replicator_Msp4_HawkBullyFromUniform_AndSurvivesMutation()
        {
            foreach (int mu in new[] { 0, 2 })
            {
                var e = ContestStudy.RunReplicator(Msp4(), Replicator.SharesPermille(ContestStudy.StartPermille("uniform", 4)), 20000, 500000, mu);
                Assert.AreEqual("H/B", ContestStudy.Region(e), "mu " + mu);
            }
            var e0 = ContestStudy.RunReplicator(Msp4(), Replicator.SharesPermille(ContestStudy.StartPermille("uniform", 4)), 20000, 500000, 0);
            Assert.Less(System.Math.Abs(e0[0] - Rational.Of(331, 575).ToQ30()), Fx3(2));
        }

        [Test]
        public void InvasionRuns_OnePercentRetaliator()
        {
            // L2: 1% R in the pure H/B mix takes over; at the H/D/B ESS it dies out (no drift). MSP: it dies out.
            var l2 = L2();
            var hb = ContestStudy.WithMutant(Replicator.SharesPermille(625, 0, 0, 375), 2, 10);
            Assert.Greater(ContestStudy.RunReplicator(l2, hb, 20000, 500000, 0)[2], Fx3(990));
            var hdb = ContestStudy.ToQ30(new[] { Rational.Of(35, 57), Rational.Of(1, 57), Rational.Zero, Rational.Of(21, 57) });
            var e = ContestStudy.RunReplicator(l2, ContestStudy.WithMutant(hdb, 2, 10), 20000, 500000, 0);
            Assert.Less(e[2], Fx3(1));
            Assert.AreEqual("H/B", ContestStudy.Region(e));
            // but 3% R crosses the saddle (2.4% away) and R wins
            var e3 = ContestStudy.RunReplicator(l2, ContestStudy.WithMutant(hdb, 2, 30), 20000, 500000, 0);
            Assert.AreEqual("R", ContestStudy.Region(e3));
            var msp = ContestStudy.RunReplicator(Msp4(), ContestStudy.WithMutant(Replicator.SharesPermille(576, 0, 0, 424), 2, 30), 20000, 500000, 0);
            Assert.AreEqual("H/B", ContestStudy.Region(msp));
            Assert.Less(msp[2], Fx3(1));
        }

        [Test]
        public void BasinCensus_Grid10_NoMutation()
        {
            CollectionAssert.AreEqual(new[] { 71, 13, 0 }, ContestStudy.Basins(L2(), 10, 20000, 500000, 0));
            CollectionAssert.AreEqual(new[] { 29, 55, 0 }, ContestStudy.Basins(Msp4(), 10, 20000, 500000, 0));
        }

        // ---------------- matched agent comparison (L2's engine, drift and mutation) ----------------

        [Test]
        public void Agents_SameEngine_L2EndsRetaliator_MspEndsHawkBully()
        {
            var l2 = ContestStudy.Game("l2", false, ContestRules.Lab);
            var msp = ContestStudy.Game("msp4", false, ContestRules.Lab);
            var counts = AgentRunSpec.Counts(200, ContestStudy.StartPermille("uniform", 4));
            for (ulong seed = 1; seed <= 5; seed++)
            {
                var a = ContestStudy.RunAgents(l2, counts, 6000, 10, 500000, 2, seed, "test/l2");
                Assert.AreEqual("R", ContestStudy.Region(ContestStudy.MeanShares(a.Shares, 3000, 6000)), "L2 seed " + seed);
                var b = ContestStudy.RunAgents(msp, counts, 6000, 10, 500000, 2, seed, "test/msp4");
                Assert.AreEqual("H/B", ContestStudy.Region(ContestStudy.MeanShares(b.Shares, 3000, 6000)), "MSP seed " + seed);
            }
        }

        [Test]
        public void Rational_Basics()
        {
            Assert.AreEqual("5/8", Rational.Of(10, 16).ToString());
            Assert.AreEqual("-0.625", Rational.Of(-5, 8).Format(3));
            Assert.AreEqual(Fx.One, Rational.One.ToQ30());
            Assert.AreEqual(-1, Rational.Of(-1, 3).Sign);
            Assert.AreEqual(Rational.Of(1, 2), Rational.Of(1, 3) + Rational.Of(1, 6));
        }
    }
}
