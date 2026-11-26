using System;
using System.IO;
using System.Runtime.Serialization;
using God.Sim.Learning;
using God.Sim.Policy;
using Newtonsoft.Json;

namespace God.Sim.Cognition
{
    /// <summary>The kinds of fact an NPC believes (COMMUNICATION_AND_COGNITION 4.4; M3: places).</summary>
    public enum BeliefKind : byte { None = 0, Food = 1, Wood = 2, Pile = 3, Hut = 4, Tool = 5 }

    /// <summary>
    /// One belief (layer 6): "berries at node 412, plenty, seen 2 days ago". It names a place and a class, never a true
    /// object id (except the NPC's own hut, which it knows as its own). Confidence decays with age; going back and
    /// seeing a change updates it. Beliefs can be wrong: a source may have been emptied since.
    /// </summary>
    public struct Belief
    {
        public BeliefKind Kind;
        /// <summary>The <see cref="ObjClass"/> believed there.</summary>
        public byte Class;
        /// <summary>The stock last seen: <see cref="ObjState"/> StockHigh, StockLow or StockEmpty (0 = unknown).</summary>
        public byte Stock;
        public short Confidence;
        /// <summary>The node it is about.</summary>
        public int Subject;
        /// <summary>A known reference: the hut id for the NPC's own hut or site, the lot id for a tool seen lying; else 0.</summary>
        public int Ref;
        public int TickLearned;
    }

    /// <summary>
    /// <c>Mind.Beliefs</c>: a bounded store of facts about places (64 slots by default), forgetting the lowest
    /// confidence x importance first. Saved as one packed blob.
    /// </summary>
    public sealed class BeliefStore
    {
        [JsonIgnore] public Belief[] Slots;
        public int Count;
        public string Blob;

        public BeliefStore() { }
        public BeliefStore(int cap) { Slots = new Belief[cap]; }

        public int Cap => Slots.Length;

        public int IndexOf(BeliefKind kind, int subject)
        {
            for (int i = 0; i < Count; i++) if (Slots[i].Kind == kind && Slots[i].Subject == subject) return i;
            return -1;
        }

        public static int Importance(BeliefKind k)
        {
            switch (k)
            {
                case BeliefKind.Hut: return 4;
                case BeliefKind.Food: return 3;
                case BeliefKind.Wood: return 2;
                case BeliefKind.Tool: return 2;
                default: return 1;
            }
        }

        /// <summary>Adds or refreshes a belief. When full, the weakest (confidence x importance, ties lowest index) goes.</summary>
        public void Upsert(BeliefKind kind, int subject, int cls, int stock, int reference, int tick)
        {
            int i = IndexOf(kind, subject);
            if (i < 0)
            {
                if (Count < Slots.Length) i = Count++;
                else
                {
                    i = 0;
                    int worst = int.MaxValue;
                    for (int j = 0; j < Count; j++)
                    {
                        int w = Slots[j].Confidence * Importance(Slots[j].Kind);
                        if (w < worst) { worst = w; i = j; }
                    }
                    int mine = 1000 * Importance(kind);
                    if (worst > mine) return;
                }
            }
            Slots[i] = new Belief { Kind = kind, Class = (byte)cls, Stock = (byte)stock, Confidence = 1000, Subject = subject, Ref = reference, TickLearned = tick };
        }

        public void RemoveAt(int i)
        {
            for (int j = i; j < Count - 1; j++) Slots[j] = Slots[j + 1];
            Count--;
            Slots[Count] = default;
        }

        /// <summary>Daily ageing: every belief loses confidence; one at zero is forgotten (own hut beliefs never fade).</summary>
        public void Age(int lossPerDay)
        {
            for (int i = 0; i < Count;)
            {
                if (Slots[i].Kind == BeliefKind.Hut && Slots[i].Ref != 0) { i++; continue; }
                int c = Slots[i].Confidence - lossPerDay;
                if (c <= 0) RemoveAt(i);
                else { Slots[i].Confidence = (short)c; i++; }
            }
        }

        public void AddTo(ref StateHasher h)
        {
            h.Add(Count);
            for (int i = 0; i < Count; i++)
            {
                var b = Slots[i];
                h.Add((int)b.Kind | (b.Class << 8) | (b.Stock << 16)); h.Add(b.Confidence); h.Add(b.Subject); h.Add(b.Ref); h.Add(b.TickLearned);
            }
        }

        [OnSerializing] internal void OnSerializingMethod(StreamingContext c) { Blob = Pack(); }
        [OnSerialized] internal void OnSerializedMethod(StreamingContext c) { Blob = null; }
        [OnDeserialized] internal void OnDeserializedMethod(StreamingContext c) { if (Blob != null) { Unpack(Blob); Blob = null; } }

        private string Pack()
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(Slots.Length);
                for (int i = 0; i < Count; i++)
                {
                    var b = Slots[i];
                    w.Write((byte)b.Kind); w.Write(b.Class); w.Write(b.Stock); w.Write(b.Confidence); w.Write(b.Subject); w.Write(b.Ref); w.Write(b.TickLearned);
                }
                w.Flush();
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private void Unpack(string blob)
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(blob)))
            using (var r = new BinaryReader(ms))
            {
                Slots = new Belief[r.ReadInt32()];
                for (int i = 0; i < Count; i++)
                    Slots[i] = new Belief { Kind = (BeliefKind)r.ReadByte(), Class = r.ReadByte(), Stock = r.ReadByte(), Confidence = r.ReadInt16(), Subject = r.ReadInt32(), Ref = r.ReadInt32(), TickLearned = r.ReadInt32() };
            }
        }
    }

    /// <summary>
    /// The last decision and why (the inspector's "why" line, NPC_LEARNING 6): the winner's verb and target, its score
    /// terms, whether the learned term changed the winner or exploration alone won it, and the three keys that
    /// contributed most to its learned value.
    /// </summary>
    public struct DecisionRecord
    {
        public int Tick;
        public byte Verb;
        public IntentTarget TargetKind;
        public int TargetId, TargetNode;
        public byte TargetClass;
        public int Instinct, Learned, Social, Culture, Personality, Cost, Explore, Total;
        public int Candidates;
        public bool LearnedChangedWinner, ExploreWon;
        public uint Key0, Key1, Key2;
        public short Stw0, Stw1, Stw2;
        /// <summary>The highest learned value among the candidates (raw), the bootstrap of the TD error.</summary>
        public int MaxLearned;
        /// <summary>The runner-up's verb and total (how close the choice was).</summary>
        public byte SecondVerb;
        public int SecondTotal;
    }

    /// <summary>One attended thing (for the inspector): class and place, salience.</summary>
    public struct AttendedThing
    {
        public byte Class;
        public int Node;
        public int Salience;
    }

    /// <summary>
    /// An NPC's mind: its learning store, beliefs, what it attended last, its last decision and why, and daily counters
    /// for the person snapshot. Holds nothing of the true state.
    /// </summary>
    public sealed class NpcMind
    {
        public AssociationStore Store;
        public BeliefStore Beliefs;
        public DecisionRecord Last;
        public AttendedThing[] Attended = new AttendedThing[3];
        public int AttendedCount;
        /// <summary>Decisions today, those where the learned term changed the winner, and those exploration alone won (LearnedShareDay, ExploreShareDay).</summary>
        public int DayDecisions, DayLearnedChanged, DayExploreWon;
        public long Decisions;
        public int DayStamp = -1;
        /// <summary>Yesterday's shares (permille), kept for the snapshot after the counters reset.</summary>
        public int LearnedSharePrev, ExploreSharePrev;
        /// <summary>Thinks counted (rotates the explore direction without a random draw).</summary>
        public int ThinkCount;

        public NpcMind() { }

        public NpcMind(LearningCaps caps, int beliefs, LearningMode mode)
        {
            Store = new AssociationStore(caps) { Mode = mode };
            Beliefs = new BeliefStore(beliefs);
        }

        public void AddTo(ref StateHasher h)
        {
            Beliefs.AddTo(ref h);
            var l = Last;
            h.Add(l.Tick); h.Add(l.Verb | ((int)l.TargetKind << 8) | (l.TargetClass << 16)); h.Add(l.TargetId); h.Add(l.TargetNode);
            h.Add(l.Instinct); h.Add(l.Learned); h.Add(l.Personality); h.Add(l.Cost); h.Add(l.Explore); h.Add(l.Total); h.Add(l.Candidates);
            h.Add((l.LearnedChangedWinner ? 1 : 0) | (l.ExploreWon ? 2 : 0)); h.Add(l.Key0); h.Add(l.Key1); h.Add(l.Key2); h.Add(l.SecondVerb); h.Add(l.SecondTotal);
            h.Add(AttendedCount);
            for (int i = 0; i < AttendedCount; i++) { h.Add(Attended[i].Class); h.Add(Attended[i].Node); h.Add(Attended[i].Salience); }
            h.Add(DayDecisions); h.Add(DayLearnedChanged); h.Add(DayExploreWon); h.Add(Decisions); h.Add(DayStamp); h.Add(LearnedSharePrev); h.Add(ExploreSharePrev); h.Add(ThinkCount);
        }
    }
}
