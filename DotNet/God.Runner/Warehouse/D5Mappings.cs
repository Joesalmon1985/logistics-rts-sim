namespace God.Runner.Warehouse
{
    /// <summary>D5's warehouse mappers (OWNER: D5). Empty in F1: D5 registers its events here when it lands them.</summary>
    public static class D5Mappings
    {
        /// <summary>The grain of each D5 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
