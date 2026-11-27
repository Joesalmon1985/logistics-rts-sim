namespace God.Runner.Warehouse
{
    /// <summary>C9's warehouse mappers (OWNER: C9). Empty in F1: C9 registers its events here when it lands them.</summary>
    public static class C9Mappings
    {
        /// <summary>The grain of each C9 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
