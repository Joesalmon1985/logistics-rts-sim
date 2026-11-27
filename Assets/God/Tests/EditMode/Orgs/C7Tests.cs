using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using God.Sim.Civ;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Orgs;
using God.Sim.Policy;
using God.Sim.Population;
using God.Sim.Population.Households;
using NUnit.Framework;
using OrgApi = God.Sim.Orgs.Orgs;

namespace God.Sim.Tests
{
    /// <summary>
    /// C7a healthcare (docs/civdiv/DEVELOPMENT_PACKAGES/C7.md section 13): Treat = M4 Tend + skill + remedy, acting on the
    /// patient's actual body (SVC-HEAL-U1/U2), brief scenario 5 (SVC-HEAL-1), capacity (SVC-HEAL-3), reach and time
    /// (C7-T1), diagnosis from perception (C7-T2), SVC-I2/I4/I5 and save/load parity. The real sim: real people, gestures, goods.
    /// </summary>
    public class C7Tests
    {
        internal sealed class W
        {
            public GodSim Sim; public GodState S; public int Glade; public List<SimEvent> Log = new List<SimEvent>();
            public void Step(int n) { for (int i = 0; i < n; i++) { Sim.Step(); Log.AddRange(Sim.DrainEvents()); } }
            public IEnumerable<T> Of<T>() => Log.OfType<T>();
        }

        internal static W World(ulong seed = 7, Action<SimTuning> tune = null)
        {
            var sim = LearningFixtures.Empty(seed, t =>
            {
                t.Orgs.Enabled = 1; t.Civ.Enabled = 1;
                t.Households.Life.InjuryDeathPermille = 0; // no random wound death in fixtures
                tune?.Invoke(t);
            });
            return new W { Sim = sim, S = sim.State, Glade = LearningFixtures.Glade(sim) };
        }

        internal static Npc Person(W w, int node, int hunger = 150) => HouseholdFixtures.Person(w.Sim, node, 0, 2000, hunger: hunger);
        internal static int At(W w, int dir, int steps) => LearningFixtures.Step(w.Sim, w.Glade, dir, steps);

        private static Npc Healer(W w, int node, int skill)
        {
            var h = Person(w, node);
            Healthcare.MakeHealer(w.S, h, skill);
            return h;
        }

        private static void Wound(Npc n, int injuryMicro) { n.Injury = injuryMicro; n.Drives[Drive.Pain] = injuryMicro; }

        // ---------------------------------------------------------------- SVC-HEAL-U2 (formula)

        [Test]
        public void Treat_Monotone_Floored()
        {
            var tu = new OrgC7Tuning();
            int tend = new SimTuning().Households.Care.TendSeverityPermille;
            foreach (bool remedy in new[] { false, true })
            {
                int prev = int.MaxValue, prevInj = int.MinValue;
                for (int skill = 0; skill <= 1000; skill += 50)
                {
                    int m = Healthcare.SeverityPermille(tend, skill, remedy, tu);
                    int inj = Healthcare.InjuryPermille(skill, remedy, tu);
                    Assert.LessOrEqual(m, prev, "severity multiplier non-increasing in skill");
                    Assert.GreaterOrEqual(m, tu.TreatFloorPermille, "floored");
                    Assert.GreaterOrEqual(inj, prevInj, "injury recovery non-decreasing in skill");
                    prev = m; prevInj = inj;
                }
            }
            Assert.AreEqual(tend, Healthcare.SeverityPermille(tend, 0, false, tu), "skill 0, no remedy: M4's Tend multiplier");
            Assert.AreEqual(tu.TreatFloorPermille, Healthcare.SeverityPermille(tend, 1000, true, tu), "skill 1000 with remedy hits the floor");
            Assert.AreEqual(595, Healthcare.SeverityPermille(tend, 300, false, tu), "700 x (1000 - 150) / 1000");
        }

        // ---------------------------------------------------------------- SVC-HEAL-U1 (M4 parity)

        /// <summary>
        /// Two identical games with an ill patient. In one an M4 carer tends it (HouseholdActs Tend); in the other a healer with
        /// skill 0 and no remedy treats it. The helper then dies in both (so nothing else differs). Severity, days left, days
        /// ill and Health are equal at every tick for three days: Treat at skill 0 equals Tend exactly for the illness.
        /// </summary>
        [Test]
        public void Treat_Skill0_EqualsTend()
        {
            (W w, Npc patient, Npc helper) Make()
            {
                var w = World(11);
                var patient = Person(w, w.Glade);
                var helper = Person(w, At(w, 0, 1));
                patient.Life.Illness = 1; patient.Life.Severity = 250; patient.Life.IllDaysLeft = 5; patient.Life.IllDays = 0;
                return (w, patient, helper);
            }
            var (a, pa, ha) = Make();
            var (b, pb, hb) = Make();
            Healthcare.MakeHealer(b.S, hb, 0);
            // the act, driven as a decision would start it
            RunAct(a, ha, HVerb.Tend, 0, IntentTarget.Npc, pa.Id, pa.Node);
            RunAct(b, hb, C7Verb.Treat, Healthcare.Give, IntentTarget.Npc, pb.Id, pb.Node, known: pb.Id);
            Assert.AreEqual(pa.Life.TendedDay, pb.Life.TendedDay, "both set M4's tended day");
            Assert.AreEqual(1, b.Of<TreatmentEnded>().Count(e => e.Outcome == TreatmentEnded.Completed));
            Assert.AreEqual(a.S.Tick, b.S.Tick);
            HouseholdFixtures.Kill(a.Sim, ha.Id, DeathCause.Accident); HouseholdFixtures.Kill(b.Sim, hb.Id, DeathCause.Accident);
            int tpd = SimCalendar.Of(a.S).TicksPerDay;
            for (int t = 0; t < 3 * tpd; t++)
            {
                a.Step(1); b.Step(1);
                Assert.AreEqual(pa.Health, pb.Health, "health at tick " + a.S.Tick);
                Assert.AreEqual(pa.Life.Severity, pb.Life.Severity);
                Assert.AreEqual(pa.Life.IllDaysLeft, pb.Life.IllDaysLeft, "days left at tick " + a.S.Tick);
                Assert.AreEqual(pa.Life.Illness, pb.Life.Illness);
            }
            Assert.Greater(pa.Life.IllDays, 0, "the illness ran its days");
        }

        /// <summary>
        /// The same pair of games with a skilled healer (300): the illness loses exactly
        /// <c>base x (TendSeverity - SeverityPermille(300))/1000</c> less Health on each of the two tended days than under M4's
        /// Tend, through the hook into the M4 path (the patient's own body; no modifier anywhere else).
        /// </summary>
        [Test]
        public void Treat_Skill_LowersIllnessLoss_Exact()
        {
            (W w, Npc patient, Npc helper) Make()
            {
                var w = World(11);
                var patient = Person(w, w.Glade);
                var helper = Person(w, At(w, 0, 1));
                patient.Life.Illness = 1; patient.Life.Severity = 250; patient.Life.IllDaysLeft = 5; patient.Life.IllDays = 0; patient.Health = 600000;
                return (w, patient, helper);
            }
            var (a, pa, ha) = Make();
            var (b, pb, hb) = Make();
            Healthcare.MakeHealer(b.S, hb, 300);
            RunAct(a, ha, HVerb.Tend, 0, IntentTarget.Npc, pa.Id, pa.Node);
            RunAct(b, hb, C7Verb.Treat, Healthcare.Give, IntentTarget.Npc, pb.Id, pb.Node, known: pb.Id);
            HouseholdFixtures.Kill(a.Sim, ha.Id, DeathCause.Accident); HouseholdFixtures.Kill(b.Sim, hb.Id, DeathCause.Accident);
            int tpd = SimCalendar.Of(a.S).TicksPerDay;
            a.Step(3 * tpd); b.Step(3 * tpd);
            var tu = b.S.Tuning;
            int baseLoss = 250 * tu.Households.Disease.SeverityScalePermille / 1000;
            int lossTend = baseLoss * tu.Households.Care.TendSeverityPermille / 1000;
            int lossTreat = baseLoss * Healthcare.SeverityPermille(tu.Households.Care.TendSeverityPermille, 300, false, tu.Orgs.C7) / 1000;
            TestContext.WriteLine("health tended " + pa.Health + ", treated " + pb.Health + "; per tended day loss " + lossTend + " vs " + lossTreat);
            Assert.AreEqual(2 * (lossTend - lossTreat) * 1000, pb.Health - pa.Health, "two tended days, each with the treated multiplier");
            Assert.AreEqual(pa.Life.IllDaysLeft, pb.Life.IllDaysLeft, "days left as M4's tended days");
            Assert.AreEqual(2L * (lossTend - lossTreat) * 1000, b.S.Organisations.Health.HealthCredited);
        }

        // ---------------------------------------------------------------- SVC-HEAL-1 (brief scenario 5)

        /// <summary>
        /// An injured person in pain gestures NeedHelp at the healer it believes in; the healer perceives the request, walks
        /// to it, treats it within reach for TreatTicks and its wound heals faster than the same person's in a control game
        /// where the same neighbour is no healer (exact: the difference is the extra recovery the treated record applied).
        /// </summary>
        [Test]
        public void Scenario5_RequestTreatRecover()
        {
            (W w, Npc patient, Npc other) Make(bool healer)
            {
                var w = World(5);
                var other = Person(w, w.Glade);
                var patient = Person(w, At(w, 0, 3));
                Wound(patient, 700000);
                if (healer) { Healthcare.MakeHealer(w.S, other, 300); Healthcare.BelieveHealer(w.S, patient, other.Id); }
                return (w, patient, other);
            }
            var (t, pt, ht) = Make(true);
            var (c, pc, hc) = Make(false);
            int tpd = SimCalendar.Of(t.S).TicksPerDay;
            t.Step(tpd); c.Step(tpd);
            var ask = t.Of<Social.GestureMade>().FirstOrDefault(g => g.Act == GestureAct.NeedHelp && g.Speaker == pt.Id && g.TopicKind == TopicKind.Service);
            Assert.IsNotNull(ask, "the patient asked for help by gesture");
            var started = t.Of<TreatmentStarted>().FirstOrDefault(e => e.Patient == pt.Id);
            Assert.IsNotNull(started, "the healer took the patient");
            Assert.AreEqual(ht.Id, started.Healer);
            var ended = t.Of<TreatmentEnded>().FirstOrDefault(e => e.Patient == pt.Id && e.Outcome == TreatmentEnded.Completed);
            Assert.IsNotNull(ended, "treated to the end");
            Assert.GreaterOrEqual(ended.Ticks, t.S.Tuning.Orgs.C7.TreatTicks);
            Assert.IsTrue(t.Of<ServiceDelivered>().Any(d => d.ProviderNpc == ht.Id && d.OutcomeEvent == 521));
            Assert.IsEmpty(c.Of<TreatmentStarted>(), "control: nobody treats");
            t.Step(2 * tpd); c.Step(2 * tpd);
            long healed = t.S.Organisations.Health.InjuryHealed;
            TestContext.WriteLine("injury after 3 days: treated " + pt.Injury / 1000 + ", control " + pc.Injury / 1000 + " (start 700); extra recovery by treatment " + healed / 1000
                + "; asked at tick " + ask.Tick + ", treated " + started.Tick + "-" + ended.Tick + ", skill " + started.Skill + ", injury multiplier " + ended.InjuryPermille);
            Assert.Less(pt.Injury, pc.Injury, "the treated wound is smaller");
            Assert.AreEqual(pc.Injury - pt.Injury, healed, "exactly the extra recovery the treatment applied (the bodies otherwise heal alike)");
            Assert.IsEmpty(SimInvariants.Check(t.S));
        }

        // ---------------------------------------------------------------- C7-T1 reach, time and the remedy

        [Test]
        public void Treat_NeedsReachAndTime()
        {
            var w = World(3);
            var healer = Healer(w, w.Glade, 400);
            var patient = Person(w, At(w, 1, 6));
            Wound(patient, 600000);
            HouseholdFixtures.Give(w.Sim, healer, Ware.Herbs, 2);
            int herbs = Healthcare.HerbsIn(healer);
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            healer.Mind.Orgs.AddKnown(new KnownRequest { Kind = ServiceKind.Treat, Requester = patient.Id, Tick = 0, SeenNeed = 1 });
            healer.Action = new NpcAction { Verb = C7Verb.Treat, TargetKind = IntentTarget.Npc, TargetId = patient.Id, TargetNode = patient.Node, Since = w.S.Tick };
            var c = new Candidate { Verb = C7Verb.Treat, Kind = IntentTarget.Npc, TargetId = patient.Id, TargetNode = patient.Node, Class = Healthcare.Give };
            Assert.IsTrue(PopulationExtensions.TryStart(w.S, ev, healer, in c, 0));
            var g = w.S.Territory.Grid;
            int calls = 0, firstInReach = -1;
            while (healer.Action.Verb == C7Verb.Treat && calls < 5000)
            {
                if (firstInReach < 0 && g.Distance(healer.Node, patient.Node) <= Reach.Touch) firstInReach = calls;
                Assert.Less(w.S.Organisations.Health.IndexOfTreated(patient.Id), 0, "no effect before the treatment completes");
                PopulationExtensions.TryAct(w.S, ev, healer, 0); calls++;
            }
            w.Log.AddRange(ev.Drain());
            Assert.Greater(firstInReach, 0, "it walked first (6 nodes away)");
            Assert.GreaterOrEqual(calls - firstInReach + 1, w.S.Tuning.Orgs.C7.TreatTicks, "TreatTicks counted only within reach (the arrival call counts)");
            Assert.GreaterOrEqual(w.S.Organisations.Health.IndexOfTreated(patient.Id), 0, "the treated record exists after completion");
            var used = w.Of<Consumed>().Where(x => x.Reason == ConsumeReason.Treatment).ToList();
            Assert.AreEqual(1, used.Count, "one remedy lot consumed once");
            Assert.AreEqual(Ware.Herbs, used[0].Ware);
            Assert.AreEqual(herbs - 1, Healthcare.HerbsIn(healer));
            Assert.IsTrue(w.Of<TreatmentEnded>().Single().Remedy);
            Assert.IsEmpty(SimInvariants.Check(w.S));
        }

        // ---------------------------------------------------------------- C7-T2 / SVC-I4 diagnosis from perception; SVC-I2

        [Test]
        public void Diagnosis_FromPerception()
        {
            var d = typeof(Healthcare).GetMethod(nameof(Healthcare.Diagnose));
            foreach (var p in d.GetParameters())
                Assert.IsFalse(p.ParameterType == typeof(GodState) || p.ParameterType == typeof(Npc) || p.ParameterType == typeof(PopulationState), "Diagnose reads " + p.Name);
            // the mind side reads only the scratch and its own mind
            var think = typeof(OrgMindHooks).Assembly.GetType("God.Sim.Orgs.HealthThink");
            foreach (var m in think.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                foreach (var p in m.GetParameters())
                    Assert.IsFalse(p.ParameterType == typeof(GodState) || p.ParameterType == typeof(Npc), "HealthThink." + m.Name + " reads " + p.ParameterType.Name);
            // a perfect healer reads exactly what it saw; a novice may err by at most the noise
            var r = new Rng(1);
            for (int i = 0; i < 200; i++)
            {
                Assert.AreEqual(2, Healthcare.Diagnose(2, 1000, 2, r));
                Assert.That(Healthcare.Diagnose(2, 0, 2, r), Is.InRange(0, 3));
            }
        }

        /// <summary>SVC-I2 (static): no health or skill multiplier keyed by organisation or faction in C7's code.</summary>
        [Test]
        public void NoOrgOrFactionModifier_Static()
        {
            foreach (var t in new[] { typeof(Healthcare), typeof(TreatedPatient), typeof(HealthEducationState) })
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    Assert.IsFalse(f.FieldType == typeof(Faction) || f.FieldType.Name.Contains("Dictionary"), t.Name + "." + f.Name);
            foreach (var p in typeof(Healthcare).GetMethod(nameof(Healthcare.SeverityPermille)).GetParameters())
                Assert.IsFalse(p.ParameterType == typeof(Organisation) || p.ParameterType == typeof(Faction));
        }

        // ---------------------------------------------------------------- SVC-HEAL-3 capacity and unmet demand

        /// <summary>
        /// One healer with its own practice and six wounded people who all ask at once: requests queue in the practice's ring
        /// (at most one Active at a time: one patient per healer), at least one waits while another is treated, and requests
        /// that wait too long expire (recorded unmet demand).
        /// </summary>
        [Test]
        public void Healer_Capacity()
        {
            var w = World(9, t => t.Orgs.C6.RequestExpireTicks = 300);
            var healer = Healer(w, w.Glade, 300);
            var ev = new EventBuffer { CurrentTick = 0 };
            var org = OrgApi.Found(w.S, ev, OrgTemplates.HealerPractice, new List<int> { healer.Id }, w.Glade, 0);
            healer.Mind.Orgs.Health.Practice = org.Id;
            w.Log.AddRange(ev.Drain());
            for (int i = 0; i < 6; i++) { var p = Person(w, At(w, i % 6, 2)); Wound(p, 700000); Healthcare.BelieveHealer(w.S, p, healer.Id); }
            int maxWaiting = 0, maxActive = 0, bothTicks = 0;
            int tpd = SimCalendar.Of(w.S).TicksPerDay;
            for (int t = 0; t < tpd; t++)
            {
                w.Step(1);
                int waiting = org.Requests.Count(r => r.Kind == ServiceKind.Treat && r.Status == RequestStatus.Waiting);
                int active = org.Requests.Count(r => r.Kind == ServiceKind.Treat && r.Status == RequestStatus.Active);
                maxWaiting = Math.Max(maxWaiting, waiting); maxActive = Math.Max(maxActive, active);
                if (waiting > 0 && active > 0) bothTicks++;
            }
            int expired = w.Of<ServiceExpired>().Count(e => e.Org == org.Id);
            int delivered = w.Of<ServiceDelivered>().Count(e => e.Org == org.Id);
            TestContext.WriteLine("requests opened " + w.Of<ServiceRequested>().Count(e => e.Org == org.Id) + ", max queue " + maxWaiting + ", max active " + maxActive
                + ", ticks with someone waiting while another is treated " + bothTicks + ", delivered " + delivered + ", expired " + expired);
            Assert.LessOrEqual(maxActive, 1, "one patient per healer at a time");
            Assert.GreaterOrEqual(maxWaiting, 2, "the queue grows");
            Assert.Greater(bothTicks, 0, "at least one patient waits while another is treated");
            Assert.Greater(expired, 0, "recorded unmet demand");
        }

        // ---------------------------------------------------------------- SVC-I5 no staff, no service

        [Test]
        public void NoHealer_NoService()
        {
            var w = World(4);
            var healer = Healer(w, w.Glade, 300);
            var ev = new EventBuffer { CurrentTick = 0 };
            OrgApi.Found(w.S, ev, OrgTemplates.HealerPractice, new List<int> { healer.Id }, w.Glade, 0);
            var p = Person(w, At(w, 0, 2)); Wound(p, 700000); Healthcare.BelieveHealer(w.S, p, healer.Id);
            HouseholdFixtures.Kill(w.Sim, healer.Id, DeathCause.Accident);
            w.Step(SimCalendar.Of(w.S).TicksPerDay * 2);
            Assert.IsEmpty(w.Of<TreatmentStarted>());
            Assert.IsEmpty(w.Of<ServiceDelivered>());
        }

        // ---------------------------------------------------------------- C7-T4 save/load parity mid-treatment

        [Test]
        public void C7_SaveLoadParity()
        {
            W Make()
            {
                var w = World(13);
                var healer = Healer(w, w.Glade, 300);
                HouseholdFixtures.Give(w.Sim, healer, Ware.Herbs, 1);
                for (int i = 0; i < 2; i++) { var p = Person(w, At(w, i, 4)); Wound(p, 700000); Healthcare.BelieveHealer(w.S, p, healer.Id); }
                return w;
            }
            var a = Make(); var b = Make();
            for (int i = 0; i < 2000 && (a.S.Organisations.Health == null || a.S.Organisations.Health.Active.Count != 1); i++) { a.Step(1); b.Step(1); }
            Assert.AreEqual(1, a.S.Organisations.Health.Active.Count, "saved mid-treatment");
            string json = SaveFile.From(a.Sim).ToJson();
            var loaded = SaveFile.FromJson(json).ToSim();
            Assert.AreEqual(a.Sim.StateHash(), loaded.StateHash());
            Assert.AreEqual(json, SaveFile.From(loaded).ToJson(), "a second save is identical");
            for (int k = 0; k < 6; k++)
            {
                a.Sim.Step(300); loaded.Step(300); b.Sim.Step(300);
                Assert.AreEqual(a.Sim.StateHash(), loaded.StateHash(), "save/load continuation at checkpoint " + k);
                Assert.AreEqual(a.Sim.StateHash(), b.Sim.StateHash(), "replay at checkpoint " + k);
            }
            foreach (var e in a.Log) if (e is TreatmentStarted || e is TreatmentEnded) TestContext.WriteLine(e.GetType().Name + " " + Newtonsoft.Json.JsonConvert.SerializeObject(e));
            Assert.Greater(a.S.Organisations.Health.TreatmentsCompleted, 0);
            Assert.IsEmpty(SimInvariants.Check(loaded.State));
        }

        // ================================================================ C7b education

        private static readonly uint WoodKey = Feature.Class(ObjClass.WoodStand);

        /// <summary>Sets one association of a person directly (fixture: what it learned before the scene).</summary>
        internal static void Assoc(Npc n, uint key, byte verb, short value)
        {
            var st = n.Mind.Store;
            int at = st.Find(key, verb);
            if (at < 0) at = st.InsertAt(~at, key, verb, 0, Origin.Own);
            var e = st.Entries[at]; e.Stw = value; e.Ltw = value; st.Entries[at] = e;
        }

        internal static short ValueOf(Npc n, uint key, byte verb, out Origin origin)
        {
            int at = n.Mind.Store.Find(key, verb);
            origin = at >= 0 ? n.Mind.Store.Entries[at].Origin : Origin.Own;
            return at >= 0 ? n.Mind.Store.Entries[at].Ltw : (short)0;
        }

        /// <summary>
        /// A teacher (skill 600 in gathering wood with an axe, a learned value for gathering at a wood stand) and a learner of its
        /// household beside it: the teacher offers by gesture, the learner chooses to attend, the session runs, the learner's
        /// copied value and skill change by the formula's exact deltas.
        /// </summary>
        internal static (W w, Npc teacher, Npc learner) Lesson(ulong seed, short teacherValue, int teacherSkill = 600, bool teach = true)
        {
            var w = World(seed);
            var teacher = Person(w, w.Glade);
            var learner = Person(w, At(w, 0, 1));
            SocialFixtures.SetRelation(w.Sim, learner, teacher, 400, 500);
            SocialFixtures.SetRelation(w.Sim, teacher, learner, 400, 500);
            Assoc(teacher, WoodKey, Verb.Gather, teacherValue);
            Assoc(teacher, Feature.Class(ObjClass.BerryBush), Verb.Eat, 900); // another topic's knowledge: never copied by a wood lesson
            if (teach) Education.MakeTeacher(w.S, teacher, (int)SkillClass.Woodcutting, teacherSkill);
            return (w, teacher, learner);
        }

        /// <summary>Runs the lesson through the real gestures and choices until a KnowledgeTransferred (or the limit).</summary>
        internal static KnowledgeTransferred RunLesson(W w, Npc teacher, Npc learner, int max = 3000)
        {
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            teacher.Action = new NpcAction { Verb = C7Verb.Teach, TargetKind = IntentTarget.Npc, TargetId = learner.Id, TargetNode = learner.Node, Since = w.S.Tick };
            var c = new Candidate { Verb = C7Verb.Teach, Kind = IntentTarget.Npc, TargetId = learner.Id, TargetNode = learner.Node, Class = (byte)(Education.OfferBase + (int)SkillClass.Woodcutting) };
            PopulationExtensions.TryStart(w.S, ev, teacher, in c, 0);
            w.Log.AddRange(ev.Drain());
            for (int i = 0; i < max && !w.Of<KnowledgeTransferred>().Any(k => k.Learner == learner.Id); i++) w.Step(1);
            return w.Of<KnowledgeTransferred>().FirstOrDefault(k => k.Learner == learner.Id);
        }

        [Test]
        public void Scenario6_TeachChangesCapability_ExactDeltas()
        {
            var (w, teacher, learner) = Lesson(21, 2000);
            short before = ValueOf(learner, WoodKey, Verb.Gather, out _);
            var kt = RunLesson(w, teacher, learner);
            Assert.IsNotNull(kt, "the learner attended and the lesson completed");
            Assert.IsTrue(w.Of<Social.GestureMade>().Any(g => g.Act == GestureAct.Teach && g.Speaker == teacher.Id), "offered by gesture");
            var tu = w.S.Tuning.Orgs.C7;
            int attention = 1000; // a sated, painless learner
            int fidelity = 600 * attention / 1000;
            int trust = Social.SocialView.Trust(learner.Mind, teacher.Id);
            int expectValue = Imitation.Scaled(2000, fidelity, Imitation.TrustFactor(trust));
            short after = ValueOf(learner, WoodKey, Verb.Gather, out var origin);
            TestContext.WriteLine("skill " + kt.SkillBefore + " -> " + kt.SkillAfter + ", fidelity " + kt.Fidelity + ", trust " + trust + ", value for gathering at a wood stand " + before + " -> " + after
                + ", entries copied " + kt.EntriesCopied);
            Assert.AreEqual(fidelity, kt.Fidelity);
            Assert.AreEqual(0, kt.SkillBefore);
            Assert.AreEqual(tu.TeachSkillGain * fidelity / 1000, kt.SkillAfter, "skill rise = TeachSkillGain x fidelity / 1000");
            Assert.AreEqual(kt.SkillAfter, Skills.Of(w.S, learner.Id, SkillClass.Woodcutting));
            Assert.AreEqual(expectValue, after, "copied value = teacher x fidelity x trust factor");
            Assert.AreEqual(Origin.Imitated, origin);
            Assert.AreEqual(0, ValueOf(learner, Feature.Class(ObjClass.BerryBush), Verb.Eat, out _), "another topic is not copied");
            Assert.IsTrue(w.Of<TeachingSession>().Any(t => t.Learner == learner.Id && t.Ticks >= tu.TeachTicks));
            Assert.IsEmpty(SimInvariants.Check(w.S));
        }

        /// <summary>SVC-EDU-U1: only associations the teacher holds are copied (origin Imitated); repeated lessons never lift the learner's skill to the teacher's.</summary>
        [Test]
        public void Teach_CopiesOnlyHeld()
        {
            var (w, teacher, learner) = Lesson(22, 1500, teacherSkill: 400);
            var held = teacher.Mind.Store;
            var teacherKeys = new HashSet<(uint, byte)>();
            for (int i = 0; i < held.Count; i++) if (held.Entries[i].Verb == Verb.Gather) teacherKeys.Add((held.Entries[i].Key, held.Entries[i].Verb));
            var teacherBefore = Enumerable.Range(0, held.Count).Select(i => held.Entries[i]).ToArray();
            for (int lesson = 0; lesson < 5; lesson++)
            {
                var kt = RunLesson(w, teacher, learner);
                Assert.IsNotNull(kt, "lesson " + lesson);
                w.Log.Clear();
                teacher.Mind.Orgs.Health.OfferedTick = 0; teacher.Mind.Orgs.Health.Teaching = false;
            }
            var ls = learner.Mind.Store;
            for (int i = 0; i < ls.Count; i++)
                if (ls.Entries[i].Origin == Origin.Imitated) Assert.IsTrue(teacherKeys.Contains((ls.Entries[i].Key, ls.Entries[i].Verb)), "imitated entry the teacher holds");
            Assert.IsTrue(Enumerable.Range(0, ls.Count).Any(i => ls.Entries[i].Origin == Origin.Imitated));
            int skill = Skills.Of(w.S, learner.Id, SkillClass.Woodcutting);
            TestContext.WriteLine("learner skill after 5 lessons " + skill + " (teacher 400)");
            Assert.Less(skill, 400, "never reaches the teacher's skill through teaching");
            Assert.AreEqual(399, skill, "capped just below the teacher");
            // the teacher's own store is unchanged by teaching (the copy reads it only); its later own learning may still move it, so compare the copied entry only
            Assert.AreEqual((short)1500, teacherBefore.First(e => e.Key == WoodKey && e.Verb == Verb.Gather).Ltw);
        }

        /// <summary>SVC-EDU-2: a teacher with a wrong (negative) association passes it on with its sign.</summary>
        [Test]
        public void Teach_PassesMistakes()
        {
            var (w, teacher, learner) = Lesson(23, -1800);
            var kt = RunLesson(w, teacher, learner);
            Assert.IsNotNull(kt);
            short v = ValueOf(learner, WoodKey, Verb.Gather, out var origin);
            TestContext.WriteLine("teacher -1800, learner " + v);
            Assert.Less(v, 0, "the mistake is copied with the teacher's sign");
            Assert.AreEqual(Origin.Imitated, origin);
        }

        /// <summary>Demonstration: a person in sight who attends gets the demonstrator's values as Vicarious entries (no skill).</summary>
        [Test]
        public void Demonstrate_GivesVicariousEntries()
        {
            var (w, teacher, learner) = Lesson(24, 2000);
            SocialFixtures.SetRelation(w.Sim, teacher, learner, 0, 0); // a stranger to the teacher: shown, not taught
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            teacher.Action = new NpcAction { Verb = C7Verb.Teach, TargetKind = IntentTarget.Node, TargetNode = teacher.Node, Since = w.S.Tick };
            var c = new Candidate { Verb = C7Verb.Teach, Kind = IntentTarget.Node, TargetNode = teacher.Node, Class = (byte)(Education.DemoBase + (int)SkillClass.Woodcutting) };
            PopulationExtensions.TryStart(w.S, ev, teacher, in c, 0);
            w.Log.AddRange(ev.Drain());
            for (int i = 0; i < 3000 && !w.Of<KnowledgeTransferred>().Any(); i++) w.Step(1);
            var kt = w.Of<KnowledgeTransferred>().FirstOrDefault();
            Assert.IsNotNull(kt, "someone attended the demonstration");
            Assert.AreEqual(TeachingSession.Demonstrate, kt.Kind);
            Assert.AreEqual(kt.SkillBefore, kt.SkillAfter, "a demonstration gives no skill");
            ValueOf(learner, WoodKey, Verb.Gather, out var origin);
            Assert.AreEqual(Origin.Vicarious, origin);
        }

        /// <summary>C7-T3: Imitation.cs uses only public members of AssociationStore and Learner and never touches culture.</summary>
        [Test]
        public void Imitation_PublicApiOnly()
        {
            string path = System.IO.Path.Combine(GoodsTests.RepoRoot(), "Assets", "God", "Sim", "Learning", "Imitation.cs");
            string src = System.IO.File.ReadAllText(path);
            foreach (var bad in new[] { "RemoveAt(", "Culture", "internal ", "Reflection", "Traits" })
                Assert.IsFalse(src.Contains(bad), "Imitation.cs uses " + bad);
            foreach (var m in new[] { "Find", "InsertAt" })
                Assert.IsTrue(typeof(AssociationStore).GetMethod(m).IsPublic);
            Assert.IsTrue(typeof(Learner).GetMethod(nameof(Learner.EvictOne)).IsPublic);
            Assert.IsTrue(typeof(AssociationStore).GetField(nameof(AssociationStore.Entries)).IsPublic);
        }

        /// <summary>
        /// SVC-EDU-1 (behaviour): over 20 seeds a taught learner gathers logs faster (logs in the first 600 ticks; over two days
        /// both fill their carry) than the same learner, who had learned from its own experience that felling was not worth it,
        /// untaught (the same game without the lesson): its copied value makes it choose to fell wood more often. Mann-Whitney
        /// reported; the skill itself does not change the felling speed (M3 has no skill input: recorded).
        /// </summary>
        [Test]
        public void Scenario6_TaughtGathersMoreLogs_20Seeds()
        {
            var taught = new int[20]; var control = new int[20]; var taught2 = new int[20]; var control2 = new int[20];
            for (int k = 0; k < 20; k++)
            {
                foreach (bool teach in new[] { true, false })
                {
                    var (w, teacher, learner) = Lesson((ulong)(100 + k), 2000, teach: teach);
                    LearningFixtures.Bush(w.Sim, SourceKind.Wood, At(w, 2, 3), 400, 400, 400);
                    HouseholdFixtures.Give(w.Sim, learner, Ware.Axe, 1);
                    Assoc(learner, WoodKey, Verb.Gather, -1500); // its own experience: felling seemed not worth it (both arms)
                    if (teach) Assert.IsNotNull(RunLesson(w, teacher, learner), "lesson on seed " + k);
                    else w.Step(1); // the control: the same game, nobody teaches
                    HouseholdFixtures.Kill(w.Sim, teacher.Id, DeathCause.Accident); // the teacher's own felling never counts
                    long from = w.S.Tick;
                    w.Log.Clear();
                    w.Step(2 * SimCalendar.Of(w.S).TicksPerDay);
                    var logEvents = w.Of<Produced>().Where(p => p.Npc == learner.Id && p.Ware == Ware.Log).ToList();
                    int logs = logEvents.Where(p => p.Tick < from + 600).Sum(p => p.Count);
                    if (teach) { taught[k] = logs; taught2[k] = logEvents.Sum(p => p.Count); } else { control[k] = logs; control2[k] = logEvents.Sum(p => p.Count); }
                }
            }
            double p = RankSumP(control, taught);
            TestContext.WriteLine("logs in the first 600 ticks (rate), taught:  " + string.Join(" ", taught) + " (mean " + taught.Average().ToString("0.00") + ")");
            TestContext.WriteLine("logs in the first 600 ticks (rate), control: " + string.Join(" ", control) + " (mean " + control.Average().ToString("0.00") + "); one-sided Mann-Whitney p = " + p.ToString("0.0000"));
            TestContext.WriteLine("logs in 2 days (carry-capped), taught " + string.Join(" ", taught2) + " / control " + string.Join(" ", control2) + ", p = " + RankSumP(control2, taught2).ToString("0.0000"));
            Assert.Less(p, 0.05, "taught learners gather more logs (SVC-EDU-1)");
        }

        /// <summary>One-sided Mann-Whitney U (b greater than a), normal approximation with ties.</summary>
        internal static double RankSumP(int[] a, int[] b)
        {
            var all = a.Select(v => (v, g: 0)).Concat(b.Select(v => (v, g: 1))).OrderBy(x => x.v).ToArray();
            var ranks = new double[all.Length];
            for (int i = 0; i < all.Length;)
            {
                int j = i; while (j < all.Length && all[j].v == all[i].v) j++;
                for (int k = i; k < j; k++) ranks[k] = (i + j + 1) / 2.0;
                i = j;
            }
            double rb = 0; for (int i = 0; i < all.Length; i++) if (all[i].g == 1) rb += ranks[i];
            double n1 = a.Length, n2 = b.Length, u = rb - n2 * (n2 + 1) / 2, mu = n1 * n2 / 2, sd = Math.Sqrt(n1 * n2 * (n1 + n2 + 1) / 12.0);
            if (sd == 0) return 1;
            double z = (u - mu - 0.5) / sd;
            return 0.5 * Erfc(z / Math.Sqrt(2));
        }

        private static double Erfc(double x)
        {
            double t = 1 / (1 + 0.5 * Math.Abs(x));
            double y = t * Math.Exp(-x * x - 1.26551223 + t * (1.00002368 + t * (0.37409196 + t * (0.09678418 + t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 + t * (1.48851587 + t * (-0.82215223 + t * 0.17087277)))))))));
            return x >= 0 ? y : 2 - y;
        }

        /// <summary>Runs one act through the population's dispatch until it ends.</summary>
        internal static void RunAct(W w, Npc n, byte verb, byte sub, IntentTarget kind, int id, int node, int known = 0, int max = 3000)
        {
            if (known != 0)
            {
                var m = n.Mind.Orgs;
                m.AddKnown(new KnownRequest { Kind = ServiceKind.Treat, Requester = known, Tick = (int)w.S.Tick, SeenNeed = 2 });
            }
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            n.Action = new NpcAction { Verb = verb, TargetKind = kind, TargetId = id, TargetNode = node, Since = w.S.Tick };
            var c = new Candidate { Verb = verb, Kind = kind, TargetId = id, TargetNode = node, Class = sub };
            PopulationExtensions.TryStart(w.S, ev, n, in c, 0);
            w.Log.AddRange(ev.Drain());
            // the act's own ticks, called as the population calls them (the sim's clock does not move: like C6's fixtures)
            for (int i = 0; i < max && n.Action.Verb == verb; i++) { ev.CurrentTick = w.S.Tick; PopulationExtensions.TryAct(w.S, ev, n, 0); }
            w.Log.AddRange(ev.Drain());
        }
    }
}
