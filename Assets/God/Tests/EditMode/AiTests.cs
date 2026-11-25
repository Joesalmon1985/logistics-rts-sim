using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>P9: the faction AI. Brains play through commands only, on sim ticks, from state alone.</summary>
    public class AiTests
    {
        /// <summary>What one AI game did (counted from the events).</summary>
        public sealed class GameLog
        {
            public ulong Seed;
            public long Ticks;
            public int Wars, Peaces, Captures, Eliminations, Citadels, HqsPlaced, UnitsMade, InvariantChecks;
            /// <summary>Trades: offers sent, accepted, refused, envoys killed (P11).</summary>
            public int Offered, Accepted, Refused, Killed;
            /// <summary>Highest VP each faction reached during the game.</summary>
            public readonly int[] MaxVp = new int[FactionInfo.Max];
            public readonly List<string> Lines = new List<string>();
            public double Minutes => Ticks / 600.0;
        }

        public static string Standings(GodState s)
        {
            var parts = new List<string>();
            foreach (var st in FactionRules.Standings(s))
            {
                int units = s.Military.CountUnits(st.Faction);
                parts.Add(FactionInfo.Name(st.Faction) + " " + st.VictoryPoints + "vp " + st.Hqs + "hq " + st.Citadels + "c " + units + "u"
                    + (st.Eliminated ? " out" : "") + " war[" + string.Join(",", s.Factions.Enemies(st.Faction).Select(f => (int)f)) + "]");
            }
            return string.Join(" | ", parts);
        }

        /// <summary>Six brains (watch mode) until game over or the cap, invariants every 500 ticks.</summary>
        public static (GodSim sim, AiDirector ai, GameLog log) Play(ulong seed, long maxTicks, int logEvery = 0, Action<GodSim> each = null, IAiTrader trader = null)
        {
            var sim = GodSim.Create(GameSetup.Watch(seed));
            var ai = AiDirector.Attach(sim, sim.State.Setup.Factions, null, trader);
            var log = new GameLog { Seed = seed };
            var s = sim.State;
            while (!s.IsGameOver && s.Tick < maxTicks)
            {
                sim.Step();
                foreach (var ev in sim.DrainEvents())
                {
                    switch (ev)
                    {
                        case WarDeclared w: log.Wars++; if (logEvery > 0 && log.Wars < 60) log.Lines.Add("t" + s.Tick + " war " + (int)w.By + "->" + (int)w.Against + " vp " + string.Join(",", Enumerable.Range(0, 6).Select(i => FactionRules.VictoryPoints(s, (Faction)i))) + " str " + string.Join(",", Enumerable.Range(0, 6).Select(i => s.Military.CountUnits((Faction)i)))); break;
                        case PeaceMade p: log.Peaces++; if (logEvery > 0 && log.Peaces < 60) log.Lines.Add("t" + s.Tick + " peace " + (int)p.A + "-" + (int)p.B); break;
                        case HqCaptured _: log.Captures++; break;
                        case FactionEliminated _: log.Eliminations++; break;
                        case CitadelCompleted _: log.Citadels++; break;
                        case HqPlaced _: log.HqsPlaced++; break;
                        case UnitProduced _: log.UnitsMade++; break;
                        case TradeOffered _: log.Offered++; break;
                        case TradeAccepted _: log.Accepted++; break;
                        case TradeRefused _: log.Refused++; break;
                        case EnvoyKilled _: log.Killed++; break;
                    }
                }
                if (s.Tick % 100 == 0)
                    for (int i = 0; i < s.FactionCount; i++) log.MaxVp[i] = Math.Max(log.MaxVp[i], FactionRules.VictoryPoints(s, (Faction)i));
                if (s.Tick % 500 == 0)
                {
                    var bad = SimInvariants.Check(s);
                    Assert.IsEmpty(bad, "seed " + seed + " tick " + s.Tick + ": " + string.Join("; ", bad));
                    log.InvariantChecks++;
                }
                if (logEvery > 0 && s.Tick % logEvery == 0)
                {
                    log.Lines.Add("t" + s.Tick + " " + Standings(s));
                    if (Environment.GetEnvironmentVariable("AI_DESCRIBE") == "1") foreach (var b in ai.Brains) log.Lines.Add("    " + b.Describe());
                }
                each?.Invoke(sim);
            }
            log.Ticks = s.Tick;
            for (int i = 0; i < s.FactionCount; i++) log.MaxVp[i] = Math.Max(log.MaxVp[i], FactionRules.VictoryPoints(s, (Faction)i));
            return (sim, ai, log);
        }


        // ---------------- helpers ----------------

        private static GodSim Solo(ulong seed, Faction f, out AiDirector ai)
        {
            var sim = GodSim.Create(GameSetup.Watch(seed));
            ai = AiDirector.Attach(sim, new[] { f });
            return sim;
        }

        private static void StepUntil(GodSim sim, long maxTicks, Func<bool> done)
        {
            while (sim.State.Tick < maxTicks && !sim.State.IsGameOver && !done())
            {
                sim.Step();
                sim.DrainEvents();
                if (sim.State.Tick % 500 == 0) Assert.IsEmpty(SimInvariants.Check(sim.State), "tick " + sim.State.Tick);
            }
        }

        private static int KindsBuilt(GodState s, Faction f) =>
            s.Economy.Buildings.Where(b => b.Faction == f && b.Built && !b.IsHq).Select(b => b.Kind).Distinct().Count();

        /// <summary>Puts down extra finished HQs for a faction on free legal corners (no reach rule), to set up VP.</summary>
        private static void GiveHqs(GodSim sim, Faction f, int count)
        {
            var s = sim.State; var t = s.Territory; var topo = t.Topology; var g = t.Grid;
            foreach (int c in topo.Corners)
            {
                if (count == 0) break;
                if (topo.VertexHexes(c).Count != 3 || t.HqNearCorner(c)) continue;
                int node = g.CornerNode(c), fn = g.FlagNodeOf(node);
                if (fn < 0 || !g.IsLand(fn) || !t.NodeAt(node).IsFree || !t.NodeAt(fn).IsFree || t.FlagNextTo(fn)) continue;
                TerritoryRules.AddHq(s, f, c, null);
                count--;
            }
            sim.Step(); sim.DrainEvents(); // economy and health records for the new HQs
        }

        private static void Army(GodSim sim, Faction f, int infantry)
        {
            var hq = sim.State.Territory.HqsOf(f)[0];
            for (int i = 0; i < infantry; i++) sim.Submit(new DevSpawnUnit(f, UnitKind.Infantry, hq.Node));
            sim.Step(); sim.DrainEvents();
        }

        private static void ThinkAll(GodSim sim, IEnumerable<AiBrain> brains, int rounds = 1)
        {
            for (int r = 0; r < rounds; r++)
            {
                foreach (var b in brains) b.Think();
                sim.Step(); sim.DrainEvents();
            }
        }

        /// <summary>Brains for set-up war and peace tests: no opening truce (<see cref="AiTuning.WarsFromTick"/> 0), as the tests start at tick 0.</summary>
        private static List<AiBrain> Brains(GodSim sim) => sim.State.Setup.Factions.Select(f => new AiBrain(sim, f, new AiTuning { WarsFromTick = 0 })).ToList();

        // ---------------- economy, expansion, Citadels ----------------

        /// <summary>The basics every AI economy builds before its army (Q4): raws, planks, bricks, bread and a barracks.</summary>
        private static readonly string[] Basics =
        {
            BuildingKinds.Woodcutter, BuildingKinds.Quarry, BuildingKinds.Sawmill, BuildingKinds.ClayPit, BuildingKinds.Kiln,
            BuildingKinds.Farm, BuildingKinds.Bakery, BuildingKinds.Barracks,
        };

        /// <summary>
        /// Q4 gear plan, a lone brain (no trade) on starts with all four lands near: it builds the basics, the armoury of its
        /// planned melee weapon and the barracks, and arms soldiers with planned weapons within 30 minutes. Seed scan (Q4):
        /// alone, Amber has three soldiers by minute 30 on seeds 2-5 and 8-11 (12 to 26 minutes; 4, 5 and 9 by minute 12.5);
        /// on 1, 6, 7 and 12 it runs short of clay or a land and needs trade.
        /// </summary>
        [TestCase(2UL)]
        [TestCase(4UL)]
        [TestCase(5UL)]
        [TestCase(9UL)]
        public void Brain_BuildsItsGearPlanChain_AndAnArmy(ulong seed)
        {
            var sim = Solo(seed, Faction.F0, out var ai);
            var s = sim.State; var brain = ai.BrainOf(Faction.F0);
            bool Built(string k) => s.Economy.Buildings.Any(b => b.Faction == Faction.F0 && b.Built && b.Kind == k);
            StepUntil(sim, 30 * 600, () => Basics.All(Built) && s.Military.CountUnits(Faction.F0) >= 3);
            var kinds = s.Economy.Buildings.Where(b => b.Faction == Faction.F0 && b.Built && !b.IsHq).Select(b => b.Kind).Distinct().ToList();
            string why = "seed " + seed + " built " + string.Join(",", kinds) + " by tick " + s.Tick + "; plan " + string.Join(",", brain.PlannedWeapons);
            CollectionAssert.IsSubsetOf(Basics, kinds, why);
            Assert.AreEqual(2, brain.PlannedWeapons.Count(w => w != Ware.None) >= 2 ? 2 : -1, "a melee and a ranged weapon planned: " + why);
            Assert.IsFalse(Gear.IsRanged(brain.PlannedWeapons[0]), "melee first");
            Assert.IsTrue(Gear.IsRanged(brain.PlannedWeapons[1]), "then ranged");
            Assert.IsTrue(Built(AiBrain.ArmouryOf(brain.PlannedWeapons[0])), "the first weapon's armoury: " + why);
            Assert.GreaterOrEqual(s.Military.CountUnits(Faction.F0), 3, "an army: " + why);
            var made = s.Economy.ProducedLog.Where(u => u.Faction == Faction.F0).ToList();
            Assert.IsTrue(made.All(u => u.Kit.IsArmed && brain.PlannedWeapons.Contains(u.Kit.Weapon)), "armed with planned weapons: " + string.Join(",", made.Select(u => u.Kit.Weapon)));
            foreach (var b in s.Economy.Buildings.Where(b => b.Faction == Faction.F0 && !b.IsHq))
                Assert.AreNotEqual(BuildStatus.SiteNoRoad, b.Status, "joined by road: " + b.Kind + " " + b.Id);
            // The other factions had no brain and sent nothing.
            Assert.IsTrue(s.Territory.Buildings.Where(b => b.Faction != Faction.F0).All(b => b.Size == BuildingSize.Hq));
        }

        /// <summary>
        /// Q4: over a whole six-brain game a faction arms with at least two weapon kinds and adds defence items once its
        /// economy runs; factions on the same island plan different weapons.
        /// </summary>
        [Test]
        public void GearPlans_TwoWeaponsPerFaction_DefenceLater_AndVariety()
        {
            var (sim, ai, _) = Play(4, 60 * 600, 0, null, new AiTrader());
            var s = sim.State;
            var made = s.Economy.ProducedLog;
            int twoKinds = 0, withDefence = 0, armies = 0;
            var firstWeapons = new HashSet<Ware>();
            foreach (var f in s.Setup.Factions)
            {
                var mine = made.Where(u => u.Faction == f).ToList();
                if (mine.Count > 0) armies++;
                if (mine.Select(u => u.Kit.Weapon).Distinct().Count() >= 2) twoKinds++;
                if (mine.Any(u => MilitaryTuning.DefencePieces(u.Kit) > 0)) withDefence++;
                var b = ai.BrainOf(f);
                if (!s.Factions.IsEliminated(f)) { b.WareNeeds(); firstWeapons.Add(b.PlannedWeapons[0]); }
            }
            TestContext.WriteLine("armies " + armies + ", two weapon kinds " + twoKinds + ", defence " + withDefence + ", first weapons " + string.Join(",", firstWeapons));
            Assert.GreaterOrEqual(armies, 4, "most factions get an army");
            Assert.GreaterOrEqual(twoKinds, 4, "most armies carry two weapon kinds or more");
            Assert.GreaterOrEqual(withDefence, 2, "defence items once the economy runs (the stronger economies)");
            Assert.GreaterOrEqual(firstWeapons.Count, 2, "not everyone plans the same melee weapon");
            Assert.IsTrue(made.Any(u => Gear.IsRanged(u.Kit.Weapon)), "bows or slings are made");
        }

        [Test]
        public void KitStrength_GrowsWithGear_ABareSwordIsTheBase()
        {
            Assert.AreEqual(2, AiBrain.KitStrength(new Kit(Ware.Sword), 2));
            Assert.AreEqual(100, AiBrain.KitStrength(new Kit(Ware.Sword), 100));
            int bare = AiBrain.KitStrength(new Kit(Ware.Axe), 100);
            int full = AiBrain.KitStrength(new Kit(Ware.Axe, Ware.SmallShield, Ware.Helmet, Ware.LightArmour), 100);
            Assert.Greater(full, bare * 12 / 10, "a full kit counts clearly more");
            Assert.Less(AiBrain.KitStrength(new Kit(Ware.Bow), 100), 100, "a bare bow counts less than a bare sword in a straight fight");
            Assert.IsTrue(new[] { Ware.Sword, Ware.Spear, Ware.Axe }.All(w => AiBrain.KitStrength(new Kit(w), 100) is int v && v > 70 && v < 140), "bare melee weapons count about the same");
        }

        [Test]
        public void Brain_ExpandsToASecondHq_JoinedByRoad()
        {
            var sim = Solo(5, Faction.F2, out _);
            var s = sim.State; var t = s.Territory;
            StepUntil(sim, 12 * 600, () => t.HqsOf(Faction.F2).Count >= 2);
            var hqs = t.HqsOf(Faction.F2);
            Assert.GreaterOrEqual(hqs.Count, 2, "a second finished HQ by tick " + s.Tick);
            Assert.AreEqual(2, t.Topology.CornerDistance(hqs[0].Corner, hqs[1].Corner));
            Assert.IsNotNull(t.RouteBetweenFlags(Faction.F2, t.BuildingById(hqs[0].Id).FlagId, t.BuildingById(hqs[1].Id).FlagId), "the HQs are joined by road");
        }

        [Test]
        public void Brain_UpgradesCitadels()
        {
            // Stage 3: a Citadel takes steel (forge) as well as bricks, mortar and planks; seed 5's Amber gets one by minute 22 alone.
            var sim = Solo(5, Faction.F0, out _);
            var s = sim.State;
            StepUntil(sim, 60 * 600, () => s.Territory.Hqs.Any(h => h.Faction == Faction.F0 && h.Citadel));
            Assert.IsTrue(s.Territory.Hqs.Any(h => h.Faction == Faction.F0 && h.Citadel), "a Citadel by tick " + s.Tick);
            Assert.GreaterOrEqual(FactionRules.VictoryPoints(s, Faction.F0), 3);
            Assert.IsTrue(s.Economy.Buildings.Any(b => b.Faction == Faction.F0 && b.Kind == BuildingKinds.Forge && b.Built), "steel from a forge");
            Assert.IsTrue(s.Economy.Buildings.Any(b => b.Faction == Faction.F0 && b.Level > 1), "it upgrades harvesters on the way");
        }

        // ---------------- war and peace (owner rules) ----------------

        [Test]
        public void War_IsDeclaredOnALeaderAheadBy2()
        {
            var sim = GodSim.Create(GameSetup.Watch(3));
            var s = sim.State;
            GiveHqs(sim, Faction.F3, 2); // F3: 3 VP, everyone else 1
            foreach (var f in s.Setup.Factions) if (f != Faction.F3) Army(sim, f, 4);
            ThinkAll(sim, Brains(sim));
            Assert.AreEqual(3, FactionRules.VictoryPoints(s, Faction.F3));
            foreach (var f in s.Setup.Factions.Where(f => f != Faction.F3))
            {
                Assert.IsTrue(s.Factions.AtWar(f, Faction.F3), FactionInfo.Name(f) + " is at war with the leader");
                Assert.AreEqual(1, s.Factions.Enemies(f).Count, FactionInfo.Name(f) + " fights only the leader");
            }
        }

        [Test]
        public void War_WaitsForTheOpeningTruce()
        {
            // Q4: the default brains declare nothing in the first 15 minutes, whatever the lead.
            var sim = GodSim.Create(GameSetup.Watch(3));
            var s = sim.State;
            GiveHqs(sim, Faction.F3, 2);
            foreach (var f in s.Setup.Factions) if (f != Faction.F3) Army(sim, f, 4);
            var brains = s.Setup.Factions.Select(f => new AiBrain(sim, f)).ToList();
            Assert.AreEqual(15 * 600, brains[0].Tune.WarsFromTick);
            ThinkAll(sim, brains, 3);
            foreach (var f in s.Setup.Factions) Assert.IsEmpty(s.Factions.Enemies(f), FactionInfo.Name(f) + " keeps the truce");
        }

        [Test]
        public void War_ALeadOfOne_IsNoReason()
        {
            var sim = GodSim.Create(GameSetup.Watch(3));
            var s = sim.State;
            GiveHqs(sim, Faction.F3, 1); // 2 VP against 1
            foreach (var f in s.Setup.Factions) Army(sim, f, 4);
            ThinkAll(sim, Brains(sim), 3);
            foreach (var f in s.Setup.Factions) Assert.IsEmpty(s.Factions.Enemies(f), FactionInfo.Name(f));
        }

        [Test]
        public void War_TiedLeaders_FightEachOtherOnly()
        {
            var sim = GodSim.Create(GameSetup.Watch(4));
            var s = sim.State;
            GiveHqs(sim, Faction.F2, 3);
            GiveHqs(sim, Faction.F4, 3); // F2 and F4 tied at 4 VP, the rest at 1
            Assert.AreEqual(4, FactionRules.VictoryPoints(s, Faction.F2));
            Assert.AreEqual(4, FactionRules.VictoryPoints(s, Faction.F4));
            foreach (var f in s.Setup.Factions) Army(sim, f, 4);
            ThinkAll(sim, Brains(sim), 3);
            foreach (var f in s.Setup.Factions)
            {
                var expected = f == Faction.F2 ? new[] { Faction.F4 } : f == Faction.F4 ? new[] { Faction.F2 } : new Faction[0];
                CollectionAssert.AreEqual(expected, s.Factions.Enemies(f), FactionInfo.Name(f) + " fights only as the rule says");
            }
        }

        [Test]
        public void Peace_IsMade_OnceTheLeaderHasFallenBack()
        {
            var sim = GodSim.Create(GameSetup.Watch(5));
            var s = sim.State;
            GiveHqs(sim, Faction.F3, 2);
            foreach (var f in s.Setup.Factions) if (f != Faction.F3) Army(sim, f, 4);
            var brains = Brains(sim);
            ThinkAll(sim, brains);
            Assert.AreEqual(5, s.Factions.Enemies(Faction.F3).Count, "the leader is fought");

            // The leader falls back to one HQ.
            foreach (var h in s.Territory.HqsOf(Faction.F3).Skip(1).ToList()) MilitaryTerritory.Remove(s, h.Id, null);
            sim.Step(); sim.DrainEvents();
            Assert.AreEqual(1, FactionRules.VictoryPoints(s, Faction.F3));
            ThinkAll(sim, brains, 3);
            foreach (var f in s.Setup.Factions) Assert.IsEmpty(s.Factions.Enemies(f), FactionInfo.Name(f) + " made peace");
        }

        [Test]
        public void Peace_IsOffered_WhenLosingBadly()
        {
            var sim = GodSim.Create(GameSetup.Watch(6));
            var s = sim.State;
            sim.Submit(new DeclareWar(Faction.F1, Faction.F0)); sim.Step();
            // F1's army stands at F0's HQ; F0 has none.
            var hq0 = s.Territory.HqsOf(Faction.F0)[0];
            for (int i = 0; i < 8; i++) sim.Submit(new DevSpawnUnit(Faction.F1, UnitKind.Infantry, hq0.Node));
            sim.Step(); sim.DrainEvents();
            var brain = new AiBrain(sim, Faction.F0);
            ThinkAll(sim, new[] { brain });
            Assert.IsTrue(s.Factions.HasOfferedPeace(Faction.F0, Faction.F1), string.Join("; ", brain.LastActions));
        }

        [Test]
        public void AtWar_TheArmyMarksTheNearestEnemyHq_ThenItsFactories()
        {
            var sim = GodSim.Create(GameSetup.Watch(7));
            var s = sim.State; var t = s.Territory; var g = t.Grid;
            var me = Faction.F0;
            var mine = t.HqsOf(me)[0];
            var foe = s.Setup.Factions.Where(f => f != me).OrderBy(f => g.Distance(t.HqsOf(f)[0].Node, mine.Node)).First();
            sim.Submit(new DeclareWar(me, foe)); sim.Step();
            Army(sim, me, 12);
            var brain = new AiBrain(sim, me);
            ThinkAll(sim, new[] { brain }, 3);
            var marks = s.Military.MarkedOf(me);
            Assert.Greater(marks.Count, 0, "a target is marked: " + string.Join("; ", brain.LastActions));
            Assert.AreEqual(t.HqsOf(foe)[0].Id, marks[0], "the nearest enemy HQ first");
            Assert.IsNotNull(s.Military.DefendAtBuilding(mine.Id), "guards stay home");
            sim.Step(40); sim.DrainEvents();
            Assert.IsTrue(s.Military.Units.Any(u => u.Faction == me && u.Activity == UnitActivity.Marching), "the rest march");
        }

        [Test]
        public void AtPeace_NoTargets_AndGuardsOnEveryHq()
        {
            var sim = Solo(8, Faction.F0, out _);
            var s = sim.State;
            StepUntil(sim, 20 * 600, () => s.Territory.HqsOf(Faction.F0).Count >= 2 && s.Military.CountUnits(Faction.F0) >= 2);
            sim.Step(40); sim.DrainEvents();
            Assert.IsEmpty(s.Military.MarkedOf(Faction.F0));
            foreach (var h in s.Territory.HqsOf(Faction.F0)) Assert.IsNotNull(s.Military.DefendAtBuilding(h.Id), "guards on HQ " + h.Id);
        }

        // ---------------- determinism, restart, budget, trade seam ----------------

        [Test]
        public void SameSeed_SixBrains_SameHash()
        {
            ulong a = Play(11, 6000).sim.StateHash();
            ulong b = Play(11, 6000).sim.StateHash();
            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, Play(12, 6000).sim.StateHash());
        }

        [Test]
        public void BrainsRebuiltFromState_ContinueTheSameGame()
        {
            const long half = 4500, full = 9000;
            var whole = Play(13, full).sim;

            var sim = GodSim.Create(GameSetup.Watch(13));
            var first = AiDirector.Attach(sim, sim.State.Setup.Factions);
            while (sim.State.Tick < half) { sim.Step(); sim.DrainEvents(); }
            string saved = first.SaveState();
            first.Detach();
            // New brains, built from nothing but the game state and the saved tag.
            var second = AiDirector.Attach(sim, AiDirector.NonHuman(sim.State));
            second.LoadState(saved);
            while (sim.State.Tick < full && !sim.State.IsGameOver) { sim.Step(); sim.DrainEvents(); }
            Assert.AreEqual(whole.State.Tick, sim.State.Tick);
            Assert.AreEqual(whole.StateHash(), sim.StateHash(), "the rebuilt brains play exactly as the first ones would have");
            Assert.Greater(second.Thinks, 0);
        }

        [Test]
        public void Director_StaggersThinks_WithinItsBudget()
        {
            var sim = GodSim.Create(GameSetup.Watch(14));
            var ai = AiDirector.Attach(sim, sim.State.Setup.Factions);
            long last = 0, most = 0;
            for (int i = 0; i < 200; i++) { sim.Step(); sim.DrainEvents(); most = Math.Max(most, ai.Thinks - last); last = ai.Thinks; }
            Assert.AreEqual(6 * 200 / ai.Tuning.ThinkIntervalTicks, ai.Thinks, "each brain thinks every 2 s");
            Assert.AreEqual(1, most, "staggered: never two brains on one tick");
            foreach (var b in ai.Brains) Assert.LessOrEqual(b.ChecksUsed, ai.Tuning.MaxChecksPerThink + 1);
            ai.Held = true;
            long held = ai.Thinks;
            sim.Step(60); sim.DrainEvents();
            Assert.AreEqual(held, ai.Thinks, "held brains do not think");
            ai.Held = false;
            Assert.AreEqual(6, AiDirector.NonHuman(sim.State).Count, "watch mode: all six");
            Assert.AreEqual(5, AiDirector.NonHuman(GodSim.Create(new GameSetup(14)).State).Count, "play: all but the human");
        }

        [Test]
        public void TradeSeam_DefaultsToNoTrade_AndIsCalledEachThink()
        {
            var sim = GodSim.Create(GameSetup.Watch(15));
            var spy = new SpyTrader();
            var ai = AiDirector.Attach(sim, new[] { Faction.F0, Faction.F1 }, null, spy);
            sim.Step(40); sim.DrainEvents();
            Assert.AreEqual(4, spy.Considered);
            Assert.AreEqual(4, spy.Proposed);
            Assert.IsFalse(NoTrade.Instance.JudgeIncoming(ai.Brains[0], Faction.F2, new int[Wares.Count], new int[Wares.Count]));
            Assert.AreSame(NoTrade.Instance, new AiBrain(sim, Faction.F3).Trader);
            var need = ai.Brains[0].WareNeeds();
            Assert.AreEqual(Wares.Count, need.Length);
            Assert.Greater(need[(int)Ware.Steel], 0, "a Citadel needs steel the start stock has not got");
            Assert.AreEqual(Wares.Count, ai.Brains[0].WareSurplus().Length);
        }

        private sealed class SpyTrader : IAiTrader
        {
            public int Considered, Proposed;
            public void ConsiderOffers(AiBrain brain) => Considered++;
            public void ProposeTrades(AiBrain brain) => Proposed++;
            public bool JudgeIncoming(AiBrain brain, Faction from, int[] give, int[] want) => true;
        }

        // ---------------- full games ----------------

        /// <summary>
        /// Six brains play whole games on several seeds to game over (10 VP or last standing) within 90 sim-minutes, with
        /// invariants every 500 ticks. Each seed's winner, minute, wars and captures go to the test output.
        /// </summary>
        [Test]
        public void SixBrains_PlayWholeGames_ToGameOver()
        {
            var seeds = new ulong[] { 1, 2, 3, 4, 5, 6 };
            int over = 0, withCapture = 0, withWar = 0;
            foreach (var seed in seeds)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var (sim, _, log) = Play(seed, 90 * 600);
                var s = sim.State;
                if (s.IsGameOver) over++;
                if (log.Captures > 0) withCapture++;
                if (log.Wars > 0) withWar++;
                TestContext.Out.WriteLine("seed " + seed + ": " + (s.IsGameOver ? "winner " + FactionInfo.Name((Faction)s.Factions.Winner) + " (" + s.Factions.EndReason + ")" : "no winner")
                    + " at minute " + log.Minutes.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                    + ", wars " + log.Wars + ", peaces " + log.Peaces + ", captures " + log.Captures + ", eliminations " + log.Eliminations
                    + ", Citadels " + log.Citadels + ", units " + log.UnitsMade + ", invariant checks " + log.InvariantChecks + ", real " + sw.ElapsedMilliseconds + " ms");
                TestContext.Out.WriteLine("    " + Standings(s));
                Assert.IsEmpty(SimInvariants.Check(s));
            }
            Assert.GreaterOrEqual(over, seeds.Length - 1, "most games end");
            Assert.GreaterOrEqual(withWar, seeds.Length - 1, "real wars");
            Assert.GreaterOrEqual(withCapture, seeds.Length / 2, "HQs are captured");
        }
    }
}
