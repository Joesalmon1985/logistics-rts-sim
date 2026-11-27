namespace God.Runner
{
    /// <summary>
    /// Exit codes (HEADLESS_SIMULATION 4.11). A batch returns the highest code among its runs (the table's priority, bottom
    /// up, is also numeric order). <see cref="NotImplemented"/> is an M0 addition for the commands of later milestones.
    /// </summary>
    public static class ExitCodes
    {
        public const int Ok = 0;
        /// <summary>A check or fail condition failed (the smoke verdict, a metric threshold).</summary>
        public const int CheckFailed = 1;
        /// <summary>Bad arguments or an unknown command.</summary>
        public const int Usage = 2;
        /// <summary>Invalid input file: scenario, replay, save (unknown key, wrong version, hash mismatch).</summary>
        public const int InvalidInput = 3;
        /// <summary>Determinism divergence: a checkpoint differs.</summary>
        public const int Divergence = 4;
        /// <summary>An invariant failed (SimInvariants).</summary>
        public const int InvariantFailed = 5;
        /// <summary>An unhandled exception in the sim.</summary>
        public const int SimException = 6;
        /// <summary>Performance budget missed (bench --compare).</summary>
        public const int PerformanceMissed = 7;
        /// <summary>I/O or recorder error; the sim result is still valid and the replay was written if possible.</summary>
        public const int IoError = 8;
        /// <summary>M0 addition: a command of a later milestone (lab, experiment, serve, dataset, verify-model, ...).</summary>
        public const int NotImplemented = 9;
        /// <summary>Interrupted (Ctrl+C); partial outputs are flushed and marked "interrupted".</summary>
        public const int Interrupted = 130;

        /// <summary>The higher-priority code of two (the batch rule).</summary>
        public static int Worst(int a, int b) => a >= b ? a : b;
    }
}
