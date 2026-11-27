namespace God.Sim.Divinity
{
    /// <summary>
    /// D4's numbers, <c>Divinity.Possession</c> (POSSESSION_AND_AVATAR 4; contracts 11.1, 11.3). Fields only in this file
    /// (contracts 20.F1.1: the JSON field order is this file's). The cost and upkeep are D1's
    /// (<c>Divinity.Mythic.Costs.BeginPossession</c>, <c>Divinity.Mythic.Upkeep.PossessionPerTick</c>); the one-per-god limit is
    /// F1's <c>Control.MaxPossessionsPerGod</c> (contracts 21.2). OWNER: D4.
    /// </summary>
    public sealed partial class PossessionTuning
    {
        /// <summary>1: a person of any faction may be possessed (sandbox default); 0: only people of the god's origin faction.</summary>
        public int AllowForeign = 1;
        /// <summary>The youngest biological age that may be possessed, in milli-years (500 = half a sim year).</summary>
        public int MinAgeMilli = 500;
        /// <summary>
        /// Contracts 11.3: rewards a possessed body earns reach its learner scaled by this (0-1000): 0 it learns nothing from what
        /// its body did, 1000 as if it had chosen.
        /// </summary>
        public int LearnPermille = 1000;
        /// <summary>Days the person keeps the memory of having been taken (<c>SelfFeature.Possessed</c>, once a base slot exists).</summary>
        public int MemoryDays = 2;
        /// <summary>Share of a perceiving victim's social evidence moved from the person to the god (once a harmful act exists).</summary>
        public int BlameShiftPermille = 700;
    }
}
