using System.Collections.Generic;

namespace God.Sim
{
    public sealed partial class GodState
    {
        /// <summary>Wares, buildings' work state, carriers and builders (P2). Null until <see cref="EconomyModule.StartGame"/>.</summary>
        public EconomyState Economy;
    }

    public enum BuildStatus
    {
        /// <summary>Site: some cost wares are still on the way or not requested yet.</summary>
        SiteWaitingWares = 0,
        /// <summary>Site: no road leads from any HQ to it.</summary>
        SiteNoRoad = 1,
        /// <summary>Site: wares are in, waiting for the builder to arrive.</summary>
        SiteWaitingBuilder = 2,
        /// <summary>Site: the builder is working.</summary>
        SiteBuilding = 3,
        Idle = 4,
        Working = 5,
        Paused = 6,
        MissingInputs = 7,
        /// <summary>The output at the flag is full; waiting for carriers.</summary>
        OutputFull = 8,
        /// <summary>Harvester: no free pile on its hex.</summary>
        NoPiles = 9,
        /// <summary>Harvester: its hex does not give what it takes.</summary>
        WrongTerrain = 10,
    }

    /// <summary>The economy record of one territory building (site, finished building or HQ). Id equals the territory building id.</summary>
    public sealed class EcoBuilding
    {
        public int Id;
        public Faction Faction;
        public string Kind;
        public bool IsHq;
        public int Node;
        public int FlagId;
        public int FlagNode;
        /// <summary>Hex the building stands in (harvesters take piles from it).</summary>
        public int Hex = -1;
        /// <summary>False while it is a construction site.</summary>
        public bool Built;
        public bool Paused;
        public BuildStatus Status;

        // ---- site ----
        /// <summary>Ticks of building work done.</summary>
        public int BuildProgress;
        public int BuilderWalker;
        public bool BuilderOnSite;

        // ---- inputs and outputs, by ware. For a site, In holds the cost wares delivered so far. ----
        public int[] In = new int[Wares.Count];
        /// <summary>Wares promised: a carrier is on the way with them.</summary>
        public int[] Incoming = new int[Wares.Count];
        public Priority[] Prio = NormalPriorities();
        /// <summary>Wares waiting at the building's flag.</summary>
        public int[] Out = new int[Wares.Count];
        /// <summary>Output units already claimed by a carrier.</summary>
        public int[] OutReserved = new int[Wares.Count];

        // ---- work ----
        public bool Working;
        public int WorkLeft;
        public int HarvestWalker;
        /// <summary>Cycles finished (harvester trips returned, workshop cycles, units).</summary>
        public int Produced;
        /// <summary>Harvester trips begun; the harvester with the fewest goes first when several share a hex.</summary>
        public int Claims;

        // ---- HQ ----
        public int[] Stock = new int[Wares.Count];
        public int[] StockReserved = new int[Wares.Count];
        public int IdleCarriers;
        public int IdleBuilders;

        public BuildingDef Def => EconomyDefs.DefOf(Kind);

        private static Priority[] NormalPriorities()
        {
            var p = new Priority[Wares.Count];
            for (int i = 0; i < p.Length; i++) p[i] = Priority.Normal;
            return p;
        }

        /// <summary>Wares this record asks for: the cost for a site, the cycle inputs for a finished workshop or factory.</summary>
        public int[] Wanted
        {
            get
            {
                var d = Def;
                if (d == null) return new int[Wares.Count];
                if (!Built) return d.Cost;
                return d.In;
            }
        }
    }

    public enum WalkerKind { Carrier = 0, Builder = 1, Harvester = 2 }

    public enum WalkerPhase
    {
        /// <summary>Carrier: to the pickup. Builder: to the site. Harvester: to the pile.</summary>
        Outbound = 0,
        /// <summary>Carrier only: loaded, on the way to the destination.</summary>
        Carrying = 1,
        /// <summary>Walking home.</summary>
        Returning = 2,
        /// <summary>Builder on site, or harvester gathering.</summary>
        Working = 3,
    }

    /// <summary>A carrier, builder or harvester on the move. Walks a node path one lattice step at a time.</summary>
    public sealed class Walker
    {
        public int Id;
        public WalkerKind Kind;
        public Faction Faction;
        /// <summary>HQ id (carrier, builder) or harvester building id.</summary>
        public int Home;
        public WalkerPhase Phase;

        /// <summary>Nodes to walk; the walker is on <c>Path[Index]</c>, heading for <c>Path[Index + 1]</c>.</summary>
        public int[] Path = new int[0];
        public int Index;
        /// <summary>Ticks spent on the current step.</summary>
        public int Progress;

        // ---- job (carrier) ----
        public Ware JobWare;
        public bool Loaded;
        public int SrcId;
        public bool SrcIsHq;
        public int DstId;
        public bool DstIsHq;

        // ---- builder: site id. Harvester: hex. ----
        public int Target;
        public int WorkLeft;

        public Ware Cargo => Loaded ? JobWare : Ware.None;
        public int CurrentNode => Path.Length == 0 ? -1 : Path[Index];
        public int NextNode => Path.Length == 0 ? -1 : (Index + 1 < Path.Length ? Path[Index + 1] : Path[Index]);
        public int StepTicks
        {
            get
            {
                switch (Kind)
                {
                    case WalkerKind.Carrier: return EconomyTuning.CarrierStepTicks;
                    case WalkerKind.Builder: return EconomyTuning.BuilderStepTicks;
                    default: return EconomyTuning.HarvesterStepTicks;
                }
            }
        }
        /// <summary>0..1000 along the current step, for views.</summary>
        public int StepPermille => Index + 1 < Path.Length ? System.Math.Min(1000, Progress * 1000 / StepTicks) : 0;
    }

    /// <summary>Economy state and its read-only snapshot API (the view reads this).</summary>
    public sealed class EconomyState
    {
        public int NextWalkerId = 1;
        /// <summary>Ascending id.</summary>
        public List<EcoBuilding> Buildings = new List<EcoBuilding>();
        public List<Walker> Walkers = new List<Walker>();
        /// <summary>Every unit order the factories have produced, in order. The military drains <c>GodState.UnitQueue</c>; this log stays.</summary>
        public List<UnitOrder> ProducedLog = new List<UnitOrder>();
        /// <summary>Piles per hex already claimed by a harvester on its way.</summary>
        public int[] PilesReserved;
        /// <summary>Wares lost with a destroyed HQ or a carrier that could not return them.</summary>
        public int LostWares;
        public int DeliveredWares;
        public int UnitsProduced;

        public EcoBuilding Find(int id)
        {
            foreach (var b in Buildings) if (b.Id == id) return b;
            return null;
        }

        public Walker FindWalker(int id)
        {
            foreach (var w in Walkers) if (w.Id == id) return w;
            return null;
        }

        /// <summary>HQ stock of one ware (0 when the HQ is unknown).</summary>
        public int Stock(int hqId, Ware w) => Find(hqId) is EcoBuilding b && b.IsHq ? b.Stock[(int)w] : 0;

        /// <summary>Total stock of one ware over a faction's HQs.</summary>
        public int TotalStock(Faction f, Ware w)
        {
            int n = 0;
            foreach (var b in Buildings) if (b.IsHq && b.Faction == f) n += b.Stock[(int)w];
            return n;
        }

        public List<EcoBuilding> HqsOf(Faction f)
        {
            var l = new List<EcoBuilding>();
            foreach (var b in Buildings) if (b.IsHq && b.Faction == f && b.Built) l.Add(b);
            return l;
        }

        /// <summary>Site progress 0..1000: wares delivered count for 400, building work for 600. 1000 for a finished building.</summary>
        public int SiteProgressPermille(int buildingId)
        {
            var b = Find(buildingId);
            if (b == null) return 0;
            if (b.Built) return 1000;
            var d = b.Def;
            int total = d.CostTotal, have = 0;
            for (int w = 0; w < Wares.Count; w++) have += System.Math.Min(b.In[w], d.Cost[w]);
            int delivered = total == 0 ? 400 : 400 * have / total;
            int work = d.BuildTicks == 0 ? 600 : 600 * b.BuildProgress / d.BuildTicks;
            return delivered + work;
        }

        public int CarriersOut(int hqId)
        {
            int n = 0;
            foreach (var w in Walkers) if (w.Kind == WalkerKind.Carrier && w.Home == hqId) n++;
            return n;
        }
    }
}
