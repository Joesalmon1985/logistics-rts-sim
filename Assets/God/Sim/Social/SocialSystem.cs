using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Social
{
    /// <summary>
    /// The social system (SHARED_CONTRACTS 9.2, order 245, before the population): at day start the daily forgetting pass
    /// and the social day rows; every tick, for each NPC whose think tick it is (the population's own rule, ascending id),
    /// what it perceives of people and reads of what it heard, and the checks it makes of its own deals; then the deal
    /// traces nobody can still complete are closed (telemetry).
    /// </summary>
    public sealed class SocialSystem : ISimSystem
    {
        public void Tick(GodState s, EventBuffer ev)
        {
            var soc = s.Social;
            if (soc == null || s.Population == null) return;
            long day = SimCalendar.Of(s).Day(s.Tick);
            if (day > soc.DayDone) SocialSim.DayPass(s, ev, (int)day);
            int every = s.Tuning.Population.ThinkEvery;
            foreach (var npc in s.Population.Npcs)
            {
                if (!Population.Bodies.IsLivingPerson(npc) || npc.Mind?.Social == null) continue;
                if ((s.Tick + npc.Id) % every == 0) SocialSim.AtThink(s, ev, npc);
            }
            SocialSim.CloseStale(s, ev);
        }
    }

    /// <summary>Walking for social acts (to a person, to where someone was seen): the M3 step rule over the shared path searcher (a cache).</summary>
    internal static class SocialWalk
    {
        /// <summary>One tick along the path to <paramref name="goal"/>; true when within <paramref name="reach"/> of it. Re-plans when the goal moved.</summary>
        public static bool Step(GodState s, Npc npc, int goal, int reach)
        {
            var g = s.Territory.Grid;
            if (g.Distance(npc.Node, goal) <= reach) { npc.Path = null; npc.PathGoal = -1; npc.StepTicks = 0; return true; }
            var tu = s.Tuning.Population;
            var soc = s.Social;
            if (soc.Pather == null) soc.Pather = new NodePather(g);
            if (npc.Path == null || npc.PathGoal != goal)
            {
                npc.Path = soc.Pather.Find(npc.Node, goal, s.Territory.NodeRoad, tu.RoadStepTicks, tu.StepTicks, tu.PathMaxExpand);
                npc.PathIdx = 0; npc.PathGoal = goal; npc.StepTicks = 0;
                if (npc.Path == null || npc.Path.Length == 0) { npc.Path = null; return false; }
            }
            if (npc.PathIdx >= npc.Path.Length) { npc.Path = null; npc.PathGoal = -1; return g.Distance(npc.Node, goal) <= reach; }
            int next = npc.Path[npc.PathIdx];
            int cost = s.Territory.NodeRoad[next] != 0 ? tu.RoadStepTicks : tu.StepTicks;
            if (++npc.StepTicks >= cost)
            {
                npc.StepTicks = 0;
                npc.Node = next;
                npc.PathIdx++;
            }
            return g.Distance(npc.Node, goal) <= reach;
        }
    }

    /// <summary>The one integration M5 makes with the learning core's records (SHARED_CONTRACTS 6): moving the decisions that wait on one outcome to wait on another.</summary>
    public static class SocialLearning
    {
        /// <summary>
        /// Every live trace and open episode waiting on <paramref name="from"/> now waits on <paramref name="to"/> (a deal's or a
        /// request's decisions on the food lot it brought, so the meal's relief reaches them as M3b's lot episodes do).
        /// The learning rule is unchanged; only which outcome a decision waits for.
        /// </summary>
        public static int Rekey(AssociationStore st, int from, int to)
        {
            if (from == 0 || from == to) return 0;
            int n = 0;
            for (int i = 0; i < st.RingCount; i++) if (st.Ring[i].EpisodeKey == from) { st.Ring[i].EpisodeKey = to; n++; }
            for (int i = 0; i < st.OpenCount; i++) if (st.Open[i].T.EpisodeKey == from) { st.Open[i].T.EpisodeKey = to; n++; }
            return n;
        }
    }
}
