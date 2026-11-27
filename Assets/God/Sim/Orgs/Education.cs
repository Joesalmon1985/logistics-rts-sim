using God.Sim.Cognition;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>C7b education (INSTITUTIONAL_SERVICES 5.2; OWNER: C7). Filled by C7b.</summary>
    public static class Education
    {
        public static string TopicName(int topic) => topic == 1 ? "heal" : topic == 2 ? "gather wood with an axe" : topic == 3 ? "forage" : "topic " + topic;
        public static void Start(GodState s, EventBuffer ev, Npc npc, OrgMind m, in Candidate c) { OrgActs.Frustrate(s, npc); }
        public static void Act(GodState s, EventBuffer ev, Npc npc, OrgMind m) { OrgActs.End(npc); }
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc) { }
    }

    internal static class EducationThink
    {
        public static void Perceive(ThinkScratch sc, OrgMind m) { }
        public static void Candidates(ThinkScratch sc, OrgMind m) { }
    }
}
