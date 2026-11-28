using System;
using System.Collections.Generic;
using God.Sim;
using God.Sim.Unified;
using God.Train.Honesty;
using God.Train.Net;
using TargetKind = God.Sim.Unified.TargetKind;

namespace God.Train.Env
{
    /// <summary>Parameters of the stub world (harness self-test only).</summary>
    public sealed class StubConfig
    {
        public int Radius = 5, Persons = 4, NeedMask = 0x033, TicksPerDay = 200, Forages = 6, Waters = 3, Rocks = 8, Sight = 4;
        public int HungerPerTick = 2, ThirstPerTick = 3, EnergyPerTick = 1;
        public int PainBand = 800;
    }

    /// <summary>
    /// <b>The stub world: a toy food-and-water hex lattice behind the same interfaces as the real unified world (the twelve
    /// controls, <c>Obs v1</c>, physical legality masks, need pressures). It exists only so the harness can test itself before
    /// UN1/UN2/UN3b/UN6 merge; nothing measured on it is a result.</b> Forage places (regrowing stock; USE empty hand within reach
    /// puts one food lot into the carry), water points (USE empty hand within reach drinks), rocks, ground lots (TAKE, DROP),
    /// eating (USE a carried food lot on self), sleep. Hunger, thirst and energy rise; pain rises in the hunger or thirst pain
    /// band; health falls at near-maximal hunger or thirst; death at health 0. No buildings, no exchange, no conflict.
    /// </summary>
    public sealed class StubWorld : IWorld
    {
        // ---- the lattice ----
        private static readonly int[] DQ = { 1, 1, 0, -1, -1, 0 }, DR = { 0, -1, -1, 0, 1, 1 };
        internal const byte Land = 0, Rock = 1, Water = 2;
        internal const int ClassForage = 1, ClassWater = 2, ClassLot = 3, ClassPerson = 4;
        internal const int Reach = 1;

        internal sealed class Person
        {
            public int Id, Cell, Facing, Health = 1000;
            public short[] Needs = new short[12];
            public bool Alive = true, Asleep;
            public Prim Action; public int TicksLeft; public PrimitiveControl Pending;
            public MotorStatus LastResult;
            public int[] Carry = new int[ObsSchema.CarrySlots]; // food lot ids, 0 = empty
            public bool[] Visited;
            public int Visits;
            public BrainState Brain = new BrainState();
            public int SleepTicks;
        }

        internal sealed class Thing { public int Id, Cell, Class, Stock, Regrow; }

        public readonly StubConfig Config;
        public readonly long Seed;
        public readonly Layout Layout;
        internal readonly int R, Side;
        internal readonly int[] CellQ, CellR;
        internal readonly int[] IndexOf; // (q+R)*Side + (r+R) -> cell or -1
        internal readonly byte[] Terrain;
        internal readonly List<Thing> Things = new List<Thing>(); // forage places and water points, sorted by id
        internal readonly List<Thing> Lots = new List<Thing>();   // ground food lots, sorted by id
        internal readonly List<Person> Persons = new List<Person>();
        private readonly List<int> _ids = new List<int>();
        private int _nextLot = 1000;
        private long _tick;
        private readonly WorldFacts _facts = new WorldFacts();

        public StubWorld(StubConfig cfg, long seed, Layout layout, EvalAccess access)
        {
            SeedPolicy.Check(seed, layout, access);
            Config = cfg; Seed = seed; Layout = layout;
            R = cfg.Radius; Side = 2 * R + 1;
            IndexOf = new int[Side * Side];
            for (int i = 0; i < IndexOf.Length; i++) IndexOf[i] = -1;
            var q = new List<int>(); var r = new List<int>();
            for (int a = -R; a <= R; a++) for (int b = -R; b <= R; b++) if (Dist(a, b, 0, 0) <= R) { IndexOf[(a + R) * Side + (b + R)] = q.Count; q.Add(a); r.Add(b); }
            CellQ = q.ToArray(); CellR = r.ToArray();
            Terrain = new byte[CellQ.Length];
            Generate();
        }

        private static int Dist(int q1, int r1, int q2, int r2) { int dq = q1 - q2, dr = r1 - r2; return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2; }
        internal int Dist(int c1, int c2) => Dist(CellQ[c1], CellR[c1], CellQ[c2], CellR[c2]);
        internal int Cell(int q, int r) => Math.Abs(q) > R || Math.Abs(r) > R ? -1 : IndexOf[(q + R) * Side + (r + R)];
        internal int Neighbour(int cell, int dir) => Cell(CellQ[cell] + DQ[dir], CellR[cell] + DR[dir]);
        internal bool Passable(int cell) => cell >= 0 && Terrain[cell] == Land;

        /// <summary>Layout transform: identity, mirror (swap axes) or a 60-degree rotation.</summary>
        private int Transform(int cell)
        {
            int a = CellQ[cell], b = CellR[cell];
            if (Layout == Layout.Mirrored) return Cell(b, a);
            if (Layout == Layout.Rotated) return Cell(-b, a + b);
            return cell;
        }

        private void Generate()
        {
            var rng = new Rng((ulong)Seed).Fork(Layout == Layout.Regenerated ? "stub-layout-regen" : "stub-layout");
            int n = CellQ.Length, centre = Cell(0, 0);
            var free = new List<int>();
            for (int c = 0; c < n; c++) if (Dist(c, centre) >= 2) free.Add(c);
            rng.Shuffle(free);
            int k = 0, id = 1;
            for (int i = 0; i < Config.Rocks; i++) Terrain[Transform(free[k++])] = Rock;
            for (int i = 0; i < Config.Waters; i++) { int c = Transform(free[k++]); Terrain[c] = Water; Things.Add(new Thing { Id = id++, Cell = c, Class = ClassWater }); }
            for (int i = 0; i < Config.Forages; i++) Things.Add(new Thing { Id = id++, Cell = Transform(free[k++]), Class = ClassForage, Stock = 3 });
            // start cells: the training start region is near the centre; an unfamiliar start is the outer ring
            var starts = new List<int>();
            for (int c = 0; c < n; c++)
            {
                if (!Passable(c)) continue;
                int d = Dist(c, centre);
                if (Layout == Layout.UnfamiliarStart ? d >= R - 1 : d <= 2) starts.Add(c);
            }
            var prng = new Rng((ulong)Seed).Fork("stub-people");
            for (int i = 0; i < Config.Persons; i++)
            {
                var p = new Person { Id = i + 1, Cell = starts[prng.NextInt(starts.Count)], Facing = prng.NextInt(6), Visited = new bool[n] };
                for (int c = 0; c < 12; c++) if ((Config.NeedMask & (1 << c)) != 0 && c != (int)NeedChannel.Pain) p.Needs[c] = (short)prng.NextRange(100, 400);
                p.Visited[p.Cell] = true; p.Visits = 1;
                Persons.Add(p); _ids.Add(p.Id);
            }
        }

        // ---------------- IWorld ----------------
        public string Kind => "stub";
        public long Tick => _tick;
        public int TicksPerDay => Config.TicksPerDay;
        public int NeedMask => Config.NeedMask;
        public int PainBand => Config.PainBand;
        public IReadOnlyList<int> People => _ids;
        internal Person P(int id) => Persons[id - 1];
        public bool Alive(int person) => P(person).Alive;
        public bool AdultAtStart(int person) => true;
        public bool ReadyToDecide(int person) { var p = P(person); return p.Alive && !p.Asleep && p.TicksLeft <= 0; }
        public BrainState BrainOf(int person) => P(person).Brain;
        public int CellsVisited(int person) => P(person).Visits;
        public WorldFacts Facts => _facts;

        public void ReadBody(int person, BodyReading into)
        {
            var p = P(person);
            Array.Copy(p.Needs, into.Needs, 12);
            into.Health = p.Health; into.Alive = p.Alive;
        }

        /// <summary>The perceived entities of a person, nearest first (distance, then class, then id), at most 16.</summary>
        internal int Perceive(Person p, (int cls, int id, int cell, int stock)[] slots)
        {
            int n = 0;
            var all = new List<(int d, int cls, int id, int cell, int stock)>();
            foreach (var t in Things) { int d = Dist(p.Cell, t.Cell); if (d <= Config.Sight) all.Add((d, t.Class, t.Id, t.Cell, t.Stock)); }
            foreach (var t in Lots) { int d = Dist(p.Cell, t.Cell); if (d <= Config.Sight) all.Add((d, ClassLot, t.Id, t.Cell, 1)); }
            foreach (var o in Persons) { if (o == p || !o.Alive) continue; int d = Dist(p.Cell, o.Cell); if (d <= Config.Sight) all.Add((d, ClassPerson, o.Id, o.Cell, 0)); }
            all.Sort((x, y) => x.d != y.d ? x.d.CompareTo(y.d) : x.cls != y.cls ? x.cls.CompareTo(y.cls) : x.id.CompareTo(y.id));
            foreach (var x in all) { if (n == slots.Length) break; slots[n++] = (x.cls, x.id, x.cell, x.stock); }
            for (int i = n; i < slots.Length; i++) slots[i] = (0, 0, -1, 0);
            return n;
        }

        /// <summary>The facing-relative direction (0 ahead, 1 right, 3 behind, 5 left) of the first step towards a cell.</summary>
        internal int Sector(Person p, int target)
        {
            int best = 0, bestD = int.MaxValue;
            for (int rel = 0; rel < 6; rel++)
            {
                int dir = (p.Facing + rel) % 6;
                int q = CellQ[p.Cell] + DQ[dir], r = CellR[p.Cell] + DR[dir];
                int d = Dist(q, r, CellQ[target], CellR[target]);
                if (d < bestD) { bestD = d; best = rel; }
            }
            return best;
        }

        internal readonly (int cls, int id, int cell, int stock)[] Slots = new (int, int, int, int)[ObsSchema.Entities];

        public void Observe(int person, ref Percept pc, short[] obs, ref ActMask m)
        {
            var p = P(person);
            pc.Actor = person; pc.Tick = _tick;
            Array.Clear(obs, 0, obs.Length);
            int o = ObsSchema.OffsetOf("facing"); obs[o + p.Facing] = 1000;
            obs[ObsSchema.OffsetOf("control")] = (short)((int)p.Action * 50);
            obs[ObsSchema.OffsetOf("last_result")] = (short)((int)p.LastResult * 50);
            obs[ObsSchema.OffsetOf("asleep")] = (short)(p.Asleep ? 1000 : 0);
            o = ObsSchema.OffsetOf("needs"); for (int c = 0; c < 12; c++) obs[o + c] = p.Needs[c];
            obs[ObsSchema.OffsetOf("health")] = (short)p.Health;
            obs[ObsSchema.OffsetOf("age_band")] = 1000;
            o = ObsSchema.OffsetOf("carry");
            bool carrying = false, room = false;
            for (int s = 0; s < ObsSchema.CarrySlots; s++)
            {
                if (p.Carry[s] != 0) { obs[o + s * 3] = 250; obs[o + s * 3 + 1] = 1000; obs[o + s * 3 + 2] = 1000; carrying = true; } else room = true;
            }
            o = ObsSchema.OffsetOf("cells");
            for (int rel = 0; rel < 6; rel++)
            {
                int c = Neighbour(p.Cell, (p.Facing + rel) % 6);
                obs[o + rel * 4] = (short)(Passable(c) ? 1000 : 0);
                obs[o + rel * 4 + 1] = (short)(c < 0 ? 1000 : Terrain[c] == Rock ? 1000 : Terrain[c] == Water ? 500 : 0);
                bool w = false, f = false;
                foreach (var t in Things) if (t.Cell == c) { if (t.Class == ClassWater) w = true; else if (t.Stock > 0) f = true; }
                obs[o + rel * 4 + 2] = (short)(w ? 1000 : 0); obs[o + rel * 4 + 3] = (short)(f ? 1000 : 0);
            }
            int n = Perceive(p, Slots);
            o = ObsSchema.OffsetOf("entities");
            ulong targets = 0; bool useOk = carrying, takeOk = false;
            for (int k = 0; k < n; k++)
            {
                var x = Slots[k]; int d = Dist(p.Cell, x.cell), b = o + k * ObsSchema.EntityFields;
                obs[b] = (short)(x.cls * 250); obs[b + 1] = (short)(Sector(p, x.cell) * 200); obs[b + 2] = (short)(d * 250);
                obs[b + 3] = (short)(x.stock > 0 ? 1000 : 0);
                if (d <= Reach)
                {
                    targets |= 1UL << k;
                    if (x.cls == ClassWater) useOk = true;
                    if (x.cls == ClassForage && x.stock > 0 && room) useOk = true;
                    if (x.cls == ClassLot && room) takeOk = true;
                }
            }
            // ---- physical legality only ----
            m = default;
            m.Control = ActMask.With(m.Control, (int)Prim.TurnLeft); m.Control = ActMask.With(m.Control, (int)Prim.TurnRight);
            if (Passable(Neighbour(p.Cell, p.Facing))) m.Control = ActMask.With(m.Control, (int)Prim.StepForward);
            if (Passable(Neighbour(p.Cell, (p.Facing + 3) % 6))) m.Control = ActMask.With(m.Control, (int)Prim.StepBack);
            if (Passable(Neighbour(p.Cell, (p.Facing + 5) % 6))) m.Control = ActMask.With(m.Control, (int)Prim.StepLeft);
            if (Passable(Neighbour(p.Cell, (p.Facing + 1) % 6))) m.Control = ActMask.With(m.Control, (int)Prim.StepRight);
            if (takeOk) m.Control = ActMask.With(m.Control, (int)Prim.Take);
            if (carrying) m.Control = ActMask.With(m.Control, (int)Prim.Drop);
            if (useOk) m.Control = ActMask.With(m.Control, (int)Prim.Use);
            m.Control = ActMask.With(m.Control, (int)Prim.Gesture);
            m.Control = ActMask.With(m.Control, (int)Prim.Sleep);
            m.Control = ActMask.With(m.Control, (int)Prim.Wait);
            m.Target = targets | (1UL << Heads.TargetSelf) | (1UL << Heads.TargetFacing);
            ulong src = 1UL << Heads.SourceEmpty;
            for (int s = 0; s < ObsSchema.CarrySlots; s++) if (p.Carry[s] != 0) src |= 1UL << s;
            m.Source = src;
            m.Slot = 1;
            m.Design = 1UL << Heads.DesignNone;
            m.Gesture = 0xFFFFFFFFUL;
            m.Referent1 = m.Referent2 = (1UL << Heads.Size[Heads.R1]) - 1;
            m.Quantity = 0xFFFF;
        }

        public PrimitiveControl Decode(int person, in ActionChoice a)
        {
            var p = P(person);
            var c = new PrimitiveControl { Control = (Prim)a.Control, Actor = person };
            if (a.Target >= 0 && a.Target < ObsSchema.Entities)
            {
                var x = Slots[a.Target];
                if (x.cell >= 0)
                    c.Target = new TargetRef { Kind = x.cls == ClassForage ? TargetKind.ForagePlace : x.cls == ClassWater ? TargetKind.WaterPoint : x.cls == ClassLot ? TargetKind.Lot : TargetKind.Npc, Id = x.id };
            }
            else if (a.Target == Heads.TargetSelf) c.Target = new TargetRef { Kind = TargetKind.Self, Id = person };
            else if (a.Target == Heads.TargetFacing) c.Target = new TargetRef { Kind = TargetKind.Facing, Id = Neighbour(p.Cell, p.Facing) };
            if (a.Source >= 0 && a.Source < ObsSchema.CarrySlots) c.SourceLot = p.Carry[a.Source];
            if (a.Gesture >= 0) c.Gesture = (byte)a.Gesture;
            if (a.Quantity >= 0) c.Quantity = (short)a.Quantity;
            return c;
        }

        private Thing ThingById(int id) { foreach (var t in Things) if (t.Id == id) return t; return null; }
        private int LotIndex(int id) { for (int i = 0; i < Lots.Count; i++) if (Lots[i].Id == id) return i; return -1; }
        private static int FreeSlot(Person p) { for (int s = 0; s < p.Carry.Length; s++) if (p.Carry[s] == 0) return s; return -1; }
        private static int SlotOf(Person p, int lot) { for (int s = 0; s < p.Carry.Length; s++) if (lot != 0 && p.Carry[s] == lot) return s; return -1; }

        public MotorStatus Submit(in PrimitiveControl c, ControlOrigin origin)
        {
            _facts.Submissions++;
            var st = Start(in c);
            if (c.Actor >= 1 && c.Actor <= Persons.Count) P(c.Actor).LastResult = st;
            if (st == MotorStatus.Accepted) _facts.Accepted++;
            return st;
        }

        private MotorStatus Start(in PrimitiveControl c)
        {
            if (c.Actor < 1 || c.Actor > Persons.Count) return MotorStatus.Invalid;
            var p = P(c.Actor);
            if (!p.Alive) return MotorStatus.Dead;
            if (p.Asleep) return MotorStatus.Asleep;
            if (p.TicksLeft > 0) return MotorStatus.Busy;
            int ticks;
            switch (c.Control)
            {
                case Prim.TurnLeft: case Prim.TurnRight: ticks = 1; break;
                case Prim.StepForward: case Prim.StepBack: case Prim.StepLeft: case Prim.StepRight:
                {
                    int rel = c.Control == Prim.StepForward ? 0 : c.Control == Prim.StepRight ? 1 : c.Control == Prim.StepBack ? 3 : 5;
                    if (!Passable(Neighbour(p.Cell, (p.Facing + rel) % 6))) return MotorStatus.Blocked;
                    ticks = 2; break;
                }
                case Prim.Take:
                {
                    if (c.Target.Kind != TargetKind.Lot) return MotorStatus.Invalid;
                    int i = LotIndex(c.Target.Id);
                    if (i < 0) return MotorStatus.Invalid;
                    if (Dist(p.Cell, Lots[i].Cell) > Reach) return MotorStatus.OutOfReach;
                    if (FreeSlot(p) < 0) return MotorStatus.NoRoom;
                    ticks = 2; break;
                }
                case Prim.Drop: if (SlotOf(p, c.SourceLot) < 0) return MotorStatus.Invalid; ticks = 1; break;
                case Prim.Use:
                {
                    if (c.SourceLot != 0)
                    {
                        if (SlotOf(p, c.SourceLot) < 0) return MotorStatus.Invalid;
                        if (c.Target.Kind != TargetKind.Self) return MotorStatus.NoInteraction;
                        ticks = 4; break;
                    }
                    if (c.Target.Kind != TargetKind.ForagePlace && c.Target.Kind != TargetKind.WaterPoint) return MotorStatus.NoInteraction;
                    var t = ThingById(c.Target.Id);
                    if (t == null) return MotorStatus.Invalid;
                    if (Dist(p.Cell, t.Cell) > Reach) return MotorStatus.OutOfReach;
                    if (t.Class == ClassForage) { if (t.Stock <= 0) return MotorStatus.NotEnough; if (FreeSlot(p) < 0) return MotorStatus.NoRoom; ticks = 6; }
                    else ticks = 8;
                    break;
                }
                case Prim.Gesture: ticks = 3; break;
                case Prim.Sleep: p.Asleep = true; p.SleepTicks = 0; p.Action = Prim.Sleep; return MotorStatus.Accepted;
                case Prim.Wait: ticks = 5; break;
                default: return MotorStatus.Invalid;
            }
            p.Action = c.Control; p.TicksLeft = ticks; p.Pending = c;
            return MotorStatus.Accepted;
        }

        private void Complete(Person p)
        {
            var c = p.Pending;
            switch (c.Control)
            {
                case Prim.TurnLeft: p.Facing = (p.Facing + 5) % 6; break;
                case Prim.TurnRight: p.Facing = (p.Facing + 1) % 6; break;
                case Prim.StepForward: case Prim.StepBack: case Prim.StepLeft: case Prim.StepRight:
                {
                    int rel = c.Control == Prim.StepForward ? 0 : c.Control == Prim.StepRight ? 1 : c.Control == Prim.StepBack ? 3 : 5;
                    int to = Neighbour(p.Cell, (p.Facing + rel) % 6);
                    if (Passable(to)) { p.Cell = to; if (!p.Visited[to]) { p.Visited[to] = true; p.Visits++; } }
                    break;
                }
                case Prim.Take:
                {
                    int i = LotIndex(c.Target.Id), s = FreeSlot(p);
                    if (i >= 0 && s >= 0 && Dist(p.Cell, Lots[i].Cell) <= Reach) { p.Carry[s] = Lots[i].Id; Lots.RemoveAt(i); }
                    break;
                }
                case Prim.Drop:
                {
                    int s = SlotOf(p, c.SourceLot);
                    if (s >= 0) { Lots.Add(new Thing { Id = p.Carry[s], Cell = p.Cell, Class = ClassLot }); p.Carry[s] = 0; Lots.Sort((a, b) => a.Id.CompareTo(b.Id)); }
                    break;
                }
                case Prim.Use:
                    if (c.SourceLot != 0)
                    {
                        int s = SlotOf(p, c.SourceLot);
                        if (s >= 0) { p.Carry[s] = 0; Relieve(p, NeedChannel.Hunger, 450); }
                    }
                    else
                    {
                        var t = ThingById(c.Target.Id);
                        if (t == null) break;
                        if (t.Class == ClassWater) Relieve(p, NeedChannel.Thirst, 600);
                        else { int s = FreeSlot(p); if (t.Stock > 0 && s >= 0) { t.Stock--; p.Carry[s] = _nextLot++; } }
                    }
                    break;
            }
            p.Action = Prim.None;
        }

        private void Relieve(Person p, NeedChannel c, int amount)
        {
            if ((Config.NeedMask & (1 << (int)c)) == 0) return;
            int v = p.Needs[(int)c] - amount; p.Needs[(int)c] = (short)(v < 0 ? 0 : v);
        }

        private void Rise(Person p, NeedChannel c, int amount)
        {
            if ((Config.NeedMask & (1 << (int)c)) == 0) return;
            int v = p.Needs[(int)c] + amount; p.Needs[(int)c] = (short)(v > 1000 ? 1000 : v);
        }

        public void Step()
        {
            foreach (var t in Things) if (t.Class == ClassForage && t.Stock < 3 && ++t.Regrow >= 60) { t.Stock++; t.Regrow = 0; }
            foreach (var p in Persons)
            {
                if (!p.Alive) continue;
                Rise(p, NeedChannel.Hunger, Config.HungerPerTick);
                Rise(p, NeedChannel.Thirst, Config.ThirstPerTick);
                if (p.Asleep)
                {
                    Relieve(p, NeedChannel.Energy, 10);
                    if (++p.SleepTicks >= 60 || p.Needs[(int)NeedChannel.Energy] <= 50) { p.Asleep = false; p.Action = Prim.None; }
                }
                else Rise(p, NeedChannel.Energy, Config.EnergyPerTick);
                if (p.TicksLeft > 0 && --p.TicksLeft == 0) Complete(p);
                int h = p.Needs[(int)NeedChannel.Hunger], th = p.Needs[(int)NeedChannel.Thirst];
                if (h >= Config.PainBand + 100 || th >= Config.PainBand + 100) Rise(p, NeedChannel.Pain, 5); else Relieve(p, NeedChannel.Pain, 2);
                int dmg = (h >= 950 ? 4 : 0) + (th >= 950 ? 4 : 0);
                if (dmg > 0) p.Health -= dmg; else if (p.Health < 1000) p.Health++;
                if (p.Health <= 0)
                {
                    p.Health = 0; p.Alive = false; p.Asleep = false; p.TicksLeft = 0;
                    for (int s = 0; s < p.Carry.Length; s++) if (p.Carry[s] != 0) { Lots.Add(new Thing { Id = p.Carry[s], Cell = p.Cell, Class = ClassLot }); p.Carry[s] = 0; }
                    Lots.Sort((a, b) => a.Id.CompareTo(b.Id));
                }
            }
            _tick++;
        }

        public ulong StateHash()
        {
            var h = new StateHasher();
            h.Add(_tick); h.Add(_nextLot);
            foreach (var t in Things) { h.Add(t.Id); h.Add(t.Stock); h.Add(t.Regrow); }
            foreach (var t in Lots) { h.Add(t.Id); h.Add(t.Cell); }
            foreach (var p in Persons)
            {
                h.Add(p.Id); h.Add(p.Cell); h.Add(p.Facing); h.Add(p.Health); h.Add(p.Alive ? 1 : 0); h.Add(p.Asleep ? 1 : 0); h.Add((int)p.Action); h.Add(p.TicksLeft);
                foreach (var x in p.Needs) h.Add(x);
                foreach (var x in p.Carry) h.Add(x);
                foreach (var x in p.Brain.Recurrent) h.Add(x);
            }
            return h.Value;
        }
    }
}
