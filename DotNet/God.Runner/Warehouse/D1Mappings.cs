using System.Collections.Generic;
using God.Sim.Divinity;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// D1's warehouse mappers (OWNER: D1), called by F1's <c>DivinityMappings.Register</c>. GodId is the god key (20.F1.11).
    /// <c>fact_mythic_ledger</c> gets one row per Mythic movement, including the opening balance from <c>GodCreated</c>, so
    /// <c>SUM(amount)</c> per god equals its balance (M1). Upkeep rows (HV events, but one per god, kind and day) are always
    /// written, never sampled, so the ledger stays exact at every level.
    /// </summary>
    public static class D1Mappings
    {
        /// <summary>The grain of each D1 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("dim_god", "one row per god (GodId 1..FactionCount), created once", "GodCreated"),
            ("dim_god_history", "one row per interval of a god's seat, policy and Mythic mode (to_tick null while open)", "GodCreated, GodPolicyChanged, MythicConfigChanged"),
            ("fact_god_faction", "one row per spell of a god's association with a faction (to_tick null while open)", "GodAssociated"),
            ("fact_mythic_ledger", "one row per movement of a god's Mythic: opening, credit, spend, one day's upkeep of one spell kind", "GodCreated, MythicCredited, MythicSpent, MythicUpkeepCharged"),
            ("fact_divine_action", "one row per accepted divine action (InterventionId)", "DivineActionAccepted"),
            ("fact_divine_observation", "one row per non-omniscient observation answer (empty in this run)", "DivineObservationAnswered"),
            ("fact_mythic_balance_day", "one row per god x finished sim day", "MythicBalanceDay"),
        };

        /// <summary>Per run: each god's open history row with its current seat and policy, and the game's mode.</summary>
        private sealed class State
        {
            public long NextHist = 1;
            public readonly SortedDictionary<int, (long key, GodSeat seat, string policy)> Open = new SortedDictionary<int, (long, GodSeat, string)>();
            public DivinityMode Mode;
        }

        private static void OpenRow(MapContext c, State st, int god, GodSeat seat, string policy, string reason)
        {
            if (st.Open.TryGetValue(god, out var cur)) c.Insert(Tables.DimGodHistoryClose, cur.key, c.Tick);
            long key = st.NextHist++;
            st.Open[god] = (key, seat, policy);
            c.Insert(Tables.DimGodHistory, key, c.God(god), c.Tick, null, (long)seat, policy, (long)st.Mode, reason);
        }

        public static void Register(EventMapRegistry r)
        {
            var st = new State();
            r.Register<GodCreated>("dim_god", "god", (c, e) =>
            {
                st.Mode = e.Mode;
                c.Insert(Tables.DimGod, c.God(e.God), (long)e.God, e.Name, c.FactionKey((int)e.OriginFaction), c.Tick, c.DateKey());
                OpenRow(c, st, e.God, e.Seat, e.Policy, "created");
                c.Insert(Tables.FactMythicLedger, c.Tick, c.Seq, c.DateKey(), c.God(e.God), 800L, null, null, e.Mythic, null, 0L, null, e.Mythic, null, null);
            });
            r.Register<GodAssociated>("fact_god_faction", "god_faction", (c, e) =>
                c.Insert(Tables.FactGodFaction, c.God(e.God), c.FactionKey((int)e.Faction), c.Tick, null, 1L));
            r.Register<GodPolicyChanged>("dim_god_history", "god_history", (c, e) => OpenRow(c, st, e.God, e.Seat, e.Policy, "config"));
            r.Register<MythicConfigChanged>("dim_god_history", "god_history", (c, e) =>
            {
                st.Mode = e.Mode;
                var gods = new List<int>(st.Open.Keys);
                foreach (int g in gods) { var cur = st.Open[g]; OpenRow(c, st, g, cur.seat, cur.policy, "mythic_mode"); }
            });
            r.Register<MythicCredited>("fact_mythic_ledger", "mythic", (c, e) =>
                c.Insert(Tables.FactMythicLedger, c.Tick, c.Seq, c.DateKey(), c.God(e.God), 804L, null, null, e.Amount, null, 0L, e.Capped, e.Balance, null, (long)e.Source));
            r.Register<MythicSpent>("fact_mythic_ledger", "mythic", (c, e) =>
                c.Insert(Tables.FactMythicLedger, c.Tick, c.Seq, c.DateKey(), c.God(e.God), 805L, (long)e.Kind, e.Intervention > 0 ? (object)(long)e.Intervention : null,
                    -e.Amount, e.Nominal, e.Waived ? 1L : 0L, null, e.Balance, null, null));
            r.Register<MythicUpkeepCharged>("fact_mythic_ledger", "mythic", (c, e) =>
                c.Insert(Tables.FactMythicLedger, c.Tick, c.Seq, c.DateKey(), c.God(e.God), 806L, (long)e.Kind, null, -e.Amount, e.Amount + e.Waived,
                    e.Waived > 0 ? 1L : 0L, null, null, (long)e.Ticks, null));
            r.Register<DivineActionAccepted>("fact_divine_action", "divine_action", (c, e) =>
                c.Insert(Tables.FactDivineAction, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), (long)e.Kind, (long)e.Act, (long)e.TargetKind,
                    (long)e.TargetId, (long)e.TargetNode, (long)e.TargetRadius, (long)e.Scope, e.Cost, e.Charged));
            r.Register<DivineObservationAnswered>("fact_divine_observation", "divine_observation", (c, e) =>
                c.Insert(Tables.FactDivineObservation, c.Tick, c.Seq, c.DateKey(), c.God(e.God), (long)e.Domain, (long)e.Channel, (long)e.Subject));
            r.Register<MythicBalanceDay>("fact_mythic_balance_day", "mythic_day", (c, e) =>
                c.Insert(Tables.FactMythicBalanceDay, c.DateKeyOfDay(e.Day), c.God(e.God), e.Balance, e.Max, e.Credited, e.Capped, e.Spent, e.Waived, e.Upkeep,
                    (long)e.Actions, (long)e.Mode));
        }
    }
}
