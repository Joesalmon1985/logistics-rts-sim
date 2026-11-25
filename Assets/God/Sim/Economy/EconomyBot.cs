using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// A scripted builder for one faction (<see cref="Side"/>) that sends the same commands a player would: it picks a node, places a building, lays a road
    /// to the road network and puts flags along it. Used by the tests and the debug sample. It steps the sim once per
    /// command (a queued command applies at the next tick), so the sim moves a little while it works.
    /// </summary>
    public class EconomyBot
    {
        public readonly GodSim Sim;
        public readonly Faction Side;
        public readonly List<string> Log = new List<string>();

        public EconomyBot(GodSim sim, Faction side) { Sim = sim; Side = side; }

        protected GodState S => Sim.State;
        protected TerritoryState T => Sim.State.Territory;

        protected bool Send(Command c)
        {
            if (S.IsGameOver || S.Factions.IsEliminated(Side)) return false;
            var r = Sim.Submit(c);
            if (!r.Ok) return false;
            Sim.Step();
            return true;
        }

        /// <summary>Quality of a hex number: 6 and 8 best, 2 and 12 worst.</summary>
        public static int NumberWeight(int n) => n <= 0 ? 0 : 6 - Math.Abs(7 - n);

        /// <summary>Flags of the faction reachable by road from its first finished HQ.</summary>
        public List<Flag> Network()
        {
            var l = new List<Flag>();
            var hqs = T.HqsOf(Side);
            if (hqs.Count == 0) return l;
            int root = hqs[0].Id;
            var rootFlag = T.BuildingById(root).FlagId;
            foreach (var f in T.Flags)
                if (f.Faction == Side && T.RouteBetweenFlags(Side, rootFlag, f.Id) != null) l.Add(f);
            return l;
        }

        /// <summary>Places a building of a kind (anywhere legal, best hex first for harvesters) and joins it to the roads. Returns its id or 0.</summary>
        public int PlaceAndConnect(string kind)
        {
            var def = EconomyDefs.Get(kind);
            var g = T.Grid;
            var cands = new List<int>();
            foreach (int n in T.BuildableNodes(Side))
                if (Sim.Check(new PlaceBuilding(Side, n, kind)).Ok) cands.Add(n);
            if (cands.Count == 0) { Log.Add(kind + ": no legal node"); return 0; }
            var net = Network();
            if (net.Count == 0) { Log.Add(kind + ": no road network"); return 0; }
            int Quality(int n) => def.Role == BuildingRole.Harvester ? NumberWeight(S.Board.HexNumber[g.HexOf(n)]) : 0;
            int Dist(int n) { int best = int.MaxValue; foreach (var f in net) best = Math.Min(best, g.Distance(g.FlagNodeOf(n), f.Node)); return best; }
            var scored = new List<(int q, int d, int n)>();
            foreach (int n in cands) scored.Add((Quality(n), Dist(n), n));
            scored.Sort((a, b) => a.q != b.q ? b.q.CompareTo(a.q) : a.d != b.d ? a.d.CompareTo(b.d) : a.n.CompareTo(b.n));
            int tries = 0;
            foreach (var c in scored)
            {
                if (++tries > 60) break;
                if (!Send(new PlaceBuilding(Side, c.n, kind))) continue;
                int id = T.NodeBuilding[c.n];
                if (id != 0 && ConnectFlag(T.BuildingById(id).FlagNode)) return id;
                if (id != 0) Send(new RemoveBuilding(Side, id));
            }
            Log.Add(kind + ": no connectable node");
            return 0;
        }

        /// <summary>Places an HQ on a corner and joins its flag to the roads. Returns the HQ id or 0.</summary>
        public int PlaceHqAndConnect(int corner)
        {
            if (!Send(new PlaceHq(Side, corner))) { Log.Add("hq refused"); return 0; }
            var hq = T.Hqs.Find(h => h.Corner == corner);
            if (hq == null) return 0;
            if (ConnectFlag(T.BuildingById(hq.Id).FlagNode)) return hq.Id;
            Log.Add("hq not connectable");
            return hq.Id;
        }

        /// <summary>Lays a road from a flag to the nearest flag of the network, then adds flags along it.</summary>
        public bool ConnectFlag(int flagNode)
        {
            var g = T.Grid;
            var net = Network();
            var self = T.FlagAtNode(flagNode);
            if (self != null && net.Exists(f => f.Id == self.Id)) return true;
            net.Sort((a, b) =>
            {
                int x = g.Distance(a.Node, flagNode).CompareTo(g.Distance(b.Node, flagNode));
                return x != 0 ? x : a.Id.CompareTo(b.Id);
            });
            int tried = 0;
            foreach (var target in net)
            {
                if (++tried > 6) break;
                var path = T.FindRoadPath(flagNode, target.Node);
                if (path == null || !Sim.Check(new BuildRoad(Side, path)).Ok) continue;
                if (!Send(new BuildRoad(Side, path))) continue;
                for (int i = 3; i < path.Length - 2; i += 3)
                    if (Sim.Check(new PlaceFlag(Side, path[i])).Ok) Send(new PlaceFlag(Side, path[i]));
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// The full chain script: sawmill, a second HQ, then harvesters, workshops and factories, strictly in that order, each
    /// placed once its wares are free. An item that has no legal spot even with every HQ finished is skipped.
    /// </summary>
    public sealed class ChainBot : EconomyBot
    {
        public readonly List<string> Wants = new List<string>
        {
            BuildingKinds.Sawmill, BuildingKinds.Quarry, BuildingKinds.Woodcutter, BuildingKinds.Kiln,
            BuildingKinds.ClayPit, BuildingKinds.Mine, BuildingKinds.Smelter, BuildingKinds.Farm,
            BuildingKinds.Barracks, BuildingKinds.VehicleWorkshop,
        };
        public readonly Dictionary<string, int> Placed = new Dictionary<string, int>();
        public readonly HashSet<string> Skipped = new HashSet<string>();

        /// <summary>A chain script for one faction. Any faction can be driven; each bot sends only that faction's commands.</summary>
        public ChainBot(GodSim sim, Faction side) : base(sim, side) { }

        public bool Done => Placed.Count + Skipped.Count == Wants.Count;

        private bool Affordable(BuildingDef d)
        {
            var e = S.Economy;
            for (int w = 1; w < Wares.Count; w++)
            {
                if (d.Cost[w] == 0) continue;
                int free = e.TotalStock(Side, (Ware)w);
                foreach (var b in e.Buildings)
                    if (!b.Built && b.Faction == Side) free -= Math.Max(0, b.Def.Cost[w] - b.In[w]);
                if (free < d.Cost[w]) return false;
            }
            return true;
        }

        private bool HqPending()
        {
            foreach (var h in T.Hqs) if (h.Faction == Side && h.Site) return true;
            return false;
        }

        private static Terrain? TerrainFor(string kind)
        {
            switch (kind)
            {
                case BuildingKinds.Woodcutter: return Terrain.Forest;
                case BuildingKinds.ClayPit: return Terrain.Hills;
                case BuildingKinds.Quarry:
                case BuildingKinds.Mine: return Terrain.Mountain;
                case BuildingKinds.Farm: return Terrain.Fields;
                default: return null;
            }
        }

        /// <summary>One decision: place the next wanted thing if its wares are free; when its land is out of reach, build an HQ that covers it. Call every second or so.</summary>
        public void Think()
        {
            foreach (var want in Wants)
            {
                if (Placed.ContainsKey(want) || Skipped.Contains(want)) continue;
                var d = EconomyDefs.Get(want);
                if (!Affordable(d)) return;
                int id = PlaceAndConnect(want);
                if (id != 0)
                {
                    Placed[want] = id;
                    // The vehicle workshop needs two iron per unit: let it win iron over the barracks.
                    if (want == BuildingKinds.VehicleWorkshop && Sim.Submit(new SetInputPriority(Side, id, Ware.Iron, Priority.High)).Ok) Sim.Step();
                    return;
                }
                var terrain = TerrainFor(want);
                if (terrain == null) { Skipped.Add(want); Log.Add("skipped " + want); continue; }
                if (HqPending()) return;
                if (!Affordable(EconomyDefs.Hq)) return;
                if (PlaceBestHq(terrain.Value) != 0) { HqsPlaced++; return; }
                Skipped.Add(want);
                Log.Add("skipped " + want);
            }
        }

        public int HqsPlaced;

        private int PlaceBestHq(Terrain wanted)
        {
            var topo = T.Topology;
            var have = new HashSet<Terrain>();
            foreach (var h in T.HqsOf(Side)) foreach (int hex in topo.VertexHexes(h.Corner)) have.Add(S.Board.HexTerrain[hex]);
            int best = -1, bestScore = -1;
            foreach (int c in topo.Corners)
            {
                if (topo.VertexHexes(c).Count != 3 || !Sim.Check(new PlaceHq(Side, c)).Ok) continue;
                int score = 0;
                var seen = new HashSet<Terrain>();
                foreach (int hex in topo.VertexHexes(c))
                {
                    var tt = S.Board.HexTerrain[hex];
                    if (tt == Terrain.Desert || (have.Contains(tt) && tt != wanted) || !seen.Add(tt)) continue;
                    int basis = tt == wanted ? 100 : tt == Terrain.Mountain || tt == Terrain.Forest ? 20 : 8;
                    score += basis + NumberWeight(S.Board.HexNumber[hex]);
                }
                if (score > bestScore) { best = c; bestScore = score; }
            }
            return best < 0 ? 0 : PlaceHqAndConnect(best);
        }
    }
}
