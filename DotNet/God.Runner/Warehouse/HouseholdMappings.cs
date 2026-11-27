using System.Collections.Generic;
using God.Sim;
using God.Sim.Estate;
using God.Sim.Population;
using God.Sim.Population.Households;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// M4's warehouse mappers (OWNER: M4; migration <c>0005_households.sql</c>, tables in <c>Tables.Households.cs</c>). Births,
    /// arrivals, the person dimension's genetic and bodily columns, households and membership intervals (persistent person and
    /// household ids), care acts (and their daily aggregate in <c>fact_social_day</c>), estate transfers (asset id = lot id),
    /// the household day and M4's person-day columns. Every row is one event, so each table reconciles exactly with the
    /// stream (M4.35); high-volume care rows are sampled below Full, their aggregate never is.
    /// </summary>
    public static class HouseholdMappings
    {
        /// <summary>The grain of each M4 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_birth", "one row per birth", "NpcBorn"),
            ("fact_migration", "one row per arrival (direction 1) or departure (-1)", "NpcImmigrated, NpcEmigrated"),
            ("dim_household", "one row per household ever formed (dissolution filled once)", "HouseholdFormed, HouseholdDissolved"),
            ("fact_household_membership", "one row per membership interval of one person in one household (to_tick null while open)", "HouseholdJoined, HouseholdLeft"),
            ("fact_care", "one row per care act: food handed to a child (ChildFed) or care given to a patient (CareGiven); sampled below Full", "ChildFed, CareGiven"),
            ("fact_inheritance", "one row per estate transfer of one lot (asset id = lot id)", "EstateTransfer"),
            ("fact_household_day", "one row per household x sim day", "HouseholdDay"),
        };

        /// <summary>The open membership intervals of one run (the recorder makes a registry per run, so this lives with it).</summary>
        private sealed class State
        {
            public long NextMembership = 1;
            public readonly Dictionary<long, long> Open = new Dictionary<long, long>();
            public static long Key(int npc, int household) => ((long)npc << 32) | (uint)household;
        }

        /// <summary>The genome hash as a 31-bit warehouse integer (sums over the column stay inside SQLite's range).</summary>
        private static long Hash31(long h) => (long)((ulong)h >> 33);

        public static void Register(EventMapRegistry r)
        {
            var st = new State();
            r.Register<NpcGenome>("dim_person", "person_genome", (c, e) =>
            {
                var p = c.Person(e.Npc);
                c.Insert(Tables.DimPersonM4, p.Key, (long)e.Sex, (long)e.Generation, Hash31(e.GenomeHash), (long)e.MaxAgeMilli, (long)e.Strength, (long)e.Size,
                    (long)e.Health, (long)e.Fertility, (long)e.Lifespan, (long)e.Features, (long)e.Complexion, (long)e.Build, (long)e.NurtureGain, (long)e.KinWeight);
            });
            r.Register<NpcBorn>("fact_birth", "birth", (c, e) =>
            {
                var p = c.Person(e.Npc);
                long date = c.DateKey();
                p.FactionKey = c.FactionKey((int)e.Faction); p.SpawnTick = e.Tick; p.SpawnDate = date; p.SpawnLocation = c.NodeLocation(e.Node); p.Dirty = true;
                object Key(int id) => id > 0 ? (object)c.Person(id).Key : null;
                c.Insert(Tables.DimPersonBirth, p.Key, e.Tick, date, c.Calendar.Year(e.Tick), Key(e.Mother), Key(e.BelievedFather), Key(e.BioFather),
                    p.FactionKey, c.NodeLocation(e.Node), (long)e.Household);
                c.Insert(Tables.FactBirth, c.Tick, c.Seq, date, p.Key, Key(e.Mother), Key(e.BelievedFather), Key(e.BioFather), (long)e.Household,
                    p.FactionKey, c.NodeLocation(e.Node), (long)e.Sex, (long)e.Generation, Hash31(e.GenomeHash));
            });
            r.Register<NpcImmigrated>("fact_migration", "arrival", (c, e) =>
            {
                var p = c.Person(e.Npc);
                long date = c.DateKey();
                p.FactionKey = c.FactionKey((int)e.Faction); p.SpawnTick = e.Tick; p.SpawnDate = date; p.SpawnLocation = c.NodeLocation(e.Node); p.Dirty = true;
                c.Insert(Tables.DimPersonArrival, p.Key, p.FactionKey);
                c.Insert(Tables.FactMigration, c.Tick, c.Seq, date, p.Key, p.FactionKey, 1L, c.NodeLocation(e.Node), (long)e.AgeMilli);
            });
            r.Register<NpcEmigrated>("fact_migration", "departure", (c, e) =>
            {
                var p = c.Person(e.Npc);
                c.Insert(Tables.FactMigration, c.Tick, c.Seq, c.DateKey(), p.Key, c.FactionKey((int)e.Faction), -1L, c.NodeLocation(e.Node), null);
            });
            r.Register<HouseholdFormed>("dim_household", "household", (c, e) =>
            {
                object founder = e.Founder > 0 ? (object)c.Person(e.Founder).Key : null;
                c.Insert(Tables.DimHousehold, (long)e.Household, c.FactionKey((int)e.Faction), c.Tick, c.DateKey(), founder, (long)e.Reason, (long)e.Hut);
            });
            r.Register<HouseholdDissolved>("dim_household", "household", (c, e) =>
                c.Insert(Tables.DimHouseholdDissolved, (long)e.Household, c.Tick, c.DateKey(), (long)e.LotsDropped));
            r.Register<HouseholdJoined>("fact_household_membership", "membership", (c, e) =>
            {
                var p = c.Person(e.Npc);
                long key = st.NextMembership++;
                st.Open[State.Key(e.Npc, e.Household)] = key;
                c.Insert(Tables.FactMembershipOpen, key, p.Key, (long)e.Household, c.Tick, c.DateKey(), (long)e.Reason);
            });
            r.Register<HouseholdLeft>("fact_household_membership", "membership", (c, e) =>
            {
                long k = State.Key(e.Npc, e.Household);
                if (!st.Open.TryGetValue(k, out long key)) return;
                st.Open.Remove(k);
                c.Insert(Tables.FactMembershipClose, key, c.Tick, c.DateKey(), (long)e.Reason);
            });
            r.Register<ChildFed>("fact_care", "care", (c, e) =>
            {
                var carer = c.Person(e.Feeder);
                c.Aggregate(Tables.FactSocialDay, new object[] { c.DateKey(), carer.FactionKey ?? 0L, 134L, (long)(int)e.Band }, null, 1, e.Count);
                if (!c.RowWanted) return;
                c.Insert(Tables.FactCare, c.Tick, c.Seq, c.DateKey(), 134L, carer.Key, c.Person(e.Child).Key, carer.FactionKey, null, (long)(int)e.Ware, (long)e.Count,
                    (long)e.Lot, (long)(int)e.Band, c.Full ? (object)(long)e.TrueR : null, (long)e.Arrangement, (long)c.Weight);
            });
            r.Register<CareGiven>("fact_care", "care", (c, e) =>
            {
                var carer = c.Person(e.Carer);
                c.Aggregate(Tables.FactSocialDay, new object[] { c.DateKey(), carer.FactionKey ?? 0L, 136L, (long)(int)e.Band }, null, 1, e.Ticks);
                if (!c.RowWanted) return;
                c.Insert(Tables.FactCare, c.Tick, c.Seq, c.DateKey(), 136L, carer.Key, c.Person(e.Patient).Key, carer.FactionKey, (long)e.Ticks, null, null,
                    null, (long)(int)e.Band, c.Full ? (object)(long)e.TrueR : null, null, (long)c.Weight);
            });
            r.Register<EstateTransfer>("fact_inheritance", "estate", (c, e) =>
            {
                object exec = e.Executor > 0 ? (object)c.Person(e.Executor).Key : null;
                c.Insert(Tables.FactInheritance, c.Tick, c.Seq, c.DateKey(), c.Person(e.Estate).Key, (long)e.AssetKind, (long)e.AssetId, (long)(int)e.Ware, (long)e.Count,
                    c.Party(e.ToKind, e.ToId), (long)e.Reason, exec);
            });
            r.Register<HouseholdDay>("fact_household_day", "household_day", (c, e) =>
                c.Insert(Tables.FactHouseholdDay, c.DateKeyOfDay(e.Day), (long)e.Household, c.FactionKey((int)e.Faction), (long)e.Hut, (long)e.Members, (long)e.Children,
                    (long)e.Elders, (long)e.StoreFood, (long)e.StoreUnits));
            r.Register<PersonDayLife>("fact_person_day", "person_day_m4", (c, e) =>
            {
                var p = c.Person(e.Npc);
                object partner = e.Partner > 0 ? (object)c.Person(e.Partner).Key : null;
                c.Insert(Tables.FactPersonDayM4, p.Key, c.DateKeyOfDay(e.Day), (long)(int)e.Stage, e.Household > 0 ? (object)(long)e.Household : null, (long)e.AgeMilli, partner,
                    (long)e.CareGivenTicks, (long)e.CareReceivedTicks, e.Pregnant ? 1L : 0L, e.Ill ? 1L : 0L, (long)e.Growth);
            });
        }
    }
}
