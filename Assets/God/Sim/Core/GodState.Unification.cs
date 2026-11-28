using Newtonsoft.Json;

namespace God.Sim
{
    /// <summary>
    /// UN0 (contracts 23.14): the unified state slot. Null unless <c>Unification.Enabled = 1</c> (and the game has a population), so
    /// every older game keeps its JSON and hash; hashed as the <c>unification</c> part only while non-null; a save without it loads
    /// with it null. OWNER: UN0 (frozen).
    /// </summary>
    public sealed partial class GodState
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public Unified.UnifiedState Unification;
    }
}
