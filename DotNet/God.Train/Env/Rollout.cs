using System;
using System.Collections.Generic;
using God.Sim;
using God.Sim.Unified;
using God.Train.Net;

namespace God.Train.Env
{
    /// <summary>Who decides for the persons of one episode (the network, or the training-only teacher).</summary>
    public interface IActor
    {
        ControlOrigin Origin { get; }
        /// <summary>Called once per episode before the first decision.</summary>
        void Begin(IWorld w, long seed);
        ActionChoice Act(IWorld w, int personIndex, int person, short[] obs, in ActMask m);
    }

    /// <summary>The quantised network as an actor: per-person recurrent state in <see cref="BrainState.Recurrent"/> and a keyed policy stream.</summary>
    public sealed class NetActor : IActor
    {
        private readonly QNet _net; private readonly bool _greedy;
        private readonly QScratch _sc;
        private Rng[] _rng = new Rng[0];
        public NetActor(QNet net, bool greedy = false) { _net = net; _greedy = greedy; _sc = new QScratch(net.Shape); }
        public ControlOrigin Origin => ControlOrigin.Policy;

        public void Begin(IWorld w, long seed)
        {
            _rng = new Rng[w.People.Count];
            for (int i = 0; i < _rng.Length; i++) _rng[i] = new Rng((ulong)seed).Fork("un6t-policy", w.People[i]);
        }

        public ActionChoice Act(IWorld w, int personIndex, int person, short[] obs, in ActMask m)
            => _net.Act(obs, w.BrainOf(person).Recurrent, in m, _greedy ? null : _rng[personIndex], _sc);
    }

    /// <summary>One recorded decision of a demonstration (training only).</summary>
    public sealed class Sample
    {
        public short[] Obs; public ActMask Mask; public ActionChoice Label;
    }

    /// <summary>One person's decisions of one episode, in order (behaviour cloning carries the recurrent state along it).</summary>
    public sealed class Sequence
    {
        public long Seed; public int Person;
        public ControlOrigin Origin;
        public List<Sample> Samples = new List<Sample>();
    }

    public sealed class PersonStats
    {
        public int Person; public bool Adult; public long DeathTick = -1; public long Return;
        public long PersonTicks, PainTicksHungerThirst, PainTicksAnyActive;
        public int CellsVisited;
    }

    public sealed class EpisodeStats
    {
        public long Seed; public string Layout = "train"; public long Ticks; public int TicksPerDay;
        public List<PersonStats> People = new List<PersonStats>();
        public WorldFacts Facts;
        public long Decisions, TeacherSubmissions;
        public long TotalReturn;
        public ulong FinalHash;

        public int AdultsAtStart() { int n = 0; foreach (var p in People) if (p.Adult) n++; return n; }
        public int AdultsAliveAt(long tick) { int n = 0; foreach (var p in People) if (p.Adult && (p.DeathTick < 0 || p.DeathTick > tick)) n++; return n; }
    }

    /// <summary>An evaluation saw a teacher submission: the rollout is invalid and the evaluation aborts.</summary>
    public sealed class EvaluationInvalidException : Exception
    {
        public EvaluationInvalidException(string m) : base(m) { }
    }

    /// <summary>
    /// Runs one episode: every tick, each person at a decision point (ascending id) observes, the actor chooses, the world decodes
    /// and submits with the actor's origin; then the world steps and every person's return is accumulated from its body only.
    /// </summary>
    public static class Rollout
    {
        public static EpisodeStats Run(IWorld w, IActor actor, long seed, long ticks, bool evaluation, List<Sequence> record = null)
        {
            if (evaluation && actor.Origin == ControlOrigin.Teacher)
                throw new EvaluationInvalidException("evaluation rollout invalid: an actor with origin Teacher was asked to decide");
            var st = new EpisodeStats { Seed = seed, TicksPerDay = w.TicksPerDay, Facts = w.Facts };
            int n = w.People.Count;
            var prev = new BodyReading[n]; var now = new BodyReading[n];
            Sequence[] seqs = record != null ? new Sequence[n] : null;
            for (int i = 0; i < n; i++)
            {
                prev[i] = new BodyReading(); now[i] = new BodyReading();
                w.ReadBody(w.People[i], prev[i]);
                st.People.Add(new PersonStats { Person = w.People[i], Adult = w.AdultAtStart(w.People[i]) });
                if (seqs != null) { seqs[i] = new Sequence { Seed = seed, Person = w.People[i], Origin = actor.Origin }; record.Add(seqs[i]); }
            }
            actor.Begin(w, seed);
            var obs = new short[ObsSchema.Length];
            int hunger = (int)NeedChannel.Hunger, thirst = (int)NeedChannel.Thirst, band = w.PainBand, mask = w.NeedMask;
            for (long t = 0; t < ticks; t++)
            {
                for (int i = 0; i < n; i++)
                {
                    var ps = st.People[i];
                    if (!prev[i].Alive) continue;
                    ps.PersonTicks++;
                    var nd = prev[i].Needs;
                    if (nd[hunger] >= band || nd[thirst] >= band) ps.PainTicksHungerThirst++;
                    for (int c = 0; c < 12; c++) if ((mask & (1 << c)) != 0 && nd[c] >= band) { ps.PainTicksAnyActive++; break; }
                    int person = w.People[i];
                    if (!w.ReadyToDecide(person)) continue;
                    var pc = default(Percept); var m = default(ActMask);
                    w.Observe(person, ref pc, obs, ref m);
                    var a = actor.Act(w, i, person, obs, in m);
                    if (seqs != null) seqs[i].Samples.Add(new Sample { Obs = (short[])obs.Clone(), Mask = m, Label = a });
                    var ctl = w.Decode(person, in a);
                    if (actor.Origin == ControlOrigin.Teacher)
                    {
                        st.TeacherSubmissions++;
                        if (evaluation) throw new EvaluationInvalidException("evaluation rollout invalid: a ControlOrigin.Teacher submission");
                    }
                    w.Submit(in ctl, actor.Origin);
                    st.Decisions++;
                }
                w.Step();
                for (int i = 0; i < n; i++)
                {
                    if (!prev[i].Alive) continue;
                    w.ReadBody(w.People[i], now[i]);
                    long r = Returns.Tick(prev[i], now[i], mask, ticks - 1 - t);
                    st.People[i].Return += r;
                    if (!now[i].Alive) st.People[i].DeathTick = w.Tick;
                    prev[i].CopyFrom(now[i]);
                }
            }
            st.Ticks = ticks;
            foreach (var p in st.People) { p.CellsVisited = w.CellsVisited(p.Person); st.TotalReturn += p.Return; }
            st.FinalHash = w.StateHash();
            return st;
        }
    }
}
