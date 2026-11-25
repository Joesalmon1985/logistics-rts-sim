using System.Linq;
using God.Sim;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>Stage 3 tutorial: the fixed island, the AI's truce with the human, the step list and its progress.</summary>
    public class TutorialTests
    {
        private static GodSim NewTutorial()
        {
            var sim = GodSim.Create(TutorialIsland.Setup());
            return sim;
        }

        private static void Settle(TutorialGuide g, GodState s) { for (int i = 0; i < 30; i++) g.Update(s, Faction.F0); }

        private static int FakeBuilding(GodState s, Faction f, string kind, bool built = true)
        {
            var e = s.Economy;
            int id = 9000 + e.Buildings.Count;
            e.Buildings.Add(new EcoBuilding { Id = id, Faction = f, Kind = kind, Built = built });
            return id;
        }

        // ---------------- the island ----------------

        [Test]
        public void TutorialSeed_IsTheFirstSeedThatQualifies()
        {
            Assert.AreEqual(TutorialIsland.Seed, TutorialIsland.Find(1));
            Assert.IsTrue(TutorialIsland.Qualifies(TutorialIsland.Seed));
        }

        [Test]
        public void TutorialIsland_FirstAreaHasForestHillsMountain_AndASecondHqReachesFields()
        {
            var sim = NewTutorial();
            var have = TutorialIsland.TerrainsInArea(sim.State, Faction.F0);
            Assert.IsTrue(have.Contains(Terrain.Forest) && have.Contains(Terrain.Hills) && have.Contains(Terrain.Mountain));
            int corner = TutorialIsland.SecondHqCornerTouching(sim, Faction.F0, Terrain.Fields);
            Assert.GreaterOrEqual(corner, 0);
            Assert.IsTrue(sim.Check(new PlaceHq(Faction.F0, corner)).Ok);
        }

        [Test]
        public void Setup_IsATutorialWithAmberHuman_AndTheFlagSurvivesASave()
        {
            var sim = NewTutorial();
            Assert.IsTrue(sim.State.Setup.Tutorial);
            Assert.AreEqual(Faction.F0, sim.State.Setup.HumanFaction);
            var back = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            Assert.IsTrue(back.State.Setup.Tutorial, "Tutorial is part of the saved setup");
            Assert.IsFalse(GodSim.Create(new GameSetup(5, 0)).State.Setup.Tutorial);
        }

        [Test]
        public void Ai_NeverDeclaresWarOnTheHuman_InATutorial()
        {
            var sim = NewTutorial();
            var s = sim.State;
            var brains = s.Setup.Factions.Where(f => f != s.Setup.HumanFaction).ToList();
            AiDirector.Attach(sim, brains, null, null);
            int warsOnHuman = 0, wars = 0;
            while (!s.IsGameOver && s.Tick < 24000)
            {
                sim.Step();
                foreach (var ev in sim.DrainEvents())
                    if (ev is WarDeclared w) { wars++; if (w.Against == Faction.F0 || w.By == Faction.F0) warsOnHuman++; }
            }
            System.Console.WriteLine("tutorial AI game: " + wars + " wars among the AIs, " + warsOnHuman + " on the human");
            Assert.AreEqual(0, warsOnHuman);
        }

        // ---------------- the steps ----------------

        [Test]
        public void Steps_AreFifteen_WithUniqueIdsAndShortText()
        {
            var all = TutorialSteps.All;
            Assert.AreEqual(15, all.Count);
            Assert.AreEqual(15, all.Select(x => x.Id).Distinct().Count());
            foreach (var st in all)
            {
                Assert.IsFalse(string.IsNullOrEmpty(st.Title), st.Id);
                Assert.IsFalse(string.IsNullOrEmpty(st.Goal), st.Id);
                Assert.That(st.Lines.Length, Is.InRange(2, 4), st.Id);
            }
        }

        [Test]
        public void Goals_AreAllUnmetAtTheStart()
        {
            var s = NewTutorial().State;
            var ctx = new TutorialContext(s, Faction.F0);
            foreach (var st in TutorialSteps.All) if (st.Done != null) Assert.IsFalse(st.Done(ctx), st.Id);
        }

        [Test]
        public void Welcome_NeedsNext_ThenWoodcutterWaits()
        {
            var s = NewTutorial().State;
            var g = new TutorialGuide { HoldTicks = 0 };
            Assert.AreEqual("welcome", g.Step.Id);
            Assert.IsTrue(g.CanContinue, "no goal: Next works at once");
            Settle(g, s);
            Assert.AreEqual(0, g.Index, "nothing moves it on but the player");
            g.Skip();
            Assert.AreEqual("woodcutter", g.Step.Id);
            Assert.IsFalse(g.CanContinue, "a goal is waiting: the button reads Skip step");
        }

        [Test]
        public void PlacingAWoodcutter_AdvancesStepTwo_AndItsRoadTheNext()
        {
            var sim = NewTutorial(); var s = sim.State;
            var g = new TutorialGuide { HoldTicks = 0 };
            g.Skip();
            Assert.AreEqual("woodcutter", g.Step.Id);
            int id = new EconomyBot(sim, Faction.F0).PlaceAndConnect(BuildingKinds.Woodcutter);
            Assert.AreNotEqual(0, id);
            sim.Step();
            g.Update(s, Faction.F0);
            Assert.AreEqual("road", g.Step.Id, "the woodcutter is placed");
            g.Update(s, Faction.F0);
            Assert.AreEqual("dice", g.Step.Id, "the bot joined it with a road");
            s.Economy.Find(id).Built = true; s.Economy.Find(id).Produced = 1;
            g.Update(s, Faction.F0);
            Assert.AreEqual("stone-clay", g.Step.Id, "a log came home");
        }

        [Test]
        public void GoalTicksOff_ThenHoldsBeforeTheNextCard()
        {
            var sim = NewTutorial(); var s = sim.State;
            var g = new TutorialGuide { HoldTicks = 15 };
            g.Skip();
            FakeBuilding(s, Faction.F0, BuildingKinds.Woodcutter);
            g.Update(s, Faction.F0);
            Assert.IsTrue(g.GoalMet);
            Assert.AreEqual("woodcutter", g.Step.Id, "the tick shows for a moment");
            Assert.IsTrue(g.CanContinue, "and Next works meanwhile");
            sim.Step(16);
            g.Update(s, Faction.F0);
            Assert.AreEqual("road", g.Step.Id);
            Assert.IsFalse(g.GoalMet);
        }

        [Test]
        public void EachGoal_ReadsItsOwnState()
        {
            var sim = NewTutorial(); var s = sim.State;
            var ctx = new TutorialContext(s, Faction.F0);
            TutorialStep S(string id) => TutorialSteps.All.First(x => x.Id == id);
            FakeBuilding(s, Faction.F1, BuildingKinds.Quarry);
            FakeBuilding(s, Faction.F0, BuildingKinds.Quarry);
            Assert.IsFalse(S("stone-clay").Done(ctx), "needs the clay pit too");
            FakeBuilding(s, Faction.F0, BuildingKinds.ClayPit, false);
            Assert.IsTrue(S("stone-clay").Done(ctx), "a site counts");
            FakeBuilding(s, Faction.F0, BuildingKinds.Farm); FakeBuilding(s, Faction.F0, BuildingKinds.Mine);
            Assert.IsFalse(S("farm-mine-bakery").Done(ctx));
            FakeBuilding(s, Faction.F0, BuildingKinds.Bakery);
            Assert.IsTrue(S("farm-mine-bakery").Done(ctx));
            FakeBuilding(s, Faction.F0, BuildingKinds.Smelter); FakeBuilding(s, Faction.F0, BuildingKinds.Axesmith);
            Assert.IsTrue(S("smelter-axesmith").Done(ctx));
            Assert.IsFalse(S("upgrade").Done(ctx));
            s.Economy.Buildings.First(b => b.Kind == BuildingKinds.Mine).Level = 2;
            Assert.IsTrue(S("upgrade").Done(ctx));
            Assert.IsFalse(S("war").Done(ctx));
            sim.Submit(new DeclareWar(Faction.F0, Faction.F1)); sim.Step();
            Assert.IsFalse(S("war").Done(ctx), "war alone is not enough: a target must be marked");
            s.Military.MarkedBy[(int)Faction.F0].Add(1234);
            Assert.IsTrue(S("war").Done(ctx));
            Assert.IsFalse(S("guards").Done(ctx));
            s.Military.Defends.Add(new DefendFlag { Id = 1, Faction = Faction.F0, Guards = 2 });
            Assert.IsTrue(S("guards").Done(ctx));
            Assert.IsFalse(S("trade").Done(ctx));
            s.Trade.History.Add(new TradeRecord { From = Faction.F2, To = Faction.F0, Outcome = TradeOutcome.Refused });
            Assert.IsFalse(S("trade").Done(ctx), "a refusal is not a trade");
            s.Trade.History.Add(new TradeRecord { From = Faction.F2, To = Faction.F0, Outcome = TradeOutcome.Accepted });
            Assert.IsTrue(S("trade").Done(ctx));
            Assert.IsFalse(S("citadel").Done(ctx));
            s.Territory.HqsOf(Faction.F0)[0].Citadel = true;
            Assert.IsTrue(S("citadel").Done(ctx));
        }

        [Test]
        public void Skip_Hide_Show_AndTheEnd()
        {
            var s = NewTutorial().State;
            var g = new TutorialGuide { HoldTicks = 0 };
            Assert.IsTrue(g.Visible);
            g.Hide();
            Assert.IsFalse(g.Visible);
            FakeBuilding(s, Faction.F0, BuildingKinds.Woodcutter);
            g.Skip();
            g.Update(s, Faction.F0);
            Assert.AreEqual(1, g.Index, "no progress while the cards are hidden");
            g.Show();
            Assert.IsTrue(g.Visible);
            g.Update(s, Faction.F0);
            Assert.AreEqual(2, g.Index, "back on: the woodcutter step is already done");
            for (int i = 0; i < 40; i++) g.Skip();
            Assert.IsTrue(g.Finished);
            Assert.IsFalse(g.Visible);
            Assert.IsNull(g.Step);
        }

        [Test]
        public void Guide_StateRoundTrips()
        {
            var g = new TutorialGuide();
            g.Skip(); g.Skip(); g.Skip(); g.Hide();
            var h = new TutorialGuide();
            h.LoadState(g.SaveState());
            Assert.AreEqual(3, h.Index);
            Assert.IsTrue(h.Hidden);
            h.LoadState("junk");
            Assert.AreEqual(3, h.Index, "junk is ignored");
        }
    }
}
