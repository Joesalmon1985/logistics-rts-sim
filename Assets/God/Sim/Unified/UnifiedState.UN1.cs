namespace God.Sim.Unified
{
    /// <summary>UN1's saved state (contracts 23.8, 23.14): the hex types, hashed in the <c>AddToUN1</c> slot. Public fields, ascending-id lists, no dictionaries.</summary>
    public sealed partial class UnifiedState
    {
        /// <summary>The <see cref="HexType"/> of every board hex (index = hex id), filled at the world start; saved, never regenerated on load.</summary>
        public HexType[] HexKinds = new HexType[0];

        partial void AddToUN1(ref StateHasher h)
        {
            h.Add(HexKinds.Length); foreach (var k in HexKinds) h.Add((int)k);
        }
    }
}
