using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Sim.Divinity;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>D3 (spec 14): the miracles change authoritative state genuinely, through Goods and the Kill path, with an exact ledger. OWNER: D3.</summary>
    public class D3MiracleTests
    {
        internal const int Day = 1200;

        /// <summary>A quiet population game with gods (god 1 human, Mythic plentiful), nothing placed, M4 on.</summary>
        internal static GodSim Game(ulong seed, Action<SimTuning> tune = null, bool m4 = true)
        {
            var t = new SimTuning();
            t.Population.HandGatherPiles = false; t.Population.FounderAxePermille = 0;
            t.Ecology.BerriesPerForest = 0; t.Ecology.BerriesPerMeadow = 0; t.Ecology.RootsPerHills = 0; t.Ecology.RootsPerFields = 0;
            t.Ecology.WoodPerForest = 0; t.Ecology.FishPerCoast = 0; t.Ecology.NightshadePerForest = 0;
            t.Divinity.Enabled = 1; t.Divinity.Mythic.Start = 500_000; t.Divinity.Mythic.Max = 1_000_000;
            if (m4) t.Households.Enabled = 1;
            tune?.Invoke(t);
            var setup = new GameSetup(seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 0, Victory = VictorySpec.SandboxOnly(), HumanGod = 1 };
            return GodSim.Create(setup, t);
        }

        internal static Npc Person(GodSim sim, int node) => sim.State.Households != null
            ? HouseholdFixtures.Person(sim, node, 0, 1500)
            : LearningFixtures.Npc(sim, node, InstinctSet.Default, sim.State.Tuning.Learning.DefaultMode);

        internal static List<string> Bad(GodSim sim)
        {
            var l = new List<string>();
            DivinityModule.Check(sim.State, l);
            PopulationInvariants.Check(sim.State, l);
            return l;
        }

        internal static CommandResult Cast(GodSim sim, MiracleKind k, DivineTarget t, int mag, int god = 1) => sim.Submit(new CastMiracle { God = god, Kind = k, Target = t, Magnitude = mag });
        internal static DivineTarget Npc(int id) => new DivineTarget(DivineTargetKind.Npc, id);

        // ---------------------------------------------------------------- D3-T1 (MM6)

        [Test]
        public void Heal_IsGenuine()
        {
            var sim = Game(31);
            int node = LearningFixtures.Glade(sim);
            var n = Person(sim, node);
            n.Injury = 600_000; n.Drives[Drive.Pain] = 600_000; Array.Copy(n.Drives, n.RewardSnap, Drive.Count);
            var probe = new DivineProbe(sim);
            long mythic = sim.State.Divinity.Gods.Of(1).Mythic;
            Assert.IsTrue(Cast(sim, MiracleKind.HealInjury, Npc(n.Id), 400).Ok);
            probe.Step(1);
            Assert.LessOrEqual(n.Injury, 200_000);
            Assert.Greater(n.Injury, 190_000, "injury 200 in state");
            var eff = probe.Of<MiracleEffectApplied>().First(e => e.Field == "injury");
            Assert.AreEqual(600_000, eff.Before); Assert.AreEqual(200_000, eff.After);
            Assert.AreEqual(1, probe.Of<MiracleCast>().Count());
            Assert.AreEqual(mythic - 4_000, sim.State.Divinity.Gods.Of(1).Mythic, "cost 1 per 100 permille, min 3: 4 Mythic");
            Assert.Less(n.DriveOf(Drive.Pain), 300, "pain falls");
            probe.Step(150);
            Assert.Greater(n.Mind.Store.RewardSumDay, 0, "the live trace is paid for the relief");
            CollectionAssert.IsEmpty(Bad(sim));
        }

        // ---------------------------------------------------------------- D3-T2

        [Test]
        public void HealDisease_IsGenuine()
        {
            var sim = Game(32);
            int node = LearningFixtures.Glade(sim);
            var n = Person(sim, node);
            n.Life.Illness = 1; n.Life.Severity = 200; n.Life.IllDaysLeft = 4; n.Life.IllDays = 1; n.Health = 600_000;
            var probe = new DivineProbe(sim);
            Assert.IsTrue(Cast(sim, MiracleKind.HealDisease, Npc(n.Id), 0).Ok);
            probe.Step(1);
            Assert.AreEqual(2, n.Life.Illness); Assert.AreEqual(0, n.Life.Severity);
            Assert.Greater(n.Life.ImmuneUntilDay, 14);
            Assert.GreaterOrEqual(n.Health, 700_000 - 2000);
            Assert.IsTrue(probe.Of<IllnessEnded>().Single().Recovered);
            Assert.AreEqual(Miracles.NoEffect, Cast(sim, MiracleKind.HealDisease, Npc(n.Id), 0).Reason, "no longer ill: refused");
        }

        // ---------------------------------------------------------------- D3-T3 (MM7), T5

        [Test]
        public void Smite_KillsThroughKill_AndWitnessesFear()
        {
            var sim = Game(33);
            int node = LearningFixtures.Glade(sim);
            var v = Person(sim, node);
            v.Health = 500_000;
            Goods.Produce(sim.State, null, Holder.Carry(v.Id), Ware.Fish, 3, 99, 0, Owner.Npc(v.Id), ProduceReason.Placed, 0, 0);
            var w = new[] { Person(sim, node), Person(sim, LearningFixtures.Step(sim, node, 0, 2)), Person(sim, LearningFixtures.Step(sim, node, 1, 3)) };
            var fears = w.Select(x => x.Drives[Drive.Fear]).ToArray();
            var probe = new DivineProbe(sim);
            Assert.IsTrue(Cast(sim, MiracleKind.SmitePerson, Npc(v.Id), 800).Ok);
            probe.Step(1);
            Assert.IsFalse(v.Alive);
            var died = probe.Of<NpcDied>().Single();
            Assert.AreEqual(DeathCause.Violence, died.Cause);
            var eff = probe.Of<MiracleEffectApplied>().Single(e => e.Death);
            Assert.AreEqual(v.Id, eff.ThingId); Assert.AreEqual(DeathCause.Violence, eff.Cause);
            Assert.AreEqual(1, sim.State.Population.Stats.DeathsByCause[(int)DeathCause.Violence]);
            Assert.AreEqual(3, sim.State.Population.Lots.Where(l => l.Lot.Ware == Ware.Fish).Sum(l => l.Lot.Count) + EstateCount(sim, Ware.Fish), "the fish are on the ground or in the estate");
            Assert.AreEqual(3, probe.Of<MiracleEffectApplied>().Count(e => e.ThingKind == MiracleThing.Witness));
            for (int i = 0; i < w.Length; i++) Assert.Greater(w[i].Drives[Drive.Fear], fears[i]);
            CollectionAssert.IsEmpty(Bad(sim));
        }

        static int EstateCount(GodSim sim, Ware w) => sim.State.Population.Bins.Sum(b => b.Lots.Where(l => l.Ware == w).Sum(l => l.Count));

        // ---------------------------------------------------------------- D3-T6 (MM8)

        [Test]
        public void CreateDestroy_Reconcile_AndSaveLoad()
        {
            var sim = Game(34);
            int node = LearningFixtures.Glade(sim);
            var taker = Person(sim, node);
            var probe = new DivineProbe(sim);
            Assert.IsTrue(Cast(sim, MiracleKind.CreateResource, new DivineTarget(DivineTargetKind.Location, 0, node), Miracles.PackCreate(Ware.Berries, 5)).Ok);
            probe.Step(1);
            var lot = sim.State.Population.Lots.Single(l => l.Lot.Ware == Ware.Berries);
            Assert.AreEqual(5, lot.Lot.Count);
            var r = Goods.Move(sim.State, null, Holder.Ground(node), lot.Lot.Id, 3, Holder.Carry(taker.Id), MoveReason.PickUp, taker.Id, OwnerRule.ToReceiver);
            Assert.IsTrue(r.Ok);
            int left = sim.State.Population.Lots.Single(l => l.Lot.Ware == Ware.Berries).Lot.Id;
            Assert.IsTrue(Cast(sim, MiracleKind.DestroyResource, new DivineTarget(DivineTargetKind.Lot, left), 2).Ok);
            probe.Step(1);
            var led = sim.State.Population.Ledger;
            Assert.AreEqual(5, led.ProducedBy(ProduceReason.Miracle, Ware.Berries));
            Assert.AreEqual(2, led.ConsumedBy(ConsumeReason.Miracle, Ware.Berries));
            Assert.AreEqual(5, probe.Of<ResourceCreated>().Sum(e => e.Count));
            Assert.AreEqual(2, probe.Of<ResourceDestroyed>().Sum(e => e.Count));
            Assert.AreEqual(3, PopulationModule.CountOf(taker, Ware.Berries));
            CollectionAssert.IsEmpty(Bad(sim));
            var loaded = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            Assert.AreEqual(sim.StateHash(), loaded.StateHash());
            CollectionAssert.IsEmpty(Bad(loaded));
            sim.Step(50); loaded.Step(50);
            Assert.AreEqual(sim.StateHash(), loaded.StateHash());
        }

        [Test]
        public void Create_IntoCarry_OwnerMiracle_NoReward()
        {
            var sim = Game(35);
            var n = Person(sim, LearningFixtures.Glade(sim));
            var probe = new DivineProbe(sim);
            var rewardBefore = n.Mind.Store.RewardSumDay;
            Assert.IsTrue(Cast(sim, MiracleKind.CreateResource, Npc(n.Id), Miracles.PackCreate(Ware.Fish, 2)).Ok);
            probe.Step(1);
            var lot = n.Carry.Single(l => !l.Empty && l.Ware == Ware.Fish);
            Assert.AreEqual(OwnerKind.Npc, lot.Owner.Kind); Assert.AreEqual(n.Id, lot.Owner.Id);
            Assert.IsTrue(probe.Of<OwnershipChanged>().Any(o => o.Basis == OwnershipBasis.Miracle && o.ToId == n.Id));
            Assert.AreEqual(rewardBefore, n.Mind.Store.RewardSumDay, "no goods reward on receipt");
        }

        // ---------------------------------------------------------------- D3-T8 (MM9)

        [Test]
        public void HutSmite_ScattersStore_OrBurns()
        {
            foreach (bool burn in new[] { false, true })
            {
                var sim = Game(36, t => t.Divinity.Miracles.SmiteBurnsContents = burn ? 1 : 0);
                int node = LearningFixtures.Glade(sim);
                var fam = HouseholdFixtures.Family(sim, node, 2, new int[0], true, (Ware.Roots, 6));
                var hut = sim.State.Population.Huts.Single();
                var people = fam.Adults.Select(id => sim.State.Population.Find(id)).ToList();
                Assert.IsTrue(people.All(p => p.HomeHut == hut.Id));
                var probe = new DivineProbe(sim);
                Assert.IsTrue(Cast(sim, MiracleKind.SmiteBuilding, new DivineTarget(DivineTargetKind.Hut, hut.Id), 0).Ok, burn ? "burn" : "scatter");
                probe.Step(1);
                Assert.AreEqual(0, sim.State.Population.Huts.Count);
                Assert.IsTrue(people.All(p => p.HomeHut == 0), "residents lose beds");
                int ground = sim.State.Population.Lots.Where(l => l.Lot.Ware == Ware.Roots && l.Node == node).Sum(l => l.Lot.Count);
                if (burn) { Assert.AreEqual(0, ground); Assert.AreEqual(6, probe.Of<ResourceDestroyed>().Sum(e => e.Count)); }
                else { Assert.AreEqual(6, ground); Assert.AreEqual(0, probe.Of<ResourceDestroyed>().Count()); }
                CollectionAssert.IsEmpty(Bad(sim));
                probe.Step(2 * Day);
                CollectionAssert.IsEmpty(Bad(sim));
            }
        }

        // ---------------------------------------------------------------- D3-T9

        [Test]
        public void SourceSmite_Blights()
        {
            var sim = Game(37);
            int node = LearningFixtures.Glade(sim);
            var src = LearningFixtures.Bush(sim, SourceKind.Berries, node, 60, 80, 400);
            var probe = new DivineProbe(sim);
            Assert.IsTrue(Cast(sim, MiracleKind.SmiteSource, new DivineTarget(DivineTargetKind.Source, src.Id), 40).Ok);
            probe.Step(1);
            var e = probe.Of<MiracleEffectApplied>().First(x => x.Field == "stock");
            Assert.AreEqual(60, e.Before); Assert.AreEqual(20, e.After); Assert.AreEqual(20, src.Stock);
            probe.Step(4 * Day);
            Assert.AreEqual(20, src.Stock, "regrowth is suspended for BlightDays");
            probe.Step(3 * Day);
            Assert.Greater(src.Stock, 20, "and resumes afterwards");
            Assert.AreEqual(0, sim.State.Divinity.Miracles.Blights.Count);
        }

        // ---------------------------------------------------------------- D3-T10, T11

        [Test]
        public void Protect_ReducesDamage_Bless_Recovers()
        {
            var sim = Game(38);
            int node = LearningFixtures.Glade(sim);
            var a = Person(sim, node); var b = Person(sim, LearningFixtures.Step(sim, node, 0, 1));
            Assert.IsTrue(Cast(sim, MiracleKind.Protect, Npc(a.Id), 0).Ok);
            sim.Step();
            Assert.AreEqual(Miracles.AlreadyActive, Cast(sim, MiracleKind.Protect, Npc(a.Id), 0).Reason);
            int ia = a.Injury, ib = b.Injury;
            Bodies.Damage(sim.State, new EventBuffer(), a, 300_000, DamageCause.Violence, 0, 0);
            Bodies.Damage(sim.State, new EventBuffer(), b, 300_000, DamageCause.Violence, 0, 0);
            Assert.AreEqual(90_000, a.Injury - ia, "injury rises by 90");
            Assert.AreEqual(300_000, b.Injury - ib);
            Assert.IsTrue(Cast(sim, MiracleKind.Bless, Npc(a.Id), 0).Ok);
            a.Health = 500_000; b.Health = 500_000; a.Nutrition = b.Nutrition = 900_000;
            sim.Step(2 * Day);
            Assert.Greater(a.Health, b.Health, "blessed recovers more");
            CollectionAssert.IsEmpty(Bad(sim));
        }

        // ---------------------------------------------------------------- D3-T12, T14

        [Test]
        public void NothingChargedWithoutEffect_AndEstateSafe()
        {
            var sim = Game(39);
            var n = Person(sim, LearningFixtures.Glade(sim));
            n.Injury = 100_000;
            long mythic = sim.State.Divinity.Gods.Of(1).Mythic;
            PopulationModule.Kill(sim.State, new EventBuffer(), n, DeathCause.Accident);
            ulong h = sim.StateHash();
            Assert.AreEqual(DivineReasons.TargetDead, Cast(sim, MiracleKind.HealInjury, Npc(n.Id), 100).Reason);
            Assert.AreEqual(mythic, sim.State.Divinity.Gods.Of(1).Mythic);
            Assert.AreEqual(h, sim.StateHash());
            var m2 = Person(sim, LearningFixtures.Glade(sim));
            Assert.AreEqual(Miracles.NoEffect, Cast(sim, MiracleKind.HealInjury, Npc(m2.Id), 100).Reason, "nothing to heal");
            Assert.AreEqual(DivineReasons.ActUnavailable, Cast(sim, MiracleKind.RevealTechnology, Npc(m2.Id), 1).Reason);
            foreach (var b in sim.State.Population.Bins.Where(b => b.Kind == BinKind.Estate))
            {
                Assert.AreEqual(DivineReasons.BadTarget, Cast(sim, MiracleKind.DestroyResource, new DivineTarget(DivineTargetKind.Bin, b.Id), 1).Reason);
                foreach (var l in b.Lots) Assert.AreEqual(DivineReasons.BadTarget, Cast(sim, MiracleKind.DestroyResource, new DivineTarget(DivineTargetKind.Lot, l.Id), 1).Reason);
            }
        }

        // ---------------------------------------------------------------- T17

        [Test]
        public void Consecrate_SpacingAndShrine()
        {
            var sim = Game(40);
            int node = LearningFixtures.Glade(sim);
            Assert.IsTrue(Cast(sim, MiracleKind.Consecrate, new DivineTarget(DivineTargetKind.Location, 0, node), 0).Ok);
            sim.Step();
            Assert.AreEqual(1, sim.State.Divinity.Miracles.Places.Count);
            var seen = new Cognition.Sensed[64];
            var npc = Person(sim, LearningFixtures.Step(sim, node, 0, 2));
            int n = DivinityHooks.SenseExtra(sim.State, npc, 10, sim.State.Territory.Grid, seen, 0);
            Assert.AreEqual(1, n); Assert.AreEqual(ObjClass.Shrine, seen[0].Class);
            sim.State.Divinity.Gods.Of(1).Seat = GodSeat.Ai; sim.State.Divinity.Gods.Of(2).Seat = GodSeat.Human; // test-only seat switch
            var other = new CastMiracle { God = 2, Kind = MiracleKind.Consecrate, Target = new DivineTarget(DivineTargetKind.Location, 0, LearningFixtures.Step(sim, node, 0, 3)) };
            Assert.AreEqual(Miracles.TooClose, sim.Check(other).Reason, "a second god's place within SacredSpacing is refused");
        }

        // ---------------------------------------------------------------- T13: the static rule

        [Test]
        public void Static_MiraclesUseGoodsOnly()
        {
            string root = GoodsTests.RepoRoot();
            var files = new List<(string, string)>();
            foreach (var f in Directory.GetFiles(Path.Combine(root, "Assets", "God", "Sim", "Divinity"), "*.cs")) files.Add((f, File.ReadAllText(f)));
            string pm = Path.Combine(root, "Assets", "God", "Sim", "Population", "PopulationMiracles.cs");
            files.Add((pm, File.ReadAllText(pm)));
            Assert.IsEmpty(GoodsTests.GoodsWrites(files));
        }

        // ---------------------------------------------------------------- neutrality

        [Test]
        public void NoMiracle_NoMiracleState()
        {
            var sim = Game(41);
            sim.Step(Day + 5);
            Assert.IsNull(sim.State.Divinity.Miracles);
            Assert.AreEqual(1, sim.State.Divinity.NextLedgerId);
        }
    }
}
