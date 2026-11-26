using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>M0: victory conditions (STATE_DESIGN). The stage 3 rule is unchanged; a sandbox never ends.</summary>
    public class VictoryConditionTests
    {
        [Test]
        public void APlainSetup_KeepsTheStage3Rules()
        {
            var v = new GameSetup(1UL).Victory;
            Assert.AreEqual(2, v.Count);
            Assert.AreEqual(VictoryKind.VictoryPoints, v[0].Kind); Assert.AreEqual(10, v[0].Target);
            Assert.AreEqual(VictoryKind.LastStanding, v[1].Kind);
            Assert.IsFalse(VictoryConditions.IsSandbox(new GameSetup(1UL)));
            Assert.IsTrue(VictoryConditions.IsSandbox(new GameSetup(1UL) { Victory = VictorySpec.SandboxOnly() }));
        }

        [Test]
        public void VictoryPoints_MostVpWins_TiesToTheLowerId()
        {
            var sim = GodSim.Create(GameSetup.Watch(6));
            var s = sim.State;
            var alive = FactionRules.Alive(s);
            Assert.AreEqual(0, new VictoryPointsCondition(1).Evaluate(new VictoryInputs(s, alive)).Winner, "everyone has 1 VP: the lowest id");
            Assert.IsFalse(new VictoryPointsCondition(2).Evaluate(new VictoryInputs(s, alive)).Ended, "nobody has 2 VP");
            s.Territory.HqsOf(Faction.F3)[0].Citadel = true; s.Territory.HqsOf(Faction.F4)[0].Citadel = true;
            var r = new VictoryPointsCondition(2).Evaluate(new VictoryInputs(s, alive));
            Assert.IsTrue(r.Ended); Assert.AreEqual(3, r.Winner); Assert.AreEqual(GameEndReason.VictoryPoints, r.Reason);
            Assert.IsFalse(new SandboxCondition().Evaluate(new VictoryInputs(s, alive)).Ended);
            Assert.IsFalse(new LastStandingCondition().Evaluate(new VictoryInputs(s, alive)).Ended);
        }

        [Test]
        public void LastStanding_EndsTheStage3Game_ButNeverASandbox()
        {
            foreach (bool sandbox in new[] { false, true })
            {
                var setup = new GameSetup(4UL, GameSetup.NoHuman, 2);
                if (sandbox) setup.Victory = VictorySpec.SandboxOnly();
                var sim = GodSim.Create(setup);
                var ev = new EventBuffer();
                FactionRules.Eliminate(sim.State, Faction.F1, ev);
                sim.Step(5);
                var over = sim.DrainEvents().OfType<GameOver>().ToList();
                if (sandbox)
                {
                    Assert.IsFalse(sim.State.IsGameOver, "a sandbox never ends, even with one faction left");
                    Assert.IsEmpty(over);
                    Assert.AreEqual(5, sim.State.Tick, "and the sim keeps ticking");
                }
                else
                {
                    Assert.IsTrue(sim.State.IsGameOver);
                    Assert.AreEqual(GameEndReason.LastStanding, sim.State.Factions.EndReason);
                    Assert.AreEqual(0, sim.State.Factions.Winner);
                    Assert.AreEqual(1, over.Count);
                }
            }
        }

        [Test]
        public void TheVictoryListOrder_DecidesWhichConditionEndsTheGame()
        {
            // Both conditions would end it at once; the first in the list names the reason (VP before last standing, as in stage 3).
            var setup = new GameSetup(4UL, GameSetup.NoHuman, 2) { Victory = new System.Collections.Generic.List<VictorySpec> { VictorySpec.VictoryPoints(1), VictorySpec.LastStanding() } };
            var sim = GodSim.Create(setup);
            FactionRules.Eliminate(sim.State, Faction.F1, new EventBuffer());
            sim.Step();
            Assert.AreEqual(GameEndReason.VictoryPoints, sim.State.Factions.EndReason);
            setup = new GameSetup(4UL, GameSetup.NoHuman, 2) { Victory = new System.Collections.Generic.List<VictorySpec> { VictorySpec.LastStanding(), VictorySpec.VictoryPoints(1) } };
            sim = GodSim.Create(setup);
            FactionRules.Eliminate(sim.State, Faction.F1, new EventBuffer());
            sim.Step();
            Assert.AreEqual(GameEndReason.LastStanding, sim.State.Factions.EndReason);
        }
    }
}
