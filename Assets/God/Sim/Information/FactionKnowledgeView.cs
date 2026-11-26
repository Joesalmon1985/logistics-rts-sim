using System;
using System.Collections.Generic;

namespace God.Sim.Information
{
    /// <summary>
    /// Layer 7 (DESIGN_CONVENTIONS §6): everything one faction knows about the world, and the only thing the HUD and every
    /// <c>IFactionPolicy</c> read (INFORMATION_AND_MEDIA_DESIGN §4.8). Built by <see cref="ViewFiller"/>, the single adapter
    /// between true state and knowledge on the player side.
    /// <para><b>M1 form.</b> A live view: it answers from the current tick (<see cref="AsOfTick"/>), so the HUD can read it
    /// every frame without copying. What it answers is filtered, never the raw state:
    /// <list type="bullet">
    /// <item><b>Own books</b> (<see cref="Own"/>): exact (the state knows its treasury, sites, buildings and orders).</item>
    /// <item><b>The island</b> (<see cref="Map"/>, <see cref="Military"/> units and building health, <see cref="Trade"/> envoys):
    /// what the faction's people can see. Stage 3 has no fog, so that is the whole island, as before M1.</item>
    /// <item><b>Other factions</b> (<see cref="Diplomacy"/>, <see cref="Foreign"/>): only declarations (war, peace offers to or
    /// from the viewer), the published standings and what can be seen. Never their stores, stock, offers between third
    /// parties or anything else internal (owner decision Q21, test I19).</item>
    /// </list>
    /// Records, KPIs and the <c>Reported</c> mode arrive with M13b and later ages; the code paths already go through here.
    /// The true-state overlay is a separate object (<see cref="ITrueStateOverlay"/>) that this view never hands out.</para>
    /// </summary>
    public sealed class FactionKnowledgeView
    {
        private readonly GodState _s;

        public Faction Faction { get; }
        public KnowledgeProfile Profile { get; }
        public MapKnowledge Map { get; }
        public OwnBooks Own { get; }
        public MilitaryKnowledge Military { get; }
        public DiplomacyKnowledge Diplomacy { get; }
        public TradeKnowledge Trade { get; }

        internal FactionKnowledgeView(GodState s, Faction faction, KnowledgeProfile profile)
        {
            _s = s ?? throw new ArgumentNullException(nameof(s));
            Faction = faction;
            Profile = profile ?? KnowledgeProfile.FullInformation();
            Map = new MapKnowledge(s, faction);
            Own = new OwnBooks(s, faction);
            Military = new MilitaryKnowledge(s, faction);
            Diplomacy = new DiplomacyKnowledge(s, faction);
            Trade = new TradeKnowledge(s, faction);
        }

        // ---------------- the game, as everyone knows it ----------------

        /// <summary>The sim tick the view describes (live: the current tick).</summary>
        public long AsOfTick => _s.Tick;
        public SimCalendar Calendar => SimCalendar.Of(_s.Setup);
        public int TicksPerDay => _s.Setup?.TicksPerDay ?? SimCalendar.DefaultTicksPerDay;
        public int FactionCount => _s.FactionCount;
        public bool InGame(Faction f) => _s.Setup?.InGame((int)f) ?? true;
        /// <summary>The human faction of the setup (a hint for views), or null in watch mode.</summary>
        public Faction? HumanFaction => _s.Setup != null && _s.Setup.HasHuman ? _s.Setup.HumanFaction : (Faction?)null;
        public bool Tutorial => _s.Setup?.Tutorial ?? false;
        public bool IsGameOver => _s.IsGameOver;

        // ---------------- foreign knowledge (Q21) ----------------

        /// <summary>
        /// Everything the faction knows about each other faction of the game, with its channel. Built on each call (ascending
        /// faction, then <see cref="ForeignFact"/> order).
        /// </summary>
        public List<ForeignKnowledge> Foreign()
        {
            var l = new List<ForeignKnowledge>();
            long now = _s.Tick;
            var standings = FactionRules.Standings(_s);
            foreach (var st in standings)
            {
                var f = st.Faction;
                if (f == Faction) continue;
                l.Add(new ForeignKnowledge(f, ForeignFact.VictoryPoints, st.VictoryPoints, KnowledgeChannel.OfficialReporting, now, 1000));
                l.Add(new ForeignKnowledge(f, ForeignFact.Hqs, st.Hqs, KnowledgeChannel.Observation, now, 1000));
                l.Add(new ForeignKnowledge(f, ForeignFact.Citadels, st.Citadels, KnowledgeChannel.Observation, now, 1000));
                l.Add(new ForeignKnowledge(f, ForeignFact.Eliminated, st.Eliminated ? 1 : 0, KnowledgeChannel.OfficialReporting, now, 1000));
                l.Add(new ForeignKnowledge(f, ForeignFact.AtWarWithViewer, Diplomacy.AtWar(Faction, f) ? 1 : 0, KnowledgeChannel.Diplomacy, now, 1000));
                l.Add(new ForeignKnowledge(f, ForeignFact.OffersPeaceToViewer, Diplomacy.HasOfferedPeace(f, Faction) ? 1 : 0, KnowledgeChannel.Diplomacy, now, 1000));
                int soldiers = 0; foreach (var u in _s.Military.Units) if (u.Faction == f) soldiers++;
                l.Add(new ForeignKnowledge(f, ForeignFact.VisibleSoldiers, soldiers, KnowledgeChannel.Observation, now, 1000));
                int buildings = 0; foreach (var b in _s.Territory.Buildings) if (b.Faction == f) buildings++;
                l.Add(new ForeignKnowledge(f, ForeignFact.VisibleBuildings, buildings, KnowledgeChannel.Observation, now, 1000));
                int envoys = 0; if (_s.Trade != null) foreach (var e in _s.Trade.Envoys) if (e.Faction == f) envoys++;
                l.Add(new ForeignKnowledge(f, ForeignFact.VisibleEnvoys, envoys, KnowledgeChannel.Observation, now, 1000));
            }
            return l;
        }

        // ---------------- digest ----------------

        /// <summary>
        /// One hash over everything this view answers: own books, the visible island, units, building health, diplomacy as
        /// the viewer knows it, its trade, and <see cref="Foreign"/>. Two states that differ only in what the faction cannot
        /// know give the same digest (the leak test I19). Not sim state; costs a full pass.
        /// </summary>
        public ulong Digest()
        {
            var h = new StateHasher();
            h.Add((int)Faction); h.Add(_s.Tick);
            Map.AddTo(ref h); Own.AddTo(ref h); Military.AddTo(ref h); Diplomacy.AddTo(ref h); Trade.AddTo(ref h);
            foreach (var k in Foreign()) { h.Add((int)k.About); h.Add((int)k.Fact); h.Add(k.Value); h.Add((int)k.Channel); h.Add(k.AsOfTick); h.Add(k.ConfidencePermille); }
            return h.Value;
        }
    }

    /// <summary>The island as the faction's people see it: the board, every HQ, building, flag and road (no fog in stage 3).</summary>
    public sealed class MapKnowledge
    {
        private readonly GodState _s;
        private readonly Faction _me;
        internal MapKnowledge(GodState s, Faction me) { _s = s; _me = me; }

        public NodeGrid Grid => _s.Territory.Grid;
        public BoardTopology Topology => _s.Territory.Topology;
        public int HexCount => _s.Board.HexTerrain.Length;
        public Terrain HexTerrain(int hex) => _s.Board.HexTerrain[hex];
        public int HexNumber(int hex) => _s.Board.HexNumber[hex];

        public IReadOnlyList<Hq> Hqs => _s.Territory.Hqs;
        public IReadOnlyList<Building> Buildings => _s.Territory.Buildings;
        public IReadOnlyList<Flag> Flags => _s.Territory.Flags;
        public IReadOnlyList<Road> Roads => _s.Territory.Roads;

        /// <summary>The id of the flag on a node, 0 for none.</summary>
        public int FlagIdAt(int node) => _s.Territory.NodeFlag[node];
        /// <summary>The id of the road through a node, 0 for none.</summary>
        public int RoadIdAt(int node) => _s.Territory.NodeRoad[node];

        public Building BuildingById(int id) => _s.Territory.BuildingById(id);
        public Building BuildingAtNode(int node) => _s.Territory.BuildingAtNode(node);
        public Flag FlagAtNode(int node) => _s.Territory.FlagAtNode(node);
        public Hq HqOf(int id) => _s.Territory.HqOf(id);
        public List<Hq> HqsOf(Faction f) => _s.Territory.HqsOf(f);

        /// <summary>The nodes the viewing faction may build on (its own build areas).</summary>
        public List<int> BuildableNodes() => _s.Territory.BuildableNodes(_me);

        /// <summary>A free road route between two nodes, avoiding some (the road tool's pathing), or null.</summary>
        public int[] FindRoadPath(int from, int to, ICollection<int> avoid) => _s.Territory.FindRoadPath(from, to, avoid);

        /// <summary>The tick of the last dice roll (the dice are public).</summary>
        public long LastRollTick => _s.Territory.LastRollTick;

        /// <summary>Changes whenever something is placed or removed on the map (for redraw caches).</summary>
        public int Revision
        {
            get
            {
                var t = _s.Territory;
                unchecked
                {
                    int h = t.NextId * 31 + t.Hqs.Count * 7 + t.Buildings.Count * 13 + t.Flags.Count * 17 + t.Roads.Count * 19;
                    foreach (var hq in t.Hqs) h = h * 31 + (hq.Site ? 1 : 2);
                    return h;
                }
            }
        }

        internal void AddTo(ref StateHasher h)
        {
            var t = _s.Territory;
            foreach (var x in t.Hqs) { h.Add(x.Id); h.Add((int)x.Faction); h.Add(x.Corner); h.Add(x.Site ? 1 : 0); h.Add(x.Citadel ? 1 : 0); }
            foreach (var b in t.Buildings) { h.Add(b.Id); h.Add((int)b.Faction); h.Add(b.Node); h.Add(b.Kind); h.Add(b.FlagNode); }
            foreach (var f in t.Flags) { h.Add(f.Id); h.Add((int)f.Faction); h.Add(f.Node); }
            foreach (var r in t.Roads) { h.Add(r.Id); h.Add((int)r.Faction); h.Add(r.FlagA); h.Add(r.FlagB); h.Add(r.Nodes.Length); }
            h.Add(t.LastRollTick);
        }
    }

    /// <summary>The faction's own books (exact under <c>FullInformation</c>): its buildings and sites, stores and progress.</summary>
    public sealed class OwnBooks
    {
        private readonly GodState _s;
        private readonly Faction _me;
        internal OwnBooks(GodState s, Faction me) { _s = s; _me = me; }

        /// <summary>One of the faction's own buildings, sites or HQs (null for anyone else's).</summary>
        public EcoBuilding Building(int id)
        {
            var b = _s.Economy.Find(id);
            return b != null && b.Faction == _me ? b : null;
        }

        /// <summary>The faction's HQs (its stores), in the sim's order.</summary>
        public List<EcoBuilding> Hqs() => _s.Economy.HqsOf(_me);

        /// <summary>Everything of a ware in the faction's HQ stores.</summary>
        public int TotalStock(Ware w) => _s.Economy.TotalStock(_me, w);

        /// <summary>The most of a ware one HQ has free to send (an offer leaves from one HQ).</summary>
        public int MostFreeAtOneHq(Ware w)
        {
            int best = 0;
            foreach (var hq in _s.Economy.HqsOf(_me)) best = Math.Max(best, TradeRules.FreeStock(hq, w));
            return best;
        }

        /// <summary>True when one HQ has all these wares free (what answering a trade needs).</summary>
        public bool CanPay(WareCount[] want) => TradeRules.CanPay(_s, _me, want);

        /// <summary>Construction progress of an own site (0 for anything else).</summary>
        public int SiteProgressPermille(int id) => Building(id) != null ? _s.Economy.SiteProgressPermille(id) : 0;

        /// <summary>Upgrade progress of an own building (0 for anything else).</summary>
        public int UpgradeProgressPermille(int id) => Building(id) != null ? _s.Economy.UpgradeProgressPermille(id) : 0;

        internal void AddTo(ref StateHasher h)
        {
            foreach (var b in _s.Economy.Buildings)
            {
                if (b.Faction != _me) continue;
                h.Add(b.Id); h.Add(b.Kind); h.Add(b.Built ? 1 : 0); h.Add(b.Paused ? 1 : 0); h.Add((int)b.Status); h.Add(b.Level);
                h.Add(b.Upgrading ? 1 : 0); h.Add(b.BuildProgress); h.Add(b.UpgradeProgress);
                for (int w = 0; w < Wares.Count; w++) { h.Add(b.In[w]); h.Add(b.Out[w]); h.Add(b.Stock[w]); h.Add(b.StockReserved[w]); h.Add((int)b.Prio[w]); }
            }
        }
    }

    /// <summary>Soldiers and building health as seen on the island, plus the faction's own defend flags and target marks.</summary>
    public sealed class MilitaryKnowledge
    {
        private readonly GodState _s;
        private readonly Faction _me;
        internal MilitaryKnowledge(GodState s, Faction me) { _s = s; _me = me; }

        /// <summary>Every soldier on the island (observation; no fog in stage 3).</summary>
        public IReadOnlyList<MilUnit> VisibleUnits => _s.Military.Units;

        /// <summary>A building's health as seen (observation), or null.</summary>
        public BuildingRecord VisibleBuilding(int id) => _s.Military.BuildingById(id);

        /// <summary>The faction's own defend flags.</summary>
        public List<DefendFlag> OwnDefends()
        {
            var l = new List<DefendFlag>();
            foreach (var f in _s.Military.Defends) if (f.Faction == _me) l.Add(f);
            return l;
        }

        /// <summary>The faction's defend flag on a building, or null.</summary>
        public DefendFlag DefendAtBuilding(int buildingId)
        {
            var f = _s.Military.DefendAtBuilding(buildingId);
            return f != null && f.Faction == _me ? f : null;
        }

        /// <summary>The faction's defend flag on a bare node, or null.</summary>
        public DefendFlag DefendAtNode(int node) => _s.Military.DefendAtNode(_me, node);

        /// <summary>Guards now at one of the faction's own flags (0 for anyone else's).</summary>
        public int GuardCount(int flagId)
        {
            foreach (var f in _s.Military.Defends) if (f.Id == flagId) return f.Faction == _me ? _s.Military.GuardCount(flagId) : 0;
            return 0;
        }

        public bool IsMarked(int buildingId) => _s.Military.IsMarked(_me, buildingId);

        /// <summary>1-based place of a building in the faction's target list, 0 when not marked.</summary>
        public int MarkOrder(int buildingId) => _s.Military.MarkOrder(_me, buildingId);

        internal void AddTo(ref StateHasher h)
        {
            var m = _s.Military;
            foreach (var u in m.Units) { h.Add(u.Id); h.Add((int)u.Faction); h.Add(u.X); h.Add(u.Z); h.Add(u.Health); h.Add((int)u.Activity); }
            foreach (var b in m.Buildings) { h.Add(b.Id); h.Add(b.Health); h.Add(b.MaxHealth); }
            foreach (var f in m.Defends) if (f.Faction == _me) { h.Add(f.Id); h.Add(f.Node); h.Add(f.AnchorBuildingId); h.Add(f.Guards); h.Add(m.GuardCount(f.Id)); }
            foreach (int id in m.MarkedOf(_me)) h.Add(id);
        }
    }

    /// <summary>War and peace as declared, the published standings and the result.</summary>
    public sealed class DiplomacyKnowledge
    {
        private readonly GodState _s;
        private readonly Faction _me;
        internal DiplomacyKnowledge(GodState s, Faction me) { _s = s; _me = me; }

        /// <summary>Wars are declared publicly: any pair.</summary>
        public bool AtWar(Faction a, Faction b) => _s.Factions != null && _s.Factions.AtWar(a, b);

        public List<Faction> Enemies(Faction f) => _s.Factions != null ? _s.Factions.Enemies(f) : new List<Faction>();

        /// <summary>A peace offer is known only to the two sides: false for offers between other factions.</summary>
        public bool HasOfferedPeace(Faction from, Faction to) =>
            _s.Factions != null && (from == _me || to == _me) && _s.Factions.HasOfferedPeace(from, to);

        public bool IsEliminated(Faction f) => _s.Factions != null && _s.Factions.IsEliminated(f);

        /// <summary>The published standings, rank order.</summary>
        public List<Standing> Standings() => FactionRules.Standings(_s);

        public int VictoryPoints(Faction f) => FactionRules.VictoryPoints(_s, f);

        public GameOutcome Outcome => _s.Factions == null ? GameOutcome.Playing : _s.Factions.OutcomeFor(_me);
        public bool HasFactions => _s.Factions != null;
        /// <summary>Winning faction id, -1 while playing or when nobody is left.</summary>
        public int Winner => _s.Factions?.Winner ?? -1;
        public GameEndReason EndReason => _s.Factions?.EndReason ?? GameEndReason.None;
        public IReadOnlyList<Standing> FinalStandings => _s.Factions != null ? (IReadOnlyList<Standing>)_s.Factions.FinalStandings : new List<Standing>();

        internal void AddTo(ref StateHasher h)
        {
            foreach (var a in FactionInfo.All)
            {
                h.Add(IsEliminated(a) ? 1 : 0);
                foreach (var b in FactionInfo.All) { h.Add(AtWar(a, b) ? 1 : 0); h.Add(HasOfferedPeace(a, b) ? 1 : 0); }
            }
            h.Add(Winner); h.Add((int)EndReason);
        }
    }

    /// <summary>The faction's trade: offers to and from it, its history, and the envoys seen walking the island.</summary>
    public sealed class TradeKnowledge
    {
        private readonly GodState _s;
        private readonly Faction _me;
        internal TradeKnowledge(GodState s, Faction me) { _s = s; _me = me; }

        /// <summary>Offers waiting for the faction's answer, oldest first.</summary>
        public List<TradeOffer> PendingForMe() => _s.Trade != null ? _s.Trade.PendingFor(_me) : new List<TradeOffer>();

        /// <summary>An offer the faction made or received (null for offers between others).</summary>
        public TradeOffer OfferById(int id)
        {
            var o = _s.Trade?.OfferById(id);
            return o != null && (o.From == _me || o.To == _me) ? o : null;
        }

        /// <summary>Envoys walking the island (observation).</summary>
        public IReadOnlyList<Envoy> VisibleEnvoys => _s.Trade != null ? (IReadOnlyList<Envoy>)_s.Trade.Envoys : new List<Envoy>();

        public Envoy EnvoyById(int id) => _s.Trade?.EnvoyById(id);

        /// <summary>Finished trades the faction took part in, oldest first.</summary>
        public List<TradeRecord> MyHistory()
        {
            var l = new List<TradeRecord>();
            if (_s.Trade != null) foreach (var r in _s.Trade.History) if (r.From == _me || r.To == _me) l.Add(r);
            return l;
        }

        internal void AddTo(ref StateHasher h)
        {
            if (_s.Trade == null) return;
            foreach (var o in _s.Trade.Offers) if (o.From == _me || o.To == _me) { h.Add(o.Id); h.Add((int)o.Stage); h.Add(o.TicksLeft); }
            foreach (var e in _s.Trade.Envoys) { h.Add(e.Id); h.Add((int)e.Faction); h.Add(e.X); h.Add(e.Z); h.Add(e.Health); for (int w = 0; w < Wares.Count; w++) h.Add(e.Cargo[w]); }
            foreach (var r in MyHistory()) { h.Add(r.OfferId); h.Add((int)r.Outcome); }
        }
    }
}
