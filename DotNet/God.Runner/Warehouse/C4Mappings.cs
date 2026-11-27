namespace God.Runner.Warehouse
{
    /// <summary>C4's warehouse mappers (OWNER: C4). Empty in F1: C4 registers its events here when it lands them.</summary>
    public static class C4Mappings
    {
        /// <summary>The grain of each C4 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
