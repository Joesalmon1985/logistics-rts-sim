namespace God.Sim.Unified
{
    /// <summary>
    /// UN6's part of a person's <see cref="BrainState"/> (contracts 23.6, spec 5.4): the decision bookkeeping the runtime needs between
    /// decisions (need pressures at the last decision for the overlay's credit, what was last submitted so its outcome can be judged,
    /// the teaching window, the planner's intention). Hashed in <see cref="AddToUN6"/>; counted in <see cref="Bytes"/>. A child's state
    /// is built only by <see cref="Innate"/>, never from another person's (R83).
    /// </summary>
    public sealed partial class BrainState
    {
        /// <summary>True once the runtime initialised this state (innate knowledge set, first need snapshot taken).</summary>
        public bool Started;
        public long LastDecisionTick = -1;
        public int Decisions;
        /// <summary>The 12 need pressures (encoder order) at the last decision.</summary>
        public short[] PrevNeeds = new short[NeedChannels.Count];
        /// <summary>The last submitted control, its status at submission, the target's class and the interaction kind it named (USE).</summary>
        public byte PrevControl, PrevStatus, PrevInteraction;
        public ushort PrevTargetClass;
        /// <summary>Last tick of the teaching window opened by a Teach or Demonstrate gesture (-1 none).</summary>
        public long TeachUntil = -1;
        /// <summary>The planner's intention (only with Brain.Planning = 1): a target slot (-1 none) and a need channel.</summary>
        public sbyte IntentSlot = -1;
        public byte IntentNeed;

        partial void AddToUN6(ref StateHasher h)
        {
            h.Add(Started ? 1 : 0); h.Add(LastDecisionTick); h.Add(Decisions);
            foreach (var x in PrevNeeds) h.Add(x);
            h.Add(PrevControl | (PrevStatus << 8) | (PrevInteraction << 16)); h.Add(PrevTargetClass);
            h.Add(TeachUntil); h.Add(IntentSlot | (IntentNeed << 8));
        }

        /// <summary>
        /// The bytes of this state's fixed content (the budget figure, §D9.2 a: at most <see cref="MaxBytes"/>): recurrent state,
        /// memory, claims, knowledge bitsets, skills, overlay id and UN6's bookkeeping. Every array is fixed-length.
        /// </summary>
        public int Bytes =>
            Recurrent.Length * 2 + Places.Length * (4 + 2 + 8) + People.Length * (4 + 2 + 8) + Claims.Length * (4 + 1 + 4 + 1 + 2 + 8 + 8) + 1
            + (Designs.Length + Recipes.Length + Interactions.Length) * 8 + Skill.Length * 2 + 4
            + 1 + 8 + 4 + PrevNeeds.Length * 2 + 3 + 2 + 8 + 2;

        /// <summary>
        /// The innate start state of a newborn, a child or a founder before the start package adds anything (R83): zero recurrent
        /// state, empty memory and claims, no designs or recipes, the innate interaction kinds only, zero skill, no overlay entries.
        /// </summary>
        public static BrainState Innate(int innateInteractions)
        {
            var b = new BrainState();
            for (int k = 0; k < 32; k++) if ((innateInteractions & (1 << k)) != 0) Knowledge.Learn(b, KnowledgeKind.Interaction, k);
            b.Started = true;
            return b;
        }

        /// <summary>The hash of this state alone (tests compare states by it).</summary>
        public ulong Hash()
        {
            var h = new StateHasher();
            AddTo(ref h);
            return h.Value;
        }
    }
}
