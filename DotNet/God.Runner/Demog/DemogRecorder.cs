using System;
using System.Collections.Generic;
using God.Recording;
using God.Sim;
using God.Sim.Population;
using God.Sim.Population.Households;

namespace God.Runner.Demog
{
    /// <summary>What the report knows of one person (analytics truth: the runner may read sex and parents; no NPC does).</summary>
    public sealed class DemogPerson
    {
        public int Id, Mother, Father, Generation, Faction;
        public byte Sex;
        public bool Founder, Born;
        public long BornTick, DeathTick = -1;
        public DeathCause Cause;
        /// <summary>Age at entry (founders: the age they started at; the born: 0), milli-years.</summary>
        public int EntryAgeMilli;
        public int FertileEndMilli;
        public int Births;
        public long MaturedTick = -1;
        // D4 preferences (read at the end) and what the person did (M4.40's paired comparison)
        public int ChildDesire = -1, Companionship = -1, Commitment = -1;
        public long LastWantTick = -1;
        public int Tries, Wants, PartnershipsFormed, UnionsFormed, UnionsOutside, AdultDays, PartneredDays;
        // household store use (free-riding): food units it put in and took out
        public long Deposited, Withdrawn;
        public int FedChildren, FedOwnChildren;
        /// <summary>Born to a biological father who was not the mother's romantic partner at the birth.</summary>
        public bool FatherOutside;
    }

    /// <summary>A shared-care arrangement as the report saw it (M4.44 free-riding).</summary>
    public sealed class DemogArrangement
    {
        public int Id, Asker, Carer, Given, Returned, EndedBy;
        public long MadeTick, EndedTick = -1;
        public byte Reason;
    }

    /// <summary>One sim year of the report (index = calendar year).</summary>
    public sealed class DemogYear
    {
        public int Year;
        // census at the year's start (day 10 y, before that year's events) and at its end
        public int PopStart, PopEnd;
        public int[] AgeBins = new int[DemogRecorder.AgeBinCount];
        public int Children, Adults, Elders;
        public int Births, Deaths, Conceptions, Miscarriages;
        public int[] DeathsByCause = new int[9];
        public int[] DeathsByAgeBand = new int[DemogRecorder.AgeBands.Length];
        public long[] PersonDaysByAgeBand = new long[DemogRecorder.AgeBands.Length];
        public long PersonDays;
        // households (from the HouseholdDay snapshots of the census day)
        public int Households, HouseholdMembers, HouseholdsWithChildren, HouseholdsWithElders;
        public int[] Composition = new int[DemogRecorder.CompositionNames.Length];
        public int MaxHouseholdSize;
        // relationships (census day)
        public int RomanticPairs, MixedPairs, SameSexPairs, Unions, UnionsOutsidePartnership;
        // reproductive opportunity: fertile woman-days by status
        public long[] WomanDays = new long[DemogRecorder.WomanStatusNames.Length];
        /// <summary>F2: the mother's daily conception checks of biologically able women (fertile, not pregnant, past the after-birth time), by coordination state (<see cref="DemogRecorder.CheckNames"/>).</summary>
        public long[] Checks = new long[DemogRecorder.CheckNames.Length];
        /// <summary>F2: of the checks with a possible father who was not trying with her both ways, those where the two stood within <see cref="DemogRecorder.NearNodes"/> nodes.</summary>
        public long ChecksNotBothNear;
        /// <summary>F2: sum of the daily conception chance (permille) over the "both trying" checks (the biology's share).</summary>
        public long BothChanceSum;
        /// <summary>F2: "both trying" checks where no reproductive relationship existed or could be formed (one held another; or not partners).</summary>
        public long BothNoUnion;
        /// <summary>F2: of <see cref="BothNoUnion"/>, those where every union in the way was dormant (its other adult not trying in it, no pregnancy by it).</summary>
        public long BothNoUnionDormant;
        /// <summary>F2: the "no possible father" checks split: single; partnered with women only and free to seek a child outside (every commitment below <see cref="Preferences.ExclusiveFrom"/>); partnered with women only and held by a commitment.</summary>
        public long[] NoFather = new long[3];
        /// <summary>F2: of the "no possible father" checks, those where she had shown that she wants a child within the last day.</summary>
        public long NoFatherSought;
        // care
        public long CareTicks;
        public int UnionsFormed, UnionsOutsideFormed;
        /// <summary>Child feedings by the feeder's believed kin band (0 none, 1 parent-child, 2 sibling, 3 other kin, 4 partner).</summary>
        public int[] ChildFedByBand = new int[8];
        public int ChildFedActs, ChildFedUnits, ChildFedByParent, ChildFedByNonParent, Tended, ArrangementsMade, ArrangementsEnded, Fostered, Orphaned;
        // food flows over the year (units)
        public long FoodForaged, FoodProducedOther, FoodEaten, FoodSpoiled, FoodBuried, FoodOtherOut, Regeneration;
        public long ForageTicks, WalkTicks;
        public int SourcesForaged;
        // food stocks at the census day
        public long WildFoodStock, WildFoodCapacity, WildFoodMsyPerDay, ReachableWildFood, StoreFood, CarriedFood, GroundFood;
        public int FoodSources;
        public int FactionsAlive;
        // starvation deaths: by age band, and the standing wild food near where each died (local scarcity or not)
        public int[] StarveByAgeBand = new int[DemogRecorder.AgeBands.Length];
        public long StarveFoodWithin5, StarveFoodWithin10, StarveCapacityWithin10;
        public int StarveDeathsNoFoodWithin10;
        /// <summary>Distance (nodes) from each starvation death to the nearest wild source holding at least 5 units: sum, and counts within 10, 11-20, 21-40, beyond 40.</summary>
        public long StarveNearestSum;
        public int[] StarveNearestHist = new int[4];
        // nutrition at the census (permille means)
        public int MeanNutritionAdults, MeanNutritionChildren;
        public int MaxGenerationAlive;
    }

    /// <summary>
    /// The demographic report's recorder (M4.45): reads the event stream and, at day ends, the state (both read-only), and
    /// keeps everything the report needs per run. It never writes the sim. One per run (built through
    /// <see cref="RunSpec.ExtraRecorders"/>), so it runs beside the warehouse recorder in the reconciliation test.
    /// </summary>
    public sealed class DemogRecorder : IRunRecorder
    {
        public const int AgeBinCount = 17; // 0.25-year bins, the last 4.0+
        /// <summary>Age bands of mortality (milli-years, lower bounds).</summary>
        public static readonly int[] AgeBands = { 0, 250, 500, 1000, 2000, 3000 };
        public static readonly string[] AgeBandNames = { "0-0.25", "0.25-0.5", "0.5-1.0", "1.0-2.0", "2.0-3.0", "3.0+" };
        public static readonly string[] CompositionNames = { "lone adult", "lone adult + children", "two adults", "two adults + children", "3+ adults", "3+ adults + children", "children only" };
        public static readonly string[] WomanStatusNames = { "pregnant", "after birth", "union (trying possible)", "partnered with a man, no union", "partnered with a woman only", "single" };
        public const int ReachNodes = 10;
        /// <summary>F2 (conception-coordination diagnosis): states of a woman at her daily check; exclusive, first that holds.</summary>
        public static readonly string[] CheckNames = { "no possible father (no man partner, no union)", "neither trying", "she only", "he only", "both trying (no conception)", "conceived" };
        public const int NearNodes = 3;

        public readonly SortedDictionary<int, DemogPerson> People = new SortedDictionary<int, DemogPerson>();
        public readonly List<DemogYear> Years = new List<DemogYear>();
        public readonly SortedDictionary<int, DemogArrangement> Arrangements = new SortedDictionary<int, DemogArrangement>();
        public int Start, Final, Births, Deaths, Arrivals, Founders;
        public int[] DeathsByCause = new int[9];
        public long FoodProducedTotal, FoodConsumedTotal, PersonDaysTotal, CareTicksTotal, ChildFedUnitsTotal, HouseholdDaysTotal, HouseholdMemberDaysTotal;
        public long[] ConsumedByReason = new long[8];
        public long[] ProducedByReason = new long[8];
        public int ExtinctDay = -1, FinalDay, MaxGeneration;
        /// <summary>The goods ledger's edible totals when the run began (the founders' packs and stores, made before the first tick).</summary>
        public long LedgerProducedAtStart, LedgerConsumedAtStart;
        public int[] FactionExtinctDay = new int[FactionInfo.Max];
        public int FactionCount;
        public int LandHexes, LandNodes;
        public long TicksPerYear;
        public int DaysPerYear;
        public bool M4;

        private GodState _s;
        private SimCalendar _cal;
        private long _lastWildStock = -1;
        private readonly HashSet<int> _foragedSources = new HashSet<int>();
        private long _foragedFromSources;

        /// <summary>Calendar ticks of a sim age (milli-years) in this run's calendar (LifeScale included).</summary>
        public long AgeTicks(int milli) => _cal.TicksForAgeMilli(milli);

        public DemogYear YearOf(long day)
        {
            int y = (int)(day / Math.Max(1, DaysPerYear));
            while (Years.Count <= y) Years.Add(new DemogYear { Year = Years.Count });
            return Years[y];
        }

        public void Begin(RunInfo info, GodSim sim)
        {
            _s = sim.State;
            _cal = SimCalendar.Of(_s);
            TicksPerYear = _cal.TicksPerYear;
            DaysPerYear = _cal.DaysPerYear;
            FactionCount = _s.Setup.FactionCount;
            for (int f = 0; f < FactionExtinctDay.Length; f++) FactionExtinctDay[f] = -1;
            var g = _s.Territory.Grid;
            for (int n = 0; n < g.Count; n++) if (g.IsLand(n)) LandNodes++;
            LandHexes = _s.Territory.Topology.HexCount;
            var p = _s.Population;
            if (p == null) return;
            M4 = _s.Households != null;
            foreach (Ware w in Enum.GetValues(typeof(Ware))) if (Edible(w) && (int)w < Wares.PersonalCount) { LedgerProducedAtStart += p.Ledger.ProducedOf(w); LedgerConsumedAtStart += p.Ledger.ConsumedOf(w); }
            Start = p.AliveCount;
            foreach (var n in p.Npcs) if (n.Alive) Register(n, false);
            Founders = People.Count;
            Census(YearOf(0), 0, true);
            _lastWildStock = WildStock(out _, out _, out _);
        }

        private DemogPerson Register(Npc n, bool born)
        {
            if (People.TryGetValue(n.Id, out var d)) return d;
            d = new DemogPerson
            {
                Id = n.Id, Faction = (int)n.Faction, Founder = n.Founder, Born = born, BornTick = n.BornTick,
                Sex = n.Life?.Sex ?? 255, Mother = n.Life?.Mother ?? 0, Father = n.Life?.BioFather ?? 0, Generation = n.Life?.Generation ?? 0,
                EntryAgeMilli = born ? 0 : (int)_cal.AgeMilli(n.BornTick, _s.Tick),
            };
            People[n.Id] = d;
            return d;
        }

        public void OnTick(GodSim sim, IReadOnlyList<SimEvent> events)
        {
            var p = _s.Population;
            if (p == null) return;
            long day = _cal.Day(Math.Max(0, _s.Tick - 1));
            var y = YearOf(day);
            if (M4) ConceptionChecks(y, (int)day);
            foreach (var e in events)
            {
                switch (e)
                {
                    case NpcBorn b:
                    {
                        var n = p.Find(b.Npc);
                        var d = Register(n, true);
                        d.Mother = b.Mother; d.Father = b.BioFather; d.Generation = b.Generation; d.Sex = b.Sex; d.FatherOutside = b.BioFather != 0 && b.BioFather != b.BelievedFather;
                        if (People.TryGetValue(b.Mother, out var m)) m.Births++;
                        Births++; y.Births++;
                        MaxGeneration = Math.Max(MaxGeneration, b.Generation);
                        break;
                    }
                    case NpcImmigrated im: Arrivals++; Register(p.Find(im.Npc), false); break;
                    case NpcDied dd:
                    {
                        Deaths++; y.Deaths++;
                        DeathsByCause[(int)dd.Cause]++; y.DeathsByCause[(int)dd.Cause]++;
                        var n = p.Find(dd.Npc);
                        var d = n != null ? Register(n, false) : null;
                        if (d != null) { d.DeathTick = dd.Tick; d.Cause = dd.Cause; }
                        long age = n != null ? _cal.AgeMilli(n.BornTick, dd.Tick) : 0;
                        y.DeathsByAgeBand[Band(age)]++;
                        if (dd.Cause == DeathCause.Starvation) StarveContext(y, dd.Node, age);
                        break;
                    }
                    case NpcMatured nm when nm.From == LifeStage.Child:
                        if (People.TryGetValue(nm.Npc, out var mp)) mp.MaturedTick = nm.Tick;
                        break;
                    case PregnancyStarted _: y.Conceptions++; break;
                    case PregnancyEnded pe when pe.Outcome == PregnancyEnded.Miscarriage: y.Miscarriages++; break;
                    case NpcDay nd:
                        PersonDaysTotal++; y.PersonDays++;
                        if (nd.Ticks != null)
                        {
                            long care = nd.Ticks.Length > Activity.Care ? nd.Ticks[Activity.Care] : 0;
                            CareTicksTotal += care; y.CareTicks += care;
                            y.ForageTicks += nd.Ticks[Activity.Forage];
                        }
                        y.WalkTicks += nd.WalkTicks;
                        break;
                    case ChildFed cf:
                    {
                        y.ChildFedActs++; y.ChildFedUnits += cf.Count; ChildFedUnitsTotal += cf.Count;
                        bool parent = People.TryGetValue(cf.Child, out var c) && (c.Mother == cf.Feeder || c.Father == cf.Feeder);
                        if (parent) y.ChildFedByParent++; else y.ChildFedByNonParent++;
                        if (People.TryGetValue(cf.Feeder, out var fp)) { fp.FedChildren++; if (parent) fp.FedOwnChildren++; }
                        y.ChildFedByBand[(int)cf.Band & 7]++;
                        break;
                    }
                    case CareGiven cg when cg.Kind == CareGiven.Tend: y.Tended++; break;
                    case CareArrangementMade am:
                        y.ArrangementsMade++;
                        Arrangements[am.Arrangement] = new DemogArrangement { Id = am.Arrangement, Asker = am.Asker, Carer = am.Carer, MadeTick = am.Tick };
                        break;
                    case CareArrangementEnded ae:
                        y.ArrangementsEnded++;
                        if (Arrangements.TryGetValue(ae.Arrangement, out var arr)) { arr.Given = ae.Given; arr.Returned = ae.Returned; arr.EndedBy = ae.EndedBy; arr.Reason = ae.Reason; arr.EndedTick = ae.Tick; }
                        break;
                    case TryForChildChosen tc: if (People.TryGetValue(tc.Npc, out var tp)) tp.Tries++; break;
                    case ChildWanted cw: if (People.TryGetValue(cw.Npc, out var wp)) { wp.Wants++; wp.LastWantTick = cw.Tick; } break;
                    case PartnershipFormed pf:
                        if (People.TryGetValue(pf.A, out var pa)) pa.PartnershipsFormed++;
                        if (People.TryGetValue(pf.B, out var pb)) pb.PartnershipsFormed++;
                        break;
                    case ReproductiveUnionFormed uf:
                        if (People.TryGetValue(uf.A, out var ua)) { ua.UnionsFormed++; if (!uf.Partners) ua.UnionsOutside++; }
                        if (People.TryGetValue(uf.B, out var ub)) { ub.UnionsFormed++; if (!uf.Partners) ub.UnionsOutside++; }
                        if (!uf.Partners) y.UnionsOutsideFormed++;
                        y.UnionsFormed++;
                        break;
                    case ItemMoved im when Edible(im.Ware) && (im.Reason == MoveReason.Deposit || im.Reason == MoveReason.Withdraw):
                        if (People.TryGetValue(im.Actor, out var sp)) { if (im.Reason == MoveReason.Deposit) sp.Deposited += im.Count; else sp.Withdrawn += im.Count; }
                        break;
                    case ChildFostered _: y.Fostered++; break;
                    case ChildOrphaned _: y.Orphaned++; break;
                    case Foraged fo:
                        _foragedFromSources += fo.Count;
                        _foragedSources.Add(fo.Source);
                        break;
                    case Produced pr when Edible(pr.Ware):
                        FoodProducedTotal += pr.Count; ProducedByReason[(int)pr.Reason & 7] += pr.Count;
                        if (pr.Reason == ProduceReason.Foraged) y.FoodForaged += pr.Count; else y.FoodProducedOther += pr.Count;
                        break;
                    case Consumed co when Edible(co.Ware):
                        FoodConsumedTotal += co.Count; ConsumedByReason[(int)co.Reason & 7] += co.Count;
                        switch (co.Reason)
                        {
                            case ConsumeReason.Eaten: y.FoodEaten += co.Count; break;
                            case ConsumeReason.Spoiled: y.FoodSpoiled += co.Count; break;
                            case ConsumeReason.Buried: y.FoodBuried += co.Count; break;
                            default: y.FoodOtherOut += co.Count; break;
                        }
                        break;
                    case HouseholdDay hd:
                    {
                        HouseholdDaysTotal++; HouseholdMemberDaysTotal += hd.Members;
                        if (hd.Day % Math.Max(1, DaysPerYear) != 0) break;
                        var hy = YearOf(hd.Day);
                        hy.Households++; hy.HouseholdMembers += hd.Members;
                        if (hd.Children > 0) hy.HouseholdsWithChildren++;
                        if (hd.Elders > 0) hy.HouseholdsWithElders++;
                        hy.MaxHouseholdSize = Math.Max(hy.MaxHouseholdSize, hd.Members);
                        int adults = hd.Members - hd.Children;
                        int k = adults == 0 ? 6 : adults == 1 ? (hd.Children > 0 ? 1 : 0) : adults == 2 ? (hd.Children > 0 ? 3 : 2) : (hd.Children > 0 ? 5 : 4);
                        hy.Composition[k]++;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// F2: classifies each woman whose daily slot ran in the tick just stepped (LifeCycleSystem: tick of day <c>1 + (id % 8) x
        /// TicksPerDay / 8</c>), read after her check (tries are not changed by the check; a conception shows as PregStart now).
        /// </summary>
        private void ConceptionChecks(DemogYear y, int day)
        {
            long t = _s.Tick - 1;
            int tpd = _cal.TicksPerDay, slotLen = Math.Max(1, tpd / 8), tod = _cal.TickOfDay(t);
            if (tod < 1 || (tod - 1) % slotLen != 0) return;
            int slot = (tod - 1) / slotLen;
            if (slot >= 8) return;
            var lt = _s.Tuning.Households.Life;
            var g = _s.Territory.Grid;
            foreach (var n in _s.Population.Npcs)
            {
                if (!Bodies.IsLivingPerson(n) || n.Life == null || n.Life.Sex != 0 || n.Id % 8 != slot || n.Life.LastDaily != day || n.Stage == LifeStage.Child) continue;
                var l = n.Life;
                bool conceived = l.PregFather != 0 && l.PregStart >= t;
                if (l.PregFather != 0 && !conceived) continue;
                long age = Bodies.AgeMilli(_cal, n, t);
                int end = Reproduction.FertileEnd(lt, l.Pheno.Fertility);
                if (age < lt.FertileFromMilli || age > end) continue;
                if (l.LastBirthTick >= 0 && t - l.LastBirthTick < _cal.TicksForAgeMilli(lt.AfterBirthMilli)) continue;
                if (conceived) { y.Checks[5]++; y.BothChanceSum += Reproduction.DailyPermille(lt, l.Pheno.Fertility, n.NutritionPermille, (int)age, end); continue; }
                // the possible father: the man she is trying with, else her union mate, else a man partner
                Npc mate = null;
                bool sheTries = l.TryWith != 0 && l.TryingUntil >= t;
                if (sheTries) { var m = _s.Population.Find(l.TryWith); if (m?.Life != null && Bodies.IsLivingPerson(m) && m.Life.Sex == 1 && m.Stage != LifeStage.Child) mate = m; }
                if (mate == null)
                    foreach (var u in _s.Households.Unions)
                    {
                        if (u.A != n.Id && u.B != n.Id) continue;
                        var m = _s.Population.Find(u.A == n.Id ? u.B : u.A);
                        if (m?.Life != null && Bodies.IsLivingPerson(m) && m.Life.Sex == 1 && m.Stage != LifeStage.Child) { mate = m; break; }
                    }
                if (mate == null)
                    foreach (int o in Partnerships.PartnersOf(_s, n.Id))
                    {
                        var m = _s.Population.Find(o);
                        if (m?.Life != null && Bodies.IsLivingPerson(m) && m.Life.Sex == 1 && m.Stage != LifeStage.Child) { mate = m; break; }
                    }
                if (mate == null)
                {
                    y.Checks[0]++;
                    var ps = Partnerships.PartnersOf(_s, n.Id);
                    y.NoFather[ps.Count == 0 ? 0 : Preferences.AllowsOutside(_s, n, 0) ? 1 : 2]++;
                    if (People.TryGetValue(n.Id, out var dw) && dw.LastWantTick >= 0 && t - dw.LastWantTick <= tpd) y.NoFatherSought++;
                    continue;
                }
                bool she = sheTries && l.TryWith == mate.Id;
                bool he = mate.Life.TryWith == n.Id && mate.Life.TryingUntil >= t;
                int k = she && he ? 4 : she ? 2 : he ? 3 : 1;
                y.Checks[k]++;
                if (k == 4)
                {
                    y.BothChanceSum += Reproduction.DailyPermille(lt, l.Pheno.Fertility, n.NutritionPermille, (int)age, end);
                    if (ReproductiveUnions.Between(_s, n.Id, mate.Id) == null)
                    {
                        y.BothNoUnion++;
                        bool dormant = true;
                        foreach (var u in _s.Households.Unions)
                        {
                            int who = u.A == n.Id || u.B == n.Id ? n.Id : u.A == mate.Id || u.B == mate.Id ? mate.Id : 0;
                            if (who == 0) continue;
                            var o = _s.Population.Find(u.A == who ? u.B : u.A);
                            if (o?.Life == null || !Bodies.IsLivingPerson(o)) continue;
                            if (o.Life.TryWith == who && o.Life.TryingUntil >= t || o.Life.PregFather == who || _s.Population.Find(who)?.Life?.PregFather == o.Id) dormant = false;
                        }
                        if (dormant) y.BothNoUnionDormant++;
                    }
                }
                else if (g.Distance(n.Node, mate.Node) <= NearNodes) y.ChecksNotBothNear++;
            }
        }

        private void StarveContext(DemogYear y, int node, long age)
        {
            y.StarveByAgeBand[Band(age)]++;
            var g = _s.Territory.Grid;
            long w5 = 0, w10 = 0, c10 = 0; int nearest = int.MaxValue;
            foreach (var src in _s.Population.Sources)
            {
                if (src.Kind != SourceKind.Berries && src.Kind != SourceKind.Roots && src.Kind != SourceKind.Fish) continue;
                int d = g.Distance(node, src.Node);
                if (src.Stock >= 5 && d < nearest) nearest = d;
                if (d <= 5) w5 += src.Stock;
                if (d <= 10) { w10 += src.Stock; c10 += src.Capacity; }
            }
            y.StarveFoodWithin5 += w5; y.StarveFoodWithin10 += w10; y.StarveCapacityWithin10 += c10;
            if (w10 == 0) y.StarveDeathsNoFoodWithin10++;
            if (nearest < int.MaxValue) { y.StarveNearestSum += nearest; y.StarveNearestHist[nearest <= 10 ? 0 : nearest <= 20 ? 1 : nearest <= 40 ? 2 : 3]++; }
        }

        public static bool Edible(Ware w) => Wares.IsFood(w) && w != Ware.Nightshade;

        public static int Band(long ageMilli)
        {
            int b = 0;
            for (int i = 0; i < AgeBands.Length; i++) if (ageMilli >= AgeBands[i]) b = i;
            return b;
        }

        public void OnDayEnd(GodSim sim, FactionDayRow[] rows)
        {
            var p = _s.Population;
            if (p == null) return;
            long day = _cal.Day(_s.Tick); // the day that starts now
            FinalDay = (int)day;
            var prev = YearOf(day - 1);
            // daily: woman-day statuses, person-days by age band, regeneration
            var lt = _s.Tuning.Households.Life;
            foreach (var n in p.Npcs)
            {
                if (!n.Alive) continue;
                long age = _cal.AgeMilli(n.BornTick, _s.Tick);
                prev.PersonDaysByAgeBand[Band(age)]++;
                if (n.Stage != LifeStage.Child && People.TryGetValue(n.Id, out var dp)) { dp.AdultDays++; if (M4 && Partnerships.Count(_s, n.Id) > 0) dp.PartneredDays++; }
                if (n.Life == null || n.Life.Sex != 0 || !M4) continue;
                int end = Reproduction.FertileEnd(lt, n.Life.Pheno.Fertility);
                if (age < lt.FertileFromMilli || age > end) continue;
                prev.WomanDays[WomanStatus(n)]++;
            }
            long stock = WildStock(out _, out _, out _);
            if (_lastWildStock >= 0) prev.Regeneration += stock - _lastWildStock + _foragedFromSources;
            _lastWildStock = stock; _foragedFromSources = 0;
            int alive = p.AliveCount;
            for (int f = 0; f < FactionCount; f++) if (FactionExtinctDay[f] < 0 && p.AliveOf((Faction)f) == 0) FactionExtinctDay[f] = (int)day;
            if (alive == 0 && ExtinctDay < 0) ExtinctDay = (int)day;
            if (day % DaysPerYear == 0)
            {
                prev.PopEnd = alive;
                prev.SourcesForaged = _foragedSources.Count;
                _foragedSources.Clear();
                Census(YearOf(day), day, false);
            }
        }

        /// <summary>A fertile woman's status today (exclusive, first that holds): pregnant, recovering, in a reproductive union, partnered with a man, partnered with women only, single.</summary>
        private int WomanStatus(Npc n)
        {
            var l = n.Life;
            if (l.PregFather != 0) return 0;
            if (l.LastBirthTick >= 0 && _s.Tick - l.LastBirthTick < _cal.TicksForAgeMilli(_s.Tuning.Households.Life.AfterBirthMilli)) return 1;
            if (ReproductiveUnions.Count(_s, n.Id) > 0) return 2;
            var ps = Partnerships.PartnersOf(_s, n.Id);
            if (ps.Count == 0) return 5;
            foreach (int o in ps) { var m = _s.Population.Find(o); if (m?.Life != null && m.Life.Sex == 1) return 3; }
            return 4;
        }

        private long WildStock(out long capacity, out long msy, out int sources)
        {
            long st = 0; capacity = 0; msy = 0; sources = 0;
            foreach (var src in _s.Population.Sources)
            {
                if (src.Kind != SourceKind.Berries && src.Kind != SourceKind.Roots && src.Kind != SourceKind.Fish) continue;
                st += src.Stock; capacity += src.Capacity; sources++;
                msy += (long)src.RegrowPermille * src.Capacity / 4000; // logistic maximum sustainable yield r C / 4 per day
            }
            return st;
        }

        /// <summary>The census at the start of a year (day = 10 y): ages, stages, relationships, food stocks, reach.</summary>
        private void Census(DemogYear y, long day, bool first)
        {
            var p = _s.Population;
            var g = _s.Territory.Grid;
            int alive = 0, maxGen = 0; long nutA = 0, nutC = 0;
            var living = new List<int>();
            var factions = new HashSet<int>();
            foreach (var n in p.Npcs)
            {
                if (!n.Alive) continue;
                alive++;
                living.Add(n.Node);
                factions.Add((int)n.Faction);
                long age = _cal.AgeMilli(n.BornTick, _s.Tick);
                y.AgeBins[Math.Min(AgeBinCount - 1, (int)(age / 250))]++;
                if (n.Stage == LifeStage.Child) { y.Children++; nutC += n.NutritionPermille; } else { if (n.Stage == LifeStage.Elder) y.Elders++; else y.Adults++; nutA += n.NutritionPermille; }
                if (n.Life != null) maxGen = Math.Max(maxGen, n.Life.Generation);
                foreach (var l in n.Carry) if (!l.Empty && Edible(l.Ware)) y.CarriedFood += l.Count;
            }
            y.PopStart = alive;
            y.MeanNutritionAdults = y.Adults + y.Elders > 0 ? (int)(nutA / (y.Adults + y.Elders)) : 0;
            y.MeanNutritionChildren = y.Children > 0 ? (int)(nutC / y.Children) : 0;
            y.FactionsAlive = factions.Count;
            y.MaxGenerationAlive = maxGen;
            foreach (var gl in p.Lots) if (Edible(gl.Lot.Ware)) y.GroundFood += gl.Lot.Count;
            foreach (var b in p.Bins) if (b.Kind == BinKind.HouseholdStore) foreach (var l in b.Lots) if (Edible(l.Ware)) y.StoreFood += l.Count;
            y.WildFoodStock = WildStock(out long cap, out long msy, out int sources);
            y.WildFoodCapacity = cap; y.WildFoodMsyPerDay = msy; y.FoodSources = sources;
            foreach (var src in p.Sources)
            {
                if (src.Kind != SourceKind.Berries && src.Kind != SourceKind.Roots && src.Kind != SourceKind.Fish) continue;
                foreach (int node in living) if (g.Distance(node, src.Node) <= ReachNodes) { y.ReachableWildFood += src.Stock; break; }
            }
            if (_s.Households != null)
            {
                foreach (var pr in _s.Households.Partnerships)
                {
                    y.RomanticPairs++;
                    var a = p.Find(pr.A); var b = p.Find(pr.B);
                    if (a?.Life != null && b?.Life != null && a.Life.Sex != b.Life.Sex) y.MixedPairs++; else y.SameSexPairs++;
                }
                y.Unions = ReproductiveUnions.Total(_s);
                y.UnionsOutsidePartnership = ReproductiveUnions.OutsidePartnership(_s);
            }
        }

        public void End(GodSim sim, IDictionary<string, long> counters)
        {
            var p = _s.Population;
            if (p == null) return;
            Final = p.AliveCount;
            var lt = _s.Tuning.Households.Life;
            foreach (var d in People.Values)
            {
                var n = p.Find(d.Id);
                if (n?.Life != null)
                {
                    d.FertileEndMilli = Reproduction.FertileEnd(lt, n.Life.Pheno.Fertility);
                    d.ChildDesire = n.Life.ChildDesire; d.Companionship = n.Life.Companionship; d.Commitment = n.Life.Commitment;
                }
            }
            // close the last (partial) year
            var last = YearOf(_cal.Day(_s.Tick));
            if (last.PopEnd == 0) last.PopEnd = Final;
        }

        public void Dispose() { }
    }
}
