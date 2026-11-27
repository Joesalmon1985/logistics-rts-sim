using System;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using God.Sim.Social;

namespace God.Sim.Civ
{
    /// <summary>Why a payment is made (spec 22: <c>Pay</c> 101 is the general hand-over act; K2 appends purposes). Append only.</summary>
    public enum PaymentPurpose : byte { None = 0, Wage = 1 }

    /// <summary>
    /// C3a: employment between individuals (spec 5; ECONOMY 4 "Jobs", owner Q19). Sim side: the contract record and its
    /// API (<see cref="Hire"/>, <see cref="Activate"/>, <see cref="Suspend"/>, <see cref="Resume"/>, <see cref="End"/>), the
    /// general payment act (<see cref="Pay"/>), the acts people choose (offer, accept, refuse, dismiss, pay; the work itself
    /// is C1's Work act under the Work right the contract grants), the per-tick accounting of shifts, pay due and
    /// arrears, and the day pass. Nothing here makes anyone hire, work, pay or walk out: every act starts from a decision
    /// of the person's own policy over the candidates its mind offers (<see cref="CivMindHooks"/>); the sim only carries
    /// out the physical act (gestures through M5's language, goods through <see cref="Goods"/> within reach) and writes
    /// what each party perceives into its own beliefs. No rule forces payment; no goods are created.
    /// </summary>
    public static class Employment
    {
        /// <summary>Learning verbs (contracts 14): Hire 99 (the employer's negotiation acts), WorkFor 100 (the worker's), Pay 101.</summary>
        public const byte VerbHire = 99, VerbWorkFor = 100, VerbPay = 101;
        /// <summary>Intent verbs (contracts 14 Civ block; C1 holds 170-172): offer, dismiss, accept (or work), refuse (or walk out, resign), pay.</summary>
        public const NpcVerb IntentOffer = (NpcVerb)173, IntentDismiss = (NpcVerb)174, IntentAccept = (NpcVerb)175, IntentRefuse = (NpcVerb)176, IntentPay = (NpcVerb)177;

        public static bool IsMine(byte v) => v == VerbHire || v == VerbWorkFor || v == VerbPay;

        public static EmploymentState StateOf(GodState s) => s.Civ?.Employment;
        private static EmploymentState Ensure(GodState s) => s.Civ.Employment ?? (s.Civ.Employment = new EmploymentState());
        private static int Day(GodState s) => (int)SimCalendar.Of(s).Day(s.Tick);
        private static byte ControlOf(GodState s, int npc) => s.Divinity != null ? (byte)Divinity.Control.Of(s, npc) : (byte)0;

        public static EmploymentContract Find(GodState s, int id)
        {
            var e = StateOf(s);
            if (e == null || id <= 0) return null;
            int lo = 0, hi = e.Contracts.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = e.Contracts[mid].Id;
                if (v == id) return e.Contracts[mid];
                if (v < id) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        /// <summary>The episode key of a contract (contracts 1: kind 17 employment contract).</summary>
        public static int EpisodeKeyOf(int contract) => contract <= 0 ? 0 : EpisodeKey.Make(EpisodeKey.EmploymentContract, contract & EpisodeKey.MaxId);

        // ================================================================ the contract record (API)

        /// <summary>
        /// The only ContractId allocator (contracts 1 row <c>ContractId</c>): an offer of work on <paramref name="asset"/> to
        /// <paramref name="worker"/>, made by <paramref name="agent"/> for <paramref name="employer"/> (any owner party).
        /// Status Offered until the worker accepts (<see cref="Activate"/>). The employer's agent believes what it offered.
        /// </summary>
        public static EmploymentContract Hire(GodState s, EventBuffer ev, Owner employer, int agent, int worker, int asset, in PayTerms terms, byte slot = 0)
        {
            if (s.Civ == null) throw new InvalidOperationException("Employment.Hire needs Civ.Enabled = 1");
            var e = Ensure(s);
            var c = new EmploymentContract
            {
                Id = s.Civ.NextContractId++, Employer = employer, Agent = agent, Worker = worker, Asset = asset, Slot = slot, Terms = terms,
                Status = ContractStatus.Offered, SinceTick = s.Tick, OfferedTick = s.Tick, StatusTick = s.Tick, ProgressTick = s.Tick,
            };
            e.Contracts.Add(c); // ids ascend
            e.Offers++;
            var a = Assets.Find(s, asset);
            Believe(s, s.Population.Find(agent), c, JobBelief.AsEmployer, worker, a);
            return c;
        }

        /// <summary>
        /// The worker's acceptance, perceived by the employer's agent: the contract becomes Active, the worker holds a Work
        /// right on the asset (C1's <see cref="Assets.GrantUse"/>, granted on behalf of the asset's owner; the worker is told
        /// by the grant) and <see cref="EmploymentStarted"/> opens its spell. False (nothing changes) when the agent is not
        /// truly the asset's owner or one of its household (a grant by anyone else is void) or the asset is gone.
        /// </summary>
        public static bool Activate(GodState s, EventBuffer ev, EmploymentContract c)
        {
            if (c == null || c.Status != ContractStatus.Offered) return false;
            var a = Assets.Find(s, c.Asset);
            var agent = s.Population.Find(c.Agent);
            var worker = s.Population.Find(c.Worker);
            if (a == null || !a.IsFinished || agent == null || worker == null || !worker.Alive) return false;
            if (Ownership.OwnerRelation(agent, a.Owner) == Ownership.RightNone) return false;
            bool had = HoldsWorkRight(a, worker.Id);
            if (!had && !Assets.GrantUse(s, ev, a.Id, Owner.Npc(worker.Id), UseKind.Work, a.Owner, -1, c.Id)) return false;
            c.Status = ContractStatus.Active;
            c.StartedTick = s.Tick; c.StatusTick = s.Tick; c.ProgressTick = s.Tick; c.DayOpen = true;
            StateOf(s).Started++;
            ev?.Add(new EmploymentStarted
            {
                Contract = c.Id, EmployerKind = c.Employer.Kind, EmployerId = c.Employer.Id, Agent = c.Agent, Worker = c.Worker, Asset = c.Asset,
                PayUnit = c.Terms.Pay.Kind, PayRef = c.Terms.Pay.UnitRef, PayAmount = c.Terms.Pay.Amount, Per = c.Terms.Per, ShiftTicks = c.Terms.ShiftTicks,
                Control = ControlOf(s, c.Worker),
            });
            Believe(s, worker, c, JobBelief.AsWorker, c.Agent, a);
            Believe(s, agent, c, JobBelief.AsEmployer, c.Worker, a);
            SetBelievedWorkRight(worker, c.Asset, true);
            return true;
        }

        /// <summary>The worker holds a Work right row on the asset (the record, not a permission query).</summary>
        public static bool HoldsWorkRight(ProductiveAsset a, int worker)
        {
            foreach (var r in a.Rights) if (r.Kind == UseKind.Work && r.Holder.Same(Owner.Npc(worker))) return true;
            return false;
        }

        /// <summary>Owner Q19: a walk-out (or an employer's suspension) suspends: no work, no pay due, the slot and the right kept, no timer.</summary>
        public static bool Suspend(GodState s, EventBuffer ev, EmploymentContract c, byte by, byte reason)
        {
            if (c == null || c.Status != ContractStatus.Active) return false;
            c.Status = ContractStatus.Suspended; c.By = by; c.StatusTick = s.Tick;
            StateOf(s).Suspensions++;
            ev?.Add(new ContractSuspended { Contract = c.Id, By = by, Reason = reason, ArrearsUnits = c.ArrearsUnits });
            var w = s.Population.Find(c.Worker);
            if (by == EmploymentContract.ByWorker) SetBelievedWorkRight(w, c.Asset, false); // it holds back its own side: it does not see itself as at work there now
            UpdateBelief(w, c);
            return true;
        }

        /// <summary>Either side takes a suspended contract up again (the worker accepting the old or renegotiated terms).</summary>
        public static bool Resume(GodState s, EventBuffer ev, EmploymentContract c, byte by)
        {
            if (c == null || c.Status != ContractStatus.Suspended) return false;
            c.Status = ContractStatus.Active; c.By = 0; c.StatusTick = s.Tick; c.ProgressTick = s.Tick; c.DayOpen = true;
            StateOf(s).Resumptions++;
            ev?.Add(new ContractResumed { Contract = c.Id, By = by, PayAmount = c.Terms.Pay.Amount });
            var w = s.Population.Find(c.Worker);
            SetBelievedWorkRight(w, c.Asset, true);
            UpdateBelief(w, c);
            UpdateBelief(s.Population.Find(c.Agent), c);
            return true;
        }

        /// <summary>New terms for a suspended or offered contract (an employer's renegotiating offer); the worker takes them up by accepting.</summary>
        public static void Renegotiate(GodState s, EmploymentContract c, in PayTerms terms)
        {
            if (c == null || c.Status == ContractStatus.Ended) return;
            c.Terms = terms;
            UpdateBelief(s.Population.Find(c.Agent), c);
        }

        /// <summary>
        /// The contract closes. An offer that never became a contract (Refused, Lapsed) ends silently (no spell); an open
        /// one ends its Work right and emits <see cref="EmploymentEnded"/>. Both parties' beliefs record the end; waiting
        /// decisions of the worker close with no reward (nothing more will come of them). Ended contracts are never reopened.
        /// </summary>
        public static void End(GodState s, EventBuffer ev, EmploymentContract c, EndReason reason, byte by)
        {
            if (c == null || c.Status == ContractStatus.Ended) return;
            var e = StateOf(s);
            bool wasOpen = c.IsOpen;
            c.Status = ContractStatus.Ended; c.Reason = reason; c.By = by; c.StatusTick = s.Tick;
            c.DueTicks.Clear();
            var w = s.Population.Find(c.Worker);
            if (wasOpen)
            {
                byte ur = reason == EndReason.Died ? UseRightEnded.HolderDied : reason == EndReason.AssetGone ? UseRightEnded.AssetGone : UseRightEnded.ByGrantor;
                if (Assets.Find(s, c.Asset) != null) Assets.EndUse(s, ev, c.Asset, Owner.Npc(c.Worker), UseKind.Work, ur);
                ev?.Add(new EmploymentEnded { Contract = c.Id, Worker = c.Worker, Reason = reason, By = by, ArrearsUnits = c.ArrearsUnits, UnitsEarned = c.UnitsEarned, UnitsPaid = c.UnitsPaid });
                SetBelievedWorkRight(w, c.Asset, false);
                if (w != null && w.Alive) Close(s, ev, w, c.Id);
            }
            else if (reason == EndReason.Refused) e.Refusals++;
            else if (reason == EndReason.Lapsed) e.Lapsed++;
            UpdateBelief(w, c);
            UpdateBelief(s.Population.Find(c.Agent), c);
            for (int i = e.Acts.Count - 1; i >= 0; i--) if (e.Acts[i].Contract == c.Id && e.Acts[i].Act == EmploymentAct.Pay) e.Acts.RemoveAt(i);
        }

        // ================================================================ the payment act (general, spec 22)

        /// <summary>
        /// The general hand-over of a payment (verb 101; K2a routes every payment through it): the payer, within
        /// <see cref="Reach.HandOver"/> of the payee, hands over <paramref name="count"/> of <paramref name="w"/> from lots it
        /// carries that are its own or its household's, one <see cref="Goods.Move"/> per lot (an ordinary <c>ItemMoved</c>
        /// HandOver and <c>OwnershipChanged</c> with the purpose's basis). Returns how many units passed (0: none: out of
        /// reach, nothing to pay with, or no room). Never creates goods.
        /// </summary>
        public static int Pay(GodState s, EventBuffer ev, Npc payer, Npc payee, Ware w, int count, PaymentPurpose purpose, out int lastLot)
        {
            lastLot = 0;
            if (payer == null || payee == null || !payee.Alive || count <= 0) return 0;
            if (s.Territory.Grid.Distance(payer.Node, payee.Node) > Reach.HandOver) return 0;
            if (Carried(payer, w) < count) return 0;
            var basis = purpose == PaymentPurpose.Wage ? OwnershipBasis.Wage : OwnershipBasis.Gift;
            int left = count;
            for (int i = 0; i < payer.Carry.Length && left > 0; i++)
            {
                var l = payer.Carry[i];
                if (l.Empty || l.Ware != w || Ownership.OwnerRelation(payer, l.Owner) == Ownership.RightNone) continue;
                int n = Math.Min(left, l.Count);
                var r = Goods.Move(s, ev, Holder.Carry(payer.Id), l.Id, n, Holder.Carry(payee.Id), MoveReason.HandOver, payer.Id, OwnerRule.ToReceiver, basis: basis);
                if (!r.Ok) break;
                left -= n; lastLot = r.IntoLot;
                i = -1; // the carry changed (a merge or a split): look again from the start
            }
            return count - left;
        }

        /// <summary>Units of a ware a person carries that are its own or its household's.</summary>
        public static int Carried(Npc n, Ware w)
        {
            int have = 0;
            foreach (var l in n.Carry) if (!l.Empty && l.Ware == w && Ownership.OwnerRelation(n, l.Owner) != Ownership.RightNone) have += l.Count;
            return have;
        }

        /// <summary>
        /// A wage hand-over settled <paramref name="units"/> pay units of a contract (due ones first, then arrears): the
        /// counters, <see cref="WagePaid"/>, both parties' beliefs (the worker's trust rises), and the worker's waiting
        /// decisions now wait on the goods it received (food: its realised value when eaten, as M5 does for deals).
        /// </summary>
        private static void Settle(GodState s, EventBuffer ev, EmploymentContract c, Npc payer, Npc worker, Ware w, int count, int units, int lot)
        {
            var tu = s.Tuning.Civ.C3;
            int fromArrears = 0;
            for (int k = 0; k < units; k++)
            {
                if (c.DueTicks.Count > 0) c.DueTicks.RemoveAt(0);
                else if (c.ArrearsUnits > 0) { c.ArrearsUnits--; fromArrears++; }
            }
            c.UnitsPaid += units; c.DayPaid += units; c.ProgressTick = s.Tick;
            StateOf(s).Wages++;
            ev?.Add(new WagePaid { Contract = c.Id, Payer = payer.Id, Worker = worker.Id, Ware = w, Count = count, ForUnits = units, Lot = lot, Control = ControlOf(s, payer.Id) });
            var wm = worker.Mind?.Civ;
            int wi = wm?.IndexOfJob(c.Id) ?? -1;
            if (wi >= 0)
            {
                var b = wm.Jobs[wi];
                b.UnitsPaid += units; b.Unpaid = Math.Max(0, b.Unpaid - fromArrears); b.Trust = (short)Fx.Clamp(b.Trust + tu.TrustGainPaid * units, -1000, 1000); b.Tick = s.Tick;
                wm.Jobs[wi] = b;
            }
            var pm = payer.Mind?.Civ;
            int pi = pm?.IndexOfJob(c.Id) ?? -1;
            if (pi >= 0) { var b = pm.Jobs[pi]; b.UnitsPaid += units; b.Tick = s.Tick; pm.Jobs[pi] = b; }
            // learning: the worker's decisions that waited on this contract now wait on what it was paid (normal goods channel)
            var st = worker.Mind?.Store;
            int key = EpisodeKeyOf(c.Id);
            if (st != null)
            {
                if (lot != 0 && Goods.CreditedLater(s, w)) SocialLearning.Rekey(st, key, Learner.LotEpisodeKey(lot));
                else Learner.Outcome(st, s.Tuning.Learning, PopulationSystem.TraitsOf(s, worker), key, 0, Day(s), Sink(s, ev, worker));
            }
        }

        private static EventLearningSink Sink(GodState s, EventBuffer ev, Npc n)
        {
            var sink = s.Population.GoodsSink;
            sink.Bind(ev, n.Id, EventLearningSink.IsSampledDay(n.Id, Day(s), s.Tuning.Learning.SampleEvery));
            return sink;
        }

        /// <summary>The worker's decisions waiting on the contract close with no reward (a unit went unpaid, or the contract ended).</summary>
        private static void Close(GodState s, EventBuffer ev, Npc worker, int contract)
        {
            var st = worker.Mind?.Store;
            if (st == null) return;
            Learner.Outcome(st, s.Tuning.Learning, PopulationSystem.TraitsOf(s, worker), EpisodeKeyOf(contract), 0, Day(s), Sink(s, ev, worker));
        }

        // ================================================================ beliefs (the sim writes what a party perceives)

        /// <summary>The mind state of a person (created when it first takes part in employment).</summary>
        internal static CivMind MindOf(Npc n) => n?.Mind == null ? null : n.Mind.Civ ?? (n.Mind.Civ = new CivMind());

        /// <summary>A party learns of a contract it takes part in (its own act or the other's gesture it perceived): its belief holds the terms as agreed.</summary>
        private static void Believe(GodState s, Npc n, EmploymentContract c, byte side, int other, ProductiveAsset a)
        {
            var cm = MindOf(n);
            if (cm == null || !n.Alive) return;
            int i = cm.IndexOfJob(c.Id);
            var b = i >= 0 ? cm.Jobs[i] : new JobBelief
            {
                Contract = c.Id, Side = side, Other = other, Asset = c.Asset, Trust = (short)Fx.Clamp(SocialView.Trust(n.Mind, other), -1000, 1000), SinceTick = s.Tick,
            };
            // an employer remembers its refusals by this person (its next offer to it pays more)
            if (i < 0 && side == JobBelief.AsEmployer)
                foreach (var j in cm.Jobs) if (j.Side == JobBelief.AsEmployer && j.Other == other && j.Refusals > b.Refusals) b.Refusals = j.Refusals;
            b.AssetNode = a?.Node ?? b.AssetNode; b.AssetClass = a != null ? AssetTable.Get(a.Kind)?.ObjClass ?? 0 : b.AssetClass;
            b.PayWare = c.Terms.Pay.Ware; b.PayCount = (byte)Fx.Clamp(c.Terms.Pay.Amount, 0, 255); b.Per = c.Terms.Per; b.ShiftTicks = c.Terms.ShiftTicks;
            b.Status = c.Status; b.By = c.By; b.Tick = s.Tick;
            if (i >= 0) { cm.Jobs[i] = b; return; }
            if (cm.Jobs.Count >= CivMind.MaxJobs)
            {
                // forget the oldest ended contract to make room; with none ended the new one is not remembered
                int old = -1;
                for (int k = 0; k < cm.Jobs.Count; k++) if (cm.Jobs[k].Status == ContractStatus.Ended && (old < 0 || cm.Jobs[k].Tick < cm.Jobs[old].Tick)) old = k;
                if (old < 0) return;
                cm.Jobs.RemoveAt(old);
            }
            int at = 0;
            while (at < cm.Jobs.Count && cm.Jobs[at].Contract < c.Id) at++;
            cm.Jobs.Insert(at, b);
        }

        /// <summary>A party perceives the contract's status and terms (it took part in the change, or perceived it).</summary>
        internal static void UpdateBelief(Npc n, EmploymentContract c)
        {
            var cm = n?.Mind?.Civ;
            int i = cm?.IndexOfJob(c.Id) ?? -1;
            if (i < 0) return;
            var b = cm.Jobs[i];
            bool newlyRefused = b.Status != ContractStatus.Ended && c.Status == ContractStatus.Ended && c.Reason == EndReason.Refused;
            b.Status = c.Status; b.By = c.By;
            b.PayWare = c.Terms.Pay.Ware; b.PayCount = (byte)Fx.Clamp(c.Terms.Pay.Amount, 0, 255); b.Per = c.Terms.Per; b.ShiftTicks = c.Terms.ShiftTicks;
            if (newlyRefused && b.Side == JobBelief.AsEmployer && b.Refusals < 255) b.Refusals++;
            cm.Jobs[i] = b;
        }

        /// <summary>C1's belief of the worker's own Work right on the asset: set while it works there under a contract, cleared while it holds back (walk-out) or after the end.</summary>
        private static void SetBelievedWorkRight(Npc w, int asset, bool on)
        {
            var cm = w?.Mind?.Civ;
            int i = cm?.IndexOfAsset(asset) ?? -1;
            if (i < 0) return;
            var b = cm.Own[i];
            byte bit = (byte)(1 << (int)UseKind.Work);
            b.MyRights = on ? (byte)(b.MyRights | bit) : (byte)(b.MyRights & ~bit);
            cm.Own[i] = b;
        }

        /// <summary>
        /// The Sense seam (sim side, each think): the mind gets C3's numbers; a person who heard an offer of work addressed to
        /// it gets its civ mind state; an employer that sees its asset or its worker sees how many units were worked and
        /// whether the worker is working or holding back; a party that sees the other sees the contract's status.
        /// </summary>
        internal static void Sense(GodState s, Npc npc, int sight)
        {
            var e = StateOf(s);
            var cm = npc.Mind?.Civ;
            if (cm == null && npc.Mind?.Social != null && HeardOffer(npc)) cm = MindOf(npc);
            if (cm == null) return;
            cm.C3 = s.Tuning.Civ.C3;
            if (e == null) return;
            var g = s.Territory.Grid;
            for (int i = 0; i < cm.Jobs.Count; i++)
            {
                var b = cm.Jobs[i];
                var c = Find(s, b.Contract);
                if (c == null) continue;
                var other = s.Population.Find(b.Other);
                bool otherSeen = other != null && other.Alive && g.Distance(npc.Node, other.Node) <= sight;
                bool assetSeen = b.AssetNode > 0 && g.Distance(npc.Node, b.AssetNode) <= sight;
                if (b.Side == JobBelief.AsEmployer && (assetSeen || otherSeen)) b.UnitsWorked = c.UnitsEarned;
                if (otherSeen || assetSeen && b.Side == JobBelief.AsEmployer) { b.Status = c.Status; b.By = c.By; }
                cm.Jobs[i] = b;
            }
        }

        private static bool HeardOffer(Npc npc)
        {
            var m = npc.Mind.Social;
            for (int i = 0; i < m.InboxCount; i++)
            {
                var msg = m.Inbox[i].Msg;
                if (msg.Act == GestureAct.Offer && msg.Topic.Kind == TopicKind.Posting && msg.Topic.Sub == npc.Id) return true;
            }
            return false;
        }

        // ================================================================ messages (M5's gesture language)

        /// <summary>
        /// An offer of work: to anyone watching (M5 answers only offers of wares), naming the person it is meant for. Topic
        /// Posting (id the contract, sub the person), quantity the pay per unit, counter the pay ware (sub the asset).
        /// </summary>
        private static int EmitOffer(GodState s, EventBuffer ev, Npc agent, EmploymentContract c)
        {
            var msg = Messages.Compose(GestureAct.Offer, new MsgTopic { Kind = TopicKind.Posting, Id = c.Id, Sub = c.Worker }, c.Terms.Pay.Amount,
                new MsgTopic { Kind = TopicKind.Ware, Id = c.Terms.Pay.UnitRef, Sub = c.Asset }, 0, MsgTime.Now, 0, Carried(agent, c.Terms.Pay.Ware) > 0);
            int id = Messages.Emit(s, ev, agent, msg);
            c.OfferMessage = id; c.OfferMessageTick = s.Tick;
            return id;
        }

        /// <summary>An answer addressed to the other party (Accept or Refuse), about the posting.</summary>
        private static int EmitAnswer(GodState s, EventBuffer ev, Npc speaker, int to, GestureAct act, EmploymentContract c) =>
            Messages.Emit(s, ev, speaker, Messages.Compose(act, new MsgTopic { Kind = TopicKind.Posting, Id = c.Id, Sub = c.Asset }, 0, default, to));

        /// <summary>Whether <paramref name="listener"/> decoded message <paramref name="id"/> as <paramref name="act"/> (its own perception).</summary>
        private static bool Perceived(Npc listener, int id, GestureAct act)
        {
            var m = listener?.Mind?.Social;
            if (m == null) return false;
            for (int i = 0; i < m.InboxCount; i++) if (m.Inbox[i].Msg.MessageId == id) return m.Inbox[i].Msg.Act == act;
            return false;
        }

        // ================================================================ acts (called through CivHooks)

        private static int ActOf(EmploymentState e, int npc)
        {
            for (int i = 0; i < e.Acts.Count; i++) if (e.Acts[i].Npc == npc) return i;
            return -1;
        }

        private static void SetAct(EmploymentState e, EmploymentAct a)
        {
            int i = ActOf(e, a.Npc);
            if (i >= 0) { e.Acts[i] = a; return; }
            int at = 0;
            while (at < e.Acts.Count && e.Acts[at].Npc < a.Npc) at++;
            e.Acts.Insert(at, a);
        }

        private static void ClearAct(EmploymentState e, int npc) { int i = ActOf(e, npc); if (i >= 0) e.Acts.RemoveAt(i); }

        /// <summary>The plan the mind made for this candidate (its terms), or -1.</summary>
        private static int PlanOf(CivMind cm, in Cognition.Candidate c)
        {
            if (cm == null) return -1;
            for (int i = 0; i < cm.PlanCount; i++)
            {
                var p = cm.Plans[i];
                if (p.Verb == c.Verb && p.Intent == c.Intent && p.Kind == c.Kind && p.Target == c.TargetId) return i;
            }
            return -1;
        }

        public static void Start(GodState s, EventBuffer ev, Npc npc, in Cognition.Candidate c, int day, ref bool handled)
        {
            if (handled || !IsMine(c.Verb) || s.Civ == null) return;
            handled = true;
            var e = Ensure(s);
            var cm = npc.Mind?.Civ;
            int pi = PlanOf(cm, c);
            if (pi < 0 || s.Social == null) { ClearAct(e, npc.Id); Frustrate(s, npc); return; }
            var plan = cm.Plans[pi];
            // re-choosing the running payment walk continues it
            int ai = ActOf(e, npc.Id);
            if (ai >= 0 && e.Acts[ai].Act == EmploymentAct.Pay && plan.Act == EmploymentAct.Pay && e.Acts[ai].Contract == plan.Contract) return;
            ClearAct(e, npc.Id);
            var act = new EmploymentAct { Npc = npc.Id, Act = plan.Act, Contract = plan.Contract, StartTick = s.Tick };
            switch (plan.Act)
            {
                case EmploymentAct.Offer: StartOffer(s, ev, npc, ref plan, ref act); break;
                case EmploymentAct.Dismiss: StartDismiss(s, ev, npc, plan, ref act); break;
                case EmploymentAct.Accept: StartAccept(s, ev, npc, plan, ref act); break;
                case EmploymentAct.Refuse: StartRefuse(s, ev, npc, plan, ref act); break;
                case EmploymentAct.Pay: break; // walks in Act
            }
            cm.Plans[pi] = plan; // the episode key reads the contract the act made
            if (act.Act != 0) SetAct(e, act);
        }

        private static void StartOffer(GodState s, EventBuffer ev, Npc npc, ref C3Plan plan, ref EmploymentAct act)
        {
            var worker = s.Population.Find(plan.Target);
            var a = Assets.Find(s, plan.Asset);
            if (worker == null || !worker.Alive || a == null) { act.Act = 0; Frustrate(s, npc); return; }
            var terms = new PayTerms { Pay = PayUnit.Of(plan.Ware, plan.Count), Per = PayPer.Shift, ShiftTicks = s.Tuning.Civ.C3.ShiftTicks };
            var c = Find(s, plan.Contract);
            if (c != null && c.Status == ContractStatus.Suspended && c.Agent == npc.Id) Renegotiate(s, c, terms); // a renegotiating offer to a worker who walked out
            else c = Hire(s, ev, Owner.Npc(npc.Id), npc.Id, worker.Id, a.Id, terms);
            plan.Contract = c.Id; act.Contract = c.Id;
            EmitOffer(s, ev, npc, c);
        }

        private static void StartDismiss(GodState s, EventBuffer ev, Npc npc, in C3Plan plan, ref EmploymentAct act)
        {
            var c = Find(s, plan.Contract);
            if (c == null || !c.IsOpen || c.Agent != npc.Id) { act.Act = 0; Frustrate(s, npc); return; }
            EmitAnswer(s, ev, npc, c.Worker, GestureAct.Refuse, c);
            End(s, ev, c, EndReason.Dismissed, EmploymentContract.ByEmployer);
        }

        /// <summary>
        /// The worker answers yes (an offer, a renegotiated offer, or taking up its own suspended work) and, when the contract
        /// is (now) active, goes to work: the action becomes C1's Work act on the asset, under the contract's Work right.
        /// </summary>
        private static void StartAccept(GodState s, EventBuffer ev, Npc npc, in C3Plan plan, ref EmploymentAct act)
        {
            var c = Find(s, plan.Contract);
            if (c == null || c.Worker != npc.Id || c.Status == ContractStatus.Ended) { act.Act = 0; Frustrate(s, npc); return; }
            var agent = s.Population.Find(c.Agent);
            if (c.Status == ContractStatus.Offered)
            {
                int id = EmitAnswer(s, ev, npc, c.Agent, GestureAct.Accept, c);
                // the agreement exists when the employer perceived the yes (its own decoding); otherwise nothing changed
                if (!Perceived(agent, id, GestureAct.Accept) || !Activate(s, ev, c)) return; // a gesture only
            }
            else if (c.Status == ContractStatus.Suspended)
            {
                int id = EmitAnswer(s, ev, npc, c.Agent, GestureAct.Accept, c);
                if (c.By == EmploymentContract.ByWorker || Perceived(agent, id, GestureAct.Accept)) Resume(s, ev, c, EmploymentContract.ByWorker);
                else return;
            }
            if (c.Status != ContractStatus.Active) return;
            var a = Assets.Find(s, c.Asset);
            if (a == null) { act.Act = 0; Frustrate(s, npc); return; }
            // to work: C1's Work act (the same reach, input and output rules for every worker)
            act.Act = 0;
            npc.Action = new NpcAction { Verb = Work.VerbWork, TargetKind = IntentTarget.Building, TargetId = a.Id, TargetNode = a.Node, Since = s.Tick };
        }

        /// <summary>The worker answers no: an offer is refused; an active contract is suspended (a walk-out); a suspended one is given up (resigned).</summary>
        private static void StartRefuse(GodState s, EventBuffer ev, Npc npc, in C3Plan plan, ref EmploymentAct act)
        {
            var c = Find(s, plan.Contract);
            if (c == null || c.Worker != npc.Id || c.Status == ContractStatus.Ended) { act.Act = 0; Frustrate(s, npc); return; }
            int id = EmitAnswer(s, ev, npc, c.Agent, GestureAct.Refuse, c);
            var agent = s.Population.Find(c.Agent);
            switch (c.Status)
            {
                case ContractStatus.Offered: End(s, ev, c, EndReason.Refused, EmploymentContract.ByWorker); break;
                case ContractStatus.Active: Suspend(s, ev, c, EmploymentContract.ByWorker, c.ArrearsUnits > 0 ? ContractSuspended.Unpaid : ContractSuspended.Other); break;
                case ContractStatus.Suspended: End(s, ev, c, EndReason.Resigned, EmploymentContract.ByWorker); break;
            }
            if (Perceived(agent, id, GestureAct.Refuse)) UpdateBelief(agent, c);
        }

        public static void Act(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled)
        {
            if (handled || !IsMine(npc.Action.Verb) || s.Civ == null) return;
            handled = true;
            var e = Ensure(s);
            int ai = ActOf(e, npc.Id);
            if (ai < 0) { EndAction(npc); return; }
            var a = e.Acts[ai];
            if (a.Act != EmploymentAct.Pay)
            {
                // a gesture lasts as long as M5's gestures
                if (s.Tick >= a.StartTick + s.Tuning.Social.GestureTicks) { e.Acts.RemoveAt(ai); EndAction(npc); }
                return;
            }
            PayAct(s, ev, npc, e, ai);
        }

        /// <summary>
        /// The Pay act (verb 101, purpose Wage): fetch the pay from the asset's store when it does not carry it (walked to
        /// within <see cref="Reach.Store"/>, a Withdraw of goods the employer owns), walk to the worker, hand it over within
        /// <see cref="Reach.HandOver"/>. Start, reach check and completion in this one function (spec 22).
        /// </summary>
        private static void PayAct(GodState s, EventBuffer ev, Npc npc, EmploymentState e, int ai)
        {
            var a = e.Acts[ai];
            var c = Find(s, a.Contract);
            var worker = c != null ? s.Population.Find(c.Worker) : null;
            if (c == null || c.Agent != npc.Id || worker == null || !worker.Alive || c.Status == ContractStatus.Ended || c.DueUnits + c.ArrearsUnits <= 0)
            { e.Acts.RemoveAt(ai); Frustrate(s, npc); return; }
            if (s.Tick - a.StartTick > s.Tuning.Social.HandOverWalkTicks * 2) { e.Acts.RemoveAt(ai); Frustrate(s, npc); return; }
            var w = c.Terms.Pay.Ware;
            int units = Math.Min(c.DueUnits + c.ArrearsUnits, Math.Max(1, PlannedUnits(npc, c.Id)));
            int amount = c.Terms.Pay.Amount * units;
            if (a.Phase == 0)
            {
                if (Carried(npc, w) < amount)
                {
                    var asset = Assets.Find(s, c.Asset);
                    var bin = asset != null && asset.Store != 0 ? Goods.BinById(s.Population, asset.Store) : null;
                    int inStore = 0;
                    if (bin != null) foreach (var l in bin.Lots) if (l.Ware == w && Ownership.OwnerRelation(npc, l.Owner) != Ownership.RightNone) inStore += l.Count;
                    if (bin == null || inStore + Carried(npc, w) < c.Terms.Pay.Amount) { e.Acts.RemoveAt(ai); Frustrate(s, npc); return; }
                    if (!Work.WalkTo(s, npc, bin.Node, Reach.Store)) { if (npc.Path == null) { e.Acts.RemoveAt(ai); Frustrate(s, npc); } return; }
                    int need = amount - Carried(npc, w);
                    for (int i = 0; i < bin.Lots.Count && need > 0;)
                    {
                        var l = bin.Lots[i];
                        if (l.Ware != w || Ownership.OwnerRelation(npc, l.Owner) == Ownership.RightNone) { i++; continue; }
                        int n = Math.Min(need, Math.Min(l.Count, PopulationModule.Room(s, npc, w)));
                        if (n <= 0) break;
                        var r = Goods.Move(s, ev, Holder.Bin(bin.Id), l.Id, n, Holder.Carry(npc.Id), MoveReason.Withdraw, npc.Id, OwnerRule.Keep);
                        if (!r.Ok) break;
                        need -= n;
                        if (n < l.Count) i++;
                    }
                }
                a.Phase = 1; e.Acts[ai] = a;
                return;
            }
            // pay what it carries, whole units only
            units = Math.Min(units, Carried(npc, w) / Math.Max(1, c.Terms.Pay.Amount));
            if (units <= 0) { e.Acts.RemoveAt(ai); Frustrate(s, npc); return; }
            if (s.Territory.Grid.Distance(npc.Node, worker.Node) > Reach.HandOver)
            {
                if (!Work.WalkTo(s, npc, worker.Node, Reach.HandOver) && npc.Path == null) { e.Acts.RemoveAt(ai); Frustrate(s, npc); }
                return;
            }
            int paid = Pay(s, ev, npc, worker, w, units * c.Terms.Pay.Amount, PaymentPurpose.Wage, out int lot);
            if (paid <= 0) { e.Acts.RemoveAt(ai); Frustrate(s, npc); return; }
            Settle(s, ev, c, npc, worker, w, paid, Math.Max(1, paid / Math.Max(1, c.Terms.Pay.Amount)), lot);
            e.Acts.RemoveAt(ai);
            EndAction(npc);
        }

        /// <summary>How many units the payer meant to pay (its plan), 0 when unknown.</summary>
        private static int PlannedUnits(Npc npc, int contract)
        {
            var cm = npc.Mind?.Civ;
            if (cm == null) return 0;
            for (int i = 0; i < cm.PlanCount; i++) if (cm.Plans[i].Act == EmploymentAct.Pay && cm.Plans[i].Contract == contract) return cm.Plans[i].Units;
            return 0;
        }

        private static void EndAction(Npc npc)
        {
            npc.Action = new NpcAction { Since = npc.Action.Since };
            npc.Path = null; npc.PathGoal = -1;
            if (npc.Mind != null) Learner.EndOngoing(npc.Mind.Store);
        }

        private static void Frustrate(GodState s, Npc npc)
        {
            if (npc.Mind != null) Learner.AddReward(npc.Mind.Store, s.Tuning.Learning.Frustration);
            EndAction(npc);
        }

        // ================================================================ accounting (CivSystem 257)

        /// <summary>
        /// Every tick: work at the asset under an active contract makes shifts (or output units) that fall due; a unit past
        /// its grace without pay becomes arrears (the worker experiences it: its belief and trust fall, its waiting decisions
        /// get nothing); an offer unanswered lapses; an active contract without progress is abandoned; a suspended one whose
        /// worker works again is resumed. Reads the tick's <see cref="AssetUsed"/> events (sim side, recording-independent).
        /// </summary>
        public static void Tick(GodState s, EventBuffer ev)
        {
            var e = StateOf(s);
            if (e == null || e.Contracts.Count == 0) return;
            var tu = s.Tuning.Civ.C3;
            var items = ev.Items;
            int from = items.Count;
            while (from > 0 && items[from - 1].Tick == s.Tick) from--;
            int to = items.Count;
            var g = s.Territory.Grid;
            for (int k = 0; k < e.Contracts.Count; k++)
            {
                var c = e.Contracts[k];
                switch (c.Status)
                {
                    case ContractStatus.Offered:
                        if (s.Tick - c.OfferedTick > tu.OfferTicks) End(s, ev, c, EndReason.Lapsed, EmploymentContract.BySim);
                        continue;
                    case ContractStatus.Ended:
                        continue;
                }
                var w = s.Population.Find(c.Worker);
                if (w == null || !w.Alive) continue; // OnDeath ends it
                bool working = w.Action.Verb == Work.VerbWork && w.Action.TargetId == c.Asset;
                var a = working ? Assets.Find(s, c.Asset) : null;
                if (a != null && g.Distance(w.Node, a.Node) > Reach.Touch) working = false;
                int outCount = 0, cycles = 0;
                for (int i = from; i < to; i++)
                    if (items[i] is AssetUsed u && u.Worker == c.Worker && u.Asset == c.Asset) { outCount += u.OutputCount; cycles++; }
                if (c.Status == ContractStatus.Suspended)
                {
                    if (working || cycles > 0) Resume(s, ev, c, EmploymentContract.ByWorker); // it went back to work: it took the job up again
                    else continue;
                }
                if (working)
                {
                    c.WorkTicks++; c.DayWorkTicks++; c.ProgressTick = s.Tick;
                    if (c.Terms.Per == PayPer.Shift && ++c.ShiftProgress >= Math.Max(1, c.Terms.ShiftTicks)) { c.ShiftProgress = 0; Earned(s, c, w); }
                }
                if (cycles > 0)
                {
                    c.OutputCount += outCount; c.DayOutput += outCount; c.ProgressTick = s.Tick;
                    if (c.Terms.Per == PayPer.Unit) for (int q = 0; q < cycles; q++) Earned(s, c, w);
                    EmployerOutput(s, ev, c, items, from, to);
                }
                // pay past its grace: arrears (no rule forces payment)
                while (c.DueTicks.Count > 0 && s.Tick - c.DueTicks[0] > tu.PayGraceTicks) { c.DueTicks.RemoveAt(0); Unpaid(s, ev, c, w); }
                if (c.Status == ContractStatus.Active && s.Tick - c.ProgressTick > tu.AbandonTicks) End(s, ev, c, EndReason.Abandoned, EmploymentContract.BySim);
            }
        }

        /// <summary>A pay unit fell due: the worker knows its own work.</summary>
        private static void Earned(GodState s, EmploymentContract c, Npc w)
        {
            c.DueTicks.Add(s.Tick); c.UnitsEarned++;
            var cm = w.Mind?.Civ;
            int i = cm?.IndexOfJob(c.Id) ?? -1;
            if (i >= 0) { var b = cm.Jobs[i]; b.UnitsWorked++; b.Tick = s.Tick; cm.Jobs[i] = b; }
        }

        private static void Unpaid(GodState s, EventBuffer ev, EmploymentContract c, Npc w)
        {
            var tu = s.Tuning.Civ.C3;
            c.ArrearsUnits++; c.UnitsUnpaid++; c.DayUnpaid++;
            StateOf(s).Unpaid++;
            // C2 not merged: a counter only (recorded as a default); Obligation 0
            ev?.Add(new WageUnpaid { Contract = c.Id, Worker = c.Worker, Units = 1, Arrears = c.ArrearsUnits, Obligation = 0 });
            var cm = w.Mind?.Civ;
            int i = cm?.IndexOfJob(c.Id) ?? -1;
            if (i >= 0)
            {
                var b = cm.Jobs[i];
                b.Unpaid++; b.Trust = (short)Fx.Clamp(b.Trust - tu.TrustLossUnpaid, -1000, 1000); b.Tick = s.Tick;
                cm.Jobs[i] = b;
            }
            Close(s, ev, w, c.Id); // unpaid: the decisions that waited get no reward
        }

        /// <summary>The employer's decisions to hire wait on the contract: the output of its worker reaches them (the goods channel's worth).</summary>
        private static void EmployerOutput(GodState s, EventBuffer ev, EmploymentContract c, System.Collections.Generic.IReadOnlyList<SimEvent> items, int from, int to)
        {
            var agent = s.Population.Find(c.Agent);
            var st = agent?.Mind?.Store;
            if (st == null || !agent.Alive) return;
            int worth = 0;
            for (int i = from; i < to; i++)
                if (items[i] is AssetUsed u && u.Worker == c.Worker && u.Asset == c.Asset && u.OutputWare != Ware.None) worth += GoodsWorth.Of(s, agent, u.OutputWare) * u.OutputCount;
            if (worth != 0) Learner.Accrue(st, s.Tuning.Learning, PopulationSystem.TraitsOf(s, agent), EpisodeKeyOf(c.Id), worth, Day(s), Sink(s, ev, agent));
        }

        /// <summary>
        /// Day start: one <see cref="JobDay"/> per contract open during the day that ended; contracts whose asset is gone end
        /// (AssetGone); ended contracts are dropped after <c>KeepEndedDays</c> (offers that never started after one day).
        /// </summary>
        public static void DayStart(GodState s, EventBuffer ev, int day)
        {
            var e = StateOf(s);
            if (e == null) return;
            e.LastDay = day;
            var tu = s.Tuning.Civ.C3;
            int tpd = SimCalendar.Of(s).TicksPerDay;
            for (int k = 0; k < e.Contracts.Count; k++)
            {
                var c = e.Contracts[k];
                if (c.DayOpen && day > 0)
                    ev?.Add(new JobDay { Contract = c.Id, Worker = c.Worker, Day = day - 1, WorkTicks = c.DayWorkTicks, OutputCount = c.DayOutput, PaidUnits = c.DayPaid, UnpaidUnits = c.DayUnpaid, Status = c.Status });
                c.DayWorkTicks = 0; c.DayOutput = 0; c.DayPaid = 0; c.DayUnpaid = 0; c.DayOpen = c.IsOpen;
                if (c.IsOpen && Assets.Find(s, c.Asset) == null) End(s, ev, c, EndReason.AssetGone, EmploymentContract.BySim);
            }
            for (int k = e.Contracts.Count - 1; k >= 0; k--)
            {
                var c = e.Contracts[k];
                if (c.Status != ContractStatus.Ended || c.DayOpen) continue;
                bool started = c.StartedTick > 0 || c.Reason != EndReason.Refused && c.Reason != EndReason.Lapsed;
                long keep = (started ? tu.KeepEndedDays : 1) * (long)tpd;
                if (s.Tick - c.StatusTick < keep) continue;
                e.Contracts.RemoveAt(k);
                Forget(s.Population.Find(c.Worker), c.Id);
                Forget(s.Population.Find(c.Agent), c.Id, keepRefusals: true);
            }
        }

        /// <summary>A record dropped from the books is forgotten by its parties too (an employer keeps a refused offer's memory: its next offer pays more).</summary>
        private static void Forget(Npc n, int contract, bool keepRefusals = false)
        {
            var cm = n?.Mind?.Civ;
            int i = cm?.IndexOfJob(contract) ?? -1;
            if (i < 0) return;
            if (keepRefusals && cm.Jobs[i].Refusals > 0) return;
            cm.Jobs.RemoveAt(i);
        }

        /// <summary>A party died: its contracts end (Died; offers lapse). Resurrection never restores them (D5.13).</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            var e = StateOf(s);
            if (e == null) return;
            foreach (var c in e.Contracts)
            {
                if (c.Status == ContractStatus.Ended || c.Worker != npc.Id && c.Agent != npc.Id) continue;
                End(s, ev, c, c.Status == ContractStatus.Offered ? EndReason.Lapsed : EndReason.Died, EmploymentContract.BySim);
            }
            ClearAct(e, npc.Id);
        }

        /// <summary>C3's invariants: ascending unique contract ids below the counter; an active contract's worker holds a Work right on its asset.</summary>
        public static void Check(GodState s, System.Collections.Generic.List<string> bad)
        {
            var e = StateOf(s);
            if (e == null) return;
            int last = 0;
            foreach (var c in e.Contracts)
            {
                if (c.Id <= last) bad.Add("CIV3: contract ids not ascending at " + c.Id);
                if (c.Id >= s.Civ.NextContractId) bad.Add("CIV3: contract " + c.Id + " not below NextContractId");
                last = c.Id;
                if (c.Status == ContractStatus.Active)
                {
                    var a = Assets.Find(s, c.Asset);
                    var w = s.Population.Find(c.Worker);
                    if (a != null && w != null && w.Alive && !HoldsWorkRight(a, w.Id)) bad.Add("CIV3: active contract " + c.Id + " without the worker's Work right");
                }
                if (c.ArrearsUnits < 0) bad.Add("CIV3: contract " + c.Id + " negative arrears");
            }
        }
    }
}
