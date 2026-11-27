namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 5 (MYTHIC 4): the only path that moves a god's Mythic. Same rules for human and AI gods: no code here reads
    /// <see cref="GodRecord.Seat"/>. Modes: <c>Standard</c> (balance, costs, upkeep, cooldowns), <c>Unlimited</c> (nominal cost
    /// recorded with <c>Waived</c>, nothing charged, balance held at max; cooldowns still apply), <c>Disabled</c> (every
    /// divine action refused <c>divinity_disabled</c>, nothing touched). OWNER: D1.
    /// <para>Usage in a command handler: <c>Validate</c> calls <see cref="CanSpend"/>; <c>Apply</c> calls <see cref="TrySpend"/>
    /// with <see cref="Interventions.Peek"/>, applies nothing if it fails, then <see cref="Interventions.Open"/>, then the effect.</para>
    /// </summary>
    public static class Mythic
    {
        public static DivinityMode ModeOf(GodState s) => s.Divinity?.Gods?.Mode ?? s.Tuning.Divinity.Mode;

        /// <summary>Would a spend of <paramref name="costMilli"/> of <paramref name="kind"/> by the god succeed now? Never mutates.</summary>
        public static CommandResult CanSpend(GodState s, int god, InterventionKind kind, long costMilli)
        {
            var reg = s.Divinity?.Gods;
            if (reg == null) return Reject(DivineReasons.DivinityOff);
            var mode = reg.Mode;
            if (mode == DivinityMode.Disabled) return Reject(DivineReasons.DivinityDisabled);
            var g = reg.Of(god);
            if (g == null) return Reject(DivineReasons.NoSuchGod);
            if (costMilli < 0) return Reject(DivineReasons.BadTarget);
            int k = (int)kind;
            if (k > 0 && k < Gods.KindCount && s.Tick < g.CooldownUntil[k]) return Reject(DivineReasons.Cooldown);
            if (mode == DivinityMode.Standard && g.Mythic < costMilli) return Reject(DivineReasons.MythicShort);
            return CommandResult.Accept();
        }

        /// <summary>
        /// Checks mode, balance and cooldown; debits (Standard) or records the nominal cost waived (Unlimited); emits
        /// <see cref="MythicSpent"/>; starts the kind's cooldown. Nothing changes when it refuses.
        /// </summary>
        public static CommandResult TrySpend(GodState s, EventBuffer ev, int god, InterventionKind kind, long costMilli, int intervention)
        {
            var r = CanSpend(s, god, kind, costMilli);
            if (!r.Ok) return r;
            var reg = s.Divinity.Gods;
            var g = reg.Of(god);
            var day = reg.DayOf(god);
            bool waived = reg.Mode == DivinityMode.Unlimited;
            long charged = waived ? 0 : costMilli;
            g.Mythic -= charged;
            if (waived) { g.Mythic = g.MythicMax; day.Waived += costMilli; }
            else day.Spent += charged;
            day.Actions++;
            int k = (int)kind;
            int cd = s.Tuning.Divinity.Mythic.Cooldown.Of(kind);
            if (cd > 0 && k > 0 && k < Gods.KindCount) g.CooldownUntil[k] = s.Tick + cd;
            ev.Add(new MythicSpent { God = god, Intervention = intervention, Kind = kind, Amount = charged, Nominal = costMilli, Waived = waived, Balance = g.Mythic });
            return CommandResult.Accept();
        }

        /// <summary>Sets a god's cooldown of a kind to end at <paramref name="untilTick"/> (D5: the avatar cooldown after a destruction).</summary>
        public static void StartCooldown(GodState s, int god, InterventionKind kind, long untilTick)
        {
            var g = s.Divinity?.Gods?.Of(god);
            int k = (int)kind;
            if (g == null || k <= 0 || k >= Gods.KindCount) return;
            if (untilTick > g.CooldownUntil[k]) g.CooldownUntil[k] = untilTick;
        }

        /// <summary>
        /// One tick of a continuous cost (called by <c>DivinitySystem</c> per open spell, ascending InterventionId). Pays
        /// <c>min(balance, perTick)</c>, aggregated into the day's <see cref="MythicUpkeepCharged"/>. Returns false on a
        /// shortfall: the god could not pay this tick in full, or what is left cannot pay the next tick; the caller then ends
        /// the dependent control this same tick with <c>MythicExhausted</c>. Unlimited records the nominal amount as waived
        /// and never falls short; Disabled charges nothing.
        /// </summary>
        public static bool ChargeUpkeep(GodState s, int god, InterventionKind kind, long perTickMilli)
        {
            var reg = s.Divinity?.Gods;
            var g = reg?.Of(god);
            if (g == null || perTickMilli <= 0 || reg.Mode == DivinityMode.Disabled) return true;
            var day = reg.DayOf(god);
            int k = (int)kind;
            if (k < 0 || k >= Gods.KindCount) k = 0;
            day.UpkeepTicks[k]++;
            if (reg.Mode == DivinityMode.Unlimited) { day.UpkeepWaived[k] += perTickMilli; g.Mythic = g.MythicMax; return true; }
            long pay = g.Mythic < perTickMilli ? g.Mythic : perTickMilli;
            g.Mythic -= pay;
            day.Upkeep[k] += pay;
            return pay == perTickMilli && g.Mythic >= perTickMilli;
        }

        /// <summary>The upkeep rate of a control spell kind (contracts 5; tuning <c>Divinity.Mythic.Upkeep</c>).</summary>
        public static long UpkeepOf(GodState s, ControlKind kind)
        {
            var u = s.Tuning.Divinity.Mythic.Upkeep;
            switch (kind)
            {
                case ControlKind.Compelled: return u.CompulsionPerTick;
                case ControlKind.Possessed: return u.PossessionPerTick;
                case ControlKind.AvatarBody: return u.AvatarPerTick;
                default: return 0;
            }
        }

        /// <summary>The intervention family a control spell kind is charged under.</summary>
        public static InterventionKind KindOf(ControlKind kind)
        {
            switch (kind)
            {
                case ControlKind.Compelled: return InterventionKind.Compulsion;
                case ControlKind.Possessed: return InterventionKind.Possession;
                case ControlKind.AvatarBody: return InterventionKind.Avatar;
                default: return InterventionKind.None;
            }
        }

        /// <summary>Credits a god (capped at its max; the excess is reported, never credited) and emits <see cref="MythicCredited"/>.</summary>
        internal static void Credit(GodState s, EventBuffer ev, GodRecord g, long amount, long day, byte source)
        {
            long room = g.MythicMax - g.Mythic;
            if (room < 0) room = 0;
            long credited = amount < room ? amount : room;
            long capped = amount - credited;
            g.Mythic += credited;
            var d = s.Divinity.Gods.DayOf(g.Id);
            d.Credited += credited; d.Capped += capped;
            ev.Add(new MythicCredited { God = g.Id, Amount = credited, Capped = capped, Balance = g.Mythic, Day = day, Source = source });
        }

        private static CommandResult Reject(string code) => CommandResult.Reject(code, DivineReasons.Sentence(code));
    }

    /// <summary>
    /// D1 test-only command (registered only with <c>Divinity.Mythic.TestCommand = 1</c>; never in a normal game): spends
    /// <see cref="Cost"/> milli-Mythic of <see cref="Kind"/> through <see cref="Mythic.TrySpend"/> and opens an intervention with
    /// no effect. It exercises the whole spend chain before D2-D7 exist (D1 spec 14 D1-T3/T5/T6, the d1 scenarios).
    /// </summary>
    public sealed class DivineTestSpend : DivineCommand
    {
        public InterventionKind Kind;
        public long Cost;
    }

    /// <summary>The handler of <see cref="DivineTestSpend"/>.</summary>
    public sealed class DivineTestSpendHandler : CommandHandler<DivineTestSpend>
    {
        protected override CommandResult Validate(GodState s, DivineTestSpend c)
        {
            if (c.Kind == InterventionKind.None || (int)c.Kind >= Gods.KindCount) return CommandResult.Reject(DivineReasons.BadTarget, DivineReasons.Sentence(DivineReasons.BadTarget));
            return Mythic.CanSpend(s, c.God, c.Kind, c.Cost);
        }

        protected override CommandResult Apply(GodState s, DivineTestSpend c, EventBuffer ev)
        {
            var r = Mythic.TrySpend(s, ev, c.God, c.Kind, c.Cost, Interventions.Peek(s));
            if (!r.Ok) return r;
            Interventions.Open(s, ev, c.God, c.Kind, DivineAct.None, default, DivineScope.None, c.Cost);
            return CommandResult.Accept();
        }
    }
}
