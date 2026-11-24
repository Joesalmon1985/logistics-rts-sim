namespace God.Sim
{
    /// <summary>Closed list of refusal codes for commands. Later packages add codes here.</summary>
    public static class Reasons
    {
        public const string Disabled = "disabled";
        public const string InvalidSite = "invalid_site";

        public static string Sentence(string code, params object[] args)
        {
            switch (code)
            {
                case Disabled: return "That action is not available.";
                case InvalidSite: return "That cannot be built there.";
                default: return code;
            }
        }
    }
}
