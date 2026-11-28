namespace God.Sim.Unified
{
    /// <summary>UB1's rows of the event catalogue (contracts 23.12, block in <see cref="UnifiedEvents.BlockOf"/>). OWNER: UB1. Empty at UN0.</summary>
    public static partial class UnifiedEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> UB1 => new ReadOnlyArray<EventInfo>();
    }
}
