using System.Collections.Generic;

namespace God.Sim.Civ
{
    /// <summary>C3's invariant slot (OWNER: C3; wired by C1c): the employment record's own checks run with <c>SimInvariants</c>.</summary>
    public static partial class CivModule
    {
        static partial void CheckC3(GodState s, List<string> bad) => Employment.Check(s, bad);
    }
}
