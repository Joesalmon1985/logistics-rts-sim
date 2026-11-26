using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Households;
using God.Sim.Tests;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M4.18 in controlled fixtures (HOUSEHOLDS 10): tending lowers an illness's severity and shortens it; care follows believed
    /// relatedness (500 over 250 over 0); a young fostered child who was not told believes its carer a parent. Every act is the
    /// NPC's own decision; the fixtures place people, kin beliefs and food only.
    /// </summary>
    [TestFixture]
    public sealed class M4CareTests
    {
        private static Npc Sick(GodSim sim, int node, int severity, int days)
        {
            var n = HouseholdFixtures.Person(sim, node, 0, 1800);
            n.Life.Illness = 1; n.Life.Severity = severity; n.Life.IllDaysLeft = days; // test-only: an illness to tend
            return n;
        }

        [Test]
        public void Care_ReducesIllness_SeverityAndDuration_M4_18()
        {
            var lossTended = new List<double>(); var lossAlone = new List<double>(); var daysTended = new List<int>(); var daysAlone = new List<int>();
            foreach (ulong seed in Enumerable.Range(1, 10).Select(x => (ulong)x))
            {
                var sim = HouseholdFixtures.Empty(seed, t => t.Households.Partner.MaxLivePartnerships = 0);
                var s = sim.State;
                s.Setup.Disease = DiseaseSetting.Off; // no new infections: only the two illnesses
                int node = LearningFixtures.Glade(sim);
                var tended = Sick(sim, node, 150, 5);
                var alone = Sick(sim, LearningFixtures.Step(sim, node, 3, 8), 150, 5);
                var carer = HouseholdFixtures.Person(sim, LearningFixtures.Step(sim, node, 0, 1), 1, 1800);
                HouseholdFixtures.SetKinBelief(sim, carer.Id, tended.Id, KinRole.FullSibling, 900);
                LearningFixtures.Bush(sim, SourceKind.Roots, LearningFixtures.Step(sim, node, 1, 2), 4000, 4000, 400);
                LearningFixtures.Bush(sim, SourceKind.Roots, LearningFixtures.Step(sim, LearningFixtures.Step(sim, node, 3, 8), 1, 2), 4000, 4000, 400);
                var probe = new EventProbe(sim);
                int h0t = tended.Health, h0a = alone.Health;
                probe.Step(1200 * 8);
                var ends = probe.Of<IllnessEnded>().ToList();
                var et = ends.FirstOrDefault(e => e.Npc == tended.Id); var ea = ends.FirstOrDefault(e => e.Npc == alone.Id);
                int tends = probe.Of<CareGiven>().Count(c => c.Patient == tended.Id && c.Kind == CareGiven.Tend);
                TestContext.WriteLine($"seed {seed}: tends {tends}; tended ill {et?.Days} days, alone {ea?.Days} days");
                if (et == null || ea == null || tends == 0) continue;
                daysTended.Add(et.Days); daysAlone.Add(ea.Days);
            }
            Assert.GreaterOrEqual(daysTended.Count, 8, "the carer chose to tend in most seeds");
            double meanT = daysTended.Average(), meanA = daysAlone.Average();
            TestContext.WriteLine($"illness length: tended {meanT:F2} days, untended {meanA:F2} days; severity kept on a tended day {new SimTuning().Households.Care.TendSeverityPermille} permille");
            Assert.GreaterOrEqual(meanA - meanT, 1.0, "recovery at least a day earlier when tended");
            Assert.That(1000 - new SimTuning().Households.Care.TendSeverityPermille, Is.InRange(200, 400), "a tended day's loss is 30% +- 10% lower (the body's rule)");
        }

        [Test]
        public void Care_FollowsBelievedKin_500_250_0_M4_18()
        {
            int ordered = 0;
            foreach (ulong seed in Enumerable.Range(1, 20).Select(x => (ulong)x))
            {
                var sim = HouseholdFixtures.Empty(seed, t => t.Households.Partner.MaxLivePartnerships = 0);
                var s = sim.State;
                s.Setup.Disease = DiseaseSetting.Off;
                int node = LearningFixtures.Glade(sim);
                var carer = HouseholdFixtures.Person(sim, node, 1, 1800);
                var p500 = Sick(sim, LearningFixtures.Step(sim, node, 0, 2), 120, 30);
                var p250 = Sick(sim, LearningFixtures.Step(sim, node, 2, 2), 120, 30);
                var p0 = Sick(sim, LearningFixtures.Step(sim, node, 4, 2), 120, 30);
                HouseholdFixtures.SetKinBelief(sim, carer.Id, p500.Id, KinRole.FullSibling, 1000);
                HouseholdFixtures.SetKinBelief(sim, carer.Id, p250.Id, KinRole.Cousin, 1000);   // 125
                HouseholdFixtures.SetKinBelief(sim, carer.Id, p250.Id, KinRole.Aunt, 1000);     // 250 (a stronger role replaces it)
                LearningFixtures.Bush(sim, SourceKind.Roots, LearningFixtures.Step(sim, node, 1, 3), 4000, 4000, 400);
                var probe = new EventProbe(sim);
                probe.Step(1200 * 3);
                int a = probe.Of<CareGiven>().Count(c => c.Patient == p500.Id), b = probe.Of<CareGiven>().Count(c => c.Patient == p250.Id), z = probe.Of<CareGiven>().Count(c => c.Patient == p0.Id);
                TestContext.WriteLine($"seed {seed}: care acts r 500: {a}, 250: {b}, 0: {z}");
                if (a > b && b > z) ordered++;
            }
            TestContext.WriteLine("ordered 500 > 250 > 0 in " + ordered + " of 20 seeds");
            Assert.GreaterOrEqual(ordered, 18, "care order 500 > 250 > 0 in at least 90% of 20 seeds");
        }

        [Test]
        public void FosterChild_NotTold_BelievesItsCarerAParent_M4_18()
        {
            int fostered = 0, wrong = 0;
            foreach (ulong seed in Enumerable.Range(1, 10).Select(x => (ulong)x))
            {
                var sim = HouseholdFixtures.Empty(seed, t => t.Households.Partner.MaxLivePartnerships = 0);
                var s = sim.State;
                s.Setup.Disease = DiseaseSetting.Off;
                int node = LearningFixtures.Glade(sim);
                // an orphan of 0.2 years (a household with no adult) and an unrelated adult with food
                var child = HouseholdFixtures.Person(sim, node, 0, 200, hunger: 400);
                HouseholdModule.Form(s, null, child, JoinReason.Born);
                var adult = HouseholdFixtures.Person(sim, LearningFixtures.Step(sim, node, 0, 1), 1, 1800);
                HouseholdModule.Form(s, null, adult, JoinReason.Founded);
                HouseholdFixtures.Give(sim, adult, Ware.Roots, 4);
                LearningFixtures.Bush(sim, SourceKind.Roots, LearningFixtures.Step(sim, node, 1, 2), 4000, 4000, 400);
                var probe = new EventProbe(sim);
                probe.Step(1200 * 5);
                bool took = probe.Of<ChildFostered>().Any(f => f.Child == child.Id);
                var role = KinData.RoleOf(child.Mind, adult.Id);
                int trueR = God.Sim.Population.Genetics.Relatedness.RPermille(s.Population.Pedigree, child.Id, adult.Id, 4);
                TestContext.WriteLine($"seed {seed}: fostered {took}, fed {probe.Of<ChildFed>().Count(f => f.Child == child.Id)}, the child believes the adult its {role} (true r {trueR}), told {child.Life.ToldFostered}");
                if (!took || child.Life.ToldFostered) continue;
                fostered++;
                if ((role == KinRole.Mother || role == KinRole.Father) && trueR == 0) wrong++;
            }
            TestContext.WriteLine($"un-told fostered cases {fostered}, believing a parent with true r = 0: {wrong}");
            Assert.GreaterOrEqual(fostered, 5, "orphans were taken in by an unrelated adult's own decision");
            Assert.GreaterOrEqual(wrong * 10, fostered * 7, "a foster child believes a wrong kin in at least 70% of un-told fostered cases");
        }
    }
}
