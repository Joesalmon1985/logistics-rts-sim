using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Social;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// The gesture language and its boundaries on the owner's island (ACCEPTANCE M5.4, M5.10, M5.15, M5.16), with
    /// SHARED_CONTRACTS 7.1's static rules, save and load, and recording neutrality.
    /// </summary>
    public class CommsTests
    {
        private static GameSetup Setup(ulong seed, int perFaction = 20, int factions = 6) =>
            new GameSetup(seed, GameSetup.NoHuman, factions) { Population = true, StartNpcsPerFaction = perFaction, Victory = VictorySpec.SandboxOnly() };

        private static List<SimEvent> Island(ulong seed, int days, Action<SimTuning> tune = null)
        {
            var t = new SimTuning();
            tune?.Invoke(t);
            var sim = GodSim.Create(Setup(seed), t);
            var probe = new EventProbe(sim);
            for (int d = 0; d < days; d++)
            {
                probe.Step(1200);
                Assert.IsEmpty(SimInvariants.Check(sim.State), "invariants, day " + (d + 1));
            }
            return probe.Events;
        }

        // ---------------------------------------------------------------- M5.4 honest gestures

        [Test]
        public void M5_4_EveryGestureIsDecodedByEachPerceivingListener_ExactlyWithMisunderstandingOff()
        {
            var ev = Island(4, 2, t => t.Comms.Misunderstanding = 0);
            var gestures = ev.OfType<GestureMade>().ToList();
            var decoded = ev.OfType<MessageDecoded>().GroupBy(d => d.MessageId).ToDictionary(g => g.Key, g => g.ToList());
            Assert.Greater(gestures.Count, 100);
            int checkedFields = 0;
            foreach (var g in gestures)
            {
                var ds = decoded.TryGetValue(g.MessageId, out var l) ? l : new List<MessageDecoded>();
                Assert.AreEqual(g.Listeners, ds.Count, "message " + g.MessageId + ": one MessageDecoded per perceiving listener");
                Assert.AreEqual(ds.Count, ds.Select(d => d.Listener).Distinct().Count());
                foreach (var d in ds)
                {
                    Assert.IsFalse(d.Mismatch);
                    Assert.AreEqual(g.Act, d.Act); Assert.AreEqual(g.TopicKind, d.TopicKind); Assert.AreEqual(g.TopicId, d.TopicId); Assert.AreEqual(g.Quantity, d.Quantity);
                    Assert.AreEqual(g.CounterKind, d.CounterKind); Assert.AreEqual(g.CounterId, d.CounterId); Assert.AreEqual(g.CounterSub, d.CounterSub);
                    Assert.AreEqual(g.Time, d.Time); Assert.AreEqual(g.DealId, d.DealId);
                    if (g.Target == d.Listener) Assert.AreEqual(d.Listener, d.Target, "the addressee knows it is addressed");
                    checkedFields++;
                }
            }
            TestContext.WriteLine("gestures " + gestures.Count + ", decoded copies checked " + checkedFields);
        }

        [Test]
        public void Misunderstanding_On_SomeMessagesAreMisread_AndOnlyByTheDesignedFields()
        {
            var ev = Island(4, 2);
            var g = ev.OfType<GestureMade>().ToDictionary(x => x.MessageId);
            var mis = ev.OfType<MessageDecoded>().Where(d => d.Mismatch).ToList();
            TestContext.WriteLine("decoded " + ev.OfType<MessageDecoded>().Count() + ", misread " + mis.Count);
            Assert.Greater(mis.Count, 0);
            foreach (var d in mis)
            {
                var s = g[d.MessageId];
                int diffs = (s.Act != d.Act ? 1 : 0) + (s.Quantity != d.Quantity ? 1 : 0) + (s.TopicId != d.TopicId ? 1 : 0);
                Assert.AreEqual(1, diffs, "one field read wrong");
                Assert.AreEqual(s.DealId, d.DealId);
            }
        }

        // ---------------------------------------------------------------- M5.15 communication decides nothing by itself

        [Test]
        public void M5_15_EveryHandOverFollowsAGestureAndTheReceiversOwnDecision_RefusalsOccur_Compositional()
        {
            int refusals = 0, counters = 0;
            var acts = new HashSet<GestureAct>();
            var topics = new Dictionary<GestureAct, HashSet<(TopicKind, int)>>();
            for (ulong seed = 1; seed <= 4; seed++)
            {
                var ev = Island(seed, 2);
                var gestures = ev.OfType<GestureMade>().ToList();
                var byDeal = gestures.Where(x => x.DealId != 0).GroupBy(x => x.DealId).ToDictionary(x => x.Key, x => x.ToList());
                var responses = ev.OfType<ResponseChosen>().Where(r => !r.Ignored).GroupBy(r => r.MessageId).ToDictionary(x => x.Key, x => x.ToList());
                var opened = ev.OfType<ExchangeOpened>().ToDictionary(o => o.DealId);
                foreach (var g in gestures)
                {
                    acts.Add(g.Act);
                    if (!topics.TryGetValue(g.Act, out var set)) topics[g.Act] = set = new HashSet<(TopicKind, int)>();
                    set.Add((g.TopicKind, g.TopicId));
                }
                refusals += gestures.Count(x => x.Act == GestureAct.Refuse);
                counters += gestures.Where(x => x.Act == GestureAct.Offer && x.DealId != 0).GroupBy(x => x.DealId).Count(x => x.Select(y => y.Speaker).Distinct().Count() > 1);
                // every exchange step or gift: a gesture came first, and its receiver decided by itself
                foreach (var step in ev.OfType<DealStepBase>())
                {
                    var o = opened[step.DealId];
                    bool preceded;
                    if (o.Kind == DealKind.Spot || byDeal.ContainsKey(step.DealId) && o.MessageId == 0)
                    {
                        // a deal agreed by gesture: the accept, by the side that did not open it, is that side's own decision
                        var gs = byDeal[step.DealId];
                        var accept = gs.FirstOrDefault(x => x.Act == GestureAct.Accept);
                        preceded = accept != null && accept.Tick <= step.Tick && gs.Any(x => x.Act == GestureAct.Offer && x.Tick < accept.Tick)
                            && responses.Values.SelectMany(r => r).Any(r => r.Listener == accept.Speaker && r.Verb == SocialVerb.SayAccept);
                    }
                    else
                    {
                        // a gift or help in answer to a request: the request was made, and the giver chose to answer it
                        var ask = gestures.FirstOrDefault(x => x.MessageId == o.MessageId);
                        preceded = ask != null && ask.Tick < step.Tick && responses.TryGetValue(ask.MessageId, out var rs) && rs.Any(r => r.Listener == step.Giver);
                        if (!preceded && byDeal.TryGetValue(step.DealId, out var gs2))
                            preceded = gs2.Any(x => x.Act == GestureAct.Cooperate && x.Speaker == step.Giver) || gs2.Any(x => x.Act == GestureAct.Accept && x.Tick <= step.Tick);
                    }
                    Assert.IsTrue(preceded, "seed " + seed + ": deal " + step.DealId + " (" + o.Kind + ") step at " + step.Tick + " has no preceding gesture and own decision");
                }
            }
            TestContext.WriteLine("acts: " + string.Join(", ", acts) + "; refusals " + refusals + ", counter-offers " + counters);
            Assert.Greater(refusals, 0, "refusals occur (counter-offers across 20 seeds: M5NaturalTests)");
            // composed, not one gesture per transaction: the same acts carry different topics, and the core acts all appear
            Assert.That(acts, Is.SupersetOf(new[] { GestureAct.Offer, GestureAct.Accept, GestureAct.Refuse, GestureAct.NeedHelp, GestureAct.Cooperate }));
            Assert.Greater(topics[GestureAct.Offer].Count, 1, "one act, several topics");
        }

        [Test]
        public void M5_15_TheGrammar_EachActComposesWithAnyTopicQuantityCounterTargetAndTime()
        {
            var acts = new[] { GestureAct.Want, GestureAct.Offer, GestureAct.Accept, GestureAct.Refuse, GestureAct.NeedFood, GestureAct.NeedHelp, GestureAct.Cooperate, GestureAct.Warn };
            var parts = new[] { default(MsgTopic), Messages.OfWare(Ware.Fish), Messages.AtPlace(42), Messages.AboutPerson(7) };
            var seen = new HashSet<string>();
            foreach (var a in acts)
                foreach (var t in parts)
                {
                    var m = Messages.Compose(a, t, 2, Messages.OfWare(Ware.Berries), 5, MsgTime.Soon, 9);
                    Assert.AreEqual(a, m.Act); Assert.AreEqual(t.Kind, m.Topic.Kind); Assert.AreEqual(2, m.Quantity); Assert.AreEqual(5, m.Target); Assert.AreEqual(MsgTime.Soon, m.Time);
                    seen.Add(a + "/" + t.Kind);
                    Assert.AreEqual(64 + (int)a, Messages.SayVerb(a), "one learning verb per act, not per message");
                }
            Assert.AreEqual(acts.Length * parts.Length, seen.Count);
        }

        [Test]
        public void M5_15_GesturesOff_NoBarter_NoGifts()
        {
            var ev = Island(2, 2, t => t.Social.Gestures = 0);
            Assert.AreEqual(0, ev.OfType<GestureMade>().Count());
            Assert.AreEqual(0, ev.OfType<ExchangeStep>().Count(), "barter falls to zero");
            Assert.AreEqual(0, ev.OfType<GiftGiven>().Count());
            var on = Island(2, 2);
            Assert.Greater(on.OfType<ExchangeStep>().Count() + on.OfType<GiftGiven>().Count(), 0, "the control trades");
        }

        // ---------------------------------------------------------------- M5.16 intention, belief, action

        [Test]
        public void M5_16_IntentionBeliefAndAction_AreSeparateRecords_ABeliefChangesOnlyFromWhatWasPerceived()
        {
            var sim = LearningFixtures.Empty(5);
            int glade = LearningFixtures.Glade(sim);
            var a = LearningFixtures.Npc(sim, glade, InstinctSet.Default, LearningMode.On, hunger: 0);
            var b = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, glade, 0, 2), InstinctSet.Default, LearningMode.On, hunger: 400);
            var far = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, glade, 3, 25), InstinctSet.Default, LearningMode.On, hunger: 400);
            SocialFixtures.Give(sim, a, Ware.Fish, 3);
            SocialFixtures.Give(sim, b, Ware.Roots, 2);
            var ev = new EventBuffer { CurrentTick = sim.State.Tick };
            // A's intention: an offer it means (its own deal memory), made as a gesture
            var msg = Messages.Compose(GestureAct.Offer, Messages.OfWare(Ware.Roots), 1, new MsgTopic { Kind = TopicKind.Ware, Id = (int)Ware.Fish, Sub = 1 }, b.Id, MsgTime.Now, 77, true);
            int id = Messages.Emit(sim.State, ev, a, msg);
            // B's belief: its own decoded copy, in its inbox; the far one perceived nothing and believes nothing
            var bm = b.Mind.Social;
            Assert.AreEqual(1, bm.InboxCount);
            Assert.AreEqual(id, bm.Inbox[0].Msg.MessageId);
            Assert.AreEqual(GestureAct.Offer, bm.Inbox[0].Msg.Act);
            Assert.AreEqual(0, far.Mind.Social.InboxCount, "out of sight: not perceived, no belief");
            Assert.AreEqual(0, a.Mind.Social.InboxCount, "the speaker does not hear itself");
            // no action: a gesture moves nothing (A still holds its fish, B its roots; no ItemMoved)
            Assert.AreEqual(3, PopulationModule.CountOf(a, Ware.Fish));
            Assert.AreEqual(2, PopulationModule.CountOf(b, Ware.Roots));
            Assert.IsFalse(ev.Items.Any(e => e is ItemMoved), "saying is not doing");
            Assert.IsTrue(ev.Items.OfType<GestureMade>().Single().Honest);
            Assert.IsTrue(ev.Items.OfType<MessageDecoded>().All(d => d.Listener != far.Id));
        }

        // ---------------------------------------------------------------- M5.10 / static rules

        [Test]
        public void M5_10_NoDecisionCodeReadsTheDealTraceOrTruth_CommsNeverReferencesGoods()
        {
            var forbidden = new HashSet<Type>(PolicyTests.StateTypes) { typeof(DealTrace), typeof(SocialState) };
            var mindSide = new[] { typeof(SocialMindHooks), typeof(SocialMind), typeof(SocialMemory), typeof(Valuation), typeof(SocialView), typeof(BenchSocialPolicy) }
                .Concat(typeof(SocialMind).Assembly.GetTypes().Where(t => t.Namespace == "God.Sim.Social" && t.Name == "SocialCandidates"))
                .Concat(typeof(SocialMind).Assembly.GetTypes().Where(t => t.Namespace == "God.Sim.Policy" || t.Namespace == "God.Sim.Cognition" || t.Namespace == "God.Sim.Learning"));
            var bad = new List<string>();
            foreach (var t in mindSide) bad.AddRange(TypeReferenceScanner.Forbidden(t, forbidden).Select(r => r.ToString()));
            Assert.IsEmpty(bad, string.Join("\n", bad.Distinct()));
            var goods = new HashSet<Type> { typeof(Goods), typeof(MoveResult), typeof(Holder) };
            var comms = typeof(SocialMind).Assembly.GetTypes().Where(t => t.Namespace == "God.Sim.Comms").ToList();
            Assert.Greater(comms.Count, 3);
            var bad2 = new List<string>();
            foreach (var t in comms) bad2.AddRange(TypeReferenceScanner.Forbidden(t, goods).Select(r => r.ToString()));
            Assert.IsEmpty(bad2, "a gesture never moves goods: no type in God.Sim.Comms references Goods\n" + string.Join("\n", bad2));
        }

        // ---------------------------------------------------------------- determinism: save and load, recording

        [Test]
        public void SaveAtDayOnePointFive_WithSocialMemoryAndOpenDeals_EqualsStraight_RecordingNeutral()
        {
            var straight = GodSim.Create(Setup(6));
            var saved = GodSim.Create(Setup(6));
            var probe = new EventProbe(GodSim.Create(Setup(6))); // Full recording (decision trace on)
            for (int i = 0; i < 1800; i++) { straight.Step(); straight.DrainEvents(); saved.Step(); saved.DrainEvents(); }
            probe.Step(1800);
            Assert.AreEqual(straight.StateHash(), probe.Sim.StateHash(), "recording on and off: one hash");
            Assert.Greater(saved.State.Population.Npcs.Sum(n => n.Mind?.Social?.RelCount ?? 0), 0);
            var json = SaveFile.From(saved).ToJson();
            var loaded = SaveFile.FromJson(json).ToSim();
            Assert.AreEqual(saved.StateHash(), loaded.StateHash(), "the loaded game hashes as the saved one");
            Assert.AreEqual(json, SaveFile.From(loaded).ToJson(), "a second save of the loaded game is byte-identical");
            for (int i = 0; i < 1200; i++) { straight.Step(); straight.DrainEvents(); loaded.Step(); loaded.DrainEvents(); }
            Assert.AreEqual(straight.StateHash(), loaded.StateHash(), "after load the game goes on exactly as the straight run");
        }
    }
}
