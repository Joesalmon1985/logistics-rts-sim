using God.Sim.Cognition;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D4b (the brief's D5; contracts 4.2, 10.2, 11; POSSESSION_AND_AVATAR 5): the manifested avatar. OWNER: D4.
    /// </summary>
    public static class Avatars
    {
        internal static void ActionStarted(GodState s, EventBuffer ev, AvatarSpell av, Npc body, in DecisionRecord last)
        {
            av.ActionsStarted++;
            ev.Add(new AvatarActionStarted
            {
                Intervention = av.Intervention, Body = body.Id, Verb = last.Verb, TargetKind = last.TargetKind, TargetId = last.TargetId, TargetNode = last.TargetNode,
                ActionTick = last.Tick, Observers = av.Observers.Count,
            });
        }
    }
}
