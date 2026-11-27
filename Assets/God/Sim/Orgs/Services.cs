using God.Sim.Learning;
using System;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// The kinds of service (INSTITUTIONAL_SERVICES 3.1). Open-ended and append-only: C7 uses Treat/Teach/Apprentice, C9 the
    /// justice kinds, R34 the knowledge and money kinds 20-23; industry adds supply, haulage, waste collection later.
    /// </summary>
    public enum ServiceKind : byte
    {
        None = 0, Treat = 1, Teach = 2, Apprentice = 3, Ration = 4, MindChild = 5, Lend = 6, Collect = 7,
        Mediate = 8, Hear = 9, Investigate = 10, Detain = 11, Rite = 12,
    }

    public enum RequestStatus : byte { None = 0, Waiting = 1, Active = 2, Done = 3, Refused = 4, Dropped = 5, Expired = 6 }

    /// <summary>What the subject of a request is (a typed reference: a person now; an asset or a site later, spec 22).</summary>
    public enum SubjectKind : byte { None = 0, Npc = 1, Asset = 2, Node = 3, Org = 4 }

    /// <summary>
    /// One service request (INSTITUTIONAL_SERVICES 3.1): in the provider organisation's ring (written by the role-holder who
    /// received it), or informal in the provider's mind (<see cref="OrgMind.Known"/>). <see cref="TrueNeed"/> is analytics-only
    /// truth (the subject's actual state when opened), never read by a rule or policy.
    /// </summary>
    public struct ServiceRequest
    {
        public int Id;
        public ServiceKind Kind;
        public int Requester;
        public bool RequesterIsOrg;
        public SubjectKind SubjectKind;
        public int Subject;
        public int Provider;
        public int ProviderNpc;
        public int Place;
        public long Opened, Expires;
        /// <summary>The provider's estimate from what it perceived (noisy; 0-3).</summary>
        public short SeenNeed;
        /// <summary>What is asked: a ware (Ration), an illness, a skill id ...</summary>
        public int Topic;
        public int TopicCount;
        public RequestStatus Status;
        public short QueuePos;
        /// <summary>The tick of the outcome state change (0 none).</summary>
        public long OutcomeTick;
        /// <summary>What the recorder wrote (-1 nothing yet).</summary>
        public short RecordedOutcome;
        /// <summary>The role-holder who received it (wrote it into the ring).</summary>
        public int ReceivedBy;
        public byte Control;
        /// <summary>Analytics only.</summary>
        public short TrueNeed;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add((int)Kind); h.Add(Requester); h.Add(RequesterIsOrg ? 1 : 0); h.Add((int)SubjectKind); h.Add(Subject); h.Add(Provider); h.Add(ProviderNpc);
            h.Add(Place); h.Add(Opened); h.Add(Expires); h.Add(SeenNeed); h.Add(Topic); h.Add(TopicCount); h.Add((int)Status); h.Add(QueuePos); h.Add(OutcomeTick);
            h.Add(RecordedOutcome); h.Add(ReceivedBy); h.Add(Control); h.Add(TrueNeed);
        }
    }

    /// <summary>
    /// The common service model (INSTITUTIONAL_SERVICES 3.2): request, queue, activity, outcome, record. Every step is called by
    /// the act of a person: a request is opened when a role-holder perceived it; it is accepted, delivered or refused by the
    /// provider in its own intent; the outcome is the owning system's state change (the event carries the request id). Nothing
    /// here moves goods or branches on the ownership model or the owner kind (ORG-INV-5). C7 and later packages add their
    /// kinds and activities on this API.
    /// </summary>
    public static class Services
    {
        /// <summary>
        /// The only <c>RequestId</c> allocator (contracts 1). Into the organisation's ring when <paramref name="org"/> is given
        /// (bounded: the oldest finished request gives way), else returned for the provider's mind (informal). Emits
        /// <see cref="ServiceRequested"/>.
        /// </summary>
        public static int Open(GodState s, EventBuffer ev, Organisation org, ServiceKind kind, int requester, SubjectKind subjectKind, int subject, int place,
            int topic, int topicCount, short seenNeed, int receivedBy, int providerNpc = 0)
        {
            var os = s.Organisations ?? throw new InvalidOperationException("Services.Open: organisations are off");
            int id = OrgIds.Take(ref os.NextRequestId);
            var req = new ServiceRequest
            {
                Id = id, Kind = kind, Requester = requester, SubjectKind = subjectKind, Subject = subject, Provider = org?.Id ?? 0, ProviderNpc = providerNpc,
                Place = place, Opened = s.Tick, Expires = s.Tick + s.Tuning.Orgs.C6.RequestExpireTicks, SeenNeed = seenNeed, Topic = topic, TopicCount = topicCount,
                Status = RequestStatus.Waiting, RecordedOutcome = -1, ReceivedBy = receivedBy, Control = ControlOf(s, requester), TrueNeed = TrueNeedOf(s, kind, subjectKind, subject),
            };
            if (org != null)
            {
                if (org.Requests.Count >= Organisation.MaxRequests)
                {
                    int drop = 0;
                    for (int i = 0; i < org.Requests.Count; i++) if (org.Requests[i].Status != RequestStatus.Waiting && org.Requests[i].Status != RequestStatus.Active) { drop = i; break; }
                    org.Requests.RemoveAt(drop);
                }
                req.QueuePos = (short)WaitingCount(org, kind);
                org.Requests.Add(req);
                org.DayOpened++;
            }
            ev?.Add(new ServiceRequested { Request = id, Org = req.Provider, Kind = kind, Requester = requester, SubjectKind = subjectKind, Subject = subject, Place = place, Control = req.Control, ReceivedBy = receivedBy, TrueNeed = req.TrueNeed });
            return id;
        }

        public static int IndexOf(Organisation org, int request)
        {
            if (org == null) return -1;
            for (int i = 0; i < org.Requests.Count; i++) if (org.Requests[i].Id == request) return i;
            return -1;
        }

        public static int WaitingCount(Organisation org, ServiceKind kind)
        {
            int n = 0;
            foreach (var r in org.Requests) if (r.Kind == kind && r.Status == RequestStatus.Waiting) n++;
            return n;
        }

        /// <summary>The provider takes the request (its own intent): <see cref="ServiceAccepted"/>.</summary>
        public static bool Accept(GodState s, EventBuffer ev, Organisation org, int request, int providerNpc)
        {
            int i = IndexOf(org, request);
            if (i >= 0 && org.Requests[i].Status == RequestStatus.Active && org.Requests[i].ProviderNpc == providerNpc) return true; // it resumes its own case
            if (i < 0 || org.Requests[i].Status != RequestStatus.Waiting) return false;
            var r = org.Requests[i]; r.Status = RequestStatus.Active; r.ProviderNpc = providerNpc; org.Requests[i] = r;
            ev?.Add(new ServiceAccepted { Request = request, Org = org.Id, ProviderNpc = providerNpc });
            return true;
        }

        /// <summary>
        /// The activity reached its outcome (the owning system's state change already happened, by this provider within reach):
        /// <see cref="ServiceDelivered"/> with the outcome event's code.
        /// </summary>
        public static void Delivered(GodState s, EventBuffer ev, Organisation org, int request, int providerNpc, int ticks, int goodsUsed, int outcomeEvent)
        {
            int i = IndexOf(org, request);
            if (i < 0) return;
            var r = org.Requests[i]; r.Status = RequestStatus.Done; r.OutcomeTick = s.Tick; r.ProviderNpc = providerNpc; org.Requests[i] = r;
            org.DayServed++;
            ev?.Add(new ServiceDelivered { Request = request, Org = org.Id, ProviderNpc = providerNpc, Ticks = ticks, GoodsUsed = goodsUsed, OutcomeEvent = outcomeEvent, Control = ControlOf(s, providerNpc) });
        }

        /// <summary>The provider refuses (what it saw is the reason): <see cref="ServiceRefused"/>.</summary>
        public static void Refuse(GodState s, EventBuffer ev, Organisation org, int request, int by, byte reasonSeen)
        {
            int i = IndexOf(org, request);
            if (i < 0) return;
            var r = org.Requests[i];
            if (r.Status == RequestStatus.Done || r.Status == RequestStatus.Refused || r.Status == RequestStatus.Expired) return;
            r.Status = RequestStatus.Refused; org.Requests[i] = r;
            org.DayRefused++;
            ev?.Add(new ServiceRefused { Request = request, Org = org.Id, By = by, ReasonSeen = reasonSeen });
        }

        /// <summary>Waiting or active requests past their time expire (per tick, ascending request id): <see cref="ServiceExpired"/>.</summary>
        public static void ExpireDue(GodState s, EventBuffer ev, Organisation org)
        {
            for (int i = 0; i < org.Requests.Count; i++)
            {
                var r = org.Requests[i];
                if ((r.Status != RequestStatus.Waiting && r.Status != RequestStatus.Active) || s.Tick < r.Expires) continue;
                r.Status = RequestStatus.Expired; org.Requests[i] = r;
                org.DayExpired++;
                ev?.Add(new ServiceExpired { Request = r.Id, Org = org.Id });
            }
        }

        /// <summary>
        /// The provider's choice of its next case among the requests it knows of (its mind), by the queue policy as it understands
        /// it: first come (oldest tick, then id) or most severe seen (highest seen need, then oldest). Returns the index or -1.
        /// </summary>
        public static int Next(OrgMind m, int org, ServiceKind kind, int queueOrder)
        {
            if (m == null) return -1;
            int best = -1;
            for (int i = 0; i < m.KnownCount; i++)
            {
                var k = m.Known[i];
                if (k.Org != org || k.Kind != kind || k.Done) continue;
                if (best < 0) { best = i; continue; }
                var b = m.Known[best];
                bool better = queueOrder == QueueOrder.MostSevereSeen
                    ? k.SeenNeed > b.SeenNeed || k.SeenNeed == b.SeenNeed && (k.Tick < b.Tick || k.Tick == b.Tick && k.Requester < b.Requester)
                    : k.Tick < b.Tick || k.Tick == b.Tick && k.Requester < b.Requester;
                if (better) best = i;
            }
            return best;
        }

        private static byte ControlOf(GodState s, int npc) => s.Divinity != null ? (byte)Divinity.Control.Of(s, npc) : (byte)0;

        /// <summary>The subject's true need (analytics only): for a ration, its hunger band 0-3 and whether it holds food.</summary>
        private static short TrueNeedOf(GodState s, ServiceKind kind, SubjectKind sk, int subject)
        {
            if (sk != SubjectKind.Npc) return 0;
            var n = s.Population.Find(subject);
            if (n == null) return 0;
            if (kind == ServiceKind.Ration)
                return (short)Cognition.Thinker.Band(s.Tuning.Population, Drive.Hunger, n.DriveOf(Drive.Hunger));
            return 0;
        }
    }
}
