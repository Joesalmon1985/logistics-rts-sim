namespace God.Sim.Divinity
{
    /// <summary>
    /// D1's part of <see cref="DivinityState"/> (F1.2: created empty by F1; OWNER: D1). Its sub-object types are declared below; the container hashes them in the fixed order.
    /// </summary>
    public sealed partial class DivinityState
    {
    }

    /// <summary>D1's sub-object (OWNER: D1). Add fields here and implement <c>partial void Hash(ref StateHasher h)</c>.</summary>
    public sealed partial class GodRegistry
    {
    }

    /// <summary>D1's sub-object (OWNER: D1). Add fields here and implement <c>partial void Hash(ref StateHasher h)</c>.</summary>
    public sealed partial class InterventionRegistry
    {
    }
}
