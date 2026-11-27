using System;
using System.IO;
using System.Runtime.Serialization;
using God.Sim.Comms;
using Newtonsoft.Json;

namespace God.Sim.Social
{
    /// <summary>Flags of a <see cref="Relation"/>, refreshed daily from M4's reads (SHARED_CONTRACTS 4.2) or, with M5 alone, from shared hut rights.</summary>
    public static class RelFlags
    {
        /// <summary>A household mate (M4), or with no households a hut-mate (shares a hut's sleeping rights, SHARED_CONTRACTS 5).</summary>
        public const byte HouseholdMate = 1;
        public const byte Partner = 2;
    }

    /// <summary>
    /// What one NPC remembers about another (SOCIAL_MEMORY 3, the M5 part): integers only, the observer's belief. Keyed by
    /// the other's NpcId as recognised (<see cref="Other"/>), never read from the truth.
    /// </summary>
    public struct Relation
    {
        public int Other;
        /// <summary>First and last interaction (not mere sighting).</summary>
        public int FirstTick, LastTick;
        /// <summary>The last tick and node it was seen at (where to look for it).</summary>
        public int LastSeenTick, LastNode;
        /// <summary>The last sim day counted as a day of contact (familiarity +40 once a day).</summary>
        public int LastContactDay;
        public short Familiarity, Trust, Affection, Fear, Grudge;
        /// <summary>The carried fraction of the daily grudge decay (thousandths), so the decay is exact over many days.</summary>
        public short GrudgeRem;
        public short HelpGiven, HelpReceived, OwedToMe, IOwe;
        /// <summary>Believed relatedness r̂/2 from M4's kin beliefs (0 none), refreshed daily.</summary>
        public byte KinCue;
        public byte Flags;
        public byte Betrayals, Apologies, Forgiven;
        /// <summary>0 none, 1 cooperated, 2 defected (the last exchange or help), mine and theirs.</summary>
        public byte LastMoveMine, LastMoveTheirs;
        /// <summary>Own counts for the relations card: deals completed, deals where it cheated me, gifts in and out.</summary>
        public byte DealsDone, Cheated, GiftsIn, GiftsOut;
        /// <summary>The wares it has seen this person show or hand over (bits: berries 1, roots 2, fish 4, logs 8), and when last.</summary>
        public byte Holds;
        public int HoldsTick;
    }

    public enum DealKind : byte { None = 0, Spot = 1, Gift = 2, Help = 3 }

    public enum DealStatus : byte { None = 0, Proposed = 1, Agreed = 2, Done = 3, Failed = 4, Refused = 5, Abandoned = 6 }

    /// <summary>
    /// One negotiation as this mind remembers it (its own copy; the other side keeps its own, possibly different). The
    /// terms are what it meant (own offers) or what it decoded (the other's). Nothing outside this mind reads it to decide.
    /// </summary>
    public struct DealMemory
    {
        public int DealId;
        /// <summary>The partner as recognised (0: not placed) and its opaque handle (for intents only, 13.12).</summary>
        public int Partner, Handle;
        /// <summary>The last message of the negotiation and the request a gift or help answers.</summary>
        public int MessageId, Cause;
        public int OpenedTick, LastTick;
        public DealKind Kind;
        public DealStatus Status;
        public byte Rounds;
        public bool IProposed;
        /// <summary>What I give and what I get, as I believe the terms; and how much has passed each way.</summary>
        public Ware MyWare, TheirWare;
        public byte MyCount, TheirCount, MyGiven, TheirGiven;
    }

    /// <summary>A heard message (its decoded form) with what the listener saw of the speaker: its handle (for an answer), node and whether it presented goods.</summary>
    public struct InboxEntry
    {
        public DecodedMessage Msg;
        public int Handle;
        public int Node;
        public bool Addressed, Answered, Presented;
        /// <summary>The listener has read it (at its first think after the gesture): its beliefs were updated from it.</summary>
        public bool Read;
    }

    /// <summary>The social action running now (one at a time): its learning verb, target, deal and message, and what it hands over.</summary>
    public struct SocialAct
    {
        public byte Verb;
        public int Handle, TargetAs, DealId, MessageId;
        public Ware Ware;
        public int Count;
        public long StartTick;
    }

    /// <summary>A sighting of someone with no relation yet (SOCIAL 3 "strangers seen", ring of 8).</summary>
    public struct SeenEntry
    {
        public int Other, Tick;
    }

    /// <summary>One social candidate's content, composed while the candidates are built (scratch, never saved).</summary>
    public struct PendingCand
    {
        public byte Verb;
        /// <summary>Its index in the think's candidates.</summary>
        public int Cand;
        public int Handle, As, Node, Dist;
        public GestureAct Act;
        public MsgTopic Topic, Counter;
        public byte Quantity;
        public MsgTime Time;
        public int DealId, AnswersMessage;
        /// <summary>The ware and count it hands over (HandOver, Give).</summary>
        public Ware Ware;
        public int Count;
        /// <summary>The believed surplus of accepting (Accept, Refuse, counter Offer) in value units, for the social term.</summary>
        public int Surplus;
        public byte Quality;
        /// <summary>A message to anyone watching (a warning): the handle only names whom it faces.</summary>
        public bool Broadcast;
    }

    /// <summary>
    /// <c>NpcMind.Social</c>: one mind's social memory (SOCIAL_MEMORY 3; COMMUNICATION 4.9): relations (bounded, evicted by
    /// importance), the inbox of decoded messages, its own memory of negotiations, the running social act, price beliefs,
    /// the strangers seen ring and daily counters. OWNER: M5. Null until M5 gives a mind one; hashed with the mind only when
    /// non-null. The scratch fields (recognition of this think, composed candidates) are rebuilt every think and never saved.
    /// </summary>
    public sealed class SocialMind
    {
        public const int MaxDeals = 6, PriceWares = 32, SeenSlots = 8, MaxPending = 8, MaxRecognised = 8;

        [JsonIgnore] public Relation[] Rel;
        public int RelCount;
        /// <summary>The relations, packed for the save (relations are fixed-size records).</summary>
        public string Blob;

        public InboxEntry[] Inbox;
        public int InboxCount;
        public DealMemory[] Deals = new DealMemory[MaxDeals];
        public int DealCount;
        public SocialAct Act;
        /// <summary>Price beliefs: the worth of one unit of each ware in this NPC's own value units (0 unknown), ECONOMY 4.5.</summary>
        public short[] Price = new short[PriceWares];
        public SeenEntry[] Seen = new SeenEntry[SeenSlots];
        public int SeenNext;
        /// <summary>Its own last request for food or help: the message, whom it asked (as recognised), when.</summary>
        public int LastAskMsg, LastAskTarget, LastAskTick;
        /// <summary>When it last warned others (it does not repeat a warning at once).</summary>
        public int LastWarnTick = -100000;
        /// <summary>Today's counters (PersonDaySocial) and lifetime totals.</summary>
        public int DaySent, DayHeard, DayMisread, DayGiven, DayReceived, DayDeals, DayCheated;
        public int Sent, Heard, Misread;

        // ---------------- scratch (never saved) ----------------
        [JsonIgnore] public readonly int[] RecTrue = new int[MaxRecognised], RecAs = new int[MaxRecognised];
        [JsonIgnore] public int RecCount;
        [JsonIgnore] public long RecTick = -1;
        [JsonIgnore] public readonly PendingCand[] Pending = new PendingCand[MaxPending];
        [JsonIgnore] public int PendingCount;
        /// <summary>The tuning this mind decides by (set by the sim side; a mind never sees <c>GodState</c>).</summary>
        [JsonIgnore] public SocialTuning Tuning;
        [JsonIgnore] public CommsTuning CommsTuning;

        public SocialMind() { }

        public SocialMind(int relSlots, int inboxSlots)
        {
            Rel = new Relation[relSlots];
            Inbox = new InboxEntry[inboxSlots];
        }

        public int RelCap => Rel.Length;

        public void ClearPending() { PendingCount = 0; }

        /// <summary>The composed content of candidate <paramref name="cand"/> of this think, or -1.</summary>
        public int PendingOf(int cand)
        {
            for (int i = 0; i < PendingCount; i++) if (Pending[i].Cand == cand) return i;
            return -1;
        }

        /// <summary>The composed content of the candidate with this verb and target (the chosen one), or -1.</summary>
        public int PendingOf(byte verb, int handle)
        {
            for (int i = 0; i < PendingCount; i++) if (Pending[i].Verb == verb && Pending[i].Handle == handle) return i;
            return -1;
        }

        /// <summary>The bytes of the fixed tables (M5.1 budget: at most 4096 per mind).</summary>
        public int Bytes =>
            16 + Rel.Length * System.Runtime.InteropServices.Marshal.SizeOf<Relation>() + Inbox.Length * 48 + Deals.Length * 40
            + Price.Length * 2 + Seen.Length * 8 + 64;

        public int Find(int other)
        {
            if (other == 0) return -1;
            for (int i = 0; i < RelCount; i++) if (Rel[i].Other == other) return i;
            return -1;
        }

        public int FindDeal(int dealId)
        {
            if (dealId == 0) return -1;
            for (int i = 0; i < DealCount; i++) if (Deals[i].DealId == dealId) return i;
            return -1;
        }

        /// <summary>The identity this think gave a seen person (recognition), or -1 when it was not seen this think.</summary>
        public int RecognisedAs(int trueId)
        {
            for (int i = 0; i < RecCount; i++) if (RecTrue[i] == trueId) return RecAs[i];
            return -1;
        }

        // ---------------- hash ----------------

        public void AddTo(ref StateHasher h)
        {
            h.Add(RelCount);
            for (int i = 0; i < RelCount; i++)
            {
                var r = Rel[i];
                h.Add(r.Other); h.Add(r.FirstTick); h.Add(r.LastTick); h.Add(r.LastSeenTick); h.Add(r.LastNode); h.Add(r.LastContactDay);
                h.Add(r.Familiarity | (r.Trust << 16)); h.Add(r.Affection | (r.Fear << 16)); h.Add(r.Grudge | (r.HelpGiven << 16));
                h.Add(r.HelpReceived | (r.OwedToMe << 16)); h.Add(r.IOwe);
                h.Add(r.KinCue | (r.Flags << 8) | (r.Betrayals << 16) | (r.Apologies << 24));
                h.Add(r.Forgiven | (r.LastMoveMine << 8) | (r.LastMoveTheirs << 16) | (r.DealsDone << 24));
                h.Add(r.Cheated | (r.GiftsIn << 8) | (r.GiftsOut << 16) | (r.Holds << 24)); h.Add(r.GrudgeRem); h.Add(r.HoldsTick);
            }
            h.Add(InboxCount);
            for (int i = 0; i < InboxCount; i++)
            {
                var e = Inbox[i];
                AddMsg(ref h, e.Msg);
                h.Add(e.Handle); h.Add(e.Node); h.Add((e.Addressed ? 1 : 0) | (e.Answered ? 2 : 0) | (e.Presented ? 4 : 0) | (e.Read ? 8 : 0));
            }
            h.Add(DealCount);
            for (int i = 0; i < DealCount; i++)
            {
                var d = Deals[i];
                h.Add(d.DealId); h.Add(d.Partner); h.Add(d.Handle); h.Add(d.MessageId); h.Add(d.Cause); h.Add(d.OpenedTick); h.Add(d.LastTick);
                h.Add((int)d.Kind | ((int)d.Status << 8) | (d.Rounds << 16) | (d.IProposed ? 1 << 24 : 0));
                h.Add((int)d.MyWare | ((int)d.TheirWare << 8)); h.Add(d.MyCount | (d.TheirCount << 8) | (d.MyGiven << 16) | (d.TheirGiven << 24));
            }
            h.Add(Act.Verb); h.Add(Act.Handle); h.Add(Act.TargetAs); h.Add(Act.DealId); h.Add(Act.MessageId); h.Add((int)Act.Ware); h.Add(Act.Count); h.Add(Act.StartTick);
            for (int i = 0; i < Price.Length; i++) h.Add(Price[i]);
            for (int i = 0; i < Seen.Length; i++) { h.Add(Seen[i].Other); h.Add(Seen[i].Tick); }
            h.Add(SeenNext); h.Add(LastAskMsg); h.Add(LastAskTarget); h.Add(LastAskTick); h.Add(LastWarnTick);
            h.Add(DaySent); h.Add(DayHeard); h.Add(DayMisread); h.Add(DayGiven); h.Add(DayReceived); h.Add(DayDeals); h.Add(DayCheated);
            h.Add(Sent); h.Add(Heard); h.Add(Misread);
        }

        private static void AddMsg(ref StateHasher h, in DecodedMessage m)
        {
            h.Add(m.MessageId); h.Add(m.Speaker); h.Add((int)m.Act | ((int)m.Topic.Kind << 8) | ((int)m.Counter.Kind << 16) | ((int)m.Time << 24));
            h.Add(m.Topic.Id); h.Add(m.Topic.Sub); h.Add(m.Quantity); h.Add(m.Counter.Id); h.Add(m.Counter.Sub); h.Add(m.Target); h.Add(m.DealId); h.Add(m.Tick);
        }

        // ---------------- save (relations packed, as the belief store) ----------------

        [OnSerializing] internal void OnSerializingMethod(StreamingContext c) { Blob = Pack(); }
        [OnSerialized] internal void OnSerializedMethod(StreamingContext c) { Blob = null; }
        [OnDeserialized] internal void OnDeserializedMethod(StreamingContext c) { if (Blob != null) { Unpack(Blob); Blob = null; } }

        private string Pack()
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(Rel.Length);
                for (int i = 0; i < RelCount; i++)
                {
                    var r = Rel[i];
                    w.Write(r.Other); w.Write(r.FirstTick); w.Write(r.LastTick); w.Write(r.LastSeenTick); w.Write(r.LastNode); w.Write(r.LastContactDay);
                    w.Write(r.Familiarity); w.Write(r.Trust); w.Write(r.Affection); w.Write(r.Fear); w.Write(r.Grudge);
                    w.Write(r.HelpGiven); w.Write(r.HelpReceived); w.Write(r.OwedToMe); w.Write(r.IOwe);
                    w.Write(r.KinCue); w.Write(r.Flags); w.Write(r.Betrayals); w.Write(r.Apologies); w.Write(r.Forgiven);
                    w.Write(r.LastMoveMine); w.Write(r.LastMoveTheirs); w.Write(r.DealsDone); w.Write(r.Cheated); w.Write(r.GiftsIn); w.Write(r.GiftsOut); w.Write(r.GrudgeRem); w.Write(r.Holds); w.Write(r.HoldsTick);
                }
                w.Flush();
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private void Unpack(string blob)
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(blob)))
            using (var rd = new BinaryReader(ms))
            {
                Rel = new Relation[rd.ReadInt32()];
                for (int i = 0; i < RelCount; i++)
                {
                    var r = new Relation
                    {
                        Other = rd.ReadInt32(), FirstTick = rd.ReadInt32(), LastTick = rd.ReadInt32(), LastSeenTick = rd.ReadInt32(), LastNode = rd.ReadInt32(),
                        LastContactDay = rd.ReadInt32(),
                        Familiarity = rd.ReadInt16(), Trust = rd.ReadInt16(), Affection = rd.ReadInt16(), Fear = rd.ReadInt16(), Grudge = rd.ReadInt16(),
                        HelpGiven = rd.ReadInt16(), HelpReceived = rd.ReadInt16(), OwedToMe = rd.ReadInt16(), IOwe = rd.ReadInt16(),
                        KinCue = rd.ReadByte(), Flags = rd.ReadByte(), Betrayals = rd.ReadByte(), Apologies = rd.ReadByte(), Forgiven = rd.ReadByte(),
                        LastMoveMine = rd.ReadByte(), LastMoveTheirs = rd.ReadByte(), DealsDone = rd.ReadByte(), Cheated = rd.ReadByte(),
                        GiftsIn = rd.ReadByte(), GiftsOut = rd.ReadByte(), GrudgeRem = rd.ReadInt16(), Holds = rd.ReadByte(), HoldsTick = rd.ReadInt32(),
                    };
                    Rel[i] = r;
                }
            }
        }
    }
}
