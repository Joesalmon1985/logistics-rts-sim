namespace God.Sim.Population.Households
{
    /// <summary>
    /// M4's rows of the event catalogue (codes 105-159, 230-249), appended to <c>EventCatalog.All</c> by F0.11. OWNER: M4.
    /// Codes follow SHARED_CONTRACTS 8.2 in the order listed there; a changed field meaning takes a new code.
    /// </summary>
    public static class HouseholdEvents
    {
        public static readonly ReadOnlyArray<EventInfo> Catalog = new ReadOnlyArray<EventInfo>(
            // ---- life cycle (105-124) ----
            new EventInfo(typeof(NpcBorn), 105, 1, TelemetryDomain.Population, "fact_birth", note: "a birth: also a dim_person row (origin born)"),
            new EventInfo(typeof(NpcMatured), 106, 1, TelemetryDomain.Population, EventCatalog.Generic, note: "a stage change (dim_person_history is left to M13a)"),
            new EventInfo(typeof(PregnancyStarted), 107, 1, TelemetryDomain.Population, EventCatalog.Generic),
            new EventInfo(typeof(PregnancyEnded), 108, 1, TelemetryDomain.Population, EventCatalog.Generic),
            new EventInfo(typeof(NpcImmigrated), 109, 1, TelemetryDomain.Population, "fact_migration", note: "an arrival: also a dim_person row (origin immigrant)"),
            new EventInfo(typeof(NpcEmigrated), 110, 1, TelemetryDomain.Population, "fact_migration", note: "not emitted in M4 (emigration off the map is not built)"),
            new EventInfo(typeof(IllnessStarted), 111, 1, TelemetryDomain.Population, EventCatalog.Generic),
            new EventInfo(typeof(IllnessEnded), 112, 1, TelemetryDomain.Population, EventCatalog.Generic),
            new EventInfo(typeof(InjuryTaken), 113, 1, TelemetryDomain.Population, EventCatalog.Generic),
            new EventInfo(typeof(GeneMutated), 114, 1, TelemetryDomain.Population, EventCatalog.Generic),
            new EventInfo(typeof(NpcGenome), 116, 1, TelemetryDomain.Population, "dim_person", note: "a person's sex, generation, genome hash and birth phenotype, once at its origin (founders at the first tick)"),
            // ---- households, kin, care (125-159) ----
            new EventInfo(typeof(HouseholdFormed), 125, 1, TelemetryDomain.Household, "dim_household"),
            new EventInfo(typeof(HouseholdJoined), 126, 1, TelemetryDomain.Household, "fact_household_membership"),
            new EventInfo(typeof(HouseholdLeft), 127, 1, TelemetryDomain.Household, "fact_household_membership"),
            new EventInfo(typeof(HouseholdDissolved), 128, 1, TelemetryDomain.Household, "dim_household"),
            new EventInfo(typeof(CourtshipAct), 129, 1, TelemetryDomain.Household, EventCatalog.Generic, true),
            new EventInfo(typeof(PartnershipFormed), 130, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(PartnershipEnded), 131, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(TryForChildChosen), 132, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(TryForChildStopped), 133, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(ChildFed), 134, 1, TelemetryDomain.Household, "fact_care", true, note: "one food hand-over to a child; also aggregated into fact_social_day"),
            new EventInfo(typeof(BeggingMade), 135, 1, TelemetryDomain.Household, EventCatalog.Generic, true),
            new EventInfo(typeof(CareGiven), 136, 1, TelemetryDomain.Household, "fact_care", true, note: "one care act (tend, feed a patient); also aggregated into fact_social_day"),
            new EventInfo(typeof(ChildOrphaned), 137, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(ChildFostered), 138, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(ChildUnattendedHazard), 139, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(KinBeliefFormed), 140, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(CareArrangementMade), 142, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(CareArrangementEnded), 143, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(HutRightGranted), 144, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(HutRightLost), 145, 1, TelemetryDomain.Household, EventCatalog.Generic),
            new EventInfo(typeof(HouseholdDay), 146, 1, TelemetryDomain.Household, "fact_household_day"),
            new EventInfo(typeof(PersonDayLife), 147, 1, TelemetryDomain.Population, "fact_person_day", note: "M4's columns of the person-day row (stage, household, partner, care ticks)"),
            // ---- D4 relationship ontology (unity/demog) ----
            new EventInfo(typeof(ReproductiveUnionFormed), 148, 1, TelemetryDomain.Household, EventCatalog.Generic, note: "D4: two adults agreed to try for a child (kind 1 partners, 2 outside a partnership)"),
            new EventInfo(typeof(ReproductiveUnionEnded), 149, 1, TelemetryDomain.Household, EventCatalog.Generic, note: "D4: a reproductive relationship lapsed or a partner died"),
            new EventInfo(typeof(ChildWanted), 150, 1, TelemetryDomain.Household, EventCatalog.Generic, true, note: "D4: an adult showed that it wants a child"),
            // ---- estates (230-249) ----
            new EventInfo(typeof(EstateOpened), 230, 1, TelemetryDomain.Estate, EventCatalog.Generic),
            new EventInfo(typeof(EstateTransfer), 231, 1, TelemetryDomain.Estate, "fact_inheritance"),
            new EventInfo(typeof(EstateSettled), 232, 1, TelemetryDomain.Estate, EventCatalog.Generic),
            new EventInfo(typeof(EstateDisputed), 234, 1, TelemetryDomain.Estate, EventCatalog.Generic));
    }

    // ---------------- life cycle ----------------

    /// <summary>A birth (105): child, mother, the believed father (the mother's partner at the birth, 0 none), the biological father (analytics truth), household, place, sex, genome hash, generation.</summary>
    public sealed class NpcBorn : DomainEvent
    {
        public int Npc, Mother, BelievedFather, BioFather, Household;
        public Faction Faction;
        public int Node;
        public byte Sex;
        public long GenomeHash;
        public int Generation;
    }

    /// <summary>A life-stage change (106): child to adult, adult to elder.</summary>
    public sealed class NpcMatured : DomainEvent
    {
        public int Npc;
        public LifeStage From, To;
        public int AgeMilli;
    }

    /// <summary>A conception (107), with both parents' ages (M4.25: both adults).</summary>
    public sealed class PregnancyStarted : DomainEvent
    {
        public int Mother, BioFather, MotherAgeMilli, FatherAgeMilli;
        public long DueTick;
    }

    /// <summary>A pregnancy ended (108): 1 birth, 2 miscarriage, 3 the mother died.</summary>
    public sealed class PregnancyEnded : DomainEvent
    {
        public int Mother, Child;
        public byte Outcome;
        public const byte Birth = 1, Miscarriage = 2, MotherDied = 3;
    }

    /// <summary>An arrival from off the island (109).</summary>
    public sealed class NpcImmigrated : DomainEvent
    {
        public int Npc;
        public Faction Faction;
        public int Node, AgeMilli;
        public byte Sex;
        public long GenomeHash;
    }

    /// <summary>A departure off the map (110). Catalogued; not emitted in M4.</summary>
    public sealed class NpcEmigrated : DomainEvent
    {
        public int Npc;
        public Faction Faction;
        public int Node;
    }

    /// <summary>An infection (111) with its one route (1 contact, 2 crowding, 3 sanitation, 4 wound, 5 import) and, for contact, the source (analytics).</summary>
    public sealed class IllnessStarted : DomainEvent
    {
        public int Npc, Source, Severity, Days;
        public byte Route;
        public const byte Contact = 1, Crowding = 2, Sanitation = 3, Wound = 4, Import = 5;
    }

    /// <summary>An illness ended (112): recovered or died of it, after so many days.</summary>
    public sealed class IllnessEnded : DomainEvent
    {
        public int Npc, Days;
        public bool Recovered;
    }

    /// <summary>An injury (113) from work (1 foraging, 2 gathering).</summary>
    public sealed class InjuryTaken : DomainEvent
    {
        public int Npc, Amount;
        public byte Cause;
    }

    /// <summary>A mutation in a gamete (114): the child, the locus, which parent (0 mother, 1 father), the allele before and after.</summary>
    public sealed class GeneMutated : DomainEvent
    {
        public int Child, Locus, Old, New;
        public byte Parent;
    }

    /// <summary>
    /// A person's genetic and bodily facts once at its origin (116): sex, generation, genome hash, the birth phenotype (physical
    /// part provisional until adulthood) and its natural death age. For the warehouse's person dimension; analytics only.
    /// </summary>
    public sealed class NpcGenome : DomainEvent
    {
        public int Npc, Generation, MaxAgeMilli;
        public byte Sex;
        public long GenomeHash;
        public int Strength, Size, Health, Fertility, Lifespan, Features, Complexion, Build, NurtureGain, KinWeight;

        public static NpcGenome Of(Npc n)
        {
            var l = n.Life; var p = l.Pheno;
            return new NpcGenome
            {
                Npc = n.Id, Generation = l.Generation, MaxAgeMilli = l.MaxAgeMilli, Sex = l.Sex, GenomeHash = Genetics.GenomeOps.Hash(l.Genome),
                Strength = p.Strength, Size = p.Size, Health = p.Health, Fertility = p.Fertility, Lifespan = p.Lifespan, Features = p.Features,
                Complexion = p.Complexion, Build = p.Build, NurtureGain = p.NurtureGain, KinWeight = n.Traits[Trait.KinWeight],
            };
        }
    }

    // ---------------- households ----------------

    /// <summary>Reasons of joining (HOUSEHOLDS "Forming").</summary>
    public static class JoinReason
    {
        public const byte Founded = 1, Partnered = 2, Born = 3, Fostered = 4, Invited = 5, Reunited = 6;
    }

    /// <summary>Reasons of leaving (HOUSEHOLDS "Breaking").</summary>
    public static class LeaveReason
    {
        public const byte Died = 1, LeftHome = 2, Separated = 3, Deserted = 4, Expelled = 5, FosteredOut = 6, Emigrated = 7, Captured = 8, Partnered = 9;
    }

    public sealed class HouseholdFormed : DomainEvent
    {
        public int Household, Hut, Founder;
        public Faction Faction;
        public byte Reason;
    }

    public sealed class HouseholdJoined : DomainEvent
    {
        public int Npc, Household, FromHousehold;
        public byte Reason;
    }

    public sealed class HouseholdLeft : DomainEvent
    {
        public int Npc, Household, ToHousehold;
        public byte Reason;
    }

    /// <summary>A household with no members ended (128): what its store held went to the ground at its hut, ownerless.</summary>
    public sealed class HouseholdDissolved : DomainEvent
    {
        public int Household, Hut, LotsDropped;
        public Faction Faction;
    }

    /// <summary>A courtship act (129): 1 a proposal display.</summary>
    public sealed class CourtshipAct : DomainEvent
    {
        public int Giver, Receiver;
        public byte Kind;
    }

    public sealed class PartnershipFormed : DomainEvent
    {
        public int Partnership, A, B, Initiator, Household;
    }

    /// <summary>A partnership ended (131): 1 death, 2 left (desertion), 3 separated.</summary>
    public sealed class PartnershipEnded : DomainEvent
    {
        public int Partnership, A, B, Initiator;
        public byte Reason;
        public const byte Death = 1, Left = 2, Separated = 3;
    }

    /// <summary>D4 (148): a reproductive relationship agreed: its id, the two adults, who offered, its kind, and whether they are romantic partners too.</summary>
    public sealed class ReproductiveUnionFormed : DomainEvent
    {
        public int Union, A, B, Initiator;
        public byte Kind;
        public bool Partners;
    }

    /// <summary>D4 (149): a reproductive relationship ended: 1 lapsed (neither tried for the lapse time), 2 a death; children born in it, days it lasted.</summary>
    public sealed class ReproductiveUnionEnded : DomainEvent
    {
        public int Union, A, B, Children, Days;
        public byte Reason;
        public const byte Lapsed = 1, Death = 2;
    }

    /// <summary>D4 (150): an adult showed that it wants a child (a display at its own spot), with its child-desire preference (analytics).</summary>
    public sealed class ChildWanted : DomainEvent
    {
        public int Npc, Desire;
        public bool Partnered;
    }

    public sealed class TryForChildChosen : DomainEvent
    {
        public int Npc, Partner, Partnership;
    }

    public sealed class TryForChildStopped : DomainEvent
    {
        public int Npc, Partner;
    }

    /// <summary>
    /// Food handed to a child (134; SHARED_CONTRACTS 2.5): the feeder, the child, the lot it now has (<c>MoveResult.Lot</c>), what
    /// and how much, where it came from (1 the feeder's carry), the feeder's believed kin band and the true r (analytics).
    /// </summary>
    public sealed class ChildFed : DomainEvent
    {
        public int Feeder, Child, Lot;
        public Ware Ware;
        public int Count;
        public byte Source;
        public KinBand Band;
        public int TrueR;
        /// <summary>The care arrangement it served, 0 none.</summary>
        public int Arrangement;
    }

    /// <summary>A child's begging display (135): at whom, how loud (1-3), and its true hunger band (analytics).</summary>
    public sealed class BeggingMade : DomainEvent
    {
        public int Child, Target, Intensity, HungerBand;
    }

    /// <summary>A care act (136): 1 food to a patient, 2 tending, 3 minding a child.</summary>
    public sealed class CareGiven : DomainEvent
    {
        public int Carer, Patient, Ticks;
        public byte Kind;
        public KinBand Band;
        public int TrueR;
        public const byte Feed = 1, Tend = 2, Mind = 3;
    }

    public sealed class ChildOrphaned : DomainEvent
    {
        public int Child, Household;
    }

    public sealed class ChildFostered : DomainEvent
    {
        public int Child, Adopter, Household, FromHousehold, DaysOrphaned;
    }

    /// <summary>A child alone met a hazard (139): 6 exposure, 8 accident (the death cause), its alone share of the day (permille), whether it died.</summary>
    public sealed class ChildUnattendedHazard : DomainEvent
    {
        public int Child, AlonePermille;
        public DeathCause Cause;
        public bool Died;
    }

    /// <summary>A kin belief formed or changed (140).</summary>
    public sealed class KinBeliefFormed : DomainEvent
    {
        public int Npc, Other, Confidence;
        public KinRole Role;
        public KinSource Source;
    }

    public sealed class CareArrangementMade : DomainEvent
    {
        public int Arrangement, Asker, Carer, AskerHousehold, CarerHousehold;
    }

    /// <summary>An arrangement ended (143): 1 by a side's decision, 2 a death, 3 households changed.</summary>
    public sealed class CareArrangementEnded : DomainEvent
    {
        public int Arrangement, EndedBy, Given, Returned;
        public byte Reason;
    }

    /// <summary>A sleeping right granted (144): source 1 built, 2 household, 3 invited; lodger = a child's half bed.</summary>
    public sealed class HutRightGranted : DomainEvent
    {
        public int Hut, Npc;
        public byte Source;
        public bool Lodger;
    }

    public sealed class HutRightLost : DomainEvent
    {
        public int Hut, Npc;
        public byte Reason;
    }

    /// <summary>The daily household snapshot (146): members, children, elders, food in its store.</summary>
    public sealed class HouseholdDay : DomainEvent
    {
        public int Household, Day, Hut, Members, Children, Elders, StoreFood, StoreUnits;
        public Faction Faction;
    }

    /// <summary>M4's columns of one living person's day (147), for the person who had an <c>NpcDay</c> row that day.</summary>
    public sealed class PersonDayLife : DomainEvent
    {
        public int Npc, Day, AgeMilli, Household, Partner, CareGivenTicks, CareReceivedTicks, Growth;
        public LifeStage Stage;
        public byte Sex;
        public bool Pregnant, Ill;
    }

    // ---------------- estates ----------------

    /// <summary>An estate opened at a death (230): the bin at the death node, the heir candidates and the lots it holds.</summary>
    public sealed class EstateOpened : DomainEvent
    {
        public int Estate, Bin, Node, Heirs, Lots;
        public Faction Faction;
    }

    /// <summary>Why an estate asset went where it went (MATERIAL 3 <c>Reason</c>).</summary>
    public static class EstateReason
    {
        public const byte Custom = 1, Burial = 2, Ownerless = 3, Escheat = 4, Collected = 5, Delivered = 6, Household = 7, Communal = 8;
    }

    /// <summary>
    /// One estate transfer (231): asset kind 1 (a lot), asset id = the lot id, ware and quantity, from (the estate) and to
    /// (owner kind and id; None for ownerless or destroyed), and the reason.
    /// </summary>
    public sealed class EstateTransfer : DomainEvent
    {
        public int Estate;
        public byte AssetKind;
        public int AssetId;
        public Ware Ware;
        public int Count;
        public OwnerKind ToKind;
        public int ToId;
        public byte Reason;
        public int Executor;
    }

    /// <summary>An estate settled (232): ticks taken, transfers, and the units that went ownerless or were destroyed.</summary>
    public sealed class EstateSettled : DomainEvent
    {
        public int Estate, Ticks, Transfers, Ownerless, Destroyed, Executor;
        public byte Custom;
    }

    /// <summary>A dispute (234): an heir whose own custom would have given it at least 25% more (its own valuation); the grudge it holds (M5 applies it).</summary>
    public sealed class EstateDisputed : DomainEvent
    {
        public int Estate, Claimant, Executor, ShortfallPermille, Grudge;
    }
}
