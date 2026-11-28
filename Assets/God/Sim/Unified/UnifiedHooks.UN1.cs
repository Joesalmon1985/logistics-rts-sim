using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>UN1's slots in <see cref="UnifiedHooks"/> (contracts 23.3): the world replaces the legacy wild sources; the seven need channels tick per person.</summary>
    public static partial class UnifiedHooks
    {
        static partial void ReplacesSourcesUN1(GodState s, ref bool replaces) { replaces = Unified.WorldOn(s); }

        static partial void PersonTickUN1(GodState s, EventBuffer ev, Npc npc)
        {
            if (Unified.WorldOn(s)) Needs.Tick(s, ev, npc);
        }
    }

    /// <summary>UN1's slots in <see cref="UnifiedModule"/>.</summary>
    public static partial class UnifiedModule
    {
        static partial void StartUN1(GodState s)
        {
            if (Unified.WorldOn(s)) World.Generate(s);
        }
    }
}
