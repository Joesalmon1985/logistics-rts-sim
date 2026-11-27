namespace God.Runner.Warehouse
{
    /// <summary>C5's warehouse mappers (OWNER: C5). Empty in F1: C5 registers its events here when it lands them.</summary>
    public static class C5Mappings
    {
        /// <summary>The grain of each C5 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
