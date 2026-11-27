namespace God.Runner.Warehouse
{
    /// <summary>D6's warehouse mappers (OWNER: D6). Empty in F1: D6 registers its events here when it lands them.</summary>
    public static class D6Mappings
    {
        /// <summary>The grain of each D6 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
