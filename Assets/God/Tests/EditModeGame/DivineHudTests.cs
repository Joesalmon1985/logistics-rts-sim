using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Sim;
using God.Sim.Divinity;
using God.Sim.Population;
using God.Sim.Policy;
using God.Sim.Tests;
using NUnit.Framework;
using UnityEngine;

namespace God.Game.Tests
{
    /// <summary>
    /// U2a EditModeGame tests: the pure parts of the divine HUD (alert aggregation, the catalogue, the factory, the event feed read by name,
    /// the cues) and the view-only rules for the new HUD types (H-UI-4, DP10's UI half, R6/R47).
    /// </summary>
    public class DivineHudTests
    {
        // ---------------------------------------------------------------- view-only rules

        /// <summary>The new HUD-side types that must read no state and name no command (the feed and host are sim-facing adapters and are not listed).</summary>
        private static readonly Type[] NewHudTypes =
        {
            typeof(DivineCatalog), typeof(DivineCues), typeof(AlertAggregator), typeof(CommandmentText), typeof(CommandmentTemplateList),
            typeof(PowerSlot), typeof(GhostState), typeof(DivineSendResult), typeof(InstructionChip),
        };

        [Test]
        public void Hud_ReadsOnlyThroughViews_IncludingDivine()
        {
            var forbidden = new HashSet<Type>(PolicyTests.StateTypes);
            var bad = new List<string>();
            foreach (var t in HudViewOnlyTests.HudTypes.Concat(NewHudTypes))
                foreach (var r in TypeReferenceScanner.Forbidden(t, forbidden)) bad.Add(r.ToString());
            Assert.IsEmpty(bad.Distinct(), "HUD types read true state:\n" + string.Join("\n", bad.Distinct()));
            foreach (var t in NewHudTypes)
                foreach (var x in TypeReferenceScanner.TypesOf(t))
                    Assert.IsFalse(typeof(DivineCommand).IsAssignableFrom(x), t.Name + " names the divine command " + x.Name + " (only the factory does)");
        }

        [Test]
        public void DivineCommandsTheHudCanSend_AreWhatAGodPolicyMaySend()
        {
            var sent = TypeReferenceScanner.TypesOf(typeof(DivineCommandFactory)).Where(x => typeof(DivineCommand).IsAssignableFrom(x) && !x.IsAbstract).ToList();
            var policy = DivineCommandSet.Types();
            var config = sent.Where(DivineCommandSet.IsConfiguration).ToList();
            var actions = sent.Where(x => !DivineCommandSet.IsConfiguration(x)).ToList();
            foreach (var a in actions) CollectionAssert.Contains(policy, a, a.Name + " is not in the god policy's command set");
            Assert.AreEqual(new[] { typeof(SetMythicMode) }, config.ToArray(), "the only configuration command the HUD sends is the sandbox Mythic mode");
            var missing = policy.Where(p => !actions.Contains(p)).Select(p => p.Name).ToList();
            TestContext.WriteLine("Not yet sent by the HUD (U2b: possession, avatar; D7: resurrection; HUD gaps): " + string.Join(", ", missing));
            Assert.That(actions.Select(x => x.Name), Is.SupersetOf(new[] { "IssueSuggestion", "IssueCommand", "IssueCompulsion", "CancelCompulsion", "CastMiracle", "RevealCommandment" }));
        }

        [Test]
        public void CommandmentText_IsUsedByNoSimCode()
        {
            string sim = Path.Combine(Application.dataPath, "God", "Sim");
            foreach (var f in Directory.GetFiles(sim, "*.cs", SearchOption.AllDirectories))
                Assert.IsFalse(File.ReadAllText(f).Contains("CommandmentText"), f + " uses CommandmentText (R6/R47: Game layer only)");
        }

        // ---------------------------------------------------------------- factory and builder

        [Test]
        public void Factory_MapsEveryPlayerFlowToTheF1Commands()
        {
            var f = new DivineCommandFactory();
            var person = new DivineRequest { God = 1, Scope = TargetScope.Person, Persons = new[] { 7 }, Node = -1 };
            person.Act = DivineActs.Command; person.Arg = (int)DivineAct.Gather; person.Arg2 = 2400;
            var c = (IssueCommand)f.Create(person);
            Assert.AreEqual(1, c.God); Assert.AreEqual(DivineAct.Gather, c.Act); Assert.AreEqual(new DivineTarget(DivineTargetKind.Npc, 7), c.Target);
            Assert.AreEqual(DivineScope.Individual, c.Scope); Assert.AreEqual(2400, c.ExpireTicks);

            person.Act = DivineActs.Compel; var comp = (IssueCompulsion)f.Create(person);
            Assert.AreEqual(600 > 0, comp.ExpireTicks > 0);
            person.Act = DivineActs.Miracle; person.Arg = (int)MiracleKind.HealInjury; person.Arg2 = 400;
            var m = (CastMiracle)f.Create(person);
            Assert.AreEqual(MiracleKind.HealInjury, m.Kind); Assert.AreEqual(400, m.Magnitude); Assert.AreEqual(DivineTargetKind.Npc, m.Target.Kind);

            var area = new DivineRequest { God = 1, Act = DivineActs.Miracle, Arg = (int)MiracleKind.HealInjury, Arg2 = 400, Scope = TargetScope.Area, Area = new AreaSpec(5, 4), Node = -1 };
            Assert.AreEqual((MiracleKind)((int)MiracleKind.Area + 1), ((CastMiracle)f.Create(area)).Kind, "an area miracle is Area + kind");

            var place = new DivineRequest { God = 1, Act = DivineActs.Miracle, Arg = (int)MiracleKind.CreateResource, Arg2 = 3, Scope = TargetScope.Location, Node = 9 };
            Assert.AreEqual(new DivineTarget(DivineTargetKind.Location, 0, 9, 0), ((CastMiracle)f.Create(place)).Target);

            var group = new DivineRequest { God = 1, Act = DivineActs.Suggest, Arg = (int)DivineAct.Eat, Scope = TargetScope.Group, Persons = new[] { 3, 4 }, Node = -1 };
            Assert.IsNull(f.Create(group), "a group is expanded into individuals by the HUD, never sent as a list");
            var each = PlayerCommandBuilder.Expand(group);
            Assert.AreEqual(2, each.Count); Assert.AreEqual(3, each[0].Persons[0]); Assert.AreEqual(TargetScope.Person, each[1].Scope);
            Assert.IsInstanceOf<IssueSuggestion>(f.Create(each[0]));

            Assert.IsInstanceOf<SetMythicMode>(f.Create(new DivineRequest { God = 1, Act = DivineActs.SetMythicMode, Arg = (int)DivinityMode.Unlimited, Node = -1 }));
            Assert.IsNull(f.Create(new DivineRequest { God = 1, Act = "unknown", Node = -1 }));
        }

        [Test]
        public void Catalog_CostsComeFromTheSimTable_AndDisabledHidesPowers()
        {
            var costs = new MythicCosts();
            Assert.AreEqual(costs.IssueCommand + 2 * costs.CommandPerExtra, DivineCatalog.CostOf(costs, DivineCatalog.Find("command"), 3));
            Assert.AreEqual(costs.IssueCompulsion, DivineCatalog.CostOf(costs, DivineCatalog.Find("compel")));
            Assert.AreEqual(Math.Max(costs.HealInjuryMin, costs.HealInjuryPer100 * DivineCatalog.HealMagnitude / 100), DivineCatalog.CostOf(costs, DivineCatalog.Find("heal")));
            var l = new List<PowerSlot>();
            DivineCatalog.Visible(DivinityMode.Disabled, l);
            CollectionAssert.AreEqual(new[] { "inspect" }, l.Select(s => s.Id), "Disabled hides every power slot");
            DivineCatalog.Visible(DivinityMode.Standard, l);
            foreach (var id in new[] { "suggest", "command", "compel", "heal", "cure", "bless", "protect", "smite", "smite_hut", "smite_source", "create", "destroy" })
                Assert.IsTrue(l.Any(s => s.Id == id), id);
            Assert.IsTrue(DivineCatalog.IsUnavailable(DivineReasons.ActUnavailable));
            Assert.IsFalse(DivineCatalog.IsUnavailable(DivineReasons.MythicShort));
            Assert.AreEqual(DivineCatalog.CompelTicks, DivineCatalog.DefaultExpiry(SlotFamily.Compel, 1200));
            Assert.AreEqual(2400, DivineCatalog.DefaultExpiry(SlotFamily.Command, 1200));
        }

        [Test]
        public void MythicText_And_CommandmentSentence()
        {
            Assert.AreEqual("50", GodHud.MythicText(50_000)); Assert.AreEqual("12.5", GodHud.MythicText(12_500));
            // package D6 adjustment: the picker reads D6's templates; T4 "Protect forests" forbids gathering at a low wood stand
            var rules = System.Linq.Enumerable.First(CommandmentTemplateList.Provisional(), t => t.Id == 4).Rules;
            string s = CommandmentText.Render(rules);
            StringAssert.Contains("must never", s); StringAssert.Contains("gather", s.ToLowerInvariant()); StringAssert.Contains("stock low", s.ToLowerInvariant());
            Assert.AreEqual(1, CommandmentTemplateList.Provisional()[0].Id, "Tier 1 templates first (T1)");
            Assert.AreEqual(s, CommandmentText.Render((CommandmentRule[])rules.Clone()), "pure");
        }

        // ---------------------------------------------------------------- alerts (U2-T9, H-UI-5)

        private const int Tpd = 1200;

        [Test]
        public void Alerts_Aggregate_OverARecordedThreeDayLog()
        {
            var agg = new AlertAggregator(Tpd, id => id == 77);
            var rng = new System.Random(5);
            int trades = 0, deaths = 0;
            for (long t = 0; t < 3L * Tpd; t += 5)
            {
                if (rng.Next(3) == 0) { agg.Add(new AlertInput { Tick = t, Kind = AlertKind.Trade }); trades++; }
                if (rng.Next(20) == 0) { agg.Add(new AlertInput { Tick = t, Kind = AlertKind.Birth }); }
                if (rng.Next(40) == 0) { agg.Add(new AlertInput { Tick = t, Kind = AlertKind.Death, Person = 100 + (int)(t % 30), Cause = (int)DeathCause.Starvation, Area = 2 }); deaths++; }
                agg.Add(new AlertInput { Tick = t, Kind = AlertKind.Trade }); trades++; // plenty of trades
            }
            agg.Add(new AlertInput { Tick = 3L * Tpd - 2, Kind = AlertKind.Death, Person = 77, Cause = (int)DeathCause.OldAge });
            agg.Advance(3L * Tpd + 1);
            Assert.Greater(trades, 100);
            Assert.AreEqual(3, agg.DigestsWritten, "one digest per day, no more");
            var cards = agg.Cards;
            Assert.LessOrEqual(cards.Count, 8, "capped");
            Assert.IsFalse(cards.Any(c => c.Class != AlertClass.Digest && c.Kind == AlertKind.Trade), "no card per trade");
            Assert.AreEqual(1, cards.Count(c => c.Class == AlertClass.Critical && c.Person == 77), "a followed person's death is one individual card");
            var digest = cards.Where(c => c.Class == AlertClass.Digest).ToList();
            StringAssert.Contains("trades", digest[0].Text);
            Assert.IsTrue(cards.Where(c => c.Class == AlertClass.Important).All(c => c.Count >= 1));
            TestContext.WriteLine(string.Join(" | ", cards.Select(c => c.Class + ": " + c.Text)) + " (deaths " + deaths + ")");
        }

        [Test]
        public void Alerts_MapSimEvents()
        {
            Assert.IsTrue(AlertAggregator.Map(new NpcDied { Npc = 4, Cause = DeathCause.Disease, Node = 130 }, 10, out var d));
            Assert.AreEqual(AlertKind.Death, d.Kind); Assert.AreEqual(4, d.Person);
            Assert.IsFalse(AlertAggregator.Map(new Ate { Npc = 1 }, 10, out _), "a meal is no alert");
            Assert.IsTrue(AlertAggregator.Map(new MythicSpent { Amount = 1000, Balance = 0 }, 10, out var m)); Assert.AreEqual(AlertKind.MythicExhausted, m.Kind);
            Assert.IsFalse(AlertAggregator.Map(new MythicSpent { Amount = 1000, Balance = 5000 }, 10, out _));
        }

        // ---------------------------------------------------------------- the feed (events read by name) and the cues

        // synthetic D2 and D3 events: the feed reads them by type name and field name, so these stand in until those branches merge
        private sealed class CommandResponded : SimEvent { public int Intervention, Npc; public string Response; public string Understood; }
        private sealed class CommandPerceived : SimEvent { public int Intervention, Npc; }
        private sealed class CompulsionStarted : SimEvent { public int Intervention, Npc; }
        private sealed class CompulsionBlocked : SimEvent { public int Intervention; public string Reason; }
        private sealed class DivineSuggestionIssued : SimEvent { public int Intervention, Npc; }
        private sealed class DivineCommandIssued : SimEvent { public int Intervention, Npc; }
        private sealed class MiracleCast : SimEvent { public int Intervention, TargetId; public string Kind; }
        private sealed class MiracleEffectApplied : SimEvent { public int Intervention, ThingId; public string ThingKind, Field; public long Before, After; }
        private sealed class ResourceCreated : SimEvent { public int Intervention, Count, Node; }

        [Test]
        public void Feed_ChipShowsTheActualResponses_NeverAGuess()
        {
            var feed = new DivineFeed { God = 1 };
            var chip = feed.Sent("Command Gather", InterventionKind.Command, DivineAct.Gather, 1, 0);
            for (int i = 0; i < 6; i++) feed.Sent("Command Gather", InterventionKind.Command, DivineAct.Gather, 1, 0, chip);
            Assert.IsTrue(chip.Pending); StringAssert.Contains("pending", chip.Text());
            for (int i = 0; i < 7; i++)
            {
                feed.Accept(new MythicSpent { God = 1, Intervention = 10 + i, Kind = InterventionKind.Command, Amount = 4000, Nominal = 4000, Balance = 50_000 - 4000 * (i + 1) });
                feed.Accept(new DivineActionAccepted { God = 1, Intervention = 10 + i, Kind = InterventionKind.Command, Act = DivineAct.Gather, TargetKind = DivineTargetKind.Npc, TargetId = 100 + i, Charged = 4000 });
            }
            Assert.IsFalse(chip.Pending); Assert.AreEqual(7, chip.Accepted); Assert.AreEqual(28_000, chip.Charged);
            for (int i = 0; i < 6; i++) feed.Accept(new CommandPerceived { Intervention = 10 + i, Npc = 100 + i });
            for (int i = 0; i < 5; i++) feed.Accept(new CommandResponded { Intervention = 10 + i, Npc = 100 + i, Response = "Followed" });
            feed.Accept(new CommandResponded { Intervention = 15, Npc = 105, Response = "Refused", Understood = "go gather wood" });
            Assert.AreEqual(5, chip.Followed); Assert.AreEqual(1, chip.Refused); Assert.AreEqual(6, chip.Delivered); Assert.AreEqual(1, chip.NotReached);
            Assert.AreEqual("Command Gather: 6 delivered, 5 followed, 1 refused, 1 not reached", chip.Text());
            var rows = feed.RowsOf(105);
            Assert.IsTrue(rows.Any(r => r.Head == "As understood" && r.Layer == God.Sim.Information.KnowledgeLayer.Belief));
            Assert.IsTrue(rows.Any(r => r.Head == "Issued" && r.Layer == God.Sim.Information.KnowledgeLayer.DivineObservation));
            foreach (var r in feed.RowsOf(105)) Assert.AreNotEqual(0, (int)r.Layer, "every Divine-tab row carries a knowledge layer: " + r.Head);
        }

        [Test]
        public void Feed_BlockedCompulsionShowsTheReason_AndRefusedAtItsTick()
        {
            var feed = new DivineFeed { God = 1 };
            var chip = feed.Sent("Compel Give", InterventionKind.Compulsion, DivineAct.Give, 1, 0);
            feed.Accept(new DivineActionAccepted { God = 1, Intervention = 3, Kind = InterventionKind.Compulsion, Act = DivineAct.Give, TargetKind = DivineTargetKind.Npc, TargetId = 9 });
            feed.Accept(new CompulsionBlocked { Intervention = 3, Reason = "nothing_to_give" });
            StringAssert.Contains("nothing_to_give", chip.Text());
            var chip2 = feed.Sent("Command Eat", InterventionKind.Command, DivineAct.Eat, 1, 0);
            feed.Accept(new CommandRejected { Command = new IssueCommand { God = 1 }, Result = CommandResult.Reject("mythic_short", "no") });
            Assert.AreEqual("mythic_short", chip2.Blocked); Assert.IsFalse(chip2.Pending);
        }

        [Test]
        public void Cues_AreDistinctPerKind_AndExpire()
        {
            var feed = new DivineFeed { God = 1 };
            var cues = new DivineCues();
            feed.Cue += s => cues.Spawn(s, 0f);
            feed.Accept(new DivineSuggestionIssued { Npc = 1 });
            feed.Accept(new DivineCommandIssued { Npc = 2 });
            feed.Accept(new CompulsionStarted { Npc = 3 });
            feed.Accept(new MiracleCast { Kind = "HealInjury", TargetId = 4 });
            feed.Accept(new MiracleCast { Kind = "SmitePerson", TargetId = 5 });
            feed.Accept(new ResourceCreated { Count = 3, Node = 12 });
            foreach (var k in new[] { CueKind.Suggest, CueKind.Command, CueKind.Compel, CueKind.Heal, CueKind.Smite, CueKind.Create })
                Assert.AreEqual(1, cues.CountOf(k), k.ToString());
            var colours = new[] { CueKind.Suggest, CueKind.Command, CueKind.Compel, CueKind.Heal, CueKind.Smite, CueKind.Create }.Select(DivineCues.Colour).Distinct().Count();
            Assert.AreEqual(6, colours, "six distinct colours");
            cues.Prune(DivineCues.Life + 0.1f);
            Assert.AreEqual(0, cues.Live.Count);
            feed.Accept(new MiracleEffectApplied { ThingKind = "Npc", ThingId = 4, Field = "injury", Before = 600, After = 200 });
            StringAssert.Contains("600 -> 200", feed.Notes[0].Text);
            feed.Accept(new ResourceCreated { Count = 5 });
            Assert.AreEqual(8, feed.CreatedUnits, "the ledger count comes from the events");
        }
    }
}
