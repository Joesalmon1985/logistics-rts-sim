namespace God.Runner.Warehouse
{
    /// <summary>UN3b's warehouse mappers (OWNER: UN3b; UN11a completes), called by UN0's <c>UnifiedMappings.Register</c>. Empty at UN0.</summary>
    public static class UN3bMappings
    {
        /// <summary>The grain of each UN3b table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
