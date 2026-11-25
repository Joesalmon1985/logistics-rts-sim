namespace God.Sim
{
    public sealed class UnitSpawned : SimEvent { public int UnitId; public Faction Faction; public UnitKind Kind; public int X, Z; }
    /// <summary>A unit is gone: killed, or <c>Disbanded</c> because its faction was eliminated.</summary>
    public sealed class UnitDied : SimEvent { public int UnitId; public Faction Faction; public UnitKind Kind; public int X, Z; public bool Disbanded; }

    /// <summary>One shot: a tracer from the attacker to the target. Positions are millimetres at the time of firing.</summary>
    public sealed class UnitFired : SimEvent
    {
        public int AttackerId; public Faction Faction; public UnitKind Kind;
        public TargetKind TargetKind; public int TargetId;
        public int FromX, FromZ, ToX, ToZ;
        public int Damage;
    }

    public sealed class BuildingDestroyed : SimEvent
    {
        public int BuildingId; public Faction Faction; public int Node; public BuildingSize Size; public string Kind; public bool IsHq;
        public int RubbleId;
    }

    public sealed class RubbleExpired : SimEvent { public int RubbleId; public int Node; }

    public sealed class TargetMarked : SimEvent { public Faction By; public int BuildingId; public int Order; }
    /// <summary>
    /// A mark of <c>By</c> was removed: by its own order (<c>Destroyed</c> false), because the target was destroyed or
    /// removed (<c>Destroyed</c> true), or because the target changed hands or <c>By</c> made peace with its owner.
    /// </summary>
    public sealed class TargetUnmarked : SimEvent { public Faction By; public int BuildingId; public bool Destroyed; }

    public sealed class DefendSet : SimEvent { public Faction Faction; public int FlagId; public int AnchorBuildingId; public int Node; public int Guards; }
    /// <summary>A defend flag was removed by its faction, or its anchor building was destroyed, removed or captured.</summary>
    public sealed class DefendRemoved : SimEvent { public Faction Faction; public int FlagId; public bool AnchorLost; }
}
