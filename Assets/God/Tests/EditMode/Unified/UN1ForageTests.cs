using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Unified;
using NUnit.Framework;
using TKind = God.Sim.Unified.TargetKind;

namespace God.Sim.Tests
{
    /// <summary>
    /// UN1b/UN1c (spec section 13): forage, water, the dice allowance and its gradual materialisation, the environment books, the seven
    /// need channels, the switches and save/load. Fixtures drive the effect handlers through <c>EffectRegistry</c> with origin Fixture and
    /// advance the real sim; the collection time is counted by the fixture exactly as the executor (UN2) will. OWNER: UN1.
    /// </summary>
    public class UN1ForageTests
    {
        static GodSim Game(ulong seed, Action<SimTuning> tune = null, int npcs = 3, int factions = 2) => UN1WorldTests.Game(seed, 4, tune, npcs, factions);

        static GodSim Game0(ulong seed)
        {
            var setup = new GameSetup(seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 3, Victory = VictorySpec.SandboxOnly() };
            return GodSim.Create(setup, new SimTuning());
        }

        static Npc Person(GodState s, int k = 0) => s.Population.Npcs.Where(Bodies.IsLivingPerson).ElementAt(k);

        static void Free(GodState s, Npc n)
        {
            for (int i = 0; i < n.Carry.Length; i++)
                if (!n.Carry[i].Empty) Assert.IsTrue(Goods.Move(s, null, Holder.Carry(n.Id), n.Carry[i].Id, n.Carry[i].Count, Holder.Ground(n.Node), MoveReason.Drop, n.Id).Ok);
        }

        static void Put(Npc n, int node) { n.Node = node; n.Path = null; n.PathIdx = 0; }

        static EffectCall Call(Npc n, EffectKind k, TKind tk, int id, int ticks, int a = 0, int reach = 1)
        {
            var row = new InteractionDef(tk, CatId.None, 0, SourceReq.EmptyHand, 0, (byte)reach, ticks, new EffectDef(k, (short)a));
            return new EffectCall { Actor = n.Id, Control = new PrimitiveControl { Control = Prim.Use, Actor = n.Id, Target = new TargetRef(tk, id) }, Origin = ControlOrigin.Fixture, Row = row, Effect = row.Effects[0], Target = new TargetRef(tk, id) };
        }

        static readonly Ware[] Foods = { Ware.Mushroom, Ware.Roots, Ware.WildEgg, Ware.Rabbit, Ware.WildVeg, Ware.CactusFruit, Ware.Seaweed };

        static ForagePlace First(GodState s, Ware w) => s.Unification.ForagePlaces.First(p => p.Ware == (short)w);

        static ulong WorldHash(GodState s)
        {
            var h = new StateHasher(); var u = s.Unification;
            foreach (var x in u.ForagePlaces) x.AddTo(ref h);
            foreach (var x in u.WaterPoints) x.AddTo(ref h);
            foreach (var x in u.RawNodes) x.AddTo(ref h);
            foreach (var x in u.Supply) x.AddTo(ref h);
            return h.Value;
        }

        static void Clean(GodState s)
        {
            var bad = new List<string>(); UnifiedInvariants.Check(s, bad);
            Assert.AreEqual(0, bad.Count, string.Join(";", bad));
        }

        // ================================================================ forage

        [Test]
        public void Forage_NoBuildingNeeded_TakesTime()
        {
            var sim = Game(1); var s = sim.State;
            Assert.AreEqual(s.Territory.Hqs.Count, s.Territory.Buildings.Count, "no building but the start halls");
            Assert.AreEqual(30, First(s, Ware.Mushroom).Ticks);
            Assert.AreEqual(3 * First(s, Ware.Mushroom).Ticks, First(s, Ware.Rabbit).Ticks, "rabbit takes three times the base time");
            var n = Person(s);
            foreach (var w in Foods)
            {
                var place = First(s, w);
                Free(s, n); Put(n, place.Node);
                var call = Call(n, EffectKind.Forage, TKind.ForagePlace, place.Id, place.Ticks);
                Assert.AreEqual(MotorStatus.Accepted, EffectRegistry.Validate(s, in call), w.ToString());
                long t0 = s.Tick;
                for (int k = 0; k < place.Ticks; k++) sim.Step();
                Put(n, place.Node); // the fixture holds the actor still
                Assert.AreEqual(place.Ticks, s.Tick - t0, "the collection takes exactly Ticks ticks");
                var ev = new EventBuffer { CurrentTick = s.Tick };
                Assert.AreEqual(MotorStatus.Accepted, EffectRegistry.Apply(s, ev, in call), w.ToString());
                Assert.AreEqual(1, PopulationModule.CountOf(n, w), "one lot of " + w);
                var lot = n.Carry.First(l => !l.Empty && l.Ware == w);
                Assert.IsTrue(UnifiedGoods.OriginOf(s, lot.Id, out var o));
                Assert.AreEqual((byte)ProduceReason.Foraged, o.Reason); Assert.AreEqual(OriginSource.ForagePlace, o.SourceKind); Assert.AreEqual(place.Id, o.SourceId);
                Assert.AreEqual(1, ev.Items.OfType<ForageCollected>().Count());
                Assert.IsTrue(lot.Owner.IsNone, "no title");
            }
            Clean(s);
        }

        [Test]
        public void Forage_RefusesForPhysicalReasonsOnly()
        {
            var sim = Game(1); var s = sim.State; var n = Person(s);
            var place = First(s, Ware.Roots);
            Put(n, place.Node); Free(s, n);
            var c = Call(n, EffectKind.Forage, TKind.ForagePlace, place.Id, 45);
            Assert.AreEqual(MotorStatus.Accepted, EffectRegistry.Validate(s, in c));
            var far = s.Unification.ForagePlaces.First(p => s.Territory.Grid.Distance(p.Node, place.Node) > 5);
            var cf = Call(n, EffectKind.Forage, TKind.ForagePlace, far.Id, 45);
            Assert.AreEqual(MotorStatus.OutOfReach, EffectRegistry.Validate(s, in cf));
            var cb = Call(n, EffectKind.Forage, TKind.ForagePlace, 999999, 45);
            Assert.AreEqual(MotorStatus.Invalid, EffectRegistry.Validate(s, in cb));
            int guard = 0;
            while (EffectRegistry.Validate(s, in c) == MotorStatus.Accepted && guard++ < 50) Assert.AreEqual(MotorStatus.Accepted, EffectRegistry.Apply(s, new EventBuffer(), in c));
            Assert.AreEqual(MotorStatus.NoRoom, EffectRegistry.Validate(s, in c), "a full carry refuses");
            long before = s.Population.Ledger.ProducedBy(ProduceReason.Foraged, Ware.Roots);
            Assert.AreEqual(MotorStatus.NoRoom, EffectRegistry.Apply(s, new EventBuffer(), in c));
            Assert.AreEqual(before, s.Population.Ledger.ProducedBy(ProduceReason.Foraged, Ware.Roots), "nothing produced on a refusal: never dropped silently");
        }

        [Test]
        public void Forage_NeverDepletes()
        {
            var sim = Game(2); var s = sim.State; var n = Person(s);
            var place = First(s, Ware.Mushroom);
            Put(n, place.Node); Free(s, n);
            ulong before = WorldHash(s);
            var c = Call(n, EffectKind.Forage, TKind.ForagePlace, place.Id, place.Ticks);
            for (int i = 0; i < 10000; i++)
            {
                Assert.AreEqual(MotorStatus.Accepted, EffectRegistry.Apply(s, new EventBuffer(), in c), "act " + i);
                var lot = n.Carry.First(l => !l.Empty);
                Assert.IsTrue(Goods.Move(s, null, Holder.Carry(n.Id), lot.Id, lot.Count, Holder.Ground(n.Node), MoveReason.Drop, n.Id).Ok);
            }
            Assert.AreEqual(before, WorldHash(s), "no place, node or book changed");
            Assert.AreEqual(0, s.Population.Sources.Count, "no ResourceSource exists");
            Assert.AreEqual(10000, s.Population.Ledger.ProducedBy(ProduceReason.Foraged, Ware.Mushroom));
            Assert.AreEqual(10000, s.Unification.ForagedEver[HexTypes.ForageIndex(Ware.Mushroom)]);
            Clean(s);
        }

        [Test]
        public void Forage_SeaweedAndFishOnCoastOnly()
        {
            var sim = Game(3); var s = sim.State; var g = s.Territory.Grid; var b = s.Board;
            var weed = s.Unification.ForagePlaces.Where(p => p.Ware == (short)Ware.Seaweed).ToList();
            Assert.Greater(weed.Count, 0);
            foreach (var p in weed) Assert.IsTrue(UnifiedBoard.IsCoast(g, p.Node), "seaweed place " + p.Id + " is on a coast node");
            for (int h = 0; h < b.HexTerrain.Length; h++)
            {
                if (s.Unification.HexKinds[h] != HexType.Sea) continue;
                var shore = UnifiedBoard.ShoreNodes(b, g, h);
                if (shore.Count > 0) Assert.IsTrue(weed.Any(p => shore.Contains(p.Node)), "sea hex " + h + " offers seaweed at its shore");
            }
            var n = Person(s);
            int inland = Enumerable.Range(0, g.Count).First(v => g.IsLand(v) && weed.All(p => g.Distance(v, p.Node) > 6));
            Put(n, inland); Free(s, n);
            var c = Call(n, EffectKind.Forage, TKind.ForagePlace, weed[0].Id, 35);
            Assert.AreEqual(MotorStatus.OutOfReach, EffectRegistry.Validate(s, in c), "a body away from the coast cannot reach seaweed");
            var fish = s.Unification.RawNodes.Where(r => r.Ware == (short)Ware.Fish).ToList();
            Assert.Greater(fish.Count, 0);
            foreach (var r in fish) Assert.IsTrue(UnifiedBoard.IsCoast(g, r.Node), "fish node " + r.Id + " is on a coast node");
        }

        [Test]
        public void Forage_LedgerAndNoLegacySources()
        {
            var sim = Game(4); var s = sim.State;
            for (int i = 0; i < 300; i++) sim.Step();
            Assert.AreEqual(0, s.Population.Sources.Count, "PopulationState.Sources is empty in a unified world");
            var n = Person(s);
            foreach (var w in Foods)
            {
                var pl = First(s, w); Free(s, n); Put(n, pl.Node);
                var c = Call(n, EffectKind.Forage, TKind.ForagePlace, pl.Id, pl.Ticks);
                Assert.AreEqual(MotorStatus.Accepted, EffectRegistry.Apply(s, new EventBuffer(), in c));
            }
            var l = s.Population.Ledger;
            foreach (var w in Foods) { if (w != Ware.Roots) Assert.AreEqual(1, l.ProducedOf(w), w + ": every unit of a new food comes from reason Foraged (Roots also come with the legacy founders until UN5)"); Assert.AreEqual(1, l.ProducedBy(ProduceReason.Foraged, w)); }
            Assert.AreEqual(0, l.ProducedBy(ProduceReason.HandGathered, Ware.Log), "no hand-gathered wood");
            Clean(s);
        }

        // ================================================================ water

        [Test]
        public void Water_FreshSaltSpring()
        {
            var sim = Game(5); var s = sim.State; var u = s.Unification; var g = s.Territory.Grid;
            var fresh = new HashSet<int>(u.WaterPoints.Where(w => w.Fresh).Select(w => w.Node));
            var nodesOf = new Dictionary<int, List<int>>();
            for (int v = 0; v < g.Count; v++) { int h = g.HexOf(v); if (h < 0) continue; if (!nodesOf.ContainsKey(h)) nodesOf[h] = new List<int>(); nodesOf[h].Add(v); }
            int springs = 0;
            foreach (int h in nodesOf.Keys.OrderBy(x => x))
            {
                var type = u.HexKinds[h]; var nodes = nodesOf[h];
                if (type == HexType.Wetland) foreach (int v in nodes) Assert.IsTrue(fresh.Contains(v), "wetland node fresh");
                else if (type == HexType.Forest || type == HexType.Mountain || type == HexType.Pastures || type == HexType.Fields) { Assert.AreEqual(1, nodes.Count(fresh.Contains), "exactly one spring in " + type + " hex " + h); springs++; }
                else if (type == HexType.Desert) Assert.AreEqual(0, nodes.Count(fresh.Contains), "no fresh water on a desert hex");
            }
            Assert.Greater(springs, 0);
            foreach (var w in u.WaterPoints.Where(w => !w.Fresh)) Assert.IsTrue(UnifiedBoard.IsCoast(g, w.Node), "salt water on a coast node only");
            for (int v = 0; v < g.Count; v++)
                if (UnifiedBoard.IsCoast(g, v) && u.HexKinds[g.HexOf(v)] != HexType.Wetland) Assert.IsNotNull(World.WaterAt(u, v), "every coast node carries water (salt unless wetland)");
            var n = Person(s);
            var salt = u.WaterPoints.First(w => !w.Fresh); var spring = u.WaterPoints.First(w => w.Fresh);
            var up = u.PersonOf(n.Id);
            up.Needs[0] = 700; up.Needs[1] = 700;
            Free(s, n);
            long produced = s.Population.Ledger.Produced.Sum(); int lots = s.Population.Lots.Count;
            Put(n, salt.Node);
            var drinkSalt = Call(n, EffectKind.DrinkWash, TKind.WaterPoint, salt.Id, 20, DrinkWashHandler.Drink);
            Assert.AreEqual(MotorStatus.NoInteraction, EffectRegistry.Validate(s, in drinkSalt), "salt water cannot be drunk");
            var washSalt = Call(n, EffectKind.DrinkWash, TKind.WaterPoint, salt.Id, 20, DrinkWashHandler.Wash);
            var ev = new EventBuffer();
            Assert.AreEqual(MotorStatus.Accepted, EffectRegistry.Apply(s, ev, in washSalt));
            Assert.AreEqual(700 - s.Tuning.Unification.UN1.WashRelief, up.Needs[1], "wash lowers Hygiene"); Assert.AreEqual(700, up.Needs[0], "and only Hygiene");
            Put(n, spring.Node);
            var drink = Call(n, EffectKind.DrinkWash, TKind.WaterPoint, spring.Id, 20, DrinkWashHandler.Drink);
            Assert.AreEqual(MotorStatus.Accepted, EffectRegistry.Apply(s, ev, in drink));
            Assert.AreEqual(700 - s.Tuning.Unification.UN1.DrinkRelief, up.Needs[0], "drink lowers Thirst by the configured amount");
            Assert.AreEqual(2, ev.Items.OfType<WaterUsed>().Count());
            Assert.AreEqual(produced, s.Population.Ledger.Produced.Sum(), "no lot is created"); Assert.AreEqual(lots, s.Population.Lots.Count);
            Assert.IsTrue(n.Carry.All(l => l.Empty), "nothing is carried: no container needed");
        }

        [Test]
        public void World_StartReachAudit()
        {
            int worst = 0, minTypes = 99;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var sim = Game(seed, npcs: 1, factions: 6); var s = sim.State;
                foreach (var hq in s.Territory.Hqs)
                {
                    var r = World.ReachAudit(s, hq.Node);
                    Assert.IsTrue(r.FreshWater, "seed " + seed + ": fresh water within reach of start corner " + hq.Corner);
                    Assert.GreaterOrEqual(r.ForageTypes, 2, "seed " + seed + ": at least two forage types within reach");
                    worst = Math.Max(worst, r.FreshDistance); minTypes = Math.Min(minTypes, r.ForageTypes);
                }
            }
            TestContext.Out.WriteLine("120 start corners: worst fresh-water distance " + worst + " steps, fewest forage types in reach " + minTypes);
        }

        // ================================================================ dice and allowance

        [Test]
        public void Dice_StreamMatchesStage3()
        {
            const ulong seed = 11;
            int ticks = 200 * TerritoryTuning.DiceIntervalTicks + 1;
            var legacy = GodSim.Create(new GameSetup(seed));
            var totals = new List<int>();
            for (int i = 0; i < ticks; i++)
            {
                legacy.Step();
                var buf = new List<SimEvent>(); legacy.DrainInto(buf);
                foreach (var e in buf.OfType<DiceRolled>()) totals.Add(e.Total);
            }
            var sim = Game(seed); var s = sim.State;
            var mine = new List<int>(); int seen = 0;
            for (int i = 0; i < ticks; i++)
            {
                sim.Step();
                if (s.Unification.RollCount > seen) { mine.Add(s.Unification.LastTotal); seen = s.Unification.RollCount; }
            }
            Assert.GreaterOrEqual(totals.Count, 200); Assert.GreaterOrEqual(mine.Count, 200);
            for (int i = 0; i < 200; i++) Assert.AreEqual(totals[i], mine[i], "roll " + i);
        }

        [Test]
        public void Allowance_FromRollThenGradual()
        {
            var sim = Game(6); var s = sim.State; var u = s.Unification;
            var probe = new EventProbe(sim);
            AllowanceGranted first = null;
            for (int i = 0; i < 6000 && first == null; i++) { probe.Step(); first = probe.Of<AllowanceGranted>().FirstOrDefault(e => World.SupplyOf(u, e.Hex).Type != (byte)HexType.Pastures); }
            Assert.IsNotNull(first, "a roll matched a hex");
            Assert.AreEqual(0, first.Tick % TerritoryTuning.DiceIntervalTicks, "the allowance is granted at the roll tick");
            Assert.AreEqual(6, first.Amount); Assert.GreaterOrEqual(first.Pending, 6, "added to Pending at the roll tick");
            for (int i = 0; i < 6 * 30 + 400; i++) probe.Step();
            var mat = probe.Of<RawMaterialised>().Where(e => e.Hex == first.Hex).Take(6).ToList();
            Assert.AreEqual(6, mat.Count, "the allowance materialised one unit at a time");
            foreach (var m in mat) { Assert.AreEqual(0, m.Tick % 30, "on the materialisation clock"); Assert.AreEqual(1, m.Units); }
            Assert.IsTrue(mat[0].Tick >= first.Tick && mat[0].Tick - first.Tick <= 30, "the first unit appears within one interval of the roll");
            for (int i = 1; i < 6; i++) Assert.AreEqual(30, mat[i].Tick - mat[i - 1].Tick, "one unit per interval");
        }

        [Test]
        public void Allowance_CapWastesExcess_FullNodeWaits()
        {
            var sim = Game(6, t => { t.Unification.UN1.AllowancePerRoll = 40; t.Unification.UN1.AllowanceCap = 30; t.Unification.UN1.NodesPerHex = 1; t.Unification.UN1.RawNodeCap = 1; });
            var s = sim.State; var u = s.Unification;
            var probe = new EventProbe(sim);
            AllowanceGranted g = null;
            for (int i = 0; i < 6000 && g == null; i++) { probe.Step(); g = probe.Of<AllowanceGranted>().FirstOrDefault(e => World.SupplyOf(u, e.Hex).Type != (byte)HexType.Pastures); }
            Assert.IsNotNull(g);
            Assert.AreEqual(30, g.Amount, "granted up to the cap");
            var w = probe.Of<AllowanceWasted>().First(e => e.Hex == g.Hex);
            Assert.AreEqual(10, w.Amount); Assert.AreEqual(g.Tick, w.Tick);
            for (int i = 0; i < 200; i++) probe.Step();
            var sup = World.SupplyOf(u, g.Hex);
            var node = World.RawNodeById(u, sup.NodeFirst);
            Assert.AreEqual(1, node.Stock, "a full node holds its cap");
            Assert.Greater(sup.Pending, 0, "the rest waits");
            int pending = sup.Pending;
            Assert.AreEqual(1, World.TakeRaw(s, node.Id, 1));
            for (int i = 0; i < 30; i++) probe.Step();
            Assert.AreEqual(1, node.Stock, "the next unit appears after a take");
            Assert.Less(sup.Pending, pending);
        }

        [Test]
        public void Geo_Conservation()
        {
            foreach (ulong seed in new ulong[] { 1, 2, 3 })
            {
                var sim = Game(seed); var s = sim.State; var u = s.Unification;
                int day = SimCalendar.Of(s).TicksPerDay;
                int takes = 0;
                for (int i = 0; i < 20 * day; i++)
                {
                    sim.Step();
                    if (i % 97 == 0) { var r = u.RawNodes.FirstOrDefault(x => x.Stock > 0); if (r != null) takes += World.TakeRaw(s, r.Id, 2); }
                    long mat = 0, stock = 0, col = 0;
                    foreach (var h in u.Supply) { Assert.AreEqual(h.IssuedEver, h.Pending + h.Wasted + h.MaterialisedEver, "Geo-1 hex " + h.Hex + " tick " + s.Tick); mat += h.MaterialisedEver; col += h.CollectedEver; }
                    foreach (var r in u.RawNodes) stock += r.Stock;
                    Assert.AreEqual(mat, stock + col, "Geo-2 tick " + s.Tick);
                }
                Assert.Greater(takes, 0);
                Clean(s);
                // Geo-5: the same seed and the same takes give the same books
                var again = Game(seed); var s2 = again.State;
                for (int i = 0; i < 20 * day; i++)
                {
                    again.Step();
                    if (i % 97 == 0) { var r = s2.Unification.RawNodes.FirstOrDefault(x => x.Stock > 0); if (r != null) World.TakeRaw(s2, r.Id, 2); }
                }
                Assert.AreEqual(WorldHash(s), WorldHash(s2), "Geo-5 seed " + seed);
                Assert.AreEqual(sim.StateHash(), again.StateHash());
            }
        }

        [Test]
        public void RawNode_NotPickable_Yet()
        {
            var sim = Game(7); var s = sim.State; var u = s.Unification;
            for (int i = 0; i < 3 * 1200; i++) sim.Step();
            Assert.Greater(u.RawNodes.Sum(r => r.Stock), 0, "raw stock exists");
            var n = Person(s);
            foreach (var r in u.RawNodes)
            {
                Put(n, r.Node);
                Assert.IsFalse(Goods.Move(s, null, Holder.Ground(r.Node), r.Id + 5000000, 1, Holder.Carry(n.Id), MoveReason.PickUp, n.Id).Ok, "a raw node is not a lot");
            }
            foreach (var w in new[] { Ware.Log, Ware.Clay, Ware.Stone, Ware.Fish, Ware.Hay, Ware.Sheep })
            {
                Assert.AreEqual(0, s.Population.Ledger.ProducedBy(ProduceReason.Foraged, w), w + " is never foraged");
                Assert.IsFalse(u.ForagePlaces.Any(p => p.Ware == (short)w), w + " has no forage place");
            }
            Assert.IsFalse(s.Population.Lots.Any(g => g.Lot.Ware == Ware.Hay || g.Lot.Ware == Ware.Sheep), "no raw lot on the ground");
        }

        [Test]
        public void Sea_FishAtShoreOnly_Desert_NoRaw_Sheep_HeadAllowance()
        {
            var sim = Game(8); var s = sim.State; var u = s.Unification; var g = s.Territory.Grid;
            foreach (var r in u.RawNodes.Where(r => r.Ware == (short)Ware.Fish)) { Assert.AreEqual(HexType.Sea, u.HexKinds[r.Hex]); Assert.IsTrue(UnifiedBoard.IsCoast(g, r.Node)); }
            Assert.IsFalse(u.Supply.Any(x => x.Type == (byte)HexType.Desert), "Desert has no raw material");
            Assert.IsFalse(u.RawNodes.Any(r => u.HexKinds[r.Hex] == HexType.Desert));
            var probe = new EventProbe(sim);
            for (int i = 0; i < 12 * 1200 && !probe.Of<RawMaterialised>().Any(e => e.Ware == Ware.Sheep); i++) probe.Step();
            var granted = probe.Of<AllowanceGranted>().FirstOrDefault(e => u.HexKinds[e.Hex] == HexType.Pastures);
            Assert.IsNotNull(granted, "a pastures hex was rolled");
            Assert.AreEqual(2, granted.Amount, "sheep allowance in head");
            Assert.IsTrue(probe.Of<RawMaterialised>().Any(e => e.Ware == Ware.Sheep), "sheep materialise as raw stock");
        }

        // ================================================================ needs

        [Test]
        public void Needs_RatesBoundedAndDeterministic()
        {
            var a = Game(9); var b = Game(9);
            var sa = a.State;
            var t = sa.Tuning.Unification.UN1;
            int day = SimCalendar.Of(sa).TicksPerDay;
            var np = Person(sa);
            for (int i = 0; i < day; i++) { a.Step(); b.Step(); }
            Assert.AreEqual(a.StateHash(), b.StateHash(), "identical on replay");
            var up = sa.Unification.PersonOf(np.Id);
            var rates = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                int m = Needs.Multiplier(sa, np, i);
                Assert.GreaterOrEqual(m, 1000 - t.NeedVariationPermille, "lower bound, channel " + i);
                Assert.LessOrEqual(m, 1000 + t.NeedVariationPermille + t.ChildExtraPermille, "upper bound, channel " + i);
                Assert.GreaterOrEqual(up.Needs[i], 0); Assert.LessOrEqual(up.Needs[i], 1000);
                // asleep time runs slower, so the awake-rate figure is an upper bound and half of it a lower bound (Comfort and Room never slow)
                int full = Math.Min(1000, t.NeedRatesPerDay[i] * m / 1000);
                Assert.LessOrEqual(up.Needs[i], full + 2, "channel " + i + " does not outrun its rate");
                Assert.GreaterOrEqual(up.Needs[i], full * t.NeedSleepPermille / 1000 - 2, "channel " + i + " moved at least at the sleeping rate");
                rates.Add(((NeedChannel)(i + 5)) + " " + t.NeedRatesPerDay[i] + "/day x" + m + " -> " + up.Needs[i]);
            }
            TestContext.Out.WriteLine(string.Join("; ", rates));
            var before = (short[])up.Needs.Clone(); var drives = (int[])np.Drives.Clone();
            Assert.IsTrue(Needs.Apply(sa, np, NeedChannel.Comfort, -50));
            for (int i = 0; i < 7; i++) if (i + 5 != (int)NeedChannel.Comfort) Assert.AreEqual(before[i], up.Needs[i], "only the named channel changes");
            Assert.AreEqual(Math.Max(0, before[(int)NeedChannel.Comfort - 5] - 50), up.NeedOf(NeedChannel.Comfort));
            Assert.AreEqual(drives, np.Drives, "the five drives are untouched");
            Needs.Apply(sa, np, NeedChannel.Bladder, -5000); Assert.AreEqual(0, up.NeedOf(NeedChannel.Bladder), "clamped at 0");
            Needs.Apply(sa, np, NeedChannel.Bladder, +5000); Assert.AreEqual(1000, up.NeedOf(NeedChannel.Bladder), "clamped at 1000");
        }

        [Test]
        public void Needs_ActiveMaskAndHealthCoupling()
        {
            const int driveOnly = 0x01F, noThirstMask = 0xFFF & ~(1 << (int)NeedChannel.Thirst);
            const int day = 1200;
            var off = Game(10, t => t.Unification.Needs.ActiveMask = driveOnly);
            var noThirst = Game(10, t => t.Unification.Needs.ActiveMask = noThirstMask);
            var thirsty = Game(10);
            for (int i = 0; i < 10 * day; i++) { off.Step(); noThirst.Step(); thirsty.Step(); }
            var so = off.State; var sn = noThirst.State; var st = thirsty.State;
            foreach (var n in so.Population.Npcs.Where(Bodies.IsPerson))
            {
                var nn = sn.Population.Find(n.Id);
                Assert.AreEqual(n.Health, nn.Health, "person " + n.Id + ": Hygiene, Bladder, Social, Fun, Comfort, Room at maximum for 10 days change no health");
                Assert.AreEqual(n.Alive, nn.Alive);
            }
            Assert.AreEqual(1000, sn.Unification.People.Max(p => p.NeedOf(NeedChannel.Social)), "the six channels reached their maximum");
            foreach (var p in sn.Unification.People) Assert.AreEqual(0, p.NeedOf(NeedChannel.Thirst), "an inactive channel stays 0");
            foreach (var p in so.Unification.People) for (int i = 0; i < 7; i++) Assert.AreEqual(0, p.Needs[i], "all extension channels held at 0 when inactive");
            int worse = 0;
            foreach (var n in so.Population.Npcs.Where(Bodies.IsPerson)) { var tt = st.Population.Find(n.Id); if (!tt.Alive || tt.Health < n.Health) worse++; }
            Assert.Greater(worse, 0, "Thirst at maximum for days reduces health");
            var h = Game(10, t => t.Unification.Needs.ActiveMask = driveOnly);
            var hn = Person(h.State); hn.Drives[Drive.Hunger] = 1000000; hn.Nutrition = 0;
            int hp = hn.Health;
            for (int i = 0; i < 100; i++) h.Step();
            Assert.Less(hn.Health, hp, "hunger at maximum still wears health down");
        }

        // ================================================================ switches

        [Test]
        public void UN1_Switches_And_Neutral()
        {
            var off = Game0(12);
            Assert.IsNull(off.State.Unification);
            Assert.AreEqual(BoardShape.SixPlayer, off.State.Board.Shape);
            Assert.IsTrue(off.State.Population.Sources.Count > 0, "legacy sources exist with the switch off");
            var noWorld = Game(12, t => t.Unification.World = 0); var s = noWorld.State;
            Assert.AreEqual(0, s.Unification.ForagePlaces.Count, "World = 0 generates nothing");
            Assert.IsTrue(s.Population.Sources.Count > 0, "and keeps the legacy sources");
            var n = Person(s);
            var c = Call(n, EffectKind.Forage, TKind.ForagePlace, 1, 30);
            Assert.AreEqual(MotorStatus.Disabled, EffectRegistry.Validate(s, in c)); Assert.AreEqual(MotorStatus.Disabled, EffectRegistry.Apply(s, new EventBuffer(), in c));
            var d = Call(n, EffectKind.DrinkWash, TKind.WaterPoint, 1, 20);
            Assert.AreEqual(MotorStatus.Disabled, EffectRegistry.Validate(s, in d));
            for (int i = 0; i < 50; i++) noWorld.Step();
            Assert.AreEqual(0, s.Unification.PersonOf(n.Id).Needs.Sum(x => (int)x), "no needs tick with World = 0");
        }

        // ================================================================ save and load

        [Test]
        public void UN1_SaveLoadParity()
        {
            var sim = Game(13); var s = sim.State; var u = s.Unification;
            for (int i = 0; i < 4 * 1200 && !(u.Supply.Any(x => x.Pending > 0) && u.RawNodes.Any(r => r.Stock > 0)); i++) sim.Step();
            for (int i = 0; i < 77; i++) sim.Step();
            Assert.IsTrue(u.Supply.Any(x => x.Pending > 0), "allowance pending at the save");
            Assert.IsTrue(u.People.Any(p => p.NeedFrac.Any(f => f != 0)), "needs mid-decay at the save");
            var n = Person(s); var pl = First(s, Ware.Roots); Put(n, pl.Node); Free(s, n);
            Assert.AreEqual(MotorStatus.Accepted, EffectRegistry.Apply(s, new EventBuffer(), Call(n, EffectKind.Forage, TKind.ForagePlace, pl.Id, 45)));
            var loaded = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            Assert.AreEqual(sim.StateHash(), loaded.StateHash(), "the load equals the save");
            Assert.AreEqual(s.Unification.ForagePlaces.Count, loaded.State.Unification.ForagePlaces.Count);
            for (int i = 0; i < 2400; i++)
            {
                sim.Step(); loaded.Step();
                if (i % 100 == 0) Assert.AreEqual(sim.StateHash(), loaded.StateHash(), "checkpoint " + i);
            }
            Assert.AreEqual(sim.StateHash(), loaded.StateHash());
        }

        // ================================================================ scale (D10)

        [Test]
        public void World_Scale_NoAllPairs()
        {
            var t0 = System.Diagnostics.Stopwatch.StartNew();
            var sim = UN1WorldTests.Game(1, 6, t => { t.Unification.Start.Tribes = 6; t.Unification.UN6.Decide = 0; /* world and needs only: the brain's cost has its own benchmarks */ }, npcs: 200, factions: 6);
            var s = sim.State; var u = s.Unification;
            t0.Stop();
            int persons = s.Population.Npcs.Count(Bodies.IsLivingPerson);
            Assert.GreaterOrEqual(persons, 1200);
            // world generation alone, repeated on an emptied world
            u.ForagePlaces.Clear(); u.WaterPoints.Clear(); u.RawNodes.Clear(); u.Supply.Clear();
            var gen = System.Diagnostics.Stopwatch.StartNew();
            World.Generate(s);
            gen.Stop();
            Assert.Less(gen.ElapsedMilliseconds, 200, "world generation at radius 6");
            // the per-tick cost of the world and of every person's needs, with no event buffer (no allocation expected)
            var people = s.Population.Npcs.Where(Bodies.IsLivingPerson).ToList();
            for (int i = 0; i < 50; i++) { s.Tick++; UnifiedHooks.Tick(s, null); foreach (var n in people) UnifiedHooks.PersonTick(s, null, n); }
            long a0 = GC.GetAllocatedBytesForCurrentThread();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            const int ticks = 600;
            for (int i = 0; i < ticks; i++) { s.Tick++; UnifiedHooks.Tick(s, null); foreach (var n in people) UnifiedHooks.PersonTick(s, null, n); }
            sw.Stop();
            long alloc = GC.GetAllocatedBytesForCurrentThread() - a0;
            double ms = sw.Elapsed.TotalMilliseconds / ticks;
            TestContext.Out.WriteLine("radius 6, " + persons + " persons: Create " + t0.ElapsedMilliseconds + " ms, World.Generate " + gen.ElapsedMilliseconds + " ms, forage places " + u.ForagePlaces.Count + ", water points " + u.WaterPoints.Count + ", raw nodes " + u.RawNodes.Count + "; TickWorld + 1200 person needs " + ms.ToString("F3") + " ms/tick, allocated " + alloc + " bytes over " + ticks + " ticks");
            Assert.Less(ms, 0.5, "TickWorld plus the needs of 1,200 persons per tick");
            Assert.Less(alloc, 4096, "no per-tick allocation on the hot path");
        }
    }
}
