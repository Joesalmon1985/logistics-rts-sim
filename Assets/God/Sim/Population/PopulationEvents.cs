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

    /// <summary>
    /// One living person's day (the M3 source of <c>fact_person_day</c>; grain: one living person x sim day, emitted at the
    /// start of the next day for every person alive then). Needs (drive levels at day end and their day means), body,
    /// survival state, time allocation (ticks per <see cref="Activity"/>), meals, and the day's learning outcomes
    /// (reward and punishment taken, decisions, the share learning or exploration changed, store size). Integers only.
    /// </summary>
    public sealed class NpcDay : DomainEvent
    {
        public int Npc;
        public Faction Faction;
        /// <summary>The sim day this row describes (the day that just ended).</summary>
        public int Day;
        public int AgeDays;
        public int Node;
        public int Health, Nutrition;
        /// <summary>Drive levels at day end (permille): hunger, tiredness, cold, fear, pain.</summary>
        public int Hunger, Fatigue, Cold, Fear, Pain;
        /// <summary>Day means of the drives (permille).</summary>
        public int MeanHunger, MeanFatigue, MeanCold, MeanFear, MeanPain;
        /// <summary>Ticks per <see cref="Activity"/> (idle, eat, forage, sleep, rest, explore, gather, build, flee, carry, other).</summary>
        public int[] Ticks;
        public int WalkTicks;
        public int Meals;
        public bool HasBed;
        public int CarryUnits;
        public int Decisions;
        /// <summary>Share of the day's decisions where the learned term changed the winner, and where exploration alone did (permille).</summary>
        public int LearnedShare, ExploreShare;
        /// <summary>Reward and punishment taken today (value points, both positive).</summary>
        public int RewardSum, PunishSum;
        public int Associations, Conjunctions;
        public LearningModeCode Mode;
    }

    /// <summary>The learning mode as an event field (On 0, Frozen 1, Off 2).</summary>
    public enum LearningModeCode : byte { On = 0, Frozen = 1, Off = 2 }

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

    /// <summary>
    /// Goods entered the population's world (ECONOMY "Produced"): the conservation ledger's source side. v2 (F0): the new lot
    /// (<see cref="Lot"/>, always a fresh id; 0 for goods used at once without becoming a lot), the id it has after merging
    /// on arrival (<see cref="IntoLot"/>; equal to Lot when it did not merge) and its owner.
    /// </summary>
    public sealed class Produced : DomainEvent
    {
        public int Npc;
        public int Source;
        public Ware Ware;
        public int Count;
        public ProduceReason Reason;
        public bool WithTool;
        public int Lot, IntoLot;
        public OwnerKind OwnerKind;
        public int OwnerId;
    }

    /// <summary>
    /// Goods left it (ECONOMY "Consumed"): eaten, spoiled, built into a hut, worn out, buried. v2 (F0): the lot they left
    /// (0 for goods that never were a lot), its owner, and a reference (the hut built with it; <see cref="Hut"/> kept).
    /// </summary>
    public sealed class Consumed : DomainEvent
    {
        public int Npc;
        public int Hut;
        public Ware Ware;
        public int Count;
        public ConsumeReason Reason;
        public int Lot;
        public OwnerKind OwnerKind;
        public int OwnerId;
        public int RefId;
    }

    /// <summary>
    /// A lot moved between holders (F0, code 205; SHARED_CONTRACTS 2.2): exactly one per successful <c>Goods.Move</c>.
    /// <see cref="Lot"/> is the id the moved goods left with (a split's new id; <see cref="ParentLot"/> the lot split from),
    /// <see cref="IntoLot"/> the id they have at the destination, <see cref="EndedLot"/> the id a merge ended (0 none).
    /// </summary>
    public sealed class ItemMoved : DomainEvent
    {
        public int Lot, IntoLot, EndedLot, ParentLot;
        public Ware Ware;
        public int Count;
        public HolderKind FromKind;
        public int FromId;
        public HolderKind ToKind;
        public int ToId;
        public MoveReason Reason;
        public int Actor;
        /// <summary>Where the actor stood and where the two holders were (-1 unknown), before the move: invariant C6 (reach) is checked from these.</summary>
        public int ActorNode, FromNode, ToNode;
        public OwnerKind OwnerBeforeKind;
        public int OwnerBeforeId;
        public OwnerKind OwnerAfterKind;
        public int OwnerAfterId;
        /// <summary>The purpose it served (a DealId, a care arrangement), 0 none.</summary>
        public int PurposeRef;
    }

    /// <summary>An asset changed owner (F0, code 206; SHARED_CONTRACTS 3). Asset kind 1 = a lot (its id after the move).</summary>
    public sealed class OwnershipChanged : DomainEvent
    {
        public const byte AssetLot = 1;
        public byte AssetKind;
        public int Lot;
        public Ware Ware;
        public int Count;
        public OwnerKind FromKind;
        public int FromId;
        public OwnerKind ToKind;
        public int ToId;
        public OwnershipBasis Basis;
        public int Actor;
    }
}
