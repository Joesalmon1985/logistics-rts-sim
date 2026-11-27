using System;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// The organisation's store and its services by people (INSTITUTIONAL_SERVICES 5.3; C8's Tier 2 taken by C6): members
    /// deposit food into the store bin (a real <see cref="LotBin"/> owned by the organisation, basis
    /// <see cref="OwnershipBasis.OrgContribution"/>); a hungry member gestures a request at the keeper; the keeper judges it from
    /// what it knows (the roster, the ration it believes holds), walks to the store, withdraws, walks to the requester and hands
    /// over within reach (<see cref="WelfareGranted"/>, <see cref="ServiceDelivered"/>). No branch on the ownership model or on
    /// the owner kind (ORG-INV-5): the store is whatever bin the organisation's record names, its goods those of its owner value.
    /// </summary>
    public static class CommunalStore
    {
        public static int FoodIn(LotBin b)
        {
            int n = 0;
            foreach (var l in b.Lots) if (Wares.IsFood(l.Ware) && l.Ware != Ware.Nightshade) n += l.Count;
            return n;
        }

        private static OrgC8Tuning W(GodState s) => s.Tuning.Orgs.C8;

        // ---------------------------------------------------------------- the request (a member at the keeper)

        /// <summary>A hungry member gestures its need at the keeper it sees (NeedFood, topic: the organisation's ration service).</summary>
        public static void StartRequest(GodState s, EventBuffer ev, Npc npc, OrgMind m, in Candidate c)
        {
            var keeper = s.Population.Find(c.TargetId);
            int bi = -1;
            for (int i = 0; i < m.Count; i++) if (m.Beliefs[i].MyRole != 0 && m.Beliefs[i].BelievedHead == c.TargetId) { bi = i; break; }
            if (keeper == null || !Bodies.IsLivingPerson(keeper) || bi < 0) { OrgActs.Frustrate(s, npc); return; }
            m.Beliefs[bi].LastAsk = (int)s.Tick;
            m.Act.Org = m.Beliefs[bi].Org;
            int id = OrgActs.Say(s, ev, npc, GestureAct.NeedFood, new MsgTopic { Kind = TopicKind.Service, Id = m.Beliefs[bi].Org, Sub = (int)ServiceKind.Ration }, keeper.Id, MsgTime.Now);
            OrgActs.Ensure(s, keeper, id);
        }

        /// <summary>
        /// Per tick (<see cref="OrganisationSystem"/>): a request a keeper perceived is written into the organisation's ring by that
        /// keeper (it is the role-holder who received it); the keeper's mind keeps the request id.
        /// </summary>
        public static void OpenPerceived(GodState s, EventBuffer ev, Organisation org)
        {
            var p = s.Population;
            foreach (var o in org.Offices)
            {
                if (o.Holder == 0 || (o.Powers & OrgPower.KeepStore) == 0) continue;
                var k = p.Find(o.Holder);
                var m = k?.Mind?.Orgs;
                if (m == null || !Bodies.IsLivingPerson(k)) continue;
                for (int i = 0; i < m.KnownCount; i++)
                {
                    if (m.Known[i].Org != org.Id || m.Known[i].Request != 0 || m.Known[i].Done) continue;
                    var kr = m.Known[i];
                    m.Known[i].Request = Services.Open(s, ev, org, kr.Kind, kr.Requester, SubjectKind.Npc, kr.Requester, org.Node, (int)Ware.None, 0, kr.SeenNeed, k.Id, k.Id);
                    OrgRecords.Witness(s, org, k.Node, RecordKind.Request, m.Known[i].Request, Ware.None, 1, k.Id);
                }
            }
        }

        // ---------------------------------------------------------------- serving (the keeper)

        /// <summary>
        /// The keeper takes the next request it knows of (by the queue order it believes). Eligibility as it sees it: the
        /// requester (as it recognised it) is on the roster under a members-only policy. Refused: <see cref="ServiceRefused"/>
        /// and a visible Refuse. Accepted: <see cref="ServiceAccepted"/>, then the walk to the store.
        /// </summary>
        public static void StartServe(GodState s, EventBuffer ev, Npc npc, OrgMind m, in Candidate c)
        {
            int ki = -1;
            for (int i = 0; i < m.KnownCount; i++) if (!m.Known[i].Done && m.Known[i].Requester == c.TargetId) { ki = i; break; }
            if (ki < 0) { OrgActs.Frustrate(s, npc); return; }
            var kr = m.Known[ki];
            var org = Orgs.Find(s, kr.Org);
            var requester = s.Population.Find(c.TargetId);
            if (org == null || org.Status == OrgStatus.Dissolved || Orgs.OfficeWith(org, npc.Id, OrgPower.KeepStore) < 0 || requester == null || !Bodies.IsLivingPerson(requester))
            {
                m.Known[ki].Done = true; OrgActs.Frustrate(s, npc); return;
            }
            if (kr.Request == 0) m.Known[ki].Request = kr.Request = Services.Open(s, ev, org, kr.Kind, kr.Requester, SubjectKind.Npc, kr.Requester, org.Node, 0, 0, kr.SeenNeed, npc.Id, npc.Id);
            int bi = m.IndexOfOrg(org.Id);
            bool membersOnly = org.PolicyOf(OrgPolicyKey.Eligibility, Eligibility.MembersOnly) == Eligibility.MembersOnly;
            if (membersOnly && !org.IsMember(kr.Requester))
            {
                RefuseNow(s, ev, npc, m, ki, org, requester, ServiceRefused.NotMember);
                return;
            }
            if (!Services.Accept(s, ev, org, kr.Request, npc.Id)) { m.Known[ki].Done = true; OrgActs.Frustrate(s, npc); return; }
            m.Act.Org = org.Id; m.Act.Request = kr.Request; m.Act.Bin = org.Stores.Count > 0 ? org.Stores[0] : 0; m.Act.Phase = 0;
            m.Act.Count = Math.Max(1, bi >= 0 && m.Beliefs[bi].Ration > 0 ? m.Beliefs[bi].Ration : org.PolicyOf(OrgPolicyKey.RationPerRequest, W(s).StartRation));
        }

        private static void RefuseNow(GodState s, EventBuffer ev, Npc keeper, OrgMind m, int ki, Organisation org, Npc requester, byte reason)
        {
            Services.Refuse(s, ev, org, m.Known[ki].Request, keeper.Id, reason);
            OrgActs.Say(s, ev, keeper, GestureAct.Refuse, new MsgTopic { Kind = TopicKind.Service, Id = org.Id, Sub = (int)ServiceKind.Ration }, requester.Id, MsgTime.Now);
            OrgRecords.Witness(s, org, keeper.Node, RecordKind.Refusal, m.Known[ki].Request, Ware.None, 0, keeper.Id);
            m.Known[ki].Done = true;
            OrgActs.End(keeper);
        }

        /// <summary>
        /// The keeper's activity: phase 0 walk to the store and withdraw up to the ration (the lots stay the organisation's while
        /// carried); phase 1 walk to the requester and hand over within <see cref="Reach.HandOver"/>. The outcome is the hand-over
        /// itself (the recipient's real holdings). A keeper who never reaches the requester still carries the organisation's food.
        /// </summary>
        public static void Serve(GodState s, EventBuffer ev, Npc npc, OrgMind m)
        {
            var org = Orgs.Find(s, m.Act.Org);
            int ki = -1;
            for (int i = 0; i < m.KnownCount; i++) if (m.Known[i].Request == m.Act.Request && m.Act.Request != 0) { ki = i; break; }
            var requester = s.Population.Find(m.Act.Target);
            if (org == null || org.Status == OrgStatus.Dissolved || requester == null || !Bodies.IsLivingPerson(requester)) { if (ki >= 0) m.Known[ki].Done = true; OrgActs.Frustrate(s, npc); return; }
            if (s.Tick - m.Act.StartTick > s.Tuning.Orgs.C6.RequestExpireTicks) { if (ki >= 0) m.Known[ki].Done = true; OrgActs.Frustrate(s, npc); return; }
            if (m.Act.Phase == 0)
            {
                var bin = Goods.BinById(s.Population, m.Act.Bin);
                if (bin == null) { OrgActs.Frustrate(s, npc); return; }
                if (!OrgActs.WalkTo(s, npc, bin.Node, Reach.Store)) { if (npc.Path == null) OrgActs.Frustrate(s, npc); return; }
                int want = m.Act.Count, took = 0, lot = 0;
                Ware ware = Ware.None;
                for (int i = 0; i < bin.Lots.Count && took < want;)
                {
                    var l = bin.Lots[i];
                    if (!Wares.IsFood(l.Ware) || l.Ware == Ware.Nightshade || !l.Owner.Same(Orgs.OwnerOf(org.Id)) || ware != Ware.None && l.Ware != ware) { i++; continue; }
                    int n = Math.Min(want - took, l.Count);
                    var r = Goods.Move(s, ev, Holder.Bin(bin.Id), l.Id, n, Holder.Carry(npc.Id), MoveReason.Withdraw, npc.Id, OwnerRule.Keep, purposeRef: m.Act.Request);
                    if (!r.Ok) break;
                    ware = l.Ware; took += n; lot = r.IntoLot;
                    if (n < l.Count) i++;
                }
                if (took == 0)
                {
                    if (ki >= 0) RefuseNow(s, ev, npc, m, ki, org, requester, ServiceRefused.StoreEmpty); else OrgActs.Frustrate(s, npc);
                    return;
                }
                OrgRecords.Witness(s, org, bin.Node, RecordKind.Withdrawal, m.Act.Request, ware, took, npc.Id);
                m.Act.Lot = lot; m.Act.Count = took; m.Act.Phase = 1;
                return;
            }
            // phase 1: to the requester
            var g = s.Territory.Grid;
            if (g.Distance(npc.Node, requester.Node) > Reach.HandOver)
            {
                OrgActs.WalkTo(s, npc, requester.Node, Reach.HandOver);
                if (g.Distance(npc.Node, requester.Node) > Reach.HandOver) { if (npc.Path == null) OrgActs.Frustrate(s, npc); return; }
            }
            int lotId = 0, have = 0;
            foreach (var l in npc.Carry) if (!l.Empty && l.Owner.Same(Orgs.OwnerOf(org.Id)) && Wares.IsFood(l.Ware) && l.Count > have) { lotId = l.Id; have = l.Count; }
            if (lotId == 0) { OrgActs.Frustrate(s, npc); return; }
            int give = Math.Min(have, m.Act.Count);
            Ware w = Ware.None;
            foreach (var l in npc.Carry) if (l.Id == lotId) w = l.Ware;
            var mv = Goods.Move(s, ev, Holder.Carry(npc.Id), lotId, give, Holder.Carry(requester.Id), MoveReason.HandOver, npc.Id, OwnerRule.ToReceiver,
                purposeRef: m.Act.Request, basis: OwnershipBasis.Gift);
            if (!mv.Ok) { OrgActs.Frustrate(s, npc); return; }
            byte control = s.Divinity != null ? (byte)Divinity.Control.Of(s, npc.Id) : (byte)0;
            ev?.Add(new WelfareGranted { Org = org.Id, Keeper = npc.Id, Recipient = requester.Id, Ware = w, Count = give, Request = m.Act.Request, Control = control });
            ev?.Add(new OrgSpent { Org = org.Id, Actor = npc.Id, Category = LedgerCause.Ration, Ware = w, Count = give, Payee = requester.Id, Request = m.Act.Request, Control = control });
            org.Ledger.Book(w, LedgerCause.Ration, 0, give);
            s.Organisations.Rations++;
            Services.Delivered(s, ev, org, m.Act.Request, npc.Id, (int)(s.Tick - m.Act.StartTick), give, 551);
            OrgRecords.Witness(s, org, npc.Node, RecordKind.Outcome, m.Act.Request, w, 1, npc.Id);
            if (ki >= 0) m.Known[ki].Done = true;
            // the recipient knows what it received from the organisation
            var rm = requester.Mind?.Orgs;
            int rb = rm == null ? -1 : rm.IndexOfOrg(org.Id);
            if (rb >= 0) { rm.Beliefs[rb].Trust = (short)Math.Min(1000, rm.Beliefs[rb].Trust + 100); rm.Beliefs[rb].Ration = (short)give; rm.Beliefs[rb].Tick = (int)s.Tick; }
            OrgActs.End(npc);
        }

        // ---------------------------------------------------------------- deposits (a member at the store)

        public static void StartDeposit(GodState s, EventBuffer ev, Npc npc, OrgMind m, in Candidate c)
        {
            m.Act.Bin = c.TargetId;
            m.Act.Org = OrgActs.OrgOfBin(s, c.TargetId);
            var org = Orgs.Find(s, m.Act.Org);
            if (org == null || org.Status == OrgStatus.Dissolved) OrgActs.Frustrate(s, npc);
        }

        /// <summary>
        /// The member walks to the store and puts food of its own (or its household's) in, keeping <c>DepositKeep</c> units:
        /// <c>Goods.Move(Carry -> Bin, Deposit, ToBinOwner)</c>, basis <see cref="OwnershipBasis.OrgContribution"/>. Who sees it
        /// (a record-keeper in sight) may record it.
        /// </summary>
        public static void Deposit(GodState s, EventBuffer ev, Npc npc, OrgMind m)
        {
            var org = Orgs.Find(s, m.Act.Org);
            var bin = Goods.BinById(s.Population, m.Act.Bin);
            if (org == null || bin == null || org.Status == OrgStatus.Dissolved) { OrgActs.Frustrate(s, npc); return; }
            if (!OrgActs.WalkTo(s, npc, bin.Node, Reach.Store)) { if (npc.Path == null) OrgActs.Frustrate(s, npc); return; }
            int keep = W(s).DepositKeep, total = 0;
            foreach (var l in npc.Carry) if (Mine(npc, l)) total += l.Count;
            int put = 0;
            for (int i = 0; i < npc.Carry.Length && total - put > keep; i++)
            {
                var l = npc.Carry[i];
                if (!Mine(npc, l)) continue;
                int n = Math.Min(l.Count, total - put - keep);
                var w = l.Ware;
                var r = Goods.Move(s, ev, Holder.Carry(npc.Id), l.Id, n, Holder.Bin(bin.Id), MoveReason.Deposit, npc.Id, OwnerRule.ToBinOwner, purposeRef: org.Id, basis: OwnershipBasis.OrgContribution);
                if (!r.Ok) break;
                put += n;
                org.Ledger.Book(w, LedgerCause.Contribution, n, 0);
                org.DayDeposits++;
                OrgRecords.Witness(s, org, bin.Node, RecordKind.Deposit, npc.Id, w, n, npc.Id);
            }
            if (put == 0) OrgActs.Frustrate(s, npc); else OrgActs.End(npc);
        }

        private static bool Mine(Npc npc, in ItemLot l) =>
            !l.Empty && Wares.IsFood(l.Ware) && l.Ware != Ware.Nightshade
            && (l.Owner.Kind == OwnerKind.Npc && l.Owner.Id == npc.Id || l.Owner.Kind == OwnerKind.Household && npc.Household != 0 && l.Owner.Id == npc.Household);
    }
}
