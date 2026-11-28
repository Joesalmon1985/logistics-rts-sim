namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.1): the one switch query. Every switch-gated site reads only <see cref="On"/> (and the sub-switch
    /// helpers below, which are false whenever <see cref="On"/> is). <see cref="On"/> is true exactly when the game is unified:
    /// <c>GodState.Unification</c> exists, which <c>GodSim.Create</c> makes only when <c>Unification.Enabled = 1</c> (and a load keeps).
    /// From another namespace call it as <c>Unified.Unified.On(s)</c> (the namespace and the class share the name).
    /// </summary>
    public static class Unified
    {
        /// <summary>True in a unified game (contracts 23.1). Legacy games: false at every site, so their paths are byte-identical.</summary>
        public static bool On(GodState s) => s != null && s.Unification != null;

        /// <summary>True when the tuning asks for a unified game (used before the state exists: <c>GodSim.Create</c>).</summary>
        public static bool Requested(SimTuning t) => t != null && t.Unification != null && t.Unification.Enabled == 1;

        public static bool WorldOn(GodState s) => On(s) && s.Tuning.Unification.World != 0;
        public static bool InteractionsOn(GodState s) => On(s) && s.Tuning.Unification.Interactions != 0;
        public static bool ConstructionOn(GodState s) => On(s) && s.Tuning.Unification.Construction != 0;
        public static bool ExchangeOn(GodState s) => On(s) && s.Tuning.Unification.Exchange != 0;
        public static bool ConflictOn(GodState s) => On(s) && s.Tuning.Unification.Conflict != 0;

        /// <summary>True when need channel <paramref name="c"/> is active (<c>Needs.ActiveMask</c>).</summary>
        public static bool NeedActive(GodState s, NeedChannel c) => On(s) && (s.Tuning.Unification.Needs.ActiveMask & (1 << (int)c)) != 0;
    }

    /// <summary>
    /// The twelve need channels in the encoder's order (contracts 23.6): the five drives (Hunger, Energy, Warmth, Safety, Pain)
    /// then the seven unified channels stored in <see cref="UnifiedNpc.Needs"/> (index = channel - <see cref="FirstExtension"/>).
    /// </summary>
    public enum NeedChannel : byte
    {
        Hunger = 0, Energy = 1, Warmth = 2, Safety = 3, Pain = 4,
        Thirst = 5, Hygiene = 6, Bladder = 7, Social = 8, Fun = 9, Comfort = 10, Room = 11,
    }

    public static class NeedChannels
    {
        public const int Count = 12, FirstExtension = 5, Extensions = 7;
    }
}
