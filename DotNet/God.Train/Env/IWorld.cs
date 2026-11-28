using System.Collections.Generic;
using God.Sim.Unified;
using God.Train.Net;

namespace God.Train.Env
{
    /// <summary>
    /// Physical facts an evaluation counts (never read by the return function). Counted by the world from its own events, per
    /// episode: completions of level-1 collection shelters, raw units collected then deposited, hand-overs between distinct
    /// persons, deposits and withdrawals, recipes completed, and the interruption fixture's sites. -1 = the world cannot produce
    /// this fact (the stub world has no buildings), reported "not evaluable".
    /// </summary>
    public sealed class WorldFacts
    {
        public long FirstShelterTick = -1, FirstRawDepositTick = -1, FirstHandoverTick = -1, FirstRecipeTick = -1;
        public int Deposits, Withdrawals;
        public bool CanBuild, CanCollect, CanHandOver, CanRecipe, CanStore;
        public int Submissions, Accepted;
    }

    /// <summary>
    /// The environment seam the trainer drives: one world (the real unified <c>GodSim</c>, or the stub lattice used only by the
    /// harness self-tests), the same twelve controls, <c>Obs v1</c> observations and physical legality masks (contracts 23.5, 23.6).
    /// The trainer adapts to the world; the world never adapts to the trainer.
    /// </summary>
    public interface IWorld
    {
        /// <summary>"stub" (harness self-test only, never a result) or "unified".</summary>
        string Kind { get; }
        long Tick { get; }
        int TicksPerDay { get; }
        int NeedMask { get; }
        int PainBand { get; }
        /// <summary>The persons present at tick 0, ascending id (dead ones stay listed).</summary>
        IReadOnlyList<int> People { get; }
        bool Alive(int person);
        bool AdultAtStart(int person);
        /// <summary>Alive, awake and not busy: a decision point (§D9.2 a).</summary>
        bool ReadyToDecide(int person);
        void Observe(int person, ref Percept p, short[] obs, ref ActMask mask);
        PrimitiveControl Decode(int person, in ActionChoice a);
        MotorStatus Submit(in PrimitiveControl c, ControlOrigin origin);
        void Step();
        void ReadBody(int person, BodyReading into);
        BrainState BrainOf(int person);
        /// <summary>Distinct cells visited by the person (S1 criterion).</summary>
        int CellsVisited(int person);
        WorldFacts Facts { get; }
        ulong StateHash();
    }
}
