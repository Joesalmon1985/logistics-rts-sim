namespace God.Sim
{
    /// <summary>What a weapon hits, for the damage table.</summary>
    public enum TargetClass { Infantry = 0, Vehicle = 1, Building = 2 }

    /// <summary>Stats of one unit kind. Lengths in millimetres, times in ticks (10 per second).</summary>
    public readonly struct UnitStats
    {
        public readonly int MaxHealth;
        /// <summary>Millimetres moved per tick.</summary>
        public readonly int SpeedMmPerTick;
        public readonly int RangeMm;
        public readonly int Damage;
        public readonly int CooldownTicks;
        public UnitStats(int hp, int speed, int range, int damage, int cooldown)
        { MaxHealth = hp; SpeedMmPerTick = speed; RangeMm = range; Damage = damage; CooldownTicks = cooldown; }
    }

    /// <summary>Tuning constants of the military (P3). Change here only. Positions are integer millimetres on the ground plane (X, Z as in the node lattice, metres times 1000).</summary>
    public static class MilitaryTuning
    {
        // Units (OpenRA feel).
        public static readonly UnitStats Infantry = new UnitStats(hp: 100, speed: 300, range: 12000, damage: 8, cooldown: 6);   // 3 m/s, 12 m, 8 dmg per 0.6 s
        public static readonly UnitStats Vehicle = new UnitStats(hp: 220, speed: 700, range: 16000, damage: 40, cooldown: 20);  // 7 m/s, 16 m, 40 dmg per 2 s

        public static UnitStats Of(UnitKind k) => k == UnitKind.Vehicle ? Vehicle : Infantry;

        public static TargetClass ClassOf(UnitKind k) => k == UnitKind.Vehicle ? TargetClass.Vehicle : TargetClass.Infantry;

        /// <summary>Sight of every unit: 20 m.</summary>
        public const int SightMm = 20000;

        /// <summary>Damage percent of an attacker kind against a target class (150 = x1.5).</summary>
        public static int DamagePercent(UnitKind attacker, TargetClass target)
        {
            if (attacker == UnitKind.Infantry) return target == TargetClass.Infantry ? 150 : 50;
            return target == TargetClass.Infantry ? 50 : 150;
        }

        /// <summary>Damage of one shot: base damage times the percent, integer division, at least 1.</summary>
        public static int ShotDamage(UnitKind attacker, TargetClass target)
        {
            int d = Of(attacker).Damage * DamagePercent(attacker, target) / 100;
            return d < 1 ? 1 : d;
        }

        // Buildings.
        public const int SmallHealth = 400;
        public const int MediumHealth = 700;
        public const int HqHealth = 1500;

        public static int HealthOf(BuildingSize s) => s == BuildingSize.Hq ? HqHealth : s == BuildingSize.Medium ? MediumHealth : SmallHealth;

        /// <summary>Radius of a building's footprint in mm: weapon range and sight are measured to its edge.</summary>
        public static int FootprintMm(BuildingSize s) => s == BuildingSize.Hq ? 5000 : s == BuildingSize.Medium ? 3500 : 2000;

        /// <summary>Rubble stays for 30 s.</summary>
        public const int RubbleTicks = 300;

        // Guards.
        public const int MaxGuards = 5;
        /// <summary>A guard stays within 8 m of its anchor: it walks back when further than <see cref="GuardReturnMm"/>.</summary>
        public const int GuardStayMm = 8000;
        public const int GuardReturnMm = 7000;
        /// <summary>A guard slot stands this far from the anchor.</summary>
        public const int GuardSlotMm = 5000;
        /// <summary>A guard chases an attacker only while the attacker is within 15 m of the anchor.</summary>
        public const int GuardChaseMm = 15000;

        /// <summary>Free units wait on a ring this far from their nearest HQ.</summary>
        public const int WaitRingMm = 10000;

        /// <summary>A shot shows for this many ticks.</summary>
        public const int FireFlashTicks = 3;

        // Enemy waves.
        public const int WaveIntervalTicks = 900;       // 90 s
        public const int FirstWaveInfantry = 2;
        public const int FirstVehicleWave = 3;

        /// <summary>Units come out of a flag with a small spread so they do not stack: offsets in mm cycled by unit id.</summary>
        public static readonly int[] SpawnSpreadX = { 0, 1500, -1500, 0, 1500, -1500, 2500, -2500 };
        public static readonly int[] SpawnSpreadZ = { 0, 0, 0, 1500, 1500, 1500, -1000, -1000 };
    }
}
