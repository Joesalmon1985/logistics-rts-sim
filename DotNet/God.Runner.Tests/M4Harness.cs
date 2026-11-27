using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Estate;
using God.Sim.Population;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M4's island runs (life cycle, households, care) for the runner tests: a sandbox game built as the runner builds one
    /// (the stage 3 AI plays each faction's state), with M4's measures recomputed from the event stream only. Every number is
    /// descriptive unless a test names its control.
    /// </summary>
    public sealed class M4Run
    {
        public ulong Seed;
        public int Days, Founders, Start, End, Births, Arrivals, Deaths, Departures, Invariants;
        public int[] DeathsByCause = new int[9];
        public int ChildDeaths, ChildDeathsStarved, ChildBirths, ChildrenMatured;
        public int[] ChildDeathsByCause = new int[9];
        public int ChildFed, ChildFedByNonParent, ChildFedByFoster, CareGiven, Tended, Begging;
        public int PartnershipsFormed, PartnershipsEnded, Left, Arrangements, ArrangementsEnded, Fostered, Orphaned;
        public int MaxGeneration, Conceptions, Infections, DiseaseDeaths, Estates, EstatesSettled, EstateTransfers, EstateLateTicks;
        public int ConceptionsUnderAge, PregnancyChecks;
        public int[] PopByDay;
        public int[] AliveByFaction = new int[6];
        public int[] FactionExtinctDay = Enumerable.Repeat(-1, 6).ToArray();
        public List<int> GenerationsReproduced = new List<int>();
        public long CareTicks, PersonDays;
        public int CareRowsTicksMismatch;
        public int MeanHouseholdSizeX100, HouseholdsAtEnd, LivingWithoutHousehold;
        public int[] Pyramid = new int[17];
        /// <summary>Food units produced by foraging, by actor age band: under 0.5, 0.5-0.75, 0.75-1.0, adults; and work ticks of those bands.</summary>
        public long[] ForagedByAge = new long[4];
        public long[] ForageTicksByAge = new long[4];
        public long MotherAgeSumMilli; public int MotherAgeN;
        public GodSim Sim;
        public int EverAlive, SpawnedAfterStart, MaxLiveSeen, MaxHouseholdSize, PartneredBy2, ReachedAge2, InfectionsWithoutRoute;
        public List<string> InvariantSample = new List<string>();

        public bool Extinct => End == 0;
        public bool FactionExtinct(int f) => FactionExtinctDay[f] >= 0;

        public static M4Run Run(ulong seed, int days, int perFaction = 20, int factions = 6, Action<SimTuning> tune = null, Action<GameSetup> setup = null,
            Action<GodSim, SimEvent> onEvent = null, bool keepSim = false)
        {
            var t = new SimTuning();
            tune?.Invoke(t);
            var g = new GameSetup(seed, GameSetup.NoHuman, factions) { Population = true, StartNpcsPerFaction = perFaction, Victory = VictorySpec.SandboxOnly() };
            setup?.Invoke(g);
            return Run(g, t, days, onEvent, keepSim, null);
        }

        /// <summary>A run of a scenario file (its setup and tuning), with the test's own changes on top.</summary>
        public static M4Run FromScenario(string name, ulong seed, int days, Action<SimTuning> tune = null, Action<GameSetup> setup = null,
            Action<GodSim, SimEvent> onEvent = null, bool keepSim = false, Func<ISimSystem>[] systems = null)
        {
            var sc = TestUtil.Load(name);
            var g = sc.ToSetup(seed);
            var t = sc.ToTuning();
            tune?.Invoke(t);
            setup?.Invoke(g);
            return Run(g, t, days, onEvent, keepSim, systems);
        }

        public static M4Run Run(GameSetup g, SimTuning t, int days, Action<GodSim, SimEvent> onEvent, bool keepSim, Func<ISimSystem>[] systems)
        {
            ulong seed = g.Seed;
            int factions = g.FactionCount;
            var built = SimBuilder.Create(g, t);
            var sim = built.Sim;
            if (systems != null) foreach (var f in systems) { var x = f(); sim.AddSystem(x is M4LifeCycleTests.Probe pr ? pr.Order : 239, x); }
            var r = new M4Run { Seed = seed, Days = days, PopByDay = new int[days + 1] };
            var p = sim.State.Population;
            r.Founders = p.Stats.Founders;
            r.Start = p.AliveCount;
            r.PopByDay[0] = r.Start;
            int tpd = g.TicksPerDay;
            var cal = SimCalendar.Of(sim.State);
            var events = new List<SimEvent>();
            var childOf = new Dictionary<int, (int mother, int father)>();
            var fostered = new HashSet<int>();
            var genOf = new Dictionary<int, int>();
            foreach (var n in p.Npcs) genOf[n.Id] = 0;
            var reproduced = new SortedSet<int>();
            var partneredEver = new HashSet<int>(); var born1 = new HashSet<int>(); var counted2 = new HashSet<int>();
            for (int d = 0; d < days; d++)
            {
                for (int i = 0; i < tpd; i++)
                {
                    sim.Step();
                    events.Clear();
                    sim.DrainInto(events);
                    foreach (var e in events)
                    {
                        onEvent?.Invoke(sim, e);
                        switch (e)
                        {
                            case NpcSpawned sp when sim.State.Tick > 1: r.SpawnedAfterStart++; break;
                            case NpcBorn b:
                                r.Births++; childOf[b.Npc] = (b.Mother, b.BelievedFather); genOf[b.Npc] = b.Generation; r.MaxGeneration = Math.Max(r.MaxGeneration, b.Generation);
                                reproduced.Add(b.Generation - 1);
                                break;
                            case NpcImmigrated im: r.Arrivals++; genOf[im.Npc] = 0; break;
                            case NpcDied dd:
                            {
                                r.Deaths++; r.DeathsByCause[(int)dd.Cause]++;
                                var dn = p.Find(dd.Npc);
                                if (dn != null && cal.AgeMilli(dn.BornTick, sim.State.Tick) < 1000) { r.ChildDeaths++; r.ChildDeathsByCause[(int)dd.Cause]++; }
                                if (dd.Cause == DeathCause.Disease) r.DiseaseDeaths++;
                                break;
                            }
                            case ChildFed cf:
                            {
                                r.ChildFed++;
                                if (!childOf.TryGetValue(cf.Child, out var par) || par.mother != cf.Feeder && par.father != cf.Feeder) r.ChildFedByNonParent++;
                                if (fostered.Contains(cf.Child)) r.ChildFedByFoster++;
                                break;
                            }
                            case God.Sim.Population.Households.CareGiven cg: r.CareGiven++; if (cg.Kind == God.Sim.Population.Households.CareGiven.Tend) r.Tended++; break;
                            case BeggingMade _: r.Begging++; break;
                            case PartnershipFormed _: r.PartnershipsFormed++; break;
                            case PartnershipEnded pe: r.PartnershipsEnded++; if (pe.Reason == PartnershipEnded.Left) r.Left++; break;
                            case CareArrangementMade _: r.Arrangements++; break;
                            case CareArrangementEnded _: r.ArrangementsEnded++; break;
                            case ChildFostered cfo: r.Fostered++; fostered.Add(cfo.Child); break;
                            case ChildOrphaned _: r.Orphaned++; break;
                            case PregnancyStarted ps:
                                r.Conceptions++; r.PregnancyChecks++;
                                if (ps.MotherAgeMilli < 1000 || ps.FatherAgeMilli < 1000) r.ConceptionsUnderAge++;
                                r.MotherAgeSumMilli += ps.MotherAgeMilli; r.MotherAgeN++;
                                break;
                            case IllnessStarted il: r.Infections++; if (il.Route < 1 || il.Route > 5) r.InfectionsWithoutRoute++; break;
                            case EstateOpened _: r.Estates++; break;
                            case EstateSettled es: r.EstatesSettled++; r.EstateLateTicks = Math.Max(r.EstateLateTicks, es.Ticks); break;
                            case EstateTransfer _: r.EstateTransfers++; break;
                            case NpcDay nd:
                            {
                                r.PersonDays++;
                                int sum = 0; foreach (int x in nd.Ticks) sum += x;
                                r.CareTicks += nd.Ticks[Activity.Care];
                                break;
                            }
                            case Produced pr when pr.Reason == ProduceReason.Foraged:
                            {
                                var a = p.Find(pr.Npc);
                                if (a == null) break;
                                r.ForagedByAge[AgeBand(cal, a, sim.State.Tick)] += pr.Count;
                                break;
                            }
                        }
                    }
                    // work ticks by age band (forage and gather), counted from the actions of the living
                    if ((sim.State.Tick % 10) == 0)
                        foreach (var n in p.Npcs)
                            if (n.Alive && (n.Action.Verb == God.Sim.Learning.Verb.ForageAt || n.Action.Verb == God.Sim.Learning.Verb.Eat && n.Action.TargetKind == God.Sim.Policy.IntentTarget.Node))
                                r.ForageTicksByAge[AgeBand(cal, n, sim.State.Tick)] += 10;
                }
                var bad = SimInvariants.Check(sim.State);
                r.Invariants += bad.Count;
                foreach (var b in bad) if (r.InvariantSample.Count < 10) r.InvariantSample.Add("day " + (d + 1) + ": " + b);
                r.PopByDay[d + 1] = p.AliveCount;
                var hsd = sim.State.Households;
                if (hsd != null)
                {
                    var live = new Dictionary<int, int>();
                    foreach (var pr in hsd.Partnerships) { live[pr.A] = live.TryGetValue(pr.A, out int x) ? x + 1 : 1; live[pr.B] = live.TryGetValue(pr.B, out int y) ? y + 1 : 1; }
                    foreach (var kv in live) r.MaxLiveSeen = Math.Max(r.MaxLiveSeen, kv.Value);
                    foreach (var h in hsd.Households) r.MaxHouseholdSize = Math.Max(r.MaxHouseholdSize, h.Members.Count);
                    foreach (var n in p.Npcs)
                    {
                        if (!n.Alive || n.Life == null) continue;
                        long age = cal.AgeMilli(n.BornTick, sim.State.Tick);
                        if (age >= 2000 && !counted2.Contains(n.Id) && born1.Contains(n.Id))
                        {
                            counted2.Add(n.Id); r.ReachedAge2++;
                            if (partneredEver.Contains(n.Id)) r.PartneredBy2++;
                        }
                        if (age < 2000 && Partnerships.Count(sim.State, n.Id) > 0) partneredEver.Add(n.Id);
                        if (age < 1200) born1.Add(n.Id);
                    }
                }
                for (int f = 0; f < factions; f++)
                    if (r.FactionExtinctDay[f] < 0 && p.AliveOf((Faction)f) == 0) r.FactionExtinctDay[f] = d + 1;
            }
            r.End = p.AliveCount;
            r.EverAlive = p.Npcs.Count;
            for (int f = 0; f < factions; f++) r.AliveByFaction[f] = p.AliveOf((Faction)f);
            r.GenerationsReproduced = reproduced.ToList();
            var hs = sim.State.Households;
            if (hs != null)
            {
                r.HouseholdsAtEnd = hs.Households.Count;
                r.MeanHouseholdSizeX100 = hs.Households.Count == 0 ? 0 : hs.Households.Sum(h => h.Members.Count) * 100 / hs.Households.Count;
            }
            foreach (var n in p.Npcs)
            {
                if (!n.Alive) continue;
                if (n.Household == 0) r.LivingWithoutHousehold++;
                int bin = (int)Math.Min(16, cal.AgeMilli(n.BornTick, sim.State.Tick) / 250);
                r.Pyramid[bin]++;
            }
            if (keepSim) r.Sim = sim;
            return r;
        }

        private static int AgeBand(SimCalendar cal, Npc n, long tick)
        {
            long a = cal.AgeMilli(n.BornTick, tick);
            return a < 500 ? 0 : a < 750 ? 1 : a < 1000 ? 2 : 3;
        }

        public override string ToString() =>
            $"seed {Seed}: start {Start} end {End} (births {Births}, arrivals {Arrivals}, deaths {Deaths}: starve {DeathsByCause[1]} wound {DeathsByCause[2]} disease {DeathsByCause[3]} old {DeathsByCause[4]} birth {DeathsByCause[5]} exposure {DeathsByCause[6]} accident {DeathsByCause[8]}); "
            + $"child deaths {ChildDeaths} (starve {ChildDeathsByCause[1]}, exposure {ChildDeathsByCause[6]}, accident {ChildDeathsByCause[8]}, birth {ChildDeathsByCause[5]}); gen max {MaxGeneration}; "
            + $"partners {PartnershipsFormed}/{PartnershipsEnded} (left {Left}); fed {ChildFed} (non-parent {ChildFedByNonParent}, foster {ChildFedByFoster}); begging {Begging}; tended {Tended}; arrangements {Arrangements}/{ArrangementsEnded}; "
            + $"orphaned {Orphaned} fostered {Fostered}; infections {Infections}; households {HouseholdsAtEnd} size {MeanHouseholdSizeX100 / 100.0:F2}; no household {LivingWithoutHousehold}; invariants {Invariants}; "
            + $"factions alive [{string.Join(",", AliveByFaction)}]";
    }

    [TestFixture, Category("M4-diagnostics"), Explicit("a diagnostic print, not a criterion")]
    public sealed class M4Diagnostics
    {
        public static string Snapshot(GodSim sim)
        {
            var s = sim.State; var p = s.Population; var cal = SimCalendar.Of(s);
            int adults = 0, partnered = 0, mixed = 0, trying = 0, pregnant = 0, nutSum = 0, kids = 0, kidNut = 0, homes = 0;
            foreach (var n in p.Npcs)
            {
                if (!n.Alive || n.Life == null) continue;
                if (n.Stage == LifeStage.Child) { kids++; kidNut += n.NutritionPermille; continue; }
                adults++; nutSum += n.NutritionPermille;
                if (n.HomeHut != 0) homes++;
                var ps = Partnerships.PartnersOf(s, n.Id);
                if (ps.Count > 0) { partnered++; var o = p.Find(ps[0]); if (o?.Life != null && o.Life.Sex != n.Life.Sex) mixed++; }
                if (n.Life.TryingUntil >= s.Tick) trying++;
                if (n.Life.PregFather != 0) pregnant++;
            }
            return $"day {cal.Day(s.Tick)}: adults {adults} partnered {partnered} mixed {mixed} trying {trying} pregnant {pregnant} adult nut {(adults == 0 ? 0 : nutSum / adults)} kids {kids} kid nut {(kids == 0 ? 0 : kidNut / kids)} with home {homes}";
        }

        [Test]
        public void Fertility_Diagnostics()
        {
            foreach (ulong seed in new ulong[] { 1, 2 })
            {
                long fertile = 0, partneredMale = 0, bothTrying = 0, pregnant = 0, sameSex = 0, single = 0, lowNut = 0;
                var r = M4Run.FromScenario("m4_families", seed, 40, onEvent: (sim, e) =>
                {
                    if (!(e is NpcDay nd) || nd.Npc != sim.State.Population.Npcs.First(n => n.Alive).Id) return;
                    var s = sim.State; var cal = SimCalendar.Of(s);
                    foreach (var n in s.Population.Npcs)
                    {
                        if (!n.Alive || n.Life == null || n.Life.Sex != 0 || n.Stage == LifeStage.Child) continue;
                        long age = cal.AgeMilli(n.BornTick, s.Tick);
                        if (age > Reproduction.FertileEnd(s.Tuning.Households.Life, n.Life.Pheno.Fertility)) continue;
                        fertile++;
                        if (n.Life.PregFather != 0) { pregnant++; continue; }
                        if (n.NutritionPermille < 600) lowNut++;
                        var ps = Partnerships.PartnersOf(s, n.Id);
                        if (ps.Count == 0) { single++; continue; }
                        var m = s.Population.Find(ps[0]);
                        if (m.Life.Sex != 1) { sameSex++; continue; }
                        partneredMale++;
                        if (n.Life.TryWith == m.Id && n.Life.TryingUntil >= s.Tick && m.Life.TryWith == n.Id && m.Life.TryingUntil >= s.Tick) bothTrying++;
                    }
                });
                TestContext.WriteLine($"seed {seed}: fertile woman-days {fertile}: pregnant {pregnant}, single {single}, same-sex {sameSex}, with a man {partneredMale} (both trying {bothTrying}); nutrition under 600 {lowNut}; births {r.Births}");
            }
        }

        [Test]
        public void Band6x20_30Days_Invariants()
        {
            var res = Watch.Seeds(1, 6, s => M4Run.Run(s, 30));
            foreach (var r in res) { TestContext.WriteLine(r.ToString()); foreach (var b in r.InvariantSample) TestContext.WriteLine("  INV " + b); }
        }

        [Test]
        public void Band6x20_30Days_Diagnostics()
        {
            var born = new int[31];
            var snaps = new List<string>();
            M4Run.Run(3, 30, onEvent: (sim, e) =>
            {
                long d = SimCalendar.Of(sim.State).Day(sim.State.Tick);
                if (e is NpcBorn) born[Math.Min(30, d)]++;
                if (e is NpcDay nd && nd.Npc == 1 && d % 5 == 0) snaps.Add(Snapshot(sim));
            });
            TestContext.WriteLine("births by day: " + string.Join(" ", born));
            foreach (var x in snaps) TestContext.WriteLine(x);
            for (ulong seed = 1; seed <= 2; seed++)
            {
                var r = M4Run.Run(seed, 30);
                TestContext.WriteLine(r.ToString());
                foreach (var b in r.InvariantSample) TestContext.WriteLine("  INV " + b);
                TestContext.WriteLine("pop by day: " + string.Join(" ", r.PopByDay));
                TestContext.WriteLine("pyramid: " + string.Join(" ", r.Pyramid));
                TestContext.WriteLine($"foraged by age band {string.Join(",", r.ForagedByAge)} ticks {string.Join(",", r.ForageTicksByAge)}");
            }
        }
    }
}
