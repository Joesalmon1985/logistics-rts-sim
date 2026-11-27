namespace God.Sim.Divinity
{
    /// <summary>
    /// F1.1 (contracts 1): the identity rules of the divinity module. OWNER: F1 (frozen).
    /// <list type="bullet">
    /// <item><c>GodId = (int)faction + 1</c>, one god per initial faction, created once by <see cref="DivinityModule.StartGame"/>
    /// (D1); 1..FactionCount; never removed, merged or re-id'd.</item>
    /// <item><c>AvatarBodyId = 0x4000_0000 + GodId</c>: one body record per god for the whole game, above every NpcId.</item>
    /// <item>Counters (public, saved, hashed): <c>InterventionId</c> and <c>LedgerId</c> in <see cref="DivinityState"/>;
    /// <c>CommandmentId</c> and <c>ReligionId</c> in <see cref="DivinityState.Doctrine"/> (D6).</item>
    /// </list>
    /// </summary>
    public static class DivinityIds
    {
        public const int AvatarBodyBase = 0x4000_0000;

        /// <summary>The god created for a faction of the setup.</summary>
        public static int GodOf(Faction f) => (int)f + 1;

        /// <summary>The faction a god was created for (history only, never an authority: contracts 2, D5.3).</summary>
        public static Faction OriginFactionOf(int god) => (Faction)(god - 1);

        /// <summary>True for a GodId this game has (1..FactionCount).</summary>
        public static bool IsGod(GodState s, int god) => god >= 1 && god <= s.FactionCount;

        public static int AvatarBodyId(int god) => AvatarBodyBase + god;

        public static bool IsAvatarBodyId(int id) => id > AvatarBodyBase;

        /// <summary>The god whose avatar body this id is (0 for a person's NpcId).</summary>
        public static int GodOfBody(int id) => id > AvatarBodyBase ? id - AvatarBodyBase : 0;

        /// <summary>Takes the next id of a counter (ids start at 1 and are never reused; 0 means none).</summary>
        public static int Take(ref int counter) => counter++;
    }
}
