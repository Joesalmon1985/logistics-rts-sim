using System;

namespace God.Sim.Learning
{
    /// <summary>
    /// C7b (OWNER: C7; NPC_LEARNING 2 mechanism 3, 4.14; CULTURAL_TRANSMISSION 4.9): the copy routine of teaching and
    /// demonstration. It uses only public members of <see cref="AssociationStore"/> and <see cref="Learner"/> (C7-T3): the
    /// teacher's strongest associations for a topic's verbs are written into the learner's own store as entries of origin
    /// <see cref="Origin.Imitated"/> (teaching) or <see cref="Origin.Vicarious"/> (demonstration), with the teacher's sign and a
    /// value scaled by the fidelity and by the learner's trust in the teacher. What is copied may be wrong: a mistake is copied
    /// as it is. The teacher's store is never changed; culture state is never touched (associations only, never traits).
    /// </summary>
    public static class Imitation
    {
        /// <summary>The learning verbs of a topic (a general topic id: SkillClass values). 0 verbs: nothing to copy.</summary>
        public static int VerbsOf(int topic, byte[] into)
        {
            switch (topic)
            {
                case 1: into[0] = 107; return 1;            // Medicine: Treat (C7)
                case 2: into[0] = Verb.Gather; return 1;    // Woodcutting: gathering (wood with an axe)
                case 3: into[0] = Verb.ForageAt; into[1] = Verb.Eat; return 2; // Foraging
                default: return 0;
            }
        }

        /// <summary>The trust factor (permille) of a trust value -1000..1000: 0 at full distrust, 500 for a stranger, 1000 at full trust.</summary>
        public static int TrustFactor(int trust) => Math.Max(0, Math.Min(1000, 500 + trust / 2));

        /// <summary>The value the learner gets for one teacher value: <c>v x fidelity/1000 x trustFactor/1000</c>.</summary>
        public static int Scaled(int v, int fidelity, int trustFactor) => (int)((long)v * fidelity / 1000 * trustFactor / 1000);

        /// <summary>
        /// Copies at most <paramref name="maxEntries"/> of the teacher's entries for the topic's verbs (strongest slow weight
        /// first; ties by (key, verb)) into the learner. Returns the number written. <paramref name="origin"/> is
        /// <see cref="Origin.Imitated"/> for teaching, <see cref="Origin.Vicarious"/> for a demonstration.
        /// </summary>
        public static int Copy(AssociationStore teacher, AssociationStore learner, int topic, int fidelity, int trustFactor, int maxEntries, ushort today, Origin origin)
        {
            if (teacher == null || learner == null || ReferenceEquals(teacher, learner) || fidelity <= 0 || trustFactor <= 0 || maxEntries <= 0) return 0;
            var verbs = new byte[4];
            int nv = VerbsOf(topic, verbs);
            if (nv == 0) return 0;
            // the teacher's candidates, best first (a small selection: at most maxEntries kept)
            var pick = new int[maxEntries];
            int np = 0;
            for (int i = 0; i < teacher.Count; i++)
            {
                var e = teacher.Entries[i];
                bool mine = false;
                for (int k = 0; k < nv; k++) if (e.Verb == verbs[k]) mine = true;
                if (!mine || e.Ltw == 0 && e.Stw == 0) continue;
                int at = np;
                while (at > 0 && Better(teacher.Entries[i], teacher.Entries[pick[at - 1]])) at--;
                if (at >= maxEntries) continue;
                if (np < maxEntries) np++;
                for (int k = np - 1; k > at; k--) pick[k] = pick[k - 1];
                pick[at] = i;
            }
            // read the teacher's values first (the learner's store may be the one that changes)
            var keys = new uint[np]; var vbs = new byte[np]; var stw = new int[np]; var ltw = new int[np];
            for (int k = 0; k < np; k++) { var e = teacher.Entries[pick[k]]; keys[k] = e.Key; vbs[k] = e.Verb; stw[k] = e.Stw; ltw[k] = e.Ltw; }
            int written = 0;
            for (int k = 0; k < np; k++)
            {
                int s = Scaled(ltw[k] != 0 ? ltw[k] : stw[k], fidelity, trustFactor);
                if (s == 0) continue;
                int at = learner.Find(keys[k], vbs[k]);
                if (at < 0)
                {
                    if (learner.Count >= learner.EntryCap && !Learner.EvictOne(learner, today)) continue;
                    at = learner.Find(keys[k], vbs[k]);
                    if (at >= 0) continue;
                    at = learner.InsertAt(~at, keys[k], vbs[k], today, origin);
                }
                var le = learner.Entries[at];
                if (le.Locked) continue;
                le.Stw = (short)Math.Max(-32000, Math.Min(32000, s));
                le.Ltw = (short)Math.Max(-32000, Math.Min(32000, s));
                le.Flags = (byte)((le.Flags & ~3) | (byte)origin);
                le.LastDay = today;
                learner.Entries[at] = le;
                written++;
            }
            return written;
        }

        private static bool Better(in AssocEntry a, in AssocEntry b)
        {
            int va = Math.Abs((int)a.Ltw) + Math.Abs((int)a.Stw) / 4, vb = Math.Abs((int)b.Ltw) + Math.Abs((int)b.Stw) / 4;
            if (va != vb) return va > vb;
            return a.Key < b.Key || a.Key == b.Key && a.Verb < b.Verb;
        }
    }
}
