using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using God.Sim.Civ;
using God.Sim.Cognition;
using God.Sim.Information;
using God.Sim.Learning;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// C3 (docs/civdiv/DEVELOPMENT_PACKAGES/C3.md section 13) at the level of the record and its rules: the ContractId
    /// counter, acceptance granting C1's Work right, the suspension that no timer ends (owner Q19), abandonment of an idle
    /// active contract, the payment act's reach, the catalogue rows, the worker's own valuation, the card rows, and the
    /// static boundaries (nothing forces payment; the mind side never sees the true state). The people's choices are
    /// tested at run level (<c>God.Runner.Tests.C3RunTests</c>).
    /// </summary>
    public class C3Tests
    {
        private sealed class W
        {
            public GodSim Sim; public GodState S; public int Glade; public EventBuffer Ev;
            public List<SimEvent> Log = new List<SimEvent>();
            public IEnumerable<T> Of<T>() { Log.AddRange(Ev.Drain()); return Log.OfType<T>(); }
        }

        private static W World(ulong seed = 3, System.Action<SimTuning> tune = null)
        {
            var sim = LearningFixtures.Empty(seed, t => { t.Civ.Enabled = 1; tune?.Invoke(t); });
            var w = new W { Sim = sim, S = sim.State, Glade = LearningFixtures.Glade(sim) };
            w.Ev = new EventBuffer { CurrentTick = w.S.Tick };
            return w;
        }

        private static Npc Person(W w, int node) => LearningFixtures.Npc(w.Sim, node, InstinctSet.Default, LearningMode.On);
        private static int At(W w, int dir, int steps) => LearningFixtures.Step(w.Sim, w.Glade, dir, steps);
        private static PayTerms Terms(Ware ware = Ware.Roots, int n = 1) => new PayTerms { Pay = PayUnit.Of(ware, n), Per = PayPer.Shift, ShiftTicks = 120 };

        /// <summary>Advances the clock and runs only C3's per-tick accounting (nobody acts: the timers alone).</summary>
        private static void Ticks(W w, int n)
        {
            for (int i = 0; i < n; i++) { w.S.Tick++; w.Ev.CurrentTick = w.S.Tick; Employment.Tick(w.S, w.Ev); }
        }

        private static (Npc owner, Npc worker, ProductiveAsset field) Pair(W w)
        {
            var owner = Person(w, w.Glade);
            var worker = Person(w, At(w, 0, 1));
            var field = Assets.Register(w.S, w.Ev, AssetKinds.Field, At(w, 3, 2), Owner.Npc(owner.Id), OwnershipBasis.Built, owner.Id);
            return (owner, worker, field);
        }

        [Test]
        public void Hire_AllocatesContractIds_AcceptanceGrantsTheWorkRight()
        {
            var w = World();
            var (owner, worker, field) = Pair(w);
            Assert.AreEqual(1, w.S.Civ.NextContractId);
            var a = Employment.Hire(w.S, w.Ev, Owner.Npc(owner.Id), owner.Id, worker.Id, field.Id, Terms());
            var b = Employment.Hire(w.S, w.Ev, Owner.Npc(owner.Id), owner.Id, worker.Id, field.Id, Terms());
            Assert.AreEqual(1, a.Id); Assert.AreEqual(2, b.Id); Assert.AreEqual(3, w.S.Civ.NextContractId, "ContractId from CivState.NextContractId");
            Assert.AreEqual(ContractStatus.Offered, a.Status);
            Assert.IsEmpty(w.Of<EmploymentStarted>(), "an offer is not a contract yet");
            Assert.IsFalse(Employment.HoldsWorkRight(field, worker.Id), "no right without the acceptance");
            Assert.IsTrue(Employment.Activate(w.S, w.Ev, a));
            Assert.AreEqual(ContractStatus.Active, a.Status);
            Assert.IsTrue(Employment.HoldsWorkRight(field, worker.Id), "C1's Assets.GrantUse(Work)");
            var st = w.Of<EmploymentStarted>().Single();
            Assert.AreEqual(a.Id, st.Contract); Assert.AreEqual(worker.Id, st.Worker); Assert.AreEqual((int)Ware.Roots, st.PayRef); Assert.AreEqual(PayPer.Shift, st.Per);
            Assert.AreEqual(1, w.Of<UseRightGranted>().Count(g => g.HolderId == worker.Id && g.Kind == UseKind.Work));
            Assert.IsTrue(Permits.BelievedAsset(new ThinkScratch { Mind = worker.Mind, Self = new SelfView { Id = worker.Id } }, field.Id, UseKind.Work).Allowed, "the worker was told by the grant");
            var wj = worker.Mind.Civ.Jobs.Single();
            Assert.AreEqual(JobBelief.AsWorker, wj.Side); Assert.AreEqual(owner.Id, wj.Other); Assert.AreEqual(ContractStatus.Active, wj.Status);
            // a grant by someone who is not the owner (nor of its household) is void
            var stranger = Person(w, At(w, 1, 1));
            var c = Employment.Hire(w.S, w.Ev, Owner.Npc(stranger.Id), stranger.Id, worker.Id, field.Id, Terms());
            Assert.IsFalse(Employment.Activate(w.S, w.Ev, c), "only the asset's owner (or its household) can give the right");
            // refused and lapsed offers never open a spell
            Employment.End(w.S, w.Ev, b, EndReason.Refused, EmploymentContract.ByWorker);
            Ticks(w, w.S.Tuning.Civ.C3.OfferTicks + 2);
            Assert.AreEqual(EndReason.Lapsed, c.Reason);
            Assert.IsEmpty(w.Of<EmploymentEnded>(), "no EmploymentEnded for an offer that never started");
            var bad = new List<string>(); Employment.Check(w.S, bad); Assert.IsEmpty(bad);
            Assert.IsEmpty(SimInvariants.Check(w.S));
        }

        /// <summary>C3-T3, the timer half: a suspended contract keeps its slot (the Work right) well past AbandonTicks with nobody acting; an idle active one is abandoned.</summary>
        [Test]
        public void Suspension_NeverEndsByTimer_IdleActiveIsAbandoned()
        {
            var w = World();
            var (owner, worker, field) = Pair(w);
            var c = Employment.Hire(w.S, w.Ev, Owner.Npc(owner.Id), owner.Id, worker.Id, field.Id, Terms());
            Assert.IsTrue(Employment.Activate(w.S, w.Ev, c));
            Assert.IsTrue(Employment.Suspend(w.S, w.Ev, c, EmploymentContract.ByWorker, ContractSuspended.Unpaid));
            Assert.AreEqual(1, w.Of<ContractSuspended>().Count());
            int abandon = w.S.Tuning.Civ.C3.AbandonTicks;
            Ticks(w, abandon * 4);
            Assert.AreEqual(ContractStatus.Suspended, c.Status, "nothing ends a suspension automatically");
            Assert.IsTrue(Employment.HoldsWorkRight(field, worker.Id), "the slot is kept");
            Assert.AreEqual(0, c.DueUnits); Assert.AreEqual(0, c.ArrearsUnits, "no pay falls due while suspended");
            Assert.IsEmpty(w.Of<EmploymentEnded>());
            Assert.IsTrue(Employment.Resume(w.S, w.Ev, c, EmploymentContract.ByWorker));
            Assert.AreEqual(1, w.Of<ContractResumed>().Count());
            Ticks(w, abandon + 2);
            Assert.AreEqual(ContractStatus.Ended, c.Status); Assert.AreEqual(EndReason.Abandoned, c.Reason, "an active contract without progress is abandoned");
            Assert.AreEqual(EndReason.Abandoned, w.Of<EmploymentEnded>().Single().Reason);
            Assert.IsFalse(Employment.HoldsWorkRight(field, worker.Id));
        }

        [Test]
        public void Pay_HandsOverWithinReachOnly_AndCreatesNoGoods()
        {
            var w = World();
            var payer = Person(w, w.Glade);
            var payee = Person(w, At(w, 0, 3));
            SocialFixtures.Give(w.Sim, payer, Ware.Roots, 3);
            long produced = w.S.Population.Ledger.ProducedOf(Ware.Roots);
            Assert.AreEqual(0, Employment.Pay(w.S, w.Ev, payer, payee, Ware.Roots, 2, PaymentPurpose.Wage, out _), "3 nodes away: out of reach");
            payee.Node = At(w, 0, 1);
            Assert.AreEqual(0, Employment.Pay(w.S, w.Ev, payer, payee, Ware.Roots, 4, PaymentPurpose.Wage, out _), "it cannot pay what it does not have");
            Assert.AreEqual(2, Employment.Pay(w.S, w.Ev, payer, payee, Ware.Roots, 2, PaymentPurpose.Wage, out int lot));
            Assert.AreEqual(2, PopulationModule.CountOf(payee, Ware.Roots)); Assert.AreEqual(1, PopulationModule.CountOf(payer, Ware.Roots));
            var m = w.Of<ItemMoved>().Last();
            Assert.AreEqual(MoveReason.HandOver, m.Reason); Assert.AreEqual(payer.Id, m.Actor);
            var o = w.Of<OwnershipChanged>().Last();
            Assert.AreEqual(OwnershipBasis.Wage, o.Basis); Assert.AreEqual(payee.Id, o.ToId);
            Assert.AreEqual(produced, w.S.Population.Ledger.ProducedOf(Ware.Roots), "no goods created");
            Assert.IsEmpty(SimInvariants.Check(w.S));
        }

        [Test]
        public void Catalogue_HasC3Rows_250To256()
        {
            var rows = CivEvents.Catalog.Where(e => e.Code >= 250 && e.Code <= 269).OrderBy(e => e.Code).ToList();
            CollectionAssert.AreEqual(new ushort[] { 250, 251, 252, 253, 254, 255, 256 }, rows.Select(r => r.Code).ToArray());
            CollectionAssert.AreEqual(new[] { typeof(EmploymentStarted), typeof(WagePaid), typeof(EmploymentEnded), typeof(JobDay), typeof(ContractSuspended), typeof(ContractResumed), typeof(WageUnpaid) },
                rows.Select(r => r.Type).ToArray());
            Assert.IsTrue(rows.All(r => r.Domain == TelemetryDomain.Employment));
            Assert.IsTrue(VerbTable.TryGet(Employment.VerbHire, out var h) && h.Group == VerbGroup.Employment);
            Assert.IsTrue(VerbTable.TryGet(Employment.VerbWorkFor, out _) && VerbTable.TryGet(Employment.VerbPay, out _));
        }

        /// <summary>The worker's own valuation (mind side): pay with no use to it is poor; food it needs, with nothing to forage, is good.</summary>
        [Test]
        public void WorkerValuation_IsItsOwn()
        {
            var w = World();
            var n = Person(w, w.Glade);
            var sc = new ThinkScratch { Mind = n.Mind, Grid = w.S.Territory.Grid, Tuning = w.S.Tuning.Population };
            sc.Self.Id = n.Id; sc.Self.Node = n.Node; sc.Self.Hunger = 600; sc.Self.Fatigue = 100;
            var tu = new CivC3Tuning();
            Assert.AreEqual(PostingValue.Poor, CivMindHooks.WorkerBand(sc, tu, Ware.Cloth, 2, 2, 0));
            Assert.AreEqual(PostingValue.Good, CivMindHooks.WorkerBand(sc, tu, Ware.Fish, 2, 2, 0));
            Assert.Less(CivMindHooks.WorkerBand(sc, tu, Ware.Fish, 2, 2, -1000), PostingValue.Good + 1, "distrust lowers it");
            Assert.LessOrEqual(CivMindHooks.WorkerBand(sc, tu, Ware.Fish, 1, 40, 0), CivMindHooks.WorkerBand(sc, tu, Ware.Fish, 1, 1, 0), "a long walk lowers it");
        }

        [Test]
        public void Card_ShowsContracts_TermsAsUnderstood_AndArrearsBelieved()
        {
            var w = World();
            var (owner, worker, field) = Pair(w);
            var c = Employment.Hire(w.S, w.Ev, Owner.Npc(owner.Id), owner.Id, worker.Id, field.Id, Terms(Ware.Berries, 2));
            Employment.Activate(w.S, w.Ev, c);
            var card = new NpcInspection();
            CivCards.Fill(w.S, worker, card);
            Assert.IsTrue(card.Economy.Rows.Any(r => r.Kind == CivCardRow.ContractHeld && r.Layer == KnowledgeLayer.Truth && r.Ref == c.Id));
            Assert.IsTrue(card.Economy.Rows.Any(r => r.Kind == CivCardRow.TermsBelieved && r.Layer == KnowledgeLayer.Belief && r.Text.Contains("pay 2 berries")));
            Assert.IsTrue(card.Economy.Rows.Any(r => r.Kind == CivCardRow.ArrearsBelieved && r.Layer == KnowledgeLayer.Belief));
        }

        private static string Src(string rel) => File.ReadAllText(Path.Combine(GoodsTests.RepoRoot(), "Assets", "God", "Sim", rel));

        /// <summary>No rule forces payment: the payment hand-over is called only from the Pay act, which only a decision starts; the mind side never sees the true state.</summary>
        [Test]
        public void NothingForcesPayment_MindSideSeesNoTruth()
        {
            var calls = new List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(GoodsTests.RepoRoot(), "Assets", "God", "Sim"), "*.cs", SearchOption.AllDirectories).OrderBy(x => x, System.StringComparer.Ordinal))
                foreach (var line in File.ReadAllLines(f))
                {
                    var t = line.TrimStart();
                    if (t.StartsWith("//")) continue;
                    if (Regex.IsMatch(t, @"(?<![A-Za-z])Pay\(s, ev,")) calls.Add(Path.GetFileName(f) + ": " + t);
                }
            Assert.AreEqual(1, calls.Count, "one call site of the hand-over:\n" + string.Join("\n", calls));
            StringAssert.Contains("Employment.cs", calls[0]);
            var src = Src("Civ/Employment.cs");
            int act = src.IndexOf("private static void PayAct(", System.StringComparison.Ordinal), call = src.IndexOf("int paid = Pay(s, ev,", System.StringComparison.Ordinal);
            Assert.Greater(call, act, "inside PayAct"); Assert.Less(call, src.IndexOf("private static int PlannedUnits", System.StringComparison.Ordinal));
            foreach (var t in new[] { typeof(CivMindHooks), typeof(CivMind), typeof(JobBelief), typeof(C3Plan), typeof(CivC3Tuning) })
            {
                var refs = TypeReferenceScanner.TypesOf(t);
                Assert.IsFalse(refs.Contains(typeof(GodState)), t.Name + " references GodState");
                Assert.IsFalse(refs.Contains(typeof(EventBuffer)), t.Name + " references EventBuffer");
                Assert.IsFalse(refs.Contains(typeof(EmploymentState)), t.Name + " reads the contract record (truth)");
            }
        }
    }
}
