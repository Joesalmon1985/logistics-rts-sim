namespace God.Sim.Civ
{
    /// <summary>C4's rows of the event catalogue (contracts 15.1 codes). OWNER: C4. Empty in F1.</summary>
    public static partial class CivEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> C4 => new ReadOnlyArray<EventInfo>();
    }
}
