using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Divinity;
using God.Sim.Population;
using God.Sim.Population.Households;
using God.Sim.Social;
using NUnit.Framework;
using UnityEngine;

namespace God.Game.Tests
{
    /// <summary>
    /// U3b pure planners (R14: EditMode): the gesture animation planner, the clips and the sequencer (U3-T4, T5, T7), the
    /// character look functions (T9's numbers) and the divine effect planner (T10, T11 planner half, T12 planner half).
    /// </summary>
    public class GestureAnimationPlannerTests
    {
        private static GestureMade G(GestureAct a, int speaker = 1, int target = 2, int msg = 10, int deal = 0, long tick = 5, int seq = 2)
            => new GestureMade { Tick = tick, Seq = seq, MessageId = msg, Speaker = speaker, Act = a, Target = target, DealId = deal, DurationTicks = 3 };

        // ---------------------------------------------------------------- T4

        [Test]
        public void EveryGestureAct_HasCue_WithSourceAndFamily()
        {
            foreach (GestureAct a in Enum.GetValues(typeof(GestureAct)))
            {
                if (a == GestureAct.None) continue;
                var p = new GestureAnimationPlanner();
                var g = G(a);
                var cues = p.Plan(g);
                Assert.GreaterOrEqual(cues.Count, 1, a + " has a cue");
                Assert.IsTrue(cues.All(c => c.SourceEventId == GestureAnimationPlanner.EventKey(g) && c.Actor == 1), a + " carries its source event and actor");
                Assert.IsTrue(cues.Any(c => c.Kind == GestureAnimationPlanner.FamilyOf(a, g.TopicKind)), a + " plays its clip family");
                Assert.IsTrue(cues.All(c => c.Phases != null && c.Phases.Length > 0), a + " has phases");
            }
            // the table of the design: a sample of the families
            Assert.AreEqual(AnimKind.Offering, GestureAnimationPlanner.FamilyOf(GestureAct.Offer));
            Assert.AreEqual(AnimKind.Refusing, GestureAnimationPlanner.FamilyOf(GestureAct.Refuse));
            Assert.AreEqual(AnimKind.Warning, GestureAnimationPlanner.FamilyOf(GestureAct.Warn));
            Assert.AreEqual(AnimKind.Threatening, GestureAnimationPlanner.FamilyOf(GestureAct.Threaten));
            Assert.AreEqual(AnimKind.Gratitude, GestureAnimationPlanner.FamilyOf(GestureAct.Thank));
            Assert.AreEqual(AnimKind.Praying, GestureAnimationPlanner.FamilyOf(GestureAct.Pray));
            Assert.AreEqual(AnimKind.Teaching, GestureAnimationPlanner.FamilyOf(GestureAct.Teach));
        }

        [Test]
        public void Offer_ShowsTheRealWare_AndTheQuantity()
        {
            var g = G(GestureAct.Offer); g.Presented = true; g.CounterKind = TopicKind.Ware; g.CounterId = (int)Ware.Berries; g.CounterSub = 3;
            g.TopicKind = TopicKind.Ware; g.TopicId = (int)Ware.Fish; g.Quantity = 2;
            var c = new GestureAnimationPlanner().Plan(g).Single(x => x.Kind == AnimKind.Offering);
            Assert.AreEqual(Ware.Berries, c.Object.Ware); Assert.AreEqual(3, c.Object.Count); Assert.AreEqual(2, c.Quantity);
            Assert.AreEqual(3, c.DurationTicks, "the duration is the sim's");
        }

        // ---------------------------------------------------------------- T5

        [Test]
        public void Giving_OnlyAfterGestureAndDecision()
        {
            var step = new ExchangeStep { Tick = 20, Seq = 1, DealId = 7, Giver = 1, Receiver = 2, Lot = 55, Ware = Ware.Berries, Count = 3, Node = 4 };
            var p = new GestureAnimationPlanner();
            Assert.AreEqual(0, p.Plan(step).Count, "a hand-over with no gesture and no decision draws no Giving");
            p.Plan(G(GestureAct.Offer, deal: 7, msg: 11));
            Assert.AreEqual(0, p.Plan(step).Count, "a gesture alone is not enough");
            p.Plan(new ResponseChosen { Tick = 10, MessageId = 11, Listener = 2, Speaker = 1, Ignored = true });
            Assert.AreEqual(0, p.Plan(step).Count, "an ignored message is no decision");
            p.Plan(new ResponseChosen { Tick = 11, MessageId = 11, Listener = 2, Speaker = 1, Ignored = false });
            var cues = p.Plan(step);
            Assert.AreEqual(2, cues.Count);
            var give = cues.Single(c => c.Kind == AnimKind.Giving); var take = cues.Single(c => c.Kind == AnimKind.Receiving);
            Assert.AreEqual(1, give.Actor); Assert.AreEqual(2, give.Target); Assert.AreEqual(2, take.Actor); Assert.AreEqual(1, take.Target);
            Assert.AreEqual(Ware.Berries, give.Object.Ware); Assert.AreEqual(3, give.Object.Count); Assert.AreEqual(55, give.Object.Lot);
            Assert.AreEqual(give.Object.Lot, take.Object.Lot);
            Assert.AreEqual(20, give.StartTick); Assert.AreEqual(take.StartTick, give.StartTick, "lockstep on the hand-over tick");
        }

        [Test]
        public void CareTransfers_UseTheFunctionalPose()
        {
            var p = new GestureAnimationPlanner();
            var fed = p.Plan(new ChildFed { Tick = 9, Feeder = 3, Child = 4, Lot = 8, Ware = Ware.Roots, Count = 1 });
            Assert.AreEqual(1, fed.Count);
            Assert.AreEqual(AnimKind.Feeding, fed[0].Kind); Assert.IsTrue(fed[0].Functional);
            var care = p.Plan(new CareGiven { Tick = 9, Carer = 3, Patient = 4, Kind = CareGiven.Tend });
            Assert.AreEqual(AnimKind.Tending, care[0].Kind); Assert.IsTrue(care[0].Functional);
            Assert.IsFalse(fed.Concat(care).Any(c => c.Kind == AnimKind.Giving || c.Kind == AnimKind.Receiving));
        }

        [Test]
        public void ResponseAnimations_OnlyFromTheirOwnEvents()
        {
            var p = new GestureAnimationPlanner();
            Assert.AreEqual(AnimKind.Refusing, p.Plan(new CommandResponded { Tick = 3, Npc = 5, Response = CommandResponse.Refused }).Single().Kind);
            Assert.AreEqual(AnimKind.Agreeing, p.Plan(new CommandResponded { Tick = 3, Npc = 5, Response = CommandResponse.Followed }).Single().Kind);
            Assert.AreEqual(0, p.Plan(new CommandResponded { Tick = 3, Npc = 5, Response = CommandResponse.Ignored }).Count);
            Assert.AreEqual(0, p.Plan(new CommandResponded { Tick = 3, Npc = 5, Response = CommandResponse.Lapsed }).Count);
            Assert.AreEqual(AnimKind.Gratitude, p.Plan(new MiracleEffectApplied { Tick = 4, ThingKind = MiracleThing.Person, ThingId = 5, Field = "injury", Before = 500, After = 0 }).Single().Kind);
            Assert.AreEqual(AnimKind.Fear, p.Plan(new MiracleEffectApplied { Tick = 4, ThingKind = MiracleThing.Witness, ThingId = 6, Field = "fear", Before = 0, After = 300 }).Single().Kind);
            Assert.AreEqual(0, p.Plan(new MiracleEffectApplied { Tick = 4, ThingKind = MiracleThing.Hut, ThingId = 2, Field = "lot" }).Count);
            Assert.AreEqual(0, p.Plan(null).Count);
        }

        [Test]
        public void SameEvents_SameCues()
        {
            var evs = new List<SimEvent>
            {
                G(GestureAct.Offer, deal: 3, msg: 1), new ResponseChosen { Tick = 6, MessageId = 1, Listener = 2, Speaker = 1 },
                new ExchangeStep { Tick = 7, DealId = 3, Giver = 1, Receiver = 2, Lot = 9, Ware = Ware.Fish, Count = 1 }, G(GestureAct.Thank, 2, 1, 2, 3, 8),
            };
            string Dump(GestureAnimationPlanner p) => string.Join("|", evs.SelectMany(e => p.Plan(e)).Select(c => c.Id + ":" + c.Kind + ":" + c.Actor + ":" + c.SourceEventId + ":" + c.Object.Lot));
            Assert.AreEqual(Dump(new GestureAnimationPlanner()), Dump(new GestureAnimationPlanner()));
        }

        [Test]
        public void Planner_CanBeExtended_ByEventType()
        {
            var p = new GestureAnimationPlanner();
            p.Register<InjuryTaken>((e, o) => o.Add(new AnimationCue { Actor = e.Npc, Kind = AnimKind.Fear, Phases = new[] { "x" } }));
            Assert.AreEqual(1, p.Plan(new InjuryTaken { Npc = 3 }).Count);
        }

        // ---------------------------------------------------------------- T7

        [Test]
        public void Clips_PureAndTruthBlind()
        {
            var a = new float[(int)PoseChannel.Count]; var b = new float[(int)PoseChannel.Count];
            int clips = 0;
            foreach (var clip in PoseClips.All)
            {
                clips++;
                Assert.Greater(clip.Mask, 0, clip.Kind + " drives something");
                Assert.IsTrue(clip.Phases.Length > 0 && clip.Phases.Length == clip.PhaseEnds.Length);
                for (int i = 0; i <= 100; i++)
                    foreach (float inten in new[] { 0f, 0.5f, 1f })
                    {
                        Array.Clear(a, 0, a.Length); Array.Clear(b, 0, b.Length);
                        clip.Sample(i / 100f, inten, a); clip.Sample(i / 100f, inten, b);
                        for (int c = 0; c < a.Length; c++)
                        {
                            Assert.AreEqual(a[c], b[c], "deterministic");
                            Assert.GreaterOrEqual(a[c], JointLimits.Min((PoseChannel)c), clip.Kind + " channel " + (PoseChannel)c);
                            Assert.LessOrEqual(a[c], JointLimits.Max((PoseChannel)c), clip.Kind + " channel " + (PoseChannel)c);
                        }
                    }
            }
            Assert.GreaterOrEqual(clips, 20, "the vocabulary of twenty (plus the care poses)");
            for (var k = AnimKind.Attention; k < AnimKind.Count; k++) Assert.IsNotNull(PoseClips.Of(k), k + " has a clip");
            // handed items pass at the same moment for both partners
            Assert.AreEqual(PoseClips.Of(AnimKind.Giving).ReleaseT, PoseClips.Of(AnimKind.Receiving).ReleaseT);
            Assert.AreEqual(PoseClips.Of(AnimKind.Giving).Seconds, PoseClips.Of(AnimKind.Receiving).Seconds);

            // the planner, clips, sequencer and divine planner name no truth flag and no sim state (comments excluded)
            string dir = Path.Combine(Application.dataPath, "God/Game/Animation");
            foreach (var f in Directory.GetFiles(dir, "*.cs"))
            {
                string code = string.Join("\n", File.ReadAllLines(f).Where(l => !l.TrimStart().StartsWith("//") && !l.TrimStart().StartsWith("///") && !l.TrimStart().StartsWith("*")));
                foreach (var bad in new[] { "Honest", "Mismatch", "GodState", "StateHash", "Submit(" })
                    Assert.IsFalse(Regex.IsMatch(code, @"\b" + Regex.Escape(bad.TrimEnd('('))), Path.GetFileName(f) + " names " + bad);
                Assert.IsFalse(code.Contains("UnityEngine"), Path.GetFileName(f) + " uses no engine type");
            }
        }

        [Test]
        public void Sequencer_PlaysInOrder_CapsTheQueue_AndHandOversPreempt()
        {
            var s = new PoseSequencer();
            AnimationCue C(AnimKind k, int id) => new AnimationCue { Id = id, Kind = k, Intensity = 0.5f };
            s.Enqueue(C(AnimKind.Attention, 1), 0.0);
            s.Enqueue(C(AnimKind.Offering, 2), 0.0);
            Assert.IsTrue(s.Advance(0.05));
            Assert.AreEqual(1, s.Active.Id);
            Assert.Greater(s.Weight, 0f);
            double later = PoseSequencer.SecondsOf(C(AnimKind.Attention, 1)) + 0.05;
            Assert.IsTrue(s.Advance(later));
            Assert.AreEqual(2, s.Active.Id, "the second cue follows the first");
            for (int i = 0; i < 10; i++) s.Enqueue(C(AnimKind.Pointing, 10 + i), later);
            Assert.LessOrEqual(s.Pending, PoseSequencer.MaxQueue);
            s.Enqueue(C(AnimKind.Giving, 99), later);
            Assert.IsTrue(s.Advance(later + 0.01));
            Assert.AreEqual(99, s.Active.Id, "a hand-over pre-empts the queue");
            Assert.IsFalse(s.Advance(later + 100), "and the queue ends");
            Assert.AreEqual(0f, s.Weight);
        }

        [Test]
        public void Duration_FromTheSim_CanOnlyLengthenTheClip()
        {
            var shortC = new AnimationCue { Kind = AnimKind.Attention, Intensity = 0.5f, DurationTicks = 3 };
            var longC = new AnimationCue { Kind = AnimKind.Attention, Intensity = 0.5f, DurationTicks = 100 };
            Assert.AreEqual(PoseClips.Of(AnimKind.Attention).Seconds, PoseSequencer.SecondsOf(shortC), 0.01f);
            Assert.AreEqual(10f, PoseSequencer.SecondsOf(longC), 0.01f);
        }

        // ---------------------------------------------------------------- T9 numbers

        [Test]
        public void CharacterLook_Functions()
        {
            Assert.AreEqual(CharacterLook.BellyScale(0f), 0.25f, 1e-4f);
            Assert.Greater(CharacterLook.BellyScale(0.9f), CharacterLook.BellyScale(0.4f));
            Assert.AreEqual(0f, CharacterLook.Progress(5, 10, 100)); Assert.AreEqual(1f, CharacterLook.Progress(500, 10, 100));
            Assert.AreEqual(0.5f, CharacterLook.Progress(55, 10, 100), 1e-4f);
            Assert.IsFalse(CharacterLook.Bandaged(0)); Assert.IsTrue(CharacterLook.Bandaged(1));
            Assert.IsFalse(CharacterLook.Slung(1000)); Assert.IsTrue(CharacterLook.Slung(CharacterLook.SlingFrom));
            Assert.Greater(CharacterLook.HeadScale(true, 0), CharacterLook.HeadScale(true, 900));
            Assert.AreEqual(1f, CharacterLook.HeadScale(false, 0));
            Assert.Greater(CharacterLook.SlumpDegrees(true, 0), CharacterLook.SlumpDegrees(false, 0));
            Assert.Greater(CharacterLook.SlumpDegrees(false, 1000), CharacterLook.SlumpDegrees(false, 600));
            Assert.Less(CharacterLook.LimpKeep(5), CharacterLook.LimpKeep(0));
        }

        // ---------------------------------------------------------------- T10

        [Test]
        public void DivineFx_Distinct_InTwoOfThreeChannels()
        {
            var names = DivineFxPlanner.Table.Keys.OrderBy(x => x).ToList();
            for (int i = 0; i < names.Count; i++)
                for (int j = i + 1; j < names.Count; j++)
                {
                    var a = DivineFxPlanner.Table[names[i]]; var b = DivineFxPlanner.Table[names[j]];
                    int diff = (a.Shape != b.Shape ? 1 : 0) + (a.Colour != b.Colour ? 1 : 0) + (a.Persist != b.Persist ? 1 : 0);
                    Assert.GreaterOrEqual(diff, 2, names[i] + " vs " + names[j] + " differ in shape, colour or persistence");
                }
        }

        private static List<FxRecord> Plan(DivineFxPlanner p, SimEvent e) => p.Plan(e);

        [Test]
        public void DivineFx_OneRecordPerEvent_NoneWithout()
        {
            var p = new DivineFxPlanner();
            Assert.AreEqual(0, p.Plan(null).Count);
            Assert.AreEqual(0, p.Plan(new CommandResponded { Tick = 5, Npc = 1, Response = CommandResponse.Refused }).Count, "the response animates the person, not an effect");
            Assert.AreEqual(0, p.Plan(new DivineCommandIssued { Tick = 5 }).Count, "the issue row alone draws nothing: the perception does");
            var evs = new SimEvent[]
            {
                new CommandPerceived { Tick = 6, Npc = 1, God = 1, Kind = InterventionKind.Suggestion, Intervention = 1 },
                new CommandPerceived { Tick = 6, Npc = 1, God = 1, Kind = InterventionKind.Command, Intervention = 2 },
                new CompulsionStarted { Tick = 6, Npc = 1, God = 1, Intervention = 3 },
                new CompulsionBlocked { Tick = 7, Npc = 1, God = 1, Intervention = 3 },
                new CompulsionEnded { Tick = 8, Npc = 1, God = 1, Intervention = 3 },
                new MiracleCast { Tick = 9, God = 1, Kind = MiracleKind.HealInjury, TargetKind = DivineTargetKind.Npc, TargetId = 1, Intervention = 4 },
                new MiracleCast { Tick = 9, God = 1, Kind = MiracleKind.SmiteBuilding, TargetKind = DivineTargetKind.Hut, TargetId = 2, Intervention = 5 },
                new MiracleEffectApplied { Tick = 9, Intervention = 5, ThingKind = MiracleThing.Hut, ThingId = 2 },
                new ResourceCreated { Tick = 10, God = 1, Ware = Ware.Berries, Count = 3, Lot = 7, Node = 2 },
                new ResourceDestroyed { Tick = 10, God = 1, Ware = Ware.Berries, Count = 3, Lot = 7, Node = 2 },
                new LocationConsecrated { Tick = 11, God = 1, Node = 3 },
            };
            foreach (var e in evs) Assert.AreEqual(1, p.Plan(e).Count, e.GetType().Name + " gives exactly one record");
            var q = new DivineFxPlanner();
            var r = new[] { 845, 846, 850, 851, 852, 860, 863, 870, 880 };
            foreach (int c in r) Assert.AreEqual(1, q.PlanCode((ushort)c, 20, 0, 1, 3, 4).Count, "code " + c);
            Assert.AreEqual(0, q.PlanCode(999, 20, 0, 1, 3, 4).Count);
        }

        [Test]
        public void DivineFx_Persistent_AreClosedByTheirEndingEvent()
        {
            var p = new DivineFxPlanner();
            var start = p.Plan(new CompulsionStarted { Tick = 6, Npc = 4, God = 2, Intervention = 3 }).Single();
            Assert.AreEqual(long.MaxValue, start.EndTick);
            var end = p.Plan(new CompulsionEnded { Tick = 90, Npc = 4, God = 2, Intervention = 3 }).Single();
            Assert.AreEqual(start.Id, end.Closes);
            Assert.AreEqual(90, start.EndTick);
        }

        [Test]
        public void DivineFx_Smite_BoltOnCast_CollapseOnlyOnEffect()
        {
            var p = new DivineFxPlanner();
            var bolt = p.Plan(new MiracleCast { Tick = 9, God = 1, Kind = MiracleKind.SmiteBuilding, TargetKind = DivineTargetKind.Hut, TargetId = 2, TargetNode = 12, Intervention = 5 }).Single();
            Assert.AreEqual(FxShape.Bolt, bolt.Shape);
            var fall = p.Plan(new MiracleEffectApplied { Tick = 9, Intervention = 5, ThingKind = MiracleThing.Hut, ThingId = 2 }).Single();
            Assert.AreEqual(FxShape.Collapse, fall.Shape); Assert.AreEqual(12, fall.Node);
            var goods = p.Plan(new ResourceDestroyed { Tick = 9, God = 1, Ware = Ware.Log, Count = 4, Lot = 33, Node = 12 }).Single();
            Assert.AreEqual(33, goods.Lot); Assert.AreEqual(Ware.Log, goods.Ware); Assert.AreEqual(4, goods.Count);
        }

        [Test]
        public void DivineFx_Budget_OverTwentyFourBecomeMarkers()
        {
            var p = new DivineFxPlanner();
            var all = new List<FxRecord>();
            for (int i = 0; i < 30; i++) all.AddRange(p.Plan(new CommandPerceived { Tick = 5, Npc = i + 1, God = 1, Kind = InterventionKind.Command, Intervention = i }));
            Assert.AreEqual(24, all.Count(r => r.Detailed));
            Assert.AreEqual(6, all.Count(r => !r.Detailed));
            Assert.AreEqual(24, p.LiveDetailed(5));
            Assert.AreEqual(0, p.LiveDetailed(5 + 1000), "they expire");
        }

        [Test]
        public void GodLook_IsAFunctionOfTheGodId()
        {
            var a = GodLook.Colour(1); var b = GodLook.Colour(1); var c = GodLook.Colour(2);
            Assert.AreEqual(a.R, b.R); Assert.AreEqual(a.G, b.G);
            Assert.IsTrue(Math.Abs(a.R - c.R) + Math.Abs(a.G - c.G) + Math.Abs(a.B - c.B) > 0.05f, "different gods differ");
            Assert.AreEqual(GodLook.Sigil(3), GodLook.Sigil(3));
            Assert.That(GodLook.Sigil(-4), Is.InRange(0, GodLook.Sigils - 1));
        }
    }
}
