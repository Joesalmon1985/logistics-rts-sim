using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using God.Sim.Lab;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// M2 lab unit tests (ACCEPTANCE M2.1, M2.14 in part, M2.16, M2.20; STRATEGY_BENCHMARKS section 10 tests 1-7, 9, 10, 12,
    /// 14, 24). The experiment presets themselves run in God.Runner.Tests (LabRunnerTests), where they are cheap.
    /// </summary>
    public class LabTests
    {
        private const Move C = Move.Cooperate, D = Move.Defect;

        private static Move[] Script(string s) => s.Split(' ').Select(x => x == "C" ? C : D).ToArray();

        private static string Answers(RepeatedKind k, string partner, int p = -1)
        {
            var st = new BenchRepeated(k, p).NewPair();
            var rng = new Rng(7).Fork("lab-test");
            var res = new List<string>();
            foreach (var their in Script(partner))
            {
                var mine = st.Decide(rng);
                res.Add(mine == C ? "C" : "D");
                st.Observe(mine, their);
            }
            return string.Join(" ", res);
        }

        // ---- M2.1 / tests 1-4: golden move sequences against the scripted partner C D D C C D ----

        [TestCase(RepeatedKind.Tft, "C C D D C C")]
        [TestCase(RepeatedKind.AllD, "D D D D D D")]
        [TestCase(RepeatedKind.AllC, "C C C C C C")]
        [TestCase(RepeatedKind.Tf2t, "C C C D C C")]
        [TestCase(RepeatedKind.Stft, "D C D D C C")]
        [TestCase(RepeatedKind.Grudger, "C C D D D D")]
        [TestCase(RepeatedKind.Pavlov, "C C D C C C")]
        [TestCase(RepeatedKind.Tester, "D C D D C C")]
        [TestCase(RepeatedKind.Sneak, "C C D D C C")]
        public void GoldenSequence_AgainstCDDCCD(RepeatedKind k, string expected)
        {
            // STRATEGY_BENCHMARKS test 1
            Assert.AreEqual(expected, Answers(k, "C D D C C D"));
        }

        [Test]
        public void Probers_GoldenSequences()
        {
            // q = 0: the Naive Prober is Tit for Tat; q = 1000: it probes whenever the partner's last move was C.
            Assert.AreEqual("C C D D C C", Answers(RepeatedKind.NaiveProber, "C D D C C D", 0));
            Assert.AreEqual("C D D D D D", Answers(RepeatedKind.NaiveProber, "C D D C C D", 1000));
            // Remorseful: probe on round 2 answered by D, so round 3 is C (remorse), round 4 echoes D, round 5 probes again
            Assert.AreEqual("C D C D D D", Answers(RepeatedKind.RemorsefulProber, "C D D C C D", 1000));
        }

        [Test]
        public void Tf2t_NeverDefectsAgainstAlternation_DefectsOnRound4Only()
        {
            // test 2
            Assert.AreEqual("C C C C C C", Answers(RepeatedKind.Tf2t, "C D C D C D"));
            Assert.AreEqual("C C C D", Answers(RepeatedKind.Tf2t, "C D D C"));
        }

        [Test]
        public void Grudger_DefectsFor100RoundsAfterOneDefection()
        {
            // test 3
            string partner = "D " + string.Join(" ", Enumerable.Repeat("C", 100));
            var a = Answers(RepeatedKind.Grudger, partner).Split(' ');
            Assert.AreEqual("C", a[0]);
            for (int i = 1; i <= 100; i++) Assert.AreEqual("D", a[i], "round " + (i + 1));
        }

        [Test]
        public void RemorsefulProber_CooperatesOnceAfterAPunishedProbe_ThenFollowsThePartner()
        {
            // test 4: round 2 is a probe (q = 1000) answered by D in the same round; round 3 is C; round 4 follows the partner's round 3 (D)
            Assert.AreEqual("C D C D", Answers(RepeatedKind.RemorsefulProber, "C D D C", 1000));
        }

        [Test]
        public void Random_CooperatesAtPPermille()
        {
            // test 7: 100,000 draws within p +/- 5 permille
            foreach (int p in new[] { 500, 300, 900 })
            {
                var st = new BenchRepeated(RepeatedKind.Random, p).NewPair();
                var rng = new Rng(11).Fork("random", p);
                int c = 0;
                for (int i = 0; i < 100000; i++) { var m = st.Decide(rng); if (m == C) c++; st.Observe(m, C); }
                Assert.That(c / 100, Is.InRange(p - 5, p + 5), "p = " + p);
            }
        }

        // ---- test 5, 6: contest strategies ----

        private static ContestOutcome Duel(ContestKind a, ContestKind b, Role ra = Role.None, Role rb = Role.None, bool pa = false, bool pb = false)
            => Contest.Resolve(a, b, ra, rb, pa, pb, true, ContestRules.Lab, true);

        [Test]
        public void ContestTurns_RetaliatorBullyDove()
        {
            var o = Duel(ContestKind.Retaliator, ContestKind.Hawk);
            Assert.AreEqual(CMove.Display, o.MovesA[0]);
            Assert.AreEqual(CMove.Escalate, o.MovesA[1], "Retaliator against Hawk escalates from turn 2");
            Assert.IsTrue(o.Fight);
            o = Duel(ContestKind.Bully, ContestKind.Retaliator);
            Assert.AreEqual(3, o.Turns);
            Assert.AreEqual(CMove.Withdraw, o.MovesA[2], "Bully against Retaliator withdraws on turn 3");
            Assert.AreEqual(-10, o.PayA); Assert.AreEqual(40, o.PayB);
            o = Duel(ContestKind.Dove, ContestKind.Hawk);
            Assert.AreEqual(2, o.Turns);
            Assert.AreEqual(CMove.Withdraw, o.MovesA[1], "Dove against Hawk withdraws on turn 2");
            Assert.AreEqual(0, o.PayA); Assert.AreEqual(50, o.PayB);
        }

        [Test]
        public void ContestTurns_BourgeoisAndProber()
        {
            var o = Duel(ContestKind.Bourgeois, ContestKind.Dove, Role.Intruder, Role.Resident);
            Assert.AreEqual(1, o.Turns);
            Assert.AreEqual(CMove.Withdraw, o.MovesA[0], "Bourgeois as intruder withdraws on turn 1");
            var always = new ContestRules(50, 100, 10, 10, 8, 1000);
            var never = new ContestRules(50, 100, 10, 10, 8, 0);
            for (int i = 0; i < 200; i++)
            {
                var rng = new Rng(3).Fork("probe", i);
                // Contest.Play draws the probe once at the start; with q = 1000 the first move is always Escalate
                bool pa = LabRandom.Permille(rng, always.ProbePermille), pn = LabRandom.Permille(rng, never.ProbePermille);
                Assert.IsTrue(pa); Assert.IsFalse(pn);
                Assert.AreEqual(CMove.Escalate, Contest.Resolve(ContestKind.Prober, ContestKind.Dove, Role.None, Role.None, pa, false, true, always, true).MovesA[0]);
                Assert.AreEqual(CMove.Display, Contest.Resolve(ContestKind.Prober, ContestKind.Dove, Role.None, Role.None, pn, false, true, never, true).MovesA[0]);
            }
        }

        [Test]
        public void ContestRules_GiveTheHawkDoveMatrix_AndBothResidentIsAFight()
        {
            var r = ContestRules.Lab;
            Assert.AreEqual(-25000, Contest.ExpectedX1000(ContestKind.Hawk, ContestKind.Hawk, false, r));
            Assert.AreEqual(50000, Contest.ExpectedX1000(ContestKind.Hawk, ContestKind.Dove, false, r));
            Assert.AreEqual(0, Contest.ExpectedX1000(ContestKind.Dove, ContestKind.Hawk, false, r));
            Assert.AreEqual(15000, Contest.ExpectedX1000(ContestKind.Dove, ContestKind.Dove, false, r));
            // the resident/intruder table of 4.2 is reproduced exactly by the turn rules with random roles
            var doc = L02Retaliator.DocHdba();
            var kinds = L02Retaliator.Hdba();
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++)
                Assert.AreEqual(doc[i][j], Contest.ExpectedX1000(kinds[i], kinds[j], true, r), kinds[i] + " vs " + kinds[j]);
            int win = Contest.Resolve(ContestKind.Bourgeois, ContestKind.Bourgeois, Role.Resident, Role.Resident, false, false, true, r).PayA;
            int lose = Contest.Resolve(ContestKind.Bourgeois, ContestKind.Bourgeois, Role.Resident, Role.Resident, false, false, false, r).PayA;
            Assert.AreEqual(-50, win + lose, "both think they are resident: a fight, -25 on average");
        }

        // ---- test 9: the dollar Prisoner's Dilemma ----

        [Test]
        public void DollarPayoffs_ArePrisonersDilemma_AndViolationFails()
        {
            var d = PdPayoffs.Dollars;
            Assert.IsTrue(d.T > d.R && d.R > d.P && d.P > d.S);
            Assert.IsTrue(d.IsPrisonersDilemma);
            Assert.IsFalse(new PdPayoffs(700, 300, -10, -100).IsPrisonersDilemma, "(T + S)/2 = R fails");
            Assert.IsTrue(PdPayoffs.Axelrod.IsPrisonersDilemma);
        }

        // ---- tests 10, 12: the replicator ----

        [Test]
        public void Replicator_HawkDoveFixedPoint_FromSeveralStarts()
        {
            var rep = new Replicator(L01HawkDove.MatrixX1000(), 500000);
            foreach (int start in new[] { 10, 500, 990 })
            {
                var x = Replicator.SharesPermille(start, 1000 - start);
                for (int g = 0; g < 3000; g++) rep.Step(x);
                Assert.That(Fx.ToPermille(x[0]), Is.InRange(580, 586), "start " + start);
            }
        }

        [Test]
        public void Replicator_SharesAlwaysSumToExactly2Pow30()
        {
            var rng = new Rng(5).Fork("rep-sum");
            for (int t = 0; t < 50; t++)
            {
                int k = 2 + rng.NextInt(10);
                var m = new long[k, k];
                for (int i = 0; i < k; i++) for (int j = 0; j < k; j++) m[i, j] = rng.NextRange(-50000, 600000);
                var x = new long[k];
                for (int i = 0; i < k; i++) x[i] = 1 + rng.NextInt(1000000);
                Replicator.Normalise(x);
                var rep = new Replicator(m, 700000, t % 2 == 0 ? Replicator.MuQ20(2, 1000) : 0);
                for (int g = 0; g < 200; g++)
                {
                    rep.Step(x);
                    Assert.AreEqual(Fx.One, x.Sum(), "trial " + t + " generation " + g);
                    Assert.IsTrue(x.All(v => v >= 0));
                }
            }
        }

        // ---- fixed point ----

        [Test]
        public void FixedPoint_LnExpSqrtMulDiv()
        {
            Assert.AreEqual(Fx.Ln2, Fx.Ln(2 * Fx.One), 2);
            Assert.AreEqual(0, Fx.Ln(Fx.One));
            Assert.AreEqual(2918732888L, Fx.Exp(Fx.One), 8, "e in Q30");           // 2.718281828 x 2^30
            Assert.AreEqual(395007542L, Fx.Exp(-Fx.One), 2, "1/e in Q30");          // 0.367879441 x 2^30
            Assert.AreEqual(Fx.One, Fx.Exp(Fx.Ln(Fx.One)));
            for (long v = 1; v < 40; v++)
                Assert.AreEqual(v * Fx.One, Fx.Exp(Fx.Ln(v * Fx.One)), v * 16, "exp(ln " + v + ")");
            Assert.AreEqual(Fx.FromInt(3), Fx.Sqrt(Fx.FromInt(9)), 1);
            Assert.AreEqual(7L, Fx.MulDiv(10, 7, 10));
            Assert.AreEqual(long.MaxValue / 3, Fx.MulDiv(long.MaxValue, 1, 3), 1);
            Assert.AreEqual(-4L, Fx.MulDiv(-7, 3, 5)); // -4.2 rounds to -4
            Assert.AreEqual(123456789L * 987654321L / 1000, Fx.MulDiv(123456789L, 987654321L, 1000), 1);
            Assert.AreEqual("0.583", Fx.Format(Fx.FromRatio(7, 12), 3));
            Assert.AreEqual("-1.50", Fx.FormatScaled(-150, 2));
        }

        // ---- L11: the book's worked example (notes C1) ----

        [Test]
        public void Hamilton_MushroomCall_FromTheNotes()
        {
            // 8 mushrooms worth +6; calling: 12 + 1/2 x 12 + 1/8 x 12 = 19.5 against keep 18 → Call
            Assert.IsTrue(L11Hamilton.Call(12000, 18000, new[] { (1L, 2L, 12000L), (1L, 8L, 12000L), (0L, 1L, 12000L) }, out long t));
            Assert.AreEqual(19500, t);
            // without the cousin: 18 against 18, a tie keeps quiet
            Assert.IsFalse(L11Hamilton.Call(12000, 18000, new[] { (1L, 2L, 12000L), (0L, 1L, 12000L) }, out t));
            Assert.AreEqual(18000, t);
        }

        // ---- M2.14 / tests 11, 13: determinism ----

        [Test]
        public void SameConfigAndSeed_SameHash_DifferentSeedDiffers()
        {
            var e = new L07SuckerCheatGrudger();
            ulong a = e.Run(new LabConfig { Seed = 1 }).ResultHash, b = e.Run(new LabConfig { Seed = 1 }).ResultHash;
            Assert.AreEqual(a, b);
            var p = new L05PeckingOrder();
            Assert.AreEqual(p.Run(new LabConfig { Seed = 3 }).ResultHash, p.Run(new LabConfig { Seed = 3 }).ResultHash);
            Assert.AreNotEqual(p.Run(new LabConfig { Seed = 3 }).ResultHash, p.Run(new LabConfig { Seed = 4 }).ResultHash);
        }

        [Test]
        public void ForkedStreams_OnePartsSizeDoesNotChangeAnother()
        {
            // test 13: every part of an experiment has its own keyed stream; changing the size of one part (L7's main
            // replicator series, or an agent run) leaves every other part's numbers unchanged
            var game = new ContestGame(L01HawkDove.Kinds(), false, ContestRules.Lab);
            var a = new AgentRunSpec { N = 100, K = 2, InitialCounts = new[] { 50, 50 }, Rounds = 2, Generations = 5, W0X1000 = 500000 };
            var big = new AgentRunSpec { N = 400, K = 2, InitialCounts = new[] { 200, 200 }, Rounds = 2, Generations = 5, W0X1000 = 500000 };
            var r1 = AgentEngine.Run(a, game, LabRandom.Root(1, "T", "part-a"));
            var other = AgentEngine.Run(big, game, LabRandom.Root(1, "T", "part-b"));
            var r1again = AgentEngine.Run(a, game, LabRandom.Root(1, "T", "part-a"));
            for (int g = 0; g < r1.Shares.Length; g++) CollectionAssert.AreEqual(r1.Shares[g], r1again.Shares[g]);
            Assert.AreNotEqual(LabRandom.Root(1, "L1", "agents").NextULong(), LabRandom.Root(1, "L1", "agents-n100").NextULong());
            var e = new L07SuckerCheatGrudger();
            var x = e.Run(new LabConfig { Seed = 1 });
            var y = e.Run(new LabConfig { Seed = 1, Generations = 400 });
            Assert.AreNotEqual(x.ResultHash, y.ResultHash, "the main series changed");
            var xb = x.Checks.Where(c => c.Name.Contains("Grudger 0.0")).Select(c => c.MeasuredMicro).ToArray();
            var yb = y.Checks.Where(c => c.Name.Contains("Grudger 0.0")).Select(c => c.MeasuredMicro).ToArray();
            Assert.AreEqual(2, xb.Length);
            CollectionAssert.AreEqual(xb, yb, "the bistability part did not change");
        }

        // ---- M2.20 / test 24: honest labels ----

        [Test]
        public void EveryExperiment_HasAStatus_AndNonExactNeverClaimsAReproduction()
        {
            var all = LabRegistry.All();
            var ids = all.Select(e => e.Id).ToList();
            CollectionAssert.AreEqual(new[] { "L1", "L2", "L3", "L4", "L5", "L6", "L7", "L8", "L9", "L9b", "L10", "L11", "L12", "L13" }, ids);
            foreach (var e in all)
            {
                Assert.IsTrue(Enum.IsDefined(typeof(ReproStatus), e.Status), e.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.StatusText), e.Id);
                if (e.Status != ReproStatus.Exact)
                    foreach (var text in new[] { e.Title, e.StatusText, e.ExpectedSummary })
                        StringAssert.DoesNotMatch("(?i)reproduc", text, e.Id + ": a non-Exact experiment must not call itself a reproduction");
            }
            Assert.AreEqual(ReproStatus.Exact, LabRegistry.Find("L1").Status);
            Assert.AreEqual(ReproStatus.ExactPayoffsCorrectedOrDifferent, LabRegistry.Find("L6").Status);
            foreach (var id in new[] { "L3", "L5", "L7", "L10", "L12", "L13" }) Assert.AreEqual(ReproStatus.Invented, LabRegistry.Find(id).Status, id);
        }

        // ---- M2.16 / test 14: source scan ----

        private static string GodRoot([CallerFilePath] string here = "")
        {
            string p = here;
            if (!Path.IsPathRooted(p)) p = Path.Combine(Directory.GetCurrentDirectory(), p);
            var dir = new DirectoryInfo(Path.GetDirectoryName(p));
            while (dir != null && !(dir.Name == "God" && dir.Parent != null && dir.Parent.Name == "Assets")) dir = dir.Parent;
            Assert.IsNotNull(dir, "cannot find Assets/God from " + here);
            return dir.FullName;
        }

        private static string StripComments(string src)
        {
            src = Regex.Replace(src, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            src = Regex.Replace(src, @"//[^\n]*", " ");
            src = Regex.Replace(src, "\"(?:[^\"\\\\]|\\\\.)*\"", "\"\"");
            return src;
        }

        [Test]
        public void SourceScan_LabIsPureIntegerCode()
        {
            string lab = Path.Combine(GodRoot(), "Sim", "Lab");
            var files = Directory.GetFiles(lab, "*.cs", SearchOption.AllDirectories);
            Assert.Greater(files.Length, 10);
            var bad = new List<string>();
            foreach (var f in files)
            {
                string code = StripComments(File.ReadAllText(f));
                foreach (var pat in new[] { @"\bUnityEngine\b", @"\bSystem\.Random\b", @"\bnew\s+Random\b", @"\bfloat\b", @"\bdouble\b", @"\bdecimal\b", @"\bMathF?\.(Exp|Log|Pow|Sqrt|Sin|Cos)\b", @"\bDateTime\b", @"\bStopwatch\b" })
                    if (Regex.IsMatch(code, pat)) bad.Add(Path.GetFileName(f) + ": " + pat);
            }
            Assert.IsEmpty(bad);
            // and by reflection: no float or double in any lab type's fields or signatures
            foreach (var t in typeof(Replicator).Assembly.GetTypes().Where(t => t.Namespace == "God.Sim.Lab"))
            {
                foreach (var fi in t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    Assert.IsFalse(fi.FieldType == typeof(float) || fi.FieldType == typeof(double), t.Name + "." + fi.Name);
                foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    Assert.IsFalse(m.ReturnType == typeof(float) || m.ReturnType == typeof(double), t.Name + "." + m.Name);
                    foreach (var p in m.GetParameters()) Assert.IsFalse(p.ParameterType == typeof(float) || p.ParameterType == typeof(double), t.Name + "." + m.Name);
                }
            }
        }

        [Test]
        public void SourceScan_NoTargetFrequencyOutsideTheLab()
        {
            // GAME_THEORY 10 test 3: no reference to 7/12, 5/6 or 5/8 outside Sim/Lab and tests (no forcing)
            string root = GodRoot();
            var bad = new List<string>();
            var rx = new Regex(@"(?<![\d/.])(7\s*/\s*12|5\s*/\s*6|5\s*/\s*8)(?![\d/])|0\.58333|0\.83333|\b0\.625\b");
            foreach (var f in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = f.Substring(root.Length).Replace('\\', '/');
                if (rel.StartsWith("/Sim/Lab/") || rel.StartsWith("/Tests/")) continue;
                string code = StripComments(File.ReadAllText(f));
                if (rx.IsMatch(code)) bad.Add(rel + ": " + rx.Match(code).Value);
            }
            Assert.IsEmpty(bad);
        }
    }
}
