using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>Helpers of the trade tests: a six-faction game (F0 human unless told), an event log and ware totals.</summary>
    internal sealed class TradeBed
    {
        public readonly GodSim Sim;
        public readonly GodState S;
        public readonly List<SimEvent> Log = new List<SimEvent>();
        public long Initial;

        public TradeBed(ulong seed = 3, int human = 0, int factions = FactionInfo.Max)
        {
            Sim = GodSim.Create(new GameSetup(seed, human, factions));
            S = Sim.State;
            Initial = TotalGoods();
        }

        public TradeState T => S.Trade;

        public static WareCount[] W(params (Ware w, int n)[] a) => a.Select(x => new WareCount(x.w, x.n)).ToArray();

        public void Step(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                Sim.Step(); Log.AddRange(Sim.DrainEvents());
                if (Sim.State.Tick % 5 == 0) Conserved();
            }
        }

        public CommandResult Do(Command c)
        {
            var r = Sim.Submit(c);
            if (r.Ok) Step(1);
            return r;
        }

        public IEnumerable<E> Ev<E>() where E : SimEvent => Log.OfType<E>();

        public bool RunUntil(int max, Func<bool> done)
        {
            for (int i = 0; i < max; i++) { if (done()) return true; Step(); }
            return done();
        }

        /// <summary>Every unit of every ware in every HQ stock, plus the packs, plus what was lost (with an HQ or an envoy).</summary>
        public long TotalGoods()
        {
            long n = 0;
            foreach (var b in S.Economy.Buildings) if (b.IsHq) foreach (int c in b.Stock) n += c;
            foreach (var e in T.Envoys) n += e.CargoTotal;
            foreach (int c in T.Lost) n += c;
            n += S.Economy.LostWares;
            return n;
        }

        public void Conserved()
        {
            Assert.AreEqual(Initial, TotalGoods(), "no ware appears or vanishes (tick " + S.Tick + ")");
            var bad = SimInvariants.Check(S);
            Assert.IsEmpty(bad, "tick " + S.Tick + ": " + string.Join("; ", bad));
        }

        public int Stock(Faction f, Ware w) => S.Economy.TotalStock(f, w);

        public Envoy Envoy => T.Envoys.FirstOrDefault();

        /// <summary>Sends 3 log for 2 brick from one faction to another and returns the offer id.</summary>
        public int Send(Faction from, Faction to, WareCount[] give = null, WareCount[] want = null, int fromHq = 0)
        {
            var r = Do(new OfferTrade(from, to, give ?? W((Ware.Log, 3)), want ?? W((Ware.Brick, 2)), fromHq));
            Assert.IsTrue(r.Ok, r.ToString());
            return Ev<TradeOffered>().Last().OfferId;
        }

        /// <summary>A point this far ahead of an envoy on the straight line to its target HQ.</summary>
        public (int x, int z) Ahead(Envoy e, int mm)
        {
            var hq = S.Economy.Find(e.TargetHq);
            long dx = TradeRules.HqX(S, hq) - e.X, dz = TradeRules.HqZ(S, hq) - e.Z;
            int d = Math.Max(1, Pathing.ISqrt(dx * dx + dz * dz));
            return (e.X + (int)(dx * mm / d), e.Z + (int)(dz * mm / d));
        }

        public MilUnit Spawn(Faction f, UnitKind k, int x, int z)
        {
            int node = Pathing.NearestLandNode(S.Territory.Grid, x, z);
            int before = S.Military.Units.Count;
            Assert.IsTrue(Do(new DevSpawnUnit(f, k, node)).Ok);
            Assert.AreEqual(before + 1, S.Military.Units.Count);
            return S.Military.Units.Last();
        }
    }

    /// <summary>Envoys, offers, answers and the rules around them (P8).</summary>
    public class TradeTests
    {
        private static WareCount[] W(params (Ware w, int n)[] a) => TradeBed.W(a);

        [Test]
        public void Offer_Validation()
        {
            var b = new TradeBed();
            var give = W((Ware.Log, 3)); var want = W((Ware.Brick, 2));
            CommandResult Check(Faction from, Faction to, WareCount[] g, WareCount[] w) => b.Sim.Check(new OfferTrade(from, to, g, w));

            Assert.IsTrue(Check(Faction.F0, Faction.F1, give, want).Ok);
            Assert.AreEqual(FactionReasons.SameFaction, Check(Faction.F0, Faction.F0, give, want).Reason, "not with itself");
            Assert.AreEqual(TradeReasons.BadAmount, Check(Faction.F0, Faction.F1, W(), want).Reason, "give at least one");
            Assert.AreEqual(TradeReasons.BadAmount, Check(Faction.F0, Faction.F1, give, W()).Reason, "want at least one");
            Assert.AreEqual(TradeReasons.BadAmount, Check(Faction.F0, Faction.F1, W((Ware.Log, 9)), want).Reason, "at most 8 each way");
            Assert.AreEqual(TradeReasons.BadAmount, Check(Faction.F0, Faction.F1, W((Ware.Log, 5), (Ware.Plank, 4)), want).Reason, "8 in total, not per ware");
            Assert.AreEqual(TradeReasons.BadAmount, Check(Faction.F0, Faction.F1, give, W((Ware.Brick, 4), (Ware.Iron, 5))).Reason);
            Assert.IsTrue(Check(Faction.F0, Faction.F1, W((Ware.Log, 4), (Ware.Plank, 4)), W((Ware.Brick, 4))).Ok, "8 exactly is fine");
            Assert.AreEqual(TradeReasons.BadAmount, Check(Faction.F0, Faction.F1, W((Ware.Log, -1)), want).Reason);
            Assert.AreEqual(TradeReasons.BadWare, Check(Faction.F0, Faction.F1, W((Ware.None, 1)), want).Reason);
            Assert.AreEqual(TradeReasons.BadWare, Check(Faction.F0, Faction.F1, give, W((Ware.Rock, 1))).Reason);
            Assert.AreEqual(TradeReasons.SameWare, Check(Faction.F0, Faction.F1, give, W((Ware.Log, 1))).Reason);
            Assert.AreEqual(TradeReasons.NoStock, Check(Faction.F0, Faction.F1, W((Ware.Iron, 1)), want).Reason, "nothing in stock");
            Assert.AreEqual(TradeReasons.NoStock, Check(Faction.F0, Faction.F1, W((Ware.Stone, 7)), want).Reason, "only 6 stone");
            Assert.AreEqual(Reasons.NotOwner, Check((Faction)7, Faction.F1, give, want).Reason);

            // Counts per ware add up when the same ware is listed twice.
            Assert.IsTrue(Check(Faction.F0, Faction.F1, W((Ware.Log, 2), (Ware.Log, 3)), want).Ok);
            Assert.AreEqual(TradeReasons.BadAmount, Check(Faction.F0, Faction.F1, W((Ware.Log, 5), (Ware.Log, 4)), want).Reason);

            // War blocks trade.
            b.Do(new DeclareWar(Faction.F0, Faction.F2));
            Assert.AreEqual(TradeReasons.AtWar, Check(Faction.F0, Faction.F2, give, want).Reason);
            Assert.AreEqual(TradeReasons.AtWar, Check(Faction.F2, Faction.F0, give, want).Reason);
            Assert.IsTrue(Check(Faction.F0, Faction.F3, give, want).Ok);

            // At most four envoys at once.
            for (int i = 0; i < TradeTuning.MaxEnvoysPerFaction; i++) b.Send(Faction.F0, Faction.F1, W((Ware.Log, 1)), W((Ware.Clay, 1)));
            Assert.AreEqual(TradeReasons.TooManyEnvoys, Check(Faction.F0, Faction.F1, W((Ware.Plank, 1)), W((Ware.Clay, 1))).Reason);
            Assert.AreEqual(4, b.T.ActiveEnvoys.Count);

            // A faction out of the game cannot be traded with.
            var two = new TradeBed(3, 0, 2);
            Assert.AreEqual(FactionReasons.NotInGame, two.Sim.Check(new OfferTrade(Faction.F0, Faction.F4, give, want)).Reason);
        }

        [Test]
        public void Offer_TakesTheGoodsAtOnce_AndSpawnsAnEnvoyAtTheSendersHq()
        {
            var b = new TradeBed();
            int logs = b.Stock(Faction.F0, Ware.Log);
            int id = b.Send(Faction.F0, Faction.F1);
            Assert.AreEqual(logs - 3, b.Stock(Faction.F0, Ware.Log), "the give wares left the HQ");
            var env = b.T.EnvoyById(id);
            Assert.NotNull(env);
            Assert.AreEqual(Faction.F0, env.Faction);
            Assert.AreEqual(EnvoyStage.ToTarget, env.Stage);
            Assert.AreEqual(3, env.Cargo[(int)Ware.Log]);
            Assert.AreEqual(TradeTuning.EnvoyHealth, env.Health);
            var hq0 = b.S.Economy.HqsOf(Faction.F0)[0];
            Assert.Less(Pathing.Dist(env.X, env.Z, TradeRules.HqX(b.S, hq0), TradeRules.HqZ(b.S, hq0)), 2000, "starts at its HQ");
            Assert.AreEqual(1, b.Ev<TradeOffered>().Count());
            Assert.IsEmpty(b.T.PendingFor(Faction.F1), "not pending until the envoy arrives");
            Assert.AreEqual(OfferStage.InFlight, b.T.OfferById(id).Stage);
        }

        [Test]
        public void Envoy_WalksOverLand_ThereAndBack_AtAnAcceptedTrade_WithExactConservation()
        {
            var b = new TradeBed(3);
            int l0 = b.Stock(Faction.F0, Ware.Log), l1 = b.Stock(Faction.F1, Ware.Log), b0 = b.Stock(Faction.F0, Ware.Brick), b1 = b.Stock(Faction.F1, Ware.Brick);
            int id = b.Send(Faction.F0, Faction.F1);
            var env = b.T.EnvoyById(id);
            var g = b.S.Territory.Grid;
            int sx = env.X, sz = env.Z;
            long moved = 0; int px = env.X, pz = env.Z;
            Assert.IsTrue(b.RunUntil(3000, () =>
            {
                Assert.IsTrue(Pathing.IsLandPoint(g, env.X, env.Z), "always on land");
                moved += Pathing.Dist(px, pz, env.X, env.Z); px = env.X; pz = env.Z;
                Assert.LessOrEqual(Pathing.Dist(px, pz, env.X, env.Z), TradeTuning.EnvoySpeedMmPerTick);
                return b.Ev<EnvoyArrived>().Any();
            }), "arrives");
            long arrivedAt = b.S.Tick;
            Assert.Greater(moved, 20000, "it walked a good way");
            Assert.AreEqual(Faction.F1, b.Ev<EnvoyArrived>().Single().To);
            // The AI target answers on arrival (default judge: 6 for 8 is within the margin).
            Assert.AreEqual(1, b.Ev<TradeAccepted>().Count());
            Assert.AreEqual(EnvoyStage.Homeward, env.Stage);
            Assert.AreEqual(l1 + 3, b.Stock(Faction.F1, Ware.Log), "the give wares went into the target's stock");
            Assert.AreEqual(b1 - 2, b.Stock(Faction.F1, Ware.Brick), "the want wares came out of it");
            Assert.AreEqual(2, env.Cargo[(int)Ware.Brick]); Assert.AreEqual(0, env.Cargo[(int)Ware.Log]);
            Assert.AreEqual(l0 - 3, b.Stock(Faction.F0, Ware.Log), "nothing is paid out before the envoy is home");
            Assert.AreEqual(b0, b.Stock(Faction.F0, Ware.Brick));

            Assert.IsTrue(b.RunUntil(3000, () => b.Ev<EnvoyHome>().Any()), "walks home");
            var home = b.Ev<EnvoyHome>().Single();
            Assert.IsTrue(home.Payment);
            Assert.AreEqual(Faction.F0, home.Faction);
            Assert.AreEqual(l0 - 3, b.Stock(Faction.F0, Ware.Log));
            Assert.AreEqual(b0 + 2, b.Stock(Faction.F0, Ware.Brick), "the payment is in the sender's stock");
            Assert.IsEmpty(b.T.Envoys); Assert.IsEmpty(b.T.Offers);
            Assert.Less(Pathing.Dist(env.X, env.Z, sx, sz), 4000, "it ended where it began");
            var rec = b.T.History.Single();
            Assert.AreEqual(TradeOutcome.Accepted, rec.Outcome);
            Assert.AreEqual(Faction.F0, rec.From); Assert.AreEqual(Faction.F1, rec.To);
            Assert.AreEqual(1, b.T.Accepted);
            TestContext.WriteLine("one way: " + arrivedAt + " ticks, " + moved / 1000 + " m");
            CollectionAssert.AreEqual(new[] { typeof(TradeOffered), typeof(EnvoyArrived), typeof(TradeAccepted), typeof(EnvoyHome) },
                b.Log.Where(e => e is TradeOffered || e is EnvoyArrived || e is TradeAccepted || e is TradeRefused || e is EnvoyHome).Select(e => e.GetType()).ToArray());
        }

        [Test]
        public void Refused_TheEnvoyBringsTheOriginalGoodsBack()
        {
            var b = new TradeBed(3);
            int l0 = b.Stock(Faction.F0, Ware.Log), b1 = b.Stock(Faction.F1, Ware.Brick), l1 = b.Stock(Faction.F1, Ware.Log);
            // F1 has 4 brick: asking for 5 is more than it can pay, so the judge refuses.
            b.Send(Faction.F0, Faction.F1, W((Ware.Log, 6)), W((Ware.Brick, 5)));
            Assert.IsTrue(b.RunUntil(3000, () => b.Ev<EnvoyHome>().Any()));
            var refused = b.Ev<TradeRefused>().Single();
            Assert.AreEqual(RefusalCause.Declined, refused.Cause);
            Assert.IsFalse(b.Ev<EnvoyHome>().Single().Payment);
            Assert.AreEqual(l0, b.Stock(Faction.F0, Ware.Log), "the goods came back");
            Assert.AreEqual(l1, b.Stock(Faction.F1, Ware.Log)); Assert.AreEqual(b1, b.Stock(Faction.F1, Ware.Brick));
            Assert.AreEqual(TradeOutcome.Refused, b.T.History.Single().Outcome);
            Assert.AreEqual(0, b.Ev<TradeAccepted>().Count());
        }

        [Test]
        public void DefaultJudge_AcceptsFairDealsTheTargetCanPay_AndRefusesTheRest()
        {
            var b = new TradeBed(3);
            var judge = new DefaultTradeJudge();
            TradeOffer Offer(WareCount[] give, WareCount[] want) => new TradeOffer { From = Faction.F0, To = Faction.F1, Give = give, Want = want };
            Assert.AreEqual(TradeVerdict.Accept, judge.Decide(b.S, Offer(W((Ware.Log, 4)), W((Ware.Clay, 4)))), "even");
            Assert.AreEqual(TradeVerdict.Accept, judge.Decide(b.S, Offer(W((Ware.Log, 3)), W((Ware.Brick, 2)))), "6 for 8 is -25%");
            Assert.AreEqual(TradeVerdict.Refuse, judge.Decide(b.S, Offer(W((Ware.Log, 2)), W((Ware.Brick, 2)))), "4 for 8");
            Assert.AreEqual(TradeVerdict.Accept, judge.Decide(b.S, Offer(W((Ware.Log, 8)), W((Ware.Clay, 2)))), "a generous offer is taken");
            Assert.AreEqual(TradeVerdict.Refuse, judge.Decide(b.S, Offer(W((Ware.Log, 8)), W((Ware.Iron, 1)))), "it has no iron");
            Assert.AreEqual(TradeVerdict.Refuse, judge.Decide(b.S, Offer(W((Ware.Log, 8)), W((Ware.Brick, 5)))), "it has 4 brick");
        }

        private sealed class WaitJudge : ITradeJudge
        {
            public int Calls;
            public TradeVerdict Decide(GodState s, TradeOffer o) { Calls++; return TradeVerdict.Wait; }
        }

        [Test]
        public void TheJudgeIsPluggable_AndWaitLeavesTheOfferForAnAnswerCommand()
        {
            var b = new TradeBed(3);
            var judge = new WaitJudge();
            b.T.Judge = judge;
            int id = b.Send(Faction.F0, Faction.F1);
            Assert.IsTrue(b.RunUntil(3000, () => b.T.PendingFor(Faction.F1).Count == 1));
            Assert.AreEqual(1, judge.Calls);
            Assert.AreEqual(EnvoyStage.Waiting, b.Envoy.Stage);
            Assert.AreEqual(Reasons.NotOwner, b.Sim.Check(new AnswerTrade(Faction.F2, id, true)).Reason, "only the target answers");
            Assert.IsTrue(b.Do(new AnswerTrade(Faction.F1, id, true)).Ok, "a bot answers with the command like a person");
            Assert.AreEqual(1, b.Ev<TradeAccepted>().Count());
        }

        [Test]
        public void HumanTarget_AnswersWithTheCommand_AndOthersCannotAnswerForIt()
        {
            var b = new TradeBed(3);
            int id = b.Send(Faction.F1, Faction.F0, W((Ware.Log, 3)), W((Ware.Brick, 2)));
            Assert.AreEqual(TradeReasons.NotPending, b.Sim.Check(new AnswerTrade(Faction.F0, id, true)).Reason, "still walking");
            Assert.IsTrue(b.RunUntil(3000, () => b.T.PendingFor(Faction.F0).Count == 1), "the offer reaches the human and waits");
            Assert.AreEqual(0, b.Ev<TradeAccepted>().Count() + b.Ev<TradeRefused>().Count(), "the AI judge is not asked for a human");
            var offer = b.T.PendingFor(Faction.F0).Single();
            Assert.AreEqual(Faction.F1, offer.From); Assert.AreEqual(TradeTuning.AnswerTicks, offer.TicksLeft + (int)(b.S.Tick - offer.ArrivedTick));
            Assert.AreEqual(Reasons.NotOwner, b.Sim.Check(new AnswerTrade(Faction.F2, id, true)).Reason);

            int l0 = b.Stock(Faction.F0, Ware.Log), br0 = b.Stock(Faction.F0, Ware.Brick);
            b.Step(40);
            Assert.AreEqual(1, b.T.PendingFor(Faction.F0).Count, "it waits");
            Assert.IsTrue(b.Do(new AnswerTrade(Faction.F0, id, true)).Ok);
            Assert.AreEqual(l0 + 3, b.Stock(Faction.F0, Ware.Log)); Assert.AreEqual(br0 - 2, b.Stock(Faction.F0, Ware.Brick));
            Assert.IsEmpty(b.T.PendingFor(Faction.F0));
            Assert.AreEqual(Reasons.NoSuchThing, b.Sim.Check(new AnswerTrade(Faction.F0, id, true)).Reason, "answered once");
            Assert.IsTrue(b.RunUntil(3000, () => b.Ev<EnvoyHome>().Any()));
            Assert.AreEqual(1, b.Ev<TradeAccepted>().Count());
        }

        [Test]
        public void HumanRefuses_AndNoAnswerIn60Seconds_IsARefusal()
        {
            var b = new TradeBed(3);
            int l1 = b.Stock(Faction.F1, Ware.Log);
            int id = b.Send(Faction.F1, Faction.F0);
            Assert.IsTrue(b.RunUntil(3000, () => b.T.PendingFor(Faction.F0).Count == 1));
            Assert.IsTrue(b.Do(new AnswerTrade(Faction.F0, id, false)).Ok);
            Assert.AreEqual(RefusalCause.Declined, b.Ev<TradeRefused>().Single().Cause);
            Assert.IsTrue(b.RunUntil(3000, () => b.Ev<EnvoyHome>().Any()));
            Assert.AreEqual(l1, b.Stock(Faction.F1, Ware.Log));

            // Silence: refused when the 60 s are up, not before.
            b.Log.Clear();
            int id2 = b.Send(Faction.F1, Faction.F0);
            Assert.IsTrue(b.RunUntil(3000, () => b.T.PendingFor(Faction.F0).Count == 1));
            long arrived = b.S.Tick;
            b.Step(TradeTuning.AnswerTicks - 2);
            Assert.AreEqual(1, b.T.PendingFor(Faction.F0).Count, "still waiting just before the end");
            Assert.AreEqual(0, b.Ev<TradeRefused>().Count());
            b.Step(3);
            var timeout = b.Ev<TradeRefused>().Single();
            Assert.AreEqual(RefusalCause.Timeout, timeout.Cause);
            Assert.AreEqual(id2, timeout.OfferId);
            Assert.AreEqual(TradeTuning.AnswerTicks, timeout.Tick - arrived);
            Assert.AreEqual(EnvoyStage.Homeward, b.Envoy.Stage);
            Assert.AreEqual(Reasons.NoSuchThing, b.Sim.Check(new AnswerTrade(Faction.F0, id2, true)).Reason, "too late");
            Assert.IsTrue(b.RunUntil(3000, () => b.Ev<EnvoyHome>().Any()));
            Assert.AreEqual(l1, b.Stock(Faction.F1, Ware.Log));
        }

        [Test]
        public void AcceptedButTheTargetCanNoLongerPay_CountsAsRefused()
        {
            var b = new TradeBed(3);
            int id = b.Send(Faction.F1, Faction.F0, W((Ware.Log, 3)), W((Ware.Brick, 4)));
            Assert.IsTrue(b.RunUntil(3000, () => b.T.PendingFor(Faction.F0).Count == 1));
            b.S.Economy.HqsOf(Faction.F0)[0].Stock[(int)Ware.Brick] = 3; // spent meanwhile
            long total = b.Initial; b.Initial -= 1; // one brick was spent outside the sim's rules for this test
            Assert.IsTrue(b.Do(new AnswerTrade(Faction.F0, id, true)).Ok);
            Assert.AreEqual(RefusalCause.CannotPay, b.Ev<TradeRefused>().Single().Cause);
            Assert.AreEqual(0, b.Ev<TradeAccepted>().Count());
            Assert.AreEqual(3, b.Stock(Faction.F0, Ware.Brick), "nothing was taken");
            Assert.IsTrue(b.RunUntil(3000, () => b.Ev<EnvoyHome>().Any()));
            Assert.AreEqual(total - 1, b.TotalGoods());
        }

        [Test]
        public void WarDeclaredMidTrip_TheEnvoyTurnsHome_WithTheGoods()
        {
            var b = new TradeBed(3);
            int l0 = b.Stock(Faction.F0, Ware.Log);
            b.Send(Faction.F0, Faction.F1);
            b.Step(30);
            Assert.AreEqual(EnvoyStage.ToTarget, b.Envoy.Stage);
            Assert.IsTrue(b.Do(new DeclareWar(Faction.F1, Faction.F0)).Ok);
            b.Step(1);
            Assert.AreEqual(EnvoyStage.Homeward, b.Envoy.Stage);
            Assert.AreEqual(RefusalCause.War, b.Ev<TradeRefused>().Single().Cause);
            Assert.IsTrue(b.RunUntil(3000, () => b.Ev<EnvoyHome>().Any()));
            Assert.AreEqual(l0, b.Stock(Faction.F0, Ware.Log));
            Assert.AreEqual(0, b.Ev<TradeAccepted>().Count());
            Assert.AreEqual(TradeReasons.AtWar, b.Sim.Check(new OfferTrade(Faction.F0, Faction.F1, W((Ware.Log, 1)), W((Ware.Clay, 1)))).Reason);

            // War declared while the envoy waits for a human answer.
            var c = new TradeBed(3);
            int id = c.Send(Faction.F1, Faction.F0);
            Assert.IsTrue(c.RunUntil(3000, () => c.T.PendingFor(Faction.F0).Count == 1));
            c.Do(new DeclareWar(Faction.F0, Faction.F1)); c.Step(1);
            Assert.AreEqual(RefusalCause.War, c.Ev<TradeRefused>().Single().Cause);
            Assert.IsEmpty(c.T.PendingFor(Faction.F0));
        }

        [Test]
        public void HostileUnits_KillAnEnvoy_AndTheGoodsAreLost_ButNotAtPeace()
        {
            // At peace with the unit's faction the envoy walks by.
            var calm = new TradeBed(3);
            calm.Send(Faction.F0, Faction.F1);
            calm.Step(20);
            var near = calm.Envoy;
            calm.Spawn(Faction.F2, UnitKind.Infantry, near.X, near.Z);
            calm.Step(30);
            Assert.AreEqual(0, calm.Ev<EnvoyKilled>().Count());
            Assert.AreEqual(TradeTuning.EnvoyHealth, near.Health, "at peace nobody shoots");

            var b = new TradeBed(3);
            int logs = b.Stock(Faction.F0, Ware.Log);
            int id = b.Send(Faction.F0, Faction.F1);
            b.Do(new DeclareWar(Faction.F0, Faction.F2));
            b.Step(25);
            var env = b.Envoy;
            // Two soldiers of the faction at war with the sender stand on its way.
            var p1 = b.Ahead(env, 8000); var p2 = b.Ahead(env, 11000);
            b.Spawn(Faction.F2, UnitKind.Infantry, p1.x, p1.z);
            b.Spawn(Faction.F2, UnitKind.Infantry, p2.x, p2.z);
            Assert.IsTrue(b.RunUntil(200, () => b.Ev<EnvoyKilled>().Any()), "shot dead");
            var dead = b.Ev<EnvoyKilled>().Single();
            Assert.AreEqual(id, dead.EnvoyId); Assert.AreEqual(Faction.F2, (Faction)dead.By);
            Assert.AreEqual(EnvoyDeathCause.Shot, dead.Cause);
            Assert.AreEqual(3, dead.Lost.Single().Count); Assert.AreEqual(Ware.Log, dead.Lost.Single().Ware);
            Assert.IsEmpty(b.T.Envoys); Assert.IsEmpty(b.T.Offers);
            Assert.AreEqual(logs - 3, b.Stock(Faction.F0, Ware.Log), "the goods are gone");
            Assert.AreEqual(3, b.T.Lost[(int)Ware.Log]);
            Assert.AreEqual(TradeOutcome.Lost, b.T.History.Single().Outcome);
            Assert.AreEqual(1, b.T.Killed);
            b.Step(300);
            Assert.AreEqual(0, b.Ev<EnvoyHome>().Count());
        }

        [Test]
        public void UnitsPreferUnits_AndAnEnvoyInTheirWayIsLeftWhileTheyFightUnits()
        {
            var b = new TradeBed(3);
            b.Send(Faction.F0, Faction.F1);
            b.Do(new DeclareWar(Faction.F0, Faction.F2));
            b.Step(25);
            var env = b.Envoy;
            var p1 = b.Ahead(env, 8000);
            var g = b.S.Territory.Grid;
            Assert.IsTrue(b.Sim.Submit(new DevSpawnUnit(Faction.F2, UnitKind.Infantry, Pathing.NearestLandNode(g, p1.x, p1.z))).Ok);
            Assert.IsTrue(b.Sim.Submit(new DevSpawnUnit(Faction.F0, UnitKind.Infantry, Pathing.NearestLandNode(g, p1.x + 3000, p1.z))).Ok);
            b.Step(1);
            var foe = b.S.Military.Units.First(u => u.Faction == Faction.F2);
            Assert.AreEqual(TargetKind.Unit, foe.TargetKind, "the soldier has an enemy unit in sight");
            Assert.AreEqual(TradeTuning.EnvoyHealth, env.Health, "so the envoy is not its target");
        }

        private static (TradeBed b, int hq2) TwoHqBed(Faction f)
        {
            var b = new TradeBed(3);
            var t = b.S.Territory; var topo = b.S.Board.Topology;
            var hq1 = b.S.Economy.HqsOf(f)[0];
            int corner = topo.Corners.First(c => topo.VertexHexes(c).Count == 3 && topo.CornerDistance(t.HqOf(hq1.Id).Corner, c) == 2 && b.Sim.Check(new PlaceHq(f, c)).Ok);
            Assert.IsTrue(b.Do(new PlaceHq(f, corner)).Ok);
            int id = t.Hqs.Single(h => h.Faction == f && h.Id != hq1.Id).Id;
            Assert.IsTrue(EconomyModule.CompleteInstantly(b.S, id));
            b.Step(2);
            Assert.AreEqual(2, b.S.Economy.HqsOf(f).Count);
            return (b, id);
        }

        [Test]
        public void TargetHqCaptured_TheEnvoyGoesToTheNextHq_OrHomeWhenNoneIsLeft()
        {
            var (b, hq2) = TwoHqBed(Faction.F1);
            b.S.Economy.Find(hq2).Stock[(int)Ware.Brick] = 0;
            var hq1 = b.S.Economy.HqsOf(Faction.F1).First(h => h.Id != hq2);
            int id = b.Send(Faction.F0, Faction.F1);
            var first = b.T.OfferById(id).TargetHq;
            b.Step(5);
            // The HQ the envoy heads for falls to F2 (a capture).
            var ev = new EventBuffer();
            FactionRules.CaptureHq(b.S, first, Faction.F2, ev);
            b.Log.AddRange(ev.Drain());
            b.Step(1);
            if (first == hq2) Assert.AreNotEqual(first, b.T.OfferById(id).TargetHq);
            Assert.AreNotEqual(first, b.T.OfferById(id).TargetHq, "re-targeted");
            Assert.AreEqual(Faction.F1, b.S.Economy.Find(b.T.OfferById(id).TargetHq).Faction);
            Assert.IsTrue(b.RunUntil(3000, () => b.T.OfferById(id) == null || b.T.OfferById(id).Stage == OfferStage.Pending || b.Ev<TradeAccepted>().Any() || b.Ev<TradeRefused>().Any()));
            Assert.AreEqual(0, b.Ev<TradeRefused>().Count(r => r.Cause == RefusalCause.TargetGone));

            // The last HQ falls: F1 is out, the envoy turns back with its goods.
            var c = new TradeBed(5);
            int l0 = c.Stock(Faction.F0, Ware.Log);
            c.T.Judge = new WaitJudge();
            int id2 = c.Send(Faction.F0, Faction.F1);
            c.Step(10);
            var ev2 = new EventBuffer();
            FactionRules.CaptureHq(c.S, c.S.Economy.HqsOf(Faction.F1)[0].Id, Faction.F2, ev2);
            c.Log.AddRange(ev2.Drain());
            c.Step(2);
            Assert.IsTrue(c.S.Factions.IsEliminated(Faction.F1));
            Assert.AreEqual(RefusalCause.TargetGone, c.Ev<TradeRefused>().Single().Cause);
            Assert.IsTrue(c.RunUntil(3000, () => c.Ev<EnvoyHome>().Any()));
            Assert.AreEqual(l0, c.Stock(Faction.F0, Ware.Log));
            Assert.AreEqual(FactionReasons.NotInGame, c.Sim.Check(new OfferTrade(Faction.F0, Faction.F1, W((Ware.Log, 1)), W((Ware.Clay, 1)))).Reason);
            Assert.NotNull(id2.ToString());
        }

        [Test]
        public void TargetEliminatedWhileTheEnvoyWaits_SendsItHome()
        {
            var b = new TradeBed(3);
            b.T.Judge = new WaitJudge();
            int l0 = b.Stock(Faction.F0, Ware.Log);
            b.Send(Faction.F0, Faction.F1);
            Assert.IsTrue(b.RunUntil(3000, () => b.T.PendingFor(Faction.F1).Count == 1));
            var ev = new EventBuffer();
            FactionRules.CaptureHq(b.S, b.S.Economy.HqsOf(Faction.F1)[0].Id, Faction.F3, ev);
            b.Log.AddRange(ev.Drain());
            b.Step(2);
            Assert.AreEqual(RefusalCause.TargetGone, b.Ev<TradeRefused>().Single().Cause);
            Assert.IsTrue(b.RunUntil(3000, () => b.Ev<EnvoyHome>().Any()));
            Assert.AreEqual(l0, b.Stock(Faction.F0, Ware.Log));
        }

        [Test]
        public void SenderHomeHqGone_DeliversToItsNextHq_AndTheGoodsAreLostWhenItHasNone()
        {
            var (b, hq2) = TwoHqBed(Faction.F0);
            var hq1 = b.S.Economy.HqsOf(Faction.F0).First(h => h.Id != hq2);
            // Give F0's second HQ the log, so the offer leaves from it (it is the one holding the wares).
            hq1.Stock[(int)Ware.Log] = 0;
            b.S.Economy.Find(hq2).Stock[(int)Ware.Log] = 8; b.Initial = b.TotalGoods();
            int id = b.Send(Faction.F0, Faction.F1, W((Ware.Log, 5)), W((Ware.Brick, 6)), hq2);
            int origin = b.T.EnvoyById(id).HomeHq;
            Assert.AreEqual(hq2, origin);
            b.Step(10);
            var ev = new EventBuffer();
            FactionRules.CaptureHq(b.S, hq2, Faction.F4, ev); b.Log.AddRange(ev.Drain());
            Assert.IsTrue(b.RunUntil(3000, () => b.Ev<EnvoyHome>().Any()), "home all the same");
            var home = b.Ev<EnvoyHome>().Single();
            Assert.AreEqual(hq1.Id, home.HqId, "to the HQ it still has");
            Assert.AreEqual(5, b.Stock(Faction.F0, Ware.Log) - 0 - (b.S.Economy.Find(hq2).Faction == Faction.F0 ? 8 : 0), "the refused goods are in");

            // No HQ left at all: eliminated, the envoy and its goods are gone.
            var c = new TradeBed(3);
            int l0 = c.Stock(Faction.F0, Ware.Log);
            c.Send(Faction.F0, Faction.F1);
            c.Step(10);
            var ev2 = new EventBuffer();
            FactionRules.CaptureHq(c.S, c.S.Economy.HqsOf(Faction.F0)[0].Id, Faction.F4, ev2); c.Log.AddRange(ev2.Drain());
            c.Step(3);
            Assert.IsTrue(c.S.Factions.IsEliminated(Faction.F0));
            Assert.IsEmpty(c.T.Envoys);
            Assert.AreEqual(EnvoyDeathCause.SenderEliminated, c.Ev<EnvoyKilled>().Single().Cause);
            Assert.AreEqual(3, c.T.Lost[(int)Ware.Log]);
            Assert.AreEqual(0, c.Ev<EnvoyHome>().Count());
            Assert.Greater(l0, 0);
        }

        [Test]
        public void SameCommands_SameHash_AndTradeChangesTheHash()
        {
            ulong Run(ulong seed, bool trade)
            {
                var b = new TradeBed(seed);
                var h0 = b.Sim.StateHash();
                if (trade)
                {
                    b.Send(Faction.F0, Faction.F1);
                    b.Send(Faction.F2, Faction.F3, W((Ware.Plank, 2)), W((Ware.Stone, 3)));
                    b.Step(100);
                    Assert.AreNotEqual(h0, b.Sim.StateHash());
                }
                b.Step(2500);
                return b.Sim.StateHash();
            }
            Assert.AreEqual(Run(7, true), Run(7, true));
            Assert.AreNotEqual(Run(7, true), Run(7, false), "the trade is in the hash");
            Assert.AreNotEqual(Run(7, true), Run(8, true));

            // Mid-trip positions are in the hash.
            var a = new TradeBed(7); var c = new TradeBed(7);
            a.Send(Faction.F0, Faction.F1); c.Send(Faction.F0, Faction.F1);
            a.Step(10); c.Step(11);
            Assert.AreNotEqual(a.Sim.StateHash(), c.Sim.StateHash());
        }

        [Test]
        public void Accessors_ShowActiveEnvoys_PendingOffers_AndTheHistory()
        {
            var b = new TradeBed(3);
            for (int i = 0; i < TradeTuning.HistorySize + 6; i++)
            {
                b.RunUntil(2000, () => b.T.EnvoyCountOf(Faction.F1) < TradeTuning.MaxEnvoysPerFaction);
                b.Send(Faction.F1, Faction.F0, W((Ware.Log, 1)), W((Ware.Clay, 1)));
                b.Step(3);
                foreach (var o in b.T.PendingFor(Faction.F0)) b.Do(new AnswerTrade(Faction.F0, o.Id, i % 2 == 0));
            }
            b.RunUntil(5000, () => b.T.Envoys.Count == 0);
            Assert.LessOrEqual(b.T.History.Count, TradeTuning.HistorySize);
            Assert.AreEqual(TradeTuning.HistorySize, b.T.History.Count, "only the last N are kept");
            Assert.Greater(b.T.HistoryOf(Faction.F0).Count, 0);
            Assert.AreEqual(0, b.T.HistoryOf(Faction.F4).Count);
            CollectionAssert.IsOrdered(b.T.History.Select(r => r.OfferId));
            var last = b.T.History.Last();
            Assert.AreEqual(Faction.F1, last.From);
        }

        [Test]
        public void ManyRandomTrades_BetweenSixFactions_KeepEveryInvariantAndEveryWare()
        {
            for (ulong seed = 1; seed <= 3; seed++)
            {
                var b = new TradeBed(seed * 11, GameSetup.NoHuman);
                var rng = new Rng(seed * 977).Fork("trade.test");
                var sentWares = new[] { Ware.Log, Ware.Clay, Ware.Plank, Ware.Stone, Ware.Brick };
                int offers = 0;
                for (int tick = 0; tick < 4000; tick++)
                {
                    b.Step();
                    if (b.S.IsGameOver) break;
                    if (tick % 7 == 0)
                    {
                        var from = (Faction)rng.NextInt(6); var to = (Faction)rng.NextInt(6);
                        var gw = sentWares[rng.NextInt(sentWares.Length)]; var ww = sentWares[rng.NextInt(sentWares.Length)];
                        var r = b.Sim.Submit(new OfferTrade(from, to, W((gw, 1 + rng.NextInt(4))), W((ww, 1 + rng.NextInt(4)))));
                        if (r.Ok) offers++;
                    }
                    if (tick % 400 == 100) { var a = (Faction)rng.NextInt(6); var c = (Faction)rng.NextInt(6); if (a != c) b.Sim.Submit(new DeclareWar(a, c)); }
                    if (tick % 400 == 130) { var a = (Faction)rng.NextInt(6); b.Sim.Submit(new DevSpawnUnit(a, rng.NextInt(2) == 0 ? UnitKind.Infantry : UnitKind.Vehicle, b.S.Economy.HqsOf((Faction)rng.NextInt(6)).FirstOrDefault()?.Node ?? 0)); }
                }
                Assert.Greater(offers, 50, "seed " + seed);
                Assert.Greater(b.T.Accepted, 5, "some were accepted (seed " + seed + ")");
                Assert.Greater(b.T.Refused, 3, "some were refused (seed " + seed + ")");
                b.Conserved();
                TestContext.WriteLine("seed " + seed + ": offers " + offers + ", accepted " + b.T.Accepted + ", refused " + b.T.Refused + ", killed " + b.T.Killed + ", game over " + b.S.IsGameOver);
            }
        }

        private sealed class RandomJudge : ITradeJudge
        {
            private readonly Rng _rng = new Rng(5);
            public TradeVerdict Decide(GodState s, TradeOffer o) => (TradeVerdict)_rng.NextInt(3);
        }

        [Test]
        public void RandomAnswersAndTimeouts_AlsoKeepTheLedgerExact()
        {
            var b = new TradeBed(9, GameSetup.NoHuman);
            b.T.Judge = new RandomJudge();
            var rng = new Rng(31).Fork("x");
            for (int tick = 0; tick < 3500; tick++)
            {
                b.Step();
                if (tick % 11 == 0)
                    b.Sim.Submit(new OfferTrade((Faction)rng.NextInt(6), (Faction)rng.NextInt(6), W((Ware.Log, 1 + rng.NextInt(3))), W((Ware.Clay, 1 + rng.NextInt(3)))));
                if (tick % 211 == 0 && b.T.Offers.Count > 0)
                {
                    var o = b.T.Offers[rng.NextInt(b.T.Offers.Count)];
                    if (o.Stage == OfferStage.Pending) b.Sim.Submit(new AnswerTrade(o.To, o.Id, rng.NextInt(2) == 0));
                }
            }
            TestContext.WriteLine("offered " + b.Ev<TradeOffered>().Count() + " arrived " + b.Ev<EnvoyArrived>().Count() + " acc " + b.T.Accepted + " ref " + b.T.Refused + " killed " + b.T.Killed + " tmo " + b.Ev<TradeRefused>().Count(r => r.Cause == RefusalCause.Timeout));
            Assert.Greater(b.Ev<TradeRefused>().Count(r => r.Cause == RefusalCause.Timeout), 0, "some waited the full 60 s");
            Assert.Greater(b.T.Accepted, 3);
            b.Conserved();
        }
    }
}
