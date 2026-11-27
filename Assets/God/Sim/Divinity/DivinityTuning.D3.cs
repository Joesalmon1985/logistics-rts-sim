namespace God.Sim.Divinity
{
    /// <summary>
    /// D3's numbers, <c>Divinity.Miracles</c> (contracts 8; MYTHIC 6; LEAD_RULINGS R55: the tuning root is not partial, so every
    /// D3 number lives in this block, and the Mythic costs stay in D1's <c>Divinity.Mythic.Costs</c>). Magnitudes and
    /// permille are on the 0..1000 scale of <c>Npc.Health</c> and <c>Npc.Injury</c> (micro = x1000). Days are sim days
    /// (converted with the calendar). Written only when changed. OWNER: D3.
    /// </summary>
    public sealed partial class MiracleTuning
    {
        /// <summary>SmitePerson: the largest magnitude one smite may have (one smite rarely kills a healthy adult outright).</summary>
        public int SmiteMax = 800;
        /// <summary>SmitePerson: fear drive added to the victim (permille).</summary>
        public int SmiteFear = 400;
        /// <summary>Fear added to every attending person within <see cref="WitnessNodes"/> of a smite (permille); they are never told why.</summary>
        public int WitnessFear = 150;
        /// <summary>Nodes within which a smite is seen and heard (witnesses).</summary>
        public int WitnessNodes = 10;
        /// <summary>HealDisease: health restored on the cure (permille, capped at 1000).</summary>
        public int HealDiseaseHealth = 100;
        /// <summary>Bless and Protect last this many sim days.</summary>
        public int BlessDays = 5, ProtectDays = 5;
        /// <summary>Protect: every injury through <c>Bodies.Damage</c> is multiplied by this (permille).</summary>
        public int ProtectPermille = 300;
        /// <summary>Bless: daily Health recovery (permille) while Nutrition is above 300.</summary>
        public int BlessRecover = 15;
        /// <summary>Bless: the multiplier of the M4 work-hazard and infection chances (permille). Read by the M4 health-roll hook when it exists (see the D3 record).</summary>
        public int BlessRiskPermille = 500;
        /// <summary>The self features of a healed, blessed or smitten person last this many sim days.</summary>
        public int MemoryDays = 5;
        /// <summary>SmiteBuilding (huts): 1 burns the stored goods (<c>ResourceDestroyed</c>), 0 scatters them on the ground at the hut's node.</summary>
        public int SmiteBurnsContents;
        /// <summary>SmiteSource: 1 suspends regrowth for <see cref="BlightDays"/> sim days.</summary>
        public int SmiteBlights = 1;
        public int BlightDays = 5;
        /// <summary>CreateResource: the largest count of one cast.</summary>
        public int CreateMax = 10;
        /// <summary>Consecrate: the least distance (nodes) between two different gods' places, and a place's radius when the target gives none.</summary>
        public int SacredSpacing = 6, SacredRadius = 2;
        /// <summary>How long a sign stays in the ring (ticks), and how far it is noticed (nodes).</summary>
        public int SignTicks = 1200, SignNodes = 10;
    }
}
