namespace God.Runner.Warehouse
{
    /// <summary>UN7's warehouse mappers (OWNER: UN7; UN11a completes), called by UN0's <c>UnifiedMappings.Register</c>. Empty at UN0.</summary>
    public static class UN7Mappings
    {
        /// <summary>The grain of each UN7 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
