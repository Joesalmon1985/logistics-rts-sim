using System;
using System.Collections.Generic;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Unified;
using God.Train.Env;
using God.Train.Net;

namespace God.Train.Teacher
{
    /// <summary>
    /// <b>TRAINING ONLY.</b> The labelled survival demonstrator for the real unified world (origin <see cref="ControlOrigin.Teacher"/>,
    /// curriculum S1-S3: move and explore, eat forage, drink water, sleep). It reads truth (the body's drives, every forage place and
    /// water point) and uses a path oracle (breadth-first search over passable nodes); it exists only in <c>God.Train</c>, never in a
    /// game scenario, and an evaluation rollout that meets it aborts as invalid. Its choices are expressed only through the twelve
    /// primitive controls and the percept's own slots (Act v1), so behaviour cloning can imitate them from Obs v1.
    /// <para>Rules, in order: sleep when tired (or exhausted); drink from a perceived fresh water point in reach when thirsty; eat a
    /// carried food when hungry; forage with an empty hand from a perceived place in reach when hungry or short of food; walk the
    /// oracle path (turn to the next node's direction, then step forward) to the nearest fresh water or forage place by need;
    /// otherwise explore (a seeded random walk), and in S1 (no food or water need active) always explore.</para>
    /// </summary>
    public sealed class SurvivalTeacher : IActor
    {
        public int ThirstAt = 300, HungerAt = 300, TiredAt = 700, Exhausted = 900, CarryFood = 2;
        public ControlOrigin Origin => ControlOrigin.Teacher;
        private Rng _rng;
        private readonly Dictionary<int, int> _wander = new Dictionary<int, int>();
        // BFS scratch
        private int[] _prev = new int[0];
        private readonly Queue<int> _q = new Queue<int>();
        private readonly HashSet<int> _goal = new HashSet<int>();

        public void Begin(IWorld w, long seed)
        {
            if (!(w is UnifiedWorld)) throw new InvalidOperationException("SurvivalTeacher drives the unified world only");
            _rng = new Rng((ulong)seed).Fork("survival-teacher");
            _wander.Clear();
            _side.Clear();
        }

        /// <summary>
        /// Slice 1 V2: the teacher reads only what the body's percept shows (Obs v1: its needs, carry, the six cells, the perceived
        /// entities and its place memory) and the static catalogue (which classes are food); no path oracle, no world query. When
        /// no fresh water (or no food) is perceived or remembered it explores even before the need is pressing.
        /// </summary>
        public bool PerceptOnly;
        /// <summary>Percept-only exploration: per-person turn side and the chance (permille per decision) of a random new heading.</summary>
        public int TurnChance = 60;
        private readonly Dictionary<int, bool> _side = new Dictionary<int, bool>();

        public ActionChoice Act(IWorld w, int personIndex, int person, short[] obs, in ActMask m)
        {
            if (PerceptOnly) return ActPercept((UnifiedWorld)w, person, in m);
            var uw = (UnifiedWorld)w;
            var s = uw.State;
            ref var p = ref uw.LastPercept;
            var npc = s.Population.Find(person);
            var u = s.Unification.PersonOf(person);
            bool foodNeed = Unified.NeedActive(s, NeedChannel.Hunger), waterNeed = Unified.NeedActive(s, NeedChannel.Thirst);
            bool sleepNeed = Unified.NeedActive(s, NeedChannel.Energy);
            int hunger = Math.Min(1000, npc.DriveOf(Drive.Hunger));
            int thirst = waterNeed ? Needs.Pressure(s, person, NeedChannel.Thirst) : 0;
            int fatigue = Math.Min(1000, npc.DriveOf(Drive.Fatigue));
            bool explore = !foodNeed && !waterNeed;

            // ---- sleep
            if (sleepNeed && Allowed(m, Prim.Sleep) && (fatigue >= Exhausted || fatigue >= TiredAt && thirst < 700 && hunger < 700))
                return ActionChoice.Of(Prim.Sleep);

            // ---- perceived things in reach
            int water = -1, forage = -1;
            var g = s.Territory.Grid;
            for (int e = 0; e < p.EntityCount; e++)
            {
                if (g.Distance(npc.Node, p.EntityNode[e]) > Reach.Touch) continue;
                if (p.EntityKind[e] == SpatialKind.WaterPoint && water < 0 && FreshWater(s, p.EntityId[e])) water = e;
                if (p.EntityKind[e] == SpatialKind.ForagePlace && forage < 0 && EdibleForage(s, p.EntityId[e])) forage = e;
            }
            int foodSlot = -1, foods = 0;
            for (int k = 0; k < ObsSchema.CarrySlots; k++)
            {
                if (p.CarryLot[k] == 0) continue;
                var ware = WareOf(npc, p.CarryLot[k]);
                var o = s.Unification.Book.OfWare(ware);
                if (o != null && o.Nutrition > 0) { foods++; if (foodSlot < 0) foodSlot = k; }
            }
            bool emptyHandRoom = HasRoom(npc) && (forage < 0 || PopulationModule.Room(s, npc, ForageWare(s, p.EntityId[forage])) > 0);

            if (!explore)
            {
                if (waterNeed && thirst >= ThirstAt && water >= 0) return Use(water, Heads.SourceEmpty, 1);
                if (foodNeed && hunger >= HungerAt && foodSlot >= 0) return Use(Heads.TargetSelf, foodSlot, 0);
                if (foodNeed && forage >= 0 && emptyHandRoom && (hunger >= HungerAt || foods < CarryFood)) return Use(forage, Heads.SourceEmpty, 0);
                if (waterNeed && water >= 0 && thirst >= 150) return Use(water, Heads.SourceEmpty, 1);

                // ---- walk to the most pressing need's nearest source (path oracle)
                bool wantWater = waterNeed && thirst >= ThirstAt / 2;
                bool wantFood = foodNeed && (foodSlot < 0 && hunger >= HungerAt / 2 || foods < CarryFood && emptyHandRoom);
                bool waterFirst = wantWater && (!wantFood || thirst >= hunger);
                // steer by what the body perceives or remembers (the sector in its own observation), so the choice is imitable
                if (waterFirst && Steer(s, ref p, true, m, out var c1)) { Steered++; return c1; }
                if (wantFood && Steer(s, ref p, false, m, out var c2)) { Steered++; return c2; }
                if (wantWater && Steer(s, ref p, true, m, out var c3)) { Steered++; return c3; }
                // nothing perceived or remembered: explore in a straight line (step forward; turn right when blocked), an imitable
                // search; only in an emergency (thirst or hunger near damage) the path oracle (training only)
                if (wantWater || wantFood)
                {
                    bool emergency = waterNeed && thirst >= OracleThirst || foodNeed && hunger >= OracleHunger;
                    if (!emergency)
                    {
                        Explored++;
                        if (p.V[OCells] != 0 && Allowed(m, Prim.StepForward)) return ActionChoice.Of(Prim.StepForward);
                        return ActionChoice.Of(Prim.TurnRight);
                    }
                }
                int step = -1;
                if (waterFirst) step = NextNode(s, npc.Node, true);
                if (step < 0 && wantFood) step = NextNode(s, npc.Node, false);
                if (step < 0 && wantWater) step = NextNode(s, npc.Node, true);
                if (step >= 0) { OracleSteps++; return Toward(s, npc.Node, u.Facing, step, m); }
                if (!wantWater && !wantFood && Allowed(m, Prim.Wait)) return ActionChoice.Of(Prim.Wait);
            }
            return Wander(s, person, npc.Node, u.Facing, m);
        }

        private static readonly int ONeeds = ObsSchema.OffsetOf("needs"), OCarry = ObsSchema.OffsetOf("carry");

        private ActionChoice ActPercept(UnifiedWorld uw, int person, in ActMask m)
        {
            var s = uw.State;
            ref var p = ref uw.LastPercept;
            var v = p.V;
            bool foodNeed = Unified.NeedActive(s, NeedChannel.Hunger), waterNeed = Unified.NeedActive(s, NeedChannel.Thirst);
            bool sleepNeed = Unified.NeedActive(s, NeedChannel.Energy);
            int hunger = v[ONeeds + (int)NeedChannel.Hunger], thirst = v[ONeeds + (int)NeedChannel.Thirst], fatigue = v[ONeeds + (int)NeedChannel.Energy];

            if (sleepNeed && Allowed(m, Prim.Sleep) && (fatigue >= Exhausted || fatigue >= TiredAt && thirst < 700 && hunger < 700))
                return ActionChoice.Of(Prim.Sleep);
            if (!foodNeed && !waterNeed) { Explored++; return Explore(person, v, m); }

            int water = -1, forage = -1;
            for (int e = 0; e < p.EntityCount; e++)
            {
                int o = OEnt + e * ObsSchema.EntityFields, cls = v[o];
                if (v[o + 2] > Reach.Touch) continue;
                if (cls == Percept.ClassFreshWater && water < 0) water = e;
                if ((v[o + 3] & 15) == 6 && forage < 0 && EdibleClass(s, cls)) forage = e;
            }
            int foodSlot = -1, foods = 0, used = 0;
            for (int k = 0; k < ObsSchema.CarrySlots; k++)
            {
                int cls = v[OCarry + k * ObsSchema.CarryFields];
                if (cls == 0) continue;
                used++;
                var o = cls > 0 && cls <= ushort.MaxValue ? s.Unification.Book.Object((ushort)cls) : null;
                if (o != null && o.Nutrition > 0) { foods++; if (foodSlot < 0) foodSlot = k; }
            }
            bool room = used < ObsSchema.CarrySlots;

            if (waterNeed && thirst >= ThirstAt && water >= 0) return Use(water, Heads.SourceEmpty, 1);
            if (foodNeed && hunger >= HungerAt && foodSlot >= 0) return Use(Heads.TargetSelf, foodSlot, 0);
            if (foodNeed && forage >= 0 && room && (hunger >= HungerAt || foods < CarryFood)) return Use(forage, Heads.SourceEmpty, 0);
            if (waterNeed && water >= 0 && thirst >= 150) return Use(water, Heads.SourceEmpty, 1);

            bool wantWater = waterNeed && thirst >= ThirstAt / 2;
            bool wantFood = foodNeed && (foodSlot < 0 && hunger >= HungerAt / 2 || foods < CarryFood && room);
            bool waterFirst = wantWater && (!wantFood || thirst >= hunger);
            if (waterFirst && Steer(s, ref p, true, m, out var c1)) { Steered++; return c1; }
            if (wantFood && Steer(s, ref p, false, m, out var c2)) { Steered++; return c2; }
            if (wantWater && Steer(s, ref p, true, m, out var c3)) { Steered++; return c3; }
            // no source of a pressing need in view or memory, or no water known at all: search (straight lines, a new heading now
            // and then, turning when blocked)
            bool knowsWater = !waterNeed || Knows(s, v, true), knowsFood = !foodNeed || foodSlot >= 0 || Knows(s, v, false);
            if (wantWater || wantFood || !knowsWater || !knowsFood) { Explored++; return Explore(person, v, m); }
            if (Allowed(m, Prim.Wait)) return ActionChoice.Of(Prim.Wait);
            return Explore(person, v, m);
        }

        /// <summary>A fresh water point (or an edible forage place) is perceived or remembered in the observation.</summary>
        private static bool Knows(GodState s, short[] v, bool water)
        {
            for (int e = 0; e < ObsSchema.Entities; e++)
            {
                int o = OEnt + e * ObsSchema.EntityFields, cls = v[o];
                if (cls == 0) break;
                if (water ? cls == Percept.ClassFreshWater : (v[o + 3] & 15) == 6 && EdibleClass(s, cls)) return true;
            }
            for (int k = 0; k < ObsSchema.Places; k++)
            {
                int cls = v[OPlaces + k * ObsSchema.PlaceFields];
                if (cls != 0 && (water ? cls == Percept.ClassFreshWater : EdibleClass(s, cls))) return true;
            }
            return false;
        }

        /// <summary>Search from the percept alone: step forward while the cell ahead is passable; now and then, or when blocked, turn (each person keeps a turning side, flipped now and then).</summary>
        private ActionChoice Explore(int person, short[] v, in ActMask m)
        {
            if (!_side.TryGetValue(person, out bool right)) { right = _rng.NextInt(2) == 0; _side[person] = right; }
            bool blocked = v[OCells] == 0 || !Allowed(m, Prim.StepForward);
            if (!blocked && _rng.NextInt(1000) >= TurnChance) return ActionChoice.Of(Prim.StepForward);
            if (_rng.NextInt(8) == 0) { right = !right; _side[person] = right; }
            var c = right ? Prim.TurnRight : Prim.TurnLeft;
            return Allowed(m, c) ? ActionChoice.Of(c) : ActionChoice.Of(Prim.Wait);
        }

        /// <summary>Decisions steered by the body's own percept or place memory, and steps that needed the path oracle.</summary>
        public long Steered, OracleSteps, Explored;
        /// <summary>The emergency levels from which the path oracle replaces straight-line exploration.</summary>
        public int OracleThirst = 750, OracleHunger = 850;

        private static readonly int OEnt = ObsSchema.OffsetOf("entities"), OPlaces = ObsSchema.OffsetOf("places"), OCells = ObsSchema.OffsetOf("cells");

        /// <summary>
        /// Turn towards the nearest perceived (else remembered) source of the need by its observed relative sector (0 ahead: step
        /// forward when the cell ahead is passable; 1-3 turn right; 4-5 turn left). False when none is perceived or remembered,
        /// or the way ahead is blocked (the oracle then decides).
        /// </summary>
        private bool Steer(GodState s, ref Percept p, bool water, in ActMask m, out ActionChoice c)
        {
            c = default;
            var v = p.V;
            int sector = -1;
            for (int e = 0; e < p.EntityCount && sector < 0; e++)
            {
                int ecls = v[OEnt + e * ObsSchema.EntityFields];
                bool hit = PerceptOnly
                    ? (water ? ecls == Percept.ClassFreshWater : (v[OEnt + e * ObsSchema.EntityFields + 3] & 15) == 6 && EdibleClass(s, ecls))
                    : water ? p.EntityKind[e] == SpatialKind.WaterPoint && FreshWater(s, p.EntityId[e])
                                 : p.EntityKind[e] == SpatialKind.ForagePlace && EdibleForage(s, p.EntityId[e]);
                if (hit) sector = v[OEnt + e * ObsSchema.EntityFields + 1];
            }
            int bestD = int.MaxValue;
            if (sector < 0)
                for (int k = 0; k < ObsSchema.Places; k++)
                {
                    int o = OPlaces + k * ObsSchema.PlaceFields, cls = v[o];
                    if (cls == 0) continue;
                    bool hit = water ? cls == Percept.ClassFreshWater : EdibleClass(s, cls);
                    if (hit && v[o + 2] < bestD) { bestD = v[o + 2]; sector = v[o + 1]; }
                }
            if (sector < 0 || sector == 6) return false;
            if (sector == 0)
            {
                if (v[OCells] == 0 || !Allowed(m, Prim.StepForward)) return false; // blocked ahead
                c = ActionChoice.Of(Prim.StepForward); return true;
            }
            c = ActionChoice.Of(sector <= 3 ? Prim.TurnRight : Prim.TurnLeft);
            return true;
        }

        private static bool EdibleClass(GodState s, int cls)
        {
            if (cls <= 0 || cls > ushort.MaxValue) return false;
            var o = s.Unification.Book.Object((ushort)cls);
            return o != null && o.Nutrition > 0 && o.Interactions.Count > 0 && o.Interactions[0].TargetKind == God.Sim.Unified.TargetKind.ForagePlace;
        }

        // ---------------- helpers ----------------

        private static bool Allowed(in ActMask m, Prim c) => ActMask.Allows(m.Control, (int)c);

        private static ActionChoice Use(int target, int source, int slot)
        {
            var a = ActionChoice.Of(Prim.Use);
            a.Target = target; a.Source = source; a.Slot = slot; a.Design = Heads.DesignNone;
            return a;
        }

        private static bool FreshWater(GodState s, int id) { foreach (var x in s.Unification.WaterPoints) if (x.Id == id) return x.Fresh; return false; }

        private static bool EdibleForage(GodState s, int id)
        {
            foreach (var x in s.Unification.ForagePlaces)
                if (x.Id == id) { var o = s.Unification.Book.OfWare((Ware)x.Ware); return o != null && o.Nutrition > 0; }
            return false;
        }

        private static Ware WareOf(Npc npc, int lot) { foreach (var l in npc.Carry) if (!l.Empty && l.Id == lot) return l.Ware; return Ware.None; }

        private static Ware ForageWare(GodState s, int id) { foreach (var x in s.Unification.ForagePlaces) if (x.Id == id) return (Ware)x.Ware; return Ware.None; }

        private static bool HasRoom(Npc npc)
        {
            if (npc.Carry == null) return false;
            foreach (var l in npc.Carry) if (l.Empty) return true;
            return false;
        }

        /// <summary>The first node of a shortest passable path from <paramref name="from"/> to a node within touch of the nearest source (-1 none, or already there).</summary>
        private int NextNode(GodState s, int from, bool water)
        {
            var g = s.Territory.Grid;
            _goal.Clear();
            if (water) { foreach (var x in s.Unification.WaterPoints) if (x.Fresh) AddGoal(s, x.Node); }
            else foreach (var x in s.Unification.ForagePlaces) { var o = s.Unification.Book.OfWare((Ware)x.Ware); if (o != null && o.Nutrition > 0) AddGoal(s, x.Node); }
            if (_goal.Count == 0 || _goal.Contains(from)) return -1;
            if (_prev.Length != g.Count) _prev = new int[g.Count];
            for (int i = 0; i < _prev.Length; i++) _prev[i] = -2;
            _q.Clear(); _q.Enqueue(from); _prev[from] = -1;
            int found = -1;
            while (_q.Count > 0)
            {
                int v = _q.Dequeue();
                if (_goal.Contains(v)) { found = v; break; }
                for (int d = 0; d < 6; d++)
                {
                    int n = g.Neighbour(v, d);
                    if (n < 0 || _prev[n] != -2 || !Motor.Passable(s, n)) continue;
                    _prev[n] = v; _q.Enqueue(n);
                }
            }
            if (found < 0) return -1;
            int at = found;
            while (_prev[at] != from) at = _prev[at];
            return at;
        }

        private void AddGoal(GodState s, int node)
        {
            var g = s.Territory.Grid;
            if (Motor.Passable(s, node)) _goal.Add(node);
            for (int d = 0; d < 6; d++) { int n = g.Neighbour(node, d); if (n >= 0 && Motor.Passable(s, n)) _goal.Add(n); }
        }

        /// <summary>Face the neighbour <paramref name="next"/>, then step forward.</summary>
        private static ActionChoice Toward(GodState s, int node, byte facing, int next, in ActMask m)
        {
            var g = s.Territory.Grid;
            int dir = -1;
            for (int d = 0; d < 6; d++) if (g.Neighbour(node, d) == next) { dir = d; break; }
            if (dir < 0) return ActionChoice.Of(Prim.Wait);
            if (dir == facing && Allowed(m, Prim.StepForward)) return ActionChoice.Of(Prim.StepForward);
            int right = (dir - facing + 6) % 6;
            return ActionChoice.Of(right <= 3 ? Prim.TurnRight : Prim.TurnLeft);
        }

        /// <summary>Explore: keep a heading (a seeded direction per person) while the cell ahead is passable, else turn.</summary>
        private ActionChoice Wander(GodState s, int person, int node, byte facing, in ActMask m)
        {
            if (!_wander.TryGetValue(person, out int dir) || _rng.NextInt(12) == 0) { dir = _rng.NextInt(6); _wander[person] = dir; }
            var g = s.Territory.Grid;
            int ahead = g.Neighbour(node, dir);
            if (ahead < 0 || !Motor.Passable(s, ahead)) { dir = _rng.NextInt(6); _wander[person] = dir; return ActionChoice.Of(Prim.TurnRight); }
            return Toward(s, node, facing, ahead, m);
        }
    }
}
