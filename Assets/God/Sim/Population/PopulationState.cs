using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;

namespace God.Sim.Population
{
    /// <summary>Why an NPC died: exactly one cause (POPULATION_DESIGN "Death").</summary>
    public enum DeathCause : byte { None = 0, Starvation = 1, Wound = 2, Disease = 3, OldAge = 4, Childbirth = 5, Exposure = 6, Violence = 7, Accident = 8 }

    /// <summary>A homogeneous quantity of one ware (ECONOMY_DESIGN 3.1, the M3 subset: the holder is the owner).</summary>
    public struct ItemLot
    {
        /// <summary>ItemLotId, never reused. 0 = empty slot.</summary>
        public int Id;
        public Ware Ware;
        public int Count;
        /// <summary>The SimCalendar day it rots; -1 = never.</summary>
        public int SpoilDay;
        /// <summary>Uses left (tools), 0 for other wares.</summary>
        public int Uses;

        public bool Empty => Id == 0 || Count <= 0;
    }

    /// <summary>What kind of wild resource a source is.</summary>
    public enum SourceKind : byte { Berries = 1, Roots = 2, Fish = 3, Nightshade = 4, Wood = 5 }

    /// <summary>A wild food source or another renewable stock on a node (ECONOMY_DESIGN 3.4), with logistic regrowth.</summary>
    public sealed class ResourceSource
    {
        public int Id;
        public SourceKind Kind;
        public int Hex, Node;
        public int Stock, Capacity;
        public int RegrowPermille;
        /// <summary>Days an empty source has waited for recovery from a neighbour.</summary>
        public int EmptyDays;
        /// <summary>0 = open access, unmanaged (commons records come at M8).</summary>
        public int CommonsId;
    }

    /// <summary>
    /// A simple hut built by NPCs from logs (HOUSEHOLDS_AND_KINSHIP "Founded"; ECONOMY "Built"): a site until its logs and
    /// work are in, then a shelter with beds. Whoever contributed labour or materials gets a sleeping right while a bed is free.
    /// </summary>
    public sealed class Hut
    {
        public int Id;
        public Faction Faction;
        public int Node, Hex;
        public bool Built;
        public int LogsIn, LogsNeeded;
        public int Work, WorkNeeded;
        public int Beds;
        /// <summary>Sleeping rights: NPC ids, 0 = free bed.</summary>
        public int[] Rights;
        /// <summary>Contributors not yet holding a right (labour or a log), in order of first contribution, at most 8.</summary>
        public List<int> Contributors = new List<int>();
        public int FoundedBy;
        public long FoundedTick, BuiltTick;

        public bool HasRight(int npc) { foreach (int r in Rights) if (r == npc) return true; return false; }
        public int FreeBeds { get { int n = 0; foreach (int r in Rights) if (r == 0) n++; return n; } }
    }

    /// <summary>A lot lying on a node (dropped, set down to sleep, left by the dead): anyone who reaches it may pick it up.</summary>
    public sealed class GroundLot
    {
        public int Node;
        public ItemLot Lot;
    }

    /// <summary>Why goods entered the population's world (the ledger's reason codes, ECONOMY A1).</summary>
    public enum ProduceReason : byte { Founder = 0, Foraged = 1, HandGathered = 2, Placed = 3 }
    /// <summary>Why goods left it.</summary>
    public enum ConsumeReason : byte { Eaten = 0, Spoiled = 1, Built = 2, Wear = 3 }

    /// <summary>
    /// The one conservation ledger of personal goods (ECONOMY_DESIGN A1): for every ware,
    /// <c>Σ Produced − Σ Consumed = Σ (carry + ground)</c> exactly, every reason counted. Stage 3 HQ stocks and walkers keep
    /// their own books until NPC labour replaces them (M6).
    /// </summary>
    public sealed class GoodsLedger
    {
        public const int ProduceReasons = 4, ConsumeReasons = 4;
        public long[] Produced = new long[ProduceReasons * Wares.PersonalCount];
        public long[] Consumed = new long[ConsumeReasons * Wares.PersonalCount];

        public void Produce(ProduceReason r, Ware w, int n) { Produced[(int)r * Wares.PersonalCount + (int)w] += n; }
        public void Consume(ConsumeReason r, Ware w, int n) { Consumed[(int)r * Wares.PersonalCount + (int)w] += n; }

        public long ProducedOf(Ware w) { long s = 0; for (int r = 0; r < ProduceReasons; r++) s += Produced[r * Wares.PersonalCount + (int)w]; return s; }
        public long ConsumedOf(Ware w) { long s = 0; for (int r = 0; r < ConsumeReasons; r++) s += Consumed[r * Wares.PersonalCount + (int)w]; return s; }
        public long ProducedBy(ProduceReason r, Ware w) => Produced[(int)r * Wares.PersonalCount + (int)w];
        public long ConsumedBy(ConsumeReason r, Ware w) => Consumed[(int)r * Wares.PersonalCount + (int)w];
    }

    /// <summary>Counters of the population (tests and reports): decisions, refused intents, perception checks, deaths by cause.</summary>
    public sealed class PopulationStats
    {
        public long Decisions, IntentsRefused, NotPerceived, PerceptionChecks;
        public long Meals, Sleeps, HutsBuilt, LogsGathered, Foraged;
        public int[] DeathsByCause = new int[9];
        public int Founders, Deaths;
    }

    /// <summary>What an NPC is doing now: the running intent (verb and target), its progress and whether it is busy.</summary>
    public struct NpcAction
    {
        /// <summary>Learning verb (<see cref="Learning.Verb"/>), 0 = idle.</summary>
        public byte Verb;
        public Policy.IntentTarget TargetKind;
        public int TargetId;
        public int TargetNode;
        /// <summary>The NPC is busy (eating) until this tick and does not think.</summary>
        public long BusyUntil;
        /// <summary>Work progress in permille of one unit (foraging, gathering, eating).</summary>
        public int Progress;
        public bool Asleep;
        /// <summary>Asleep in a hut (the hut id), 0 = in the open.</summary>
        public int SleepHut;
        public long Since;
    }

    /// <summary>The 12 personality traits (POPULATION_DESIGN "Personality"), -100..100, fixed at birth (random per founder until M4's genome).</summary>
    public static class Trait
    {
        public const int Aggression = 0, Boldness = 1, Patience = 2, Generosity = 3, Trustfulness = 4, Vengefulness = 5,
            Forgiveness = 6, Honesty = 7, Loyalty = 8, Conformity = 9, Industry = 10, KinWeight = 11;
        public const int Count = 12;
        public static readonly ReadOnlyArray<string> Names = new ReadOnlyArray<string>(
            "aggression", "boldness", "patience", "generosity", "trustfulness", "vengefulness", "forgiveness", "honesty", "loyalty", "conformity", "industry", "kin weight");
    }

    /// <summary>
    /// One person for life (POPULATION_DESIGN section 3, the M3 part): identity, body, drives (micro-permille, x1000),
    /// position and path on the node grid, personal carry, home hut, traits and learning parameters, the running action,
    /// its own random streams, and its mind. All public fields: saved and hashed.
    /// </summary>
    public sealed class Npc
    {
        public int Id;
        public Faction Faction;
        public bool Alive = true;
        public long BornTick, DiedTick;
        public DeathCause DeathCause;
        /// <summary>True for a founder of the scenario (no NpcBorn: M4 brings births).</summary>
        public bool Founder;

        // ---- position ----
        public int Node;
        public int[] Path;
        public int PathIdx;
        public int PathGoal = -1;
        public int StepTicks;

        // ---- body and drives (micro: 0..1,000,000) ----
        public int Health = 1000000, Nutrition = 800000, Injury;
        public int[] Drives = new int[Drive.Count];
        /// <summary>The drives when reward was last settled (micro): drive relief and rise are paid on the change since then.</summary>
        public int[] RewardSnap = new int[Drive.Count];
        /// <summary>Pain added by events (a toxic meal), decaying; pain = max(injury, this).</summary>
        public int PainExtra;

        // ---- goods ----
        public ItemLot[] Carry;
        public int HomeHut;
        /// <summary>The hut site it is building, 0 for none.</summary>
        public int SiteHut;
        /// <summary>Units of each food eaten today (the variety rule): berries, roots, fish, nightshade.</summary>
        public int[] EatenToday = new int[4];
        public int EatenDay = -1;

        // ---- nature ----
        public int[] Traits = new int[Trait.Count];
        public int LearnRate = 1000, ForgetRate = 1000, Curiosity = 1000;
        public InstinctSet Instincts = InstinctSet.Default;
        /// <summary>The <c>INpcPolicy</c> registry name driving it.</summary>
        public string Policy = Policies.Associative;

        // ---- now ----
        public NpcAction Action;
        public long LastThinkTick = -1;

        // ---- streams (keyed: npc id, then purpose) ----
        public Rng RngPolicy, RngWork, RngLearn;

        public NpcMind Mind;

        public int DriveOf(int d) => Drives[d] / 1000;
        public int HealthPermille => Health / 1000;
        public int NutritionPermille => Nutrition / 1000;

        public LearnTraits LearnTraits => new LearnTraits
        {
            LearnRate = LearnRate, ForgetRate = ForgetRate, Curiosity = Curiosity, Patience = Traits[Trait.Patience], AlphaShift = 2,
        };
    }

    /// <summary>The policy names of the civilian level.</summary>
    public static class Policies
    {
        public const string Associative = "AssociativeNpcPolicy";
    }

    /// <summary>
    /// The population module's state (conventions §5): every NPC ever made (sorted by id; the dead keep their record
    /// without a mind), the wild sources, huts, lots on the ground, the hand-gathering state of dice piles, the goods
    /// ledger and counters. Ids of every kind are monotonic and never reused.
    /// </summary>
    public sealed class PopulationState
    {
        public List<Npc> Npcs = new List<Npc>();
        public int NextNpcId = 1, NextLotId = 1, NextHutId = 1, NextSourceId = 1;
        public List<ResourceSource> Sources = new List<ResourceSource>();
        public List<Hut> Huts = new List<Hut>();
        public List<GroundLot> Lots = new List<GroundLot>();
        /// <summary>Per hex: logs left in the pile an NPC opened by hand (a dice pile holds 4 units, ECONOMY 4.2).</summary>
        public int[] OpenPileUnits;
        public GoodsLedger Ledger = new GoodsLedger();
        public PopulationStats Stats = new PopulationStats();
        /// <summary>The last day whose day-start work (spoiling, regrowth) ran.</summary>
        public long DayDone = -1;
        /// <summary>True once the founders' NpcSpawned events went out (on the first population tick).</summary>
        public bool FoundersAnnounced;
        /// <summary>Set when sources, huts or lots change, so the system's spatial index is rebuilt (a cache flag, not state).</summary>
        [Newtonsoft.Json.JsonIgnore] public bool IndexDirty = true;

        public Npc Find(int id)
        {
            int lo = 0, hi = Npcs.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = Npcs[mid].Id;
                if (v == id) return Npcs[mid];
                if (v < id) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        public Hut HutById(int id) { foreach (var h in Huts) if (h.Id == id) return h; return null; }
        public ResourceSource SourceById(int id) { foreach (var s in Sources) if (s.Id == id) return s; return null; }

        public int AliveCount { get { int n = 0; foreach (var p in Npcs) if (p.Alive) n++; return n; } }

        public int AliveOf(Faction f) { int n = 0; foreach (var p in Npcs) if (p.Alive && p.Faction == f) n++; return n; }
    }
}
