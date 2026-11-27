using System.Linq;
using God.Sim.Lab;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// The first-blow cost variant (D3; docs/stage4/research/HAWK_BULLY_RETALIATOR_INVESTIGATION.md, "First-blow cost
    /// variant"). Lab only: ContestRules.FirstBlow is 0 in L2 and every preset (L2's golden hash is pinned in
    /// LabRunnerTests), and the variant is reached through "--rule FirstBlow=N" on lab invade and lab sweep.
    /// </summary>
    public class FirstBlowStudyTests
    {
        private static readonly string[] N4 = { "H", "D", "R", "B" };
        private static ContestRules Fb(int c) => ContestRules.Lab.WithFirstBlow(c);

        [Test]
        public void FirstBlowZero_IsExactlyL2_ContestByContest()
        {
            Assert.AreEqual(0, ContestRules.Lab.FirstBlow);
            var kinds = L02Retaliator.Hdrbp();
            var zero = Fb(0);
            foreach (var a in kinds) foreach (var b in kinds)
            foreach (bool pa in new[] { false, true }) foreach (bool pb in new[] { false, true }) foreach (bool coin in new[] { false, true })
            {
                var x = Contest.Resolve(a, b, Role.None, Role.None, pa, pb, coin, ContestRules.Lab, true);
                var y = Contest.Resolve(a, b, Role.None, Role.None, pa, pb, coin, zero, true);
                Assert.AreEqual(x.PayA, y.PayA); Assert.AreEqual(x.PayB, y.PayB);
                Assert.AreEqual(x.Turns, y.Turns); Assert.AreEqual(x.Fight, y.Fight);
                CollectionAssert.AreEqual(x.MovesA, y.MovesA); CollectionAssert.AreEqual(x.MovesB, y.MovesB);
            }
            CollectionAssert.AreEqual(ContestStudy.TurnRuleX1000(5, ContestRules.Lab), ContestStudy.TurnRuleX1000(5, zero));
        }

        [Test]
        public void SixArgumentConstructor_HasNoFirstBlowCost()
        {
            var r = new ContestRules(50, 100, 10, 10, 8, 100);
            Assert.AreEqual(0, r.FirstBlow);
            CollectionAssert.AreEqual(ContestStudy.TurnRuleX1000(4, ContestRules.Lab), ContestStudy.TurnRuleX1000(4, r));
        }

        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(15)]
        [TestCase(20)]
        [TestCase(30)]
        public void PayoffMatrix_ChargesOnlyDisplayThenStruck(int c)
        {
            var l2 = ContestStudy.TurnRuleX1000(4, ContestRules.Lab);
            var m = ContestStudy.TurnRuleX1000(4, Fb(c));
            long fb = c * 1000L;
            // Hawk and Bully open with Escalate: they never pay. Dove and Retaliator open with Display: they pay against an opening Hawk or Bully only.
            for (int j = 0; j < 4; j++)
            {
                Assert.AreEqual(l2[0, j], m[0, j], "H row, col " + j);
                Assert.AreEqual(l2[3, j], m[3, j], "B row, col " + j);
            }
            foreach (int row in new[] { 1, 2 })
            {
                Assert.AreEqual(l2[row, 0] - fb, m[row, 0], "row " + row + " vs H");
                Assert.AreEqual(l2[row, 3] - fb, m[row, 3], "row " + row + " vs B");
                Assert.AreEqual(l2[row, 1], m[row, 1], "row " + row + " vs D");
                Assert.AreEqual(l2[row, 2], m[row, 2], "row " + row + " vs R");
            }
        }

        [Test]
        public void PayoffMatrix_AtCost10_ExactValues()
        {
            var want = new long[,]
            {
                { -25000, 50000, -25000, 40000 },
                { -10000, 15000, 15000, -10000 },
                { -35000, 15000, 15000, 30000 },
                { -10000, 50000, -10000, 15000 },
            };
            CollectionAssert.AreEqual(want, ContestStudy.TurnRuleX1000(4, Fb(10)));
        }

        [Test]
        public void TheCostIsPaidInTheContestItself_NotOnlyInTheMatrix()
        {
            // R (display) against H (escalate) is a fight; R pays the cost on top of the fight result
            var plain = Contest.Resolve(ContestKind.Retaliator, ContestKind.Hawk, Role.None, Role.None, false, false, true, ContestRules.Lab);
            var cost = Contest.Resolve(ContestKind.Retaliator, ContestKind.Hawk, Role.None, Role.None, false, false, true, Fb(7));
            Assert.AreEqual(plain.PayA - 7, cost.PayA);
            Assert.AreEqual(plain.PayB, cost.PayB);
            // a probing Prober opens with Escalate: the Retaliator pays, the Prober does not
            var probe = Contest.Resolve(ContestKind.Prober, ContestKind.Retaliator, Role.None, Role.None, true, false, true, Fb(7));
            var probe0 = Contest.Resolve(ContestKind.Prober, ContestKind.Retaliator, Role.None, Role.None, true, false, true, ContestRules.Lab);
            Assert.AreEqual(probe0.PayA, probe.PayA);
            Assert.AreEqual(probe0.PayB - 7, probe.PayB);
        }

        [Test]
        public void Retaliator_NoLongerEquivalentToHawk_AgainstHawkAndBully_WhenCostPositive()
        {
            var m = ContestStudy.TurnRuleX1000(4, Fb(5));
            Assert.AreEqual(m[0, 0] - 5000, m[2, 0]);
            Assert.AreEqual(m[0, 3] - 5000, m[2, 3]);
        }

        // ---------------- exact analysis ----------------

        private static InvasionVerdict VsMix(ContestRules r, int mutant, out Candidate c)
        {
            c = ContestStudy.Analyse(ContestStudy.TurnRuleX1000(4, r), ContestStudy.StatePermille(625, 0, 0, 375), "H/B");
            return c.Mutants.Single(x => x.Mutant == mutant).Verdict;
        }

        [Test]
        public void HawkBullyMix_IsInvadedAtCost0_AndStrictEssFromCost1()
        {
            Assert.AreEqual(InvasionVerdict.Invades, VsMix(Fb(0), 1, out _), "Dove invades at 0");
            Assert.AreEqual(InvasionVerdict.Invades, VsMix(Fb(0), 2, out _), "Retaliator invades at 0 (second order)");
            foreach (int c in new[] { 1, 2, 5, 10, 15, 20, 30 })
            {
                Assert.AreEqual(InvasionVerdict.Resists, VsMix(Fb(c), 1, out var cand), "Dove, cost " + c);
                Assert.AreEqual(InvasionVerdict.Resists, VsMix(Fb(c), 2, out _), "Retaliator, cost " + c);
                Assert.IsTrue(cand.IsRestPoint);
                Assert.IsTrue(cand.StableInsideSupport);
                Assert.IsTrue(cand.Verdict(N4).StartsWith("ESS"), cand.Verdict(N4));
            }
        }

        [Test]
        public void Threshold_IsFiveEighthsOfAPoint()
        {
            // Dove earns -c against the 5/8 Hawk, 3/8 Bully mix, and the mix earns -5/8. Scale every payoff by 10 so that
            // integer costs can sit either side: 0.6 is invaded by Dove, 0.7 is a strict ESS.
            var scaled = new ContestRules(500, 1000, 100, 100, 8, 100);
            Assert.AreEqual(InvasionVerdict.Invades, VsMix(scaled.WithFirstBlow(6), 1, out var below));
            Assert.IsFalse(below.Verdict(N4).StartsWith("ESS"));
            Assert.AreEqual(InvasionVerdict.Resists, VsMix(scaled.WithFirstBlow(7), 1, out var above));
            Assert.IsTrue(above.Verdict(N4).StartsWith("ESS"), above.Verdict(N4));
            // Exactly 5/8 (scale 8: V 400, I 800, T 80, S 80, cost 5): Dove ties at first order and loses at second order (its 15 against itself, scaled, is below the mix's 50 against it), so the mix is still an ESS
            var eighth = new ContestRules(400, 800, 80, 80, 8, 100);
            Assert.AreEqual(InvasionVerdict.ResistsSecondOrder, VsMix(eighth.WithFirstBlow(5), 1, out var at));
            Assert.IsTrue(at.Verdict(N4).StartsWith("ESS"), at.Verdict(N4));
            Assert.IsTrue(at.Mutants.Single(x => x.Mutant == 1).First.IsZero);
        }

        [Test]
        public void EssSet_ByCost()
        {
            var ess0 = ContestStudy.RestPoints(ContestStudy.TurnRuleX1000(4, Fb(0)), N4).Where(c => c.Verdict(N4).StartsWith("ESS")).ToList();
            Assert.AreEqual(1, ess0.Count);
            Assert.AreEqual(Rational.Of(35, 57), ess0[0].X[0], "cost 0: the H/D/B mix of the earlier finding");
            foreach (int c in new[] { 2, 5, 10, 15, 20, 30 })
            {
                var ess = ContestStudy.RestPoints(ContestStudy.TurnRuleX1000(4, Fb(c)), N4).Where(x => x.Verdict(N4).StartsWith("ESS")).ToList();
                Assert.AreEqual(1, ess.Count, "cost " + c + ": " + string.Join("; ", ess.Select(e => e.Label)));
                Assert.AreEqual(Rational.Of(5, 8), ess[0].X[0], "cost " + c);
                Assert.AreEqual(Rational.Of(3, 8), ess[0].X[3], "cost " + c);
            }
        }

        [Test]
        public void RetaliatorEdge_StaysNeutral_AtEveryCost()
        {
            foreach (int c in new[] { 0, 5, 30 })
            {
                var m = ContestStudy.TurnRuleX1000(4, Fb(c));
                var r = ContestStudy.Analyse(m, ContestStudy.StatePermille(0, 0, 1000, 0), "R");
                Assert.IsTrue(r.Verdict(N4).StartsWith("neutrally stable only"), r.Verdict(N4));
                Assert.AreEqual(InvasionVerdict.Neutral, r.Mutants.Single(x => x.Mutant == 1).Verdict, "Dove ties with R (cost " + c + ")");
            }
        }

        [Test]
        public void Agents_AtCost10_FromAHawkBullyStart_StayInTheHawkBullyBasin()
        {
            var game = ContestStudy.Game("l2", false, Fb(10));
            var counts = AgentRunSpec.Counts(200, ContestStudy.StartPermille("hb", 4));
            for (ulong seed = 1; seed <= 3; seed++)
            {
                var a = ContestStudy.RunAgents(game, counts, 3000, 10, 500000, 2, seed, "test/fb10");
                Assert.AreEqual("H/B", ContestStudy.Region(ContestStudy.MeanShares(a.Shares, 1500, 3000)), "seed " + seed);
            }
        }
    }
}
