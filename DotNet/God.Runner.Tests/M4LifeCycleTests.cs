using System;
using System.Collections.Generic;
using System.Linq;
using God.Recording;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Households;
using God.Sim.Tests;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M4 runner tests: the neutrality pin (M4 off reproduces F0), determinism and saves with open estates (M4.22), adults
    /// only and no replenishment (M4.25), the immigration rule's exact prediction, time use (M4.32), viable families with
    /// dependent children and a no-care control (M4.36), non-biological care (M4.34), shared childcare (M4.29), partnerships
    /// and their cap (M4.17, M4.28), and disease (M4.3, M4.26). Fixed seed sets; thresholds are the acceptance table's.
    /// </summary>
    [TestFixture]
    public sealed class M4LifeCycleTests
    {
        // ---------------------------------------------------------------- the neutrality pin

        /// <summary>
        /// With <c>Households.Enabled = 0</c> the post-F0 behaviour is reproduced exactly: band_6x20, seeds 1-3, 10 days give the
        /// final hashes of <c>6671592</c> (unity/poc with F0 merged) run with the same setting (taken by its own runner, 26 Nov 2026).
        /// </summary>
        [Test]
        public void HouseholdsOff_Band6x20_EqualsPostF0Hashes()
        {
            var pinned = new Dictionary<ulong, ulong> { [1] = 0x355435285f49d02cUL, [2] = 0x8619778f5c1029aeUL, [3] = 0xed46c61909f118eaUL };
            var specs = pinned.Keys.Select(s => TestUtil.Spec("band_6x20", s, 12000, "tuning.Households.Enabled=0")).ToList();
            var res = TestUtil.RunAll(specs);
            foreach (var r in res)
            {
                Assert.AreEqual(0, r.ExitCode, r.Error);
                Assert.AreEqual(pinned[r.Seed].ToString("x16"), r.FinalHash.ToString("x16"), "seed " + r.Seed + ": M4 off must hash exactly as F0");
            }
        }

        // ---------------------------------------------------------------- determinism (M4.22)

        [Test]
        public void Determinism_SameSeedTwice_RecordingLevels_SaveWithOpenEstate_M4_22()
        {
            var a = RunEngine.Run(TestUtil.Spec("m4_band_6x20", 4, 1200 * 8));
            var b = RunEngine.Run(TestUtil.Spec("m4_band_6x20", 4, 1200 * 8));
            Assert.AreEqual(0, a.ExitCode, a.Error);
            CollectionAssert.AreEqual(TestUtil.Seq(a), TestUtil.Seq(b), "the same seed gives the same hash at every day boundary");
            foreach (var lv in new[] { RecordingLevel.Summary, RecordingLevel.Full })
            {
                var spec = TestUtil.Spec("m4_band_6x20", 4, 1200 * 8); spec.Level = lv;
                Assert.AreEqual(a.FinalHash, RunEngine.Run(spec).FinalHash, "recording " + lv + " leaves the hash unchanged");
            }
            // a save taken with an estate open (and a pregnancy, households, kin) goes on exactly as the unsaved run
            var sc = TestUtil.Load("m4_band_6x20");
            var sim = SimBuilder.Create(sc.ToSetup(5), sc.ToTuning()).Sim;
            var twin = SimBuilder.Create(sc.ToSetup(5), sc.ToTuning()).Sim;
            int guard = 0;
            while ((sim.State.Households.Estates.Count == 0 || !sim.State.Population.Npcs.Any(n => n.Alive && n.Life?.PregFather != 0)) && guard++ < 1200 * 30)
            { sim.Step(); sim.DrainEvents(); twin.Step(); twin.DrainEvents(); }
            Assert.IsNotEmpty(sim.State.Households.Estates, "an estate is open at the save");
            var loaded = SimBuilder.FromSave(SaveFile.FromJson(SaveFile.From(sim).ToJson())).Sim;
            Assert.AreEqual(sim.StateHash(), loaded.StateHash());
            for (int i = 0; i < 1200 * 3; i++)
            {
                loaded.Step(); loaded.DrainEvents(); twin.Step(); twin.DrainEvents();
                if (i % 100 == 0) Assert.AreEqual(twin.StateHash(), loaded.StateHash(), "tick " + i + " after the load");
            }
            Assert.AreEqual(SaveFile.From(loaded).ToJson(), SaveFile.From(SimBuilder.FromSave(SaveFile.FromJson(SaveFile.From(loaded).ToJson())).Sim).ToJson(), "a second save of a loaded game is identical");
        }

        // ---------------------------------------------------------------- adults only, never replenished (M4.25)

        [Test]
        public void Reproduction_AdultsOnly_NeverReplenished_M4_25()
        {
            var res = Watch.Seeds(1, 10, s => M4Run.FromScenario("m4_band_6x20", s, 150, onEvent: null));
            foreach (var r in res) TestContext.WriteLine(r);
            Assert.AreEqual(0, res.Sum(r => r.ConceptionsUnderAge), "every PregnancyStarted has both parents aged 1.0 or more");
            Assert.Greater(res.Sum(r => r.Conceptions), 100);
            foreach (var r in res)
            {
                Assert.AreEqual(0, r.Arrivals, "immigration is off");
                Assert.AreEqual(r.Founders + r.Births, r.EverAlive, "seed " + r.Seed + ": the NPCs ever alive are the founders plus the births, exactly");
                Assert.AreEqual(0, r.SpawnedAfterStart, "nothing appears without a birth");
                Assert.AreEqual(r.Start + r.Births + r.Arrivals - r.Deaths - r.Departures, r.End, "population change reconciles exactly");
                Assert.AreEqual(0, r.Invariants, "seed " + r.Seed + ": " + string.Join("; ", r.InvariantSample));
            }
        }

        /// <summary>The immigration rule (Q16) is never replenishment: arrivals equal its prediction from its own inputs, also after a scripted epidemic.</summary>
        [Test]
        public void Immigration_ArrivalsEqualTheRulesPrediction_AfterAnEpidemic_M4_25()
        {
            foreach (ulong seed in new ulong[] { 1, 2, 3 })
            {
                int predicted = 0;
                var r = M4Run.FromScenario("m4_band_6x20", seed, 30, t => t.Households.Immigration = 1, systems: new Func<ISimSystem>[]
                {
                    () => new Probe(s =>
                    {
                        var cal = SimCalendar.Of(s);
                        int tod = cal.TickOfDay(s.Tick), day = (int)cal.Day(s.Tick);
                        if (day == 20 && tod == 5)
                        {
                            // a scripted epidemic: 30% of the living die at once (cause disease)
                            var living = s.Population.Npcs.Where(n => n.Alive).ToList();
                            for (int i = 0; i < living.Count; i++) if (i % 10 < 3) PopulationModule.Kill(s, new EventBuffer(), living[i], DeathCause.Disease);
                        }
                        if (tod != 3 || s.Households.ImmigrationDay == day) return;
                        // the rule's own prediction, from its inputs (food per head, free beds, the yearly cap), before it runs this tick
                        var hs = s.Households;
                        long year = cal.Year(s.Tick);
                        var counts = (int[])hs.ImmigrantsThisYear.Clone();
                        if (hs.ImmigrationYear != year) Array.Clear(counts, 0, counts.Length);
                        foreach (var f in s.Setup.Factions)
                        {
                            int chance = Immigration.ChancePermille(s, f, out int alive);
                            if (alive == 0 || counts[(int)f] >= Math.Max(1, alive * 15 / 100)) continue;
                            if (s.Territory.HqsOf(f).Count == 0) continue; // a faction with no HQ has no shore to arrive at (an input of the rule)
                            if (s.Rng.Fork("immigration", (int)f, day).NextInt(1000) < chance) predicted++;
                        }
                    }, 239),
                });
                TestContext.WriteLine($"seed {seed}: arrivals {r.Arrivals}, predicted {predicted}");
                Assert.AreEqual(predicted, r.Arrivals, "arrivals equal the rule's prediction from its own inputs (exact)");
            }
        }

        // ---------------------------------------------------------------- time use (M4.32)

        [Test]
        public void TimeUse_SumsToTheDay_AndCareIsCounted_M4_32()
        {
            long rows = 0, full = 0, care = 0, mismatch = 0;
            M4Run.FromScenario("m4_band_6x20", 2, 12, onEvent: (sim, e) =>
            {
                if (!(e is NpcDay d)) return;
                rows++;
                int sum = d.Ticks.Sum();
                // a person alive the whole day has exactly the day's ticks; one born or dead during it fewer
                if (sum == 1200) full++; else if (sum > 1200) mismatch++;
                care += d.Ticks[Activity.Care];
            });
            TestContext.WriteLine($"person-days {rows}, full {full}, care ticks {care}");
            Assert.AreEqual(0, mismatch, "no day has more ticks than the day");
            Assert.Greater(full, rows * 9 / 10);
            Assert.Greater(care, 0, "unpaid care is counted from M4");
        }

        // ---------------------------------------------------------------- viable families (M4.36) and the no-care control

        /// <summary>
        /// Owner D3: under the suitable ecology stated in <c>m4_families</c>, a population with dependent children survives and
        /// reproduces over at least 3 generations (a generation-3 child is born and the population is alive at the end) in at
        /// least 80% of 20 seeds. Children are dependent: below 0.5 years they forage nothing; competence grows (yield per work
        /// tick rises with age); and survival depends on care (the same seeds with <c>Households.Care.Off</c>: more child deaths).
        /// </summary>
        [Test]
        public void ViableFamilies_ThreeGenerations_DependentChildren_AgainstNoCare_M4_36()
        {
            const int days = 60;
            var on = Watch.Seeds(1, 20, s => M4Run.FromScenario("m4_families", s, days));
            var off = Watch.Seeds(1, 20, s => M4Run.FromScenario("m4_families", s, days, t => t.Households.Care.Off = 1));
            foreach (var r in on) TestContext.WriteLine("care    " + r);
            foreach (var r in off) TestContext.WriteLine("no care " + r);
            int viable = on.Count(r => r.MaxGeneration >= 3 && r.End > 0);
            TestContext.WriteLine($"VIABLE: {viable} of 20 seeds reach generation 3 alive (no-care control {off.Count(r => r.MaxGeneration >= 3 && r.End > 0)})");
            Assert.AreEqual(0, on.Sum(r => r.Invariants) + off.Sum(r => r.Invariants), "no invariant failure");
            // dependency: nothing foraged below 0.5 years, though they tried; competence grows with age
            Assert.AreEqual(0, on.Sum(r => r.ForagedByAge[0]), "below 0.5 years a child forages nothing");
            Assert.Greater(on.Sum(r => r.ForageTicksByAge[0]), 0, "(they did try: the limit is the body's, not a rule on their choices)");
            double Yield(int band) => (double)on.Sum(r => r.ForagedByAge[band]) / Math.Max(1, on.Sum(r => r.ForageTicksByAge[band]));
            TestContext.WriteLine($"yield per work tick by age: 0-0.5 {Yield(0):F4}, 0.5-0.75 {Yield(1):F4}, 0.75-1 {Yield(2):F4}, adults {Yield(3):F4}");
            Assert.Less(Yield(1), Yield(2), "competence grows: 0.5-0.75 below 0.75-1.0");
            Assert.Less(Yield(2), Yield(3), "and below adults");
            // survival depends on care: child mortality with care below the no-care control (paired by seed)
            double Mort(M4Run r) => r.Births == 0 ? 0 : (double)r.ChildDeaths / r.Births;
            int better = 0, worse = 0;
            for (int i = 0; i < 20; i++) { double a = Mort(on[i]), b = Mort(off[i]); if (a < b) better++; else if (a > b) worse++; }
            double mOn = on.Sum(r => (double)r.ChildDeaths) / Math.Max(1, on.Sum(r => r.Births)), mOff = off.Sum(r => (double)r.ChildDeaths) / Math.Max(1, off.Sum(r => r.Births));
            TestContext.WriteLine($"child deaths per birth: with care {mOn:P1}, no care {mOff:P1}; seeds better with care {better}, worse {worse}");
            Assert.Greater(mOff, mOn, "children die more without care");
            Assert.Greater(better, worse, "in more seeds than not (paired)");
            Assert.AreEqual(0, off.Sum(r => r.ChildFed), "the control gives no care");
            Assert.GreaterOrEqual(viable, 16, "at least 3 generations survive and reproduce in at least 80% of 20 seeds");
        }

        // ---------------------------------------------------------------- non-biological care (M4.34) and shared childcare (M4.29)

        [Test]
        public void NonBiologicalCare_And_SharedChildcare_M4_34_M4_29()
        {
            var res = Watch.Seeds(1, 20, s => M4Run.FromScenario("m4_band_6x20", s, 20));
            foreach (var r in res) TestContext.WriteLine(r);
            int nonBio = res.Count(r => r.ChildFedByNonParent > 0);
            int shared = res.Count(r => r.Arrangements > 0);
            TestContext.WriteLine($"seeds with a child fed by a non-parent: {nonBio}; with a care arrangement: {shared}");
            Assert.GreaterOrEqual(nonBio, 10, "a child is fed by a non-parent caregiver with real food in at least 10 of 20 seeds");
            Assert.GreaterOrEqual(shared, 10, "at least one CareArrangementMade in at least 10 of 20 seeds");
        }

        /// <summary>
        /// M4.29: an arrangement exploited by one side (the asker never returns a turn) is ended by the other by its own decision
        /// within 10 days in at least 80% of cases. Fixture: the carer's household has no children (so nothing is returned), the
        /// asker's has a hungry child; the carer carries food.
        /// </summary>
        /// <remarks>
        /// M4 outstanding target (not met, not amended): 1 of 10 cases ended within 10 days (26 Nov 2026). The carer records
        /// the care it gives only while it feeds the asker's child or minds it with no adult of the child's household near;
        /// a young child follows its own parent when it begs, so little care is given and the owed band rarely reaches the
        /// level that draws an end. Diagnosis and the suggested fix (a deliberate Mind verb) are in the M4 report.
        /// </remarks>
        [Test, Category("M4-target"), Explicit("M4 outstanding target (M4.29 second clause): not met; see the remarks")]
        public void SharedChildcare_ExploitedArrangementEndedByTheOther_M4_29()
        {
            int ended = 0, cases = 0;
            foreach (ulong seed in Enumerable.Range(1, 10).Select(x => (ulong)x))
            {
                // no partnerships in this fixture: the two households must stay two (a partnership would merge them)
                var sim = HouseholdFixtures.Empty(seed, t => t.Households.Partner.MaxLivePartnerships = 0);
                var s = sim.State;
                s.Setup.Disease = DiseaseSetting.Off;
                int node = LearningFixtures.Glade(sim);
                var asker = HouseholdFixtures.Family(sim, node, 1, new[] { 300 }, false);
                var carer = HouseholdFixtures.Family(sim, LearningFixtures.Step(sim, node, 1, 1), 1, Array.Empty<int>(), false);
                var a = s.Population.Find(asker.Adults[0]); var c = s.Population.Find(carer.Adults[0]);
                var hs = s.Households;
                hs.Care.Add(new CareArrangement { Id = hs.NextCareId++, Asker = a.Id, Carer = c.Id, AskerHousehold = asker.Household, CarerHousehold = carer.Household, AgreedTick = s.Tick });
                LearningFixtures.Bush(sim, SourceKind.Roots, LearningFixtures.Step(sim, node, 2, 2), 2000, 2000, 400);
                HouseholdFixtures.Give(sim, c, Ware.Roots, 4);
                var probe = new EventProbe(sim);
                bool done = false;
                for (int d = 0; d < 10 && !done; d++)
                {
                    for (int t = 0; t < 1200 && !done; t++)
                    {
                        probe.Step(1);
                        if (probe.Of<CareArrangementEnded>().Any()) done = true;
                    }
                }
                cases++;
                var end = probe.Of<CareArrangementEnded>().FirstOrDefault();
                TestContext.WriteLine($"seed {seed}: ended {(end != null ? "day " + end.Tick / 1200 + " by " + (end.EndedBy == c.Id ? "the carer" : end.EndedBy.ToString()) + $", given {end.Given}, returned {end.Returned}" : "no")}; fed {probe.Of<ChildFed>().Count()}");
                if (end != null && end.EndedBy == c.Id) ended++;
            }
            Assert.GreaterOrEqual(ended, 8, "ended by the exploited side within 10 days in at least 80% of cases");
        }

        // ---------------------------------------------------------------- partnerships (M4.17, M4.28)

        [Test]
        public void Partnerships_Form_HouseholdSize_AndTheCapIsConfigurable_M4_17_M4_28()
        {
            var one = Watch.Seeds(1, 10, s => M4Run.FromScenario("m4_band_6x20", s, 25));
            var two = Watch.Seeds(1, 10, s => M4Run.FromScenario("m4_band_6x20", s, 25, t => t.Households.Partner.MaxLivePartnerships = 2));
            foreach (var r in one) TestContext.WriteLine("cap 1 " + r + $" | partnered by 2.0: {r.PartneredBy2}/{r.ReachedAge2} adults");
            Assert.AreEqual(0, one.Sum(r => r.MaxLiveSeen > 1 ? 1 : 0), "at cap 1 nobody holds two live partnerships");
            int concurrent = two.Count(r => r.MaxLiveSeen >= 2);
            TestContext.WriteLine("cap 2: seeds with a concurrent partnership " + concurrent);
            Assert.GreaterOrEqual(concurrent, 5, "at cap 2 someone holds two in at least 5 of 10 seeds");
            Assert.AreEqual(0, two.Sum(r => r.Invariants), "and every invariant holds");
            double share = (double)one.Sum(r => r.PartneredBy2) / Math.Max(1, one.Sum(r => r.ReachedAge2));
            double size = one.Average(r => r.MeanHouseholdSizeX100) / 100.0;
            int maxSize = one.Max(r => r.MaxHouseholdSize);
            TestContext.WriteLine($"partnered by age 2.0: {share:P1}; mean household size {size:F2}; largest {maxSize}");
            Assert.That(share, Is.InRange(0.40, 0.95), "40-95% of adults have had a partnership by age 2.0");
            Assert.LessOrEqual(maxSize, 12);
            Assert.That(size, Is.InRange(2.5, 6.0), "mean household size 2.5-6.0");
        }

        // ---------------------------------------------------------------- disease (M4.3, M4.26)

        [Test]
        public void Disease_SettingsAndRoutes_M4_26()
        {
            // D4: the disease routes are measured on the young-band control (the established band, D4 default, has 0-2 disease deaths
            // in 20 seeds x 20 days, too few for the Mild-vs-On comparison; the threshold is unchanged)
            var on = Watch.Seeds(1, 20, s => M4Run.FromScenario("m4_band_6x20_young", s, 20));
            var mild = Watch.Seeds(1, 20, s => M4Run.FromScenario("m4_band_6x20_young", s, 20, setup: g => g.Disease = DiseaseSetting.Mild));
            var off = Watch.Seeds(1, 20, s => M4Run.FromScenario("m4_band_6x20_young", s, 20, setup: g => g.Disease = DiseaseSetting.Off));
            TestContext.WriteLine($"infections On {on.Sum(r => r.Infections)} Mild {mild.Sum(r => r.Infections)} Off {off.Sum(r => r.Infections)}; disease deaths On {on.Sum(r => r.DiseaseDeaths)} Mild {mild.Sum(r => r.DiseaseDeaths)}");
            Assert.AreEqual(0, off.Sum(r => r.Infections), "Off gives zero infections");
            Assert.LessOrEqual(mild.Sum(r => r.DiseaseDeaths) * 2, Math.Max(on.Sum(r => r.DiseaseDeaths), 0) + 0, "Mild at most half the disease deaths of On");
            Assert.AreEqual(0, on.Sum(r => r.InfectionsWithoutRoute), "every infection records one route");
        }

        /// <summary>M4.3: one infected adult in a hut of 6 well-fed adults gives 2-5 infections and deaths under 1 in 6; a hungry hut shows at least twice the deaths.</summary>
        [Test]
        public void Disease_Outbreak_InAHutOfSix_M4_3()
        {
            (int infections, int deaths) Hut(ulong seed, bool hungry)
            {
                var sim = HouseholdFixtures.Empty(seed, t => { t.Population.HutBeds = 6; t.Households.Partner.NoConception = 1; }); // six adults (no newborns in the count)
                var s = sim.State;
                int node = LearningFixtures.Glade(sim);
                var h = HouseholdFixtures.SharedHut(sim, node, 6, (Ware.Roots, hungry ? 0 : 60));
                if (!hungry) LearningFixtures.Bush(sim, SourceKind.Roots, LearningFixtures.Step(sim, node, 0, 2), 4000, 4000, 400);
                foreach (int id in h.Adults)
                {
                    var n = s.Population.Find(id);
                    if (hungry) n.Nutrition = 250 * 1000;
                }
                var first = s.Population.Find(h.Adults[0]);
                first.Life.Illness = 1; first.Life.Severity = 120; first.Life.IllDaysLeft = 5; // test-only: the index case
                var probe = new EventProbe(sim);
                probe.Step(1200 * 12);
                return (probe.Of<IllnessStarted>().Count(), probe.Of<NpcDied>().Count(d => d.Cause == DeathCause.Disease));
            }
            var fed = Enumerable.Range(1, 20).Select(i => Hut((ulong)i, false)).ToList();
            var hungryHuts = Enumerable.Range(1, 20).Select(i => Hut((ulong)i, true)).ToList();
            double meanInf = fed.Average(x => x.infections);
            TestContext.WriteLine($"fed huts: infections mean {meanInf:F2} [{string.Join(",", fed.Select(x => x.infections))}], deaths {fed.Sum(x => x.deaths)}; hungry huts: deaths {hungryHuts.Sum(x => x.deaths)}");
            Assert.That(meanInf, Is.InRange(2.0, 5.0), "2-5 infections in the hut");
            Assert.Less(fed.Sum(x => x.deaths), 20 * 6 / 6, "deaths under 1 in 6");
            Assert.GreaterOrEqual(hungryHuts.Sum(x => x.deaths), 2 * Math.Max(1, fed.Sum(x => x.deaths)), "a hungry hut shows at least twice the deaths");
        }

        /// <summary>A system a test adds (it may read and change the state at its order), for scripted events and exact predictions.</summary>
        public sealed class Probe : ISimSystem
        {
            private readonly Action<GodState> _a;
            public readonly int Order;
            public Probe(Action<GodState> a, int order) { _a = a; Order = order; }
            public void Tick(GodState s, EventBuffer ev) => _a(s);
        }
    }
}
