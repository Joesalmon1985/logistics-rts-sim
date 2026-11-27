using System.Linq;
using God.Sim.Learning;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// M3b: credit for goods (lot episodes and realised value), the late outcome reaching a live trace, the decision trace
    /// event, and founders placed in sight of food.
    /// </summary>
    public class LearningCreditTests
    {
        private static readonly uint[] Keys = { Feature.Class(ObjClass.BerryBush), Feature.Category(ObjCategory.Food) };

        private static AssociationStore NewStore() => new AssociationStore(new LearningTuning().Caps) { Mode = LearningMode.On };

        /// <summary>A forage decision that gained lot 7, its trace run out so that it waits as an episode.</summary>
        private static AssociationStore ForageWaitingOnLot(LearningTuning tu, int key)
        {
            var st = NewStore();
            var tr = LearnTraits.Neutral;
            Learner.OpenTrace(st, tu, tr, 0, Verb.ForageAt, Keys, 0, Keys.Length, 0, 0, 0, null);
            Learner.AwaitOutcome(st, Verb.ForageAt, key);
            Learner.EndOngoing(st);
            for (int i = 0; i < 20; i++) Learner.DecayTraces(st, tu.TraceShift);
            Learner.BeginThink(st, tu, tr, 0, null);
            return st;
        }

        [Test]
        public void LotEpisode_TheSpoilageOfALotReachesTheDecisionThatGainedIt()
        {
            var tu = new LearningTuning();
            int key = Learner.LotEpisodeKey(7);
            var st = ForageWaitingOnLot(tu, key);
            Assert.AreEqual(0, st.RingCount, "the trace ran out");
            Assert.AreEqual(1, st.OpenCount, "a forage that gained a lot waits for the lot's fate");
            Assert.AreEqual(0, Learner.Outcome(st, tu, LearnTraits.Neutral, Learner.LotEpisodeKey(8), -400, 2, null, true), "another lot reaches nobody");
            Assert.AreEqual(1, Learner.Outcome(st, tu, LearnTraits.Neutral, key, -400, 2, null, true));
            Assert.AreEqual(0, st.OpenCount, "the spoilage closes it");
            Assert.Less(st.SumStw(Keys, 0, Keys.Length, Verb.ForageAt, out _), 0, "foraging what then spoiled is now valued lower");
        }

        [Test]
        public void RealisedValue_EachMealPaysBack_WithoutClosing_UntilTheLotIsEatenUp()
        {
            var tu = new LearningTuning();
            int key = Learner.LotEpisodeKey(7);
            var st = ForageWaitingOnLot(tu, key);
            Assert.AreEqual(1, Learner.Accrue(st, tu, LearnTraits.Neutral, key, 600, 1, null));
            int q1 = st.SumStw(Keys, 0, Keys.Length, Verb.ForageAt, out _);
            Assert.Greater(q1, 0, "the meal's relief reaches the forage decision");
            Assert.AreEqual(1, st.OpenCount, "more meals may follow from the same lot");
            Learner.Accrue(st, tu, LearnTraits.Neutral, key, 600, 1, null);
            Assert.Greater(st.SumStw(Keys, 0, Keys.Length, Verb.ForageAt, out _), q1);
            Learner.Outcome(st, tu, LearnTraits.Neutral, key, 0, 1, null, true);
            Assert.AreEqual(0, st.OpenCount, "eaten up: it waits for nothing more");
        }

        [Test]
        public void Outcome_AlsoTeachesATraceThatIsStillLive()
        {
            // M3b fix: under TD a live trace's outcome used to go into a return nothing read (a builder finishing its own hut
            // never learned the bed)
            var tu = new LearningTuning();
            var st = NewStore();
            Learner.OpenTrace(st, tu, LearnTraits.Neutral, 0, Verb.Build, Keys, 0, Keys.Length, 0, 99, 0, null);
            Assert.AreEqual(1, Learner.Outcome(st, tu, LearnTraits.Neutral, 99, 400, 0, null));
            Assert.Greater(st.SumStw(Keys, 0, Keys.Length, Verb.Build, out _), 0);
            Assert.AreEqual(0, st.Ring[0].EpisodeKey, "it waits no longer");
        }

        [Test]
        public void Frozen_LotOutcomesChangeNoWeight()
        {
            var tu = new LearningTuning();
            int key = Learner.LotEpisodeKey(7);
            var st = ForageWaitingOnLot(tu, key);
            st.Mode = LearningMode.Frozen;
            ulong before = st.HashWeights();
            Learner.Accrue(st, tu, LearnTraits.Neutral, key, 600, 1, null);
            Learner.Outcome(st, tu, LearnTraits.Neutral, key, -400, 2, null, true);
            Assert.AreEqual(before, st.HashWeights());
        }

        [Test]
        public void DecisionMade_OnlyWhenTraced_OnePerChosenIntent_TermsAddUp_AndTheHashIsUnchanged()
        {
            GodSim Make() => GodSim.Create(PopulationTests.Setup(3, 6, 2));
            var plain = Make();
            var traced = Make(); traced.TraceDecisions = true;
            int untraced = 0, n = 0;
            for (int i = 0; i < 1500; i++)
            {
                plain.Step(); traced.Step();
                untraced += plain.DrainEvents().OfType<DecisionMade>().Count();
                foreach (var d in traced.DrainEvents().OfType<DecisionMade>())
                {
                    n++;
                    Assert.AreEqual(d.Total, d.Instinct + d.Learned + d.Social + d.Culture + d.Personality - d.Cost + d.Explore, "the terms add up to the score it won with");
                    Assert.AreNotEqual(0, d.Verb);
                }
            }
            Assert.AreEqual(0, untraced, "nothing is built unless the host asks (Full recording)");
            Assert.Greater(n, 100);
            Assert.AreEqual(plain.StateHash(), traced.StateHash(), "recording the decisions never changes the game");
            var info = EventCatalog.Of(typeof(DecisionMade));
            Assert.AreEqual("fact_decision", info.Table);
            Assert.AreEqual(TelemetryDomain.Learning, info.Domain);
        }

        [Test]
        public void Founders_StartInSightOfFood()
        {
            int seen = 0, all = 0;
            foreach (ulong seed in new ulong[] { 1, 2, 3, 4, 5, 6 })
            {
                var s = GodSim.Create(PopulationTests.Setup(seed)).State;
                var g = s.Territory.Grid;
                foreach (var n in s.Population.Npcs)
                {
                    all++;
                    if (s.Population.Sources.Any(src => src.Kind != SourceKind.Wood && src.Kind != SourceKind.Nightshade && g.Distance(n.Node, src.Node) <= s.Tuning.Population.SightDay)) seen++;
                }
            }
            TestContext.WriteLine($"founders with food in sight at the start: {seen} of {all}");
            Assert.GreaterOrEqual(seen * 100, all * 98, "at least 98% of founders see food when they arrive");
        }
    }
}
