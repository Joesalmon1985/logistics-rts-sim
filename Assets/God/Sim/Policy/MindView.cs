using System;
using God.Sim.Cognition;
using God.Sim.Learning;

namespace God.Sim.Policy
{
    /// <summary>The score terms of one candidate as integers (the first six of <c>CandidateTerms</c>, plus familiarity and the strongest entry's origin).</summary>
    public struct CandidateScore
    {
        public int Instinct, Learned, Social, Culture, Personality, Cost;
        /// <summary>The part of <see cref="Instinct"/> a need (a drive) drives; Instinct - Need is situational instinct. Not a separate score term.</summary>
        public int Need;
        /// <summary>How many of the candidate's keys have an entry for its verb (0: never tried here).</summary>
        public int Familiar;
        /// <summary>How many of its target features have an entry for its verb (0: this kind of target never tried with this verb: novel).</summary>
        public int FamiliarTarget;
        public Origin StrongestOrigin;
    }

    /// <summary>One top association as the view gives it (NPC_LEARNING 3.6): verb, feature kind, weights scaled by /32, origin and age.</summary>
    public struct AssocSlot
    {
        public byte Verb;
        public byte FeatureKind;
        public sbyte Stw;
        public sbyte Ltw;
        public byte OriginAge;
    }

    /// <summary>
    /// The read-only view of an NPC's own mind that every individual-level policy receives (NPC_LEARNING_DESIGN 3.6,
    /// layout version 1): the score terms of each candidate in the current context (instinct, learned, social, culture,
    /// personality, cost, familiarity), the exploration amplitude, its top associations, a per-verb-group summary, origin
    /// shares, drives and a belief summary. It stores nothing, allocates nothing, and holds no true state: only the NPC's
    /// own store, beliefs and the context Cognition built (reflection test).
    /// </summary>
    public readonly struct MindView
    {
        /// <summary>Goes into every model file header (AI_TRAINING_DESIGN 3.2). Bumped by any change of size or meaning.</summary>
        public const ushort Layout = 1;
        public const int CandTerms = 16, TopA = 8, LearnDim = 16, BeliefDim = 12, MemDim = 8;

        private readonly ThinkScratch _sc;

        internal MindView(ThinkScratch sc) { _sc = sc; }

        /// <summary>No mind (soldiers before M9).</summary>
        public static MindView Empty => default;

        public bool IsEmpty => _sc == null;
        public int LayoutVersion => _sc == null ? 0 : Layout;
        public LearningMode Mode => _sc?.Mind.Store.Mode ?? LearningMode.Off;
        public int CandidateCount => _sc?.CandCount ?? 0;

        // ---------------- scoring ----------------

        /// <summary>The score terms of candidate <paramref name="i"/>: instinct prior, learned value Q (0 when Off), social and culture (0 before M5 and M11), personality, perceived cost.</summary>
        public CandidateScore Terms(int i)
        {
            var r = new CandidateScore();
            if (_sc == null || i < 0 || i >= _sc.CandCount) return r;
            var c = _sc.Cand[i];
            var st = _sc.Mind.Store;
            r.Instinct = InstinctTable.Prior(_sc.Instincts, _sc.Keys, c.KeyOffset, c.KeyCount, c.Verb);
            r.Need = InstinctTable.NeedPrior(_sc.Instincts, _sc.Keys, c.KeyOffset, c.KeyCount, c.Verb);
            if (st.Mode != LearningMode.Off)
            {
                int q = st.SumStw(_sc.Keys, c.KeyOffset, c.KeyCount, c.Verb, out r.Familiar);
                st.SumStw(_sc.TargetKeys, c.TargetOffset, c.TargetCount, c.Verb, out r.FamiliarTarget);
                if (c.TargetCount == 0) r.FamiliarTarget = r.Familiar;
                r.Learned = (int)((long)q * _sc.LearningTuning.LearnGain >> 10);
                r.StrongestOrigin = StrongestOrigin(st, c);
            }
            r.Personality = Personality(c.Verb);
            r.Cost = c.Cost;
            return r;
        }

        /// <summary>
        /// <c>CandidateTerms</c> (16 values): Instinct, Learned, Social, Culture, Personality, Cost, familiarity, origin of the
        /// strongest entry, then the top 4 keys' Stw and Ltw. Values are clamped to the short range.
        /// </summary>
        public void CandidateTerms(int i, Span<short> t)
        {
            var r = Terms(i);
            t[0] = S(r.Instinct); t[1] = S(r.Learned); t[2] = S(r.Social); t[3] = S(r.Culture); t[4] = S(r.Personality); t[5] = S(r.Cost);
            t[6] = S(r.Familiar); t[7] = (short)r.StrongestOrigin;
            for (int k = 8; k < 16; k++) t[k] = 0;
            if (_sc == null || i < 0 || i >= _sc.CandCount || _sc.Mind.Store.Mode == LearningMode.Off) return;
            Span<uint> keys = stackalloc uint[4];
            Span<short> stw = stackalloc short[4];
            int n = TopKeys(i, keys, stw);
            var st = _sc.Mind.Store;
            for (int k = 0; k < n; k++)
            {
                int at = st.Find(keys[k], _sc.Cand[i].Verb);
                t[8 + 2 * k] = stw[k];
                t[9 + 2 * k] = at >= 0 ? st.Entries[at].Ltw : (short)0;
            }
        }

        private static short S(int v) => (short)(v > short.MaxValue ? short.MaxValue : v < short.MinValue ? short.MinValue : v);

        /// <summary>The keys of candidate <paramref name="i"/> with the largest fast weight for its verb (descending; ties by lower key). Returns how many.</summary>
        public int TopKeys(int i, Span<uint> keys, Span<short> stw)
        {
            if (_sc == null || i < 0 || i >= _sc.CandCount) return 0;
            var c = _sc.Cand[i];
            var st = _sc.Mind.Store;
            int n = 0, cap = Math.Min(keys.Length, stw.Length);
            for (int k = 0; k < c.KeyCount; k++)
            {
                uint key = _sc.Keys[c.KeyOffset + k];
                int at = st.Find(key, c.Verb);
                if (at < 0) continue;
                short w = st.Entries[at].Stw;
                int pos;
                if (n < cap) pos = n++;
                else if (Fx.Abs(w) > Fx.Abs(stw[cap - 1])) pos = cap - 1;
                else continue;
                while (pos > 0 && Fx.Abs(stw[pos - 1]) < Fx.Abs(w)) { keys[pos] = keys[pos - 1]; stw[pos] = stw[pos - 1]; pos--; }
                keys[pos] = key; stw[pos] = w;
            }
            return n;
        }

        private static Origin StrongestOrigin(AssociationStore st, in Candidate c) => Origin.Own; // only own experience before M7

        /// <summary>Trait biases per verb (POPULATION "Personality"): industry favours gathering and building, boldness exploring over fleeing.</summary>
        public int Personality(byte verb)
        {
            if (_sc == null) return 0;
            var tr = _sc.Traits;
            switch (verb)
            {
                case Verb.Gather: case Verb.Build: return tr[Population.Trait.Industry];
                case Verb.Rest: return -tr[Population.Trait.Industry] / 2;
                case Verb.Explore: return tr[Population.Trait.Boldness];
                case Verb.Flee: return -tr[Population.Trait.Boldness] / 2;
                default: return 0;
            }
        }

        /// <summary><c>Amp = clamp(ExploreBase x Curiosity / 1000 + Boredom / 4 - Fear / 4, 0, ExploreMax)</c> (no boredom drive before later milestones).</summary>
        public int ExploreAmplitude
        {
            get
            {
                if (_sc == null) return 0;
                var tu = _sc.LearningTuning;
                int amp = tu.ExploreBase * _sc.Learn.Curiosity / 1000 - _sc.Self.Fear / 4;
                return Fx.Clamp(amp, 0, tu.ExploreMax);
            }
        }

        /// <summary>The novelty bonus for a candidate none of whose keys has an entry for its verb.</summary>
        public int Novelty => _sc == null ? 0 : _sc.LearningTuning.NoveltyBonus * _sc.Learn.Curiosity / 1000;

        // ---------------- summaries (fixed sizes, for encoders and the inspector) ----------------

        /// <summary>Drives in permille: hunger, fatigue, cold, fear, pain.</summary>
        public void Drives(Span<short> d)
        {
            if (_sc == null) return;
            var s = _sc.Self;
            d[0] = (short)s.Hunger; d[1] = (short)s.Fatigue; d[2] = (short)s.Cold; d[3] = (short)s.Fear; d[4] = (short)s.Pain;
        }

        /// <summary>The 8 entries with the largest |Ltw| (ties by (Key, Verb) ascending), scaled by /32.</summary>
        public int TopAssociations(Span<AssocSlot> s)
        {
            if (_sc == null) return 0;
            var st = _sc.Mind.Store;
            int n = 0, max = Math.Min(s.Length, TopA);
            Span<int> idx = stackalloc int[TopA];
            for (int i = 0; i < st.Count; i++)
            {
                int a = Fx.Abs(st.Entries[i].Ltw);
                if (a == 0) continue;
                int pos;
                if (n < max) pos = n++;
                else if (a > Fx.Abs(st.Entries[idx[max - 1]].Ltw)) pos = max - 1;
                else continue;
                while (pos > 0 && Fx.Abs(st.Entries[idx[pos - 1]].Ltw) < a) { idx[pos] = idx[pos - 1]; pos--; }
                idx[pos] = i;
            }
            int today = _sc.Self.Day;
            for (int k = 0; k < n; k++)
            {
                var e = st.Entries[idx[k]];
                int age = today - e.LastDay; if (age < 0) age = 0; if (age > 63) age = 63;
                s[k] = new AssocSlot { Verb = e.Verb, FeatureKind = (byte)Feature.KindOf(e.Key), Stw = (sbyte)Fx.Clamp(e.Stw / 32, -125, 125), Ltw = (sbyte)Fx.Clamp(e.Ltw / 32, -125, 125), OriginAge = (byte)((int)e.Origin * 64 + age) };
            }
            return n;
        }

        /// <summary>Per verb group (16): the summed Stw of the entries matching the current base context, scaled to -127..127.</summary>
        public void AssocSummary(Span<sbyte> v)
        {
            for (int g = 0; g < LearnDim && g < v.Length; g++) v[g] = 0;
            if (_sc == null) return;
            Span<int> sum = stackalloc int[LearnDim];
            var st = _sc.Mind.Store;
            for (int b = 0; b < _sc.BaseCount; b++)
            {
                uint key = _sc.Base[b];
                int at = st.Find(key, 0);
                if (at < 0) at = ~at;
                for (int i = at; i < st.Count && st.Entries[i].Key == key; i++) sum[(int)VerbTable.GroupOf(st.Entries[i].Verb)] += st.Entries[i].Stw;
            }
            for (int g = 0; g < LearnDim && g < v.Length; g++) v[g] = (sbyte)Fx.Clamp(sum[g] / 32, -127, 127);
        }

        /// <summary>Own, vicarious, imitated (permille of entries). Only own experience exists before M7.</summary>
        public void OriginShares(Span<short> v)
        {
            v[0] = 0; v[1] = 0; v[2] = 0;
            if (_sc == null) return;
            var st = _sc.Mind.Store;
            if (st.Count == 0) return;
            Span<int> c = stackalloc int[3];
            for (int i = 0; i < st.Count; i++) c[(int)st.Entries[i].Origin]++;
            for (int k = 0; k < 3; k++) v[k] = (short)(c[k] * 1000 / st.Count);
        }

        /// <summary>
        /// BELIEF_DIM = 12: food places known, of them with stock, wood places, piles, own hut known, own site known, tools
        /// seen, the mean confidence, the nearest food place's distance (nodes, capped), and 3 reserved.
        /// </summary>
        public void BeliefSummary(Span<short> v)
        {
            for (int i = 0; i < BeliefDim && i < v.Length; i++) v[i] = 0;
            if (_sc == null) return;
            var b = _sc.Mind.Beliefs;
            int conf = 0, nearest = 999;
            for (int i = 0; i < b.Count; i++)
            {
                var x = b.Slots[i];
                conf += x.Confidence;
                switch (x.Kind)
                {
                    case BeliefKind.Food:
                        v[0]++; if (x.Stock != ObjState.StockEmpty) v[1]++;
                        int d = _sc.Grid.Distance(_sc.Self.Node, x.Subject); if (d < nearest) nearest = d;
                        break;
                    case BeliefKind.Wood: v[2]++; break;
                    case BeliefKind.Pile: v[3]++; break;
                    case BeliefKind.Hut: if (x.Ref == _sc.Self.HomeHut && x.Ref != 0) v[4] = 1; if (x.Ref == _sc.Self.SiteHut && x.Ref != 0) v[5] = 1; break;
                    case BeliefKind.Tool: v[6]++; break;
                }
            }
            v[7] = (short)(b.Count > 0 ? conf / b.Count : 0);
            v[8] = (short)nearest;
        }

        public int BeliefCount => _sc?.Mind.Beliefs.Count ?? 0;
    }

    /// <summary>The candidates an NPC has in front of it this think (read-only; a copy per index, no allocation).</summary>
    public readonly struct CandidateList
    {
        private readonly ThinkScratch _sc;
        internal CandidateList(ThinkScratch sc) { _sc = sc; }
        public int Count => _sc?.CandCount ?? 0;
        public Candidate this[int i] => _sc.Cand[i];
    }

    /// <summary>
    /// What one civilian NPC perceives now (COMMUNICATION_AND_COGNITION 3, layer 2), built by its senses and Cognition:
    /// its own body and carry (<see cref="Self"/>), the things in sight (classes and relative places, never true ids except
    /// what it can pick up or knows as its own), what it attends, and the candidates Cognition built from perception and
    /// memory. Value types only; no true state.
    /// </summary>
    public readonly struct NpcObservation
    {
        private readonly ThinkScratch _sc;
        public readonly int NpcId;
        public readonly long Tick;

        internal NpcObservation(ThinkScratch sc, int npcId, long tick) { _sc = sc; NpcId = npcId; Tick = tick; }

        public SelfView Self => _sc != null ? _sc.Self : default;
        public int SeenCount => _sc?.SeenCount ?? 0;
        public Sensed Seen(int i) => _sc.Seen[i];
        public int AttendedCount => _sc?.AttendedCount ?? 0;
        public Sensed Attended(int i) => _sc.Seen[_sc.Attended[i]];
        public CandidateList Candidates => new CandidateList(_sc);
    }

    /// <summary>Builds observation and mind views over a think's scratch (the population's thinking loop and controlled tests).</summary>
    public static class MindViews
    {
        public static NpcObservation Observe(ThinkScratch sc, int npcId, long tick) => new NpcObservation(sc, npcId, tick);
        public static MindView Of(ThinkScratch sc) => new MindView(sc);
    }
}
