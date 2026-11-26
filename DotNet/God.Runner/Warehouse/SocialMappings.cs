namespace God.Runner.Warehouse
{
    /// <summary>
    /// M5's warehouse mappers (F0.14 stub; OWNER: M5). <c>EventMapRegistry.CreateDefault</c> calls <see cref="Register"/>; M5
    /// registers its events here, declares its tables in <c>Tables.Social.cs</c> and its DDL in <c>Migrations/0006_social.sql</c>.
    /// </summary>
    public static class SocialMappings
    {
        /// <summary>The grain of each M5 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = new (string, string, string)[0];

        public static void Register(EventMapRegistry r) { }
    }
}
