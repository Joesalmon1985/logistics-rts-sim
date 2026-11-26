using System.Collections.Generic;
using God.Sim.Cognition;

namespace God.Sim.Population
{
    /// <summary>A believed kin role of another person (SHARED_CONTRACTS 4.3). Data frozen by F0; written only by M4.</summary>
    public enum KinRole : byte
    {
        None = 0, Mother = 1, Father = 2, Daughter = 3, Son = 4, FullSibling = 5, HalfSibling = 6, Grandparent = 7, Grandchild = 8,
        Uncle = 9, Aunt = 10, Nephew = 11, Niece = 12, Cousin = 13, Partner = 14, InLaw = 15, StepParent = 16, StepChild = 17,
        FosterParent = 18, FosterChild = 19,
    }

    /// <summary>Where a kin belief came from (every belief has a source).</summary>
    public enum KinSource : byte { None = 0, Born = 1, Raised = 2, Told = 3, Seen = 4, Resemblance = 5, Partnered = 6 }

    /// <summary>The kin band of a relation (warehouse <c>dim_relationship</c>).</summary>
    public enum KinBand : byte { None = 0, ParentChild = 1, Sibling = 2, OtherKin = 3, Partner = 4 }

    /// <summary>One NPC's belief about how another is related to it (HOUSEHOLDS 3): a belief, never the pedigree.</summary>
    public struct KinBelief
    {
        public int Other;
        public KinRole Role;
        /// <summary>0..1000.</summary>
        public short Confidence;
        public KinSource Source;
        public int Tick;
    }

    /// <summary>
    /// <c>NpcMind.Kin</c>: the believed kin graph of one mind, at most <see cref="Cap"/> beliefs sorted by <see cref="KinBelief.Other"/>
    /// (one per other). Data frozen by F0; written only by M4. Null until M4 gives a mind one.
    /// </summary>
    public sealed class KinMind
    {
        public const int Cap = 32;
        public List<KinBelief> Beliefs = new List<KinBelief>();

        /// <summary>The index of the belief about <paramref name="other"/>, or -1.</summary>
        public int IndexOf(int other)
        {
            int lo = 0, hi = Beliefs.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = Beliefs[mid].Other;
                if (v == other) return mid;
                if (v < other) lo = mid + 1; else hi = mid - 1;
            }
            return -1;
        }

        public void AddTo(ref StateHasher h)
        {
            h.Add(Beliefs.Count);
            foreach (var b in Beliefs) { h.Add(b.Other); h.Add((int)b.Role | ((int)b.Source << 8)); h.Add(b.Confidence); h.Add(b.Tick); }
        }
    }

    /// <summary>
    /// Pure reads of a mind's kin beliefs (SHARED_CONTRACTS 4.2), for anyone (M5 reads kin through these). With no
    /// <c>KinMind</c> every answer is the default: <see cref="KinRole.None"/>, 0, <see cref="KinBand.None"/>.
    /// </summary>
    public static class KinData
    {
        public static KinRole RoleOf(NpcMind me, int other)
        {
            var k = me?.Kin;
            if (k == null) return KinRole.None;
            int i = k.IndexOf(other);
            return i < 0 ? KinRole.None : k.Beliefs[i].Role;
        }

        /// <summary>Estimated relatedness r̂ in 0..500 (HOUSEHOLDS 4: parent, child, full sibling 500; half sibling, grandparent, grandchild, uncle, aunt, nephew, niece 250; cousin 125; others 0), times Confidence / 1000.</summary>
        public static int RHat(NpcMind me, int other)
        {
            var k = me?.Kin;
            if (k == null) return 0;
            int i = k.IndexOf(other);
            if (i < 0) return 0;
            var b = k.Beliefs[i];
            return RoleR(b.Role) * b.Confidence / 1000;
        }

        public static int RoleR(KinRole r)
        {
            switch (r)
            {
                case KinRole.Mother: case KinRole.Father: case KinRole.Daughter: case KinRole.Son: case KinRole.FullSibling: return 500;
                case KinRole.HalfSibling: case KinRole.Grandparent: case KinRole.Grandchild: case KinRole.Uncle: case KinRole.Aunt:
                case KinRole.Nephew: case KinRole.Niece: return 250;
                case KinRole.Cousin: return 125;
                default: return 0;
            }
        }

        public static KinBand BandOf(NpcMind me, int other) => BandOfRole(RoleOf(me, other));

        public static KinBand BandOfRole(KinRole r)
        {
            switch (r)
            {
                case KinRole.None: return KinBand.None;
                case KinRole.Mother: case KinRole.Father: case KinRole.Daughter: case KinRole.Son: return KinBand.ParentChild;
                case KinRole.FullSibling: case KinRole.HalfSibling: return KinBand.Sibling;
                case KinRole.Partner: return KinBand.Partner;
                default: return KinBand.OtherKin;
            }
        }
    }
}
