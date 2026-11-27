namespace God.Runner.Warehouse
{
    /// <summary>D7's warehouse mappers (OWNER: D7). Empty in F1: D7 registers its events here when it lands them.</summary>
    public static class D7Mappings
    {
        /// <summary>The grain of each D7 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
