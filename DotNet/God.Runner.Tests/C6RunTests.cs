using System;
using System.Collections.Generic;
using System.Linq;
using God.Recording;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Orgs;
using God.Sim.Population;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// C6 at run level (docs/civdiv/DEVELOPMENT_PACKAGES/C6.md section 13): pin P5, scenario 4 (independent institution),
    /// the threshold-never-builds test, the service invariants over seeds, save/load parity and the warehouse reconciliation.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class C6RunTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("c6"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        /// <summary>A recorder that keeps the events and samples the organisation side of the minds each day (diagnostics only).</summary>
        internal sealed class OrgProbe : IRunRecorder
        {
            public readonly List<SimEvent> Events = new List<SimEvent>();
            public GodState State;
            public int MaxEvidence, Opps, Invites, Accepted, Meetings, Minds, WithSupporter, Ready;
            public List<int> Trusts = new List<int>();
            public void Begin(RunInfo info, GodSim sim) { }
            public void OnTick(GodSim sim, IReadOnlyList<SimEvent> events) { Events.AddRange(events); }
            public void OnDayEnd(GodSim sim, FactionDayRow[] rows)
            {
                var s = sim.State;
                if (s.Organisations == null) return;
                Meetings = Math.Max(Meetings, s.Organisations.Meetings.Count);
                foreach (var n in s.Population.Npcs)
                {
                    var m = n.Mind?.Orgs;
                    if (m == null) continue;
                    Minds++;
                    for (int i = 0; i < m.OppCount; i++)
                    {
                        var o = m.Opportunities[i];
                        Opps++; MaxEvidence = Math.Max(MaxEvidence, o.NeedEvidence); Accepted += o.AcceptedCount();
                        if (o.S0 != 0) WithSupporter++;
                        if (o.S0 != 0 && o.NeedEvidence >= s.Tuning.Orgs.C6.NeedThreshold) Ready++;
                    }
                    Invites += m.InviteCount;
                    var sm = n.Mind.Social;
                    if (sm != null && s.Population.Find(n.Id) != null)
                        for (int r = 0; r < sm.RelCount; r++) { var o = s.Population.Find(sm.Rel[r].Other); if (o != null && o.Household != n.Household) Trusts.Add(sm.Rel[r].Trust); }
                }
            }
            public void End(GodSim sim, IDictionary<string, long> counters) { State = sim.State; }
            public void Dispose() { }
        }

        internal static (RunResult res, OrgProbe probe) Run(string scenario, ulong seed, long ticks, params string[] sets)
        {
            var spec = TestUtil.Spec(scenario, seed, ticks, sets);
            spec.Scenario.Checks.Remove("smoke");
            var probe = new OrgProbe();
            spec.ExtraRecorders.Add(() => probe);
            var res = RunEngine.Run(spec);
            Assert.IsNull(res.Error, res.Error);
            Assert.AreEqual(0, res.ExitCode, string.Join("; ", res.Messages));
            return (res, probe);
        }

        // ---------------------------------------------------------------- P5

        /// <summary>
        /// P5 <c>Orgs_NoEmergence_Neutral</c> (contracts 16): <c>Orgs.Enabled = 1</c>, <c>Emergence = 0</c>, no seeds, on the pin
        /// scenarios, seeds 1-3, 10 days, every 600 ticks: every part except organisations (and core, which holds the tuning)
        /// equals the switches-off run, which equals P4's base digest.
        /// </summary>
        [Test]
        public void Orgs_NoEmergence_Neutral()
        {
            var specs = new List<RunSpec>(); var keys = new List<string>();
            foreach (var sc in F1NeutralityTests.Scenarios)
                for (ulong seed = 1; seed <= 3; seed++)
                {
                    keys.Add(sc + "/" + seed);
                    specs.Add(F1NeutralityTests.Spec(sc, seed));
                    var on = TestUtil.Spec(sc, seed, F1NeutralityTests.Ticks, "tuning.Divinity.Enabled=0", "tuning.Civ.Enabled=0", "tuning.Orgs.Enabled=1", "tuning.Orgs.Emergence=0");
                    on.Scenario.Checks.Remove("smoke");
                    on.CheckpointEvery = F1NeutralityTests.Every;
                    specs.Add(on);
                }
            var r = TestUtil.RunAll(specs);
            int org = Array.IndexOf(GodSim.AllHashPartNames.ToArray(), "organisations");
            for (int k = 0; k < keys.Count; k++)
            {
                var off = r[2 * k]; var on = r[2 * k + 1];
                Assert.AreEqual(0, off.ExitCode, keys[k] + ": " + off.Error); Assert.AreEqual(0, on.ExitCode, keys[k] + ": " + on.Error);
                Assert.AreEqual(F1NeutralityTests.Base[keys[k]], F1NeutralityTests.Digest(off), keys[k] + ": the off run is P4");
                bool population = off.FinalParts.Length > GodSim.HashPartNames.Count;
                Assert.AreEqual(off.FinalParts.Length + (population ? 1 : 0), on.FinalParts.Length, keys[k] + ": the organisations part is appended (only with a population)");
                for (int c = 0; c < off.Checkpoints.Count; c++)
                    for (int i = 0; i < off.Checkpoints[c].Parts.Length; i++)
                        if (GodSim.AllHashPartNames[i] != "core") Assert.AreEqual(off.Checkpoints[c].Parts[i], on.Checkpoints[c].Parts[i], keys[k] + " checkpoint " + c + " part " + GodSim.AllHashPartNames[i]);
            }
            Assert.Greater(org, 0);
        }

        // ---------------------------------------------------------------- ORG-INV-6, ORG-ACC-4, SVC-INV, ORG-INV-1/2

        private static List<(RunResult res, OrgProbe probe)> Seeds(string scenario, int seeds, long ticks)
        {
            var specs = new List<RunSpec>(); var probes = new List<OrgProbe>();
            for (ulong seed = 1; seed <= (ulong)seeds; seed++)
            {
                var spec = TestUtil.Spec(scenario, seed, ticks);
                spec.Scenario.Checks.Remove("smoke");
                var probe = new OrgProbe(); probes.Add(probe);
                spec.ExtraRecorders.Add(() => probe);
                specs.Add(spec);
            }
            var res = TestUtil.RunAll(specs);
            var l = new List<(RunResult, OrgProbe)>();
            for (int i = 0; i < res.Length; i++)
            {
                Assert.IsNull(res[i].Error, res[i].Error);
                Assert.AreEqual(0, res[i].ExitCode, scenario + " seed " + (i + 1) + ": " + string.Join("; ", res[i].Messages)); // includes the invariants check (C1-C6 conservation, ORG1-ORG6)
                l.Add((res[i], probes[i]));
            }
            return l;
        }

        /// <summary>ORG-INV-6: the template is available, nobody knows the practice: no OrgFounded over 30 days on 20 seeds (exact).</summary>
        [Test]
        public void ThresholdNeverBuilds_20Seeds()
        {
            var runs = Seeds("org_shared_store_nopractice", 20, 30 * 1200);
            foreach (var (_, p) in runs) Assert.IsEmpty(p.Events.OfType<OrgFounded>());
        }

        /// <summary>
        /// ORG-ACC-4 <c>Scenario4_IndependentInstitution</c> with the attribution and service invariants on the same runs:
        /// org_shared_store, 20 seeds x 20 days (reported, not tuned); with the practice absent none; with the need absent
        /// (equal food) fewer (Mann-Whitney, reported). On every run: every decision names its proposer as a living office-holder
        /// at that tick (or counted votes), every welfare grant and spend has the keeper's own hand-over within reach at that
        /// tick (ORG-INV-2, SVC-I1), and the run's invariants hold (ORG-INV-1).
        /// </summary>
        [Test]
        public void Scenario4_IndependentInstitution()
        {
            var main = Seeds("org_shared_store", 20, 20 * 1200);
            var none = Seeds("org_shared_store_nopractice", 20, 20 * 1200);
            var equal = Seeds("org_shared_store_equal", 20, 20 * 1200);
            int[] Count(List<(RunResult, OrgProbe p)> runs) => runs.Select(x => x.p.Events.OfType<OrgFounded>().Count()).ToArray();
            var m = Count(main); var n = Count(none); var e = Count(equal);
            for (int i = 0; i < main.Count; i++)
            {
                var ev = main[i].Item2.Events;
                TestContext.WriteLine("seed " + (i + 1) + ": founded " + m[i] + " (no practice " + n[i] + ", equal food " + e[i] + "), proposals " + ev.OfType<OrgOpportunityAvailable>().Count()
                    + ", deposits " + ev.OfType<ItemMoved>().Count(x => x.Reason == MoveReason.Deposit && x.OwnerAfterKind == OwnerKind.Org)
                    + ", requests " + ev.OfType<ServiceRequested>().Count() + ", rations " + ev.OfType<WelfareGranted>().Count() + " (" + ev.OfType<WelfareGranted>().Sum(x => x.Count) + " units)"
                    + ", refused " + ev.OfType<ServiceRefused>().Count() + ", expired " + ev.OfType<ServiceExpired>().Count() + ", decisions " + ev.OfType<OrgDecisionTaken>().Count()
                    + ", records " + ev.OfType<RecordWritten>().Count());
                Attribution(main[i].Item2.Events, "seed " + (i + 1));
            }
            int seedsWith = m.Count(x => x > 0);
            double p = MannWhitneyP(m, e);
            TestContext.WriteLine("ORG-ACC-4: seeds with a founding " + seedsWith + "/20 (criterion 10, reported not tuned); practice absent " + n.Count(x => x > 0) + "/20; equal food " + e.Count(x => x > 0)
                + "/20; Mann-Whitney (main vs equal) p = " + p.ToString("0.0000"));
            Assert.AreEqual(0, n.Sum(), "with the practice absent nothing is founded");
            if (seedsWith < 10) Assert.Warn("ORG-ACC-4: only " + seedsWith + "/20 seeds founded a shared store within 20 days (the conditions were not loosened, D5.33)");
        }

        /// <summary>ORG-INV-2 and SVC-I1 from events: deciders were office-holders (as the events show), spends and grants were hand-overs by the keeper within reach.</summary>
        internal static void Attribution(List<SimEvent> ev, string what)
        {
            var holders = new Dictionary<(int org, int office), int>();
            var died = new HashSet<int>(ev.OfType<NpcDied>().Select(d => d.Npc));
            var diedAt = ev.OfType<NpcDied>().ToDictionary(d => d.Npc, d => d.Tick);
            foreach (var x in ev)
            {
                if (x is OrgHeadChanged h) holders[(h.Org, h.Office)] = h.To;
                else if (x is OrgDecisionTaken d)
                {
                    Assert.IsFalse(diedAt.TryGetValue(d.Proposer, out long t) && t < d.Tick, what + ": decision " + d.Decision + " by a dead proposer");
                    bool counted = d.Rule == DecisionKind.MajorityPresent && (d.Accept + d.Refuse > 0 || d.Outcome == DecisionOutcome.Lapsed);
                    bool office = holders.Any(kv => kv.Key.org == d.Org && kv.Value == d.Proposer);
                    Assert.IsTrue(office || counted || d.What == DecisionWhat.Join && holders.Any(kv => kv.Key.org == d.Org && kv.Value == d.Counter), what + ": decision " + d.Decision + " has no decider");
                }
            }
            var handovers = ev.OfType<ItemMoved>().Where(x => x.Reason == MoveReason.HandOver).ToList();
            foreach (var g in ev.OfType<WelfareGranted>())
                Assert.IsTrue(handovers.Any(hm => hm.Tick == g.Tick && hm.Actor == g.Keeper && hm.FromId == g.Keeper && hm.ToId == g.Recipient && hm.Count == g.Count && hm.OwnerBeforeKind == OwnerKind.Org),
                    what + ": welfare grant without the keeper's hand-over (within reach: Goods checks it)");
            foreach (var d in ev.OfType<ServiceDelivered>())
                Assert.IsTrue(ev.OfType<WelfareGranted>().Any(g => g.Request == d.Request && g.Tick == d.Tick && g.Keeper == d.ProviderNpc), what + ": delivery " + d.Request + " without its outcome event");
        }

        /// <summary>Two-sided Mann-Whitney U (normal approximation with tie correction): the p value.</summary>
        internal static double MannWhitneyP(int[] a, int[] b)
        {
            var all = a.Select(x => (v: x, g: 0)).Concat(b.Select(x => (v: x, g: 1))).OrderBy(x => x.v).ToList();
            int N = all.Count; var ranks = new double[N]; double tie = 0;
            for (int i = 0; i < N;)
            {
                int j = i; while (j < N && all[j].v == all[i].v) j++;
                double r = (i + j + 1) / 2.0; for (int k = i; k < j; k++) ranks[k] = r;
                double t = j - i; tie += t * t * t - t; i = j;
            }
            double r1 = 0; for (int i = 0; i < N; i++) if (all[i].g == 0) r1 += ranks[i];
            double n1 = a.Length, n2 = b.Length, u = r1 - n1 * (n1 + 1) / 2, mu = n1 * n2 / 2;
            double sigma = Math.Sqrt(n1 * n2 / 12.0 * ((N + 1) - tie / (N * (double)(N - 1))));
            if (sigma <= 0) return 1;
            double z = Math.Abs(u - mu) / sigma;
            return 2 * (1 - Phi(z));
        }

        private static double Phi(double z)
        {
            double t = 1 / (1 + 0.2316419 * z);
            double d = 0.3989422804014327 * Math.Exp(-z * z / 2);
            return 1 - d * t * (0.319381530 + t * (-0.356563782 + t * (1.781477937 + t * (-1.821255978 + t * 1.330274429))));
        }

        // ---------------------------------------------------------------- C6-T5 warehouse reconciliation (ORG-ACC-17, SVC-ACC-17)

        private static long Q(string db, string sql) { using (var d = Warehouse.WarehouseDb.OpenRead(db)) return Convert.ToInt64(d.Scalar(sql) ?? 0L); }

        /// <summary>
        /// From a 10-day run (the reconciliation fixture scenario, first seed of 1-6 with organisations): members, role and office
        /// spells, store totals, requests and welfare grants rebuilt from the event stream equal the fact tables exactly;
        /// dim_org count = OrgFounded; open spells = the roles and filled offices in state at the end; recording neutral.
        /// </summary>
        [Test]
        public void C6_Warehouse_Reconciles()
        {
            RunResult res = null; OrgProbe probe = null; string db = null;
            for (ulong seed = 1; seed <= 6; seed++)
            {
                var spec = TestUtil.Spec("org_shared_store_reconcile", seed, 10 * 1200);
                spec.Scenario.Checks.Remove("smoke");
                spec.Level = RecordingLevel.Full;
                spec.Sqlite = new Warehouse.WarehouseOptions { ForeignKeys = true, Async = false };
                spec.RunDir = System.IO.Path.Combine(_dir, "s" + seed); spec.OutRoot = _dir; spec.SkipJsonl = true;
                probe = new OrgProbe();
                var p = probe; spec.ExtraRecorders.Add(() => p);
                res = RunEngine.Run(spec);
                Assert.IsNull(res.Error, res.Error); Assert.AreEqual(0, res.ExitCode, string.Join("; ", res.Messages));
                db = res.WarehousePath;
                if (probe.Events.OfType<OrgFounded>().Any()) { TestContext.WriteLine("reconciling seed " + seed); break; }
            }
            var ev = probe.Events;
            int founded = ev.OfType<OrgFounded>().Count();
            Assert.Greater(founded, 0, "organisations exist in the fixture");
            Assert.AreEqual(founded, Q(db, "SELECT COUNT(*) FROM dim_org"), "dim_org count = OrgFounded");
            Assert.AreEqual(ev.OfType<OrgDissolved>().Count(), Q(db, "SELECT COUNT(*) FROM dim_org WHERE dissolved_tick IS NOT NULL"));
            Assert.AreEqual(founded + ev.OfType<OrgStatusChanged>().Count(), Q(db, "SELECT COUNT(*) FROM dim_org_history"));
            int joined = ev.OfType<MemberJoined>().Count(), offices = ev.OfType<OrgHeadChanged>().Count(h => h.To != 0);
            Assert.AreEqual(joined + offices, Q(db, "SELECT COUNT(*) FROM fact_org_membership"), "one spell per role taken and per office filled");
            Assert.AreEqual(joined, Q(db, "SELECT COUNT(*) FROM fact_org_membership WHERE office_index IS NULL"));
            Assert.AreEqual(ev.OfType<MemberLeft>().Count(), Q(db, "SELECT COUNT(*) FROM fact_org_membership WHERE office_index IS NULL AND to_tick IS NOT NULL"));
            var st = probe.State.Organisations;
            Assert.AreEqual(st.Orgs.Sum(o => o.Roles.Count), Q(db, "SELECT COUNT(*) FROM fact_org_membership WHERE office_index IS NULL AND to_tick IS NULL"), "open role spells = roles in state");
            Assert.AreEqual(st.Orgs.Sum(o => o.Offices.Count(x => x.Holder != 0)), Q(db, "SELECT COUNT(*) FROM fact_org_membership WHERE office_index IS NOT NULL AND to_tick IS NULL"), "open office spells = filled offices");
            Assert.AreEqual(0L, Q(db, "SELECT COUNT(*) FROM fact_org_membership WHERE to_tick IS NOT NULL AND to_tick < from_tick"), "half-open spells");
            var days = ev.OfType<OrgDay>().ToList();
            Assert.AreEqual(days.Count, Q(db, "SELECT COUNT(*) FROM fact_org_day"));
            Assert.AreEqual(days.Sum(d => d.StoreCounts.Sum()), Q(db, "SELECT COALESCE(SUM(store_units), 0) FROM fact_org_day"), "store totals");
            Assert.AreEqual(days.Sum(d => d.StoreCounts.Sum()), Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_org_day_store"));
            Assert.AreEqual(days.Sum(d => d.Members), Q(db, "SELECT COALESCE(SUM(members), 0) FROM fact_org_day"), "members per day");
            Assert.AreEqual(ev.OfType<ServiceRequested>().Count(), Q(db, "SELECT COUNT(*) FROM fact_service_request"));
            Assert.AreEqual(ev.OfType<ServiceDelivered>().Count(), Q(db, "SELECT COUNT(*) FROM fact_service_request WHERE status_code = 3"));
            Assert.AreEqual(ev.OfType<ServiceRefused>().Count(), Q(db, "SELECT COUNT(*) FROM fact_service_request WHERE status_code = 4"));
            Assert.AreEqual(ev.OfType<ServiceExpired>().Count(), Q(db, "SELECT COUNT(*) FROM fact_service_request WHERE status_code = 6"));
            Assert.AreEqual(ev.OfType<ServiceDelivered>().Count(), Q(db, "SELECT COUNT(*) FROM fact_service_delivery"));
            Assert.AreEqual(ev.OfType<ServiceOutcomeRecorded>().Count(), Q(db, "SELECT COUNT(*) FROM fact_service_outcome"));
            Assert.AreEqual(ev.OfType<WelfareGranted>().Count(), Q(db, "SELECT COUNT(*) FROM fact_welfare"));
            Assert.AreEqual(ev.OfType<WelfareGranted>().Sum(w => w.Count), Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_welfare"), "welfare units");
            Assert.AreEqual(ev.OfType<OrgSpent>().Count(), Q(db, "SELECT COUNT(*) FROM fact_org_expenditure"));
            Assert.AreEqual(ev.OfType<OrgDecisionTaken>().Count(), Q(db, "SELECT COUNT(*) FROM fact_org_decision"));
            TestContext.WriteLine("founded " + founded + ", members joined " + joined + ", offices filled " + offices + ", org days " + days.Count + ", requests " + ev.OfType<ServiceRequested>().Count()
                + ", rations " + ev.OfType<WelfareGranted>().Count() + ", decisions " + ev.OfType<OrgDecisionTaken>().Count() + ", records " + ev.OfType<RecordWritten>().Count());
            // recording changes no hash: the same seed with recording off
            var plain = TestUtil.Spec("org_shared_store_reconcile", res.Seed, 10 * 1200);
            plain.Scenario.Checks.Remove("smoke");
            var r2 = RunEngine.Run(plain);
            Assert.AreEqual(res.FinalHash, r2.FinalHash, "Full recording with the warehouse is neutral");
            Attribution(ev, "reconcile");
        }

        [Test, Explicit("diagnostics")]
        public void Diagnose_SharedStore()
        {
            for (ulong seed = 1; seed <= 4; seed++)
            {
                var (_, p) = Run(Environment.GetEnvironmentVariable("C6_DIAG") ?? "org_shared_store", seed, 24000);
                TestContext.WriteLine("  refusals " + string.Join(",", p.Events.OfType<ServiceRefused>().Select(r => r.ReasonSeen)) + " requestGestures "
                    + p.Events.OfType<God.Sim.Social.GestureMade>().Count(g => g.Act == GestureAct.NeedFood && g.TopicKind == TopicKind.Service)
                    + " withdraws " + p.Events.OfType<ItemMoved>().Count(m => m.Reason == MoveReason.Withdraw && m.OwnerBeforeKind == OwnerKind.Org)
                    + " dissolved " + p.Events.OfType<OrgDissolved>().Count() + " left " + string.Join(",", p.Events.OfType<MemberLeft>().Select(l => l.Reason)));
                var e = p.Events;
                TestContext.WriteLine("seed " + seed + ": minds " + p.Minds + " opps " + p.Opps + " maxEvidence " + p.MaxEvidence + " invites " + p.Invites + " accepted " + p.Accepted
                    + " meetingsMax " + p.Meetings + " proposals " + e.OfType<OrgOpportunityAvailable>().Count()
                    + " askjoin " + e.OfType<God.Sim.Social.GestureMade>().Count(g => g.Act == GestureAct.AskJoin && g.TopicKind == TopicKind.Org)
                    + " accept " + e.OfType<God.Sim.Social.GestureMade>().Count(g => g.Act == GestureAct.Accept && g.TopicKind == TopicKind.Org)
                    + " founded " + e.OfType<OrgFounded>().Count() + " deposits " + e.OfType<ItemMoved>().Count(m => m.Reason == MoveReason.Deposit && m.OwnerAfterKind == OwnerKind.Org)
                    + " requests " + e.OfType<ServiceRequested>().Count() + " welfare " + e.OfType<WelfareGranted>().Count() + " refused " + e.OfType<ServiceRefused>().Count()
                    + " decisions " + e.OfType<OrgDecisionTaken>().Count() + " records " + e.OfType<RecordWritten>().Count()
                    + " withSupporter " + p.WithSupporter + " ready " + p.Ready + " calls " + e.OfType<God.Sim.Social.GestureMade>().Count(g => g.Act == GestureAct.AskJoin && g.TopicKind == TopicKind.Org && g.Time == MsgTime.Now)
                    + " trust(other hh) n " + p.Trusts.Count + " >=300 " + p.Trusts.Count(t => t >= 300) + " median " + (p.Trusts.Count > 0 ? p.Trusts.OrderBy(t => t).ElementAt(p.Trusts.Count / 2) : 0));
            }
        }
    }
}
