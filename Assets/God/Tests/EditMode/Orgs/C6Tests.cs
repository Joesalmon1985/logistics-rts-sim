using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Orgs;
using God.Sim.Policy;
using God.Sim.Population;
using NUnit.Framework;
using OrgApi = God.Sim.Orgs.Orgs;

namespace God.Sim.Tests
{
    /// <summary>
    /// C6 (docs/civdiv/DEVELOPMENT_PACKAGES/C6.md section 13): organisations made of people. Founding by member agreement
    /// (proposal, answers, meeting), the ration service through the keeper (C6-T1), records written by people (C6-T2), the
    /// relay recipients (C6-T3), votes counted as seen (C6-T4), no staff no effect (ORG-INV-3), information separation
    /// (ORG-INV-4), no branching on the model (ORG-INV-5), the threshold that never builds (ORG-INV-6 fixture), save/load
    /// parity and replay determinism (ORG-INV-7, C6-T5). Fixtures use the real sim: real people, real gestures, real goods.
    /// </summary>
    public class C6Tests
    {
        private sealed class W
        {
            public GodSim Sim; public GodState S; public int Glade; public List<SimEvent> Log = new List<SimEvent>();
            public InstitutionDirector Director;
            public void Step(int n) { for (int i = 0; i < n; i++) { Sim.Step(); Log.AddRange(Sim.DrainEvents()); } }
            public IEnumerable<T> Of<T>() => Log.OfType<T>();
        }

        private static W World(ulong seed = 5, Action<SimTuning> tune = null, bool director = true)
        {
            var sim = LearningFixtures.Empty(seed, t => { t.Orgs.Enabled = 1; tune?.Invoke(t); });
            var w = new W { Sim = sim, S = sim.State, Glade = LearningFixtures.Glade(sim) };
            if (director) w.Director = InstitutionDirector.Attach(sim);
            return w;
        }

        private static Npc Person(W w, int node, int hunger = 150) => LearningFixtures.Npc(w.Sim, node, InstinctSet.Default, LearningMode.On, hunger: hunger, fatigue: 50);
        private static int At(W w, int dir, int steps) => LearningFixtures.Step(w.Sim, w.Glade, dir, steps);

        private static void Trust(W w, Npc a, Npc b, int t) { SocialFixtures.SetRelation(w.Sim, a, b, t, 500); SocialFixtures.SetRelation(w.Sim, b, a, t, 500); }

        /// <summary>A shared store founded through <see cref="OrgApi.Found"/> by these founders at the glade (the keeper first).</summary>
        private static Organisation Store(W w, params Npc[] founders)
        {
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            var org = OrgApi.Found(w.S, ev, OrgTemplates.SharedFoodStore, founders.Select(f => f.Id).ToList(), w.Glade, 0);
            w.Log.AddRange(ev.Drain());
            return org;
        }

        private static LotBin BinOf(W w, Organisation org) => Goods.BinById(w.S.Population, org.Stores[0]);

        private static int Food(Npc n) => n.Carry.Where(l => !l.Empty && Wares.IsFood(l.Ware)).Sum(l => l.Count);

        /// <summary>Runs one C6 act through the Orgs dispatch (the intent set as a decision would set it) until it ends.</summary>
        private static void Act(W w, Npc n, byte verb, byte sub, IntentTarget kind, int id, int node, int max = 2000)
        {
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            n.Action = new NpcAction { Verb = verb, TargetKind = kind, TargetId = id, TargetNode = node, Since = w.S.Tick };
            var c = new Candidate { Verb = verb, Kind = kind, TargetId = id, TargetNode = node, Class = sub };
            Assert.IsTrue(OrgHooks.TryStart(w.S, ev, n, in c, 0), "the Orgs dispatch takes C6's verbs");
            for (int i = 0; i < max && n.Action.Verb == verb; i++) { ev.CurrentTick = w.S.Tick; OrgHooks.TryAct(w.S, ev, n, 0); }
            w.Log.AddRange(ev.Drain());
        }

        // ---------------------------------------------------------------- founding by member agreement (ORGANISATIONS 4.2)

        /// <summary>
        /// Two people of different households who trust each other, both of whom lived through the need and know the practice:
        /// the proposer asks, the other accepts by its own scores, the proposer calls the meeting, the other accepts again, and
        /// only then is the store founded (an empty bin owned by the organisation; both are members; offices filled by them).
        /// </summary>
        [Test]
        public void Founding_ByMemberAgreement()
        {
            var w = World();
            var a = Person(w, w.Glade); var b = Person(w, At(w, 0, 1));
            Trust(w, a, b, 600);
            foreach (var (n, other) in new[] { (a, b), (b, a) })
            {
                var m = n.Mind.Orgs = OrgApi.NewMind(w.S);
                m.Practices = 1 << OrgTemplates.SharedFoodStore;
                m.Opportunities[0] = new OpportunityBelief { Template = OrgTemplates.SharedFoodStore, NeedEvidence = 9, S0 = other.Id, LastSignalDay = 0, LastSignalA = -1, LastSignalB = -1 };
                m.OppCount = 1;
            }
            w.Step(3000);
            var founded = w.Of<OrgFounded>().ToList();
            Assert.AreEqual(1, founded.Count, "one store founded by the two");
            CollectionAssert.AreEquivalent(new[] { a.Id, b.Id }, founded[0].Founders);
            var org = w.S.Organisations.Orgs[0];
            Assert.AreEqual(2, org.Roles.Count);
            var bin = BinOf(w, org);
            Assert.AreEqual(BinKind.CommunalStore, bin.Kind);
            Assert.IsTrue(bin.Owner.Same(OrgApi.OwnerOf(org.Id)));
            var asks = w.Of<Social.GestureMade>().Where(g => g.Act == GestureAct.AskJoin && g.TopicKind == TopicKind.Org && g.TopicId == 0).ToList();
            var accepts = w.Of<Social.GestureMade>().Where(g => g.Act == GestureAct.Accept && g.TopicKind == TopicKind.Org && g.TopicId == 0 && g.Tick <= founded[0].Tick).ToList();
            Assert.IsTrue(asks.Any(g => g.Time != MsgTime.Now) && asks.Any(g => g.Time == MsgTime.Now), "a proposal and a meeting call were gestured");
            Assert.GreaterOrEqual(accepts.Count, 2, "accepted twice (the answer and the meeting)");
            foreach (var n in new[] { a, b }) Assert.AreEqual((byte)RoleKind.Member, n.Mind.Orgs.Beliefs[n.Mind.Orgs.IndexOfOrg(org.Id)].MyRole, "each founder believes it is a member");
            Assert.IsEmpty(SimInvariants.Check(w.S));
        }

        /// <summary>ORG-INV-6 (fixture): the template is available, the two trust each other, but neither knows the practice nor lived the need: nothing is founded.</summary>
        [Test]
        public void ThresholdNeverBuilds_Fixture()
        {
            var w = World();
            var a = Person(w, w.Glade, hunger: 700); var b = Person(w, At(w, 0, 1));
            SocialFixtures.Give(w.Sim, b, Ware.Berries, 4);
            Trust(w, a, b, 600);
            w.Step(6000);
            Assert.IsEmpty(w.Of<OrgFounded>());
            Assert.IsEmpty(w.S.Organisations.Orgs);
            Assert.IsNull(a.Mind.Orgs, "nothing organisational entered the mind");
        }

        // ---------------------------------------------------------------- C6-T1 ration through the keeper

        [Test]
        public void Ration_ThroughKeeper()
        {
            var w = World();
            var keeper = Person(w, w.Glade); var member = Person(w, At(w, 0, 1), hunger: 700); var stranger = Person(w, At(w, 3, 1), hunger: 700);
            var org = Store(w, keeper, member);
            Assert.IsTrue(OrgRecords.MayWrite(org, member.Id), "the second founder keeps the records");
            // a deposit by the keeper (its own berries) through the act
            SocialFixtures.Give(w.Sim, keeper, Ware.Berries, 6);
            Act(w, keeper, OrgVerb.OrgWork, OrgVerb.Deposit, IntentTarget.Bin, org.Stores[0], w.Glade);
            Assert.AreEqual(5, CommunalStore.FoodIn(BinOf(w, org)), "deposited all but one");
            Assert.IsTrue(BinOf(w, org).Lots.All(l => l.Owner.Same(OrgApi.OwnerOf(org.Id))), "deposits became the organisation's (basis OrgContribution)");
            Assert.IsTrue(w.Of<OwnershipChanged>().Any(o => o.Basis == OwnershipBasis.OrgContribution));

            // the member gestures its need at the keeper; the keeper perceives it at its think and the request is opened by it
            Act(w, member, OrgVerb.RequestService, OrgVerb.ActRequest, IntentTarget.Npc, keeper.Id, keeper.Node);
            for (int i = 0; i < 40 && w.Of<ServiceRequested>().Count() == 0; i++) w.Step(1);
            var req = w.Of<ServiceRequested>().Single();
            Assert.AreEqual(keeper.Id, req.ReceivedBy, "written into the ring by the keeper who perceived it");
            // the keeper decides by its own scores to serve the request it knows of (nothing forces it)
            for (int i = 0; i < 400 && !w.Of<WelfareGranted>().Any(); i++) w.Step(1);
            var grant = w.Of<WelfareGranted>().Single();
            Assert.AreEqual(member.Id, grant.Recipient); Assert.AreEqual(keeper.Id, grant.Keeper); Assert.AreEqual(2, grant.Count, "the charter's ration");
            Assert.AreEqual(2, Food(member), "the member holds the ration");
            Assert.IsTrue(w.Of<ItemMoved>().Any(m => m.Reason == MoveReason.HandOver && m.Actor == keeper.Id && m.ToId == member.Id && m.OwnerBeforeKind == OwnerKind.Org), "handed over within reach by the keeper");
            Assert.IsTrue(w.Of<ServiceDelivered>().Any(d => d.Request == req.Request && d.OutcomeEvent == 551));

            // a non-member asking is refused under MembersByRation (members only)
            var mk = keeper.Mind.Orgs;
            mk.AddKnown(new KnownRequest { Org = org.Id, Kind = ServiceKind.Ration, Requester = stranger.Id, Tick = (int)w.S.Tick, SeenNeed = 1 });
            Act(w, keeper, OrgVerb.OrgWork, OrgVerb.Serve, IntentTarget.Npc, stranger.Id, stranger.Node);
            Assert.AreEqual(1, w.Of<ServiceRefused>().Count(r => r.ReasonSeen == ServiceRefused.NotMember));
            Assert.AreEqual(0, Food(stranger));

            // with no keeper alive no ration is ever given
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            PopulationModule.Kill(w.S, ev, keeper, DeathCause.Accident);
            w.Log.AddRange(ev.Drain());
            int before = w.Of<WelfareGranted>().Count();
            member.Drives[Drive.Hunger] = 800 * 1000;
            w.Step(3000);
            Assert.AreEqual(before, w.Of<WelfareGranted>().Count(), "no keeper, no ration");
            Assert.IsEmpty(SimInvariants.Check(w.S));
        }

        // ---------------------------------------------------------------- C6-T2 records written by people

        [Test]
        public void Records_AreWrittenByPeople()
        {
            var w = World();
            var keeper = Person(w, w.Glade); var recorder = Person(w, At(w, 0, 1));
            var org = Store(w, keeper, recorder);
            int[] stock = new int[Wares.PersonalCount];
            // the recorder sees the first deposit
            SocialFixtures.Give(w.Sim, keeper, Ware.Berries, 4);
            Act(w, keeper, OrgVerb.OrgWork, OrgVerb.Deposit, IntentTarget.Bin, org.Stores[0], w.Glade);
            Assert.AreEqual(1, recorder.Mind.Orgs.WitnessCount);
            // the second deposit happens while the recorder is far away (out of its sight): it never sees it
            recorder.Node = At(w, 3, 7); recorder.Node = LearningFixtures.Step(w.Sim, recorder.Node, 3, 8);
            SocialFixtures.Give(w.Sim, keeper, Ware.Roots, 3);
            Act(w, keeper, OrgVerb.OrgWork, OrgVerb.Deposit, IntentTarget.Bin, org.Stores[0], w.Glade);
            Assert.AreEqual(1, recorder.Mind.Orgs.WitnessCount, "a deposit out of sight is not witnessed");
            OrgRecords.RecordedStock(org, stock);
            Assert.AreEqual(0, stock.Sum(), "nothing written yet: the organisation's view is stale");
            Act(w, recorder, OrgVerb.WriteRecord, OrgVerb.ActRecord, IntentTarget.Bin, org.Stores[0], w.Glade);
            var rec = w.Of<RecordWritten>().ToList();
            Assert.AreEqual(1, rec.Count); Assert.AreEqual(recorder.Id, rec[0].Recorder);
            OrgRecords.RecordedStock(org, stock);
            Assert.AreEqual(3, stock[(int)Ware.Berries]); Assert.AreEqual(0, stock[(int)Ware.Roots], "the unseen deposit is missing from the record");
            Assert.AreEqual(3 + 3, CommunalStore.FoodIn(BinOf(w, org)), "the bin holds both deposits (the second: its kept berry and two roots)");

            // with no recorder office filled, nothing is written
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            OrgApi.SetOffice(w.S, ev, org, 1, 0, OfficeFilling.None);
            SocialFixtures.Give(w.Sim, keeper, Ware.Berries, 3);
            Act(w, keeper, OrgVerb.OrgWork, OrgVerb.Deposit, IntentTarget.Bin, org.Stores[0], w.Glade);
            recorder.Node = w.Glade;
            int n = OrgRecords.Write(w.S, ev, org, recorder);
            Assert.AreEqual(0, n);
            Assert.AreEqual(1, w.Of<RecordWritten>().Count() + ev.Items.OfType<RecordWritten>().Count());
        }

        // ---------------------------------------------------------------- C6-T3 relay recipients

        [Test]
        public void RelayRecipients_Rule()
        {
            var w = World();
            var k = Person(w, w.Glade); var r = Person(w, At(w, 0, 1)); var m1 = Person(w, At(w, 1, 1)); var m2 = Person(w, At(w, 2, 1));
            var org = Store(w, k, r);
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            OrgApi.AddRole(w.S, ev, org, m1.Id, RoleKind.Member, RoleBasis.Gesture);
            OrgApi.AddRole(w.S, ev, org, m2.Id, RoleKind.Member, RoleBasis.Gesture);
            var into = new List<int>();
            OrgApi.RelayRecipients(w.S, org.Id, OrgPower.Spend, false, into);
            CollectionAssert.AreEqual(new[] { k.Id }, into, "only the keeper's office has Spend");
            OrgApi.RelayRecipients(w.S, org.Id, OrgPower.Propose, false, into);
            CollectionAssert.AreEqual(new[] { k.Id, r.Id }.OrderBy(x => x).ToArray(), into, "office-holders with Propose, ascending");
            // a meeting rule: plus the members present at the meeting place; an absent member is not a recipient
            m2.Node = LearningFixtures.Step(w.Sim, LearningFixtures.Step(w.Sim, w.Glade, 4, 8), 4, 8);
            OrgApi.RelayRecipients(w.S, org.Id, OrgPower.Spend, true, into);
            CollectionAssert.AreEqual(new[] { k.Id, r.Id, m1.Id }.OrderBy(x => x).ToArray(), into);
            Assert.IsFalse(into.Contains(m2.Id));
        }

        // ---------------------------------------------------------------- C6-T4 votes counted as seen

        private static (W w, Organisation org, Npc keeper, List<Npc> voters) Meeting(int fed, int hungry)
        {
            var w = World(director: false);
            var keeper = Person(w, w.Glade); var recorder = Person(w, At(w, 0, 1), hunger: 700);
            var org = Store(w, keeper, recorder);
            var voters = new List<Npc> { recorder };
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            for (int i = 0; i < fed + hungry - 1; i++)
            {
                var v = Person(w, At(w, 1 + i % 5, 1), hunger: i < fed ? 100 : 700);
                if (i < fed) SocialFixtures.Give(w.Sim, v, Ware.Berries, 3);
                OrgApi.AddRole(w.S, ev, org, v.Id, RoleKind.Member, RoleBasis.Gesture);
                voters.Add(v);
            }
            if (fed + hungry - 1 < 0) { }
            return (w, org, keeper, voters);
        }

        private static void Propose(W w, Organisation org, Npc proposer, int value)
        {
            int day = (int)SimCalendar.Of(w.S).Day(w.S.Tick);
            org.Proposals.Add(new OrgProposal { Id = org.NextProposalId++, Proposer = proposer.Id, Source = DecisionSource.Member, What = DecisionWhat.SetPolicy, A0 = (int)OrgPolicyKey.RationPerRequest, A1 = value, OpenedDay = day - 1 });
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            OrgMeetings.Convene(w.S, ev, org);
            w.Log.AddRange(ev.Drain());
        }

        [Test]
        public void Vote_CountedAsSeen()
        {
            // 3 present: the proposer (keeper) and the hungry recorder accept a larger ration, a fed member with food refuses: adopted 2-1
            var (w, org, keeper, voters) = Meeting(fed: 1, hungry: 1);
            Assert.AreEqual(3, org.Roles.Count);
            Propose(w, org, keeper, 3);
            var d = w.Of<OrgDecisionTaken>().Single();
            Assert.AreEqual(DecisionOutcome.Adopted, d.Outcome); Assert.AreEqual(2, d.Accept); Assert.AreEqual(1, d.Refuse); Assert.AreEqual(0, d.Absent);
            Assert.AreEqual(voters[0].Id, d.Counter, "the recorder counts");
            Assert.AreEqual(3, org.PolicyOf(OrgPolicyKey.RationPerRequest));
            Assert.AreEqual(3, w.Of<Social.GestureMade>().Count(g => g.TopicKind == TopicKind.Org && g.TopicId == org.Id && (g.Act == GestureAct.Accept || g.Act == GestureAct.Refuse)), "every present member gestured its vote");

            // a tie keeps the old state: proposer and the hungry recorder accept, two fed members refuse
            var (w2, org2, k2, _) = Meeting(fed: 2, hungry: 1);
            Propose(w2, org2, k2, 3);
            var d2 = w2.Of<OrgDecisionTaken>().Single();
            Assert.AreEqual(DecisionOutcome.Rejected, d2.Outcome); Assert.AreEqual(2, d2.Accept); Assert.AreEqual(2, d2.Refuse);
            Assert.AreEqual(2, org2.PolicyOf(OrgPolicyKey.RationPerRequest), "the tie kept the old ration");

            // below quorum it lapses: only the proposer is present
            var (w3, org3, k3, v3) = Meeting(fed: 0, hungry: 1);
            foreach (var v in v3) v.Node = LearningFixtures.Step(w3.Sim, LearningFixtures.Step(w3.Sim, w3.Glade, 4, 8), 4, 8);
            Propose(w3, org3, k3, 3);
            var d3 = w3.Of<OrgDecisionTaken>().Single();
            Assert.AreEqual(DecisionOutcome.Lapsed, d3.Outcome);
            Assert.AreEqual(2, org3.PolicyOf(OrgPolicyKey.RationPerRequest));
        }

        // ---------------------------------------------------------------- decisions through the director (HeadDecides)

        /// <summary>The keeper dies: at the next decision slot the recorder (Propose) proposes a member for the vacant keeper office; with no DecideAlone it waits for the meeting; RuleFollower never changes a policy.</summary>
        [Test]
        public void Director_FillsVacancy_ByLivingDecider()
        {
            var w = World();
            var keeper = Person(w, w.Glade); var recorder = Person(w, At(w, 0, 1)); var m1 = Person(w, At(w, 1, 1));
            var org = Store(w, keeper, recorder);
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            OrgApi.AddRole(w.S, ev, org, m1.Id, RoleKind.Member, RoleBasis.Gesture);
            Trust(w, recorder, m1, 700);
            PopulationModule.Kill(w.S, ev, keeper, DeathCause.Accident);
            w.Log.AddRange(ev.Drain());
            Assert.AreEqual(0, org.Offices[0].Holder, "death ended the keeper's office");
            Assert.IsTrue(w.Of<MemberLeft>().Any(l => l.Npc == keeper.Id && l.Reason == MemberLeft.Died));
            w.Step(2 * SimCalendar.Of(w.S).TicksPerDay + 50);
            var ds = w.Of<OrgDecisionTaken>().ToList();
            Assert.IsNotEmpty(ds, "a living decider decided");
            foreach (var d in ds) Assert.IsTrue(d.Proposer == recorder.Id, "every decision names the living office-holder who proposed it");
            var adopted = ds.FirstOrDefault(d => d.What == DecisionWhat.Appoint && d.Outcome == DecisionOutcome.Adopted);
            if (adopted != null) Assert.AreEqual(m1.Id, org.Offices[0].Holder, "the trusted member fills the keeper's office");
        }

        // ---------------------------------------------------------------- ORG-INV-3 no staff, no effect

        [Test]
        public void NoStaff_NoEffect()
        {
            var w = World();
            var keeper = Person(w, w.Glade); var recorder = Person(w, At(w, 0, 1));
            var org = Store(w, keeper, recorder);
            SocialFixtures.Give(w.Sim, keeper, Ware.Berries, 5);
            Act(w, keeper, OrgVerb.OrgWork, OrgVerb.Deposit, IntentTarget.Bin, org.Stores[0], w.Glade);
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            PopulationModule.Kill(w.S, ev, keeper, DeathCause.Accident);
            PopulationModule.Kill(w.S, ev, recorder, DeathCause.Accident);
            w.Log.AddRange(ev.Drain());
            long after = w.S.Tick;
            Assert.AreEqual(OrgStatus.Dissolved, org.Status, "the last member gone: dissolved by its rule");
            var hungry = Person(w, At(w, 2, 1), hunger: 800);
            w.Step(10 * SimCalendar.Of(w.S).TicksPerDay);
            Assert.IsEmpty(w.Log.Where(e => e.Tick > after && (e is ServiceDelivered || e is OrgSpent || e is WelfareGranted || e is OrgDecisionTaken || e is ServiceRequested)));
            Assert.IsTrue(BinOf(w, org).Lots.All(l => !l.Owner.Same(OrgApi.OwnerOf(org.Id))), "its goods were re-owned in place by the dissolution rule");
            Assert.IsEmpty(SimInvariants.Check(w.S));
        }

        // ---------------------------------------------------------------- ORG-INV-4, ORG-INV-5 (static)

        [Test]
        public void InformationSeparation_Reflection()
        {
            bool Refs(Type t, Type bad) => t == bad || t.IsGenericType && t.GetGenericArguments().Any(a => Refs(a, bad)) || t.IsArray && Refs(t.GetElementType(), bad);
            foreach (var f in typeof(InstitutionView).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                Assert.IsFalse(Refs(f.FieldType, typeof(GodState)) || f.FieldType == typeof(Organisation) || f.FieldType == typeof(OrgState), "InstitutionView." + f.Name);
            foreach (var t in new[] { typeof(MemberUtilityPolicy), typeof(RuleFollowerPolicy), typeof(InstitutionPolicies) })
            {
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Assert.IsFalse(Refs(m.ReturnType, typeof(GodState)), t.Name + "." + m.Name);
                    foreach (var p in m.GetParameters()) Assert.IsFalse(Refs(p.ParameterType, typeof(GodState)) || p.ParameterType == typeof(Organisation), t.Name + "." + m.Name + "(" + p.Name + ")");
                    var body = m.GetMethodBody();
                    if (body != null) foreach (var l in body.LocalVariables) Assert.IsFalse(Refs(l.LocalType, typeof(GodState)) || l.LocalType == typeof(Organisation), t.Name + "." + m.Name + " local");
                }
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) Assert.IsFalse(Refs(f.FieldType, typeof(GodState)), t.Name + "." + f.Name);
            }
            string root = GoodsTests.RepoRoot();
            string pol = File.ReadAllText(Path.Combine(root, "Assets", "God", "Sim", "Policy", "InstitutionPolicies.cs"));
            foreach (var word in new[] { "GodState", "Warehouse", "Sqlite", "TrueNeed", "TrueOutcome", "Falsified", "Organisations" }) StringAssert.DoesNotContain(word, pol);
        }

        [Test]
        public void NoBranchOnModel_Static()
        {
            string root = GoodsTests.RepoRoot();
            foreach (var f in new[] { "Services.cs", "CommunalStore.cs" })
            {
                string text = File.ReadAllText(Path.Combine(root, "Assets", "God", "Sim", "Orgs", f));
                StringAssert.DoesNotContain("OwnershipModel", text, f);
                StringAssert.DoesNotContain("OwnerKind.Org", text, f);
            }
        }

        // ---------------------------------------------------------------- ORG-INV-7 / C6-T5 determinism and save/load

        private static W Active(ulong seed)
        {
            var w = World(seed);
            var keeper = Person(w, w.Glade); var member = Person(w, At(w, 0, 1), hunger: 700); var m2 = Person(w, At(w, 1, 1), hunger: 600);
            var org = Store(w, keeper, member);
            var ev = new EventBuffer { CurrentTick = w.S.Tick };
            OrgApi.AddRole(w.S, ev, org, m2.Id, RoleKind.Member, RoleBasis.Gesture);
            SocialFixtures.Give(w.Sim, keeper, Ware.Berries, 6);
            Act(w, keeper, OrgVerb.OrgWork, OrgVerb.Deposit, IntentTarget.Bin, org.Stores[0], w.Glade);
            // an open request queued (perceived by the keeper) and a pending MajorityPresent proposal
            keeper.Mind.Orgs.AddKnown(new KnownRequest { Org = org.Id, Kind = ServiceKind.Ration, Requester = member.Id, Tick = (int)w.S.Tick, SeenNeed = 2 });
            w.Sim.Submit(new OrgSetPolicy { Org = org.Id, Actor = member.Id, Key = OrgPolicyKey.RationPerRequest, Value = 3 });
            w.Step(3);
            Assert.AreEqual(1, org.Proposals.Count, "a pending proposal");
            Assert.IsTrue(org.Requests.Any(r => r.Status == RequestStatus.Waiting), "an open request");
            return w;
        }

        [Test]
        public void C6_SaveLoadParity_AndReplay()
        {
            var a = Active(9); var b = Active(9);
            Assert.AreEqual(a.Sim.StateHash(), b.Sim.StateHash(), "replay: the same setup gives the same state");
            string json = SaveFile.From(a.Sim).ToJson();
            var loaded = SaveFile.FromJson(json).ToSim();
            InstitutionDirector.Attach(loaded);
            Assert.AreEqual(a.Sim.StateHash(), loaded.StateHash());
            Assert.AreEqual(json, SaveFile.From(loaded).ToJson(), "a second save is identical");
            for (int k = 0; k < 6; k++)
            {
                a.Sim.Step(500); loaded.Step(500); b.Sim.Step(500);
                Assert.AreEqual(a.Sim.StateHash(), loaded.StateHash(), "save/load continuation at checkpoint " + k);
                Assert.AreEqual(a.Sim.StateHash(), b.Sim.StateHash(), "replay at checkpoint " + k);
            }
            Assert.IsEmpty(SimInvariants.Check(loaded.State));
        }

        // ---------------------------------------------------------------- permission belief (Permits OrgRole)

        [Test]
        public void Permits_OrgRole_Belief()
        {
            var w = World();
            var keeper = Person(w, w.Glade); var member = Person(w, At(w, 0, 1)); var stranger = Person(w, At(w, 1, 1));
            var org = Store(w, keeper, member);
            ThinkScratch Sc(Npc n) { var sc = new ThinkScratch { Mind = n.Mind }; sc.Self.Id = n.Id; sc.Self.Household = n.Household; return sc; }
            var owner = OrgApi.OwnerOf(org.Id);
            Assert.AreEqual(PermissionBasis.OrgRole, Permits.Believed(Sc(keeper), owner).Basis, "the keeper believes it may take from the store");
            Assert.IsFalse(Permits.Believed(Sc(member), owner).Allowed, "a member under MembersByRation is handed rations, it does not take");
            Assert.IsFalse(Permits.Believed(Sc(stranger), owner).Allowed);
            Assert.IsTrue(OrgApi.Truth(w.S, keeper, owner).Allowed);
            Assert.IsFalse(OrgApi.Truth(w.S, member, owner).Allowed);
        }

        [Test]
        public void OrgCard_FillsRolesAndBeliefs()
        {
            var w = World();
            var keeper = Person(w, w.Glade); var member = Person(w, At(w, 0, 1));
            var org = Store(w, keeper, member);
            var card = new Information.NpcInspection();
            Information.OrgCards.Fill(w.S, keeper, card);
            Assert.IsTrue(card.Orgs.Rows.Any(r => r.Kind == Information.OrgCardRow.Role && r.Layer == Information.KnowledgeLayer.InstitutionRecord));
            Assert.IsTrue(card.Orgs.Rows.Any(r => r.Kind == Information.OrgCardRow.BelievedMembership && r.Layer == Information.KnowledgeLayer.Belief));
            var oc = Information.OrgCards.CardOf(w.S, org.Id);
            Assert.AreEqual(Information.KnowledgeLayer.InstitutionRecord, oc.Layer);
            Assert.AreEqual(2, oc.Members.Count);
        }
    }
}
