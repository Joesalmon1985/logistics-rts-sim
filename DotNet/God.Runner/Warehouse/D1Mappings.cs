namespace God.Runner.Warehouse
{
    /// <summary>D1's warehouse mappers (OWNER: D1). Empty in F1: D1 registers its events here when it lands them.</summary>
    public static class D1Mappings
    {
        /// <summary>The grain of each D1 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
