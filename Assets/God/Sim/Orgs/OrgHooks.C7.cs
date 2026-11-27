using God.Sim.Cognition;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>C7's sim-side hook slots (OWNER: C7): the acts of Treat (107), Teach (108) and Attend (109), time use, deaths.</summary>
    public static partial class OrgHooks
    {
        static partial void TryStartC7(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled)
        {
            if (handled || !C7Verb.IsMine(c.Verb)) return;
            handled = true;
            var m = npc.Mind?.Orgs;
            if (m == null) { OrgActs.Frustrate(s, npc); return; }
            byte kind = (byte)(c.Verb == C7Verb.Treat ? 40 + c.Class : c.Verb == C7Verb.Teach ? 100 + c.Class : 70 + c.Class);
            // re-choosing the running act continues it
            if (npc.Action.Since < s.Tick && m.Act.Kind == kind && m.Act.Target == (c.Kind == IntentTarget.Node ? 0 : c.TargetId) && m.Act.StartTick > 0) return;
            m.Act = new OrgAct { Kind = kind, Target = c.Kind == IntentTarget.Node ? 0 : c.TargetId, StartTick = s.Tick };
            if (c.Verb == C7Verb.Treat)
            {
                switch (c.Class)
                {
                    case Healthcare.Ask: Healthcare.StartAsk(s, ev, npc, m, in c); break;
                    case Healthcare.Give: Healthcare.StartGive(s, ev, npc, m, in c); break;
                    case Healthcare.Gather: break; // the walk starts at the first act tick
                    case Healthcare.Found: Healthcare.FoundPractice(s, ev, npc, m); break;
                    default: OrgActs.Frustrate(s, npc); break;
                }
                return;
            }
            Education.Start(s, ev, npc, m, in c);
        }

        static partial void TryActC7(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled)
        {
            if (handled || !C7Verb.IsMine(npc.Action.Verb)) return;
            handled = true;
            var m = npc.Mind?.Orgs;
            if (m == null) { OrgActs.End(npc); return; }
            switch (m.Act.Kind)
            {
                case 40 + Healthcare.Give: Healthcare.GiveTick(s, ev, npc, m); break;
                case 40 + Healthcare.Gather: Healthcare.GatherTick(s, ev, npc); break;
                case 40 + Healthcare.Ask: Healthcare.AskTick(s, ev, npc, m); break;
                case 40 + Healthcare.Found:
                    if (s.Tick >= m.Act.StartTick + s.Tuning.Social.GestureTicks) OrgActs.End(npc);
                    break;
                default: Education.Act(s, ev, npc, m); break;
            }
        }

        static partial void ActivityOfC7(byte verb, ref int activity)
        {
            if (activity != Activity.Other) return;
            if (verb == C7Verb.Treat) activity = Activity.Care;
            else if (verb == C7Verb.Teach || verb == C7Verb.Attend) activity = Activity.Social;
        }

        static partial void OnDeathC7(GodState s, EventBuffer ev, Npc npc)
        {
            Healthcare.OnDeath(s, ev, npc);
            Education.OnDeath(s, ev, npc);
        }
    }
}
