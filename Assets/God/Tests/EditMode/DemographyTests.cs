using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// Owner decisions D4 (unity/demog): the relationship ontology as separate records (M4.39), preferences and commitments
    /// respected (M4.40, the unit part), and the established-band founder generator (M4.42, the EditMode part).
    /// </summary>
    public class DemographyTests
    {
        private static int Day(GodState s) => (int)SimCalendar.Of(s).Day(s.Tick);

        /// <summary>Runs one of M4's acts for a person now (test-only: the intent is set directly, as its policy would choose it).</summary>
        private static List<SimEvent> Act(GodSim sim, Npc npc, byte verb, int target, int times = 1)
        {
            var s = sim.State;
            var ev = new EventBuffer();
            npc.Action = new NpcAction { Verb = verb, TargetId = target, Since = s.Tick };
            for (int i = 0; i < times && npc.Action.Verb == verb; i++) HouseholdActs.Act(s, ev, npc, Day(s));
            var l = new List<SimEvent>();
            ev.DrainInto(l);
            return l;
        }

        private static Household Couple(GodSim sim, Npc a, Npc b)
        {
            var s = sim.State;
            var h = HouseholdModule.Form(s, null, a, JoinReason.Founded);
            HouseholdModule.Join(s, null, h, b, JoinReason.Founded, 0);
            Partnerships.Form(s, null, a.Id, b.Id, a.Id, h.Id);
            KinCues.Believe(s, null, a, b.Id, KinRole.Partner, 900, KinSource.Partnered);
            KinCues.Believe(s, null, b, a.Id, KinRole.Partner, 900, KinSource.Partnered);
            return h;
        }

        // ---------------------------------------------------------------- M4.39: each relationship kind apart from the others

        [Test]
        public void Ontology_SeparateRecords_EachCombinationApart_M4_39()
        {
            var sim = HouseholdFixtures.Empty(41, t => { t.Households.Life.ConceiveBasePermille = 1000; t.Households.Life.ConceiveSpanPermille = 0; });
            var s = sim.State;
            s.Setup.Disease = DiseaseSetting.Off;
            int node = LearningFixtures.Glade(sim);
            // two same-sex couples, each its own household; nobody prefers exclusivity
            var w1 = HouseholdFixtures.Person(sim, node, 0, 1500); var w2 = HouseholdFixtures.Person(sim, node, 0, 1600);
            var m1 = HouseholdFixtures.Person(sim, node, 1, 1500); var m2 = HouseholdFixtures.Person(sim, node, 1, 1600);
            foreach (var n in new[] { w1, w2, m1, m2 }) n.Life.Commitment = 0;
            var hw = Couple(sim, w1, w2); var hm = Couple(sim, m1, m2);

            // (1) a same-sex couple raising an adopted child: partnership + household + caregiving, no parenthood, no reproductive relationship
            var orphan = HouseholdFixtures.Person(sim, node, 1, 400);
            HouseholdModule.Form(s, null, orphan, JoinReason.Founded);
            orphan.Life.OrphanSince = Day(s);
            var adopt = Act(sim, w2, HVerb.Adopt, orphan.Id);
            Assert.IsTrue(adopt.OfType<ChildFostered>().Any(), "the couple's adult took the orphan in");
            Assert.AreEqual(hw.Id, orphan.Household);
            Assert.IsNotNull(Partnerships.Between(s, w1.Id, w2.Id));
            Assert.AreEqual(0, ReproductiveUnions.Total(s), "raising a child needs no reproductive relationship");
            Assert.AreNotEqual(w1.Id, orphan.Life.Mother); Assert.AreNotEqual(w2.Id, orphan.Life.Mother);

            // (2) a reproductive relationship outside both partnerships and households, agreed by both
            var seek = Act(sim, w1, HVerb.SeekChild, 0);
            Assert.IsTrue(seek.OfType<ChildWanted>().Any(), "w1 shows that it wants a child");
            var accept = Act(sim, m1, HVerb.AcceptChild, w1.Id);
            var formed = accept.OfType<ReproductiveUnionFormed>().Single();
            Assert.AreEqual(ReproductiveUnion.Independent, formed.Kind);
            Assert.IsFalse(formed.Partners);
            Assert.AreEqual(m1.Id, w1.Mind.Kin.UnionWith); Assert.AreEqual(w1.Id, m1.Mind.Kin.UnionWith);
            Assert.IsNotNull(Partnerships.Between(s, w1.Id, w2.Id), "no partnership changed");
            Assert.IsNotNull(Partnerships.Between(s, m1.Id, m2.Id), "no partnership changed");
            Assert.AreEqual(hw.Id, w1.Household); Assert.AreEqual(hm.Id, m1.Household);

            // both choose to try; the mother's day check conceives; the birth
            Act(sim, w1, HVerb.TryForChild, m1.Id); Act(sim, m1, HVerb.TryForChild, w1.Id);
            w1.Nutrition = 900000;
            var ev = new EventBuffer();
            var cal = SimCalendar.Of(s);
            Reproduction.Daily(s, ev, w1, Day(s), cal.AgeMilli(w1.BornTick, s.Tick));
            Assert.AreEqual(m1.Id, w1.Life.PregFather, "conceived in the reproductive relationship");
            var child = Reproduction.Birth(s, ev, w1, Day(s));
            Assert.AreEqual(w1.Id, child.Life.Mother); Assert.AreEqual(m1.Id, child.Life.BioFather);
            Assert.AreEqual(hw.Id, child.Household, "the child lives in its mother's household");
            Assert.AreNotEqual(m1.Household, child.Household, "its biological father lives elsewhere");
            Assert.AreEqual(KinCues.ChildRole(child.Life.Sex), KinData.RoleOf(m1.Mind, child.Id), "the father knows his child");
            Assert.AreEqual(1, ReproductiveUnions.Between(s, w1.Id, m1.Id).Children);
            Assert.IsNotNull(Partnerships.Between(s, w1.Id, w2.Id)); Assert.IsNotNull(Partnerships.Between(s, m1.Id, m2.Id));

            // (3) caregiving with no parenthood, partnership or household: m2 feeds the orphan of another household
            HouseholdFixtures.Give(sim, m2, Ware.Roots, 2);
            var fed = Act(sim, m2, HVerb.Feed, orphan.Id, 5).OfType<ChildFed>().ToList();
            Assert.AreEqual(1, fed.Count, "fed by an unrelated adult of another household");
            Assert.AreNotEqual(m2.Household, orphan.Household);

            // (4) household membership with no kinship or partnership
            var hut = HouseholdFixtures.SharedHut(sim, node, 3);
            foreach (int a in hut.Adults) { Assert.AreEqual(0, Partnerships.Count(s, a)); Assert.AreEqual(0, ReproductiveUnions.Count(s, a)); }

            // (5) the economic partnership is its own (stub) record: M4 forms none
            Assert.IsEmpty(s.Households.Economic);
            CollectionAssert.IsEmpty(SimInvariants.Check(s));
        }

        // ---------------------------------------------------------------- M4.40 (unit part): consent and commitments

        [Test]
        public void ReproductionOutside_NeedsBothConsents_AndRespectsCommitments_M4_40()
        {
            var sim = HouseholdFixtures.Empty(42);
            var s = sim.State;
            int node = LearningFixtures.Glade(sim);
            var w = HouseholdFixtures.Person(sim, node, 0, 1500); var partner = HouseholdFixtures.Person(sim, node, 0, 1500);
            var m = HouseholdFixtures.Person(sim, node, 1, 1500);
            Couple(sim, w, partner);
            HouseholdModule.Form(s, null, m, JoinReason.Founded);
            // the woman's partner states commitment (exclusive): the woman's wish is not even shown, nobody leaves anyone
            w.Life.Commitment = 0; partner.Life.Commitment = 900; m.Life.Commitment = 0;
            var seek = Act(sim, w, HVerb.SeekChild, 0);
            Assert.IsFalse(seek.OfType<ChildWanted>().Any(), "her partner's commitment is respected");
            // an accept without an offer is refused (consent of both)
            var acc = Act(sim, m, HVerb.AcceptChild, w.Id);
            Assert.IsFalse(acc.OfType<ReproductiveUnionFormed>().Any(), "no relationship without her offer");
            // commitments relaxed: her offer and his acceptance agree one
            partner.Life.Commitment = 0;
            Assert.IsTrue(Act(sim, w, HVerb.SeekChild, 0).OfType<ChildWanted>().Any());
            // his own commitment, were he partnered, would stop him: here he is not, so he may accept
            Assert.IsTrue(Act(sim, m, HVerb.AcceptChild, w.Id).OfType<ReproductiveUnionFormed>().Any());
            Assert.IsNotNull(Partnerships.Between(s, w.Id, partner.Id), "her partnership is untouched");
            Assert.AreEqual(1, ReproductiveUnions.Count(s, w.Id));
            // the cap: she cannot take a second one at once
            var m2 = HouseholdFixtures.Person(sim, node, 1, 1500);
            Assert.IsFalse(Act(sim, w, HVerb.SeekChild, 0).OfType<ChildWanted>().Any(), "one reproductive relationship at a time (MaxLiveUnions 1)");
            CollectionAssert.IsEmpty(SimInvariants.Check(s));
            Assert.IsNotNull(m2);
        }

        [Test]
        public void WantingAChild_IsSeenOnlyByThoseItCouldConceiveWith()
        {
            var sim = HouseholdFixtures.Empty(43);
            var s = sim.State;
            int node = LearningFixtures.Glade(sim);
            var w = HouseholdFixtures.Person(sim, node, 0, 1500); var other = HouseholdFixtures.Person(sim, node, 0, 1500); var m = HouseholdFixtures.Person(sim, node, 1, 1500);
            var kid = HouseholdFixtures.Person(sim, node, 1, 500);
            foreach (var n in new[] { w, other, m, kid }) HouseholdModule.Form(s, null, n, JoinReason.Founded);
            Act(sim, w, HVerb.SeekChild, 0);
            SeenPerson Seen(Npc observer) { var p = new SeenPerson(); HouseholdHooks.FillPerson(s, observer, w, ref p); return p; }
            Assert.AreEqual(DisplayKind.SeekChild, Seen(m).Display);
            Assert.AreNotEqual(DisplayKind.SeekChild, Seen(other).Display, "another woman does not see an offer she could not take up");
            Assert.AreNotEqual(DisplayKind.SeekChild, Seen(kid).Display, "a child does not see it");
        }

        [Test]
        public void Preferences_DrawnPerPerson_AndReadAsTraits()
        {
            var sim = HouseholdFixtures.Empty(44);
            int node = LearningFixtures.Glade(sim);
            var people = Enumerable.Range(0, 200).Select(i => HouseholdFixtures.Person(sim, node, (byte)(i % 2), 1500)).ToList();
            Assert.That(people.Select(p => p.Life.ChildDesire).Distinct().Count(), Is.GreaterThan(100), "child desire varies between people");
            Assert.That(people.Average(p => p.Life.ChildDesire), Is.InRange(440, 560));
            foreach (var p in people)
            {
                Assert.AreEqual(p.Life.ChildDesire, p.Traits[Preferences.TraitChildDesire]);
                Assert.AreEqual(p.Life.Companionship, p.Traits[Preferences.TraitCompanionship]);
                Assert.AreEqual(p.Life.Commitment, p.Traits[Preferences.TraitCommitment]);
            }
        }

        // ---------------------------------------------------------------- M4.42 (EditMode part): the established band

        [Test]
        public void EstablishedBand_HouseholdsChildrenWithCarersMixedAges_M4_42()
        {
            var t = new SimTuning();
            FounderTuning.EstablishedBand(t.Households.Founders);
            var sim = GodSim.Create(PopulationTests.Setup(5, 20, 6), t);
            var s = sim.State;
            var p = s.Population;
            var cal = SimCalendar.Of(s);
            CollectionAssert.IsEmpty(SimInvariants.Check(s));
            foreach (var f in s.Setup.Factions)
            {
                var ppl = p.Npcs.Where(n => n.Faction == f && n.Founder).ToList();
                var ages = ppl.Select(n => (int)cal.AgeMilli(n.BornTick, s.Tick)).ToList();
                Assert.GreaterOrEqual(ages.Count(a => a < 1000), 5, f + ": dependent children");
                Assert.GreaterOrEqual(ages.Count(a => a >= 1000 && a < 2000), 3, f + ": young adults");
                Assert.GreaterOrEqual(ages.Count(a => a >= 2000 && a < 3000), 5, f + ": mature adults");
                Assert.GreaterOrEqual(ages.Count(a => a >= 3000), 2, f + ": elders");
                Assert.GreaterOrEqual(ages.Distinct().Count(), ages.Count - 2, f + ": birth dates spread");
                foreach (var kid in ppl.Where(n => n.Stage == LifeStage.Child))
                {
                    var h = s.Households.Find(kid.Household);
                    Assert.IsNotNull(h);
                    Assert.IsTrue(h.Members.Any(m => p.Find(m).Stage != LifeStage.Child), "every dependent child has a caregiver in its household");
                }
                Assert.Greater(s.Households.Partnerships.Count(x => p.Find(x.A).Faction == f), 2, f + ": established couples");
            }
            int bio = p.Npcs.Count(n => n.Life.Mother != 0);
            Assert.Greater(bio, 10, "some founder children are their founder parents' biological children");
            foreach (var n in p.Npcs.Where(n => n.Life.Mother != 0))
            {
                Assert.AreEqual(n.Household, p.Find(n.Life.Mother).Household, "a biological founder child lives with its mother");
                Assert.AreEqual(KinRole.Mother, KinData.RoleOf(n.Mind, n.Life.Mother));
                Assert.GreaterOrEqual(cal.AgeMilli(p.Find(n.Life.Mother).BornTick, s.Tick) - cal.AgeMilli(n.BornTick, s.Tick), 1000);
            }
            // deterministic: the same seed builds the same band
            var again = GodSim.Create(PopulationTests.Setup(5, 20, 6), t);
            Assert.AreEqual(sim.StateHash(), again.StateHash());
        }
    }
}
