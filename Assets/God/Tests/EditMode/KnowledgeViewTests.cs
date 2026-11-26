using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Information;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// M1: <see cref="FactionKnowledgeView"/> under <c>FullInformation</c> (INFORMATION_AND_MEDIA_DESIGN §4.8): own books
    /// exact, foreign items only through a channel (I19, Q21), view figures equal to direct reads (M1.7, sim half), and the
    /// debug overlay's rules (I18, Q22) and neutrality (I11).
    /// </summary>
    public class KnowledgeViewTests
    {
        private static GodSim Played(ulong seed, int ticks)
        {
            var b = SimBuilder.Create(GameSetup.Watch(seed));
            for (int i = 0; i < ticks && !b.Sim.State.IsGameOver; i++) { b.Sim.Step(); b.Sim.DrainEvents(); }
            return b.Sim;
        }

        [Test]
        public void NoForeignTruth_ChangingAnotherFactionsStoresLeavesTheViewUnchanged()
        {
            // I19: under FullInformation, a foreign faction's unreported treasury (HQ stores), its sites' contents and a
            // peace offer between two other factions leave every field of the viewer's view unchanged; its own do not.
            var sim = Played(11, 4000);
            var s = sim.State;
            var view = ViewFiller.For(sim, Faction.F0);
            ulong before = view.Digest();
            Assert.AreEqual(before, view.Digest(), "the digest is a pure read");

            var foreignHq = s.Economy.HqsOf(Faction.F3)[0];
            foreignHq.Stock[(int)Ware.Plank] += 7; foreignHq.StockReserved[(int)Ware.Stone] += 1;
            foreach (var b in s.Economy.Buildings) if (b.Faction == Faction.F4 && !b.IsHq) { b.In[(int)Ware.Log] += 3; b.Out[(int)Ware.Plank] += 2; }
            s.Factions.PeaceOffer[(int)Faction.F1 * FactionInfo.Max + (int)Faction.F2] = true;
            Assert.AreEqual(before, view.Digest(), "foreign internals changed, the view did not");
            Assert.IsNull(view.Own.Building(foreignHq.Id), "a foreign store is not in the own books");
            Assert.IsFalse(view.Diplomacy.HasOfferedPeace(Faction.F1, Faction.F2), "a peace offer between others is not known");

            var mine = s.Economy.HqsOf(Faction.F0)[0];
            mine.Stock[(int)Ware.Plank] += 1;
            Assert.AreNotEqual(before, view.Digest(), "the own books are in the view");
            Assert.AreEqual(mine.Stock[(int)Ware.Plank], view.Own.Building(mine.Id).Stock[(int)Ware.Plank]);
        }

        [Test]
        public void ForeignItems_NameTheirChannel_AndCoverEveryOtherFaction()
        {
            var sim = Played(12, 3000);
            var view = ViewFiller.For(sim, Faction.F2);
            var items = view.Foreign();
            Assert.IsNotEmpty(items);
            foreach (var k in items)
            {
                Assert.AreNotEqual(KnowledgeChannel.Own, k.Channel, k.ToString());
                Assert.AreNotEqual(Faction.F2, k.About);
                Assert.AreEqual(sim.State.Tick, k.AsOfTick);
                Assert.AreEqual(1000, k.ConfidencePermille);
            }
            CollectionAssert.AreEquivalent(new[] { Faction.F0, Faction.F1, Faction.F3, Faction.F4, Faction.F5 }, items.Select(k => k.About).Distinct().ToArray());
            var vp = items.Where(k => k.Fact == ForeignFact.VictoryPoints).ToList();
            foreach (var k in vp) Assert.AreEqual(FactionRules.VictoryPoints(sim.State, k.About), k.Value);
            Assert.IsFalse(items.Any(k => k.Fact.ToString().Contains("Stock")), "no stock figure is foreign knowledge");
        }

        [Test]
        public void ViewFigures_EqualTheDirectReads_FiveSeeds()
        {
            // M1.7 (sim half): every figure the HUD shows, read through the view, equals the direct read it replaced.
            foreach (ulong seed in new ulong[] { 1, 2, 3, 4, 5 })
            {
                var b = SimBuilder.Create(new GameSetup(seed, 0));
                var sim = b.Sim; var s = sim.State;
                for (int i = 0; i < 6000 && !s.IsGameOver; i++) { sim.Step(); sim.DrainEvents(); }
                var me = Faction.F0;
                var v = ViewFiller.For(sim, me);
                string at = "seed " + seed;
                Assert.AreEqual(s.Tick, v.AsOfTick, at);
                Assert.AreEqual(SimCalendar.Of(s).Label(s.Tick), v.Calendar.Label(v.AsOfTick), at);
                for (int w = 1; w < Wares.Count; w++)
                {
                    Assert.AreEqual(s.Economy.TotalStock(me, (Ware)w), v.Own.TotalStock((Ware)w), at + " stock " + (Ware)w);
                    int best = 0; foreach (var hq in s.Economy.HqsOf(me)) best = Math.Max(best, TradeRules.FreeStock(hq, (Ware)w));
                    Assert.AreEqual(best, v.Own.MostFreeAtOneHq((Ware)w), at);
                }
                var st = FactionRules.Standings(s); var vs = v.Diplomacy.Standings();
                Assert.AreEqual(st.Count, vs.Count, at);
                for (int i = 0; i < st.Count; i++) Assert.AreEqual(st[i].ToString() + st[i].Hqs + st[i].Citadels, vs[i].ToString() + vs[i].Hqs + vs[i].Citadels, at);
                foreach (var f in FactionInfo.All)
                {
                    CollectionAssert.AreEqual(s.Factions.Enemies(f), v.Diplomacy.Enemies(f), at);
                    Assert.AreEqual(s.Factions.IsEliminated(f), v.Diplomacy.IsEliminated(f), at);
                }
                Assert.AreEqual(s.Military.Units.Count, v.Military.VisibleUnits.Count, at);
                foreach (var eb in s.Economy.Buildings.Where(x => x.Faction == me))
                {
                    Assert.AreSame(eb, v.Own.Building(eb.Id), at);
                    Assert.AreEqual(s.Economy.SiteProgressPermille(eb.Id), v.Own.SiteProgressPermille(eb.Id), at);
                    Assert.AreEqual(s.Military.DefendAtBuilding(eb.Id), v.Military.DefendAtBuilding(eb.Id), at);
                }
                foreach (var tb in s.Territory.Buildings)
                {
                    Assert.AreSame(tb, v.Map.BuildingById(tb.Id), at);
                    Assert.AreEqual(s.Military.IsMarked(me, tb.Id), v.Military.IsMarked(tb.Id), at);
                    Assert.AreEqual(s.Military.BuildingById(tb.Id)?.Health, v.Military.VisibleBuilding(tb.Id)?.Health, at);
                }
                CollectionAssert.AreEqual(s.Territory.BuildableNodes(me), v.Map.BuildableNodes(), at);
                CollectionAssert.AreEqual(s.Trade.PendingFor(me), v.Trade.PendingForMe(), at);
                Assert.AreEqual(s.Factions.OutcomeFor(me), v.Diplomacy.Outcome, at);
            }
        }

        [Test]
        public void ReportedMode_IsRefusedUntilRecordsExist()
        {
            var sim = GodSim.Create(GameSetup.Watch(1));
            var p = KnowledgeProfile.FullInformation().With(KnowledgeDomain.Crime, KnowledgeMode.Reported);
            Assert.IsFalse(p.IsFullInformation);
            Assert.Throws<NotSupportedException>(() => ViewFiller.For(sim, Faction.F0, p));
            Assert.IsTrue(ViewFiller.For(sim, Faction.F0).Profile.IsFullInformation, "FullInformation is the stage 4 default");
        }

        // ---------------------------------------------------------------- the debug overlay (Q22)

        [Test]
        public void Overlay_OffInCompetitivePlay_OptionalInSandbox_AllowedForDeveloperLabAndObservation()
        {
            // I18 / M1.8 (setup half)
            Assert.IsFalse(TrueStateOverlayRules.Allowed(ScenarioPurpose.Competitive));
            Assert.IsFalse(TrueStateOverlayRules.Allowed(ScenarioPurpose.Competitive, false));
            Assert.Throws<ArgumentException>(() => TrueStateOverlayRules.Allowed(ScenarioPurpose.Competitive, true), "never in competitive play");
            Assert.IsFalse(TrueStateOverlayRules.Allowed(ScenarioPurpose.Sandbox), "a sandbox: off unless enabled");
            Assert.IsTrue(TrueStateOverlayRules.Allowed(ScenarioPurpose.Sandbox, true));
            foreach (var p in new[] { ScenarioPurpose.Developer, ScenarioPurpose.Lab, ScenarioPurpose.Observation })
            {
                Assert.IsTrue(TrueStateOverlayRules.Allowed(p), p.ToString());
                Assert.IsFalse(TrueStateOverlayRules.Allowed(p, false), p + " may switch it off");
            }
            Assert.AreEqual(ScenarioPurpose.Competitive, TrueStateOverlayRules.DefaultPurpose(new GameSetup(1, 0)));
            Assert.AreEqual(ScenarioPurpose.Observation, TrueStateOverlayRules.DefaultPurpose(GameSetup.Watch(1)));
            Assert.AreEqual(ScenarioPurpose.Sandbox, TrueStateOverlayRules.DefaultPurpose(new GameSetup(1, 0) { Victory = new List<VictorySpec> { VictorySpec.Sandbox() } }));

            var competitive = GodSim.Create(TrueStateOverlayRules.Apply(new GameSetup(2, 0), ScenarioPurpose.Competitive));
            Assert.IsNull(ViewFiller.Overlay(competitive, Faction.F0), "no overlay object exists in competitive play");
            var dev = GodSim.Create(TrueStateOverlayRules.Apply(new GameSetup(2, 0), ScenarioPurpose.Developer));
            var ov = ViewFiller.Overlay(dev, Faction.F0);
            Assert.IsNotNull(ov);
            Assert.AreEqual(dev.State.Economy.TotalStock(Faction.F3, Ware.Plank), ov.TrueStock(Faction.F3, Ware.Plank), "the overlay shows foreign truth");
        }

        [Test]
        public void OverlaySettings_AreNotSimState_TheHashIsTheSame()
        {
            var a = GodSim.Create(TrueStateOverlayRules.Apply(new GameSetup(9, 0), ScenarioPurpose.Competitive));
            var b = GodSim.Create(TrueStateOverlayRules.Apply(new GameSetup(9, 0), ScenarioPurpose.Developer, true));
            Assert.AreEqual(a.StateHash(), b.StateHash());
            Assert.AreEqual(GodSim.Create(new GameSetup(9, 0)).StateHash(), a.StateHash(), "and equal to a setup that never heard of it (M0's hash)");
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(b.State.Setup, SaveFile.Settings);
            StringAssert.DoesNotContain("Overlay", json, "not saved");
            StringAssert.DoesNotContain("Purpose", json);
        }

        [Test]
        public void TogglingAndReadingTheOverlayEveryDay_LeavesTheHashIdentical()
        {
            // I11 / M1.5 (EM half; the runner test covers whole games)
            ulong[] Run(bool overlay)
            {
                var b = SimBuilder.Create(TrueStateOverlayRules.Apply(GameSetup.Watch(17), ScenarioPurpose.Observation));
                var sim = b.Sim; var hashes = new List<ulong>();
                ITrueStateOverlay ov = null; long sink = 0;
                for (int day = 0; day < 6; day++)
                {
                    for (int i = 0; i < 1200; i++) { sim.Step(); sim.DrainEvents(); }
                    if (overlay)
                    {
                        ov = ov == null ? ViewFiller.Overlay(sim, Faction.F0) : null; // toggled every day
                        var o = ov ?? ViewFiller.Overlay(sim, Faction.F1);
                        foreach (var f in FactionInfo.All) sink += o.TrueStockAll(f) + o.TrueSoldiers(f) + o.TrueBuildings(f) + o.TrueWaresInTransit(f) + o.TrueStock(f, Ware.Plank);
                        sink += ViewFiller.For(sim, Faction.F2).Digest() == 0 ? 1 : 0;
                    }
                    hashes.Add(sim.StateHash());
                }
                Assert.GreaterOrEqual(sink, 0);
                return hashes.ToArray();
            }
            CollectionAssert.AreEqual(Run(false), Run(true));
        }
    }
}
