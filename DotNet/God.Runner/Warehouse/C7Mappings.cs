namespace God.Runner.Warehouse
{
    /// <summary>C7's warehouse mappers (OWNER: C7). Empty in F1: C7 registers its events here when it lands them.</summary>
    public static class C7Mappings
    {
        /// <summary>The grain of each C7 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
