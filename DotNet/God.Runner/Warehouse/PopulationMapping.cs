using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Runner.Warehouse
{
    /// <summary>One in-memory row of <c>dim_person</c> (type 1); written as an upsert on the day it changed.</summary>
    public sealed class PersonDim
    {
        public long Key; public int NpcId;
        public long? FactionKey, SpawnLocation;
        public bool IsFounder;
        public long? SpawnTick, SpawnDate, DeathTick, DeathDate, AgeAtDeath; public int? DeathCause;
        public bool Dirty;
    }

    /// <summary>
    /// The M3 population mappers (NpcSpawned, NpcDied, NpcDay, Produced, Consumed, the four learning events and M3b's DecisionMade), registered
    /// the same way as the core ones. Tables and grains: see <c>Migrations/0002_population.sql</c>, <c>0003_decision.sql</c> and <see cref="Grains"/>.
    /// </summary>
    public static class PopulationMapping
    {
        /// <summary>Learning event kinds of <c>fact_learning.kind_code</c> (dim_code 'learning_kind').</summary>
        public const int LearnUpdated = 1, LearnRecruited = 2, LearnFreed = 3, LearnEpisode = 4;

        /// <summary>The grain of each M3 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("dim_person", "one row per person ever alive in the run (type 1; death fields filled once)", "NpcSpawned, NpcDied"),
            ("fact_death", "one row per death", "NpcDied"),
            ("fact_person_day", "one row per living person x sim day (the day that just ended; every person alive at the day start)", "NpcDay"),
            ("fact_production", "one row per WaresProduced (stage 3, building), Produced or Consumed (personal goods, person) event; direction 1 in, -1 out", "WaresProduced, Produced, Consumed"),
            ("fact_learning", "one row per learning event; kind_code says which (association updates are sampled below Full)", "AssociationUpdated, ConjunctionRecruited, ConjunctionFreed, EpisodeClosed"),
            ("fact_decision", "one row per chosen intent of one person at one think tick (written only when the learning domain records at Full)", "DecisionMade"),
            ("dim_party", "one row per economic party seen in the run (person, household, faction or estate); exactly one reference set", "OwnershipChanged"),
            ("fact_ownership", "one row per OwnershipChanged: one ownership change of one lot (spells are derived from it)", "OwnershipChanged"),
            ("fact_social_day", "one row per day x faction x kind x believed kin band (an aggregate of help-like acts; M4 and M5 fill it)", "ChildFed, CareGiven, GiftGiven, HelpGiven, LabourHelped (M4, M5)"),
        };

        public static void Register(EventMapRegistry r)
        {
            r.Register<NpcSpawned>("dim_person", "person", (c, e) =>
            {
                var p = c.Person(e.Npc);
                p.IsFounder = e.Founder; p.SpawnTick = e.Tick; p.SpawnDate = c.DateKey(); p.SpawnLocation = c.NodeLocation(e.Node);
                p.FactionKey = c.FactionKey((int)e.Faction); p.Dirty = true;
            });
            r.Register<NpcDied>("fact_death", "death", (c, e) =>
            {
                var p = c.Person(e.Npc);
                long date = c.DateKey();
                p.DeathTick = e.Tick; p.DeathDate = date; p.DeathCause = (int)e.Cause; p.AgeAtDeath = e.AgeTicks;
                p.FactionKey = c.FactionKey((int)e.Faction) ?? p.FactionKey; p.Dirty = true;
                if (c.RowWanted)
                    c.Insert(Tables.FactDeath, c.Tick, c.Seq, date, p.Key, p.FactionKey, (long)(int)e.Cause, c.NodeLocation(e.Node), e.AgeTicks);
            });
            r.Register<NpcDay>("fact_person_day", "person_day", (c, e) =>
            {
                var p = c.Person(e.Npc);
                if (p.FactionKey == null) { p.FactionKey = c.FactionKey((int)e.Faction); p.Dirty = true; }
                if (!c.RowWanted) return;
                long T(int i) => e.Ticks != null && i < e.Ticks.Length ? e.Ticks[i] : 0L;
                c.Insert(Tables.FactPersonDay, c.DateKeyOfDay(e.Day), p.Key, c.FactionKey((int)e.Faction), c.Tick, (long)e.AgeDays, c.NodeLocation(e.Node), (long)e.Health, (long)e.Nutrition,
                    (long)e.Hunger, (long)e.Fatigue, (long)e.Cold, (long)e.Fear, (long)e.Pain,
                    (long)e.MeanHunger, (long)e.MeanFatigue, (long)e.MeanCold, (long)e.MeanFear, (long)e.MeanPain,
                    T(0), T(1), T(2), T(3), T(4), T(5), T(6), T(7), T(8), T(9), T(10),
                    (long)e.WalkTicks, (long)e.Meals, e.HasBed ? 1L : 0L, (long)e.CarryUnits, (long)e.Decisions, (long)e.LearnedShare, (long)e.ExploreShare,
                    (long)e.RewardSum, (long)e.PunishSum, (long)e.Associations, (long)e.Conjunctions, (long)(int)e.Mode, T(11), T(12));
            });
            r.Register<Produced>("fact_production", "person_goods", (c, e) =>
            {
                if (!c.RowWanted) return;
                var p = c.Person(e.Npc);
                c.Insert(Tables.FactProductionPerson, c.Tick, c.Seq, c.DateKey(), p.FactionKey, (long)(int)e.Ware, (long)e.Count, (long)c.Weight,
                    p.Key, 1L, (long)(int)e.Reason, (long)e.Source, null, e.WithTool ? 1L : 0L, (long)e.Lot, (long)e.IntoLot, (long)(int)e.OwnerKind, (long)e.OwnerId);
            });
            r.Register<Consumed>("fact_production", "person_goods", (c, e) =>
            {
                if (!c.RowWanted) return;
                var p = c.Person(e.Npc);
                c.Insert(Tables.FactProductionPerson, c.Tick, c.Seq, c.DateKey(), p.FactionKey, (long)(int)e.Ware, (long)e.Count, (long)c.Weight,
                    p.Key, -1L, (long)(int)e.Reason, null, (long)e.Hut, null, (long)e.Lot, null, (long)(int)e.OwnerKind, (long)e.OwnerId);
            });
            r.Register<AssociationUpdated>("fact_learning", "learning", (c, e) => Learn(c, e.Npc, LearnUpdated, (long)e.Verb, (long)e.Key, (long)e.Delta, (long)e.NewStw, (long)e.NewLtw, (long)(int)e.Origin,
                null, null, null, null, null, null, null));
            r.Register<ConjunctionRecruited>("fact_learning", "learning", (c, e) => Learn(c, e.Npc, LearnRecruited, null, null, null, null, null, null,
                (long)e.Slot, (long)e.A, (long)e.B, (long)e.C, (long)e.Strength, null, null));
            r.Register<ConjunctionFreed>("fact_learning", "learning", (c, e) => Learn(c, e.Npc, LearnFreed, null, null, null, null, null, null,
                (long)e.Slot, (long)e.A, (long)e.B, (long)e.C, null, null, null));
            r.Register<EpisodeClosed>("fact_learning", "learning", (c, e) => Learn(c, e.Npc, LearnEpisode, (long)e.Verb, (long)e.EpisodeKey, null, null, null, null,
                null, null, null, null, null, (long)e.Return, (long)e.DaysElapsed));
            // F0.14: ownership changes (one row each; the parties enter dim_party on first sight)
            r.Register<OwnershipChanged>("fact_ownership", "ownership", (c, e) =>
            {
                long? from = c.Party(e.FromKind, e.FromId), to = c.Party(e.ToKind, e.ToId);
                if (!c.RowWanted) return;
                c.Insert(Tables.FactOwnership, c.Tick, c.Seq, c.DateKey(), (long)e.AssetKind, (long)e.Lot, (long)(int)e.Ware, (long)e.Count, from, to, (long)(int)e.Basis,
                    e.Actor > 0 ? (object)c.Person(e.Actor).Key : null, (long)c.Weight);
            });
            // M3b's decision trace: Full only (the sim emits it only then; the level gate here keeps a Summary run of a hand-fed stream empty too)
            r.Register<DecisionMade>("fact_decision", "decision", (c, e) =>
            {
                if (!c.Full || !c.RowWanted) return;
                var p = c.Person(e.Npc);
                c.Insert(Tables.FactDecision, c.Tick, c.Seq, c.DateKey(), p.Key, p.FactionKey, (long)e.Verb, (long)(int)e.TargetKind, (long)e.TargetId,
                    e.TargetNode >= 0 ? (object)c.NodeLocation(e.TargetNode) : null, (long)e.TargetClass, (long)e.Total, (long)e.Instinct, (long)e.Need, (long)e.Learned,
                    (long)e.Social, (long)e.Culture, (long)e.Personality, (long)e.Cost, (long)e.Explore, (long)e.Candidates,
                    e.LearnedChangedWinner ? 1L : 0L, e.ExploreWon ? 1L : 0L, e.Continued ? 1L : 0L, (long)e.SecondVerb, (long)e.SecondTotal, (long)c.Weight);
            });
        }

        private static void Learn(MapContext c, int npc, int kind, object verb, object feature, object delta, object stw, object ltw, object origin,
            object slot, object a, object b, object cc, object strength, object ret, object days)
        {
            if (!c.RowWanted) return;
            var p = c.Person(npc);
            c.Insert(Tables.FactLearning, c.Tick, c.Seq, c.DateKey(), p.Key, p.FactionKey, (long)kind, verb, feature, delta, stw, ltw, origin, slot, a, b, cc, strength, ret, days, (long)c.Weight);
        }
    }
}
