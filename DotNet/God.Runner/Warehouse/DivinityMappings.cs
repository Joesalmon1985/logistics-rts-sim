namespace God.Runner.Warehouse
{
    /// <summary>
    /// F1.15 (contracts 18.2): the divinity mappers, called by <c>EventMapRegistry.CreateDefault</c> after M4 and M5. Each package
    /// registers its mappers only in its own file (<c>&lt;Pkg&gt;Mappings.cs</c>), its tables only in its <c>Tables.*.cs</c>
    /// and its migration. OWNER: F1 (this dispatcher frozen); the package stubs to D1-D7.
    /// </summary>
    public static class DivinityMappings
    {
        /// <summary>Registers the packages' mappers in package order.</summary>
        public static void Register(EventMapRegistry r)
        {
            D1Mappings.Register(r);
            D2Mappings.Register(r);
            D3Mappings.Register(r);
            D4Mappings.Register(r);
            D5Mappings.Register(r);
            D6Mappings.Register(r);
            D7Mappings.Register(r);
        }
    }
}
