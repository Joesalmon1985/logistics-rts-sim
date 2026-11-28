namespace God.Runner.Warehouse
{
    /// <summary>UN8a's warehouse mappers (OWNER: UN8a; UN11a completes), called by UN0's <c>UnifiedMappings.Register</c>. Empty at UN0.</summary>
    public static class UN8aMappings
    {
        /// <summary>The grain of each UN8a table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
