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
                for (int i = 0; i < tpd; i++) { sim.Step(); sim.DrainEvents(); }
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
