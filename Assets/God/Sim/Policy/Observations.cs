using System.Collections.Generic;

namespace God.Sim.Policy
{
    /// <summary>What one institution's manager reads (INSTITUTIONAL_STATE_DESIGN "InstitutionView"). PLACEHOLDER until M12.</summary>
    public readonly struct InstitutionView
    {
        public readonly int InstitutionId;
        public readonly Faction Faction;
        public readonly long Tick;
        public InstitutionView(int institutionId, Faction faction, long tick) { InstitutionId = institutionId; Faction = faction; Tick = tick; }
    }

    /// <summary>An enemy soldier a soldier sees: id, position (mm), squared distance and distance (mm, rounded down).</summary>
    public readonly struct SeenUnit
    {
        public readonly int Id; public readonly int X, Z; public readonly long Dist2; public readonly int Edge;
        public SeenUnit(int id, int x, int z, long dist2, int edge) { Id = id; X = x; Z = z; Dist2 = dist2; Edge = edge; }
    }

    /// <summary>An enemy building a soldier sees: id, position (mm) and the distance to its edge (mm).</summary>
    public readonly struct SeenBuilding
    {
        public readonly int Id; public readonly int X, Z; public readonly int Edge;
        public SeenBuilding(int id, int x, int z, int edge) { Id = id; X = x; Z = z; Edge = edge; }
    }

    /// <summary>
    /// What one soldier on duty perceives now (MILITARY_DESIGN "IMilitaryPolicy"), built by the military system from the
    /// unit's own sight: the enemies in sight (units at war with it, within its sight and, for a guard, its chase area),
    /// in the sim's unit and building order, plus its own orders (guard post, its faction's marked targets) and kit.
    /// Stage 3 soldiers are <c>MilUnit</c>s, not NPCs, so the NPC parts (morale, rations, pay) wait for M9.
    /// </summary>
    public readonly struct SoldierObservation
    {
        public readonly int UnitId;
        public readonly Faction Faction;
        public readonly int X, Z;
        public readonly long Tick;
        /// <summary>The kit's reach and sight (mm), and whether it shoots.</summary>
        public readonly int RangeMm, SightMm;
        public readonly bool Ranged;
        /// <summary>True when it guards a defend flag (its post).</summary>
        public readonly bool OnGuard;
        /// <summary>Enemy units in sight, in the sim's unit order.</summary>
        public readonly IReadOnlyList<SeenUnit> Enemies;
        /// <summary>Enemy buildings in sight, in the sim's building order.</summary>
        public readonly IReadOnlyList<SeenBuilding> EnemyBuildings;
        /// <summary>The ids its faction has marked as targets, in marking order (an order known to every soldier of the faction).</summary>
        public readonly IReadOnlyList<int> Marked;
        /// <summary>Where the first marked target stands (mm), when it still stands; <see cref="HasMarchTarget"/> false otherwise.</summary>
        public readonly bool HasMarchTarget;
        public readonly int MarchX, MarchZ;

        public SoldierObservation(int unitId, Faction faction, int x, int z, long tick, int rangeMm, int sightMm, bool ranged, bool onGuard,
            IReadOnlyList<SeenUnit> enemies, IReadOnlyList<SeenBuilding> enemyBuildings, IReadOnlyList<int> marked,
            bool hasMarchTarget, int marchX, int marchZ)
        {
            UnitId = unitId; Faction = faction; X = x; Z = z; Tick = tick; RangeMm = rangeMm; SightMm = sightMm; Ranged = ranged; OnGuard = onGuard;
            Enemies = enemies; EnemyBuildings = enemyBuildings; Marked = marked; HasMarchTarget = hasMarchTarget; MarchX = marchX; MarchZ = marchZ;
        }
    }
}
