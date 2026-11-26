namespace God.Runner.Warehouse
{
    /// <summary>
    /// M4's warehouse mappers (F0.14 stub; OWNER: M4). <c>EventMapRegistry.CreateDefault</c> calls <see cref="Register"/>; M4
    /// registers its events here, declares its tables in <c>Tables.Households.cs</c> and its DDL in <c>Migrations/0005_households.sql</c>.
    /// </summary>
    public static class HouseholdMappings
    {
        /// <summary>The grain of each M4 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = new (string, string, string)[0];

        public static void Register(EventMapRegistry r) { }
    }
}
