namespace God.Sim.Unified
{
    /// <summary>A remembered place: node, what was there (catalogue class), when.</summary>
    public struct MemPlace
    {
        public int Node;
        public ushort Class;
        public long Tick;
        public void AddTo(ref StateHasher h) { h.Add(Node); h.Add(Class); h.Add(Tick); }
    }

    /// <summary>A remembered person: who, trust band (-1000..1000), when last seen.</summary>
    public struct MemPerson
    {
        public int Npc;
        public short Trust;
        public long Tick;
        public void AddTo(ref StateHasher h) { h.Add(Npc); h.Add(Trust); h.Add(Tick); }
    }

    /// <summary>Where a claim came from: own judgement, observation, or being told.</summary>
    public enum ClaimSource : byte { Own = 0, Observed = 1, Told = 2 }

    /// <summary>
    /// UN0 (contracts 23.9): a claim is a brain belief, never world state: claimant, subject (a <see cref="TargetKind"/> and id), source,
    /// confidence (permille), time and expiry. Contradictory claims coexist. Written by UN7 through UN6's <c>Brain.AddClaim</c>.
    /// </summary>
    public struct Claim
    {
        public int Claimant;
        public TargetKind SubjectKind;
        public int SubjectId;
        public ClaimSource Source;
        public short Confidence;
        public long Tick, Expiry;
        public void AddTo(ref StateHasher h) { h.Add(Claimant); h.Add((int)SubjectKind | ((int)Source << 8) | (Confidence << 16)); h.Add(SubjectId); h.Add(Tick); h.Add(Expiry); }
    }

    /// <summary>
    /// UN0 (contracts 23.6, UN0 spec 5.2): one person's brain state, the shape UN6 fills: the recurrent state (64 x int16), memory
    /// slots (8 places, 4 people, a ring of 8 claims), knowledge bitsets over catalogue indices (designs, recipes, interactions),
    /// skills and the run-time overlay store id. Bounded (<see cref="MaxBytes"/>); hashed with the person; brain weights are never
    /// here (the artifact holds them). Children never copy an adult's (R83). PARTIAL: UN6 adds behaviour (and, if it must, fields
    /// hashed in <see cref="AddToUN6"/>).
    /// </summary>
    public sealed partial class BrainState
    {
        public const int MaxBytes = 4096, RecurrentSize = 64, PlaceSlots = 8, PeopleSlots = 4, ClaimSlots = 8, Skills = 8;
        public const int DesignBits = 128, RecipeBits = 128, InteractionBits = 256;

        public short[] Recurrent = new short[RecurrentSize];
        public MemPlace[] Places = new MemPlace[PlaceSlots];
        public MemPerson[] People = new MemPerson[PeopleSlots];
        public Claim[] Claims = new Claim[ClaimSlots];
        /// <summary>The next ring slot of <see cref="Claims"/>.</summary>
        public byte ClaimHead;
        public ulong[] Designs = new ulong[DesignBits / 64];
        public ulong[] Recipes = new ulong[RecipeBits / 64];
        public ulong[] Interactions = new ulong[InteractionBits / 64];
        public short[] Skill = new short[Skills];
        /// <summary>The per-person associative overlay store (0 none).</summary>
        public int Overlay;

        public void AddTo(ref StateHasher h)
        {
            foreach (var x in Recurrent) h.Add(x);
            foreach (var x in Places) x.AddTo(ref h);
            foreach (var x in People) x.AddTo(ref h);
            foreach (var x in Claims) x.AddTo(ref h);
            h.Add(ClaimHead);
            foreach (var x in Designs) h.Add((long)x);
            foreach (var x in Recipes) h.Add((long)x);
            foreach (var x in Interactions) h.Add((long)x);
            foreach (var x in Skill) h.Add(x);
            h.Add(Overlay);
            AddToUN6(ref h);
        }

        partial void AddToUN6(ref StateHasher h);
    }

    /// <summary>What a knowledge bit names.</summary>
    public enum KnowledgeKind : byte { Design = 0, Recipe = 1, Interaction = 2 }

    /// <summary>
    /// UN0: the knowledge accessors over <see cref="BrainState"/>'s bitsets. A design's index is its BLD row (<see cref="CatId.Row"/>),
    /// a recipe's and an interaction's the catalogue index UN3a assigns. Not knowing a design is a legality fact
    /// (<see cref="MotorStatus.NotKnown"/>), never a permission.
    /// </summary>
    public static class Knowledge
    {
        public static bool Knows(BrainState b, KnowledgeKind k, int index)
        {
            var a = Bits(b, k);
            return index >= 0 && index < a.Length * 64 && (a[index >> 6] & (1UL << (index & 63))) != 0;
        }

        /// <summary>Sets the bit; false when the index is out of range (nothing changes).</summary>
        public static bool Learn(BrainState b, KnowledgeKind k, int index)
        {
            var a = Bits(b, k);
            if (index < 0 || index >= a.Length * 64) return false;
            a[index >> 6] |= 1UL << (index & 63);
            return true;
        }

        public static bool KnowsDesign(BrainState b, ushort design) => CatId.IsBuilding(design) && Knows(b, KnowledgeKind.Design, CatId.Row(design));

        private static ulong[] Bits(BrainState b, KnowledgeKind k) => k == KnowledgeKind.Design ? b.Designs : k == KnowledgeKind.Recipe ? b.Recipes : b.Interactions;
    }
}
