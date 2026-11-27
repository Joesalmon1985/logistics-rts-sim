namespace God.Sim.Civ
{
    /// <summary>C5's rows of the event catalogue (contracts 15.1 codes). OWNER: C5. Empty in F1.</summary>
    public static partial class CivEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> C5 => new ReadOnlyArray<EventInfo>();
    }
}
