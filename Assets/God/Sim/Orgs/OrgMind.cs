namespace God.Sim.Orgs
{
    /// <summary>
    /// Contracts 0.1 F1.9b: one belief of a person about an organisation (C6 adds the fields in its own partial file; at most
    /// <see cref="OrgMind.Cap"/> per mind, about 144 B in all).
    /// </summary>
    public partial struct OrgBelief
    {
    }

    /// <summary>
    /// F1.9b: <c>NpcMind.Orgs</c>, a person's organisation beliefs (C6). Null unless Orgs is on and C6 creates it; hashed only
    /// when present. OWNER: C6 (F1 lands the slot; C6 fills <c>HashC6</c>).
    /// </summary>
    public sealed partial class OrgMind
    {
        public const int Cap = 6;

        public OrgBelief[] Beliefs;
        public int Count;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Count);
            HashC6(ref h);
        }
        partial void HashC6(ref StateHasher h);
    }
}
