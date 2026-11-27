using God.Sim.Learning;
using System;
using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>One office of a template: kind, powers, whether it is the head, and which founder fills it at founding (0 the proposer, 1 the next founder ...).</summary>
    public readonly struct TemplateOffice
    {
        public readonly OfficeKind Kind;
        public readonly OrgPower Powers;
        public readonly bool IsHead;
        public readonly int FounderSlot;
        public TemplateOffice(OfficeKind kind, OrgPower powers, bool isHead, int founderSlot) { Kind = kind; Powers = powers; IsHead = isHead; FounderSlot = founderSlot; }
    }

    /// <summary>
    /// An organisation template (ORGANISATIONS 4.1): data, like a recipe. A type, purposes, a default charter, the offices it
    /// needs, its founding minimum and its need signal. Availability (knowing the practice) lets a founding candidate exist; a
    /// template creates nothing. New organisation types add rows here and service kinds, never framework branches (spec 22).
    /// </summary>
    public sealed class OrgTemplate
    {
        public readonly int Id;
        public readonly string Name;
        public readonly OrgType Type;
        public readonly PurposeKind Purpose;
        public readonly int PurposeRef;
        public readonly OwnershipModel Ownership;
        public readonly int MinFounders;
        /// <summary>Founders must come from at least this many households (a communal store between households: 2).</summary>
        public readonly int MinHouseholds;
        public readonly MembershipRule Membership;
        public readonly DecisionKind Routine, PolicyRule;
        public readonly byte Quorum;
        public readonly DistributionRule Distribution;
        public readonly StoreAccessRule StoreAccess;
        public readonly DissolutionRule Dissolution;
        public readonly BinKind StoreKind;
        public readonly ReadOnlyArray<TemplateOffice> Offices;
        /// <summary>The service it provides (none: no service).</summary>
        public readonly ServiceKind Service;

        public OrgTemplate(int id, string name, OrgType type, PurposeKind purpose, int purposeRef, OwnershipModel ownership, int minFounders, int minHouseholds,
            MembershipRule membership, DecisionKind routine, DecisionKind policyRule, byte quorum, DistributionRule distribution, StoreAccessRule storeAccess,
            DissolutionRule dissolution, BinKind storeKind, ServiceKind service, params TemplateOffice[] offices)
        {
            Id = id; Name = name; Type = type; Purpose = purpose; PurposeRef = purposeRef; Ownership = ownership; MinFounders = minFounders; MinHouseholds = minHouseholds;
            Membership = membership; Routine = routine; PolicyRule = policyRule; Quorum = quorum; Distribution = distribution; StoreAccess = storeAccess;
            Dissolution = dissolution; StoreKind = storeKind; Service = service; Offices = new ReadOnlyArray<TemplateOffice>(offices);
        }
    }

    /// <summary>The templates (data; append-only ids, below 31: <see cref="OrgMind.Practices"/> is a bit set).</summary>
    public static class OrgTemplates
    {
        public const int SharedFoodStore = 1;

        public static readonly ReadOnlyArray<OrgTemplate> Rows = new ReadOnlyArray<OrgTemplate>(
            new OrgTemplate(SharedFoodStore, "shared food store", OrgType.ServiceBody, PurposeKind.MutualAid, (int)ServiceKind.Ration, OwnershipModel.Communal,
                minFounders: 2, minHouseholds: 2, MembershipRule.ByAcceptance, DecisionKind.HeadDecides, DecisionKind.MajorityPresent, quorum: 2, DistributionRule.ByNeed,
                StoreAccessRule.MembersByRation, DissolutionRule.ToMembersEqual, BinKind.CommunalStore, ServiceKind.Ration,
                new TemplateOffice(OfficeKind.Keeper, OrgPower.Propose | OrgPower.DecideAlone | OrgPower.KeepStore | OrgPower.Spend | OrgPower.Represent, true, 0),
                new TemplateOffice(OfficeKind.Recorder, OrgPower.Propose | OrgPower.KeepRecords, false, 1)));

        public static OrgTemplate Get(int id)
        {
            foreach (var t in Rows) if (t.Id == id) return t;
            return null;
        }
    }

    /// <summary>
    /// The organisation framework's operations (ORGANISATIONS 4.2-4.4, 4.13): founding (the only OrgId allocator), roles,
    /// offices, policies, decisions, dormancy, dissolution and the relay recipients of an instruction. Every operation is called
    /// by a person's act or by a decision with a living decider; none moves goods except dissolution's in-place re-owning.
    /// </summary>
    public static class Orgs
    {
        public static OrgState StateOf(GodState s) => s.Organisations;

        public static Owner OwnerOf(int org) => new Owner(OwnerKind.Org, org);

        public static Organisation Find(GodState s, int org) => s.Organisations?.Find(org);

        /// <summary>A new organisation mind with the game's numbers bound (sim side).</summary>
        public static OrgMind NewMind(GodState s) => OrgMind.Create(s.Tuning.Orgs.C6, s.Tuning.Orgs.C8);

        // ---------------------------------------------------------------- founding

        /// <summary>
        /// Founds an organisation (the only <c>OrgId</c> allocator, contracts 1) from a template, with these founders (the proposer
        /// first) at a place. Offices are filled by the founders the template names; every founder is a member; each founder's
        /// household holds an equal share; the store is an empty bin of the template's kind at the place, owned by the
        /// organisation (no goods: contributions come later by deposits). Emits <see cref="OrgFounded"/>, the founders'
        /// <see cref="MemberJoined"/>, the offices' <see cref="OrgHeadChanged"/> and the charter's policies. The founders were at
        /// the meeting, so each knows it (an <see cref="OrgBelief"/> in its own mind).
        /// </summary>
        public static Organisation Found(GodState s, EventBuffer ev, int template, IReadOnlyList<int> founders, int node, int originOpportunity)
        {
            var os = s.Organisations ?? throw new InvalidOperationException("Orgs.Found: organisations are off");
            var t = OrgTemplates.Get(template) ?? throw new ArgumentException("unknown template " + template);
            if (founders == null || founders.Count == 0) throw new ArgumentException("an organisation needs founders");
            if (os.Orgs.Count >= 256) return null; // ORGANISATIONS 4.14 bound
            var p = s.Population;
            int id = OrgIds.Take(ref os.NextOrgId);
            var first = p.Find(founders[0]);
            var org = new Organisation
            {
                Id = id, Type = t.Type, Template = t.Id, Ownership = t.Ownership, Faction = first?.Faction ?? Faction.F0, Node = node, Status = OrgStatus.Operating,
                Founded = s.Tick, OriginOpportunity = originOpportunity,
            };
            org.Purposes.Add(new Purpose { Kind = t.Purpose, Ref = t.PurposeRef });
            var c = org.Charter;
            c.AdoptedTick = s.Tick; c.Membership = t.Membership; c.Distribution = t.Distribution; c.StoreAccess = t.StoreAccess; c.Dissolution = t.Dissolution;
            c.Governance = new DecisionRuleSpec { Kind = t.Routine, Quorum = 1, MeetingPlace = node };
            c.PolicyRule = new DecisionRuleSpec { Kind = t.PolicyRule, Quorum = t.Quorum, MeetingPlace = node };
            for (int i = 0; i < founders.Count && org.Founders.Count < Organisation.MaxFounders; i++) org.Founders.Add(founders[i]);
            // shares: each founder household (or the founder) equally
            var holders = new List<Owner>();
            foreach (int f in org.Founders)
            {
                var n = p.Find(f);
                var h = n != null && n.Household != 0 ? Owner.Household(n.Household) : Owner.Npc(f);
                bool have = false; foreach (var x in holders) if (x.Same(h)) have = true;
                if (!have) holders.Add(h);
            }
            holders.Sort((a, b) => a.Kind != b.Kind ? ((int)a.Kind).CompareTo((int)b.Kind) : a.Id.CompareTo(b.Id));
            for (int i = 0; i < holders.Count && i < Organisation.MaxShares; i++)
                org.Shares.Add(new OrgShare { Holder = holders[i], Permille = (short)(1000 / holders.Count + (i == 0 ? 1000 % holders.Count : 0)) });
            if (t.StoreKind != 0 && node >= 0)
                org.Stores.Add(Goods.NewBin(s, t.StoreKind, OwnerOf(id), node, s.Tuning.Orgs.C8.StoreWeightCap));
            // the charter's policies
            org.Policies.Add(new OrgPolicy { Key = OrgPolicyKey.QueueOrder, Value = QueueOrder.FirstCome, SinceTick = s.Tick });
            org.Policies.Add(new OrgPolicy { Key = OrgPolicyKey.Eligibility, Value = Eligibility.MembersOnly, SinceTick = s.Tick });
            if (t.Service == ServiceKind.Ration) org.Policies.Add(new OrgPolicy { Key = OrgPolicyKey.RationPerRequest, Value = s.Tuning.Orgs.C8.StartRation, SinceTick = s.Tick });
            os.Orgs.Add(org); // ids ascend: the counter only grows
            os.Founded++;
            ev?.Add(new OrgFounded { Org = id, Type = t.Type, Ownership = t.Ownership, Template = t.Id, Founders = org.Founders.ToArray(), Place = node, OriginOpportunity = originOpportunity, Store = org.Stores.Count > 0 ? org.Stores[0] : 0 });
            var sorted = new List<int>(org.Founders); sorted.Sort();
            foreach (int f in sorted) AddRole(s, ev, org, f, RoleKind.Member, RoleBasis.Charter);
            for (int i = 0; i < t.Offices.Count && i < Organisation.MaxOffices; i++)
            {
                var to = t.Offices[i];
                org.Offices.Add(new Office { Kind = to.Kind, Powers = to.Powers, IsHead = to.IsHead, Holder = 0, Since = s.Tick });
                int holder = to.FounderSlot < org.Founders.Count ? org.Founders[to.FounderSlot] : 0;
                if (holder != 0) SetOffice(s, ev, org, i, holder, OfficeFilling.Founder);
            }
            foreach (var pol in org.Policies) ev?.Add(new OrgPolicySet { Org = id, Key = pol.Key, Value = pol.Value, Before = 0, Decision = 0 });
            foreach (int f in org.Founders) TellFounding(s, org, p.Find(f));
            return org;
        }

        /// <summary>A person present at the founding (or told of a role) believes it: its role, office, head, place and the charter's policies.</summary>
        internal static void TellFounding(GodState s, Organisation org, Npc n)
        {
            if (n?.Mind == null) return;
            var m = n.Mind.Orgs ?? (n.Mind.Orgs = Orgs.NewMind(s));
            int bi = m.BeliefFor(org.Id, (int)s.Tick);
            ref var b = ref m.Beliefs[bi];
            b.Template = org.Template;
            b.MyRole = org.IsMember(n.Id) ? (byte)RoleKind.Member : (byte)0;
            b.MyOffice = -1; b.MyPowers = 0;
            for (int i = 0; i < org.Offices.Count; i++) if (org.Offices[i].Holder == n.Id) { b.MyOffice = (sbyte)i; b.MyPowers = (uint)org.Offices[i].Powers; break; }
            int head = org.HeadOffice();
            b.BelievedHead = head >= 0 ? org.Offices[head].Holder : 0;
            b.BelievedPlace = org.Node; b.BelievedStoreNode = org.Node; b.BelievedStoreBin = org.Stores.Count > 0 ? org.Stores[0] : 0;
            b.Ration = (short)org.PolicyOf(OrgPolicyKey.RationPerRequest);
            b.QueueOrder = (byte)org.PolicyOf(OrgPolicyKey.QueueOrder);
            b.StoreAccess = (byte)org.Charter.StoreAccess;
            foreach (var pol in org.Policies) b.KnownPolicies |= 1u << (int)pol.Key;
            if (b.Trust == 0) b.Trust = 500;
            b.Confidence = 255; b.Tick = (int)s.Tick;
            m.Practices |= 1 << org.Template;
        }

        // ---------------------------------------------------------------- roles and offices

        /// <summary>Adds a role (ascending NpcId, then role; at most 64; a person in at most 4 organisations). Emits <see cref="MemberJoined"/>.</summary>
        public static bool AddRole(GodState s, EventBuffer ev, Organisation org, int npc, RoleKind role, RoleBasis basis)
        {
            if (org.IndexOfRole(npc, role) >= 0 || org.Roles.Count >= Organisation.MaxRoles || org.Status == OrgStatus.Dissolved) return false;
            if (OrgsOf(s, npc) >= 4 && !org.IsMember(npc)) return false;
            var n = s.Population.Find(npc);
            if (n == null || !Bodies.IsLivingPerson(n)) return false;
            int at = 0;
            while (at < org.Roles.Count && (org.Roles[at].Npc < npc || org.Roles[at].Npc == npc && org.Roles[at].Role < role)) at++;
            org.Roles.Insert(at, new RoleHolding { Npc = npc, Role = role, Office = -1, Since = s.Tick, Basis = basis });
            ev?.Add(new MemberJoined { Org = org.Id, Npc = npc, Role = role, Basis = basis });
            return true;
        }

        /// <summary>How many organisations (not dissolved) a person holds a role in.</summary>
        public static int OrgsOf(GodState s, int npc)
        {
            int k = 0;
            foreach (var o in s.Organisations.Orgs) if (o.Status != OrgStatus.Dissolved && o.IsMember(npc)) k++;
            return k;
        }

        /// <summary>Sets an office's holder (0 vacates it): <see cref="OrgHeadChanged"/>. The holder must be a member.</summary>
        public static void SetOffice(GodState s, EventBuffer ev, Organisation org, int office, int holder, OfficeFilling filling)
        {
            var o = org.Offices[office];
            if (o.Holder == holder) return;
            int from = o.Holder;
            if (from != 0) { int ri = org.IndexOfRole(from, RoleKind.Member); if (ri >= 0) { var r = org.Roles[ri]; r.Office = -1; org.Roles[ri] = r; } }
            o.Holder = holder; o.Since = s.Tick; o.Filling = holder != 0 ? filling : OfficeFilling.None;
            org.Offices[office] = o;
            if (holder != 0) { int ri = org.IndexOfRole(holder, RoleKind.Member); if (ri >= 0) { var r = org.Roles[ri]; r.Office = office; org.Roles[ri] = r; } }
            ev?.Add(new OrgHeadChanged { Org = org.Id, Office = office, Kind = o.Kind, IsHead = o.IsHead, From = from, To = holder, Filling = o.Filling });
        }

        /// <summary>Every role of a person in an organisation ends (left, dismissed, died, dissolved); its offices fall vacant. Dissolves an organisation left without members.</summary>
        public static void EndRoles(GodState s, EventBuffer ev, Organisation org, int npc, byte reason)
        {
            bool any = false;
            for (int i = 0; i < org.Offices.Count; i++) if (org.Offices[i].Holder == npc) SetOffice(s, ev, org, i, 0, OfficeFilling.None);
            for (int i = org.Roles.Count - 1; i >= 0; i--)
            {
                if (org.Roles[i].Npc != npc) continue;
                var r = org.Roles[i];
                org.Roles.RemoveAt(i);
                any = true;
                ev?.Add(new MemberLeft { Org = org.Id, Npc = npc, Role = r.Role, Reason = reason });
            }
            if (any && org.Roles.Count == 0 && org.Status != OrgStatus.Dissolved && reason != MemberLeft.Dissolved) Dissolve(s, ev, org, OrgDissolved.LastMemberGone);
        }

        // ---------------------------------------------------------------- policies and decisions (records only)

        /// <summary>Sets a policy record (ascending key) by a decision: <see cref="OrgPolicySet"/>.</summary>
        public static void SetPolicy(GodState s, EventBuffer ev, Organisation org, OrgPolicyKey key, int value, int decision)
        {
            int before = 0;
            for (int i = 0; i < org.Policies.Count; i++)
                if (org.Policies[i].Key == key)
                {
                    before = org.Policies[i].Value;
                    org.Policies[i] = new OrgPolicy { Key = key, Value = value, SinceTick = s.Tick, Decision = decision };
                    ev?.Add(new OrgPolicySet { Org = org.Id, Key = key, Value = value, Before = before, Decision = decision });
                    return;
                }
            if (org.Policies.Count >= Organisation.MaxPolicies) return;
            int at = 0;
            while (at < org.Policies.Count && org.Policies[at].Key < key) at++;
            org.Policies.Insert(at, new OrgPolicy { Key = key, Value = value, SinceTick = s.Tick, Decision = decision });
            ev?.Add(new OrgPolicySet { Org = org.Id, Key = key, Value = value, Before = 0, Decision = decision });
        }

        /// <summary>Records a decision (ring of 64) and emits <see cref="OrgDecisionTaken"/>. Returns its id.</summary>
        public static int Record(GodState s, EventBuffer ev, Organisation org, int proposer, DecisionSource source, int sourceRef, DecisionWhat what, int a0, int a1,
            DecisionOutcome outcome, DecisionKind rule, int accept, int refuse, int absent, int counter)
        {
            var d = new OrgDecision
            {
                Id = org.NextDecisionId++, Tick = s.Tick, Proposer = proposer, Source = source, SourceRef = sourceRef, What = what, A0 = a0, A1 = a1, Outcome = outcome,
                Accept = (byte)Math.Min(255, accept), Refuse = (byte)Math.Min(255, refuse), Absent = (byte)Math.Min(255, absent),
            };
            if (org.Decisions.Count >= Organisation.MaxDecisions) org.Decisions.RemoveAt(0);
            org.Decisions.Add(d);
            ev?.Add(new OrgDecisionTaken
            {
                Org = org.Id, Decision = d.Id, Proposer = proposer, Source = source, SourceRef = sourceRef, What = what, A0 = a0, A1 = a1, Outcome = outcome, Rule = rule,
                Accept = accept, Refuse = refuse, Absent = absent, Counter = counter,
            });
            return d.Id;
        }

        /// <summary>The office of <paramref name="npc"/> in the organisation with all of <paramref name="powers"/> (-1 none).</summary>
        public static int OfficeWith(Organisation org, int npc, OrgPower powers)
        {
            for (int i = 0; i < org.Offices.Count; i++) if (org.Offices[i].Holder == npc && (org.Offices[i].Powers & powers) == powers) return i;
            return -1;
        }

        // ---------------------------------------------------------------- status and dissolution

        public static void SetStatus(GodState s, EventBuffer ev, Organisation org, OrgStatus to)
        {
            if (org.Status == to) return;
            var from = org.Status;
            org.Status = to;
            ev?.Add(new OrgStatusChanged { Org = org.Id, From = from, To = to });
        }

        /// <summary>
        /// Dissolution by the charter's rule (ORGANISATIONS 4.13): every role ends; the goods in its stores are re-owned in place
        /// (<c>Goods.SetOwner</c>, basis Divided): whole lots in ascending lot id, round robin to the members' share holders
        /// (ToMembersEqual, ToShares), or ownerless (Ownerless, or no holder). Collected later physically. Obligations would be
        /// orphaned through C2's registry (none exist in C6). The record stays (ids never reused).
        /// </summary>
        public static void Dissolve(GodState s, EventBuffer ev, Organisation org, byte reason)
        {
            if (org.Status == OrgStatus.Dissolved) return;
            var holders = new List<Owner>();
            if (org.Charter.Dissolution == DissolutionRule.ToMembersEqual || org.Charter.Dissolution == DissolutionRule.ToShares)
                foreach (var sh in org.Shares) holders.Add(sh.Holder);
            var members = new List<int>(); foreach (var r in org.Roles) if (!members.Contains(r.Npc)) members.Add(r.Npc);
            foreach (int m in members) EndRoles(s, ev, org, m, MemberLeft.Dissolved);
            int k = 0;
            foreach (int bin in org.Stores)
            {
                var b = Goods.BinById(s.Population, bin);
                if (b == null) continue;
                var lots = new List<ItemLot>(b.Lots);
                foreach (var l in lots)
                {
                    if (!l.Owner.Same(OwnerOf(org.Id))) continue;
                    var to = holders.Count > 0 ? holders[k++ % holders.Count] : Owner.None;
                    Goods.SetOwner(s, ev, Holder.Bin(bin), l.Id, to, OwnershipBasis.Divided, 0);
                    org.Ledger.Book(l.Ware, LedgerCause.Dissolution, 0, l.Count);
                }
            }
            SetStatus(s, ev, org, OrgStatus.Dissolved);
            org.DissolvedTick = s.Tick;
            s.Organisations.Dissolved++;
            ev?.Add(new OrgDissolved { Org = org.Id, Reason = reason, Rule = org.Charter.Dissolution });
        }

        // ---------------------------------------------------------------- relay (contracts 10.2, ORGANISATIONS 4.12)

        /// <summary>
        /// The recipients of an instruction addressed to an organisation (D2 emits <c>InstructionRelayed</c> and delivers; C10
        /// uses it for governments): the office-holders whose powers cover the act (all of <paramref name="powers"/>), in
        /// ascending NpcId, plus, when the act needs the organisation's meeting rule, the members present (alive, awake, within
        /// their sight of the meeting place) in ascending NpcId. Absent members are not recipients. Works for any organisation type.
        /// </summary>
        public static void RelayRecipients(GodState s, int orgId, OrgPower powers, bool meetingRule, List<int> into)
        {
            into.Clear();
            var org = Find(s, orgId);
            if (org == null || org.Status == OrgStatus.Dissolved) return;
            foreach (var o in org.Offices)
            {
                if (o.Holder == 0 || (o.Powers & powers) != powers || into.Contains(o.Holder)) continue;
                var n = s.Population.Find(o.Holder);
                if (n != null && Bodies.IsLivingPerson(n)) into.Add(o.Holder);
            }
            if (meetingRule)
            {
                int place = org.Charter.PolicyRule.MeetingPlace >= 0 ? org.Charter.PolicyRule.MeetingPlace : org.Node;
                foreach (var r in org.Roles)
                    if (!into.Contains(r.Npc) && Present(s, r.Npc, place)) into.Add(r.Npc);
            }
            into.Sort();
        }

        /// <summary>The powers an act of a decision kind needs (for the relay of an instruction about it).</summary>
        public static OrgPower PowersFor(DecisionWhat what)
        {
            switch (what)
            {
                case DecisionWhat.Spend: case DecisionWhat.SetBudget: return OrgPower.Spend;
                case DecisionWhat.Hire: return OrgPower.Hire;
                case DecisionWhat.Dismiss: return OrgPower.Dismiss;
                default: return OrgPower.Propose;
            }
        }

        /// <summary>A person is present at a place: alive, awake, within its own sight of it.</summary>
        public static bool Present(GodState s, int npc, int place)
        {
            var n = s.Population.Find(npc);
            if (n == null || !Bodies.IsLivingPerson(n) || n.Action.Asleep || place < 0) return false;
            bool night = PopulationSystem.TimeOf(SimCalendar.Of(s), s.Tick) == TimeOfDay.Night;
            return s.Territory.Grid.Distance(n.Node, place) <= Comms.CommsRange.Gesture(s.Tuning.Population, night);
        }

        /// <summary>
        /// The TRUE permission over goods of an organisation (sim side only, layer 1; for C1's theft classification): an office
        /// whose powers keep the store, or a role under a store access that lets members (or anyone) take. Not allowed otherwise
        /// (and for any owner that is not an existing organisation). Never callable from policies or mind hooks.
        /// </summary>
        public static Cognition.PermissionResult Truth(GodState s, Npc actor, Owner owner)
        {
            var org = actor != null && owner.Kind == OwnerKind.Org ? Find(s, owner.Id) : null;
            if (org == null || org.Status == OrgStatus.Dissolved) return new Cognition.PermissionResult(false, Cognition.PermissionBasis.None);
            var access = org.Charter.StoreAccess;
            bool ok = OfficeWith(org, actor.Id, OrgPower.KeepStore) >= 0 || access == StoreAccessRule.Anyone
                || access == StoreAccessRule.Members && org.IsMember(actor.Id)
                || access == StoreAccessRule.Officers && OfficeWith(org, actor.Id, OrgPower.None) >= 0;
            return new Cognition.PermissionResult(ok, ok ? Cognition.PermissionBasis.OrgRole : Cognition.PermissionBasis.None);
        }

        /// <summary>Whether a listener decoded a given message (it is in its inbox): what a person perceived of a gesture.</summary>
        public static bool Perceived(Npc listener, int messageId)
        {
            var sm = listener?.Mind?.Social;
            if (sm == null || messageId == 0) return false;
            for (int i = 0; i < sm.InboxCount; i++) if (sm.Inbox[i].Msg.MessageId == messageId) return true;
            return false;
        }
    }
}
