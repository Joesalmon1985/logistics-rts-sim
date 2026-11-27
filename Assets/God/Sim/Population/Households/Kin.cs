using System;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// Kin cues (HOUSEHOLDS "The kinship graph (believed)", BIOLOGICAL 4.6): the only writer of <c>NpcMind.Kin</c>. Beliefs form
    /// from what a person could witness (a birth, being raised, co-residence, a partnership, being told) and never from the
    /// pedigree; every belief has a source (M4.11). At most 32 beliefs: the weakest (confidence, then the higher id) goes.
    /// </summary>
    public static class KinCues
    {
        /// <summary>Sets (or strengthens) <paramref name="npc"/>'s belief about <paramref name="other"/>. A stronger role replaces a weaker one.</summary>
        public static void Believe(GodState s, EventBuffer ev, Npc npc, int other, KinRole role, int confidence, KinSource source)
        {
            var k = npc.Mind?.Kin;
            if (k == null || other == npc.Id || other <= 0) return;
            confidence = Math.Max(0, Math.Min(1000, confidence));
            int i = k.IndexOf(other);
            if (i >= 0)
            {
                var b = k.Beliefs[i];
                if (b.Role == role && b.Confidence >= confidence) return;
                if (b.Role != role && b.Confidence > confidence && Strength(b.Role) >= Strength(role)) return;
                b.Role = role; b.Confidence = (short)confidence; b.Source = source; b.Tick = (int)s.Tick;
                k.Beliefs[i] = b;
            }
            else
            {
                if (k.Beliefs.Count >= KinMind.Cap)
                {
                    int worst = 0;
                    for (int j = 1; j < k.Beliefs.Count; j++) if (k.Beliefs[j].Confidence <= k.Beliefs[worst].Confidence) worst = j;
                    if (k.Beliefs[worst].Confidence > confidence) return;
                    k.Beliefs.RemoveAt(worst);
                }
                var nb = new KinBelief { Other = other, Role = role, Confidence = (short)confidence, Source = source, Tick = (int)s.Tick };
                int at = 0;
                while (at < k.Beliefs.Count && k.Beliefs[at].Other < other) at++;
                k.Beliefs.Insert(at, nb);
            }
            ev?.Add(new KinBeliefFormed { Npc = npc.Id, Other = other, Role = role, Confidence = confidence, Source = source });
        }

        /// <summary>Drops a belief (a partnership ended, a partner died: the role no longer holds).</summary>
        public static void Forget(Npc npc, int other)
        {
            var k = npc.Mind?.Kin;
            if (k == null) return;
            int i = k.IndexOf(other);
            if (i >= 0) k.Beliefs.RemoveAt(i);
        }

        /// <summary>How much a role binds (so a parent belief is not overwritten by a weaker household cue).</summary>
        public static int Strength(KinRole r)
        {
            switch (r)
            {
                case KinRole.Mother: case KinRole.Father: case KinRole.Daughter: case KinRole.Son: return 5;
                case KinRole.Partner: return 4;
                case KinRole.FullSibling: case KinRole.HalfSibling: return 3;
                case KinRole.FosterParent: case KinRole.FosterChild: case KinRole.StepParent: case KinRole.StepChild: return 2;
                default: return 1;
            }
        }

        public static KinRole ChildRole(byte sex) => sex == 0 ? KinRole.Daughter : KinRole.Son;
        public static KinRole ParentRole(byte sex) => sex == 0 ? KinRole.Mother : KinRole.Father;

        /// <summary>
        /// The cues of a birth (HOUSEHOLDS "Kin beliefs at birth"): the mother saw it (950); the believed father is her partner
        /// (700, from the partnership); siblings in the household are believed full or half siblings from what the parents
        /// believe; the newborn's own beliefs start from who is there.
        /// </summary>
        public static void Birth(GodState s, EventBuffer ev, Npc child, Npc mother, Npc partner)
        {
            var cl = child.Life;
            Believe(s, ev, mother, child.Id, ChildRole(cl.Sex), 950, KinSource.Born);
            Believe(s, ev, child, mother.Id, KinRole.Mother, 900, KinSource.Born);
            if (partner != null && partner.Alive)
            {
                Believe(s, ev, partner, child.Id, ChildRole(cl.Sex), 700, KinSource.Partnered);
                Believe(s, ev, child, partner.Id, ParentRole(partner.Life?.Sex ?? 1), 700, KinSource.Partnered);
            }
            // siblings: the mother's other believed children
            var mk = mother.Mind?.Kin;
            if (mk == null) return;
            foreach (var b in mk.Beliefs)
            {
                if (b.Other == child.Id || b.Role != KinRole.Daughter && b.Role != KinRole.Son) continue;
                var sib = s.Population.Find(b.Other);
                if (sib == null || !sib.Alive) continue;
                bool sameFather = partner != null && KinData.RoleOf(partner.Mind, sib.Id) is KinRole r && (r == KinRole.Daughter || r == KinRole.Son);
                var role = sameFather ? KinRole.FullSibling : KinRole.HalfSibling;
                Believe(s, ev, sib, child.Id, role, 800, KinSource.Raised);
                Believe(s, ev, child, sib.Id, role, 800, KinSource.Raised);
            }
        }
    }
}
