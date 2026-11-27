using God.Sim.Learning;
using System.Collections.Generic;
using God.Sim.Comms;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// C6's organisation command types (plain <see cref="Command"/>s, never <c>FactionCommand</c>): what an office-holder proposes
    /// as the organisation, submitted in-step by the <see cref="InstitutionDirector"/> for the seated <c>IInstitutionPolicy</c>.
    /// Validated: the organisation exists and is not dissolved, the actor is alive and holds an office with the power the act
    /// needs (<see cref="OrgPower.Propose"/> at least). Applied by the charter's rule: decided at once by an actor with
    /// <see cref="OrgPower.DecideAlone"/> under OwnerDecides/HeadDecides, else opened as a proposal for the next meeting.
    /// Decisions change records only.
    /// </summary>
    public abstract class OrgCommand : Command
    {
        public int Org;
        public int Actor;
        public DecisionSource Source = DecisionSource.Member;
        public int SourceRef;
    }

    /// <summary>Fill (or vacate, Npc 0) an office with a member (routine: the Governance rule).</summary>
    public sealed class OrgAppoint : OrgCommand { public int Office; public int Npc; }

    /// <summary>Set a policy (the PolicyRule).</summary>
    public sealed class OrgSetPolicy : OrgCommand { public OrgPolicyKey Key; public int Value; }

    /// <summary>Dissolve the organisation (the PolicyRule, as the amendment rule).</summary>
    public sealed class OrgDissolve : OrgCommand { }

    public static class OrgReasons
    {
        public const string OrgsOff = "orgs_off", NoSuchOrg = "no_such_org", NoPower = "no_power", BadArgs = "bad_args", Dissolved = "org_dissolved";
    }

    internal static class OrgCommandRules
    {
        public static CommandResult Common(GodState s, OrgCommand c, out Organisation org)
        {
            org = null;
            if (s.Organisations == null) return CommandResult.Reject(OrgReasons.OrgsOff, "Organisations are off.");
            org = s.Organisations.Find(c.Org);
            if (org == null) return CommandResult.Reject(OrgReasons.NoSuchOrg, "No such organisation.");
            if (org.Status == OrgStatus.Dissolved) return CommandResult.Reject(OrgReasons.Dissolved, "The organisation is dissolved.");
            var a = s.Population.Find(c.Actor);
            if (a == null || !Bodies.IsLivingPerson(a) || Orgs.OfficeWith(org, c.Actor, OrgPower.Propose) < 0)
                return CommandResult.Reject(OrgReasons.NoPower, "The actor holds no office with the power to propose.");
            return CommandResult.Accept();
        }

        /// <summary>Decided at once (the actor decides alone under its rule) or opened as a proposal for the meeting rule.</summary>
        public static void Decide(GodState s, EventBuffer ev, Organisation org, OrgCommand c, DecisionRuleSpec rule, DecisionWhat what, int a0, int a1)
        {
            bool alone = (rule.Kind == DecisionKind.HeadDecides || rule.Kind == DecisionKind.OwnerDecides) && Orgs.OfficeWith(org, c.Actor, OrgPower.DecideAlone) >= 0;
            if (alone)
            {
                int d = Orgs.Record(s, ev, org, c.Actor, c.Source, c.SourceRef, what, a0, a1, DecisionOutcome.Adopted, rule.Kind, 1, 0, 0, c.Actor);
                OrgMeetings.Carry(s, ev, org, what, a0, a1, d);
                return;
            }
            if (org.Proposals.Count >= Organisation.MaxProposals) return;
            foreach (var p in org.Proposals) if (p.What == what && p.A0 == a0) return; // one open proposal per question
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            org.Proposals.Add(new OrgProposal { Id = org.NextProposalId++, Proposer = c.Actor, Source = c.Source, SourceRef = c.SourceRef, What = what, A0 = a0, A1 = a1, OpenedDay = day });
        }
    }

    public sealed class OrgAppointHandler : CommandHandler<OrgAppoint>
    {
        protected override CommandResult Validate(GodState s, OrgAppoint c)
        {
            var r = OrgCommandRules.Common(s, c, out var org);
            if (!r.Ok) return r;
            if (c.Office < 0 || c.Office >= org.Offices.Count) return CommandResult.Reject(OrgReasons.BadArgs, "No such office.");
            if (c.Npc != 0 && !org.IsMember(c.Npc)) return CommandResult.Reject(OrgReasons.BadArgs, "Only a member can hold an office.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, OrgAppoint c, EventBuffer ev)
        {
            var org = s.Organisations.Find(c.Org);
            OrgCommandRules.Decide(s, ev, org, c, org.Charter.Governance, DecisionWhat.Appoint, c.Office, c.Npc);
            return CommandResult.Accept();
        }
    }

    public sealed class OrgSetPolicyHandler : CommandHandler<OrgSetPolicy>
    {
        protected override CommandResult Validate(GodState s, OrgSetPolicy c)
        {
            var r = OrgCommandRules.Common(s, c, out _);
            if (!r.Ok) return r;
            if (c.Key == OrgPolicyKey.None) return CommandResult.Reject(OrgReasons.BadArgs, "No such policy.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, OrgSetPolicy c, EventBuffer ev)
        {
            var org = s.Organisations.Find(c.Org);
            OrgCommandRules.Decide(s, ev, org, c, org.Charter.PolicyRule, DecisionWhat.SetPolicy, (int)c.Key, c.Value);
            return CommandResult.Accept();
        }
    }

    public sealed class OrgDissolveHandler : CommandHandler<OrgDissolve>
    {
        protected override CommandResult Validate(GodState s, OrgDissolve c) => OrgCommandRules.Common(s, c, out _);

        protected override CommandResult Apply(GodState s, OrgDissolve c, EventBuffer ev)
        {
            var org = s.Organisations.Find(c.Org);
            OrgCommandRules.Decide(s, ev, org, c, org.Charter.PolicyRule, DecisionWhat.Dissolve, 0, 0);
            return CommandResult.Accept();
        }
    }

    /// <summary>
    /// Meetings of a meeting rule (ORGANISATIONS 4.4 step 2): at the organisation's decision slot, each open proposal whose
    /// proposer is present at the meeting place is put to the members present. Each present member decides by its own
    /// interest (<see cref="Policy.InstitutionPolicies.MemberVote"/>, from its own body and mind) and gestures Accept or Refuse;
    /// the counter (the recorder if present, else the proposer) counts what it perceived. Below quorum the proposal lapses; a
    /// tie keeps the old state; more accepts than refusals adopt it. A proposal whose proposer never comes lapses after
    /// <c>ProposalWindowDays</c>.
    /// </summary>
    public static class OrgMeetings
    {
        public static void Convene(GodState s, EventBuffer ev, Organisation org)
        {
            if (org.Proposals.Count == 0) return;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            var rule = org.Charter.PolicyRule;
            int place = rule.MeetingPlace >= 0 ? rule.MeetingPlace : org.Node;
            var members = new List<int>();
            foreach (var r in org.Roles) if (!members.Contains(r.Npc)) members.Add(r.Npc);
            for (int i = 0; i < org.Proposals.Count;)
            {
                var pr = org.Proposals[i];
                if (pr.OpenedDay >= day) { i++; continue; } // announced for the next meeting
                if (day - pr.OpenedDay > s.Tuning.Orgs.C6.ProposalWindowDays)
                {
                    Orgs.Record(s, ev, org, pr.Proposer, pr.Source, pr.SourceRef, pr.What, pr.A0, pr.A1, DecisionOutcome.Lapsed, rule.Kind, 0, 0, members.Count, 0);
                    org.Proposals.RemoveAt(i);
                    continue;
                }
                if (!Orgs.Present(s, pr.Proposer, place)) { i++; continue; }
                Vote(s, ev, org, pr, rule, place, members);
                org.Proposals.RemoveAt(i);
            }
        }

        /// <summary>One vote at the meeting (also used by tests through <see cref="Convene"/>).</summary>
        private static void Vote(GodState s, EventBuffer ev, Organisation org, OrgProposal pr, DecisionRuleSpec rule, int place, List<int> members)
        {
            var p = s.Population;
            var present = new List<int>();
            foreach (int m in members) if (Orgs.Present(s, m, place)) present.Add(m);
            int absent = members.Count - present.Count;
            int counter = 0;
            foreach (var o in org.Offices) if (o.Holder != 0 && (o.Powers & OrgPower.KeepRecords) != 0 && present.Contains(o.Holder)) { counter = o.Holder; break; }
            if (counter == 0) counter = pr.Proposer;
            if (present.Count < rule.Quorum)
            {
                Orgs.Record(s, ev, org, pr.Proposer, pr.Source, pr.SourceRef, pr.What, pr.A0, pr.A1, DecisionOutcome.Lapsed, rule.Kind, 0, 0, absent, counter);
                return;
            }
            var counterNpc = p.Find(counter);
            int accept = 0, refuse = 0;
            foreach (int v in present)
            {
                var voter = p.Find(v);
                bool yes = v == pr.Proposer || Policy.InstitutionPolicies.MemberVote(voter.DriveOf(Drive.Hunger), FoodInHand(voter), pr.What, (OrgPolicyKey)pr.A0, Current(org, pr), pr.A1);
                int msg = OrgActs.Say(s, ev, voter, yes ? GestureAct.Accept : GestureAct.Refuse, new MsgTopic { Kind = TopicKind.Org, Id = org.Id, Sub = pr.Id }, 0, MsgTime.Now);
                if (v != counter && !Orgs.Perceived(counterNpc, msg)) continue; // the counter did not see it
                if (yes) accept++; else refuse++;
            }
            var outcome = accept > refuse ? DecisionOutcome.Adopted : DecisionOutcome.Rejected; // a tie keeps the old state
            int d = Orgs.Record(s, ev, org, pr.Proposer, pr.Source, pr.SourceRef, pr.What, pr.A0, pr.A1, outcome, rule.Kind, accept, refuse, absent, counter);
            if (outcome != DecisionOutcome.Adopted) return;
            Carry(s, ev, org, pr.What, pr.A0, pr.A1, d);
            // those present saw the outcome
            foreach (int v in present) Learn(s, org, p.Find(v), pr.What, pr.A0, pr.A1);
        }

        private static int Current(Organisation org, OrgProposal pr) => pr.What == DecisionWhat.SetPolicy ? org.PolicyOf((OrgPolicyKey)pr.A0) : 0;

        private static int FoodInHand(Npc n)
        {
            int k = 0;
            foreach (var l in n.Carry) if (!l.Empty && Wares.IsFood(l.Ware) && l.Ware != Ware.Nightshade) k += l.Count;
            return k;
        }

        /// <summary>An adopted decision changes records only: an office, a policy, or the status (dissolution).</summary>
        public static void Carry(GodState s, EventBuffer ev, Organisation org, DecisionWhat what, int a0, int a1, int decision)
        {
            switch (what)
            {
                case DecisionWhat.Appoint:
                    if (a0 >= 0 && a0 < org.Offices.Count && (a1 == 0 || org.IsMember(a1))) Orgs.SetOffice(s, ev, org, a0, a1, OfficeFilling.Appointed);
                    break;
                case DecisionWhat.SetPolicy: Orgs.SetPolicy(s, ev, org, (OrgPolicyKey)a0, a1, decision); break;
                case DecisionWhat.Dissolve: Orgs.Dissolve(s, ev, org, OrgDissolved.ByDecision); break;
            }
        }

        private static void Learn(GodState s, Organisation org, Npc n, DecisionWhat what, int a0, int a1)
        {
            var m = n?.Mind?.Orgs;
            int bi = m == null ? -1 : m.IndexOfOrg(org.Id);
            if (bi < 0 || what != DecisionWhat.SetPolicy) return;
            ref var b = ref m.Beliefs[bi];
            b.KnownPolicies |= 1u << a0;
            if ((OrgPolicyKey)a0 == OrgPolicyKey.RationPerRequest) b.Ration = (short)a1;
            if ((OrgPolicyKey)a0 == OrgPolicyKey.QueueOrder) b.QueueOrder = (byte)a1;
            b.Tick = (int)s.Tick;
        }
    }
}
