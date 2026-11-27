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

        // flagged edit (package D6): rules carry learning verbs (VerbTable rows), D6's selectors and conditions (a second condition in ConditionParam bits 0-7)
        public static string Verb(byte v)
        {
            if (Sim.Learning.VerbTable.TryGet(v, out var info)) return info.Name;
            return "act " + v;
        }

        private static string Who(ApplySelector a)
        {
            switch (a)
            {
                case ApplySelector.Everyone: return "Everyone";
                case ApplySelector.Adults: return "Adults";
                case ApplySelector.Children: return "Children";
                case ApplySelector.Elders: return "Elders";
                case ApplySelector.Affiliated: return "My followers";
                case ApplySelector.Community: return "Those of the community";
                case ApplySelector.HouseholdHeads: return "Heads of households";
                default: return "Those named (" + (int)a + ")";
            }
        }

        private static string When(ConditionKind c, int p)
        {
            if (c == ConditionKind.Always) return "";
            string w = "when " + Humanize(c.ToString()).ToLowerInvariant();
            var also = (ConditionKind)(p & 0xFF);
            return also == ConditionKind.Always ? w : w + " and " + Humanize(also.ToString()).ToLowerInvariant();
        }

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
    /// The template list of the picker (R11: the template picker is Tier 1; the free builder is U2b), read from D6's sim-side
    /// <c>CommandmentTemplates</c> (flagged edit by package D6): Tier 1 and Tier 2 templates first, then the data-only ones with
    /// the package they wait for. Importance is D6's 1-5 scale.
    /// </summary>
    public static class CommandmentTemplateList
    {
        public static IReadOnlyList<CommandmentTemplateDef> Provisional()
        {
            var l = new List<CommandmentTemplateDef>();
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < CommandmentTemplates.All.Count; i++)
                {
                    var t = CommandmentTemplates.All[i];
                    if ((t.Tier > 0) != (pass == 0)) continue;
                    l.Add(new CommandmentTemplateDef
                    {
                        Id = t.Id, Name = t.Name, Rules = CommandmentTemplates.RulesOf(t.Id),
                        Note = t.Tier == 0 ? "Matches nothing yet: waits for " + t.WaitsFor + "." : t.Tier == 2 ? "Tier 2: " + t.WaitsFor + "." : null,
                    });
                }
            return l;
        }

        public static readonly string[] ImportanceNames = { "Low", "Normal", "High" };
        public static readonly int[] ImportanceValues = { 2, 3, 5 };
    }
}
