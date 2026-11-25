using System.Collections.Generic;
using System.Linq;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Dev script: sets up a sample layout by sending the same commands a player would (second HQ of the human faction, two small
    /// buildings, one medium building, roads joining everything, plus one building of another faction). Steps the sim once per
    /// command so each one is applied before the next is checked. Returns the refusals, empty when all went through.
    /// </summary>
    public static class TerritorySample
    {
        public static List<string> Apply(GodSim sim)
        {
            var log = new List<string>();
            var s = sim.State; var t = s.Territory; var g = t.Grid; var topo = s.Board.Topology;
            var me = FactionLook.Viewer(s);
            var other = me == Faction.F0 ? Faction.F1 : Faction.F0;
            var hq1 = t.HqsOf(me).First();
            int N(int di, int dj) => g.Id(g.I(hq1.Node) + di, g.J(hq1.Node) + dj);

            void Send(string what, Command c)
            {
                var r = sim.Submit(c);
                if (!r.Ok) log.Add(what + " refused: " + r);
                else sim.Step();
            }

            // A second HQ of the human faction: the first legal corner two steps away.
            int corner = topo.Corners.FirstOrDefault(c => topo.VertexHexes(c).Count == 3
                && topo.CornerDistance(hq1.Corner, c) == 2 && sim.Check(new PlaceHq(me, c)).Ok);
            Hq hq2 = null;
            if (topo.Corners.Contains(corner) && sim.Check(new PlaceHq(me, corner)).Ok)
            {
                Send("second HQ", new PlaceHq(me, corner));
                hq2 = t.HqsOf(me).FirstOrDefault(h => h.Corner == corner);
            }
            else log.Add("no legal second HQ corner");

            Send("small A", new PlaceBuilding(me, N(-3, 0), BuildingSize.Small));
            Send("small B", new PlaceBuilding(me, N(3, 2), BuildingSize.Small));
            Send("medium", new PlaceBuilding(me, N(0, 4), BuildingSize.Medium));

            // Roads from every building flag to the first HQ's flag.
            int hubFlag = g.FlagNodeOf(hq1.Node);
            foreach (var b in t.Buildings.Where(b => b.Faction == me && b.Size != BuildingSize.Hq).ToList())
                Road(sim, log, b.FlagNode, hubFlag, "road to building " + b.Id);

            // The long road between the two HQ flags (it leaves both circles), with a flag halfway.
            if (hq2 != null)
            {
                var path = t.FindRoadPath(g.FlagNodeOf(hq2.Node), hubFlag);
                if (path != null && sim.Check(new BuildRoad(me, path)).Ok)
                {
                    Send("road between HQs", new BuildRoad(me, path));
                    int mid = path[path.Length / 2];
                    if (sim.Check(new PlaceFlag(me, mid)).Ok) Send("mid flag", new PlaceFlag(me, mid));
                }
                else log.Add("no road between the HQs");
            }

            // One enemy building and its road, to show the second colour.
            var e = t.HqsOf(other).First();
            int EN(int di, int dj) => g.Id(g.I(e.Node) + di, g.J(e.Node) + dj);
            Send("enemy small", new PlaceBuilding(other, EN(-3, 0), BuildingSize.Small));
            var eb = t.Buildings.FirstOrDefault(b => b.Faction == other && b.Size == BuildingSize.Small);
            if (eb != null) Road(sim, log, eb.FlagNode, g.FlagNodeOf(e.Node), "enemy road");
            return log;
        }

        private static void Road(GodSim sim, List<string> log, int from, int to, string what)
        {
            var path = sim.State.Territory.FindRoadPath(from, to);
            if (path == null) { log.Add(what + ": no path"); return; }
            var r = sim.Submit(new BuildRoad(sim.State.Territory.FlagAtNode(from).Faction, path));
            if (!r.Ok) log.Add(what + " refused: " + r); else sim.Step();
        }
    }
}
