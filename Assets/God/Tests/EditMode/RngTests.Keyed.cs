using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>M0.11: keyed random streams (<c>Rng.Fork(label, id)</c>): by entity and purpose, never by order of use.</summary>
    public partial class RngTests
    {
        [Test]
        public void ForkLabelId_IsStable_AndDoesNotAdvanceTheParent()
        {
            var root = new Rng(7).Fork("sim");
            ulong before = root.State;
            var a = root.Fork("npc", 12); var b = root.Fork("npc", 12);
            Assert.AreEqual(before, root.State, "forking never advances the parent");
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextULong(), b.NextULong());
            var c = root.Fork("duel", 3, 400); var d = root.Fork("duel", 3, 400);
            for (int i = 0; i < 100; i++) Assert.AreEqual(c.NextULong(), d.NextULong());
        }

        [Test]
        public void ForkLabelId_DiffersByLabelIdAndParent()
        {
            var root = new Rng(7).Fork("sim");
            var firsts = new HashSet<ulong>
            {
                root.Fork("npc", 1).NextULong(), root.Fork("npc", 2).NextULong(), root.Fork("inst", 1).NextULong(),
                root.Fork("npc").NextULong(), root.Fork("duel", 1, 2).NextULong(), root.Fork("duel", 2, 1).NextULong(),
                new Rng(8).Fork("sim").Fork("npc", 1).NextULong(),
            };
            Assert.AreEqual(7, firsts.Count, "every key gives its own stream");
            Assert.AreNotEqual(root.Fork("npc", 1).NextULong(), root.Fork("npc", 1L << 32).NextULong(), "high bits of the id count");
        }

        [Test]
        public void AddingAnEntity_DoesNotShiftOtherStreams()
        {
            var root = new Rng(99).Fork("sim");
            // World A: entities 1..10 each draw five numbers.
            var a = new Dictionary<int, List<int>>();
            for (int id = 1; id <= 10; id++) { var r = root.Fork("npc", id).Fork("forage"); a[id] = Enumerable.Range(0, 5).Select(_ => r.NextInt(1000)).ToList(); }
            // World B: entity 11 exists too and draws first and in between; the others' draws are unchanged.
            var b = new Dictionary<int, List<int>>();
            var extra = root.Fork("npc", 11).Fork("forage");
            for (int id = 1; id <= 10; id++)
            {
                extra.NextInt(1000);
                var r = root.Fork("npc", id).Fork("forage");
                b[id] = Enumerable.Range(0, 5).Select(_ => r.NextInt(1000)).ToList();
            }
            for (int id = 1; id <= 10; id++) CollectionAssert.AreEqual(a[id], b[id], "entity " + id);
        }

        [Test]
        public void DiceRolls_AreKeyedByTheRollNumber_AndTheRootNeverMoves()
        {
            var sim = GodSim.Create(GameSetup.Watch(3));
            ulong root = sim.State.Rng.State;
            var rolls = new List<DiceRolled>();
            for (int i = 0; i < TerritoryTuning.DiceIntervalTicks * 30; i++)
            {
                sim.Step();
                rolls.AddRange(sim.DrainEvents().OfType<DiceRolled>());
            }
            Assert.AreEqual(30, rolls.Count);
            Assert.AreEqual(root, sim.State.Rng.State, "nothing draws from the root stream directly");
            for (int k = 0; k < rolls.Count; k++)
            {
                var r = new Rng(root).Fork("dice", k);
                Assert.AreEqual(r.NextRange(1, 7), rolls[k].Die1, "roll " + k);
                Assert.AreEqual(r.NextRange(1, 7), rolls[k].Die2, "roll " + k);
            }
        }

        [Test]
        public void AFight_DoesNotShiftTheDice()
        {
            // Two games on one island: in one a duel happens, in the other nobody fights. The dice are the same.
            List<int> Dice(bool fight)
            {
                var sim = GodSim.Create(GameSetup.Watch(5));
                if (fight)
                {
                    sim.Submit(new DeclareWar(Faction.F0, Faction.F1));
                    var g = sim.State.Territory.Grid; var hq = sim.State.Territory.HqsOf(Faction.F0)[0];
                    sim.Submit(DevSpawnUnit.WithKit(Faction.F0, new Kit(Ware.Sword), hq.Node));
                    sim.Submit(DevSpawnUnit.WithKit(Faction.F1, new Kit(Ware.Axe), hq.Node));
                }
                var l = new List<int>();
                for (int i = 0; i < 1200; i++) { sim.Step(); foreach (var e in sim.DrainEvents().OfType<DiceRolled>()) l.Add(e.Total); }
                return l;
            }
            CollectionAssert.AreEqual(Dice(false), Dice(true));
        }
    }
}
