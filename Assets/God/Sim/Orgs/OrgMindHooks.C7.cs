using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using God.Sim.Population.Households;

namespace God.Sim.Orgs
{
    /// <summary>C7's learning and intent verbs (contracts 14: Treat 107, Teach 108, Attend 109; intents 185-187 of the Orgs block 180-188).</summary>
    public static class C7Verb
    {
        public const byte Treat = 107, Teach = 108, Attend = 109;
        public const NpcVerb IntentTreat = (NpcVerb)185, IntentTeach = (NpcVerb)186, IntentAttend = (NpcVerb)187;
        public static bool IsMine(byte v) => v == Treat || v == Teach || v == Attend;
    }

    /// <summary>C7's values of <see cref="FeatureKind.Org"/> (from 100, clear of C6's).</summary>
    public static class C7Feature
    {
        /// <summary>A person it sees in need of treatment, by its seen need 1-3: 100 + level.</summary>
        public const int TreatNeed0 = 100;
        public const int AskHealer = 110, GatherHerbs = 111, FoundPractice = 112;
        /// <summary>Teaching (C7b): offering to teach a topic (120 + topic), attending an offer (130 + topic), demonstrating (140 + topic).</summary>
        public const int TeachTopic0 = 120, AttendTopic0 = 130, DemoTopic0 = 140;
        public static uint Of(int v) => Feature.Make(FeatureKind.Org, v);
    }

    /// <summary>
    /// C7's mind-side slots (OWNER: C7). Read only the think scratch (what is sensed and heard now, its own beliefs); write
    /// only the mind's own <see cref="OrgMind"/> (its <see cref="HealthMind"/> and the requests it perceived). A mind without a
    /// <see cref="HealthMind"/> gets no C7 candidate.
    /// </summary>
    public static partial class OrgMindHooks
    {
        static partial void VerbsC7(List<VerbInfo> into)
        {
            into.Add(new VerbInfo(C7Verb.Treat, "treat", VerbGroup.CoopGive, C7Verb.IntentTreat));
            into.Add(new VerbInfo(C7Verb.Teach, "teach", VerbGroup.CoopGive, C7Verb.IntentTeach));
            into.Add(new VerbInfo(C7Verb.Attend, "attend a lesson", VerbGroup.CoopTake, C7Verb.IntentAttend));
        }

        /// <summary>Innate priors (only for C7's own candidates, which exist only in a mind C7 entered): treat by the need seen, ask a believed healer when in pain, gather herbs, found a practice, teach, attend.</summary>
        static partial void InstinctsC7(List<Instinct> into)
        {
            into.Add(new Instinct(C7Feature.Of(C7Feature.TreatNeed0 + 1), 0, C7Verb.Treat, 300));
            into.Add(new Instinct(C7Feature.Of(C7Feature.TreatNeed0 + 2), 0, C7Verb.Treat, 400));
            into.Add(new Instinct(C7Feature.Of(C7Feature.TreatNeed0 + 3), 0, C7Verb.Treat, 500));
            into.Add(new Instinct(C7Feature.Of(C7Feature.AskHealer), Feature.DriveActive(Drive.Pain), C7Verb.Treat, 350));
            into.Add(new Instinct(C7Feature.Of(C7Feature.GatherHerbs), 0, C7Verb.Treat, 120));
            into.Add(new Instinct(C7Feature.Of(C7Feature.FoundPractice), 0, C7Verb.Treat, 250));
            for (int t = 1; t <= 3; t++)
            {
                into.Add(new Instinct(C7Feature.Of(C7Feature.TeachTopic0 + t), 0, C7Verb.Teach, 200));
                into.Add(new Instinct(C7Feature.Of(C7Feature.AttendTopic0 + t), 0, C7Verb.Attend, 300));
            }
        }

        static partial void AddCandidatesC7(ThinkScratch sc)
        {
            var m = sc.Mind?.Orgs;
            if (m?.Health == null || m.C7 == null) return;
            HealthThink.Perceive(sc, m);
            HealthThink.Candidates(sc, m);
            EducationThink.Perceive(sc, m);
            EducationThink.Candidates(sc, m);
        }

        static partial void DescribeC7(uint f, ref string text)
        {
            if (text != null || (f >> 24) != (uint)FeatureKind.Org) return;
            int v = (int)(f & 0xFFFFFF);
            if (v > C7Feature.TreatNeed0 && v <= C7Feature.TreatNeed0 + 3) text = "someone in need of treatment (" + (v - C7Feature.TreatNeed0) + ")";
            else if (v == C7Feature.TreatNeed0) text = "someone who may need treatment";
            else if (v == C7Feature.AskHealer) text = "asking a healer for help";
            else if (v == C7Feature.GatherHerbs) text = "gathering herbs";
            else if (v == C7Feature.FoundPractice) text = "setting up as a healer";
            else if (v > C7Feature.TeachTopic0 && v < C7Feature.TeachTopic0 + 10) text = "teaching " + Education.TopicName(v - C7Feature.TeachTopic0);
            else if (v > C7Feature.AttendTopic0 && v < C7Feature.AttendTopic0 + 10) text = "learning " + Education.TopicName(v - C7Feature.AttendTopic0);
            else if (v > C7Feature.DemoTopic0 && v < C7Feature.DemoTopic0 + 10) text = "showing how to " + Education.TopicName(v - C7Feature.DemoTopic0);
        }
    }

    /// <summary>The mind-side logic of C7a (reads the scratch only; writes only the mind's own <see cref="OrgMind"/>).</summary>
    internal static class HealthThink
    {
        public static void Perceive(ThinkScratch sc, OrgMind m)
        {
            var h = m.Health;
            var self = sc.Self;
            int tick = self.Tick;
            int maxId = h.LastMsg;
            for (int i = 0; i < sc.HeardCount; i++)
            {
                var msg = sc.Heard[i];
                if (msg.MessageId <= h.LastMsg) continue;
                if (msg.MessageId > maxId) maxId = msg.MessageId;
                if (!h.Healer || msg.Act != GestureAct.NeedHelp || msg.Topic.Kind != TopicKind.Service || msg.Topic.Sub != (int)ServiceKind.Treat) continue;
                if (msg.Target != self.Id || msg.Speaker == 0 || msg.Speaker == self.Id) continue;
                // a request it perceived: its seen need is the sign it sees on the asker (else 1: it was asked)
                short seen = 1;
                for (int k = 0; k < sc.PeopleCount; k++) if (sc.People[k].As == msg.Speaker && sc.People[k].Display == DisplayKind.Sick) seen = (short)System.Math.Max(1, sc.People[k].DisplayLevel & 3);
                m.AddKnown(new KnownRequest { Org = h.Practice, Kind = ServiceKind.Treat, Requester = msg.Speaker, Tick = tick, SeenNeed = seen, MessageId = msg.MessageId });
            }
            h.LastMsg = maxId;
            if (!h.Healer) return;
            // visible signs: a person it sees sick or wounded and not tended today (SERVICES 3.2 step 1: the provider opens it)
            for (int k = 0; k < sc.PeopleCount; k++)
            {
                var p = sc.People[k];
                if (p.As == 0 || p.As == self.Id || p.Display != DisplayKind.Sick || (p.DisplayLevel & 4) != 0) continue;
                m.AddKnown(new KnownRequest { Org = h.Practice, Kind = ServiceKind.Treat, Requester = p.As, Tick = tick, SeenNeed = (short)System.Math.Max(1, p.DisplayLevel & 3) });
            }
            for (int i = m.KnownCount - 1; i >= 0; i--) if (m.Known[i].Kind == ServiceKind.Treat && tick - m.Known[i].Tick > m.C7.RequestTicks) m.RemoveKnown(i);
        }

        public static void Candidates(ThinkScratch sc, OrgMind m)
        {
            var h = m.Health;
            var self = sc.Self;
            var tu = m.C7;
            int tick = self.Tick;
            if (h.Healer)
            {
                // treat the most severe it knows of (then the oldest) whom it sees; a treatment it is giving goes on (one patient at a time)
                int best = -1;
                int running = self.RunVerb == C7Verb.Treat && self.RunKind == IntentTarget.Npc ? PersonAs(sc, self.RunTarget) : 0;
                for (int i = 0; i < m.KnownCount; i++)
                {
                    var k = m.Known[i];
                    if (k.Kind != ServiceKind.Treat || k.Done || OrgThink.PersonHandle(sc, k.Requester) == 0) continue;
                    if (running != 0 && k.Requester == running) { best = i; break; }
                    if (best < 0 || k.SeenNeed > m.Known[best].SeenNeed || k.SeenNeed == m.Known[best].SeenNeed && (k.Tick < m.Known[best].Tick || k.Tick == m.Known[best].Tick && k.Requester < m.Known[best].Requester)) best = i;
                }
                if (best >= 0)
                {
                    int who = OrgThink.PersonHandle(sc, m.Known[best].Requester);
                    Cand(sc, C7Verb.Treat, Healthcare.Give, IntentTarget.Npc, who, NodeOf(sc, who), C7Feature.TreatNeed0 + System.Math.Min(3, (int)m.Known[best].SeenNeed));
                }
                // herbs: a root patch in sight while it carries few
                if (Herbs(sc) < tu.HerbKeep)
                    for (int i = 0; i < sc.SeenCount; i++)
                        if (sc.Seen[i].Class == ObjClass.RootPatch && sc.Seen[i].State != ObjState.StockEmpty) { Cand(sc, C7Verb.Treat, Healthcare.Gather, IntentTarget.Node, 0, sc.Seen[i].Node, C7Feature.GatherHerbs); break; }
                // its own practice, once it has treated enough people informally
                if (h.Practice == 0 && h.GivenInformal >= tu.FoundAfterTreatments && m.Knows(OrgTemplates.HealerPractice))
                    Cand(sc, C7Verb.Treat, Healthcare.Found, IntentTarget.Node, 0, self.Node, C7Feature.FoundPractice);
            }
            // a patient: in pain from a wound, it asks a believed healer it sees (or goes on waiting for the one it asked)
            bool waiting = self.RunVerb == C7Verb.Treat && self.RunKind == IntentTarget.Npc && h.LastAsk != 0 && tick - h.LastAsk <= tu.AskWaitTicks && h.Believes(PersonAs(sc, self.RunTarget));
            if (waiting) { Cand(sc, C7Verb.Treat, Healthcare.Ask, IntentTarget.Npc, self.RunTarget, NodeOf(sc, self.RunTarget), C7Feature.AskHealer); }
            else if ((self.Injured || self.Pain >= sc.Tuning.BandHigh) && (h.LastAsk == 0 || tick - h.LastAsk > tu.AskCooldownTicks))
                for (int i = 0; i < HealthMind.HealerCap; i++)
                {
                    int who = OrgThink.PersonHandle(sc, h.HealerAt(i));
                    if (who == 0 || h.HealerAt(i) == self.Id) continue;
                    Cand(sc, C7Verb.Treat, Healthcare.Ask, IntentTarget.Npc, who, NodeOf(sc, who), C7Feature.AskHealer);
                    break;
                }
        }

        private static int PersonAs(ThinkScratch sc, int handle)
        {
            for (int i = 0; i < sc.PeopleCount; i++) if (sc.People[i].Id == handle) return sc.People[i].As;
            return 0;
        }

        private static int Herbs(ThinkScratch sc)
        {
            int n = 0;
            for (int i = 0; i < sc.CarryCount; i++) if (!sc.Carry[i].Empty && sc.Carry[i].Ware == Ware.Herbs) n += sc.Carry[i].Count;
            return n;
        }

        public static int NodeOf(ThinkScratch sc, int handle)
        {
            for (int i = 0; i < sc.PeopleCount; i++) if (sc.People[i].Id == handle) return sc.People[i].Node;
            return sc.Self.Node;
        }

        public static void Cand(ThinkScratch sc, byte verb, byte sub, IntentTarget kind, int id, int node, int feature, uint extra = 0)
        {
            int dist = sc.Grid.Distance(sc.Self.Node, node);
            int at = Thinker.AddCandidate(sc, verb, kind, id, node, sub, dist);
            if (at < 0) return;
            Thinker.TargetFeatures(sc, at, C7Feature.Of(feature), extra);
        }
    }
}
