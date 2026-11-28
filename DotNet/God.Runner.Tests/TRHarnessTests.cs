using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using God.Sim;
using God.Sim.Unified;
using God.Train;
using God.Train.Artifact;
using God.Train.Curriculum;
using God.Train.Env;
using God.Train.Eval;
using God.Train.Honesty;
using God.Train.Learn;
using God.Train.Net;
using God.Train.Report;
using God.Train.Teacher;
using NUnit.Framework;

namespace God.Runner.Tests
{
    using Checkpoint = God.Train.Learn.Checkpoint;
    using TargetKind = God.Sim.Unified.TargetKind;

    /// <summary>
    /// UN6t-a acceptance (UN6ta-T1 ... T9): the founder training harness on the <b>stub world</b>. Everything measured here is a
    /// HARNESS SELF-TEST, never a training result.
    /// </summary>
    [TestFixture]
    public class UN6ta_TRHarnessTests
    {
        private static string Root()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !(Directory.Exists(Path.Combine(d.FullName, "Assets")) && Directory.Exists(Path.Combine(d.FullName, "DotNet")))) d = d.Parent;
            Assert.That(d, Is.Not.Null, "repository root");
            return d.FullName;
        }

        private static Thresholds T => Thresholds.Load(Path.Combine(Root(), "docs", "unification", "TRAINING_THRESHOLDS.md"));
        private static Stage S2 => CurriculumData.Load("S2");

        private static int Cli(out string o, out string e, params string[] args)
        {
            var ow = new StringWriter(); var ew = new StringWriter();
            int c = God.Train.Cli.Run(args, ow, ew);
            o = ow.ToString(); e = ew.ToString();
            return c;
        }

        // ---------------- T1 ----------------

        [Test]
        public void UN6ta_T1_Thresholds_CommittedFirst()
        {
            var t = T;
            Assert.That(t.Hash, Has.Length.EqualTo(64));
            Assert.That(t.Gate, Is.EqualTo(new[] { "T1", "T2", "T3", "T4", "T6" }), "R86 gate");
            Assert.That(t.Measure("T1").Value, Is.EqualTo(0.90)); Assert.That(t.Measure("T1").MinAboveUntrained, Is.EqualTo(0.40)); Assert.That(t.Measure("T1").Days, Is.EqualTo(10)); Assert.That(t.Measure("T1").Population, Is.EqualTo(12));
            Assert.That(t.Measure("T2").Value, Is.EqualTo(0.10)); Assert.That(t.Measure("T2").Op, Is.EqualTo("<="));
            Assert.That((t.Measure("T3").Count, t.Measure("T3").Of, t.Measure("T3").Days), Is.EqualTo((14, 20, 5)));
            Assert.That((t.Measure("T4").Count, t.Measure("T4").Of, t.Measure("T4").Days), Is.EqualTo((10, 20, 8)));
            Assert.That(t.Measure("T5").Value, Is.EqualTo(0.50));
            Assert.That((t.Measure("T6").Value, t.Measure("T6").Days, t.Measure("T6").Population), Is.EqualTo((0.75, 20, 72)));
            Assert.That((t.Measure("T7").Count, t.Measure("T7").Of, t.Measure("T7").Days), Is.EqualTo((10, 20, 10)));
            Assert.That(t.HeldOutSeeds, Is.EqualTo(new long[] { 900001, 900020 }));
            Assert.That(t.Full72Seeds, Is.EqualTo(new long[] { 900001, 900003 }));
            Assert.That(t.RefuseSeedsFrom, Is.EqualTo(SeedPolicy.RefuseFrom));
            Assert.That(t.Arms, Is.EqualTo(new[] { "untrained", "frozen", "trained" }));

            // the T1-T7 table rows of design D9.2 c appear verbatim in the thresholds file
            var design = File.ReadAllLines(Path.Combine(Root(), "docs", "unification", "UNIFICATION_DESIGN.md"));
            var file = File.ReadAllText(Path.Combine(Root(), "docs", "unification", "TRAINING_THRESHOLDS.md"));
            int rows = 0;
            foreach (var line in design)
                if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\| T[1-7] [a-z]")) { rows++; Assert.That(file, Does.Contain(line.TrimEnd()), "verbatim row: " + line); }
            Assert.That(rows, Is.EqualTo(7));

            // the hash is recorded in DEV_LOG
            var log = File.ReadAllText(Path.Combine(Root(), "docs", "stage4", "DEV_LOG.md"));
            Assert.That(log, Does.Contain(t.Hash), "thresholds hash recorded in DEV_LOG");

            // eval refuses without or with a wrong hash
            Assert.Throws<ThresholdsHashMismatchException>(() => Evaluator.Authorise(t, null));
            Assert.Throws<ThresholdsHashMismatchException>(() => Evaluator.Authorise(t, new string('0', 64)));
            var dir = TestUtil.TempDir("un6ta-t1");
            try
            {
                Assert.That(Cli(out _, out var e1, "eval", "--stub", "--out", dir), Is.EqualTo(God.Train.Cli.Refused), e1);
                Assert.That(Cli(out _, out var e2, "eval", "--stub", "--thresholds-hash", "abc", "--out", dir), Is.EqualTo(God.Train.Cli.Refused), e2);
                // a result carries the hash
                var res = Evaluator.Run(t, t.Hash, Setup(new[] { 990001L }, ("untrained", "untrained", Untrained())));
                Assert.That(res.ThresholdsHash, Is.EqualTo(t.Hash));
                Assert.That(EvalResults.FromJson(res.ToJson()).ThresholdsHash, Is.EqualTo(t.Hash));
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void UN6ta_Thresholds_HashRuleIgnoresLineEnds()
        {
            var lf = System.Text.Encoding.UTF8.GetBytes("a\nb\n");
            var crlf = System.Text.Encoding.UTF8.GetBytes("a\r\nb\r\n");
            Assert.That(Thresholds.HashOf(crlf), Is.EqualTo(Thresholds.HashOf(lf)));
        }

        [Test]
        public void UN6ta_Curriculum_CriteriaMatchThresholdsFile()
        {
            var t = T;
            foreach (var id in CurriculumData.Order)
            {
                var st = CurriculumData.Load(id); var spec = t.Stage(id);
                Assert.That((st.NeedMask, st.Population, st.Days, st.ValidationSeeds), Is.EqualTo((spec.NeedMask, spec.Population, spec.Days, spec.Seeds)), id);
                Assert.That(st.Criteria.Select(c => (c.Kind, c.Op, c.Value)), Is.EqualTo(spec.Criteria.Select(c => (c.Kind, c.Op, c.Value))), id);
                Assert.That(st.ValidationFrom + st.ValidationSeeds, Is.LessThanOrEqualTo(SeedPolicy.RefuseFrom), id);
            }
            Assert.That(CurriculumData.Load("S8").NeedMask, Is.EqualTo(0xFFF));
        }

        /// <summary>Risk "hidden oracle through the observation": the trainer reads only Obs v1, whose fields are listed here.</summary>
        [Test]
        public void UN6ta_Observation_FieldsListed_NoOracle()
        {
            var names = ObsSchema.Fields.Select(f => f.Name).ToArray();
            Assert.That(names, Is.EqualTo(new[] { "facing", "control", "progress", "last_result", "asleep", "occupying", "equipped", "needs", "health", "injury", "age_band", "traits",
                "carry", "cells", "entities", "gestures", "places", "people", "claims", "know_designs", "know_recipes", "know_interactions", "divine" }));
            foreach (var n in names) foreach (var bad in new[] { "owner", "true", "path", "oracle", "title" }) Assert.That(n, Does.Not.Contain(bad));
            Assert.That(ObsSchema.Length, Is.EqualTo(284)); Assert.That(ActSchema.Length, Is.EqualTo(166));
            Assert.That(NetShape.Default.In, Is.EqualTo(ObsSchema.Length)); Assert.That(NetShape.Default.Act, Is.EqualTo(ActSchema.Length));
        }

        /// <summary>After the UN1 merge: the real body reading (needs, health) of a unified game, and the probe names what is still missing.</summary>
        [Test]
        public void UN6ta_UnifiedWorld_BodyReading_UN1()
        {
            var sim = UnifiedWorld.Create(5, 0xFFF);
            Assert.That(sim.State.Unification, Is.Not.Null);
            int npc = sim.State.Unification.People[0].Npc;
            var before = new BodyReading(); var after = new BodyReading();
            UnifiedWorld.ReadBody(sim.State, npc, before);
            sim.Step(sim.State.Setup.TicksPerDay / 2);
            UnifiedWorld.ReadBody(sim.State, npc, after);
            Assert.That(after.Alive, Is.True); Assert.That(after.Health, Is.GreaterThan(0));
            Assert.That(after.Needs[(int)NeedChannel.Thirst], Is.GreaterThan(before.Needs[(int)NeedChannel.Thirst]), "UN1 thirst rises");
            Assert.That(Returns.Tick(before, after, 0xFFF, 0), Is.LessThan(0));
            var masked = UnifiedWorld.Create(5, 0x01F); var r = new BodyReading();
            masked.Step(100);
            UnifiedWorld.ReadBody(masked.State, masked.State.Unification.People[0].Npc, r);
            for (int c = NeedChannels.FirstExtension; c < NeedChannels.Count; c++) Assert.That(r.Needs[c], Is.EqualTo(0), "inactive channel " + c);
            Assert.That(UnifiedWorld.Unavailable(1), Is.Null, "slice 1: every training seam answers on the merged sim (Obs v1, Motor.Legal, ControlOf, Submit)");
        }

        // ---------------- T2 ----------------

        [Test]
        public void UN6ta_T2_Trainer_RefusesHeldOut()
        {
            var cfg = new StubConfig();
            Assert.Throws<HeldOutRefusedException>(() => new StubWorld(cfg, 900001, Layout.Train, null));
            Assert.Throws<HeldOutRefusedException>(() => new StubWorld(cfg, 900020, Layout.Train, null));
            Assert.Throws<HeldOutRefusedException>(() => new StubWorld(cfg, 990001, Layout.Train, null));
            foreach (var l in Layouts.HeldOut) Assert.Throws<HeldOutRefusedException>(() => new StubWorld(cfg, 5, l, null), l.ToString());
            Assert.DoesNotThrow(() => new StubWorld(cfg, 899999, Layout.Train, null));
            Assert.Throws<HeldOutRefusedException>(() => Pipeline.Demos(S2, new long[] { 1, 900003 }, 1, 10));
            var start = Checkpoint.Untrained(NetShape.Default, 1, "stub");
            Assert.Throws<HeldOutRefusedException>(() => EvolutionStrategies.Train(start, s => Pipeline.Stub(S2, s), new EsConfig { Seeds = new long[] { 900010 } }, 1));
            Assert.Throws<HeldOutRefusedException>(() => UnifiedWorld.Unavailable(900001));
            var dir = TestUtil.TempDir("un6ta-t2");
            try
            {
                foreach (var cmd in new[] { "demo", "bc", "es" })
                    Assert.That(Cli(out _, out var e, cmd, "--stub", "--stage", "S2", "--seeds", "900001-900002", "--out", dir), Is.EqualTo(God.Train.Cli.Refused), cmd + ": " + e);
                // the stub world never runs the real held-out seeds, even in eval
                var t = T;
                Assert.Throws<HeldOutRefusedException>(() => Evaluator.Run(t, t.Hash, Setup(new[] { 900001L }, ("untrained", "untrained", Untrained()))));
            }
            finally { TestUtil.Cleanup(dir); }
        }

        // ---------------- T3 ----------------

        [Test]
        public void UN6ta_T3_Teacher_NeverEvaluated()
        {
            var t = T;
            var setup = Setup(new[] { 990001L, 990002L }, ("untrained", "untrained", Untrained()));
            setup.Arms.Add(new ArmSpec { Name = "trained", Brain = "trained", Actor = () => new StubTeacher() });
            var ex = Assert.Throws<EvaluationAbortedException>(() => Evaluator.Run(t, t.Hash, setup));
            Assert.That(ex.Results.Invalid, Is.True);
            Assert.That(ex.Results.InvalidReason, Does.Contain("Teacher"));
            Assert.That(ex.Results.Verdict.Tr01Passed, Is.False);
            Assert.That(ReportWriter.Write(ex.Results, t), Does.Contain("INVALID"));
            // rollout-level guard
            var w = new StubWorld(new StubConfig(), 3, Layout.Train, null);
            Assert.Throws<EvaluationInvalidException>(() => Rollout.Run(w, new StubTeacher(), 3, 10, evaluation: true));
            // static: no teacher type in God.Sim, nothing under Assets/God/Sim references the trainer
            Assert.That(typeof(GodSim).Assembly.GetTypes().Where(x => x.Name.IndexOf("Teacher", StringComparison.OrdinalIgnoreCase) >= 0).Select(x => x.FullName), Is.Empty);
            Assert.That(typeof(StubTeacher).Assembly.GetName().Name, Is.EqualTo("god-train"));
            var bad = Directory.GetFiles(Path.Combine(Root(), "Assets", "God", "Sim"), "*.cs", SearchOption.AllDirectories).Where(f => System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(f), @"using\s+God\.Train|God\.Train\.[A-Z]")).ToList();
            Assert.That(bad, Is.Empty, "Assets/God/Sim references God.Train");
            Assert.That(typeof(GodSim).Assembly.GetReferencedAssemblies().Select(a => a.Name), Has.None.EqualTo("god-train"));
        }

        // ---------------- T4 ----------------

        [Test]
        public void UN6ta_T4_Quantisation_Parity()
        {
            var shape = NetShape.Default;
            Assert.That(shape.MacPerDecision, Is.LessThanOrEqualTo(NetShape.MacBudget));
            // float master weights (initialised, then a short behaviour cloning so the weights are not just the initial ones)
            var demos = Pipeline.Demos(S2, SeedPolicy.ParseSeeds("11-20"), 4, 600);
            var ck = BehaviourCloning.Train(Checkpoint.Untrained(shape, 5, "stub"), demos, new BcConfig { Epochs = 2, Workers = 2 }, 2);
            var direct = FloatNet.ToQNet(shape, ck.P);
            // -> int16 artifact -> parsed back
            var text = ArtifactWriter.Write(new ArtifactHeader { Status = "SELF-TEST", ThresholdsHash = T.Hash }, direct);
            var h = ArtifactWriter.ParseHeader(text, out var w);
            Assert.That(ArtifactWriter.Verify(h, w, null), Is.Null);
            var fromArtifact = new QNet(NetShape.Parse(h.NetShape), w);
            // an external optimiser's export: raw little-endian int16 bytes
            var external = new QNet(shape, QNet.FromBytes(direct.WeightBytes()));
            // 1,000 recorded observation vectors
            var obs = demos.SelectMany(s => s.Samples).Take(1000).ToList();
            Assert.That(obs.Count, Is.EqualTo(1000));
            var nets = new[] { direct, fromArtifact, external };
            var hs = nets.Select(_ => new short[BrainState.RecurrentSize]).ToArray();
            var scs = nets.Select(n => new QScratch(n.Shape)).ToArray();
            var rngs = nets.Select(_ => new Rng(99)).ToArray();
            foreach (var x in obs)
            {
                var choices = new ActionChoice[nets.Length];
                var logits = new List<int[]>();
                for (int i = 0; i < nets.Length; i++)
                {
                    choices[i] = nets[i].Act(x.Obs, hs[i], in x.Mask, rngs[i], scs[i]);
                    var all = new List<int>();
                    for (int hd = 0; hd < Heads.Count; hd++) { nets[i].Logits(hd, hs[i], scs[i]); all.AddRange(scs[i].Logits.Take(Heads.Size[hd])); }
                    logits.Add(all.ToArray());
                }
                for (int i = 1; i < nets.Length; i++)
                {
                    Assert.That(hs[i], Is.EqualTo(hs[0]));
                    Assert.That(logits[i], Is.EqualTo(logits[0]));
                    Assert.That(choices[i], Is.EqualTo(choices[0]));
                }
            }
            // the UN0 IPrimitivePolicy path gives the same choice as the network (UN6's inference re-runs this at UN6t-b)
            IPrimitivePolicy pol = new QNetPolicy(direct, (in Percept p, in ActionChoice a) => new PrimitiveControl { Control = (Prim)a.Control, Actor = p.Actor });
            var b = new BrainState(); var rng = new Rng(3); var h2 = new short[BrainState.RecurrentSize]; var sc2 = new QScratch(shape); var rng2 = new Rng(3);
            foreach (var x in obs.Take(200))
            {
                var pc = new Percept { Actor = 7 };
                var c = pol.Decide(in pc, x.Obs, ref b, in x.Mask, ref rng);
                var a2 = direct.Act(x.Obs, h2, in x.Mask, rng2, sc2);
                Assert.That((int)c.Control, Is.EqualTo(a2.Control));
                Assert.That(b.Recurrent, Is.EqualTo(h2));
            }
            // the exp table is integer-built and fixed
            Assert.That(QNet.Exp[0], Is.EqualTo(65536)); Assert.That(QNet.Exp[1], Is.EqualTo(64496));
        }

        // ---------------- T5 ----------------

        [Test]
        public void UN6ta_T5_Train_Deterministic_AcrossWorkers()
        {
            var shape = NetShape.Default;
            var d1 = Pipeline.Demos(S2, new long[] { 21, 22, 23 }, 1, 300);
            var d4 = Pipeline.Demos(S2, new long[] { 21, 22, 23 }, 4, 300);
            Assert.That(Bytes(d4), Is.EqualTo(Bytes(d1)), "demonstrations");
            var start = Checkpoint.Untrained(shape, 2, "stub");
            var b1 = BehaviourCloning.Train(start, d1, new BcConfig { Epochs = 3, Workers = 1 }, 3);
            var b4 = BehaviourCloning.Train(start, d1, new BcConfig { Epochs = 3, Workers = 4 }, 3);
            Assert.That(b4.ToBytes(), Is.EqualTo(b1.ToBytes()), "bc weights, workers 1 vs 4");
            var es = new EsConfig { Pairs = 3, EpisodesPerCandidate = 1, Ticks = 150, Seeds = new long[] { 31, 32, 33 } };
            es.Workers = 1; var e1 = EvolutionStrategies.Train(b1, s => Pipeline.Stub(S2, s), es, 2);
            es.Workers = 4; var e4 = EvolutionStrategies.Train(b1, s => Pipeline.Stub(S2, s), es, 2);
            Assert.That(e4.ToBytes(), Is.EqualTo(e1.ToBytes()), "es weights, workers 1 vs 4");
        }

        [Test]
        public void UN6ta_T5_Train_Resume_Exact()
        {
            var shape = NetShape.Default;
            var d = Pipeline.Demos(S2, new long[] { 41, 42 }, 2, 300);
            var start = Checkpoint.Untrained(shape, 3, "stub");
            var cfg = new BcConfig { Epochs = 4, Workers = 2 };
            var full = BehaviourCloning.Train(start, d, cfg, 4);
            var half = Checkpoint.FromBytes(BehaviourCloning.Train(start, d, cfg, 2).ToBytes());
            var resumed = BehaviourCloning.Train(half, d, cfg, 4);
            Assert.That(resumed.ToBytes(), Is.EqualTo(full.ToBytes()), "bc resume");
            var es = new EsConfig { Pairs = 2, EpisodesPerCandidate = 1, Ticks = 120, Seeds = new long[] { 51, 52, 53 }, Workers = 2 };
            var eFull = EvolutionStrategies.Train(full, s => Pipeline.Stub(S2, s), es, 3);
            var eHalf = Checkpoint.FromBytes(EvolutionStrategies.Train(full, s => Pipeline.Stub(S2, s), es, 1).ToBytes());
            var eRes = EvolutionStrategies.Train(eHalf, s => Pipeline.Stub(S2, s), es, 3);
            Assert.That(eRes.ToBytes(), Is.EqualTo(eFull.ToBytes()), "es resume");
        }

        private static byte[] Bytes(List<Sequence> d)
        {
            var f = Path.Combine(Path.GetTempPath(), "un6ta-demo-" + Guid.NewGuid().ToString("N") + ".bin");
            try { DemoFile.Write(f, d); var b = File.ReadAllBytes(f); Assert.That(DemoFile.Read(f).Count, Is.EqualTo(d.Count)); return b; }
            finally { File.Delete(f); }
        }

        // ---------------- T6 ----------------

        [Test]
        public void UN6ta_T6_Harness_SelfTest_StubWorld()
        {
            var t = T; var st = S2; var shape = NetShape.Default;
            int workers = Math.Max(2, Math.Min(8, Environment.ProcessorCount / 2));
            var demos = Pipeline.Demos(st, SeedPolicy.ParseSeeds("1-32"), workers);
            Assert.That(demos.All(s => s.Origin == ControlOrigin.Teacher), "demonstrations are labelled Teacher");
            var untrained = Checkpoint.Untrained(shape, t.UntrainedInitSeed, "stub");
            var frozen = BehaviourCloning.Train(untrained, demos, new BcConfig { Epochs = 100, Workers = workers }, 100);
            double agree = BehaviourCloning.Agreement(frozen.ToQNet(), demos);
            TestContext.Progress.WriteLine($"HARNESS SELF-TEST, not a result: stub BC agreement {agree:0.0000}");
            Assert.That(agree, Is.GreaterThanOrEqualTo(0.95), "behaviour cloning action agreement (integer network)");
            var trained = EvolutionStrategies.Train(frozen, s => Pipeline.Stub(st, s), new EsConfig { Pairs = 8, EpisodesPerCandidate = 2, Ticks = Pipeline.Ticks(st), Seeds = SeedPolicy.ParseSeeds("1-64"), Workers = workers }, 6);
            var setup = Setup(SeedPolicy.ParseSeeds("990001-990008"),
                ("untrained", "untrained", untrained.ToQNet()), ("frozen", "frozen", frozen.ToQNet()), ("trained", "trained", trained.ToQNet()));
            setup.NeedMask = st.NeedMask; setup.Workers = workers;
            var res = Evaluator.Run(t, t.Hash, setup);
            long R(string arm) => res.Arm(arm).PerSeed.Sum(x => x.Return);
            TestContext.Progress.WriteLine($"HARNESS SELF-TEST, not a result: returns untrained {R("untrained")}, frozen {R("frozen")}, trained {R("trained")}");
            Assert.That(R("trained"), Is.GreaterThan(R("untrained")), "ES-trained beats untrained on episode return");
            Assert.That(res.SelfTest, Is.True); Assert.That(res.Label, Is.EqualTo("HARNESS SELF-TEST, not a result"));
            Assert.That(res.Verdict.Tr01Passed, Is.False, "a stub world never passes TR-01");
            var md = ReportWriter.Write(res, t);
            Assert.That(md, Does.StartWith("# HARNESS SELF-TEST, not a result"));
            Assert.That(md, Does.Not.Contain(Verdict.Passed));
            Assert.That(res.Arms.SelectMany(a => a.PerSeed).Sum(s => s.TeacherSubmissions), Is.EqualTo(0));
        }

        // ---------------- T7 ----------------

        [Test]
        public void UN6ta_T7_Report_FailureIsReported()
        {
            var t = T;
            var r = Fabricated(t, trainedBrain: "trained", t1: 0.55, t3: 9);
            var md = ReportWriter.Write(r, t);
            Assert.That(md, Does.Contain("TR-01: not passed; UA-learned: not green"));
            Assert.That(md, Does.Contain("T1")); Assert.That(md, Does.Contain("T3"));
            var v = Verdict.Compute(r, t);
            Assert.That(v.FailedGate, Has.Some.StartsWith("T1")); Assert.That(v.FailedGate, Has.Some.StartsWith("T3"));
            Assert.That(v.FailedGate, Has.None.StartsWith("T2"));
            Assert.That(md, Does.Contain("Brain.Kind = 2"));
            Assert.That(md, Does.Not.Contain(Verdict.Passed));
            // a stored "passed" verdict in the JSON is ignored: the report recomputes it
            r.Verdict = new VerdictResult { Tr01Passed = true, UaLearnedGreen = true, Headline = Verdict.Passed };
            Assert.That(ReportWriter.Write(EvalResults.FromJson(r.ToJson()), t), Does.Not.Contain(Verdict.Passed));
            // a fallback brain with every measure met is still never counted as trained
            var fb = Fabricated(t, trainedBrain: "fallback", t1: 0.99, t3: 20);
            Assert.That(Verdict.Compute(fb, t).Tr01Passed, Is.False);
            Assert.That(ReportWriter.Write(fb, t), Does.Contain("never counted as trained"));
            // a T5/T7 miss alone does not fail the gate, and is reported
            var ok = Fabricated(t, trainedBrain: "trained", t1: 0.99, t3: 20);
            var vo = Verdict.Compute(ok, t);
            Assert.That(vo.Tr01Passed, Is.True, string.Join("; ", vo.FailedGate));
            Assert.That(vo.ReportedNotGate, Has.Some.StartsWith("T7: not met"));
            Assert.That(ReportWriter.Write(ok, t), Does.Contain("T7: not met"));
        }

        private static EvalResults Fabricated(Thresholds t, string trainedBrain, double t1, int t3)
        {
            var r = new EvalResults { ThresholdsHash = t.Hash, ThresholdsVersion = t.Version, World = "unified", SelfTest = false, Label = "fabricated", Gate = new List<string>(t.Gate) };
            for (long s = 900001; s <= 900020; s++) r.Seeds.Add(s);
            ArmResult Arm(string name, string brain, double v1, int c3) => new ArmResult
            {
                Name = name, Brain = brain,
                Measures = new List<MeasureResult>
                {
                    new MeasureResult { Id = "T1", Evaluable = true, Value = v1, Met = v1 >= 0.90, Gate = true },
                    new MeasureResult { Id = "T2", Evaluable = true, Value = 0.05, Met = true, Gate = true },
                    new MeasureResult { Id = "T3", Evaluable = true, Count = c3, Of = 20, Met = c3 >= 14, Gate = true },
                    new MeasureResult { Id = "T4", Evaluable = true, Count = 12, Of = 20, Met = true, Gate = true },
                    new MeasureResult { Id = "T5", Evaluable = true, Value = 0.6, Met = true },
                    new MeasureResult { Id = "T6", Evaluable = true, Value = 0.8, Met = true, Gate = true },
                    new MeasureResult { Id = "T7", Evaluable = true, Count = 3, Of = 20, Met = false },
                },
            };
            r.Arms.Add(Arm("untrained", "untrained", 0.1, 0));
            r.Arms.Add(Arm("trained", trainedBrain, t1, t3));
            return r;
        }

        // ---------------- T8 ----------------

        [Test]
        public void UN6ta_T8_Artifact_Reproducible()
        {
            var shape = NetShape.Default;
            Checkpoint Make()
            {
                var d = Pipeline.Demos(S2, new long[] { 61, 62 }, 2, 300);
                return BehaviourCloning.Train(Checkpoint.Untrained(shape, 4, "stub"), d, new BcConfig { Epochs = 2, Workers = 2 }, 2);
            }
            ArtifactHeader Header(Checkpoint c) => new ArtifactHeader
            {
                Version = 3, Status = "SELF-TEST", CatalogueHash = ArtifactWriter.CurrentCatalogueHash(), TuningHash = "stub-world", CurriculumId = "founder-v1",
                StagesTrained = "S2", StagesNotTrained = "S1,S3,S4,S5,S6,S7,S8", TrainingSeeds = c.SeedsText, TrainerCommit = "test", ThresholdsHash = T.Hash,
                TrainingMetricsJson = "{\"x\":\"y\"}",
            };
            var c1 = Make(); var c2 = Make();
            var a1 = ArtifactWriter.Write(Header(c1), c1.ToQNet());
            var a2 = ArtifactWriter.Write(Header(c2), c2.ToQNet());
            Assert.That(System.Text.Encoding.UTF8.GetBytes(a2), Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(a1)), "byte-identical artifact from the same inputs");
            Assert.That(a1, Does.Contain("public static class FounderPolicyV3"));
            Assert.That(a1, Does.Not.Contain("\r"));
            foreach (var f in ArtifactHeader.StringFields.Concat(ArtifactHeader.IntFields)) Assert.That(a1, Does.Contain(" " + f + " = "), f);
            var h = ArtifactWriter.ParseHeader(a1, out var w);
            Assert.That(h.ThresholdsHash, Is.EqualTo(T.Hash)); Assert.That(h.TrainingMetricsJson, Is.EqualTo("{\"x\":\"y\"}"));
            Assert.That(h.ObsSchemaHash, Is.EqualTo(ObsSchema.SchemaHash)); Assert.That(h.ActSchemaHash, Is.EqualTo(ActSchema.SchemaHash));
            Assert.That(ArtifactWriter.Verify(h, w, ArtifactWriter.CurrentCatalogueHash()), Is.Null);
            h.ObsSchemaHash = new string('0', 64);
            Assert.That(ArtifactWriter.Verify(h, w, null), Is.EqualTo("Obs schema changed"));
            var h2 = ArtifactWriter.ParseHeader(a1, out _); h2.ActSchemaHash = "x";
            Assert.That(ArtifactWriter.Verify(h2, w, null), Is.EqualTo("Act schema changed"));
            var h3 = ArtifactWriter.ParseHeader(a1, out _);
            Assert.That(ArtifactWriter.Verify(h3, w, "other-catalogue"), Is.EqualTo("catalogue changed"));
            w[0]++;
            Assert.That(ArtifactWriter.Verify(h3, w, null), Is.EqualTo("weights hash mismatch"));
            // a stub checkpoint can only give a SELF-TEST artifact
            var dir = TestUtil.TempDir("un6ta-t8");
            try
            {
                var ck = Path.Combine(dir, "c.ckpt"); c1.Save(ck);
                Assert.That(Cli(out _, out _, "artifact", "--from", ck, "--status", "TRAINED", "--out", Path.Combine(dir, "a.g.cs")), Is.EqualTo(God.Train.Cli.Refused));
                Assert.That(Cli(out _, out var e, "artifact", "--from", ck, "--out", Path.Combine(dir, "a.g.cs")), Is.EqualTo(God.Train.Cli.Ok), e);
                Assert.That(File.ReadAllText(Path.Combine(dir, "a.g.cs")), Does.Contain("public const string Status = @\"SELF-TEST\";"));
            }
            finally { TestUtil.Cleanup(dir); }
        }

        // ---------------- T9 ----------------

        [Test]
        public void UN6ta_T9_Returns_NoGoodsNoTitle()
        {
            // static: the return function's source reads no goods, holder, building, site or title
            var src = File.ReadAllText(Path.Combine(Root(), "DotNet", "God.Train", "Env", "Returns.cs"));
            var code = string.Join("\n", src.Split('\n').Where(l => !l.TrimStart().StartsWith("///") && !l.TrimStart().StartsWith("//")));
            foreach (var word in new[] { "Lot", "Holder", "Building", "Site", "Owner", "Goods", "Ware", "Carry", "Title", "Stock", "Facts", "StubWorld", "GodState", "IWorld" })
                Assert.That(code, Does.Not.Contain(word), word);
            Assert.That(typeof(BodyReading).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Select(f => f.Name).OrderBy(x => x), Is.EqualTo(new[] { "Alive", "Health", "Needs" }));
            foreach (var m in typeof(Returns).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                foreach (var p in m.GetParameters()) Assert.That(new[] { typeof(BodyReading), typeof(int), typeof(long) }, Does.Contain(p.ParameterType), m.Name + "." + p.Name);
            // run time: two bodies that differ only in what they carry give the same return
            bool compared = false;
            for (long seed = 71; seed < 400 && !compared; seed++)
            {
                var w1 = new StubWorld(new StubConfig { Persons = 1 }, seed, Layout.Train, null);
                var w2 = new StubWorld(new StubConfig { Persons = 1 }, seed, Layout.Train, null);
                if (!StartForage(w2)) continue;
                long r1 = 0, r2 = 0;
                var a = new BodyReading(); var b = new BodyReading(); var a2 = new BodyReading(); var b2 = new BodyReading();
                for (int k = 0; k < 12; k++)
                {
                    w1.ReadBody(1, a); w2.ReadBody(1, b);
                    w1.Step(); w2.Step();
                    w1.ReadBody(1, a2); w2.ReadBody(1, b2);
                    r1 += Returns.Tick(a, a2, 0x033, 12 - k); r2 += Returns.Tick(b, b2, 0x033, 12 - k);
                }
                Assert.That(w2.StateHash(), Is.Not.EqualTo(w1.StateHash()), "the second person now carries a food lot");
                Assert.That(r2, Is.EqualTo(r1), "carrying goods changes no return");
                compared = true;
            }
            Assert.That(compared, Is.True);
            // dying is never better than living at full pressure
            var alive = new BodyReading { Alive = true, Health = 1 }; var dead = new BodyReading { Alive = false };
            var full = new BodyReading { Alive = true, Health = 1 }; for (int c = 0; c < 12; c++) { alive.Needs[c] = 1000; full.Needs[c] = 1000; }
            long liveRest = 0; for (int k = 0; k < 50; k++) liveRest += Returns.Tick(full, full, 0xFFF, 0);
            Assert.That(Returns.Tick(alive, dead, 0xFFF, 49), Is.LessThanOrEqualTo(liveRest));
        }

        private static bool StartForage(StubWorld w)
        {
            var pc = default(Percept); var obs = new short[ObsSchema.Length]; var m = default(ActMask);
            w.Observe(1, ref pc, obs, ref m);
            if (!ActMask.Allows(m.Control, (int)Prim.Use)) return false;
            for (int k = 0; k < ObsSchema.Entities; k++)
            {
                if (!ActMask.Allows(m.Target, k)) continue;
                var a = ActionChoice.Of(Prim.Use); a.Target = k; a.Source = Heads.SourceEmpty;
                var c = w.Decode(1, a);
                if (c.Target.Kind == TargetKind.ForagePlace && w.Submit(in c, ControlOrigin.Fixture) == MotorStatus.Accepted) return true;
            }
            return false;
        }

        // ---------------- helpers ----------------

        private static QNet Untrained() => Checkpoint.Untrained(NetShape.Default, 1, "stub").ToQNet();

        private static EvalSetup Setup(long[] seeds, params (string name, string brain, QNet net)[] arms)
        {
            var s = new EvalSetup { World = "stub", Seeds = seeds, NeedMask = 0x033, Workers = 2 };
            foreach (var a in arms) { var n = a.net; s.Arms.Add(new ArmSpec { Name = a.name, Brain = a.brain, WeightsSha256 = n.WeightsSha256(), Actor = () => new NetActor(n) }); }
            return s;
        }
    }
}
