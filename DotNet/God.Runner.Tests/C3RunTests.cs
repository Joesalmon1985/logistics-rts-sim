using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Civ;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Social;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// C3 at run level (docs/civdiv/DEVELOPMENT_PACKAGES/C3.md section 13): end-to-end scenario 3 (an individual operates a
    /// productive asset and compensates another for work with goods handed over within reach) in controlled fixtures
    /// where every choice is the people's own, and in the civ_employment scenario.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class C3RunTests
    {
        /// <summary>The scenario-3 fixture: an owner with a field (worked plot: roots from labour), a hungry neighbour without an asset nearby.</summary>
        internal sealed class Fixture
        {
            public M5SocialLab Lab;
            public GodState S => Lab.Sim.State;
            public Npc Owner, Worker;
            public ProductiveAsset Field;

            public Fixture(ulong seed, Action<SimTuning> tune = null, int workerHunger = 500, int fieldDir = 3)
            {
                Lab = new M5SocialLab(seed, t => { t.Civ.Enabled = 1; tune?.Invoke(t); });
                Owner = Lab.Add(Lab.Glade, null, LearningMode.On, fed: false);
                Worker = Lab.AddH(Lab.At(0, 2), null, LearningMode.On, false, workerHunger);
                Field = Assets.Register(S, null, AssetKinds.Field, Lab.At(fieldDir, 2), God.Sim.Population.Owner.Npc(Owner.Id), OwnershipBasis.Built, Owner.Id);
                Ownership.Observe(S, Owner, Field, God.Sim.Population.Owner.Npc(Owner.Id), 255, 0xFF); // it built it: it knows it is its own
            }

            public List<T> Of<T>() where T : SimEvent => Lab.Of<T>().ToList();
        }

        [Test]
        public void Probe_Scenario3()
        {
            for (ulong seed = 1; seed <= 5; seed++)
            {
                var f = new Fixture(seed);
                f.Lab.Step(2400);
                var offers = f.Of<GestureMade>().Count(g => g.Act == GestureAct.Offer && g.TopicKind == TopicKind.Posting);
                TestContext.WriteLine("seed " + seed + ": offers " + offers + ", started " + f.Of<EmploymentStarted>().Count + ", wages " + f.Of<WagePaid>().Count
                    + ", unpaid " + f.Of<WageUnpaid>().Count + ", used " + f.Of<AssetUsed>().Count(u => u.Worker == f.Worker.Id) + ", susp " + f.Of<ContractSuspended>().Count
                    + ", ended " + string.Join(",", f.Of<EmploymentEnded>().Select(e => e.Reason)) + ", refusals " + f.Of<GestureMade>().Count(g => g.Act == GestureAct.Refuse && g.TopicKind == TopicKind.Posting)
                    + ", accepts " + f.Of<GestureMade>().Count(g => g.Act == GestureAct.Accept && g.TopicKind == TopicKind.Posting)
                    + ", jobdays " + f.Of<JobDay>().Count + ", inv " + string.Join(";", SimInvariants.Check(f.S)));
            }
        }
    }
}
