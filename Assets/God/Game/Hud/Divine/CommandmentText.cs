using System.Collections.Generic;
using System.Text;
using God.Sim;
using God.Sim.Divinity;

namespace God.Game
{
    /// <summary>
    /// Renders the structured rules of a commandment as a sentence (R6, R47: it lives here in the Game layer, never in the sim; a static test
    /// checks that no sim code uses it). One-way: nothing parses text back into rules. D6 appends selector, condition and verb values; unknown
    /// values fall back to a plain "(kind N)" phrase so the sentence never lies about what the rule carries.
    /// </summary>
    public static class CommandmentText
    {
        public static string Render(CommandmentRule[] rules)
        {
            if (rules == null || rules.Length == 0) return "(no rules)";
            var sb = new StringBuilder();
            for (int i = 0; i < rules.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(RenderOne(rules[i]));
            }
            return sb.ToString();
        }

        public static string RenderOne(CommandmentRule r)
        {
            var sb = new StringBuilder();
            sb.Append(Who(r.Applies)).Append(' ');
            switch (r.Modality)
            {
                case Modality.Required: sb.Append("must "); break;
                case Modality.Prohibited: sb.Append("must never "); break;
                case Modality.Encouraged: sb.Append("should "); break;
                default: sb.Append("may "); break;
            }
            sb.Append(Verb(r.LearningVerb));
            if (r.Ware != Ware.None) sb.Append(' ').Append(Wares.Name(r.Ware));
            else if (r.ObjClass != 0) sb.Append(" (thing kind ").Append(r.ObjClass).Append(')');
            string cond = When(r.Condition, r.ConditionParam);
            if (cond.Length > 0) sb.Append(' ').Append(cond);
            sb.Append('.');
            return sb.ToString();
        }

        public static string Verb(byte v)
        {
            if (System.Enum.IsDefined(typeof(Sim.Policy.NpcVerb), (int)v)) return Humanize(((Sim.Policy.NpcVerb)v).ToString());
            return "act " + v;
        }

        private static string Who(ApplySelector a) => a == ApplySelector.Everyone ? "Everyone" : "Those named (" + (int)a + ")";

        private static string When(ConditionKind c, int p) => c == ConditionKind.Always ? "" : "when " + Humanize(c.ToString()).ToLowerInvariant();

        private static string Humanize(string pascal)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < pascal.Length; i++)
            {
                if (i > 0 && char.IsUpper(pascal[i])) sb.Append(' ');
                sb.Append(i == 0 ? pascal[i] : char.ToLowerInvariant(pascal[i]));
            }
            return sb.ToString().ToLowerInvariant();
        }
    }

    /// <summary>One template of the picker: a name and the structured rules it fills. Importance is a slot the player sets.</summary>
    public sealed class CommandmentTemplateDef
    {
        public int Id;
        public string Name;
        public CommandmentRule[] Rules;
        /// <summary>Why it may match nothing yet (a package it waits for), or null.</summary>
        public string Note;
    }

    /// <summary>
    /// The template list of the picker (R11: the template picker is Tier 1; the free builder is U2b). D6 owns the real
    /// <c>CommandmentTemplates</c>; until it merges this is a provisional list built only from rules the F1 enums can express. One place to swap.
    /// </summary>
    public static class CommandmentTemplateList
    {
        public static IReadOnlyList<CommandmentTemplateDef> Provisional() => new[]
        {
            new CommandmentTemplateDef { Id = 4, Name = "Protect forests", Rules = new[] { new CommandmentRule { Modality = Modality.Prohibited, LearningVerb = (byte)Sim.Policy.NpcVerb.Gather, Ware = Ware.Log, Applies = ApplySelector.Everyone, Condition = ConditionKind.Always } }, Note = "Provisional until D6's templates merge." },
            new CommandmentTemplateDef { Id = 3, Name = "Honour agreements", Rules = new[] { new CommandmentRule { Modality = Modality.Required, LearningVerb = (byte)Sim.Policy.NpcVerb.HandOver, Applies = ApplySelector.Everyone, Condition = ConditionKind.Always } }, Note = "Provisional until D6's templates merge." },
            new CommandmentTemplateDef { Id = 1, Name = "Share food", Rules = new[] { new CommandmentRule { Modality = Modality.Encouraged, LearningVerb = (byte)Sim.Policy.NpcVerb.HandOver, Ware = Ware.Grain, Applies = ApplySelector.Everyone, Condition = ConditionKind.Always } }, Note = "Provisional until D6's templates merge." },
        };

        public static readonly string[] ImportanceNames = { "Low", "Normal", "High" };
        public static readonly int[] ImportanceValues = { 300, 600, 900 };
    }
}
