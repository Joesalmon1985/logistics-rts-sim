namespace God.Sim.Unified
{
    /// <summary>UN8a's rows of the event catalogue (contracts 23.12, block in <see cref="UnifiedEvents.BlockOf"/>). OWNER: UN8a. Empty at UN0.</summary>
    public static partial class UnifiedEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> UN8a => new ReadOnlyArray<EventInfo>();
    }
}
