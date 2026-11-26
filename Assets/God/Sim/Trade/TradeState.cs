using System.Collections.Generic;

namespace God.Sim
{
    public sealed partial class GodState
    {
        /// <summary>Envoys, offers and trade history (P8). Null until <see cref="TradeModule.StartGame"/>.</summary>
        public TradeState Trade;
    }

    /// <summary>A ware and how many of it.</summary>
    public struct WareCount
    {
        public Ware Ware;
        public int Count;
        public WareCount(Ware w, int n) { Ware = w; Count = n; }
        public override string ToString() => Count + " " + Wares.Name(Ware);
    }

    public enum OfferStage
    {
        /// <summary>The envoy is on its way to the target.</summary>
        InFlight = 0,
        /// <summary>The envoy has arrived: the target is to answer.</summary>
        Pending = 1,
    }

    /// <summary>An offer that has not been answered yet. Once answered it leaves <see cref="TradeState.Offers"/> and the result goes to the history.</summary>
    public sealed class TradeOffer
    {
        /// <summary>Equals the id of the envoy that carries it.</summary>
        public int Id;
        public Faction From;
        public Faction To;
        /// <summary>What the sender gives, ascending by ware. Left the sender's stock when the offer was sent.</summary>
        public WareCount[] Give;
        /// <summary>What the sender wants, ascending by ware.</summary>
        public WareCount[] Want;
        public OfferStage Stage;
        public long SentTick;
        public long ArrivedTick;
        /// <summary>Ticks left to answer while Pending.</summary>
        public int TicksLeft;
        /// <summary>The HQ the envoy is heading for or standing at.</summary>
        public int TargetHq;

        public int TotalGive { get { int n = 0; foreach (var g in Give) n += g.Count; return n; } }
        public int TotalWant { get { int n = 0; foreach (var g in Want) n += g.Count; return n; } }
    }

    public enum EnvoyStage
    {
        /// <summary>Walking to the target's HQ with the goods on offer.</summary>
        ToTarget = 0,
        /// <summary>Standing at the target's HQ waiting for the answer.</summary>
        Waiting = 1,
        /// <summary>Walking home with the goods (the original ones when refused, the payment when accepted).</summary>
        Homeward = 2,
    }

    /// <summary>A trade envoy: a walker that crosses the land (not the roads) with a few wares. Owned by the sender.</summary>
    public sealed class Envoy
    {
        public int Id;
        /// <summary>The offer this envoy carries (same id).</summary>
        public int OfferId => Id;
        public Faction Faction;
        public Faction Target;
        public EnvoyStage Stage;
        public int Health = TradeTuning.EnvoyHealth;
        /// <summary>Units carried, by ware.</summary>
        public int[] Cargo = new int[Wares.Count];
        /// <summary>The HQ it set out from; replaced by the nearest remaining HQ of its owner when that one is gone.</summary>
        public int HomeHq;
        /// <summary>HQ of the target it is heading for or standing at.</summary>
        public int TargetHq;
        /// <summary>True once the answer was "accepted": it carries the payment home.</summary>
        public bool CarriesPayment;
        /// <summary>The body: position, facing and path cache (not a military unit; not in the military's list).</summary>
        public MilUnit Body = new MilUnit { Kind = UnitKind.Infantry };
        /// <summary>Ticks the "hit" flash still shows.</summary>
        public int HitFlash;
        /// <summary>Ticks without progress while walking; at <see cref="TradeTuning.StuckTicks"/> the walk is given up.</summary>
        public int Stuck;

        public int X => Body.X;
        public int Z => Body.Z;
        public double PosX => Body.X / 1000.0;
        public double PosZ => Body.Z / 1000.0;
        public float FacingDegrees => Body.FacingDegrees;
        public float HealthFraction => (float)Health / TradeTuning.EnvoyHealth;

        public int CargoTotal { get { int n = 0; foreach (int c in Cargo) n += c; return n; } }

        /// <summary>The cargo as a list, ascending by ware.</summary>
        public List<WareCount> CargoList()
        {
            var l = new List<WareCount>();
            for (int w = 1; w < Wares.Count; w++) if (Cargo[w] > 0) l.Add(new WareCount((Ware)w, Cargo[w]));
            return l;
        }
    }

    public enum TradeOutcome
    {
        Accepted = 0,
        Refused = 1,
        /// <summary>The envoy was killed: the goods are lost.</summary>
        Lost = 2,
    }

    /// <summary>Why an offer was refused.</summary>
    public enum RefusalCause
    {
        Declined = 0,
        /// <summary>No answer in 60 s.</summary>
        Timeout = 1,
        /// <summary>The target accepted but could no longer pay.</summary>
        CannotPay = 2,
        /// <summary>War was declared between the two.</summary>
        War = 3,
        /// <summary>The target was eliminated or has no HQ left to go to.</summary>
        TargetGone = 4,
    }

    /// <summary>One finished trade, for the history and the HUD log.</summary>
    public sealed class TradeRecord
    {
        public int OfferId;
        public Faction From, To;
        public WareCount[] Give, Want;
        public TradeOutcome Outcome;
        public RefusalCause Cause;
        public long Tick;
    }

    public enum TradeVerdict { Accept = 0, Refuse = 1, Wait = 2 }

    /// <summary>
    /// Pluggable answer of a faction to an offer that has reached it. <see cref="TradeVerdict.Wait"/> leaves the offer
    /// pending for the faction to answer with <see cref="AnswerTrade"/> (it is refused after 60 s like a human's).
    /// Must be deterministic and must not change state.
    /// </summary>
    public interface ITradeJudge
    {
        TradeVerdict Decide(GodState s, TradeOffer offer);
    }

    /// <summary>Trade state (P8): envoys in flight, unanswered offers, the history, and a ledger that proves no ware is made or lost silently.</summary>
    public sealed class TradeState
    {
        public int NextId = 1;
        /// <summary>Active envoys, ascending id.</summary>
        public List<Envoy> Envoys = new List<Envoy>();
        /// <summary>Unanswered offers (in flight or pending), ascending id.</summary>
        public List<TradeOffer> Offers = new List<TradeOffer>();
        /// <summary>The last <see cref="TradeTuning.HistorySize"/> finished trades, oldest first.</summary>
        public List<TradeRecord> History = new List<TradeRecord>();

        /// <summary>Wares that left an HQ stock into an envoy's cargo, by ware.</summary>
        public int[] Departed = new int[Wares.Count];
        /// <summary>Wares an envoy put into an HQ stock, by ware.</summary>
        public int[] Arrived = new int[Wares.Count];
        /// <summary>Wares lost with a killed envoy or with no HQ to go home to, by ware.</summary>
        public int[] Lost = new int[Wares.Count];
        public int Accepted, Refused, Killed;

        /// <summary>The judge AI factions (and everyone in watch mode) answer with. Not part of the state hash; set it after creating or loading a game.</summary>
        [Newtonsoft.Json.JsonIgnore] public ITradeJudge Judge = new DefaultTradeJudge();

        // ---------------- snapshot API ----------------

        public Envoy EnvoyById(int id) { foreach (var e in Envoys) if (e.Id == id) return e; return null; }
        public TradeOffer OfferById(int id) { foreach (var o in Offers) if (o.Id == id) return o; return null; }

        /// <summary>Active envoys (position, cargo, stage), ascending id. Do not modify.</summary>
        public IReadOnlyList<Envoy> ActiveEnvoys => Envoys;

        /// <summary>Offers waiting for <paramref name="f"/>'s answer, ascending id.</summary>
        public List<TradeOffer> PendingFor(Faction f)
        {
            var l = new List<TradeOffer>();
            foreach (var o in Offers) if (o.To == f && o.Stage == OfferStage.Pending) l.Add(o);
            return l;
        }

        /// <summary>Envoys of one faction still walking (any stage).</summary>
        public int EnvoyCountOf(Faction f) { int n = 0; foreach (var e in Envoys) if (e.Faction == f) n++; return n; }

        /// <summary>Units of one ware in transit in envoys' packs.</summary>
        public int InTransit(Ware w) { int n = 0; foreach (var e in Envoys) n += e.Cargo[(int)w]; return n; }

        /// <summary>The finished trades involving a faction, oldest first.</summary>
        public List<TradeRecord> HistoryOf(Faction f)
        {
            var l = new List<TradeRecord>();
            foreach (var r in History) if (r.From == f || r.To == f) l.Add(r);
            return l;
        }

        internal void Record(TradeRecord r)
        {
            History.Add(r);
            while (History.Count > TradeTuning.HistorySize) History.RemoveAt(0);
        }
    }

    // ---------------- events ----------------

    /// <summary>An offer was sent: the give wares left the HQ and the envoy set out.</summary>
    public sealed class TradeOffered : DomainEvent
    {
        public int OfferId; public Faction From; public Faction To; public WareCount[] Give; public WareCount[] Want; public int HqId;
    }

    /// <summary>The envoy reached the target's HQ and the offer is now pending (or answered by the judge this tick).</summary>
    public sealed class EnvoyArrived : DomainEvent
    {
        public int OfferId; public int EnvoyId; public Faction From; public Faction To; public int HqId; public WareCount[] Give; public WareCount[] Want;
    }

    /// <summary>The target accepted: the give wares went into its stock and the payment is on its way home.</summary>
    public sealed class TradeAccepted : DomainEvent
    {
        public int OfferId; public Faction From; public Faction To; public WareCount[] Give; public WareCount[] Want;
    }

    /// <summary>The offer was refused (see Cause): the envoy brings the goods back.</summary>
    public sealed class TradeRefused : DomainEvent
    {
        public int OfferId; public Faction From; public Faction To; public WareCount[] Give; public WareCount[] Want; public RefusalCause Cause;
    }

    public enum EnvoyDeathCause { Shot = 0, NoHomeLeft = 1, SenderEliminated = 2 }

    /// <summary>An envoy died and its goods are lost. <c>By</c> is the shooter's faction id or -1.</summary>
    public sealed class EnvoyKilled : DomainEvent
    {
        public int EnvoyId; public int OfferId; public Faction Owner; public int By; public int X, Z; public WareCount[] Lost; public EnvoyDeathCause Cause;
    }

    /// <summary>An envoy reached home and put its cargo into the HQ's stock.</summary>
    public sealed class EnvoyHome : DomainEvent
    {
        public int EnvoyId; public int OfferId; public Faction Faction; public int HqId; public WareCount[] Cargo; public bool Payment;
    }
}
