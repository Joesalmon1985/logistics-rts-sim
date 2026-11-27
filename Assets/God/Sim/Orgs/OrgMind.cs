using God.Sim.Population;
using Newtonsoft.Json;

namespace God.Sim.Orgs
{
    /// <summary>
    /// Contracts 0.1 F1.9b: one belief of a person about an organisation (ORGANISATIONS 3.4): what it believes its own role is,
    /// who it believes heads it, where it believes the place and the store are, the policies it knows and its own terms as it
    /// understood them, its trust in the organisation (a pseudo-agent of its social memory), how sure it is and when it last
    /// learned anything. At most <see cref="OrgMind.Cap"/> per mind (about 6 x 44 B).
    /// </summary>
    public partial struct OrgBelief
    {
        public int Org;
        public int Template;
        /// <summary>Its believed role (<see cref="RoleKind"/>; 0 none: it only knows the organisation exists).</summary>
        public byte MyRole;
        /// <summary>The office it believes it holds (-1 none).</summary>
        public sbyte MyOffice;
        /// <summary>The powers it believes its office gives it (<see cref="OrgPower"/> bits).</summary>
        public uint MyPowers;
        public int BelievedHead;
        /// <summary>When it last asked this organisation for a service (tick, 0 never).</summary>
        public int LastAsk;
        public int BelievedPlace, BelievedStoreNode, BelievedStoreBin;
        /// <summary>Bits of the <see cref="OrgPolicyKey"/> values it has heard or read.</summary>
        public uint KnownPolicies;
        /// <summary>Its terms as understood: the ration it believes it may get, the queue order and store access it believes hold.</summary>
        public short Ration;
        public byte QueueOrder, StoreAccess;
        public short Trust;
        public byte Confidence;
        public int Tick;
    }

    /// <summary>
    /// An opportunity it recognised (ORGANISATIONS 3.4, 4.1): a template, its need evidence from its own experience, up to four
    /// believed supporters (as it recognises them) with which of them accepted its proposal and when it last asked each.
    /// </summary>
    public struct OpportunityBelief
    {
        public int Template;
        public short NeedEvidence;
        public int S0, S1, S2, S3;
        public int Ask0, Ask1, Ask2, Ask3;
        /// <summary>Bit i: supporter i accepted (within the proposal window, from <see cref="AcceptTick"/>).</summary>
        public byte AcceptedMask;
        public int AcceptTick;
        public int Formed;
        public int LastSignalDay, LastSignalA, LastSignalB;

        public int Supporter(int i) => i == 0 ? S0 : i == 1 ? S1 : i == 2 ? S2 : S3;
        public int AskTick(int i) => i == 0 ? Ask0 : i == 1 ? Ask1 : i == 2 ? Ask2 : Ask3;
        public void SetSupporter(int i, int v) { if (i == 0) S0 = v; else if (i == 1) S1 = v; else if (i == 2) S2 = v; else S3 = v; }
        public void SetAsk(int i, int v) { if (i == 0) Ask0 = v; else if (i == 1) Ask1 = v; else if (i == 2) Ask2 = v; else Ask3 = v; }
        public int IndexOf(int npcAs) { for (int i = 0; i < 4; i++) if (npcAs != 0 && Supporter(i) == npcAs) return i; return -1; }
        public int AcceptedCount() { int n = 0; for (int i = 0; i < 4; i++) if ((AcceptedMask & (1 << i)) != 0) n++; return n; }
    }

    /// <summary>A proposal or a call it heard and how it answered: founding (Org 0, a template) or joining an existing organisation.</summary>
    public struct OrgInvite
    {
        public int Proposer;
        public int Template;
        public int Org;
        public int Tick;
        /// <summary>It answered Accept (a pledge: binds no one).</summary>
        public bool Accepted;
        /// <summary>A meeting call from this proposer is open as it heard it (tick, 0 none).</summary>
        public int MeetingTick;
        /// <summary>It answered the meeting call.</summary>
        public bool MeetingAnswered;
    }

    /// <summary>A request it perceived as a provider (in its own mind; the organisation knows of it once it is opened in the ring).</summary>
    public struct KnownRequest
    {
        public int Org;
        public ServiceKind Kind;
        /// <summary>The requester as it recognised it.</summary>
        public int Requester;
        public int Tick;
        public short SeenNeed;
        /// <summary>The RequestId once opened (0 before; informal requests keep 0 until a provider opens them).</summary>
        public int Request;
        public bool Done;
        public int MessageId;
    }

    /// <summary>Something a record-keeper saw happen (what it may write later): a deposit, a ration, a refusal, a member joining.</summary>
    public struct Witnessed
    {
        public int Org;
        public RecordKind Kind;
        public int Ref;
        public Ware Ware;
        public int Count;
        public int Tick;
    }

    /// <summary>The C6 act in progress (like M5's <c>SocialAct</c>): its kind, organisation, target, request and phase.</summary>
    public struct OrgAct
    {
        public byte Kind;
        public int Org;
        public int Target;
        public int Bin;
        public int Request;
        public byte Phase;
        public int Lot;
        public int Count;
        public long StartTick;
    }

    /// <summary>
    /// F1.9b: <c>NpcMind.Orgs</c>, a person's organisation beliefs (C6). Null unless Orgs is on and something about an
    /// organisation enters this mind (a practice, an opportunity, a proposal heard, a membership), so with nothing of the kind
    /// the mind hashes as before. OWNER: C6. The <see cref="Tuning"/> reference is bound by <see cref="OrgModule"/> (never saved).
    /// </summary>
    public sealed partial class OrgMind
    {
        public const int Cap = 6, OppCap = 2, InviteCap = 3, KnownCap = 4, WitnessCap = 6;

        // ---- F1.9b slot (unchanged): the beliefs and the hash entry ----
        public OrgBelief[] Beliefs;
        public int Count;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Count);
            HashC6(ref h);
        }
        partial void HashC6(ref StateHasher h);

        /// <summary>Bits of template ids whose practice it knows (culture at the start, told, or seen).</summary>
        public int Practices;
        public OpportunityBelief[] Opportunities = new OpportunityBelief[OppCap];
        public int OppCount;
        public OrgInvite[] Invites = new OrgInvite[InviteCap];
        public int InviteCount;
        /// <summary>Requests it perceived as a provider (C7 reuses it for informal requests, INSTITUTIONAL_SERVICES 3.1).</summary>
        public KnownRequest[] Known = new KnownRequest[KnownCap];
        public int KnownCount;
        public Witnessed[] Witness = new Witnessed[WitnessCap];
        public int WitnessCount;
        /// <summary>The highest message id it has read for organisation purposes (each message is read once).</summary>
        public int LastMsg;
        public OrgAct Act;

        [JsonIgnore] public OrgC6Tuning Tuning;
        [JsonIgnore] public OrgC8Tuning Welfare;

        /// <summary>A new organisation mind bound to the game's numbers (sim side: <see cref="Orgs.NewMind"/>).</summary>
        public static OrgMind Create(OrgC6Tuning c6, OrgC8Tuning c8) => new OrgMind { Beliefs = new OrgBelief[Cap], Tuning = c6, Welfare = c8 };

        public int IndexOfOrg(int org) { for (int i = 0; i < Count; i++) if (Beliefs[i].Org == org) return i; return -1; }
        public int IndexOfOpp(int template) { for (int i = 0; i < OppCount; i++) if (Opportunities[i].Template == template) return i; return -1; }
        public bool Knows(int template) => template > 0 && template < 31 && (Practices & (1 << template)) != 0;

        /// <summary>The belief about an organisation, created (evicting the oldest non-member belief) when absent.</summary>
        public int BeliefFor(int org, int tick)
        {
            int i = IndexOfOrg(org);
            if (i >= 0) return i;
            if (Beliefs == null) Beliefs = new OrgBelief[Cap];
            if (Count < Cap) i = Count++;
            else
            {
                i = -1;
                for (int k = 0; k < Count; k++) if (Beliefs[k].MyRole == 0 && (i < 0 || Beliefs[k].Tick < Beliefs[i].Tick)) i = k;
                if (i < 0) i = 0;
                for (int k = i; k < Count - 1; k++) Beliefs[k] = Beliefs[k + 1];
                i = Count - 1;
            }
            Beliefs[i] = new OrgBelief { Org = org, MyOffice = -1, Tick = tick };
            return i;
        }

        public void Remember(in Witnessed w)
        {
            if (WitnessCount == WitnessCap) { for (int k = 0; k < WitnessCap - 1; k++) Witness[k] = Witness[k + 1]; WitnessCount--; }
            Witness[WitnessCount++] = w;
        }

        public void RemoveWitness(int i) { for (int k = i; k < WitnessCount - 1; k++) Witness[k] = Witness[k + 1]; WitnessCount--; Witness[WitnessCount] = default; }

        public void AddKnown(in KnownRequest r)
        {
            for (int i = 0; i < KnownCount; i++) if (Known[i].Org == r.Org && Known[i].Requester == r.Requester && Known[i].Kind == r.Kind && !Known[i].Done) return;
            if (KnownCount == KnownCap)
            {
                int drop = 0;
                for (int i = 0; i < KnownCount; i++) if (Known[i].Done) { drop = i; break; }
                RemoveKnown(drop);
            }
            Known[KnownCount++] = r;
        }

        public void RemoveKnown(int i) { for (int k = i; k < KnownCount - 1; k++) Known[k] = Known[k + 1]; KnownCount--; Known[KnownCount] = default; }

        public int InviteFrom(int proposer, int template, int org)
        {
            for (int i = 0; i < InviteCount; i++) if (Invites[i].Proposer == proposer && Invites[i].Template == template && Invites[i].Org == org) return i;
            return -1;
        }

        public int AddInvite(in OrgInvite v)
        {
            int i = InviteFrom(v.Proposer, v.Template, v.Org);
            if (i >= 0) { Invites[i].Tick = v.Tick; return i; }
            if (InviteCount == InviteCap) { for (int k = 0; k < InviteCap - 1; k++) Invites[k] = Invites[k + 1]; InviteCount--; }
            Invites[InviteCount] = v;
            return InviteCount++;
        }

        public void RemoveInvite(int i) { for (int k = i; k < InviteCount - 1; k++) Invites[k] = Invites[k + 1]; InviteCount--; Invites[InviteCount] = default; }

        public void RemoveOpp(int i) { for (int k = i; k < OppCount - 1; k++) Opportunities[k] = Opportunities[k + 1]; OppCount--; Opportunities[OppCount] = default; }

        partial void HashC6(ref StateHasher h)
        {
            for (int i = 0; i < Count; i++)
            {
                var b = Beliefs[i];
                h.Add(b.Org); h.Add(b.Template); h.Add(b.MyRole); h.Add(b.MyOffice); h.Add(b.MyPowers); h.Add(b.LastAsk); h.Add(b.BelievedHead); h.Add(b.BelievedPlace); h.Add(b.BelievedStoreNode); h.Add(b.BelievedStoreBin);
                h.Add(b.KnownPolicies); h.Add(b.Ration); h.Add(b.QueueOrder); h.Add(b.StoreAccess); h.Add(b.Trust); h.Add(b.Confidence); h.Add(b.Tick);
            }
            h.Add(Practices);
            h.Add(OppCount);
            for (int i = 0; i < OppCount; i++)
            {
                var o = Opportunities[i];
                h.Add(o.Template); h.Add(o.NeedEvidence); h.Add(o.S0); h.Add(o.S1); h.Add(o.S2); h.Add(o.S3); h.Add(o.Ask0); h.Add(o.Ask1); h.Add(o.Ask2); h.Add(o.Ask3);
                h.Add(o.AcceptedMask); h.Add(o.AcceptTick); h.Add(o.Formed); h.Add(o.LastSignalDay); h.Add(o.LastSignalA); h.Add(o.LastSignalB);
            }
            h.Add(InviteCount);
            for (int i = 0; i < InviteCount; i++)
            {
                var v = Invites[i];
                h.Add(v.Proposer); h.Add(v.Template); h.Add(v.Org); h.Add(v.Tick); h.Add(v.Accepted ? 1 : 0); h.Add(v.MeetingTick); h.Add(v.MeetingAnswered ? 1 : 0);
            }
            h.Add(KnownCount);
            for (int i = 0; i < KnownCount; i++)
            {
                var k = Known[i];
                h.Add(k.Org); h.Add((int)k.Kind); h.Add(k.Requester); h.Add(k.Tick); h.Add(k.SeenNeed); h.Add(k.Request); h.Add(k.Done ? 1 : 0); h.Add(k.MessageId);
            }
            h.Add(WitnessCount);
            for (int i = 0; i < WitnessCount; i++) { var w = Witness[i]; h.Add(w.Org); h.Add((int)w.Kind); h.Add(w.Ref); h.Add((int)w.Ware); h.Add(w.Count); h.Add(w.Tick); }
            h.Add(LastMsg);
            h.Add(Act.Kind); h.Add(Act.Org); h.Add(Act.Target); h.Add(Act.Bin); h.Add(Act.Request); h.Add(Act.Phase); h.Add(Act.Lot); h.Add(Act.Count); h.Add(Act.StartTick);
        }
    }
}
