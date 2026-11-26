namespace God.Sim
{
    /// <summary>Life stages by sim age (POPULATION_DESIGN "Time and life stages").</summary>
    public enum LifeStage { Child = 0, Adult = 1, Elder = 2 }

    /// <summary>
    /// Days and years from ticks (conventions section 4). A day is <see cref="TicksPerDay"/> ticks (1200 = 2 minutes at 1x),
    /// a year <see cref="DaysPerYear"/> days (10). Day 0 is the first; tick t lies in day <c>t / TicksPerDay</c>, and the
    /// step that reaches a multiple of <see cref="TicksPerDay"/> ends a day (<see cref="IsDayEnd"/>). <see cref="LifeScale"/>
    /// (percent, 100 = default, higher = faster lives) scales only life-cycle durations: sim age advances at
    /// <c>LifeScale / 100</c> years per calendar year. Integers only. A value type built from the <see cref="GameSetup"/>:
    /// the settings live in the setup (saved and hashed), this only computes.
    /// </summary>
    public readonly struct SimCalendar
    {
        public const int DefaultTicksPerDay = 1200;
        public const int DefaultDaysPerYear = 10;
        public const int DefaultLifeScale = 100;
        /// <summary>The HUD shows a human age of sim years times this (an "age of 16" at sim year 1).</summary>
        public const int HudYearsPerSimYear = 16;
        /// <summary>Life stage boundaries in milli-years of sim age (child before 1.0, elder from 3.0).</summary>
        public const int AdultFromMilli = 1000, ElderFromMilli = 3000;

        public readonly int TicksPerDay;
        public readonly int DaysPerYear;
        public readonly int LifeScale;
        /// <summary>When true, per-day decay rates are scaled by LifeScale too (owner question 1; default false).</summary>
        public readonly bool DecayFollowsLifeScale;

        public SimCalendar(int ticksPerDay, int daysPerYear, int lifeScale, bool decayFollowsLifeScale = false)
        {
            TicksPerDay = ticksPerDay > 0 ? ticksPerDay : DefaultTicksPerDay;
            DaysPerYear = daysPerYear > 0 ? daysPerYear : DefaultDaysPerYear;
            LifeScale = lifeScale > 0 ? lifeScale : DefaultLifeScale;
            DecayFollowsLifeScale = decayFollowsLifeScale;
        }

        /// <summary>The calendar of a game (defaults when the setup is null).</summary>
        public static SimCalendar Of(GameSetup setup) => setup == null
            ? new SimCalendar(DefaultTicksPerDay, DefaultDaysPerYear, DefaultLifeScale)
            : new SimCalendar(setup.TicksPerDay, setup.DaysPerYear, setup.LifeScale, setup.DecayFollowsLifeScale);

        public static SimCalendar Of(GodState s) => Of(s?.Setup);

        public long TicksPerYear => (long)TicksPerDay * DaysPerYear;

        /// <summary>The day a tick lies in (day 0 first).</summary>
        public long Day(long tick) => tick < 0 ? 0 : tick / TicksPerDay;
        /// <summary>The calendar year a tick lies in (year 0 first).</summary>
        public long Year(long tick) => Day(tick) / DaysPerYear;
        /// <summary>Day within its year, 0 to DaysPerYear - 1.</summary>
        public int DayOfYear(long tick) => (int)(Day(tick) % DaysPerYear);
        /// <summary>Ticks into the current day, 0 to TicksPerDay - 1.</summary>
        public int TickOfDay(long tick) => (int)((tick < 0 ? 0 : tick) % TicksPerDay);
        /// <summary>Progress through the current day in permille (0 at dawn).</summary>
        public int DayPermille(long tick) => (int)((long)TickOfDay(tick) * 1000 / TicksPerDay);
        /// <summary>True for the tick whose step ended a day (a positive multiple of TicksPerDay).</summary>
        public bool IsDayEnd(long tick) => tick > 0 && tick % TicksPerDay == 0;
        /// <summary>The first tick of a day.</summary>
        public long DayStart(long day) => day * TicksPerDay;

        /// <summary>
        /// Sim age in milli-years of something born at <paramref name="bornTick"/>, at <paramref name="tick"/>:
        /// <c>(tick - born) * LifeScale / 100 * 1000 / TicksPerYear</c>, computed without losing precision.
        /// </summary>
        public long AgeMilli(long bornTick, long tick)
        {
            long dt = tick - bornTick;
            if (dt <= 0) return 0;
            return dt * LifeScale * 1000L / (100L * TicksPerYear);
        }

        /// <summary>Calendar ticks a sim age takes (the inverse of <see cref="AgeMilli"/>, rounded up).</summary>
        public long TicksForAgeMilli(long ageMilli)
        {
            long num = ageMilli * 100L * TicksPerYear, den = LifeScale * 1000L;
            return (num + den - 1) / den;
        }

        public static LifeStage StageOf(long ageMilli) =>
            ageMilli < AdultFromMilli ? LifeStage.Child : ageMilli < ElderFromMilli ? LifeStage.Adult : LifeStage.Elder;

        public LifeStage StageAt(long bornTick, long tick) => StageOf(AgeMilli(bornTick, tick));

        /// <summary>The human age the HUD shows for a sim age: sim years x 16, rounded down.</summary>
        public static int HudAge(long ageMilli) => (int)(ageMilli * HudYearsPerSimYear / 1000);

        /// <summary>
        /// A per-day decay rate as the rules apply it: unchanged by default; scaled by LifeScale / 100 when the setup asks
        /// forgetting to keep pace with lives (<see cref="DecayFollowsLifeScale"/>). Clamped to 0..1000.
        /// </summary>
        public int DecayPermille(int perDayPermille)
        {
            long v = DecayFollowsLifeScale ? (long)perDayPermille * LifeScale / 100 : perDayPermille;
            return v < 0 ? 0 : v > 1000 ? 1000 : (int)v;
        }

        /// <summary>"Day 3, year 1" (1-based, for the HUD and logs).</summary>
        public string Label(long tick) => "Day " + (DayOfYear(tick) + 1) + ", year " + (Year(tick) + 1);
    }
}
