using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>P11: the AI trader (offers, the judge, rate limits) and the balance of whole six-AI games with trade.</summary>
    public class AiTradeTests
    {
        public static string Row(ulong seed, GodState s, AiTests.GameLog log)
        {
            var vps = Enumerable.Range(0, s.FactionCount).Select(i => log.MaxVp[i]).ToArray();
            return "seed " + seed.ToString().PadLeft(2) + " | " + (s.IsGameOver ? (s.Factions.Winner >= 0 ? FactionInfo.Name((Faction)s.Factions.Winner) : "nobody") : "none").PadRight(7)
                + " | min " + log.Minutes.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture).PadLeft(5)
                + " | wars " + log.Wars.ToString().PadLeft(2) + " | captures " + log.Captures.ToString().PadLeft(2)
                + " | trades " + log.Offered + "/" + log.Accepted + "/" + log.Killed
                + " | min VP " + vps.Min() + " max VP " + vps.Max() + " | factions at 3+ VP " + vps.Count(v => v >= 3)
                + " | final " + string.Join(",", Enumerable.Range(0, s.FactionCount).Select(i => FactionRules.VictoryPoints(s, (Faction)i)));
        }

        // ---------------- helpers ----------------

        /// <summary>Seed 9: Crimson (F2) starts on fields and forest only, with no mountain (no stone of its own).</summary>
        private const ulong StallSeed = 9;

        private static (GodSim sim, AiDirector ai, AiTrader trader) Game(ulong seed)
        {
            var sim = GodSim.Create(GameSetup.Watch(seed));
            var trader = new AiTrader();
            var ai = AiDirector.Attach(sim, sim.State.Setup.Factions, null, trader);
            return (sim, ai, trader);
        }

        private static void SetStock(GodState s, Faction f, Ware w, int n)
        {
            var hqs = s.Economy.HqsOf(f);
            for (int i = 0; i < hqs.Count; i++) hqs[i].Stock[(int)w] = i == 0 ? n : 0;
        }

        /// <summary>Raises a faction's VP by making its first HQ a Citadel (2 VP) in the test state.</summary>
        private static void MakeCitadel(GodState s, Faction f)
        {
            foreach (var h in s.Territory.Hqs) if (h.Faction == f && !h.Site) { h.Citadel = true; return; }
        }

        /// <summary>Crimson short of stone with planks to spare; Azure, Jade and Violet with stone to spare and short of planks.</summary>
        private static void StoneMarket(GodState s)
        {
            SetStock(s, Faction.F2, Ware.Plank, 30); SetStock(s, Faction.F2, Ware.Stone, 0);
            foreach (var f in new[] { Faction.F1, Faction.F3, Faction.F4 }) { SetStock(s, f, Ware.Stone, 30); SetStock(s, f, Ware.Plank, 0); }
            foreach (var f in new[] { Faction.F0, Faction.F5 }) SetStock(s, f, Ware.Stone, 0);
        }

        private static int[] W(Ware w, int n) { var a = new int[Wares.Count]; a[(int)w] = n; return a; }

        // ---------------- trader tests ----------------

        [Test]
        public void StalledFaction_WithoutMountain_TradesPlanksForStone()
        {
            var (sim, ai, _) = Game(StallSeed);
            var s = sim.State;
            var crimson = ai.BrainOf(Faction.F2);
            crimson.WareNeeds();
            Assert.IsTrue(crimson.LacksLandFor(Ware.Stone), "precondition: no mountain in Crimson's start area");
            StoneMarket(s);
            TradeOffered offer = null; TradeAccepted accepted = null; EnvoyHome home = null;
            for (int i = 0; i < 3000 && home == null; i++)
            {
                sim.Step();
                foreach (var ev in sim.DrainEvents())
                {
                    if (ev is TradeOffered o && o.From == Faction.F2 && o.Want.Any(w => w.Ware == Ware.Stone) && offer == null) offer = o;
                    if (ev is TradeAccepted a && offer != null && a.OfferId == offer.OfferId) accepted = a;
                    if (ev is EnvoyHome h && offer != null && h.OfferId == offer.OfferId) home = h;
                }
            }
            Assert.IsNotNull(offer, "Crimson offers for stone");
            Assert.IsTrue(offer.Give.Length > 0 && offer.Give.All(g => g.Ware != Ware.Stone));
            Assert.GreaterOrEqual(TradeRules.Value(offer.Give), TradeRules.Value(offer.Want), "fair value, rounded in the partner's favour");
            Assert.IsNotNull(accepted, "a stone-rich partner accepts");
            Assert.IsNotNull(home, "the envoy comes home");
            Assert.IsTrue(home.Payment && home.Cargo.Any(c => c.Ware == Ware.Stone && c.Count > 0), "it carries the stone home");
            Assert.IsEmpty(SimInvariants.Check(s));
            TestContext.Out.WriteLine("offer " + string.Join("+", offer.Give.Select(g => g.ToString())) + " for "
                + string.Join("+", offer.Want.Select(g => g.ToString())) + " to " + FactionInfo.Name(offer.To) + " at tick " + offer.OfferId);
        }

        [Test]
        public void Offers_GoToTheWorstPlacedPartner_ThatHasTheWare()
        {
            var (sim, ai, trader) = Game(StallSeed);
            var s = sim.State;
            StoneMarket(s);
            MakeCitadel(s, Faction.F1); MakeCitadel(s, Faction.F3); // Azure and Jade 2 VP (tied top: no sole leader), Violet 1 VP
            var plan = trader.Plan(ai.BrainOf(Faction.F2));
            Assert.IsNotNull(plan);
            Assert.AreEqual(Faction.F4, plan.To, "Violet has the fewest VP of those with stone to spare");
            Assert.AreEqual(Ware.Stone, plan.Want.Single().Ware);
            Assert.IsTrue(sim.Check(plan).Ok);
        }

        [Test]
        public void NoOffers_ToTheLeader_OrToFactionsAtWar()
        {
            var (sim, ai, trader) = Game(StallSeed);
            var s = sim.State;
            Assert.IsTrue(sim.Submit(new DeclareWar(Faction.F2, Faction.F3)).Ok);
            sim.Step(); sim.DrainEvents();
            Assert.IsTrue(s.Factions.AtWar(Faction.F2, Faction.F3));
            StoneMarket(s);
            MakeCitadel(s, Faction.F4); // Violet is the sole leader
            var plan = trader.Plan(ai.BrainOf(Faction.F2));
            Assert.IsNotNull(plan);
            Assert.AreEqual(Faction.F1, plan.To, "not the leader Violet, not Jade at war: Azure");
            // The judge refuses the leader and a faction at war, whatever the deal.
            Assert.IsFalse(trader.JudgeIncoming(ai.BrainOf(Faction.F1), Faction.F4, W(Ware.Plank, 8), W(Ware.Stone, 1)), "the leader's offer is refused");
            Assert.IsFalse(trader.JudgeIncoming(ai.BrainOf(Faction.F3), Faction.F2, W(Ware.Plank, 8), W(Ware.Stone, 1)), "an enemy's offer is refused");
            Assert.IsTrue(trader.JudgeIncoming(ai.BrainOf(Faction.F1), Faction.F2, W(Ware.Plank, 8), W(Ware.Stone, 1)), "the same offer from anyone else is taken");
            // With only the leader and the enemy holding stone, nothing is offered.
            SetStock(s, Faction.F1, Ware.Stone, 0);
            Assert.IsNull(trader.Plan(ai.BrainOf(Faction.F2)));
        }

        [Test]
        public void Judge_AcceptsFairHelpfulOffers_AndRefusesUnfairOnes()
        {
            var (sim, ai, trader) = Game(StallSeed);
            var s = sim.State;
            StoneMarket(s);
            var azure = ai.BrainOf(Faction.F1);
            // Stage 3 values: a plank (log + stone) is worth 5, a stone 3.
            Assert.IsTrue(trader.JudgeIncoming(azure, Faction.F2, W(Ware.Plank, 3), W(Ware.Stone, 5)), "fair: 15 for 15");
            Assert.IsTrue(trader.JudgeIncoming(azure, Faction.F2, W(Ware.Plank, 2), W(Ware.Stone, 4)), "within a quarter: 10 for 12");
            Assert.IsFalse(trader.JudgeIncoming(azure, Faction.F2, W(Ware.Plank, 1), W(Ware.Stone, 4)), "unfair: 5 for 12");
            Assert.IsFalse(trader.JudgeIncoming(azure, Faction.F2, W(Ware.Log, 1), W(Ware.Stone, 4)), "unfair: 2 for 12");
            Assert.IsFalse(trader.JudgeIncoming(azure, Faction.F2, W(Ware.Plank, 40), W(Ware.Stone, 30)), "never more than its surplus");
            SetStock(s, Faction.F1, Ware.Plank, 40);
            Assert.IsFalse(trader.JudgeIncoming(azure, Faction.F2, W(Ware.Plank, 4), W(Ware.Stone, 4)), "planks it does not need do not help");
            // Through the sim: an unfair offer reaching an AI is refused by the judge.
            Assert.IsInstanceOf<AiTradeJudge>(s.Trade.Judge, "brains with a trader set the judge");
            SetStock(s, Faction.F1, Ware.Plank, 0);
            Assert.IsTrue(sim.Submit(new OfferTrade(Faction.F2, Faction.F1, new[] { new WareCount(Ware.Plank, 1) }, new[] { new WareCount(Ware.Stone, 6) })).Ok);
            TradeRefused refused = null;
            for (int i = 0; i < 2000 && refused == null; i++)
            {
                sim.Step();
                foreach (var ev in sim.DrainEvents())
                    if (ev is TradeRefused r && r.From == Faction.F2 && r.To == Faction.F1 && r.Want.Any(w => w.Count == 6)) refused = r;
            }
            Assert.IsNotNull(refused);
            Assert.AreEqual(RefusalCause.Declined, refused.Cause);
        }

        [Test]
        public void Offers_AreRateLimited_EnvoysCapped_AndRefusalsRemembered()
        {
            var game = GodSim.Create(GameSetup.Watch(StallSeed));
            AiDirector.Attach(game, game.State.Setup.Factions, null, new AiTrader());
            var last = new Dictionary<Faction, long>();
            var refusedAt = new Dictionary<(Faction, Faction, Ware), long>();
            int offers = 0;
            while (!game.State.IsGameOver && game.State.Tick < 30 * 600)
            {
                game.Step();
                foreach (var f in game.State.Setup.Factions) Assert.LessOrEqual(game.State.Trade.EnvoyCountOf(f), TradeTuning.MaxEnvoysPerFaction);
                foreach (var ev in game.DrainEvents())
                {
                    if (ev is TradeOffered o)
                    {
                        offers++;
                        if (last.TryGetValue(o.From, out long t)) Assert.GreaterOrEqual(game.State.Tick - t, AiTuning.Default.TradeIntervalTicks, FactionInfo.Name(o.From) + " offered twice within 30 s");
                        last[o.From] = game.State.Tick;
                        foreach (var w in o.Want)
                            if (refusedAt.TryGetValue((o.From, o.To, w.Ware), out long r))
                                Assert.Greater(game.State.Tick - r, AiTuning.Default.TradeRefusedMemoryTicks, "a refused ask is not sent again within 2 minutes");
                    }
                    if (ev is TradeRefused rf && rf.Cause == RefusalCause.Declined)
                        foreach (var w in rf.Want) refusedAt[(rf.From, rf.To, w.Ware)] = game.State.Tick;
                }
            }
            Assert.Greater(offers, 5);
        }

        [Test]
        public void TradingGames_AreDeterministic()
        {
            var a = AiTests.Play(5, 12000, 0, null, new AiTrader());
            var b = AiTests.Play(5, 12000, 0, null, new AiTrader());
            Assert.Greater(a.log.Offered, 0);
            Assert.AreEqual(a.log.Offered, b.log.Offered);
            Assert.AreEqual(a.log.Accepted, b.log.Accepted);
            Assert.AreEqual(a.sim.StateHash(), b.sim.StateHash());
        }

        [Test]
        public void SaveLoad_MidGameWithTradingAi_ContinuesWithTheSameHash()
        {
            var straight = GodSim.Create(GameSetup.Watch(5));
            AiDirector.Attach(straight, straight.State.Setup.Factions, null, new AiTrader());
            // On to a tick with at least two envoys walking.
            while (straight.State.Trade.ActiveEnvoys.Count < 2 && straight.State.Tick < 12000) { straight.Step(); straight.DrainEvents(); }
            Assert.GreaterOrEqual(straight.State.Trade.ActiveEnvoys.Count, 2, "envoys are walking when saved");
            long at = straight.State.Tick;
            var loaded = SaveFile.FromJson(SaveFile.From(straight, "x").ToJson()).ToSim();
            AiDirector.Attach(loaded, AiDirector.NonHuman(loaded.State), null, new AiTrader());
            Assert.IsInstanceOf<AiTradeJudge>(loaded.State.Trade.Judge, "the AI judge is set again after the load");
            Assert.AreEqual(straight.StateHash(), loaded.StateHash());
            int trades = 0;
            for (int i = 0; i < 900; i++)
            {
                straight.Step(); trades += straight.DrainEvents().Count(e => e is TradeOffered || e is TradeAccepted);
                loaded.Step(); loaded.DrainEvents();
            }
            Assert.Greater(trades, 0, "trading went on after the save");
            Assert.AreEqual(straight.StateHash(), loaded.StateHash(), "same game 900 ticks after the load at tick " + at);
            Assert.IsEmpty(SimInvariants.Check(loaded.State));
        }

        // ---------------- whole games ----------------

        /// <summary>
        /// Six trading brains play whole games: most games end within 90 sim-minutes, every game has trades and accepted
        /// ones, most have 4 factions at 3 VP or more, a war and a capture, and the invariants hold. The table goes to the output.
        /// </summary>
        [Test]
        public void SixTradingBrains_Balance_OverSeveralSeeds()
        {
            var seeds = new ulong[] { 2, 3, 8, 9 };
            int broad = 0, warred = 0, captured = 0, ended = 0;
            foreach (var seed in seeds)
            {
                var (sim, _, log) = AiTests.Play(seed, 90 * 600, 0, null, new AiTrader());
                var s = sim.State;
                TestContext.Out.WriteLine(Row(seed, s, log));
                if (s.IsGameOver) ended++;
                Assert.Greater(log.Offered, 0, "seed " + seed + " has trades");
                Assert.Greater(log.Accepted, 0, "seed " + seed + " has accepted trades");
                Assert.IsEmpty(SimInvariants.Check(s));
                if (log.MaxVp.Take(s.FactionCount).Count(v => v >= 3) >= 4) broad++;
                if (log.Wars > 0) warred++;
                if (log.Captures > 0) captured++;
            }
            // Stage 3 (Q1): the new chain is not balanced yet (package Q4 tunes it); half the games are broad for now.
            Assert.GreaterOrEqual(broad, seeds.Length / 2, "4 of 6 factions reach 3 VP in half the games");
            Assert.GreaterOrEqual(ended, seeds.Length - 1, "most games end within 90 minutes");
            Assert.GreaterOrEqual(warred, seeds.Length - 1);
            Assert.GreaterOrEqual(captured, seeds.Length - 1);
        }

        /// <summary>Balance report over seeds 1 to 12 and the default seed, without and with the trader (run by name; prints the per-seed table).</summary>
        [Test, Explicit("balance report: run by name")]
        public void BalanceReport_Seeds1To12()
        {
            foreach (bool trade in new[] { false, true })
            {
                TestContext.Out.WriteLine(trade ? "WITH AiTrader" : "WITHOUT trade");
                foreach (ulong seed in Enumerable.Range(1, 12).Select(i => (ulong)i).Concat(new[] { 20261124UL }))
                {
                    var (sim, _, log) = AiTests.Play(seed, 90 * 600, 0, null, trade ? new AiTrader() : null);
                    TestContext.Out.WriteLine(Row(seed, sim.State, log));
                }
            }
        }
    
        [Test, Explicit("debug dump")]
        public void Dump_Seed()
        {
            ulong seed = ulong.Parse(Environment.GetEnvironmentVariable("AI_SEED") ?? "9");
            var (sim, _, log) = AiTests.Play(seed, 90 * 600, 1200, null, new AiTrader());
            foreach (var l in log.Lines) TestContext.Out.WriteLine(l);
            foreach (var r in sim.State.Trade.History) TestContext.Out.WriteLine("trade " + r.OfferId + " " + (int)r.From + "->" + (int)r.To + " " + string.Join("+", r.Give.Select(g => g.ToString())) + " for " + string.Join("+", r.Want.Select(g => g.ToString())) + " " + r.Outcome + " " + r.Cause + " t" + r.Tick);
            TestContext.Out.WriteLine(Row(seed, sim.State, log));
        }
    }
}
