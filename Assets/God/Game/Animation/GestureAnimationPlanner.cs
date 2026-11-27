using System;
using System.Collections.Generic;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Divinity;
using God.Sim.Population;
using God.Sim.Population.Households;
using God.Sim.Social;

namespace God.Game
{
    /// <summary>
    /// Maps sim events to animation cues (NPC_ANIMATION_DESIGN 4 and 6; U3 M5.19). Pure: no Unity types, no sim writes, no
    /// truth flags. The table of handlers is keyed by event type, so later packages register cue kinds (vehicles, machines)
    /// with <see cref="Register{T}"/> without changing this code. It remembers only which deals had a gesture and a decision,
    /// which is what lets a hand-over read as Giving and Receiving (M5.19, H-NA-3); anything else is functional.
    /// </summary>
    public sealed class GestureAnimationPlanner
    {
        private readonly Dictionary<Type, Action<SimEvent, List<AnimationCue>>> _handlers = new Dictionary<Type, Action<SimEvent, List<AnimationCue>>>();
        private readonly HashSet<int> _gestured = new HashSet<int>(), _decided = new HashSet<int>();
        private readonly Dictionary<int, int> _msgDeal = new Dictionary<int, int>();
        private int _nextId = 1;

        /// <summary>Default view seconds a tick stands for (10 ticks per second).</summary>
        public const int TicksPerSecond = 10;

        public GestureAnimationPlanner()
        {
            Register<GestureMade>(OnGesture);
            Register<MessageDecoded>((e, o) => Add(o, e, e.Listener, AnimKind.Listen, e.Speaker, 0.3f));
            Register<ResponseChosen>(OnResponse);
            Register<ExchangeStep>(OnExchange);
            Register<ChildFed>((e, o) => { var c = Add(o, e, e.Feeder, AnimKind.Feeding, e.Child, 0.5f); c.Functional = true; c.Object = new CueObject { Ware = e.Ware, Count = e.Count, Lot = e.Lot }; });
            Register<CareGiven>((e, o) => { var c = Add(o, e, e.Carer, e.Kind == CareGiven.Tend ? AnimKind.Tending : AnimKind.Feeding, e.Patient, 0.5f); c.Functional = true; });
            Register<BeggingMade>((e, o) => Add(o, e, e.Child, AnimKind.Begging, e.Target, 0.3f + 0.2f * Math.Min(3, Math.Max(1, e.Intensity))));
            Register<InjuryTaken>((e, o) => Add(o, e, e.Npc, AnimKind.Fear, 0, 0.5f));
            Register<CommandResponded>(OnCommandResponded);
            Register<MiracleEffectApplied>(OnMiracleEffect);
        }

        /// <summary>Adds or replaces the handler for an event type (the extension point for later packages).</summary>
        public void Register<T>(Action<T, List<AnimationCue>> handler) where T : SimEvent
            => _handlers[typeof(T)] = (e, o) => handler((T)e, o);

        /// <summary>The cues for one event (empty when the event has none). Same event stream in, same cues out.</summary>
        public List<AnimationCue> Plan(SimEvent e)
        {
            var o = new List<AnimationCue>(2);
            if (e != null && _handlers.TryGetValue(e.GetType(), out var h)) h(e, o);
            return o;
        }

        /// <summary>Forgets what it saw (a loaded game starts with nothing).</summary>
        public void Reset() { _gestured.Clear(); _decided.Clear(); _msgDeal.Clear(); _nextId = 1; }

        public static long EventKey(SimEvent e) => (e.Tick << 20) | (uint)(e.Seq & 0xFFFFF);

        private AnimationCue Add(List<AnimationCue> o, SimEvent e, int actor, AnimKind kind, int target, float intensity, int duration = 0)
        {
            var clip = PoseClips.Of(kind);
            var c = new AnimationCue
            {
                Id = _nextId++, SourceEventId = EventKey(e), Actor = actor, Kind = kind, Target = target, StartTick = e.Tick,
                DurationTicks = duration, Phases = clip?.Phases, Intensity = intensity,
            };
            o.Add(c);
            return c;
        }

        /// <summary>The act's main clip family (the NPC_ANIMATION 6.2 table), and the one that follows it, if any.</summary>
        public static AnimKind FamilyOf(GestureAct act, TopicKind topic = TopicKind.None)
        {
            switch (act)
            {
                case GestureAct.Want: case GestureAct.NeedHelp: return AnimKind.Requesting;
                case GestureAct.Offer: return AnimKind.Offering;
                case GestureAct.Accept: return topic == TopicKind.Ware ? AnimKind.Accepting : AnimKind.Agreeing;
                case GestureAct.Refuse: return AnimKind.Refusing;
                case GestureAct.NeedFood: return AnimKind.Begging;
                case GestureAct.Cooperate: return AnimKind.Cooperating;
                case GestureAct.Promise: case GestureAct.Accuse: return AnimKind.Pointing;
                case GestureAct.Follow: case GestureAct.Greet: return AnimKind.Attention;
                case GestureAct.Warn: case GestureAct.Alarm: return AnimKind.Warning;
                case GestureAct.Surrender: case GestureAct.AskJoin: return AnimKind.Submission;
                case GestureAct.Defend: case GestureAct.Threaten: return AnimKind.Threatening;
                case GestureAct.Apologise: return AnimKind.Apologising;
                case GestureAct.Forgive: case GestureAct.Praise: return AnimKind.Agreeing;
                case GestureAct.Teach: case GestureAct.Demonstrate: return AnimKind.Teaching;
                case GestureAct.Pray: case GestureAct.Worship: return AnimKind.Praying;
                case GestureAct.Thank: return AnimKind.Gratitude;
                default: return AnimKind.Attention;
            }
        }

        private void OnGesture(GestureMade g, List<AnimationCue> o)
        {
            _gestured.Add(g.DealId);
            _msgDeal[g.MessageId] = g.DealId;
            var main = FamilyOf(g.Act, g.TopicKind);
            if (g.Target != 0 && main != AnimKind.Attention && main != AnimKind.Submission && main != AnimKind.Praying && main != AnimKind.Begging)
                Add(o, g, g.Speaker, AnimKind.Attention, g.Target, 0.3f);
            float inten = g.Act == GestureAct.Threaten || g.Act == GestureAct.Defend ? 1f : g.Act == GestureAct.Warn || g.Act == GestureAct.Alarm ? 0.8f : 0.5f;
            var c = Add(o, g, g.Speaker, main, g.Target, inten, g.DurationTicks);
            c.Quantity = g.Quantity > 9 ? 9 : g.Quantity;
            if (g.Act == GestureAct.Offer)
            {
                // the goods it gives: held when it carries them (Presented), else the counter is only shown by fingers
                if (g.Presented && g.CounterKind == TopicKind.Ware) c.Object = new CueObject { Ware = (Ware)g.CounterId, Count = g.CounterSub, Lot = 0 };
                if (g.TopicKind != TopicKind.Ware) c.Quantity = 0;
            }
            if (g.Act == GestureAct.Cooperate) o.Insert(o.Count - 1, new AnimationCue
            {
                Id = _nextId++, SourceEventId = EventKey(g), Actor = g.Speaker, Kind = AnimKind.Agreeing, Target = g.Target, StartTick = g.Tick,
                Phases = PoseClips.Of(AnimKind.Agreeing).Phases, Intensity = 0.4f,
            });
        }

        private void OnResponse(ResponseChosen r, List<AnimationCue> o)
        {
            // the listener's own decision is recorded so a later hand-over can be read as part of the exchange; it has no cue
            // of its own (nothing is invented for a decision the sim did not draw as a gesture).
            if (!r.Ignored && _msgDeal.TryGetValue(r.MessageId, out int deal)) _decided.Add(deal);
        }

        private void OnExchange(ExchangeStep s, List<AnimationCue> o)
        {
            if (!_gestured.Contains(s.DealId) || !_decided.Contains(s.DealId)) return; // H-NA-3: no gesture and decision, no Giving
            var obj = new CueObject { Ware = s.Ware, Count = s.Count, Lot = s.Lot };
            var give = Add(o, s, s.Giver, AnimKind.Giving, s.Receiver, 0.5f); give.Object = obj;
            var take = Add(o, s, s.Receiver, AnimKind.Receiving, s.Giver, 0.5f); take.Object = obj;
        }

        private void OnCommandResponded(CommandResponded r, List<AnimationCue> o)
        {
            // the person's body language for its own decision (NPC_ANIMATION 10); ignored, lapsed and misread draw nothing
            if (r.Response == CommandResponse.Followed) Add(o, r, r.Npc, AnimKind.Agreeing, 0, 0.6f);
            else if (r.Response == CommandResponse.Refused) Add(o, r, r.Npc, AnimKind.Refusing, 0, 0.7f);
        }

        private void OnMiracleEffect(MiracleEffectApplied m, List<AnimationCue> o)
        {
            // reactions only from the person's own effect row
            if (m.ThingKind == MiracleThing.Person || m.ThingKind == MiracleThing.Witness)
            {
                bool better = (m.Field == "injury" || m.Field == "illness") && m.After < m.Before || m.Field == "health" && m.After > m.Before || m.Field == "blessing" && m.After > m.Before;
                if (better && !m.Death) Add(o, m, m.ThingId, AnimKind.Gratitude, 0, 0.7f);
                else if (m.Field == "fear" && m.After > m.Before) Add(o, m, m.ThingId, AnimKind.Fear, 0, 0.8f);
            }
        }
    }
}
