using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// The military tick: sync building health, spawn from the unit queue, keep defend flags staffed, run every unit's
    /// brain (move and fire, all shots resolved together at the end of the tick), apply damage, capture HQs and destroy
    /// other buildings, then age rubble. Units fight only factions theirs is at war with. After the game is over nothing
    /// runs (the faction system decides the end).
    /// </summary>
    public sealed class MilitarySystem : ISimSystem
    {
        private struct Hit { public TargetKind Kind; public int Id; public int Amount; public Faction By; }

        private static readonly int[] RingCos = { 1000, 866, 500, 0, -500, -866, -1000, -866, -500, 0, 500, 866 };
        private static readonly int[] RingSin = { 0, 500, 866, 1000, 866, 500, 0, -500, -866, -1000, -866, -500 };

        public void Tick(GodState s, EventBuffer ev)
        {
            var m = s.Military;
            if (m == null) return;
            if (s.IsGameOver) { s.UnitQueue.Clear(); return; }

            MilitaryRules.SyncBuildings(s, ev);
            MilitaryRules.DrainQueue(s, ev);
            StaffDefends(s);

            var hits = new List<Hit>();
            var units = new List<MilUnit>(m.Units);
            foreach (var u in units) Think(s, u, hits, ev);

            ApplyHits(s, hits, ev);
            MilitaryRules.TickRubble(s, ev);
        }

        // ---------------- groups and defend flags ----------------

        /// <summary>Releases surplus guards and fills flags short of guards, oldest flag first, with the nearest free unit (ties: lowest id).</summary>
        private static void StaffDefends(GodState s)
        {
            var m = s.Military;
            foreach (var u in m.Units)
                if (u.GuardFlagId != 0 && m.DefendById(u.GuardFlagId) == null) { u.GuardFlagId = 0; u.Returning = false; }

            foreach (var f in m.Defends)
            {
                int count = 0;
                foreach (var u in m.Units) if (u.GuardFlagId == f.Id) count++;
                while (count > f.Guards)
                {
                    MilUnit worst = null; long wd = -1;
                    foreach (var u in m.Units)
                    {
                        if (u.GuardFlagId != f.Id) continue;
                        long d = Pathing.Dist2(u.X, u.Z, f.X, f.Z);
                        if (d >= wd) { wd = d; worst = u; }
                    }
                    worst.GuardFlagId = 0; worst.Returning = false;
                    count--;
                }
            }

            foreach (var f in m.Defends)
            {
                int count = 0;
                foreach (var u in m.Units) if (u.GuardFlagId == f.Id) count++;
                while (count < f.Guards)
                {
                    MilUnit best = null; long bd = long.MaxValue;
                    foreach (var u in m.Units)
                    {
                        if (u.Faction != f.Faction || u.GuardFlagId != 0 || u.HasGoal || u.TargetKind != TargetKind.None) continue;
                        long d = Pathing.Dist2(u.X, u.Z, f.X, f.Z);
                        if (d < bd) { bd = d; best = u; }
                    }
                    if (best == null) break;
                    best.GuardFlagId = f.Id; best.Returning = false;
                    count++;
                }
            }
        }

        // ---------------- one unit ----------------

        private static void Think(GodState s, MilUnit u, List<Hit> hits, EventBuffer ev)
        {
            var m = s.Military; var g = s.Territory.Grid;
            if (u.Health <= 0) return;
            if (u.Cooldown > 0) u.Cooldown--;
            if (u.FireFlash > 0) u.FireFlash--;

            if (u.HasGoal)
            {
                u.TargetKind = TargetKind.None; u.TargetId = 0; u.Activity = UnitActivity.Moving;
                if (Pathing.MoveToward(g, u, u.GoalX, u.GoalZ) || (u.X == u.GoalX && u.Z == u.GoalZ)) { u.HasGoal = false; u.Activity = UnitActivity.Idle; }
                return;
            }

            DefendFlag flag = u.GuardFlagId != 0 ? m.DefendById(u.GuardFlagId) : null;
            Acquire(s, u, flag, out var tk, out int tid, out int tx, out int tz, out int edge);
            u.TargetKind = tk; u.TargetId = tid;

            if (tk != TargetKind.None)
            {
                u.Activity = UnitActivity.Fighting;
                var st = MilitaryTuning.Of(u.Kind);
                if (edge <= st.RangeMm)
                {
                    if (tx != u.X || tz != u.Z) { u.FaceX = tx - u.X; u.FaceZ = tz - u.Z; }
                    if (u.Cooldown == 0) Fire(s, u, tk, tid, tx, tz, hits, ev);
                }
                else Pathing.MoveToward(g, u, tx, tz);
                return;
            }

            if (flag != null) { Guard(s, u, flag); return; }

            // Rule 3: march on the faction's first marked target.
            var marks = m.MarkedOf(u.Faction);
            if (marks.Count > 0)
            {
                var b = m.BuildingById(marks[0]);
                if (b != null) { u.Activity = UnitActivity.Marching; Pathing.MoveToward(g, u, b.X, b.Z); return; }
            }
            Wait(s, u);
        }

        private static void Fire(GodState s, MilUnit u, TargetKind tk, int tid, int tx, int tz, List<Hit> hits, EventBuffer ev)
        {
            var m = s.Military;
            TargetClass cls;
            if (tk == TargetKind.Unit) cls = MilitaryTuning.ClassOf(m.UnitById(tid).Kind);
            else cls = TargetClass.Building;
            int dmg = MilitaryTuning.ShotDamage(u.Kind, cls);
            u.Cooldown = MilitaryTuning.Of(u.Kind).CooldownTicks;
            u.FireFlash = MilitaryTuning.FireFlashTicks;
            hits.Add(new Hit { Kind = tk, Id = tid, Amount = dmg, By = u.Faction });
            ev.Add(new UnitFired { AttackerId = u.Id, Faction = u.Faction, Kind = u.Kind, TargetKind = tk, TargetId = tid, FromX = u.X, FromZ = u.Z, ToX = tx, ToZ = tz, Damage = dmg });
        }

        /// <summary>
        /// Rule 1: an enemy (a faction at war with the unit's) within sight; everyone else is walked past. Enemy units first (nearest, lowest id), else enemy buildings (a marked one first,
        /// in marked order, else the nearest, lowest id). A guard only takes targets within 15 m of its anchor, or ones it can
        /// already shoot from where it stands. <paramref name="edge"/> is the distance to the target's edge.
        /// </summary>
        private static void Acquire(GodState s, MilUnit u, DefendFlag flag, out TargetKind kind, out int id, out int tx, out int tz, out int edge)
        {
            var m = s.Military;
            int sight = MilitaryTuning.SightMm, range = MilitaryTuning.Of(u.Kind).RangeMm;
            long sight2 = (long)sight * sight;
            long chase2 = (long)MilitaryTuning.GuardChaseMm * MilitaryTuning.GuardChaseMm;
            kind = TargetKind.None; id = 0; tx = tz = 0; edge = 0;
            var fs = s.Factions;
            bool Hostile(Faction o) => fs != null && fs.AtWar(u.Faction, o);

            long bestD = long.MaxValue;
            foreach (var e in m.Units)
            {
                if (e.Health <= 0 || !Hostile(e.Faction)) continue;
                long d = Pathing.Dist2(u.X, u.Z, e.X, e.Z);
                if (d > sight2 || d >= bestD) continue;
                if (flag != null && Pathing.Dist2(e.X, e.Z, flag.X, flag.Z) > chase2 && d > (long)range * range) continue;
                bestD = d; kind = TargetKind.Unit; id = e.Id; tx = e.X; tz = e.Z; edge = Pathing.ISqrt(d);
            }
            if (kind != TargetKind.None) return;

            BuildingRecord pick = null; int pickEdge = 0;
            bool Visible(BuildingRecord b, out int edgeMm)
            {
                int c = Pathing.Dist(u.X, u.Z, b.X, b.Z);
                edgeMm = System.Math.Max(0, c - MilitaryTuning.FootprintMm(b.Size));
                if (edgeMm > sight) return false;
                if (flag != null && Pathing.Dist2(b.X, b.Z, flag.X, flag.Z) > chase2 && edgeMm > range) return false;
                return true;
            }
            foreach (int mid in m.MarkedOf(u.Faction))
            {
                var b = m.BuildingById(mid);
                if (b != null && Hostile(b.Faction) && Visible(b, out int em)) { pick = b; pickEdge = em; break; }
            }
            if (pick == null)
            {
                int bestEdge = int.MaxValue;
                foreach (var b in m.Buildings)
                {
                    if (!Hostile(b.Faction)) continue;
                    if (Visible(b, out int em) && em < bestEdge) { bestEdge = em; pick = b; pickEdge = em; }
                }
            }
            if (pick == null) return;
            kind = TargetKind.Building; id = pick.Id; tx = pick.X; tz = pick.Z; edge = pickEdge;
        }

        /// <summary>Rule 2: stay at the anchor within 8 m; walk back once further than 7 m, to a slot 5 m from the anchor.</summary>
        private static void Guard(GodState s, MilUnit u, DefendFlag f)
        {
            var g = s.Territory.Grid;
            int k = u.Id % RingCos.Length;
            int sx = f.X + MilitaryTuning.GuardSlotMm * RingCos[k] / 1000, sz = f.Z + MilitaryTuning.GuardSlotMm * RingSin[k] / 1000;
            if (!Pathing.IsLandPoint(g, sx, sz)) { sx = f.X; sz = f.Z; }
            long far = (long)MilitaryTuning.GuardReturnMm * MilitaryTuning.GuardReturnMm;
            if (!u.Returning && Pathing.Dist2(u.X, u.Z, f.X, f.Z) > far) u.Returning = true;
            if (u.Returning)
            {
                u.Activity = UnitActivity.Returning;
                Pathing.MoveToward(g, u, sx, sz);
                if (Pathing.Dist2(u.X, u.Z, sx, sz) <= 1500L * 1500L) u.Returning = false;
                return;
            }
            u.Activity = UnitActivity.Guarding;
        }

        /// <summary>Rule 4: wait on a ring 10 m round the nearest HQ.</summary>
        private static void Wait(GodState s, MilUnit u)
        {
            var m = s.Military; var g = s.Territory.Grid;
            BuildingRecord hq = null; long bd = long.MaxValue;
            foreach (var b in m.Buildings)
            {
                if (!b.IsHq || b.Faction != u.Faction) continue;
                long d = Pathing.Dist2(u.X, u.Z, b.X, b.Z);
                if (d < bd) { bd = d; hq = b; }
            }
            if (hq == null) { u.Activity = UnitActivity.Idle; return; }
            int k = u.Id % RingCos.Length;
            int sx = hq.X + MilitaryTuning.WaitRingMm * RingCos[k] / 1000, sz = hq.Z + MilitaryTuning.WaitRingMm * RingSin[k] / 1000;
            if (!Pathing.IsLandPoint(g, sx, sz)) { sx = hq.X; sz = hq.Z; }
            if (Pathing.Dist2(u.X, u.Z, sx, sz) <= 1000L * 1000L) { u.Activity = UnitActivity.Waiting; return; }
            u.Activity = UnitActivity.Waiting;
            Pathing.MoveToward(g, u, sx, sz);
        }

        // ---------------- damage ----------------

        private static void ApplyHits(GodState s, List<Hit> hits, EventBuffer ev)
        {
            var m = s.Military;
            var byId = new Dictionary<int, MilUnit>();
            foreach (var u in m.Units) byId[u.Id] = u;
            // The faction whose shot took each building to zero (the first such shot in fire order).
            var fellTo = new Dictionary<int, Faction>();
            foreach (var h in hits)
            {
                if (h.Kind == TargetKind.Unit) { if (byId.TryGetValue(h.Id, out var u)) u.Health -= h.Amount; }
                else
                {
                    var b = m.BuildingById(h.Id);
                    if (b == null) continue;
                    bool wasUp = b.Health > 0;
                    b.Health -= h.Amount;
                    if (wasUp && b.Health <= 0) fellTo[b.Id] = h.By;
                }
            }

            for (int i = 0; i < m.Units.Count; i++)
            {
                var u = m.Units[i];
                if (u.Health > 0) continue;
                m.Units.RemoveAt(i--);
                ev.Add(new UnitDied { UnitId = u.Id, Faction = u.Faction, Kind = u.Kind, X = u.X, Z = u.Z });
            }
            var dead = new List<BuildingRecord>();
            foreach (var b in m.Buildings) if (b.Health <= 0) dead.Add(b);
            foreach (var b in dead)
            {
                if (m.BuildingById(b.Id) != b || b.Health > 0) continue;
                var hq = b.IsHq ? s.Territory.HqOf(b.Id) : null;
                // A finished HQ or Citadel is captured by the faction whose units brought it down; everything else burns.
                if (hq != null && !hq.Site && fellTo.TryGetValue(b.Id, out var by) && by != hq.Faction) FactionRules.CaptureHq(s, hq.Id, by, ev);
                else MilitaryRules.Destroy(s, b, ev);
            }
        }
    }

    public static class MilitaryModule
    {
        /// <summary>Registers the military commands and system (order <see cref="SystemOrder.Military"/>).</summary>
        public static void Install(GodSim sim)
        {
            sim.RegisterHandler(new MarkTargetHandler());
            sim.RegisterHandler(new UnmarkTargetHandler());
            sim.RegisterHandler(new SetDefendHandler());
            sim.RegisterHandler(new RemoveDefendHandler());
            sim.RegisterHandler(new DevSpawnUnitHandler());
            sim.RegisterHandler(new DevMoveUnitHandler());
            sim.AddSystem(SystemOrder.Military, new MilitarySystem());
        }

        /// <summary>Creates the military state with a full health record for every building that exists at the start (the HQs).</summary>
        public static void StartGame(GodState s)
        {
            s.Military = new MilitaryState();
            MilitaryRules.SyncBuildings(s, new EventBuffer());
        }
    }
}
