namespace God.Sim
{
    public sealed class UnitSpawned : DomainEvent { public int UnitId; public Faction Faction; public UnitKind Kind; public Kit Kit; public int X, Z; }
    /// <summary>
    /// A unit is gone: killed (<c>KillerId</c> is the unit whose attack did it, 0 when unknown), or <c>Disbanded</c>
    /// because its faction was eliminated.
    /// </summary>
    public sealed class UnitDied : DomainEvent { public int UnitId; public Faction Faction; public UnitKind Kind; public Kit Kit; public int X, Z; public bool Disbanded; public int KillerId; }

    /// <summary>
    /// One attack resolved: a swing or a shot from the attacker at its target, how it ended (<see cref="AttackOutcome"/>:
    /// miss, blocked, hit, crit, or a strike on a building) and the damage done. Positions are millimetres at the time of
    /// the attack. <c>Weapon</c> tells the view what to draw (a bow or sling shot flies; melee is a swing).
    /// </summary>
    public sealed class UnitFired : DomainEvent
    {
        public int AttackerId; public Faction Faction; public UnitKind Kind;
        public Ware Weapon;
        public TargetKind TargetKind; public int TargetId;
        public int FromX, FromZ, ToX, ToZ;
        public AttackOutcome Outcome;
        public int Damage;
        public bool Ranged => Gear.IsRanged(Weapon);
    }

    public sealed class BuildingDestroyed : DomainEvent
    {
        public int BuildingId; public Faction Faction; public int Node; public BuildingSize Size; public string Kind; public bool IsHq;
        public int RubbleId;
    }

    public sealed class RubbleExpired : DomainEvent { public int RubbleId; public int Node; }

    public sealed class TargetMarked : DomainEvent { public Faction By; public int BuildingId; public int Order; }
    /// <summary>
    /// A mark of <c>By</c> was removed: by its own order (<c>Destroyed</c> false), because the target was destroyed or
    /// removed (<c>Destroyed</c> true), or because the target changed hands or <c>By</c> made peace with its owner.
    /// </summary>
    public sealed class TargetUnmarked : DomainEvent { public Faction By; public int BuildingId; public bool Destroyed; }

    public sealed class DefendSet : DomainEvent { public Faction Faction; public int FlagId; public int AnchorBuildingId; public int Node; public int Guards; }
    /// <summary>A defend flag was removed by its faction, or its anchor building was destroyed, removed or captured.</summary>
    public sealed class DefendRemoved : DomainEvent { public Faction Faction; public int FlagId; public bool AnchorLost; }
}
