using System;
using System.Collections.Generic;

namespace God.Sim
{
    public sealed class BuildingCompleted : SimEvent { public Faction Faction; public int BuildingId; public string Kind; }
    public sealed class UnitProduced : SimEvent { public Faction Faction; public int BuildingId; public UnitKind Kind; }
    /// <summary>An HQ disappeared (destroyed): its stock and carriers are gone.</summary>
    public sealed class HqLost : SimEvent { public Faction Faction; public int HqId; }

    /// <summary>
    /// The economy tick: keep records in step with the territory, sweep walkers whose job vanished, send builders,
    /// run harvesters, workshops and sites, match requests with supplies, then move every walker one tick.
    /// Everything is integer and iterates in ascending id order.
    /// </summary>
    public sealed class EconomySystem : ISimSystem
    {
        // Route cache, valid while the road network is unchanged. Not part of the state.
        private readonly Dictionary<(int, int), FlagRoute> _routes = new Dictionary<(int, int), FlagRoute>();
        private int _sigNext = -1, _sigRoads = -1, _sigFlags = -1, _sigVersion = -1;

        public void Tick(GodState s, EventBuffer ev)
        {
            var e = s.Economy; var t = s.Territory;
            if (e == null || t == null) return;
            Sync(s, e, t, ev);
            Sweep(s, e, t);
            DispatchBuilders(e, t);
            Produce(s, e, t, ev);
            Match(e, t);
            Move(s, e, t, ev);
        }

        // ---------------- routes ----------------

        private FlagRoute Route(TerritoryState t, Faction f, int a, int b)
        {
            if (_sigNext != t.NextId || _sigRoads != t.Roads.Count || _sigFlags != t.Flags.Count || _sigVersion != t.Version)
            {
                _routes.Clear();
                _sigNext = t.NextId; _sigRoads = t.Roads.Count; _sigFlags = t.Flags.Count; _sigVersion = t.Version;
            }
            // Faction rides in the key via the flags themselves (a flag belongs to one faction).
            var key = (a, b);
            if (_routes.TryGetValue(key, out var r)) return r;
            r = t.RouteBetweenFlags(f, a, b);
            _routes[key] = r;
            return r;
        }

        // ---------------- records in step with the territory ----------------

        private static void Sync(GodState s, EconomyState e, TerritoryState t, EventBuffer ev)
        {
            for (int i = e.Buildings.Count - 1; i >= 0; i--)
            {
                var b = e.Buildings[i];
                bool gone = t.BuildingById(b.Id) == null || (b.IsHq && t.HqOf(b.Id) == null);
                if (!gone) continue;
                e.Buildings.RemoveAt(i);
                if (b.IsHq)
                {
                    foreach (int w in AllWares(b.Stock)) e.LostWares += b.Stock[w];
                    ev.Add(new HqLost { Faction = b.Faction, HqId = b.Id });
                }
            }
            foreach (var tb in t.Buildings)
                if (e.Find(tb.Id) == null) EconomyModule.AddRecord(s, tb, false);
        }

        private static IEnumerable<int> AllWares(int[] arr)
        {
            for (int i = 0; i < arr.Length; i++) yield return i;
        }

        // ---------------- walkers whose job vanished ----------------

        private static void Sweep(GodState s, EconomyState e, TerritoryState t)
        {
            for (int i = 0; i < e.Walkers.Count; i++)
            {
                var w = e.Walkers[i];
                var home = e.Find(w.Home);
                switch (w.Kind)
                {
                    case WalkerKind.Harvester:
                        if (home == null) { Kill(e, w); }
                        break;
                    case WalkerKind.Builder:
                        if (home == null || !home.IsHq) { Kill(e, w); break; }
                        if (w.Phase == WalkerPhase.Returning) break;
                        var site = e.Find(w.Target);
                        if (site == null || site.Built || site.Faction != w.Faction) GoHome(w, e);
                        break;
                    default:
                        if (home == null || !home.IsHq) { Kill(e, w); break; }
                        if (w.Phase == WalkerPhase.Outbound && (e.Find(w.SrcId) == null || !DstWants(e, w)))
                        {
                            ReleaseJob(e, w);
                            ClearJob(w);
                            GoHome(w, e);
                        }
                        break;
                }
            }
            e.Walkers.RemoveAll(w => w.Id < 0);
        }

        /// <summary>
        /// Removes a walker at once (a capture or an elimination): frees what it held, loses its cargo, and puts a carrier
        /// or builder back into its home HQ's store whoever owns it now, so carriers are never lost or duplicated. The
        /// walker is only marked; call <see cref="RemoveRecalled"/> after a batch.
        /// </summary>
        internal static void Recall(EconomyState e, Walker w)
        {
            if (w.Id < 0) return;
            ReleaseJob(e, w);
            if (w.Loaded) e.LostWares++;
            if (e.Find(w.Home) is EcoBuilding home && home.IsHq && home.Built)
            {
                if (w.Kind == WalkerKind.Carrier) home.IdleCarriers++;
                else if (w.Kind == WalkerKind.Builder) home.IdleBuilders++;
            }
            w.Id = -w.Id - 1;
        }

        internal static void RemoveRecalled(EconomyState e) => e.Walkers.RemoveAll(w => w.Id < 0);

        /// <summary>Starts a Citadel upgrade on a finished HQ record (the command has checked it).</summary>
        internal static void StartUpgrade(EcoBuilding hq)
        {
            hq.Upgrading = true; hq.UpgradeProgress = 0;
            Array.Clear(hq.In, 0, hq.In.Length);
            Array.Clear(hq.Incoming, 0, hq.Incoming.Length);
        }

        /// <summary>Marks the walker for removal and frees what it held. Cargo is lost.</summary>
        private static void Kill(EconomyState e, Walker w)
        {
            ReleaseJob(e, w);
            if (w.Kind == WalkerKind.Carrier && w.Loaded) e.LostWares++;
            w.Id = -w.Id - 1;
        }

        /// <summary>Gives back reservations: a pickup not yet made, a destination's incoming claim, a site's builder, a pile.</summary>
        private static void ReleaseJob(EconomyState e, Walker w)
        {
            switch (w.Kind)
            {
                case WalkerKind.Carrier:
                    if (w.JobWare == Ware.None) return;
                    int wi = (int)w.JobWare;
                    if (!w.Loaded && e.Find(w.SrcId) is EcoBuilding src)
                    {
                        if (w.SrcIsHq) src.StockReserved[wi] = Math.Max(0, src.StockReserved[wi] - 1);
                        else src.OutReserved[wi] = Math.Max(0, src.OutReserved[wi] - 1);
                    }
                    if (!w.DstIsHq && e.Find(w.DstId) is EcoBuilding dst) dst.Incoming[wi] = Math.Max(0, dst.Incoming[wi] - 1);
                    break;
                case WalkerKind.Builder:
                    if (e.Find(w.Target) is EcoBuilding site && site.BuilderWalker == WalkerIdOf(w)) { site.BuilderWalker = 0; site.BuilderOnSite = false; }
                    break;
                case WalkerKind.Harvester:
                    if (w.Phase == WalkerPhase.Outbound || w.Phase == WalkerPhase.Working)
                        if (w.Target >= 0 && w.Target < e.PilesReserved.Length) e.PilesReserved[w.Target] = Math.Max(0, e.PilesReserved[w.Target] - 1);
                    if (e.Find(w.Home) is EcoBuilding hb && hb.HarvestWalker == WalkerIdOf(w)) hb.HarvestWalker = 0;
                    break;
            }
        }

        /// <summary>The destination still exists and still asks for the ware (a site finished at once no longer does).</summary>
        private static bool DstWants(EconomyState e, Walker w)
        {
            var d = e.Find(w.DstId);
            return d != null && (w.DstIsHq || d.Wanted[(int)w.JobWare] > 0);
        }

        private static int WalkerIdOf(Walker w) => w.Id < 0 ? -w.Id - 1 : w.Id;

        private static void ClearJob(Walker w)
        {
            w.JobWare = Ware.None; w.Loaded = false; w.SrcId = 0; w.DstId = 0; w.SrcIsHq = false; w.DstIsHq = false;
        }

        /// <summary>Walk back along the way already walked (every path starts at home), or arrive at once when still there.</summary>
        private static void GoHome(Walker w, EconomyState e)
        {
            if (w.Kind == WalkerKind.Builder) ReleaseJob(e, w);
            var back = new int[w.Index + 1];
            for (int k = 0; k <= w.Index; k++) back[k] = w.Path[w.Index - k];
            w.Path = back; w.Index = 0; w.Progress = 0; w.Phase = WalkerPhase.Returning;
        }

        // ---------------- builders ----------------

        private void DispatchBuilders(EconomyState e, TerritoryState t)
        {
            foreach (var site in e.Buildings)
            {
                if (site.Built || site.Paused || site.BuilderWalker != 0) continue;
                var d = site.Def;
                int need = 0, have = 0;
                for (int w = 0; w < Wares.Count; w++) { need += d.Cost[w]; have += site.In[w] + site.Incoming[w]; }
                if (have < need) continue;
                EcoBuilding best = null; FlagRoute bestRoute = null;
                foreach (var hq in e.Buildings)
                {
                    if (!hq.IsHq || !hq.Built || hq.Faction != site.Faction || hq.IdleBuilders <= 0 || hq.Id == site.Id) continue;
                    var r = Route(t, site.Faction, hq.FlagId, site.FlagId);
                    if (r == null) continue;
                    if (best == null || r.Length < bestRoute.Length) { best = hq; bestRoute = r; }
                }
                if (best == null) continue;
                var path = new int[bestRoute.Nodes.Length + 1];
                Array.Copy(bestRoute.Nodes, path, bestRoute.Nodes.Length);
                path[path.Length - 1] = site.Node;
                var bw = new Walker { Id = e.NextWalkerId++, Kind = WalkerKind.Builder, Faction = site.Faction, Home = best.Id, Phase = WalkerPhase.Outbound, Path = path, Target = site.Id };
                e.Walkers.Add(bw);
                best.IdleBuilders--;
                site.BuilderWalker = bw.Id;
            }
        }

        // ---------------- production ----------------

        private void Produce(GodState s, EconomyState e, TerritoryState t, EventBuffer ev)
        {
            var g = t.Grid;
            foreach (var b in e.Buildings)
            {
                if (b.IsHq && b.Built)
                {
                    b.Status = BuildStatus.Idle;
                    if (b.Upgrading) UpgradeTick(s, b, ev);
                    continue;
                }
                var d = b.Def;
                if (!b.Built)
                {
                    SiteTick(s, e, t, b, d, ev);
                    continue;
                }
                if (b.Paused) { b.Status = BuildStatus.Paused; continue; }
                if (d.Role == BuildingRole.Harvester) HarvestStart(s, e, t, b, d);
                else if (d.Role == BuildingRole.Workshop || d.Role == BuildingRole.Factory) Cycle(s, e, b, d, ev);
            }
        }

        private void SiteTick(GodState s, EconomyState e, TerritoryState t, EcoBuilding b, BuildingDef d, EventBuffer ev)
        {
            bool all = true;
            for (int w = 0; w < Wares.Count; w++) if (b.In[w] < d.Cost[w]) all = false;
            if (b.Paused) { b.Status = BuildStatus.Paused; return; }
            if (all && b.BuilderOnSite)
            {
                b.Status = BuildStatus.SiteBuilding;
                if (++b.BuildProgress >= d.BuildTicks) Complete(s, e, t, b, ev);
                return;
            }
            if (all) { b.Status = BuildStatus.SiteWaitingBuilder; return; }
            bool connected = false;
            foreach (var hq in e.Buildings)
                if (hq.IsHq && hq.Built && hq.Faction == b.Faction && hq.Id != b.Id && Route(t, b.Faction, hq.FlagId, b.FlagId) != null) { connected = true; break; }
            b.Status = connected ? BuildStatus.SiteWaitingWares : BuildStatus.SiteNoRoad;
        }

        /// <summary>
        /// One tick of a Citadel upgrade: the HQ's own free stock goes straight in (it is already there), the rest is
        /// carried in by the matching like a site's; once every ware is in, the work runs for the Citadel's build time.
        /// </summary>
        private static void UpgradeTick(GodState s, EcoBuilding b, EventBuffer ev)
        {
            var cost = EconomyDefs.Citadel.Cost;
            bool all = true;
            for (int w = 0; w < Wares.Count; w++)
            {
                int need = cost[w] - b.In[w] - b.Incoming[w];
                int free = b.Stock[w] - b.StockReserved[w];
                int take = Math.Min(need, free);
                if (take > 0) { b.Stock[w] -= take; b.In[w] += take; }
                if (b.In[w] < cost[w]) all = false;
            }
            if (!all) return;
            if (++b.UpgradeProgress < EconomyDefs.Citadel.BuildTicks) return;
            b.Upgrading = false; b.UpgradeProgress = 0;
            Array.Clear(b.In, 0, b.In.Length);
            Array.Clear(b.Incoming, 0, b.Incoming.Length);
            var hq = s.Territory.HqOf(b.Id);
            if (hq != null) hq.Citadel = true;
            ev?.Add(new CitadelCompleted { Faction = b.Faction, HqId = b.Id });
        }

        internal static void Complete(GodState s, EconomyState e, TerritoryState t, EcoBuilding b, EventBuffer ev)
        {
            b.Built = true;
            Array.Clear(b.In, 0, b.In.Length);
            Array.Clear(b.Incoming, 0, b.Incoming.Length);
            b.BuildProgress = 0;
            b.Status = BuildStatus.Idle;
            if (e.FindWalker(b.BuilderWalker) is Walker bw)
            {
                b.BuilderWalker = 0; b.BuilderOnSite = false;
                GoHome(bw, e);
            }
            if (b.IsHq)
            {
                b.IdleCarriers = EconomyTuning.CarriersPerHq;
                b.IdleBuilders = EconomyTuning.BuildersPerHq;
                TerritoryRules.CompleteHq(s, b.Id, ev);
            }
            ev?.Add(new BuildingCompleted { Faction = b.Faction, BuildingId = b.Id, Kind = b.Kind });
        }

        private void HarvestStart(GodState s, EconomyState e, TerritoryState t, EcoBuilding b, BuildingDef d)
        {
            if (b.HarvestWalker != 0) { b.Status = BuildStatus.Working; return; }
            int hex = b.Hex;
            if (hex < 0 || RawKinds.OfTerrain(s.Board.HexTerrain[hex]) != d.Harvests) { b.Status = BuildStatus.WrongTerrain; return; }
            if (b.Out[(int)d.HarvestWare] + EconomyTuning.WaresPerPile > EconomyTuning.OutputCap) { b.Status = BuildStatus.OutputFull; return; }
            if (t.Piles[hex] - e.PilesReserved[hex] <= 0) { b.Status = BuildStatus.NoPiles; return; }
            // Sharing a hex: the harvester with the fewest trips so far goes first, ties to the lower id.
            foreach (var o in e.Buildings)
            {
                if (o == b || o.IsHq || !o.Built || o.Paused || o.Hex != hex || o.HarvestWalker != 0) continue;
                var od = o.Def;
                if (od.Role != BuildingRole.Harvester || od.Harvests != d.Harvests) continue;
                if (o.Out[(int)od.HarvestWare] + EconomyTuning.WaresPerPile > EconomyTuning.OutputCap) continue;
                if (o.Claims < b.Claims || (o.Claims == b.Claims && o.Id < b.Id)) { b.Status = BuildStatus.NoPiles; return; }
            }
            b.Claims++;
            var g = t.Grid;
            var path = StraightPath(g, b.Node, g.HexCentreNode(hex));
            var w = new Walker { Id = e.NextWalkerId++, Kind = WalkerKind.Harvester, Faction = b.Faction, Home = b.Id, Phase = WalkerPhase.Outbound, Path = path, Target = hex };
            e.Walkers.Add(w);
            e.PilesReserved[hex]++;
            b.HarvestWalker = w.Id;
            b.Status = BuildStatus.Working;
        }

        /// <summary>Off-road walk: each step takes the neighbour nearest the goal (lattice distance, then straight-line distance).</summary>
        public static int[] StraightPath(NodeGrid g, int from, int to)
        {
            var list = new List<int> { from };
            int cur = from;
            while (cur != to && list.Count < 200)
            {
                int best = -1, bestD = int.MaxValue; double bestE = double.MaxValue;
                for (int d = 0; d < 6; d++)
                {
                    int nb = g.Neighbour(cur, d);
                    if (nb < 0) continue;
                    int dist = g.Distance(nb, to);
                    double dx = g.X(nb) - g.X(to), dz = g.Z(nb) - g.Z(to);
                    double eu = dx * dx + dz * dz;
                    if (dist < bestD || (dist == bestD && eu < bestE)) { best = nb; bestD = dist; bestE = eu; }
                }
                if (best < 0) break;
                list.Add(best); cur = best;
            }
            return list.ToArray();
        }

        private void Cycle(GodState s, EconomyState e, EcoBuilding b, BuildingDef d, EventBuffer ev)
        {
            if (b.Working)
            {
                b.Status = BuildStatus.Working;
                if (--b.WorkLeft > 0) return;
                b.Working = false;
                b.Produced++;
                for (int w = 0; w < Wares.Count; w++) b.Out[w] += d.Out[w];
                if (d.Role == BuildingRole.Factory)
                {
                    var order = new UnitOrder { Faction = b.Faction, BuildingId = b.Id, Kind = d.Unit };
                    s.UnitQueue.Add(order);
                    e.ProducedLog.Add(order);
                    e.UnitsProduced++;
                    ev.Add(new UnitProduced { Faction = b.Faction, BuildingId = b.Id, Kind = d.Unit });
                }
                b.Status = BuildStatus.Idle;
                return;
            }
            for (int w = 0; w < Wares.Count; w++)
                if (b.In[w] < d.In[w]) { b.Status = BuildStatus.MissingInputs; return; }
            for (int w = 0; w < Wares.Count; w++)
                if (d.Out[w] > 0 && b.Out[w] + d.Out[w] > EconomyTuning.OutputCap) { b.Status = BuildStatus.OutputFull; return; }
            for (int w = 0; w < Wares.Count; w++) b.In[w] -= d.In[w];
            b.Working = true; b.WorkLeft = d.CycleTicks;
            b.Status = BuildStatus.Working;
        }

        // ---------------- matching ----------------

        private struct Request
        {
            public EcoBuilding Consumer;
            public Ware Ware;
            public Priority Prio;
            public int Len;
        }

        private static int Avail(EcoBuilding b, Ware w) =>
            b.IsHq ? b.Stock[(int)w] - b.StockReserved[(int)w] : b.Out[(int)w] - b.OutReserved[(int)w];

        private void Match(EconomyState e, TerritoryState t)
        {
            for (int fi = 0; fi < FactionInfo.Max; fi++)
            {
                var f = (Faction)fi;
                bool anyCarrier = false;
                foreach (var hq in e.Buildings) if (hq.IsHq && hq.Built && hq.Faction == f && hq.IdleCarriers > 0) { anyCarrier = true; break; }
                if (!anyCarrier) continue;

                // Requests: one per missing unit of an input.
                var reqs = new List<Request>();
                foreach (var c in e.Buildings)
                {
                    if ((c.IsHq && c.Built && !c.Upgrading) || c.Faction != f || c.Paused) continue;
                    var want = c.Wanted;
                    for (int w = 1; w < Wares.Count; w++)
                    {
                        if (want[w] <= 0 || c.Prio[w] == Priority.Stop) continue;
                        int cap = c.SiteLike ? want[w] : Math.Max(EconomyTuning.InputBuffer, want[w]);
                        int missing = cap - c.In[w] - c.Incoming[w];
                        for (int k = 0; k < missing; k++)
                        {
                            int len = BestLen(e, t, f, c, (Ware)w);
                            if (len < 0) break;
                            reqs.Add(new Request { Consumer = c, Ware = (Ware)w, Prio = c.Prio[w], Len = len });
                        }
                    }
                }
                reqs.Sort((a, b) =>
                {
                    int x = ((int)b.Prio).CompareTo((int)a.Prio); if (x != 0) return x;
                    x = a.Len.CompareTo(b.Len); if (x != 0) return x;
                    x = a.Consumer.Id.CompareTo(b.Consumer.Id); if (x != 0) return x;
                    return ((int)a.Ware).CompareTo((int)b.Ware);
                });
                foreach (var r in reqs)
                {
                    var c = r.Consumer;
                    int cap = c.SiteLike ? c.Wanted[(int)r.Ware] : Math.Max(EconomyTuning.InputBuffer, c.Wanted[(int)r.Ware]);
                    if (c.In[(int)r.Ware] + c.Incoming[(int)r.Ware] >= cap) continue;
                    TryAssign(e, t, f, r);
                }

                // Leftover outputs go to the nearest HQ.
                foreach (var b in e.Buildings)
                {
                    if (b.IsHq || !b.Built || b.Faction != f) continue;
                    for (int w = 1; w < Wares.Count; w++)
                        while (Avail(b, (Ware)w) > 0)
                            if (!SendToHq(e, t, f, b, (Ware)w)) break;
                }
            }
        }

        private struct Cand { public EcoBuilding Src; public int Len; }

        private List<Cand> Candidates(EconomyState e, TerritoryState t, Faction f, EcoBuilding c, Ware w)
        {
            var l = new List<Cand>();
            foreach (var s in e.Buildings)
            {
                if (s.Faction != f || s.Id == c.Id || (!s.IsHq && !s.Built) || (s.IsHq && !s.Built)) continue;
                if (Avail(s, w) <= 0) continue;
                var r = Route(t, f, s.FlagId, c.FlagId);
                if (r == null) continue;
                l.Add(new Cand { Src = s, Len = r.Length });
            }
            l.Sort((a, b) =>
            {
                int x = a.Len.CompareTo(b.Len); if (x != 0) return x;
                x = (a.Src.IsHq ? 1 : 0).CompareTo(b.Src.IsHq ? 1 : 0); if (x != 0) return x;
                return a.Src.Id.CompareTo(b.Src.Id);
            });
            return l;
        }

        private int BestLen(EconomyState e, TerritoryState t, Faction f, EcoBuilding c, Ware w)
        {
            int best = -1;
            foreach (var s in e.Buildings)
            {
                if (s.Faction != f || s.Id == c.Id || !s.Built || Avail(s, w) <= 0) continue;
                var r = Route(t, f, s.FlagId, c.FlagId);
                if (r != null && (best < 0 || r.Length < best)) best = r.Length;
            }
            return best;
        }

        /// <summary>The HQ with an idle carrier nearest to the flag (the source itself when it is an HQ).</summary>
        private EcoBuilding CarrierHome(EconomyState e, TerritoryState t, Faction f, EcoBuilding src, out FlagRoute toSrc)
        {
            toSrc = null;
            if (src.IsHq)
            {
                if (src.IdleCarriers <= 0) return null;
                toSrc = new FlagRoute { Flags = new[] { src.FlagId }, Roads = new int[0], Nodes = new[] { src.FlagNode } };
                return src;
            }
            EcoBuilding best = null;
            foreach (var hq in e.Buildings)
            {
                if (!hq.IsHq || !hq.Built || hq.Faction != f || hq.IdleCarriers <= 0) continue;
                var r = Route(t, f, hq.FlagId, src.FlagId);
                if (r == null) continue;
                if (best == null || r.Length < toSrc.Length) { best = hq; toSrc = r; }
            }
            return best;
        }

        private bool TryAssign(EconomyState e, TerritoryState t, Faction f, Request r)
        {
            foreach (var cand in Candidates(e, t, f, r.Consumer, r.Ware))
            {
                var home = CarrierHome(e, t, f, cand.Src, out var toSrc);
                if (home == null) continue;
                Launch(e, f, home, toSrc, cand.Src, r.Consumer, false, r.Ware);
                return true;
            }
            return false;
        }

        private bool SendToHq(EconomyState e, TerritoryState t, Faction f, EcoBuilding src, Ware w)
        {
            EcoBuilding dest = null; int destLen = 0;
            foreach (var hq in e.Buildings)
            {
                if (!hq.IsHq || !hq.Built || hq.Faction != f) continue;
                var r = Route(t, f, src.FlagId, hq.FlagId);
                if (r == null) continue;
                if (dest == null || r.Length < destLen) { dest = hq; destLen = r.Length; }
            }
            if (dest == null) return false;
            var home = CarrierHome(e, t, f, src, out var toSrc);
            if (home == null) return false;
            Launch(e, f, home, toSrc, src, dest, true, w);
            return true;
        }

        private static void Launch(EconomyState e, Faction f, EcoBuilding home, FlagRoute toSrc, EcoBuilding src, EcoBuilding dst, bool dstIsHq, Ware w)
        {
            var cw = new Walker
            {
                Id = e.NextWalkerId++, Kind = WalkerKind.Carrier, Faction = f, Home = home.Id, Phase = WalkerPhase.Outbound,
                Path = (int[])toSrc.Nodes.Clone(), JobWare = w, SrcId = src.Id, SrcIsHq = src.IsHq, DstId = dst.Id, DstIsHq = dstIsHq,
            };
            e.Walkers.Add(cw);
            home.IdleCarriers--;
            if (src.IsHq) src.StockReserved[(int)w]++; else src.OutReserved[(int)w]++;
            if (!dstIsHq) dst.Incoming[(int)w]++;
        }

        // ---------------- movement ----------------

        private void Move(GodState s, EconomyState e, TerritoryState t, EventBuffer ev)
        {
            for (int i = 0; i < e.Walkers.Count; i++)
            {
                var w = e.Walkers[i];
                if (w.Phase == WalkerPhase.Working)
                {
                    if (w.Kind == WalkerKind.Harvester && --w.WorkLeft <= 0) HarvestTaken(t, e, w);
                    continue;
                }
                if (w.Index >= w.Path.Length - 1) { Arrive(s, e, t, w, ev); continue; }
                if (++w.Progress >= w.StepTicks)
                {
                    w.Index++; w.Progress = 0;
                    if (w.Index >= w.Path.Length - 1) Arrive(s, e, t, w, ev);
                }
            }
            e.Walkers.RemoveAll(w => w.Id < 0);
        }

        private static void Retire(Walker w) { w.Id = -w.Id - 1; }

        private static void HarvestTaken(TerritoryState t, EconomyState e, Walker w)
        {
            var home = e.Find(w.Home);
            if (home == null) { Retire(w); return; }
            // The pile was reserved for this harvester, so it is there.
            if (t.Piles[w.Target] > 0) t.Piles[w.Target]--;
            e.PilesReserved[w.Target] = Math.Max(0, e.PilesReserved[w.Target] - 1);
            w.JobWare = home.Def.HarvestWare; w.Loaded = true;
            w.Phase = WalkerPhase.Returning;
            GoHomeFull(w);
        }

        private static void GoHomeFull(Walker w)
        {
            var back = new int[w.Path.Length];
            for (int k = 0; k < back.Length; k++) back[k] = w.Path[w.Path.Length - 1 - k];
            w.Path = back; w.Index = 0; w.Progress = 0; w.Phase = WalkerPhase.Returning;
        }

        private void Arrive(GodState s, EconomyState e, TerritoryState t, Walker w, EventBuffer ev)
        {
            var home = e.Find(w.Home);
            switch (w.Kind)
            {
                case WalkerKind.Harvester:
                    if (w.Phase == WalkerPhase.Outbound) { w.Phase = WalkerPhase.Working; w.WorkLeft = EconomyTuning.GatherTicks; }
                    else if (w.Phase == WalkerPhase.Returning)
                    {
                        if (home != null)
                        {
                            home.Out[(int)w.JobWare] += EconomyTuning.WaresPerPile;
                            home.Produced++;
                            home.HarvestWalker = 0;
                        }
                        Retire(w);
                    }
                    break;
                case WalkerKind.Builder:
                    if (w.Phase == WalkerPhase.Outbound)
                    {
                        var site = e.Find(w.Target);
                        if (site == null || site.Built) { GoHome(w, e); break; }
                        w.Phase = WalkerPhase.Working;
                        site.BuilderOnSite = true;
                    }
                    else if (w.Phase == WalkerPhase.Returning)
                    {
                        if (home != null) home.IdleBuilders++;
                        Retire(w);
                    }
                    break;
                default:
                    ArriveCarrier(e, t, w, home);
                    break;
            }
        }

        private void ArriveCarrier(EconomyState e, TerritoryState t, Walker w, EcoBuilding home)
        {
            int wi = (int)w.JobWare;
            switch (w.Phase)
            {
                case WalkerPhase.Returning:
                    if (home != null) home.IdleCarriers++;
                    Retire(w);
                    return;
                case WalkerPhase.Outbound:
                {
                    var src = e.Find(w.SrcId);
                    var dst = e.Find(w.DstId);
                    FlagRoute r = null;
                    if (src != null && dst != null && (r = Route(t, w.Faction, src.FlagId, dst.FlagId)) != null && Avail0(src, w))
                    {
                        if (w.SrcIsHq) { src.Stock[wi]--; src.StockReserved[wi]--; } else { src.Out[wi]--; src.OutReserved[wi]--; }
                        w.Loaded = true; w.Phase = WalkerPhase.Carrying;
                        w.Path = (int[])r.Nodes.Clone(); w.Index = 0; w.Progress = 0;
                    }
                    else
                    {
                        ReleaseJob(e, w); ClearJob(w); GoHome(w, e);
                    }
                    return;
                }
                case WalkerPhase.Carrying:
                {
                    var dst = e.Find(w.DstId);
                    int here = w.Path[w.Path.Length - 1];
                    if (dst == null || (w.DstIsHq && !dst.Built) || (!w.DstIsHq && dst.Wanted[wi] <= 0))
                    {
                        // Destination gone: take it to the nearest HQ, or lose it.
                        var fl = t.FlagAtNode(here);
                        EcoBuilding best = null; FlagRoute br = null;
                        if (fl != null)
                            foreach (var hq in e.Buildings)
                            {
                                if (!hq.IsHq || !hq.Built || hq.Faction != w.Faction) continue;
                                var r = Route(t, w.Faction, fl.Id, hq.FlagId);
                                if (r != null && (br == null || r.Length < br.Length)) { best = hq; br = r; }
                            }
                        if (best == null) { e.LostWares++; ClearJob(w); StartReturnTo(e, t, w, home, here); return; }
                        w.DstId = best.Id; w.DstIsHq = true;
                        w.Path = (int[])br.Nodes.Clone(); w.Index = 0; w.Progress = 0;
                        return;
                    }
                    if (w.DstIsHq) dst.Stock[wi]++;
                    else { dst.In[wi]++; dst.Incoming[wi] = Math.Max(0, dst.Incoming[wi] - 1); }
                    e.DeliveredWares++;
                    int dstFlagNode = dst.FlagNode;
                    ClearJob(w);
                    StartReturnTo(e, t, w, home, dst.FlagNode);
                    return;
                }
            }
        }

        private static bool Avail0(EcoBuilding src, Walker w)
        {
            int wi = (int)w.JobWare;
            return w.SrcIsHq ? src.Stock[wi] > 0 : src.Out[wi] > 0;
        }

        /// <summary>Route from the flag at (or nearest to) a node back to the home HQ; without one the carrier is simply home.</summary>
        private void StartReturnTo(EconomyState e, TerritoryState t, Walker w, EcoBuilding home, int fromNode)
        {
            if (home == null) { Retire(w); return; }
            var fl = t.FlagAtNode(fromNode);
            FlagRoute r = fl == null ? null : Route(t, w.Faction, fl.Id, home.FlagId);
            if (r == null) { home.IdleCarriers++; Retire(w); return; }
            w.Path = (int[])r.Nodes.Clone(); w.Index = 0; w.Progress = 0; w.Phase = WalkerPhase.Returning;
        }
    }
}
