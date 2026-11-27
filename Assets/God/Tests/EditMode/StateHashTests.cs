using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>M0 (HEADLESS 4.4, G2.9): the hash covers every module, the tuning, the calendar and the pending commands; per-module parts.</summary>
    public class StateHashTests
    {
        private static int[] Differing(ulong[] a, ulong[] b) => Enumerable.Range(0, a.Length).Where(i => a[i] != b[i]).ToArray();
        private static int Part(string name) => GodSim.HashPartNames.IndexOf(name);

        [Test]
        public void TheHash_IsTheFoldOfItsParts()
        {
            var sim = GodSim.Create(3);
            sim.Step(100);
            var parts = sim.StateHashParts();
            Assert.AreEqual(GodSim.HashPartNames.Count, parts.Length);
            CollectionAssert.AreEqual(new[] { "territory", "economy", "military", "factions", "trade", "core", "pending" }, GodSim.HashPartNames.ToArray());
            Assert.AreEqual(GodSim.Fold(parts), sim.StateHash());
            Assert.AreEqual(GodSim.StateHashOf(sim.State), sim.StateHash(), "nothing pending: the state hash alone");
        }

        [Test]
        public void PendingCommands_AreInTheHash()
        {
            var a = GodSim.Create(GameSetup.Watch(4)); var b = GodSim.Create(GameSetup.Watch(4));
            a.Step(10); b.Step(10);
            Assert.AreEqual(a.StateHash(), b.StateHash());
            Assert.IsTrue(a.Submit(new DeclareWar(Faction.F0, Faction.F1)).Ok);
            Assert.AreNotEqual(a.StateHash(), b.StateHash(), "a queued command changes the hash");
            CollectionAssert.AreEqual(new[] { Part("pending") }, Differing(a.StateHashParts(), b.StateHashParts()), "and only its part");
            Assert.IsTrue(b.Submit(new DeclareWar(Faction.F0, Faction.F2)).Ok);
            Assert.AreNotEqual(a.StateHash(), b.StateHash(), "a different command, a different hash");
            var c = GodSim.Create(GameSetup.Watch(4)); c.Step(10);
            Assert.IsTrue(c.Submit(new DeclareWar(Faction.F0, Faction.F1)).Ok);
            Assert.AreEqual(a.StateHash(), c.StateHash(), "the same command, the same hash");
            Assert.AreEqual(GodSim.StateHashOf(a.State), GodSim.StateHashOf(b.State), "the state itself has not changed yet");
        }

        [Test]
        public void ChangingAModule_ChangesItsPart()
        {
            var baseSim = GodSim.Create(GameSetup.Watch(5)); baseSim.Step(5);
            ulong[] Mutated(System.Action<GodState> change)
            {
                var sim = GodSim.Create(GameSetup.Watch(5)); sim.Step(5);
                change(sim.State);
                return sim.StateHashParts();
            }
            var b = baseSim.StateHashParts();
            CollectionAssert.AreEqual(new[] { Part("territory") }, Differing(b, Mutated(s => s.Territory.Piles[0]++)));
            CollectionAssert.AreEqual(new[] { Part("economy") }, Differing(b, Mutated(s => s.Economy.HqsOf(Faction.F0)[0].Stock[(int)Ware.Log]++)));
            CollectionAssert.AreEqual(new[] { Part("military") }, Differing(b, Mutated(s => s.Military.Buildings[0].Health--)));
            CollectionAssert.AreEqual(new[] { Part("factions") }, Differing(b, Mutated(s => s.Factions.War[1] = true)));
            CollectionAssert.AreEqual(new[] { Part("trade") }, Differing(b, Mutated(s => s.Trade.Departed[(int)Ware.Log]++)));
            CollectionAssert.AreEqual(new[] { Part("core") }, Differing(b, Mutated(s => s.Tuning.Military.CritPct++)), "the tuning");
            CollectionAssert.AreEqual(new[] { Part("core") }, Differing(b, Mutated(s => s.Setup.LifeScale = 150)), "the calendar");
            CollectionAssert.AreEqual(new[] { Part("core") }, Differing(b, Mutated(s => s.Setup.Victory = VictorySpec.SandboxOnly())), "the victory list");
            CollectionAssert.AreEqual(new[] { Part("core") }, Differing(b, Mutated(s => s.Rng.State++)), "the root stream");
        }

        [Test]
        public void TwoTunings_GiveTwoGames_AndNeitherLeaksIntoTheOther()
        {
            var t = new SimTuning(); Assert.IsTrue(t.TrySet("Military.CritPct", 0, out _));
            var a = GodSim.Create(GameSetup.Watch(6), t);
            var b = GodSim.Create(GameSetup.Watch(6));
            Assert.AreEqual(0, a.State.Tuning.Military.CritPct);
            Assert.AreEqual(10, b.State.Tuning.Military.CritPct, "the default is untouched");
            Assert.AreNotSame(t, a.State.Tuning, "the game takes a copy");
            t.Military.CritPct = 50;
            Assert.AreEqual(0, a.State.Tuning.Military.CritPct, "changing the caller's object later changes nothing");
            Assert.AreNotEqual(a.StateHash(), b.StateHash());
        }

        [Test]
        public void TuningPaths_AreStrict()
        {
            var t = new SimTuning();
            Assert.IsFalse(t.TrySet("Military.NoSuchThing", 1, out var e1)); StringAssert.Contains("unknown", e1);
            Assert.IsFalse(t.TrySet("Military", 1, out _), "not a leaf");
            Assert.IsFalse(t.TrySet("Seed", 1, out var e2)); StringAssert.Contains("seed", e2);
            Assert.IsTrue(t.TrySet("Military.BigShield.BlockPct", 30, out _));
            Assert.AreEqual(30, t.Military.BigShield.BlockPct);
            Assert.AreEqual(45, t.Military.BigShield.BlockVsRangedPct, "the rest of the struct is kept");
            Assert.IsTrue(t.TryGet("Military.BigShield.BlockPct", out long v)); Assert.AreEqual(30, v);
            Assert.IsTrue(t.TrySet("Military.Helmet.StopsCrits", 0, out _)); Assert.IsFalse(t.Military.Helmet.StopsCrits);
            Assert.IsFalse(t.TrySet("Military.Helmet.StopsCrits", 2, out _));
            CollectionAssert.Contains(SimTuning.Paths(), "Military.CritPct");
            CollectionAssert.Contains(SimTuning.Paths(), "Decay.GrudgeBasePermille");
            // The 22 tunables of MilitaryTuning as leaves: 5 weapons x 7 fields, 5 defence items x 6, 12 plain numbers.
            Assert.AreEqual(5 * 7 + 5 * 6 + 12, SimTuning.Paths().Count(p => p.StartsWith("Military.")));
        }
    }
}
