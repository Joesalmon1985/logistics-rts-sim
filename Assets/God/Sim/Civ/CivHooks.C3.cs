using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Civ
{
    /// <summary>C3's sim-side hook slots (OWNER: C3): its acts, its episode keys, time use, deaths, and the Sense seam that writes what a party perceives of its contracts.</summary>
    public static partial class CivHooks
    {
        static partial void TryStartC3(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled) => Employment.Start(s, ev, npc, in c, day, ref handled);

        static partial void TryActC3(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled) => Employment.Act(s, ev, npc, day, ref handled);

        /// <summary>A C3 decision waits on its contract (episode kind 17): wages, unpaid units and the worker's output reach it.</summary>
        static partial void EpisodeKeyC3(Npc npc, in Candidate c, ref int key)
        {
            if (key != 0 || !Employment.IsMine(c.Verb)) return;
            var cm = npc.Mind?.Civ;
            if (cm == null) return;
            for (int i = 0; i < cm.PlanCount; i++)
            {
                var p = cm.Plans[i];
                if (p.Verb == c.Verb && p.Intent == c.Intent && p.Kind == c.Kind && p.Target == c.TargetId) { key = Employment.EpisodeKeyOf(p.Contract); return; }
            }
        }

        static partial void ActivityOfC3(byte verb, ref int activity)
        {
            if (activity != Activity.Other) return;
            if (verb == Employment.VerbHire) activity = Activity.Social;
            else if (verb == Employment.VerbWorkFor) activity = Activity.Gather;
            else if (verb == Employment.VerbPay) activity = Activity.Carry;
        }

        static partial void OnDeathC3(GodState s, EventBuffer ev, Npc npc) => Employment.OnDeath(s, ev, npc);

        /// <summary>The Sense seam: adds nothing to what is seen; updates the person's beliefs of its contracts from what it sees (sim side).</summary>
        static partial void SenseExtraC3(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n) => Employment.Sense(s, npc, sight);
    }

    /// <summary>C3's slots of the civilisation system (257): the day pass and the per-tick accounting.</summary>
    public sealed partial class CivSystem
    {
        partial void DayStartC3(GodState s, EventBuffer ev, int day) => Employment.DayStart(s, ev, day);
        partial void TickC3(GodState s, EventBuffer ev) => Employment.Tick(s, ev);
    }
}
