namespace God.Runner.Warehouse
{
    /// <summary>
    /// F1.15 (contracts 18.2): the organisation mappers, called by <c>EventMapRegistry.CreateDefault</c> after M4 and M5. Each package
    /// registers its mappers only in its own file (<c>&lt;Pkg&gt;Mappings.cs</c>), its tables only in its <c>Tables.*.cs</c>
    /// and its migration. OWNER: F1 (this dispatcher frozen); the package stubs to C6-C10.
    /// </summary>
    public static class OrgMappings
    {
        /// <summary>Registers the packages' mappers in package order.</summary>
        public static void Register(EventMapRegistry r)
        {
            C6Mappings.Register(r);
            C7Mappings.Register(r);
            C8Mappings.Register(r);
            C9Mappings.Register(r);
            C10Mappings.Register(r);
        }
    }
}
