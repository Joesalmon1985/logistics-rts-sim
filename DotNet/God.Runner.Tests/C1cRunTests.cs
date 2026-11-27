using System;
using System.Collections.Generic;
using System.Linq;
using God.Recording;
using God.Sim;
using God.Sim.Civ;
using God.Sim.Learning;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// C1c (owners use their productive output): an owner, a household member of the owner, or a holder of a Store right
    /// perceives the asset's store, walks to it and withdraws its own output within reach (to eat, carry home, pay or
    /// barter), by its own decision. Measured on civ_assets and civ_employment: store saturation, produce eaten by the
    /// owners' people, and how employment contracts end.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class C1cRunTests
    {
        /// <summary>What one run shows (all from the event stream and the state the probe samples each tick).</summary>
        internal sealed class Probe : IRunRecorder
        {
            public long AssetTicks, FullTicks;
            public int FullAtEnd, WorkedAtEnd;
            public int StoreThefts, UseThefts;
            public int Withdrawals, WithdrawnUnits, WithdrawnByOwners, WithdrawnFood;
            public int ProducedFood, EatenProduce, EatenByOwners, SpoiledInStore, Deposited;
            public long OwnerHungerSum, OwnerHungerN;
            public int WorkUnits, OwnerDeaths;
            public readonly Dictionary<EndReason, int> Ends = new Dictionary<EndReason, int>();
            public int Contracts, Wages, WageGoods, Unpaid, WalkOuts;
            public bool Invariants = true;

            private readonly Dictionary<int, int> _origin = new Dictionary<int, int>(); // lot id -> produce units (left an asset store)
            private HashSet<int> _inStore = new HashSet<int>();                       // lot ids in asset stores at the last tick end
            private GodState _s;

            public void Begin(RunInfo info, GodSim sim) { _s = sim.State; }

            private bool IsAssetStore(int bin) { var b = Goods.BinById(_s.Population, bin); return b != null && b.Kind == BinKind.AssetStore; }

            /// <summary>Whether the person is an asset owner's people (the owner, or a member of the owning household) for an owner.</summary>
            private bool OwnersPeople(int npc, Owner owner)
            {
                var n = _s.Population.Find(npc);
                if (n == null || owner.IsNone) return false;
                if (owner.Kind == OwnerKind.Npc) return owner.Id == npc;
                return owner.Kind == OwnerKind.Household && n.Household != 0 && owner.Id == n.Household;
            }

            private bool IsAssetOwner(Owner owner) => _s.Civ?.Ownership != null && _s.Civ.Ownership.Assets.Any(a => a.Owner.Same(owner));

            public void OnTick(GodSim sim, IReadOnlyList<SimEvent> events)
            {
                foreach (var e in events)
                {
                    switch (e)
                    {
                        case AssetUsed u:
                            WorkUnits++;
                            if (Wares.IsFood(u.OutputWare)) ProducedFood += u.OutputCount;
                            break;
                        case ItemMoved m:
                        {
                            int src = m.ParentLot != 0 ? m.ParentLot : m.Lot;
                            bool fromStore = m.FromKind == HolderKind.Bin && _inStore.Contains(src) || m.FromKind == HolderKind.Bin && IsAssetStore(m.FromId);
                            int k;
                            if (fromStore && !(m.ToKind == HolderKind.Bin && IsAssetStore(m.ToId)))
                            {
                                k = Wares.IsFood(m.Ware) ? m.Count : 0;
                                if (m.Reason == MoveReason.Withdraw)
                                {
                                    Withdrawals++; WithdrawnUnits += m.Count;
                                    if (Wares.IsFood(m.Ware)) WithdrawnFood += m.Count;
                                    if (OwnersPeople(m.Actor, new Owner(m.OwnerBeforeKind, m.OwnerBeforeId))) WithdrawnByOwners += m.Count;
                                }
                            }
                            else
                            {
                                _origin.TryGetValue(src, out int have);
                                k = Math.Min(have, m.Count);
                                if (k > 0) { if (have - k == 0) _origin.Remove(src); else _origin[src] = have - k; }
                                if (k > 0 && m.Reason == MoveReason.Deposit) Deposited += k;
                            }
                            int into = m.IntoLot != 0 ? m.IntoLot : m.Lot;
                            if (m.EndedLot != 0 && m.EndedLot != into && _origin.TryGetValue(m.EndedLot, out int ended)) { _origin.Remove(m.EndedLot); k += ended; }
                            if (k > 0) { _origin.TryGetValue(into, out int at); _origin[into] = at + k; }
                            break;
                        }
                        case Consumed c:
                        {
                            if (c.Reason == ConsumeReason.Spoiled && _inStore.Contains(c.Lot)) SpoiledInStore += c.Count;
                            if (!_origin.TryGetValue(c.Lot, out int have) || have <= 0) break;
                            int k = Math.Min(have, c.Count);
                            if (have - k == 0) _origin.Remove(c.Lot); else _origin[c.Lot] = have - k;
                            if (c.Reason != ConsumeReason.Eaten) break;
                            EatenProduce += k;
                            var lotOwner = new Owner(c.OwnerKind, c.OwnerId);
                            if (IsAssetOwner(lotOwner) && OwnersPeople(c.Npc, lotOwner)) EatenByOwners += k;
                            break;
                        }
                        case EmploymentStarted _: Contracts++; break;
                        case TheftCommitted th when th.Kind == TheftCommitted.Goods && th.FromKind == HolderKind.Bin && th.Asset != 0: StoreThefts++; break;
                        case TheftCommitted tu when tu.Kind == TheftCommitted.Use: UseThefts++; break;
                        case WagePaid w: Wages++; WageGoods += w.Count; break;
                        case WageUnpaid _: Unpaid++; break;
                        case ContractSuspended _: WalkOuts++; break;
                        case EmploymentEnded x: Ends.TryGetValue(x.Reason, out int n0); Ends[x.Reason] = n0 + 1; break;
                        case NpcDied d:
                        {
                            var dead = _s.Population.Find(d.Npc);
                            if (dead != null && _s.Civ?.Ownership != null && _s.Civ.Ownership.Assets.Any(a => OwnersPeople(d.Npc, a.Owner))) OwnerDeaths++;
                            break;
                        }
                    }
                }
                // store saturation: a worked asset whose store has no room for one more cycle
                var o = _s.Civ?.Ownership;
                if (o == null) return;
                _inStore = new HashSet<int>();
                foreach (var a in o.Assets)
                {
                    if (!a.IsFinished || a.Store == 0) continue;
                    var bin = Goods.BinById(_s.Population, a.Store);
                    if (bin == null) continue;
                    foreach (var l in bin.Lots) _inStore.Add(l.Id);
                    var def = AssetTable.Get(a.Kind);
                    if (def.Recipe == null || def.WorkTicks <= 0) continue;
                    AssetTicks++;
                    if (!Room(bin, def)) FullTicks++;
                }
            }

            public static bool Room(LotBin bin, AssetDef def)
            {
                if (bin.WeightCap <= 0) return true;
                int w = bin.Weight;
                foreach (var i in def.Recipe.Inputs)
                {
                    int inStore = 0;
                    foreach (var l in bin.Lots) if (l.Ware == i.Ware) inStore += l.Count;
                    w -= PopulationModule.Weight(i.Ware) * Math.Min(inStore, i.Count);
                }
                foreach (var x in def.Recipe.Outputs) w += PopulationModule.Weight(x.Ware) * x.Count;
                return w <= bin.WeightCap;
            }

            public void OnDayEnd(GodSim sim, FactionDayRow[] rows)
            {
                var o = _s.Civ?.Ownership;
                if (o == null) return;
                foreach (var n in _s.Population.Npcs)
                {
                    if (!n.Alive || n.Stage == LifeStage.Child) continue;
                    if (!o.Assets.Any(a => a.IsFinished && OwnersPeople(n.Id, a.Owner))) continue;
                    OwnerHungerSum += n.DriveOf(Drive.Hunger); OwnerHungerN++;
                }
            }

            public void End(GodSim sim, IDictionary<string, long> counters)
            {
                var o = _s.Civ?.Ownership;
                Invariants = SimInvariants.Check(_s).Count == 0;
                if (o == null) return;
                foreach (var a in o.Assets)
                {
                    if (!a.IsFinished || a.Store == 0) continue;
                    var def = AssetTable.Get(a.Kind);
                    if (def.Recipe == null || def.WorkTicks <= 0) continue;
                    WorkedAtEnd++;
                    if (!Room(Goods.BinById(_s.Population, a.Store), def)) FullAtEnd++;
                }
            }

            public void Dispose() { }

            public int End(EndReason r) => Ends.TryGetValue(r, out int n) ? n : 0;
            public int EndedTotal => Ends.Where(x => x.Key != EndReason.Refused && x.Key != EndReason.Lapsed).Sum(x => x.Value);
        }

        internal static List<Probe> Run(string scenario, int seeds, long ticks = 12000, params string[] sets)
        {
            var probes = new List<Probe>(); var specs = new List<RunSpec>();
            for (ulong seed = 1; seed <= (ulong)seeds; seed++)
            {
                var p = new Probe();
                var spec = TestUtil.Spec(scenario, seed, ticks, sets);
                spec.Scenario.Checks.Remove("smoke");
                spec.ExtraRecorders.Add(() => p);
                probes.Add(p); specs.Add(spec);
            }
            var res = TestUtil.RunAll(specs);
            for (int i = 0; i < res.Length; i++)
            {
                Assert.IsNull(res[i].Error, res[i].Error);
                Assert.AreEqual(0, res[i].ExitCode, "invariants and no exceptions: " + string.Join("; ", res[i].Messages));
            }
            return probes;
        }

        internal static string Line(Probe p) =>
            "work units " + p.WorkUnits + ", food produced " + p.ProducedFood + ", store full " + (p.AssetTicks == 0 ? 0 : p.FullTicks * 1000 / p.AssetTicks) + " permille of asset-ticks"
            + " (" + p.FullAtEnd + "/" + p.WorkedAtEnd + " full at the end), withdrawals " + p.Withdrawals + " (" + p.WithdrawnUnits + " units, " + p.WithdrawnByOwners + " by the owners' people)"
            + ", produce eaten " + p.EatenProduce + " (by the owners' people " + p.EatenByOwners + "), deposited at home " + p.Deposited + ", spoiled in store " + p.SpoiledInStore
            + ", owners' mean hunger " + (p.OwnerHungerN == 0 ? 0 : p.OwnerHungerSum / p.OwnerHungerN)
            + "; contracts " + p.Contracts + ", ended " + string.Join(" ", p.Ends.Where(x => x.Key != EndReason.Refused && x.Key != EndReason.Lapsed).OrderBy(x => x.Key).Select(x => x.Key + " " + x.Value));

        internal static string Totals(List<Probe> ps)
        {
            long at = ps.Sum(p => p.AssetTicks), full = ps.Sum(p => p.FullTicks);
            long hs = ps.Sum(p => p.OwnerHungerSum), hn = ps.Sum(p => p.OwnerHungerN);
            int ended = ps.Sum(p => p.EndedTotal), ab = ps.Sum(p => p.End(EndReason.Abandoned));
            return "TOTAL: work units " + ps.Sum(p => p.WorkUnits) + ", food produced " + ps.Sum(p => p.ProducedFood)
                + ", store full " + (at == 0 ? 0 : full * 1000 / at) + " permille of asset-ticks (" + ps.Sum(p => p.FullAtEnd) + "/" + ps.Sum(p => p.WorkedAtEnd) + " full at the end)"
                + ", withdrawals " + ps.Sum(p => p.Withdrawals) + " (" + ps.Sum(p => p.WithdrawnUnits) + " units, " + ps.Sum(p => p.WithdrawnByOwners) + " by the owners' people)"
                + ", produce eaten " + ps.Sum(p => p.EatenProduce) + " (by the owners' people " + ps.Sum(p => p.EatenByOwners) + "; seeds with any " + ps.Count(p => p.EatenByOwners > 0) + "/" + ps.Count + ")"
                + ", deposited at home " + ps.Sum(p => p.Deposited) + ", spoiled in store " + ps.Sum(p => p.SpoiledInStore)
                + ", owners' mean hunger " + (hn == 0 ? 0 : hs / hn) + ", owner deaths " + ps.Sum(p => p.OwnerDeaths) + ", store thefts " + ps.Sum(p => p.StoreThefts) + ", use thefts " + ps.Sum(p => p.UseThefts)
                + "; contracts " + ps.Sum(p => p.Contracts) + ", wages " + ps.Sum(p => p.Wages) + " (" + ps.Sum(p => p.WageGoods) + " goods), unpaid units " + ps.Sum(p => p.Unpaid) + ", walk-outs " + ps.Sum(p => p.WalkOuts) + ", ended " + ended + ": " + string.Join(" ", Enum.GetValues(typeof(EndReason)).Cast<EndReason>().Where(r => r != EndReason.None && r != EndReason.Refused && r != EndReason.Lapsed).Select(r => r + " " + ps.Sum(p => p.End(r))))
                + " (Abandoned " + (ended == 0 ? 0 : ab * 100 / ended) + "%)";
        }

        /// <summary>
        /// The before/after report (20 seeds x 10 days on each scenario): "before" is <c>Civ.C1.TakeOutEnabled = 0</c> (nobody
        /// perceives an asset's store as a store to take from: C1 and C3 as merged), "after" the default. Explicit: 80 runs.
        /// </summary>
        [Test, Explicit("report: 4 x 20 runs of 10 days")]
        public void Report_OwnersUseOutput_20Seeds()
        {
            foreach (var sc in new[] { "civ_assets", "civ_employment" })
                foreach (var arm in new[] { "before", "after" })
                {
                    var ps = arm == "before" ? Run(sc, 20, 12000, "tuning.Civ.C1.TakeOutEnabled=0") : Run(sc, 20);
                    for (int i = 0; i < ps.Count; i++) TestContext.WriteLine(sc + " " + arm + " seed " + (i + 1) + ": " + Line(ps[i]));
                    TestContext.WriteLine(sc + " " + arm + " " + Totals(ps));
                }
        }

        /// <summary>
        /// C1c on civ_employment (8 seeds x 10 days, both arms): with the default, owners' people withdraw their own output
        /// (every withdrawal within reach and permitted: no theft is classified on an asset store), eat it on most seeds,
        /// the stores spend less time full than with nobody taking output out, and a smaller share of contracts ends
        /// Abandoned. The numbers are printed; the thresholds only say "better than before".
        /// </summary>
        [Test]
        public void OwnersUseTheirOutput_StoresDrainContractsLast()
        {
            var off = Run("civ_employment", 8, 12000, "tuning.Civ.C1.TakeOutEnabled=0");
            var on = Run("civ_employment", 8);
            TestContext.WriteLine("before " + Totals(off));
            TestContext.WriteLine("after  " + Totals(on));
            Assert.IsTrue(off.All(p => p.Invariants) && on.All(p => p.Invariants), "invariants (C1 and C3) hold at the end");
            Assert.GreaterOrEqual(on.Count(p => p.WithdrawnByOwners > 0 && p.EatenByOwners > 0), 6, "owners' people take out and eat their own produce on most seeds");
            Assert.AreEqual(on.Sum(p => p.WithdrawnUnits), on.Sum(p => p.WithdrawnByOwners), "every withdrawal from an asset store is by the owner's people (no Store rights are granted here)");
            long fullOff = off.Sum(p => p.FullTicks) * 1000 / Math.Max(1, off.Sum(p => p.AssetTicks)), fullOn = on.Sum(p => p.FullTicks) * 1000 / Math.Max(1, on.Sum(p => p.AssetTicks));
            Assert.Less(fullOn * 2, fullOff, "stores are full less than half as often");
            Assert.Greater(on.Sum(p => p.EatenByOwners), off.Sum(p => p.EatenByOwners) * 5, "the owners' people eat far more of their own produce");
            int endOff = off.Sum(p => p.EndedTotal), endOn = on.Sum(p => p.EndedTotal);
            Assert.Less(on.Sum(p => p.End(EndReason.Abandoned)) * Math.Max(1, endOff), off.Sum(p => p.End(EndReason.Abandoned)) * Math.Max(1, endOn), "a smaller share of contracts ends Abandoned");
            Assert.AreEqual(0, on.Sum(p => p.StoreThefts), "no withdrawal from an asset store is classified as theft");
        }
    }
}
