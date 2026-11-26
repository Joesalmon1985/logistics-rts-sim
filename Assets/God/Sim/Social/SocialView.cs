using God.Sim.Cognition;

namespace God.Sim.Social
{
    /// <summary>
    /// Reads of a mind's social memory for anyone (SHARED_CONTRACTS 4.2; M4 reads trust through these). Signatures frozen by
    /// F0; bodies OWNER: M5. Defaults when there is no social memory: 0, 0, <see cref="NpcRelClass.Unknown"/>.
    /// </summary>
    public static class SocialView
    {
        /// <summary>-1000..1000.</summary>
        public static int Trust(NpcMind me, int other) => 0;
        /// <summary>0..1000.</summary>
        public static int Familiarity(NpcMind me, int other) => 0;
        public static NpcRelClass ClassOf(NpcMind me, int other) => NpcRelClass.Unknown;
    }
}
