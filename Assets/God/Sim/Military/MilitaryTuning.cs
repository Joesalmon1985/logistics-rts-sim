namespace God.Sim
{
    /// <summary>What a weapon does beyond its numbers (stage 3 combat).</summary>
    public enum WeaponSpecial
    {
        None = 0,
        /// <summary>Sword: +<see cref="MilitaryTuning.ParryBonusPct"/> block when its bearer carries no shield.</summary>
        Parry = 1,
        /// <summary>Spear: ready to strike the moment a melee fight starts (its wait is cut to 0).</summary>
        FirstStrike = 2,
        /// <summary>Axe: halves the target's block chance, and a blocked blow still does <see cref="MilitaryTuning.AxeChipDamage"/>.</summary>
        ShieldBreaker = 3,
        /// <summary>Sling: no combat effect (it is cheap to make).</summary>
        Cheap = 4,
    }

    /// <summary>Stats of one weapon (plan table "Combat: live duels in rounds"). Lengths in mm, times in ticks (10 per second).</summary>
    public readonly struct WeaponStats
    {
        public readonly int Damage;
        public readonly int Pierce;
        public readonly int HitPct;
        /// <summary>Ticks between attacks.</summary>
        public readonly int CooldownTicks;
        public readonly int RangeMm;
        /// <summary>Damage against buildings in percent of <see cref="Damage"/>.</summary>
        public readonly int BuildingPct;
        public readonly WeaponSpecial Special;

        public WeaponStats(int damage, int pierce, int hitPct, int cooldownTicks, int rangeMm, int buildingPct, WeaponSpecial special)
        { Damage = damage; Pierce = pierce; HitPct = hitPct; CooldownTicks = cooldownTicks; RangeMm = rangeMm; BuildingPct = buildingPct; Special = special; }
    }

    /// <summary>Stats of one defence item. Percent values add up over the kit; speed is a percent change.</summary>
    public readonly struct DefenceStats
    {
        public readonly int BlockPct;
        /// <summary>Block chance against bows and slings.</summary>
        public readonly int BlockVsRangedPct;
        public readonly int Armour;
        public readonly int DodgePct;
        public readonly int SpeedPct;
        public readonly bool StopsCrits;

        public DefenceStats(int blockPct, int blockVsRangedPct, int armour, int dodgePct, int speedPct, bool stopsCrits)
        { BlockPct = blockPct; BlockVsRangedPct = blockVsRangedPct; Armour = armour; DodgePct = dodgePct; SpeedPct = speedPct; StopsCrits = stopsCrits; }
    }

    /// <summary>Everything a soldier fights with, derived from its <see cref="Kit"/> by <see cref="MilitaryTuning.Of(Kit)"/>.</summary>
    public readonly struct SoldierStats
    {
        public readonly Ware Weapon;
        public readonly WeaponStats W;
        public readonly int MaxHealth;
        public readonly int SpeedMmPerTick;
        public readonly int BlockPct;
        public readonly int BlockVsRangedPct;
        public readonly int Armour;
        public readonly int DodgePct;
        public readonly bool StopsCrits;
        public readonly bool HasShield;

        public SoldierStats(Ware weapon, WeaponStats w, int maxHealth, int speed, int block, int blockVsRanged, int armour, int dodge, bool stopsCrits, bool hasShield)
        {
            Weapon = weapon; W = w; MaxHealth = maxHealth; SpeedMmPerTick = speed; BlockPct = block; BlockVsRangedPct = blockVsRanged;
            Armour = armour; DodgePct = dodge; StopsCrits = stopsCrits; HasShield = hasShield;
        }

        public int RangeMm => W.RangeMm;
        public int CooldownTicks => W.CooldownTicks;
        public bool Ranged => Gear.IsRanged(Weapon);
        /// <summary>How far it sees enemies: the common sight, or its range when that is longer (a bow).</summary>
        public int SightMm => W.RangeMm > MilitaryTuning.SightMm ? W.RangeMm : MilitaryTuning.SightMm;
    }

    /// <summary>
    /// Tuning constants of the military. Change here only (package Q4 balances them). Positions are integer millimetres on
    /// the ground plane (X, Z as in the node lattice, metres times 1000). Stage 3: no vehicles; every unit is a soldier
    /// whose stats come from its kit.
    /// </summary>
    public static class MilitaryTuning
    {
        // ---------------- gear (plan stage 3 tables) ----------------
        // Weapon:        damage, pierce, hit %, ticks between attacks, range mm, vs buildings %, special
        public static WeaponStats Sword = new WeaponStats(12, 2, 85, 8, 2000, 100, WeaponSpecial.Parry);
        public static WeaponStats Spear = new WeaponStats(10, 3, 80, 10, 3500, 75, WeaponSpecial.FirstStrike);
        public static WeaponStats Axe = new WeaponStats(16, 4, 75, 12, 2000, 200, WeaponSpecial.ShieldBreaker);
        public static WeaponStats Bow = new WeaponStats(9, 1, 65, 14, 25000, 25, WeaponSpecial.None);
        public static WeaponStats Sling = new WeaponStats(7, 0, 60, 9, 18000, 25, WeaponSpecial.Cheap);

        // Defence:              block %, block vs ranged %, armour, dodge %, speed %, stops crits
        public static DefenceStats BigShield = new DefenceStats(35, 55, 0, 0, -10, false);
        public static DefenceStats SmallShield = new DefenceStats(25, 25, 0, 5, 0, false);
        public static DefenceStats Helmet = new DefenceStats(0, 0, 2, 0, 0, true);
        public static DefenceStats Breastplate = new DefenceStats(0, 0, 5, -5, -20, false);
        public static DefenceStats LightArmour = new DefenceStats(0, 0, 2, 10, 0, false);

        /// <summary>Every soldier's health.</summary>
        public static int SoldierHealth = 100;
        /// <summary>Base walking speed: 3 m/s.</summary>
        public static int BaseSpeedMmPerTick = 300;
        /// <summary>Dodge of a soldier with nothing that changes it.</summary>
        public static int BaseDodgePct = 5;
        public static int CritPct = 10;
        public static int CritMultiplier = 2;
        /// <summary>What an axe still does through a block.</summary>
        public static int AxeChipDamage = 3;
        /// <summary>Sword parry: block added when the swordsman carries no shield.</summary>
        public static int ParryBonusPct = 10;
        /// <summary>Shield breaker: the target's block is divided by this.</summary>
        public static int ShieldBreakerDivisor = 2;
        public static int MinDamage = 1;

        /// <summary>Soldiers strike envoys from at least this far (an envoy is no fighter: no rolls, the weapon's damage).</summary>
        public static int EnvoyReachMm = 12000;

        public static WeaponStats WeaponOf(Ware w)
        {
            switch (w)
            {
                case Ware.Spear: return Spear;
                case Ware.Axe: return Axe;
                case Ware.Bow: return Bow;
                case Ware.Sling: return Sling;
                default: return Sword;
            }
        }

        /// <summary>Stats of a defence item; zero for <see cref="Ware.None"/> or anything that is not defence gear.</summary>
        public static DefenceStats DefenceOf(Ware w)
        {
            switch (w)
            {
                case Ware.BigShield: return BigShield;
                case Ware.SmallShield: return SmallShield;
                case Ware.Helmet: return Helmet;
                case Ware.Breastplate: return Breastplate;
                case Ware.LightArmour: return LightArmour;
                default: return default;
            }
        }

        /// <summary>
        /// The kit a soldier actually fights with: no weapon (a stage 2 order) means a sword and nothing else; items in the
        /// wrong slot are dropped; a bow drops its shield.
        /// </summary>
        public static Kit Normalize(Kit k)
        {
            if (!k.IsArmed) return new Kit(Ware.Sword);
            var n = new Kit(k.Weapon,
                Gear.SlotOf(k.Shield) == GearSlot.Shield && Gear.AllowsShield(k.Weapon) ? k.Shield : Ware.None,
                Gear.SlotOf(k.Head) == GearSlot.Head ? k.Head : Ware.None,
                Gear.SlotOf(k.Body) == GearSlot.Body ? k.Body : Ware.None);
            return n;
        }

        /// <summary>Derived stats of a kit (normalised first).</summary>
        public static SoldierStats Of(Kit kit)
        {
            var k = Normalize(kit);
            var w = WeaponOf(k.Weapon);
            var s = DefenceOf(k.Shield); var h = DefenceOf(k.Head); var b = DefenceOf(k.Body);
            int speedPct = 100 + s.SpeedPct + h.SpeedPct + b.SpeedPct;
            int speed = BaseSpeedMmPerTick * (speedPct < 10 ? 10 : speedPct) / 100;
            int dodge = BaseDodgePct + s.DodgePct + h.DodgePct + b.DodgePct;
            return new SoldierStats(k.Weapon, w, SoldierHealth, speed,
                s.BlockPct + h.BlockPct + b.BlockPct, s.BlockVsRangedPct + h.BlockVsRangedPct + b.BlockVsRangedPct,
                s.Armour + h.Armour + b.Armour, dodge < 0 ? 0 : dodge,
                s.StopsCrits || h.StopsCrits || b.StopsCrits, k.Shield != Ware.None);
        }

        /// <summary>Defence pieces a kit carries (0..3), a cheap measure of how well a soldier is kitted.</summary>
        public static int DefencePieces(Kit k)
        {
            var n = Normalize(k);
            return (n.Shield != Ware.None ? 1 : 0) + (n.Head != Ware.None ? 1 : 0) + (n.Body != Ware.None ? 1 : 0);
        }

        /// <summary>Damage of one strike on a building: the weapon's damage times its building percent, at least 1. No rolls.</summary>
        public static int BuildingDamage(Ware weapon)
        {
            var w = WeaponOf(weapon);
            int d = w.Damage * w.BuildingPct / 100;
            return d < MinDamage ? MinDamage : d;
        }

        /// <summary>Sight of every unit: 20 m (a bow sees as far as it shoots).</summary>
        public const int SightMm = 20000;

        // Buildings.
        public const int SmallHealth = 400;
        public const int MediumHealth = 700;
        public const int HqHealth = 1500;
        /// <summary>Health of an HQ upgraded to a Citadel.</summary>
        public const int CitadelHealth = 3000;

        public static int HealthOf(BuildingSize s) => s == BuildingSize.Hq ? HqHealth : s == BuildingSize.Medium ? MediumHealth : SmallHealth;

        public static int HqMaxHealth(bool citadel) => citadel ? CitadelHealth : HqHealth;

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

        /// <summary>A strike shows for this many ticks.</summary>
        public const int FireFlashTicks = 3;

        /// <summary>Units come out of a flag with a small spread so they do not stack: offsets in mm cycled by unit id.</summary>
        public static readonly int[] SpawnSpreadX = { 0, 1500, -1500, 0, 1500, -1500, 2500, -2500 };
        public static readonly int[] SpawnSpreadZ = { 0, 0, 0, 1500, 1500, 1500, -1000, -1000 };
    }
}
