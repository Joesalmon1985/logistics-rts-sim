namespace God.Sim.Civ
{
    /// <summary>C1's rows of the event catalogue (contracts 15.1 codes). OWNER: C1. Empty in F1.</summary>
    public static partial class CivEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> C1 => new ReadOnlyArray<EventInfo>();
    }
}
