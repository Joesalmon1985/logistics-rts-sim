namespace God.Sim.Orgs
{
    /// <summary>C7's slots of the organisation system (OWNER: C7): the treated records act on bodies each tick; day-start recovery credit and pruning; teaching sessions.</summary>
    public sealed partial class OrganisationSystem
    {
        partial void DayStartC7(GodState s, EventBuffer ev, int day)
        {
            Healthcare.DayStart(s, ev, day);
        }

        partial void TickC7(GodState s, EventBuffer ev)
        {
            Healthcare.Tick(s, ev);
        }
    }
}
