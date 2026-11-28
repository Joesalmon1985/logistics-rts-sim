namespace God.Sim.Unified
{
    /// <summary>UN1's slots in <see cref="UnifiedHooks"/> (contracts 23.3): the world replaces the legacy wild sources.</summary>
    public static partial class UnifiedHooks
    {
        static partial void ReplacesSourcesUN1(GodState s, ref bool replaces) { replaces = Unified.WorldOn(s); }
    }

    /// <summary>UN1's slots in <see cref="UnifiedModule"/>.</summary>
    public static partial class UnifiedModule
    {
        static partial void StartUN1(GodState s)
        {
            if (!Unified.WorldOn(s)) return;
            s.Unification.HexKinds = UnifiedBoard.TypesOf(s.Board);
        }
    }
}
