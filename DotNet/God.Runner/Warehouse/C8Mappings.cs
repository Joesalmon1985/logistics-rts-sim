namespace God.Runner.Warehouse
{
    /// <summary>C8's warehouse mappers (OWNER: C8). Empty in F1: C8 registers its events here when it lands them.</summary>
    public static class C8Mappings
    {
        /// <summary>The grain of each C8 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
