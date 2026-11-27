using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// C6's sim-side hook slots (OWNER: C6): the acts of its verbs (founding gestures, joining, leaving, records; the store's
    /// deposits and services are in <see cref="CommunalStore"/>), the Sense seam for organisation stores and time use. Every
    /// act is a person's: it walks within reach first and changes goods only through <see cref="Goods"/>.
    /// </summary>
    public static partial class OrgHooks
    {
        static partial void TryStartC6(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled)
        {
            if (handled || !OrgVerb.IsMine(c.Verb)) return;
            handled = true;
            var m = npc.Mind?.Orgs;
            if (m == null) { OrgActs.Frustrate(s, npc); return; }
            byte kind = OrgActs.KindOf(c);
            // re-choosing the running act continues it
            if (npc.Action.Since < s.Tick && m.Act.Kind == kind && m.Act.Target == (c.Kind == IntentTarget.Node ? 0 : c.TargetId) && m.Act.StartTick > 0) return;
            m.Act = new OrgAct { Kind = kind, Target = c.Kind == IntentTarget.Node ? 0 : c.TargetId, StartTick = s.Tick };
            switch (c.Verb)
            {
                case OrgVerb.Join: OrgActs.StartJoin(s, ev, npc, m, in c); break;
                case OrgVerb.Leave: OrgActs.Leave(s, ev, npc, m); break;
                case OrgVerb.RequestService: CommunalStore.StartRequest(s, ev, npc, m, in c); break;
                case OrgVerb.WriteRecord: m.Act.Bin = c.TargetId; m.Act.Org = OrgActs.OrgOfBin(s, c.TargetId); break;
                case OrgVerb.OrgWork:
                    if (c.Class == OrgVerb.Deposit) CommunalStore.StartDeposit(s, ev, npc, m, in c);
                    else CommunalStore.StartServe(s, ev, npc, m, in c);
                    break;
            }
        }

        static partial void TryActC6(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled)
        {
            if (handled || !OrgVerb.IsMine(npc.Action.Verb)) return;
            handled = true;
            var m = npc.Mind?.Orgs;
            if (m == null) { OrgActs.End(npc); return; }
            switch (m.Act.Kind)
            {
                case OrgVerb.Deposit: CommunalStore.Deposit(s, ev, npc, m); break;
                case OrgVerb.Serve: CommunalStore.Serve(s, ev, npc, m); break;
                case OrgVerb.ActRecord: OrgActs.Record(s, ev, npc, m); break;
                default:
                    // a gesture (founding, joining, asking, leaving): it lasts the gesture's ticks
                    if (s.Tick >= m.Act.StartTick + s.Tuning.Social.GestureTicks) OrgActs.End(npc);
                    break;
            }
        }

        static partial void ActivityOfC6(byte verb, ref int activity)
        {
            if (activity != Activity.Other) return;
            if (verb == OrgVerb.Join || verb == OrgVerb.Leave || verb == OrgVerb.RequestService) activity = Activity.Social;
            else if (verb == OrgVerb.OrgWork) activity = Activity.Carry;
        }

        /// <summary>
        /// The Sense seam (contracts 6): the stores of organisations in sight, as the object class of their template with how
        /// stocked with food they look, and the bin as a thing it can name (like a household store).
        /// </summary>
        static partial void SenseExtraC6(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n)
        {
            foreach (var org in s.Organisations.Orgs)
            {
                if (org.Status == OrgStatus.Dissolved) continue;
                int cls = OrgClass.StoreOf(org.Template);
                if (cls == 0) continue;
                foreach (int binId in org.Stores)
                {
                    if (n >= near.Length) return;
                    var b = Goods.BinById(s.Population, binId);
                    if (b == null) continue;
                    int d = g.Distance(npc.Node, b.Node);
                    if (d > sight) continue;
                    int units = CommunalStore.FoodIn(b);
                    near[n++] = new Sensed
                    {
                        Class = (byte)cls, Category = ObjCategory.Store, State = (byte)(units == 0 ? ObjState.StockEmpty : units < 4 ? ObjState.StockLow : ObjState.StockHigh),
                        Kind = IntentTarget.Bin, Node = b.Node, Dist = d, Ref = b.Id,
                    };
                }
            }
        }
    }

    /// <summary>The person acts of the framework (founding, joining, leaving, writing records) and their shared helpers.</summary>
    public static class OrgActs
    {
        public static byte KindOf(in Candidate c)
        {
            switch (c.Verb)
            {
                case OrgVerb.Join: case OrgVerb.OrgWork: return c.Class;
                case OrgVerb.RequestService: return OrgVerb.ActRequest;
                case OrgVerb.WriteRecord: return OrgVerb.ActRecord;
                default: return OrgVerb.ActLeave;
            }
        }

        // ---------------------------------------------------------------- founding and joining (gestures)

        public static void StartJoin(GodState s, EventBuffer ev, Npc npc, OrgMind m, in Candidate c)
        {
            var os = s.Organisations;
            var p = s.Population;
            int tick = (int)s.Tick;
            bool emergence = s.Tuning.Orgs.Emergence != 0;
            var other = c.Kind == IntentTarget.Npc ? p.Find(c.TargetId) : null;
            if (c.Kind == IntentTarget.Npc && (other == null || !Bodies.IsLivingPerson(other))) { Frustrate(s, npc); return; }
            switch (c.Class)
            {
                case OrgVerb.Propose:
                {
                    if (!emergence) { Frustrate(s, npc); return; }
                    int oi = -1, si = -1;
                    for (int i = 0; i < m.OppCount && oi < 0; i++) { int k = m.Opportunities[i].IndexOf(c.TargetId); if (k >= 0) { oi = i; si = k; } }
                    if (oi < 0) { Frustrate(s, npc); return; }
                    int template = m.Opportunities[oi].Template;
                    m.Opportunities[oi].SetAsk(si, tick);
                    int id = Say(s, ev, npc, GestureAct.AskJoin, new MsgTopic { Kind = TopicKind.Org, Id = 0, Sub = template }, other.Id, MsgTime.Soon);
                    Ensure(s, other, id);
                    if (s.Tuning.Orgs.Enabled != 0) ev?.Add(new OrgOpportunityAvailable { Npc = npc.Id, Template = template, NeedEvidence = m.Opportunities[oi].NeedEvidence });
                    break;
                }
                case OrgVerb.Answer:
                {
                    int v = m.InviteFrom(c.TargetId, InviteTemplate(m, c.TargetId), 0);
                    if (v < 0) { Frustrate(s, npc); return; }
                    m.Invites[v].Accepted = true; m.Invites[v].Tick = tick;
                    int id = Say(s, ev, npc, GestureAct.Accept, new MsgTopic { Kind = TopicKind.Org, Id = 0, Sub = m.Invites[v].Template }, other.Id, MsgTime.Soon);
                    Ensure(s, other, id);
                    break;
                }
                case OrgVerb.CallMeeting:
                {
                    if (!emergence) { Frustrate(s, npc); return; }
                    int oi = -1;
                    for (int i = 0; i < m.OppCount; i++) { var t = OrgTemplates.Get(m.Opportunities[i].Template); if (t != null && m.Opportunities[i].AcceptedCount() >= t.MinFounders - 1) { oi = i; break; } }
                    if (oi < 0) { Frustrate(s, npc); return; }
                    int template = m.Opportunities[oi].Template;
                    for (int i = os.Meetings.Count - 1; i >= 0; i--) if (os.Meetings[i].Proposer == npc.Id) os.Meetings.RemoveAt(i);
                    var meet = new FoundingMeeting { Proposer = npc.Id, Template = template, Node = npc.Node, Opened = s.Tick, Expires = s.Tick + s.Tuning.Orgs.C6.MeetingTicks };
                    int at = 0; while (at < os.Meetings.Count && os.Meetings[at].Proposer < npc.Id) at++;
                    os.Meetings.Insert(at, meet);
                    int id = Say(s, ev, npc, GestureAct.AskJoin, new MsgTopic { Kind = TopicKind.Org, Id = 0, Sub = template }, 0, MsgTime.Now);
                    EnsureAll(s, npc, id);
                    break;
                }
                case OrgVerb.AnswerMeeting:
                {
                    int v = -1;
                    for (int i = 0; i < m.InviteCount; i++) if (m.Invites[i].Proposer == c.TargetId && m.Invites[i].Org == 0 && m.Invites[i].Accepted) { v = i; break; }
                    if (v < 0) { Frustrate(s, npc); return; }
                    m.Invites[v].MeetingAnswered = true;
                    int template = m.Invites[v].Template;
                    int id = Say(s, ev, npc, GestureAct.Accept, new MsgTopic { Kind = TopicKind.Org, Id = 0, Sub = template }, other.Id, MsgTime.Now);
                    FoundingMeeting meet = null;
                    foreach (var x in os.Meetings) if (x.Proposer == other.Id && x.Template == template && x.Expires >= s.Tick) { meet = x; break; }
                    if (meet == null || !Orgs.Present(s, npc.Id, meet.Node) || !Orgs.Perceived(other, id)) return; // nobody counted it
                    if (!meet.Accepts.Contains(npc.Id)) { meet.Accepts.Add(npc.Id); meet.Accepts.Sort(); }
                    TryFound(s, ev, meet);
                    break;
                }
                case OrgVerb.AskJoin:
                {
                    int bi = -1;
                    for (int i = 0; i < m.Count; i++) if (m.Beliefs[i].MyRole == 0 && m.Beliefs[i].BelievedHead == c.TargetId) { bi = i; break; }
                    if (bi < 0) { Frustrate(s, npc); return; }
                    m.Beliefs[bi].LastAsk = tick;
                    int id = Say(s, ev, npc, GestureAct.AskJoin, new MsgTopic { Kind = TopicKind.Org, Id = m.Beliefs[bi].Org, Sub = 0 }, other.Id, MsgTime.Soon);
                    Ensure(s, other, id);
                    break;
                }
                case OrgVerb.AcceptJoin:
                {
                    int v = -1;
                    for (int i = 0; i < m.InviteCount; i++) if (m.Invites[i].Proposer == c.TargetId && m.Invites[i].Org > 0) { v = i; break; }
                    if (v < 0) { Frustrate(s, npc); return; }
                    var org = Orgs.Find(s, m.Invites[v].Org);
                    m.RemoveInvite(v);
                    // the deciding office answers (HeadDecides: DecideAlone; any office with Propose under a meeting rule is not enough)
                    if (org == null || org.Status == OrgStatus.Dissolved || Orgs.OfficeWith(org, npc.Id, OrgPower.DecideAlone) < 0
                        || org.Charter.Membership != MembershipRule.ByAcceptance && org.Charter.Membership != MembershipRule.Open) { Frustrate(s, npc); return; }
                    int id = Say(s, ev, npc, GestureAct.Accept, new MsgTopic { Kind = TopicKind.Org, Id = org.Id, Sub = 0 }, other.Id, MsgTime.Soon);
                    if (!Orgs.AddRole(s, ev, org, other.Id, RoleKind.Member, RoleBasis.Gesture)) return;
                    Orgs.Record(s, ev, org, npc.Id, DecisionSource.Request, other.Id, DecisionWhat.Join, other.Id, 0, DecisionOutcome.Adopted, org.Charter.Governance.Kind, 1, 0, 0, npc.Id);
                    OrgRecords.Witness(s, org, npc.Node, RecordKind.MemberJoined, other.Id, Ware.None, 1, npc.Id);
                    if (Orgs.Perceived(other, id)) Orgs.TellFounding(s, org, other);
                    break;
                }
                default: Frustrate(s, npc); return;
            }
        }

        private static int InviteTemplate(OrgMind m, int proposer)
        {
            for (int i = 0; i < m.InviteCount; i++) if (m.Invites[i].Proposer == proposer && m.Invites[i].Org == 0 && !m.Invites[i].Accepted) return m.Invites[i].Template;
            return 0;
        }

        /// <summary>
        /// The founding meeting counts: the proposer present, at least the template's minimum of founders (the proposer and the
        /// supporters whose second Accept it perceived, all alive and present) from at least its minimum of households. Then
        /// <see cref="Orgs.Found"/> at the meeting place; the meeting closes.
        /// </summary>
        public static Organisation TryFound(GodState s, EventBuffer ev, FoundingMeeting meet)
        {
            var t = OrgTemplates.Get(meet.Template);
            var p = s.Population;
            if (t == null || !Orgs.Present(s, meet.Proposer, meet.Node)) return null;
            var founders = new List<int> { meet.Proposer };
            foreach (int a in meet.Accepts) if (a != meet.Proposer && Orgs.Present(s, a, meet.Node)) founders.Add(a);
            if (founders.Count < t.MinFounders) return null;
            var households = new List<int>();
            foreach (int f in founders) { var n = p.Find(f); int h = n.Household != 0 ? n.Household : -f; if (!households.Contains(h)) households.Add(h); }
            if (households.Count < t.MinHouseholds) return null;
            s.Organisations.Meetings.Remove(meet);
            var org = Orgs.Found(s, ev, t.Id, founders, meet.Node, t.Id);
            if (org == null) return null;
            foreach (int f in founders)
            {
                var m = p.Find(f)?.Mind?.Orgs;
                if (m == null) continue;
                int oi = m.IndexOfOpp(t.Id); if (oi >= 0) m.RemoveOpp(oi);
                for (int i = m.InviteCount - 1; i >= 0; i--) if (m.Invites[i].Org == 0 && m.Invites[i].Template == t.Id) m.RemoveInvite(i);
            }
            return org;
        }

        /// <summary>A member who distrusts an organisation (and holds no office) leaves it: <see cref="MemberLeft"/> (Left).</summary>
        public static void Leave(GodState s, EventBuffer ev, Npc npc, OrgMind m)
        {
            for (int i = 0; i < m.Count; i++)
            {
                var b = m.Beliefs[i];
                if (b.MyRole == 0 || b.Trust >= 0 || b.MyOffice >= 0) continue;
                var org = Orgs.Find(s, b.Org);
                if (org != null && org.IsMember(npc.Id)) Orgs.EndRoles(s, ev, org, npc.Id, MemberLeft.Left);
                m.Beliefs[i].MyRole = 0;
                return;
            }
            Frustrate(s, npc);
        }

        // ---------------------------------------------------------------- records

        public static int OrgOfBin(GodState s, int bin)
        {
            foreach (var o in s.Organisations.Orgs) if (o.Stores.Contains(bin)) return o.Id;
            return 0;
        }

        /// <summary>The recorder walks to the organisation's place and writes what it witnessed (<see cref="OrgRecords.Write"/>).</summary>
        public static void Record(GodState s, EventBuffer ev, Npc npc, OrgMind m)
        {
            var org = Orgs.Find(s, m.Act.Org);
            var bin = Goods.BinById(s.Population, m.Act.Bin);
            if (org == null || bin == null) { Frustrate(s, npc); return; }
            if (!WalkTo(s, npc, bin.Node, Reach.Store)) { if (npc.Path == null) Frustrate(s, npc); return; }
            if (OrgRecords.Write(s, ev, org, npc) == 0) Frustrate(s, npc); else End(npc);
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>The speaker makes a gesture (M5's emission and perception; nothing else). Returns the message id (0 without M5).</summary>
        public static int Say(GodState s, EventBuffer ev, Npc speaker, GestureAct act, MsgTopic topic, int target, MsgTime time)
        {
            if (s.Social == null) return 0;
            return Messages.Emit(s, ev, speaker, Messages.Compose(act, topic, 0, default, target, time));
        }

        /// <summary>A listener who perceived an organisational message gets an organisation mind (so its hooks can read it).</summary>
        public static void Ensure(GodState s, Npc listener, int msgId)
        {
            if (listener?.Mind == null || listener.Mind.Orgs != null || !Orgs.Perceived(listener, msgId)) return;
            listener.Mind.Orgs = Orgs.NewMind(s);
        }

        /// <summary><see cref="Ensure"/> for every person in the speaker's gesture range.</summary>
        public static void EnsureAll(GodState s, Npc speaker, int msgId)
        {
            if (msgId == 0) return;
            var g = s.Territory.Grid;
            int range = Math.Max(s.Tuning.Population.SightDay, s.Tuning.Population.SightNight);
            foreach (var l in s.Population.Npcs)
                if (Bodies.IsLivingPerson(l) && l.Id != speaker.Id && l.Mind != null && l.Mind.Orgs == null && g.Distance(l.Node, speaker.Node) <= range) Ensure(s, l, msgId);
        }

        public static void End(Npc npc)
        {
            npc.Action = new NpcAction { Since = npc.Action.Since };
            npc.Path = null; npc.PathGoal = -1;
            if (npc.Mind?.Orgs != null) npc.Mind.Orgs.Act = default;
            if (npc.Mind != null) Learner.EndOngoing(npc.Mind.Store);
        }

        public static void Frustrate(GodState s, Npc npc)
        {
            if (npc.Mind != null) Learner.AddReward(npc.Mind.Store, s.Tuning.Learning.Frustration);
            End(npc);
        }

        /// <summary>One step towards a node (M3's walking rule, C6's own path cache): true when within <paramref name="reach"/>.</summary>
        public static bool WalkTo(GodState s, Npc npc, int goal, int reach)
        {
            var g = s.Territory.Grid;
            if (g.Distance(npc.Node, goal) <= reach) { npc.Path = null; npc.PathGoal = -1; npc.StepTicks = 0; return true; }
            var tu = s.Tuning.Population;
            var os = s.Organisations;
            if (os.Pather == null) os.Pather = new NodePather(g);
            if (npc.Path == null || npc.PathGoal != goal || npc.PathIdx >= npc.Path.Length)
            {
                npc.Path = os.Pather.Find(npc.Node, goal, s.Territory.NodeRoad, tu.RoadStepTicks, tu.StepTicks, tu.PathMaxExpand);
                npc.PathIdx = 0; npc.PathGoal = goal; npc.StepTicks = 0;
                if (npc.Path == null || npc.Path.Length == 0) { npc.Path = null; return false; }
            }
            int next = npc.Path[npc.PathIdx];
            int cost = s.Territory.NodeRoad[next] != 0 ? tu.RoadStepTicks : tu.StepTicks;
            if (++npc.StepTicks >= cost) { npc.StepTicks = 0; npc.Node = next; npc.PathIdx++; }
            return g.Distance(npc.Node, goal) <= reach;
        }
    }
}
