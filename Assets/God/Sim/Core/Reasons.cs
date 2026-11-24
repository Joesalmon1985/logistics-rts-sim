namespace God.Sim
{
    /// <summary>Closed list of refusal codes for commands. Later packages add codes here.</summary>
    public static class Reasons
    {
        public const string Disabled = "disabled";
        public const string InvalidSite = "invalid_site";

        // P1: territory
        public const string NotLandCorner = "not_land_corner";
        public const string HqTooClose = "hq_too_close";
        public const string HqOutOfReach = "hq_out_of_reach";
        public const string NotBuildable = "not_buildable";
        public const string Occupied = "occupied";
        public const string TooCrowded = "too_crowded";
        public const string FlagTooClose = "flag_too_close";
        public const string NotOwner = "not_owner";
        public const string NoSuchThing = "no_such_thing";
        public const string CannotRemove = "cannot_remove";
        public const string BadRoad = "bad_road";
        public const string NotOnRoad = "not_on_road";

        public static string Sentence(string code, params object[] args)
        {
            switch (code)
            {
                case Disabled: return "That action is not available.";
                case InvalidSite: return "That cannot be built there.";
                case NotLandCorner: return "An HQ goes on a corner where three land hexes meet.";
                case HqTooClose: return "An HQ cannot stand on or next to another HQ.";
                case HqOutOfReach: return "A new HQ must be two corners away from one of your HQs.";
                case NotBuildable: return "That spot is not inside your build area.";
                case Occupied: return "Something is already there.";
                case TooCrowded: return "It is too close to another building, road or flag.";
                case FlagTooClose: return "Flags must be at least two steps apart.";
                case NotOwner: return "That is not yours.";
                case NoSuchThing: return "There is no such thing.";
                case CannotRemove: return "That cannot be removed.";
                case BadRoad: return args != null && args.Length > 0 && args[0] is string s ? s : "That road cannot be laid.";
                case NotOnRoad: return "A flag goes on one of your road nodes.";
                default: return code;
            }
        }
    }
}
