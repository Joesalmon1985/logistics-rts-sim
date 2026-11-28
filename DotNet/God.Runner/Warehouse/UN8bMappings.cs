namespace God.Runner.Warehouse
{
    /// <summary>UN8b's warehouse mappers (OWNER: UN8b; UN11a completes), called by UN0's <c>UnifiedMappings.Register</c>. Empty at UN0.</summary>
    public static class UN8bMappings
    {
        /// <summary>The grain of each UN8b table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
