using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>C6's learning and intent verbs (contracts 14: Orgs block 104-115, intents 180-188) and its sub-acts.</summary>
    public static class OrgVerb
    {
        /// <summary>Join 104 (propose, answer, call or answer a founding meeting, ask to join, accept a member), LeaveOrg 105, RequestService 106 (contracts 14);
        /// WriteRecord 114 and OrgWork 115 (deposit, serve a request) take the two Orgs spares (C6 is the framework; recorded for the lead).</summary>
        public const byte Join = 104, Leave = 105, RequestService = 106, WriteRecord = 114, OrgWork = 115;
        public const NpcVerb IntentJoin = (NpcVerb)180, IntentLeave = (NpcVerb)181, IntentRequest = (NpcVerb)182, IntentRecord = (NpcVerb)183, IntentWork = (NpcVerb)184;
        /// <summary>Sub-acts of <see cref="Join"/> (carried in <c>Candidate.Class</c> and <c>OrgAct.Kind</c>).</summary>
        public const byte Propose = 1, Answer = 2, CallMeeting = 3, AnswerMeeting = 4, AskJoin = 5, AcceptJoin = 6;
        /// <summary>Sub-acts of <see cref="OrgWork"/> (offset so they never equal a Join sub-act in <c>OrgAct.Kind</c>).</summary>
        public const byte Deposit = 11, Serve = 12;
        /// <summary>The act kinds of the other verbs in <c>OrgAct.Kind</c>.</summary>
        public const byte ActRequest = 21, ActRecord = 22, ActLeave = 23;

        public static bool IsMine(byte v) => v == Join || v == Leave || v == RequestService || v == WriteRecord || v == OrgWork;
    }

    /// <summary>C6's object classes (contracts 14 Orgs block 100-109): an organisation's store, by template.</summary>
    public static class OrgClass
    {
        public const int SharedFoodStore = 100;
        public static int StoreOf(int template) => template == OrgTemplates.SharedFoodStore ? SharedFoodStore : 0;
        public static int TemplateOfStore(int cls) => cls == SharedFoodStore ? OrgTemplates.SharedFoodStore : 0;
    }

    /// <summary>The values of <see cref="FeatureKind.Org"/> (C6): the organisational context of a candidate (target features).</summary>
    public static class OrgFeature
    {
        /// <summary>A founding proposal at need band 1-3 (own evidence over the threshold, twice, three times).</summary>
        public const int ProposeNeed1 = 1, ProposeNeed2 = 2, ProposeNeed3 = 3;
        public const int ProposalHeard = 10;
        /// <summary>Its own need band (0-3) for the template it is asked about: 20 + band.</summary>
        public const int OwnNeed0 = 20;
        public const int MeetingCall = 30, MeetingAnswer = 31;
        public const int Deposit = 40, RationRequest = 41, Serve = 42, Record = 43, Leave = 44, AskJoin = 45, AcceptJoin = 46;
        public static uint Of(int v) => Feature.Make(FeatureKind.Org, v);
    }

    /// <summary>
    /// C6's mind-side slots (OWNER: C6): verb rows, instincts and candidates of the organisation framework. Like every mind
    /// hook they read only the think scratch (what is sensed and heard now, the mind's own beliefs, its carry, its social
    /// memory): never the true state, never an organisation record (gate G4.1, ORG-INV-4). The only writes are to this mind's
    /// own <see cref="OrgMind"/> (what it heard and experienced, like <c>Thinker.UpdateBeliefs</c>). A mind with no
    /// <see cref="OrgMind"/> gets no candidate (neutral while nothing organisational entered it).
    /// </summary>
    public static partial class OrgMindHooks
    {
        static partial void VerbsC6(List<VerbInfo> into)
        {
            into.Add(new VerbInfo(OrgVerb.Join, "join or found", VerbGroup.Relationships, OrgVerb.IntentJoin));
            into.Add(new VerbInfo(OrgVerb.Leave, "leave organisation", VerbGroup.Relationships, OrgVerb.IntentLeave));
            into.Add(new VerbInfo(OrgVerb.RequestService, "request service", VerbGroup.CoopTake, OrgVerb.IntentRequest));
            into.Add(new VerbInfo(OrgVerb.WriteRecord, "write record", VerbGroup.Employment, OrgVerb.IntentRecord));
            into.Add(new VerbInfo(OrgVerb.OrgWork, "work for organisation", VerbGroup.CoopGive, OrgVerb.IntentWork));
        }

        /// <summary>
        /// Innate priors (starting points: learning outweighs them; they matter only for C6's own candidates, which exist only
        /// while an organisation entered the mind): propose more readily the stronger one's own need evidence; answer a
        /// proposal by one's own need; answer and call a founding meeting; deposit spare food; ask the keeper when hungry;
        /// serve a request one knows of as keeper; write what one saw as recorder; ask to join; leave an organisation one distrusts.
        /// </summary>
        static partial void InstinctsC6(List<Instinct> into)
        {
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.ProposeNeed1), 0, OrgVerb.Join, 150));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.ProposeNeed2), 0, OrgVerb.Join, 250));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.ProposeNeed3), 0, OrgVerb.Join, 350));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.ProposalHeard), OrgFeature.Of(OrgFeature.OwnNeed0), OrgVerb.Join, 40));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.ProposalHeard), OrgFeature.Of(OrgFeature.OwnNeed0 + 1), OrgVerb.Join, 200));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.ProposalHeard), OrgFeature.Of(OrgFeature.OwnNeed0 + 2), OrgVerb.Join, 300));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.ProposalHeard), OrgFeature.Of(OrgFeature.OwnNeed0 + 3), OrgVerb.Join, 350));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.MeetingCall), 0, OrgVerb.Join, 400));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.MeetingAnswer), 0, OrgVerb.Join, 450));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.Deposit), 0, OrgVerb.OrgWork, 200));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.RationRequest), Feature.DriveActive(Drive.Hunger), OrgVerb.RequestService, 350));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.Serve), 0, OrgVerb.OrgWork, 400));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.Record), 0, OrgVerb.WriteRecord, 300));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.AskJoin), 0, OrgVerb.Join, 200));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.AcceptJoin), 0, OrgVerb.Join, 350));
            into.Add(new Instinct(OrgFeature.Of(OrgFeature.Leave), 0, OrgVerb.Leave, 100));
        }

        static partial void AddCandidatesC6(ThinkScratch sc)
        {
            var m = sc.Mind?.Orgs;
            if (m == null || m.Tuning == null) return;
            OrgThink.Perceive(sc, m);
            OrgThink.Candidates(sc, m);
        }

        static partial void DescribeC6(uint f, ref string text)
        {
            if (text != null) return;
            if ((f >> 24) == (uint)FeatureKind.ObjClass && (int)(f & 0xFFFFFF) == OrgClass.SharedFoodStore) { text = "a shared food store"; return; }
            if ((f >> 24) != (uint)FeatureKind.Org) return;
            int v = (int)(f & 0xFFFFFF);
            switch (v)
            {
                case OrgFeature.ProposeNeed1: case OrgFeature.ProposeNeed2: case OrgFeature.ProposeNeed3: text = "a shared store would help (need " + v + ")"; break;
                case OrgFeature.ProposalHeard: text = "asked to found a shared store"; break;
                case OrgFeature.MeetingCall: text = "calling the founders together"; break;
                case OrgFeature.MeetingAnswer: text = "a founding meeting"; break;
                case OrgFeature.Deposit: text = "giving to the shared store"; break;
                case OrgFeature.RationRequest: text = "asking the keeper for food"; break;
                case OrgFeature.Serve: text = "a request to serve"; break;
                case OrgFeature.Record: text = "things to record"; break;
                case OrgFeature.Leave: text = "leaving an organisation"; break;
                case OrgFeature.AskJoin: text = "asking to join"; break;
                case OrgFeature.AcceptJoin: text = "someone asks to join"; break;
                default: if (v >= OrgFeature.OwnNeed0 && v <= OrgFeature.OwnNeed0 + 3) text = "own need " + (v - OrgFeature.OwnNeed0); break;
            }
        }
    }

    /// <summary>The mind-side logic of C6 (reads the scratch only; writes only the mind's own <see cref="OrgMind"/>).</summary>
    internal static class OrgThink
    {
        // ---------------------------------------------------------------- perception: what it heard and lived through

        public static void Perceive(ThinkScratch sc, OrgMind m)
        {
            var self = sc.Self;
            int tick = self.Tick, day = self.Day;
            var tu = m.Tuning;
            int maxId = m.LastMsg;
            for (int i = 0; i < sc.HeardCount; i++)
            {
                var msg = sc.Heard[i];
                if (msg.MessageId <= m.LastMsg) continue;
                if (msg.MessageId > maxId) maxId = msg.MessageId;
                Heard(sc, m, msg, tick);
            }
            m.LastMsg = maxId;
            // seeing an organisation's store teaches its practice ("has seen such an organisation operating", ORGANISATIONS 4.1)
            for (int i = 0; i < sc.SeenCount; i++)
            {
                int t = OrgClass.TemplateOfStore(sc.Seen[i].Class);
                if (t != 0) m.Practices |= 1 << t;
            }
            Evidence(sc, m, day, tu);
            // forgetting: stale invites, acceptances and requests
            for (int i = m.InviteCount - 1; i >= 0; i--) if (tick - m.Invites[i].Tick > tu.ProposalWindowTicks) m.RemoveInvite(i);
            for (int i = 0; i < m.OppCount; i++)
                if (m.Opportunities[i].AcceptedMask != 0 && tick - m.Opportunities[i].AcceptTick > tu.ProposalWindowTicks) m.Opportunities[i].AcceptedMask = 0;
            for (int i = m.KnownCount - 1; i >= 0; i--) if (tick - m.Known[i].Tick > tu.RequestExpireTicks) m.RemoveKnown(i);
        }

        private static void Heard(ThinkScratch sc, OrgMind m, in DecodedMessage msg, int tick)
        {
            var self = sc.Self;
            bool toMe = msg.Target == self.Id;
            if (msg.Topic.Kind == TopicKind.Org)
            {
                int org = msg.Topic.Id, template = msg.Topic.Sub;
                if (org == 0 && template > 0 && template < 31)
                {
                    if (msg.Act == GestureAct.AskJoin && msg.Time != MsgTime.Now && toMe && msg.Speaker != 0)
                    {
                        // a founding proposal: told of the practice; an invitation to answer by its own scores
                        m.Practices |= 1 << template;
                        m.AddInvite(new OrgInvite { Proposer = msg.Speaker, Template = template, Org = 0, Tick = tick });
                    }
                    else if (msg.Act == GestureAct.AskJoin && msg.Time == MsgTime.Now && msg.Speaker != 0)
                    {
                        int v = m.InviteFrom(msg.Speaker, template, 0);
                        if (v >= 0 && m.Invites[v].Accepted) { m.Invites[v].MeetingTick = tick; m.Invites[v].MeetingAnswered = false; }
                    }
                    else if (msg.Act == GestureAct.Accept && msg.Time != MsgTime.Now && toMe && msg.Speaker != 0)
                    {
                        int oi = m.IndexOfOpp(template);
                        if (oi < 0) return;
                        ref var o = ref m.Opportunities[oi];
                        int si = o.IndexOf(msg.Speaker);
                        if (si < 0) { for (int k = 0; k < 4; k++) if (o.Supporter(k) == 0) { o.SetSupporter(k, msg.Speaker); si = k; break; } }
                        if (si < 0) return;
                        o.AcceptedMask |= (byte)(1 << si);
                        o.AcceptTick = tick;
                    }
                }
                else if (org > 0 && msg.Act == GestureAct.AskJoin && msg.Time != MsgTime.Now && toMe && msg.Speaker != 0)
                {
                    // someone asks to join an organisation in which it holds an office
                    int bi = m.IndexOfOrg(org);
                    if (bi >= 0 && m.Beliefs[bi].MyOffice >= 0) m.AddInvite(new OrgInvite { Proposer = msg.Speaker, Template = 0, Org = org, Tick = tick });
                }
                return;
            }
            if (msg.Topic.Kind == TopicKind.Service && msg.Topic.Id > 0)
            {
                int org = msg.Topic.Id;
                int bi = m.IndexOfOrg(org);
                if (bi < 0) return;
                if (msg.Act == GestureAct.NeedFood && toMe && msg.Speaker != 0 && (m.Beliefs[bi].MyPowers & (uint)OrgPower.KeepStore) != 0)
                {
                    // a request it perceived as the keeper: its seen need from what it sees of the requester (a visible display), else 1
                    short seen = 1;
                    for (int k = 0; k < sc.PeopleCount; k++) if (sc.People[k].As == msg.Speaker && sc.People[k].DisplayLevel > 0) seen = sc.People[k].DisplayLevel;
                    m.AddKnown(new KnownRequest { Org = org, Kind = (ServiceKind)msg.Topic.Sub, Requester = msg.Speaker, Tick = tick, SeenNeed = seen, MessageId = msg.MessageId });
                }
                else if (msg.Act == GestureAct.Refuse && toMe)
                {
                    ref var b = ref m.Beliefs[bi];
                    b.Trust = (short)System.Math.Max(-1000, b.Trust - 150); b.Tick = tick;
                }
            }
        }

        /// <summary>
        /// Need evidence from its own experience (ORGANISATIONS 4.1, shared food store): a day it was hungry with no food while it
        /// saw someone of another household carrying food; a day it carried food while someone of another household begged or
        /// asked for food. Only for a practice it knows, and not while it believes itself a member of such an organisation. The
        /// people involved whom it trusts at least <c>SupportTrust</c> become believed supporters (at most 4).
        /// </summary>
        private static void Evidence(ThinkScratch sc, OrgMind m, int day, OrgC6Tuning tu)
        {
            int t = OrgTemplates.SharedFoodStore;
            if (!m.Knows(t)) return;
            for (int i = 0; i < m.Count; i++) if (m.Beliefs[i].Template == t && m.Beliefs[i].MyRole != 0) return;
            var self = sc.Self;
            bool hungry = self.Hunger >= sc.Tuning.BandHigh;
            int sigA = 0, sigB = 0;
            for (int i = 0; i < sc.PeopleCount; i++)
            {
                var p = sc.People[i];
                if (p.SameHousehold || p.As == 0 || p.Asleep) continue;
                if (hungry && !self.CarryFood && (p.CarryMask & CarryMask.Food) != 0 && sigA == 0) sigA = p.As;
                if (self.CarryFood && p.Display != 0 && p.DisplayLevel > 0 && sigB == 0) sigB = p.As;
            }
            if (self.CarryFood && sigB == 0)
                for (int i = 0; i < sc.HeardCount; i++)
                {
                    var h = sc.Heard[i];
                    if (h.Act != GestureAct.NeedFood || h.Speaker == 0 || h.Topic.Kind == TopicKind.Service) continue;
                    bool mate = false;
                    for (int k = 0; k < sc.PeopleCount; k++) if (sc.People[k].As == h.Speaker && sc.People[k].SameHousehold) mate = true;
                    if (!mate && h.Speaker != self.Id) { sigB = h.Speaker; break; }
                }
            int oi = m.IndexOfOpp(t);
            if (oi < 0 && sigA == 0 && sigB == 0) return;
            if (oi < 0)
            {
                if (m.OppCount >= OrgMind.OppCap) return;
                oi = m.OppCount++;
                m.Opportunities[oi] = new OpportunityBelief { Template = t, Formed = self.Tick, LastSignalDay = day, LastSignalA = -1, LastSignalB = -1 };
            }
            ref var o = ref m.Opportunities[oi];
            if (sigA != 0 && o.LastSignalA != day) { o.LastSignalA = day; o.NeedEvidence = (short)System.Math.Min(30, o.NeedEvidence + 1); o.LastSignalDay = day; Support(sc, m, ref o, sigA, tu); }
            if (sigB != 0 && o.LastSignalB != day) { o.LastSignalB = day; o.NeedEvidence = (short)System.Math.Min(30, o.NeedEvidence + 1); o.LastSignalDay = day; Support(sc, m, ref o, sigB, tu); }
            if (day - o.LastSignalDay >= tu.EvidenceDecayDays && o.NeedEvidence > 0) { o.NeedEvidence--; o.LastSignalDay = day; }
        }

        private static void Support(ThinkScratch sc, OrgMind m, ref OpportunityBelief o, int who, OrgC6Tuning tu)
        {
            if (o.IndexOf(who) >= 0) return;
            if (TrustOf(sc, who) < tu.SupportTrust) return;
            for (int k = 0; k < 4; k++) if (o.Supporter(k) == 0) { o.SetSupporter(k, who); return; }
        }

        /// <summary>Its own trust in a person (<c>SocialView.Trust</c>, its M5 social memory: no record, no trust).</summary>
        public static int TrustOf(ThinkScratch sc, int who) => Social.SocialView.Trust(sc.Mind, who);

        public static int Band(int evidence, int threshold)
        {
            if (threshold <= 0 || evidence < threshold) return 0;
            return evidence >= 3 * threshold ? 3 : evidence >= 2 * threshold ? 2 : 1;
        }

        // ---------------------------------------------------------------- candidates (quota 4, the most pressing first)

        public static void Candidates(ThinkScratch sc, OrgMind m)
        {
            var self = sc.Self;
            var tu = m.Tuning;
            int tick = self.Tick;
            bool hungry = self.Hunger >= sc.Tuning.BandHigh;

            // serve a request it knows of (keeper), by the queue order it believes holds
            for (int bi = 0; bi < m.Count; bi++)
            {
                var b = m.Beliefs[bi];
                if ((b.MyPowers & (uint)OrgPower.KeepStore) == 0) continue;
                int k = Services.Next(m, b.Org, ServiceKind.Ration, b.QueueOrder);
                if (k < 0) continue;
                int who = PersonHandle(sc, m.Known[k].Requester);
                if (who != 0) Cand(sc, OrgVerb.OrgWork, OrgVerb.Serve, IntentTarget.Npc, who, NodeOf(sc, who), OrgFeature.Serve, Feature.NpcRel((int)RelClassOf(sc, who)));
            }
            // founding: answer a meeting call, call the meeting, answer a proposal; accept a member
            for (int i = 0; i < m.InviteCount; i++)
            {
                var v = m.Invites[i];
                int who = PersonHandle(sc, v.Proposer);
                if (who == 0) continue;
                if (v.Org == 0 && v.Accepted && v.MeetingTick != 0 && !v.MeetingAnswered && tick - v.MeetingTick <= tu.MeetingTicks)
                    Cand(sc, OrgVerb.Join, OrgVerb.AnswerMeeting, IntentTarget.Npc, who, NodeOf(sc, who), OrgFeature.MeetingAnswer, 0);
                else if (v.Org == 0 && !v.Accepted)
                {
                    int oi = m.IndexOfOpp(v.Template);
                    int band = oi < 0 ? 0 : Band(m.Opportunities[oi].NeedEvidence, tu.NeedThreshold);
                    Cand(sc, OrgVerb.Join, OrgVerb.Answer, IntentTarget.Npc, who, NodeOf(sc, who), OrgFeature.ProposalHeard, OrgFeature.Of(OrgFeature.OwnNeed0 + band));
                }
                else if (v.Org > 0)
                    Cand(sc, OrgVerb.Join, OrgVerb.AcceptJoin, IntentTarget.Npc, who, NodeOf(sc, who), OrgFeature.AcceptJoin, 0);
            }
            for (int oi = 0; oi < m.OppCount; oi++)
            {
                var o = m.Opportunities[oi];
                var t = OrgTemplates.Get(o.Template);
                if (t == null || o.AcceptedCount() < t.MinFounders - 1) continue;
                int inSight = 0;
                for (int k = 0; k < 4; k++) if ((o.AcceptedMask & (1 << k)) != 0 && PersonHandle(sc, o.Supporter(k)) != 0) inSight++;
                if (inSight >= t.MinFounders - 1) Cand(sc, OrgVerb.Join, OrgVerb.CallMeeting, IntentTarget.Node, 0, self.Node, OrgFeature.MeetingCall, 0);
            }
            // a member: ask the keeper for a ration when hungry with no food; write records; deposit spare food
            for (int bi = 0; bi < m.Count; bi++)
            {
                var b = m.Beliefs[bi];
                if (b.MyRole == 0) continue;
                if (hungry && !self.CarryFood && (b.MyPowers & (uint)OrgPower.KeepStore) == 0 && (b.LastAsk == 0 || tick - b.LastAsk > tu.RequestExpireTicks))
                {
                    int keeper = PersonHandle(sc, b.BelievedHead);
                    if (keeper != 0) Cand(sc, OrgVerb.RequestService, OrgVerb.ActRequest, IntentTarget.Npc, keeper, NodeOf(sc, keeper), OrgFeature.RationRequest, 0);
                }
                if ((b.MyPowers & (uint)OrgPower.KeepRecords) != 0 && b.BelievedStoreBin != 0 && HasWitness(m, b.Org) && Thinker.Known(sc, IntentTarget.Bin, b.BelievedStoreBin, b.BelievedStoreNode))
                    Cand(sc, OrgVerb.WriteRecord, OrgVerb.ActRecord, IntentTarget.Bin, b.BelievedStoreBin, b.BelievedStoreNode, OrgFeature.Record, 0);
                if (!hungry && b.BelievedStoreBin != 0 && FoodInHand(sc) > m.Welfare.DepositKeep && Thinker.Known(sc, IntentTarget.Bin, b.BelievedStoreBin, b.BelievedStoreNode))
                    Cand(sc, OrgVerb.OrgWork, OrgVerb.Deposit, IntentTarget.Bin, b.BelievedStoreBin, b.BelievedStoreNode, OrgFeature.Deposit, Feature.Category(ObjCategory.Store));
                if (b.Trust < 0 && b.MyOffice < 0) Cand(sc, OrgVerb.Leave, OrgVerb.ActLeave, IntentTarget.Node, 0, self.Node, OrgFeature.Leave, 0);
            }
            // a founding proposal to a believed supporter in sight, or asking to join an organisation it knows
            for (int oi = 0; oi < m.OppCount; oi++)
            {
                var o = m.Opportunities[oi];
                int band = Band(o.NeedEvidence, tu.NeedThreshold);
                if (band == 0 || !m.Knows(o.Template)) continue;
                for (int k = 0; k < 4; k++)
                {
                    int s = o.Supporter(k);
                    if (s == 0 || (o.AcceptedMask & (1 << k)) != 0 || o.AskTick(k) != 0 && tick - o.AskTick(k) < tu.ProposeCooldownTicks) continue;
                    int who = PersonHandle(sc, s);
                    if (who == 0) continue;
                    Cand(sc, OrgVerb.Join, OrgVerb.Propose, IntentTarget.Npc, who, NodeOf(sc, who), OrgFeature.ProposeNeed1 + band - 1, 0);
                    break;
                }
                for (int bi = 0; bi < m.Count; bi++)
                {
                    var b = m.Beliefs[bi];
                    if (b.MyRole != 0 || b.Template != o.Template || b.BelievedHead == 0 || b.LastAsk != 0 && tick - b.LastAsk < tu.ProposeCooldownTicks) continue;
                    int head = PersonHandle(sc, b.BelievedHead);
                    if (head != 0) Cand(sc, OrgVerb.Join, OrgVerb.AskJoin, IntentTarget.Npc, head, NodeOf(sc, head), OrgFeature.AskJoin, 0);
                }
            }
        }

        private static bool HasWitness(OrgMind m, int org)
        {
            for (int i = 0; i < m.WitnessCount; i++) if (m.Witness[i].Org == org) return true;
            return false;
        }

        /// <summary>Food units in its own carry that are its own or its household's (the scratch copy of its carry).</summary>
        public static int FoodInHand(ThinkScratch sc)
        {
            int n = 0;
            for (int i = 0; i < sc.CarryCount; i++)
            {
                var l = sc.Carry[i];
                if (l.Empty || !Wares.IsFood(l.Ware) || l.Ware == Ware.Nightshade) continue;
                if (l.Owner.Kind == OwnerKind.Npc && l.Owner.Id == sc.Self.Id || l.Owner.Kind == OwnerKind.Household && sc.Self.Household != 0 && l.Owner.Id == sc.Self.Household) n += l.Count;
            }
            return n;
        }

        /// <summary>The handle of a person it recognises as <paramref name="As"/> and sees now (0: not in sight).</summary>
        public static int PersonHandle(ThinkScratch sc, int As)
        {
            if (As == 0) return 0;
            for (int i = 0; i < sc.PeopleCount; i++) if (sc.People[i].As == As && !sc.People[i].Asleep) return sc.People[i].Id;
            return 0;
        }

        private static int NodeOf(ThinkScratch sc, int handle)
        {
            for (int i = 0; i < sc.PeopleCount; i++) if (sc.People[i].Id == handle) return sc.People[i].Node;
            return sc.Self.Node;
        }

        private static NpcRelClass RelClassOf(ThinkScratch sc, int handle)
        {
            for (int i = 0; i < sc.PeopleCount; i++) if (sc.People[i].Id == handle) return Relations.ClassOf(sc, sc.People[i]);
            return NpcRelClass.Unknown;
        }

        private static void Cand(ThinkScratch sc, byte verb, byte sub, IntentTarget kind, int id, int node, int feature, uint extra)
        {
            int dist = sc.Grid.Distance(sc.Self.Node, node);
            int at = Thinker.AddCandidate(sc, verb, kind, id, node, sub, dist);
            if (at < 0) return;
            Thinker.TargetFeatures(sc, at, OrgFeature.Of(feature), extra, kind == IntentTarget.Bin ? Feature.Class(OrgClass.SharedFoodStore) : 0);
        }
    }
}
