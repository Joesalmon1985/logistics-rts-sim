namespace God.Runner.Warehouse
{
    /// <summary>C1's warehouse mappers (OWNER: C1). Empty in F1: C1 registers its events here when it lands them.</summary>
    public static class C1Mappings
    {
        /// <summary>The grain of each C1 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
