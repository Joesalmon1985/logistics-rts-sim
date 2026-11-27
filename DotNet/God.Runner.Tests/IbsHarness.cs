using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Recording;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Estate;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Households;
using God.Sim.Social;
using God.Sim.Tests;
using Microsoft.Data.Sqlite;

namespace God.Runner.Tests
{
    /// <summary>
    /// The I-BS run recorder (ACCEPTANCE I-BS; SHARED_CONTRACTS 11): it keeps the events the I-BS rows are asserted from, follows
    /// every lot id (<see cref="GoodsTrace"/>), checks population accounting and the invariants at every day boundary, and records
    /// the facts only the state at the tick can tell (a speaker's life stage, a withdrawer's household and its permission). It
    /// reads the sim; it never writes it.
    /// </summary>
    internal sealed class IbsRecorder : IRunRecorder
    {
        public GoodsTrace Trace;
        public readonly List<SimEvent> Kept = new List<SimEvent>();
        public int Start, Births, Arrivals, Deaths, Departures, Days, AccountingBreaks;
        public readonly List<string> Problems = new List<string>();
        /// <summary>Messages spoken by a child (life stage at the tick of the gesture).</summary>
        public readonly HashSet<int> ChildMessages = new HashSet<int>();
        /// <summary>Withdrawals from a household store: (actor, bin, store's household, actor's household, permission, hut) at the tick.</summary>
        public readonly List<(long tick, int actor, int bin, int storeHousehold, int actorHousehold, bool mayTake, int hut, bool ownerKept)> StoreTakes = new List<(long, int, int, int, int, bool, int, bool)>();
        /// <summary>Estate bins (bin -> deceased), from <c>EstateOpened</c> and the share bins seen at the tick.</summary>
        public readonly Dictionary<int, int> EstateBins = new Dictionary<int, int>();
        /// <summary>Lots (ids) that spoiled anywhere (<c>Consumed</c>, Spoiled); the spoilage events themselves are not kept.</summary>
        public readonly Dictionary<int, long> SpoiledLots = new Dictionary<int, long>(); // lot -> last spoilage tick
        public long PerceptionChecks, NotPerceived;
        public int TotalEvents;

        public void Begin(RunInfo info, GodSim sim)
        {
            Trace = GoodsTrace.Start(sim.State);
            Start = sim.State.Population.AliveCount;
        }

        private static bool Keep(SimEvent e)
        {
            switch (e)
            {
                case ChildFed _: case BeggingMade _: case CareGiven _: case GiftGiven _: case ExchangeStep _: case ExchangeClosed _: case ExchangeOpened _:
                case GestureMade _: case ResponseChosen _: case EstateOpened _: case EstateTransfer _: case EstateSettled _: case NpcBorn _: case NpcDied _:
                case BetrayalRecorded _:
                    return true;
                case DecisionMade d: return d.TargetKind == God.Sim.Policy.IntentTarget.Npc || d.TargetKind == God.Sim.Policy.IntentTarget.Bin;
                case TrustChanged t: return t.After > t.Before || t.FamAfter > t.FamBefore;
                case ItemMoved m: return m.Reason != MoveReason.Drop;
                case Produced p: return p.Reason == ProduceReason.Foraged || p.Reason == ProduceReason.HandGathered || p.WithTool;
                case Consumed c: return c.Reason == ConsumeReason.Eaten && c.Npc > 0;
                case OwnershipChanged o: return o.Basis == OwnershipBasis.FirstArrival;
                case Slept sl: return sl.Hut != 0;
                default: return false;
            }
        }

        public void OnTick(GodSim sim, IReadOnlyList<SimEvent> events)
        {
            var s = sim.State;
            var p = s.Population;
            TotalEvents += events.Count;
            foreach (var e in events)
            {
                Trace.Add(e);
                switch (e)
                {
                    case NpcBorn _: Births++; break;
                    case NpcImmigrated _: Arrivals++; break;
                    case NpcDied _: Deaths++; break;
                    case NpcEmigrated _: Departures++; break;
                    case GestureMade g:
                    {
                        var sp = p.Find(g.Speaker);
                        if (sp != null && sp.Stage == LifeStage.Child) ChildMessages.Add(g.MessageId);
                        break;
                    }
                    case EstateOpened eo: EstateBins[eo.Bin] = eo.Estate; break;
                    case ItemMoved m when m.Reason == MoveReason.EstateOut && m.ToKind == HolderKind.Bin && m.FromKind == HolderKind.Bin && EstateBins.TryGetValue(m.FromId, out int dec):
                        EstateBins[m.ToId] = dec; break;
                    case ItemMoved m when m.Reason == MoveReason.Withdraw && m.FromKind == HolderKind.Bin:
                    {
                        var bin = Goods.BinById(p, m.FromId);
                        var a = p.Find(m.Actor);
                        if (bin != null && bin.Kind == BinKind.HouseholdStore && a != null)
                        {
                            int hut = s.Households?.Find(bin.Owner.Id)?.Hut ?? 0;
                            StoreTakes.Add((m.Tick, m.Actor, bin.Id, bin.Owner.Id, a.Household, StoreAccess.MayTake(s, a, bin), hut,
                                m.OwnerBeforeKind == OwnerKind.Household && m.OwnerAfterKind == OwnerKind.Household && m.OwnerBeforeId == bin.Owner.Id && m.OwnerAfterId == bin.Owner.Id));
                        }
                        break;
                    }
                }
                if (e is Consumed rot && rot.Reason == ConsumeReason.Spoiled) SpoiledLots[rot.Lot] = rot.Tick;
                if (Keep(e)) Kept.Add(e);
            }
        }

        public void OnDayEnd(GodSim sim, FactionDayRow[] rows)
        {
            Days++;
            var s = sim.State;
            int alive = s.Population.AliveCount;
            if (Start + Births + Arrivals - Deaths - Departures != alive)
            {
                AccountingBreaks++;
                Problems.Add($"day {Days}: start {Start} + births {Births} + arrivals {Arrivals} - deaths {Deaths} - departures {Departures} != alive {alive}");
            }
            foreach (var b in SimInvariants.Check(s)) Problems.Add("day " + Days + ": " + b);
        }

        public void End(GodSim sim, IDictionary<string, long> counters)
        {
            var s = sim.State;
            foreach (var b in Trace.CheckC3(s)) Problems.Add("C3: " + b);
            foreach (var b in Trace.CheckC6(s.Territory.Grid)) Problems.Add("C6: " + b);
            foreach (var b in Trace.Problems) Problems.Add("trace: " + b);
            PerceptionChecks = s.Population.Stats.PerceptionChecks;
            NotPerceived = s.Population.Stats.NotPerceived;
        }

        public void Dispose() { }
    }

    /// <summary>One I-BS seed: the Full run with its warehouse and recorder, and the Summary and Off hashes.</summary>
    internal sealed class IbsSeed
    {
        public ulong Seed;
        public RunResult Full;
        public ulong SummaryHash, OffHash;
        public IbsRecorder R;
        public string Db;
        public readonly List<string> Reconcile = new List<string>();
        public double Seconds;

        public List<T> Of<T>() where T : SimEvent => R.Kept.OfType<T>().ToList();
    }

    /// <summary>
    /// The I-BS gate's runs: the <c>ibs_families</c> scenario (M4 and M5 on) on seeds 1-10, each at Full with the SQLite
    /// warehouse (reconciled here against the event stream) and again at Summary and Off for recording neutrality. Run once per
    /// test session and shared by the I-BS tests.
    /// </summary>
    internal static class IbsRuns
    {
        public const string Scenario = "ibs_families";
        public static readonly int Seeds = int.TryParse(Environment.GetEnvironmentVariable("IBS_SEEDS"), out int n) && n > 0 ? n : 10;
        public static readonly int Days = int.TryParse(Environment.GetEnvironmentVariable("IBS_DAYS"), out int d) && d > 0 ? d : 15;
        private static readonly object Gate = new object();
        private static List<IbsSeed> _all;
        private static string _dir;

        public static List<IbsSeed> All
        {
            get
            {
                lock (Gate)
                {
                    if (_all != null) return _all;
                    _dir = TestUtil.TempDir("ibs");
                    _all = Enumerable.Range(1, Seeds).AsParallel().AsOrdered().WithDegreeOfParallelism(Math.Max(1, Environment.ProcessorCount / 2))
                        .Select(sd => RunSeed((ulong)sd)).ToList();
                    return _all;
                }
            }
        }

        public static void Cleanup()
        {
            SqliteConnection.ClearAllPools();
            if (_dir != null) TestUtil.Cleanup(_dir);
        }

        private static RunSpec Spec(ulong seed, string tag, RecordingLevel level, bool sqlite)
        {
            var spec = TestUtil.Spec(Scenario, seed, (long)Days * 1200);
            spec.Level = level;
            spec.RunDir = Path.Combine(_dir, "s" + seed + "-" + tag); spec.OutRoot = _dir; spec.SkipJsonl = true;
            if (sqlite) spec.Sqlite = new WarehouseOptions { ForeignKeys = true };
            return spec;
        }

        private static IbsSeed RunSeed(ulong seed)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var x = new IbsSeed { Seed = seed, R = new IbsRecorder() };
            var full = Spec(seed, "full", RecordingLevel.Full, true);
            full.ExtraRecorders.Add(() => x.R);
            x.Full = RunEngine.Run(full);
            x.Db = x.Full.WarehousePath;
            x.SummaryHash = RunEngine.Run(Spec(seed, "summary", RecordingLevel.Summary, false)).FinalHash;
            x.OffHash = RunEngine.Run(Spec(seed, "off", RecordingLevel.Off, false)).FinalHash;
            if (x.Full.Error == null && x.Db != null) Reconcile(x);
            x.Seconds = sw.Elapsed.TotalSeconds;
            return x;
        }

        private static long Q(string db, string sql) { using (var d = WarehouseDb.OpenRead(db)) return Convert.ToInt64(d.Scalar(sql) ?? 0L); }

        /// <summary>Every I-BS event type reaches the warehouse and reconciles exactly (D4 §9): counts, persons, lots, quantities.</summary>
        private static void Reconcile(IbsSeed x)
        {
            var db = x.Db; var r = x.R;
            void Eq(string what, long expected, long actual) { if (expected != actual) x.Reconcile.Add(what + ": events " + expected + ", warehouse " + actual); }
            var fed = x.Of<ChildFed>(); var cared = x.Of<CareGiven>(); var gifts = x.Of<GiftGiven>(); var steps = x.Of<ExchangeStep>();
            var closed = x.Of<ExchangeClosed>(); var opened = x.Of<ExchangeOpened>(); var gest = x.Of<GestureMade>(); var resp = x.Of<ResponseChosen>();
            var transfers = x.Of<EstateTransfer>(); var died = x.Of<NpcDied>(); var born = x.Of<NpcBorn>();
            Eq("ChildFed rows", fed.Count, Q(db, "SELECT COUNT(*) FROM fact_care WHERE kind_code = 134"));
            Eq("ChildFed lot ids", fed.Sum(f => (long)f.Lot), Q(db, "SELECT COALESCE(SUM(lot_id), 0) FROM fact_care WHERE kind_code = 134"));
            Eq("CareGiven rows", cared.Count, Q(db, "SELECT COUNT(*) FROM fact_care WHERE kind_code = 136"));
            Eq("GiftGiven lines", gifts.Count, Q(db, "SELECT COUNT(*) FROM fact_transaction_line WHERE kind_code = 213"));
            Eq("hand-over lines", gifts.Count + steps.Count, Q(db, "SELECT COUNT(*) FROM fact_transaction_line"));
            Eq("hand-over lot ids", gifts.Sum(g => (long)g.Lot) + steps.Sum(g => (long)g.Lot), Q(db, "SELECT COALESCE(SUM(lot_id), 0) FROM fact_transaction_line"));
            Eq("deals opened", opened.Count, Q(db, "SELECT COUNT(*) FROM fact_transaction"));
            Eq("deals closed", closed.Count, Q(db, "SELECT COUNT(*) FROM fact_transaction WHERE outcome_code IS NOT NULL"));
            Eq("gestures", gest.Count, Q(db, "SELECT COUNT(*) FROM fact_social_interaction WHERE kind_code = 1"));
            Eq("gesture message ids", gest.Sum(g => (long)g.MessageId), Q(db, "SELECT COALESCE(SUM(message_id), 0) FROM fact_social_interaction WHERE kind_code = 1"));
            Eq("responses", resp.Count, Q(db, "SELECT COUNT(*) FROM fact_social_interaction WHERE kind_code = 3"));
            Eq("inheritance rows", transfers.Count, Q(db, "SELECT COUNT(*) FROM fact_inheritance"));
            Eq("inheritance lot ids", transfers.Sum(t => (long)t.AssetId), Q(db, "SELECT COALESCE(SUM(asset_id), 0) FROM fact_inheritance"));
            Eq("deaths", died.Count, Q(db, "SELECT COUNT(*) FROM fact_death"));
            Eq("births", born.Count, Q(db, "SELECT COUNT(*) FROM fact_birth"));
            Eq("first-arrival claims", x.Of<OwnershipChanged>().Count, Q(db, "SELECT COUNT(*) FROM fact_ownership WHERE basis_code = " + (int)OwnershipBasis.FirstArrival));
            Eq("meals eaten (personal)", x.Of<Consumed>().Count, Q(db, "SELECT COUNT(*) FROM fact_production WHERE direction = -1 AND lot_id IS NOT NULL AND person_key IS NOT NULL AND reason_code = " + (int)ConsumeReason.Eaten));
            Eq("no npc 0 person", 0, Q(db, "SELECT COUNT(*) FROM dim_person WHERE npc_id = 0"));
            using (var d = WarehouseDb.OpenRead(db)) Eq("foreign keys", 0, WarehouseRecorder.ForeignKeyViolations(d));
        }

        // ---------------- event helpers for the rows ----------------

        /// <summary>
        /// How the lot <paramref name="lotId"/> came into <paramref name="holder"/>'s carry before event index <paramref name="before"/>:
        /// the latest event that put it (or the lot it was split from) there, a forage or gather (<see cref="Produced"/>) or a move
        /// (<see cref="ItemMoved"/>: a withdrawal, a pick-up, a hand-over). Null when none is found.
        /// </summary>
        public static SimEvent Acquisition(List<SimEvent> evs, int before, int holder, int lotId)
        {
            var ids = new HashSet<int> { lotId };
            for (int i = before - 1; i >= 0; i--)
            {
                switch (evs[i])
                {
                    case ItemMoved m:
                        if (m.ToKind == HolderKind.Carry && m.ToId == holder && (ids.Contains(m.Lot) || m.IntoLot != 0 && ids.Contains(m.IntoLot))) return m;
                        if (ids.Contains(m.Lot) && m.ParentLot != 0) ids.Add(m.ParentLot);
                        break;
                    case Produced p:
                        if (p.Npc == holder && (ids.Contains(p.Lot) || p.IntoLot != 0 && ids.Contains(p.IntoLot))) return p;
                        break;
                }
            }
            return null;
        }
    }
}
