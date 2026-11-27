using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Social;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>Social memory as units (ACCEPTANCE M5.1, M5.2, M5.3, M5.14; SOCIAL_MEMORY 10 tests 1-5, 12-14).</summary>
    public class SocialMemoryTests
    {
        private static SocialTuning Tu => new SocialTuning();

        // ---------------------------------------------------------------- M5.1 bounds and eviction

        private static List<int> MeetMany(int seed, SocialMind m, out List<(int evicted, int imp)> evictions)
        {
            evictions = new List<(int, int)>();
            var rng = new Rng((ulong)seed);
            // a partner, a household mate and someone with an open deal are protected
            int partner = SocialMemory.Touch(m, 1001, 0, 1200, 0, 0, out _, out _); m.Rel[partner].Flags = RelFlags.Partner;
            int mate = SocialMemory.Touch(m, 1002, 0, 1200, 0, 0, out _, out _); m.Rel[mate].Flags = RelFlags.HouseholdMate;
            SocialMemory.Touch(m, 1003, 0, 1200, 0, 0, out _, out _);
            m.Deals[m.DealCount++] = new DealMemory { DealId = 1, Partner = 1003, Status = DealStatus.Agreed };
            for (int k = 0; k < 200; k++)
            {
                int tick = 100 + k * 50;
                int i = SocialMemory.Touch(m, 2000 + k, tick, 1200, rng.NextRange(-300, 301), 0, out var gone, out int imp);
                if (i >= 0)
                {
                    m.Rel[i].Familiarity = (short)rng.NextRange(0, 400);
                    if (gone.Other != 0) evictions.Add((gone.Other, imp));
                }
            }
            return Enumerable.Range(0, m.RelCount).Select(i => m.Rel[i].Other).ToList();
        }

        [Test]
        public void M5_1_TwoHundredMeetings_AtMost40Relations_ProtectedKept_SameEvictionsEachRun()
        {
            List<(int, int)> first = null;
            for (int run = 0; run < 3; run++)
            {
                var m = new SocialMind(40, 6);
                var kept = MeetMany(7, m, out var ev);
                Assert.LessOrEqual(m.RelCount, 40);
                CollectionAssert.IsSubsetOf(new[] { 1001, 1002, 1003 }, kept, "partner, household mate and open deal kept");
                Assert.Greater(ev.Count, 0, "newcomers replaced forgotten strangers");
                if (first == null) first = ev; else CollectionAssert.AreEqual(first, ev, "same seed, same evictions (run " + run + ")");
            }
        }

        [Test]
        public void M5_1_Eviction_LowestImportance_TiesToLowestSlot()
        {
            var m = new SocialMind(4, 6);
            for (int k = 0; k < 4; k++) SocialMemory.Touch(m, 10 + k, 0, 1200, 0, 0, out _, out _);
            // slots 1 and 2 tie at the lowest importance; 0 and 3 are more important
            m.Rel[0].Trust = 500; m.Rel[3].Trust = 400; m.Rel[1].Trust = 100; m.Rel[2].Trust = 100;
            int i = SocialMemory.Touch(m, 99, 0, 1200, 300, 0, out var gone, out _);
            Assert.AreEqual(1, i, "the lowest slot of the least important");
            Assert.AreEqual(11, gone.Other);
            // a newcomer less important than everyone stays a stranger
            int j = SocialMemory.Touch(m, 98, 0, 1200, 0, 0, out var none, out _);
            Assert.AreEqual(-1, j);
            Assert.AreEqual(0, none.Other);
        }

        [Test]
        public void M5_1_Budget_AtMost4096BytesPerMind_600MindsUnder2Point5MB()
        {
            var m = new SocialMind(new SocialTuning().RelSlots, new God.Sim.Comms.CommsTuning().InboxSlots);
            TestContext.WriteLine("SocialMind tables: " + m.Bytes + " B; relation " + System.Runtime.InteropServices.Marshal.SizeOf<Relation>() + " B");
            Assert.LessOrEqual(m.Bytes, 4096);
            Assert.LessOrEqual(600L * m.Bytes, 2_500_000);
        }

        // ---------------------------------------------------------------- M5.2 recognition

        private static int MissRate(int familiarity, int draws = 10000)
        {
            var root = new Rng(12345).Fork("sim");
            var r = new Rng();
            int miss = 0, p = SocialMemory.MissPermille(Tu, familiarity, false, false);
            for (int k = 0; k < draws; k++)
            {
                r.SetToFork(root, "recognise", 7, ((long)k << 20) | 9); // the sim's keyed draw (observer 7, person 9, tick k)
                if (r.NextInt(1000) < p) miss++;
            }
            return miss * 1000 / draws;
        }

        [Test]
        public void M5_2_RecognitionMissRates_Familiarity50_800_1000()
        {
            int a = MissRate(50), b = MissRate(800), c = MissRate(1000);
            TestContext.WriteLine("miss per mille: fam 50 " + a + ", fam 800 " + b + ", fam 1000 " + c);
            Assert.That(a, Is.InRange(300, 360));
            Assert.That(b, Is.InRange(50, 90));
            Assert.AreEqual(0, c);
        }

        [Test]
        public void M5_2_ACheatMetAgain_HidesInAThird_AfterFiveInteractionsUnder3Percent()
        {
            var tu = Tu;
            // met once (one day of contact): the stranger's familiarity with the cheat
            int once = tu.FamDayContact;
            int five = Math.Min(1000, 5 * tu.FamInteraction + tu.FamDayContact);
            int a = MissRate(once), b = MissRate(five);
            TestContext.WriteLine("after one sighting (fam " + once + "): " + a + " per mille unrecognised; after five interactions (fam " + five + "): " + b);
            Assert.That(a, Is.InRange(300, 360));
            Assert.Less(b, 30);
        }

        // ---------------------------------------------------------------- M5.3 trust, grudge, forgiveness

        [Test]
        public void M5_3_Grudge300AtSeverity100_Vengefulness_SetsTheDecay()
        {
            var d = new SocialDecayTuning();
            var r = new Relation { Other = 2 };
            SocialMemory.Betrayed(ref r, 100, 0);
            Assert.AreEqual(300, r.Grudge);
            int Twenty(int veng)
            {
                var x = new Relation { Other = 2, Grudge = 300, Familiarity = 500 };
                for (int day = 0; day < 20; day++) SocialMemory.DayPass(d, ref x, veng, true, 0);
                return x.Grudge;
            }
            int hi = Twenty(100), lo = Twenty(-100);
            TestContext.WriteLine("grudge after 20 days: vengefulness +100 " + hi + ", -100 " + lo);
            Assert.GreaterOrEqual(hi, 245);
            Assert.LessOrEqual(lo, 30);
        }

        [Test]
        public void M5_3_AfterAGrudgeOf600_AHelpfulActRaisesTrustByFortyPercent()
        {
            var tu = Tu;
            var clean = new Relation { Other = 2 };
            int full = SocialMemory.GiftReceived(tu, ref clean, 200, 0);
            var bad = new Relation { Other = 2, Grudge = 600, Betrayals = 1 };
            int cut = SocialMemory.GiftReceived(tu, ref bad, 200, 0);
            Assert.AreEqual(full * 400 / 1000, cut, "delta x 0.4");
        }

        [Test]
        public void M5_3_Apology_GiftAddsHalfItsValue_RepeatApologyAfterThreeBetrayalsScores120Lower()
        {
            var r = new Relation { Other = 2, Affection = 200, Grudge = 100 };
            int none = SocialMemory.ForgiveScore(30, r, 0), gift = SocialMemory.ForgiveScore(30, r, 80);
            Assert.AreEqual(40, gift - none, "giftValue / 2");
            var thrice = r; thrice.Betrayals = 3;
            Assert.AreEqual(120, none - SocialMemory.ForgiveScore(30, thrice, 0));
            SocialMemory.Forgive(ref r, 80);
            Assert.AreEqual(0, r.Grudge);
            Assert.AreEqual(1, r.Forgiven);
        }

        // ---------------------------------------------------------------- M5.14 decay independent of LifeScale, configurable

        private static (int[] grudge, ulong learning) GrudgeRun(int lifeScale, Action<SimTuning> tune)
        {
            var t = new SimTuning();
            tune?.Invoke(t);
            var setup = new GameSetup(3, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 0, Victory = VictorySpec.SandboxOnly(), LifeScale = lifeScale };
            var sim = GodSim.Create(setup, t);
            int glade = LearningFixtures.Glade(sim);
            var a = LearningFixtures.Npc(sim, glade, InstinctSet.Default, LearningMode.On);
            var b = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, glade, 0, 30), InstinctSet.Default, LearningMode.On);
            a.Traits[Trait.Vengefulness] = 0;
            SocialFixtures.SetRelation(sim, a, b, -200, 500);
            a.Mind.Social.Rel[a.Mind.Social.Find(b.Id)].Grudge = 300;
            int tpd = setup.TicksPerDay;
            var g = new int[20];
            for (int day = 0; day < 20; day++)
            {
                for (int k = 0; k < tpd; k++) { sim.Step(); sim.DrainEvents(); }
                g[day] = a.Mind.Social.Rel[a.Mind.Social.Find(b.Id)].Grudge;
            }
            return (g, GodSim.PartsOf(sim.State, null)[Array.IndexOf(GodSim.PartNamesOf(sim.State), "learning")]);
        }

        [Test]
        public void M5_14_SocialDecay_IndependentOfLifeScale_AndConfigurable()
        {
            var baseRun = GrudgeRun(100, null);
            var life200 = GrudgeRun(200, null);
            var fast = GrudgeRun(100, t => t.Social.Decay.ScalePermille = 2000);
            var off = GrudgeRun(100, t => { t.Social.Decay.GrudgeBase = 0; t.Social.Decay.GrudgeFloor = 0; t.Social.Decay.GrudgeCeil = 0; });
            TestContext.WriteLine("grudge by day (1000): " + string.Join(",", baseRun.grudge) + "\n(2000): " + string.Join(",", fast.grudge));
            CollectionAssert.AreEqual(baseRun.grudge, life200.grudge, "LifeScale 200: the same grudge every day");
            for (int d = 0; d < 20; d++) if (baseRun.grudge[d] > 0) Assert.Less(fast.grudge[d], baseRun.grudge[d], "ScalePermille 2000: strictly lower on day " + (d + 1));
            Assert.AreEqual(baseRun.learning, fast.learning, "the learning store is untouched by the social decay setting");
            Assert.AreEqual(300, off.grudge[19], "grudge rates 0: unchanged after 20 days");
        }
    }
}
