namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.1): <c>SimTuning.Unification</c>, the unified switch. <see cref="Enabled"/> 0 (every existing scenario):
    /// no <c>GodState.Unification</c>, nothing of this namespace runs, the block is omitted from the JSON (so every older game's
    /// tuning, core part and hash are unchanged). <see cref="Enabled"/> 1: the unified game; goods have no title (23.4) and the
    /// legacy decision and economy paths are off (23.3). <b>No sub-switch disables those two consequences.</b>
    /// <para>PARTIAL per package (R85): each package's numbers live in its own block (<see cref="UN1"/> ...), declared in the
    /// package's own file <c>UnifiedTuning.UNn.cs</c>, where it adds its fields. <b>No file other than this one adds fields to the root</b>:
    /// the root's field order is the JSON order the core hash reads, and fields spread over several files of one class would take the
    /// compiler's file order, which may differ between Unity and <c>dotnet</c>. One block = one file keeps every order fixed.</para>
    /// </summary>
    public sealed partial class UnifiedTuning : IOmitWhenDefault
    {
        /// <summary>1 = the unified game (contracts 23.1).</summary>
        public int Enabled;
        /// <summary>Behaviour sub-switches, effective only with <see cref="Enabled"/> 1: 0 makes their effect kinds refuse with <c>Disabled</c> (isolated tests).</summary>
        public int World = 1, Interactions = 1, Construction = 1, Exchange = 1, Conflict = 1;
        /// <summary>UB sub-switches (0 until UB).</summary>
        public int Settlements, Roads, Groups;
        /// <summary>The twelve need channels (23.6).</summary>
        public UnifiedNeedsTuning Needs = new UnifiedNeedsTuning();
        /// <summary>The brain (23.1, §D9.2).</summary>
        public UnifiedBrainTuning Brain = new UnifiedBrainTuning();
        /// <summary>Board scale (§D9.6, D10): never constants in code.</summary>
        public UnifiedBoardTuning Board = new UnifiedBoardTuning();
        /// <summary>Start scale (§D9.6, D10): never constants in code.</summary>
        public UnifiedStartTuning Start = new UnifiedStartTuning();
        // ---- per-package numbers (each block's fields in that package's own file) ----
        public UN1Tuning UN1 = new UN1Tuning();
        public UN2Tuning UN2 = new UN2Tuning();
        public UN3Tuning UN3 = new UN3Tuning();
        public UN4Tuning UN4 = new UN4Tuning();
        public UN5Tuning UN5 = new UN5Tuning();
        public UN6Tuning UN6 = new UN6Tuning();
        public UN7Tuning UN7 = new UN7Tuning();
        public UN8Tuning UN8 = new UN8Tuning();
        public UN9Tuning UN9 = new UN9Tuning();
        public UBTuning UB = new UBTuning();
    }

    /// <summary><c>Unification.Needs</c>: bit i = need channel i in the order of <see cref="NeedChannel"/> (default all 12).</summary>
    public sealed class UnifiedNeedsTuning : IOmitWhenDefault
    {
        public int ActiveMask = 0xFFF;
    }

    /// <summary>
    /// <c>Unification.Brain</c>: <see cref="Kind"/> 0 Untrained, 1 Trained, 2 Fallback (always labelled); the teacher exists only in
    /// <c>DotNet/God.Train</c> and test fixtures. <see cref="Artifact"/>: the artifact version (with Kind 1 the sim refuses to start on a
    /// hash mismatch: UN6). <see cref="Planning"/>: bounded internal planning (§D9.2 e), off.
    /// </summary>
    public sealed class UnifiedBrainTuning : IOmitWhenDefault
    {
        public int Kind, Artifact, Planning;
    }

    /// <summary><c>Unification.Board</c>: land radius 3 plus a sea ring (radius 4 in all), one sea ring.</summary>
    public sealed class UnifiedBoardTuning : IOmitWhenDefault
    {
        public int Radius = 4, SeaRing = 1;
    }

    /// <summary><c>Unification.Start</c>: six tribes of 7 adults and 5 children, one settlement each.</summary>
    public sealed class UnifiedStartTuning : IOmitWhenDefault
    {
        public int Tribes = 6, Adults = 7, Children = 5, SettlementsPerTribe = 1;
    }

    /// <summary>The brain kinds of <c>Unification.Brain.Kind</c>.</summary>
    public static class BrainKind
    {
        public const int Untrained = 0, Trained = 1, Fallback = 2;
    }
}
