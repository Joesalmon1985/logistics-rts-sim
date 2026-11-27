namespace God.Runner.Warehouse
{
    /// <summary>C2's warehouse mappers (OWNER: C2). Empty in F1: C2 registers its events here when it lands them.</summary>
    public static class C2Mappings
    {
        /// <summary>The grain of each C2 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
