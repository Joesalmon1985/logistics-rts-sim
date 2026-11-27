namespace God.Sim.Population.Households
{
    /// <summary>
    /// F2 (owner D4, LEAD_RULINGS R19): the conception-coordination rule behind <c>Households.Life.ConceptionMode</c>.
    /// <para>Mode 0 (default) is the M4/D4 rule unchanged: at the mother's daily check two romantic partners who both hold
    /// "try for a child" with each other form a reproductive relationship only when both have room under
    /// <c>Partner.MaxLiveUnions</c>. A relationship either of them agreed earlier with a third adult blocks them until it lapses
    /// (<c>Partner.UnionLapseMilli</c>, 0.5 years of neither trying), even when the holder has since chosen to try with someone
    /// else. The F2 diagnosis found this in about a third of all checks where both partners were trying.</para>
    /// <para>Mode 1: a holder's own current choice ends a <b>dormant</b> relationship at once. When both partners hold "try"
    /// with each other at the check, any relationship either holds with a third adult ends (reason <c>Lapsed</c>, the lapse
    /// rule's own meaning: the person stopped choosing it) if it is dormant: the third adult is not trying in it now and no
    /// pregnancy by it is running. Nothing else changes: consent (both must hold "try" with each other), the cap, commitments,
    /// the biology and the random draw are those of mode 0, and a relationship with a live intent or pregnancy still blocks.</para>
    /// </summary>
    public static class Conception
    {
        /// <summary>
        /// Mode 1 only: before the union check, end the dormant relationships that keep two mutually trying partners apart.
        /// Called from <see cref="Reproduction.Daily"/> after both tries were confirmed and before the union is looked up.
        /// </summary>
        public static void BeforeUnion(GodState s, EventBuffer ev, Npc mother, Npc father)
        {
            if (s.Tuning.Households.Life.ConceptionMode != 1) return;
            if (ReproductiveUnions.Between(s, mother.Id, father.Id) != null) return;
            if (Partnerships.Between(s, mother.Id, father.Id) == null) return;
            EndDormant(s, ev, mother);
            EndDormant(s, ev, father);
        }

        /// <summary>Ends this adult's relationships with third adults that are dormant (the other not trying in it, no pregnancy by it).</summary>
        private static void EndDormant(GodState s, EventBuffer ev, Npc who)
        {
            var hs = s.Households;
            for (int i = hs.Unions.Count - 1; i >= 0; i--)
            {
                var u = hs.Unions[i];
                if (u.A != who.Id && u.B != who.Id) continue;
                var other = s.Population.Find(u.A == who.Id ? u.B : u.A);
                if (other?.Life != null && Bodies.IsLivingPerson(other))
                {
                    if (other.Life.TryWith == who.Id && other.Life.TryingUntil >= s.Tick) continue;
                    if (other.Life.PregFather == who.Id || who.Life.PregFather == other.Id) continue;
                }
                ReproductiveUnions.End(s, ev, u, ReproductiveUnionEnded.Lapsed);
            }
        }
    }
}
