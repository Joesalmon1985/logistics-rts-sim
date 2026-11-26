namespace God.Sim.Population
{
    /// <summary>A founder of the scenario entered the game (M3: founders only; births come with M4 as NpcBorn).</summary>
    public sealed class NpcSpawned : DomainEvent
    {
        public int Npc;
        public Faction Faction;
        public int Node;
        public bool Founder;
    }

    /// <summary>An NPC died: exactly one cause (POPULATION "Death_HasCause").</summary>
    public sealed class NpcDied : DomainEvent
    {
        public int Npc;
        public Faction Faction;
        public DeathCause Cause;
        public int Node;
        public long AgeTicks;
    }

    /// <summary>An NPC ate one unit: which food, from where (carry or a source), and the hunger relief it got.</summary>
    public sealed class Ate : DomainEvent
    {
        public int Npc;
        public Ware Ware;
        public int Node;
        public bool FromCarry;
        /// <summary>Hunger drive points relieved (after the variety rule).</summary>
        public int Relief;
        public int HungerAfter;
    }

    /// <summary>An NPC fell asleep (by choice or collapsing) or woke.</summary>
    public sealed class Slept : DomainEvent
    {
        public int Npc;
        public int Node;
        /// <summary>The hut it sleeps in, 0 in the open.</summary>
        public int Hut;
        public bool Collapsed;
        public bool Woke;
        public int Fatigue;
    }

    /// <summary>An NPC took food from a wild source into its carry (or ate it there).</summary>
    public sealed class Foraged : DomainEvent
    {
        public int Npc;
        public int Source;
        public Ware Ware;
        public int Count;
        public int StockAfter;
    }

    /// <summary>A hut site was founded by an NPC.</summary>
    public sealed class HutSiteFounded : DomainEvent
    {
        public int Hut;
        public int Npc;
        public Faction Faction;
        public int Node;
    }

    /// <summary>A hut was finished: its sleeping rights went to these contributors.</summary>
    public sealed class HutBuilt : DomainEvent
    {
        public int Hut;
        public Faction Faction;
        public int Node;
        public int[] Rights;
    }

    /// <summary>Goods entered the population's world (ECONOMY "Produced"): the conservation ledger's source side.</summary>
    public sealed class Produced : DomainEvent
    {
        public int Npc;
        public int Source;
        public Ware Ware;
        public int Count;
        public ProduceReason Reason;
        public bool WithTool;
    }

    /// <summary>Goods left it (ECONOMY "Consumed"): eaten, spoiled, built into a hut, worn out.</summary>
    public sealed class Consumed : DomainEvent
    {
        public int Npc;
        public int Hut;
        public Ware Ware;
        public int Count;
        public ConsumeReason Reason;
    }
}
