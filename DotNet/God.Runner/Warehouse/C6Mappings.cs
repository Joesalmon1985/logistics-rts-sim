namespace God.Runner.Warehouse
{
    /// <summary>C6's warehouse mappers (OWNER: C6). Empty in F1: C6 registers its events here when it lands them.</summary>
    public static class C6Mappings
    {
        /// <summary>The grain of each C6 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
