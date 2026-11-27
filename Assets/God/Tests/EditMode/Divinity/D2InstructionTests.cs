using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Cognition;
using God.Sim.Divinity;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// D2a (D2 spec 14): suggestions and commands are perceived messages the NPC decides on with its own policy (brief §47
    /// scenario 8). T1 (DP1), T2 (DP2), T3 (DP3), T4, T5, T10, T12, T15. Rates are over fixed seed sets and printed.
    /// OWNER: D2.
    /// </summary>
    public class D2InstructionTests
    {
        private static CommandResponded CommandOnce(ulong seed, InstinctSet inst, int hunger, int belief, int trust, int fear = 0, InterventionKind kind = InterventionKind.Command,
            Action<SimTuning> tune = null, Action<D2Fixtures.Scene> check = null)
        {
            var sc = D2Fixtures.Glade(seed, inst, hunger, tune);
            D2Fixtures.Believe(sc.Sim, sc.Npc, 1, belief, trust, fear);
            var t = D2Fixtures.Person(sc.Npc.Id, sc.WoodNode);
            var r = kind == InterventionKind.Command
                ? sc.Sim.Submit(new IssueCommand { God = 1, Act = DivineAct.Gather, Target = t, Scope = DivineScope.Individual })
                : sc.Sim.Submit(new IssueSuggestion { God = 1, Act = DivineAct.Gather, Target = t, Scope = DivineScope.Individual });
            Assert.IsTrue(r.Ok, r.Reason);
            var resp = D2Fixtures.StepToResponse(sc);
            check?.Invoke(sc);
            return resp;
        }

        private static int Count(IEnumerable<CommandResponded> rs, CommandResponse x) => rs.Count(r => r != null && r.Response == x);

        // ---------------------------------------------------------------- D2-T1 (DP1)

        [Test]
        public void Command_PerceivedAndDecided()
        {
            // food values and hunger instinct zeroed (a naive mind: no instincts, nothing learned), hungry (700: band 2, not in pain), food in sight
            var naive = new List<CommandResponded>();
            for (ulong seed = 1; seed <= 100; seed++)
                naive.Add(CommandOnce(seed, InstinctSet.None, 700, 800, 600, check: sc =>
                {
                    Assert.AreEqual(1, sc.Probe.Of<CommandPerceived>().Count, "one CommandPerceived");
                    Assert.AreEqual(1, sc.Probe.Of<CommandResponded>().Count, "one CommandResponded");
                }));
            int followed = Count(naive, CommandResponse.Followed);
            TestContext.Progress.WriteLine("DP1 naive, hunger 700 (belief 800, trust 600): followed " + followed + "/100, refused " + Count(naive, CommandResponse.Refused));
            Assert.GreaterOrEqual(followed, 90, "follows in >= 90% of 100 seeds");
            Assert.IsTrue(naive.Where(r => r.Response == CommandResponse.Followed).All(r => r.Divine == 1440), "the decision carries the Divine term 3000 x 800 x 600 / 1e6");

            // normal values, hunger not in pain (700): the command still wins (reported)
            var hungry = new List<CommandResponded>();
            for (ulong seed = 1; seed <= 100; seed++) hungry.Add(CommandOnce(seed, InstinctSet.Default, 700, 800, 600));
            TestContext.Progress.WriteLine("DP1 normal, hunger 700 (belief 800, trust 600): followed " + Count(hungry, CommandResponse.Followed) + " refused " + Count(hungry, CommandResponse.Refused));
            Assert.GreaterOrEqual(Count(hungry, CommandResponse.Followed), 90, "a strong believer not in pain follows");

            // normal values, hunger in the pain band (850 >= BandPain 800), the brief's strong believer: survival wins by its own policy
            // (lead ruling: a command's term is capped below the pain-band instincts, Instruction.PainBandCap 300)
            var pain = new List<CommandResponded>();
            for (ulong seed = 1; seed <= 100; seed++) pain.Add(CommandOnce(seed, InstinctSet.Default, 850, 800, 600));
            int refused = Count(pain, CommandResponse.Refused);
            TestContext.Progress.WriteLine("DP1 normal, pain band 850 (belief 800, trust 600): refused " + refused + "/100, followed " + Count(pain, CommandResponse.Followed));
            Assert.GreaterOrEqual(refused, 60, "refuses in >= 60%");
            Assert.IsTrue(pain.Where(r => r.Response == CommandResponse.Followed).All(r => r.Divine == 300), "the capped term");
        }

        // ---------------------------------------------------------------- D2-T2 (DP2)

        [Test]
        public void Defiance_FromDistrust()
        {
            var rs = new List<CommandResponded>();
            int negative = 0;
            for (ulong seed = 1; seed <= 100; seed++)
                rs.Add(CommandOnce(seed, InstinctSet.None, 850, 800, -800, 0, check: sc =>
                {
                    // the term the instructed candidate carried (the opportunity slots keep only the first 8 candidates)
                    var m = sc.Npc.Mind.Divine;
                    if (DivinityMindHooks.Weight(m, m.Tuning, InterventionKind.Command, 1) == -1920 && m.Read[0].Offered) negative++;
                }));
            int followed = Count(rs, CommandResponse.Followed);
            TestContext.Progress.WriteLine("DP2 trust -800: followed " + followed + "/100, refused " + Count(rs, CommandResponse.Refused) + ", negative term seen " + negative);
            Assert.LessOrEqual(followed, 10, "follows in <= 10% of seeds");
            Assert.AreEqual(100, negative, "the divine term of the instructed candidate is negative (-1920, clamped to -2000..2000)");
        }

        // ---------------------------------------------------------------- D2-T3 (DP3)

        [Test]
        public void Suggestion_WeighsLessThanCommand()
        {
            int sf = 0, cf = 0;
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var s = CommandOnce(seed, InstinctSet.Default, 700, 500, 500, kind: InterventionKind.Suggestion);
                var c = CommandOnce(seed, InstinctSet.Default, 700, 500, 500, kind: InterventionKind.Command);
                bool sFol = s != null && s.Response == CommandResponse.Followed, cFol = c != null && c.Response == CommandResponse.Followed;
                if (sFol) sf++;
                if (cFol) cf++;
                Assert.IsFalse(sFol && !cFol, "seed " + seed + ": the suggestion was followed where the command was not");
            }
            TestContext.Progress.WriteLine("DP3 belief/trust 500, hunger 700: suggestion followed " + sf + "/100, command " + cf + "/100");
            Assert.Less(sf, cf, "a suggestion weighs less than a command");
        }

        // ---------------------------------------------------------------- D2-T4 (I-DIV-2)

        /// <summary>A policy that always rests (the "Rest" candidate), drawing nothing.</summary>
        private sealed class RestPolicy : INpcPolicy
        {
            public string Name => "test-rest";
            public PolicyLevel Level => PolicyLevel.Civilian;
            public NpcIntent Decide(in NpcObservation obs, MindView mind, ref Rng rng)
            {
                for (int i = 0; i < obs.Candidates.Count; i++) if (obs.Candidates[i].Verb == Verb.Rest) return AssociativeNpcPolicy.ToIntent(obs.Candidates[i]);
                return NpcIntent.Nothing;
            }
        }

        [Test]
        public void Suggestion_DecidedByOwnPolicy()
        {
            var trails = new List<string>[2];
            for (int run = 0; run < 2; run++)
            {
                var sc = D2Fixtures.Glade(7, InstinctSet.Default, 600, t => t.Divinity.Instruction.PerceiveSuggestion = 1000);
                sc.Sim.Systems.OfType<PopulationSystem>().First().SetPolicy("test-rest", new RestPolicy());
                sc.Npc.Policy = "test-rest";
                if (run == 1)
                {
                    D2Fixtures.Believe(sc.Sim, sc.Npc, 1, 1000, 1000);
                    Assert.IsTrue(sc.Sim.Submit(new IssueSuggestion { God = 1, Act = DivineAct.Gather, Target = D2Fixtures.Person(sc.Npc.Id, sc.WoodNode), Scope = DivineScope.Individual }).Ok);
                }
                var trail = new List<string>();
                for (int i = 0; i < 300; i++)
                {
                    sc.Probe.Step(1);
                    var n = sc.Npc;
                    trail.Add(n.Node + "/" + n.Action.Verb + "/" + string.Join(",", n.Drives) + "/" + n.Carry.Count(l => !l.Empty));
                }
                trails[run] = trail;
                if (run == 1)
                {
                    var resp = sc.Probe.Of<CommandResponded>().Single();
                    Assert.IsTrue(resp.Response == CommandResponse.Refused || resp.Response == CommandResponse.Ignored, "the policy's own choice stands: " + resp.Response);
                    Assert.AreEqual(1, sc.Npc.Mind.Divine.InboxCount, "only the inbox (and the mind's divine part) changed");
                }
            }
            CollectionAssert.AreEqual(trails[0], trails[1], "body, place, action and carry identical with and without the suggestion");
        }

        // ---------------------------------------------------------------- D2-T5

        [Test]
        public void Misread_Possible()
        {
            int perceived = 0, mismatch = 0, actDiffers = 0, misread = 0;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var sc = D2Fixtures.Glade(seed, InstinctSet.None, 850, t => { t.Divinity.Instruction.SuggestionNoisePermille = 1000; t.Divinity.Instruction.PerceiveSuggestion = 1000; });
                D2Fixtures.Believe(sc.Sim, sc.Npc, 1, 1000, 1000);
                // "gather at the bush" read as "eat at the bush" when the act is misread (Similar(Gather) = Eat)
                Assert.IsTrue(sc.Sim.Submit(new IssueSuggestion { God = 1, Act = DivineAct.Gather, Target = D2Fixtures.Person(sc.Npc.Id, sc.BushNode), Scope = DivineScope.Individual }).Ok);
                var resp = D2Fixtures.StepToResponse(sc);
                var p = sc.Probe.Of<CommandPerceived>().Single();
                perceived++;
                if (p.Mismatch) mismatch++;
                if (p.DecodedAct != DivineAct.Gather) actDiffers++;
                if (resp?.Response == CommandResponse.Misread) misread++;
                if (resp?.Response == CommandResponse.Followed) Assert.Fail("a fully misread message cannot be followed as sent");
            }
            TestContext.Progress.WriteLine("T5 noise 1000: perceived " + perceived + ", mismatch " + mismatch + ", act differs " + actDiffers + ", Misread responses " + misread);
            Assert.AreEqual(perceived, mismatch, "every message is misread at noise 1000");
            Assert.Greater(actDiffers, 0);
            Assert.Greater(misread, 0, "following a misread act is reported Misread");
        }

        // ---------------------------------------------------------------- D2-T10

        [Test]
        public void Scope_ResolvedAtApply()
        {
            var sim = D2Fixtures.Game(31);
            int c = LearningFixtures.Glade(sim, 8);
            var a = LearningFixtures.Npc(sim, c, InstinctSet.Default, LearningMode.On);
            var b = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, c, 0, 1), InstinctSet.Default, LearningMode.On);
            var dead = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, c, 1, 1), InstinctSet.Default, LearningMode.On);
            var far = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, c, 3, 8), InstinctSet.Default, LearningMode.On);
            foreach (var n in new[] { a, b, dead }) n.Household = 77; // test-only: one household (M4 is off here)
            var probe = new DivineProbe(sim);
            PopulationModule.Kill(sim.State, new EventBuffer(), dead, DeathCause.Violence);
            Assert.IsTrue(sim.Submit(new IssueCommand { God = 1, Act = DivineAct.MoveTo, Target = new DivineTarget(DivineTargetKind.Household, 77, c), Scope = DivineScope.Household }).Ok);
            // a location command submitted now; the far person walks in (test-only move) before the tick that applies it
            Assert.IsTrue(sim.Submit(new IssueCommand { God = 1, Act = DivineAct.MoveTo, Target = new DivineTarget(DivineTargetKind.Location, 0, c, 2), Scope = DivineScope.Location }).Ok);
            far.Node = LearningFixtures.Step(sim, c, 3, 2);
            probe.Step(1);
            var issued = probe.Of<DivineCommandIssued>();
            Assert.AreEqual(2, issued.Count);
            Assert.AreEqual(2, issued[0].Recipients); Assert.AreEqual(1, issued[0].NotReached, "the dead member is reported not reached");
            var hh = sim.State.Divinity.Messages.Items[0];
            CollectionAssert.AreEqual(new[] { a.Id, b.Id, dead.Id }, hh.Recipients.Select(r => r.Npc).ToArray(), "ascending NpcId");
            Assert.AreEqual(RecipientState.NotReached, hh.Recipients[2].State);
            var loc = sim.State.Divinity.Messages.Items[1];
            CollectionAssert.AreEqual(new[] { a.Id, b.Id, far.Id }, loc.Recipients.Select(r => r.Npc).ToArray(), "resolved at the apply tick: the person who walked in is a recipient");
            var perceived = probe.Of<CommandPerceived>().Select(e => e.Npc).ToList();
            CollectionAssert.DoesNotContain(perceived, dead.Id);
            // Group needs an id list the frozen command type does not carry
            Assert.AreEqual(DivineReasons.BadTarget, sim.Submit(new IssueCommand { God = 1, Act = DivineAct.MoveTo, Target = D2Fixtures.Person(a.Id, c), Scope = DivineScope.Group }).Reason);
        }

        // ---------------------------------------------------------------- D2-T12

        [Test]
        public void ActUnavailable_NeverFaked()
        {
            var sc = D2Fixtures.Glade(5, InstinctSet.Default, 300);
            long before = sc.Sim.State.Divinity.Gods.Of(1).Mythic;
            ulong h = sc.Sim.StateHash();
            foreach (var act in new[] { DivineAct.Work, DivineAct.Construct, DivineAct.Trade, DivineAct.Attack, DivineAct.Teach, DivineAct.InstitutionalAction })
            {
                Assert.AreEqual(DivineReasons.ActUnavailable, sc.Sim.Submit(new IssueCommand { God = 1, Act = act, Target = D2Fixtures.Person(sc.Npc.Id), Scope = DivineScope.Individual }).Reason, act.ToString());
                Assert.AreEqual(DivineReasons.ActUnavailable, sc.Sim.Submit(new IssueCompulsion { God = 1, Act = act, Target = D2Fixtures.Person(sc.Npc.Id) }).Reason, act.ToString());
            }
            Assert.AreEqual(h, sc.Sim.StateHash(), "nothing queued, nothing changed");
            sc.Probe.Step(1);
            Assert.AreEqual(before, sc.Sim.State.Divinity.Gods.Of(1).Mythic, "no Mythic spent");
            Assert.IsNull(sc.Sim.State.Divinity.Messages);
        }

        // ---------------------------------------------------------------- D2-T15 (P2 part)

        [Test]
        public void D2_Neutral_NoCommands()
        {
            var t = new SimTuning();
            t.Divinity.Enabled = 1;
            var sim = GodSim.Create(new GameSetup(3, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 6, Victory = VictorySpec.SandboxOnly() }, t);
            GodDirector.Attach(sim);
            for (int i = 0; i < 2 * D2Fixtures.Day; i++) sim.Step();
            Assert.IsNull(sim.State.Divinity.Messages, "no instruction registry");
            Assert.IsTrue(sim.State.Population.Npcs.All(n => n.Mind == null || n.Mind.Divine == null), "no divine mind written");
            Assert.IsTrue(sim.State.Population.Npcs.All(n => n.Mind == null || n.Mind.Last.Divine == 0), "term 0");
        }
    }
}
