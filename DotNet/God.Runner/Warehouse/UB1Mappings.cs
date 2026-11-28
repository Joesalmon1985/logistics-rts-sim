namespace God.Runner.Warehouse
{
    /// <summary>UB1's warehouse mappers (OWNER: UB1; UN11a completes), called by UN0's <c>UnifiedMappings.Register</c>. Empty at UN0.</summary>
    public static class UB1Mappings
    {
        /// <summary>The grain of each UB1 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
