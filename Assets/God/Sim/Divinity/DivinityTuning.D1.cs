namespace God.Sim.Divinity
{
    /// <summary>
    /// D1's numbers, <c>Divinity.Mythic</c> (contracts 5; MYTHIC 4.5). All amounts are milli-Mythic (1 Mythic = 1000).
    /// Contracts 20.F1.1 (LEAD_RULINGS R55): the tuning root is not partial, so D1's costs, upkeep, cooldowns, the god
    /// think interval and the scripted schedule live inside this block (<c>Divinity.Mythic.Costs.*</c>,
    /// <c>.Upkeep.*</c>, <c>.Cooldown.*</c>, <c>.GodThinkTicks</c>, <c>.Schedule.*</c>). Balancing is out of scope (brief 7):
    /// the defaults only let a 100-Mythic god do a few things a day. OWNER: D1.
    /// </summary>
    public sealed partial class MythicTuning
    {
        /// <summary>Balance of every god at the start (Standard; Unlimited starts at <see cref="Max"/>).</summary>
        public long Start = 50_000;
        public long Max = 100_000;
        /// <summary>Credited at every day start after day 0, capped at the god's max (MYTHIC 4.2).</summary>
        public long RegenPerDay = 20_000;
        /// <summary>The god director's think interval (contracts 12 <c>Divinity.AiThinkTicks</c>), staggered by god id.</summary>
        public int GodThinkTicks = 20;
        /// <summary>1 registers the test-only command <see cref="DivineTestSpend"/> (tests and the d1 scenarios only).</summary>
        public int TestCommand;
        public MythicCosts Costs = new MythicCosts();
        public MythicUpkeep Upkeep = new MythicUpkeep();
        public MythicCooldowns Cooldown = new MythicCooldowns();
        /// <summary>The fixed schedule a <c>ScheduleGod</c> issues (scenario tuning: integers only, so a fixed set of slots).</summary>
        public GodSchedule Schedule = new GodSchedule();
    }

    /// <summary><c>Divinity.Mythic.Costs</c> (MYTHIC 4.5, milli-Mythic). Read by the command packages D2-D7.</summary>
    public sealed class MythicCosts
    {
        public long IssueSuggestion = 1_000, SuggestionPerExtra = 500;
        public long IssueCommand = 4_000, CommandPerExtra = 2_000;
        public long IssueCompulsion = 10_000;
        public long BeginPossession = 15_000;
        public long ManifestAvatar = 20_000;
        public long RevealCommandment = 25_000, ReviseCommandment = 10_000, RevokeCommandment = 5_000;
        /// <summary>HealInjury: per 100 permille healed, minimum.</summary>
        public long HealInjuryPer100 = 1_000, HealInjuryMin = 3_000;
        public long HealDisease = 12_000;
        public long Bless = 15_000, Protect = 15_000;
        /// <summary>SmitePerson: per 100 permille of injury, minimum.</summary>
        public long SmitePersonPer100 = 2_000, SmitePersonMin = 4_000;
        public long SmiteBuilding = 30_000, SmiteSource = 10_000;
        /// <summary>CreateResource per unit: food or raw, tool.</summary>
        public long CreatePerUnit = 1_000, CreateToolPerUnit = 5_000;
        public long DestroyPerUnit = 500;
        public long RevealKnowledge = 8_000;
        public long Consecrate = 40_000;
    }

    /// <summary><c>Divinity.Mythic.Upkeep</c>: continuous costs per tick (milli-Mythic), charged by <c>DivinitySystem</c>.</summary>
    public sealed class MythicUpkeep
    {
        public long CompulsionPerTick = 10;
        public long PossessionPerTick = 50;
        public long AvatarPerTick = 20;
    }

    /// <summary><c>Divinity.Mythic.Cooldown</c>: ticks after a spend of a kind before the next of that kind (0: none).</summary>
    public sealed class MythicCooldowns
    {
        public int SuggestionTicks, CommandTicks, CompulsionTicks, PossessionTicks, CommandmentTicks, MiracleTicks, AvatarTicks;
        /// <summary>D5: the avatar cooldown after a destruction (MYTHIC 4.5).</summary>
        public int AvatarAfterDestructionTicks = 600;

        public int Of(InterventionKind k)
        {
            switch (k)
            {
                case InterventionKind.Suggestion: return SuggestionTicks;
                case InterventionKind.Command: return CommandTicks;
                case InterventionKind.Compulsion: return CompulsionTicks;
                case InterventionKind.Possession: return PossessionTicks;
                case InterventionKind.Commandment: return CommandmentTicks;
                case InterventionKind.Miracle: return MiracleTicks;
                case InterventionKind.Avatar: return AvatarTicks;
                default: return 0;
            }
        }
    }

    /// <summary>What a schedule slot issues (a command template; the god is always the ScheduleGod's own).</summary>
    public enum ScheduleTemplate : byte
    {
        None = 0,
        /// <summary><see cref="DivineTestSpend"/> { Kind = A, Cost = B milli } (needs <c>Mythic.TestCommand = 1</c>).</summary>
        TestSpend = 1,
        /// <summary><c>IssueSuggestion</c> { Act = A, Target = Npc B, Scope Individual, ExpireTicks = C } (D2).</summary>
        Suggestion = 2,
        /// <summary><c>IssueCommand</c> { Act = A, Target = Npc B, Scope Individual, ExpireTicks = C } (D2).</summary>
        Command = 3,
        /// <summary><c>CastMiracle</c> { Kind = A, Target = Npc B, Magnitude = C } (D3).</summary>
        Miracle = 4,
        /// <summary><c>IssueCompulsion</c> { Act = A, Target = Npc B, ExpireTicks = C } (D2).</summary>
        Compulsion = 5,
        /// <summary><c>BeginPossession</c> { Npc = B } (D4).</summary>
        Possess = 6,
        /// <summary><c>EndPossession</c> (D4).</summary>
        EndPossession = 7,
        /// <summary>D6 (flagged additive edit): <c>RevealCommandment</c> { Template = A (its rules and default importance), Recipients = Npc B with Radius C }.</summary>
        RevealCommandment = 8,
        /// <summary>D6: <c>ReviseCommandment</c> { Commandment = A, Rules = template B's, Importance = C (0: the template's) }.</summary>
        ReviseCommandment = 9,
        /// <summary>D6: <c>RevokeCommandment</c> { Commandment = A }.</summary>
        RevokeCommandment = 10,
    }

    /// <summary>
    /// One schedule slot: from <see cref="Tick"/> (0: unused), for god <see cref="God"/> (0: every ScheduleGod), the
    /// template with its arguments; repeated every <see cref="Every"/> ticks (0: once) up to <see cref="Until"/> (0: no end).
    /// </summary>
    public sealed class ScheduleSlot
    {
        public long Tick;
        public int Every;
        public long Until;
        public int God;
        public ScheduleTemplate Template;
        public int A, B, C;

        /// <summary>True when the slot fires at tick <paramref name="t"/>.</summary>
        public bool FiresAt(long t)
        {
            if (Tick <= 0 || Template == ScheduleTemplate.None || t < Tick || (Until > 0 && t > Until)) return false;
            return Every <= 0 ? t == Tick : (t - Tick) % Every == 0;
        }
    }

    /// <summary><c>Divinity.Mythic.Schedule</c>: eight fixed slots (scenario files set them by path, e.g. <c>Divinity.Mythic.Schedule.S1.Tick</c>).</summary>
    public sealed class GodSchedule
    {
        public ScheduleSlot S1 = new ScheduleSlot(), S2 = new ScheduleSlot(), S3 = new ScheduleSlot(), S4 = new ScheduleSlot(),
            S5 = new ScheduleSlot(), S6 = new ScheduleSlot(), S7 = new ScheduleSlot(), S8 = new ScheduleSlot();

        public ScheduleSlot[] Slots() => new[] { S1, S2, S3, S4, S5, S6, S7, S8 };
    }
}
