using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// The AI of one faction (P9). It plays like a person: it only reads <see cref="GodState"/> and sends commands through
    /// <see cref="GodSim.Submit"/>. One <see cref="Think"/> every 2 s (driven by <see cref="AiDirector"/>):
    /// <list type="number">
    /// <item>look: a snapshot of its HQs, buildings, free wares, road network, armies and VP;</item>
    /// <item>diplomacy: war and peace by the owner rules (<see cref="Diplomacy"/>);</item>
    /// <item>army: guards on every HQ, factory pause and ratio, attack marks (<see cref="Army"/>);</item>
    /// <item>trade hooks (<see cref="IAiTrader"/>: answer offers first, propose trades last; <see cref="AiTrader"/> since P11);</item>
    /// <item>economy: input priorities, then up to <see cref="AiTuning.BuildsPerThink"/> builds from an ordered want list
    /// (chain, expansion, Citadels, growth) with ware reservations, then road repairs.</item>
    /// </list>
    /// It keeps no memory between thinks: everything is derived from the state, so a brain rebuilt from a loaded game
    /// continues it exactly. Any randomness comes from <see cref="ThinkRng"/>, a fork of the game seed by faction and tick.
    /// </summary>
    public sealed partial class AiBrain
    {
        public readonly GodSim Sim;
        public readonly Faction Side;
        public readonly AiTuning Tune;
        /// <summary>The trade seam (<see cref="NoTrade"/> unless a trader is given; the game gives <see cref="AiTrader"/>).</summary>
        public IAiTrader Trader;

        /// <summary>Seeded per faction and tick (stateless): the only source of randomness of a think.</summary>
        public Rng ThinkRng { get; private set; }

        /// <summary>Rule checks spent in the current (or last) think.</summary>
        public int ChecksUsed { get; private set; }

        /// <summary>What the last think did, for logs and tests (not state).</summary>
        public readonly List<string> LastActions = new List<string>();

        public AiBrain(GodSim sim, Faction side, AiTuning tuning = null, IAiTrader trader = null)
        {
            Sim = sim ?? throw new ArgumentNullException(nameof(sim));
            Side = side;
            Tune = tuning ?? AiTuning.Default;
            Trader = trader ?? NoTrade.Instance;
        }

        private GodState S => Sim.State;
        private TerritoryState T => Sim.State.Territory;
        private EconomyState E => Sim.State.Economy;
        private MilitaryState M => Sim.State.Military;
        private NodeGrid G => Sim.State.Territory.Grid;
        private BoardTopology Topo => Sim.State.Territory.Topology;

        // ---------------- the snapshot of one think ----------------

        private readonly List<Hq> _hqs = new List<Hq>();
        private readonly List<Hq> _hqSites = new List<Hq>();
        private readonly Dictionary<string, int> _have = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _built = new Dictionary<string, int>();
        private int _openSites, _citadels;
        private bool _upgrading;
        /// <summary>A harvester of the faction is being upgraded (one at a time).</summary>
        private bool _harvUpgrading;
        /// <summary>Free wares: HQ stock not yet promised, minus what open sites and an upgrade still need, minus reservations.</summary>
        private readonly int[] _budget = new int[Wares.Count];
        /// <summary>Nodes of the flags joined by road to the faction's main network.</summary>
        private readonly HashSet<int> _net = new HashSet<int>();
        /// <summary>Flag id to network component (finished HQs give components their weight).</summary>
        private readonly Dictionary<int, int> _comp = new Dictionary<int, int>();
        private int _mainComp = -1;
        /// <summary>Nodes claimed by commands sent in this think (they apply next tick).</summary>
        private readonly HashSet<int> _reserved = new HashSet<int>();
        private readonly HashSet<Terrain> _lands = new HashSet<Terrain>();
        private readonly int[] _strength = new int[FactionInfo.Max];
        private readonly int[] _vp = new int[FactionInfo.Max];
        private readonly List<MilUnit> _mine = new List<MilUnit>();
        private int _inf;
        private int[,] _harvOnHex;
        /// <summary>(hex, kind) of every harvester or harvester site of the faction.</summary>
        private readonly HashSet<(int, string)> _myHarvOn = new HashSet<(int, string)>();
        /// <summary>Hexes touched by the faction's build areas, ascending.</summary>
        private readonly SortedSet<int> _areaHexes = new SortedSet<int>();
        /// <summary>Free wares before this think's reservations.</summary>
        private readonly int[] _free = new int[Wares.Count];
        private bool _newHexOnly;

        // Pure functions of the grid, cached per brain.
        private readonly Dictionary<int, List<int>> _circleCache = new Dictionary<int, List<int>>();

        /// <summary>Quality of a hex number: 6 and 8 weigh 5, 2 and 12 weigh 1, the desert 0.</summary>
        public static int NumberWeight(int n) => n <= 0 ? 0 : 6 - Math.Abs(7 - n);

        // ---------------- think ----------------

        /// <summary>One decision round. Sends commands only; never steps the sim.</summary>
        public void Think()
        {
            LastActions.Clear();
            ChecksUsed = 0;
            if (S.IsGameOver || S.Factions.IsEliminated(Side)) return;
            ThinkRng = new Rng(S.Seed).Fork("ai." + (int)Side + "." + S.Tick);
            Look();
            if (_hqs.Count == 0) return;
            Diplomacy();
            Army();
            Trader.ConsiderOffers(this);
            Priorities();
            Build();
            KeepRaws();
            Roads();
            Trader.ProposeTrades(this);
        }

        /// <summary>A one-line picture of the faction as the brain sees it (for logs): HQs, corners in reach, free wares, army.</summary>
        public string Describe()
        {
            if (S.Factions.IsEliminated(Side)) return FactionInfo.Name(Side) + " out";
            ThinkRng = new Rng(S.Seed).Fork("ai.describe");
            Look();
            return FactionInfo.Name(Side) + " hqs " + _hqs.Count + "+" + _hqSites.Count + " corners " + CountExpansionCorners()
                + " lands " + string.Join("/", _lands) + " free p" + _budget[(int)Ware.Plank] + " s" + _budget[(int)Ware.Stone]
                + " b" + _budget[(int)Ware.Brick] + " m" + _budget[(int)Ware.Mortar] + " st" + _budget[(int)Ware.Steel] + " sites " + _openSites + " army " + _inf;
        }

        private bool Send(Command c, string note)
        {
            if (!Sim.Submit(c).Ok) return false;
            LastActions.Add(note);
            return true;
        }

        private bool Check(Command c)
        {
            ChecksUsed++;
            return Sim.Check(c).Ok;
        }

        private bool CheckBudgetLeft => ChecksUsed < Tune.MaxChecksPerThink;

        private void Look()
        {
            var s = S; var t = T; var e = E;
            _hqs.Clear(); _hqSites.Clear(); _have.Clear(); _built.Clear(); _reserved.Clear(); _lands.Clear();
            _openSites = 0; _citadels = 0; _upgrading = false; _harvUpgrading = false;
            foreach (var h in t.Hqs)
                if (h.Faction == Side) { if (h.Site) _hqSites.Add(h); else { _hqs.Add(h); if (h.Citadel) _citadels++; } }
            _myHarvOn.Clear(); _areaHexes.Clear();
            foreach (var h in _hqs) foreach (int hex in Topo.VertexHexes(h.Corner)) { _lands.Add(s.Board.HexTerrain[hex]); _areaHexes.Add(hex); }

            for (int w = 0; w < Wares.Count; w++) _budget[w] = 0;
            _harvOnHex = new int[Topo.HexCount, 5];
            foreach (var b in e.Buildings)
            {
                if (!b.IsHq)
                {
                    var d = b.Def;
                    if (d != null && d.Role == BuildingRole.Harvester && b.Hex >= 0)
                    {
                        _harvOnHex[b.Hex, (int)d.Harvests]++;
                        if (b.Faction == Side) _myHarvOn.Add((b.Hex, b.Kind));
                    }
                }
                if (b.Faction != Side) continue;
                if (b.IsHq)
                {
                    if (b.Built) for (int w = 0; w < Wares.Count; w++) _budget[w] += b.Stock[w] - b.StockReserved[w];
                    if (b.Upgrading)
                    {
                        _upgrading = true;
                        var c = EconomyDefs.Citadel.Cost;
                        for (int w = 0; w < Wares.Count; w++) _budget[w] -= Math.Max(0, c[w] - b.In[w] - b.Incoming[w]);
                    }
                    if (!b.Built) for (int w = 0; w < Wares.Count; w++) _budget[w] -= Math.Max(0, b.Def.Cost[w] - b.In[w] - b.Incoming[w]);
                    continue;
                }
                _have[b.Kind] = (_have.TryGetValue(b.Kind, out int n) ? n : 0) + 1;
                if (b.Built) _built[b.Kind] = (_built.TryGetValue(b.Kind, out int k) ? k : 0) + 1;
                if (b.Built)
                {
                    if (b.Upgrading && b.UpgradeDef != null)
                    {
                        _harvUpgrading = true;
                        var c = b.UpgradeDef.Cost;
                        for (int w = 0; w < Wares.Count; w++) _budget[w] -= Math.Max(0, c[w] - b.In[w] - b.Incoming[w]);
                    }
                }
                else
                {
                    _openSites++;
                    var cost = b.Def.Cost;
                    for (int w = 0; w < Wares.Count; w++) _budget[w] -= Math.Max(0, cost[w] - b.In[w] - b.Incoming[w]);
                }
            }

            Array.Copy(_budget, _free, Wares.Count);
            BuildNetwork();
            LookArmies();
        }

        private int Have(string kind) => _have.TryGetValue(kind, out int n) ? n : 0;
        private int Built(string kind) => _built.TryGetValue(kind, out int n) ? n : 0;

        // ---------------- road network ----------------

        private void BuildNetwork()
        {
            var t = T;
            _net.Clear(); _comp.Clear(); _mainComp = -1;
            var parent = new Dictionary<int, int>();
            int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
            foreach (var f in t.Flags) if (f.Faction == Side) parent[f.Id] = f.Id;
            foreach (var r in t.Roads)
            {
                if (r.Faction != Side || !parent.ContainsKey(r.FlagA) || !parent.ContainsKey(r.FlagB)) continue;
                int a = Find(r.FlagA), b = Find(r.FlagB);
                if (a != b) { if (a < b) parent[b] = a; else parent[a] = b; }
            }
            foreach (var f in t.Flags) if (f.Faction == Side) _comp[f.Id] = Find(f.Id);
            // The main network: the component holding the most finished HQs (ties: the lowest HQ id first seen).
            var hqCount = new Dictionary<int, int>();
            foreach (var h in _hqs)
            {
                var b = t.BuildingById(h.Id);
                if (b == null || !_comp.TryGetValue(b.FlagId, out int c)) continue;
                hqCount[c] = (hqCount.TryGetValue(c, out int n) ? n : 0) + 1;
                if (_mainComp < 0 || hqCount[c] > hqCount[_mainComp]) _mainComp = c;
            }
            foreach (var f in t.Flags) if (f.Faction == Side && _comp[f.Id] == _mainComp) _net.Add(f.Node);
        }

        private bool InNetwork(int flagId) => _comp.TryGetValue(flagId, out int c) && c == _mainComp;

        private readonly Dictionary<int, int> _bfsPrev = new Dictionary<int, int>();
        private readonly Queue<int> _bfsQueue = new Queue<int>();

        /// <summary>
        /// A road from the main network to <paramref name="start"/> (a future flag node): breadth-first from the start over
        /// free land clear of medium buildings and of <paramref name="avoid"/> and this think's claims, up to
        /// <see cref="AiTuning.MaxRoadSteps"/>, to the nearest network flag. Returned network flag first, the start last.
        /// Null when none; a single node when the start already is a network flag.
        /// </summary>
        private int[] PathFromNetwork(int start, ICollection<int> avoid)
        {
            var t = T; var g = G;
            if (_net.Contains(start)) return new[] { start };
            _bfsPrev.Clear(); _bfsQueue.Clear();
            _bfsPrev[start] = -1;
            var depth = new Dictionary<int, int> { [start] = 0 };
            _bfsQueue.Enqueue(start);
            int end = -1;
            while (_bfsQueue.Count > 0 && end < 0)
            {
                int u = _bfsQueue.Dequeue();
                int du = depth[u];
                if (du >= Tune.MaxRoadSteps) continue;
                for (int d = 0; d < 6; d++)
                {
                    int v = g.Neighbour(u, d);
                    if (v < 0 || !g.IsLand(v) || _bfsPrev.ContainsKey(v)) continue;
                    if (_net.Contains(v)) { _bfsPrev[v] = u; end = v; break; }
                    if (t.NodeBuilding[v] != 0 || t.NodeFlag[v] != 0 || t.NodeRoad[v] != 0) continue;
                    if (_reserved.Contains(v) || (avoid != null && avoid.Contains(v)) || t.InMediumHalo(v)) continue;
                    _bfsPrev[v] = u; depth[v] = du + 1;
                    _bfsQueue.Enqueue(v);
                }
            }
            if (end < 0) return null;
            var path = new List<int>();
            for (int c = end; c != -1; c = _bfsPrev[c]) path.Add(c);
            return path.ToArray(); // end (network flag) first, start last
        }

        private int NetDistance(int node)
        {
            int best = int.MaxValue;
            foreach (int n in _net) { int d = G.Distance(n, node); if (d < best) best = d; }
            return best == int.MaxValue ? 99 : best;
        }

        private void Claim(int node)
        {
            _reserved.Add(node);
            for (int d = 0; d < 6; d++) { int nb = G.Neighbour(node, d); if (nb >= 0) _reserved.Add(nb); }
        }

        private List<int> Circle(int centre)
        {
            if (!_circleCache.TryGetValue(centre, out var l)) _circleCache[centre] = l = G.CircleNodes(centre);
            return l;
        }

        // ---------------- wares ----------------

        private bool Afford(int[] cost)
        {
            for (int w = 0; w < Wares.Count; w++) if (cost[w] > 0 && _budget[w] < cost[w]) return false;
            return true;
        }

        private void Spend(int[] cost)
        {
            for (int w = 0; w < Wares.Count; w++) _budget[w] -= cost[w];
        }

        /// <summary>
        /// Wares the faction is short of for its open sites, its next HQ and a Citadel (for the trader): positive counts.
        /// The mirror of <see cref="WareSurplus"/>: a ware is needed below that reserve and surplus above it.
        /// </summary>
        public int[] WareNeeds()
        {
            var need = new int[Wares.Count];
            if (S.Territory == null) return need;
            Look();
            for (int w = 0; w < Wares.Count; w++) if (_budget[w] < 0) need[w] = -_budget[w];
            var citadel = EconomyDefs.Citadel.Cost; var hq = EconomyDefs.Hq.Cost;
            for (int w = 0; w < Wares.Count; w++) need[w] = Math.Max(need[w], citadel[w] + hq[w] - Math.Max(0, _budget[w]));
            return need;
        }

        /// <summary>Free wares above what a Citadel and an HQ would need (for the trader to pay with).</summary>
        public int[] WareSurplus()
        {
            var have = new int[Wares.Count];
            if (S.Territory == null) return have;
            Look();
            var keep = new int[Wares.Count];
            for (int w = 0; w < Wares.Count; w++) keep[w] = EconomyDefs.Citadel.Cost[w] + EconomyDefs.Hq.Cost[w];
            for (int w = 1; w < Wares.Count; w++) have[w] = Math.Max(0, _budget[w] - keep[w]);
            return have;
        }

        /// <summary>True when one of the faction's finished HQ areas touches land of this terrain (as of the last look).</summary>
        public bool HasLand(Terrain tt) => _lands.Contains(tt);

        /// <summary>The land a raw comes from (log forest, clay hills, grain fields, stone and ore mountain).</summary>
        private static Terrain LandOf(Ware raw)
        {
            switch (raw)
            {
                case Ware.Log: return Terrain.Forest;
                case Ware.Clay: return Terrain.Hills;
                case Ware.Grain: return Terrain.Fields;
                default: return Terrain.Mountain;
            }
        }

        /// <summary>
        /// True when the faction cannot make this ware itself: a land its chain needs is in none of its HQ areas. A raw
        /// needs its land; a processed ware the lands of both its raws; gear the lands of both inputs of its armoury.
        /// Uses the last look (call <see cref="WareNeeds"/> first).
        /// </summary>
        public bool LacksLandFor(Ware w)
        {
            if (w == Ware.None || w == Ware.Kindling) return false;
            if (w == Ware.Rock) return !HasLand(Terrain.Mountain);
            if (Wares.IsRaw(w)) return !HasLand(LandOf(w));
            var maker = EconomyDefs.MakerOf(w);
            if (maker == null) return false;
            for (int i = 1; i < Wares.Count; i++)
                if (maker.In[i] > 0 && LacksLandFor((Ware)i)) return true;
            return false;
        }

        // ---------------- input priorities and factories ----------------

        private void SetPrio(EcoBuilding b, Ware w, Priority p)
        {
            if (b.Built && b.Def.In[(int)w] > 0 && b.Prio[(int)w] != p) Send(new SetInputPriority(Side, b.Id, w, p), "prio " + b.Kind + " " + w + " " + p);
        }

        private void SetPause(EcoBuilding b, bool paused)
        {
            if (b.Paused != paused) Send(new SetPaused(Side, b.Id, paused), (paused ? "pause " : "resume ") + b.Kind);
        }

        /// <summary>
        /// Steers scarce wares (simple stage 3 version): the barracks pauses when the army is big enough, and so does the
        /// axesmith once axes pile up; planks go to the axesmith at low priority while a Citadel is upgrading in peace.
        /// </summary>
        private void Priorities()
        {
            bool war = S.Factions.Enemies(Side).Count > 0;
            int units = _mine.Count;
            int wanted = ArmyWanted(war);
            bool capped = units >= Math.Min(Tune.ArmyCap, wanted + (war ? 6 : 0)) && !(war && units < Tune.ArmyCap);
            int weapons = 0;
            foreach (var g in Gear.Attack) weapons += _free[(int)g];
            foreach (var b in E.Buildings)
            {
                if (b.Faction != Side || b.IsHq || !b.Built) continue;
                var d = b.Def;
                // Enough of its ware in stock: a processor pauses and leaves its raws to the other processors and sites.
                if (d.Processor) SetPause(b, _free[(int)d.Makes] >= Tune.EnoughProcessed);
                switch (b.Kind)
                {
                    case BuildingKinds.Barracks:
                        SetPause(b, capped);
                        break;
                    case BuildingKinds.Axesmith:
                        SetPrio(b, Ware.Plank, _upgrading && !war ? Priority.Low : Priority.Normal);
                        SetPause(b, capped && weapons >= Tune.SpareWeapons);
                        break;
                }
            }
        }

        /// <summary>
        /// After the want list has reserved its wares: a processor stops taking a raw that the wants are short of while it
        /// already has a few of its own ware in stock, so the raw can pile up in the HQ for the next building. A raw with no
        /// harvester of the faction's own yet is kept for building one: all of it while there is not even a site, and
        /// <see cref="AiTuning.KeepRawWithoutHarvester"/> beyond the site's cost once there is.
        /// </summary>
        private void KeepRaws()
        {
            foreach (var b in E.Buildings)
            {
                if (b.Faction != Side || b.IsHq || !b.Built) continue;
                var d = b.Def;
                if (!d.Processor) continue;
                bool stocked = _free[(int)d.Makes] >= Tune.KeepProcessed;
                foreach (var raw in Wares.Raws)
                {
                    if (d.In[(int)raw] <= 0) continue;
                    string harvester = HarvesterOf(raw);
                    bool noHarvester = Built(harvester) == 0 && (Have(harvester) == 0 || _free[(int)raw] < Tune.KeepRawWithoutHarvester);
                    bool keep = noHarvester || (_budget[(int)raw] < 0 && stocked);
                    SetPrio(b, raw, keep ? Priority.Stop : Priority.Normal);
                }
            }
        }

        private static string HarvesterOf(Ware raw)
        {
            switch (raw)
            {
                case Ware.Log: return BuildingKinds.Woodcutter;
                case Ware.Clay: return BuildingKinds.ClayPit;
                case Ware.Grain: return BuildingKinds.Farm;
                case Ware.Stone: return BuildingKinds.Quarry;
                default: return BuildingKinds.Mine;
            }
        }

        private int ArmyWanted(bool war)
        {
            int peace = Tune.ArmyBase + Tune.ArmyPerHq * _hqs.Count;
            return Math.Min(Tune.ArmyCap, war ? peace * Tune.WarArmyPercent / 100 : peace);
        }

        // ---------------- building ----------------

        private int _actions;
        private bool _hqDone;
        /// <summary>The urgent HQ found no corner this think: its cost is held so the cheaper wants cannot eat it (stage 3).</summary>
        private bool _hqHeld;

        /// <summary>The ordered want list: each want is placed when its wares are free, else its cost is reserved so cheaper wants below cannot starve it.</summary>
        private void Build()
        {
            _actions = 0; _hqDone = false; _hqHeld = false;
            int hqs = _hqs.Count;
            int maxSites = Tune.MaxOpenSitesBase + hqs / 2;
            bool missingLand = !_lands.Contains(Terrain.Forest) || !_lands.Contains(Terrain.Mountain) || !_lands.Contains(Terrain.Hills);

            // Stage 3 chain (simple; Q4 refines it): logs and stone first (every building needs them), the sawmill for planks,
            // then an urgent HQ toward missing forest, mountain or hills, then clay for bricks and mortar.
            Want(BuildingKinds.Woodcutter, 1, maxSites, true);
            Want(BuildingKinds.Quarry, 1, maxSites, true);
            Want(BuildingKinds.Sawmill, 1, maxSites, true);
            if (missingLand) Expand(true);
            Want(BuildingKinds.ClayPit, 1, maxSites, true);
            Want(BuildingKinds.Kiln, 1, maxSites);
            Want(BuildingKinds.MortarMill, 1, maxSites);
            // A short ware gets a harvester on every hex of its land in reach first (one harvester per hex: piles are shared).
            if (_free[(int)Ware.Stone] < Tune.ScarceWare) Cover(BuildingKinds.Quarry, maxSites);
            if (_free[(int)Ware.Log] < Tune.ScarceWare || _free[(int)Ware.Plank] < Tune.ScarceWare) Cover(BuildingKinds.Woodcutter, maxSites);
            if ((_free[(int)Ware.Brick] < Tune.ScarceWare || _free[(int)Ware.Mortar] < Tune.ScarceWare) && Built(BuildingKinds.Kiln) > 0) Cover(BuildingKinds.ClayPit, maxSites);

            // Better harvesters as soon as planks and bricks are made (one at a time, only with free wares).
            if (Built(BuildingKinds.Sawmill) > 0 && Built(BuildingKinds.Kiln) > 0) UpgradeHarvester();

            bool chainUp = Built(BuildingKinds.Forge) > 0;
            int corners = CountExpansionCorners();
            // Citadels from CitadelFromHqs HQs on, or as soon as there is no corner left to take.
            bool citadelTime = chainUp && (hqs >= Tune.CitadelFromHqs || (corners == 0 && _hqSites.Count == 0));
            bool citadelFirst = citadelTime && (_citadels < hqs - Tune.CitadelLag || corners == 0 || hqs >= Tune.TargetHqs);
            if (citadelFirst) Citadel();
            // A town grows before it spreads: a further HQ once each HQ has its share of finished buildings.
            int builtTotal = 0; foreach (var kv in _built) builtTotal += kv.Value;
            bool developed = builtTotal >= Tune.BuildingsPerHqBeforeExpand * (hqs - 1);
            if (developed && (hqs < Tune.TargetHqs || !_lands.Contains(Terrain.Hills) || !_lands.Contains(Terrain.Fields))) Expand(false);

            // The army: bread (farm, bakery) and axes (mine, smelter, axesmith) for the barracks; steel (forge) for Citadels.
            Want(BuildingKinds.Farm, 1, maxSites);
            Want(BuildingKinds.Mine, 1, maxSites);
            Want(BuildingKinds.Bakery, 1, maxSites);
            Want(BuildingKinds.Smelter, 1, maxSites);
            Want(BuildingKinds.Axesmith, 1, maxSites);
            Want(BuildingKinds.Barracks, 1, maxSites);
            Want(BuildingKinds.Forge, 1, maxSites);
            if (!citadelFirst && citadelTime) Citadel();

            Cover(BuildingKinds.Quarry, maxSites);
            Cover(BuildingKinds.Woodcutter, maxSites);
            Want(BuildingKinds.ClayPit, 1 + hqs / 3, maxSites);
            Want(BuildingKinds.Mine, 1 + hqs / 3, maxSites);
            Want(BuildingKinds.Sawmill, 1 + hqs / 3, maxSites);
            Want(BuildingKinds.Farm, 1 + hqs / 4, maxSites);
            Want(BuildingKinds.Kiln, 1 + hqs / 4, maxSites);
            Want(BuildingKinds.MortarMill, 1 + hqs / 4, maxSites);
            Want(BuildingKinds.Bakery, 1 + hqs / 4, maxSites);
            Want(BuildingKinds.Smelter, 1 + hqs / 4, maxSites);
            Want(BuildingKinds.Axesmith, 1 + hqs / 4, maxSites);
            Want(BuildingKinds.Barracks, 1 + hqs / 5, maxSites);
            // Late game: more Citadels whenever wares allow.
            if (citadelTime) Citadel();
        }

        /// <summary>
        /// Upgrades one harvester at a time when the wares are free: the lowest level first, the
        /// bottleneck raws (stone, log, clay) before the others, then the busiest hex.
        /// </summary>
        private void UpgradeHarvester()
        {
            if (_harvUpgrading || !ActionsLeft) return;
            EcoBuilding best = null; int bestScore = int.MinValue;
            foreach (var b in E.Buildings)
            {
                if (b.Faction != Side || b.IsHq || !b.Built || b.Upgrading || b.Paused) continue;
                var up = b.UpgradeDef;
                if (up == null || !Afford(up.Cost)) continue;
                int score = -1000 * b.Level + Tune.PipScore * NumberWeight(S.Board.HexNumber[Math.Max(0, b.Hex)]) / 10;
                switch (b.Kind) { case BuildingKinds.Quarry: score += 300; break; case BuildingKinds.Woodcutter: score += 200; break; case BuildingKinds.ClayPit: score += 100; break; }
                if (score > bestScore) { bestScore = score; best = b; }
            }
            if (best == null) return;
            if (Send(new UpgradeBuilding(Side, best.Id), "upgrade " + best.Kind + " " + best.Id + " to " + (best.Level + 1)))
            {
                _harvUpgrading = true;
                Spend(best.UpgradeDef.Cost);
                _actions++;
            }
        }

        /// <summary>One more harvester of a kind when a hex of its land in reach has none of the faction's yet (placed on such a hex).</summary>
        private void Cover(string kind, int maxSites)
        {
            var d = EconomyDefs.Get(kind);
            bool open = false;
            foreach (int hex in _areaHexes)
                if (RawKinds.OfTerrain(S.Board.HexTerrain[hex]) == d.Harvests && !_myHarvOn.Contains((hex, kind))) { open = true; break; }
            if (!open) return;
            _newHexOnly = true;
            Want(kind, Have(kind) + 1, maxSites);
            _newHexOnly = false;
        }

        private bool ActionsLeft => _actions < Tune.BuildsPerThink && CheckBudgetLeft;

        /// <summary>
        /// Wants <paramref name="count"/> of a kind (sites included). Unaffordable or out of actions: its cost is reserved. An
        /// <paramref name="essential"/> want also keeps its cost reserved when it has no plot yet (its land is missing), so an
        /// HQ toward that land never spends the last stone or planks it needs.
        /// </summary>
        private void Want(string kind, int count, int maxSites, bool essential = false)
        {
            if (Have(kind) >= count) return;
            var d = EconomyDefs.Get(kind);
            if (!Afford(d.Cost)) { Spend(d.Cost); return; } // reserve
            if (!ActionsLeft || _openSites >= maxSites) { Spend(d.Cost); return; }
            if (Place(kind))
            {
                Spend(d.Cost);
                _have[kind] = Have(kind) + 1;
                _openSites++; _actions++;
            }
            else if (essential) Spend(d.Cost);
        }

        /// <summary>Places one building of a kind on the best plot that can be joined to the network, and the road to it.</summary>
        private bool Place(string kind)
        {
            var d = EconomyDefs.Get(kind);
            var t = T; var g = G; var board = S.Board;
            bool harvester = d.Role == BuildingRole.Harvester;
            var seen = new HashSet<int>();
            var cands = new List<(int score, int node)>();
            foreach (var h in _hqs)
                foreach (int n in Circle(h.Node))
                {
                    if (!seen.Add(n) || !g.IsLand(n) || _reserved.Contains(n)) continue;
                    if (t.NodeBuilding[n] != 0 || t.NodeFlag[n] != 0 || t.NodeRoad[n] != 0) continue;
                    int hex = g.HexOf(n);
                    if (harvester && RawKinds.OfTerrain(board.HexTerrain[hex]) != d.Harvests) continue;
                    if (_newHexOnly && _myHarvOn.Contains((hex, kind))) continue;
                    int fn = g.FlagNodeOf(n);
                    if (fn < 0 || _reserved.Contains(fn)) continue;
                    int score = -Tune.RoadStepPenalty * NetDistance(fn);
                    if (harvester) score += Tune.PipScore * NumberWeight(board.HexNumber[hex]) - Tune.CrowdedHexPenalty * _harvOnHex[hex, (int)d.Harvests];
                    else score -= g.Distance(n, h.Node);
                    cands.Add((score, n));
                }
            if (cands.Count == 0) return false;
            cands.Sort((a, b) => a.score != b.score ? b.score.CompareTo(a.score) : a.node.CompareTo(b.node));
            int tries = 0;
            foreach (var c in cands)
            {
                if (tries >= Tune.PlaceTries || !CheckBudgetLeft) break;
                var place = new PlaceBuilding(Side, c.node, kind);
                if (!Check(place)) continue;
                tries++;
                int fn = g.FlagNodeOf(c.node);
                int[] road = null;
                var fl = t.FlagAtNode(fn);
                bool joined = (fl != null && InNetwork(fl.Id)) || (t.NodeRoad[fn] != 0 && RoadInNetwork(t.NodeRoad[fn]));
                if (!joined)
                {
                    var avoid = new HashSet<int> { c.node };
                    if (d.Size == BuildingSize.Medium)
                        for (int k = 0; k < 6; k++) { int nb = g.Neighbour(c.node, k); if (nb >= 0 && nb != fn) avoid.Add(nb); }
                    road = PathFromNetwork(fn, avoid);
                    if (road == null || road.Length < 2) continue;
                }
                if (!Send(place, "build " + kind + " at " + c.node)) continue;
                Claim(c.node);
                if (road != null)
                {
                    Sim.Submit(new BuildRoad(Side, road));
                    foreach (int n in road) _reserved.Add(n);
                }
                return true;
            }
            return false;
        }

        private bool RoadInNetwork(int roadId)
        {
            var r = T.RoadById(roadId);
            return r != null && r.Faction == Side && InNetwork(r.FlagA);
        }

        // ---------------- expansion ----------------

        private bool CanReachCorner(int c)
        {
            if (Topo.VertexHexes(c).Count != 3 || T.HqNearCorner(c)) return false;
            foreach (var h in _hqs) if (Topo.CornerDistance(h.Corner, c) == 2) return true;
            return false;
        }

        private int CountExpansionCorners()
        {
            int n = 0;
            foreach (int c in Topo.Corners) if (CanReachCorner(c)) n++;
            return n;
        }

        private int LandValue(Terrain tt)
        {
            switch (tt)
            {
                case Terrain.Forest: return Tune.ForestValue;
                case Terrain.Mountain: return Tune.MountainValue;
                case Terrain.Hills: return Tune.HillsValue;
                case Terrain.Fields: return Tune.FieldsValue;
                default: return 0;
            }
        }

        /// <summary>Score of a corner for a new HQ: land the faction lacks, rich land, minus enemy HQs nearby (more when stronger).</summary>
        private int CornerScore(int c)
        {
            var board = S.Board;
            int score = 0;
            var covered = new HashSet<int>();
            foreach (var h in _hqs) foreach (int hex in Topo.VertexHexes(h.Corner)) covered.Add(hex);
            foreach (int hex in Topo.VertexHexes(c))
            {
                var tt = board.HexTerrain[hex];
                int v = LandValue(tt);
                if (v == 0) continue;
                if (!_lands.Contains(tt)) v += Critical(tt) ? Tune.MissingCriticalScore : Tune.MissingTerrainScore;
                v += Tune.CornerPipScore * NumberWeight(board.HexNumber[hex]);
                if (covered.Contains(hex)) v = v * Tune.CoveredHexPercent / 100;
                score += v;
            }
            foreach (var h in T.Hqs)
            {
                if (h.Faction == Side || S.Factions.IsEliminated(h.Faction)) continue;
                int dist = Topo.CornerDistance(h.Corner, c);
                if (dist < 0 || dist > Tune.NeighbourCornerDistance) continue;
                int pen = Tune.EnemyNearPenalty * (Tune.NeighbourCornerDistance + 1 - dist) / 2;
                if (_strength[(int)h.Faction] > _strength[(int)Side]) pen *= 2;
                if (S.Factions.AtWar(Side, h.Faction)) pen *= 2;
                score -= pen;
            }
            return score + ThinkRng.NextInt(5);
        }

        /// <summary>Forest, mountain and hills: without them there are no logs, stone or bricks, and the faction stalls.</summary>
        private static bool Critical(Terrain tt) => tt == Terrain.Forest || tt == Terrain.Mountain || tt == Terrain.Hills;

        private bool GivesMissingCritical(int c)
        {
            foreach (int hex in Topo.VertexHexes(c)) { var tt = S.Board.HexTerrain[hex]; if (Critical(tt) && !_lands.Contains(tt)) return true; }
            return false;
        }

        /// <summary>Places a new HQ on the best reachable corner, with its road. <paramref name="urgent"/>: the faction lacks forest or mountain.</summary>
        private void Expand(bool urgent)
        {
            if (_hqDone || _hqSites.Count > 0) return;
            _hqDone = true;
            var cost = EconomyDefs.Hq.Cost;
            if (_hqHeld) { for (int w = 0; w < Wares.Count; w++) _budget[w] += cost[w]; _hqHeld = false; }
            if (!Afford(cost)) { Spend(cost); return; }
            if (!ActionsLeft) { Spend(cost); return; }
            var cands = new List<(int score, int corner)>();
            foreach (int c in Topo.Corners)
                if (CanReachCorner(c) && (!urgent || GivesMissingCritical(c))) cands.Add((CornerScore(c), c));
            // P11: no corner in reach gives the missing land: the normal expansion later in the want list may still spread
            // towards it (before, this blocked every HQ and the faction stalled on one HQ).
            // Stage 3: its cost stays held (the land it needs may come with any later HQ), and the normal expansion releases it.
            if (cands.Count == 0) { if (urgent) { _hqDone = false; Spend(cost); _hqHeld = true; } return; }
            cands.Sort((a, b) => a.score != b.score ? b.score.CompareTo(a.score) : a.corner.CompareTo(b.corner));
            var g = G; int tries = 0;
            foreach (var c in cands)
            {
                if (tries++ >= Tune.PlaceTries || !CheckBudgetLeft) break;
                var cmd = new PlaceHq(Side, c.corner);
                if (!Check(cmd)) continue;
                int node = g.CornerNode(c.corner), fn = g.FlagNodeOf(node);
                var road = PathFromNetwork(fn, new HashSet<int> { node });
                if (road == null || road.Length < 2) continue;
                if (!Send(cmd, "hq at corner " + c.corner)) continue;
                Sim.Submit(new BuildRoad(Side, road));
                Claim(node);
                foreach (int n in road) _reserved.Add(n);
                Spend(cost);
                _actions++;
                return;
            }
        }

        // ---------------- Citadels ----------------

        /// <summary>Upgrades the safest finished HQ (fewest enemy HQs near) when the Citadel wares are free; one upgrade at a time.</summary>
        private void Citadel()
        {
            if (_upgrading) return;
            // An army first.
            bool war = S.Factions.Enemies(Side).Count > 0;
            if (_mine.Count * 100 < ArmyWanted(war) * Tune.CitadelArmyPercent) return;
            var cost = EconomyDefs.Citadel.Cost;
            // Steel is not waited for: only the Citadel asks for it, so it is carried in as the forge makes it.
            var noSteel = (int[])cost.Clone(); noSteel[(int)Ware.Steel] = 0;
            if (!Afford(noSteel) || !ActionsLeft) { Spend(cost); return; }
            Hq best = null; int bestDanger = int.MaxValue;
            foreach (var h in _hqs)
            {
                if (h.Citadel) continue;
                var rec = E.Find(h.Id);
                if (rec == null || rec.Upgrading) continue;
                int danger = 0;
                foreach (var o in T.Hqs)
                    if (o.Faction != Side) { int d = Topo.CornerDistance(o.Corner, h.Corner); if (d >= 0 && d <= Tune.NeighbourCornerDistance) danger += Tune.NeighbourCornerDistance + 1 - d; }
                if (danger < bestDanger) { bestDanger = danger; best = h; }
            }
            if (best == null) return;
            if (Send(new UpgradeToCitadel(Side, best.Id), "citadel " + best.Id))
            {
                _upgrading = true;
                Spend(cost);
                _actions++;
            }
        }

        // ---------------- road repairs ----------------

        /// <summary>Joins HQs and sites that lost their road, removes sites that cannot be joined, and puts flags on long roads.</summary>
        private void Roads()
        {
            var t = T; var g = G;
            if (!CheckBudgetLeft) return;
            // A finished HQ or HQ site off the main network.
            foreach (var h in _hqs.Count > 0 ? Concat(_hqs, _hqSites) : _hqSites)
            {
                var b = t.BuildingById(h.Id);
                if (b == null || InNetwork(b.FlagId)) continue;
                var road = PathFromNetwork(b.FlagNode, null);
                if (road != null && road.Length >= 2 && Send(new BuildRoad(Side, road), "join hq " + h.Id)) { foreach (int n in road) _reserved.Add(n); return; }
            }
            // A site with no road.
            foreach (var rec in E.Buildings)
            {
                if (rec.Faction != Side || rec.Built || rec.IsHq || rec.Status != BuildStatus.SiteNoRoad) continue;
                var road = PathFromNetwork(rec.FlagNode, null);
                if (road != null && road.Length >= 2 && Send(new BuildRoad(Side, road), "join site " + rec.Id)) { foreach (int n in road) _reserved.Add(n); return; }
                Send(new RemoveBuilding(Side, rec.Id), "drop site " + rec.Id);
                return;
            }
            // A finished building cut off (its road was lost to a capture or rubble).
            foreach (var rec in E.Buildings)
            {
                if (rec.Faction != Side || !rec.Built || rec.IsHq || InNetwork(rec.FlagId)) continue;
                var road = PathFromNetwork(rec.FlagNode, null);
                if (road != null && road.Length >= 2 && Send(new BuildRoad(Side, road), "join " + rec.Kind)) { foreach (int n in road) _reserved.Add(n); return; }
            }
            // A flag in the middle of a long road, so later buildings can join it there.
            foreach (var r in t.Roads)
            {
                if (r.Faction != Side || r.Length < Tune.SplitRoadSteps) continue;
                for (int k = r.Nodes.Length / 2, step = 0; step < r.Nodes.Length - 4; step++)
                {
                    int i = k + ((step & 1) == 0 ? step / 2 : -(step / 2 + 1));
                    if (i < 2 || i > r.Nodes.Length - 3) continue;
                    var cmd = new PlaceFlag(Side, r.Nodes[i]);
                    if (_reserved.Contains(r.Nodes[i]) || !Check(cmd)) continue;
                    Send(cmd, "flag on road " + r.Id);
                    _reserved.Add(r.Nodes[i]);
                    return;
                }
            }
        }

        private static IEnumerable<Hq> Concat(List<Hq> a, List<Hq> b)
        {
            foreach (var x in a) yield return x;
            foreach (var x in b) yield return x;
        }
    }
}
