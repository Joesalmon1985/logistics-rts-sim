using Newtonsoft.Json;

namespace God.Sim
{
    /// <summary>
    /// F1.2 (contracts 0.1): the organisations state slot. Null unless <c>Orgs.Enabled = 1</c> and the game has a population, so every older game
    /// keeps its JSON and hash; hashed as the <c>organisations</c> part only while non-null. OWNER: F1 (frozen).
    /// </summary>
    public sealed partial class GodState
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public Orgs.OrgState Organisations;
    }
}
