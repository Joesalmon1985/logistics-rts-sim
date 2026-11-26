namespace God.Sim
{
    /// <summary>F0.12: the social module's state (M5). Null unless M5 starts it, so every older game keeps its hash. OWNER: M5.</summary>
    public sealed partial class GodState
    {
        /// <summary>Social counters, deal traces and contests (M5); null when the module is not running.</summary>
        public Social.SocialState Social;
    }
}

namespace God.Sim.Social
{
    /// <summary>
    /// The social module's state (SHARED_CONTRACTS 9.3): public fields, ascending-id lists, no dictionaries. F0 stub.
    /// OWNER: M5. Its hash part ("social") is appended only while <c>GodState.Social</c> is non-null.
    /// </summary>
    public sealed class SocialState
    {
        public int NextMessageId = 1, NextDealId = 1, NextContestId = 1;

        public void AddTo(ref StateHasher h)
        {
            h.Add(NextMessageId); h.Add(NextDealId); h.Add(NextContestId);
        }
    }
}
