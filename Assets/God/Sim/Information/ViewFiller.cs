using System;

namespace God.Sim.Information
{
    /// <summary>
    /// The single player-side adapter between true state and knowledge (INFORMATION_AND_MEDIA_DESIGN §4.8). It builds a
    /// faction's <see cref="FactionKnowledgeView"/> and, only where the setup allows it, the debug
    /// <see cref="ITrueStateOverlay"/>. Nothing else on the player side (HUD, faction policies) may read
    /// <see cref="GodState"/>; the reflection tests allow it here and in the view types of this namespace.
    /// </summary>
    public static class ViewFiller
    {
        /// <summary>A faction's view of a game. Throws <see cref="NotSupportedException"/> for a <c>Reported</c> domain (later ages).</summary>
        public static FactionKnowledgeView For(GodState s, Faction faction, KnowledgeProfile profile = null)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            profile = profile ?? KnowledgeProfile.FullInformation();
            if (!profile.IsFullInformation)
                throw new NotSupportedException("The Reported knowledge mode needs institutional records (Stage 6-7); stage 4 fills every domain with FullInformation.");
            return new FactionKnowledgeView(s, faction, profile);
        }

        public static FactionKnowledgeView For(GodSim sim, Faction faction, KnowledgeProfile profile = null) =>
            For(sim?.State ?? throw new ArgumentNullException(nameof(sim)), faction, profile);

        /// <summary>
        /// The debug true-state overlay for a viewer, or null when the setup does not allow it (Q22: never in competitive
        /// play). Policies never receive it; it carries no sim state and changes nothing.
        /// </summary>
        public static ITrueStateOverlay Overlay(GodState s, Faction viewer)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            return s.Setup != null && s.Setup.AllowTrueStateOverlay ? new TrueStateOverlay(s, viewer) : null;
        }

        public static ITrueStateOverlay Overlay(GodSim sim, Faction viewer) => Overlay(sim?.State ?? throw new ArgumentNullException(nameof(sim)), viewer);
    }

    /// <summary>
    /// The debug "true state" figures shown beside the view's (Q22): what each faction really holds, including what the
    /// viewer cannot know (other factions' stores). View side only: no <c>IFactionPolicy</c>, <c>IInstitutionPolicy</c>,
    /// <c>IMilitaryPolicy</c> or <c>INpcPolicy</c> receives it (reflection test), it is not saved, and reading it never
    /// changes the sim (test I11).
    /// </summary>
    public interface ITrueStateOverlay
    {
        Faction Viewer { get; }
        long Tick { get; }
        /// <summary>A faction's real stock of a ware over all its HQ stores.</summary>
        int TrueStock(Faction f, Ware w);
        /// <summary>A faction's real stock of every ware together.</summary>
        int TrueStockAll(Faction f);
        int TrueSoldiers(Faction f);
        int TrueBuildings(Faction f);
        /// <summary>Wares a faction's walkers are carrying right now.</summary>
        int TrueWaresInTransit(Faction f);
    }

    internal sealed class TrueStateOverlay : ITrueStateOverlay
    {
        private readonly GodState _s;
        public Faction Viewer { get; }
        public TrueStateOverlay(GodState s, Faction viewer) { _s = s; Viewer = viewer; }

        public long Tick => _s.Tick;
        public int TrueStock(Faction f, Ware w) => _s.Economy.TotalStock(f, w);

        public int TrueStockAll(Faction f)
        {
            int n = 0;
            for (int w = 1; w < Wares.Count; w++) n += _s.Economy.TotalStock(f, (Ware)w);
            return n;
        }

        public int TrueSoldiers(Faction f) { int n = 0; foreach (var u in _s.Military.Units) if (u.Faction == f) n++; return n; }
        public int TrueBuildings(Faction f) { int n = 0; foreach (var b in _s.Economy.Buildings) if (b.Faction == f) n++; return n; }
        public int TrueWaresInTransit(Faction f) { int n = 0; foreach (var w in _s.Economy.Walkers) if (w.Faction == f && w.Loaded) n++; return n; }
    }

    /// <summary>What a game is for (Q22): decides whether the true-state overlay may be shown.</summary>
    public enum ScenarioPurpose
    {
        /// <summary>Normal play against AIs or people with a victory condition: the overlay is never allowed.</summary>
        Competitive = 0,
        /// <summary>A sandbox: allowed only when the scenario or the settings opt in (default off).</summary>
        Sandbox = 1,
        /// <summary>Developer mode, debugging and test runs: allowed (on by default).</summary>
        Developer = 2,
        /// <summary>Lab experiments: allowed.</summary>
        Lab = 3,
        /// <summary>Dedicated observation scenarios (watch mode, no human): allowed.</summary>
        Observation = 4,
    }

    /// <summary>The Q22 rules for the true-state overlay, in one place for the runner, the game and the tests.</summary>
    public static class TrueStateOverlayRules
    {
        /// <summary>
        /// Whether the overlay is allowed. <paramref name="requested"/> is the scenario's or settings' explicit choice (null:
        /// none). Competitive play refuses a request with <see cref="ArgumentException"/>; a sandbox follows the request
        /// (default off); developer, lab and observation scenarios allow it unless asked not to.
        /// </summary>
        public static bool Allowed(ScenarioPurpose purpose, bool? requested = null)
        {
            switch (purpose)
            {
                case ScenarioPurpose.Competitive:
                    if (requested == true) throw new ArgumentException("The true-state overlay is never allowed in competitive play (owner decision Q22).");
                    return false;
                case ScenarioPurpose.Sandbox: return requested ?? false;
                default: return requested ?? true;
            }
        }

        /// <summary>The purpose a setup implies when nothing says otherwise: a sandbox victory list, else watch mode (observation), else competitive.</summary>
        public static ScenarioPurpose DefaultPurpose(GameSetup setup)
        {
            if (setup == null) return ScenarioPurpose.Competitive;
            foreach (var v in setup.Victory) if (v.Kind == VictoryKind.Sandbox) return ScenarioPurpose.Sandbox;
            return setup.HasHuman ? ScenarioPurpose.Competitive : ScenarioPurpose.Observation;
        }

        /// <summary>Sets <see cref="GameSetup.Purpose"/> and <see cref="GameSetup.AllowTrueStateOverlay"/> by the rules.</summary>
        public static GameSetup Apply(GameSetup setup, ScenarioPurpose purpose, bool? requested = null)
        {
            setup.Purpose = purpose;
            setup.AllowTrueStateOverlay = Allowed(purpose, requested);
            return setup;
        }
    }
}
