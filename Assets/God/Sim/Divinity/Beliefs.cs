using System;
using System.Collections.Generic;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 13.1, 3 (COMMANDMENTS 4.1, 4.2, 4.6): the five separate fields per god (belief, faith, trust, fear, loyalty;
    /// none derived from another, no obedience score), moved only by what the person perceives, each change a
    /// <see cref="BeliefChanged"/> with its cause; founding religions and affiliation spells, which <b>never</b> write
    /// citizenship, <c>Npc.Faction</c> or M5's <c>SocialMind</c> (I-CR-3). Tier 2 runs only with <c>Divinity.Doctrine.Beliefs = 1</c>;
    /// <see cref="Read"/> (the defaults without an entry) serves Tier 1. OWNER: D6.
    /// </summary>
    public static class Beliefs
    {
        /// <summary>A mind's five fields about a god; without an entry, D2's instruction defaults (belief, trust) and D6's (faith, loyalty), fear 0.</summary>
        public static void Read(DivineMind m, int god, out int belief, out int faith, out int trust, out int fear, out int loyalty)
        {
            int i = m?.IndexOfGod(god) ?? -1;
            if (i >= 0)
            {
                var g = m.Gods[i];
                belief = g.Belief; faith = g.Faith; trust = g.Trust; fear = g.Fear; loyalty = g.Loyalty;
                return;
            }
            var it = m?.Tuning ?? new InstructionTuning();
            var dt = m?.DTuning ?? new DoctrineTuning();
            belief = it.DefaultBelief; trust = it.DefaultTrust; faith = dt.DefaultFaith; loyalty = dt.DefaultLoyalty; fear = 0;
        }

        /// <summary>
        /// COMMANDMENTS 4.5: the adoption a person's beliefs in the issuing god support, <c>Belief x (Faith+ + Loyalty) / 2000 + Fear / 4 - offset</c>
        /// (pure; the daily pull moves adoption towards it).
        /// </summary>
        public static int AdoptionTarget(int belief, int faith, int fear, int loyalty, int offset) =>
            (int)((long)belief * (Math.Max(0, faith) + loyalty) / 2000) + fear / 4 - offset;

        // ---------------- evidence (4.2) ----------------

        /// <summary>Moves one person's fields about one god (an entry created at 0 when it has none) and emits <see cref="BeliefChanged"/>.</summary>
        public static void Move(GodState s, EventBuffer ev, Npc n, int god, int cause, int causeRef, int dB, int dF, int dT, int dFe, int dL)
        {
            var m = Doctrine.MindOf(s, n);
            if (m == null) return;
            int i = EntryOf(s, m, god);
            ref var g = ref m.Gods[i];
            int b0 = g.Belief, f0 = g.Faith, t0 = g.Trust, fe0 = g.Fear, l0 = g.Loyalty;
            g.Belief = (short)Fx.Clamp(g.Belief + dB, 0, 1000);
            g.Faith = (short)Fx.Clamp(g.Faith + dF, -1000, 1000);
            g.Trust = (short)Fx.Clamp(g.Trust + dT, -1000, 1000);
            g.Fear = (short)Fx.Clamp(g.Fear + dFe, 0, 1000);
            g.Loyalty = (short)Fx.Clamp(g.Loyalty + dL, -1000, 1000);
            if (cause != 9) g.LastEvidenceTick = s.Tick;
            if (g.Belief == b0 && g.Faith == f0 && g.Trust == t0 && g.Fear == fe0 && g.Loyalty == l0) return;
            ev.Add(new BeliefChanged
            {
                Npc = n.Id, God = god, Cause = cause, CauseRef = causeRef,
                DBelief = g.Belief - b0, DFaith = g.Faith - f0, DTrust = g.Trust - t0, DFear = g.Fear - fe0, DLoyalty = g.Loyalty - l0,
                Belief = g.Belief, Faith = g.Faith, Trust = g.Trust, Fear = g.Fear, Loyalty = g.Loyalty,
            });
        }

        private static int EntryOf(GodState s, DivineMind m, int god)
        {
            int i = m.IndexOfGod(god);
            if (i >= 0) return i;
            int cap = Math.Max(s.FactionCount, god);
            if (m.Gods == null || m.Gods.Length < cap) { var a = new GodBelief[cap]; if (m.Gods != null) Array.Copy(m.Gods, a, m.GodCount); m.Gods = a; }
            // ascending god id
            int at = m.GodCount;
            while (at > 0 && m.Gods[at - 1].God > god) { m.Gods[at] = m.Gods[at - 1]; at--; }
            m.Gods[at] = new GodBelief { God = god };
            m.GodCount++;
            return at;
        }

        /// <summary>
        /// The evidence a person perceived this tick, read from the tick's own events (the divine messages it perceived, signs
        /// it saw, miracles on itself or witnessed, goods a god gave it): attributed to the acting god (DIVINE_PLAYER 4.2).
        /// </summary>
        public static void Evidence(GodState s, EventBuffer ev)
        {
            var items = ev.Items;
            int from = items.Count;
            while (from > 0 && items[from - 1].Tick == s.Tick) from--;
            int to = items.Count;
            if (from == to) return;
            var tu = s.Tuning.Divinity.Doctrine.Evidence;
            var p = s.Population;
            for (int i = from; i < to; i++)
            {
                switch (items[i])
                {
                    case CommandPerceived c:
                    {
                        var n = p.Find(c.Npc);
                        if (n != null && n.Alive) Move(s, ev, n, c.God, 1, c.Intervention, tu.InstructionBelief, 0, 0, 0, 0);
                        break;
                    }
                    case DivineSignPerceived w:
                    {
                        var n = p.Find(w.Witness);
                        int god = GodOf(s, w.Intervention);
                        if (n != null && n.Alive && god != 0) Move(s, ev, n, god, 5, w.Intervention, tu.SignBelief, 0, 0, 0, 0);
                        break;
                    }
                    case MiracleEffectApplied e:
                    {
                        if (e.ThingKind != MiracleThing.Person && e.ThingKind != MiracleThing.Witness) break;
                        if (!Interventions.TryGet(s, e.Intervention, out var r)) break;
                        var n = p.Find(e.ThingId);
                        if (n == null || !n.Alive || !Bodies.IsPerson(n)) break;
                        var kind = KindOf(s, e.Intervention);
                        bool heal = kind == MiracleKind.HealInjury || kind == MiracleKind.HealDisease || kind == MiracleKind.Bless || kind == MiracleKind.Protect;
                        bool smite = kind == MiracleKind.SmitePerson;
                        if (e.ThingKind == MiracleThing.Person)
                        {
                            if (heal) Move(s, ev, n, r.God, 2, e.Intervention, tu.HealedBelief, tu.HealedFaith, tu.HealedTrust, tu.HealedFear, tu.HealedLoyalty);
                            else if (smite && e.Field == "injury") Move(s, ev, n, r.God, 3, e.Intervention, tu.SmittenBelief, 0, tu.SmittenTrust, tu.SmittenFear, tu.SmittenLoyalty);
                        }
                        else
                        {
                            if (heal) Move(s, ev, n, r.God, 7, e.Intervention, tu.HealWitnessedBelief, tu.HealWitnessedFaith, tu.HealWitnessedTrust, 0, tu.HealWitnessedLoyalty);
                            else if (smite) Move(s, ev, n, r.God, 6, e.Intervention, tu.SmiteWitnessedBelief, 0, 0, tu.SmiteWitnessedFear, 0);
                        }
                        break;
                    }
                    case ResourceCreated rc:
                    {
                        if (rc.HolderKind != HolderKind.Carry) break;
                        var n = p.Find(rc.HolderId);
                        if (n != null && n.Alive && Bodies.IsPerson(n)) Move(s, ev, n, rc.God, 4, rc.Intervention, tu.GoodsBelief, tu.GoodsFaith, tu.GoodsTrust, 0, tu.GoodsLoyalty);
                        break;
                    }
                }
            }
        }

        private static int GodOf(GodState s, int intervention) => Interventions.TryGet(s, intervention, out var r) ? r.God : 0;

        private static MiracleKind KindOf(GodState s, int intervention)
        {
            var ms = s.Divinity?.Miracles;
            if (ms == null) return MiracleKind.None;
            foreach (var r in ms.Records) if (r.Intervention == intervention) return r.Kind & ~MiracleKind.Area;
            return MiracleKind.None;
        }

        // ---------------- religions and affiliation (4.1, 4.6) ----------------

        /// <summary>Tier 2 at the first divinity tick: one founding religion per god (867); the god's origin community's living people affiliated (868) and seeded (keyed per person).</summary>
        public static void Found(GodState s, EventBuffer ev)
        {
            var st = Doctrine.StateOf(s);
            if (st.Founded) return;
            st.Founded = true;
            var reg = s.Divinity.Gods;
            if (reg == null) return;
            var f = s.Tuning.Divinity.Doctrine.Founders;
            foreach (var g in reg.Gods)
            {
                var rel = new Religion { Id = DivinityIds.Take(ref st.NextReligionId), Gods = new[] { g.Id }, FoundedTick = s.Tick };
                st.Religions.Add(rel);
                ev.Add(new ReligionFounded { Religion = rel.Id, God = g.Id });
                foreach (var n in s.Population.Npcs)
                {
                    if (!Bodies.IsLivingPerson(n) || n.Mind == null || n.Faction != g.OriginFaction) continue;
                    var rng = s.Rng.Fork("doctrine-founder", n.Id, g.Id);
                    int b = f.Belief + Spread(ref rng, f.BeliefSpread), fa = f.Faith + Spread(ref rng, f.FaithSpread), l = f.Loyalty + Spread(ref rng, f.LoyaltySpread);
                    Move(s, ev, n, g.Id, 8, 0, b, fa, f.Trust, f.Fear, l);
                    Doctrine.MindOf(s, n).Seeded = true;
                    Open(s, ev, n, rel, 1);
                }
            }
        }

        private static int Spread(ref Rng rng, int spread) => spread <= 0 ? 0 : rng.NextInt(2 * spread + 1) - spread;

        /// <summary>Opens an affiliation spell (868). Never touches citizenship, <c>Npc.Faction</c> or <c>SocialMind</c>.</summary>
        public static void Open(GodState s, EventBuffer ev, Npc n, Religion rel, int reason)
        {
            var st = Doctrine.StateOf(s);
            st.Affiliations.Add(new Affiliation { Npc = n.Id, Religion = rel.Id, From = s.Tick, To = -1 });
            Refresh(s, n);
            ev.Add(new AffiliationChanged { Npc = n.Id, Religion = rel.Id, God = rel.Gods[0], Opened = true, Reason = reason, Community = (int)n.Faction, Citizenship = CitizenshipOf(s, n) });
        }

        /// <summary>Closes one open spell (868).</summary>
        public static void Close(GodState s, EventBuffer ev, Npc n, int index, int reason)
        {
            var st = Doctrine.StateOf(s);
            var a = st.Affiliations[index];
            a.To = s.Tick;
            st.Affiliations[index] = a;
            Refresh(s, n);
            var rel = st.FindReligion(a.Religion);
            ev.Add(new AffiliationChanged { Npc = n.Id, Religion = a.Religion, God = rel?.Gods[0] ?? 0, Opened = false, Reason = reason, Community = (int)n.Faction, Citizenship = CitizenshipOf(s, n) });
        }

        /// <summary>The citizenship stand-in (the community until C5 lands <c>CivState.Citizenship</c>): read only, for the event.</summary>
        public static int CitizenshipOf(GodState s, Npc n) => (int)n.Faction;

        /// <summary>The person's own knowledge of its affiliations (the gods of its open spells), for its rules' <c>Affiliated</c> selector.</summary>
        private static void Refresh(GodState s, Npc n)
        {
            var m = Doctrine.MindOf(s, n);
            if (m == null) return;
            int mask = 0;
            var st = s.Divinity.Doctrine;
            foreach (var a in st.Affiliations)
            {
                if (a.Npc != n.Id || !a.Open) continue;
                var rel = st.FindReligion(a.Religion);
                if (rel?.Gods != null) foreach (int g in rel.Gods) if (g >= 1 && g <= 30) mask |= 1 << (g - 1);
            }
            m.AffiliatedGods = mask;
        }

        /// <summary>
        /// One person's day (Tier 2): every field decays towards 0; conversion when the god it holds strongest
        /// (<c>Loyalty + Belief / 2</c>) is not among its religions' gods for <c>ConvertDays</c> with loyalty at least
        /// <c>ConvertFrom</c>; an affiliation lapses when every belief in its gods is below <c>LapseBelief</c>.
        /// </summary>
        public static void Day(GodState s, EventBuffer ev, Npc n, DivineMind m)
        {
            var dt = s.Tuning.Divinity.Doctrine;
            var e = dt.Evidence;
            for (int i = 0; i < m.GodCount; i++)
            {
                var g = m.Gods[i];
                int dB = -g.Belief * e.DecayBelief / 1000, dF = -g.Faith * e.DecayFaith / 1000, dT = -g.Trust * e.DecayTrust / 1000, dFe = -g.Fear * e.DecayFear / 1000,
                    dL = -g.Loyalty * e.DecayLoyalty / 1000;
                if (dB != 0 || dF != 0 || dT != 0 || dFe != 0 || dL != 0) Move(s, ev, n, g.God, 9, 0, dB, dF, dT, dFe, dL);
            }
            var st = s.Divinity.Doctrine;
            if (st == null || m.GodCount == 0) return;
            int best = -1, bv = int.MinValue;
            for (int i = 0; i < m.GodCount; i++)
            {
                int v = m.Gods[i].Loyalty + m.Gods[i].Belief / 2;
                if (v > bv) { bv = v; best = i; }
            }
            var top = m.Gods[best];
            bool inMine = (m.AffiliatedGods >> (top.God - 1) & 1) != 0;
            if (!inMine && top.Loyalty >= dt.ConvertFrom) m.ConvertDays++;
            else m.ConvertDays = 0;
            if (m.ConvertDays >= dt.ConvertDays)
            {
                Religion to = null;
                foreach (var r in st.Religions) if (r.Worships(top.God)) { to = r; break; }
                if (to != null)
                {
                    if (dt.KeepOtherAffiliations == 0)
                        for (int i = 0; i < st.Affiliations.Count; i++) if (st.Affiliations[i].Npc == n.Id && st.Affiliations[i].Open) Close(s, ev, n, i, 4);
                    Open(s, ev, n, to, 2);
                }
                m.ConvertDays = 0;
            }
            for (int i = 0; i < st.Affiliations.Count; i++)
            {
                var a = st.Affiliations[i];
                if (a.Npc != n.Id || !a.Open) continue;
                var rel = st.FindReligion(a.Religion);
                bool any = false;
                if (rel?.Gods != null) foreach (int g in rel.Gods) { int gi = m.IndexOfGod(g); if (gi >= 0 && m.Gods[gi].Belief >= dt.LapseBelief) any = true; }
                if (!any) Close(s, ev, n, i, 3);
            }
        }
    }
}
