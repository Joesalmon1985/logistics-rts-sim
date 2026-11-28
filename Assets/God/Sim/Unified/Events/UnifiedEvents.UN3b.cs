namespace God.Sim.Unified
{
    /// <summary>UN3b's rows of the event catalogue (contracts 23.12, block in <see cref="UnifiedEvents.BlockOf"/>). OWNER: UN3b. Empty at UN0.</summary>
    public static partial class UnifiedEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> UN3b => new ReadOnlyArray<EventInfo>();
    }
}
