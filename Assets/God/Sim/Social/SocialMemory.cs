using System;
using God.Sim.Cognition;
using God.Sim.Learning;

namespace God.Sim.Social
{
    /// <summary>What a change of one relation was: trust and familiarity before and after (for <c>TrustChanged</c>).</summary>
    public struct RelChange
    {
        public int Other;
        public int TrustBefore, TrustAfter, FamBefore, FamAfter;
        public bool Changed => TrustBefore != TrustAfter || FamBefore != FamAfter;
    }

    /// <summary>
    /// The rules of social memory (SOCIAL_MEMORY 4, the M5 part), pure over one mind's tables: recognition chances,
    /// relation slots and eviction by importance, the evidence table, the daily forgetting pass, the relationship class,
    /// and the forgiveness score. Integers only; no state is read but the mind's own; no random draw (the callers draw
    /// from their keyed streams).
    /// </summary>
    public static class SocialMemory
    {
        // ---------------- recognition (4.1) ----------------

        /// <summary>The chance (per mille) of not recognising someone of this familiarity: <c>FailBase x (1000 - Fam)/1000</c>, +dark, +far, clamp 0..900.</summary>
        public static int MissPermille(SocialTuning tu, int familiarity, bool dark, bool far)
        {
            int miss = tu.RecogniseFailBase * (1000 - Fx.Clamp(familiarity, 0, 1000)) / 1000;
            if (dark) miss += tu.RecogniseDark;
            if (far) miss += tu.RecogniseFar;
            return Fx.Clamp(miss, 0, 900);
        }

        /// <summary>The trust a stranger starts with: <c>Trustfulness x 3</c>, +100 for the same faction (SOCIAL 4.2).</summary>
        public static int StrangerTrust(int trustfulness, bool sameFaction) => Fx.Clamp(trustfulness * 3 + (sameFaction ? 100 : 0), -1000, 1000);

        // ---------------- slots and eviction (4.7) ----------------

        public static bool HasOpenDeal(SocialMind m, int other)
        {
            for (int i = 0; i < m.DealCount; i++)
            {
                var d = m.Deals[i];
                if (d.Partner == other && (d.Status == DealStatus.Agreed || d.Status == DealStatus.Proposed)) return true;
            }
            return false;
        }

        public static bool Protected(SocialMind m, in Relation r) =>
            (r.Flags & (RelFlags.HouseholdMate | RelFlags.Partner)) != 0 || HasOpenDeal(m, r.Other);

        /// <summary>
        /// <c>Importance = |Trust|/2 + |Affection|/2 + Fear + Grudge/2 + Familiarity/4 + 150 (open deal) + 150 (ledger) +
        /// 100 (betrayed)</c>, divided by <c>1 + days since the last interaction / 10</c> (integer: x10 / (10 + days)).
        /// </summary>
        public static int Importance(SocialMind m, in Relation r, int tick, int ticksPerDay)
        {
            int v = Fx.Abs(r.Trust) / 2 + Fx.Abs(r.Affection) / 2 + r.Fear + r.Grudge / 2 + r.Familiarity / 4
                + (HasOpenDeal(m, r.Other) ? 150 : 0) + (r.OwedToMe != 0 || r.IOwe != 0 ? 150 : 0) + (r.Betrayals > 0 ? 100 : 0);
            int days = ticksPerDay > 0 ? Math.Max(0, (tick - r.LastTick) / ticksPerDay) : 0;
            return v * 10 / (10 + days);
        }

        /// <summary>
        /// The slot of <paramref name="other"/>, making one when it has none: a free slot, else the least important
        /// unprotected slot (ties to the lowest index) if the newcomer's importance is higher; else -1 (it stays a stranger
        /// seen). <paramref name="evicted"/> is the relation it replaced (Other 0 for none).
        /// </summary>
        public static int Touch(SocialMind m, int other, int tick, int ticksPerDay, int startTrust, int startFamiliarity, out Relation evicted, out int evictedImportance)
        {
            evicted = default; evictedImportance = 0;
            if (other <= 0) return -1;
            int i = m.Find(other);
            if (i >= 0) return i;
            var fresh = new Relation { Other = other, FirstTick = tick, LastTick = tick, LastSeenTick = tick, LastNode = -1, LastContactDay = -1, Trust = (short)startTrust, Familiarity = (short)startFamiliarity };
            if (m.RelCount < m.Rel.Length) { m.Rel[m.RelCount] = fresh; return m.RelCount++; }
            int worst = -1, worstImp = int.MaxValue;
            for (int k = 0; k < m.RelCount; k++)
            {
                if (Protected(m, m.Rel[k])) continue;
                int imp = Importance(m, m.Rel[k], tick, ticksPerDay);
                if (imp < worstImp) { worstImp = imp; worst = k; }
            }
            if (worst < 0) return -1;
            int mine = Importance(m, fresh, tick, ticksPerDay);
            if (mine <= worstImp) return -1;
            evicted = m.Rel[worst]; evictedImportance = worstImp;
            m.Rel[worst] = fresh;
            return worst;
        }

        // ---------------- the evidence table (4.2) ----------------

        private static short S(int v, int lo, int hi) => (short)Fx.Clamp(v, lo, hi);

        /// <summary>A trust gain, slowed by a grudge once it was betrayed: <c>x (1000 - Grudge)/1000</c>.</summary>
        public static int GainAfterGrudge(in Relation r, int delta) => r.Betrayals > 0 || r.Grudge > 0 ? delta * (1000 - r.Grudge) / 1000 : delta;

        public static void Familiar(ref Relation r, int add) { r.Familiarity = S(r.Familiarity + add, 0, 1000); }

        /// <summary>Received a gift or help of value v (value units): trust <c>+min(cap, v/2) x (1000 - Trust)/1000</c> (after the grudge factor), affection +delta/2.</summary>
        public static int GiftReceived(SocialTuning tu, ref Relation r, int v, int tick)
        {
            int delta = Math.Min(tu.TrustGiftCap, Math.Max(0, v) / 2) * (1000 - r.Trust) / 1000;
            delta = GainAfterGrudge(r, delta);
            r.Trust = S(r.Trust + delta, -1000, 1000);
            r.Affection = S(r.Affection + delta / 2, -1000, 1000);
            r.HelpReceived = S(r.HelpReceived + v, -30000, 30000);
            r.IOwe = S(r.IOwe + v, -30000, 30000);
            r.LastMoveTheirs = 1;
            if (r.GiftsIn < 255) r.GiftsIn++;
            r.LastTick = tick;
            return delta;
        }

        /// <summary>Gave a gift or help of value v: affection <c>+v/10</c> (liking those we help), the ledger.</summary>
        public static void GiftGiven(ref Relation r, int v, int tick)
        {
            r.Affection = S(r.Affection + Math.Max(0, v) / 10, -1000, 1000);
            r.HelpGiven = S(r.HelpGiven + v, -30000, 30000);
            r.OwedToMe = S(r.OwedToMe + v, -30000, 30000);
            r.LastMoveMine = 1;
            if (r.GiftsOut < 255) r.GiftsOut++;
            r.LastTick = tick;
        }

        /// <summary>A barter completed, both hand-overs seen: trust +30 (after the grudge factor), affection +10.</summary>
        public static void BarterCompleted(SocialTuning tu, ref Relation r, int tick)
        {
            r.Trust = S(r.Trust + GainAfterGrudge(r, tu.TrustBarter), -1000, 1000);
            r.Affection = S(r.Affection + tu.AffectionBarter, -1000, 1000);
            r.LastMoveTheirs = 1; r.LastMoveMine = 1;
            if (r.DealsDone < 255) r.DealsDone++;
            r.LastTick = tick;
        }

        /// <summary>The severity of a betrayal of value v (the partner took and did not hand over): <c>clamp(v/2, 20, 255)</c>.</summary>
        public static int SeverityOf(int value) => Fx.Clamp(value / 2, 20, 255);

        /// <summary>A betrayal of severity sev: trust <c>-4 sev</c>, affection <c>-sev</c>, grudge <c>+3 sev</c> (cap 1000), a betrayal counted.</summary>
        public static void Betrayed(ref Relation r, int sev, int tick)
        {
            r.Trust = S(r.Trust - 4 * sev, -1000, 1000);
            r.Affection = S(r.Affection - sev, -1000, 1000);
            r.Grudge = S(r.Grudge + 3 * sev, 0, 1000);
            if (r.Betrayals < 255) r.Betrayals++;
            r.LastMoveTheirs = 2;
            r.LastTick = tick;
        }

        /// <summary>Refused my request or offer: trust -60, affection -30 (not a betrayal: nothing was promised).</summary>
        public static void Refused(SocialTuning tu, ref Relation r, int tick)
        {
            r.Trust = S(r.Trust + tu.RefusedTrust, -1000, 1000);
            r.Affection = S(r.Affection + tu.RefusedAffection, -1000, 1000);
            r.LastMoveTheirs = 2;
            r.LastTick = tick;
        }

        // ---------------- apology and forgiveness (4.5; the scores, used from M7) ----------------

        /// <summary>The receiver's score for forgiving: <c>Forgiveness x 3 + Affection/4 + gift/2 - Grudge/2 - Betrayals x 40</c> (noise ±50 is the caller's).</summary>
        public static int ForgiveScore(int forgiveness, in Relation r, int giftValue) =>
            forgiveness * 3 + r.Affection / 4 + giftValue / 2 - r.Grudge / 2 - r.Betrayals * 40;

        /// <summary>Choosing to forgive: grudge <c>-(200 + gift)</c>, trust +100, forgiven counted.</summary>
        public static void Forgive(ref Relation r, int giftValue)
        {
            r.Grudge = S(r.Grudge - 200 - giftValue, 0, 1000);
            r.Trust = S(r.Trust + 100, -1000, 1000);
            if (r.Forgiven < 255) r.Forgiven++;
        }

        // ---------------- forgetting (4.7) ----------------

        /// <summary>A per-day rate times <c>ScalePermille</c>, rounded half up, at least 1 where the base is non-zero.</summary>
        public static int Scaled(int rate, int scalePermille)
        {
            if (rate <= 0) return 0;
            if (scalePermille <= 0) return 0;
            int v = (int)(((long)rate * scalePermille + 500) / 1000);
            return v < 1 ? 1 : v;
        }

        /// <summary>The grudge decay of this NPC per mille per day: <c>clamp(GrudgeBase - Vengefulness/2, GrudgeFloor, GrudgeCeil)</c>, scaled.</summary>
        public static int GrudgeRate(SocialDecayTuning d, int vengefulness)
        {
            if (d.GrudgeBase == 0 && d.GrudgeFloor == 0 && d.GrudgeCeil == 0) return 0;
            int rate = Fx.Clamp(d.GrudgeBase - vengefulness / 2, d.GrudgeFloor, d.GrudgeCeil);
            return Scaled(rate, d.ScalePermille);
        }

        /// <summary>
        /// One relation's daily pass: familiarity falls without contact today; the grudge decays at the NPC's rate (the
        /// fraction carried, so the decay is exact); trust drifts towards the stranger prior while the other is little known
        /// and not resented; fear fades.
        /// </summary>
        public static void DayPass(SocialDecayTuning d, ref Relation r, int vengefulness, bool contactToday, int strangerPrior)
        {
            if (!contactToday) r.Familiarity = S(r.Familiarity - Scaled(d.FamiliarityPerDay, d.ScalePermille), 0, 1000);
            int rate = GrudgeRate(d, vengefulness);
            if (r.Grudge > 0 && rate > 0)
            {
                int loss = r.Grudge * rate + r.GrudgeRem;
                r.Grudge = S(r.Grudge - loss / 1000, 0, 1000);
                r.GrudgeRem = (short)(r.Grudge == 0 ? 0 : loss % 1000);
            }
            int drift = Scaled(d.TrustDriftPermille, d.ScalePermille);
            if (r.Familiarity < 300 && r.Grudge <= 300 && drift > 0)
                r.Trust = S(r.Trust + (strangerPrior - r.Trust) * drift / 1000, -1000, 1000);
            if (d.FearShift > 0 && r.Fear > 0)
            {
                int loss = Math.Max(1, (r.Fear >> d.FearShift) * d.ScalePermille / 1000);
                r.Fear = S(r.Fear - loss, 0, 1000);
            }
        }

        // ---------------- the relationship class (4.3) ----------------

        /// <summary>
        /// The class of a relation (SOCIAL 4.3 priority): partner and household mate (flags), enemy, rival, debtor or
        /// creditor (an exchange half done), kin (believed r̂ at least about 0.12), friend, acquaintance, stranger.
        /// </summary>
        public static NpcRelClass ClassOf(SocialMind m, in Relation r)
        {
            if ((r.Flags & RelFlags.Partner) != 0) return NpcRelClass.Partner;
            if ((r.Flags & RelFlags.HouseholdMate) != 0) return NpcRelClass.HouseholdMate;
            if (r.Grudge > 600 || r.Fear > 500 && r.Trust < 0) return NpcRelClass.Enemy;
            if (r.Grudge > 300) return NpcRelClass.Rival;
            for (int i = 0; i < m.DealCount; i++)
            {
                var d = m.Deals[i];
                if (d.Partner != r.Other || d.Status != DealStatus.Agreed) continue;
                if (d.MyGiven > 0 && d.TheirGiven < d.TheirCount) return NpcRelClass.Debtor;
                if (d.TheirGiven > 0 && d.MyGiven < d.MyCount) return NpcRelClass.Creditor;
            }
            if (r.KinCue >= 60) return NpcRelClass.Kin;
            if (r.Trust > 300 && r.Affection > 300) return NpcRelClass.Friend;
            if (r.Familiarity >= 100) return NpcRelClass.Acquaintance;
            return NpcRelClass.Stranger;
        }
    }
}
