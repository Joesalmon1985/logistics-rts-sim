using System;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// F2b (LEAD_RULINGS R61, owner D4 §2): the partner-sex preference, the one place outside reproduction and the phenotype
    /// that reads a person's sex (M4.28's static check lists this file with that reason). An individual preference about
    /// romantic partnership only: which sexes this person seeks as a partner. It is asked by proposing and accepting a
    /// partnership and nowhere else; reproductive relationships, care, households, adoption and fostering never read it.
    /// While <c>Households.Prefs.PartnerSexMode</c> is 0 nothing is drawn and every answer is "yes" (sex-blind, the M4 rule).
    /// </summary>
    public static partial class Preferences
    {
        /// <summary>Draws the preference from its own stream ("partnersex", id), so no other draw moves.</summary>
        public static void DrawPartnerSex(GodState s, Npc npc, NpcLife life) =>
            life.PartnerSeeks = PartnerSex(s.Rng.Fork("partnersex", npc.Id).NextInt(1000), life.Sex, s.Tuning.Households.Prefs);

        /// <summary>F2b partner-sex preference values (<see cref="NpcLife.PartnerSeeks"/>): bits for women and men; 0 = not drawn.</summary>
        public const int SeeksWomen = 1, SeeksMen = 2, SeeksBoth = 3;

        /// <summary>F2b: the preference for a roll 0..999 and its own sex (0 woman, 1 man): the other sex, its own sex, else both.</summary>
        public static int PartnerSex(int roll, byte sex, PrefTuning pt)
        {
            int other = Math.Max(0, Math.Min(1000, pt.SeekOtherSexPermille)), same = Math.Max(0, Math.Min(1000 - other, pt.SeekSameSexPermille));
            int own = sex == 0 ? SeeksWomen : SeeksMen, opp = sex == 0 ? SeeksMen : SeeksWomen;
            return roll < other ? opp : roll < other + same ? own : SeeksBoth;
        }

        /// <summary>F2b: whether this person seeks a partner of that sex (always, when its preference was not drawn).</summary>
        public static bool Seeks(NpcLife l, byte sex) => l == null || l.PartnerSeeks == 0 || (l.PartnerSeeks & (sex == 0 ? SeeksWomen : SeeksMen)) != 0;

        /// <summary>
        /// F2b: whether each of the two seeks the other's sex as a partner (mutual). Asked only of romantic partnership
        /// (proposing and accepting); true for everyone while the preference is not drawn (<c>PartnerSexMode</c> 0).
        /// </summary>
        public static bool PartnerCompatible(Npc a, Npc b)
        {
            if (a?.Life == null || b?.Life == null) return true;
            return Seeks(a.Life, b.Life.Sex) && Seeks(b.Life, a.Life.Sex);
        }
    }
}
