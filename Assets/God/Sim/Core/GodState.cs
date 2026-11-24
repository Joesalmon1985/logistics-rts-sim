namespace God.Sim
{
    /// <summary>
    /// The complete simulation state. Plain data only (no UnityEngine types). PARTIAL: later packages (economy,
    /// military) add their fields in their own files.
    /// </summary>
    public sealed partial class GodState
    {
        public ulong Seed;

        /// <summary>Number of completed <see cref="GodSim.Step()"/> calls. 10 ticks = 1 second.</summary>
        public long Tick;

        /// <summary>Shared deterministic RNG. Prefer <c>Rng.Fork("module")</c> for module-local streams.</summary>
        public Rng Rng = new Rng();

        public Board Board;
    }
}
