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
        public CandidatePolicy(QNet net, string label) : base(new BrainNet(BrainShape.Parse(net.Shape.Text), (short[])net.W.Clone())) { _label = label; }
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
