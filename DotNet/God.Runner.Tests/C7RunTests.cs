using System;
using System.Collections.Generic;
using System.Linq;
using God.Recording;
using God.Sim;
using God.Sim.Orgs;
using God.Sim.Population;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// C7 at run level (docs/civdiv/DEVELOPMENT_PACKAGES/C7.md section 13): SVC-HEAL-2 (a staffed healer and wound deaths, 20
    /// seeds, reported honestly), SVC-ACC-4 (a practice founded with no player action, reported per seed), the service
    /// invariants SVC-I1/I5 for Treat over 20 seeds, no C7 activity without the practice, and the warehouse reconciliation.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class C7RunTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("c7"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private const int Tpd = 1200;

        private static List<C6RunTests.OrgProbe> Seeds(string scenario, int seeds, long ticks, params string[] sets)
        {
            var specs = new List<RunSpec>(); var probes = new List<C6RunTests.OrgProbe>();
            for (ulong seed = 1; seed <= (ulong)seeds; seed++)
            {
                var spec = TestUtil.Spec(scenario, seed, ticks, sets);
                spec.Scenario.Checks.Remove("smoke");
                var probe = new C6RunTests.OrgProbe(); probes.Add(probe);
                spec.ExtraRecorders.Add(() => probe);
                specs.Add(spec);
            }
            var res = TestUtil.RunAll(specs);
            for (int i = 0; i < res.Length; i++)
            {
                Assert.IsNull(res[i].Error, res[i].Error);
                Assert.AreEqual(0, res[i].ExitCode, scenario + " seed " + (i + 1) + ": " + string.Join("; ", res[i].Messages)); // includes the invariants (C1-C6, ORG, C7, SK)
            }
            return probes;
        }

        private static List<C6RunTests.OrgProbe> _health, _control;
        private static List<C6RunTests.OrgProbe> Health() => _health ?? (_health = Seeds("org_health", 20, 20 * Tpd));
        private static List<C6RunTests.OrgProbe> Control() => _control ?? (_control = Seeds("org_health_nohealer", 20, 20 * Tpd));

        /// <summary>
        /// SVC-HEAL-2: deaths from wounds over 20 days with one staffed healer per faction vs none, 20 seeds each (Mann-Whitney,
        /// effect size reported; the significance is reported, the scenario is not tuned to reach it). Also reported: all deaths,
        /// treatments, wounds taken. Asserted only what must hold: treatments happen with healers and never without.
        /// </summary>
        [Test]
        public void Healer_LowersWoundDeaths()
        {
            var h = Health(); var c = Control();
            int[] Wound(List<C6RunTests.OrgProbe> l) => l.Select(p => p.Events.OfType<NpcDied>().Count(d => d.Cause == DeathCause.Wound)).ToArray();
            int[] All(List<C6RunTests.OrgProbe> l) => l.Select(p => p.Events.OfType<NpcDied>().Count()).ToArray();
            int[] Treated(List<C6RunTests.OrgProbe> l) => l.Select(p => p.Events.OfType<TreatmentEnded>().Count(e => e.Outcome == TreatmentEnded.Completed)).ToArray();
            int[] Injuries(List<C6RunTests.OrgProbe> l) => l.Select(p => p.Events.OfType<God.Sim.Population.Households.InjuryTaken>().Count()).ToArray();
            var wh = Wound(h); var wc = Wound(c);
            double p = C6RunTests.MannWhitneyP(wc, wh);
            TestContext.WriteLine("wound deaths with healers  per seed: " + string.Join(" ", wh) + " (sum " + wh.Sum() + ", mean " + wh.Average().ToString("0.00") + ")");
            TestContext.WriteLine("wound deaths without        per seed: " + string.Join(" ", wc) + " (sum " + wc.Sum() + ", mean " + wc.Average().ToString("0.00") + ")");
            TestContext.WriteLine("all deaths with / without: " + All(h).Sum() + " / " + All(c).Sum() + "; injuries taken " + Injuries(h).Sum() + " / " + Injuries(c).Sum()
                + "; treatments completed " + Treated(h).Sum() + " / " + Treated(c).Sum() + "; Mann-Whitney p (control > healers) = " + p.ToString("0.0000")
                + "; effect (mean difference) " + (wc.Average() - wh.Average()).ToString("0.00"));
            if (p >= 0.05) TestContext.WriteLine("SVC-HEAL-2 NOT significant at 0.05 (reported, not tuned)");
            Assert.Greater(Treated(h).Sum(), 0, "healers treat");
            Assert.AreEqual(0, Treated(c).Sum(), "no practice, no treatment");
        }

        /// <summary>SVC-ACC-4: seeds on which a healer who treated at least three people informally founded its own practice (no player action), reported per seed.</summary>
        [Test]
        public void HealerPractice_Founded()
        {
            var h = Health();
            var per = h.Select(p => p.Events.OfType<OrgFounded>().Count(o => o.Template == OrgTemplates.HealerPractice)).ToArray();
            int seeds = per.Count(x => x > 0);
            TestContext.WriteLine("practices founded per seed: " + string.Join(" ", per) + "; seeds with a practice " + seeds + "/20 (criterion 10/20)");
            if (seeds < 10) TestContext.WriteLine("SVC-ACC-4 criterion NOT met (reported, conditions unchanged)");
            foreach (var p in h)
                foreach (var o in p.Events.OfType<OrgFounded>().Where(o => o.Template == OrgTemplates.HealerPractice))
                {
                    int healer = o.Founders[0];
                    int before = p.Events.OfType<TreatmentEnded>().Count(e => e.Healer == healer && e.Outcome == TreatmentEnded.Completed && e.Tick <= o.Tick);
                    Assert.GreaterOrEqual(before, 3, "a practice is founded only by someone who treated at least three people");
                }
        }

        /// <summary>
        /// SVC-I1 / SVC-I5 for Treat, 20 seeds x 20 days (exact): every delivered treatment (ServiceDelivered outcome 521) has a
        /// completed TreatmentEnded of the same request by the same healer, who took at least TreatTicks and was alive; a healer
        /// who died delivers nothing afterwards. Without the practice no C7 state or event exists.
        /// </summary>
        [Test]
        public void ServiceInvariants_Treat_20Seeds()
        {
            int delivered = 0;
            foreach (var p in Health())
            {
                var ends = p.Events.OfType<TreatmentEnded>().Where(e => e.Outcome == TreatmentEnded.Completed).ToDictionary(e => e.Request);
                var deaths = p.Events.OfType<NpcDied>().ToDictionary(d => d.Npc, d => d.Tick);
                foreach (var d in p.Events.OfType<ServiceDelivered>().Where(d => d.OutcomeEvent == 521))
                {
                    delivered++;
                    Assert.IsTrue(ends.TryGetValue(d.Request, out var e), "a delivered treatment completed");
                    Assert.AreEqual(e.Healer, d.ProviderNpc);
                    Assert.GreaterOrEqual(e.Ticks, p.State.Tuning.Orgs.C7.TreatTicks);
                    Assert.IsFalse(deaths.TryGetValue(d.ProviderNpc, out long t) && t < d.Tick, "SVC-I5: a dead healer delivers nothing");
                }
            }
            TestContext.WriteLine("delivered treatments checked: " + delivered);
            Assert.Greater(delivered, 0);
            foreach (var p in Control())
            {
                Assert.IsNull(p.State.Organisations.Health, "no C7 state without the practice");
                Assert.IsEmpty(p.Events.OfType<TreatmentStarted>());
            }
        }

        /// <summary>
        /// C7-T4 <c>C7_Warehouse_Reconciles</c> (Full + SQLite): fact_health_encounter rows = TreatmentStarted, ended rows =
        /// TreatmentEnded, completed rows; enrolment rows = TeachingSession; assessments = KnowledgeTransferred + skill changes; per
        /// person the sum of skill changes (after - before) equals the skill store at the end; every encounter joins one service request.
        /// </summary>
        [Test]
        public void C7_Warehouse_Reconciles()
        {
            RunResult res = null; C6RunTests.OrgProbe probe = null; ulong used = 0;
            for (ulong seed = 1; seed <= 10; seed++)
            {
                var spec = TestUtil.Spec("org_health_reconcile", seed, 12 * Tpd);
                spec.Scenario.Checks.Remove("smoke");
                spec.Level = RecordingLevel.Full;
                spec.Sqlite = new Warehouse.WarehouseOptions { ForeignKeys = true, Async = false };
                spec.RunDir = System.IO.Path.Combine(_dir, "s" + seed); spec.OutRoot = _dir; spec.SkipJsonl = true;
                probe = new C6RunTests.OrgProbe();
                var pr0 = probe; spec.ExtraRecorders.Add(() => pr0);
                res = RunEngine.Run(spec);
                Assert.IsNull(res.Error, res.Error); Assert.AreEqual(0, res.ExitCode, string.Join("; ", res.Messages));
                used = seed;
                if (probe.Events.OfType<TreatmentEnded>().Count(e => e.Outcome == TreatmentEnded.Completed) >= 2) { TestContext.WriteLine("reconciling seed " + seed); break; }
            }
            string db = res.WarehousePath;
            var ev = probe.Events;
            int started = ev.OfType<TreatmentStarted>().Count();
            Assert.Greater(started, 0, "treatments in the fixture");
            Assert.AreEqual(started, Q(db, "SELECT COUNT(*) FROM fact_health_encounter"));
            Assert.AreEqual(ev.OfType<TreatmentEnded>().Count(), Q(db, "SELECT COUNT(*) FROM fact_health_encounter WHERE end_tick IS NOT NULL"));
            Assert.AreEqual(ev.OfType<TreatmentEnded>().Count(e => e.Outcome == TreatmentEnded.Completed), Q(db, "SELECT COUNT(*) FROM fact_health_encounter WHERE outcome_code = 1"));
            Assert.AreEqual(ev.OfType<TreatmentEnded>().Count(e => e.Remedy), Q(db, "SELECT COALESCE(SUM(remedy_used), 0) FROM fact_health_encounter"));
            Assert.AreEqual(ev.OfType<TreatmentEnded>().Sum(e => (long)(e.InjuryBefore - e.InjuryAfter)), Q(db, "SELECT COALESCE(SUM(injury_before - injury_after), 0) FROM fact_health_encounter WHERE end_tick IS NOT NULL"), "truth columns at Full");
            Assert.AreEqual(started, Q(db, "SELECT COUNT(*) FROM fact_health_encounter h JOIN fact_service_request r ON r.run_key = h.run_key AND r.request_id = h.request_id WHERE r.kind_code = 1"), "1:1 with service requests of kind Treat");
            Assert.AreEqual(ev.OfType<TeachingSession>().Count(), Q(db, "SELECT COUNT(*) FROM fact_education_enrolment"));
            Assert.AreEqual(ev.OfType<KnowledgeTransferred>().Count(), Q(db, "SELECT COUNT(*) FROM fact_education_assessment WHERE assessment_kind = 1"));
            var skills = ev.OfType<HealerSkillChanged>().ToList();
            Assert.AreEqual(skills.Count, Q(db, "SELECT COUNT(*) FROM fact_education_assessment WHERE assessment_kind = 2"));
            Assert.AreEqual(skills.Sum(x => (long)(x.To - x.From)), Q(db, "SELECT COALESCE(SUM(skill_after - skill_before), 0) FROM fact_education_assessment WHERE assessment_kind = 2"), "skill gains sum");
            // the skill store at the end = the start skill + the sum of recorded gains, per living healer
            var st = probe.State;
            foreach (var g in skills.GroupBy(x => (x.Npc, x.Skill)))
            {
                var n = st.Population.Find(g.Key.Npc);
                if (n == null || !n.Alive) continue;
                int first = g.OrderBy(x => x.Tick).ThenBy(x => x.Seq).First().From;
                Assert.AreEqual(first + g.Sum(x => x.To - x.From), God.Sim.Civ.Skills.Of(st, g.Key.Npc, (God.Sim.Civ.SkillClass)g.Key.Skill), "skill of " + g.Key.Npc);
            }
            TestContext.WriteLine("treatments " + started + ", completed " + ev.OfType<TreatmentEnded>().Count(e => e.Outcome == TreatmentEnded.Completed) + ", skill changes " + skills.Count
                + ", herbs used " + ev.OfType<TreatmentEnded>().Count(e => e.Remedy) + ", practices " + ev.OfType<OrgFounded>().Count(o => o.Template == OrgTemplates.HealerPractice));
            // recording changes no hash
            var plain = TestUtil.Spec("org_health_reconcile", used, 12 * Tpd);
            plain.Scenario.Checks.Remove("smoke");
            var pr = RunEngine.Run(plain);
            Assert.AreEqual(res.FinalHash, pr.FinalHash, "Full recording is neutral");
        }

        /// <summary>
        /// C7b at island scale (org_teach vs org_teach_none, 20 seeds x 15 days): lessons and demonstrations happen with nothing
        /// scripted; SVC-I1/I5 for Teach (every transfer has its session with at least TeachTicks, by a living teacher); taught
        /// skill never reaches the teacher's; no C7 state without a teacher. Counts reported.
        /// </summary>
        [Test]
        public void Teaching_NaturalRun_20Seeds()
        {
            var t = Seeds("org_teach", 20, 15 * Tpd);
            var c = Seeds("org_teach_none", 20, 15 * Tpd);
            int sessions = 0, transfers = 0, entries = 0, demos = 0, seedsWith = 0; long gained = 0;
            foreach (var p in t)
            {
                var kts = p.Events.OfType<KnowledgeTransferred>().ToList();
                var ses = p.Events.OfType<TeachingSession>().ToList();
                var deaths = p.Events.OfType<NpcDied>().ToDictionary(d => d.Npc, d => d.Tick);
                if (kts.Count > 0) seedsWith++;
                sessions += ses.Count; transfers += kts.Count; entries += kts.Sum(k => k.EntriesCopied); demos += kts.Count(k => k.Kind == TeachingSession.Demonstrate);
                gained += kts.Sum(k => (long)(k.SkillAfter - k.SkillBefore));
                foreach (var k in kts)
                {
                    Assert.IsTrue(ses.Any(x => x.Teacher == k.Teacher && x.Learner == k.Learner && x.Tick == k.Tick && x.Ticks >= p.State.Tuning.Orgs.C7.TeachTicks), "a transfer has its session");
                    Assert.Less(k.SkillAfter, p.State.Tuning.Orgs.C7.StartTeachSkill, "taught skill stays below the teacher's");
                    Assert.IsFalse(deaths.TryGetValue(k.Teacher, out long d) && d < k.Tick, "a dead teacher teaches nothing");
                }
            }
            TestContext.WriteLine("org_teach 20 seeds x 15 days: seeds with a lesson " + seedsWith + "/20, sessions " + sessions + " (demonstrations " + demos + "), transfers " + transfers
                + ", associations copied " + entries + ", skill gained in total " + gained);
            foreach (var p in c) { Assert.IsNull(p.State.Organisations.Health); Assert.IsEmpty(p.Events.OfType<TeachingSession>()); }
            Assert.Greater(transfers, 0, "lessons happen without a script");
        }

        private static long Q(string db, string sql)
        {
            using (var c = new SqliteConnection("Data Source=" + db))
            {
                c.Open();
                using (var cmd = c.CreateCommand()) { cmd.CommandText = sql; return Convert.ToInt64(cmd.ExecuteScalar()); }
            }
        }
    }
}
