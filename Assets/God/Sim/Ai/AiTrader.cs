using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// The faction AI's trade (P11), on P8's envoys. Owner rules: trade with anyone who is not winning, preferring the
    /// worst-placed faction; trade for missing wares and target builds, or to shed surplus; accept fair offers that help;
    /// never trade with a faction at war (the sim refuses that anyway).
    /// <list type="bullet">
    /// <item><b>Propose.</b> A ware the brain needs (<see cref="AiBrain.WareNeeds"/>) and cannot make, because its land is in
    /// none of its HQ areas (the stalled-without-mountain case), is asked for at once; a ware it could make is asked for
    /// only when it has <see cref="AiTuning.TradeShedSurplus"/> spare wares to pay with. Partners: alive, at peace, not
    /// the sole VP leader, with the ware to spare; the lowest VP first, then the most to spare. Payment: wares of its own
    /// surplus the partner needs, worth at least what it asks (<see cref="TradeTuning.ValueOf"/>, rounded in the
    /// partner's favour), all from one HQ.</item>
    /// <item><b>Rate.</b> At most one new offer per brain per <see cref="AiTuning.TradeIntervalTicks"/>: a brain proposes
    /// only on the one think of each window that falls on its slot (stateless, so a loaded game goes on alike). Never
    /// more than <see cref="TradeTuning.MaxEnvoysPerFaction"/> envoys out. A ware a partner refused is not asked of it
    /// again for <see cref="AiTuning.TradeRefusedMemoryTicks"/> (read from the trade history).</item>
    /// <item><b>Judge.</b> <see cref="JudgeIncoming"/>: refuse the sole VP leader and anyone at war; accept when what it
    /// receives is worth at least <see cref="AiTuning.TradeFairPercent"/>% of what it pays, every wanted ware is surplus,
    /// and at least one received ware is needed. <see cref="AiTradeJudge"/> plugs this into
    /// <see cref="TradeState.Judge"/>; it never waits.</item>
    /// </list>
    /// Keeps no memory: everything is read from <see cref="GodState"/> each think, ties go to the lowest id after a
    /// <see cref="AiBrain.ThinkRng"/> shuffle of equals, so games stay deterministic and saves need nothing extra.
    /// </summary>
    public sealed class AiTrader : IAiTrader
    {
        /// <summary>The director whose brains are the partners (set by <see cref="AiDirector.Attach"/>); partners without a brain are estimated from their stock.</summary>
        public AiDirector Director;

        /// <summary>Order in which needed wares are asked for: the building wares first.</summary>
        private static readonly Ware[] AskOrder =
        {
            Ware.Stone, Ware.Plank, Ware.Brick, Ware.Mortar, Ware.Steel, Ware.Log, Ware.Clay, Ware.Ore, Ware.Grain,
            Ware.Iron, Ware.Bread, Ware.Bronze, Ware.Pottery, Ware.Leather, Ware.Cloth,
        };

        /// <summary>What a faction keeps back before it calls a ware surplus: a Citadel and an HQ.</summary>
        public static int Keep(Ware w) => EconomyDefs.Citadel.Cost[(int)w] + EconomyDefs.Hq.Cost[(int)w];

        /// <summary>The sole VP leader (ahead of everyone else), or null when the lead is shared.</summary>
        public static Faction? SoleLeader(GodState s)
        {
            int top = -1, count = 0; Faction lead = Faction.F0;
            foreach (var f in FactionRules.Alive(s))
            {
                int v = FactionRules.VictoryPoints(s, f);
                if (v > top) { top = v; count = 1; lead = f; }
                else if (v == top) count++;
            }
            return count == 1 ? lead : (Faction?)null;
        }

        /// <summary>True when a brain may send a new offer on this tick: the one think of its trade window.</summary>
        public static bool InTradeSlot(AiBrain brain)
        {
            var t = brain.Tune;
            int interval = Math.Max(t.ThinkIntervalTicks, t.TradeIntervalTicks);
            int offset = (int)brain.Side * t.ThinkIntervalTicks / FactionInfo.Max;
            return (brain.Sim.State.Tick + offset) % interval < Math.Max(1, t.ThinkIntervalTicks);
        }

        // ---------------- partners' wants ----------------

        private AiBrain BrainOf(Faction f) => Director != null && Director.Enabled ? Director.BrainOf(f) : null;

        private int[] NeedsOf(GodState s, Faction f)
        {
            var b = BrainOf(f);
            if (b != null) return b.WareNeeds();
            var n = new int[Wares.Count];
            for (int w = 1; w < Wares.Count; w++) n[w] = Math.Max(0, Keep((Ware)w) - TradeRules.FreeStock(s, f, (Ware)w));
            return n;
        }

        private int[] SurplusOf(GodState s, Faction f)
        {
            var b = BrainOf(f);
            if (b != null) return b.WareSurplus();
            var n = new int[Wares.Count];
            for (int w = 1; w < Wares.Count; w++) n[w] = Math.Max(0, TradeRules.FreeStock(s, f, (Ware)w) - Keep((Ware)w));
            return n;
        }

        // ---------------- answering ----------------

        public void ConsiderOffers(AiBrain brain)
        {
            var s = brain.Sim.State;
            if (s.Trade == null) return;
            foreach (var o in s.Trade.PendingFor(brain.Side))
            {
                bool yes = JudgeIncoming(brain, o.From, Counts(o.Give), Counts(o.Want));
                brain.Sim.Submit(new AnswerTrade(brain.Side, o.Id, yes));
                brain.LastActions.Add((yes ? "accept" : "refuse") + " offer " + o.Id);
            }
        }

        public bool JudgeIncoming(AiBrain brain, Faction from, int[] give, int[] want)
        {
            var s = brain.Sim.State;
            if (from == brain.Side || s.Factions.IsEliminated(from) || s.Factions.AtWar(brain.Side, from)) return false;
            if (SoleLeader(s) == from) return false;
            int got = 0, paid = 0;
            for (int w = 1; w < Wares.Count; w++) { got += give[w] * TradeTuning.ValueOf((Ware)w); paid += want[w] * TradeTuning.ValueOf((Ware)w); }
            if (paid <= 0 || got * 100 < paid * brain.Tune.TradeFairPercent) return false;
            var surplus = brain.WareSurplus();
            for (int w = 1; w < Wares.Count; w++) if (want[w] > surplus[w]) return false;
            var needs = brain.WareNeeds();
            for (int w = 1; w < Wares.Count; w++) if (give[w] > 0 && needs[w] > 0) return true;
            // Q4: shedding a glut also helps: every ware asked for lies far above what it keeps (a mountain faction sitting
            // on a heap of stone sells it to one without mountain for any fair payment).
            return Sheds(brain.Tune, surplus, want);
        }

        /// <summary>True when every wanted ware is surplus by at least <see cref="AiTuning.TradeShedSurplus"/> beyond the ask.</summary>
        public static bool Sheds(AiTuning tune, int[] surplus, int[] want)
        {
            bool any = false;
            for (int w = 1; w < Wares.Count; w++)
            {
                if (want[w] <= 0) continue;
                if (surplus[w] < want[w] + tune.TradeShedSurplus) return false;
                any = true;
            }
            return any;
        }

        public static int[] Counts(WareCount[] list)
        {
            var a = new int[Wares.Count];
            if (list != null) foreach (var l in list) a[(int)l.Ware] += l.Count;
            return a;
        }

        // ---------------- proposing ----------------

        public void ProposeTrades(AiBrain brain)
        {
            var s = brain.Sim.State; var t = s.Trade;
            if (t == null || s.IsGameOver || !InTradeSlot(brain)) return;
            if (t.EnvoyCountOf(brain.Side) >= TradeTuning.MaxEnvoysPerFaction) return;
            var offer = Plan(brain);
            if (offer == null) return;
            if (brain.Sim.Submit(offer).Ok)
                brain.LastActions.Add("offer " + FactionInfo.Name(offer.To) + " " + string.Join("+", Array.ConvertAll(offer.Give, g => g.ToString()))
                    + " for " + string.Join("+", Array.ConvertAll(offer.Want, g => g.ToString())));
        }

        /// <summary>The offer this brain would send now, checked against the rules, or null. Reads state only.</summary>
        public OfferTrade Plan(AiBrain brain)
        {
            var s = brain.Sim.State; var me = brain.Side; var tune = brain.Tune;
            var need = brain.WareNeeds(); // also refreshes the brain's look (its lands)
            var lacks = new bool[Wares.Count];
            foreach (var w in AskOrder) lacks[(int)w] = brain.LacksLandFor(w);
            var mySurplus = brain.WareSurplus();
            int spare = 0;
            for (int w = 1; w < Wares.Count; w++) if (TradeTuning.IsTradable((Ware)w)) spare += mySurplus[w];

            // Partners: alive, at peace, not the sole leader, with a finished HQ; lowest VP first.
            var leader = SoleLeader(s);
            var partners = new List<(int vp, int roll, Faction f)>();
            foreach (var f in FactionRules.Alive(s))
            {
                if (f == me || s.Factions.AtWar(me, f) || leader == f || s.Economy.HqsOf(f).Count == 0) continue;
                partners.Add((FactionRules.VictoryPoints(s, f), brain.ThinkRng != null ? brain.ThinkRng.NextInt(1000) : 0, f));
            }
            if (partners.Count == 0) return null;
            partners.Sort((a, b) => a.vp != b.vp ? a.vp.CompareTo(b.vp) : a.roll != b.roll ? a.roll.CompareTo(b.roll) : ((int)a.f).CompareTo((int)b.f));
            var surplusOf = new Dictionary<Faction, int[]>();
            var needsOf = new Dictionary<Faction, int[]>();
            foreach (var p in partners) { surplusOf[p.f] = SurplusOf(s, p.f); needsOf[p.f] = NeedsOf(s, p.f); }
            // The partners' looks may have run: refresh this brain's own look (it is the thinking brain).
            brain.WareNeeds();

            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1 && spare < tune.TradeShedSurplus) break;
                foreach (var ware in AskOrder)
                {
                    int wi = (int)ware;
                    if (need[wi] <= 0 || (pass == 0) != lacks[wi]) continue;
                    if (pass == 1 && lacks[wi]) continue;
                    int ask = Math.Min(Math.Min(need[wi], tune.TradeMaxWant), TradeTuning.MaxWaresPerSide);
                    // Among the partners of the lowest VP that have it to spare, the one with the most to spare.
                    Faction? pick = null; int pickVp = int.MaxValue, pickSpare = 0;
                    foreach (var p in partners)
                    {
                        if (p.vp > pickVp) break;
                        int theirs = surplusOf[p.f][wi];
                        if (theirs <= 0 || RecentlyRefused(s, me, p.f, ware, tune.TradeRefusedMemoryTicks)) continue;
                        bool sheds = theirs >= Math.Min(ask, theirs) + tune.TradeShedSurplus;
                        if (Pay(s, me, mySurplus, needsOf[p.f], ware, Math.Min(ask, theirs), sheds, out _, out _) == null) continue;
                        if (pick == null || theirs > pickSpare) { pick = p.f; pickVp = p.vp; pickSpare = theirs; }
                    }
                    if (pick == null) continue;
                    int n = Math.Min(ask, pickSpare);
                    var give = Pay(s, me, mySurplus, needsOf[pick.Value], ware, n, pickSpare >= n + tune.TradeShedSurplus, out int hqId, out n);
                    var cmd = new OfferTrade(me, pick.Value, give, new[] { new WareCount(ware, n) }, hqId);
                    if (brain.Sim.Check(cmd).Ok) return cmd;
                }
            }
            return null;
        }

        /// <summary>True when <paramref name="to"/> refused an offer of <paramref name="from"/> asking this ware within the memory.</summary>
        public static bool RecentlyRefused(GodState s, Faction from, Faction to, Ware ware, int memoryTicks)
        {
            foreach (var r in s.Trade.History)
            {
                if (r.From != from || r.To != to || r.Outcome != TradeOutcome.Refused || s.Tick - r.Tick > memoryTicks) continue;
                if (r.Cause != RefusalCause.Declined && r.Cause != RefusalCause.CannotPay && r.Cause != RefusalCause.Timeout) continue;
                foreach (var l in r.Want) if (l.Ware == ware) return true;
            }
            return false;
        }

        /// <summary>
        /// The payment for <paramref name="count"/> of <paramref name="ware"/> (the count may drop to what can be paid for):
        /// surplus wares, the ones the partner needs most first (at least one of them needed, unless the partner
        /// <paramref name="sheds"/> a glut of the ware), worth at least the ask (rounded up in the partner's favour), from the
        /// first HQ by id that can pay it all. Null when no payment works.
        /// </summary>
        private static WareCount[] Pay(GodState s, Faction me, int[] mySurplus, int[] theirNeeds, Ware ware, int count, bool sheds, out int hqId, out int asked)
        {
            hqId = 0; asked = count;
            for (int n = count; n >= 1; n--)
            {
                int value = n * TradeTuning.ValueOf(ware);
                foreach (var hq in s.Economy.HqsOf(me))
                {
                    // Wares the partner needs first (most needed first), then other spare wares; at least one must be needed.
                    var avail = new List<(int need, int spare, Ware w)>();
                    bool helps = false;
                    for (int w = 1; w < Wares.Count; w++)
                    {
                        if (w == (int)ware || !TradeTuning.IsTradable((Ware)w)) continue;
                        int c = Math.Min(mySurplus[w], TradeRules.FreeStock(hq, (Ware)w));
                        if (c <= 0) continue;
                        avail.Add((theirNeeds[w], c, (Ware)w));
                        if (theirNeeds[w] > 0) helps = true;
                    }
                    if (!helps && !sheds) continue;
                    avail.Sort((a, b) => a.need != b.need ? b.need.CompareTo(a.need) : a.spare != b.spare ? b.spare.CompareTo(a.spare) : ((int)a.w).CompareTo((int)b.w));
                    var give = new int[Wares.Count];
                    int got = 0, units = 0;
                    foreach (var a in avail)
                    {
                        int per = TradeTuning.ValueOf(a.w);
                        int take = Math.Min(a.spare, (value - got + per - 1) / per); // rounded up: the partner's favour
                        take = Math.Min(take, TradeTuning.MaxWaresPerSide - units);
                        give[(int)a.w] += take; got += take * per; units += take;
                        if (got >= value || units >= TradeTuning.MaxWaresPerSide) break;
                    }
                    if (got < value) continue;
                    hqId = hq.Id; asked = n;
                    var list = new List<WareCount>();
                    for (int w = 1; w < Wares.Count; w++) if (give[w] > 0) list.Add(new WareCount((Ware)w, give[w]));
                    return list.ToArray();
                }
            }
            return null;
        }
    }

    /// <summary>
    /// <see cref="ITradeJudge"/> for AI targets: the target's brain answers by <see cref="AiTrader.JudgeIncoming"/>. A
    /// target without a brain falls back to <see cref="DefaultTradeJudge"/>. Set on <see cref="TradeState.Judge"/> by
    /// <see cref="AiDirector.Attach"/> whenever brains start (it is not saved, so after every load too).
    /// </summary>
    public sealed class AiTradeJudge : ITradeJudge
    {
        public readonly AiDirector Director;
        public readonly IAiTrader Trader;
        private static readonly DefaultTradeJudge Fallback = new DefaultTradeJudge();

        public AiTradeJudge(AiDirector director, IAiTrader trader) { Director = director; Trader = trader; }

        public TradeVerdict Decide(GodState s, TradeOffer offer)
        {
            var b = Director != null && Director.Enabled ? Director.BrainOf(offer.To) : null;
            if (b == null) return Fallback.Decide(s, offer);
            if (!TradeRules.CanPay(s, offer.To, offer.Want)) return TradeVerdict.Refuse;
            return Trader.JudgeIncoming(b, offer.From, AiTrader.Counts(offer.Give), AiTrader.Counts(offer.Want)) ? TradeVerdict.Accept : TradeVerdict.Refuse;
        }
    }
}
