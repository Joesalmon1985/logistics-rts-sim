namespace God.Runner.Warehouse
{
    /// <summary>D3's warehouse mappers (OWNER: D3). Empty in F1: D3 registers its events here when it lands them.</summary>
    public static class D3Mappings
    {
        /// <summary>The grain of each D3 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
