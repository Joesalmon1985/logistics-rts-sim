namespace God.Sim.Orgs
{
    /// <summary>C9's rows of the event catalogue (contracts 15.1 codes). OWNER: C9. Empty in F1.</summary>
    public static partial class OrgEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> C9 => new ReadOnlyArray<EventInfo>();
    }
}
