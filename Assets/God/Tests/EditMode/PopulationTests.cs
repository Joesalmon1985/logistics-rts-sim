using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// M3: the population module and the learning core as unit tests (EM): the stage 3 game is untouched with the population
    /// off; the island lives with it on; body, starvation, spoiling, variety, the ledger; decay independent of LifeScale;
    /// the Concorde rule; memory caps; save and load with open traces; perception only; integer purity.
    /// </summary>
    public class PopulationTests
    {
        public static GameSetup Setup(ulong seed, int perFaction = 20, int factions = 6) =>
            new GameSetup(seed, GameSetup.NoHuman, factions) { Population = true, StartNpcsPerFaction = perFaction, Victory = VictorySpec.SandboxOnly() };

        private static void Run(GodSim sim, int ticks) { for (int i = 0; i < ticks; i++) { sim.Step(); sim.DrainEvents(); } }

        // ---------------------------------------------------------------- stage 3 untouched (G6.1, G6.5)

        [Test]
        public void PopulationOff_NoStateNoSystem_SameParts()
        {
            var sim = GodSim.Create(GameSetup.Watch(4));
            Assert.IsNull(sim.State.Population);
            Assert.IsFalse(sim.Systems.Any(x => x is PopulationSystem));
            Assert.AreEqual(GodSim.HashPartNames.Count, sim.StateHashParts().Length);
            // the new setup fields and tuning blocks are not written while at their defaults (the hash keeps its old input)
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(sim.State.Setup, SaveFile.Settings);
            StringAssert.DoesNotContain("Population", json);
            StringAssert.DoesNotContain("StartNpcsPerFaction", json);
            string tj = sim.State.Tuning.ToJson();
            StringAssert.DoesNotContain("Learning", tj);
            StringAssert.DoesNotContain("Ecology", tj);
            var t = new SimTuning(); t.Learning.TraceShift = 3;
            StringAssert.Contains("Learning", t.ToJson(), "a changed block is written (and hashed)");
            Assert.AreEqual(3, t.Clone().Learning.TraceShift);
        }

        [Test]
        public void PopulationOn_FoundersSourcesAndThreeMoreHashParts()
        {
            var sim = GodSim.Create(Setup(3));
            var p = sim.State.Population;
            Assert.AreEqual(120, p.Npcs.Count);
            Assert.AreEqual(120, p.Stats.Founders);
            Assert.Greater(p.Sources.Count, 30);
            // M5 (SHARED_CONTRACTS 9.3): "household" and "social" are appended while their states exist
            Assert.AreEqual(GodSim.HashPartNames.Count + 3 + (sim.State.Households != null ? 1 : 0) + (sim.State.Social != null ? 1 : 0), sim.StateHashParts().Length);
            foreach (var f in sim.State.Setup.Factions) Assert.AreEqual(20, p.AliveOf(f));
            Assert.IsEmpty(SimInvariants.Check(sim.State));
        }

        [Test]
        public void Island_TwoDays_NpcsLiveEatSleepAndBuild_InvariantsHold()
        {
            var sim = GodSim.Create(Setup(3));
            var p = sim.State.Population;
            var spawned = 0;
            for (int i = 0; i < 2400; i++)
            {
                sim.Step();
                foreach (var e in sim.DrainEvents()) if (e is NpcSpawned) spawned++;
                if (i % 600 == 599) CollectionAssert.IsEmpty(SimInvariants.Check(sim.State), "tick " + i);
            }
            Assert.AreEqual(120, spawned, "every founder is announced once (NpcSpawned)");
            Assert.AreEqual(120, p.AliveCount);
            Assert.Greater(p.Stats.Meals, 300);
            Assert.Greater(p.Stats.Sleeps, 100);
            Assert.Greater(p.Stats.LogsGathered, 10);
            Assert.AreEqual(0, p.Stats.IntentsRefused);
        }

        /// <summary>
        /// Inspectability (owner, 26 Nov): the card read through the faction view shows the options weighed at the last
        /// decision with their score terms (need, instinct, learned, personality, cost, explore) adding up to each option's
        /// total and the chosen one marked, the recent experiences with their rewards, and the strongest associations; one
        /// person-day row per living person per day carries needs, time allocation and learning outcomes.
        /// </summary>
        [Test]
        public void Inspector_ShowsOptionsWithScoreTerms_RecentExperiences_AndThePersonDayRow()
        {
            var sim = GodSim.Create(Setup(3));
            var p = sim.State.Population;
            int rows = 0, alive = 0;
            for (int i = 0; i < 1300; i++)
            {
                int before = p.AliveCount;
                sim.Step();
                int now = 0;
                foreach (var e in sim.DrainEvents())
                    if (e is NpcDay row)
                    {
                        now++;
                        Assert.AreEqual(1199, row.Ticks.Sum(), "a row covers every tick the person lived that day (day 0 starts at tick 1)");
                        Assert.AreEqual(0, row.Day);
                    }
                if (now > 0) { rows += now; alive = before; }
            }
            Assert.AreEqual(alive, rows, "one row per living person at the day's end");
            var view = God.Sim.Information.ViewFiller.For(sim.State, Faction.F0);
            int withOptions = 0, withExperiences = 0;
            foreach (var n in p.Npcs.Where(x => x.Alive && x.Faction == Faction.F0))
            {
                var card = view.Population.Inspect(n.Id);
                Assert.IsNotNull(card);
                if (card.Opportunities.Count > 0)
                {
                    withOptions++;
                    Assert.AreEqual(1, card.Opportunities.Count(o => o.Chosen), "exactly one option is the choice");
                    foreach (var o in card.Opportunities)
                    {
                        Assert.AreEqual(o.Total, o.Instinct + o.Learned + o.Social + o.Personality - o.Cost + o.Explore, "the terms add up: " + o.Breakdown);
                        Assert.That(o.Need, Is.InRange(Math.Min(0, o.Instinct), Math.Max(0, o.Instinct)), "need is part of instinct");
                    }
                    var chosen = card.Opportunities.First(o => o.Chosen);
                    Assert.AreEqual(card.Total, chosen.Total);
                    Assert.AreEqual(card.Need, chosen.Need);
                }
                if (card.Experiences.Count > 0) withExperiences++;
            }
            Assert.Greater(withOptions, 15, "most people have weighed options");
            Assert.Greater(withExperiences, 15, "and remember rewarded experiences");
        }

        // ---------------------------------------------------------------- body (M3.14) and food (M3.15)

        [Test]
        public void Starvation_AdultAtNutritionZero_DiesIn5To10Days_OneCause()
        {
            var sim = LearningFixtures.Empty(1);
            int glade = LearningFixtures.Glade(sim);
            var npc = LearningFixtures.Npc(sim, glade, InstinctSet.Default, LearningMode.On, hunger: 1000);
            npc.Nutrition = 0;
            var died = new List<NpcDied>();
            int tpd = sim.State.Setup.TicksPerDay;
            for (int i = 0; i < 12 * tpd && npc.Alive; i++) { sim.Step(); died.AddRange(sim.DrainEvents().OfType<NpcDied>()); }
            Assert.IsFalse(npc.Alive);
            double days = npc.DiedTick / (double)tpd;
            TestContext.WriteLine("starved after " + days.ToString("F2") + " days");
            Assert.That(days, Is.InRange(5.0, 10.0));
            Assert.AreEqual(1, died.Count);
            Assert.AreEqual(DeathCause.Starvation, died[0].Cause);
            Assert.AreEqual(1, sim.State.Population.Stats.DeathsByCause.Sum());
            Assert.IsEmpty(SimInvariants.Check(sim.State));
        }

        [Test]
        public void Spoiling_ALotPastItsDayGoesAtTheNextDayStart_OneConsumedSpoiled()
        {
            var sim = LearningFixtures.Empty(2);
            int glade = LearningFixtures.Glade(sim);
            var s = sim.State;
            // its day is day 0: gone at the start of day 1 (placed through the goods API, F0)
            Goods.Produce(s, null, Holder.Ground(glade), Ware.Fish, 2, 0, 0, Owner.None, ProduceReason.Placed, 0, 0);
            var spoiled = new List<Consumed>();
            for (int i = 0; i < 1300; i++) { sim.Step(); spoiled.AddRange(sim.DrainEvents().OfType<Consumed>().Where(c => c.Reason == ConsumeReason.Spoiled)); }
            Assert.AreEqual(1, spoiled.Count);
            Assert.AreEqual(2, spoiled[0].Count);
            Assert.AreEqual(Ware.Fish, spoiled[0].Ware);
            Assert.AreEqual(1200, spoiled[0].Tick, "at the first tick of day 1");
            Assert.AreEqual(0, s.Population.Lots.Count(l => l.Lot.Ware == Ware.Fish));
            Assert.IsEmpty(SimInvariants.Check(s));
        }

        [Test]
        public void Variety_TheThirdBerryRelievesTwoThirdsOfTheFirst()
        {
            Assert.AreEqual(150, PopulationSystem.VarietyRelief(150, 1));
            Assert.AreEqual(120, PopulationSystem.VarietyRelief(150, 2));
            Assert.AreEqual(100, PopulationSystem.VarietyRelief(150, 3), "1000/1500 of the first");
            Assert.AreEqual(150 * 1000 / 1500, PopulationSystem.VarietyRelief(150, 3));
        }

        [Test]
        public void Ledger_IsConservedOverAWeekOnTheIsland_AndAfterSaveAndLoad()
        {
            var sim = GodSim.Create(Setup(5, 10, 4));
            for (int d = 0; d < 7; d++) { Run(sim, 1200); CollectionAssert.IsEmpty(SimInvariants.Check(sim.State), "day " + d); }
            var p = sim.State.Population;
            Assert.Greater(p.Ledger.ConsumedBy(ConsumeReason.Eaten, Ware.Berries) + p.Ledger.ConsumedBy(ConsumeReason.Eaten, Ware.Roots) + p.Ledger.ConsumedBy(ConsumeReason.Eaten, Ware.Fish), 100);
            var loaded = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            CollectionAssert.IsEmpty(SimInvariants.Check(loaded.State));
        }

        // ---------------------------------------------------------------- learning core units

        private static AssociationStore StoreWithEntries(int n, short w)
        {
            var st = new AssociationStore(new LearningTuning().Caps);
            for (int i = 0; i < n; i++) { int at = st.Find((uint)(i + 1), 1); at = st.InsertAt(~at, (uint)(i + 1), 1, 0, Origin.Own); st.Entries[at].Stw = w; st.Entries[at].Ltw = w; }
            return st;
        }

        private static int DecayOnce(int lifeScale, int decayScale, bool follow = false)
        {
            var setup = new GameSetup(1) { LifeScale = lifeScale, DecayFollowsLifeScale = follow };
            var tuning = new SimTuning(); tuning.Learning.DecayScalePermille = decayScale;
            var s = new GodState { Setup = setup, Tuning = tuning };
            var st = StoreWithEntries(4, 3200);
            st.LastConsolidatedDay = 0;
            Learner.Consolidate(st, tuning.Learning, LearnTraits.Neutral, 1, PopulationSystem.DecayScale(s), new RecruitScratch(), null);
            return 3200 - st.Entries[0].Ltw;
        }

        [Test]
        public void Decay_IsIndependentOfLifeScale_AndConfigurable()
        {
            int baseLoss = DecayOnce(100, 1000);
            Assert.AreEqual(100, baseLoss, "3200 >> 5 = 100: about 3% a day");
            Assert.AreEqual(baseLoss, DecayOnce(200, 1000), "LifeScale 200 leaves the daily decay unchanged (owner decision Q1)");
            Assert.AreEqual(2 * baseLoss, DecayOnce(100, 2000), "DecayScalePermille 2000 doubles it");
            Assert.AreEqual(0, DecayOnce(100, 0), "0 stops it");
            Assert.AreEqual(2 * baseLoss, DecayOnce(200, 1000, follow: true), "only an explicit setup choice makes forgetting follow LifeScale");
        }

        [Test]
        public void Fx_ShiftsTowardsZero_AndClamps()
        {
            Assert.AreEqual(-2, Fx.Shr(-9, 2));
            Assert.AreEqual(2, Fx.Shr(9, 2));
            Assert.AreEqual(4000, Fx.Clamp16(9999));
            Assert.AreEqual(-4000, Fx.Clamp16(-9999));
            Assert.AreEqual(600, Learner.DriveReward(1000, 300, 600, 450), "relief above comfort");
            Assert.AreEqual(0, Learner.DriveReward(1000, 300, 280, 100), "eating when full is not rewarded");
            Assert.AreEqual(-400, Learner.DriveReward(1000, 300, 250, 400), "rising above comfort is punished from comfort up");
        }

        [Test]
        public void Store_StaysSortedAndBounded_EvictingTheLeastUseful()
        {
            var tu = new LearningTuning();
            var st = new AssociationStore(tu.Caps);
            var keys = new uint[24];
            var rng = new Rng(9);
            for (int round = 0; round < 3000; round++)
            {
                for (int k = 0; k < 10; k++) keys[k] = Feature.Make(FeatureKind.PlaceRef, rng.NextInt(5000));
                Array.Sort(keys, 0, 10);
                Learner.Update(st, tu, LearnTraits.Neutral, keys, 0, 10, (byte)(1 + rng.NextInt(10)), rng.NextInt(4001) - 2000, round / 100, null, true);
            }
            Assert.LessOrEqual(st.Count, 256);
            Assert.Greater(st.Evictions, 0);
            var bad = new List<string>();
            PopulationInvariants.CheckStore(1, st, tu, bad);
            CollectionAssert.IsEmpty(bad);
            Assert.LessOrEqual(st.Bytes, 10240, "L11: learning bytes per NPC within 10 KiB at the default caps");
            TestContext.WriteLine("learning bytes per NPC at default caps: " + st.Bytes);
        }

        [Test]
        public void Store_PacksAndUnpacksExactly()
        {
            var tu = new LearningTuning();
            var st = StoreWithEntries(40, 123);
            var keys = new uint[] { Feature.Class(1), Feature.Place(2) };
            Learner.OpenTrace(st, tu, LearnTraits.Neutral, 50, Verb.Eat, keys, 0, 2, 7, 0, 0, null);
            st.RewardAcc = 33;
            var copy = new AssociationStore { Count = st.Count, RingCount = st.RingCount, RewardAcc = st.RewardAcc };
            copy.Unpack(st.Pack());
            Assert.AreEqual(st.Hash(), copy.Hash());
        }

        [Test]
        public void Concorde_ContinuingCostsWhatRemains_NotWhatWasSpent()
        {
            // two NPCs walking to the same bush, one far along, one just started, at the same remaining distance: the same cost
            int Cost(int spentNodes, int remaining)
            {
                var sim = LearningFixtures.Empty(1);
                int glade = LearningFixtures.Glade(sim);
                int bush = LearningFixtures.Step(sim, glade, 0, remaining);
                var npc = LearningFixtures.Npc(sim, glade, InstinctSet.Default, LearningMode.On, hunger: 600);
                LearningFixtures.Bush(sim, SourceKind.Berries, bush);
                npc.Action = new NpcAction { Verb = Verb.Eat, TargetKind = IntentTarget.Node, TargetNode = bush, Since = -spentNodes * 6 };
                int cost = -1;
                var sys = sim.Systems.OfType<PopulationSystem>().First();
                sys.Inspect = (id, sc) =>
                {
                    for (int i = 0; i < sc.CandCount; i++) if (sc.Cand[i].Verb == Verb.Eat && sc.Cand[i].TargetNode == bush && cost < 0) cost = sc.Cand[i].Cost;
                };
                for (int i = 0; i < 20 && cost < 0; i++) sim.Step();
                return cost;
            }
            Assert.AreEqual(Cost(1, 5), Cost(30, 5), "the amount already spent does not change the score");
            Assert.AreNotEqual(Cost(1, 5), Cost(1, 8), "the remaining cost does");
        }

        // ---------------------------------------------------------------- perception only (M3.12), determinism, save and load

        [Test]
        public void PerceptionOnly_EveryTargetWasSeenOrBelieved()
        {
            var t = new SimTuning(); t.Population.CheckPerception = true;
            var sim = GodSim.Create(Setup(7, 10, 2), t);
            Run(sim, 1200 * 3);
            var st = sim.State.Population.Stats;
            Assert.Greater(st.PerceptionChecks, 3000);
            Assert.AreEqual(0, st.NotPerceived, "no NPC chose a target it had not perceived or did not believe in");
            Assert.AreEqual(0, st.IntentsRefused);
        }

        [Test]
        public void SameSeed_SameHash_DifferentSeed_Different()
        {
            ulong H(ulong seed) { var sim = GodSim.Create(Setup(seed, 8, 3)); Run(sim, 2000); return sim.StateHash(); }
            Assert.AreEqual(H(11), H(11));
            Assert.AreNotEqual(H(11), H(12));
        }

        [Test]
        public void SaveMidDay_WithOpenTraces_ContinuesExactly()
        {
            var a = GodSim.Create(Setup(21, 6, 3));
            Run(a, 3000); // mid-day, traces open
            Assert.IsTrue(a.State.Population.Npcs.Any(n => n.Mind.Store.RingCount > 0), "traces are open at the save");
            var json = SaveFile.From(a).ToJson();
            var b = SaveFile.FromJson(json).ToSim();
            Assert.AreEqual(a.StateHash(), b.StateHash(), "the loaded game hashes the same");
            Assert.AreEqual(json, SaveFile.From(b).ToJson(), "a second save of the loaded game is identical");
            for (int i = 0; i < 1500; i++)
            {
                a.Step(); b.Step(); a.DrainEvents(); b.DrainEvents();
                if (i % 100 == 0) Assert.AreEqual(a.StateHash(), b.StateHash(), "tick " + a.State.Tick);
            }
            CollectionAssert.AreEqual(a.StateHashParts(), b.StateHashParts());
        }

        [Test]
        public void Frozen_LeavesTheWeightsUnchanged_Off_LearnsNothing()
        {
            foreach (var mode in new[] { LearningMode.Frozen, LearningMode.Off })
            {
                var t = new SimTuning(); t.Learning.DefaultMode = mode;
                var sim = GodSim.Create(Setup(4, 5, 2), t);
                var before = sim.State.Population.Npcs.Select(n => n.Mind.Store.HashWeights()).ToList();
                Run(sim, 2400);
                var after = sim.State.Population.Npcs.Where(n => n.Mind != null).Select(n => n.Mind.Store.HashWeights()).ToList();
                CollectionAssert.AreEqual(before.Take(after.Count), after, mode + " never changes a weight");
            }
        }

        // ---------------------------------------------------------------- the inspector's read path (layer 7)

        [Test]
        public void KnowledgeView_InspectsOwnCitizensOnly_AndReadingChangesNothing()
        {
            var sim = GodSim.Create(Setup(3, 5, 2));
            Run(sim, 900);
            ulong before = sim.StateHash();
            var v0 = Information.ViewFiller.For(sim, Faction.F0);
            var mine = v0.Population.Citizens();
            Assert.AreEqual(5, mine.Count);
            var card = v0.Population.Inspect(mine[0]);
            Assert.IsNotNull(card);
            Assert.IsNotEmpty(card.LastDecision, "its last decision and why");
            Assert.Greater(card.Candidates, 0);
            Assert.AreEqual(5, card.Drives.Length);
            Assert.Greater(card.Entries, 0, "it has learned something in 900 ticks");
            Assert.IsTrue(card.HabitsPositive.Count + card.HabitsNegative.Count > 0);
            TestContext.WriteLine("card: " + card.LastDecision + " | why " + string.Join("; ", card.Why) + " | habits " + string.Join("; ", card.HabitsPositive));
            int foreign = sim.State.Population.Npcs.First(n => n.Faction == Faction.F1).Id;
            Assert.IsNull(v0.Population.Inspect(foreign), "another faction's people have no card (Q21)");
            Assert.IsNotNull(v0.Population.See(foreign), "but can be seen");
            Assert.AreEqual(before, sim.StateHash(), "reading the view changes nothing");
        }

        // ---------------------------------------------------------------- budgets and purity (L11, L12, G4.9)

        [Test]
        public void MemoryPerNpc_WithinTheBudget()
        {
            var st = new AssociationStore(new LearningTuning().Caps);
            int learning = st.Bytes;
            int beliefs = PopulationModule.BeliefSlots * 20;
            int core = 320;
            TestContext.WriteLine($"per NPC: learning {learning} B, beliefs {beliefs} B, core record about {core} B");
            Assert.LessOrEqual(learning, 10240);
            Assert.LessOrEqual(beliefs, 1536);
            Assert.LessOrEqual(learning + beliefs + core, 20 * 1024, "within the combined 20 KiB budget (POPULATION memory table)");
        }

        private static string GodRoot([CallerFilePath] string here = "")
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(here));
            while (dir != null && !(dir.Name == "God" && dir.Parent != null && dir.Parent.Name == "Assets")) dir = dir.Parent;
            Assert.IsNotNull(dir);
            return dir.FullName;
        }

        [Test]
        public void SourceScan_NewStage4CodeIsIntegerOnly()
        {
            var roots = new[] { Path.Combine(GodRoot(), "Sim", "Learning"), Path.Combine(GodRoot(), "Sim", "Cognition"), Path.Combine(GodRoot(), "Sim", "Population") };
            var files = roots.SelectMany(r => Directory.GetFiles(r, "*.cs", SearchOption.AllDirectories)).ToList();
            files.Add(Path.Combine(GodRoot(), "Sim", "Policy", "MindView.cs"));
            files.Add(Path.Combine(GodRoot(), "Sim", "Policy", "AssociativeNpcPolicy.cs"));
            Assert.Greater(files.Count, 12);
            var bad = new List<string>();
            foreach (var f in files)
            {
                string code = File.ReadAllText(f);
                code = Regex.Replace(code, @"/\*.*?\*/", " ", RegexOptions.Singleline);
                code = Regex.Replace(code, @"//[^\n]*", " ");
                code = Regex.Replace(code, "\"(?:[^\"\\\\]|\\\\.)*\"", "\"\"");
                foreach (var pat in new[] { @"\bUnityEngine\b", @"\bSystem\.Random\b", @"\bnew\s+Random\b", @"\bfloat\b", @"\bdouble\b", @"\bdecimal\b", @"\bMathF?\.(Exp|Log|Pow|Sqrt|Sin|Cos|Round)\b", @"\bDateTime\b", @"\bStopwatch\b" })
                    if (Regex.IsMatch(code, pat)) bad.Add(Path.GetFileName(f) + ": " + pat);
            }
            CollectionAssert.IsEmpty(bad);
        }
    }
}
