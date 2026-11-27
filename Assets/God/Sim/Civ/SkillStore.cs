using System;
using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Civ
{
    /// <summary>
    /// The person skill classes (contracts 22.4 "one skill store": the owner's values first, the other package appends; LEAD_RULINGS
    /// R42/R48). A general topic id (C7 spec 22): K1 appends its industrial skills from 16 on without a new mechanism. Append-only.
    /// </summary>
    public enum SkillClass : ushort
    {
        None = 0,
        /// <summary>Healing (INSTITUTIONAL_SERVICES 5.1 <c>Skill.Healing</c>; contracts 22.4 call it Medicine).</summary>
        Medicine = 1,
        /// <summary>Gathering wood with an axe (the C7b teaching topic of scenario 6).</summary>
        Woodcutting = 2,
        /// <summary>Foraging food at wild sources.</summary>
        Foraging = 3,
        // 4-15 free for C7/C-packages; 16+ K1 (Construction, Mechanics, Electrical, ...)
    }

    /// <summary>One person's level in one skill (0-1000).</summary>
    public struct SkillRow
    {
        public int Npc;
        public SkillClass Class;
        public short Value;
    }

    /// <summary>
    /// LEAD_RULINGS R42: the shared person skill store, <c>CivState.Skills</c> (OWNER: C7, the first of C7/K1 to land it; K1
    /// extends it through the same accessor). Rows sorted by (NpcId, class), at most <c>Orgs.C7.SkillSlots</c> classes per
    /// person, created on first use: a game in which nobody healed, taught or learned has no store (null) and hashes as before.
    /// About 8 bytes per held skill.
    /// </summary>
    public sealed partial class SkillStore
    {
        public List<SkillRow> Rows = new List<SkillRow>();

        partial void Hash(ref StateHasher h)
        {
            h.Add(Rows.Count);
            foreach (var r in Rows) { h.Add(r.Npc); h.Add((int)r.Class); h.Add(r.Value); }
        }

        /// <summary>Index of (npc, class), or <c>~insertionPoint</c>.</summary>
        public int IndexOf(int npc, SkillClass cls)
        {
            int lo = 0, hi = Rows.Count - 1;
            long want = ((long)npc << 16) | (ushort)cls;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                long o = ((long)Rows[mid].Npc << 16) | (ushort)Rows[mid].Class;
                if (o == want) return mid;
                if (o < want) lo = mid + 1; else hi = mid - 1;
            }
            return ~lo;
        }

        public int CountOf(int npc)
        {
            int n = 0;
            foreach (var r in Rows) if (r.Npc == npc) n++;
            return n;
        }
    }

    /// <summary>
    /// The one accessor of the skill store (contracts 22.4: <c>Skills.Of</c>, <c>Skills.Gain</c>). Sim side only: a mind never
    /// reads it (a person's skill is a capability of its body and practice, used by its own acts). With the Civ module off
    /// there is no store and every skill reads 0 (so <c>Treat</c> equals <c>Tend</c>).
    /// </summary>
    public static class Skills
    {
        public const int Max = 1000;

        public static int Of(GodState s, int npc, SkillClass cls)
        {
            var st = s.Civ?.Skills;
            if (st == null) return 0;
            int i = st.IndexOf(npc, cls);
            return i >= 0 ? st.Rows[i].Value : 0;
        }

        /// <summary>
        /// Sets a skill (fixtures, start culture and the gain below). Creates the store lazily. A person already holding the
        /// maximum number of classes loses its lowest other skill to make room. Returns false with the Civ module off.
        /// </summary>
        public static bool Set(GodState s, int npc, SkillClass cls, int value, int slots = 4)
        {
            if (s.Civ == null) return false;
            value = Math.Max(0, Math.Min(Max, value));
            var st = s.Civ.SkillsOrCreate();
            int i = st.IndexOf(npc, cls);
            if (i >= 0) { var r = st.Rows[i]; r.Value = (short)value; st.Rows[i] = r; return true; }
            if (value == 0) return true;
            if (st.CountOf(npc) >= Math.Max(1, slots))
            {
                int low = -1;
                for (int k = 0; k < st.Rows.Count; k++) if (st.Rows[k].Npc == npc && (low < 0 || st.Rows[k].Value < st.Rows[low].Value)) low = k;
                if (low >= 0) st.Rows.RemoveAt(low);
                i = st.IndexOf(npc, cls);
            }
            st.Rows.Insert(~i, new SkillRow { Npc = npc, Class = cls, Value = (short)value });
            return true;
        }

        /// <summary>
        /// Raises (or lowers) a skill by <paramref name="delta"/>, never above <paramref name="cap"/> (teaching: below the
        /// teacher's). Returns the new value; <paramref name="from"/> is the old one.
        /// </summary>
        public static int Gain(GodState s, int npc, SkillClass cls, int delta, int cap, int slots, out int from)
        {
            from = Of(s, npc, cls);
            if (s.Civ == null) return from;
            int to = Math.Max(0, Math.Min(Math.Min(Max, cap), from + delta));
            if (delta > 0 && to < from) to = from; // a cap below the current level never lowers it
            if (to != from) Set(s, npc, cls, to, slots);
            return to;
        }

        /// <summary>A person died: its skills go with it (rows removed).</summary>
        public static void Forget(GodState s, int npc)
        {
            var st = s.Civ?.Skills;
            if (st == null) return;
            for (int i = st.Rows.Count - 1; i >= 0; i--) if (st.Rows[i].Npc == npc) st.Rows.RemoveAt(i);
        }

        /// <summary>Sorted, unique, in range.</summary>
        public static void Check(GodState s, List<string> bad)
        {
            var st = s.Civ?.Skills;
            if (st == null) return;
            long prev = -1;
            foreach (var r in st.Rows)
            {
                long o = ((long)r.Npc << 16) | (ushort)r.Class;
                if (o <= prev) bad.Add("SK1: skill rows not ascending at npc " + r.Npc);
                if (r.Value < 0 || r.Value > Max) bad.Add("SK2: skill out of range for npc " + r.Npc);
                prev = o;
            }
        }
    }
}
