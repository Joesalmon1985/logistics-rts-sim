using System.Collections.Generic;
using God.Sim.Population;
using Newtonsoft.Json;

namespace God.Sim.Orgs
{
    // ============================================================== value sets (append-only: spec 22)

    /// <summary>What kind of body an organisation is (ORGANISATIONS 3.1); purposes say what it does. Append-only.</summary>
    public enum OrgType : byte { None = 0, Government = 1, Enterprise = 2, ServiceBody = 3, Association = 4, Religious = 5, Military = 6, Commons = 7 }

    /// <summary>The ownership model (D5.17): an attribute of the record. No service, store or production code branches on it (ORG-INV-5).</summary>
    public enum OwnershipModel : byte { None = 0, State = 1, Private = 2, Communal = 3, Cooperative = 4, Religious = 5 }

    /// <summary>What an organisation does (append-only; industry adds SupplyPower, Research ...).</summary>
    public enum PurposeKind : byte { None = 0, Produce = 1, Service = 2, MutualAid = 3, ManageCommons = 4, Worship = 5, Defend = 6, Govern = 7, Represent = 8, Lend = 9 }

    /// <summary>An office kind (a byte, append-only: Clerk and later kinds append).</summary>
    public enum OfficeKind : byte { None = 0, Head = 1, Manager = 2, Treasurer = 3, Keeper = 4, Recorder = 5, Priest = 6, Commander = 7, Judge = 8, Teacher = 9, Healer = 10, Councillor = 11 }

    /// <summary>Powers of an office (a 32-bit mask, append-only: Issue and later powers take free bits).</summary>
    [System.Flags]
    public enum OrgPower : uint
    {
        None = 0, Propose = 1, DecideAlone = 2, Spend = 4, Hire = 8, Dismiss = 16, KeepRecords = 32, KeepStore = 64, Represent = 128, Enforce = 256,
    }

    /// <summary>How an organisation decides (C6's internal type, contracts 7.1): C6 implements the first three; the rest are Tier 3.</summary>
    public enum DecisionKind : byte { None = 0, OwnerDecides = 1, HeadDecides = 2, MajorityPresent = 3, Consensus = 4, Council = 5 }

    public enum MembershipRule : byte { None = 0, Open = 1, ByAcceptance = 2, KinOnly = 3, HouseholdOnly = 4, ByContribution = 5, Hereditary = 6 }
    public enum DistributionRule : byte { None = 0, ToOwners = 1, EqualToMembers = 2, ByWorkTicks = 3, ByContribution = 4, ByNeed = 5, Retained = 6 }
    public enum StoreAccessRule : byte { None = 0, Officers = 1, Members = 2, MembersByRation = 3, Anyone = 4 }
    public enum DissolutionRule : byte { None = 0, ToMembersEqual = 1, ToShares = 2, ToAnotherOrg = 3, Ownerless = 4 }
    public enum OrgStatus : byte { None = 0, Forming = 1, Operating = 2, Dormant = 3, Insolvent = 4, Dissolved = 5 }
    public enum RoleKind : byte { None = 0, Member = 1, Employee = 2, Manager = 3, Officer = 4, Apprentice = 5, Client = 6 }
    /// <summary>How a role was taken: a founder's charter, an agreement by gesture, a decision (appointment), a contract (C3).</summary>
    public enum RoleBasis : byte { None = 0, Charter = 1, Gesture = 2, Decision = 3, Contract = 4 }
    public enum OfficeFilling : byte { None = 0, Appointed = 1, Elected = 2, Hereditary = 3, Seniority = 4, Founder = 5 }
    /// <summary>What a decision does (append-only).</summary>
    public enum DecisionWhat : byte { None = 0, Hire = 1, Dismiss = 2, Appoint = 3, SetPolicy = 4, SetBudget = 5, Spend = 6, Post = 7, FoundService = 8, Join = 9, Leave = 10, Amend = 11, Merge = 12, Split = 13, Dissolve = 14, Respond = 15 }
    public enum DecisionSource : byte { None = 0, Member = 1, Request = 2, Relation = 3, DivineInstruction = 4 }
    public enum DecisionOutcome : byte { None = 0, Adopted = 1, Rejected = 2, Lapsed = 3, Modified = 4 }
    /// <summary>Policy keys (type-specific; append-only: K2 adds UnitOfAccount, AcceptsInPayment, PaysWagesIn, TariffPer, PriceList).</summary>
    public enum OrgPolicyKey : byte { None = 0, QueueOrder = 1, Eligibility = 2, RationPerRequest = 3, Fee = 4, OpeningHours = 5, StoreAccess = 6 }
    /// <summary>Values of <see cref="OrgPolicyKey.QueueOrder"/>.</summary>
    public static class QueueOrder { public const int FirstCome = 1, MostSevereSeen = 2; }
    /// <summary>Values of <see cref="OrgPolicyKey.Eligibility"/>.</summary>
    public static class Eligibility { public const int MembersOnly = 1, Anyone = 2; }
    /// <summary>Relation kinds (record shape only in C6, Tier 3 implements them).</summary>
    public enum OrgRelationKind : byte { None = 0, MemberOf = 1, Funds = 2, FundedBy = 3, Regulates = 4, RegulatedBy = 5, Recognises = 6, ContractsWith = 7, Owns = 8, OwnedBy = 9, Allied = 10, Rival = 11 }
    /// <summary>Ledger causes (append-only: K2 adds money accounts beside ware flows).</summary>
    public enum LedgerCause : byte { None = 0, Contribution = 1, Fee = 2, Sale = 3, Grant = 4, Tax = 5, Wage = 6, Purchase = 7, Ration = 8, Treatment = 9, LoanOut = 10, Repayment = 11, Dissolution = 12 }

    // ============================================================== the record (ORGANISATIONS 3.2, first-version subset)

    public struct Purpose { public PurposeKind Kind; public int Ref; }
    public struct OrgShare { public Owner Holder; public short Permille; }

    /// <summary>A decision rule with its meeting parameters.</summary>
    public struct DecisionRuleSpec
    {
        public DecisionKind Kind;
        /// <summary>Members present needed for a meeting rule (below: the proposal lapses).</summary>
        public byte Quorum;
        /// <summary>The meeting place (a node; -1 none).</summary>
        public int MeetingPlace;
    }

    /// <summary>What the founders agreed (amended only through the decision rule).</summary>
    public sealed class Charter
    {
        public short Version = 1;
        public long AdoptedTick;
        public int AdoptedByDecision;
        public MembershipRule Membership;
        /// <summary>The rule for routine decisions (appointments).</summary>
        public DecisionRuleSpec Governance;
        /// <summary>The rule for policy changes and amendments.</summary>
        public DecisionRuleSpec PolicyRule;
        public DistributionRule Distribution;
        public StoreAccessRule StoreAccess;
        public DissolutionRule Dissolution;
        public int DissolveToOrg;
    }

    /// <summary>A seat with powers. Holder 0 = vacant.</summary>
    public struct Office
    {
        public OfficeKind Kind;
        public OrgPower Powers;
        public int Holder;
        public long Since;
        public OfficeFilling Filling;
        /// <summary>The head office (HeadDecides decides through it).</summary>
        public bool IsHead;
    }

    /// <summary>One role of one person (ascending NpcId, then role). Office: the index of the office it holds (-1 none).</summary>
    public struct RoleHolding
    {
        public int Npc;
        public RoleKind Role;
        public int Office;
        public long Since;
        public RoleBasis Basis;
        /// <summary>The contribution terms of the role as recorded (the member's own understanding is in its mind).</summary>
        public Ware TermWare;
        public int TermCount;
    }

    public struct LedgerLine { public Ware Ware; public LedgerCause Cause; public int In, Out; }

    public sealed class OrgLedger
    {
        /// <summary>Today's flows by ware and cause (reset after the day's snapshot).</summary>
        public List<LedgerLine> Day = new List<LedgerLine>();
        /// <summary>All-time totals by ware and cause.</summary>
        public List<LedgerLine> Total = new List<LedgerLine>();
        /// <summary>Formal obligations on and by this organisation (C2's registry ids; none in C6).</summary>
        public List<int> Obligations = new List<int>();
        public short ArrearsDays;

        public void Book(Ware w, LedgerCause cause, int inCount, int outCount)
        {
            Add(Day, w, cause, inCount, outCount);
            Add(Total, w, cause, inCount, outCount);
        }

        private static void Add(List<LedgerLine> l, Ware w, LedgerCause cause, int i, int o)
        {
            for (int k = 0; k < l.Count; k++)
                if (l[k].Ware == w && l[k].Cause == cause) { var x = l[k]; x.In += i; x.Out += o; l[k] = x; return; }
            int at = 0;
            while (at < l.Count && ((int)l[at].Ware < (int)w || l[at].Ware == w && l[at].Cause < cause)) at++;
            l.Insert(at, new LedgerLine { Ware = w, Cause = cause, In = i, Out = o });
        }
    }

    public struct BudgetLine { public Ware Ware; public int PerDay; public LedgerCause Cause; public int Decision; }
    public struct OrgPolicy { public OrgPolicyKey Key; public int Value; public long SinceTick; public int Decision; }
    public struct OrgRelation { public int Other; public OrgRelationKind Kind; public long Since; public int Basis; }

    /// <summary>An organisation decision (its own record; also <see cref="OrgDecisionTaken"/>). Ids are stable references.</summary>
    public struct OrgDecision
    {
        public int Id;
        public long Tick;
        public int Proposer;
        public DecisionSource Source;
        public int SourceRef;
        public DecisionWhat What;
        public int A0, A1, A2, A3;
        public byte Accept, Refuse, Absent;
        public DecisionOutcome Outcome;
    }

    /// <summary>A proposal under a meeting rule, waiting for the next meeting with its proposer present.</summary>
    public struct OrgProposal
    {
        public int Id;
        public int Proposer;
        public DecisionSource Source;
        public int SourceRef;
        public DecisionWhat What;
        public int A0, A1, A2, A3;
        public int OpenedDay;
    }

    /// <summary>
    /// The organisation record (ORGANISATIONS 3.2, first version). Every list is bounded and sorted as stated. Relations,
    /// merger and split fields exist but stay unused in C6 (spec 5 item 2).
    /// </summary>
    public sealed class Organisation
    {
        public const int MaxPurposes = 4, MaxShares = 16, MaxOffices = 8, MaxRoles = 64, MaxAssets = 8, MaxStores = 4, MaxBudget = 8, MaxPolicies = 16,
            MaxRecords = 512, MaxRequests = 256, MaxDecisions = 64, MaxRelations = 16, MaxFounders = 8, MaxProposals = 4;

        public int Id;
        public OrgType Type;
        public int Template;
        public List<Purpose> Purposes = new List<Purpose>();
        public Charter Charter = new Charter();
        public OwnershipModel Ownership;
        /// <summary>Holders and permille (sums to 1000), ascending by holder kind then id.</summary>
        public List<OrgShare> Shares = new List<OrgShare>();
        public Faction Faction;
        public List<Office> Offices = new List<Office>();
        /// <summary>Ascending NpcId, then role.</summary>
        public List<RoleHolding> Roles = new List<RoleHolding>();
        /// <summary>C1 AssetIds it owns or uses (ascending).</summary>
        public List<int> Assets = new List<int>();
        /// <summary>BinIds of its stores (Owner = Org(Id)).</summary>
        public List<int> Stores = new List<int>();
        /// <summary>Its place (the meeting place and store node).</summary>
        public int Node = -1;
        public OrgLedger Ledger = new OrgLedger();
        public List<BudgetLine> Budget = new List<BudgetLine>();
        /// <summary>Ascending key.</summary>
        public List<OrgPolicy> Policies = new List<OrgPolicy>();
        /// <summary>Layer 4: what role-holders wrote (ring, oldest first).</summary>
        public List<RecordEntry> Records = new List<RecordEntry>();
        /// <summary>Service requests (ring, ascending id).</summary>
        public List<ServiceRequest> Requests = new List<ServiceRequest>();
        /// <summary>Decisions (ring, ascending id).</summary>
        public List<OrgDecision> Decisions = new List<OrgDecision>();
        public List<OrgProposal> Proposals = new List<OrgProposal>();
        public List<OrgRelation> Relations = new List<OrgRelation>();
        public OrgStatus Status = OrgStatus.Operating;
        public long Founded;
        public List<int> Founders = new List<int>();
        public int OriginOpportunity;
        public int MergedInto, SplitFrom;
        public long DissolvedTick = -1;
        public int NextDecisionId = 1, NextRecordId = 1, NextProposalId = 1;
        /// <summary>The first day with no living office-holder with Propose (-1: there is one).</summary>
        public int NoDeciderSince = -1;
        /// <summary>Today's service counters (reset after the day snapshot).</summary>
        public int DayOpened, DayServed, DayRefused, DayExpired, DayDeposits, DayRecords;

        public int IndexOfRole(int npc, RoleKind role)
        {
            for (int i = 0; i < Roles.Count; i++) if (Roles[i].Npc == npc && Roles[i].Role == role) return i;
            return -1;
        }

        public bool IsMember(int npc)
        {
            foreach (var r in Roles) if (r.Npc == npc) return true;
            return false;
        }

        public int PolicyOf(OrgPolicyKey key, int fallback = 0)
        {
            foreach (var p in Policies) if (p.Key == key) return p.Value;
            return fallback;
        }

        public int HeadOffice()
        {
            for (int i = 0; i < Offices.Count; i++) if (Offices[i].IsHead) return i;
            return Offices.Count > 0 ? 0 : -1;
        }

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add((int)Type); h.Add(Template); h.Add((int)Ownership); h.Add((int)Faction); h.Add(Node); h.Add((int)Status); h.Add(Founded);
            h.Add(OriginOpportunity); h.Add(MergedInto); h.Add(SplitFrom); h.Add(DissolvedTick); h.Add(NextDecisionId); h.Add(NextRecordId); h.Add(NextProposalId);
            h.Add(NoDeciderSince); h.Add(DayOpened); h.Add(DayServed); h.Add(DayRefused); h.Add(DayExpired); h.Add(DayDeposits); h.Add(DayRecords);
            h.Add(Purposes.Count); foreach (var p in Purposes) { h.Add((int)p.Kind); h.Add(p.Ref); }
            var c = Charter;
            h.Add(c.Version); h.Add(c.AdoptedTick); h.Add(c.AdoptedByDecision); h.Add((int)c.Membership); h.Add((int)c.Governance.Kind); h.Add(c.Governance.Quorum); h.Add(c.Governance.MeetingPlace);
            h.Add((int)c.PolicyRule.Kind); h.Add(c.PolicyRule.Quorum); h.Add(c.PolicyRule.MeetingPlace); h.Add((int)c.Distribution); h.Add((int)c.StoreAccess); h.Add((int)c.Dissolution); h.Add(c.DissolveToOrg);
            h.Add(Shares.Count); foreach (var x in Shares) { h.Add((int)x.Holder.Kind); h.Add(x.Holder.Id); h.Add(x.Permille); }
            h.Add(Offices.Count); foreach (var o in Offices) { h.Add((int)o.Kind); h.Add((long)o.Powers); h.Add(o.Holder); h.Add(o.Since); h.Add((int)o.Filling); h.Add(o.IsHead ? 1 : 0); }
            h.Add(Roles.Count); foreach (var r in Roles) { h.Add(r.Npc); h.Add((int)r.Role); h.Add(r.Office); h.Add(r.Since); h.Add((int)r.Basis); h.Add((int)r.TermWare); h.Add(r.TermCount); }
            h.Add(Assets.Count); foreach (var a in Assets) h.Add(a);
            h.Add(Stores.Count); foreach (var b in Stores) h.Add(b);
            h.Add(Ledger.Day.Count); foreach (var l in Ledger.Day) { h.Add((int)l.Ware); h.Add((int)l.Cause); h.Add(l.In); h.Add(l.Out); }
            h.Add(Ledger.Total.Count); foreach (var l in Ledger.Total) { h.Add((int)l.Ware); h.Add((int)l.Cause); h.Add(l.In); h.Add(l.Out); }
            h.Add(Ledger.Obligations.Count); foreach (var o in Ledger.Obligations) h.Add(o);
            h.Add(Ledger.ArrearsDays);
            h.Add(Budget.Count); foreach (var b in Budget) { h.Add((int)b.Ware); h.Add(b.PerDay); h.Add((int)b.Cause); h.Add(b.Decision); }
            h.Add(Policies.Count); foreach (var p in Policies) { h.Add((int)p.Key); h.Add(p.Value); h.Add(p.SinceTick); h.Add(p.Decision); }
            h.Add(Records.Count); foreach (var r in Records) r.AddTo(ref h);
            h.Add(Requests.Count); foreach (var r in Requests) r.AddTo(ref h);
            h.Add(Decisions.Count);
            foreach (var d in Decisions)
            {
                h.Add(d.Id); h.Add(d.Tick); h.Add(d.Proposer); h.Add((int)d.Source); h.Add(d.SourceRef); h.Add((int)d.What); h.Add(d.A0); h.Add(d.A1); h.Add(d.A2); h.Add(d.A3);
                h.Add(d.Accept); h.Add(d.Refuse); h.Add(d.Absent); h.Add((int)d.Outcome);
            }
            h.Add(Proposals.Count);
            foreach (var p in Proposals) { h.Add(p.Id); h.Add(p.Proposer); h.Add((int)p.Source); h.Add(p.SourceRef); h.Add((int)p.What); h.Add(p.A0); h.Add(p.A1); h.Add(p.A2); h.Add(p.A3); h.Add(p.OpenedDay); }
            h.Add(Relations.Count); foreach (var r in Relations) { h.Add(r.Other); h.Add((int)r.Kind); h.Add(r.Since); h.Add(r.Basis); }
            h.Add(Founders.Count); foreach (var f in Founders) h.Add(f);
        }
    }

    /// <summary>A founding meeting called by a proposer (ORGANISATIONS 4.2 step 3): open until it expires or the founders are counted.</summary>
    public sealed class FoundingMeeting
    {
        public int Proposer;
        public int Template;
        public int Node;
        public long Opened, Expires;
        /// <summary>The supporters whose second Accept the proposer perceived (ascending NpcId).</summary>
        public List<int> Accepts = new List<int>();
    }

    /// <summary>
    /// C6's part of <see cref="OrgState"/> (OWNER: C6): the organisations (ascending OrgId, dissolved ones keep their record),
    /// the open founding meetings (ascending proposer id) and the day bookkeeping. Hashed in <c>HashC6</c> in this order.
    /// </summary>
    public sealed partial class OrgState
    {
        public List<Organisation> Orgs = new List<Organisation>();
        public List<FoundingMeeting> Meetings = new List<FoundingMeeting>();
        /// <summary>The last day the day start ran.</summary>
        public int LastDay = -1;
        /// <summary>Counters (analytics and the card): orgs founded, dissolved, rations given.</summary>
        public long Founded, Dissolved, Rations;

        /// <summary>A path cache for the C6 acts (rebuilt on demand, never saved or hashed).</summary>
        [JsonIgnore] internal NodePather Pather;

        public Organisation Find(int id)
        {
            int lo = 0, hi = Orgs.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = Orgs[mid].Id;
                if (v == id) return Orgs[mid];
                if (v < id) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        partial void HashC6(ref StateHasher h)
        {
            h.Add(Orgs.Count);
            foreach (var o in Orgs) o.AddTo(ref h);
            h.Add(Meetings.Count);
            foreach (var m in Meetings)
            {
                h.Add(m.Proposer); h.Add(m.Template); h.Add(m.Node); h.Add(m.Opened); h.Add(m.Expires);
                h.Add(m.Accepts.Count); foreach (var a in m.Accepts) h.Add(a);
            }
            h.Add(LastDay); h.Add(Founded); h.Add(Dissolved); h.Add(Rations);
        }
    }
}
