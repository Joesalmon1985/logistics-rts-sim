using System.Collections.Generic;
using God.Sim.Policy;

namespace God.Sim
{
    /// <summary>
    /// The military tick: sync building health, spawn from the unit queue, keep defend flags staffed, run every unit's
    /// brain (move, and say whether it attacks this tick), then resolve the attacks one by one in turn order (longer reach
    /// first, then lower unit id; a soldier killed earlier in the tick does not strike), remove the dead, capture HQs and
    /// destroy other buildings, then age rubble. Attacks on soldiers are duels (<see cref="Duel"/>, rolls from
    /// the keyed stream <c>Rng.Fork("duel", attacker, tick)</c>); attacks on buildings always land for the weapon's building damage. Units fight
    /// only factions theirs is at war with. After the game is over nothing runs (the faction system decides the end).
    /// </summary>
    public sealed class MilitarySystem : ISimSystem
    {
        private struct Attack { public MilUnit By; public TargetKind Kind; public int Id; public int ToX, ToZ; public int Reach; }

        private static readonly ReadOnlyArray<int> RingCos = new ReadOnlyArray<int>(1000, 866, 500, 0, -500, -866, -1000, -866, -500, 0, 500, 866);
        private static readonly ReadOnlyArray<int> RingSin = new ReadOnlyArray<int>(0, 500, 866, 1000, 866, 500, 0, -500, -866, -1000, -866, -500);

        /// <summary>
        /// The combat policy of each faction's soldiers (M1 seam, MILITARY_DESIGN "IMilitaryPolicy"), indexed by faction id.
        /// Default Stage3MilitaryPolicy for all; a scenario's policies.military block sets others through SetPolicy.
        /// Host wiring, not state: a loaded game gets them from its host again.
        /// </summary>
        private readonly IMilitaryPolicy[] _policies = new IMilitaryPolicy[FactionInfo.Max];

        // Per-system scratch, reused for every soldier (no allocation per decision).
        private readonly List<SeenUnit> _seenUnits = new List<SeenUnit>();
        private readonly List<SeenBuilding> _seenBuildings = new List<SeenBuilding>();
        private readonly Rng _scratch = new Rng();

        public MilitarySystem()
        {
            var stage3 = new Stage3MilitaryPolicy();
            for (int i = 0; i < _policies.Length; i++) _policies[i] = stage3;
        }

        public IMilitaryPolicy PolicyOf(Faction f) => _policies[(int)f];

        /// <summary>Sets the combat policy of one faction's soldiers (null is refused: soldiers must decide).</summary>
        public void SetPolicy(Faction f, IMilitaryPolicy policy)
        {
            _policies[(int)f] = policy ?? throw new System.ArgumentNullException(nameof(policy));
        }

        public void Tick(GodState s, EventBuffer ev)
        {
            var m = s.Military;
            if (m == null) return;
            if (s.IsGameOver) { s.UnitQueue.Clear(); return; }

            MilitaryRules.SyncBuildings(s, ev);
            MilitaryRules.DrainQueue(s, ev);
            StaffDefends(s);

            var attacks = new List<Attack>();
            var units = new List<MilUnit>(m.Units);
            foreach (var u in units) Think(s, u, attacks);

            ResolveAttacks(s, attacks, ev);
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

        /// <summary>
        /// One soldier's tick: the body (cooldowns), a developer move order if it has one, else its faction's
        /// <see cref="IMilitaryPolicy"/> decides from what the soldier perceives (<see cref="Perceive"/>) and the intent is
        /// carried out (<see cref="Execute"/>). With <see cref="Stage3MilitaryPolicy"/> this is exactly the stage 3 Think.
        /// </summary>
        private void Think(GodState s, MilUnit u, List<Attack> attacks)
        {
            var m = s.Military; var g = s.Territory.Grid;
            if (u.Health <= 0) return;
            if (u.Cooldown > 0) u.Cooldown--;
            if (u.FireFlash > 0) u.FireFlash--;

            if (u.HasGoal)
            {
                u.TargetKind = TargetKind.None; u.TargetId = 0; u.EngagedId = 0; u.Activity = UnitActivity.Moving;
                if (Pathing.MoveToward(s.Tuning.Military, g, u, u.GoalX, u.GoalZ) || (u.X == u.GoalX && u.Z == u.GoalZ)) { u.HasGoal = false; u.Activity = UnitActivity.Idle; }
                return;
            }

            DefendFlag flag = u.GuardFlagId != 0 ? m.DefendById(u.GuardFlagId) : null;
            var obs = Perceive(s, u, flag);
            var policy = _policies[(int)u.Faction];
            _scratch.SetToFork(s.Rng, "soldier", u.Id, s.Tick);
            var rng = _scratch;
            var intent = IntentRules.Require(PolicyLevel.Military, policy.Name, policy.Decide(obs, MindView.Empty, ref rng));
            Execute(s, u, flag, intent, attacks);
        }

        /// <summary>
        /// What a soldier sees (stage 3 rule 1's filter): enemy units (factions at war with its own) within its sight, and,
        /// for a guard, only those within GuardChaseMm of its post or already in reach; enemy buildings whose edge is within
        /// sight (the same guard limit); its faction's marked targets; and where the first of them stands. Lists keep the
        /// sim's order.
        /// </summary>
        private SoldierObservation Perceive(GodState s, MilUnit u, DefendFlag flag)
        {
            var m = s.Military;
            var st = s.Tuning.Military.Of(u.Kit);
            int sight = st.SightMm, range = st.RangeMm;
            long sight2 = (long)sight * sight;
            long chase2 = (long)MilitaryTuning.GuardChaseMm * MilitaryTuning.GuardChaseMm;
            var fs = s.Factions;
            bool Hostile(Faction o) => fs != null && fs.AtWar(u.Faction, o);

            _seenUnits.Clear();
            foreach (var e in m.Units)
            {
                if (e.Health <= 0 || !Hostile(e.Faction)) continue;
                long d = Pathing.Dist2(u.X, u.Z, e.X, e.Z);
                if (d > sight2) continue;
                if (flag != null && Pathing.Dist2(e.X, e.Z, flag.X, flag.Z) > chase2 && d > (long)range * range) continue;
                _seenUnits.Add(new SeenUnit(e.Id, e.X, e.Z, d, Pathing.ISqrt(d)));
            }

            _seenBuildings.Clear();
            // Buildings matter only when no enemy unit is in sight (rule 1 takes units first), so the scan is skipped then.
            if (_seenUnits.Count == 0)
                foreach (var b in m.Buildings)
                {
                    if (!Hostile(b.Faction)) continue;
                    int c = Pathing.Dist(u.X, u.Z, b.X, b.Z);
                    int edge = System.Math.Max(0, c - MilitaryTuning.FootprintMm(b.Size));
                    if (edge > sight) continue;
                    if (flag != null && Pathing.Dist2(b.X, b.Z, flag.X, flag.Z) > chase2 && edge > range) continue;
                    _seenBuildings.Add(new SeenBuilding(b.Id, b.X, b.Z, edge));
                }

            var marks = m.MarkedOf(u.Faction);
            var first = marks.Count > 0 ? m.BuildingById(marks[0]) : null;
            return new SoldierObservation(u.Id, u.Faction, u.X, u.Z, s.Tick, range, sight, st.Ranged, flag != null,
                _seenUnits, _seenBuildings, marks, first != null, first != null ? first.X : 0, first != null ? first.Z : 0);
        }

        /// <summary>Carries out a combat intent exactly as stage 3 did: fight (strike or close in), guard, march or wait.</summary>
        private static void Execute(GodState s, MilUnit u, DefendFlag flag, NpcIntent intent, List<Attack> attacks)
        {
            var g = s.Territory.Grid;
            bool fight = (intent.Verb == NpcVerb.Attack || intent.Verb == NpcVerb.Charge)
                && (intent.Target == IntentTarget.Unit || intent.Target == IntentTarget.Building);
            var tk = !fight ? TargetKind.None : intent.Target == IntentTarget.Unit ? TargetKind.Unit : TargetKind.Building;
            u.TargetKind = tk; u.TargetId = fight ? intent.TargetId : 0;

            if (fight)
            {
                u.Activity = UnitActivity.Fighting;
                var st = s.Tuning.Military.Of(u.Kit);
                int tx = intent.X, tz = intent.Z, tid = intent.TargetId;
                if (intent.Verb == NpcVerb.Attack)
                {
                    // Ranged soldiers stop at their range; melee ones have closed in.
                    if (tx != u.X || tz != u.Z) { u.FaceX = tx - u.X; u.FaceZ = tz - u.Z; }
                    if (tk == TargetKind.Unit)
                    {
                        // A fight starts (nobody was within reach the tick before): the spear strikes first. Switching from one
                        // foe to the next within a melee is no new fight (else a spear in a crowd would strike every tick).
                        if (u.EngagedId == 0 && !st.Ranged && st.W.Special == WeaponSpecial.FirstStrike) u.Cooldown = 0;
                        u.EngagedId = tid;
                    }
                    else u.EngagedId = 0;
                    if (u.Cooldown == 0) attacks.Add(new Attack { By = u, Kind = tk, Id = tid, ToX = tx, ToZ = tz, Reach = st.RangeMm });
                }
                else { u.EngagedId = 0; Pathing.MoveToward(s.Tuning.Military, g, u, tx, tz); }
                return;
            }
            u.EngagedId = 0;

            if (intent.Verb == NpcVerb.HoldPost && flag != null) { Guard(s, u, flag); return; }
            if (intent.Verb == NpcVerb.Charge && intent.Target == IntentTarget.Point)
            {
                u.Activity = UnitActivity.Marching; Pathing.MoveToward(s.Tuning.Military, g, u, intent.X, intent.Z); return;
            }
            Wait(s, u);
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
                Pathing.MoveToward(s.Tuning.Military, g, u, sx, sz);
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
            Pathing.MoveToward(s.Tuning.Military, g, u, sx, sz);
        }

        // ---------------- damage ----------------

        /// <summary>
        /// Resolves the attacks of this tick in turn order: longer reach first, then lower unit id. An attacker killed
        /// earlier in the tick, or whose target is already down, does not strike (and stays ready).
        /// </summary>
        private static void ResolveAttacks(GodState s, List<Attack> attacks, EventBuffer ev)
        {
            var m = s.Military;
            attacks.Sort((a, b) => a.Reach != b.Reach ? b.Reach.CompareTo(a.Reach) : a.By.Id.CompareTo(b.By.Id));
            var byId = new Dictionary<int, MilUnit>();
            foreach (var u in m.Units) byId[u.Id] = u;
            // The faction whose strike took each building to zero, and the unit that killed each soldier.
            var fellTo = new Dictionary<int, Faction>();
            var killer = new Dictionary<int, int>();
            foreach (var a in attacks)
            {
                var u = a.By;
                if (u.Health <= 0) continue;
                var st = s.Tuning.Military.Of(u.Kit);
                AttackResult res;
                if (a.Kind == TargetKind.Unit)
                {
                    if (!byId.TryGetValue(a.Id, out var t) || t.Health <= 0) continue;
                    var tu = s.Tuning.Military;
                    bool close = st.Ranged && Pathing.Dist2(u.X, u.Z, t.X, t.Z) <= (long)tu.CloseQuartersMm * tu.CloseQuartersMm;
                    // M0: the rolls come from a stream keyed by the attacker and the tick (a unit strikes at most once a tick),
                    // not from a shared stream, so one fight never shifts another's rolls.
                    res = Duel.Resolve(tu, st, tu.Of(t.Kit), s.Rng.Fork("duel", u.Id, s.Tick), close);
                    t.Health -= res.Damage;
                    if (t.Health <= 0) killer[t.Id] = u.Id;
                }
                else
                {
                    var b = m.BuildingById(a.Id);
                    if (b == null || b.Health <= 0) continue;
                    res = new AttackResult(AttackOutcome.Building, s.Tuning.Military.BuildingDamage(st.Weapon));
                    b.Health -= res.Damage;
                    if (b.Health <= 0) fellTo[b.Id] = u.Faction;
                }
                u.Cooldown = st.CooldownTicks;
                u.FireFlash = MilitaryTuning.FireFlashTicks;
                u.LastOutcome = res.Outcome; u.LastDamage = res.Damage;
                ev.Add(new UnitFired
                {
                    AttackerId = u.Id, Faction = u.Faction, Kind = u.Kind, Weapon = st.Weapon, TargetKind = a.Kind, TargetId = a.Id,
                    FromX = u.X, FromZ = u.Z, ToX = a.ToX, ToZ = a.ToZ, Outcome = res.Outcome, Damage = res.Damage,
                });
            }

            for (int i = 0; i < m.Units.Count; i++)
            {
                var u = m.Units[i];
                if (u.Health > 0) continue;
                m.Units.RemoveAt(i--);
                ev.Add(new UnitDied { UnitId = u.Id, Faction = u.Faction, Kind = u.Kind, Kit = u.Kit, X = u.X, Z = u.Z, KillerId = killer.TryGetValue(u.Id, out int kid) ? kid : 0 });
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
