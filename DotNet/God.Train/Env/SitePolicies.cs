using System;
using God.Sim;
using God.Sim.Unified;
using God.Train.Net;

namespace God.Train.Env
{
    /// <summary>
    /// A world whose persons decide at the game's own decision site (UN6's <c>PersonTickUN6</c>: sense through <c>MotorBody</c>,
    /// remember, overlay credit and bias, temperament temperature, the policy, <c>Motor.Submit</c> with the origin), not in the
    /// trainer's loop. <see cref="Rollout"/> binds the actor and only steps and measures.
    /// </summary>
    public interface ISiteWorld : IWorld
    {
        /// <summary>Installs the actor for this episode; <paramref name="record"/> (per person, People order) receives teacher-labelled samples.</summary>
        void Bind(IActor actor, Sequence[] record, bool evaluation);
        long Decisions { get; }
        long TeacherSubmissions { get; }
        /// <summary>Accepted submissions per control (Prim 0-12) this episode.</summary>
        long[] AppliedBy { get; }
        /// <summary>The cause of death of a person ("" while alive).</summary>
        string CauseOf(int person);
    }

    /// <summary>An actor that runs as a game policy at the decision site (a network over int16 weights), or null for the game's own brain.</summary>
    public interface ISitePolicySource
    {
        /// <summary>A fresh policy for one world (null: do not inject; the game's own <c>Brain.Kind</c> decides).</summary>
        IPrimitivePolicy MakePolicy();
    }

    /// <summary>
    /// A network candidate (training, validation, the frozen arm) as a game <see cref="NetPolicy"/>: the game's integer inference
    /// (<c>BrainNet</c>) over the candidate's int16 weights, with the game's temperature and overlay bias. Injected (never the game's
    /// own brain), so it is never reported as trained.
    /// </summary>
    public sealed class CandidatePolicy : NetPolicy
    {
        private readonly string _label;
        public CandidatePolicy(QNet net, string label) : base(new BrainNet(BrainShape.Parse(net.Shape.Text), (short[])net.W.Clone()) { InputMap = net.InputMap }) { _label = label; }
        public override int Kind => BrainKind.Untrained;
        public override string Label => _label;
    }

    /// <summary>The game's own brain of the world's <c>Unification.Brain.Kind</c> (no injection): the evaluated untrained and trained arms.</summary>
    public sealed class GameBrainActor : IActor, ISitePolicySource
    {
        public ControlOrigin Origin => ControlOrigin.Policy;
        public void Begin(IWorld w, long seed) { if (!(w is ISiteWorld)) throw new InvalidOperationException("the game's own brain runs only in the unified world"); }
        public ActionChoice Act(IWorld w, int personIndex, int person, short[] obs, in ActMask m) => throw new InvalidOperationException("the game's own brain decides at the decision site");
        public IPrimitivePolicy MakePolicy() => null;
    }

    /// <summary>Any <see cref="IActor"/> (the teacher) at the decision site: its choice decodes through the percept (<c>Percept.ControlOf</c>).</summary>
    public sealed class ActorPolicy : IPrimitivePolicy
    {
        private readonly UnifiedWorld _w; private readonly IActor _actor; private readonly Sequence[] _record;
        public long Calls;

        public ActorPolicy(UnifiedWorld w, IActor actor, Sequence[] record) { _w = w; _actor = actor; _record = record; }

        public PrimitiveControl Decide(in Percept p, short[] obs, ref BrainState b, in ActMask m, ref Rng rng)
        {
            int idx = _w.IndexOf(p.Actor);
            _w.SetPercept(in p);
            var a = _actor.Act(_w, idx, p.Actor, obs, in m);
            if (_record != null && idx >= 0) _record[idx].Samples.Add(new Sample { Obs = (short[])obs.Clone(), Mask = m, Label = a });
            Calls++;
            return Percept.ControlOf(_w.State, in p, a.Control, a.Target, a.Source, a.Slot, a.Design, a.Gesture, a.R1, a.R2, a.Quantity);
        }
    }
}

namespace God.Train.Env
{
    /// <summary>
    /// <b>TRAINING ONLY</b> (DAgger data aggregation): the network acts (the game's integer inference at the decision site), the
    /// teacher labels every decision it meets; with probability <see cref="Beta"/> (per decision, keyed stream) the teacher's own
    /// choice is executed instead. Injected with origin Teacher (the episode is a demonstration), never in an evaluation.
    /// </summary>
    public sealed class DaggerActor : IActor, ISitePolicySource
    {
        private readonly God.Train.Net.QNet _net; private readonly IActor _teacher; private readonly int _betaPermille;
        private UnifiedWorld _w; private Sequence[] _record; private long _seed;
        public DaggerActor(God.Train.Net.QNet net, IActor teacher, int betaPermille) { _net = net; _teacher = teacher; _betaPermille = betaPermille; }
        public God.Sim.Unified.ControlOrigin Origin => God.Sim.Unified.ControlOrigin.Teacher;
        public void Begin(IWorld w, long seed) { _w = (UnifiedWorld)w; _seed = seed; _teacher.Begin(w, seed); }
        public God.Train.Net.ActionChoice Act(IWorld w, int personIndex, int person, short[] obs, in God.Sim.Unified.ActMask m) => throw new System.InvalidOperationException("DAgger decides at the decision site");
        /// <summary>Called by the world's Bind with the recording sequences.</summary>
        public void SetRecord(Sequence[] record) { _record = record; }
        public God.Sim.Unified.IPrimitivePolicy MakePolicy() => new DaggerPolicy(this);

        private sealed class DaggerPolicy : God.Sim.Unified.BrainPolicy
        {
            private readonly DaggerActor _a; private readonly CandidatePolicy _inner; private readonly God.Sim.Rng _mix;
            public DaggerPolicy(DaggerActor a) { _a = a; _inner = new CandidatePolicy(a._net, "DAGGER"); _mix = new God.Sim.Rng((ulong)a._seed).Fork("dagger-mix"); }
            public override int Kind => God.Sim.Unified.BrainKind.Untrained;
            public override string Label => "DAGGER (training only)";
            public override God.Sim.Unified.PrimitiveControl Decide(in God.Sim.Unified.Percept p, short[] obs, ref God.Sim.Unified.BrainState b, in God.Sim.Unified.ActMask m, ref God.Sim.Rng rng)
            {
                var w = _a._w;
                int idx = w.IndexOf(p.Actor);
                w.SetPercept(in p);
                var label = _a._teacher.Act(w, idx, p.Actor, obs, in m);
                if (_a._record != null && idx >= 0) _a._record[idx].Samples.Add(new Sample { Obs = (short[])obs.Clone(), Mask = m, Label = label });
                _inner.Body = Body; _inner.State = State; _inner.Bias = Bias; _inner.Temperature = Temperature; _inner.Sample = Sample;
                var own = _inner.Decide(in p, obs, ref b, in m, ref rng); // the network always runs (its recurrent state advances)
                if (_mix.NextInt(1000) < _a._betaPermille)
                    return God.Sim.Unified.Percept.ControlOf(State, in p, label.Control, label.Target, label.Source, label.Slot, label.Design, label.Gesture, label.R1, label.R2, label.Quantity);
                return own;
            }
        }
    }
}
