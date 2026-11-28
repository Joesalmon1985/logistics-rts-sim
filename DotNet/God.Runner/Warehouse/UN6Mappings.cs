namespace God.Runner.Warehouse
{
    /// <summary>UN6's warehouse mappers (OWNER: UN6; UN11a completes), called by UN0's <c>UnifiedMappings.Register</c>. Empty at UN0.</summary>
    public static class UN6Mappings
    {
        /// <summary>The grain of each UN6 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
