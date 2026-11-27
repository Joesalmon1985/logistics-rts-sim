namespace God.Runner.Warehouse
{
    /// <summary>C10's warehouse mappers (OWNER: C10). Empty in F1: C10 registers its events here when it lands them.</summary>
    public static class C10Mappings
    {
        /// <summary>The grain of each C10 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
