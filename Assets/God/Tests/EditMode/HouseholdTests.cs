using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Genetics;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// M4's life cycle, households and care as unit tests (EM): natural death, fertility and pregnancy, membership
    /// invariants and telemetry, feeding as a real hand-over the child then eats, children's dependency and competence,
    /// time use, sex-neutral rules, the M4 switch.
    /// </summary>
    public class HouseholdTests
    {
        private static void Run(GodSim sim, int ticks) { for (int i = 0; i < ticks; i++) { sim.Step(); sim.DrainEvents(); } }

        // ---------------------------------------------------------------- the switch

        [Test]
        public void Disabled_NoHouseholdState_NoLifeParts_SameTuningJson()
        {
            var t = new SimTuning(); t.Households.Enabled = 0;
            var sim = GodSim.Create(PopulationTests.Setup(3, 5, 2), t);
            Assert.IsNull(sim.State.Households, "M4 off: no household state, no hash part");
            Assert.IsFalse(sim.State.Population.Npcs.Any(n => n.Life != null || n.Mind.Kin != null));
            Assert.AreEqual("{\"Enabled\":0,\"Immigration\":0}", Newtonsoft.Json.JsonConvert.SerializeObject(t.Households, SaveFile.Settings),
                "the tuning F0 wrote: every M4 number is in a block written only when changed");
            Assert.IsFalse(GodSim.PartNamesOf(sim.State).Contains("household"));
            var on = GodSim.Create(PopulationTests.Setup(3, 5, 2));
            Assert.IsNotNull(on.State.Households);
            Assert.IsTrue(GodSim.PartNamesOf(on.State).Contains("household"));
            Assert.IsTrue(on.State.Population.Npcs.All(n => n.Life != null && n.Mind.Kin != null), "founders are M4 people");
        }

        // ---------------------------------------------------------------- life stages and death (M4.1)

        [Test]
        public void NaturalDeath_Window_M4_1()
        {
            var sim = HouseholdFixtures.Empty(2);
            int node = LearningFixtures.Glade(sim);
            var ages = new List<int>();
            for (int i = 0; i < 1000; i++) ages.Add(HouseholdFixtures.Person(sim, node, (byte)(i % 2), 1500).Life.MaxAgeMilli);
            TestContext.WriteLine($"MaxAge: min {ages.Min()} max {ages.Max()} mean {ages.Average():F0}");
            Assert.That(ages.Min(), Is.GreaterThanOrEqualTo(3300)); Assert.That(ages.Max(), Is.LessThanOrEqualTo(4700));
            Assert.That(ages.Average(), Is.InRange(3900, 4100), "mean natural death age 3.9-4.1");
        }

        [Test]
        public void NaturalDeath_AtMaxAge_WithCauseOldAge_M4_1()
        {
            var sim = HouseholdFixtures.Empty(4, t => { t.Population.HungerPerDay = 0; t.Population.FatiguePerDay = 0; t.Population.FatigueNightPerDay = 0; t.Population.ColdNightPerDay = 0; });
            sim.State.Setup.Disease = DiseaseSetting.Off;
            int node = LearningFixtures.Glade(sim);
            var cal = SimCalendar.Of(sim.State);
            var people = new List<Npc>();
            for (int i = 0; i < 30; i++)
            {
                var n = HouseholdFixtures.Person(sim, node, (byte)(i % 2), 1000);
                Founders.SetAge(sim.State, n, n.Life.MaxAgeMilli - 300); // test-only: close to the end of life
                people.Add(n);
            }
            var probe = new EventProbe(sim);
            probe.Step(1200 * 6);
            var deaths = probe.Of<NpcDied>().ToList();
            Assert.AreEqual(30, deaths.Count, "every one dies within 6 days of 0.3 years before its MaxAge");
            foreach (var d in deaths)
            {
                Assert.AreEqual(DeathCause.OldAge, d.Cause);
                var n = sim.State.Population.Find(d.Npc);
                long age = cal.AgeMilli(n.BornTick, d.Tick);
                Assert.That(age - n.Life.MaxAgeMilli, Is.InRange(0, 101), "dies on its first day check at or past its MaxAge");
            }
        }

        [Test]
        public void LifeStages_Boundaries_AndLifeScale()
        {
            var cal = new SimCalendar(1200, 10, 100);
            Assert.AreEqual(LifeStage.Child, cal.StageAt(0, cal.TicksForAgeMilli(999)));
            Assert.AreEqual(LifeStage.Adult, cal.StageAt(0, cal.TicksForAgeMilli(1000)));
            Assert.AreEqual(LifeStage.Elder, cal.StageAt(0, cal.TicksForAgeMilli(3000)));
            var fast = new SimCalendar(1200, 10, 200);
            Assert.AreEqual(cal.TicksForAgeMilli(1000) / 2, fast.TicksForAgeMilli(1000), "at LifeScale 200 the same ages in half the ticks");
            Assert.AreEqual(16, SimCalendar.HudAge(1000));
        }

        // ---------------------------------------------------------------- fertility and pregnancy (M4.2)

        private static (GodSim sim, Npc mother, Npc father) Couple(ulong seed, int motherAge = 1500, int fatherAge = 1500)
        {
            var sim = HouseholdFixtures.Empty(seed);
            sim.State.Setup.Disease = DiseaseSetting.Off;
            int node = LearningFixtures.Glade(sim);
            var fam = HouseholdFixtures.Family(sim, node, 2, Array.Empty<int>(), false);
            var m = sim.State.Population.Find(fam.Adults[0]); var f = sim.State.Population.Find(fam.Adults[1]);
            Founders.SetAge(sim.State, m, motherAge); Founders.SetAge(sim.State, f, fatherAge);
            return (sim, m, f);
        }

        /// <summary>Runs the mother's daily check for <paramref name="days"/> days with the given "try" choices held (test-only: the choices are set directly).</summary>
        private static int ConceiveWithin(GodSim sim, Npc m, Npc f, bool motherTries, bool fatherTries, int days)
        {
            var s = sim.State;
            var cal = SimCalendar.Of(s);
            for (int d = 0; d < days; d++)
            {
                m.Nutrition = 900000; f.Nutrition = 900000;
                if (motherTries) { m.Life.TryWith = f.Id; m.Life.TryingUntil = s.Tick + 1200; } else { m.Life.TryWith = 0; m.Life.TryingUntil = 0; }
                if (fatherTries) { f.Life.TryWith = m.Id; f.Life.TryingUntil = s.Tick + 1200; } else { f.Life.TryWith = 0; f.Life.TryingUntil = 0; }
                Reproduction.Daily(s, new EventBuffer(), m, (int)cal.Day(s.Tick) + d, cal.AgeMilli(m.BornTick, s.Tick));
                if (m.Life.PregFather != 0) return d + 1;
            }
            return -1;
        }

        [Test]
        public void Fertility_NeedsBothPartners_M4_2()
        {
            int conceptions = 0;
            for (int c = 0; c < 100; c++)
            {
                var (sim, m, f) = Couple((ulong)(100 + c));
                if (ConceiveWithin(sim, m, f, true, false, 5) > 0) conceptions++;
                var (sim2, m2, f2) = Couple((ulong)(300 + c));
                if (ConceiveWithin(sim2, m2, f2, false, true, 5) > 0) conceptions++;
            }
            Assert.AreEqual(0, conceptions, "no conception in 1,000 couple-days when either says no");
            int ok = 0;
            for (int c = 0; c < 100; c++) { var (sim, m, f) = Couple((ulong)(500 + c)); int d = ConceiveWithin(sim, m, f, true, true, 12); if (d > 0) ok++; }
            TestContext.WriteLine("conceived within 12 days: " + ok + " of 100 couples");
            Assert.GreaterOrEqual(ok, 90, "both yes and fertile: conception within 12 days in at least 90% of couples");
        }

        [Test]
        public void Fertility_Window_AdultsOnly_M4_2()
        {
            var (sim, m, f) = Couple(7, motherAge: 950);
            Assert.AreEqual(-1, ConceiveWithin(sim, m, f, true, true, 4), "no conception below 1.0 (mother a child)");
            var (sim2, m2, f2) = Couple(8, fatherAge: 950);
            Assert.AreEqual(-1, ConceiveWithin(sim2, m2, f2, true, true, 4), "nor with a child as the father");
            var (sim3, m3, f3) = Couple(9, motherAge: 2900);
            Assert.AreEqual(-1, ConceiveWithin(sim3, m3, f3, true, true, 6), "nor above the fertility end");
            // same-sex partners never conceive (the reproductive model), every other capability equal
            var sim4 = HouseholdFixtures.Empty(10);
            int node = LearningFixtures.Glade(sim4);
            var a = HouseholdFixtures.Person(sim4, node, 0, 1500); var b = HouseholdFixtures.Person(sim4, node, 0, 1500);
            Partnerships.Form(sim4.State, null, a.Id, b.Id, a.Id, 0);
            Assert.AreEqual(-1, ConceiveWithin(sim4, a, b, true, true, 12));
        }

        [Test]
        public void Pregnancy_Length_And_BirthSpacing_M4_2()
        {
            var (sim, m, f) = Couple(21);
            var s = sim.State;
            var cal = SimCalendar.Of(s);
            var probe = new EventProbe(sim);
            var started = new List<PregnancyStarted>();
            for (int day = 0; day < 30 && probe.Of<NpcBorn>().Count() < 2; day++)
            {
                for (int t = 0; t < 1200; t++)
                {
                    // the couple keeps choosing to try (test-only), and is kept fed
                    m.Life.TryWith = f.Id; m.Life.TryingUntil = s.Tick + 1200; f.Life.TryWith = m.Id; f.Life.TryingUntil = s.Tick + 1200;
                    m.Nutrition = 900000; f.Nutrition = 900000;
                    if (!m.Alive) break;
                    probe.Step(1);
                }
            }
            var births = probe.Of<NpcBorn>().ToList();
            var preg = probe.Of<PregnancyStarted>().ToList();
            Assert.GreaterOrEqual(births.Count, 2, "two births in 30 days of trying");
            for (int i = 0; i < births.Count; i++)
            {
                long len = births[i].Tick - preg[i].Tick;
                Assert.That(cal.AgeMilli(0, len), Is.InRange(245, 255), "full-term pregnancy 0.25 +- 0.005 years");
            }
            Assert.GreaterOrEqual(cal.AgeMilli(0, births[1].Tick - births[0].Tick), 350, "no birth sooner than 0.35 years after the last");
            foreach (var p in preg) { Assert.GreaterOrEqual(p.MotherAgeMilli, 1000); Assert.GreaterOrEqual(p.FatherAgeMilli, 1000); }
        }

        // ---------------------------------------------------------------- a birth

        [Test]
        public void Birth_JoinsMothersHousehold_WithKinFromCues_AndAnEmptyStore()
        {
            var (sim, m, f) = Couple(31);
            var s = sim.State;
            m.Life.PregFather = f.Id; m.Life.PregStart = s.Tick; m.Life.PregDue = s.Tick + 1;
            var probe = new EventProbe(sim);
            probe.Step(5);
            var born = probe.Of<NpcBorn>().Single();
            var child = s.Population.Find(born.Npc);
            Assert.AreEqual(m.Household, child.Household);
            Assert.AreEqual(LifeStage.Child, child.Stage);
            Assert.AreEqual(KinRole.Mother, KinData.RoleOf(child.Mind, m.Id));
            Assert.AreEqual(950, m.Mind.Kin.Beliefs[m.Mind.Kin.IndexOf(child.Id)].Confidence, "the mother saw the birth");
            Assert.AreEqual(f.Id, born.BelievedFather);
            Assert.AreEqual(f.Id, born.BioFather);
            Assert.AreEqual(0, child.Mind.Store.Count, "a newborn learns from nothing yet");
            Assert.AreEqual(1, child.Life.Generation);
            int i = Relatedness.IndexOf(s.Population.Pedigree, child.Id);
            Assert.AreEqual(m.Id, s.Population.Pedigree[i].Mother);
            Assert.AreEqual(512, Relatedness.Phi(s.Population.Pedigree, child.Id, m.Id, 4) * 2);
        }

        // ---------------------------------------------------------------- care: a real hand-over, then the child eats (I-BS.1 seam)

        [Test]
        public void Feeding_IsAHandOverTheChildThenEats_TracedFromProductionToConsumption()
        {
            var sim = HouseholdFixtures.Empty(41);
            sim.State.Setup.Disease = DiseaseSetting.Off;
            var s = sim.State;
            int node = LearningFixtures.Glade(sim);
            var fam = HouseholdFixtures.Family(sim, node, 1, new[] { 200 }, false);
            var parent = s.Population.Find(fam.Adults[0]); var child = s.Population.Find(fam.Children[0]);
            child.Drives[Drive.Hunger] = 650 * 1000; Array.Copy(child.Drives, child.RewardSnap, Drive.Count);
            parent.Drives[Drive.Hunger] = 100 * 1000; Array.Copy(parent.Drives, parent.RewardSnap, Drive.Count);
            int lot = HouseholdFixtures.Give(sim, parent, Ware.Roots, 3);
            var probe = new EventProbe(sim, traceGoods: true);
            probe.Step(1200);
            var fed = probe.Of<ChildFed>().ToList();
            Assert.IsNotEmpty(fed, "the parent fed its begging child (its own decision)");
            var first = fed[0];
            Assert.AreEqual(parent.Id, first.Feeder); Assert.AreEqual(child.Id, first.Child);
            Assert.AreEqual(KinBand.ParentChild, first.Band);
            var moved = probe.Of<ItemMoved>().First(m => m.Reason == MoveReason.HandOver);
            Assert.AreEqual(HolderKind.Carry, moved.FromKind); Assert.AreEqual(parent.Id, moved.FromId);
            Assert.AreEqual(HolderKind.Carry, moved.ToKind); Assert.AreEqual(child.Id, moved.ToId);
            Assert.AreEqual(first.Lot, moved.Lot);
            Assert.IsTrue(probe.Of<Consumed>().Any(c => c.Reason == ConsumeReason.Eaten && c.Npc == child.Id), "the child ate food it was given");
            Assert.IsTrue(probe.Of<BeggingMade>().Any(b => b.Child == child.Id), "it begged first (a display)");
            Assert.IsEmpty(probe.Trace.CheckC3(s));
            Assert.IsEmpty(probe.Trace.CheckC6(s.Territory.Grid), "every hand-over within reach");
        }

        [Test]
        public void YoungChild_CannotForage_OlderChildForagesSlowly_D3()
        {
            int Picked(int ageMilli)
            {
                var sim = HouseholdFixtures.Empty(51);
                sim.State.Setup.Disease = DiseaseSetting.Off;
                int node = LearningFixtures.Glade(sim);
                var c = HouseholdFixtures.Person(sim, node, 0, ageMilli, hunger: 500);
                HouseholdModule.Form(sim.State, null, c, JoinReason.Founded);
                LearningFixtures.Bush(sim, SourceKind.Berries, LearningFixtures.Step(sim, node, 0, 2));
                var probe = new EventProbe(sim);
                for (int t = 0; t < 600; t++) { c.Drives[Drive.Hunger] = 700 * 1000; probe.Step(1); } // test-only: kept hungry, it keeps picking
                return probe.Of<Produced>().Where(p => p.Npc == c.Id && p.Reason == ProduceReason.Foraged).Sum(p => p.Count)
                    + probe.Of<Ate>().Count(a => a.Npc == c.Id && !a.FromCarry);
            }
            int infant = Picked(300), older = Picked(750), adult = Picked(1500);
            TestContext.WriteLine($"food picked in half a day: age 0.3 {infant}, 0.75 {older}, 1.5 {adult}");
            Assert.AreEqual(0, infant, "below 0.5 years a child cannot pick or forage at all");
            Assert.Greater(older, 0, "a 0.75-year-old can");
            Assert.Less(older, adult, "but less than an adult (its competence grows gradually)");
        }

        // ---------------------------------------------------------------- membership (M4.14) and time use (M4.32)

        [Test]
        public void Membership_Invariants_And_Intervals_M4_14_M4_32()
        {
            var t = new SimTuning();
            t.Households.Founders.ChildPermille = 200; t.Households.Founders.ElderPermille = 50; t.Households.Founders.CouplePermille = 600;
            t.Households.Founders.SameSexPermille = 200; t.Households.Founders.StartHuts = 1; t.Households.Founders.StoreFood = 4;
            var sim = GodSim.Create(PopulationTests.Setup(5, 12, 2), t);
            var s = sim.State;
            var events = new List<SimEvent>();
            var tickSums = new List<int>();
            for (int d = 0; d < 20; d++)
            {
                for (int i = 0; i < 1200; i++)
                {
                    sim.Step();
                    var batch = sim.DrainEvents();
                    events.AddRange(batch.Where(e => e is HouseholdJoined || e is HouseholdLeft || e is NpcDied || e is NpcDay || e is NpcBorn));
                }
                var bad = SimInvariants.Check(s);
                Assert.IsEmpty(bad, "day " + d + ": " + string.Join("; ", bad));
            }
            // intervals: joins and leaves pair, never overlap, the last ends at death; founders' first joins were at the start (no buffer)
            var open = new Dictionary<int, int>();
            foreach (var n in s.Population.Npcs) if (n.Founder && n.Life != null) open[n.Id] = -1; // placeholder, set below
            var initial = new Dictionary<int, int>();
            foreach (var e in events)
            {
                if (e is HouseholdLeft l)
                {
                    if (open.TryGetValue(l.Npc, out int h) && h > 0) Assert.AreEqual(h, l.Household, "npc " + l.Npc + " leaves the household it is in");
                    open[l.Npc] = 0;
                }
                else if (e is HouseholdJoined j)
                {
                    if (open.TryGetValue(j.Npc, out int h) && h > 0) Assert.Fail("npc " + j.Npc + " joins household " + j.Household + " while in " + h);
                    open[j.Npc] = j.Household;
                }
            }
            foreach (var n in s.Population.Npcs)
            {
                if (!n.Alive) { Assert.AreEqual(0, n.Household); Assert.IsFalse(open.TryGetValue(n.Id, out int ho) && ho > 0, "npc " + n.Id + "'s last interval ends at its death"); continue; }
                if (open.TryGetValue(n.Id, out int h) && h > 0) Assert.AreEqual(n.Household, h, "the open interval is the household it is in");
            }
            // time use: every person-day row's ticks sum to the day (or the part lived), care counted (Q12)
            long care = 0;
            foreach (var row in events.OfType<NpcDay>())
            {
                int sum = row.Ticks.Sum();
                Assert.That(sum, Is.LessThanOrEqualTo(1200));
                care += row.Ticks[Activity.Care];
            }
            Assert.Greater(care, 0, "care work is counted in time use");
            int full = events.OfType<NpcDay>().Count(r => r.Ticks.Sum() == 1200);
            Assert.Greater(full, events.OfType<NpcDay>().Count() / 2, "a full day's categories sum exactly to its ticks");
        }

        // ---------------------------------------------------------------- Q6: nothing outside reproduction reads a partner's sex (M4.28 static part)

        [Test]
        public void Static_SexIsReadOnlyByReproductionAndThePhenotype_M4_28()
        {
            string root = null;
            for (var d = new DirectoryInfo(Environment.CurrentDirectory); d != null; d = d.Parent)
                if (Directory.Exists(Path.Combine(d.FullName, "Assets", "God", "Sim"))) { root = d.FullName; break; }
            Assert.IsNotNull(root);
            // each allowed use with its reason
            var allowed = new Dictionary<string, string>
            {
                ["LifeCycle.cs"] = "Reproduction: a conception needs a woman and a man; the person-day row records sex",
                ["Founders.cs"] = "the founder mix (scenario data: couples, same-sex couples) and recording a person's sex",
                ["Kin.cs"] = "the name of a kin role (mother or father, daughter or son); r̂ and every rule are the same",
                ["HouseholdState.cs"] = "the field itself",
                ["HouseholdEvents.cs"] = "recorded in events (analytics)",
                ["HouseholdActs.cs"] = "a raising cue's role name (mother or father) for a fostered child",
            };
            var bad = new List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(root, "Assets", "God", "Sim"), "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
            {
                var text = File.ReadAllText(f);
                if (!Regex.IsMatch(text, @"\.Sex\b|\bSex\s*[=!<>]")) continue;
                if (!allowed.ContainsKey(Path.GetFileName(f))) bad.Add(f);
            }
            Assert.IsEmpty(bad, "files read a person's sex outside reproduction, the phenotype and recording:\n" + string.Join("\n", bad));
        }
    }
}
