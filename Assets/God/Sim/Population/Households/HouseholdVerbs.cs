using God.Sim.Learning;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// M4's learning verbs (SHARED_CONTRACTS 6.1: 32-47, plus 17 Store and 18 TakeFromStore) and its feature values (ObjClass
    /// 40-59, ObjState 16-23, SelfFeature 16-31, Situation 16-47, Role 4-5, NpcGroup 6-7). Data; never reused.
    /// </summary>
    public static class HVerb
    {
        public const byte Store = Verb.Store, TakeFromStore = Verb.TakeFromStore;
        public const byte Feed = 32, Beg = 33, Propose = 34, Accept = 35, TryForChild = 36, Leave = 37, Tend = 38, Adopt = 39,
            AskCare = 40, AcceptCare = 41, EndCare = 42;

        public static bool IsMine(byte v) => v == Store || v == TakeFromStore || v >= Feed && v <= EndCare;

        /// <summary>Verbs aimed at a person (a person target: the seen person's handle and its node, see <c>HouseholdMindHooks</c>).</summary>
        public static bool AtPerson(byte v) => v >= Feed && v <= EndCare;
    }

    public static class HClass { public const int Store = 40, Estate = 41; }

    public static class HState
    {
        public const int Begging1 = 16, Begging2 = 17, Begging3 = 18, Proposing = 19, Trying = 20, Sick = 21, NeedsHome = 22, AskingCare = 23;
    }

    public static class HSelf { public const int Unpartnered = 16, HasDependants = 17; }

    public static class HSituation { public const int DependantInNeed = 16, CareOwed1 = 17, CareOwed2 = 18, CareOwed3 = 19; }

    public static class HRole { public const int Child = 4, Elder = 5; }
}
