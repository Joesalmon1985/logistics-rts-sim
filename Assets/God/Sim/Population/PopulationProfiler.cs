namespace God.Sim.Population
{
    /// <summary>The phases of the population update a host profiler can time (M3b, ACCEPTANCE M3.19).</summary>
    public static class PopPhase
    {
        public const int Day = 0, Body = 1, Credit = 2, Sense = 3, Cognition = 4, Decide = 5, LearnUpdate = 6, Act = 7, Consolidate = 8;
        public const int Count = 9;
        public static readonly ReadOnlyArray<string> Names = new ReadOnlyArray<string>(
            "day start", "body", "credit and close", "senses", "cognition", "decide", "learning update", "act", "consolidation");
    }

    /// <summary>
    /// A host hook (the runner's bench), never state: the population calls <see cref="Mark"/> at the end of each phase, and
    /// the host attributes the time since the previous mark to that phase. The sim never reads a clock; with no profiler
    /// set nothing is called. <see cref="Begin"/> starts each tick's population update.
    /// </summary>
    public interface IPopulationProfiler
    {
        void Begin();
        void Mark(int phase);
    }
}
