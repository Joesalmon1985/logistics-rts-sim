using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// C8's rows of the event catalogue, taken by C6 for the Tier 2 welfare slot (spec 10, R23): 551 only. 550 <c>DepositMade</c>
    /// and 552 stay reserved for the deferred banking package. OWNER: C6 (C8 Tier 2).
    /// </summary>
    public static partial class OrgEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> C8 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(WelfareGranted), 551, 1, TelemetryDomain.Finance, "fact_welfare", note: "a keeper handed rationed goods of the organisation to a recipient within reach (also ItemMoved HandOver)"));
    }

    /// <summary>551: Keeper handed Count of Ware to Recipient (the ration of request Request).</summary>
    public sealed class WelfareGranted : DomainEvent
    {
        public int Org, Keeper, Recipient;
        public Ware Ware;
        public int Count, Request;
        public byte Control;
    }
}
