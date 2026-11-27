using System;

namespace God.Game
{
    /// <summary>
    /// How a body shows its state (VISUAL_WORLD 10; U3-T9): pure functions of sim numbers, so the view and the tests agree on
    /// them. Nothing here reads the sim; the view passes the numbers in.
    /// </summary>
    public static class CharacterLook
    {
        /// <summary>Injury (sim units, x1000 of a permille) from which an arm is in a sling.</summary>
        public const int SlingFrom = 300000;
        /// <summary>Fatigue drive permille from which a person droops.</summary>
        public const int TiredFrom = 700;

        public static float Progress(long tick, long start, long due)
        {
            if (due <= start) return 0f;
            float p = (tick - start) / (float)(due - start);
            return p < 0f ? 0f : p > 1f ? 1f : p;
        }

        /// <summary>The belly part's uniform scale for a pregnancy at a progress 0..1.</summary>
        public static float BellyScale(float progress) => 0.25f + 1.0f * Math.Max(0f, Math.Min(1f, progress));

        /// <summary>The head's scale: a young child has a big head, easing to 1 by adulthood (growth 0..1000).</summary>
        public static float HeadScale(bool child, int growth) => child ? 1.35f - 0.35f * Math.Max(0, Math.Min(1000, growth)) / 1000f : 1f;

        /// <summary>Forward lean in degrees from fatigue and illness.</summary>
        public static float SlumpDegrees(bool ill, int fatiguePermille)
        {
            float d = ill ? 8f : 0f;
            if (fatiguePermille > TiredFrom) d += 12f * Math.Min(1f, (fatiguePermille - TiredFrom) / 300f);
            return d;
        }

        public static bool Bandaged(int injury) => injury > 0;
        public static bool Slung(int injury) => injury >= SlingFrom;
        /// <summary>How much of its swing the injured leg keeps (a limp).</summary>
        public static float LimpKeep(int injury) => injury > 0 ? 0.45f : 1f;
    }
}
