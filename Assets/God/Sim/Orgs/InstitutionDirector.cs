using God.Sim.Learning;
using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// The institution seats (contracts 11.4: order 905; host-attached like <see cref="AiDirector"/> by <see cref="SimBuilder"/>):
    /// at each organisation's daily decision slot (tick of day <c>(OrgId x 7) % TicksPerDay</c>), first the meeting for the
    /// open proposals (<see cref="OrgMeetings.Convene"/>), then, for each office with <see cref="OrgPower.Propose"/> whose holder
    /// is alive and awake (ascending office index), it builds that person's <see cref="InstitutionView"/> (the organisation's
    /// records and the decider's own mind only) and asks the seated <see cref="IInstitutionPolicy"/>. The commands are submitted
    /// in-step (applied at the next tick, recomputed in a replay); a command that is not an organisation command of this
    /// organisation in this decider's name is dropped and counted. No living decider: nothing is decided (ORG-INV-3).
    /// Stateless between thinks: a director attached to a loaded game continues it exactly.
    /// </summary>
    public sealed class InstitutionDirector : ISimSystem
    {
        public const int SystemOrder = 905;

        public GodSim Sim { get; }
        public IInstitutionPolicy Policy { get; }
        /// <summary>Thinks run and commands dropped (not state).</summary>
        public long Thinks, Inadmissible;

        private readonly List<Command> _out = new List<Command>();
        private readonly ThinkScratch _sc = new ThinkScratch();
        private readonly List<InstitutionRecordView> _records = new List<InstitutionRecordView>();
        private readonly List<InstitutionRequestView> _requests = new List<InstitutionRequestView>();
        private readonly List<InstitutionOfficeView> _offices = new List<InstitutionOfficeView>();
        private readonly List<InstitutionPolicyView> _policies = new List<InstitutionPolicyView>();
        private readonly List<int> _members = new List<int>(), _trust = new List<int>(), _stock = new List<int>();
        private readonly int[] _stockBuf = new int[Wares.PersonalCount];

        private InstitutionDirector(GodSim sim, IInstitutionPolicy policy) { Sim = sim; Policy = policy ?? new MemberUtilityPolicy(); }

        /// <summary>Seats the policy (default <see cref="MemberUtilityPolicy"/>) when the game has organisations; null otherwise. Call once per sim.</summary>
        public static InstitutionDirector Attach(GodSim sim, IInstitutionPolicy policy = null)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            if (sim.State.Organisations == null) return null;
            foreach (var sys in sim.Systems) if (sys is InstitutionDirector d0) return d0;
            var d = new InstitutionDirector(sim, policy);
            sim.AddSystem(SystemOrder, d);
            return d;
        }

        public static int SlotOf(int orgId, int ticksPerDay) => (int)((long)orgId * 7 % Math.Max(1, ticksPerDay));

        public void Tick(GodState s, EventBuffer ev)
        {
            var os = s.Organisations;
            if (os == null || s.IsGameOver || os.Orgs.Count == 0) return;
            int tpd = SimCalendar.Of(s).TicksPerDay;
            int tod = (int)(s.Tick % tpd);
            foreach (var org in os.Orgs)
            {
                if (org.Status == OrgStatus.Dissolved || SlotOf(org.Id, tpd) != tod) continue;
                OrgMeetings.Convene(s, ev, org);
                if (org.Status == OrgStatus.Dissolved) continue;
                for (int i = 0; i < org.Offices.Count; i++)
                {
                    var o = org.Offices[i];
                    if (o.Holder == 0 || (o.Powers & OrgPower.Propose) == 0) continue;
                    var n = s.Population.Find(o.Holder);
                    if (n == null || !Bodies.IsLivingPerson(n) || n.Action.Asleep || n.Mind == null) continue;
                    var view = Build(s, org, i, n);
                    var rng = new Rng(s.Seed).Fork("inst-policy", org.Id, s.Tick * 64 + i);
                    _out.Clear();
                    Policy.Decide(view, ref rng, _out);
                    foreach (var c in _out)
                    {
                        if (!(c is OrgCommand oc) || oc.Org != org.Id || oc.Actor != n.Id) { Inadmissible++; continue; }
                        Sim.Submit(c);
                    }
                    _out.Clear();
                    Thinks++;
                }
            }
        }

        /// <summary>The decider's view: the organisation's records (layer 4) and its own mind (layer 6), nothing else.</summary>
        public InstitutionView Build(GodState s, Organisation org, int office, Npc decider)
        {
            _records.Clear(); _requests.Clear(); _offices.Clear(); _policies.Clear(); _members.Clear(); _trust.Clear(); _stock.Clear();
            foreach (var r in org.Records) _records.Add(new InstitutionRecordView(r.Id, r.Tick, r.Recorder, (int)r.Kind, r.Ref, (int)r.Ware, r.Count));
            foreach (var r in org.Requests) _requests.Add(new InstitutionRequestView(r.Id, (int)r.Kind, r.Requester, r.Opened, (int)r.Status, r.SeenNeed));
            for (int i = 0; i < org.Offices.Count; i++) { var o = org.Offices[i]; _offices.Add(new InstitutionOfficeView(i, (int)o.Kind, (uint)o.Powers, o.Holder, o.IsHead)); }
            foreach (var p in org.Policies) _policies.Add(new InstitutionPolicyView((int)p.Key, p.Value));
            var sm = decider.Mind.Social;
            foreach (var r in org.Roles)
            {
                if (_members.Contains(r.Npc)) continue;
                _members.Add(r.Npc);
                int ri = sm == null ? -1 : sm.Find(r.Npc);
                _trust.Add(ri < 0 ? 0 : sm.Rel[ri].Trust);
            }
            OrgRecords.RecordedStock(org, _stockBuf);
            foreach (int v in _stockBuf) _stock.Add(v);
            // the decider's own mind as a policy sees it (MindView over its store, beliefs and drives)
            _sc.Mind = decider.Mind; _sc.CandCount = 0; _sc.PeopleCount = 0; _sc.HeardCount = 0; _sc.SeenCount = 0;
            _sc.Self = new SelfView
            {
                Id = decider.Id, Hunger = decider.DriveOf(Drive.Hunger), Fatigue = decider.DriveOf(Drive.Fatigue), Cold = decider.DriveOf(Drive.Cold),
                Fear = decider.DriveOf(Drive.Fear), Pain = decider.DriveOf(Drive.Pain), Node = decider.Node, Household = decider.Household, Tick = (int)s.Tick,
            };
            _sc.Traits = decider.Traits; _sc.Learn = decider.LearnTraits; _sc.LearningTuning = s.Tuning.Learning; _sc.Tuning = s.Tuning.Population; _sc.Grid = s.Territory.Grid;
            var o2 = org.Offices[office];
            return new InstitutionView(org.Id, decider.Id, s.Tick, office, (int)o2.Kind, (uint)o2.Powers, (int)org.Charter.Governance.Kind, (int)org.Charter.PolicyRule.Kind,
                _records, _requests, _offices, _policies, _members, _trust, _stock, org.Proposals.Count, new MindView(_sc));
        }
    }
}
