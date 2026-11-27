namespace God.Game
{
    /// <summary>
    /// View-only switches (M5.18, D5.18 to D5.20, UI-8). None of them enters the sim, the save or the state hash.
    /// All default off in the player. <see cref="DecodedMessages"/> and <see cref="GridOverlay"/> are developer switches;
    /// <see cref="Captions"/> is the accessibility option (plain words from what a watcher could have seen).
    /// Keys (GodHud; F6 and F9 only in developer games): F6 decode overlay, F7 captions, F9 grid overlay.
    /// Session-only for now (not persisted to local preferences).
    /// </summary>
    public static class DevOverlays
    {
        /// <summary>Developer: the decoded message text and the line to the addressee above a gesturing person.</summary>
        public static bool DecodedMessages;
        /// <summary>Accessibility: a short plain caption of the visible gesture, no truth flags.</summary>
        public static bool Captions;
        /// <summary>Developer: the node lattice and build circles (TerritoryView debug) and the producing-hex glow.</summary>
        public static bool GridOverlay;

        /// <summary>Back to the player defaults (tests).</summary>
        public static void Reset() { DecodedMessages = false; Captions = false; GridOverlay = false; }
    }
}
