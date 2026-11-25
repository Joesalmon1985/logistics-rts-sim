using System;
using System.IO;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// The save slots on disk: <c>Application.persistentDataPath/saves/slotN.json</c> (N = 1 to 3). Writes go to a
    /// temporary file first, so a crash never leaves half a save. The folder can be moved (tests).
    /// </summary>
    public static class SaveStore
    {
        public const int Slots = 3;

        private static string _root;

        /// <summary>The folder the slots live in (created on first write).</summary>
        public static string Root
        {
            get => _root ?? Path.Combine(Application.persistentDataPath, "saves");
            set => _root = value;
        }

        public static string PathOf(int slot)
        {
            if (slot < 1 || slot > Slots) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(Root, "slot" + slot + ".json");
        }

        public static bool Exists(int slot) => File.Exists(PathOf(slot));

        public static void Write(int slot, SaveFile file)
        {
            string path = PathOf(slot);
            Directory.CreateDirectory(Root);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, file.ToJson());
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        /// <summary>The header of a slot, or null when the slot is empty or its file is not a save.</summary>
        public static SaveHeader ReadHeader(int slot)
        {
            try { return Exists(slot) ? SaveFile.TryReadHeader(File.ReadAllText(PathOf(slot))) : null; }
            catch (IOException) { return null; }
        }

        /// <summary>Reads a slot. Throws <see cref="SaveException"/> when it is empty, unreadable or damaged.</summary>
        public static SaveFile Read(int slot)
        {
            string text;
            try { text = File.ReadAllText(PathOf(slot)); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { throw new SaveException("That slot is empty.", e); }
            return SaveFile.FromJson(text);
        }

        /// <summary>One line for a slot button: who plays, the seed and how far the game is.</summary>
        public static string Describe(SaveHeader h)
        {
            string who = h.Human == GameSetup.NoHuman ? "Watching" : "Playing " + FactionInfo.Name((Faction)h.Human);
            int secs = (int)(h.Tick / GodSim.TicksPerSecond);
            string when = "";
            if (DateTime.TryParse(h.SavedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                when = "   " + dt.ToLocalTime().ToString("d MMM HH:mm");
            return who + "   seed " + h.Seed + "   " + secs / 60 + ":" + (secs % 60).ToString("00") + when + (h.GameOver ? "   (finished)" : "");
        }
    }
}
