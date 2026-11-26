using System;

namespace God.Sim.Learning
{
    /// <summary>The per-NPC learning parameters (permille, 500-1500; genetic from M4, per founder before) and the patience trait.</summary>
    public struct LearnTraits
    {
        public int LearnRate, ForgetRate, Curiosity;
        /// <summary>The personality trait Patience (-100..100): patient NPCs keep traces longer (4.5).</summary>
        public int Patience;
        /// <summary>Adults 2, children 1, elders 3 (life stages arrive at M4; adults only in M3).</summary>
        public int AlphaShift;

        public static LearnTraits Neutral => new LearnTraits { LearnRate = 1000, ForgetRate = 1000, Curiosity = 1000, Patience = 0, AlphaShift = 2 };
    }

    /// <summary>
    /// Where the learning core reports (sampled <c>AssociationUpdated</c>, conjunction and episode events). Null: nothing is
    /// emitted. Emitting never changes learning.
    /// </summary>
    public interface ILearningSink
    {
        bool Sampled { get; }
        void Updated(uint key, byte verb, int delta, short stw, short ltw, Origin origin);
        void Recruited(int slot, uint a, uint b, uint c, int strength);
        void Freed(int slot, uint a, uint b, uint c);
        void EpisodeClosed(int episodeKey, byte verb, int ret, int daysElapsed);
    }

    /// <summary>
    /// The associative learning core (NPC_LEARNING_DESIGN 4.5-4.12) over one <see cref="AssociationStore"/>: crediting
    /// reward to eligibility traces, closing traces with the prediction-error update, long-delayed episodes, sleep-time
    /// consolidation with forgetting, conjunction recruitment and capacity eviction. Integers only, fixed order, no
    /// allocation in the update path. <c>Frozen</c> and <c>Off</c> skip every write to the weights; traces still run.
    /// </summary>
    public static class Learner
    {
        // ---------------- reward ----------------

        /// <summary>Adds signed value points to the reward collected since the last think (4.4).</summary>
        public static void AddReward(AssociationStore st, int vp)
        {
            if (vp == 0) return;
            st.RewardAcc += vp;
            if (vp > 0) st.RewardSumDay += vp; else st.PunishSumDay -= vp;
        }

        /// <summary>
        /// The Creatures drive rule (4.4): a drive falling from above its comfort level is reward, a drive rising above
        /// comfort is punishment, both in proportion to the part above comfort. Values are permille.
        /// </summary>
        public static int DriveReward(int w, int comfort, int prev, int now)
        {
            if (now < prev)
            {
                if (prev <= comfort) return 0;
                int low = now > comfort ? now : comfort;
                return Fx.MulDiv(w, prev - low, 250);
            }
            if (now > prev)
            {
                if (now <= comfort) return 0;
                int low = prev > comfort ? prev : comfort;
                return -Fx.MulDiv(w, now - low, 250);
            }
            return 0;
        }

        // ---------------- the think tick (4.1 steps 2.1, 2.2, 2.6, 2.7) ----------------

        /// <summary>
        /// Steps 2.1-2.2: credit, then close the traces whose eligibility ran out. Under <see cref="CreditRule.MonteCarlo"/> the
        /// raw reward is spread over the live traces here; under <see cref="CreditRule.TdLambda"/> (the default) the credit is
        /// the TD error of <see cref="Step"/>, once the new decision's value is known.
        /// </summary>
        public static void BeginThink(AssociationStore st, LearningTuning tu, in LearnTraits tr, int today, ILearningSink sink)
        {
            if (tu.Credit == CreditRule.MonteCarlo) CreditRaw(st);
            for (int i = 0; i < st.RingCount;)
            {
                var t = st.Ring[i];
                if (!t.Ongoing && t.E < tu.CloseBelowE) CloseTrace(st, tu, tr, i, today, sink); // removes index i
                else i++;
            }
            ExpireEpisodes(st, tu, tr, today, sink);
        }

        /// <summary>
        /// The TD(lambda) credit of one think (NPC_LEARNING 4.6 "a Rescorla-Wagner or TD(lambda) update"; SARSA(lambda) with
        /// linear features): <c>delta = r + gamma Q(new decision) - Q(previous decision)</c>, with both values from the store
        /// now, added to every live trace weighted by its eligibility. A reward that the value of the action taking it already
        /// predicted gives the bystanders before it nothing, so an action learns its own consequence, not the average reward
        /// that happens to follow it (M3 finding, DEV_LOG). <paramref name="qNew"/> is the raw learned value (sum of fast
        /// weights) of the decision just made, 0 when there is none. No-op under Monte Carlo credit.
        /// </summary>
        public static void Step(AssociationStore st, LearningTuning tu, in LearnTraits tr, int qNew, int today, ILearningSink sink)
        {
            if (tu.Credit != CreditRule.TdLambda) return;
            int r = st.RewardAcc;
            st.RewardAcc = 0;
            if (st.RingCount == 0) return;
            int last = st.RingCount - 1;
            int qPrev = st.SumStw(st.TraceKeys, last * AssociationStore.MaxKeys, st.Ring[last].KeyCount, st.Ring[last].Verb, out _);
            int delta = Fx.Clamp(r + Fx.MulDiv(qNew, tu.GammaPermille, 1000) - qPrev, -4000, 4000);
            // online (backward-view) TD: every live trace learns now, in proportion to its eligibility, so repeated
            // decisions never stack stale corrections
            for (int i = 0; i < st.RingCount; i++)
            {
                int d = Fx.MulDiv(delta, st.Ring[i].E, 1024);
                st.Ring[i].Return += d;
                bool newest = i == last;
                if (d != 0 || newest)
                    Update(st, tu, tr, st.TraceKeys, i * AssociationStore.MaxKeys, st.Ring[i].KeyCount, st.Ring[i].Verb, d, today, newest ? sink : null, newest);
            }
        }

        /// <summary>Spreads the pending reward over the live traces as it is (Monte Carlo credit; before consolidation under either rule).</summary>
        public static void CreditRaw(AssociationStore st)
        {
            if (st.RewardAcc == 0) return;
            int r = st.RewardAcc;
            for (int i = 0; i < st.RingCount; i++) st.Ring[i].Return += Fx.MulDiv(r, st.Ring[i].E, 1024);
            st.RewardAcc = 0;
        }

        /// <summary>
        /// Step 2.6: a new trace for the decision just made (E = 1024, ongoing). Every other trace stops being ongoing (the
        /// previous intent ended or was replaced). A full ring closes its oldest trace first.
        /// </summary>
        public static void OpenTrace(AssociationStore st, LearningTuning tu, in LearnTraits tr, int tick, byte verb, uint[] keys, int offset, int n,
            int qPred, int episodeKey, int today, ILearningSink sink)
        {
            for (int i = 0; i < st.RingCount; i++) st.Ring[i].Flags &= unchecked((ushort)~Trace.OngoingBit);
            if (st.RingCount >= st.Ring.Length) CloseTrace(st, tu, tr, 0, today, sink);
            if (n > AssociationStore.MaxKeys) n = AssociationStore.MaxKeys;
            int at = st.RingCount++;
            st.Ring[at] = new Trace
            {
                Tick = tick, Verb = verb, KeyCount = (byte)n, E = 1024, QPred = Fx.Clamp16(qPred), Return = 0,
                Flags = Trace.OngoingBit, EpisodeKey = episodeKey,
            };
            Array.Copy(keys, offset, st.TraceKeys, at * AssociationStore.MaxKeys, n);
        }

        /// <summary>The running intent ended (completed, refused or interrupted): its trace starts to decay.</summary>
        public static void EndOngoing(AssociationStore st)
        {
            for (int i = 0; i < st.RingCount; i++) st.Ring[i].Flags &= unchecked((ushort)~Trace.OngoingBit);
        }

        /// <summary>Step 2.7: every non-ongoing trace decays by <c>E &gt;&gt; shift</c>.</summary>
        public static void DecayTraces(AssociationStore st, int shift)
        {
            for (int i = 0; i < st.RingCount; i++)
            {
                if (st.Ring[i].Ongoing) continue;
                int e = st.Ring[i].E;
                e -= shift >= 16 ? e : e >> shift;
                st.Ring[i].E = (ushort)(e < 0 ? 0 : e);
            }
        }

        public static int TraceShiftOf(LearningTuning tu, in LearnTraits tr) => tr.Patience >= tu.PatientFrom && tu.TraceShift < 16 ? tu.PatientTraceShift : tu.TraceShift;

        // ---------------- closing and the update rule (4.6) ----------------

        private static void CloseTrace(AssociationStore st, LearningTuning tu, in LearnTraits tr, int i, int today, ILearningSink sink)
        {
            var t = st.Ring[i];
            int keyOff = i * AssociationStore.MaxKeys;
            if (tu.EpisodesOn && t.EpisodeKey != 0 && VerbTable.IsDeferred(t.Verb) && st.Open.Length > 0)
            {
                // A long-delayed outcome may still come: keep the trace open as an episode (4.7).
                if (st.OpenCount >= st.Open.Length) CloseEpisode(st, tu, tr, 0, 0, today, sink);
                int at = st.OpenCount++;
                t.Flags |= Trace.EpisodeBit;
                t.Flags &= unchecked((ushort)~Trace.OngoingBit);
                st.Open[at] = new Episode { T = t, OpenedDay = (ushort)today, ExpiresDay = (ushort)(today + tu.EpisodeExpireDays) };
                Array.Copy(st.TraceKeys, keyOff, st.OpenKeys, at * AssociationStore.MaxKeys, AssociationStore.MaxKeys);
                st.RemoveTraceAt(i);
                return;
            }
            if (tu.Credit == CreditRule.MonteCarlo)
                Update(st, tu, tr, st.TraceKeys, keyOff, t.KeyCount, t.Verb, t.Return - QNow(st, tu, st.TraceKeys, keyOff, t.KeyCount, t.Verb), today, sink, true);
            st.RemoveTraceAt(i);
        }

        /// <summary>
        /// A long-delayed outcome arrived (a hut finished, M3; wages, repayments and promises later): every open episode and
        /// every live trace with this key receives the reward, an episode discounted by half per two days elapsed; the
        /// episodes then close through the normal update.
        /// </summary>
        public static void Outcome(AssociationStore st, LearningTuning tu, in LearnTraits tr, int episodeKey, int reward, int today, ILearningSink sink)
        {
            if (episodeKey == 0) return;
            for (int i = 0; i < st.RingCount; i++)
                if (st.Ring[i].EpisodeKey == episodeKey) { st.Ring[i].Return += reward; st.Ring[i].EpisodeKey = 0; }
            for (int i = 0; i < st.OpenCount;)
            {
                if (st.Open[i].T.EpisodeKey == episodeKey)
                {
                    int days = today - st.Open[i].OpenedDay;
                    if (days < 0) days = 0;
                    CloseEpisode(st, tu, tr, i, Fx.Shr(reward, days / 2), today, sink);
                }
                else i++;
            }
        }

        private static void ExpireEpisodes(AssociationStore st, LearningTuning tu, in LearnTraits tr, int today, ILearningSink sink)
        {
            for (int i = 0; i < st.OpenCount;)
            {
                if (st.Open[i].ExpiresDay <= today) CloseEpisode(st, tu, tr, i, 0, today, sink);
                else i++;
            }
        }

        private static void CloseEpisode(AssociationStore st, LearningTuning tu, in LearnTraits tr, int i, int extra, int today, ILearningSink sink)
        {
            var ep = st.Open[i];
            int ret = ep.T.Return + extra;
            int off = i * AssociationStore.MaxKeys;
            // TD: the trace already learned from the stream; the late outcome is news. Monte Carlo: the whole return against the prediction.
            int delta = tu.Credit == CreditRule.TdLambda ? extra : ret - QNow(st, tu, st.OpenKeys, off, ep.T.KeyCount, ep.T.Verb);
            Update(st, tu, tr, st.OpenKeys, off, ep.T.KeyCount, ep.T.Verb, delta, today, sink, true);
            st.EpisodesClosed++;
            sink?.EpisodeClosed(ep.T.EpisodeKey, ep.T.Verb, ret, today - ep.OpenedDay);
            st.RemoveEpisodeAt(i);
        }

        /// <summary>A key's salience in the update: drive features and conjunctions, target features, then the situation.</summary>
        public static int Salience(LearningTuning tu, uint key)
        {
            var k = Feature.KindOf(key);
            if (k == FeatureKind.DriveBand || k == FeatureKind.DriveActive || k == FeatureKind.Conj) return tu.SalienceDrive;
            if (Feature.IsTargetKind(k)) return tu.SalienceTarget;
            return tu.SalienceBase;
        }

        private static int QNow(AssociationStore st, LearningTuning tu, uint[] keys, int off, int n, byte verb) =>
            (int)((long)st.SumStw(keys, off, n, verb, out _) * tu.LearnGain >> 10);

        /// <summary>
        /// The prediction-error update (4.6) for one decision's keys: the error <paramref name="error"/> is spread over the
        /// keys, the fast weight by <c>error &gt;&gt; AlphaShift * LearnRate / (1000 n)</c> and the slow weight by an eighth of
        /// that while awake. A notable error is kept as a moment for recruitment. Skipped entirely unless the mode is <c>On</c>.
        /// </summary>
        public static void Update(AssociationStore st, LearningTuning tu, in LearnTraits tr, uint[] keys, int off, int n, byte verb, int error,
            int today, ILearningSink sink, bool moment)
        {
            if (st.Mode != LearningMode.On || n <= 0) return;
            int delta = Fx.Clamp(error, -4000, 4000);
            // Rescorla-Wagner cue salience: the error is shared out in proportion to each key's salience (drives and
            // conjunctions, then what was acted on, then the situation), not equally (M3 finding; DEV_LOG).
            int sumSal = 0;
            for (int k = 0; k < n; k++) sumSal += Salience(tu, keys[off + k]);
            int share = Fx.MulDiv(Fx.Shr(delta, tr.AlphaShift), tr.LearnRate, 1000);
            ushort day = (ushort)today;
            for (int k = 0; k < n; k++)
            {
                uint key = keys[off + k];
                int dFast = Fx.MulDiv(share, Salience(tu, key), sumSal);
                int absD = Fx.Abs(dFast);
                int at = st.Find(key, verb);
                if (at < 0)
                {
                    if (absD < tu.MinInsertDelta) continue;
                    if (st.Count >= st.Entries.Length)
                    {
                        EvictOne(st, today);
                        at = st.Find(key, verb);
                        if (at >= 0) goto found;
                    }
                    at = st.InsertAt(~at, key, verb, day, Origin.Own);
                }
            found:
                ref var e = ref st.Entries[at];
                e.Stw = Fx.Clamp16(e.Stw + dFast);
                e.Ltw = Fx.Clamp16(e.Ltw + Fx.Shr(dFast, 3));
                int use = e.Use + 1 + absD / 16;
                e.Use = (ushort)(use > 65535 ? 65535 : use);
                e.LastDay = day;
                e.Flags |= AssocEntry.TouchedBit;
                if (Feature.KindOf(key) == FeatureKind.Conj)
                {
                    int slot = Feature.ValueOf(key) >> 8;
                    if (slot < st.Conj.Length && st.Conj[slot].Strength > 0)
                    {
                        st.Conj[slot].Flags |= 1;
                        int dd = st.Conj[slot].DeltaDay + Fx.Abs(delta) / 32;
                        st.Conj[slot].DeltaDay = (ushort)(dd > 60000 ? 60000 : dd);
                        st.Conj[slot].LastDay = day;
                    }
                }
                if (sink != null && (sink.Sampled || Fx.Abs(delta) >= 1000)) sink.Updated(key, verb, delta, e.Stw, e.Ltw, e.Origin);
            }
            int absAll = Fx.Abs(share);
            // A verb tried here with no notable outcome is still no longer untried: if no key holds an entry for it, the first
            // target key (else the first key) gets a zero entry, so the novelty bonus (4.2) stops (M3 finding; DEV_LOG).
            bool familiar = false;
            for (int k = 0; k < n && !familiar; k++) if (st.Find(keys[off + k], verb) >= 0) familiar = true;
            if (!familiar)
            {
                uint mark = keys[off];
                for (int k = 0; k < n; k++) if (Feature.IsTargetKind(Feature.KindOf(keys[off + k]))) { mark = keys[off + k]; break; }
                if (st.Count >= st.Entries.Length) EvictOne(st, today);
                int at = st.Find(mark, verb);
                if (at < 0) { at = st.InsertAt(~at, mark, verb, day, Origin.Own); st.Entries[at].Flags |= AssocEntry.TouchedBit; }
            }
            st.Updates++;
            if (moment && Fx.Abs(delta) >= tu.MomentThreshold) RecordMoment(st, keys, off, n, verb, delta);
        }

        /// <summary>Keeps a strongly rewarded or punished moment (its non-conjunction keys, targets first), replacing the smallest |delta| when full.</summary>
        private static void RecordMoment(AssociationStore st, uint[] keys, int off, int n, byte verb, int delta)
        {
            if (st.Day.Length == 0) return;
            int at;
            if (st.DayCount < st.Day.Length) at = st.DayCount++;
            else
            {
                at = 0;
                for (int i = 1; i < st.DayCount; i++) if (Fx.Abs(st.Day[i].Delta) < Fx.Abs(st.Day[at].Delta)) at = i;
                if (Fx.Abs(st.Day[at].Delta) >= Fx.Abs(delta)) return;
            }
            int baseOff = at * AssociationStore.MomentKeys, m = 0;
            // target keys first (what was acted on), then the situation, at most 12; kept sorted
            for (int pass = 0; pass < 2 && m < AssociationStore.MomentKeys; pass++)
                for (int k = 0; k < n && m < AssociationStore.MomentKeys; k++)
                {
                    uint key = keys[off + k];
                    var kind = Feature.KindOf(key);
                    if (kind == FeatureKind.Conj) continue;
                    bool target = Feature.IsTargetKind(kind);
                    if ((pass == 0) != target) continue;
                    st.DayKeys[baseOff + m++] = key;
                }
            Array.Sort(st.DayKeys, baseOff, m);
            st.Day[at] = new DayMoment { Verb = verb, KeyCount = (byte)m, Delta = (short)Fx.Clamp(delta, -32000, 32000) };
        }

        // ---------------- eviction (4.10) ----------------

        public static int EvictScore(in AssocEntry e, int today, int recencyDays = 30)
        {
            int since = today - e.LastDay;
            if (since < 0) since = 0;
            int recent = since >= recencyDays ? 0 : 256 * (recencyDays - since) / recencyDays;
            return e.Use + Fx.Abs(e.Ltw) / 4 + recent;
        }

        /// <summary>Removes the entry with the lowest eviction score (ties: lowest (Key, Verb)); locked entries are kept.</summary>
        public static bool EvictOne(AssociationStore st, int today)
        {
            int best = -1, bestScore = int.MaxValue;
            for (int i = 0; i < st.Count; i++)
            {
                if (st.Entries[i].Locked) continue;
                int sc = EvictScore(st.Entries[i], today);
                if (sc < bestScore) { bestScore = sc; best = i; } // ascending order: the first minimum is the lowest (Key, Verb)
            }
            if (best < 0) return false;
            st.RemoveAt(best);
            st.Evictions++;
            return true;
        }

        // ---------------- sleep consolidation (4.8) ----------------

        /// <summary>
        /// Once per NPC per day, at sleep start: close every non-ongoing trace, move the slow weights towards the day's
        /// lesson and relax the fast ones, decay untouched entries (per sim day since the last consolidation, scaled by
        /// <paramref name="decayScalePermille"/>, never by LifeScale), decay usefulness and conjunction strength, free empty
        /// slots, recruit new conjunctions from the day's moments, and evict down to the headroom.
        /// </summary>
        public static void Consolidate(AssociationStore st, LearningTuning tu, in LearnTraits tr, int today, int decayScalePermille,
            RecruitScratch scratch, ILearningSink sink)
        {
            if (st.LastConsolidatedDay >= today) return;
            int days = st.LastConsolidatedDay < 0 ? 1 : today - st.LastConsolidatedDay;
            if (days > 30) days = 30;
            st.LastConsolidatedDay = today;

            // 1. close everything that is not ongoing (credit what is owed first: the day ends here)
            if (tu.Credit == CreditRule.TdLambda) Step(st, tu, tr, 0, today, sink); else CreditRaw(st);
            for (int i = 0; i < st.RingCount;)
            {
                if (!st.Ring[i].Ongoing) CloseTrace(st, tu, tr, i, today, sink);
                else i++;
            }
            if (st.Mode != LearningMode.On)
            {
                st.DayCount = 0;
                for (int i = 0; i < st.Conj.Length; i++) { st.Conj[i].Flags = 0; st.Conj[i].DeltaDay = 0; }
                return;
            }

            // 2-4. consolidate, decay untouched entries, usefulness
            for (int i = 0; i < st.Count; i++)
            {
                ref var e = ref st.Entries[i];
                int ltw = e.Ltw + Fx.Shr(e.Stw - e.Ltw, 2);
                int stw = e.Stw + Fx.Shr(ltw - e.Stw, 1);
                if (!e.Touched)
                    for (int d = 0; d < days; d++)
                    {
                        ltw -= Fx.MulDiv(Fx.MulDiv(Fx.Shr(ltw, 5), tr.ForgetRate, 1000), decayScalePermille, 1000);
                        stw -= Fx.MulDiv(Fx.MulDiv(Fx.Shr(stw, 5), tr.ForgetRate, 1000), decayScalePermille, 1000);
                    }
                e.Ltw = Fx.Clamp16(ltw);
                e.Stw = Fx.Clamp16(stw);
                for (int d = 0; d < days; d++) e.Use = (ushort)(e.Use - (e.Use >> tu.UseDecayShift));
                e.Flags &= unchecked((byte)~AssocEntry.TouchedBit);
            }

            // 5. conjunction strength; free empty slots (their entries go)
            for (int s = 0; s < st.Conj.Length; s++)
            {
                ref var c = ref st.Conj[s];
                if (c.Strength == 0) { c.Flags = 0; c.DeltaDay = 0; continue; }
                int strength = c.Strength;
                if ((c.Flags & 1) != 0) strength += tu.ConjGain + c.DeltaDay;
                if (strength > 4000) strength = 4000;
                for (int d = 0; d < days && strength > 0; d++)
                {
                    int loss = strength >> tu.ConjDecayShift;
                    strength -= loss < 1 ? 1 : loss;
                }
                c.Flags = 0; c.DeltaDay = 0;
                if (strength <= 0) FreeSlot(st, s, sink);
                else c.Strength = (ushort)strength;
            }

            // 6. recruitment from the day's moments, then the day is cleared
            Recruit(st, tu, today, scratch, sink);
            st.DayCount = 0;

            // 7. eviction down to the headroom
            int keep = st.Entries.Length - tu.EvictHeadroom;
            while (st.Count > keep && EvictOne(st, today)) { }
        }

        private static void FreeSlot(AssociationStore st, int s, ILearningSink sink)
        {
            ref var c = ref st.Conj[s];
            uint key = Feature.Conj(s, c.Generation);
            sink?.Freed(s, c.A, c.B, c.C);
            for (int i = st.Count - 1; i >= 0; i--) if (st.Entries[i].Key == key) st.RemoveAt(i);
            byte gen = (byte)(c.Generation + 1);
            c = new Conjunction { Generation = gen };
            st.Frees++;
        }

        /// <summary>
        /// Conjunction recruitment (4.11), pairs only in M3: every (drive feature, other key) pair of every moment, scored by
        /// the summed |delta| of the moments it appears in; existing pairs skipped; the best <c>RecruitPerNight</c> (ties by
        /// lowest pair) go into free slots, or replace the weakest slot below <c>ConjReplaceBelow</c>.
        /// </summary>
        private static void Recruit(AssociationStore st, LearningTuning tu, int today, RecruitScratch sc, ILearningSink sink)
        {
            if (st.DayCount == 0 || st.Conj.Length == 0 || tu.RecruitPerNight <= 0) return;
            sc.Count = 0;
            for (int m = 0; m < st.DayCount; m++)
            {
                int off = m * AssociationStore.MomentKeys, n = st.Day[m].KeyCount, w = Fx.Abs(st.Day[m].Delta);
                for (int i = 0; i < n; i++)
                {
                    uint a = st.DayKeys[off + i];
                    if (!Feature.IsDrive(a)) continue;
                    for (int j = 0; j < n; j++)
                    {
                        uint b = st.DayKeys[off + j];
                        if (Feature.IsDrive(b)) continue; // at most one drive per conjunction
                        // A drive with what was acted on ("food is good when hungry") counts double: base features such as
                        // the place or the time are in every moment and would otherwise always win (M3 finding; DEV_LOG).
                        sc.Add(a, b, Feature.IsTargetKind(Feature.KindOf(b)) ? w * tu.RecruitTargetWeight : w);
                    }
                }
            }
            if (sc.Count == 0) return;
            sc.SortByScore();
            int taken = 0;
            for (int i = 0; i < sc.Count && taken < tu.RecruitPerNight; i++)
            {
                uint a = sc.A[i], b = sc.B[i];
                if (HasPair(st, a, b)) continue;
                int slot = -1;
                for (int s = 0; s < st.Conj.Length; s++) if (st.Conj[s].Strength == 0) { slot = s; break; }
                if (slot < 0)
                {
                    int weakest = -1;
                    for (int s = 0; s < st.Conj.Length; s++)
                        if (st.Conj[s].Strength < tu.ConjReplaceBelow && (weakest < 0 || st.Conj[s].Strength < st.Conj[weakest].Strength)) weakest = s;
                    if (weakest < 0) break;
                    FreeSlot(st, weakest, sink);
                    slot = weakest;
                }
                byte gen = st.Conj[slot].Generation;
                st.Conj[slot] = new Conjunction { A = a, B = b, C = 0, Strength = (ushort)tu.ConjStartStrength, LastDay = (ushort)today, Generation = gen };
                st.Recruits++;
                sink?.Recruited(slot, a, b, 0, tu.ConjStartStrength);
                taken++;
            }
        }

        private static bool HasPair(AssociationStore st, uint a, uint b)
        {
            for (int s = 0; s < st.Conj.Length; s++)
                if (st.Conj[s].Strength > 0 && st.Conj[s].A == a && st.Conj[s].B == b) return true;
            return false;
        }

        // ---------------- conjunction matching (4.3) ----------------

        /// <summary>Once per think: the used slots whose drive part is in the sorted base set ("armed"), in slot order.</summary>
        public static int Arm(AssociationStore st, uint[] baseKeys, int nBase, int[] armed)
        {
            int n = 0;
            for (int s = 0; s < st.Conj.Length && n < armed.Length; s++)
            {
                var c = st.Conj[s];
                if (c.Strength == 0) continue;
                if (InstinctTable.Contains(baseKeys, 0, nBase, c.A)) armed[n++] = s;
            }
            return n;
        }

        /// <summary>
        /// Per candidate: the armed slots whose other part is in the base set or the candidate's target keys add their Conj
        /// key (at most 8, slot order). Returns how many were written to <paramref name="into"/> at <paramref name="at"/>.
        /// </summary>
        public static int MatchConj(AssociationStore st, int[] armed, int nArmed, uint[] baseKeys, int nBase, uint[] target, int tOff, int nTarget,
            uint[] into, int at, int max)
        {
            int w = 0;
            for (int i = 0; i < nArmed && w < max; i++)
            {
                var c = st.Conj[armed[i]];
                bool hit = InstinctTable.Contains(baseKeys, 0, nBase, c.B);
                if (!hit) for (int t = 0; t < nTarget; t++) if (target[tOff + t] == c.B) { hit = true; break; }
                if (hit) into[at + w++] = Feature.Conj(armed[i], c.Generation);
            }
            return w;
        }
    }

    /// <summary>Reusable buffers for recruitment (not state): pairs and their scores.</summary>
    public sealed class RecruitScratch
    {
        public uint[] A = new uint[2048];
        public uint[] B = new uint[2048];
        public int[] Score = new int[2048];
        public int Count;

        public void Add(uint a, uint b, int w)
        {
            for (int i = 0; i < Count; i++) if (A[i] == a && B[i] == b) { Score[i] += w; return; }
            if (Count >= A.Length) return;
            A[Count] = a; B[Count] = b; Score[Count] = w; Count++;
        }

        /// <summary>Highest score first; ties by lowest (A, B). Insertion sort (small lists).</summary>
        public void SortByScore()
        {
            for (int i = 1; i < Count; i++)
            {
                uint a = A[i], b = B[i]; int s = Score[i];
                int j = i - 1;
                while (j >= 0 && Before(s, a, b, Score[j], A[j], B[j])) { A[j + 1] = A[j]; B[j + 1] = B[j]; Score[j + 1] = Score[j]; j--; }
                A[j + 1] = a; B[j + 1] = b; Score[j + 1] = s;
            }
        }

        private static bool Before(int s1, uint a1, uint b1, int s2, uint a2, uint b2) =>
            s1 != s2 ? s1 > s2 : a1 != a2 ? a1 < a2 : b1 < b2;
    }
}
