namespace God.Runner.Warehouse
{
    /// <summary>UN4's warehouse mappers (OWNER: UN4; UN11a completes), called by UN0's <c>UnifiedMappings.Register</c>. Empty at UN0.</summary>
    public static class UN4Mappings
    {
        /// <summary>The grain of each UN4 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
