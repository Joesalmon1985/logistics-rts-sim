namespace God.Sim
{
    /// <summary>
    /// Every ware. The first four share their numbers with <see cref="RawKind"/> (what a pile holds). Rock is a raw
    /// pile kind that is never hauled: the quarry turns it into stone and the mine into ore on the spot.
    /// </summary>
    public enum Ware
    {
        None = 0,
        Log = 1,
        Clay = 2,
        Grain = 3,
        Rock = 4,
        Stone = 5,
        Ore = 6,
        Plank = 7,
        Kindling = 8,
        Brick = 9,
        Iron = 10,
    }

    /// <summary>How urgently a consumer wants one input ware. Stop means it asks for none.</summary>
    public enum Priority
    {
        Stop = 0,
        Low = 1,
        Normal = 2,
        High = 3,
    }

    public static class Wares
    {
        public const int Count = 11;

        public static Ware OfRaw(RawKind k) => (Ware)(int)k;

        public static string Name(Ware w)
        {
            switch (w)
            {
                case Ware.Log: return "log";
                case Ware.Clay: return "clay";
                case Ware.Grain: return "grain";
                case Ware.Rock: return "rock";
                case Ware.Stone: return "stone";
                case Ware.Ore: return "ore";
                case Ware.Plank: return "plank";
                case Ware.Kindling: return "kindling";
                case Ware.Brick: return "brick";
                case Ware.Iron: return "iron";
                default: return "none";
            }
        }
    }
}
