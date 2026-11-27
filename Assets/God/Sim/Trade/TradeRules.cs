using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>Refusal codes of the trade commands.</summary>
    public static class TradeReasons
    {
        public const string AtWar = "trade_at_war";
        public const string BadWare = "trade_bad_ware";
        public const string BadAmount = "trade_bad_amount";
        public const string SameWare = "trade_same_ware";
        public const string TooManyEnvoys = "trade_too_many_envoys";
        public const string NoTargetHq = "trade_no_target_hq";
        public const string NoStock = "trade_no_stock";
        public const string NoPath = "trade_no_path";
        public const string NotPending = "trade_not_pending";
    }

    /// <summary>
    /// The default judge: accept when the target can pay the wanted wares and the deal is roughly fair by the ware value
    /// table (<see cref="TradeTuning.ValueOf"/>): what the target receives is worth at least
    /// <see cref="TradeTuning.FairPercent"/>% of what it pays. A generous offer is accepted. P9's AI replaces it by
    /// setting <see cref="TradeState.Judge"/>.
    /// </summary>
    public sealed class DefaultTradeJudge : ITradeJudge
    {
        public TradeVerdict Decide(GodState s, TradeOffer offer)
        {
            if (!TradeRules.CanPay(s, offer.To, offer.Want)) return TradeVerdict.Refuse;
            int give = TradeRules.Value(offer.Give), want = TradeRules.Value(offer.Want);
            return give * 100 >= want * TradeTuning.FairPercent ? TradeVerdict.Accept : TradeVerdict.Refuse;
        }
    }

    /// <summary>Trade queries and the answer rule. Read-only queries are safe for views and bots.</summary>
    public static class TradeRules
    {
        // ---------------- helpers ----------------

        /// <summary>Merges duplicates, drops zero counts and sorts by ware. Null when a count is negative or a ware cannot be traded.</summary>
        public static WareCount[] Normalize(IEnumerable<WareCount> lines, out string badReason)
        {
            badReason = null;
            var n = new int[Wares.Count];
            if (lines != null)
                foreach (var l in lines)
                {
                    if (l.Count == 0) continue;
                    if (l.Count < 0) { badReason = TradeReasons.BadAmount; return null; }
                    if (!TradeTuning.IsTradable(l.Ware)) { badReason = TradeReasons.BadWare; return null; }
                    n[(int)l.Ware] += l.Count;
                    if (n[(int)l.Ware] > 1000) { badReason = TradeReasons.BadAmount; return null; }
                }
            var list = new List<WareCount>();
            for (int w = 1; w < Wares.Count; w++) if (n[w] > 0) list.Add(new WareCount((Ware)w, n[w]));
            return list.ToArray();
        }

        public static int Total(WareCount[] a) { int n = 0; foreach (var l in a) n += l.Count; return n; }

        /// <summary>Trade value of a list of wares (the default judge's table).</summary>
        public static int Value(WareCount[] a) { int n = 0; foreach (var l in a) n += l.Count * TradeTuning.ValueOf(l.Ware); return n; }

        /// <summary>Free stock of one ware in an HQ: stock not already promised to a carrier.</summary>
        public static int FreeStock(EcoBuilding hq, Ware w) => Math.Max(0, hq.Stock[(int)w] - hq.StockReserved[(int)w]);

        /// <summary>Free stock of one ware over all of a faction's finished HQs.</summary>
        public static int FreeStock(GodState s, Faction f, Ware w)
        {
            int n = 0;
            foreach (var hq in s.Economy.HqsOf(f)) n += FreeStock(hq, w);
            return n;
        }

        /// <summary>True when the faction's HQs together have the wares free in stock.</summary>
        public static bool CanPay(GodState s, Faction f, WareCount[] want)
        {
            foreach (var l in want) if (FreeStock(s, f, l.Ware) < l.Count) return false;
            return true;
        }

        public static int HqX(GodState s, EcoBuilding hq) => Pathing.NodeMmX(s.Territory.Grid, hq.Node);
        public static int HqZ(GodState s, EcoBuilding hq) => Pathing.NodeMmZ(s.Territory.Grid, hq.Node);

        /// <summary>The faction's finished HQ nearest to a point (ties: lowest id), or null when it has none.</summary>
        public static EcoBuilding NearestHq(GodState s, Faction f, int x, int z)
        {
            EcoBuilding best = null; long bd = long.MaxValue;
            foreach (var hq in s.Economy.HqsOf(f))
            {
                long d = Pathing.Dist2(x, z, HqX(s, hq), HqZ(s, hq));
                if (d < bd) { bd = d; best = hq; }
            }
            return best;
        }

        /// <summary>True when the HQ exists, is finished and belongs to the faction.</summary>
        public static bool IsLiveHq(GodState s, int hqId, Faction f)
        {
            var hq = s.Economy.Find(hqId);
            return hq != null && hq.IsHq && hq.Built && hq.Faction == f;
        }

        /// <summary>
        /// The sender's HQ that gives the wares: one with all of them free in stock, the one nearest the target (ties: lowest id).
        /// <paramref name="wanted"/> 0 means any. Null when no single HQ has them.
        /// </summary>
        public static EcoBuilding PickSenderHq(GodState s, Faction from, WareCount[] give, int tx, int tz, int wanted)
        {
            EcoBuilding best = null; long bd = long.MaxValue;
            foreach (var hq in s.Economy.HqsOf(from))
            {
                if (wanted != 0 && hq.Id != wanted) continue;
                bool ok = true;
                foreach (var l in give) if (FreeStock(hq, l.Ware) < l.Count) { ok = false; break; }
                if (!ok) continue;
                long d = Pathing.Dist2(tx, tz, HqX(s, hq), HqZ(s, hq));
                if (d < bd) { bd = d; best = hq; }
            }
            return best;
        }

        // ---------------- the answer ----------------

        /// <summary>
        /// Answers a pending offer. Accepted: the give wares go into the target's HQ stock, the want wares come out of the
        /// target's free stock (the HQ the envoy stands at first, then the others by id) into the envoy's pack, and the envoy
        /// walks home with them. If the target can no longer pay, or an HQ to deliver to is missing, the offer counts as
        /// refused. Refused: the envoy walks home with the original goods. Returns false when there is no such pending offer.
        /// </summary>
        public static bool Answer(GodState s, int offerId, bool accept, EventBuffer ev = null)
        {
            var t = s.Trade;
            var o = t?.OfferById(offerId);
            var env = t?.EnvoyById(offerId);
            if (o == null || env == null || o.Stage != OfferStage.Pending) return false;
            if (!accept) { Resolve(s, o, env, false, RefusalCause.Declined, ev); return true; }
            if (s.Factions.IsEliminated(o.To) || !CanPay(s, o.To, o.Want)) { Resolve(s, o, env, false, s.Factions.IsEliminated(o.To) ? RefusalCause.TargetGone : RefusalCause.CannotPay, ev); return true; }
            Resolve(s, o, env, true, RefusalCause.Declined, ev);
            return true;
        }

        /// <summary>Finishes an unanswered offer (accepted or refused for a cause): moves the goods, sends the envoy home, records and announces it.</summary>
        internal static void Resolve(GodState s, TradeOffer o, Envoy env, bool accept, RefusalCause cause, EventBuffer ev)
        {
            var t = s.Trade; var e = s.Economy;
            t.Offers.Remove(o);
            env.Stage = EnvoyStage.Homeward;
            env.Body.Path = null; env.Body.PathGoalNode = -1; env.Stuck = 0;
            if (accept)
            {
                var arrival = e.Find(o.TargetHq);
                if (arrival == null || !arrival.IsHq || !arrival.Built || arrival.Faction != o.To) arrival = NearestHq(s, o.To, env.X, env.Z);
                // arrival is not null here: Answer checked CanPay, which needs a finished HQ whenever something is wanted.
                for (int w = 1; w < Wares.Count; w++)
                {
                    int n = env.Cargo[w];
                    if (n == 0) continue;
                    arrival.Stock[w] += n; t.Arrived[w] += n; env.Cargo[w] = 0;
                }
                var hqs = e.HqsOf(o.To);
                foreach (var l in o.Want)
                {
                    int need = l.Count;
                    int wi = (int)l.Ware;
                    int take = Math.Min(need, FreeStock(arrival, l.Ware));
                    arrival.Stock[wi] -= take; need -= take;
                    foreach (var hq in hqs)
                    {
                        if (need == 0) break;
                        if (hq == arrival) continue;
                        take = Math.Min(need, FreeStock(hq, l.Ware));
                        hq.Stock[wi] -= take; need -= take;
                    }
                    env.Cargo[wi] += l.Count; t.Departed[wi] += l.Count;
                }
                env.CarriesPayment = true;
                t.Accepted++;
                t.Record(new TradeRecord { OfferId = o.Id, From = o.From, To = o.To, Give = o.Give, Want = o.Want, Outcome = TradeOutcome.Accepted, Tick = s.Tick });
                ev?.Add(new TradeAccepted { OfferId = o.Id, From = o.From, To = o.To, Give = o.Give, Want = o.Want });
            }
            else
            {
                t.Refused++;
                t.Record(new TradeRecord { OfferId = o.Id, From = o.From, To = o.To, Give = o.Give, Want = o.Want, Outcome = TradeOutcome.Refused, Cause = cause, Tick = s.Tick });
                ev?.Add(new TradeRefused { OfferId = o.Id, From = o.From, To = o.To, Give = o.Give, Want = o.Want, Cause = cause });
            }
        }

        /// <summary>Removes an envoy; its cargo is lost. An unanswered offer is recorded as lost.</summary>
        internal static void KillEnvoy(GodState s, Envoy env, int by, EnvoyDeathCause cause, EventBuffer ev)
        {
            var t = s.Trade;
            var lost = env.CargoList();
            for (int w = 1; w < Wares.Count; w++) { t.Lost[w] += env.Cargo[w]; env.Cargo[w] = 0; }
            var o = t.OfferById(env.Id);
            if (o != null)
            {
                t.Offers.Remove(o);
                t.Record(new TradeRecord { OfferId = o.Id, From = o.From, To = o.To, Give = o.Give, Want = o.Want, Outcome = TradeOutcome.Lost, Tick = s.Tick });
            }
            t.Envoys.Remove(env);
            t.Killed++;
            ev?.Add(new EnvoyKilled { EnvoyId = env.Id, OfferId = env.Id, Owner = env.Faction, By = by, X = env.X, Z = env.Z, Lost = lost.ToArray(), Cause = cause });
        }

        /// <summary>A faction was eliminated: its envoys vanish with their goods (called by the elimination rule).</summary>
        internal static void OnEliminated(GodState s, Faction f, EventBuffer ev)
        {
            var t = s.Trade;
            if (t == null) return;
            foreach (var env in new List<Envoy>(t.Envoys))
                if (env.Faction == f) KillEnvoy(s, env, -1, EnvoyDeathCause.SenderEliminated, ev);
        }
    }
}
