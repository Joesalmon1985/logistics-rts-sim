using System.Collections.Generic;
using God.Sim;
using God.Sim.Divinity;

namespace God.Game
{
    /// <summary>The families of the divine toolbar (the glyph and colour language of VISUAL_WORLD 9.1: one colour per family).</summary>
    public enum SlotFamily { Inspect, Suggest, Command, Compel, Miracle, Commandment }

    /// <summary>
    /// One slot of the toolbar (data, so later packages add one without touching the drawing code: I6, U2b). The act and arguments are
    /// the <see cref="DivineIntent"/> the slot sends; nothing here says whether it is allowed (the gate does).
    /// </summary>
    public sealed class PowerSlot
    {
        public string Id, Label, Glyph, Tip;
        public SlotFamily Family;
        /// <summary>The <see cref="DivineActs"/> name ("" for Inspect).</summary>
        public string Act;
        public int Arg, Arg2;
        /// <summary>Colour as 0xRRGGBB.</summary>
        public int Colour;
        public InterventionKind Kind;
        /// <summary>Miracles act the moment the target is clicked; suggestions, commands, compulsions and laws open a form.</summary>
        public bool Immediate;
        /// <summary>Hide the slot when the Mythic mode is Disabled (every divine power).</summary>
        public bool NeedsPower = true;
    }

    /// <summary>One act the instruct popover offers.</summary>
    public sealed class ActChoice
    {
        public DivineAct Act;
        public string Label;
    }

    /// <summary>A scope the popover offers.</summary>
    public sealed class ScopeChoice
    {
        public TargetScope Scope;
        public string Label;
    }

    /// <summary>
    /// The HUD-side catalogue of divine powers (R13): what the toolbar and popover list. It is built from the command vocabulary
    /// and asks <c>Gate.Check</c> (through the builder) whether a power is possible; there is no sim-side catalogue and no rule here.
    /// Costs shown are read from the sim's tuning table (<see cref="MythicCosts"/>) and are estimates until the spend event reports the real figure.
    /// </summary>
    public static class DivineCatalog
    {
        public const int ColSuggest = 0x6FB3E8, ColCommand = 0xE8B53A, ColCompel = 0x9A6FD8, ColHeal = 0x5CC46A, ColSmite = 0xD9553F, ColCreate = 0xF2EAD0, ColBless = 0xF5D76E, ColInspect = 0xB8C0C8, ColLaw = 0xD9A066;

        /// <summary>Default expiry in days of an instruction: suggestion 1, command 2; a compulsion lasts 600 ticks (spec U2 6).</summary>
        public const int SuggestDays = 1, CommandDays = 2, CompelTicks = 600;

        /// <summary>Default magnitudes of the miracle slots (the slot sends them; the gate and the handler decide).</summary>
        public const int HealMagnitude = 400, SmiteMagnitude = 400, CreateCount = 3, DestroyCount = 2;

        public static readonly PowerSlot[] Slots =
        {
            new PowerSlot { Id = "inspect", Label = "Inspect", Glyph = "?", Tip = "Look at a person: click someone to open their card.", Family = SlotFamily.Inspect, Act = "", Colour = ColInspect, NeedsPower = false, Immediate = true },
            new PowerSlot { Id = "suggest", Label = "Suggest", Glyph = "~", Tip = "Suggest an act. They hear it, weigh it with their own mind, and may refuse.", Family = SlotFamily.Suggest, Act = DivineActs.Suggest, Colour = ColSuggest, Kind = InterventionKind.Suggestion },
            new PowerSlot { Id = "command", Label = "Command", Glyph = "!", Tip = "Command an act. A stronger voice, still their own choice.", Family = SlotFamily.Command, Act = DivineActs.Command, Colour = ColCommand, Kind = InterventionKind.Command },
            new PowerSlot { Id = "compel", Label = "Compel", Glyph = "#", Tip = "Compel one person to do one possible act now. Costs Mythic while it lasts.", Family = SlotFamily.Compel, Act = DivineActs.Compel, Colour = ColCompel, Kind = InterventionKind.Compulsion },
            Miracle("heal", "Heal", "+", MiracleKind.HealInjury, HealMagnitude, ColHeal, "Heal a person's injury."),
            Miracle("cure", "Cure", "c", MiracleKind.HealDisease, 0, ColHeal, "Cure a person's illness."),
            Miracle("bless", "Bless", "*", MiracleKind.Bless, 0, ColBless, "Bless a person: they recover faster."),
            Miracle("protect", "Protect", "o", MiracleKind.Protect, 0, ColBless, "Protect a person: they take less harm."),
            Miracle("smite", "Smite", "x", MiracleKind.SmitePerson, SmiteMagnitude, ColSmite, "Smite a person: injury, perhaps death."),
            Miracle("smite_hut", "Smite hut", "X", MiracleKind.SmiteBuilding, 0, ColSmite, "Destroy a hut or building."),
            Miracle("smite_source", "Blight", "v", MiracleKind.SmiteSource, 0, ColSmite, "Blight a wild source of food."),
            Miracle("create", "Create", "^", MiracleKind.CreateResource, CreateCount, ColCreate, "Create food on the ground at a place."),
            Miracle("destroy", "Destroy", "_", MiracleKind.DestroyResource, DestroyCount, ColSmite, "Destroy goods lying at a place."),
            new PowerSlot { Id = "law", Label = "Law", Glyph = "L", Tip = "Reveal a commandment from a template: pick it, fill its slots, reveal.", Family = SlotFamily.Commandment, Act = DivineActs.Reveal, Colour = ColLaw, Kind = InterventionKind.Commandment },
        };

        private static PowerSlot Miracle(string id, string label, string glyph, MiracleKind k, int mag, int colour, string tip) => new PowerSlot
        {
            Id = id, Label = label, Glyph = glyph, Tip = tip, Family = SlotFamily.Miracle, Act = DivineActs.Miracle, Arg = (int)k, Arg2 = mag, Colour = colour, Kind = InterventionKind.Miracle, Immediate = true,
        };

        /// <summary>The acts a suggestion or command may carry (the Tier 1 subset; each is greyed when the gate says <c>act_unavailable</c>).</summary>
        public static readonly ActChoice[] InstructActs =
        {
            new ActChoice { Act = DivineAct.Gather, Label = "Gather" }, new ActChoice { Act = DivineAct.Eat, Label = "Eat" },
            new ActChoice { Act = DivineAct.Sleep, Label = "Sleep" }, new ActChoice { Act = DivineAct.Work, Label = "Work" },
            new ActChoice { Act = DivineAct.Construct, Label = "Build" }, new ActChoice { Act = DivineAct.Cooperate, Label = "Cooperate" },
            new ActChoice { Act = DivineAct.Communicate, Label = "Talk" }, new ActChoice { Act = DivineAct.Treat, Label = "Tend" },
            new ActChoice { Act = DivineAct.Protect, Label = "Protect" }, new ActChoice { Act = DivineAct.Retreat, Label = "Retreat" },
            new ActChoice { Act = DivineAct.Teach, Label = "Teach" }, new ActChoice { Act = DivineAct.Trade, Label = "Trade" },
        };

        /// <summary>The acts a compulsion may force: a physical act the body can do now.</summary>
        public static readonly ActChoice[] CompelActs =
        {
            new ActChoice { Act = DivineAct.Gather, Label = "Gather" }, new ActChoice { Act = DivineAct.Eat, Label = "Eat" },
            new ActChoice { Act = DivineAct.Sleep, Label = "Sleep" }, new ActChoice { Act = DivineAct.Work, Label = "Work" },
            new ActChoice { Act = DivineAct.Take, Label = "Take" }, new ActChoice { Act = DivineAct.Drop, Label = "Drop" },
            new ActChoice { Act = DivineAct.Give, Label = "Give" }, new ActChoice { Act = DivineAct.Retreat, Label = "Retreat" },
        };

        public static readonly ScopeChoice[] Scopes =
        {
            new ScopeChoice { Scope = TargetScope.Person, Label = "Person" }, new ScopeChoice { Scope = TargetScope.Group, Label = "Group" },
            new ScopeChoice { Scope = TargetScope.Household, Label = "Household" }, new ScopeChoice { Scope = TargetScope.Area, Label = "Area" },
            new ScopeChoice { Scope = TargetScope.Location, Label = "Place" },
        };

        public static PowerSlot Find(string id) { foreach (var s in Slots) if (s.Id == id) return s; return null; }

        /// <summary>True when the slot is an instruction (suggest or command) or a compulsion: the popover.</summary>
        public static bool IsInstruct(PowerSlot s) => s != null && (s.Family == SlotFamily.Suggest || s.Family == SlotFamily.Command || s.Family == SlotFamily.Compel);

        /// <summary>The slots shown for a Mythic mode: Disabled hides every power slot (inspect stays).</summary>
        public static void Visible(DivinityMode mode, List<PowerSlot> into)
        {
            into.Clear();
            foreach (var s in Slots) if (!(s.NeedsPower && mode == DivinityMode.Disabled)) into.Add(s);
        }

        /// <summary>Default expiry in ticks for a family.</summary>
        public static int DefaultExpiry(SlotFamily f, int ticksPerDay) =>
            f == SlotFamily.Suggest ? SuggestDays * ticksPerDay : f == SlotFamily.Command ? CommandDays * ticksPerDay : f == SlotFamily.Compel ? CompelTicks : 0;

        /// <summary>The intent a slot sends (the popover overrides act, scope and expiry).</summary>
        public static DivineIntent Intent(PowerSlot s, DivineAct act, int expiry) =>
            s.Family == SlotFamily.Miracle ? new DivineIntent(s.Act, s.Arg, s.Arg2) : new DivineIntent(s.Act, (int)act, expiry);

        /// <summary>
        /// The estimated Mythic cost of a slot in milli-Mythic for this many recipients, from the sim's tuning table. An estimate:
        /// the figure that moved the balance comes from the spend event.
        /// </summary>
        public static long CostOf(MythicCosts c, PowerSlot s, int recipients = 1)
        {
            if (c == null || s == null) return 0;
            int n = recipients < 1 ? 1 : recipients;
            switch (s.Family)
            {
                case SlotFamily.Suggest: return c.IssueSuggestion + (n - 1) * c.SuggestionPerExtra;
                case SlotFamily.Command: return c.IssueCommand + (n - 1) * c.CommandPerExtra;
                case SlotFamily.Compel: return c.IssueCompulsion;
                case SlotFamily.Commandment: return c.RevealCommandment;
                case SlotFamily.Miracle:
                    switch ((MiracleKind)s.Arg)
                    {
                        case MiracleKind.HealInjury: return System.Math.Max(c.HealInjuryMin, c.HealInjuryPer100 * s.Arg2 / 100);
                        case MiracleKind.HealDisease: return c.HealDisease;
                        case MiracleKind.Bless: return c.Bless;
                        case MiracleKind.Protect: return c.Protect;
                        case MiracleKind.SmitePerson: return System.Math.Max(c.SmitePersonMin, c.SmitePersonPer100 * s.Arg2 / 100);
                        case MiracleKind.SmiteBuilding: return c.SmiteBuilding;
                        case MiracleKind.SmiteSource: return c.SmiteSource;
                        case MiracleKind.CreateResource: return c.CreatePerUnit * s.Arg2;
                        case MiracleKind.DestroyResource: return c.DestroyPerUnit * s.Arg2;
                        case MiracleKind.Consecrate: return c.Consecrate;
                    }
                    return 0;
                default: return 0;
            }
        }

        /// <summary>What the gate says about one power on one request: valid or the refusal (R13: the answer is the gate's).</summary>
        public readonly struct SlotCheck
        {
            public readonly bool Valid;
            public readonly string Reason, Message;
            public SlotCheck(bool valid, string reason, string message) { Valid = valid; Reason = reason; Message = message; }
        }

        /// <summary>Checks a request through the builder (the gate). A group is checked person by person; the first refusal is reported.</summary>
        public static SlotCheck Check(PlayerCommandBuilder b, DivineRequest r)
        {
            if (b == null || r == null) return new SlotCheck(false, "no_selection", "Select someone or somewhere first.");
            foreach (var one in PlayerCommandBuilder.Expand(r))
            {
                var res = b.Check(one);
                if (!res.Ok) return new SlotCheck(false, res.Reason, res.Message);
            }
            return new SlotCheck(true, null, null);
        }

        /// <summary>True for the refusals that mean "this power is not implemented or not possible at all" (greyed, not merely not now).</summary>
        public static bool IsUnavailable(string reason) => reason == DivineReasons.ActUnavailable || reason == Reasons.Disabled || reason == DivineReasons.DivinityDisabled || reason == DivineReasons.DivinityOff;
    }
}
