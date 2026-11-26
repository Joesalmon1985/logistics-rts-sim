using System;
using System.Linq;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M3 statistical island experiment (ACCEPTANCE 1.3 tier 3): the owner's first scenario, six factions of 20 founders in
    /// a sandbox, living on foraging. Survival and the causes of death are recorded, not tuned; an invariant failure fails.
    /// </summary>
    public class IslandPopulationTests
    {
        public sealed class IslandResult
        {
            public ulong Seed; public int Founders, Alive, Deaths, Starved, Exposure, HutsBuilt, HutSites, Sheltered;
            public long Meals, Decisions; public string DeathDays = "";
            public int SourcesEmpty, Sources; public long FoodStock; public double MeanNutrition, MeanHealth, LearnedShare; public int Invariants; public int MeanEntries;
            // from the person-day rows (NpcDay): need satisfaction and time allocation over every person-day
            public long PersonDays, RowsExpected, DayTicks, WalkTicks, MealsRows; public int RowTicks; public long FoodSpoiled, FoodTaken;
            public long[] Ticks = new long[Activity.Count];
            public long SumMeanHunger, SumMeanFatigue, SumMeanCold, SumNutrition, SumReward, SumPunish, SumLearnedShare, SumExploreShare;
            public int HungryDays; // person-days with a mean hunger above comfort (500)
        }

        public static IslandResult Run(ulong seed, int days, int perFaction = 20, int factions = 6, LearningMode mode = LearningMode.On)
        {
            var t = new SimTuning(); t.Learning.DefaultMode = mode;
            var setup = new GameSetup(seed, GameSetup.NoHuman, factions) { Population = true, StartNpcsPerFaction = perFaction, Victory = VictorySpec.SandboxOnly() };
            var built = SimBuilder.Create(setup, t); // the stage 3 AI brains play the factions' states, as in a real sandbox game
            var sim = built.Sim;
            var r = new IslandResult { Seed = seed };
            int tpd = setup.TicksPerDay;
            for (int d = 0; d < days; d++)
            {
                for (int i = 0; i < tpd; i++)
                {
                    int aliveBefore = sim.State.Population.AliveCount, rowsNow = 0;
                    sim.Step();
                    foreach (var e in sim.DrainEvents())
                    {
                        if (e is Consumed c && c.Reason == ConsumeReason.Spoiled && Wares.IsFood(c.Ware)) r.FoodSpoiled += c.Count;
                        if (e is Produced pr && Wares.IsFood(pr.Ware)) r.FoodTaken += pr.Count;
                        if (!(e is NpcDay row)) continue;
                        r.PersonDays++; rowsNow++;
                        foreach (var tk in row.Ticks) r.DayTicks += tk;
                        for (int k = 0; k < Activity.Count; k++) r.Ticks[k] += row.Ticks[k];
                        r.WalkTicks += row.WalkTicks; r.MealsRows += row.Meals;
                        r.SumMeanHunger += row.MeanHunger; r.SumMeanFatigue += row.MeanFatigue; r.SumMeanCold += row.MeanCold; r.SumNutrition += row.Nutrition;
                        r.SumReward += row.RewardSum; r.SumPunish += row.PunishSum; r.SumLearnedShare += row.LearnedShare; r.SumExploreShare += row.ExploreShare;
                        if (row.MeanHunger > 500) r.HungryDays++;
                    }
                    // the rows of a day are written together at the day's start, one per person alive then
                    if (rowsNow > 0) { r.RowsExpected += aliveBefore; r.RowTicks++; }
                }
                r.Invariants += SimInvariants.Check(sim.State).Count;
            }
            var p = sim.State.Population;
            r.Founders = p.Stats.Founders; r.Alive = p.AliveCount; r.Deaths = p.Stats.Deaths;
            r.Starved = p.Stats.DeathsByCause[(int)DeathCause.Starvation]; r.Exposure = p.Stats.DeathsByCause[(int)DeathCause.Exposure];
            r.HutsBuilt = (int)p.Stats.HutsBuilt; r.HutSites = p.Huts.Count(h => !h.Built); r.Meals = p.Stats.Meals; r.Decisions = p.Stats.Decisions;
            var alive = p.Npcs.Where(n => n.Alive).ToList();
            r.Sheltered = alive.Count(n => n.HomeHut != 0);
            r.MeanNutrition = alive.Count == 0 ? 0 : alive.Average(n => n.NutritionPermille);
            r.MeanHealth = alive.Count == 0 ? 0 : alive.Average(n => n.HealthPermille);
            r.LearnedShare = alive.Count == 0 ? 0 : alive.Average(n => n.Mind.LearnedSharePrev);
            r.MeanEntries = alive.Count == 0 ? 0 : (int)alive.Average(n => n.Mind.Store.Count);
            r.DeathDays = string.Join(",", p.Npcs.Where(n => !n.Alive).Select(n => n.DiedTick / tpd).OrderBy(x => x));
            r.Sources = p.Sources.Count(x => x.Kind != SourceKind.Wood); r.SourcesEmpty = p.Sources.Count(x => x.Kind != SourceKind.Wood && x.Stock == 0);
            r.FoodStock = p.Sources.Where(x => x.Kind != SourceKind.Wood).Sum(x => (long)x.Stock);
            return r;
        }

        [Test]
        public void Island_6x20_30Days_Recorded()
        {
            const int days = 30;
            var res = Watch.Seeds(1, 6, s => Run(s, days));
            foreach (var r in res)
                TestContext.WriteLine($"30d seed {r.Seed}: alive {r.Alive}/{r.Founders}, starved {r.Starved}, exposure {r.Exposure} on days [{r.DeathDays}]; food sources empty {r.SourcesEmpty}/{r.Sources}, food in the wild {r.FoodStock}; huts {r.HutsBuilt}, with a bed {r.Sheltered}, nutrition {r.MeanNutrition:F0}");
            TestContext.WriteLine($"ISLAND 6x20 x {days} days, seeds 1-6: survival {res.Sum(r => r.Alive)} of {res.Sum(r => r.Founders)}; starvation {res.Sum(r => r.Starved)}, exposure {res.Sum(r => r.Exposure)}");
            Assert.AreEqual(0, res.Sum(r => r.Invariants));
        }

        /// <summary>
        /// The learning control (owner, 26 Nov): the same 6 x 20 island over 30 days with learning On, Frozen (the store never
        /// changes: instincts and exploration only) and Off (no learned term at all), seeds 1-6. Survival, need satisfaction
        /// (day-mean drives, nutrition, hungry person-days, meals) and time allocation from the person-day rows. The result
        /// is recorded as it comes out (DEV_LOG); only invariants and the person-day grain are asserted.
        /// </summary>
        [Test]
        public void Island_LearningOn_VsFrozen_VsOff_Control()
        {
            const int days = 30;
            var modes = new[] { LearningMode.On, LearningMode.Frozen, LearningMode.Off };
            var all = modes.Select(m => (mode: m, res: Watch.Seeds(1, 6, s => Run(s, days, mode: m)))).ToList();
            TestContext.WriteLine($"CONTROL 6x20, {days} days, seeds 1-6 (720 founders per mode)");
            TestContext.WriteLine("mode | alive | starved | exposure | meals/person-day | mean hunger | mean tiredness | mean cold | nutrition | hungry person-days | with a bed | huts | reward/punish per person-day | learned-changed share | explore share");
            foreach (var (mode, res) in all)
            {
                long pd = res.Sum(r => r.PersonDays);
                TestContext.WriteLine($"{mode} | {res.Sum(r => r.Alive)}/{res.Sum(r => r.Founders)} | {res.Sum(r => r.Starved)} | {res.Sum(r => r.Exposure)} | {(double)res.Sum(r => r.MealsRows) / pd:F2} | "
                    + $"{(double)res.Sum(r => r.SumMeanHunger) / pd:F0} | {(double)res.Sum(r => r.SumMeanFatigue) / pd:F0} | {(double)res.Sum(r => r.SumMeanCold) / pd:F0} | {(double)res.Sum(r => r.SumNutrition) / pd:F0} | "
                    + $"{100.0 * res.Sum(r => r.HungryDays) / pd:F1}% | {res.Sum(r => r.Sheltered)} | {res.Sum(r => r.HutsBuilt)} | {(double)res.Sum(r => r.SumReward) / pd:F0}/{(double)res.Sum(r => r.SumPunish) / pd:F0} | "
                    + $"{(double)res.Sum(r => r.SumLearnedShare) / pd / 10:F1}% | {(double)res.Sum(r => r.SumExploreShare) / pd / 10:F1}%");
            }
            foreach (var (mode, res) in all)
                TestContext.WriteLine($"{mode}: food sources empty at day {days} {res.Sum(r => r.SourcesEmpty)}/{res.Sum(r => r.Sources)}, food left in the wild {res.Sum(r => r.FoodStock)}, total meals {res.Sum(r => r.Meals)}, food taken from the wild {res.Sum(r => r.FoodTaken)}, spoiled {res.Sum(r => r.FoodSpoiled)}; starvation days [{string.Join(",", res.SelectMany(r => r.DeathDays.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).Select(int.Parse).OrderBy(x => x).Distinct())}]");
            TestContext.WriteLine("time allocation (% of person-day ticks): mode | " + string.Join(" | ", Activity.Names) + " | of which walking");
            foreach (var (mode, res) in all)
            {
                double tot = res.Sum(r => r.DayTicks);
                TestContext.WriteLine($"{mode} | " + string.Join(" | ", Enumerable.Range(0, Activity.Count).Select(k => (100.0 * res.Sum(r => r.Ticks[k]) / tot).ToString("F1"))) + $" | {100.0 * res.Sum(r => r.WalkTicks) / tot:F1}");
            }
            foreach (var (mode, res) in all)
            {
                Assert.AreEqual(0, res.Sum(r => r.Invariants), mode + ": no invariant failure");
                Assert.AreEqual(res.Sum(r => r.RowsExpected), res.Sum(r => r.PersonDays), mode + ": exactly one person-day row per living person per day");
                Assert.IsTrue(res.All(r => r.RowTicks == days - 1 || r.RowTicks == days), mode + ": rows come once a day");
            }
        }

        [Test]
        public void Island_6x20_10Days_Survival()
        {
            const int days = 10;
            var res = Watch.Seeds(1, 10, s => Run(s, days));
            foreach (var r in res)
                TestContext.WriteLine($"seed {r.Seed}: alive {r.Alive}/{r.Founders}, deaths {r.Deaths} (starved {r.Starved}, exposure {r.Exposure}), huts built {r.HutsBuilt} (open sites {r.HutSites}), with a bed {r.Sheltered}, meals/person-day {(double)r.Meals / (r.Founders * days):F2}, mean nutrition {r.MeanNutrition:F0}, health {r.MeanHealth:F0}, learned-changed-the-winner {r.LearnedShare / 10.0:F1}%, mean entries {r.MeanEntries}");
            TestContext.WriteLine($"ISLAND 6x20 x {days} days, seeds 1-10: survival {res.Sum(r => r.Alive)} of {res.Sum(r => r.Founders)} ({100.0 * res.Sum(r => r.Alive) / res.Sum(r => r.Founders):F1}%); deaths {res.Sum(r => r.Deaths)} (starvation {res.Sum(r => r.Starved)}, exposure {res.Sum(r => r.Exposure)}); huts built {res.Sum(r => r.HutsBuilt)}; NPCs with a bed {res.Sum(r => r.Sheltered)}");
            Assert.AreEqual(0, res.Sum(r => r.Invariants), "no invariant failure on any day of any seed");
            Assert.Greater(res.Sum(r => r.Alive), res.Sum(r => r.Founders) / 2, "most founders survive on foraging (recorded; collapse is not tuned away)");
        }
    }
}
