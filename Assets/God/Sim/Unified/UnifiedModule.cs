using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.1, 23.3, 23.14): the unified module's entry points, called by <c>GodSim</c>. A legacy game never reaches
    /// any of them except <see cref="Loaded"/>, which only widens the ledgers of a save written before UN0 (no state of a current
    /// game changes). PARTIAL: each package's start and install slot, called in the fixed order below.
    /// </summary>
    public static partial class UnifiedModule
    {
        /// <summary>
        /// The order of <see cref="UnifiedSystem"/>: right after the population (250), before miracles (252). (The UN0 spec's "before
        /// DivinitySystem" cannot hold: divinity runs at 230, before the population; recorded as amendment row A6.)
        /// </summary>
        public const int SystemOrder = 251;

        /// <summary>The empty unified state (from <c>GodSim.Create</c>, before the population starts, so founder goods get origins).</summary>
        public static UnifiedState NewState(GodState s) => new UnifiedState();

        /// <summary>
        /// Before the first tick, after every legacy module started: a unified record for every person, then the package slots in
        /// order: world (UN1), buildings (UN3), construction (UN4), start (UN5), brain (UN6).
        /// </summary>
        public static void StartGame(GodState s)
        {
            var u = s.Unification;
            if (u == null || s.Population == null) return;
            foreach (var n in s.Population.Npcs) if (Bodies.IsPerson(n)) u.PersonOrNew(n.Id);
            StartUN1(s); StartUN3(s); StartUN4(s); StartUN5(s); StartUN6(s); StartUB(s);
        }

        /// <summary>Registers the unified systems and the packages' handlers (new game and loaded save).</summary>
        public static void Install(GodSim sim)
        {
            if (sim.State.Unification == null) return;
            sim.AddSystem(SystemOrder, new UnifiedSystem());
            InstallUN1(sim); InstallUN2(sim); InstallUN3(sim); InstallUN4(sim); InstallUN6(sim); InstallUN7(sim); InstallUN8(sim); InstallUN9(sim); InstallUB(sim);
        }

        /// <summary>
        /// After a load, before the systems are installed (<c>GodSim.FromState</c>): ledgers saved at the F1 width (8 reasons x 40
        /// wares; the miracle ledger at 40 wares per god) are widened in place, every count kept at its ware and reason; a tuning
        /// that asks for a unified game must come with its state.
        /// </summary>
        public static void Loaded(GodState s)
        {
            if (Unified.Requested(s.Tuning) && s.Unification == null) throw new System.InvalidOperationException("A unified game (Unification.Enabled = 1) without its unified state.");
            var l = s.Population?.Ledger;
            if (l != null)
            {
                l.Produced = Widen(l.Produced, GoodsLedger.ProduceReasons, Wares.PersonalCount);
                l.Consumed = Widen(l.Consumed, GoodsLedger.ConsumeReasons, Wares.PersonalCount);
            }
            var m = s.Divinity?.Miracles?.Ledger;
            int gods = s.FactionCount + 1;
            if (m != null && m.Created != null && m.Created.Length == gods * Wares.F1PersonalCount)
            {
                int rows = gods;
                m.Created = Widen(m.Created, rows, Wares.PersonalCount);
                m.Destroyed = Widen(m.Destroyed, rows, Wares.PersonalCount);
            }
        }

        /// <summary>A row-major array re-laid at <paramref name="width"/> columns; an array already that size is returned as it is.</summary>
        private static long[] Widen(long[] a, int rows, int width)
        {
            if (a == null) return new long[rows * width];
            if (a.Length == rows * width) return a;
            int oldWidth = a.Length % Wares.F1PersonalCount == 0 && a.Length / Wares.F1PersonalCount <= rows ? Wares.F1PersonalCount : 0;
            if (oldWidth == 0) throw new System.InvalidOperationException("ledger of unexpected size " + a.Length);
            var b = new long[rows * width];
            int oldRows = a.Length / oldWidth;
            for (int r = 0; r < oldRows; r++) for (int w = 0; w < oldWidth; w++) b[r * width + w] = a[r * oldWidth + w];
            return b;
        }

        // ---- package slots (each implemented in the package's own file UnifiedModule.UNn.cs) ----
        static partial void StartUN1(GodState s);
        static partial void StartUN3(GodState s);
        static partial void StartUN4(GodState s);
        static partial void StartUN5(GodState s);
        static partial void StartUN6(GodState s);
        static partial void StartUB(GodState s);
        static partial void InstallUN1(GodSim sim);
        static partial void InstallUN2(GodSim sim);
        static partial void InstallUN3(GodSim sim);
        static partial void InstallUN4(GodSim sim);
        static partial void InstallUN6(GodSim sim);
        static partial void InstallUN7(GodSim sim);
        static partial void InstallUN8(GodSim sim);
        static partial void InstallUN9(GodSim sim);
        static partial void InstallUB(GodSim sim);
    }
}
