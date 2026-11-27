namespace God.Sim.Information
{
    /// <summary>
    /// F1.18 (contracts 0.1): procedural names. Pure functions of their arguments: no state is read, nothing is hashed or
    /// saved, the same arguments always give the same string. Player aliases, favourites and follows live in the non-hashed
    /// <c>ui</c> save extra (U1), never here. Original syllable tables. OWNER: F1; the tables pass to U3 after F1 merges
    /// (LEAD_RULINGS R2).
    /// </summary>
    public static class Names
    {
        private static readonly ReadOnlyArray<string> PersonA = new ReadOnlyArray<string>( "Ka", "Mo", "Te", "Ri", "Sa", "Lu", "Ve", "No", "Ha", "Ti", "Bo", "Ne", "Ya", "Do", "Fi", "Gu");
        private static readonly ReadOnlyArray<string> PersonB = new ReadOnlyArray<string>( "ran", "li", "mek", "sa", "tor", "vi", "dun", "ela", "ko", "rin", "pa", "sel", "mu", "ta", "wen", "zo");
        private static readonly ReadOnlyArray<string> GodA = new ReadOnlyArray<string>( "Aun", "Vel", "Orr", "Ishe", "Tamu", "Ked", "Ylo", "Sarn");
        private static readonly ReadOnlyArray<string> GodB = new ReadOnlyArray<string>( "ath", "orun", "eli", "um", "aru", "esh", "ion", "aven");
        private static readonly ReadOnlyArray<string> OrgPlace = new ReadOnlyArray<string>( "River", "Stone", "Reed", "Ash", "Hill", "Shell", "Oak", "Salt");
        private static readonly ReadOnlyArray<string> OrgKind = new ReadOnlyArray<string>( "Fellowship", "Company", "Circle", "Council", "House", "Guild", "Hall", "Lodge");

        /// <summary>A person's name, from the game seed and the NpcId.</summary>
        public static string Person(ulong seed, int npcId)
        {
            ulong h = Mix(seed, (ulong)(uint)npcId, 0x50455253UL);
            return PersonA[(int)(h & 15)] + PersonB[(int)((h >> 4) & 15)] + (((h >> 8) & 3) == 0 ? PersonB[(int)((h >> 10) & 15)] : "");
        }

        /// <summary>A god's name, from the game seed and the GodId.</summary>
        public static string God(ulong seed, int godId)
        {
            ulong h = Mix(seed, (ulong)(uint)godId, 0x474F4453UL);
            return GodA[(int)(h & 7)] + GodB[(int)((h >> 3) & 7)];
        }

        /// <summary>An organisation's name, from the game seed, the OrgId and its kind (C6's kind code).</summary>
        public static string Org(ulong seed, int orgId, int kind)
        {
            ulong h = Mix(seed, (ulong)(uint)orgId, 0x4F524753UL ^ (ulong)(uint)kind);
            return OrgPlace[(int)(h & 7)] + " " + OrgKind[(int)((h >> 3) & 7)];
        }

        /// <summary>SplitMix64 over the three words.</summary>
        private static ulong Mix(ulong a, ulong b, ulong c)
        {
            ulong z = a ^ (b * 0x9E3779B97F4A7C15UL) ^ (c * 0xBF58476D1CE4E5B9UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
