using God.Sim.Information;
using God.Sim.Policy;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 2 (D5.3-D5.5): the god registry's rules. One god per initial faction, created by
    /// <see cref="DivinityModule.StartGame"/> only (<c>GodId = (int)faction + 1</c>), never removed. The seat is sim state
    /// (<see cref="GodRecord.Seat"/>); the authorship gate asks <see cref="DivinityModule.HoldsHumanSeat"/>. OWNER: D1.
    /// </summary>
    public static class Gods
    {
        /// <summary>Length of the per-kind arrays (<see cref="InterventionKind"/> 0..7).</summary>
        public const int KindCount = 8;

        /// <summary>The registry of a game (null without divinity state).</summary>
        public static GodRegistry Of(GodState s) => s.Divinity?.Gods;

        /// <summary>A god's record, or null.</summary>
        public static GodRecord Get(GodState s, int god) => s.Divinity?.Gods?.Of(god);

        /// <summary>The god holding the <c>Human</c> seat now (0: none, watch). Before the registry exists: <c>GameSetup.HumanGod</c>.</summary>
        public static int HumanSeatHolder(GodState s)
        {
            var reg = s?.Divinity?.Gods;
            if (reg == null) return s?.Setup?.HumanGod ?? 0;
            foreach (var g in reg.Gods) if (g.Seat == GodSeat.Human) return g.Id;
            return 0;
        }

        /// <summary>
        /// Sandbox purpose for the configuration commands: the saved setup has a <c>sandbox</c> victory (deterministic, so a
        /// replay or a loaded save answers the same; the host's non-saved <c>GameSetup.Purpose</c> is not consulted).
        /// </summary>
        public static bool IsSandbox(GodState s)
        {
            if (s.Setup?.Victory == null) return false;
            foreach (var v in s.Setup.Victory) if (v.Kind == VictoryKind.Sandbox) return true;
            return false;
        }

        /// <summary>Builds the registry (StartGame only): every faction of the setup in ascending order.</summary>
        internal static GodRegistry Create(GodState s)
        {
            var mt = s.Tuning.Divinity.Mythic;
            var reg = new GodRegistry { Mode = s.Tuning.Divinity.Mode, DayDone = SimCalendar.Of(s).Day(s.Tick) };
            long max = mt.Max < 0 ? 0 : mt.Max;
            long start = reg.Mode == DivinityMode.Unlimited ? max : (mt.Start < 0 ? 0 : (mt.Start > max ? max : mt.Start));
            int human = s.Setup?.HumanGod ?? 0;
            foreach (var f in s.Setup.Factions)
            {
                int id = DivinityIds.GodOf(f);
                reg.Gods.Add(new GodRecord
                {
                    Id = id, Name = Names.God(s.Seed, id), OriginFaction = f, Seat = id == human ? GodSeat.Human : GodSeat.Ai,
                    PolicyName = GodPolicies.Idle, Mythic = start, MythicMax = max, CreatedTick = s.Tick,
                });
                reg.Days.Add(new GodDay());
            }
            return reg;
        }

        internal static CommandResult Reject(string code) => CommandResult.Reject(code, DivineReasons.Sentence(code));

        internal static GodPolicyChanged Changed(GodRecord g, GodSeat prevSeat, string prevPolicy, int by) => new GodPolicyChanged
        {
            God = g.Id, Seat = g.Seat, PreviousSeat = prevSeat, Policy = g.PolicyName, PreviousPolicy = prevPolicy,
            Profile = g.Profile?.Default ?? DivinePerception.Omniscient, By = by,
        };

        /// <summary>Registers the configuration handlers (and the test command when its switch is on).</summary>
        internal static void Install(GodSim sim)
        {
            sim.RegisterHandler(new SwitchGodHandler());
            sim.RegisterHandler(new SetGodPolicyHandler());
            sim.RegisterHandler(new SetMythicModeHandler());
            sim.RegisterHandler(new SetDivineProfileHandler());
            if (sim.State.Tuning.Divinity.Mythic.TestCommand == 1) sim.RegisterHandler(new DivineTestSpendHandler());
        }
    }

    // ---------------- configuration commands (contracts 10.2; external only, sandbox only; not affected by the mode) ----------------

    /// <summary><c>SwitchGod { From, To }</c>: moves the Human seat from the issuing god (<c>God == From</c>) to <c>To</c> (0: watch). Only the two seats change.</summary>
    public sealed class SwitchGodHandler : CommandHandler<SwitchGod>
    {
        protected override CommandResult Validate(GodState s, SwitchGod c)
        {
            var reg = Gods.Of(s);
            if (reg == null) return Gods.Reject(DivineReasons.DivinityOff);
            if (!Gods.IsSandbox(s)) return Gods.Reject(DivineReasons.NotSandbox);
            var from = reg.Of(c.From);
            if (from == null) return Gods.Reject(DivineReasons.NoSuchGod);
            if (c.From != c.God || from.Seat != GodSeat.Human) return Gods.Reject(DivineReasons.NotYourGod);
            if (c.To != 0 && reg.Of(c.To) == null) return Gods.Reject(DivineReasons.NoSuchGod);
            if (c.To == c.From) return Gods.Reject(DivineReasons.BadTarget);
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, SwitchGod c, EventBuffer ev)
        {
            var reg = Gods.Of(s);
            var from = reg.Of(c.From);
            from.Seat = GodSeat.Ai;
            ev.Add(Gods.Changed(from, GodSeat.Human, from.PolicyName, c.God));
            var to = reg.Of(c.To);
            if (to != null)
            {
                var prev = to.Seat;
                to.Seat = GodSeat.Human;
                ev.Add(Gods.Changed(to, prev, to.PolicyName, c.God));
            }
            return CommandResult.Accept();
        }
    }

    /// <summary><c>SetGodPolicy { God, Policy }</c>: the policy the god director seats the issuing god with while it is AI-seated.</summary>
    public sealed class SetGodPolicyHandler : CommandHandler<SetGodPolicy>
    {
        protected override CommandResult Validate(GodState s, SetGodPolicy c)
        {
            var reg = Gods.Of(s);
            if (reg == null) return Gods.Reject(DivineReasons.DivinityOff);
            if (!Gods.IsSandbox(s)) return Gods.Reject(DivineReasons.NotSandbox);
            if (reg.Of(c.God) == null) return Gods.Reject(DivineReasons.NoSuchGod);
            if (!GodPolicies.IsKnown(c.Policy)) return Gods.Reject(DivineReasons.BadTarget);
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, SetGodPolicy c, EventBuffer ev)
        {
            var g = Gods.Get(s, c.God);
            string prev = g.PolicyName;
            g.PolicyName = c.Policy;
            ev.Add(Gods.Changed(g, g.Seat, prev, c.God));
            return CommandResult.Accept();
        }
    }

    /// <summary><c>SetMythicMode { Mode }</c>: the game's Mythic mode. Entering <c>Unlimited</c> tops every god up to max (credited, source 1).</summary>
    public sealed class SetMythicModeHandler : CommandHandler<SetMythicMode>
    {
        protected override CommandResult Validate(GodState s, SetMythicMode c)
        {
            if (Gods.Of(s) == null) return Gods.Reject(DivineReasons.DivinityOff);
            if (!Gods.IsSandbox(s)) return Gods.Reject(DivineReasons.NotSandbox);
            if (c.Mode != DivinityMode.Standard && c.Mode != DivinityMode.Unlimited && c.Mode != DivinityMode.Disabled) return Gods.Reject(DivineReasons.BadTarget);
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, SetMythicMode c, EventBuffer ev)
        {
            var reg = Gods.Of(s);
            var prev = reg.Mode;
            reg.Mode = c.Mode;
            ev.Add(new MythicConfigChanged { Mode = c.Mode, Previous = prev, By = c.God });
            if (c.Mode == DivinityMode.Unlimited && prev != DivinityMode.Unlimited)
            {
                long day = SimCalendar.Of(s).Day(s.Tick);
                foreach (var g in reg.Gods) if (g.Mythic < g.MythicMax) Mythic.Credit(s, ev, g, g.MythicMax - g.Mythic, day, 1);
            }
            return CommandResult.Accept();
        }
    }

    /// <summary><c>SetDivineProfile { God, Profile }</c>: only <c>Omniscient</c> is accepted in this run (others <c>act_unavailable</c>).</summary>
    public sealed class SetDivineProfileHandler : CommandHandler<SetDivineProfile>
    {
        protected override CommandResult Validate(GodState s, SetDivineProfile c)
        {
            var reg = Gods.Of(s);
            if (reg == null) return Gods.Reject(DivineReasons.DivinityOff);
            if (!Gods.IsSandbox(s)) return Gods.Reject(DivineReasons.NotSandbox);
            if (reg.Of(c.God) == null) return Gods.Reject(DivineReasons.NoSuchGod);
            if (c.Profile == null) return Gods.Reject(DivineReasons.BadTarget);
            if (c.Profile.Default != DivinePerception.Omniscient) return Gods.Reject(DivineReasons.ActUnavailable);
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, SetDivineProfile c, EventBuffer ev)
        {
            var g = Gods.Get(s, c.God);
            g.Profile = new DivineObservationProfile { Default = c.Profile.Default, Channels = c.Profile.Channels };
            ev.Add(Gods.Changed(g, g.Seat, g.PolicyName, c.God));
            return CommandResult.Accept();
        }
    }
}
