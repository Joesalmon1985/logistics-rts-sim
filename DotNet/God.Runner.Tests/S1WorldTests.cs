using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Unified;
using God.Train;
using God.Train.Curriculum;
using God.Train.Env;
using God.Train.Honesty;
using God.Train.Learn;
using God.Train.Net;
using God.Train.Teacher;
using NUnit.Framework;

namespace God.Runner.Tests
{
    using Checkpoint = God.Train.Learn.Checkpoint;

    /// <summary>
    /// Slice 1: the real unified training environment (UnifiedWorld at the game's decision site), the survival teacher and the
    /// honesty seams on the real world. Mechanics only: no training result is claimed here.
    /// </summary>
    [TestFixture]
    public class S1WorldTests
    {
        private static UnifiedWorld World(long seed, int persons = 1, int mask = 0x033) =>
            new UnifiedWorld(new UnifiedConfig { Persons = persons, NeedMask = mask }, seed, Layout.Train, null);

        [Test]
        public void S1_World_OneTribe_RealSeams()
        {
            Assert.That(UnifiedWorld.Unavailable(1), Is.Null);
            var w = World(3, 4);
            Assert.That(w.Kind, Is.EqualTo("unified"));
            Assert.That(w.People.Count, Is.EqualTo(4), "the episode's tribe only (the second faction is removed at tick 0)");
            Assert.That(w.People.All(p => w.Alive(p) && w.AdultAtStart(p)));
            var obs = new short[ObsSchema.Length]; var p0 = default(Percept); var m = default(ActMask);
            w.Observe(w.People[0], ref p0, obs, ref m);
            Assert.That(ActMask.Allows(m.Control, (int)Prim.StepForward) || ActMask.Allows(m.Control, (int)Prim.TurnLeft), "Motor.Legal answers");
            Assert.That(w.ReadyToDecide(w.People[0]), Is.False, "decisions are the game's own decision site's, never the trainer loop's");
        }

        [Test]
        public void S1_Teacher_SurvivesAndIsLabelled()
        {
            var st = CurriculumData.Load("S2");
            var w = World(7, 2);
            var rec = new List<Sequence>();
            var ep = Rollout.Run(w, new SurvivalTeacher(), 7, 3 * w.TicksPerDay, evaluation: false, record: rec);
            Assert.That(ep.AdultsAliveAt(ep.Ticks), Is.EqualTo(2), "the teacher keeps both alive for 3 days");
            Assert.That(w.Drinks, Is.GreaterThan(0)); Assert.That(w.Forages, Is.GreaterThan(0)); Assert.That(w.Sleeps, Is.GreaterThan(0));
            Assert.That(ep.TeacherSubmissions, Is.GreaterThan(0));
            Assert.That(rec.Count, Is.EqualTo(2)); Assert.That(rec.All(s => s.Origin == ControlOrigin.Teacher && s.Samples.Count > 0));
            Assert.That(ep.People.Sum(p => p.PainTicksHungerThirst), Is.EqualTo(0));
        }

        [Test]
        public void S1_Honesty_RealWorld()
        {
            Assert.Throws<HeldOutRefusedException>(() => World(900001));
            Assert.Throws<HeldOutRefusedException>(() => new UnifiedWorld(new UnifiedConfig(), 5, Layout.Mirrored, null));
            var w = World(9);
            Assert.Throws<EvaluationInvalidException>(() => Rollout.Run(w, new SurvivalTeacher(), 9, 10, evaluation: true), "an evaluation never meets the teacher");
        }

        [Test]
        public void S1_InputMap_RescalesAndDerives()
        {
            var obs = new short[ObsSchema.Length];
            int ent = ObsSchema.OffsetOf("entities");
            // slot 0: a person ahead at 2; slot 1: fresh water to the left (sector 5) at 1, in reach
            obs[ent] = Percept.ClassPerson; obs[ent + 1] = 0; obs[ent + 2] = 2; obs[ent + 3] = 1;
            int o = ent + ObsSchema.EntityFields;
            obs[o] = Percept.ClassFreshWater; obs[o + 1] = 5; obs[o + 2] = 1; obs[o + 3] = 7 | 512;
            Assert.That(BrainInput.Apply(BrainInput.MapRaw, o + 1, 5, 2000), Is.EqualTo(5), "map 0 is the raw vector");
            Assert.That(BrainInput.Apply(BrainInput.MapV1, o + 1, 5, 2000), Is.EqualTo(-333), "a left sector becomes a signed turn");
            Assert.That(BrainInput.Apply(BrainInput.MapV1, o + 2, 1, 2000), Is.EqualTo(100));
            Assert.That(BrainInput.Apply(BrainInput.MapV1, o, Percept.ClassFreshWater, 2000), Is.EqualTo(600));
            var x = new int[ObsSchema.Length + BrainInput.Derived];
            BrainInput.Fill(BrainInput.MapV2, obs, x, x.Length, 2000);
            int w = ObsSchema.Length;
            Assert.That(new[] { x[w], x[w + 1], x[w + 2], x[w + 3], x[w + 4], x[w + 5], x[w + 6] }, Is.EqualTo(new[] { 1000, 0, 0, 1000, 100, 1000, 120 }), "nearest water: present, left, distance, in reach, slot 1");
            Assert.That(x[w + 7], Is.EqualTo(0), "no food perceived or remembered");
            var y = new int[ObsSchema.Length];
            BrainInput.Fill(BrainInput.MapV1, obs, y, y.Length, 2000);
            Assert.That(y, Is.EqualTo(x.Take(ObsSchema.Length).ToArray()), "map 2's first 284 inputs are map 1's");
        }

        [Test]
        public void S1_Network_Deterministic_AtDecisionSite()
        {
            var net = Checkpoint.Untrained(NetShape.Default, 1, "unified").ToQNet();
            ulong H(long seed) { var w = World(seed, 2); Rollout.Run(w, new NetActor(net), seed, 600, evaluation: false); return w.StateHash(); }
            Assert.That(H(11), Is.EqualTo(H(11)), "same seed, same weights: same world");
            var w2 = World(11, 2);
            var ep = Rollout.Run(w2, new NetActor(net), 11, 600, evaluation: false);
            Assert.That(ep.Decisions, Is.GreaterThan(0));
            Assert.That(w2.Runtime.Injected, Is.True, "a network candidate is injected, never the game's own brain");
        }
    }
}
