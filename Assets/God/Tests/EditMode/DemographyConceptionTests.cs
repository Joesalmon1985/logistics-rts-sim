using System.Collections.Generic;
using System.Linq;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// F2 (owner D4): the conception-coordination rule behind <c>Households.Life.ConceptionMode</c> (<see cref="Conception"/>).
    /// F2-T2: mode 1 changes only the coordination rule (a dormant reproductive relationship with a third adult no longer keeps
    /// two partners who both try apart); consent is kept in both modes; a live intent or a running pregnancy still blocks.
    /// The flag's serialisation (mode 0 writes nothing) is checked here too; F2-T1's run-level pins are in the runner tests.
    /// </summary>
    public class DemographyConceptionTests
    {
        private static int Day(GodState s) => (int)SimCalendar.Of(s).Day(s.Tick);

        private static void Act(GodSim sim, Npc npc, byte verb, int target)
        {
            var s = sim.State;
            npc.Action = new NpcAction { Verb = verb, TargetId = target, Since = s.Tick };
            HouseholdActs.Act(s, new EventBuffer(), npc, Day(s));
        }

        private static void Couple(GodSim sim, Npc a, Npc b)
        {
            var s = sim.State;
            var h = HouseholdModule.Form(s, null, a, JoinReason.Founded);
            HouseholdModule.Join(s, null, h, b, JoinReason.Founded, 0);
            Partnerships.Form(s, null, a.Id, b.Id, a.Id, h.Id);
            KinCues.Believe(s, null, a, b.Id, KinRole.Partner, 900, KinSource.Partnered);
            KinCues.Believe(s, null, b, a.Id, KinRole.Partner, 900, KinSource.Partnered);
        }

        /// <summary>A woman and a man in a partnership, and a third man; certain conception when allowed (chance 1000).</summary>
        private static (GodSim sim, Npc w, Npc m, Npc x) Setup(int mode, ulong seed)
        {
            var sim = HouseholdFixtures.Empty(seed, t =>
            {
                t.Households.Life.ConceiveBasePermille = 1000; t.Households.Life.ConceiveSpanPermille = 0;
                t.Households.Life.ConceptionMode = mode;
            });
            sim.State.Setup.Disease = DiseaseSetting.Off;
            int node = LearningFixtures.Glade(sim);
            var w = HouseholdFixtures.Person(sim, node, 0, 1500);
            var m = HouseholdFixtures.Person(sim, node, 1, 1500);
            var x = HouseholdFixtures.Person(sim, node, 1, 1600);
            HouseholdModule.Form(sim.State, null, x, JoinReason.Founded);
            Couple(sim, w, m);
            foreach (var n in new[] { w, m, x }) { n.Life.Commitment = 0; n.Nutrition = 900000; }
            return (sim, w, m, x);
        }

        private static List<SimEvent> Check(GodSim sim, Npc mother)
        {
            var s = sim.State;
            var ev = new EventBuffer();
            Reproduction.Daily(s, ev, mother, Day(s), SimCalendar.Of(s).AgeMilli(mother.BornTick, s.Tick));
            var l = new List<SimEvent>();
            ev.DrainInto(l);
            return l;
        }

        [Test]
        public void ConceptionMode1_CoordinationOnly_F2_T2()
        {
            foreach (int mode in new[] { 0, 1 })
            foreach (bool heHoldsIt in new[] { false, true })
            {
                string tag = "mode " + mode + (heHoldsIt ? ", the man holds the old relationship" : ", the woman holds the old relationship");
                var (sim, w, m, x) = Setup(mode, heHoldsIt ? 61UL : 62UL);
                var s = sim.State;
                // an earlier reproductive relationship with a third adult, which that adult no longer tries in (dormant)
                Npc third = x;
                if (heHoldsIt) { third = HouseholdFixtures.Person(sim, LearningFixtures.Glade(sim), 0, 1500); HouseholdModule.Form(s, null, third, JoinReason.Founded); third.Life.Commitment = 0; }
                var old = ReproductiveUnions.Form(s, null, heHoldsIt ? m : w, third, third.Id, ReproductiveUnion.Independent);

                // consent: one partner's intent alone never conceives, in either mode, and ends nothing
                Act(sim, w, HVerb.TryForChild, m.Id);
                var one = Check(sim, w);
                Assert.AreEqual(0, w.Life.PregFather, tag + ": her intent alone");
                Assert.IsFalse(one.OfType<ReproductiveUnionEnded>().Any(), tag + ": nothing ends on one intent");
                Assert.AreEqual(1, ReproductiveUnions.Total(s));

                // both partners hold "try" with each other
                Act(sim, m, HVerb.TryForChild, w.Id);
                var both = Check(sim, w);
                if (mode == 0)
                {
                    Assert.AreEqual(0, w.Life.PregFather, tag + ": the dormant relationship blocks them (the M4/D4 rule)");
                    Assert.IsNotNull(ReproductiveUnions.Between(s, old.A, old.B), tag + ": mode 0 ends nothing");
                    Assert.IsFalse(both.Any(e => e is ReproductiveUnionEnded || e is ReproductiveUnionFormed || e is PregnancyStarted));
                }
                else
                {
                    Assert.AreEqual(m.Id, w.Life.PregFather, tag + ": the partners conceive");
                    Assert.IsNull(ReproductiveUnions.Between(s, old.A, old.B), tag + ": the dormant relationship ended");
                    var ended = both.OfType<ReproductiveUnionEnded>().Single();
                    Assert.AreEqual(ReproductiveUnionEnded.Lapsed, ended.Reason);
                    Assert.IsNotNull(ReproductiveUnions.Between(s, w.Id, m.Id), tag + ": their own relationship formed");
                    Assert.IsTrue(both.OfType<PregnancyStarted>().Any());
                }
                Assert.IsNotNull(Partnerships.Between(s, w.Id, m.Id), tag + ": no partnership changed");
                CollectionAssert.IsEmpty(SimInvariants.Check(s), tag);
            }
        }

        [Test]
        public void ConceptionMode1_LiveIntentOrPregnancy_StillBlocks_F2_T2()
        {
            // the third adult still tries in the old relationship: it is not dormant, so mode 1 leaves it and nothing is conceived
            var (sim, w, m, x) = Setup(1, 63);
            var s = sim.State;
            ReproductiveUnions.Form(s, null, w, x, x.Id, ReproductiveUnion.Independent);
            Act(sim, x, HVerb.TryForChild, w.Id);
            Act(sim, w, HVerb.TryForChild, m.Id); Act(sim, m, HVerb.TryForChild, w.Id);
            var ev = Check(sim, w);
            Assert.AreEqual(0, w.Life.PregFather);
            Assert.IsNotNull(ReproductiveUnions.Between(s, w.Id, x.Id), "a relationship the other still tries in is kept");
            Assert.IsFalse(ev.OfType<ReproductiveUnionEnded>().Any());

            // the man's old relationship carries a pregnancy (the other woman's by him): kept, and it blocks
            var (sim2, w2, m2, _) = Setup(1, 64);
            var s2 = sim2.State;
            var other = HouseholdFixtures.Person(sim2, LearningFixtures.Glade(sim2), 0, 1500);
            HouseholdModule.Form(s2, null, other, JoinReason.Founded);
            ReproductiveUnions.Form(s2, null, m2, other, other.Id, ReproductiveUnion.Independent);
            other.Life.PregFather = m2.Id; other.Life.PregStart = s2.Tick; other.Life.PregDue = s2.Tick + 3000;
            Act(sim2, w2, HVerb.TryForChild, m2.Id); Act(sim2, m2, HVerb.TryForChild, w2.Id);
            Check(sim2, w2);
            Assert.AreEqual(0, w2.Life.PregFather);
            Assert.IsNotNull(ReproductiveUnions.Between(s2, m2.Id, other.Id), "a relationship with a running pregnancy is kept");
        }

        [Test]
        public void ConceptionMode_DefaultOneIsNotWritten_ZeroIs()
        {
            // Checkpoint B re-pin (LEAD_RULINGS R60): the default is 1 and is omitted, so the default tuning JSON is unchanged
            var t = new SimTuning();
            Assert.AreEqual(1, t.Households.Life.ConceptionMode, "mode 1 is the default since Checkpoint B");
            string fresh = t.ToJson();
            StringAssert.DoesNotContain("ConceptionMode", fresh);
            t.Households.Life.ConceptionMode = 1;
            Assert.AreEqual(fresh, t.ToJson(), "the default serialises exactly as before the flag existed");
            t.Households.Life.TryingTicks = 1300; // a changed Life block writes its fields, still without the flag at its default
            StringAssert.DoesNotContain("ConceptionMode", t.ToJson());
            t.Households.Life.ConceptionMode = 0;
            StringAssert.Contains("\"ConceptionMode\":0", t.ToJson());
            Assert.AreEqual(0, Newtonsoft.Json.JsonConvert.DeserializeObject<SimTuning>(t.ToJson(), SaveFile.Settings).Households.Life.ConceptionMode, "an explicit 0 survives the round trip");
            Assert.IsTrue(new SimTuning().TrySet("Households.Life.ConceptionMode", 0, out string err), err);
        }
    }
}
