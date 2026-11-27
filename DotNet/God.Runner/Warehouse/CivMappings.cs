namespace God.Runner.Warehouse
{
    /// <summary>
    /// F1.15 (contracts 18.2): the civilisation mappers, called by <c>EventMapRegistry.CreateDefault</c> after M4 and M5. Each package
    /// registers its mappers only in its own file (<c>&lt;Pkg&gt;Mappings.cs</c>), its tables only in its <c>Tables.*.cs</c>
    /// and its migration. OWNER: F1 (this dispatcher frozen); the package stubs to C1-C5.
    /// </summary>
    public static class CivMappings
    {
        /// <summary>Registers the packages' mappers in package order.</summary>
        public static void Register(EventMapRegistry r)
        {
            C1Mappings.Register(r);
            C2Mappings.Register(r);
            C3Mappings.Register(r);
            C4Mappings.Register(r);
            C5Mappings.Register(r);
        }
    }
}
