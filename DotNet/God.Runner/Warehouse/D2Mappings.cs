namespace God.Runner.Warehouse
{
    /// <summary>D2's warehouse mappers (OWNER: D2). Empty in F1: D2 registers its events here when it lands them.</summary>
    public static class D2Mappings
    {
        /// <summary>The grain of each D2 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
