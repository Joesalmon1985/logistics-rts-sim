namespace God.Runner.Warehouse
{
    /// <summary>
    /// UN0 (contracts 23.13, R85): the unified mappers, called by <c>EventMapRegistry.CreateDefault</c> after divinity. Each event-owning
    /// package registers its mappers only in its own stub file (<c>UNnMappings.cs</c>); UN11a completes them and owns migrations 0044-0047.
    /// OWNER: UN0 (this dispatcher frozen); the stubs to their packages.
    /// </summary>
    public static class UnifiedMappings
    {
        /// <summary>Registers the packages' mappers in code-block order.</summary>
        public static void Register(EventMapRegistry r)
        {
            UN1Mappings.Register(r);
            UN2Mappings.Register(r);
            UN3bMappings.Register(r);
            UN4Mappings.Register(r);
            UN5Mappings.Register(r);
            UN6Mappings.Register(r);
            UN7Mappings.Register(r);
            UN8aMappings.Register(r);
            UN8bMappings.Register(r);
            UB1Mappings.Register(r);
        }
    }
}
