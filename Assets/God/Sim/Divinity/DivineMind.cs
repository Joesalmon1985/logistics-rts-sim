using God.Sim.Comms;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 10.3 step 2: one divine instruction as the recipient decoded it (its belief, possibly misread), in
    /// <see cref="DivineMind.Inbox"/> and, at a think, in <c>ThinkScratch.Divine</c>. The topic names its target (a place by
    /// node, a person or thing by id), so <c>Thinker.Known</c> accepts it as communicated knowledge. OWNER: F1 (frozen; D2 fills it).
    /// </summary>
    public struct DecodedDivine
    {
        public int Intervention;
        public int God;
        public InterventionKind Kind;
        public DivineAct Act;
        public MsgTopic Topic;
        public MsgTime Time;
        public long Expires;
        public byte Clarity;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Intervention); h.Add(God); h.Add((int)Kind | ((int)Act << 8) | ((int)Time << 16) | (Clarity << 24));
            h.Add((int)Topic.Kind); h.Add(Topic.Id); h.Add(Topic.Sub); h.Add(Expires);
        }
    }

    /// <summary>Contracts 13.1: one person's beliefs about one god; five independent fields (none derived from another). D6.</summary>
    public struct GodBelief
    {
        public int God;
        public short Belief, Faith, Trust, Fear, Loyalty;
        public long LastEvidenceTick;

        public void AddTo(ref StateHasher h)
        {
            h.Add(God); h.Add(Belief); h.Add(Faith); h.Add(Trust); h.Add(Fear); h.Add(Loyalty); h.Add(LastEvidenceTick);
        }
    }

    /// <summary>
    /// Contracts 13.1: one commandment as this person knows it (<c>Commandment, Revision, Source, FromNpc, Adoption, Salience,
    /// Interpretation</c>). F1 lands the type empty; D6 adds the fields in its own partial file and hashes them in
    /// <see cref="DivineMind"/>'s <c>HashKnownD6</c> slot.
    /// </summary>
    public partial struct KnownCommandment
    {
    }

    /// <summary>
    /// F1.9b (contracts 13.1): <c>NpcMind.Divine</c>, separate fields (D5.15): D2's inbox (cap 4), D6's god beliefs (cap = god
    /// count) and known commandments (cap 8). Null unless Divinity is on and a D package creates it; hashed only when present.
    /// OWNER: F1 (shape frozen); D2 the inbox, D6 the rest.
    /// </summary>
    public sealed partial class DivineMind
    {
        public const int InboxCap = 4, KnownCap = 8;

        public DecodedDivine[] Inbox;
        public int InboxCount;
        public GodBelief[] Gods;
        public int GodCount;
        public KnownCommandment[] Known;
        public int KnownCount;

        public void AddTo(ref StateHasher h)
        {
            h.Add(InboxCount);
            for (int i = 0; i < InboxCount; i++) Inbox[i].AddTo(ref h);
            h.Add(GodCount);
            for (int i = 0; i < GodCount; i++) Gods[i].AddTo(ref h);
            h.Add(KnownCount);
            HashKnownD6(ref h);
        }
        partial void HashKnownD6(ref StateHasher h);
    }
}
