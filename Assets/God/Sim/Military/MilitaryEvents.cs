namespace God.Sim
{
    public sealed class UnitSpawned : SimEvent { public int UnitId; public Faction Faction; public UnitKind Kind; public int X, Z; public int WaveId; }
    public sealed class UnitDied : SimEvent { public int UnitId; public Faction Faction; public UnitKind Kind; public int X, Z; }

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

    public sealed class TargetMarked : SimEvent { public int BuildingId; public int Order; }
    /// <summary>A mark was removed by the player (<c>Destroyed</c> false) or because the target was destroyed or removed.</summary>
    public sealed class TargetUnmarked : SimEvent { public int BuildingId; public bool Destroyed; }

    public sealed class DefendSet : SimEvent { public int FlagId; public int AnchorBuildingId; public int Node; public int Guards; }
    /// <summary>A defend flag was removed by the player, or its anchor building was destroyed or removed.</summary>
    public sealed class DefendRemoved : SimEvent { public int FlagId; public bool AnchorLost; }

    public sealed class WaveSpawned : SimEvent { public int Number; public int Infantry; public int Vehicles; }
    public sealed class WavesStopped : SimEvent { }

    /// <summary>The game ended. <c>Won</c> is true when the enemy HQ fell, false when the player lost the last HQ.</summary>
    public sealed class GameOver : SimEvent { public bool Won; }
}
