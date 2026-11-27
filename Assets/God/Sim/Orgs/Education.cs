using System;
using System.Collections.Generic;
using God.Sim.Civ;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// C7b education (INSTITUTIONAL_SERVICES 5.2; OWNER: C7). A teacher offers by gesture (<c>Teach</c> 18, to a household
    /// member or someone it trusts; or <c>Demonstrate</c> 21, to whoever is in sight); a learner who chooses to attend
    /// (<c>Attend</c> 109, its own candidate) walks within <c>TeachRange</c> and stays; the session counts only ticks with at
    /// least one attending learner in range (at most <c>MaxLearnersPerSession</c>). After <c>TeachTicks</c> each attending
    /// learner gets the teacher's associations for the topic (<see cref="Imitation.Copy"/>: Imitated for teaching, Vicarious
    /// for a demonstration) with fidelity <c>TeacherSkill x Attention / 1000</c> (demonstration: <c>DemonstratePermille x
    /// Attention / 1000</c>) and value scaled by its trust in the teacher; a taught learner's skill rises by
    /// <c>TeachSkillGain x fidelity / 1000</c>, capped below the teacher's. Costs: both persons' ticks.
    /// </summary>
    public static class Education
    {
        /// <summary>Teach sub-acts in <c>Candidate.Class</c>: 10 + topic offer to teach, 20 + topic demonstrate; Attend: 1.</summary>
        public const byte OfferBase = 10, DemoBase = 20, AttendSub = 1;

        private static OrgC7Tuning T(GodState s) => s.Tuning.Orgs.C7;

        public static string TopicName(int topic) => topic == 1 ? "heal" : topic == 2 ? "gather wood with an axe" : topic == 3 ? "forage" : "topic " + topic;

        /// <summary>A person who can teach a topic (fixtures, the start culture): it knows it as a teacher and holds the skill.</summary>
        public static void MakeTeacher(GodState s, Npc n, int topic, int skill)
        {
            var m = n.Mind.Orgs ?? (n.Mind.Orgs = Orgs.NewMind(s));
            m.HealthOrCreate().TeachTopics |= 1 << topic;
            Skills.Set(s, n.Id, (SkillClass)topic, skill, T(s).SkillSlots);
        }

        /// <summary>The start culture: <c>StartTeachers</c> adults per faction (highest ids, so apart from the start healers) teach <c>StartTeachTopic</c>.</summary>
        public static void StartGame(GodState s)
        {
            var tu = T(s);
            if (tu.StartTeachers <= 0 || s.Population == null) return;
            var per = new Dictionary<int, int>();
            for (int i = s.Population.Npcs.Count - 1; i >= 0; i--)
            {
                var n = s.Population.Npcs[i];
                if (!Bodies.IsLivingPerson(n) || n.Mind == null || n.Stage == LifeStage.Child) continue;
                per.TryGetValue((int)n.Faction, out int k);
                if (k >= tu.StartTeachers) continue;
                per[(int)n.Faction] = k + 1;
                MakeTeacher(s, n, tu.StartTeachTopic, tu.StartTeachSkill);
            }
        }

        // ---------------------------------------------------------------- starting the acts

        public static void Start(GodState s, EventBuffer ev, Npc npc, OrgMind m, in Candidate c)
        {
            if (c.Verb == C7Verb.Attend) { StartAttend(s, ev, npc, m, in c); return; }
            var tu = T(s);
            bool demo = c.Class >= DemoBase;
            int topic = c.Class - (demo ? DemoBase : OfferBase);
            var h = m.Health;
            if (h == null || topic <= 0 || (h.TeachTopics & (1 << topic)) == 0 || Skills.Of(s, npc.Id, (SkillClass)topic) < tu.TeachMinSkill) { OrgActs.Frustrate(s, npc); return; }
            var learner = demo ? null : s.Population.Find(c.TargetId);
            if (!demo && (learner == null || !Bodies.IsLivingPerson(learner) || learner.Id == npc.Id)) { OrgActs.Frustrate(s, npc); return; }
            var hs = s.Organisations.HealthOrCreate();
            int si = hs.IndexOfSession(npc.Id);
            if (si >= 0 && (hs.Sessions[si].Topic != topic || hs.Sessions[si].Kind != (demo ? TeachingSession.Demonstrate : TeachingSession.Teach))) { hs.Sessions.RemoveAt(si); si = -1; }
            if (si < 0)
            {
                var ns = new TeachSession { Teacher = npc.Id, Topic = topic, Kind = demo ? TeachingSession.Demonstrate : TeachingSession.Teach, StartTick = s.Tick, Org = 0 };
                int at = 0; while (at < hs.Sessions.Count && hs.Sessions[at].Teacher < npc.Id) at++;
                hs.Sessions.Insert(at, ns);
            }
            h.OfferedTo = learner?.Id ?? 0; h.OfferedTick = (int)s.Tick; h.Teaching = true;
            var topicMsg = new MsgTopic { Kind = TopicKind.Service, Id = 0, Sub = 100 + topic };
            int id = OrgActs.Say(s, ev, npc, demo ? GestureAct.Demonstrate : GestureAct.Teach, topicMsg, learner?.Id ?? 0, MsgTime.Now);
            if (demo) OrgActs.EnsureAll(s, npc, id); else OrgActs.Ensure(s, learner, id);
        }

        private static void StartAttend(GodState s, EventBuffer ev, Npc npc, OrgMind m, in Candidate c)
        {
            var h = m.Health;
            var hs = s.Organisations.Health;
            var teacher = s.Population.Find(c.TargetId);
            int si = hs == null ? -1 : hs.IndexOfSession(c.TargetId);
            if (h == null || teacher == null || !Bodies.IsLivingPerson(teacher) || si < 0 || h.OfferFrom != c.TargetId) { if (h != null) h.OfferFrom = 0; OrgActs.Frustrate(s, npc); return; }
            var ses = hs.Sessions[si];
            int slot = -1;
            for (int i = 0; i < 4; i++) if (ses.Learner(i) == npc.Id) slot = i;
            int cap = Math.Max(1, Math.Min(4, T(s).MaxLearnersPerSession));
            if (slot < 0) for (int i = 0; i < cap; i++) if (ses.Learner(i) == 0) { slot = i; break; }
            if (slot < 0) { h.OfferFrom = 0; OrgActs.Frustrate(s, npc); return; } // the session is full (capacity)
            ses.SetLearner(slot, npc.Id);
            hs.Sessions[si] = ses;
            h.Attending = true;
        }

        // ---------------------------------------------------------------- the acts' ticks

        public static void Act(GodState s, EventBuffer ev, Npc npc, OrgMind m)
        {
            if (npc.Action.Verb == C7Verb.Attend) { AttendTick(s, ev, npc, m); return; }
            var tu = T(s);
            var hs = s.Organisations.Health;
            int si = hs == null ? -1 : hs.IndexOfSession(npc.Id);
            if (si < 0) { OrgActs.End(npc); return; }
            var ses = hs.Sessions[si];
            int range = Range(s, ses.Kind);
            var g = s.Territory.Grid;
            int present = 0;
            for (int i = 0; i < 4; i++)
            {
                var l = s.Population.Find(ses.Learner(i));
                if (l != null && Attending(l, npc.Id) && g.Distance(l.Node, npc.Node) <= range) present++;
            }
            if (present > 0) ses.Progress++;
            hs.Sessions[si] = ses;
            if (ses.Progress >= tu.TeachTicks) { Complete(s, ev, npc, hs, si); return; }
            if (present == 0 && s.Tick - ses.StartTick > tu.TeachOfferTicks + 2 * tu.TeachTicks) { hs.Sessions.RemoveAt(si); if (m.Health != null) m.Health.Teaching = false; OrgActs.Frustrate(s, npc); }
        }

        private static bool Attending(Npc l, int teacher) => Bodies.IsLivingPerson(l) && l.Action.Verb == C7Verb.Attend && l.Action.TargetId == teacher;

        private static int Range(GodState s, byte kind) => kind == TeachingSession.Demonstrate ? s.Tuning.Population.SightDay / 2 : T(s).TeachRange;

        private static void AttendTick(GodState s, EventBuffer ev, Npc npc, OrgMind m)
        {
            var hs = s.Organisations.Health;
            var teacher = s.Population.Find(npc.Action.TargetId);
            int si = hs == null ? -1 : hs.IndexOfSession(npc.Action.TargetId);
            bool listed = false;
            if (si >= 0) for (int i = 0; i < 4; i++) if (hs.Sessions[si].Learner(i) == npc.Id) listed = true;
            if (teacher == null || !Bodies.IsLivingPerson(teacher) || !listed) { if (m.Health != null) { m.Health.Attending = false; m.Health.OfferFrom = 0; } OrgActs.End(npc); return; }
            OrgActs.WalkTo(s, npc, teacher.Node, Range(s, hs.Sessions[si].Kind));
        }

        /// <summary>The outcome, measured on each attending learner itself: its copied associations and its skill (exact).</summary>
        private static void Complete(GodState s, EventBuffer ev, Npc teacher, HealthEducationState hs, int si)
        {
            var tu = T(s);
            var ses = hs.Sessions[si];
            hs.Sessions.RemoveAt(si);
            hs.SessionsHeld++;
            var g = s.Territory.Grid;
            int range = Range(s, ses.Kind);
            int tskill = Skills.Of(s, teacher.Id, (SkillClass)ses.Topic);
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            bool demo = ses.Kind == TeachingSession.Demonstrate;
            for (int i = 0; i < 4; i++)
            {
                var l = s.Population.Find(ses.Learner(i));
                if (l == null || !Attending(l, teacher.Id) || g.Distance(l.Node, teacher.Node) > range || l.Mind == null) continue;
                int attention = AttentionOf(s, l);
                int fidelity = demo ? tu.DemonstratePermille * attention / 1000 : tskill * attention / 1000;
                int trust = Social.SocialView.Trust(l.Mind, teacher.Id);
                int copied = Imitation.Copy(teacher.Mind?.Store, l.Mind.Store, ses.Topic, fidelity, Imitation.TrustFactor(trust), tu.TeachMaxEntries, (ushort)day, demo ? Origin.Vicarious : Origin.Imitated);
                hs.EntriesCopied += copied;
                int before = Skills.Of(s, l.Id, (SkillClass)ses.Topic), after = before;
                if (!demo)
                {
                    after = Skills.Gain(s, l.Id, (SkillClass)ses.Topic, tu.TeachSkillGain * fidelity / 1000, tskill - 1, tu.SkillSlots, out before);
                    if (after != before) ev?.Add(new HealerSkillChanged { Npc = l.Id, Skill = ses.Topic, From = before, To = after, Cause = HealerSkillChanged.Taught });
                    var lm = l.Mind.Orgs ?? (l.Mind.Orgs = Orgs.NewMind(s));
                    if (ses.Topic == (int)SkillClass.Medicine) { lm.HealthOrCreate().Healer = true; lm.Practices |= 1 << OrgTemplates.HealerPractice; } // taught the practice
                }
                ev?.Add(new TeachingSession { Teacher = teacher.Id, Learner = l.Id, Topic = ses.Topic, Kind = ses.Kind, Ticks = (int)Math.Max(1, s.Tick - ses.StartTick), Org = ses.Org, Control = ControlOf(s, teacher.Id) });
                ev?.Add(new KnowledgeTransferred { Teacher = teacher.Id, Learner = l.Id, Topic = ses.Topic, EntriesCopied = copied, Fidelity = fidelity, SkillBefore = before, SkillAfter = after, Kind = ses.Kind });
                var h = l.Mind.Orgs?.Health;
                if (h != null) { h.Attending = false; h.OfferFrom = 0; }
                OrgActs.End(l);
            }
            var th = teacher.Mind?.Orgs?.Health;
            if (th != null) th.Teaching = false; // it saw the lesson end
            OrgActs.End(teacher);
        }

        /// <summary>Attention (permille): full unless hunger or pain is at a high band, then half (the learner's own body).</summary>
        public static int AttentionOf(GodState s, Npc l)
        {
            int high = s.Tuning.Population.BandHigh * 1000;
            return l.DriveOf(Drive.Hunger) >= high || l.DriveOf(Drive.Pain) >= high ? 500 : 1000;
        }

        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            var hs = s.Organisations.Health;
            if (hs == null) return;
            for (int i = hs.Sessions.Count - 1; i >= 0; i--)
            {
                var ses = hs.Sessions[i];
                if (ses.Teacher == npc.Id) { hs.Sessions.RemoveAt(i); continue; }
                bool changed = false;
                for (int k = 0; k < 4; k++) if (ses.Learner(k) == npc.Id) { ses.SetLearner(k, 0); changed = true; }
                if (changed) hs.Sessions[i] = ses;
            }
        }

        private static byte ControlOf(GodState s, int npc) => s.Divinity != null ? (byte)Divinity.Control.Of(s, npc) : (byte)0;
    }

    /// <summary>The mind-side logic of C7b (reads the scratch only; writes only the mind's own <see cref="OrgMind"/>).</summary>
    internal static class EducationThink
    {
        /// <summary>Offers heard: a Teach gesture addressed to it, or a Demonstrate gesture it saw (topic: service 100 + topic).</summary>
        public static void Perceive(ThinkScratch sc, OrgMind m)
        {
            var self = sc.Self;
            int last = m.Health?.LastTeachMsg ?? 0, maxId = last;
            for (int i = 0; i < sc.HeardCount; i++)
            {
                var msg = sc.Heard[i];
                if (msg.MessageId <= last) continue;
                if (msg.MessageId > maxId) maxId = msg.MessageId;
                if (msg.Act != GestureAct.Teach && msg.Act != GestureAct.Demonstrate || msg.Topic.Kind != TopicKind.Service || msg.Topic.Id != 0 || msg.Topic.Sub <= 100) continue;
                if (msg.Speaker == 0 || msg.Speaker == self.Id || msg.Act == GestureAct.Teach && msg.Target != self.Id) continue;
                var h = m.HealthOrCreate();
                h.OfferFrom = msg.Speaker; h.OfferTopic = msg.Topic.Sub - 100; h.OfferTick = self.Tick;
            }
            if (m.Health != null) m.Health.LastTeachMsg = maxId;
        }

        public static void Candidates(ThinkScratch sc, OrgMind m)
        {
            var h = m.Health;
            var self = sc.Self;
            var tu = m.C7;
            int tick = self.Tick;
            // a learner: attend an offer it heard (or go on attending)
            if (h.OfferFrom != 0 && tick - h.OfferTick <= tu.TeachOfferTicks + tu.TeachTicks * 2)
            {
                int who = OrgThink.PersonHandle(sc, h.OfferFrom);
                if (who != 0) HealthThink.Cand(sc, C7Verb.Attend, Education.AttendSub, IntentTarget.Npc, who, HealthThink.NodeOf(sc, who), C7Feature.AttendTopic0 + Math.Min(9, h.OfferTopic));
            }
            if (h.TeachTopics == 0) return;
            int topic = 1;
            while (topic < 16 && (h.TeachTopics & (1 << topic)) == 0) topic++;
            // a teacher: go on with (or come back to) the lesson it believes open
            if (h.Teaching && tick - h.OfferedTick <= tu.TeachOfferTicks + 2 * tu.TeachTicks)
            {
                int who = OrgThink.PersonHandle(sc, h.OfferedTo);
                if (h.OfferedTo != 0 && who != 0) HealthThink.Cand(sc, C7Verb.Teach, (byte)(Education.OfferBase + topic), IntentTarget.Npc, who, HealthThink.NodeOf(sc, who), C7Feature.TeachTopic0 + topic);
                else if (h.OfferedTo == 0) HealthThink.Cand(sc, C7Verb.Teach, (byte)(Education.DemoBase + topic), IntentTarget.Node, 0, self.RunVerb == C7Verb.Teach ? self.RunTarget : self.Node, C7Feature.DemoTopic0 + topic);
                return;
            }
            if (h.OfferedTick != 0 && tick - h.OfferedTick <= tu.TeachCooldownTicks) return;
            int learner = 0; bool others = false;
            for (int i = 0; i < sc.PeopleCount; i++)
            {
                var p = sc.People[i];
                if (p.Asleep || p.As == 0 || p.As == self.Id) continue;
                others = true;
                if (learner == 0 && (p.SameHousehold || OrgThink.TrustOf(sc, p.As) >= 200)) learner = p.Id;
            }
            if (learner != 0) HealthThink.Cand(sc, C7Verb.Teach, (byte)(Education.OfferBase + topic), IntentTarget.Npc, learner, HealthThink.NodeOf(sc, learner), C7Feature.TeachTopic0 + topic);
            else if (others) HealthThink.Cand(sc, C7Verb.Teach, (byte)(Education.DemoBase + topic), IntentTarget.Node, 0, self.Node, C7Feature.DemoTopic0 + topic);
        }
    }
}
