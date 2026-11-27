using System.Collections.Generic;
using God.Sim.Orgs;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// C6's warehouse mappers (OWNER: C6; migration <c>0018_organisations.sql</c>, tables in <c>Tables.C6.cs</c>): the
    /// organisation dimension and its status history, membership and office spells (half-open, closed at the event that ends
    /// them), spending, the daily snapshot, service requests (accumulating), deliveries, recorded outcomes and decisions.
    /// Analytics-only truth fields (TrueNeed, TrueOutcome) are written at Full only. Every row is written whatever the
    /// sampling (each is a reconciled fact, ORG-ACC-17 / SVC-ACC-17).
    /// </summary>
    public static class C6Mappings
    {
        /// <summary>The grain of each C6 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("dim_org", "one row per organisation (org_key = OrgId)", "OrgFounded, OrgDissolved"),
            ("dim_org_history", "one row per status spell of one organisation", "OrgFounded, OrgStatusChanged"),
            ("fact_org_membership", "one row per spell of one role or one office of one person", "MemberJoined, MemberLeft, OrgHeadChanged"),
            ("fact_org_expenditure", "one row per spend of the organisation's goods", "OrgSpent"),
            ("fact_org_day", "one row per organisation x day", "OrgDay"),
            ("fact_org_day_store", "one row per organisation x day x ware in its stores", "OrgDay"),
            ("fact_service_request", "one row per request, updated through its steps", "ServiceRequested, ServiceAccepted, ServiceDelivered, ServiceRefused, ServiceExpired, ServiceOutcomeRecorded"),
            ("fact_service_delivery", "one row per delivered service", "ServiceDelivered"),
            ("fact_service_outcome", "one row per outcome written by a recorder", "ServiceOutcomeRecorded"),
            ("fact_org_decision", "one row per organisation decision", "OrgDecisionTaken"),
        };

        public const long EndLeft = 1, EndDismissed = 2, EndDied = 3, EndDissolved = 4, EndOfficeChanged = 5, RoleOfficer = 4;

        private sealed class State
        {
            public long NextHist = 1, NextSpell = 1;
            public readonly Dictionary<int, long> OpenHist = new Dictionary<int, long>();
            public readonly Dictionary<(int org, int npc, int role), long> OpenRole = new Dictionary<(int, int, int), long>();
            public readonly Dictionary<(int org, int office), long> OpenOffice = new Dictionary<(int, int), long>();
        }

        public static void Register(EventMapRegistry r)
        {
            var st = new State();
            object P(MapContext c, int npc) => npc > 0 ? (object)c.Person(npc).Key : null;
            void History(MapContext c, int org, OrgStatus status)
            {
                if (st.OpenHist.TryGetValue(org, out long open)) c.Insert(Tables.DimOrgHistoryClose, open, c.Tick);
                long key = st.NextHist++;
                st.OpenHist[org] = key;
                c.Insert(Tables.DimOrgHistoryOpen, key, (long)org, (long)(int)status, c.Tick);
            }
            r.Register<OrgFounded>("dim_org", "org", (c, e) =>
            {
                c.Insert(Tables.DimOrg, (long)e.Org, (long)e.Template, (long)(int)e.Type, (long)(int)e.Ownership, null,
                    e.Place >= 0 ? (object)c.NodeLocation(e.Place) : null, (long)e.Store, (long)(e.Founders?.Length ?? 0), (long)e.OriginOpportunity, c.Tick, c.DateKey());
                History(c, e.Org, OrgStatus.Operating);
            });
            r.Register<OrgStatusChanged>("dim_org_history", "org_status", (c, e) => History(c, e.Org, e.To));
            r.Register<OrgDissolved>("dim_org", "org_dissolved", (c, e) => c.Insert(Tables.DimOrgDissolved, (long)e.Org, c.Tick, c.DateKey(), (long)e.Reason, (long)(int)e.Rule));
            r.Register<MemberJoined>("fact_org_membership", "org_member", (c, e) =>
            {
                long key = st.NextSpell++;
                st.OpenRole[(e.Org, e.Npc, (int)e.Role)] = key;
                c.Insert(Tables.FactOrgMembershipOpen, key, (long)e.Org, P(c, e.Npc), (long)(int)e.Role, null, null, (long)(int)e.Basis, c.Tick, c.DateKey());
            });
            r.Register<MemberLeft>("fact_org_membership", "org_member", (c, e) =>
            {
                var k = (e.Org, e.Npc, (int)e.Role);
                if (!st.OpenRole.TryGetValue(k, out long key)) return;
                st.OpenRole.Remove(k);
                c.Insert(Tables.FactOrgMembershipClose, key, c.Tick, c.DateKey(), (long)e.Reason);
            });
            r.Register<OrgHeadChanged>("fact_org_membership", "org_office", (c, e) =>
            {
                var k = (e.Org, e.Office);
                if (st.OpenOffice.TryGetValue(k, out long open)) { st.OpenOffice.Remove(k); c.Insert(Tables.FactOrgMembershipClose, open, c.Tick, c.DateKey(), EndOfficeChanged); }
                if (e.To == 0) return;
                long key = st.NextSpell++;
                st.OpenOffice[k] = key;
                c.Insert(Tables.FactOrgMembershipOpen, key, (long)e.Org, P(c, e.To), RoleOfficer, (long)e.Office, (long)(int)e.Kind, (long)(int)e.Filling, c.Tick, c.DateKey());
            });
            r.Register<OrgSpent>("fact_org_expenditure", "org_spent", (c, e) =>
                c.Insert(Tables.FactOrgExpenditure, c.Tick, c.Seq, c.DateKey(), (long)e.Org, P(c, e.Actor), (long)(int)e.Category,
                    e.Ware != God.Sim.Ware.None ? (object)(long)(int)e.Ware : null, (long)e.Count, P(c, e.Payee), e.Request != 0 ? (object)(long)e.Request : null, (long)e.Control));
            r.Register<OrgDay>("fact_org_day", "org_day", (c, e) =>
            {
                long units = 0;
                if (e.StoreCounts != null) foreach (int n in e.StoreCounts) units += n;
                c.Insert(Tables.FactOrgDay, (long)e.Org, (long)e.Day, c.DateKeyOfDay(e.Day), (long)(int)e.Status, (long)e.Members, (long)e.Staff, (long)e.OfficesVacant, units,
                    (long)e.RequestsOpen, (long)e.Served, (long)e.Refused, (long)e.Expired, (long)e.Deposits, (long)e.Records, (long)e.Decisions, c.Full ? (object)(long)e.TrueUnmet : null);
                if (e.StoreWares != null)
                    for (int i = 0; i < e.StoreWares.Length; i++) c.Insert(Tables.FactOrgDayStore, (long)e.Org, (long)e.Day, (long)e.StoreWares[i], (long)e.StoreCounts[i]);
            });
            r.Register<ServiceRequested>("fact_service_request", "service_request", (c, e) =>
                c.Insert(Tables.FactServiceRequest, (long)e.Request, e.Org > 0 ? (object)(long)e.Org : null, (long)(int)e.Kind, P(c, e.Requester), (long)(int)e.SubjectKind,
                    (long)e.Subject, e.Place >= 0 ? (object)c.NodeLocation(e.Place) : null, P(c, e.ReceivedBy), (long)e.Control, c.Full ? (object)(long)e.TrueNeed : null,
                    c.Tick, c.DateKey(), 1L));
            r.Register<ServiceAccepted>("fact_service_request", "service_request", (c, e) => c.Insert(Tables.FactServiceAccepted, (long)e.Request, c.Tick, P(c, e.ProviderNpc)));
            r.Register<ServiceRefused>("fact_service_request", "service_request", (c, e) => c.Insert(Tables.FactServiceRefused, (long)e.Request, c.Tick, (long)e.ReasonSeen));
            r.Register<ServiceExpired>("fact_service_request", "service_request", (c, e) => c.Insert(Tables.FactServiceExpired, (long)e.Request, c.Tick));
            r.Register<ServiceDelivered>("fact_service_delivery", "service_delivery", (c, e) =>
            {
                c.Insert(Tables.FactServiceDelivered, (long)e.Request, c.Tick, P(c, e.ProviderNpc));
                c.Insert(Tables.FactServiceDelivery, c.Tick, c.Seq, c.DateKey(), (long)e.Request, e.Org > 0 ? (object)(long)e.Org : null, P(c, e.ProviderNpc),
                    (long)e.Ticks, (long)e.GoodsUsed, (long)e.OutcomeEvent, (long)e.Control);
            });
            r.Register<ServiceOutcomeRecorded>("fact_service_outcome", "service_outcome", (c, e) =>
            {
                c.Insert(Tables.FactServiceRecorded, (long)e.Request, (long)e.RecordedOutcome);
                c.Insert(Tables.FactServiceOutcome, c.Tick, c.Seq, c.DateKey(), (long)e.Request, e.Org > 0 ? (object)(long)e.Org : null, P(c, e.Recorder),
                    (long)e.RecordedOutcome, c.Full ? (object)(long)e.TrueOutcome : null);
            });
            r.Register<OrgDecisionTaken>("fact_org_decision", "org_decision", (c, e) =>
                c.Insert(Tables.FactOrgDecision, c.Tick, c.Seq, c.DateKey(), (long)e.Org, (long)e.Decision, P(c, e.Proposer), (long)(int)e.Source, (long)e.SourceRef,
                    (long)(int)e.What, (long)e.A0, (long)e.A1, (long)(int)e.Outcome, (long)(int)e.Rule, (long)e.Accept, (long)e.Refuse, (long)e.Absent, P(c, e.Counter)));
        }
    }
}
