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

        /// <summary>
        /// The root random stream of the game. M0: nothing draws from it directly and it never advances; every draw uses a
        /// stream keyed by purpose and entity forked from it (<c>Rng.Fork("dice", roll)</c>, <c>Rng.Fork("duel", unit, tick)</c>),
        /// so adding an entity or a draw never shifts another stream (conventions section 3).
        /// </summary>
        public Rng Rng = new Rng();

        /// <summary>This game's tuning (M0: replaces the mutable statics). Saved and hashed with the state.</summary>
        public SimTuning Tuning = new SimTuning();

        public Board Board;
    }
}
