using God.Sim.Learning;

namespace God.Sim.Divinity
{
    /// <summary>One row of <see cref="CommandmentTemplates"/>: id (T-number), name for the picker, its rules, default importance, and whether every verb and condition it uses exists on this branch.</summary>
    public sealed class CommandmentTemplate
    {
        public int Id;
        public string Name;
        public CommandmentRule[] Rules;
        public int Importance;
        /// <summary>1 Tier 1 (works now), 2 Tier 2, 0 data only: matches nothing until <see cref="WaitsFor"/> lands.</summary>
        public int Tier;
        public string WaitsFor;
    }

    /// <summary>
    /// COMMANDMENTS 4.9 (brief 12 examples; LEAD_RULINGS R5): the template table. Tier 1 set T1, T3, T4, T6, T7, T12; T11 is
    /// Tier 2 (its warn part works); T2, T5, T8-T10 are data only (their verbs or conditions belong to later packages and match
    /// nothing until then). Pure static data: the template picker (U2a) and the builder (U2b) read it, a god's command carries
    /// the rules. Learning-verb ids are the contracts' (M4 Feed 32, Tend 38; M5 Give 48, HandOver 49, Say(act) 64 + act; C7
    /// Teach 108). OWNER: D6.
    /// </summary>
    public static class CommandmentTemplates
    {
        public const byte Feed = 32, Tend = 38, Give = 48, HandOver = 49, SayCooperate = 71, SayWarn = 74, SayForgive = 79, Teach = 108;

        private static CommandmentRule R(Modality m, byte verb, ApplySelector who, ConditionKind when, ConditionKind also = ConditionKind.Always, byte cls = 0, Ware ware = Ware.None) =>
            new CommandmentRule { Modality = m, LearningVerb = verb, Applies = who, Condition = when, ConditionParam = (int)also, ObjClass = cls, Ware = ware };

        public static readonly ReadOnlyArray<CommandmentTemplate> All = new ReadOnlyArray<CommandmentTemplate>(
            new CommandmentTemplate { Id = 1, Name = "Share surplus food with hungry children", Tier = 1, Importance = 3, Rules = new[] {
                R(Modality.Encouraged, Feed, ApplySelector.Adults, ConditionKind.TargetBegging),
                R(Modality.Encouraged, Give, ApplySelector.Adults, ConditionKind.TargetIsChild, ConditionKind.CarryingFood) } },
            new CommandmentTemplate { Id = 2, Name = "Do not steal", Tier = 0, Importance = 4, WaitsFor = "C1 (unpermitted takes)", Rules = new[] {
                R(Modality.Prohibited, Verb.PickUp, ApplySelector.Everyone, ConditionKind.NotPermitted),
                R(Modality.Prohibited, Verb.TakeFromStore, ApplySelector.Everyone, ConditionKind.NotPermitted) } },
            new CommandmentTemplate { Id = 3, Name = "Honour agreements", Tier = 1, Importance = 3, Rules = new[] {
                R(Modality.Required, HandOver, ApplySelector.Everyone, ConditionKind.IOwe) } },
            new CommandmentTemplate { Id = 4, Name = "Protect forests", Tier = 1, Importance = 3, Rules = new[] {
                R(Modality.Prohibited, Verb.Gather, ApplySelector.Everyone, ConditionKind.StockLow, cls: (byte)ObjClass.WoodStand) } },
            new CommandmentTemplate { Id = 5, Name = "Teach children", Tier = 0, Importance = 2, WaitsFor = "C7 (Teach)", Rules = new[] {
                R(Modality.Encouraged, Teach, ApplySelector.Adults, ConditionKind.TargetIsChild) } },
            new CommandmentTemplate { Id = 6, Name = "Assist strangers", Tier = 1, Importance = 3, Rules = new[] {
                R(Modality.Encouraged, Give, ApplySelector.Everyone, ConditionKind.TargetIsStranger),
                R(Modality.Encouraged, SayCooperate, ApplySelector.Everyone, ConditionKind.TargetIsStranger) } },
            new CommandmentTemplate { Id = 7, Name = "Care for elderly people", Tier = 1, Importance = 3, Rules = new[] {
                R(Modality.Encouraged, Tend, ApplySelector.Adults, ConditionKind.TargetIsElder),
                R(Modality.Encouraged, Feed, ApplySelector.Adults, ConditionKind.TargetIsElder) } },
            new CommandmentTemplate { Id = 8, Name = "Forgive those who repent", Tier = 0, Importance = 2, WaitsFor = "C2 (M7 apologies)", Rules = new[] {
                R(Modality.Encouraged, SayForgive, ApplySelector.Everyone, ConditionKind.TargetApologised) } },
            new CommandmentTemplate { Id = 9, Name = "Protect communal property", Tier = 0, Importance = 3, WaitsFor = "C1, C8 (communal stores)", Rules = new[] {
                R(Modality.Prohibited, Verb.TakeFromStore, ApplySelector.Everyone, ConditionKind.NotPermitted) } },
            new CommandmentTemplate { Id = 10, Name = "Contribute to public services", Tier = 0, Importance = 2, WaitsFor = "C8 (public stores)", Rules = new[] {
                R(Modality.Encouraged, Verb.Store, ApplySelector.Adults, ConditionKind.HouseholdSurplus) } },
            new CommandmentTemplate { Id = 11, Name = "Defend your community", Tier = 2, Importance = 3, WaitsFor = "C4 (attack); the warn part works", Rules = new[] {
                R(Modality.Encouraged, SayWarn, ApplySelector.Adults, ConditionKind.EnemyInSight) } },
            new CommandmentTemplate { Id = 12, Name = "Treat injured individuals", Tier = 1, Importance = 3, Rules = new[] {
                R(Modality.Encouraged, Tend, ApplySelector.Adults, ConditionKind.TargetInjured) } });

        /// <summary>The template with this id (null when none).</summary>
        public static CommandmentTemplate Get(int id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].Id == id) return All[i];
            return null;
        }

        /// <summary>A copy of a template's rules (null when unknown): commands carry their own arrays.</summary>
        public static CommandmentRule[] RulesOf(int id)
        {
            var t = Get(id);
            return t == null ? null : (CommandmentRule[])t.Rules.Clone();
        }
    }

    /// <summary>COMMANDMENTS 4.9: structural validation of a rule set (pure; never mutates; never reads text).</summary>
    public static class CommandmentRules
    {
        /// <summary>Rules per commandment (a person's interpretation holds two: contracts 13.1 <c>Interpretation</c> plus D6's second slot).</summary>
        public const int MaxRules = 2;

        /// <summary>Null when valid, else <c>bad_target</c>: empty or oversized, an unknown modality, verb, ware, selector, condition, importance or scope kind.</summary>
        public static string Validate(CommandmentRule[] rules, int importance, int template)
        {
            if (rules == null || rules.Length == 0 || rules.Length > MaxRules) return DivineReasons.BadTarget;
            if (importance < 1 || importance > 5) return DivineReasons.BadTarget;
            if (template != 0 && CommandmentTemplates.Get(template) == null) return DivineReasons.BadTarget;
            foreach (var r in rules)
            {
                if (r.Modality != Modality.Required && r.Modality != Modality.Prohibited && r.Modality != Modality.Encouraged) return DivineReasons.BadTarget;
                if (!KnownVerb(r.LearningVerb)) return DivineReasons.BadTarget;
                if (!System.Enum.IsDefined(typeof(Ware), r.Ware)) return DivineReasons.BadTarget;
                if (!System.Enum.IsDefined(typeof(ApplySelector), r.Applies)) return DivineReasons.BadTarget;
                if (!System.Enum.IsDefined(typeof(ConditionKind), r.Condition)) return DivineReasons.BadTarget;
                int also = r.ConditionParam & 0xFF, sel = (r.ConditionParam >> 8) & 0xFF;
                if (!System.Enum.IsDefined(typeof(ConditionKind), (ConditionKind)also)) return DivineReasons.BadTarget;
                if ((r.ConditionParam & ~0xFFFF) != 0) return DivineReasons.BadTarget;
                if (sel != 0 && r.Applies != ApplySelector.Community && r.Applies != ApplySelector.Role) return DivineReasons.BadTarget;
                if (r.ObjClass > 127) return DivineReasons.BadTarget;
                var k = r.Scope.Kind;
                if (k != DivineTargetKind.None && k != DivineTargetKind.Location && k != DivineTargetKind.Household && k != DivineTargetKind.Org) return DivineReasons.BadTarget;
                if (k == DivineTargetKind.Location && (r.Scope.Node < 0 || r.Scope.Radius < 0)) return DivineReasons.BadTarget;
            }
            return null;
        }

        /// <summary>A verb of the verb table, or one the contracts reserve for a later package (it then matches nothing until that package lands it).</summary>
        public static bool KnownVerb(byte v) => v != 0 && (VerbTable.TryGet(v, out _) || Verb.IsCiv(v) || Verb.IsOrgs(v) || Verb.IsDivinity(v));
    }
}
