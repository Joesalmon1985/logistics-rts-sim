namespace God.Sim.Divinity
{
    /// <summary>D7's rows of the event catalogue (contracts 15.1 codes). OWNER: D7. Empty in F1.</summary>
    public static partial class DivinityEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> D7 => new ReadOnlyArray<EventInfo>();
    }
}
