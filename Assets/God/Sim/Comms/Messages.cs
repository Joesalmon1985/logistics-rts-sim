using System;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Social;

namespace God.Sim.Comms
{
    /// <summary>
    /// A message as its speaker composes it (COMMUNICATION 4.9 grammar): one act and up to five separate parts, each a
    /// separate gesture element (topic, quantity, counter, target, time), plus whether the speaker presents the goods it
    /// offers (a display of the object). The same acts combine with any topic, so a need, an offer, a request for help,
    /// an acceptance, a refusal, cooperation and a warning are all one grammar, not one gesture per transaction (M5.15).
    /// </summary>
    public struct Composed
    {
        public GestureAct Act;
        public MsgTopic Topic;
        public byte Quantity;
        public MsgTopic Counter;
        /// <summary>The addressee (true id, the speaker's handle of a person it sees); 0 = anyone watching.</summary>
        public int Target;
        public MsgTime Time;
        public int DealId;
        public bool Presented;
    }

    /// <summary>
    /// The gesture language (Sim/Comms, M5): composing, emitting and perceiving messages. A message is information only:
    /// nothing here moves goods, writes an obligation or decides a response (SHARED_CONTRACTS 7.1 rule 1; this namespace
    /// never references <c>Goods</c>, static test). A perceiving listener is awake, within its own current sight radius of
    /// the speaker (<see cref="CommsRange.Gesture"/>, A7) and attends the speaker: it is addressed, or the speaker is among
    /// the nearest <see cref="CommsTuning.AttendPeople"/> people it sees. Each listener decodes the message into its own
    /// <see cref="DecodedMessage"/> at once (its belief: the speaker as it recognises it, and possibly misread), into its
    /// inbox, and reads it at its first think after the gesture (rule 3).
    /// </summary>
    public static class Messages
    {
        // ---------------- composition (the grammar) ----------------

        public static MsgTopic OfWare(Ware w) => new MsgTopic { Kind = TopicKind.Ware, Id = (int)w };
        public static MsgTopic AtPlace(int node) => new MsgTopic { Kind = TopicKind.Place, Id = node };
        public static MsgTopic AboutPerson(int npc) => new MsgTopic { Kind = TopicKind.Npc, Id = npc };

        public static Composed Compose(GestureAct act, MsgTopic topic = default, int quantity = 0, MsgTopic counter = default, int target = 0,
            MsgTime time = MsgTime.Now, int dealId = 0, bool presented = false) =>
            new Composed
            {
                Act = act, Topic = topic, Quantity = (byte)Fx.Clamp(quantity, 0, 9), Counter = counter, Target = target, Time = time, DealId = dealId,
                Presented = presented,
            };

        /// <summary>The learning verb of saying an act: <c>Say(act) = 64 + act</c> (SHARED_CONTRACTS 6.1).</summary>
        public static byte SayVerb(GestureAct act) => (byte)(64 + (int)act);
        public static bool IsSay(byte verb) => verb >= 64 && verb <= 95;
        public static GestureAct ActOf(byte sayVerb) => (GestureAct)(sayVerb - 64);

        // ---------------- emission and perception ----------------

        /// <summary>
        /// The speaker makes the gesture: a new <see cref="DealId"/>-carrying message id, one <see cref="GestureMade"/>, and
        /// for each perceiving listener (ascending id) a decoded copy in its inbox and one <see cref="MessageDecoded"/>.
        /// Returns the message id.
        /// </summary>
        public static int Emit(GodState s, EventBuffer ev, Npc speaker, in Composed m)
        {
            var soc = s.Social;
            int id = soc.NextMessageId++;
            soc.Gestures++;
            var gm = new GestureMade
            {
                MessageId = id, Speaker = speaker.Id, Act = m.Act, TopicKind = m.Topic.Kind, TopicId = m.Topic.Id, TopicSub = m.Topic.Sub,
                Quantity = m.Quantity, CounterKind = m.Counter.Kind, CounterId = m.Counter.Id, CounterSub = m.Counter.Sub, Target = m.Target,
                Time = m.Time, DealId = m.DealId, Node = speaker.Node, Presented = m.Presented, Honest = true,
                DurationTicks = s.Tuning.Social.GestureTicks, // F1.17: GestureMade v2
            };
            ev?.Add(gm);
            var sm = speaker.Mind?.Social;
            if (sm != null) { sm.DaySent++; sm.Sent++; }
            gm.Listeners = Deliver(s, ev, speaker, m, id);
            return id;
        }

        private static bool Night(GodState s) => PopulationSystem.TimeOf(SimCalendar.Of(s), s.Tick) == TimeOfDay.Night;

        private static int Deliver(GodState s, EventBuffer ev, Npc speaker, in Composed m, int id)
        {
            var p = s.Population;
            var g = s.Territory.Grid;
            bool night = Night(s);
            int sight = CommsRange.Gesture(s.Tuning.Population, night);
            var ct = s.Tuning.Comms;
            int n = 0;
            foreach (var l in p.Npcs)
            {
                if (!Population.Bodies.IsLivingPerson(l) || l.Id == speaker.Id || l.Mind?.Social == null || l.Action.Asleep) continue; // F1.8
                int d = g.Distance(l.Node, speaker.Node);
                if (d > sight) continue;
                bool addressed = m.Target == l.Id;
                if (!addressed && !Attends(p, g, l, speaker, d, sight, ct.AttendPeople)) continue;
                Decode(s, ev, l, speaker, m, id, d, sight, night, addressed);
                n++;
            }
            return n;
        }

        /// <summary>The speaker is among the listener's nearest <paramref name="k"/> people in sight (by distance, then id), as the listener's attention to people is.</summary>
        public static bool Attends(PopulationState p, NodeGrid g, Npc listener, Npc speaker, int d, int sight, int k)
        {
            int nearer = 0;
            foreach (var x in p.Npcs)
            {
                if (!x.Alive || x.Id == listener.Id || x.Id == speaker.Id) continue;
                int dx = g.Distance(listener.Node, x.Node);
                if (dx > sight) continue;
                if (dx < d || dx == d && x.Id < speaker.Id) { if (++nearer >= k) return false; }
            }
            return true;
        }

        private static void Decode(GodState s, EventBuffer ev, Npc l, Npc speaker, in Composed m, int id, int d, int sight, bool night, bool addressed)
        {
            var sm = l.Mind.Social;
            var soc = s.Social;
            var rng = soc.Scratch;
            rng.SetToFork(s.Rng, "decode", l.Id, (s.Tick << 20) | (id & 0xFFFFF));
            // who the listener takes the speaker to be (SOCIAL 4.1): a known person can be missed; a new face is a new identity
            int speakerAs = speaker.Id;
            int ri = sm.Find(speaker.Id);
            int miss = ri >= 0 ? SocialMemory.MissPermille(s.Tuning.Social, sm.Rel[ri].Familiarity, night, d * 2 > sight) : 0;
            if (rng.NextInt(1000) < miss)
            {
                speakerAs = 0;
                ev?.Add(new RecognitionError { Observer = l.Id, Observed = speaker.Id, Kind = 1, Familiarity = sm.Rel[ri].Familiarity });
            }
            var msg = new DecodedMessage
            {
                MessageId = id, Speaker = speakerAs, Act = m.Act, Topic = m.Topic, Quantity = m.Quantity, Counter = m.Counter,
                Target = m.Target == 0 ? 0 : m.Target == l.Id ? l.Id : (sm.Find(m.Target) >= 0 ? m.Target : -1), Time = m.Time, DealId = m.DealId,
                Tick = (int)s.Tick,
            };
            bool mismatch = false;
            var ct = s.Tuning.Comms;
            if (ct.Misunderstanding != 0)
            {
                int chance = ct.MisreadEdgePermille * d / Math.Max(1, sight) + (night ? ct.MisreadDark : 0) - (m.Presented ? ct.MisreadPresented : 0);
                if (rng.NextInt(1000) < Fx.Clamp(chance, 0, 500)) mismatch = Misread(ref msg, rng.NextInt(3));
            }
            if (mismatch) { sm.DayMisread++; sm.Misread++; soc.Misread++; }
            sm.DayHeard++; sm.Heard++; soc.Decoded++;
            Social.SocialSim.Heard(s, ev, l, speaker, speakerAs, msg, addressed, m.Presented);
            ev?.Add(new MessageDecoded
            {
                MessageId = id, Listener = l.Id, Speaker = msg.Speaker, Act = msg.Act, TopicKind = msg.Topic.Kind, TopicId = msg.Topic.Id, TopicSub = msg.Topic.Sub,
                Quantity = msg.Quantity, CounterKind = msg.Counter.Kind, CounterId = msg.Counter.Id, CounterSub = msg.Counter.Sub, Target = msg.Target,
                Time = msg.Time, DealId = msg.DealId, Dist = d, Mismatch = mismatch,
            });
        }

        /// <summary>One field read wrong (COMMUNICATION 4.9 "what an error looks like"): an act as a similar act, a quantity off by one, a ware as a neighbouring ware. False when that field has nothing to confuse.</summary>
        public static bool Misread(ref DecodedMessage msg, int field)
        {
            switch (field)
            {
                case 0:
                {
                    var a = Similar(msg.Act);
                    if (a == msg.Act) return false;
                    msg.Act = a; return true;
                }
                case 1:
                    if (msg.Quantity == 0) return false;
                    msg.Quantity = (byte)(msg.Quantity >= 9 ? 8 : msg.Quantity == 1 ? 2 : msg.Quantity + (msg.MessageId % 2 == 0 ? 1 : -1));
                    return true;
                default:
                {
                    if (msg.Topic.Kind != TopicKind.Ware) return false;
                    var w = Neighbour((Ware)msg.Topic.Id);
                    if ((int)w == msg.Topic.Id) return false;
                    msg.Topic.Id = (int)w; return true;
                }
            }
        }

        public static GestureAct Similar(GestureAct a)
        {
            switch (a)
            {
                case GestureAct.Offer: return GestureAct.Want;
                case GestureAct.Want: return GestureAct.Offer;
                case GestureAct.NeedFood: return GestureAct.NeedHelp;
                case GestureAct.NeedHelp: return GestureAct.NeedFood;
                case GestureAct.Accept: return GestureAct.Cooperate;
                case GestureAct.Cooperate: return GestureAct.Accept;
                default: return a;
            }
        }

        public static Ware Neighbour(Ware w)
        {
            switch (w)
            {
                case Sim.Ware.Berries: return Sim.Ware.Roots;
                case Sim.Ware.Roots: return Sim.Ware.Berries;
                case Sim.Ware.Fish: return Sim.Ware.Roots;
                default: return w;
            }
        }
    }
}
