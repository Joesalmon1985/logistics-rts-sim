using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Learning;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// The M3 learning criteria revised under owner decision D3 section 5 (ACCEPTANCE_TESTS M3, "Revised acceptance criteria").
    /// Each revised criterion is one <c>Evaluate</c> function that returns the clauses it fails, so the same criterion runs
    /// twice: on the learner, where it must pass, and on a control with learning disabled (Frozen from birth, or traces off),
    /// where it must fail. The control proves that the revised criterion still needs the learning mechanism.
    /// Seed sets are fixed in advance (ACCEPTANCE 1.4); intervals are two-sided 95% t-intervals of the paired per-seed values.
    /// </summary>
    public class M3RevisedCriteriaTests
    {
        private static void Pass(List<string> fails, string what)
        {
            TestContext.WriteLine(what + (fails.Count == 0 ? ": all clauses met" : ": FAILS " + string.Join("; ", fails)));
        }

        // ---------------------------------------------------------------- M3.1 and M3.20: LS1 / L1, hungry near food

        /// <summary>
        /// Revised M3.1 and M3.20. A 10-day probe run (probes on days 1-9, at least 20 per seed), 100 seeds.
        /// M3.1: the learner eats within 5 thinks in >= 90% of all probes, and in >= 80% of the probes of >= 90 of 100 seeds.
        /// M3.20: the paired per-seed difference learner minus Frozen-from-birth has a 95% lower bound >= 20 points, and the
        /// learner is better in >= 90 of 100 seeds.
        /// </summary>
        public static List<string> EvaluateLS1(LearningMode learner)
        {
            var on = Watch.Seeds(1, 100, s => LearningScenarioTests.ForagerProbes(s, learner, InstinctSet.None, 2, 10));
            var fr = Watch.Seeds(1, 100, s => LearningScenarioTests.ForagerProbes(s, LearningMode.Frozen, InstinctSet.None, 2, 10));
            var rOn = on.Select(x => 100.0 * x.Within5 / x.Probes).ToArray();
            var rFr = fr.Select(x => 100.0 * x.Within5 / x.Probes).ToArray();
            double pooled = Watch.Pct(on.Sum(x => x.Within5), on.Sum(x => x.Probes));
            int seeds80 = rOn.Count(r => r >= 80);
            var d = Enumerable.Range(0, 100).Select(i => rOn[i] - rFr[i]).ToArray();
            var ci = Stat.Ci95(d);
            int better = d.Count(x => x > 0);
            TestContext.WriteLine($"LS1 ({learner} vs Frozen, 10 days, seeds 1-100): probes per seed {on.Min(x => x.Probes)}-{on.Max(x => x.Probes)}; pooled {pooled:F1}% (Frozen {Watch.Pct(fr.Sum(x => x.Within5), fr.Sum(x => x.Probes)):F1}%); seeds >= 80%: {seeds80} (>= 95%: {rOn.Count(r => r >= 95)}); paired difference {Stat.Fmt(ci)} points, better in {better}; mean thinks to eat {on.Average(x => x.MeanToEat):F2} vs {fr.Average(x => x.MeanToEat):F2}");
            var fails = new List<string>();
            if (on.Min(x => x.Probes) < 20) fails.Add("fewer than 20 probes in a seed");
            if (pooled < 90) fails.Add($"M3.1 pooled {pooled:F1}% < 90%");
            if (seeds80 < 90) fails.Add($"M3.1 {seeds80} seeds at >= 80% < 90");
            if (ci.lo < 20) fails.Add($"M3.20 lower bound {ci.lo:F1} < 20 points");
            if (better < 90) fails.Add($"M3.20 better in {better} < 90 seeds");
            return fails;
        }

        [Test]
        public void LS1_M3_1_And_M3_20_Revised()
        {
            var fails = EvaluateLS1(LearningMode.On);
            Pass(fails, "LS1 revised, learner");
            Assert.IsEmpty(fails, "revised M3.1 and M3.20");
        }

        [Test]
        public void LS1_M3_1_And_M3_20_Revised_FailWithLearningFrozen()
        {
            var fails = EvaluateLS1(LearningMode.Frozen);
            Pass(fails, "LS1 revised, control: the learner frozen from birth");
            Assert.That(fails.Any(f => f.StartsWith("M3.1")), "M3.1 needs learning");
            Assert.That(fails.Any(f => f.StartsWith("M3.20")), "M3.20 needs learning");
        }

        // ---------------------------------------------------------------- M3.2: L2, eat when hungry, not when sated

        /// <summary>
        /// Revised M3.2, the LS1 natural run (no probes), days 3-5, 100 seeds, thinks with hunger at or below comfort while
        /// the bush is attended: the learner eats in <= 20% of them, and the paired per-seed difference learner minus
        /// Frozen-from-birth has a 95% upper bound <= -10 points, with the learner lower in >= 90 of 100 seeds.
        /// </summary>
        public static List<string> EvaluateL2(LearningMode learner)
        {
            var on = Watch.Seeds(1, 100, s => LearningScenarioTests.Natural(s, learner));
            var fr = Watch.Seeds(1, 100, s => LearningScenarioTests.Natural(s, LearningMode.Frozen));
            double pooled = Watch.Pct(on.Sum(x => x.FullEats), on.Sum(x => x.FullThinks));
            var idx = Enumerable.Range(0, 100).Where(i => on[i].FullThinks > 0 && fr[i].FullThinks > 0).ToArray();
            var d = idx.Select(i => 100.0 * on[i].FullEats / on[i].FullThinks - 100.0 * fr[i].FullEats / fr[i].FullThinks).ToArray();
            var ci = Stat.Ci95(d);
            int lower = d.Count(x => x < 0);
            TestContext.WriteLine($"L2 ({learner} vs Frozen, natural days 3-5, seeds 1-100): eats when sated {pooled:F1}% of {on.Sum(x => x.FullThinks)} thinks (Frozen {Watch.Pct(fr.Sum(x => x.FullEats), fr.Sum(x => x.FullThinks)):F1}%); paired difference {Stat.Fmt(ci)} points over {idx.Length} seeds, lower in {lower}; sign test p = {Stat.SignTestP(lower, d.Count(x => x != 0)):G3}");
            var fails = new List<string>();
            if (idx.Length < 95) fails.Add($"only {idx.Length} seeds with sated thinks in both arms");
            if (pooled > 20) fails.Add($"M3.2 pooled {pooled:F1}% > 20%");
            if (ci.hi > -10) fails.Add($"M3.2 upper bound {ci.hi:F1} > -10 points");
            if (lower < 90) fails.Add($"M3.2 lower in {lower} < 90 seeds");
            return fails;
        }

        [Test]
        public void L2_M3_2_Revised()
        {
            var fails = EvaluateL2(LearningMode.On);
            Pass(fails, "L2 revised, learner");
            Assert.IsEmpty(fails, "revised M3.2");
        }

        [Test]
        public void L2_M3_2_Revised_FailsWithLearningFrozen()
        {
            var fails = EvaluateL2(LearningMode.Frozen);
            Pass(fails, "L2 revised, control: the learner frozen from birth");
            Assert.That(fails.Any(f => f.StartsWith("M3.2")), "M3.2 needs learning");
        }

        // ---------------------------------------------------------------- M3.3: L3, delayed reward

        /// <summary>
        /// Revised M3.3 (the filler clause replaced; the TestA clauses unchanged). TestA in >= 90% of the last 50 trials in
        /// >= 95 of 100 seeds, and the delay is bridged by the traces: with traces disabled (TraceShift 16) the same lab stays
        /// <= 65%. The filler's learned value is printed, not asserted (it predicts the reward that follows it; NPC_LEARNING 4.5).
        /// </summary>
        public static List<string> EvaluateL3(int traceShift, LearningMode mode)
        {
            var on = Watch.Seeds(1, 100, s => LearningScenarioTests2.DelayedRewardRun(s, traceShift, 200, mode));
            var off = Watch.Seeds(1, 100, s => LearningScenarioTests2.DelayedRewardRun(s, 16));
            int seeds90 = on.Count(r => r.ALast50 >= 0.9);
            double meanOff = off.Average(r => r.ALast50);
            TestContext.WriteLine($"L3 (TraceShift {traceShift}, {mode}): TestA in the last 50 trials {on.Average(r => r.ALast50):P1}, seeds >= 90%: {seeds90}; traces off {meanOff:P1}; recorded, not asserted: filler Q {on.Average(r => r.FillerQ):F0} vp, TestA {on.Average(r => r.QA):F0}, TestB {on.Average(r => r.QB):F0}");
            var fails = new List<string>();
            if (seeds90 < 95) fails.Add($"M3.3 {seeds90} seeds at >= 90% < 95");
            if (meanOff > 0.65) fails.Add($"M3.3 traces-off arm {meanOff:P1} > 65%");
            return fails;
        }

        [Test]
        public void L3_M3_3_Revised()
        {
            var fails = EvaluateL3(2, LearningMode.On);
            Pass(fails, "L3 revised, learner");
            Assert.IsEmpty(fails, "revised M3.3");
        }

        [Test]
        public void L3_M3_3_Revised_FailsWithTracesOffOrLearningFrozen()
        {
            var off = EvaluateL3(16, LearningMode.On);
            Pass(off, "L3 revised, control: traces off");
            var frozen = EvaluateL3(2, LearningMode.Frozen);
            Pass(frozen, "L3 revised, control: frozen");
            Assert.That(off.Any(f => f.StartsWith("M3.3")), "M3.3 needs the traces");
            Assert.That(frozen.Any(f => f.StartsWith("M3.3")), "M3.3 needs learning");
        }

        // ---------------------------------------------------------------- M3.7: L8, same nature, different nurture

        /// <summary>
        /// Revised M3.7. Identical-nature pairs, one raised 5 days on berries, the other on nightshade and roots; the store is
        /// then frozen as loaded (NPC_LEARNING 4.16) and both live 5 days in the same test glade (nightshade and roots), so the
        /// rate measured is what the nurture taught, over every meal choice (median about 31 per NPC instead of 6). The pair
        /// difference in nightshade share has a 95% lower bound >= 30 points, and the rich-raised twin eats more nightshade in
        /// >= 45 of 50 pairs. The store-hash and identical-life clauses are unchanged (LearningScenarioTests2.L8).
        /// </summary>
        public static List<string> EvaluateL8(LearningMode nurture)
        {
            var rich = Watch.Seeds(1, 50, s => LearningScenarioTests2.NurtureProbe(s, false, nurture));
            var bitter = Watch.Seeds(1, 50, s => LearningScenarioTests2.NurtureProbe(s, true, nurture));
            var d = Enumerable.Range(0, 50).Select(i => 100 * (rich[i].ShadeShare - bitter[i].ShadeShare)).ToArray();
            var ci = Stat.Ci95(d);
            int pos = d.Count(x => x > 0), neg = d.Count(x => x < 0);
            TestContext.WriteLine($"L8 (nurture {nurture}, seeds 1-50): nightshade share rich {rich.Average(x => x.ShadeShare):P1}, bitter {bitter.Average(x => x.ShadeShare):P1}; median meal choices {LearningScenarioTests.Median(rich.Select(x => x.Choices).Concat(bitter.Select(x => x.Choices)))}; pair difference {Stat.Fmt(ci)} points; >= 30 in {d.Count(x => x >= 30)}, > 0 in {pos}, < 0 in {neg}; sign test p = {Stat.SignTestP(pos, pos + neg):G3}");
            var fails = new List<string>();
            if (ci.lo < 30) fails.Add($"M3.7 lower bound {ci.lo:F1} < 30 points");
            if (pos < 45) fails.Add($"M3.7 rich-raised higher in {pos} < 45 pairs");
            return fails;
        }

        [Test]
        public void L8_M3_7_Revised()
        {
            var fails = EvaluateL8(LearningMode.On);
            Pass(fails, "L8 revised, learner");
            Assert.IsEmpty(fails, "revised M3.7");
        }

        [Test]
        public void L8_M3_7_Revised_FailsWithLearningFrozen()
        {
            var fails = EvaluateL8(LearningMode.Frozen);
            Pass(fails, "L8 revised, control: both twins frozen from birth");
            Assert.That(fails.Any(f => f.StartsWith("M3.7")), "M3.7 needs learning");
        }

        // ---------------------------------------------------------------- M3.11: L13, generalisation to a new food

        /// <summary>
        /// Revised M3.11 (the conjunction clause unchanged, asserted in LearningScenarioTests2.L13). The first fish meal, eaten
        /// at the spot or from the carry after foraging it, comes within 6 thinks of the fish appearing in >= 80% of 100 seeds;
        /// the control raised where berries were never edible stays <= 40%; and the learner beats the same NPC frozen from
        /// birth by >= 20 seeds.
        /// </summary>
        public static List<string> EvaluateL13(LearningMode learner)
        {
            var raised = Watch.Seeds(1, 100, s => LearningScenarioTests2.GeneraliseDetail(s, true, learner));
            var control = Watch.Seeds(1, 100, s => LearningScenarioTests2.GeneraliseDetail(s, false, learner));
            var frozen = Watch.Seeds(1, 100, s => LearningScenarioTests2.GeneraliseDetail(s, true, LearningMode.Frozen));
            int Fast(LearningScenarioTests2.GeneraliseResult[] r) => r.Count(x => x.FirstMeal > 0 && x.FirstMeal <= 6);
            int fast = Fast(raised), fastControl = Fast(control), fastFrozen = Fast(frozen);
            TestContext.WriteLine($"L13 ({learner}, seeds 1-100): first fish meal within 6 thinks: raised on berries {fast}, control (berries never edible) {fastControl}, raised but frozen from birth {fastFrozen}; published count (Eat at the spot within 3 attended thinks) {raised.Count(x => x.EatSpotWithin3Attended)}, frozen {frozen.Count(x => x.EatSpotWithin3Attended)}; first act on the fish was ForageAt in {raised.Count(x => x.FirstVerbOnFish == Verb.ForageAt)}");
            var fails = new List<string>();
            if (fast < 80) fails.Add($"M3.11 {fast} seeds < 80");
            if (fastControl > 40) fails.Add($"M3.11 control {fastControl} > 40");
            if (fast - fastFrozen < 20) fails.Add($"M3.11 only {fast - fastFrozen} seeds above frozen-from-birth (< 20)");
            return fails;
        }

        [Test]
        public void L13_M3_11_Revised()
        {
            var fails = EvaluateL13(LearningMode.On);
            Pass(fails, "L13 revised, learner");
            Assert.IsEmpty(fails, "revised M3.11");
        }

        [Test]
        public void L13_M3_11_Revised_FailsWithLearningFrozen()
        {
            var fails = EvaluateL13(LearningMode.Frozen);
            Pass(fails, "L13 revised, control: the learner frozen from birth");
            Assert.That(fails.Any(f => f.StartsWith("M3.11")), "M3.11 needs learning");
        }
    }
}
