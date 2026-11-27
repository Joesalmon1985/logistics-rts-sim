using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Social;
using God.Sim.Tests;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M5 barter in controlled scenes (ACCEPTANCE M5.7, M5.8): separate hand-overs by two actors, defection possible and
    /// unrestored, honest pairs completing, price beliefs from trades, incremental trade at low trust.
    /// </summary>
    [Parallelizable(ParallelScope.Children)]
    public class M5BarterTests
    {
        /// <summary>
        /// M5.7: a take-and-leave counterpart A offers fish for a root; the utility NPC B accepts and gives first; A keeps
        /// everything. A holds both goods and B neither, the deal closes Defected, nothing returns the goods, B's trust in A falls.
        /// </summary>
        [Test]
        public void M5_7_DefectionIsPossible_TakeAndLeaveKeepsBothGoods_BsTrustFalls()
        {
            int defected = 0;
            for (ulong seed = 1; seed <= 10; seed++)
            {
                var lab = new M5SocialLab(seed);
                var cheat = new BenchSocialPolicy("bench.takeandleave") { Delivers = false };
                var a = lab.Add(lab.At(0, 2), cheat, LearningMode.On, fed: true, (Ware.Fish, 4));
                var b = lab.AddH(lab.Glade, null, LearningMode.On, false, 50);
                SocialFixtures.Give(lab.Sim, b, Ware.Roots, 1);
                SocialFixtures.SetRelation(lab.Sim, b, a, 400, 500); // B trusts A: it gives its side at once
                int trustBefore = SocialView.Trust(b.Mind, a.Id);
                lab.Step(1200 * 2);
                var closed = lab.Of<ExchangeClosed>().Where(e => e.A == a.Id && e.B == b.Id).ToList();
                var bGave = lab.Of<ExchangeStep>().Where(e => e.Giver == b.Id && e.Receiver == a.Id).Sum(e => e.Count);
                var aGave = lab.Of<ExchangeStep>().Where(e => e.Giver == a.Id).Sum(e => e.Count);
                TestContext.WriteLine("seed " + seed + ": B gave " + bGave + ", A gave " + aGave + ", outcomes " + string.Join(",", closed.Select(c => c.Outcome))
                    + ", B trust in A " + trustBefore + " -> " + SocialView.Trust(b.Mind, a.Id) + ", A roots " + PopulationModule.CountOf(a, Ware.Roots) + " fish " + PopulationModule.CountOf(a, Ware.Fish)
                    + ", B roots " + PopulationModule.CountOf(b, Ware.Roots) + " fish " + PopulationModule.CountOf(b, Ware.Fish));
                Assert.AreEqual(0, aGave, "the take-and-leave counterpart never hands over");
                if (bGave == 0) continue; // B did not go first in this seed (its own decision)
                defected++;
                Assert.IsTrue(closed.Any(c => c.Outcome == DealOutcome.Defected), "seed " + seed + ": ExchangeClosed Defected");
                Assert.AreEqual(1, PopulationModule.CountOf(a, Ware.Roots), "A holds B's root");
                Assert.AreEqual(0, PopulationModule.CountOf(b, Ware.Roots) + PopulationModule.CountOf(b, Ware.Fish), "B holds neither");
                Assert.Less(SocialView.Trust(b.Mind, a.Id), trustBefore, "B's trust in A fell");
                Assert.IsTrue(lab.Of<BetrayalRecorded>().Any(x => x.Victim == b.Id && x.Offender == a.Id), "B recorded a betrayal");
                // nothing returns the goods: no move from A to B, ever
                Assert.IsFalse(lab.Of<ItemMoved>().Any(m => m.FromKind == HolderKind.Carry && m.FromId == a.Id && m.ToId == b.Id), "nothing returns the goods");
                Assert.IsEmpty(SimInvariants.Check(lab.Sim.State));
            }
            TestContext.WriteLine("defected in " + defected + " of 10 seeds");
            Assert.GreaterOrEqual(defected, 8, "B gives first and is cheated in most seeds");
        }

        /// <summary>
        /// M5.8: complementary utility NPCs (one carries fish, the other berries; both default learners) complete at least
        /// 90% of their accepted deals, over 20 runs. Price beliefs and incremental trade are reported.
        /// </summary>
        [Test]
        public void M5_8_HonestPair_CompletesAcceptedDeals_PriceBeliefs_IncrementalTrade()
        {
            int accepted = 0, completed = 0, lowTrustDeals = 0, incremental = 0;
            var ratios = new List<double>();
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var lab = new M5SocialLab(seed);
                var a = lab.Add(lab.Glade, null, LearningMode.On, fed: false, (Ware.Fish, 4));
                var b = lab.Add(lab.At(0, 2), null, LearningMode.On, fed: false, (Ware.Berries, 4));
                lab.Step(1200 * 3);
                var agreed = lab.Of<GestureMade>().Where(g => g.Act == GestureAct.Accept && (g.Speaker == a.Id || g.Speaker == b.Id)).Select(g => g.DealId).Distinct().ToList();
                var closed = lab.Of<ExchangeClosed>().ToDictionary(c => c.DealId, c => c);
                var opened = lab.Of<ExchangeOpened>().ToDictionary(c => c.DealId, c => c);
                int acc = 0, comp = 0;
                foreach (int d in agreed)
                {
                    if (!opened.TryGetValue(d, out var o) || o.Kind != DealKind.Spot) continue;
                    if (!closed.TryGetValue(d, out var c)) continue; // still open at the end
                    acc++;
                    if (c.Outcome == DealOutcome.Completed) comp++;
                    int steps = lab.Of<ExchangeStep>().Count(e => e.DealId == d);
                    if (o.ACount + o.BCount > 2) { lowTrustDeals++; if (steps > 2) incremental++; }
                }
                accepted += acc; completed += comp;
                var pa = a.Mind.Social.Price; var pb = b.Mind.Social.Price;
                if (pa[(int)Ware.Berries] > 0 && pa[(int)Ware.Fish] > 0) ratios.Add((double)pa[(int)Ware.Fish] / pa[(int)Ware.Berries]);
                TestContext.WriteLine("seed " + seed + ": accepted spot deals " + acc + ", completed " + comp + "; A's P(fish)/P(berries) "
                    + (pa[(int)Ware.Berries] > 0 ? ((double)pa[(int)Ware.Fish] / pa[(int)Ware.Berries]).ToString("0.00") : "-"));
                Assert.IsEmpty(SimInvariants.Check(lab.Sim.State));
            }
            TestContext.WriteLine("accepted " + accepted + ", completed " + completed + " (" + (accepted > 0 ? 100 * completed / accepted : 0) + "%); multi-unit deals " + lowTrustDeals
                + ", traded in more than two steps " + incremental + "; mean believed P(fish)/P(berries) " + (ratios.Count > 0 ? ratios.Average().ToString("0.00") : "-"));
            Assert.Greater(accepted, 20, "the pair trades");
            Assert.GreaterOrEqual(completed * 10, accepted * 9, "at least 90% of accepted deals complete");
        }

        /// <summary>M5.8 price beliefs (ECONOMY 4.5): after 10 trades at 3 berries for 1 fish, the believed ratio is within 2.5-3.5, from any start.</summary>
        [Test]
        public void M5_8_PriceBeliefs_TenTradesAtThreeBerriesForOneFish_RatioWithin2_5And3_5()
        {
            foreach (var (pb, pf) in new[] { (0, 0), (150, 300), (300, 100), (50, 900) })
            {
                var price = new short[SocialMind.PriceWares];
                price[(int)Ware.Berries] = (short)pb; price[(int)Ware.Fish] = (short)pf;
                for (int k = 0; k < 10; k++) Valuation.Observe(price, Ware.Berries, 3, Ware.Fish, 1, 4);
                double r = (double)price[(int)Ware.Fish] / price[(int)Ware.Berries];
                TestContext.WriteLine("start " + pb + "/" + pf + ": ratio " + r.ToString("0.000"));
                Assert.That(r, Is.InRange(2.5, 3.5));
            }
        }
    }
}
