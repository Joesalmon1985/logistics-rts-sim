using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// How each faction looks: its colour (from <see cref="FactionInfo.ColourRgb"/>) and its building style. For now
    /// the two building styles alternate by faction id: even ids build in warm timber and thatch, odd ids in dark stone
    /// and slate (P10 will give each faction its own). View code only.
    /// </summary>
    public static class FactionLook
    {
        private static readonly Color[] Colours = Build();

        private static Color[] Build()
        {
            var c = new Color[FactionInfo.Max];
            for (int i = 0; i < c.Length; i++)
            {
                int rgb = FactionInfo.ColourRgb((Faction)i);
                c[i] = new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
            }
            return c;
        }

        /// <summary>The faction colour (banners, caps, shields, flags).</summary>
        public static Color Colour(Faction f) => (int)f >= 0 && (int)f < Colours.Length ? Colours[(int)f] : Color.gray;

        /// <summary>True for the dark stone and slate style (odd ids), false for timber and thatch (even ids).</summary>
        public static bool Stone(Faction f) => ((int)f & 1) == 1;

        public static string Name(Faction f) => FactionInfo.Name(f);

        /// <summary>The human faction of the game, or null in watch mode.</summary>
        public static Faction? Human(GodState s) => s.Setup != null && s.Setup.HasHuman ? s.Setup.HumanFaction : (Faction?)null;

        /// <summary>The faction the view speaks for: the human one, or F0 in watch mode (debug overlays only).</summary>
        public static Faction Viewer(GodState s) => Human(s) ?? Faction.F0;

        /// <summary>True when the faction is the human one.</summary>
        public static bool IsHuman(GodState s, Faction f) => Human(s) == f;
    }
}
