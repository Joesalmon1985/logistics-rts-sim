using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;

namespace God.Sim.Population
{
    /// <summary>
    /// The population system (M3), once per tick in ascending NPC id:
    /// <list type="number">
    /// <item>day start (first tick of a day): spoiling, regrowth, beliefs age, the day's learning shares roll over;</item>
    /// <item>body: drives and body change by what the NPC is doing; starvation and exposure; death with one cause;</item>
    /// <item>reflexes: wake, collapse asleep at top fatigue, finish a meal;</item>
    /// <item>on its think tick (<c>(tick + id) % ThinkEvery == 0</c>, not asleep, not busy): settle drive reward, credit and
    /// close traces, sense (perception within a radius, from a spatial index), think (beliefs, attention, context,
    /// candidates: <see cref="Thinker"/>), decide (its <see cref="INpcPolicy"/> on <see cref="NpcObservation"/> and
    /// <see cref="MindView"/> only), validate the intent against the candidates, open a trace, start the action;</item>
    /// <item>act: walk a step, work a tick (forage, gather, build), finishing units, rewards for goods and the bed right.</item>
    /// </list>
    /// Caches (the path searcher, the spatial index, the think scratch) are rebuilt from state and never saved.
    /// </summary>
    public sealed class PopulationSystem : ISimSystem
    {
        private const int CellMm = 60000;

        private NodeGrid _g;
        private NodePather _pather;
        private bool[] _coast;
        private int[] _cellOf;
        private int _cellsX, _cellsZ, _minX, _minZ;
        private List<int>[] _srcCells, _hutCells, _lotCells, _unitCells;
        private readonly ThinkScratch _sc = new ThinkScratch();
        private readonly EventLearningSink _sink = new EventLearningSink();
        private readonly RecruitScratch _recruit = new RecruitScratch();
        private readonly AssociativeNpcPolicy _assoc = new AssociativeNpcPolicy();
        private readonly SortedDictionary<string, INpcPolicy> _policies = new SortedDictionary<string, INpcPolicy>(StringComparer.Ordinal);
        private readonly Sensed[] _near = new Sensed[96];
        private long _unitsTick = -1;

        /// <summary>Thinks run so far (the runner's decision counter; not state).</summary>
        public long Thinks;

        /// <summary>A read-only hook for tests and tools: called with the NPC id and the think's scratch after Cognition, before the policy decides. It must not change anything.</summary>
        public Action<int, ThinkScratch> Inspect;

        public PopulationSystem()
        {
            _policies[Policies.Associative] = _assoc;
        }

        /// <summary>Makes a civilian policy available under its name (scenario <c>policies.civilian</c>). The NPC record names the policy it uses.</summary>
        public void SetPolicy(string name, INpcPolicy policy) { _policies[name] = policy ?? throw new ArgumentNullException(nameof(policy)); }

        public INpcPolicy PolicyOf(string name) => name != null && _policies.TryGetValue(name, out var p) ? p : _assoc;

        public NodePather Pather => _pather;

        /// <summary>The NPC's learning parameters with the tuning's adult learning-rate shift (life stages arrive at M4).</summary>
        public static LearnTraits TraitsOf(GodState s, Npc npc)
        {
            var t = npc.LearnTraits;
            t.AlphaShift = s.Tuning.Learning.AlphaShift;
            return t;
        }

        // ---------------- setup of the caches ----------------

        private void Init(GodState s)
        {
            if (_g != null) return;
            _g = s.Territory.Grid;
            _pather = new NodePather(_g);
            _coast = new bool[_g.Count];
            _cellOf = new int[_g.Count];
            int minX = int.MaxValue, minZ = int.MaxValue, maxX = int.MinValue, maxZ = int.MinValue;
            for (int n = 0; n < _g.Count; n++)
            {
                int x = MmX(_g, n), z = MmZ(_g, n);
                if (x < minX) minX = x; if (z < minZ) minZ = z; if (x > maxX) maxX = x; if (z > maxZ) maxZ = z;
            }
            _minX = minX; _minZ = minZ;
            _cellsX = (maxX - minX) / CellMm + 1; _cellsZ = (maxZ - minZ) / CellMm + 1;
            for (int n = 0; n < _g.Count; n++)
            {
                _coast[n] = PopulationModule.IsCoast(_g, n);
                _cellOf[n] = CellOfMm(MmX(_g, n), MmZ(_g, n));
            }
            int cells = _cellsX * _cellsZ;
            _srcCells = NewCells(cells); _hutCells = NewCells(cells); _lotCells = NewCells(cells); _unitCells = NewCells(cells);
            s.Population.IndexDirty = true;
        }

        private static List<int>[] NewCells(int n) { var a = new List<int>[n]; for (int i = 0; i < n; i++) a[i] = new List<int>(); return a; }

        /// <summary>Integer millimetres of a node (the lattice's X = 4330 i, Z = 2500 i + 5000 j; 0.13 mm per column from the exact value).</summary>
        public static int MmX(NodeGrid g, int n) => 4330 * g.I(n);
        public static int MmZ(NodeGrid g, int n) => 2500 * g.I(n) + 5000 * g.J(n);

        private int CellOfMm(int x, int z)
        {
            int cx = (x - _minX) / CellMm, cz = (z - _minZ) / CellMm;
            if (cx < 0) cx = 0; if (cz < 0) cz = 0; if (cx >= _cellsX) cx = _cellsX - 1; if (cz >= _cellsZ) cz = _cellsZ - 1;
            return cx * _cellsZ + cz;
        }

        private void RebuildIndex(PopulationState p)
        {
            foreach (var c in _srcCells) c.Clear();
            foreach (var c in _hutCells) c.Clear();
            foreach (var c in _lotCells) c.Clear();
            for (int i = 0; i < p.Sources.Count; i++) _srcCells[_cellOf[p.Sources[i].Node]].Add(i);
            for (int i = 0; i < p.Huts.Count; i++) _hutCells[_cellOf[p.Huts[i].Node]].Add(i);
            for (int i = 0; i < p.Lots.Count; i++) _lotCells[_cellOf[p.Lots[i].Node]].Add(i);
            p.IndexDirty = false;
        }

        private void IndexUnits(GodState s)
        {
            if (_unitsTick == s.Tick) return;
            _unitsTick = s.Tick;
            foreach (var c in _unitCells) c.Clear();
            if (s.Military == null) return;
            var units = s.Military.Units;
            for (int i = 0; i < units.Count; i++) _unitCells[CellOfMm(units[i].X, units[i].Z)].Add(i);
        }

        // ---------------- the tick ----------------

        public void Tick(GodState s, EventBuffer ev)
        {
            var p = s.Population;
            if (p == null) return;
            Init(s);
            var cal = SimCalendar.Of(s);
            long day = cal.Day(s.Tick);
            if (!p.FoundersAnnounced)
            {
                foreach (var n in p.Npcs) if (n.Founder) ev.Add(new NpcSpawned { Npc = n.Id, Faction = n.Faction, Node = n.Node, Founder = true });
                p.FoundersAnnounced = true;
            }
            if (day > p.DayDone) DayStart(s, ev, (int)day);
            if (p.IndexDirty) RebuildIndex(p);
            var tu = s.Tuning.Population;
            for (int i = 0; i < p.Npcs.Count; i++)
            {
                var npc = p.Npcs[i];
                if (!npc.Alive) continue;
                Body(s, npc, cal);
                if (npc.Health <= 0) { Die(s, ev, npc, npc.DeathCause == DeathCause.None ? DeathCause.Starvation : npc.DeathCause); continue; }
                Account(npc);
                if (npc.Action.Asleep)
                {
                    // rested, or hungry enough to get up once somewhat rested (exhaustion keeps a starving sleeper asleep)
                    int f = npc.DriveOf(Drive.Fatigue);
                    if (f <= tu.WakeFatigue || npc.DriveOf(Drive.Hunger) >= tu.WakeHunger && f < tu.SleepFrom) Wake(s, ev, npc);
                    else continue;
                }
                if (npc.DriveOf(Drive.Fatigue) >= tu.CollapseFatigue) { Collapse(s, ev, npc); continue; }
                if (npc.Action.BusyUntil > 0)
                {
                    if (s.Tick >= npc.Action.BusyUntil) FinishBusy(s, ev, npc);
                    else continue;
                }
                if ((s.Tick + npc.Id) % tu.ThinkEvery == 0)
                {
                    if (p.IndexDirty) RebuildIndex(p);
                    Think(s, ev, npc, (int)day);
                }
                if (npc.Alive && !npc.Action.Asleep && npc.Action.BusyUntil == 0) Act(s, ev, npc, (int)day);
                if (p.IndexDirty) RebuildIndex(p);
            }
        }

        /// <summary>Today's time allocation and need levels: one tick of what the NPC is doing (read-only of the sim; counters only).</summary>
        private static void Account(Npc npc)
        {
            npc.DayTicks[Activity.Of(npc.Action)]++;
            if (!npc.Action.Asleep && npc.Path != null && npc.PathIdx < npc.Path.Length) npc.DayWalkTicks++;
            for (int d = 0; d < Drive.Count; d++) npc.DayDriveSum[d] += npc.Drives[d] / 1000;
        }

        /// <summary>The person-day row of the day that just ended (<see cref="NpcDay"/>), then the counters reset.</summary>
        private static void PersonDay(GodState s, EventBuffer ev, Npc npc, int endedDay, int tpd)
        {
            int ticks = 0;
            foreach (int t in npc.DayTicks) ticks += t;
            var m = npc.Mind;
            int Mean(int d) => ticks == 0 ? 0 : npc.DayDriveSum[d] / ticks;
            int carry = 0;
            foreach (var l in npc.Carry) if (!l.Empty) carry += l.Count;
            var mode = m.Store.Mode;
            ev.Add(new NpcDay
            {
                Npc = npc.Id, Faction = npc.Faction, Day = endedDay, AgeDays = (int)((s.Tick - npc.BornTick) / tpd), Node = npc.Node,
                Health = npc.HealthPermille, Nutrition = npc.NutritionPermille,
                Hunger = npc.DriveOf(Drive.Hunger), Fatigue = npc.DriveOf(Drive.Fatigue), Cold = npc.DriveOf(Drive.Cold), Fear = npc.DriveOf(Drive.Fear), Pain = npc.DriveOf(Drive.Pain),
                MeanHunger = Mean(Drive.Hunger), MeanFatigue = Mean(Drive.Fatigue), MeanCold = Mean(Drive.Cold), MeanFear = Mean(Drive.Fear), MeanPain = Mean(Drive.Pain),
                Ticks = (int[])npc.DayTicks.Clone(), WalkTicks = npc.DayWalkTicks, Meals = npc.DayMeals, HasBed = npc.HomeHut != 0, CarryUnits = carry,
                Decisions = m.DayDecisions,
                LearnedShare = m.DayDecisions > 0 ? m.DayLearnedChanged * 1000 / m.DayDecisions : 0,
                ExploreShare = m.DayDecisions > 0 ? m.DayExploreWon * 1000 / m.DayDecisions : 0,
                RewardSum = m.Store.RewardSumDay, PunishSum = m.Store.PunishSumDay, Associations = m.Store.Count, Conjunctions = m.Store.ConjUsed,
                Mode = mode == LearningMode.On ? LearningModeCode.On : mode == LearningMode.Frozen ? LearningModeCode.Frozen : LearningModeCode.Off,
            });
            System.Array.Clear(npc.DayTicks, 0, npc.DayTicks.Length);
            System.Array.Clear(npc.DayDriveSum, 0, npc.DayDriveSum.Length);
            npc.DayWalkTicks = 0; npc.DayMeals = 0;
        }

        // ---------------- day start ----------------

        private void DayStart(GodState s, EventBuffer ev, int day)
        {
            var p = s.Population;
            bool rowDue = p.DayDone >= 0; // no row before the first day
            p.DayDone = day;
            int tpd = SimCalendar.Of(s).TicksPerDay;
            if (rowDue) foreach (var npc in p.Npcs) if (npc.Alive) PersonDay(s, ev, npc, day - 1, tpd);
            var lt = s.Tuning.Learning;
            // spoiling (ECONOMY A3): carried and ground lots past their day are gone, one Consumed(Spoiled) each
            foreach (var npc in p.Npcs)
            {
                if (!npc.Alive) continue;
                for (int i = 0; i < npc.Carry.Length; i++)
                {
                    var l = npc.Carry[i];
                    if (l.Empty || l.SpoilDay < 0 || l.SpoilDay >= day) continue;
                    p.Ledger.Consume(ConsumeReason.Spoiled, l.Ware, l.Count);
                    ev.Add(new Consumed { Npc = npc.Id, Ware = l.Ware, Count = l.Count, Reason = ConsumeReason.Spoiled });
                    Learner.AddReward(npc.Mind.Store, -Worth(s, npc, l.Ware) * l.Count); // goods lost
                    npc.Carry[i] = default;
                }
                // the mind's day: beliefs age, the shares roll over
                var m = npc.Mind;
                m.Beliefs.Age(50);
                m.LearnedSharePrev = m.DayDecisions > 0 ? m.DayLearnedChanged * 1000 / m.DayDecisions : 0;
                m.ExploreSharePrev = m.DayDecisions > 0 ? m.DayExploreWon * 1000 / m.DayDecisions : 0;
                m.DayDecisions = 0; m.DayLearnedChanged = 0; m.DayExploreWon = 0; m.DayStamp = day;
                m.Store.RewardSumDay = 0; m.Store.PunishSumDay = 0;
            }
            for (int i = p.Lots.Count - 1; i >= 0; i--)
            {
                var g = p.Lots[i];
                if (g.Lot.SpoilDay < 0 || g.Lot.SpoilDay >= day) continue;
                p.Ledger.Consume(ConsumeReason.Spoiled, g.Lot.Ware, g.Lot.Count);
                ev.Add(new Consumed { Ware = g.Lot.Ware, Count = g.Lot.Count, Reason = ConsumeReason.Spoiled });
                p.Lots.RemoveAt(i);
                p.IndexDirty = true;
            }
            Regrow(s);
        }

        /// <summary>Logistic regrowth once a day (ECONOMY 4.8): <c>Stock += r Stock (C - Stock) / (C 1000)</c>, at least 1 while partly stocked; an empty source recovers one unit every few days only beside a neighbour of its kind above half capacity.</summary>
        private void Regrow(GodState s)
        {
            var p = s.Population;
            var eco = s.Tuning.Ecology;
            foreach (var src in p.Sources)
            {
                if (src.Stock >= src.Capacity) { src.EmptyDays = 0; continue; }
                if (src.Stock > 0)
                {
                    long add = (long)src.RegrowPermille * src.Stock * (src.Capacity - src.Stock) / ((long)src.Capacity * 1000);
                    src.Stock = (int)Math.Min(src.Capacity, src.Stock + Math.Max(1, add));
                    src.EmptyDays = 0;
                    continue;
                }
                bool neighbour = false;
                foreach (var o in p.Sources)
                    if (o != src && o.Kind == src.Kind && o.Stock * 2 > o.Capacity && _g.Distance(o.Node, src.Node) <= eco.NeighbourNodes) { neighbour = true; break; }
                if (!neighbour) { src.EmptyDays = 0; continue; }
                if (++src.EmptyDays >= eco.EmptyRecoverDays) { src.Stock = 1; src.EmptyDays = 0; }
            }
        }

        // ---------------- body ----------------

        private static int PerTick(int perDay, int ticksPerDay) => (int)((long)perDay * 1000 / ticksPerDay);

        private void Body(GodState s, Npc npc, SimCalendar cal)
        {
            var tu = s.Tuning.Population;
            int tpd = cal.TicksPerDay;
            var a = npc.Action;
            bool working = !a.Asleep && (a.Verb == Verb.Gather || a.Verb == Verb.Build || a.Verb == Verb.ForageAt) && a.Progress > 0;
            bool walking = !a.Asleep && npc.Path != null && npc.PathIdx < npc.Path.Length;
            bool inHut = a.Asleep && a.SleepHut != 0;
            bool night = TimeOf(cal, s.Tick) == TimeOfDay.Night;

            int hunger = tu.HungerPerDay + (working ? tu.HungerWorkPerDay : 0);
            Add(npc, Drive.Hunger, PerTick(hunger, tpd));
            if (a.Asleep) Add(npc, Drive.Fatigue, -PerTick(inHut ? tu.SleepHutPerDay : tu.SleepOpenPerDay, tpd));
            else Add(npc, Drive.Fatigue, PerTick(tu.FatiguePerDay + (working ? tu.FatigueWorkPerDay : 0) + (walking ? tu.FatigueWalkPerDay : 0)
                + (TimeOf(cal, s.Tick) != TimeOfDay.Day ? tu.FatigueNightPerDay : 0), tpd));
            if (inHut) Add(npc, Drive.Cold, -PerTick(tu.ColdHutFallPerDay, tpd));
            else if (night) Add(npc, Drive.Cold, PerTick(tu.ColdNightPerDay, tpd));
            else Add(npc, Drive.Cold, -PerTick(tu.ColdDayFallPerDay, tpd));
            Add(npc, Drive.Fear, -PerTick(tu.FearFallPerDay, tpd));
            // pain: the injury, or a decaying extra (a toxic meal), whichever is higher
            bool resting = a.Verb == Verb.Rest || a.Asleep;
            npc.PainExtra = Math.Max(0, npc.PainExtra - PerTick(resting ? tu.PainRestFallPerDay : tu.PainFallPerDay, tpd));
            npc.Injury = Math.Max(0, npc.Injury - PerTick(tu.InjuryHealPerDay, tpd));
            npc.Drives[Drive.Pain] = Math.Max(npc.Injury, npc.PainExtra);

            int h = npc.DriveOf(Drive.Hunger);
            if (h >= tu.BandPain) npc.Nutrition -= PerTick(tu.NutritionLossPain, tpd);
            else if (h >= tu.BandHigh) npc.Nutrition -= PerTick(tu.NutritionLossHigh, tpd);
            else if (h < tu.ComfortHunger) npc.Nutrition += PerTick(tu.NutritionGainLow, tpd);
            npc.Nutrition = Math.Max(0, Math.Min(1000000, npc.Nutrition));

            int loss = 0; var cause = DeathCause.None;
            if (npc.Nutrition == 0) { loss = PerTick(tu.StarveLossAtZero, tpd); cause = DeathCause.Starvation; }
            else if (npc.Nutrition < tu.NutritionLowFrom * 1000) { loss = PerTick(tu.StarveLossLow, tpd); cause = DeathCause.Starvation; }
            if (!inHut && npc.DriveOf(Drive.Cold) >= tu.BandPain)
            {
                int ex = PerTick(tu.ExposureLossPerDay, tpd);
                if (ex > loss) cause = DeathCause.Exposure;
                loss += ex;
            }
            if (loss > 0) { npc.Health -= loss; npc.DeathCause = cause; }
            else if (npc.Nutrition > 500000) npc.Health = Math.Min(1000000, npc.Health + PerTick(tu.HealthRecoverPerDay, tpd));
            if (npc.Health > 0 && loss == 0) npc.DeathCause = DeathCause.None;
        }

        private static void Add(Npc npc, int d, int micro)
        {
            int v = npc.Drives[d] + micro;
            npc.Drives[d] = v < 0 ? 0 : v > 1000000 ? 1000000 : v;
        }

        public static int TimeOf(SimCalendar cal, long tick)
        {
            int pm = cal.DayPermille(tick);
            if (pm >= 750) return TimeOfDay.Night;
            if (pm >= 650) return TimeOfDay.Dusk;
            return TimeOfDay.Day;
        }

        /// <summary>Pays the Creatures drive rule (4.4) on every drive's change since the last settlement, into the reward collected for the next think.</summary>
        private void Settle(GodState s, Npc npc)
        {
            var tu = s.Tuning.Population;
            var lt = s.Tuning.Learning;
            int r = 0;
            for (int d = 0; d < Drive.Count; d++)
            {
                int prev = npc.RewardSnap[d], now = npc.Drives[d];
                if (prev == now) continue;
                r += Learner.DriveReward(lt.W(d), Thinker.Comfort(tu, d) * 1000, prev, now) / 1000;
                npc.RewardSnap[d] = now;
            }
            Learner.AddReward(npc.Mind.Store, r);
        }

        // ---------------- death ----------------

        private void Die(GodState s, EventBuffer ev, Npc npc, DeathCause cause)
        {
            var p = s.Population;
            npc.Alive = false; npc.DiedTick = s.Tick; npc.DeathCause = cause; npc.Health = 0;
            for (int i = 0; i < npc.Carry.Length; i++) { if (!npc.Carry[i].Empty) PopulationModule.Drop(s, npc.Node, npc.Carry[i]); npc.Carry[i] = default; }
            foreach (var hut in p.Huts)
            {
                for (int i = 0; i < hut.Rights.Length; i++) if (hut.Rights[i] == npc.Id) hut.Rights[i] = 0;
                hut.Contributors.Remove(npc.Id);
            }
            npc.HomeHut = 0; npc.SiteHut = 0; npc.Path = null; npc.Action = default;
            npc.Mind = null; // the dead keep their record, not their mind
            p.Stats.Deaths++;
            p.Stats.DeathsByCause[(int)cause]++;
            ev.Add(new NpcDied { Npc = npc.Id, Faction = npc.Faction, Cause = cause, Node = npc.Node, AgeTicks = s.Tick - npc.BornTick });
        }

        // ---------------- sleep ----------------

        private void StartSleep(GodState s, EventBuffer ev, Npc npc, int hut, bool collapsed, int day)
        {
            Settle(s, npc);
            var st = npc.Mind.Store;
            var tr = TraitsOf(s, npc);
            if (collapsed) Learner.EndOngoing(st); // nothing was chosen: no decision takes the night's relief
            _sink.Bind(ev, npc.Id, EventLearningSink.IsSampledDay(npc.Id, day, s.Tuning.Learning.SampleEvery));
            Learner.Consolidate(st, s.Tuning.Learning, tr, day, DecayScale(s), _recruit, _sink);
            // sleeping sets tools down beside the sleeper
            for (int i = 0; i < npc.Carry.Length; i++)
                if (!npc.Carry[i].Empty && npc.Carry[i].Ware == Ware.Axe) { PopulationModule.Drop(s, npc.Node, npc.Carry[i]); npc.Carry[i] = default; }
            npc.Path = null; npc.PathGoal = -1;
            npc.Action = new NpcAction { Verb = Verb.Sleep, TargetKind = hut != 0 ? IntentTarget.Hut : IntentTarget.Node, TargetId = hut, TargetNode = npc.Node, Asleep = true, SleepHut = hut, Since = s.Tick };
            s.Population.Stats.Sleeps++;
            ev.Add(new Slept { Npc = npc.Id, Node = npc.Node, Hut = hut, Collapsed = collapsed, Fatigue = npc.DriveOf(Drive.Fatigue) });
        }

        /// <summary>The nightly decay scale: <c>Learning.DecayScalePermille</c>, independent of LifeScale (Q1) unless the setup asks forgetting to follow it.</summary>
        public static int DecayScale(GodState s)
        {
            long v = s.Tuning.Learning.DecayScalePermille;
            if (s.Setup != null && s.Setup.DecayFollowsLifeScale) v = v * s.Setup.LifeScale / 100;
            return (int)Math.Max(0, Math.Min(100000, v));
        }

        private void Collapse(GodState s, EventBuffer ev, Npc npc)
        {
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            StartSleep(s, ev, npc, 0, true, day);
        }

        private void Wake(GodState s, EventBuffer ev, Npc npc)
        {
            Settle(s, npc);
            int hut = npc.Action.SleepHut;
            npc.Action = new NpcAction { Since = s.Tick };
            Learner.EndOngoing(npc.Mind.Store);
            ev.Add(new Slept { Npc = npc.Id, Node = npc.Node, Hut = hut, Woke = true, Fatigue = npc.DriveOf(Drive.Fatigue) });
        }

        // ---------------- senses (layer 2: what is in sight now) ----------------

        private int StockBand(ResourceSource src)
        {
            if (src.Stock <= 0) return ObjState.StockEmpty;
            return src.Stock * 4 >= src.Capacity ? ObjState.StockHigh : ObjState.StockLow;
        }

        private void Sense(GodState s, Npc npc, int sight)
        {
            var p = s.Population;
            var tu = s.Tuning.Population;
            int n = 0;
            int cell = _cellOf[npc.Node];
            int cx = cell / _cellsZ, cz = cell % _cellsZ;
            for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    int x = cx + dx, z = cz + dz;
                    if (x < 0 || z < 0 || x >= _cellsX || z >= _cellsZ) continue;
                    int c = x * _cellsZ + z;
                    foreach (int i in _srcCells[c])
                    {
                        var src = p.Sources[i];
                        int d = _g.Distance(npc.Node, src.Node);
                        if (d > sight || n >= _near.Length) continue;
                        _near[n++] = new Sensed
                        {
                            Class = (byte)PopulationModule.ClassOf(src.Kind), Category = (byte)(src.Kind == SourceKind.Wood ? ObjCategory.Raw : ObjCategory.Food),
                            State = (byte)StockBand(src), Kind = IntentTarget.Node, Node = src.Node, Dist = d,
                        };
                    }
                    foreach (int i in _hutCells[c])
                    {
                        var hut = p.Huts[i];
                        int d = _g.Distance(npc.Node, hut.Node);
                        if (d > sight || n >= _near.Length) continue;
                        bool mine = hut.Built ? hut.HasRight(npc.Id) : npc.SiteHut == hut.Id;
                        bool open = !hut.Built && hut.Faction == npc.Faction && hut.FreeBeds > hut.Contributors.Count;
                        _near[n++] = new Sensed
                        {
                            Class = (byte)(hut.Built ? ObjClass.Hut : ObjClass.HutSite), Category = ObjCategory.Shelter,
                            State = (byte)(hut.Built ? (mine ? ObjState.Mine : ObjState.Others) : ObjState.Unfinished),
                            Kind = IntentTarget.Hut, Node = hut.Node, Dist = d, Ref = mine || open ? hut.Id : 0,
                        };
                    }
                    foreach (int i in _lotCells[c])
                    {
                        var g = p.Lots[i];
                        int d = _g.Distance(npc.Node, g.Node);
                        if (d > sight || n >= _near.Length) continue;
                        int cls = g.Lot.Ware == Ware.Axe ? ObjClass.Axe : Wares.IsFood(g.Lot.Ware) ? ObjClass.FoodLot : g.Lot.Ware == Ware.Log ? ObjClass.LogLot : 0;
                        if (cls == 0) continue;
                        _near[n++] = new Sensed
                        {
                            Class = (byte)cls, Category = (byte)(cls == ObjClass.Axe ? ObjCategory.Tool : cls == ObjClass.FoodLot ? ObjCategory.Food : ObjCategory.Raw),
                            Kind = IntentTarget.Lot, Node = g.Node, Dist = d, Ref = g.Lot.Id,
                        };
                    }
                    if (s.Military != null && s.Factions != null)
                        foreach (int i in _unitCells[c])
                        {
                            var u = s.Military.Units[i];
                            if (!s.Factions.AtWar(npc.Faction, u.Faction)) continue;
                            long ddx = u.X - MmX(_g, npc.Node), ddz = u.Z - MmZ(_g, npc.Node);
                            long d2 = ddx * ddx + ddz * ddz, r = (long)sight * 5000;
                            if (d2 > r * r || n >= _near.Length) continue;
                            _near[n++] = new Sensed { Class = ObjClass.EnemySoldier, Category = ObjCategory.Danger, Kind = IntentTarget.Unit, Node = npc.Node, Dist = 0, Ref = u.Id };
                        }
                }
            // dice piles on hexes nearby, gathered by hand (ECONOMY 4.2, Q9)
            if (tu.HandGatherPiles)
            {
                var t = s.Territory;
                for (int hx = 0; hx < t.Piles.Length; hx++)
                {
                    int avail = t.Piles[hx] - (s.Economy != null ? s.Economy.PilesReserved[hx] : 0);
                    int units = p.OpenPileUnits[hx] + Math.Max(0, avail) * EconomyTuning.WaresPerPile;
                    if (units <= 0 || TerritoryState.PileKind(s.Board, hx) != RawKind.Log) continue;
                    int centre = _g.HexCentreNode(hx);
                    int d = _g.Distance(npc.Node, centre);
                    if (d > sight || n >= _near.Length) continue;
                    _near[n++] = new Sensed
                    {
                        Class = ObjClass.LogPile, Category = ObjCategory.Raw, State = (byte)(units >= 2 * EconomyTuning.WaresPerPile ? ObjState.StockHigh : ObjState.StockLow),
                        Kind = IntentTarget.Node, Node = centre, Dist = d,
                    };
                }
            }
            // the nearest PerceiveMax things (ties: the order found, which is fixed)
            int keep = Math.Min(n, Math.Min(tu.PerceiveMax, ThinkScratch.MaxSeen));
            for (int k = 0; k < keep; k++)
            {
                int best = k;
                for (int j = k + 1; j < n; j++) if (_near[j].Dist < _near[best].Dist) best = j;
                if (best != k) { var tmp = _near[k]; _near[k] = _near[best]; _near[best] = tmp; }
                _sc.Seen[k] = _near[k];
            }
            _sc.SeenCount = keep;
        }

        // ---------------- think ----------------

        private void FillSelf(GodState s, Npc npc, int day, int sight)
        {
            var tu = s.Tuning.Population;
            var p = s.Population;
            var self = new SelfView
            {
                Id = npc.Id, Hunger = npc.DriveOf(Drive.Hunger), Fatigue = npc.DriveOf(Drive.Fatigue), Cold = npc.DriveOf(Drive.Cold),
                Fear = npc.DriveOf(Drive.Fear), Pain = npc.DriveOf(Drive.Pain), Health = npc.HealthPermille, Nutrition = npc.NutritionPermille,
                Node = npc.Node, HomeNode = -1, SiteNode = -1, HomeHut = npc.HomeHut, SiteHut = npc.SiteHut, HasBed = npc.HomeHut != 0, HasSite = npc.SiteHut != 0,
                Injured = npc.Injury > 300000, Tick = (int)s.Tick, Day = day, TimeOfDay = TimeOf(SimCalendar.Of(s), s.Tick),
                RunVerb = npc.Action.Verb, RunKind = npc.Action.TargetKind,
                RunTarget = npc.Action.TargetKind == IntentTarget.Node ? npc.Action.TargetNode : npc.Action.TargetId,
                ExploreA = -1, ExploreB = -1,
            };
            for (int i = 0; i < npc.Carry.Length; i++)
            {
                var l = npc.Carry[i];
                if (l.Empty) continue;
                if (Wares.IsFood(l.Ware))
                {
                    self.CarryFood = true;
                    if (self.FoodLot0 == 0) { self.FoodLot0 = l.Id; self.FoodWare0 = l.Ware; }
                    else if (self.FoodLot1 == 0 && l.Ware != self.FoodWare0) { self.FoodLot1 = l.Id; self.FoodWare1 = l.Ware; }
                }
                if (l.Ware == Ware.Log) self.CarryLog = true;
                if (l.Ware == Ware.Axe) self.CarryTool = true;
            }
            self.CarryFull = PopulationModule.Room(s, npc, Ware.Berries) == 0;
            self.CanCarryLog = PopulationModule.Room(s, npc, Ware.Log) > 0;
            self.CanCarryTool = PopulationModule.Room(s, npc, Ware.Axe) > 0;
            if (npc.HomeHut != 0) { var h = p.HutById(npc.HomeHut); if (h != null) self.HomeNode = h.Node; }
            if (npc.SiteHut != 0) { var h = p.HutById(npc.SiteHut); if (h != null) self.SiteNode = h.Node; }
            self.AtHome = self.HomeNode >= 0 && _g.Distance(npc.Node, self.HomeNode) <= tu.HomeRadius;
            self.OnRoad = s.Territory.NodeRoad[npc.Node] != 0;
            self.PlaceKind = _coast[npc.Node] ? PlaceKind.Coast : PlaceOfTerrain(s, _g.HexOf(npc.Node));
            self.CanFoundHere = CanFound(s, npc, npc.Node);
            int dir = (npc.Mind.ThinkCount + npc.Id) % 6;
            self.ExploreA = Walk(npc.Node, dir, tu.ExploreNodes);
            self.ExploreB = Walk(npc.Node, (dir + 3) % 6, tu.ExploreNodes);
            _sc.Self = self;
        }

        private int Walk(int node, int dir, int steps)
        {
            int cur = node;
            for (int k = 0; k < steps; k++)
            {
                int v = _g.Neighbour(cur, dir);
                if (v < 0 || !_g.IsLand(v)) break;
                cur = v;
            }
            return cur == node ? -1 : cur;
        }

        private static int PlaceOfTerrain(GodState s, int hex)
        {
            if (hex < 0) return PlaceKind.Coast;
            switch (s.Board.HexTerrain[hex])
            {
                case Terrain.Forest: return PlaceKind.Forest;
                case Terrain.Hills: return PlaceKind.Hills;
                case Terrain.Fields: return PlaceKind.Fields;
                case Terrain.Mountain: return PlaceKind.Mountain;
                case Terrain.Pasture: return PlaceKind.Pasture;
                case Terrain.Desert: return PlaceKind.Desert;
                default: return PlaceKind.Meadow;
            }
        }

        /// <summary>A hut may stand on a free land node outside every HQ's build circle, away from other huts and sources.</summary>
        public bool CanFound(GodState s, Npc npc, int node)
        {
            var t = s.Territory;
            var tu = s.Tuning.Population;
            if (!_g.IsLand(node) || t.NodeBuilding[node] != 0 || t.NodeFlag[node] != 0 || t.NodeRoad[node] != 0) return false;
            foreach (var hq in t.Hqs) if (_g.InCircle(hq.Node, node)) return false;
            foreach (var h in s.Population.Huts) if (_g.Distance(h.Node, node) < tu.HutSpacing) return false;
            foreach (var src in s.Population.Sources) if (src.Node == node) return false;
            return true;
        }

        private void Think(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var p = s.Population;
            var tu = s.Tuning.Population;
            var lt = s.Tuning.Learning;
            var mind = npc.Mind;
            var st = mind.Store;
            var tr = TraitsOf(s, npc);
            Thinks++;
            npc.LastThinkTick = s.Tick;
            mind.ThinkCount++;
            bool sampled = EventLearningSink.IsSampledDay(npc.Id, day, lt.SampleEvery);
            _sink.Bind(ev, npc.Id, sampled);

            var prev = mind.Last; // the decision the reward since then belongs to (its experience)
            // 2.1-2.2: reward since the last think, credit, close
            Settle(s, npc);
            Learner.BeginThink(st, lt, tr, day, _sink);

            // senses and fear
            int sight = TimeOf(SimCalendar.Of(s), s.Tick) == TimeOfDay.Night ? tu.SightNight : tu.SightDay;
            IndexUnits(s);
            Sense(s, npc, sight);
            for (int i = 0; i < _sc.SeenCount; i++)
                if (_sc.Seen[i].Category == ObjCategory.Danger) { Add(npc, Drive.Fear, tu.FearSeenEnemy * 1000); break; }

            // cognition (pure)
            FillSelf(s, npc, day, sight);
            _sc.Mind = mind;
            _sc.Instincts = InstinctTable.Of(npc.Instincts);
            _sc.Traits = npc.Traits;
            _sc.Learn = tr;
            _sc.LearningTuning = lt;
            _sc.Tuning = tu;
            _sc.Grid = _g;
            Thinker.Think(_sc);
            Inspect?.Invoke(npc.Id, _sc);

            // decide: the policy sees only the observation and its own mind
            var obs = MindViews.Observe(_sc, npc.Id, s.Tick);
            var view = MindViews.Of(_sc);
            var policy = PolicyOf(npc.Policy);
            var rng = npc.RngPolicy;
            NpcIntent intent;
            var why = default(DecisionRecord);
            if (policy is AssociativeNpcPolicy ap) intent = ap.DecideExplained(obs, view, ref rng, ref why, mind.Opportunities, out mind.OpportunityCount);
            else { intent = policy.Decide(obs, view, ref rng); mind.OpportunityCount = 0; }
            IntentRules.Require(PolicyLevel.Civilian, policy.Name, intent);
            p.Stats.Decisions++;
            mind.Decisions++;
            if (intent.Verb == NpcVerb.None) { Experience(mind, st, s.Tick, prev); Learner.Step(st, lt, tr, 0, day, _sink); EndAction(npc); Learner.DecayTraces(st, Learner.TraceShiftOf(lt, tr)); return; }

            // the intent must be one of the candidates (perception only, M3.12)
            int ci = Match(intent);
            if (tu.CheckPerception)
            {
                p.Stats.PerceptionChecks++;
                if (!Thinker.Known(_sc, intent.Target, intent.TargetId, intent.X)) p.Stats.NotPerceived++;
            }
            if (ci < 0)
            {
                p.Stats.IntentsRefused++;
                Experience(mind, st, s.Tick, prev);
                Learner.Step(st, lt, tr, 0, day, _sink);
                Learner.AddReward(st, lt.Frustration);
                EndAction(npc);
                Learner.DecayTraces(st, Learner.TraceShiftOf(lt, tr));
                return;
            }
            var cand = _sc.Cand[ci];
            if (why.Candidates == 0)
            {
                why.Tick = (int)s.Tick; why.Verb = cand.Verb; why.TargetKind = cand.Kind; why.TargetId = cand.TargetId; why.TargetNode = cand.TargetNode;
                why.TargetClass = cand.Class; why.Candidates = _sc.CandCount;
            }
            mind.Last = why;
            mind.DayDecisions++;
            if (why.LearnedChangedWinner) mind.DayLearnedChanged++;
            if (why.ExploreWon) mind.DayExploreWon++;

            // Re-choosing the running intent while still on the way to its target continues the same decision (NPC_LEARNING
            // 4.5 "ongoing": a long walk keeps its trace at full eligibility until the intent ends); otherwise it is a new
            // decision: credit the decisions before it with this one's value (TD), act on it, then open its trace (Build
            // carries its hut as the episode key).
            bool travelling = cand.IsRunning && npc.Path != null && npc.PathIdx < npc.Path.Length;
            if (travelling) { Start(s, ev, npc, cand, day); return; }
            Experience(mind, st, s.Tick, prev);
            Learner.Step(st, lt, tr, NextValue(st, lt, cand, why), day, _sink);
            Start(s, ev, npc, cand, day);
            int q = view.Terms(ci).Learned;
            int episodeKey = cand.Verb == Verb.Build ? (npc.SiteHut != 0 ? npc.SiteHut : cand.TargetId) : 0;
            Learner.OpenTrace(st, lt, tr, (int)s.Tick, cand.Verb, _sc.Keys, cand.KeyOffset, cand.KeyCount, q, episodeKey, day, _sink);
            Learner.DecayTraces(st, Learner.TraceShiftOf(lt, tr));
        }

        /// <summary>The reward the previous decision earned (consumed by the next TD step), remembered as an experience when not zero.</summary>
        private static void Experience(NpcMind mind, AssociationStore st, long tick, in DecisionRecord prev)
        {
            if (st.RewardAcc != 0 && prev.Verb != 0) mind.Remember((int)tick, prev.Verb, prev.TargetClass, st.RewardAcc, false);
        }

        /// <summary>
        /// The value the TD error bootstraps from (NPC_LEARNING 4.6, M3): the best learned value among this think's candidates
        /// (<see cref="LearningTuning.BootstrapBest"/>, default) or the chosen candidate's (SARSA). Raw store units.
        /// </summary>
        private int NextValue(AssociationStore st, LearningTuning lt, in Candidate cand, in DecisionRecord why)
        {
            if (st.Mode == LearningMode.Off || lt.GammaPermille == 0) return 0;
            if (lt.BootstrapBest)
            {
                if (why.Candidates > 0) return why.MaxLearned;
                int best = int.MinValue;
                for (int i = 0; i < _sc.CandCount; i++) best = Math.Max(best, st.SumStw(_sc.Keys, _sc.Cand[i].KeyOffset, _sc.Cand[i].KeyCount, _sc.Cand[i].Verb, out _));
                return best == int.MinValue ? 0 : best;
            }
            return st.SumStw(_sc.Keys, cand.KeyOffset, cand.KeyCount, cand.Verb, out _);
        }

        private int Match(in NpcIntent intent)
        {
            for (int i = 0; i < _sc.CandCount; i++)
            {
                var c = _sc.Cand[i];
                if (c.Intent != intent.Verb || c.Kind != intent.Target) continue;
                if (intent.Detail != 0 && intent.Detail != c.Verb) continue;
                int id = c.Kind == IntentTarget.Node ? c.TargetNode : c.TargetId;
                if (id == intent.TargetId) return i;
            }
            return -1;
        }

        // ---------------- actions ----------------

        private static void EndAction(Npc npc)
        {
            npc.Action = new NpcAction { Since = npc.Action.Since };
            npc.Path = null; npc.PathGoal = -1;
            if (npc.Mind != null) Learner.EndOngoing(npc.Mind.Store);
        }

        private void Frustrate(GodState s, Npc npc)
        {
            Learner.AddReward(npc.Mind.Store, s.Tuning.Learning.Frustration);
            EndAction(npc);
        }

        private void Start(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day)
        {
            var a = npc.Action;
            bool same = a.Verb == c.Verb && a.TargetKind == c.Kind && (c.Kind == IntentTarget.Node ? a.TargetNode == c.TargetNode : a.TargetId == c.TargetId);
            if (!same)
            {
                npc.Action = new NpcAction { Verb = c.Verb, TargetKind = c.Kind, TargetId = c.TargetId, TargetNode = c.TargetNode, Since = s.Tick };
                if (npc.PathGoal != c.TargetNode) { npc.Path = null; npc.PathGoal = -1; }
            }
            if (c.Verb == Verb.Build && c.TargetId == 0 && c.Kind == IntentTarget.Node)
            {
                // found a hut site here
                if (npc.HomeHut != 0 || npc.SiteHut != 0 || !CanFound(s, npc, npc.Node)) { Frustrate(s, npc); return; }
                var hut = PopulationModule.AddHut(s, npc.Faction, npc.Node, false);
                hut.FoundedBy = npc.Id;
                hut.Contributors.Add(npc.Id);
                npc.SiteHut = hut.Id;
                ev.Add(new HutSiteFounded { Hut = hut.Id, Npc = npc.Id, Faction = npc.Faction, Node = npc.Node });
                npc.Action = new NpcAction { Verb = Verb.Build, TargetKind = IntentTarget.Hut, TargetId = hut.Id, TargetNode = hut.Node, Since = s.Tick };
            }
            else if (c.Verb == Verb.Sleep && c.Kind == IntentTarget.Node)
            {
                StartSleep(s, ev, npc, 0, false, day);
            }
            else if (c.Verb == Verb.Eat && c.Kind == IntentTarget.Lot)
            {
                if (npc.DriveOf(Drive.Hunger) < s.Tuning.Population.SatedBelow) { Frustrate(s, npc); return; }
                npc.Action.BusyUntil = s.Tick + s.Tuning.Population.EatTicks;
            }
        }

        private int ReachOf(byte verb, IntentTarget kind) => kind == IntentTarget.Lot ? 1 : verb == Verb.Gather ? 2 : 1;

        /// <summary>Walks one tick along the path to a node. Returns true when within <paramref name="reach"/> of it.</summary>
        private bool WalkTo(GodState s, Npc npc, int goal, int reach)
        {
            if (_g.Distance(npc.Node, goal) <= reach) { npc.Path = null; npc.PathGoal = -1; npc.StepTicks = 0; return true; }
            var tu = s.Tuning.Population;
            if (npc.Path == null || npc.PathGoal != goal)
            {
                npc.Path = _pather.Find(npc.Node, goal, s.Territory.NodeRoad, tu.RoadStepTicks, tu.StepTicks, tu.PathMaxExpand);
                npc.PathIdx = 0; npc.PathGoal = goal; npc.StepTicks = 0;
                if (npc.Path == null || npc.Path.Length == 0) { npc.Path = null; return false; }
            }
            if (npc.PathIdx >= npc.Path.Length) { npc.Path = null; npc.PathGoal = -1; return _g.Distance(npc.Node, goal) <= reach; }
            int next = npc.Path[npc.PathIdx];
            int cost = s.Territory.NodeRoad[next] != 0 ? tu.RoadStepTicks : tu.StepTicks;
            if (++npc.StepTicks >= cost)
            {
                npc.StepTicks = 0;
                npc.Node = next;
                npc.PathIdx++;
            }
            return _g.Distance(npc.Node, goal) <= reach;
        }

        private void Act(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var a = npc.Action;
            if (a.Verb == 0 || a.Verb == Verb.Rest) return;
            var tu = s.Tuning.Population;
            var p = s.Population;
            switch (a.Verb)
            {
                case Verb.Explore:
                    if (WalkTo(s, npc, a.TargetNode, 0) || npc.Path == null) EndAction(npc);
                    return;
                case Verb.Flee:
                {
                    if (a.TargetNode == npc.Node || a.Progress == 0)
                    {
                        // run directly away from where the danger was seen: the opposite of the explore direction it last faced
                        int target = Walk(npc.Node, (npc.Mind.ThinkCount + 3) % 6, 6);
                        npc.Action.TargetNode = target < 0 ? npc.Node : target;
                        npc.Action.Progress = 1;
                    }
                    if (WalkTo(s, npc, npc.Action.TargetNode, 0)) EndAction(npc);
                    return;
                }
                case Verb.Sleep:
                {
                    var hut = p.HutById(a.TargetId);
                    if (hut == null || !hut.Built || !hut.HasRight(npc.Id)) { Frustrate(s, npc); return; }
                    if (WalkTo(s, npc, hut.Node, 0)) StartSleep(s, ev, npc, hut.Id, false, day);
                    return;
                }
                case Verb.Eat:
                {
                    if (a.TargetKind != IntentTarget.Node) return; // eating from carry is a busy action
                    if (!WalkTo(s, npc, a.TargetNode, 1)) return;
                    var src = SourceAt(p, a.TargetNode);
                    if (src == null || src.Stock <= 0) { NoteEmpty(npc, a.TargetNode); Frustrate(s, npc); return; }
                    if (npc.DriveOf(Drive.Hunger) < tu.SatedBelow) { Frustrate(s, npc); return; }
                    npc.Action.BusyUntil = s.Tick + tu.ForageTicks; // pick and eat on the spot
                    return;
                }
                case Verb.ForageAt:
                {
                    if (!WalkTo(s, npc, a.TargetNode, 1)) return;
                    var src = SourceAt(p, a.TargetNode);
                    if (src == null || src.Stock <= 0) { NoteEmpty(npc, a.TargetNode); Frustrate(s, npc); return; }
                    var ware = PopulationModule.WareOf(src.Kind);
                    if (PopulationModule.Room(s, npc, ware) <= 0) { EndAction(npc); return; }
                    if (++npc.Action.Progress < tu.ForageTicks) return;
                    npc.Action.Progress = 0;
                    int yield = 1 + npc.RngWork.NextInt(2);
                    yield = Math.Min(yield, Math.Min(src.Stock, PopulationModule.Room(s, npc, ware)));
                    src.Stock -= yield;
                    var lot = PopulationModule.NewLot(s, ware, yield, day + PopulationModule.SpoilDays(s.Tuning.Ecology, ware), 0);
                    if (!PopulationModule.PutInCarry(s, npc, lot)) throw new InvalidOperationException("forage: no room after the room check");
                    p.Ledger.Produce(ProduceReason.Foraged, ware, yield);
                    p.Stats.Foraged += yield;
                    ev.Add(new Produced { Npc = npc.Id, Source = src.Id, Ware = ware, Count = yield, Reason = ProduceReason.Foraged });
                    ev.Add(new Foraged { Npc = npc.Id, Source = src.Id, Ware = ware, Count = yield, StockAfter = src.Stock });
                    Learner.AddReward(npc.Mind.Store, Worth(s, npc, ware) * yield);
                    if (PopulationModule.Room(s, npc, ware) <= 0 || src.Stock <= 0) EndAction(npc);
                    return;
                }
                case Verb.Gather:
                {
                    if (!WalkTo(s, npc, a.TargetNode, ReachOf(a.Verb, a.TargetKind))) return;
                    var src = SourceAt(p, a.TargetNode);
                    int hex = _g.HexOf(a.TargetNode);
                    bool pile = src == null && hex >= 0 && _g.HexCentreNode(hex) == a.TargetNode;
                    if (src == null && !pile || src != null && (src.Kind != SourceKind.Wood || src.Stock <= 0) || pile && PileUnits(s, hex) <= 0)
                    { NoteEmpty(npc, a.TargetNode); Frustrate(s, npc); return; }
                    if (PopulationModule.Room(s, npc, Ware.Log) <= 0) { EndAction(npc); return; }
                    int axe = AxeSlot(npc);
                    npc.Action.Progress += axe >= 0 ? tu.AxeSpeedPermille : 1000;
                    if (npc.Action.Progress < tu.GatherTicks * 1000) return;
                    npc.Action.Progress -= tu.GatherTicks * 1000;
                    if (pile) TakePileUnit(s, hex); else src.Stock--;
                    var lot = PopulationModule.NewLot(s, Ware.Log, 1, -1, 0);
                    if (!PopulationModule.PutInCarry(s, npc, lot)) throw new InvalidOperationException("gather: no room after the room check");
                    p.Ledger.Produce(ProduceReason.HandGathered, Ware.Log, 1);
                    p.Stats.LogsGathered++;
                    ev.Add(new Produced { Npc = npc.Id, Source = src?.Id ?? 0, Ware = Ware.Log, Count = 1, Reason = ProduceReason.HandGathered, WithTool = axe >= 0 });
                    Learner.AddReward(npc.Mind.Store, Worth(s, npc, Ware.Log));
                    if (axe >= 0 && --npc.Carry[axe].Uses <= 0)
                    {
                        p.Ledger.Consume(ConsumeReason.Wear, Ware.Axe, 1);
                        ev.Add(new Consumed { Npc = npc.Id, Ware = Ware.Axe, Count = 1, Reason = ConsumeReason.Wear });
                        npc.Carry[axe] = default;
                    }
                    if (PopulationModule.Room(s, npc, Ware.Log) <= 0) { npc.Action.Progress = 0; EndAction(npc); }
                    return;
                }
                case Verb.PickUp:
                case Verb.Equip:
                {
                    int li = LotIndex(p, a.TargetId);
                    if (li < 0) { Frustrate(s, npc); return; }
                    if (!WalkTo(s, npc, p.Lots[li].Node, 1)) return;
                    li = LotIndex(p, a.TargetId);
                    var g = p.Lots[li];
                    int take = Math.Min(g.Lot.Count, PopulationModule.Room(s, npc, g.Lot.Ware));
                    if (take <= 0) { Frustrate(s, npc); return; }
                    var lot = g.Lot; lot.Count = take;
                    if (take < g.Lot.Count) { g.Lot.Count -= take; lot.Id = p.NextLotId++; }
                    else { p.Lots.RemoveAt(li); }
                    p.IndexDirty = true;
                    if (!PopulationModule.PutInCarry(s, npc, lot)) throw new InvalidOperationException("pick up: no room after the room check");
                    Learner.AddReward(npc.Mind.Store, Worth(s, npc, lot.Ware) * take);
                    EndAction(npc);
                    return;
                }
                case Verb.Build:
                {
                    var hut = p.HutById(a.TargetId);
                    if (hut == null || hut.Built) { Frustrate(s, npc); return; }
                    if (!WalkTo(s, npc, hut.Node, 1)) return;
                    if (hut.LogsIn < hut.LogsNeeded)
                    {
                        int slot = -1;
                        for (int i = 0; i < npc.Carry.Length; i++) if (!npc.Carry[i].Empty && npc.Carry[i].Ware == Ware.Log) { slot = i; break; }
                        if (slot < 0) { Frustrate(s, npc); return; }
                        if (--npc.Carry[slot].Count <= 0) npc.Carry[slot] = default;
                        hut.LogsIn++;
                        p.Ledger.Consume(ConsumeReason.Built, Ware.Log, 1);
                        ev.Add(new Consumed { Npc = npc.Id, Hut = hut.Id, Ware = Ware.Log, Count = 1, Reason = ConsumeReason.Built });
                        Contribute(hut, npc);
                        EndAction(npc);
                        return;
                    }
                    npc.Action.Progress = Math.Max(npc.Action.Progress, 1);
                    Contribute(hut, npc);
                    if (++hut.Work >= hut.WorkNeeded) FinishHut(s, ev, hut, day);
                    return;
                }
            }
        }

        private static void Contribute(Hut hut, Npc npc)
        {
            if (!hut.Contributors.Contains(npc.Id) && hut.Contributors.Count < 8) hut.Contributors.Add(npc.Id);
        }

        private void FinishHut(GodState s, EventBuffer ev, Hut hut, int day)
        {
            var p = s.Population;
            hut.Built = true;
            hut.BuiltTick = s.Tick;
            int bed = 0;
            foreach (int id in hut.Contributors)
            {
                var npc = p.Find(id);
                if (npc == null || !npc.Alive || npc.HomeHut != 0 || bed >= hut.Rights.Length) continue;
                hut.Rights[bed++] = id;
                npc.HomeHut = hut.Id;
                if (npc.SiteHut == hut.Id) npc.SiteHut = 0;
                // the bed right (an opportunity, 4.4): it closes the Build episodes that waited for it
                _sink.Bind(ev, npc.Id, EventLearningSink.IsSampledDay(npc.Id, day, s.Tuning.Learning.SampleEvery));
                Learner.Outcome(npc.Mind.Store, s.Tuning.Learning, TraitsOf(s, npc), hut.Id, s.Tuning.Learning.BedGained, day, _sink);
                npc.Mind.Remember((int)s.Tick, Verb.Build, ObjClass.Hut, s.Tuning.Learning.BedGained, true);
                if (npc.Action.Verb == Verb.Build && npc.Action.TargetId == hut.Id) EndAction(npc);
            }
            hut.Contributors.Clear();
            foreach (var npc in p.Npcs) if (npc.Alive && npc.SiteHut == hut.Id) npc.SiteHut = 0;
            p.Stats.HutsBuilt++;
            p.IndexDirty = true;
            ev.Add(new HutBuilt { Hut = hut.Id, Faction = hut.Faction, Node = hut.Node, Rights = (int[])hut.Rights.Clone() });
        }

        private void FinishBusy(GodState s, EventBuffer ev, Npc npc)
        {
            var a = npc.Action;
            npc.Action.BusyUntil = 0;
            var p = s.Population;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            if (a.Verb == Verb.Eat && a.TargetKind == IntentTarget.Lot)
            {
                int slot = -1;
                for (int i = 0; i < npc.Carry.Length; i++) if (npc.Carry[i].Id == a.TargetId && !npc.Carry[i].Empty) { slot = i; break; }
                if (slot < 0) { Frustrate(s, npc); return; }
                var ware = npc.Carry[slot].Ware;
                if (--npc.Carry[slot].Count <= 0) npc.Carry[slot] = default;
                Eat(s, ev, npc, ware, true, day);
                EndAction(npc);
                return;
            }
            if (a.Verb == Verb.Eat && a.TargetKind == IntentTarget.Node)
            {
                var src = SourceAt(p, a.TargetNode);
                if (src == null || src.Stock <= 0) { Frustrate(s, npc); return; }
                var ware = PopulationModule.WareOf(src.Kind);
                src.Stock--;
                p.Ledger.Produce(ProduceReason.Foraged, ware, 1);
                p.Stats.Foraged++;
                ev.Add(new Produced { Npc = npc.Id, Source = src.Id, Ware = ware, Count = 1, Reason = ProduceReason.Foraged });
                ev.Add(new Foraged { Npc = npc.Id, Source = src.Id, Ware = ware, Count = 1, StockAfter = src.Stock });
                Eat(s, ev, npc, ware, false, day);
                EndAction(npc);
            }
        }

        /// <summary>Eats one unit: hunger falls by its nutrition under the variety rule (the n-th unit of a food in a day relieves <c>1000 / (1000 + 250 (n - 1))</c> of the first; ECONOMY A4); nightshade hurts.</summary>
        private void Eat(GodState s, EventBuffer ev, Npc npc, Ware ware, bool fromCarry, int day)
        {
            var p = s.Population;
            if (npc.EatenDay != day) { Array.Clear(npc.EatenToday, 0, npc.EatenToday.Length); npc.EatenDay = day; }
            int k = ware == Ware.Berries ? 0 : ware == Ware.Roots ? 1 : ware == Ware.Fish ? 2 : 3;
            int nth = ++npc.EatenToday[k];
            int relief = VarietyRelief(PopulationModule.Nutrition(s.Tuning.Ecology, ware), nth);
            Settle(s, npc);
            Add(npc, Drive.Hunger, -relief * 1000);
            if (ware == Ware.Nightshade)
            {
                npc.PainExtra = Math.Min(1000000, npc.PainExtra + s.Tuning.Population.NightshadePain * 1000);
                npc.Drives[Drive.Pain] = Math.Max(npc.Injury, npc.PainExtra);
            }
            Settle(s, npc);
            p.Ledger.Consume(ConsumeReason.Eaten, ware, 1);
            p.Stats.Meals++;
            npc.DayMeals++;
            ev.Add(new Consumed { Npc = npc.Id, Ware = ware, Count = 1, Reason = ConsumeReason.Eaten });
            ev.Add(new Ate { Npc = npc.Id, Ware = ware, Node = npc.Node, FromCarry = fromCarry, Relief = relief, HungerAfter = npc.DriveOf(Drive.Hunger) });
        }

        public static int VarietyRelief(int nutrition, int nth) => nutrition * 1000 / (1000 + 250 * (nth - 1));

        /// <summary>The NPC's own worth of a ware gained or lost (the M3 goods channel): food a little, logs a lot while it needs a hut, tools some.</summary>
        public static int Worth(GodState s, Npc npc, Ware w)
        {
            var tu = s.Tuning.Population;
            if (Wares.IsFood(w)) return tu.FoodWorth;
            if (w == Ware.Log) return npc.HomeHut == 0 ? tu.LogWorthNeeded : tu.LogWorthHoused;
            if (w == Ware.Axe) return tu.ToolWorth;
            return 0;
        }

        private static ResourceSource SourceAt(PopulationState p, int node)
        {
            foreach (var src in p.Sources) if (src.Node == node) return src;
            return null;
        }

        private static int LotIndex(PopulationState p, int id)
        {
            for (int i = 0; i < p.Lots.Count; i++) if (p.Lots[i].Lot.Id == id) return i;
            return -1;
        }

        private static int AxeSlot(Npc npc)
        {
            for (int i = 0; i < npc.Carry.Length; i++) if (!npc.Carry[i].Empty && npc.Carry[i].Ware == Ware.Axe) return i;
            return -1;
        }

        private static int PileUnits(GodState s, int hex)
        {
            int avail = s.Territory.Piles[hex] - (s.Economy != null ? s.Economy.PilesReserved[hex] : 0);
            return s.Population.OpenPileUnits[hex] + Math.Max(0, avail) * EconomyTuning.WaresPerPile;
        }

        /// <summary>Takes one log from a dice pile by hand: an opened pile first; else a whole unreserved pile is opened (4 units).</summary>
        private static void TakePileUnit(GodState s, int hex)
        {
            var p = s.Population;
            if (p.OpenPileUnits[hex] > 0) { p.OpenPileUnits[hex]--; return; }
            s.Territory.Piles[hex]--;
            p.OpenPileUnits[hex] = EconomyTuning.WaresPerPile - 1;
        }

        private static void NoteEmpty(Npc npc, int node)
        {
            var b = npc.Mind.Beliefs;
            for (int i = 0; i < b.Count; i++)
                if (b.Slots[i].Subject == node && (b.Slots[i].Kind == BeliefKind.Food || b.Slots[i].Kind == BeliefKind.Wood || b.Slots[i].Kind == BeliefKind.Pile))
                    b.Slots[i].Stock = ObjState.StockEmpty;
        }
    }
}
