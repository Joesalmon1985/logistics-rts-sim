using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Recording;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Civ;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using God.Sim.Social;
using God.Sim.Tests;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// C3 at run level (docs/civdiv/DEVELOPMENT_PACKAGES/C3.md section 13): end-to-end scenario 3 (an individual operates a
    /// productive asset and compensates another for work with goods handed over within reach, no money) in the
    /// civ_employment scenario and in controlled fixtures. In every fixture each choice (offering, accepting, refusing,
    /// working, paying, walking out, going back, resigning, dismissing) is the person's own policy over its candidates;
    /// the only fixture policy is <see cref="NeverPays"/>, an employer whose own policy never chooses to pay.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class C3RunTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("c3"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        // ================================================================ fixtures

        /// <summary>
        /// The scenario-3 fixture: an empty glade, an owner (its own field: a worked plot giving roots from labour, which it
        /// knows it owns), a hungry neighbour with no asset two nodes away. Optional: what the owner carries, a berry bush
        /// in the neighbour's sight (its foraging alternative).
        /// </summary>
        internal sealed class Fixture
        {
            public M5SocialLab Lab;
            public GodState S => Lab.Sim.State;
            public Npc Owner, Worker;
            public ProductiveAsset Field;

            public Fixture(ulong seed, Action<SimTuning> tune = null, int workerHunger = 500, (Ware w, int n)[] ownerCarries = null, bool bush = false)
            {
                Lab = new M5SocialLab(seed, t => { t.Civ.Enabled = 1; tune?.Invoke(t); });
                Owner = Lab.Add(Lab.Glade, null, LearningMode.On, fed: false);
                Worker = Lab.AddH(Lab.At(0, 2), null, LearningMode.On, false, workerHunger);
                Field = Assets.Register(S, null, AssetKinds.Field, Lab.At(3, 2), God.Sim.Population.Owner.Npc(Owner.Id), OwnershipBasis.Built, Owner.Id);
                Ownership.Observe(S, Owner, Field, God.Sim.Population.Owner.Npc(Owner.Id), 255, 0xFF); // it built it: it knows it is its own
                if (ownerCarries != null) foreach (var (w, n) in ownerCarries) SocialFixtures.Give(Lab.Sim, Owner, w, n);
                if (bush) LearningFixtures.Bush(Lab.Sim, SourceKind.Berries, Lab.At(0, 5));
            }

            public List<T> Of<T>() where T : SimEvent => Lab.Of<T>().ToList();
            public EmploymentContract Contract(int id) => Employment.Find(S, id);

            /// <summary>Steps until <paramref name="until"/> holds (checked every 10 ticks) or <paramref name="max"/> ticks passed.</summary>
            public bool StepUntil(Func<bool> until, int max)
            {
                for (int t = 0; t < max; t += 10) { if (until()) return true; Lab.Step(10); }
                return until();
            }

            public void SetPolicy(Npc n, INpcPolicy p) { Lab.Pop.SetPolicy(p.Name, p); n.Policy = p.Name; }
            public void DefaultPolicy(Npc n) { n.Policy = God.Sim.Population.Policies.Associative; }
        }

        /// <summary>
        /// An employer whose own policy never chooses to pay a wage: it decides as the associative policy does, and when that
        /// would pay it takes its next best option instead. <see cref="Passive"/>: once it has hired, it also never offers
        /// again nor dismisses (it leaves a walk-out alone).
        /// </summary>
        internal sealed class NeverPays : INpcPolicy
        {
            public bool Passive, Hired;
            private bool Excluded(NpcVerb v) => v == Employment.IntentPay || Passive && Hired && (v == Employment.IntentDismiss || v == Employment.IntentOffer);
            private readonly AssociativeNpcPolicy _inner = new AssociativeNpcPolicy();
            private readonly OpportunitySlot[] _opps = new OpportunitySlot[ThinkScratch.MaxCand];
            public string Name => "c3_never_pays";
            public PolicyLevel Level => PolicyLevel.Civilian;

            public NpcIntent Decide(in NpcObservation obs, MindView mind, ref Rng rng)
            {
                var why = default(DecisionRecord);
                var it = _inner.DecideExplained(obs, mind, ref rng, ref why, _opps, out int n);
                if (!Excluded(it.Verb)) return it;
                int best = -1;
                for (int i = 0; i < n; i++)
                {
                    if (Excluded(obs.Candidates[i].Intent)) continue;
                    if (best < 0 || _opps[i].Total > _opps[best].Total) best = i;
                }
                return best < 0 ? NpcIntent.Nothing : AssociativeNpcPolicy.ToIntent(obs.Candidates[best]);
            }
        }

        /// <summary>Keeps the grid of a run (for reach checks on its events).</summary>
        private sealed class GridKeeper : IRunRecorder
        {
            public NodeGrid Grid;
            public GodState Final;
            public void Begin(RunInfo info, GodSim sim) { Grid = sim.State.Territory.Grid; }
            public void OnTick(GodSim sim, IReadOnlyList<SimEvent> events) { }
            public void OnDayEnd(GodSim sim, FactionDayRow[] rows) { }
            public void End(GodSim sim, IDictionary<string, long> counters) { Final = sim.State; }
            public void Dispose() { }
        }

        private static RunSpec Spec(ulong seed, long ticks, CaptureRecorder cap, GridKeeper keep, params string[] sets)
        {
            var spec = TestUtil.Spec("civ_employment", seed, ticks, sets);
            spec.Scenario.Checks.Remove("smoke");
            if (cap != null) spec.ExtraRecorders.Add(() => cap);
            if (keep != null) spec.ExtraRecorders.Add(() => keep);
            return spec;
        }

        /// <summary>
        /// The scenario-3 evidence in one run's events: a contract whose worker produced at the employer's asset under a
        /// use right (the output owned by the asset's owner, never the worker) and was paid by the employer's agent with a
        /// hand-over within reach (WagePaid + ItemMoved HandOver + OwnershipChanged basis Wage, the same tick).
        /// </summary>
        private static int Operated(List<SimEvent> e, NodeGrid g, out int produced, out int paid)
        {
            produced = 0; paid = 0;
            int ok = 0;
            foreach (var k in e.OfType<EmploymentStarted>())
            {
                var used = e.OfType<AssetUsed>().Where(u => u.Worker == k.Worker && u.Asset == k.Asset && u.Right == Ownership.RightUse && u.Tick >= k.Tick).ToList();
                bool outputOwners = used.Count > 0 && used.All(u => !(u.OwnerKind == OwnerKind.Npc && u.OwnerId == k.Worker))
                    && e.OfType<Produced>().Where(p => p.Npc == k.Worker && used.Any(u => u.Tick == p.Tick)).All(p => p.OwnerKind != OwnerKind.Npc || p.OwnerId != k.Worker);
                var wages = e.OfType<WagePaid>().Where(w => w.Contract == k.Contract).ToList();
                bool handed = wages.Count > 0 && wages.All(w =>
                    w.Payer == k.Agent && e.OfType<ItemMoved>().Any(m => m.Tick == w.Tick && m.Reason == MoveReason.HandOver && m.Actor == w.Payer && m.ToKind == HolderKind.Carry && m.ToId == w.Worker
                        && g.Distance(m.ActorNode, m.ToNode) <= Reach.HandOver)
                    && e.OfType<OwnershipChanged>().Any(o => o.Tick == w.Tick && o.Basis == OwnershipBasis.Wage && o.ToKind == OwnerKind.Npc && o.ToId == w.Worker));
                produced += used.Count; paid += wages.Count;
                if (outputOwners && handed) ok++;
            }
            return ok;
        }

        // ================================================================ C3-T1

        /// <summary>C3-T1: civ_employment, 20 seeds x 10 days: on at least one seed an owner hires a non-owner, who produces at the owner's asset (output the owner's) and is paid by hand within reach; invariants (C1 conservation) hold.</summary>
        [Test]
        public void Scenario3_OperateAndCompensate()
        {
            var caps = new List<CaptureRecorder>(); var keeps = new List<GridKeeper>(); var specs = new List<RunSpec>();
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var cap = new CaptureRecorder(); var keep = new GridKeeper();
                caps.Add(cap); keeps.Add(keep); specs.Add(Spec(seed, 12000, cap, keep));
            }
            var res = TestUtil.RunAll(specs);
            int good = 0;
            for (int i = 0; i < res.Length; i++)
            {
                Assert.IsNull(res[i].Error, res[i].Error);
                Assert.AreEqual(0, res[i].ExitCode, "invariants and no exceptions: " + string.Join("; ", res[i].Messages));
                var e = caps[i].Events;
                int ok = Operated(e, keeps[i].Grid, out int produced, out int paid);
                if (ok > 0) good++;
                Assert.IsEmpty(SimInvariants.Check(keeps[i].Final));
                var bad = new List<string>(); Employment.Check(keeps[i].Final, bad); Assert.IsEmpty(bad);
                TestContext.WriteLine("seed " + (i + 1) + ": offers " + e.OfType<GestureMade>().Count(g => g.Act == GestureAct.Offer && g.TopicKind == TopicKind.Posting)
                    + ", refused " + e.OfType<GestureMade>().Count(g => g.Act == GestureAct.Refuse && g.TopicKind == TopicKind.Posting)
                    + ", contracts " + e.OfType<EmploymentStarted>().Count() + ", work units under contract " + produced + ", wages paid " + paid
                    + " (" + e.OfType<WagePaid>().Sum(w => w.Count) + " goods), unpaid units " + e.OfType<WageUnpaid>().Count() + ", walk-outs " + e.OfType<ContractSuspended>().Count()
                    + ", resumed " + e.OfType<ContractResumed>().Count() + ", ended " + string.Join(" ", e.OfType<EmploymentEnded>().GroupBy(x => x.Reason).OrderBy(x => x.Key).Select(x => x.Key + " " + x.Count()))
                    + "; contracts meeting scenario 3: " + ok);
            }
            TestContext.WriteLine("seeds where an owner hired, the worker produced for the owner and was paid within reach: " + good + "/20");
            Assert.GreaterOrEqual(good, 1);
        }

        // ================================================================ ORG-ACC-3 (C3a part)

        /// <summary>
        /// ORG-ACC-3 (C3a): the hired worker's output is the employer's; the employer hands over the wage within reach; an
        /// unpaid unit makes exactly one arrears record (C2 absent: a counter, Obligation 0, one WageUnpaid per unit).
        /// </summary>
        [Test]
        public void Fixture_WorkRight_OutputAndPay()
        {
            int fixtures = 0;
            for (ulong seed = 1; seed <= 5; seed++)
            {
                var f = new Fixture(seed);
                if (!f.StepUntil(() => f.Of<WagePaid>().Count > 0, 2400)) continue;
                fixtures++;
                var start = f.Of<EmploymentStarted>().First();
                Assert.AreEqual(f.Owner.Id, start.Agent); Assert.AreEqual(f.Worker.Id, start.Worker); Assert.AreEqual(f.Field.Id, start.Asset);
                var prod = f.Of<Produced>().Where(p => p.Npc == f.Worker.Id).ToList();
                Assert.IsNotEmpty(prod, "the worker produced");
                Assert.IsTrue(prod.All(p => p.OwnerKind == OwnerKind.Npc && p.OwnerId == f.Owner.Id), "every unit it produced is the employer's");
                Assert.IsTrue(f.Of<AssetUsed>().Where(u => u.Worker == f.Worker.Id).All(u => u.Right == Ownership.RightUse), "it worked under the contract's Work right");
                var g = f.S.Territory.Grid;
                foreach (var w in f.Of<WagePaid>())
                {
                    Assert.AreEqual(f.Owner.Id, w.Payer);
                    var m = f.Of<ItemMoved>().Single(x => x.Tick == w.Tick && x.Reason == MoveReason.HandOver && x.Actor == f.Owner.Id && x.ToId == f.Worker.Id);
                    Assert.LessOrEqual(g.Distance(m.ActorNode, m.ToNode), Reach.HandOver, "handed over within reach");
                    Assert.AreEqual(1, f.Of<OwnershipChanged>().Count(o => o.Tick == w.Tick && o.Basis == OwnershipBasis.Wage && o.ToId == f.Worker.Id));
                }
                // now the employer stops paying (its own policy): every unpaid unit is one arrears record
                f.SetPolicy(f.Owner, new NeverPays());
                var c = f.Contract(start.Contract);
                int before = f.Of<WageUnpaid>().Count;
                f.StepUntil(() => f.Of<WageUnpaid>().Count >= before + 2 || c.Status != ContractStatus.Active, 3000);
                var unpaid = f.Of<WageUnpaid>().Where(u => u.Contract == c.Id).ToList();
                Assert.GreaterOrEqual(unpaid.Count, 1, "seed " + seed + ": unpaid units");
                Assert.IsTrue(unpaid.All(u => u.Units == 1 && u.Obligation == 0), "one record per unit; C2 absent: no obligation id");
                for (int i = 0; i < unpaid.Count; i++) Assert.AreEqual(i + 1 + unpaid[0].Arrears - 1, unpaid[i].Arrears, "arrears grow by one per unpaid unit");
                Assert.AreEqual(c.UnitsUnpaid, unpaid.Count);
                Assert.IsEmpty(SimInvariants.Check(f.S));
                TestContext.WriteLine("seed " + seed + ": units produced " + prod.Count + ", wages " + f.Of<WagePaid>().Count + ", then unpaid units " + unpaid.Count + ", arrears " + c.ArrearsUnits);
            }
            Assert.GreaterOrEqual(fixtures, 3, "the owner hires and pays in most fixtures");
        }

        // ================================================================ C3-T2

        /// <summary>One-sided Fisher exact test: P(X >= a) for a 2x2 table [[a, b], [c, d]].</summary>
        internal static double FisherGreater(int a, int b, int c, int d)
        {
            double LogF(int n) { double s = 0; for (int i = 2; i <= n; i++) s += Math.Log(i); return s; }
            int r1 = a + b, r2 = c + d, c1 = a + c, n = r1 + r2;
            double p = 0;
            for (int x = a; x <= Math.Min(r1, c1); x++)
                p += Math.Exp(LogF(r1) + LogF(r2) + LogF(c1) + LogF(n - c1) - LogF(n) - LogF(x) - LogF(r1 - x) - LogF(c1 - x) - LogF(r2 - c1 + x));
            return p;
        }

        /// <summary>
        /// C3-T2: a worker offered pay it values below its own foraging alternative (2 cloth: no use to it; a berry bush in
        /// sight) refuses more often than one offered pay it values highly (2 fish), paired over 20 seeds (one-sided Fisher
        /// p reported); no contract ever starts without the worker's own Accept gesture.
        /// </summary>
        [Test]
        public void Hiring_IsAChoice()
        {
            int lowRefuse = 0, lowAccept = 0, highRefuse = 0, highAccept = 0, lowNone = 0, highNone = 0;
            for (ulong seed = 1; seed <= 20; seed++)
                foreach (bool high in new[] { false, true })
                {
                    var f = new Fixture(seed, t => t.Civ.C3.BasePay = 2, 400, new[] { (high ? Ware.Fish : Ware.Cloth, 3) }, bush: true);
                    bool Answered() => f.Of<GestureMade>().Any(g => g.Speaker == f.Worker.Id && g.TopicKind == TopicKind.Posting && (g.Act == GestureAct.Accept || g.Act == GestureAct.Refuse));
                    f.StepUntil(Answered, 1800);
                    var first = f.Of<GestureMade>().FirstOrDefault(g => g.Speaker == f.Worker.Id && g.TopicKind == TopicKind.Posting && (g.Act == GestureAct.Accept || g.Act == GestureAct.Refuse));
                    var offer = f.Of<GestureMade>().FirstOrDefault(g => g.Speaker == f.Owner.Id && g.Act == GestureAct.Offer && g.TopicKind == TopicKind.Posting);
                    if (offer != null) Assert.AreEqual((int)(high ? Ware.Fish : Ware.Cloth), offer.CounterId, "the owner offered what it carries");
                    if (first == null) { if (high) highNone++; else lowNone++; }
                    else if (first.Act == GestureAct.Refuse) { if (high) highRefuse++; else lowRefuse++; }
                    else { if (high) highAccept++; else lowAccept++; }
                    // nobody is assigned work without its own Accept: every contract started at the worker's Accept on it
                    foreach (var s in f.Of<EmploymentStarted>())
                        Assert.IsTrue(f.Of<GestureMade>().Any(g => g.Tick == s.Tick && g.Speaker == s.Worker && g.Act == GestureAct.Accept && g.TopicKind == TopicKind.Posting && g.TopicId == s.Contract),
                            "contract " + s.Contract + " started without the worker's Accept");
                }
            double p = FisherGreater(lowRefuse, lowAccept, highRefuse, highAccept);
            TestContext.WriteLine("low-valued pay (2 cloth, berries in sight): refused " + lowRefuse + ", accepted " + lowAccept + ", no answer " + lowNone);
            TestContext.WriteLine("high-valued pay (2 fish, berries in sight): refused " + highRefuse + ", accepted " + highAccept + ", no answer " + highNone);
            TestContext.WriteLine("one-sided Fisher exact p (refusal more likely with low pay) = " + p.ToString("0.0000"));
            Assert.Greater(lowRefuse + lowAccept, 10, "the low arm answered"); Assert.Greater(highRefuse + highAccept, 10, "the high arm answered");
            Assert.Greater(lowRefuse * (highRefuse + highAccept), highRefuse * (lowRefuse + lowAccept), "low pay is refused more often");
            Assert.Less(p, 0.05);
        }

        // ================================================================ C3-T3, C3-T4

        private sealed class Story
        {
            public int Suspensions, Resumes, Renegotiations, Resigned, Dismissed, Unpaid, LongSuspensions, TrustDrops;
            public override string ToString() => "unpaid units " + Unpaid + ", walk-outs " + Suspensions + " (" + LongSuspensions + " longer than AbandonTicks), resumed " + Resumes
                + ", renegotiating offers " + Renegotiations + ", resigned " + Resigned + ", dismissed " + Dismissed + ", trust fell " + TrustDrops;
        }

        /// <summary>Runs a cheating-employer fixture; at the walk-out the employer may go back to its own default policy (it may then pay what it owes).</summary>
        private static Story Cheated(ulong seed, bool relent, int ticks, bool passive = false)
        {
            var st = new Story();
            var f = new Fixture(seed);
            var np = new NeverPays { Passive = passive };
            f.SetPolicy(f.Owner, np);
            var suspendedAt = new Dictionary<int, long>();
            int trustAtStart = int.MinValue;
            bool trustFell = false;
            f.Lab.OnEvents = evs =>
            {
                foreach (var e in evs)
                {
                    switch (e)
                    {
                        case EmploymentStarted s:
                            np.Hired = true;
                            trustAtStart = int.MinValue; trustFell = false; break;
                        case ContractSuspended s:
                            suspendedAt[s.Contract] = s.Tick;
                            if (relent) f.DefaultPolicy(f.Owner);
                            break;
                        case ContractResumed r:
                            if (suspendedAt.TryGetValue(r.Contract, out long t0) && r.Tick - t0 > f.S.Tuning.Civ.C3.AbandonTicks) st.LongSuspensions++;
                            suspendedAt.Remove(r.Contract);
                            break;
                        case EmploymentEnded x:
                            Assert.AreNotEqual(EndReason.Abandoned, suspendedAt.ContainsKey(x.Contract) ? x.Reason : EndReason.None, "a suspension is never ended by a timer");
                            if (suspendedAt.TryGetValue(x.Contract, out long t1) && x.Tick - t1 > f.S.Tuning.Civ.C3.AbandonTicks) st.LongSuspensions++;
                            suspendedAt.Remove(x.Contract);
                            break;
                        case GestureMade g:
                            if (g.Act == GestureAct.Offer && g.TopicKind == TopicKind.Posting && suspendedAt.ContainsKey(g.TopicId)) st.Renegotiations++;
                            break;
                    }
                }
            };
            for (int t = 0; t < ticks; t += 10)
            {
                f.Lab.Step(10);
                // the worker's own trust in its employer (its belief) before and after the unpaid units
                var jobs = f.Worker.Mind?.Civ?.Jobs;
                if (jobs != null && jobs.Count > 0)
                {
                    var j = jobs[jobs.Count - 1];
                    if (trustAtStart == int.MinValue && j.Status == ContractStatus.Active) trustAtStart = j.Trust;
                    if (trustAtStart != int.MinValue && !trustFell && j.Unpaid > 0 && j.Trust < trustAtStart) { st.TrustDrops++; trustFell = true; }
                }
            }
            // a suspension still open at the end that has lasted past AbandonTicks
            foreach (var kv in suspendedAt)
            {
                var c = f.Contract(kv.Key);
                if (c != null && c.Status == ContractStatus.Suspended && f.S.Tick - kv.Value > f.S.Tuning.Civ.C3.AbandonTicks)
                {
                    st.LongSuspensions++;
                    Assert.IsTrue(Employment.HoldsWorkRight(Assets.Find(f.S, c.Asset), c.Worker), "the slot (the Work right) is kept while suspended");
                }
            }
            var ev = f.Lab.Events;
            st.Suspensions = f.Of<ContractSuspended>().Count;
            st.Resumes = f.Of<ContractResumed>().Count;
            st.Resigned = f.Of<EmploymentEnded>().Count(x => x.Reason == EndReason.Resigned);
            st.Dismissed = f.Of<EmploymentEnded>().Count(x => x.Reason == EndReason.Dismissed);
            st.Unpaid = f.Of<WageUnpaid>().Count;
            // while suspended: no work under the contract and no pay falls due (no unpaid unit is recorded)
            foreach (var s in f.Of<ContractSuspended>())
            {
                long end = f.Of<ContractResumed>().Where(r => r.Contract == s.Contract && r.Tick >= s.Tick).Select(r => r.Tick)
                    .Concat(f.Of<EmploymentEnded>().Where(x => x.Contract == s.Contract && x.Tick >= s.Tick).Select(x => x.Tick)).DefaultIfEmpty(long.MaxValue).Min();
                var c = f.Contract(s.Contract);
                int asset = f.Of<EmploymentStarted>().First(x => x.Contract == s.Contract).Asset;
                Assert.IsFalse(f.Of<AssetUsed>().Any(u => u.Worker == f.Worker.Id && u.Asset == asset && u.Tick > s.Tick && u.Tick < end), "no work while suspended");
                Assert.IsFalse(f.Of<WageUnpaid>().Any(u => u.Contract == s.Contract && u.Tick > s.Tick && u.Tick < end), "no pay due while suspended");
                Assert.IsFalse(f.Of<JobDay>().Any(d => d.Contract == s.Contract && d.Day * 1200L > s.Tick && (d.Day + 1) * 1200L < end && d.WorkTicks > 0), "no JobDay work on a day wholly suspended");
                Assert.GreaterOrEqual(f.Of<WageUnpaid>().Count(u => u.Contract == s.Contract && u.Tick <= s.Tick), f.S.Tuning.Civ.C3.WalkOutUnpaid, "it walked out after its unpaid units");
            }
            Assert.IsTrue(f.Of<WagePaid>().All(w => relent), "the cheating employer never paid (no rule forced it)");
            Assert.IsEmpty(SimInvariants.Check(f.S));
            return st;
        }

        /// <summary>
        /// C3-T3 (owner Q19): a worker unpaid for 2 units walks out and the contract is suspended; while suspended there is no
        /// work and no pay due and the Work right (the slot) is kept; no suspension ever ends as Abandoned; going back,
        /// renegotiating, resigning and dismissing each occur in the fixtures (by the people's own choices). The slot kept
        /// past AbandonTicks with nobody acting: C3Tests.Suspension_NeverEndsByTimer.
        /// </summary>
        [Test]
        public void WalkOut_Suspends()
        {
            var total = new Story();
            for (ulong seed = 1; seed <= 8; seed++)
                for (int arm = 0; arm < 3; arm++)
                {
                    var st = Cheated(seed, arm == 1, 6000, passive: arm == 2);
                    TestContext.WriteLine("seed " + seed + (arm == 1 ? " (the employer goes back to its own policy at the walk-out)" : arm == 2 ? " (the employer never pays and leaves the walk-out alone)" : " (the employer never pays)") + ": " + st);
                    total.Suspensions += st.Suspensions; total.Resumes += st.Resumes; total.Renegotiations += st.Renegotiations; total.Resigned += st.Resigned;
                    total.Dismissed += st.Dismissed; total.LongSuspensions += st.LongSuspensions; total.Unpaid += st.Unpaid; total.TrustDrops += st.TrustDrops;
                }
            TestContext.WriteLine("all fixtures: " + total);
            Assert.Greater(total.Suspensions, 0, "walk-outs happen");
            // the timer half (a suspension outlasts AbandonTicks, nothing ends it): C3Tests.Suspension_NeverEndsByTimer (EditMode,
            // nobody acting); here the people act within that time (resign, dismiss, go back), and no suspension ever ended as Abandoned
            Assert.Greater(total.Resumes, 0, "resume occurs");
            Assert.Greater(total.Renegotiations, 0, "renegotiation occurs");
            Assert.Greater(total.Resigned, 0, "resignation occurs");
            Assert.Greater(total.Dismissed, 0, "dismissal occurs");
        }

        /// <summary>C3-T4: an employer that does not pay: WageUnpaid, arrears grow, the worker's own trust in it falls; nothing forces payment.</summary>
        [Test]
        public void Employer_CanCheat()
        {
            int cheated = 0, drops = 0;
            for (ulong seed = 1; seed <= 6; seed++)
            {
                var st = Cheated(seed, false, 3000);
                if (st.Unpaid >= 2) cheated++;
                drops += st.TrustDrops;
                TestContext.WriteLine("seed " + seed + ": " + st);
            }
            Assert.GreaterOrEqual(cheated, 3, "unpaid units accrue as arrears");
            Assert.GreaterOrEqual(drops, cheated, "the worker's trust in the employer falls from its own experience");
        }

        // ================================================================ C3-T5

        /// <summary>C3-T5: the worker's death ends the contract (Died) and its Work right. (D7 not merged: the resurrection half is not testable yet.)</summary>
        [Test]
        public void Death_EndsContracts_NotRestored()
        {
            var f = new Fixture(1);
            Assert.IsTrue(f.StepUntil(() => f.Of<EmploymentStarted>().Count > 0, 2400));
            var c = f.Contract(f.Of<EmploymentStarted>()[0].Contract);
            Assert.AreEqual(ContractStatus.Active, c.Status);
            var ev = new EventBuffer { CurrentTick = f.S.Tick };
            PopulationModule.Kill(f.S, ev, f.Worker, DeathCause.Accident);
            var evs = ev.Drain();
            var end = evs.OfType<EmploymentEnded>().Single();
            Assert.AreEqual(EndReason.Died, end.Reason); Assert.AreEqual(c.Id, end.Contract);
            Assert.AreEqual(ContractStatus.Ended, c.Status);
            Assert.IsFalse(Employment.HoldsWorkRight(f.Field, f.Worker.Id), "the right ended with the holder");
            f.Lab.Step(600);
            Assert.AreEqual(ContractStatus.Ended, c.Status, "never reopened");
            Assert.IsFalse(f.Of<EmploymentStarted>().Any(s => s.Worker == f.Worker.Id && s.Tick > end.Tick));
        }

        // ================================================================ C3-T7

        /// <summary>C3_SaveLoadParity (spec 11): saved during a shift with a suspended second contract; loaded and continued = the straight run at every later checkpoint.</summary>
        [Test]
        public void C3_SaveLoadParity()
        {
            GodSim Game()
            {
                var f = new Fixture(2);
                Assert.IsTrue(f.StepUntil(() => f.S.Civ.Employment != null && f.S.Civ.Employment.Contracts.Any(k => k.Status == ContractStatus.Active && k.ShiftProgress > 10), 3000), "a shift in progress");
                // a second worker, hired (its own accept is not the point here: the API path) and walked out
                var w2 = f.Lab.AddH(f.Lab.At(1, 2), null, LearningMode.On, false, 300);
                var k2 = Employment.Hire(f.S, null, God.Sim.Population.Owner.Npc(f.Owner.Id), f.Owner.Id, w2.Id, f.Field.Id,
                    new PayTerms { Pay = PayUnit.Of(Ware.Roots, 1), Per = PayPer.Shift, ShiftTicks = 120 });
                Assert.IsTrue(Employment.Activate(f.S, null, k2));
                Assert.IsTrue(Employment.Suspend(f.S, null, k2, EmploymentContract.ByWorker, ContractSuspended.Other));
                f.Lab.Step(3);
                Assert.AreEqual(ContractStatus.Suspended, k2.Status);
                return f.Lab.Sim;
            }
            var straight = Game();
            var other = Game();
            Assert.AreEqual(straight.StateHash(), other.StateHash(), "deterministic set-up");
            Assert.IsTrue(other.State.Civ.Employment.Contracts.Any(k => k.Status == ContractStatus.Suspended));
            var loaded = SaveFile.FromJson(SaveFile.From(other, "x").ToJson()).ToSim();
            Assert.AreEqual(straight.StateHash(), loaded.StateHash(), "same hash straight after the load");
            Assert.IsNotNull(loaded.State.Civ.Employment);
            for (int k = 0; k < 8; k++)
            {
                straight.Step(600); loaded.Step(600);
                CollectionAssert.AreEqual(straight.StateHashParts(), loaded.StateHashParts(), "checkpoint " + k);
            }
            Assert.AreEqual(SaveFile.From(straight).ToJson(), SaveFile.From(loaded).ToJson());
            Assert.IsEmpty(SimInvariants.Check(loaded.State));
        }

        private (RunResult res, string db, CaptureRecorder cap, GridKeeper keep) Recorded(string tag, RecordingLevel level, ulong seed, long ticks)
        {
            var cap = new CaptureRecorder(); var keep = new GridKeeper();
            var spec = Spec(seed, ticks, cap, keep);
            spec.Level = level;
            spec.Sqlite = new WarehouseOptions { ForeignKeys = true, Async = false };
            spec.RunDir = Path.Combine(_dir, tag); spec.OutRoot = _dir; spec.SkipJsonl = true;
            var res = RunEngine.Run(spec);
            Assert.IsNull(res.Error, res.Error);
            Assert.AreEqual(0, res.ExitCode, string.Join("; ", res.Messages));
            return (res, res.WarehousePath, cap, keep);
        }

        private static long Q(string db, string sql) { using (var d = WarehouseDb.OpenRead(db)) return Convert.ToInt64(d.Scalar(sql) ?? 0L); }

        /// <summary>
        /// C3_Warehouse_Reconciles (spec 12): rows equal events for 250-256; the wage rows equal the wage lines of the goods
        /// tables (fact_ownership basis 16, count and quantity); open employment spells equal the active or suspended
        /// contracts at the run's end; recording changes no hash.
        /// </summary>
        [Test]
        public void C3_Warehouse_Reconciles()
        {
            const ulong seed = 2; const long ticks = 12000;
            var (full, db, cap, keep) = Recorded("full", RecordingLevel.Full, seed, ticks);
            var e = cap.Events;
            int started = e.OfType<EmploymentStarted>().Count(), ended = e.OfType<EmploymentEnded>().Count();
            Assert.Greater(started, 0, "contracts in civ_employment seed " + seed);
            Assert.AreEqual(started, Q(db, "SELECT COUNT(*) FROM fact_employment"), "one spell per EmploymentStarted");
            Assert.AreEqual(ended, Q(db, "SELECT COUNT(*) FROM fact_employment WHERE to_tick IS NOT NULL"), "closed at EmploymentEnded");
            Assert.AreEqual(e.OfType<JobDay>().Count(), Q(db, "SELECT COUNT(*) FROM fact_job_day"));
            Assert.AreEqual(e.OfType<WagePaid>().Count(), Q(db, "SELECT COUNT(*) FROM fact_employment_event WHERE kind_code = 251"));
            Assert.AreEqual(e.OfType<ContractSuspended>().Count(), Q(db, "SELECT COUNT(*) FROM fact_employment_event WHERE kind_code = 254"));
            Assert.AreEqual(e.OfType<ContractResumed>().Count(), Q(db, "SELECT COUNT(*) FROM fact_employment_event WHERE kind_code = 255"));
            Assert.AreEqual(e.OfType<WageUnpaid>().Count(), Q(db, "SELECT COUNT(*) FROM fact_employment_event WHERE kind_code = 256"));
            // wage lines: every unit of every wage is one ownership change with basis Wage (16), and nothing else has that basis
            long wageGoods = e.OfType<WagePaid>().Sum(w => (long)w.Count);
            Assert.AreEqual(wageGoods, Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_employment_event WHERE kind_code = 251"));
            Assert.AreEqual(wageGoods, Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_ownership WHERE basis_code = 16"), "wage rows reconcile to the wage lines of the goods tables");
            Assert.AreEqual(e.OfType<OwnershipChanged>().Count(o => o.Basis == OwnershipBasis.Wage), Q(db, "SELECT COUNT(*) FROM fact_ownership WHERE basis_code = 16"));
            // open spells = the contracts open at the end
            int open = keep.Final.Civ.Employment.Contracts.Count(k => k.IsOpen);
            Assert.AreEqual(open, Q(db, "SELECT COUNT(*) FROM fact_employment WHERE to_tick IS NULL"));
            Assert.AreEqual(started, Q(db, "SELECT COUNT(*) FROM fact_economic_relationship WHERE kind_code = 2"), "one employs relationship per contract");
            Assert.AreEqual(0L, Q(db, "SELECT COUNT(*) FROM fact_employment WHERE to_tick IS NOT NULL AND to_tick < from_tick"), "half-open spells");
            TestContext.WriteLine("civ_employment seed " + seed + ": contracts " + started + ", ended " + ended + ", open " + open + ", job days " + e.OfType<JobDay>().Count()
                + ", wages " + e.OfType<WagePaid>().Count() + " (" + wageGoods + " goods), unpaid " + e.OfType<WageUnpaid>().Count() + "; end reasons: "
                + string.Join(", ", Enumerable.Range(1, 5).Select(r => (EndReason)r + " " + Q(db, "SELECT COUNT(*) FROM fact_employment WHERE end_reason_code = " + r))));
            var (sum, db2, _, _) = Recorded("summary", RecordingLevel.Summary, seed, ticks);
            Assert.AreEqual(started, Q(db2, "SELECT COUNT(*) FROM fact_employment"), "employment rows at Summary too");
            var p = RunEngine.Run(Spec(seed, ticks, null, null));
            Assert.AreEqual(p.FinalHash, full.FinalHash, "recording at Full changes no hash");
            Assert.AreEqual(p.FinalHash, sum.FinalHash, "recording at Summary changes no hash");
        }
    }
}
