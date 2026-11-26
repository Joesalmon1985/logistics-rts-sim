using System;

namespace God.Sim.Learning
{
    /// <summary>
    /// Episode keys (SHARED_CONTRACTS 1): <c>Make(kind, id) = (kind &lt;&lt; 24) | id</c>, id below 2^24. Kind 0 is M3's hut (the raw hut
    /// id); 1-7 M4 (1 partnership, 2 care arrangement, 3 child); 8-15 M5 (8 deal, 9 reciprocity with a person, 10 message);
    /// <see cref="Lot"/> = 64 is M3b's lot episode (its existing key <c>1 &lt;&lt; 30 | lotId</c>, see 12.15); the rest later.
    /// </summary>
    public static class EpisodeKey
    {
        public const int Hut = 0, Partnership = 1, CareArrangement = 2, Child = 3, Deal = 8, Reciprocity = 9, Message = 10, Lot = 64;
        public const int MaxId = (1 << 24) - 1;

        public static int Make(int kind, int id)
        {
            if (id < 0 || id > MaxId) throw new ArgumentOutOfRangeException(nameof(id), "episode ids are below 2^24");
            if (kind < 0 || kind > 127) throw new ArgumentOutOfRangeException(nameof(kind));
            return (kind << 24) | id;
        }

        public static int KindOf(int key) => key >> 24;
        public static int IdOf(int key) => key & MaxId;
    }
}
