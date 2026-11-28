namespace God.Sim.Unified
{
    /// <summary>
    /// <c>Unification.UN6</c>: OWNER UN6 (brain runtime). Fields only in this file; written only when changed. Every number is a lead
    /// default (L), recorded in the UN6 record of <c>docs/stage4/DEV_LOG.md</c>. The brain kind, artifact version and planning switch
    /// are UN0's <c>Unification.Brain</c> block.
    /// </summary>
    public sealed partial class UN6Tuning : IOmitWhenDefault
    {
        // ---- inference ----
        /// <summary>The untrained network's weight seed (Brain.Kind = 0).</summary>
        public int UntrainedSeed = 1;
        /// <summary>Sampling temperature spread (permille) from temperament: T = 1000 + (Boldness - 500) x spread / 500. 0 = the trainer's T = 1000.</summary>
        public int TemperatureSpread = 300;
        /// <summary>1 = sample the action (keyed policy stream); 0 = greedy.</summary>
        public int Sample = 1;
        /// <summary>A person's first decision waits until tick (NpcId mod Stagger) (sim state only, never the camera); 1 = no stagger.</summary>
        public int Stagger = 1;
        /// <summary>1 = the brain decides for every person (the game); 0 = no brain decisions at all (motor fixtures that drive bodies by hand).</summary>
        public int Decide = 1;

        // ---- associative overlay (§D9.2 d) ----
        /// <summary>1 = the overlay learns and biases the control head; 0 = off (no store, no bias).</summary>
        public int Overlay = 1;
        /// <summary>Entries, eligibility traces per person (bounded store).</summary>
        public int OverlayEntries = 64, OverlayTraces = 8;
        /// <summary>Bias = clamp(sum of fast weights x Gain / 1024, +-Cap) in milli-nats on the control logits.</summary>
        public int OverlayGain = 1024, OverlayBiasCap = 1000;
        /// <summary>Need-pressure credit: comfort level below which a falling pressure pays nothing (permille, as UN6t's returns).</summary>
        public int CreditComfort = 250;
        /// <summary>The overlay's TD discount per decision (permille): delayed reliefs reach the decisions before them (SARSA(lambda) over the traces).</summary>
        public int OverlayGamma = 900;

        // ---- knowledge (§D9.2 d, R83) ----
        /// <summary>Innate interaction kinds (bit = <see cref="EffectKind"/>): Forage, DrinkWash, Eat (the "eat, drink" of R83).</summary>
        public int InnateInteractions = (1 << (int)EffectKind.Forage) | (1 << (int)EffectKind.DrinkWash) | (1 << (int)EffectKind.Eat);
        /// <summary>Observation: base chance (permille) at distance 0 for an average-attention observer; range in lattice steps.</summary>
        public int ObserveChance = 300, ObserveRange = 6;
        /// <summary>A Teach or Demonstrate gesture within <see cref="TeachWindow"/> ticks before a completion multiplies the chance (permille).</summary>
        public int TeachBoost = 2500, TeachWindow = 600;
        /// <summary>The imitation bias a taught observer's overlay receives on (target class, USE) (fast weight points).</summary>
        public int ImitationBias = 200;
        /// <summary>Practice gained per own success (skill 0-1000).</summary>
        public int SkillGain = 10;

        // ---- bounded planning (§D9.2 e; only with Brain.Planning = 1) ----
        public int PlanDepth = 4, PlanExpansions = 64;
    }
}
