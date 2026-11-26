using System;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace God.Sim.Learning
{
    /// <summary>How an NPC learns (NPC_LEARNING_DESIGN 4.16): <c>On</c> learns and uses what it learned; <c>Frozen</c> uses what it has but never updates; <c>Off</c> ignores the store (instinct-only baseline).</summary>
    public enum LearningMode : byte { On = 0, Frozen = 1, Off = 2 }

    /// <summary>Where an entry came from (its flags): own experience, watching others (M7), copying a model (M11).</summary>
    public enum Origin : byte { Own = 0, Vicarious = 1, Imitated = 2 }

    /// <summary>One association: a feature (or conjunction) and a verb, with a fast and a slow weight in value points.</summary>
    public struct AssocEntry
    {
        public uint Key;
        public byte Verb;
        /// <summary>Bits 0-1 origin, bit 2 locked (never evicted), bit 3 touched today.</summary>
        public byte Flags;
        public short Stw;
        public short Ltw;
        public ushort Use;
        public ushort LastDay;

        public const byte LockedBit = 4, TouchedBit = 8;
        public Origin Origin => (Origin)(Flags & 3);
        public bool Locked => (Flags & LockedBit) != 0;
        public bool Touched => (Flags & TouchedBit) != 0;
    }

    /// <summary>A learned AND-detector over a drive feature and one other feature (pairs only in M3; C reserved for triples, M5).</summary>
    public struct Conjunction
    {
        public uint A, B, C;
        /// <summary>0 = free slot.</summary>
        public ushort Strength;
        public ushort LastDay;
        /// <summary>Bumped when the slot is reused, so stale Conj keys never match.</summary>
        public byte Generation;
        /// <summary>Bit 0: took part in a closed trace today.</summary>
        public byte Flags;
        /// <summary>Sum of |delta| / 32 of today's closed traces it took part in (strength gain at sleep, 4.8 step 5).</summary>
        public ushort DeltaDay;
    }

    /// <summary>The record of one decision, kept while reward can still be credited to it (4.5). Its keys live in <see cref="AssociationStore.TraceKeys"/>.</summary>
    public struct Trace
    {
        /// <summary>The decision tick.</summary>
        public int Tick;
        /// <summary>Reward credited so far, weighted by E (vp).</summary>
        public int Return;
        /// <summary>The key of the long-delayed outcome it waits for (a hut id for Build), 0 for none.</summary>
        public int EpisodeKey;
        /// <summary>The learned value predicted at decision time (vp).</summary>
        public short QPred;
        /// <summary>Eligibility 0..1024.</summary>
        public ushort E;
        /// <summary><see cref="OngoingBit"/>, <see cref="EpisodeBit"/>.</summary>
        public ushort Flags;
        public byte Verb, KeyCount;

        public const ushort OngoingBit = 1, EpisodeBit = 2, VicariousBit = 4;
        public bool Ongoing => (Flags & OngoingBit) != 0;
    }

    /// <summary>A trace kept open for a long-delayed outcome (4.7). Its keys live in <see cref="AssociationStore.OpenKeys"/>.</summary>
    public struct Episode
    {
        /// <summary>The trace; its <see cref="Trace.EpisodeKey"/> names the outcome that closes it.</summary>
        public Trace T;
        public ushort OpenedDay, ExpiresDay;
    }

    /// <summary>A strongly rewarded or punished moment of the day, kept for conjunction recruitment at sleep (4.11).</summary>
    public struct DayMoment
    {
        public byte Verb, KeyCount;
        public short Delta;
    }

    /// <summary>
    /// One NPC's lifetime learning (NPC_LEARNING_DESIGN 3.3): a bounded, sorted table of (feature or conjunction, verb)
    /// entries with fast and slow weights, a pool of conjunctions, a ring of eligibility traces, open episodes and the
    /// day's notable moments. All sizes are fixed when the NPC is made (<see cref="LearningCaps"/>). Integers only. Saved as
    /// one compact binary blob (<see cref="Blob"/>) so a save of 500 NPCs stays small; hashed field by field.
    /// </summary>
    public sealed class AssociationStore
    {
        public const int MaxKeys = 24;
        public const int MomentKeys = 12;

        [JsonIgnore] public AssocEntry[] Entries;
        [JsonIgnore] public Conjunction[] Conj;
        [JsonIgnore] public Trace[] Ring;
        [JsonIgnore] public uint[] TraceKeys;
        [JsonIgnore] public Episode[] Open;
        [JsonIgnore] public uint[] OpenKeys;
        [JsonIgnore] public DayMoment[] Day;
        [JsonIgnore] public uint[] DayKeys;

        public int Count, RingCount, OpenCount, DayCount;
        /// <summary>Reward since the last think (vp).</summary>
        public int RewardAcc;
        public LearningMode Mode;
        /// <summary>Lifetime counters (telemetry).</summary>
        public int Updates, Evictions, Recruits, Frees, EpisodesClosed;
        /// <summary>Reward and punishment summed today (snapshot fields), reset at the first think of a new day.</summary>
        public int RewardSumDay, PunishSumDay, SumDay;
        /// <summary>The last day consolidation ran (-1: never).</summary>
        public int LastConsolidatedDay = -1;

        /// <summary>The packed store as the save holds it (base64 of a fixed binary layout). Filled while saving; read on load.</summary>
        public string Blob;

        public AssociationStore() { }

        public AssociationStore(LearningCaps caps)
        {
            Allocate(caps.Entries, caps.Conjunctions, caps.Traces, caps.Episodes, caps.Moments);
        }

        private void Allocate(int entries, int conj, int traces, int episodes, int moments)
        {
            Entries = new AssocEntry[entries];
            Conj = new Conjunction[conj];
            Ring = new Trace[traces];
            TraceKeys = new uint[traces * MaxKeys];
            Open = new Episode[episodes];
            OpenKeys = new uint[episodes * MaxKeys];
            Day = new DayMoment[moments];
            DayKeys = new uint[moments * MomentKeys];
        }

        public int EntryCap => Entries.Length;
        public int ConjCap => Conj.Length;

        /// <summary>Slots of the conjunction pool in use.</summary>
        public int ConjUsed
        {
            get { int n = 0; for (int i = 0; i < Conj.Length; i++) if (Conj[i].Strength > 0) n++; return n; }
        }

        /// <summary>Bytes of the fixed arrays (the per-NPC learning budget, L11: at most 10,240 at default caps).</summary>
        public int Bytes => Entries.Length * 16 + Conj.Length * 20 + Ring.Length * (20 + MaxKeys * 4) + Open.Length * (24 + MaxKeys * 4)
            + Day.Length * (4 + MomentKeys * 4) + 64;

        // ---------------- lookup ----------------

        private static long Ord(uint key, byte verb) => ((long)key << 8) | verb;

        /// <summary>Index of the entry for (key, verb), or <c>~insertionPoint</c> when absent (binary search over the sorted table).</summary>
        public int Find(uint key, byte verb)
        {
            long want = Ord(key, verb);
            int lo = 0, hi = Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                long o = Ord(Entries[mid].Key, Entries[mid].Verb);
                if (o == want) return mid;
                if (o < want) lo = mid + 1; else hi = mid - 1;
            }
            return ~lo;
        }

        /// <summary>Inserts a zero entry at the insertion point (the caller evicted first if full). Returns its index.</summary>
        public int InsertAt(int at, uint key, byte verb, ushort day, Origin origin)
        {
            if (Count >= Entries.Length) throw new InvalidOperationException("association store full");
            for (int i = Count; i > at; i--) Entries[i] = Entries[i - 1];
            Entries[at] = new AssocEntry { Key = key, Verb = verb, Flags = (byte)origin, LastDay = day };
            Count++;
            return at;
        }

        internal void RemoveAt(int at)
        {
            for (int i = at; i < Count - 1; i++) Entries[i] = Entries[i + 1];
            Count--;
            Entries[Count] = default;
        }

        /// <summary>The sum of the fast weights of a context's keys for a verb, and how many of them have an entry.</summary>
        public int SumStw(uint[] keys, int offset, int n, byte verb, out int familiar)
        {
            int q = 0; familiar = 0;
            for (int i = 0; i < n; i++)
            {
                int at = Find(keys[offset + i], verb);
                if (at >= 0) { q += Entries[at].Stw; familiar++; }
            }
            return q;
        }

        // ---------------- trace ring (oldest first) ----------------

        internal void RemoveTraceAt(int i)
        {
            for (int j = i; j < RingCount - 1; j++)
            {
                Ring[j] = Ring[j + 1];
                Array.Copy(TraceKeys, (j + 1) * MaxKeys, TraceKeys, j * MaxKeys, MaxKeys);
            }
            RingCount--;
            Ring[RingCount] = default;
            Array.Clear(TraceKeys, RingCount * MaxKeys, MaxKeys);
        }

        internal void RemoveEpisodeAt(int i)
        {
            for (int j = i; j < OpenCount - 1; j++)
            {
                Open[j] = Open[j + 1];
                Array.Copy(OpenKeys, (j + 1) * MaxKeys, OpenKeys, j * MaxKeys, MaxKeys);
            }
            OpenCount--;
            Open[OpenCount] = default;
            Array.Clear(OpenKeys, OpenCount * MaxKeys, MaxKeys);
        }

        // ---------------- hash ----------------

        /// <summary>Folds the whole store into a hash (every field that drives behaviour).</summary>
        public void AddTo(ref StateHasher h)
        {
            h.Add(Count); h.Add(RingCount); h.Add(OpenCount); h.Add(DayCount); h.Add(RewardAcc); h.Add((int)Mode);
            h.Add(Updates); h.Add(Evictions); h.Add(Recruits); h.Add(Frees); h.Add(EpisodesClosed);
            h.Add(RewardSumDay); h.Add(PunishSumDay); h.Add(SumDay); h.Add(LastConsolidatedDay);
            for (int i = 0; i < Count; i++)
            {
                var e = Entries[i];
                h.Add(e.Key); h.Add(e.Verb | (e.Flags << 8)); h.Add((e.Stw & 0xFFFF) | (e.Ltw << 16)); h.Add(e.Use | (e.LastDay << 16));
            }
            for (int i = 0; i < Conj.Length; i++)
            {
                var c = Conj[i];
                if (c.Strength == 0 && c.Generation == 0) continue;
                h.Add(i); h.Add(c.A); h.Add(c.B); h.Add(c.C); h.Add(c.Strength | (c.LastDay << 16)); h.Add(c.Generation | (c.Flags << 8) | (c.DeltaDay << 16));
            }
            for (int i = 0; i < RingCount; i++) AddTrace(ref h, Ring[i], TraceKeys, i * MaxKeys);
            for (int i = 0; i < OpenCount; i++) { AddTrace(ref h, Open[i].T, OpenKeys, i * MaxKeys); h.Add(Open[i].OpenedDay | (Open[i].ExpiresDay << 16)); }
            for (int i = 0; i < DayCount; i++)
            {
                h.Add(Day[i].Verb | (Day[i].KeyCount << 8) | (Day[i].Delta << 16));
                for (int k = 0; k < Day[i].KeyCount; k++) h.Add(DayKeys[i * MomentKeys + k]);
            }
        }

        private static void AddTrace(ref StateHasher h, in Trace t, uint[] keys, int off)
        {
            h.Add(t.Tick); h.Add(t.Verb | (t.KeyCount << 8) | (t.E << 16)); h.Add(t.QPred); h.Add(t.Return); h.Add(t.Flags | ((long)t.EpisodeKey << 16));
            for (int k = 0; k < t.KeyCount; k++) h.Add(keys[off + k]);
        }

        /// <summary>A hash of this store alone (tests: <c>Frozen</c> leaves it unchanged; same nature, different nurture).</summary>
        public ulong Hash()
        {
            var h = new StateHasher();
            AddTo(ref h);
            return h.Value;
        }

        /// <summary>A hash of what was learned only (entries and conjunctions, not the running traces): <c>Frozen</c> leaves it unchanged (L7).</summary>
        public ulong HashWeights()
        {
            var h = new StateHasher();
            h.Add(Count);
            for (int i = 0; i < Count; i++) { var e = Entries[i]; h.Add(e.Key); h.Add(e.Verb); h.Add(e.Stw); h.Add(e.Ltw); }
            for (int i = 0; i < Conj.Length; i++) { var c = Conj[i]; h.Add(c.A); h.Add(c.B); h.Add(c.Strength); h.Add(c.Generation); }
            return h.Value;
        }

        // ---------------- save: one binary blob ----------------

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context) { Blob = Pack(); }

        [OnSerialized]
        internal void OnSerializedMethod(StreamingContext context) { Blob = null; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (Blob != null) { Unpack(Blob); Blob = null; }
        }

        public string Pack()
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(1); // layout version
                w.Write(Entries.Length); w.Write(Conj.Length); w.Write(Ring.Length); w.Write(Open.Length); w.Write(Day.Length);
                for (int i = 0; i < Count; i++)
                {
                    var e = Entries[i];
                    w.Write(e.Key); w.Write(e.Verb); w.Write(e.Flags); w.Write(e.Stw); w.Write(e.Ltw); w.Write(e.Use); w.Write(e.LastDay);
                }
                for (int i = 0; i < Conj.Length; i++)
                {
                    var c = Conj[i];
                    w.Write(c.A); w.Write(c.B); w.Write(c.C); w.Write(c.Strength); w.Write(c.LastDay); w.Write(c.Generation); w.Write(c.Flags); w.Write(c.DeltaDay);
                }
                for (int i = 0; i < RingCount; i++) WriteTrace(w, Ring[i], TraceKeys, i * MaxKeys);
                for (int i = 0; i < OpenCount; i++) { WriteTrace(w, Open[i].T, OpenKeys, i * MaxKeys); w.Write(Open[i].OpenedDay); w.Write(Open[i].ExpiresDay); }
                for (int i = 0; i < DayCount; i++)
                {
                    w.Write(Day[i].Verb); w.Write(Day[i].KeyCount); w.Write(Day[i].Delta);
                    for (int k = 0; k < Day[i].KeyCount; k++) w.Write(DayKeys[i * MomentKeys + k]);
                }
                w.Flush();
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private static void WriteTrace(BinaryWriter w, in Trace t, uint[] keys, int off)
        {
            w.Write(t.Tick); w.Write(t.Verb); w.Write(t.KeyCount); w.Write(t.E); w.Write(t.QPred); w.Write(t.Return); w.Write(t.Flags); w.Write(t.EpisodeKey);
            for (int k = 0; k < t.KeyCount; k++) w.Write(keys[off + k]);
        }

        public void Unpack(string blob)
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(blob)))
            using (var r = new BinaryReader(ms))
            {
                int version = r.ReadInt32();
                if (version != 1) throw new InvalidDataException("unknown association store layout " + version);
                Allocate(r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
                for (int i = 0; i < Count; i++)
                    Entries[i] = new AssocEntry { Key = r.ReadUInt32(), Verb = r.ReadByte(), Flags = r.ReadByte(), Stw = r.ReadInt16(), Ltw = r.ReadInt16(), Use = r.ReadUInt16(), LastDay = r.ReadUInt16() };
                for (int i = 0; i < Conj.Length; i++)
                    Conj[i] = new Conjunction { A = r.ReadUInt32(), B = r.ReadUInt32(), C = r.ReadUInt32(), Strength = r.ReadUInt16(), LastDay = r.ReadUInt16(), Generation = r.ReadByte(), Flags = r.ReadByte(), DeltaDay = r.ReadUInt16() };
                for (int i = 0; i < RingCount; i++) Ring[i] = ReadTrace(r, TraceKeys, i * MaxKeys);
                for (int i = 0; i < OpenCount; i++) { Open[i] = new Episode { T = ReadTrace(r, OpenKeys, i * MaxKeys), OpenedDay = r.ReadUInt16(), ExpiresDay = r.ReadUInt16() }; }
                for (int i = 0; i < DayCount; i++)
                {
                    Day[i] = new DayMoment { Verb = r.ReadByte(), KeyCount = r.ReadByte(), Delta = r.ReadInt16() };
                    for (int k = 0; k < Day[i].KeyCount; k++) DayKeys[i * MomentKeys + k] = r.ReadUInt32();
                }
            }
        }

        private static Trace ReadTrace(BinaryReader r, uint[] keys, int off)
        {
            var t = new Trace { Tick = r.ReadInt32(), Verb = r.ReadByte(), KeyCount = r.ReadByte(), E = r.ReadUInt16(), QPred = r.ReadInt16(), Return = r.ReadInt32(), Flags = r.ReadUInt16(), EpisodeKey = r.ReadInt32() };
            for (int k = 0; k < t.KeyCount; k++) keys[off + k] = r.ReadUInt32();
            return t;
        }
    }

    /// <summary>The fixed sizes of a store (SimTuning.Learning; the defaults give 9,856 bytes per NPC).</summary>
    public readonly struct LearningCaps
    {
        public readonly int Entries, Conjunctions, Traces, Episodes, Moments;
        public LearningCaps(int entries, int conj, int traces, int episodes, int moments)
        {
            Entries = entries; Conjunctions = conj; Traces = traces; Episodes = episodes; Moments = moments;
        }
    }
}
