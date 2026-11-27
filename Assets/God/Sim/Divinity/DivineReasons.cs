namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 10.1: the common refusal codes of divine commands (and the god-only gate). Packages add codes of their own
    /// commands in their own files; these are frozen. OWNER: F1.
    /// </summary>
    public static class DivineReasons
    {
        public const string DivinityOff = "divinity_off";
        public const string DivinityDisabled = "divinity_disabled";
        public const string NoSuchGod = "no_such_god";
        public const string MythicShort = "mythic_short";
        public const string Cooldown = "cooldown";
        public const string BadTarget = "bad_target";
        public const string TargetDead = "target_dead";
        public const string NotAPerson = "not_a_person";
        public const string ActUnavailable = "act_unavailable";
        public const string AlreadyControlled = "already_controlled";
        public const string ControlLimit = "control_limit";
        /// <summary>A faction command from outside the sim in god-only mode.</summary>
        public const string GodOnly = "god_only";
        public const string NotSandbox = "not_sandbox";
        /// <summary>An external divine command whose god does not hold the <c>Human</c> seat (authorship gate).</summary>
        public const string NotYourGod = "not_your_god";

        public static string Sentence(string code)
        {
            switch (code)
            {
                case DivinityOff: return "There are no gods in this game.";
                case DivinityDisabled: return "Divine power is disabled in this game.";
                case NoSuchGod: return "There is no such god.";
                case MythicShort: return "Not enough Mythic.";
                case Cooldown: return "That power is still recovering.";
                case BadTarget: return "That cannot be the target.";
                case TargetDead: return "The target is dead.";
                case NotAPerson: return "Only a person can be the target.";
                case ActUnavailable: return "That act is not possible yet.";
                case AlreadyControlled: return "Another power already controls that body.";
                case ControlLimit: return "That god already controls as many bodies as it may.";
                case GodOnly: return "Governments decide for themselves.";
                case NotSandbox: return "Only in a sandbox game.";
                case NotYourGod: return "That is not your god.";
                default: return code;
            }
        }
    }
}
