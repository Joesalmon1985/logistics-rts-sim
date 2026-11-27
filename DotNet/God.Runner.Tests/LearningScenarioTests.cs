using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M3 controlled learning scenarios (NPC_LEARNING 10, the owner's LS1, LS3, LS4, and L1-L13 where they apply). Every
    /// scenario is a real game with the whole sim ticking; nothing scripts the learner's choices. Seeds are fixed sets
    /// named in advance (ACCEPTANCE 1.4). Numbers are printed for the report.
    /// </summary>
    public class LearningScenarioTests
    {
        // ---------------------------------------------------------------- LS1 / L1 / L2: hungry near food

        public sealed class ForagerResult
        {
            public int ThinksToFirstEat = -1;
            public int Episodes2, Success2, Episodes1, Success1;
            public int FullThinks, FullEats;
            public int[] HungryThinksByDay = new int[6], HungryEatsByDay = new int[6];
            public int Meals;
        }

        public static ForagerResult Forager(ulong seed, LearningMode mode, InstinctSet inst, int days = 5)
        {
            var sim = LearningFixtures.Empty(seed);
            int glade = LearningFixtures.Glade(sim);
            var npc = LearningFixtures.Npc(sim, glade, inst, mode, hunger: 600);
            LearningFixtures.Bush(sim, SourceKind.Berries, LearningFixtures.Step(sim, glade, 0, 2));
            var w = new Watch(sim, npc.Id);
            w.RunDays(days);
            var r = new ForagerResult { Meals = (int)sim.State.Population.Stats.Meals };
            var rows = w.Rows[npc.Id];
            for (int i = 0; i < rows.Count; i++) if (rows[i].Verb == Verb.Eat) { r.ThinksToFirstEat = i + 1; break; }
            int comfort = sim.State.Tuning.Population.ComfortHunger, high = sim.State.Tuning.Population.BandHigh;
            bool in2 = false, in1 = false; int since2 = 0, since1 = 0; bool done2 = false, done1 = false;
            foreach (var t in rows)
            {
                bool bush = t.Attends(ObjClass.BerryBush);
                bool eat = t.Verb == Verb.Eat;
                if (t.Hunger > comfort && bush) { r.HungryThinksByDay[Math.Min(5, t.Day)]++; if (eat) r.HungryEatsByDay[Math.Min(5, t.Day)]++; }
                if (t.Day < 3) { in2 = t.Hunger >= high; in1 = t.Hunger > comfort; continue; }
                if (t.Hunger <= comfort && bush) { r.FullThinks++; if (eat) r.FullEats++; }
                // episodes: hunger crossing into band 2 (and, reported beside it, band 1) while the bush is attended
                if (t.Hunger >= high && bush) { if (!in2) { in2 = true; since2 = 0; done2 = false; r.Episodes2++; } since2++; if (!done2 && eat) { done2 = true; if (since2 <= 5) r.Success2++; } }
                else if (t.Hunger < high) in2 = false;
                if (t.Hunger > comfort && bush) { if (!in1) { in1 = true; since1 = 0; done1 = false; r.Episodes1++; } since1++; if (!done1 && eat) { done1 = true; if (since1 <= 5) r.Success1++; } }
                else if (t.Hunger <= comfort) in1 = false;
            }
            return r;
        }

        public sealed class ProbeResult { public int Probes, Within5, FullThinks, FullEats; public double MeanToEat; public int FullEatCarry, FullEatBush; public int[] ByDayProbes = new int[16], ByDayWithin = new int[16], FullByDay = new int[16], FullEatsByDay = new int[16]; }

        /// <summary>
        /// LS1 with hunger probes: days 1-2 are natural; from day 1 on, four times a day (ticks 100, 400, 700, 1000 of the
        /// day) the NPC's hunger is set to 600 (band 2), the same manipulation in every arm. A probe succeeds when it chooses
        /// Eat within 5 thinks while the bush is in view. L2 is counted on the natural thinks below comfort.
        /// </summary>
        public static ProbeResult ForagerProbes(ulong seed, LearningMode mode, InstinctSet inst, int dist = 3, int days = 5, Action<SimTuning> tune = null)
        {
            var sim = LearningFixtures.Empty(seed, tune);
            int glade = LearningFixtures.Glade(sim);
            var npc = LearningFixtures.Npc(sim, glade, inst, mode, hunger: 600);
            LearningFixtures.Bush(sim, SourceKind.Berries, LearningFixtures.Step(sim, glade, 0, dist));
            var w = new Watch(sim, npc.Id);
            int tpd = w.TicksPerDay;
            var probeAt = new List<long>();
            w.RunDays(days, tick =>
            {
                int tod = (int)(tick % tpd);
                if (tick / tpd >= 1 && (tod == 100 || tod == 400 || tod == 700 || tod == 1000) && npc.Alive && !npc.Action.Asleep)
                {
                    npc.Drives[Drive.Hunger] = 600000; npc.RewardSnap[Drive.Hunger] = 600000;
                    probeAt.Add(tick);
                }
            });
            var r = new ProbeResult();
            var rows = w.Rows[npc.Id];
            var waits = new List<int>();
            foreach (long p in probeAt)
            {
                int day = (int)(p / tpd);
                var after = rows.Where(x => x.Tick > p).Take(12).ToList();
                if (after.Count == 0) continue;
                r.Probes++; r.ByDayProbes[day]++;
                int k = after.FindIndex(x => x.Verb == Verb.Eat);
                if (k >= 0 && k < 5) { r.Within5++; r.ByDayWithin[day]++; }
                waits.Add(k < 0 ? 12 : k + 1);
            }
            r.MeanToEat = waits.Count > 0 ? waits.Average() : double.NaN;
            int comfort = sim.State.Tuning.Population.ComfortHunger;
            foreach (var t in rows)
            {
                if (t.Day < 2 || t.Hunger > comfort || !t.Attends(ObjClass.BerryBush)) continue;
                bool nearProbe = probeAt.Any(p => t.Tick > p && t.Tick - p < 150);
                if (nearProbe) continue;
                r.FullThinks++; if (t.Verb == Verb.Eat) r.FullEats++;
                r.FullByDay[t.Day]++; if (t.Verb == Verb.Eat) r.FullEatsByDay[t.Day]++;
                if (t.Verb == Verb.Eat) { if (t.TargetClass == ObjClass.FoodLot) r.FullEatCarry++; else r.FullEatBush++; }
            }
            return r;
        }

        public static int[] Seeds100 => Enumerable.Range(1, 100).ToArray();

        /// <summary>
        /// LS1 (M3.1, M3.2, M3.20; NPC_LEARNING L1, L2, LS1): a naive NPC (no instincts) beside a berry bush. L1 is measured
        /// with hunger probes on days 1-4 (identical in every arm), L2 on natural days (no probes) below comfort, 100 seeds.
        /// </summary>
        [Test]
        public void LS1_HungryNearFood()
        {
            var on = Watch.Seeds(1, 100, s => ForagerProbes(s, LearningMode.On, InstinctSet.None, 2, 5));
            var frozen = Watch.Seeds(1, 100, s => ForagerProbes(s, LearningMode.Frozen, InstinctSet.None, 2, 5));
            var natOn = Watch.Seeds(1, 100, s => Natural(s, LearningMode.On));
            var natFrozen = Watch.Seeds(1, 100, s => Natural(s, LearningMode.Frozen));
            double rateOn = Watch.Pct(on.Sum(x => x.Within5), on.Sum(x => x.Probes)), rateFrozen = Watch.Pct(frozen.Sum(x => x.Within5), frozen.Sum(x => x.Probes));
            int seeds95 = on.Count(x => x.Probes > 0 && 100.0 * x.Within5 / x.Probes >= 95);
            double firstOn = Median(natOn.Select(x => x.ThinksToFirstEat)), firstFrozen = Median(natFrozen.Select(x => x.ThinksToFirstEat));
            double fullOn = Watch.Pct(natOn.Sum(x => x.FullEats), natOn.Sum(x => x.FullThinks)), fullFrozen = Watch.Pct(natFrozen.Sum(x => x.FullEats), natFrozen.Sum(x => x.FullThinks));
            double hungryOn = Watch.Pct(natOn.Sum(x => x.HungryEats), natOn.Sum(x => x.HungryThinks)), hungryFrozen = Watch.Pct(natFrozen.Sum(x => x.HungryEats), natFrozen.Sum(x => x.HungryThinks));
            TestContext.WriteLine($"LS1 probes (seeds 1-100): learner eats within 5 thinks in {rateOn:F1}% of {on.Sum(x => x.Probes)} hungry probes; {seeds95} of 100 seeds at >= 95%; frozen-from-birth {rateFrozen:F1}%");
            TestContext.WriteLine("LS1 probe success by day, learner: " + string.Join(" ", Enumerable.Range(1, 4).Select(d => "d" + d + " " + Watch.Pct(on.Sum(x => x.ByDayWithin[d]), on.Sum(x => x.ByDayProbes[d])).ToString("F1") + "%"))
                + " | frozen: " + string.Join(" ", Enumerable.Range(1, 4).Select(d => "d" + d + " " + Watch.Pct(frozen.Sum(x => x.ByDayWithin[d]), frozen.Sum(x => x.ByDayProbes[d])).ToString("F1") + "%")));
            TestContext.WriteLine($"LS1 mean thinks to eat after a probe: learner {on.Average(x => x.MeanToEat):F2}, frozen {frozen.Average(x => x.MeanToEat):F2}; median thinks to the first meal ever (starting at hunger 600): learner {firstOn}, frozen {firstFrozen}");
            TestContext.WriteLine($"L2 natural days 2-4, bush in view: eats in {fullOn:F1}% of thinks below comfort (frozen {fullFrozen:F1}%), {hungryOn:F1}% of thinks above comfort (frozen {hungryFrozen:F1}%)");
            // the learning effect itself (owner: the result must come from learning)
            Assert.Greater(rateOn, rateFrozen + 20, "learning raises eating when hungry well above the frozen-from-birth NPC");
            Assert.Greater(on.Sum(x => x.ByDayWithin[4]) * 100.0 / on.Sum(x => x.ByDayProbes[4]), on.Sum(x => x.ByDayWithin[1]) * 100.0 / on.Sum(x => x.ByDayProbes[1]), "the probability rises with experience");
            Assert.Less(fullOn, fullFrozen, "the learner eats less than the frozen NPC when not hungry");
        }

        public sealed class NaturalResult { public int ThinksToFirstEat = -1, FullThinks, FullEats, HungryThinks, HungryEats; public int[] FullByDay = new int[32], FullEatsByDay = new int[32]; }

        public static NaturalResult Natural(ulong seed, LearningMode mode, int days = 5)
        {
            var sim = LearningFixtures.Empty(seed);
            int glade = LearningFixtures.Glade(sim);
            var npc = LearningFixtures.Npc(sim, glade, InstinctSet.None, mode, hunger: 600);
            LearningFixtures.Bush(sim, SourceKind.Berries, LearningFixtures.Step(sim, glade, 0, 2));
            var w = new Watch(sim, npc.Id);
            w.RunDays(days);
            var r = new NaturalResult();
            var rows = w.Rows[npc.Id];
            for (int i = 0; i < rows.Count; i++) if (rows[i].Verb == Verb.Eat) { r.ThinksToFirstEat = i + 1; break; }
            int comfort = sim.State.Tuning.Population.ComfortHunger;
            foreach (var t in rows)
            {
                if (t.Day < 2 || !t.Attends(ObjClass.BerryBush)) continue;
                if (t.Hunger <= comfort) { r.FullThinks++; if (t.Verb == Verb.Eat) r.FullEats++; r.FullByDay[Math.Min(31, t.Day)]++; if (t.Verb == Verb.Eat) r.FullEatsByDay[Math.Min(31, t.Day)]++; }
                else { r.HungryThinks++; if (t.Verb == Verb.Eat) r.HungryEats++; }
            }
            return r;
        }

        public static double Median(IEnumerable<int> xs)
        {
            var a = xs.OrderBy(x => x).ToArray();
            return a.Length == 0 ? double.NaN : a[a.Length / 2];
        }
    }
}
