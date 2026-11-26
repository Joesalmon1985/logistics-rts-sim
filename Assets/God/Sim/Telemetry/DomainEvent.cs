using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// The analysis domains of DATA_WAREHOUSE_DESIGN 3.1, used by recording levels and per-domain switches. M0 adds
    /// <see cref="Territory"/> for the stage 1-3 board events (placement, roads, dice), which the warehouse list did not name.
    /// </summary>
    public enum TelemetryDomain
    {
        Commands = 0,
        Territory = 1,
        Economy = 2,
        Production = 3,
        Military = 4,
        State = 5,
        Population = 10, Household = 11, Citizenship = 12, Employment = 13, Ownership = 14, Estate = 15,
        Social = 20, Contest = 21, Comms = 22, Learning = 23, Culture = 24,
        Crime = 30, Justice = 31, Health = 32, Education = 33, Finance = 34, Politics = 35,
        Records = 40, Media = 41, Policy = 42, Institutions = 43, Kpi = 44,
        Lab = 50,
    }

    /// <summary>
    /// A typed domain event (DATA_WAREHOUSE_DESIGN 3.1): a <see cref="SimEvent"/> with a stable numeric <see cref="Code"/>,
    /// a schema <see cref="Version"/>, a <see cref="Domain"/> and (from <see cref="SimEvent"/>) the tick and its order in the
    /// tick (<see cref="SimEvent.Seq"/>). Pure data: ints, enums, ids and small arrays. Code, version, domain and target table
    /// come from <see cref="EventCatalog"/>, the one table every event type must be in (a test checks it).
    /// </summary>
    public abstract class DomainEvent : SimEvent
    {
        public ushort Code => EventCatalog.Of(GetType()).Code;
        public byte Version => EventCatalog.Of(GetType()).Version;
        public TelemetryDomain Domain => EventCatalog.Of(GetType()).Domain;
        public string Name => EventCatalog.Of(GetType()).Name;
    }

    /// <summary>One row of the event catalogue (also the warehouse's <c>meta_event_type</c> / <c>meta_event_map</c>).</summary>
    public sealed class EventInfo
    {
        public readonly Type Type;
        public readonly ushort Code;
        public readonly byte Version;
        public readonly TelemetryDomain Domain;
        /// <summary>Target warehouse table (<c>generic</c> = <c>fact_event</c>), or <c>analysis-none</c>.</summary>
        public readonly string Table;
        /// <summary>Why the event maps where it does (required for <c>analysis-none</c>).</summary>
        public readonly string Note;
        /// <summary>Emitted many times a tick or a second (fights, dice, piles, production): the Summary level samples it.</summary>
        public readonly bool HighVolume;

        public EventInfo(Type type, ushort code, byte version, TelemetryDomain domain, string table, bool highVolume = false, string note = null)
        {
            Type = type; Code = code; Version = version; Domain = domain; Table = table; HighVolume = highVolume; Note = note;
        }

        public string Name => Type.Name;
    }

    /// <summary>
    /// Every domain event type with its code (DATA_WAREHOUSE_DESIGN 3.7 and 7: stage 1-3 events in 1-99, later modules in
    /// their ranges). Codes are never reused; renaming or changing a field's meaning takes a new code. Frozen.
    /// </summary>
    public static class EventCatalog
    {
        public const string Generic = "generic";

        public static readonly ReadOnlyArray<EventInfo> All = new ReadOnlyArray<EventInfo>(
            // ---- commands (1-9) ----
            new EventInfo(typeof(CommandApplied), 1, 1, TelemetryDomain.Commands, "fact_command"),
            new EventInfo(typeof(CommandRejected), 2, 1, TelemetryDomain.Commands, "fact_command"),
            // ---- territory (10-29) ----
            new EventInfo(typeof(DiceRolled), 10, 1, TelemetryDomain.Territory, Generic, true),
            new EventInfo(typeof(PileAdded), 11, 1, TelemetryDomain.Territory, Generic, true),
            new EventInfo(typeof(PileWasted), 12, 1, TelemetryDomain.Territory, Generic, true),
            new EventInfo(typeof(HqPlaced), 13, 1, TelemetryDomain.Territory, Generic),
            new EventInfo(typeof(BuildingPlaced), 14, 1, TelemetryDomain.Territory, Generic),
            new EventInfo(typeof(BuildingRemoved), 15, 1, TelemetryDomain.Territory, Generic),
            new EventInfo(typeof(FlagPlaced), 16, 1, TelemetryDomain.Territory, Generic),
            new EventInfo(typeof(RoadBuilt), 17, 1, TelemetryDomain.Territory, Generic),
            new EventInfo(typeof(RoadRemoved), 18, 1, TelemetryDomain.Territory, Generic),
            // ---- economy and production (30-39) ----
            new EventInfo(typeof(BuildingCompleted), 30, 1, TelemetryDomain.Economy, Generic),
            new EventInfo(typeof(BuildingUpgradeStarted), 31, 1, TelemetryDomain.Economy, Generic),
            new EventInfo(typeof(BuildingUpgraded), 32, 1, TelemetryDomain.Economy, Generic),
            new EventInfo(typeof(UnitProduced), 33, 1, TelemetryDomain.Production, Generic),
            new EventInfo(typeof(HqLost), 34, 1, TelemetryDomain.Economy, Generic),
            new EventInfo(typeof(WaresProduced), 35, 1, TelemetryDomain.Production, Generic, true),
            // ---- military (40-59) ----
            new EventInfo(typeof(UnitSpawned), 40, 1, TelemetryDomain.Military, Generic),
            new EventInfo(typeof(UnitDied), 41, 1, TelemetryDomain.Military, Generic),
            new EventInfo(typeof(UnitFired), 42, 1, TelemetryDomain.Military, Generic, true),
            new EventInfo(typeof(BuildingDestroyed), 43, 1, TelemetryDomain.Military, Generic),
            new EventInfo(typeof(RubbleExpired), 44, 1, TelemetryDomain.Military, Generic),
            new EventInfo(typeof(TargetMarked), 45, 1, TelemetryDomain.Military, Generic),
            new EventInfo(typeof(TargetUnmarked), 46, 1, TelemetryDomain.Military, Generic),
            new EventInfo(typeof(DefendSet), 47, 1, TelemetryDomain.Military, Generic),
            new EventInfo(typeof(DefendRemoved), 48, 1, TelemetryDomain.Military, Generic),
            // ---- factions and the state (60-69) ----
            new EventInfo(typeof(WarDeclared), 60, 1, TelemetryDomain.State, Generic),
            new EventInfo(typeof(PeaceOffered), 61, 1, TelemetryDomain.State, Generic),
            new EventInfo(typeof(PeaceMade), 62, 1, TelemetryDomain.State, Generic),
            new EventInfo(typeof(CitadelStarted), 63, 1, TelemetryDomain.State, Generic),
            new EventInfo(typeof(CitadelCompleted), 64, 1, TelemetryDomain.State, Generic),
            new EventInfo(typeof(HqCaptured), 65, 1, TelemetryDomain.Military, Generic),
            new EventInfo(typeof(FactionEliminated), 66, 1, TelemetryDomain.State, Generic),
            new EventInfo(typeof(GameOver), 67, 1, TelemetryDomain.State, Generic),
            // ---- trade between factions (70-79) ----
            new EventInfo(typeof(TradeOffered), 70, 1, TelemetryDomain.Economy, Generic),
            new EventInfo(typeof(EnvoyArrived), 71, 1, TelemetryDomain.Economy, Generic),
            new EventInfo(typeof(TradeAccepted), 72, 1, TelemetryDomain.Economy, Generic),
            new EventInfo(typeof(TradeRefused), 73, 1, TelemetryDomain.Economy, Generic),
            new EventInfo(typeof(EnvoyKilled), 74, 1, TelemetryDomain.Economy, Generic),
            new EventInfo(typeof(EnvoyHome), 75, 1, TelemetryDomain.Economy, Generic),
            // ---- population, life cycle (100-199), M3 ----
            new EventInfo(typeof(Population.NpcSpawned), 100, 1, TelemetryDomain.Population, "dim_person", note: "a founder of the scenario (births are NpcBorn from M4)"),
            new EventInfo(typeof(Population.NpcDied), 101, 1, TelemetryDomain.Population, "fact_death"),
            new EventInfo(typeof(Population.Ate), 102, 1, TelemetryDomain.Population, Generic, true),
            new EventInfo(typeof(Population.Slept), 103, 1, TelemetryDomain.Population, Generic, true),
            new EventInfo(typeof(Population.NpcDay), 104, 1, TelemetryDomain.Population, "fact_person_day", note: "one living person x sim day: needs, body, time allocation, meals, learning outcomes (M3 source; SnapshotBuilder.PersonDay at M13)"),
            // ---- economy of personal goods (200-299), M3 ----
            new EventInfo(typeof(Population.Produced), 200, 1, TelemetryDomain.Production, "fact_production", true),
            new EventInfo(typeof(Population.Consumed), 201, 1, TelemetryDomain.Economy, "fact_production", true),
            new EventInfo(typeof(Population.Foraged), 202, 1, TelemetryDomain.Production, Generic, true),
            new EventInfo(typeof(Population.HutBuilt), 203, 1, TelemetryDomain.Economy, Generic),
            new EventInfo(typeof(Population.HutSiteFounded), 204, 1, TelemetryDomain.Economy, Generic),
            // ---- learning (300-399), M3 ----
            new EventInfo(typeof(Learning.AssociationUpdated), 330, 1, TelemetryDomain.Learning, "fact_learning", true),
            new EventInfo(typeof(Learning.ConjunctionRecruited), 331, 1, TelemetryDomain.Learning, "fact_learning"),
            new EventInfo(typeof(Learning.ConjunctionFreed), 332, 1, TelemetryDomain.Learning, "fact_learning"),
            new EventInfo(typeof(Learning.EpisodeClosed), 333, 1, TelemetryDomain.Learning, "fact_learning"));

        /// <summary>Lookup over <see cref="All"/>; filled once in the static constructor (allow-listed in the static audit).</summary>
        private static readonly Dictionary<Type, EventInfo> ByType = BuildIndex();

        private static Dictionary<Type, EventInfo> BuildIndex()
        {
            var d = new Dictionary<Type, EventInfo>();
            foreach (var e in All) d.Add(e.Type, e);
            return d;
        }

        /// <summary>The catalogue row of an event type. Throws for a type missing from the catalogue (a test catches that first).</summary>
        public static EventInfo Of(Type t)
        {
            if (ByType.TryGetValue(t, out var e)) return e;
            throw new InvalidOperationException("Event type " + t.Name + " is not in the EventCatalog.");
        }

        public static bool TryOf(Type t, out EventInfo info) => ByType.TryGetValue(t, out info);

        /// <summary>The module code range a code must lie in (DATA_WAREHOUSE_DESIGN 7), as (low, high).</summary>
        public static (int lo, int hi) RangeOf(TelemetryDomain d, bool stage3)
        {
            if (stage3) return (1, 99);
            switch (d)
            {
                case TelemetryDomain.Population: case TelemetryDomain.Household: return (100, 199);
                case TelemetryDomain.Economy: case TelemetryDomain.Production: case TelemetryDomain.Employment:
                case TelemetryDomain.Ownership: case TelemetryDomain.Estate: case TelemetryDomain.State: case TelemetryDomain.Citizenship: return (200, 299);
                case TelemetryDomain.Social: case TelemetryDomain.Contest: case TelemetryDomain.Comms: case TelemetryDomain.Learning: case TelemetryDomain.Culture: return (300, 399);
                case TelemetryDomain.Military: return (400, 499);
                case TelemetryDomain.Institutions: case TelemetryDomain.Justice: case TelemetryDomain.Health: case TelemetryDomain.Education:
                case TelemetryDomain.Finance: case TelemetryDomain.Politics: case TelemetryDomain.Crime: return (500, 599);
                case TelemetryDomain.Records: case TelemetryDomain.Media: return (600, 699);
                case TelemetryDomain.Policy: case TelemetryDomain.Kpi: return (700, 799);
                case TelemetryDomain.Lab: return (900, 999);
                default: return (1, 99);
            }
        }
    }
}
