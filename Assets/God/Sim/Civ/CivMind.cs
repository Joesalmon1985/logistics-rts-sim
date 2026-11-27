namespace God.Sim.Civ
{
    /// <summary>
    /// F1.9b: <c>NpcMind.Civ</c>, a person's believed obligations (C2) and believed ownership (C1). Null unless Civ is on and a
    /// C package creates it; hashed only when present. OWNER: C2/C1 (F1 lands the slot; each fills its own hash slot in its
    /// own partial file, C1 first).
    /// </summary>
    public sealed partial class CivMind
    {
        public void AddTo(ref StateHasher h)
        {
            HashC1(ref h);
            HashC2(ref h);
        }
        partial void HashC1(ref StateHasher h);
        partial void HashC2(ref StateHasher h);
    }
}
