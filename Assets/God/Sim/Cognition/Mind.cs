using System;
using System.IO;
using System.Runtime.Serialization;
using God.Sim.Learning;
using God.Sim.Policy;
using Newtonsoft.Json;

namespace God.Sim.Cognition
{
    /// <summary>The kinds of fact an NPC believes (COMMUNICATION_AND_COGNITION 4.4; M3: places).</summary>
    public enum BeliefKind : byte { None = 0, Food = 1, Wood = 2, Pile = 3, Hut = 4, Tool = 5,
        /// <summary>I-BS amendment (SHARED_CONTRACTS 12.32): a bin it has seen (its household's store, estate goods); <see cref="Belief.Ref"/> = the bin id, Stock = what it may take there as last looked.</summary>
        Store = 6 }

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
                case BeliefKind.Store: return 4;
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
        /// <summary>The part of <see cref="Instinct"/> driven by a need (drive-gated instinct rules).</summary>
        public int Need;
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

    /// <summary>
    /// One option the NPC weighed at its last decision (its perceived opportunities, for the inspector and learn-dump): verb,
    /// target class and distance, and the score terms. Need is the drive-driven part of Instinct; Total = Instinct + Learned
    /// + Personality - Cost + Explore.
    /// </summary>
    public struct OpportunitySlot
    {
        public byte Verb;
        public byte Class;
        public int Dist;
        public int Instinct, Need, Learned, Personality, Cost, Explore, Total;
        /// <summary>I-BS amendment (SHARED_CONTRACTS 12.34): the Social term (M4's kin term + M5's social term), so the terms add up to the total.</summary>
        public int Social;
        public bool FromBelief, Chosen;
    }

    /// <summary>One experience the NPC learned from (the reward its last decision earned, or a closed episode): when, what, and the reward.</summary>
    public struct ExperienceSlot
    {
        public int Tick;
        public byte Verb;
        public byte Class;
        public int Reward;
        /// <summary>True for a deferred outcome (an episode closed, e.g. the hut it helped build was finished).</summary>
        public bool Episode;
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
        /// <summary>The options weighed at the last decision (at most <see cref="MaxOpportunities"/>, in candidate order).</summary>
        public OpportunitySlot[] Opportunities = new OpportunitySlot[MaxOpportunities];
        public int OpportunityCount;
        /// <summary>The last experiences with a reward (a ring of <see cref="MaxExperiences"/>; <see cref="ExperienceNext"/> is the next slot).</summary>
        public ExperienceSlot[] Experiences = new ExperienceSlot[MaxExperiences];
        public int ExperienceCount, ExperienceNext;

        public const int MaxOpportunities = 8, MaxExperiences = 8;

        /// <summary>F0.5: its believed kin (written only by M4; null without M4). Hashed only when present.</summary>
        public Population.KinMind Kin;
        /// <summary>F0.6: its social memory (written only by M5; null without M5). Hashed only when present.</summary>
        public God.Sim.Social.SocialMind Social;

        /// <summary>Remembers an experience (the ring overwrites the oldest).</summary>
        public void Remember(int tick, byte verb, byte cls, int reward, bool episode)
        {
            Experiences[ExperienceNext] = new ExperienceSlot { Tick = tick, Verb = verb, Class = cls, Reward = reward, Episode = episode };
            ExperienceNext = (ExperienceNext + 1) % MaxExperiences;
            if (ExperienceCount < MaxExperiences) ExperienceCount++;
        }

        /// <summary>The k-th most recent experience (0 = newest), k below <see cref="ExperienceCount"/>.</summary>
        public ExperienceSlot Recent(int k) => Experiences[((ExperienceNext - 1 - k) % MaxExperiences + MaxExperiences) % MaxExperiences];

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
            h.Add(l.Need);
            h.Add(OpportunityCount);
            for (int i = 0; i < OpportunityCount; i++)
            {
                var o = Opportunities[i];
                h.Add(o.Verb | (o.Class << 8) | (o.FromBelief ? 1 << 16 : 0) | (o.Chosen ? 1 << 17 : 0)); h.Add(o.Dist);
                h.Add(o.Instinct); h.Add(o.Need); h.Add(o.Learned); h.Add(o.Personality); h.Add(o.Cost); h.Add(o.Explore); h.Add(o.Total);
            }
            // 12.34: the Social column only for a mind with an M4 or M5 part (an M3 mind hashes exactly as before; its Social is 0)
            if (Kin != null || Social != null) for (int i = 0; i < OpportunityCount; i++) h.Add(Opportunities[i].Social);
            h.Add(ExperienceCount); h.Add(ExperienceNext);
            for (int i = 0; i < ExperienceCount; i++) { var e = Experiences[i]; h.Add(e.Tick); h.Add(e.Verb | (e.Class << 8) | (e.Episode ? 1 << 16 : 0)); h.Add(e.Reward); }
            // F0.12: the later modules' parts of a mind, only when present (M3 minds hash exactly as before)
            if (Kin != null) { h.Add(0x4B494E); Kin.AddTo(ref h); }
            if (Social != null) { h.Add(0x534F43); Social.AddTo(ref h); }
        }
    }
}
