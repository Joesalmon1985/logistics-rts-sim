namespace God.Sim.Estate
{
    /// <summary>
    /// <c>SimTuning.Estate</c> (F0.12): M4's estate numbers (MATERIAL_INHERITANCE 4). Written only when changed. OWNER: M4.
    /// The executor ladder is configurable data (Q13): <see cref="Ladder"/> lists rung codes as decimal digits, read left to
    /// right (1 named in a will, 2 partner, 3 eldest adult child, 4 eldest household adult, 5 appointed), default 12345; a
    /// rung that is missing is skipped, and no rung left means no executor (every asset ownerless).
    /// </summary>
    public sealed class EstateTuning : IOmitWhenDefault
    {
        /// <summary>No one takes an estate's goods for this long after the death (MATERIAL 4.1 "MournTicks"; not scaled by LifeScale).</summary>
        public int MournTicks = 600;
        /// <summary>Every estate settles within this (2 days at 1200 ticks a day).</summary>
        public int SettleWithinTicks = 2400;
        public int Ladder = 12345;
        /// <summary>
        /// The custom applied when a faction sets none (<see cref="Custom"/> code). Before M11 customs are a fixed per-faction
        /// default (the <c>StaticCultureProvider</c> of MATERIAL 4.3); no custom is preferred by the sim, so scenarios choose:
        /// <see cref="CustomF0"/>..<see cref="CustomF5"/> override it per faction (-1 = the default).
        /// </summary>
        public int DefaultCustom = (int)Custom.EqualChildren;
        public int CustomF0 = -1, CustomF1 = -1, CustomF2 = -1, CustomF3 = -1, CustomF4 = -1, CustomF5 = -1;
        /// <summary>The share of each stackable lot BuriedWithDead destroys (permille, rounded down).</summary>
        public int BuriedPermille = 500;
        /// <summary>Acknowledged kin: the deceased's believed kin above this confidence.</summary>
        public int AcknowledgeConfidence = 400;
        /// <summary>A dispute is raised for an heir whose own custom would give it at least this much more (permille).</summary>
        public int DisputeFromPermille = 250;
        /// <summary>1: lots an heir did not collect by the deadline go to its household store (WORLD_ONTOLOGY O4, a temporary cross-node move); 0: they go ownerless.</summary>
        public int Delivery = 1;

        public Custom CustomOf(Faction f)
        {
            int v;
            switch ((int)f)
            {
                case 0: v = CustomF0; break; case 1: v = CustomF1; break; case 2: v = CustomF2; break;
                case 3: v = CustomF3; break; case 4: v = CustomF4; break; case 5: v = CustomF5; break;
                default: v = -1; break;
            }
            return (Custom)(v < 0 ? DefaultCustom : v);
        }
    }
}
