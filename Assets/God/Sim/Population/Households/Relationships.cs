using System;
using System.Collections.Generic;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// The relationship ontology (owner D4 §2, M4.39). Six kinds, each its own record, any of which may exist without the others:
    /// <list type="bullet">
    /// <item>romantic partnership: <see cref="Partnership"/> (companionship; formed by proposal and acceptance, any sexes);</item>
    /// <item>reproductive relationship: <see cref="ReproductiveUnion"/> (both adults agreed to try for a child; the only route to a conception);</item>
    /// <item>biological parenthood: <c>NpcLife.Mother</c>, <c>NpcLife.BioFather</c> and the pedigree (truth; no mind reads it);</item>
    /// <item>caregiving: care acts (<c>ChildFed</c>, <c>CareGiven</c>), a child's main carer (<c>NpcLife.CueCarer</c>) and <see cref="CareArrangement"/>;</item>
    /// <item>household membership: <see cref="Household.Members"/>;</item>
    /// <item>economic partnership: <see cref="EconomicPartnership"/> (a stub type; M5 and M6 own it).</item>
    /// </list>
    /// No code here ends a romantic partnership, moves anyone between households or reads a partnership's sexes; whether two
    /// people can conceive is asked of <see cref="Reproduction.MayConceiveWith"/> only (with the phenotype builder, the one place that reads sex).
    /// </summary>
    public static class ReproductiveUnions
    {
        public static int Count(GodState s, int npc)
        {
            var hs = s.Households;
            if (hs == null) return 0;
            int k = 0;
            foreach (var u in hs.Unions) if (u.A == npc || u.B == npc) k++;
            return k;
        }

        public static ReproductiveUnion Between(GodState s, int a, int b)
        {
            var hs = s.Households;
            if (hs == null) return null;
            foreach (var u in hs.Unions) if (u.A == a && u.B == b || u.A == b && u.B == a) return u;
            return null;
        }

        public static int Total(GodState s) => s.Households?.Unions.Count ?? 0;

        /// <summary>Unions whose two adults are not romantic partners (kind 2, or partners who separated since).</summary>
        public static int OutsidePartnership(GodState s)
        {
            int k = 0;
            if (s.Households == null) return 0;
            foreach (var u in s.Households.Unions) if (Partnerships.Between(s, u.A, u.B) == null) k++;
            return k;
        }

        /// <summary>Whether this adult may take another reproductive relationship now (under the cap).</summary>
        public static bool HasRoom(GodState s, Npc n) => Count(s, n.Id) < Math.Max(1, s.Tuning.Households.Partner.MaxLiveUnions);

        public static ReproductiveUnion Form(GodState s, EventBuffer ev, Npc a, Npc b, int initiator, byte kind)
        {
            var hs = s.Households;
            var u = new ReproductiveUnion { Id = hs.NextUnionId++, A = Math.Min(a.Id, b.Id), B = Math.Max(a.Id, b.Id), StartTick = s.Tick, LastTryTick = s.Tick, Initiator = initiator, Kind = kind };
            hs.Unions.Add(u);
            hs.Stats.Unions++;
            if (a.Mind?.Kin != null) a.Mind.Kin.UnionWith = b.Id;
            if (b.Mind?.Kin != null) b.Mind.Kin.UnionWith = a.Id;
            ev?.Add(new ReproductiveUnionFormed { Union = u.Id, A = u.A, B = u.B, Initiator = initiator, Kind = kind, Partners = Partnerships.Between(s, a.Id, b.Id) != null });
            return u;
        }

        public static void End(GodState s, EventBuffer ev, ReproductiveUnion u, byte reason)
        {
            var hs = s.Households;
            hs.Unions.Remove(u);
            hs.Stats.UnionsEnded++;
            Clear(s, u.A, u.B); Clear(s, u.B, u.A);
            ev?.Add(new ReproductiveUnionEnded { Union = u.Id, A = u.A, B = u.B, Reason = reason, Children = u.Children, Days = (int)((s.Tick - u.StartTick) / Math.Max(1, SimCalendar.Of(s).TicksPerDay)) });
        }

        private static void Clear(GodState s, int who, int other)
        {
            var n = s.Population.Find(who);
            if (n?.Mind?.Kin != null && n.Mind.Kin.UnionWith == other) n.Mind.Kin.UnionWith = 0;
        }

        /// <summary>Once a day: relationships neither adult has tried in for the lapse time end (the people simply stopped choosing it).</summary>
        public static void Daily(GodState s, EventBuffer ev)
        {
            var hs = s.Households;
            if (hs.Unions.Count == 0) return;
            long lapse = SimCalendar.Of(s).TicksForAgeMilli(s.Tuning.Households.Partner.UnionLapseMilli);
            foreach (var u in hs.Unions.ToArray())
            {
                var a = s.Population.Find(u.A); var b = s.Population.Find(u.B);
                if (a?.Life != null && a.Life.TryWith == u.B && a.Life.TryingUntil >= s.Tick) u.LastTryTick = Math.Max(u.LastTryTick, s.Tick);
                if (b?.Life != null && b.Life.TryWith == u.A && b.Life.TryingUntil >= s.Tick) u.LastTryTick = Math.Max(u.LastTryTick, s.Tick);
                bool pregnantHere = a?.Life != null && a.Life.PregFather == u.B || b?.Life != null && b.Life.PregFather == u.A;
                if (!pregnantHere && s.Tick - u.LastTryTick > lapse) End(s, ev, u, ReproductiveUnionEnded.Lapsed);
            }
        }

        /// <summary>At a death: its reproductive relationships end.</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            foreach (var u in s.Households.Unions.ToArray()) if (u.A == npc.Id || u.B == npc.Id) End(s, ev, u, ReproductiveUnionEnded.Death);
        }
    }

    /// <summary>
    /// D4 §6 care-arrangement experiments (M4.44): which adults a begging child's display reaches, by what the adult believes
    /// the child is to it (<c>Care.FeedFrom</c>, a scenario setting; all by default). A controlled scenario switches whole
    /// forms of care off; nothing here decides whether an adult feeds: that stays its own choice.
    /// </summary>
    public static class CareStructure
    {
        public const int Parents = 1, Kin = 2, Household = 4, Communal = 8, Adoptive = 16, All = 31;

        /// <summary>The form of care an adult would give this child: its own child, adopted or step child, other believed kin, a household mate, anyone else.</summary>
        public static int FormOf(Npc adult, Npc child)
        {
            var role = KinData.RoleOf(adult.Mind, child.Id);
            switch (role)
            {
                case KinRole.Daughter: case KinRole.Son: return Parents;
                case KinRole.FosterChild: case KinRole.StepChild: return Adoptive;
            }
            if (KinData.RHat(adult.Mind, child.Id) > 0) return Kin;
            if (adult.Household != 0 && adult.Household == child.Household) return Household;
            return Communal;
        }

        public static bool MayAnswer(GodState s, Npc adult, Npc child)
        {
            int allowed = s.Tuning.Households.Care.FeedFrom;
            return allowed == All || (FormOf(adult, child) & allowed) != 0;
        }
    }

    /// <summary>
    /// Individual preferences (owner D4 §2, M4.40): drawn once at a person's origin from <c>Rng.Fork("prefs", id)</c>, uniform in
    /// mean +- spread (tuning), clamped to 0..1000. The decider sees them as instinct gains (2 x pref / 1000: 500 is neutral):
    /// companionship on proposing and accepting a partner, child desire on trying for a child, showing that it wants one and
    /// accepting such an offer. Commitment from <see cref="ExclusiveFrom"/> means "reproduction only inside my partnership".
    /// </summary>
    public static class Preferences
    {
        /// <summary>A partnered adult with commitment at or above this keeps reproduction inside its partnership (and its partners respect it).</summary>
        public const int ExclusiveFrom = 500;
        /// <summary>Indices in <c>Npc.Traits</c> after the four genome gains.</summary>
        public const int TraitCompanionship = Trait.Count + 4, TraitChildDesire = Trait.Count + 5, TraitCommitment = Trait.Count + 6;
        public const int TraitLength = Trait.Count + 7;

        public static void Draw(GodState s, Npc npc, NpcLife life)
        {
            var pt = s.Tuning.Households.Prefs;
            var r = s.Rng.Fork("prefs", npc.Id);
            life.Companionship = One(r, pt.CompanionshipMean, pt.CompanionshipSpread);
            life.ChildDesire = One(r, pt.ChildDesireMean, pt.ChildDesireSpread);
            life.Commitment = One(r, pt.CommitmentMean, pt.CommitmentSpread);
        }

        private static int One(Rng r, int mean, int spread)
        {
            int v = spread > 0 ? mean - spread + r.NextInt(2 * spread + 1) : mean;
            return Math.Max(0, Math.Min(1000, v));
        }

        /// <summary>
        /// Whether its commitments allow a reproductive relationship outside its partnerships: unpartnered, or partnered with its own
        /// commitment and every partner's below <see cref="ExclusiveFrom"/>. Asked by the actor on arrival (A4); nothing forces a choice.
        /// </summary>
        public static bool AllowsOutside(GodState s, Npc n, int other)
        {
            if (n.Life == null) return false;
            var ps = Partnerships.PartnersOf(s, n.Id);
            foreach (int p in ps)
            {
                if (p == other) continue;
                if (n.Life.Commitment >= ExclusiveFrom) return false;
                var o = s.Population.Find(p);
                if (o?.Life != null && o.Life.Commitment >= ExclusiveFrom) return false;
            }
            return true;
        }
    }
}
