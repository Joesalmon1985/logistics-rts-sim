namespace God.Runner.Warehouse
{
    /// <summary>D4's warehouse mappers (OWNER: D4). Empty in F1: D4 registers its events here when it lands them.</summary>
    public static class D4Mappings
    {
        /// <summary>The grain of each D4 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
