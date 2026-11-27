namespace God.Sim.Divinity
{
    /// <summary>
    /// D2's numbers, <c>Divinity.Instruction</c> (contracts 10.3, 11.3; DIVINE_PLAYER 4.2-4.5). Fields only in this file
    /// (contracts 20.F1.1: the JSON field order is this file's). The costs are D1's (<c>Divinity.Mythic.Costs</c>), the
    /// compulsion upkeep D1's (<c>Divinity.Mythic.Upkeep.CompulsionPerTick</c>). OWNER: D2.
    /// </summary>
    public sealed partial class InstructionTuning
    {
        /// <summary>Recipients an instruction may reach; a scope resolving to more is refused <c>bad_target</c>.</summary>
        public int MaxRecipients = 32;
        /// <summary>1: an asleep recipient perceives the instruction on waking (a dream or vision); 0: it is not reached.</summary>
        public int WakeDelivery = 1;
        /// <summary>Chance (per mille) that an awake recipient perceives a suggestion / a command (draw <c>divine-perceive</c>).</summary>
        public int PerceiveSuggestion = 800, PerceiveCommand = 1000;
        /// <summary>The decode-noise chance (per mille) of a suggestion, on top of M5's (none for a divine message by default).</summary>
        public int SuggestionNoisePermille = 100;
        /// <summary>The decode-noise chance (per mille) of a command (contracts: clarity 255, no noise).</summary>
        public int CommandNoisePermille;
        /// <summary>The authority weight (vp at full belief and full trust or fear) of a suggestion and of a command (4.3.1).</summary>
        public int AuthoritySuggestion = 1500, AuthorityCommand = 3000;
        /// <summary>
        /// The belief and trust a mind without a <c>GodBelief</c> for the sending god gives it (D6 replaces them by the mind's
        /// own beliefs; until D6 lands every mind uses these). Fear defaults to 0.
        /// </summary>
        public int DefaultBelief = 500, DefaultTrust = 500;
        /// <summary>
        /// The innate prior (vp) for a candidate a command names (the configured <c>Commanded</c> instinct prior, default 0).
        /// Added inside the D2 divine term (the instinct table is static and cannot read the game's tuning).
        /// </summary>
        public int CommandedPrior;
        /// <summary>Default and maximum expiry of an instruction (ticks: 1 day, 10 days).</summary>
        public int DefaultExpireTicks = 1200, MaxExpireTicks = 12000;
        /// <summary>Default expiry of a compulsion (ticks), used when the command gives 0.</summary>
        public int CompulsionTimeoutTicks = 600;
        /// <summary>Rewards earned under compulsion scale by this (per mille; 1000: the NPC learns from what its body did).</summary>
        public int CompelLearnPermille = 1000;
        /// <summary>Days the mind remembers having been compelled (<c>SelfFeature.Compelled</c>).</summary>
        public int MemoryDays = 2;
        /// <summary>1: witnesses within gesture range of a command's recipient who attend it perceive a divine sign (819).</summary>
        public int Witnesses = 1;
        /// <summary>Recipients an organisation-scope instruction is relayed to (the faction stand-in until C6).</summary>
        public int MaxRelay = 8;
    }
}
