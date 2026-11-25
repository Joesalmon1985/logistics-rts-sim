using System.Linq;
using God.Sim;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>Save, load and continue (P10): the loaded game must run exactly like the uninterrupted one.</summary>
    public class SaveTests
    {
        private const int Before = 2400, After = 1500;

        /// <summary>Six chain bots build for a while, then war, units and a Citadel upgrade are set going, and a command is left queued.</summary>
        private static GodSim BusyGame(ulong seed)
        {
            var sim = GodSim.Create(new GameSetup(seed));
            var bots = GodStartAll(sim);
            for (int i = 0; i < Before; i++)
            {
                sim.Step();
                if (i % 20 == 0) foreach (var b in bots) b.Think();
                sim.DrainEvents();
            }
            var s = sim.State; var t = s.Territory; var e = s.Economy;
            Assert.IsTrue(sim.Submit(new DeclareWar(Faction.F0, Faction.F1)).Ok);
            sim.Step();
            foreach (var kv in new[] { (Faction.F0, 0), (Faction.F1, 0) })
            {
                var hq = t.HqsOf(kv.Item1)[0];
                for (int k = 0; k < 3; k++) sim.Submit(k == 2 ? DevSpawnUnit.WithKit(kv.Item1, new Kit(Ware.Bow, Ware.None, Ware.Helmet), hq.Node) : new DevSpawnUnit(kv.Item1, UnitKind.Infantry, hq.Node));
            }
            var hq3 = t.HqsOf(Faction.F3)[0];
            var rec = e.Find(hq3.Id);
            foreach (var w in new[] { Ware.Plank, Ware.Stone, Ware.Brick, Ware.Iron }) rec.Stock[(int)w] = 20;
            Assert.IsTrue(sim.Submit(new UpgradeToCitadel(Faction.F3, hq3.Id)).Ok);
            for (int i = 0; i < 40; i++) { sim.Step(); sim.DrainEvents(); }
            // Left in the queue at save time.
            Assert.IsTrue(sim.Submit(new DeclareWar(Faction.F2, Faction.F4)).Ok);
            return sim;
        }

        private static ChainBot[] GodStartAll(GodSim sim) => sim.State.Setup.Factions.Select(f => new ChainBot(sim, f)).ToArray();

        private static void Continue(GodSim sim)
        {
            for (int i = 0; i < After; i++)
            {
                if (i == 100) sim.Submit(new OfferPeace(Faction.F1, Faction.F0));
                if (i == 300) sim.Submit(new OfferPeace(Faction.F0, Faction.F1));
                sim.Step();
                sim.DrainEvents();
            }
        }

        [TestCase(1UL)]
        [TestCase(20261124UL)]
        [TestCase(777UL)]
        public void SaveLoadContinue_GivesTheSameHash_AsAnUninterruptedRun(ulong seed)
        {
            var straight = BusyGame(seed);
            Assert.Greater(straight.State.Economy.Walkers.Count, 0, "walkers are on the roads");
            Assert.Greater(straight.State.Military.Units.Count, 0, "units are out");
            Assert.IsTrue(straight.State.Economy.Buildings.Any(b => b.Upgrading), "a Citadel is under way");
            Assert.AreEqual(1, straight.PendingCommands.Count, "a command is in flight");

            var other = BusyGame(seed);
            var json = SaveFile.From(other, "2026-11-25T10:00:00Z").ToJson();
            var loaded = SaveFile.FromJson(json).ToSim();
            Assert.AreEqual(straight.StateHash(), loaded.StateHash(), "same hash straight after the load");
            Assert.AreEqual(1, loaded.PendingCommands.Count);

            Continue(straight);
            Continue(loaded);
            Assert.AreEqual(straight.StateHash(), loaded.StateHash(), "same hash after the continued run");
            Assert.AreEqual(straight.State.Tick, loaded.State.Tick);
            Assert.AreEqual(SaveFile.From(straight).ToJson(), SaveFile.From(loaded).ToJson(), "the whole state is equal, not just the hash");
            Assert.IsEmpty(SimInvariants.Check(loaded.State));
            Assert.IsTrue(straight.State.Factions.AtWar(Faction.F2, Faction.F4), "the queued command applied");
        }

        [Test]
        public void SaveLoad_WithEnvoysOnTheRoad_GivesTheSameHash()
        {
            GodSim Game()
            {
                var sim = GodSim.Create(5UL);
                for (int k = 1; k < 6; k++)
                {
                    var from = (Faction)k; var to = (Faction)((k + 1) % 6);
                    Assert.IsTrue(sim.Submit(new OfferTrade(from, to, new[] { new WareCount(Ware.Log, 2) }, new[] { new WareCount(Ware.Clay, 1) })).Ok);
                }
                for (int i = 0; i < 150; i++) { sim.Step(); sim.DrainEvents(); }
                return sim;
            }
            var straight = Game();
            Assert.Greater(straight.State.Trade.ActiveEnvoys.Count(), 0, "envoys are walking");
            var loaded = SaveFile.FromJson(SaveFile.From(Game(), "x").ToJson()).ToSim();
            Assert.IsNotNull(loaded.State.Trade.Judge, "the judge is set again after a load");
            Assert.AreEqual(straight.StateHash(), loaded.StateHash());
            for (int i = 0; i < 1500; i++) { straight.Step(); straight.DrainEvents(); loaded.Step(); loaded.DrainEvents(); }
            Assert.AreEqual(straight.StateHash(), loaded.StateHash(), "same hash after the envoys finished their trips");
            Assert.IsEmpty(SimInvariants.Check(loaded.State));
        }

        [Test]
        public void ASecondSave_OfALoadedGame_IsIdentical()
        {
            var sim = BusyGame(5UL);
            var a = SaveFile.From(sim, "x").ToJson();
            var b = SaveFile.From(SaveFile.FromJson(a).ToSim(), "x").ToJson();
            Assert.AreEqual(a, b);
        }

        [Test]
        public void Header_RoundTrips()
        {
            var sim = GodSim.Create(GameSetup.Watch(99UL));
            sim.Step(250);
            var f = SaveFile.From(sim, "2026-11-25T10:00:00Z");
            var h = SaveFile.TryReadHeader(f.ToJson());
            Assert.NotNull(h);
            Assert.AreEqual(99UL, h.Seed);
            Assert.AreEqual(250L, h.Tick);
            Assert.AreEqual(GameSetup.NoHuman, h.Human);
            Assert.AreEqual(6, h.FactionCount);
            Assert.AreEqual("2026-11-25T10:00:00Z", h.SavedAt);
            StringAssert.StartsWith("Amber ", h.VpLine);
            Assert.IsFalse(h.GameOver);
        }

        private class Extra : ISaveExtra
        {
            public string Data = "hello";
            public string SaveKey => "test.extra";
            public string SaveState() => Data;
            public void LoadState(string d) { Data = d; }
        }

        [Test]
        public void Extras_AreStoredByKey()
        {
            var sim = GodSim.Create(3UL);
            var f = SaveFile.FromJson(SaveFile.From(sim, null, new ISaveExtra[] { new Extra { Data = "abc" } }).ToJson());
            Assert.AreEqual("abc", f.Extras["test.extra"]);
        }

        [TestCase("")]
        [TestCase("not json at all")]
        [TestCase("{}")]
        [TestCase("{\"Version\":1}")]
        [TestCase("{\"Version\":99,\"State\":{}}")]
        [TestCase("[1,2,3]")]
        public void CorruptFiles_AreRefusedCleanly(string text)
        {
            Assert.Throws<SaveException>(() => SaveFile.FromJson(text));
            Assert.IsNull(SaveFile.TryReadHeader(text));
        }

        [Test]
        public void ATruncatedOrTamperedSave_IsRefused()
        {
            var json = SaveFile.From(GodSim.Create(4UL)).ToJson();
            Assert.Throws<SaveException>(() => SaveFile.FromJson(json.Substring(0, json.Length / 2)));
            Assert.Throws<SaveException>(() => SaveFile.FromJson(json.Replace("\"NodeBuilding\":[", "\"NodeBuilding\":[7,")));
            Assert.Throws<SaveException>(() => SaveFile.FromJson(json.Replace("\"Pending\":[]", "\"Pending\":[{\"Type\":\"System.Object\",\"Json\":\"{}\"}]")).ToSim());
        }
    }
}
