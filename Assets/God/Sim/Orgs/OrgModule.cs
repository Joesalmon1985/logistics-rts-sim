using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// F1.5 (contracts 0.1): the module entry points. Call sites (frozen): <c>GodSim.Create</c> (after
    /// <c>PopulationExtensions.StartGame</c>), <c>GodSim.InstallModules</c>, <c>PopulationModule.AddNpc</c> (through
    /// <c>PopulationExtensions.NewMind</c>), <c>Kill</c> (through the hooks' <c>OnDeath</c>) and <c>SimInvariants.Check</c>.
    /// OWNER of the bodies: C6.
    /// </summary>
    public static partial class OrgModule
    {
        /// <summary>
        /// Before the first tick: the state when the switch is on and the game has a population. With <c>Orgs.Emergence</c> on,
        /// the starting adults know the practices of <c>Orgs.C6.StartPractices</c> (their culture; nothing is founded by it).
        /// </summary>
        public static void StartGame(GodState s)
        {
            if (s.Population == null || s.Tuning.Orgs.Enabled == 0) return;
            s.Organisations = new OrgState();
            Healthcare.StartGame(s); // C7 (flagged edit): the healing start culture, nothing when Orgs.C7.StartHealers = 0
            int practices = s.Tuning.Orgs.C6.StartPractices;
            if (s.Tuning.Orgs.Emergence == 0 || practices == 0) return;
            foreach (var n in s.Population.Npcs)
            {
                if (!Bodies.IsLivingPerson(n) || n.Mind == null || n.Stage == LifeStage.Child) continue;
                var m = n.Mind.Orgs ?? (n.Mind.Orgs = Orgs.NewMind(s));
                m.Practices |= practices & ~1;
            }
        }

        /// <summary>Registers the organisation command handlers and the <see cref="OrganisationSystem"/> (order 260), only while the state exists; binds the minds' tuning (also after a load).</summary>
        public static void Install(GodSim sim)
        {
            var s = sim.State;
            if (s.Organisations == null) return;
            sim.RegisterHandler(new OrgAppointHandler());
            sim.RegisterHandler(new OrgSetPolicyHandler());
            sim.RegisterHandler(new OrgDissolveHandler());
            sim.AddSystem(OrganisationSystem.Order, new OrganisationSystem());
            if (s.Population == null) return;
            foreach (var n in s.Population.Npcs)
            {
                var m = Bodies.IsPerson(n) ? n.Mind?.Orgs : null;
                if (m == null) continue;
                m.Tuning = s.Tuning.Orgs.C6; m.Welfare = s.Tuning.Orgs.C8; m.C7 = s.Tuning.Orgs.C7; // C7 (flagged edit)
                if (m.Beliefs == null) m.Beliefs = new OrgBelief[OrgMind.Cap];
            }
        }

        /// <summary>A new mind: nothing (a newborn knows no organisation; it learns of them by being told or by seeing one).</summary>
        public static void NewMind(GodState s, Npc npc) { }

        /// <summary>A person died: every role it held ends (its offices fall vacant; an organisation left without members dissolves); its founding meeting closes. A resurrected person regains nothing.</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            var os = s.Organisations;
            foreach (var org in os.Orgs)
            {
                for (int i = org.Proposals.Count - 1; i >= 0; i--) if (org.Proposals[i].Proposer == npc.Id) org.Proposals.RemoveAt(i); // nobody puts a dead person's proposal
                if (org.Status != OrgStatus.Dissolved && org.IsMember(npc.Id)) Orgs.EndRoles(s, ev, org, npc.Id, MemberLeft.Died);
            }
            for (int i = os.Meetings.Count - 1; i >= 0; i--) if (os.Meetings[i].Proposer == npc.Id) os.Meetings.RemoveAt(i);
        }

        /// <summary>
        /// C6's invariants: ascending unique ids below the counter; roles ascending and held by living people; office holders are
        /// members; stores are bins of the organisation (ORG-I7); lots owned by an organisation lie only where its stores are or
        /// in a person's carry (never on the ground of nowhere) and belong to an organisation that exists; request ids below the
        /// counter; bounded rings.
        /// </summary>
        public static void Check(GodState s, List<string> bad)
        {
            var os = s.Organisations;
            if (os == null) return;
            int last = 0;
            foreach (var org in os.Orgs)
            {
                if (org.Id <= last) bad.Add("ORG1: org ids not ascending at " + org.Id);
                if (org.Id >= os.NextOrgId) bad.Add("ORG1: org " + org.Id + " not below NextOrgId");
                last = org.Id;
                int prev = 0; RoleKind prevRole = 0;
                foreach (var r in org.Roles)
                {
                    if (r.Npc < prev || r.Npc == prev && r.Role <= prevRole) bad.Add("ORG2: roles of org " + org.Id + " not ascending");
                    prev = r.Npc; prevRole = r.Role;
                    var n = s.Population.Find(r.Npc);
                    if (n == null || !Bodies.IsLivingPerson(n)) bad.Add("ORG2: org " + org.Id + " role held by a dead or unknown person " + r.Npc);
                }
                foreach (var o in org.Offices) if (o.Holder != 0 && !org.IsMember(o.Holder)) bad.Add("ORG3: org " + org.Id + " office held by a non-member " + o.Holder);
                if (org.Status == OrgStatus.Dissolved && org.Roles.Count > 0) bad.Add("ORG3: dissolved org " + org.Id + " has roles");
                foreach (int b in org.Stores)
                {
                    var bin = Goods.BinById(s.Population, b);
                    if (bin == null || !bin.Owner.Same(Orgs.OwnerOf(org.Id))) bad.Add("ORG4: org " + org.Id + " store " + b + " missing or not its own");
                    else if (org.Status == OrgStatus.Dissolved)
                        foreach (var l in bin.Lots) if (l.Owner.Same(Orgs.OwnerOf(org.Id))) bad.Add("ORG4: dissolved org " + org.Id + " still owns lot " + l.Id);
                }
                foreach (var r in org.Requests) if (r.Id <= 0 || r.Id >= os.NextRequestId) bad.Add("ORG5: org " + org.Id + " request id " + r.Id + " out of range");
                if (org.Records.Count > Organisation.MaxRecords || org.Requests.Count > Organisation.MaxRequests || org.Decisions.Count > Organisation.MaxDecisions || org.Roles.Count > Organisation.MaxRoles)
                    bad.Add("ORG6: org " + org.Id + " ring over its bound");
            }
            foreach (var b in s.Population.Bins)
            {
                if (b.Owner.Kind != OwnerKind.Org) continue;
                if (os.Find(b.Owner.Id) == null) bad.Add("ORG4: bin " + b.Id + " owned by unknown org " + b.Owner.Id);
            }
            Healthcare.Check(s, bad); // C7 (flagged edit)
        }
    }
}
