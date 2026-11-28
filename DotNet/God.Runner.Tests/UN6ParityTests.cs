using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using God.Runner.Tests.Support;
using God.Sim;
using God.Sim.Unified;
using God.Train.Artifact;
using God.Train.Net;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// UN6 against UN6t-a (lead request after the UN6t-a merge): bit-exact parity of the game's integer inference
    /// (<see cref="BrainNet"/>) with the trainer's reference (<see cref="QNet"/>), the artifact header contract of <c>b441f57</c>, and
    /// per-game policy injection.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class UN6ParityTests
    {
        private static ActMask Mask(Random r, bool all)
        {
            ulong Bits(int n) => n >= 64 ? ulong.MaxValue : (1UL << n) - 1;
            if (all) return new ActMask { Control = 0x1FFEUL, Target = Bits(19), Source = Bits(5), Slot = Bits(4), Design = Bits(33), Gesture = Bits(32), Referent1 = Bits(22), Referent2 = Bits(22), Quantity = Bits(16) };
            ulong R(int n) => ((ulong)r.Next() << 32 | (uint)r.Next()) & Bits(n);
            return new ActMask { Control = R(13) | (1UL << r.Next(1, 13)), Target = R(19), Source = R(5), Slot = R(4), Design = R(33), Gesture = R(32), Referent1 = R(22), Referent2 = R(22), Quantity = R(16) };
        }

        /// <summary>
        /// 5,000 decisions (one recurrent stream per net), observations uniform in +-2,500 (beyond the clamp), all-legal and random
        /// masks, greedy and sampled: identical choices on every head, identical recurrent state after every decision, identical Rng
        /// state (the same draws), identical logits of all nine heads at the end. Untrained weights (seed 1) and random full-range weights.
        /// </summary>
        [Test]
        public void Brain_QNetParity()
        {
            Assert.AreEqual(NetShape.Default.Text, BrainShape.DefaultText, "the same default shape");
            Assert.AreEqual(NetShape.Default.Count, BrainShape.Default.Count, "22,654 weights");
            Assert.AreEqual(NetShape.Default.MacPerDecision, BrainShape.Default.MacPerDecision, "19,616 MAC");
            Assert.AreEqual(QNet.WeightShift, BrainNet.WeightShift); Assert.AreEqual(QNet.ExpRatioQ16, BrainNet.ExpRatioQ16);
            CollectionAssert.AreEqual(QNet.Exp, BrainNet.BuildExp(), "the same integer softmax table");
            for (int h = 0; h < BrainHeads.Count; h++) { Assert.AreEqual(Heads.Size[h], BrainHeads.Size[h]); Assert.AreEqual(Heads.Offset[h], BrainHeads.Offset[h]); }
            for (int c = 0; c < 13; c++) Assert.AreEqual(Heads.UsedBy(c), BrainHeads.UsedBy(c));

            var wr = new Random(77);
            var full = new short[BrainShape.Default.Count];
            for (int i = 0; i < full.Length; i++) full[i] = (short)wr.Next(short.MinValue, short.MaxValue + 1);
            int decisions = 0;
            foreach (var (name, w) in new[] { ("untrained seed 1", BrainNet.UntrainedWeights(BrainShape.Default, 1)), ("full-range random", full) })
            {
                var q = new QNet(NetShape.Default, w); var sc = new QScratch(q.Shape);
                var b = new BrainNet(BrainShape.Default, w);
                var hq = new short[BrainState.RecurrentSize]; var hb = new short[BrainState.RecurrentSize];
                var rq = new Rng(5); var rb = new Rng(5);
                var r = new Random(9);
                for (int i = 0; i < 5000; i++)
                {
                    var o = new short[ObsSchema.Length];
                    for (int k = 0; k < o.Length; k++) o[k] = (short)r.Next(-2500, 2501);
                    var m = Mask(r, i % 2 == 0);
                    bool greedy = i % 3 == 0;
                    var a = q.Act(o, hq, in m, greedy ? null : rq, sc);
                    var c = b.Act(o, hb, in m, greedy ? null : rb);
                    for (int hd = 0; hd < BrainHeads.Count; hd++) Assert.AreEqual(a.Get(hd), c.Get(hd), $"{name}: decision {i} head {hd}");
                    CollectionAssert.AreEqual(hq, hb, $"{name}: recurrent state after decision {i}");
                    Assert.AreEqual(rq.State, rb.State, $"{name}: rng after decision {i}");
                    decisions++;
                }
                for (int hd = 0; hd < BrainHeads.Count; hd++)
                {
                    q.Logits(hd, hq, sc); b.Logits(hd, hb);
                    for (int k = 0; k < BrainHeads.Size[hd]; k++) Assert.AreEqual(sc.Logits[k], b.Scratch.Logits[k], $"{name}: logit {hd}/{k}");
                }
                Assert.AreEqual(q.WeightsSha256(), BrainNet.WeightsSha256(w), "the same weight hash");
            }
            TestContext.WriteLine("UN6 parity with QNet: " + decisions + " decisions, 0 differences (choices, recurrent state, rng, logits)");

            // the IPrimitivePolicy path: QNetPolicy vs UntrainedPolicy over the same weights, temperature 1000, no overlay bias
            var w1 = BrainNet.UntrainedWeights(BrainShape.Default, 1);
            var qp = new QNetPolicy(new QNet(NetShape.Default, w1), (in Percept p, in ActionChoice a) => new PrimitiveControl { Control = (Prim)a.Control, Actor = p.Actor });
            var up = new UntrainedPolicy(new BrainNet(BrainShape.Default, w1));
            var bq = new BrainState(); var bu = new BrainState(); var g1 = new Rng(3); var g2 = new Rng(3); var pr = new Percept { Actor = 1 };
            var rr = new Random(4);
            for (int i = 0; i < 500; i++)
            {
                var o = new short[ObsSchema.Length]; for (int k = 0; k < o.Length; k++) o[k] = (short)rr.Next(-1000, 1001);
                var m = Mask(rr, true);
                var x = qp.Decide(in pr, o, ref bq, in m, ref g1); var y = up.Decide(in pr, o, ref bu, in m, ref g2);
                Assert.AreEqual(x.Control, y.Control);
                for (int hd = 0; hd < BrainHeads.Count; hd++) Assert.AreEqual(qp.Last.Get(hd), up.LastChoice.Get(hd));
            }
            CollectionAssert.AreEqual(bq.Recurrent, bu.Recurrent);
        }

        /// <summary>
        /// The header contract of b441f57: UN6t's writer and UN6's test generator name the same constants; an artifact written by
        /// <see cref="ArtifactWriter"/> parses, verifies and carries the same weights; UN6's test artifacts parse and verify with UN6t's
        /// reader; UN6's loader reads the compiled test artifacts with the same header values UN6t's parser reads from their text.
        /// </summary>
        [Test]
        public void Artifact_HeaderContract()
        {
            var w = UN6TestArtifacts.Weights();
            var shape = NetShape.Parse(UN6TestArtifacts.Shape);
            var h = new ArtifactHeader { Version = UN6TestArtifacts.Valid, Status = "SELF-TEST", CatalogueHash = ArtifactWriter.CurrentCatalogueHash() };
            string theirs = ArtifactWriter.Write(h, new QNet(shape, w));
            string mine = UN6TestArtifacts.Text(UN6TestArtifacts.Valid);
            foreach (var f in ArtifactHeader.IntFields.Concat(ArtifactHeader.StringFields))
            {
                Assert.IsTrue(Regex.IsMatch(theirs, "public const (int|string) " + f + " = "), "writer: " + f);
                Assert.IsTrue(Regex.IsMatch(mine, "public const (int|string) " + f + " = "), "UN6 generator: " + f);
            }
            var th = ArtifactWriter.ParseHeader(theirs, out var tw);
            Assert.IsNull(ArtifactWriter.Verify(th, tw, ArtifactWriter.CurrentCatalogueHash()));
            var mh = ArtifactWriter.ParseHeader(mine, out var mw);
            Assert.IsNull(ArtifactWriter.Verify(mh, mw, ArtifactWriter.CurrentCatalogueHash()), "UN6's test artifact is a valid UN6t artifact");
            CollectionAssert.AreEqual(tw, mw); Assert.AreEqual(th.WeightsSha256, mh.WeightsSha256);
            Assert.AreEqual(ArtifactWriter.CurrentCatalogueHash(), ArtifactLoader.CurrentCatalogueHash(), "the same catalogue id rule");
            // UN6's loader over the compiled test artifact: the same header and weights as UN6t's parser of the text
            var a = ArtifactLoader.Read(UN6TestArtifacts.Valid, out var err);
            Assert.IsNotNull(a, err);
            Assert.AreEqual(mh.WeightsSha256, a.WeightsSha256); Assert.AreEqual(mh.NetShape, a.NetShape); Assert.AreEqual(mh.WeightCount, a.WeightCount);
            Assert.AreEqual(mh.ObsSchemaHash, a.ObsSchemaHash); Assert.AreEqual(mh.ActSchemaHash, a.ActSchemaHash); Assert.AreEqual(mh.CatalogueHash, a.CatalogueHash);
            CollectionAssert.AreEqual(mw, a.Weights);
            // the two refusal rules agree on the corrupted variants
            foreach (var v in new[] { UN6TestArtifacts.FlippedWeight, UN6TestArtifacts.SchemaChanged, UN6TestArtifacts.CatalogueChanged })
            {
                var xh = ArtifactWriter.ParseHeader(UN6TestArtifacts.Text(v), out var xw);
                Assert.IsNotNull(ArtifactWriter.Verify(xh, xw, ArtifactWriter.CurrentCatalogueHash()), "UN6t refuses v" + v);
                Assert.IsNotNull(ArtifactLoader.Verify(ArtifactLoader.Read(v, out _), ArtifactLoader.CurrentCatalogueHash()), "UN6 refuses v" + v);
            }
            // a SELF-TEST artifact outside the test range can never run as a game brain; INTERIM / FAILED-TR01 run labelled, never trained
            var self = a.Copy(); self.Version = 3;
            StringAssert.Contains("SELF-TEST", ArtifactLoader.Verify(self, ArtifactLoader.CurrentCatalogueHash()));
            var interim = a.Copy(); interim.Version = 3; interim.Status = "INTERIM";
            Assert.IsNull(ArtifactLoader.Verify(interim, ArtifactLoader.CurrentCatalogueHash()));
            Assert.AreEqual("ARTIFACT v3 INTERIM", new TrainedPolicy(interim).Label);
            var odd = a.Copy(); odd.Status = "BEST-EVER";
            StringAssert.Contains("unknown artifact status", ArtifactLoader.Verify(odd, ArtifactLoader.CurrentCatalogueHash()));
        }

        /// <summary>Per-game injection: two games in parallel threads, one with its own brain, one with an injected policy; neither affects the other.</summary>
        [Test]
        public void Policy_InjectionPerGame()
        {
            ulong Solo(bool inject)
            {
                var sim = UN6Games.Game(141, 3);
                UN6Games.Attach(sim);
                if (inject) Assert.IsTrue(Brain.InjectPolicy(sim, new FallbackPolicy(), ControlOrigin.Teacher));
                sim.Step(150);
                return sim.StateHash();
            }
            ulong own = Solo(false), injected = Solo(true);
            Assert.AreNotEqual(own, injected, "the injected policy decides");
            var results = new ulong[8];
            Parallel.For(0, 8, i => results[i] = Solo(i % 2 == 1));
            for (int i = 0; i < 8; i++) Assert.AreEqual(i % 2 == 1 ? injected : own, results[i], "game " + i + " unaffected by the others");
            // labelling: an injected policy never counts as the game's brain, and Inject(null) restores it
            var g = UN6Games.Game(142, 2, t => { t.Unification.Brain.Kind = 1; t.Unification.Brain.Artifact = UN6TestArtifacts.Valid; });
            var rt = g.State.Unification.BrainRuntime;
            Assert.IsFalse(rt.Injected);
            rt.Inject(new ScriptedPolicy(new PrimitiveControl { Control = Prim.Wait }), ControlOrigin.Teacher);
            Assert.IsTrue(rt.Injected); Assert.AreEqual(ControlOrigin.Teacher, rt.Origin);
            StringAssert.Contains("injected policy", Brain.Report(g.State));
            Assert.IsFalse(Brain.Badge(g.State).Trained);
            rt.Inject(null, ControlOrigin.Teacher);
            Assert.IsFalse(rt.Injected); Assert.AreEqual(ControlOrigin.Policy, rt.Origin); Assert.IsInstanceOf<TrainedPolicy>(rt.Policy);
            Assert.IsFalse(Brain.InjectPolicy(GodSim.Create(new GameSetup(1, GameSetup.NoHuman, 2) { Population = true, Victory = VictorySpec.SandboxOnly() }, new SimTuning()), new FallbackPolicy(), ControlOrigin.Teacher), "a legacy game has no brain site");
        }
    }
}
