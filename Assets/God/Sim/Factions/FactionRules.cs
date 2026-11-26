using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// Victory points, standings, capture and elimination (P7). Read-only queries are safe for views and bots; the
    /// mutating rules are called by the military (capture) and the faction system (elimination, game end) only.
    /// </summary>
    public static class FactionRules
    {
        // ---------------- queries ----------------

        /// <summary>VP of a faction: 1 per finished HQ, 2 per Citadel. HQ sites count nothing.</summary>
        public static int VictoryPoints(GodState s, Faction f)
        {
            int vp = 0;
            foreach (var h in s.Territory.Hqs)
                if (h.Faction == f && !h.Site) vp += h.Citadel ? FactionTuning.CitadelVp : FactionTuning.HqVp;
            return vp;
        }

        /// <summary>
        /// Standings of every faction in the game: most VP first, then most HQs, then factions still in before eliminated
        /// ones (the later out ranks higher), then the lower id. Rank is 1-based; equal keys still get distinct ranks.
        /// </summary>
        public static List<Standing> Standings(GodState s)
        {
            var l = new List<Standing>();
            var fs = s.Factions;
            for (int i = 0; i < s.FactionCount; i++)
            {
                var f = (Faction)i;
                int hqs = 0, cit = 0;
                foreach (var h in s.Territory.Hqs) if (h.Faction == f && !h.Site) { hqs++; if (h.Citadel) cit++; }
                l.Add(new Standing { Faction = f, VictoryPoints = VictoryPoints(s, f), Hqs = hqs, Citadels = cit, Eliminated = fs != null && fs.IsEliminated(f) });
            }
            l.Sort((a, b) =>
            {
                int x = b.VictoryPoints.CompareTo(a.VictoryPoints); if (x != 0) return x;
                x = b.Hqs.CompareTo(a.Hqs); if (x != 0) return x;
                x = a.Eliminated.CompareTo(b.Eliminated); if (x != 0) return x;
                if (a.Eliminated) { x = fs.EliminatedTick[(int)b.Faction].CompareTo(fs.EliminatedTick[(int)a.Faction]); if (x != 0) return x; }
                return ((int)a.Faction).CompareTo((int)b.Faction);
            });
            for (int i = 0; i < l.Count; i++) { var st = l[i]; st.Rank = i + 1; l[i] = st; }
            return l;
        }

        /// <summary>Factions of the game that are not eliminated, ascending.</summary>
        public static List<Faction> Alive(GodState s)
        {
            var l = new List<Faction>();
            for (int i = 0; i < s.FactionCount; i++) if (!s.Factions.IsEliminated((Faction)i)) l.Add((Faction)i);
            return l;
        }

        /// <summary>Ids of the territory buildings (HQ included), flags and roads that pass with an HQ if it is captured now.</summary>
        public static void CaptureArea(GodState s, Hq hq, List<int> buildings, List<int> flags, List<int> roads)
        {
            var t = s.Territory; var old = hq.Faction;
            buildings.Add(hq.Id);
            foreach (var b in t.Buildings)
                if (b.Id != hq.Id && b.Faction == old && b.Size != BuildingSize.Hq && t.InArea(hq, b.Node)) buildings.Add(b.Id);
            var flagSet = new SortedSet<int>();
            foreach (var f in t.Flags) if (f.Faction == old && t.InArea(hq, f.Node)) flagSet.Add(f.Id);
            // A building's own flag goes with it even if it stands just outside the circle.
            foreach (int id in buildings) { var b = t.BuildingById(id); if (b != null && t.FlagById(b.FlagId)?.Faction == old) flagSet.Add(b.FlagId); }
            flags.AddRange(flagSet);
            foreach (var r in t.Roads)
            {
                if (r.Faction != old) continue;
                bool inside = true;
                foreach (int n in r.Nodes) if (!t.InArea(hq, n)) { inside = false; break; }
                if (inside) roads.Add(r.Id);
            }
        }

        // ---------------- peace ----------------

        /// <summary>After peace: each side's marks on the other's buildings are dropped.</summary>
        internal static void DropMarksBetween(GodState s, Faction a, Faction b, EventBuffer ev)
        {
            var m = s.Military; var t = s.Territory;
            if (m == null) return;
            foreach (var (by, other) in new[] { (a, b), (b, a) })
            {
                var marks = m.MarkedBy[(int)by];
                for (int i = marks.Count - 1; i >= 0; i--)
                {
                    var bld = t.BuildingById(marks[i]);
                    if (bld == null || bld.Faction != other) continue;
                    int id = marks[i];
                    marks.RemoveAt(i);
                    ev?.Add(new TargetUnmarked { By = by, BuildingId = id, Destroyed = false });
                }
            }
        }

        // ---------------- capture ----------------

        /// <summary>
        /// An HQ or Citadel fell to <paramref name="to"/>'s units. It passes to <paramref name="to"/> at
        /// <see cref="FactionTuning.CaptureHealthPercent"/>% health with its stock, its idle carriers and builders and any
        /// Citadel work in progress, together with every building and site of the old owner in its build area, the flags in
        /// the area and the roads wholly inside it. The old owner's walkers tied to the area (homed at the HQ or a captured
        /// building, standing in the area, or with a job at a captured building) are removed and recalled to their home store
        /// (whoever owns it now), so carriers are neither lost nor duplicated; loaded cargo is lost. Marks on captured
        /// buildings are dropped unless the marking faction is also at war with the new owner; defend flags anchored on them
        /// are removed.
        /// </summary>
        public static void CaptureHq(GodState s, int hqId, Faction to, EventBuffer ev)
        {
            var t = s.Territory; var e = s.Economy; var m = s.Military;
            var hq = t.HqOf(hqId);
            if (hq == null || hq.Site || hq.Faction == to) return;
            var from = hq.Faction;
            var g = t.Grid;

            var buildings = new List<int>(); var flags = new List<int>(); var roads = new List<int>();
            CaptureArea(s, hq, buildings, flags, roads);
            var captured = new HashSet<int>(buildings);

            // Economy first, while the old ownership still tells which walkers are the old owner's.
            int removed = 0;
            if (e != null)
            {
                foreach (var w in e.Walkers)
                {
                    if (w.Faction != from || w.Id < 0) continue;
                    int node = w.CurrentNode;
                    bool tied = captured.Contains(w.Home)
                        || (node >= 0 && g.IsValid(node) && t.InArea(hq, node))
                        || captured.Contains(w.SrcId) || captured.Contains(w.DstId)
                        || (w.Kind == WalkerKind.Builder && captured.Contains(w.Target));
                    if (!tied) continue;
                    EconomySystem.Recall(e, w);
                    removed++;
                }
                EconomySystem.RemoveRecalled(e);
                foreach (int id in buildings) { var rec = e.Find(id); if (rec != null) rec.Faction = to; }
            }

            // Territory.
            hq.Faction = to;
            foreach (int id in buildings) { var b = t.BuildingById(id); if (b != null) b.Faction = to; }
            foreach (int id in flags) { var f = t.FlagById(id); if (f != null) f.Faction = to; }
            foreach (int id in roads) { var r = t.RoadById(id); if (r != null) r.Faction = to; }
            t.Version++;

            // Military: health, marks, defend flags.
            if (m != null)
            {
                foreach (int id in buildings)
                {
                    var rec = m.BuildingById(id);
                    if (rec == null) continue;
                    rec.Faction = to;
                    var tb = t.BuildingById(id);
                    if (tb != null) m.Known[id] = (to, tb.FlagNode);
                }
                var hrec = m.BuildingById(hqId);
                if (hrec != null) hrec.Health = Math.Max(1, hrec.MaxHealth * FactionTuning.CaptureHealthPercent / 100);
                foreach (var f in FactionInfo.All)
                {
                    var marks = m.MarkedBy[(int)f];
                    for (int i = marks.Count - 1; i >= 0; i--)
                    {
                        if (!captured.Contains(marks[i])) continue;
                        if (f != to && s.Factions.AtWar(f, to)) continue;
                        int id = marks[i];
                        marks.RemoveAt(i);
                        ev?.Add(new TargetUnmarked { By = f, BuildingId = id, Destroyed = false });
                    }
                }
                for (int i = m.Defends.Count - 1; i >= 0; i--)
                {
                    var d = m.Defends[i];
                    if (d.Faction == from && d.AnchorBuildingId != 0 && captured.Contains(d.AnchorBuildingId)) MilitaryRules.DropDefend(m, d.Id, true, ev);
                }
            }

            s.Factions.LastCapturedBy[(int)from] = (int)to;
            ev?.Add(new HqCaptured
            {
                HqId = hqId, From = from, To = to, Citadel = hq.Citadel,
                BuildingIds = buildings.ToArray(), FlagIds = flags.ToArray(), RoadIds = roads.ToArray(), WalkersRemoved = removed,
            });
        }

        // ---------------- elimination ----------------

        /// <summary>
        /// A faction with no finished HQ is out: its HQ sites and any other building it still has burn to rubble, its units
        /// disband, its walkers are removed, its remaining roads and spare flags are removed (no ruins are kept as state),
        /// its marks and defend flags go, and it is at peace with everyone.
        /// </summary>
        public static void Eliminate(GodState s, Faction f, EventBuffer ev)
        {
            var fs = s.Factions; var t = s.Territory; var e = s.Economy; var m = s.Military;
            if (fs.IsEliminated(f)) return;
            fs.Eliminated[(int)f] = true;
            fs.EliminatedTick[(int)f] = s.Tick;

            TradeRules.OnEliminated(s, f, ev);
            if (e != null)
            {
                foreach (var w in e.Walkers) if (w.Faction == f) EconomySystem.Recall(e, w);
                EconomySystem.RemoveRecalled(e);
            }
            if (m != null)
            {
                var left = new List<BuildingRecord>();
                foreach (var r in m.Buildings) if (r.Faction == f) left.Add(r);
                foreach (var r in left) MilitaryRules.Destroy(s, r, ev);
                for (int i = 0; i < m.Units.Count; i++)
                {
                    var u = m.Units[i];
                    if (u.Faction != f) continue;
                    m.Units.RemoveAt(i--);
                    ev?.Add(new UnitDied { UnitId = u.Id, Faction = f, Kind = u.Kind, Kit = u.Kit, X = u.X, Z = u.Z, Disbanded = true });
                }
                foreach (var o in FactionInfo.All)
                {
                    var marks = m.MarkedBy[(int)o];
                    for (int i = marks.Count - 1; i >= 0; i--)
                    {
                        var b = t.BuildingById(marks[i]);
                        if (o != f && b != null && b.Faction != f) continue;
                        ev?.Add(new TargetUnmarked { By = o, BuildingId = marks[i], Destroyed = false });
                        marks.RemoveAt(i);
                    }
                }
                for (int i = m.Defends.Count - 1; i >= 0; i--) if (m.Defends[i].Faction == f) MilitaryRules.DropDefend(m, m.Defends[i].Id, false, ev);
            }
            // Buildings never seen by the military (none in practice) go too, then the roads and spare flags.
            foreach (var b in t.Buildings.ToArray()) if (b.Faction == f) MilitaryTerritory.Remove(s, b.Id, ev);
            TerritoryRules.ClearFaction(s, f, ev);
            s.UnitQueue.RemoveAll(o => o.Faction == f);
            foreach (var o in FactionInfo.All) if (o != f) fs.SetWar(f, o, false);
            ev?.Add(new FactionEliminated { Faction = f, By = fs.LastCapturedBy[(int)f] });
        }
    }

    // ---------------- start corners ----------------

    public static class StartCorners
    {
        /// <summary>
        /// One start corner per faction, from the seed. Farthest-point sampling over the legal land corners (three land
        /// hexes, a land flag node): the first is the corner farthest from the island's middle, each next one the corner
        /// whose nearest chosen start is farthest away on the corner graph (ties: the larger sum of distances to the chosen
        /// starts, then a seeded pick). Every pair is then at least two hex edges apart, so the distance rule holds. The
        /// corners are dealt to the factions in a seeded order. Index i of the result is faction i's corner.
        /// </summary>
        public static int[] Pick(Board board, ulong seed, int count)
        {
            var topo = board.Topology;
            var g = NodeGrid.Get(board.Radius, board.Shape);
            var land = new List<int>();
            foreach (int c in BoardGenerator.LandCorners(board, topo))
            {
                int fn = g.FlagNodeOf(g.CornerNode(c));
                if (fn >= 0 && g.IsLand(fn)) land.Add(c);
            }
            if (land.Count < count) throw new InvalidOperationException("Not enough land corners for the factions.");
            var rng = new Rng(seed).Fork("start.corners");

            double cx = 0, cz = 0;
            for (int h = 0; h < topo.HexCount; h++) { cx += topo.HexX(h); cz += topo.HexZ(h); }
            cx /= topo.HexCount; cz /= topo.HexCount;
            // Squared distance in whole square metres keeps the comparison integer.
            long Far(int c) { double dx = topo.VertexX(c) - cx, dz = topo.VertexZ(c) - cz; return (long)Math.Round(dx * dx + dz * dz); }

            var chosen = new List<int>();
            var ties = new List<int>();
            long best = -1;
            foreach (int c in land)
            {
                long d = Far(c);
                if (d > best) { best = d; ties.Clear(); }
                if (d == best) ties.Add(c);
            }
            chosen.Add(ties[rng.NextInt(ties.Count)]);

            while (chosen.Count < count)
            {
                int bestMin = -1, bestSum = -1;
                ties.Clear();
                foreach (int c in land)
                {
                    if (chosen.Contains(c)) continue;
                    int mn = int.MaxValue, sum = 0;
                    foreach (int o in chosen) { int d = topo.CornerDistance(c, o); mn = Math.Min(mn, d); sum += d; }
                    if (mn > bestMin || (mn == bestMin && sum > bestSum)) { bestMin = mn; bestSum = sum; ties.Clear(); }
                    if (mn == bestMin && sum == bestSum) ties.Add(c);
                }
                if (bestMin < 2) throw new InvalidOperationException("The starts cannot keep the distance rule.");
                chosen.Add(ties[rng.NextInt(ties.Count)]);
            }
            rng.Shuffle(chosen);
            return chosen.ToArray();
        }
    }

    // ---------------- system and install ----------------

    /// <summary>
    /// Runs after the military each tick: eliminates factions left without a finished HQ, then asks each victory condition
    /// of <see cref="GameSetup.Victory"/> in order (M0: <see cref="IVictoryCondition"/>; the stage 3 default is 10 VP, most
    /// VP wins with ties to the lower id, then last standing). The first that returns a result ends the game. Emits
    /// <see cref="GameOver"/> once; after that the sim stops. A sandbox game never ends.
    /// </summary>
    public sealed class FactionSystem : ISimSystem
    {
        /// <summary>Built from the setup on the first tick (the setup never changes once the game starts). Not state.</summary>
        private List<IVictoryCondition> _conditions;

        public void Tick(GodState s, EventBuffer ev)
        {
            var fs = s.Factions;
            if (fs == null || fs.GameOver) return;
            for (int i = 0; i < s.FactionCount; i++)
            {
                var f = (Faction)i;
                if (!fs.IsEliminated(f) && s.Territory.HqsOf(f).Count == 0) FactionRules.Eliminate(s, f, ev);
            }
            var alive = FactionRules.Alive(s);
            if (_conditions == null) _conditions = VictoryConditions.Of(s.Setup);
            var inputs = new VictoryInputs(s, alive);
            int winner = -1;
            var reason = GameEndReason.None;
            foreach (var c in _conditions)
            {
                var r = c.Evaluate(inputs);
                if (!r.Ended) continue;
                winner = r.Winner; reason = r.Reason;
                break;
            }
            if (reason == GameEndReason.None) return;

            fs.GameOver = true;
            fs.GameOverTick = s.Tick;
            fs.Winner = winner;
            fs.EndReason = reason;
            fs.FinalStandings = FactionRules.Standings(s);
            if (s.Military != null)
                foreach (var u in s.Military.Units) { u.Activity = UnitActivity.Idle; u.TargetKind = TargetKind.None; u.TargetId = 0; u.HasGoal = false; }
            ev.Add(new GameOver { Winner = winner, Reason = reason, Standings = new List<Standing>(fs.FinalStandings) });
        }
    }

    public static class FactionsModule
    {
        /// <summary>Creates the faction state: everyone at peace, nobody out.</summary>
        public static void StartGame(GodState s)
        {
            s.Factions = new FactionsState();
        }

        /// <summary>Registers the war, peace and Citadel commands and the faction system.</summary>
        public static void Install(GodSim sim)
        {
            sim.RegisterHandler(new DeclareWarHandler());
            sim.RegisterHandler(new OfferPeaceHandler());
            sim.RegisterHandler(new UpgradeToCitadelHandler());
            sim.AddSystem(FactionTuning.SystemOrder, new FactionSystem());
        }

        /// <summary>FNV-1a over the faction state (relations, eliminations, result).</summary>
        public static ulong Hash(GodState s)
        {
            ulong h = 14695981039346656037UL;
            void Mix(long v) { unchecked { for (int i = 0; i < 8; i++) { h ^= (ulong)((v >> (8 * i)) & 0xFF); h *= 1099511628211UL; } } }
            var fs = s.Factions;
            foreach (bool b in fs.War) Mix(b ? 1 : 0);
            foreach (bool b in fs.PeaceOffer) Mix(b ? 1 : 0);
            for (int i = 0; i < FactionInfo.Max; i++) { Mix(fs.Eliminated[i] ? 1 : 0); Mix(fs.EliminatedTick[i]); Mix(fs.LastCapturedBy[i]); }
            Mix(fs.GameOver ? 1 : 0); Mix(fs.GameOverTick); Mix(fs.Winner); Mix((int)fs.EndReason);
            return h;
        }
    }
}
