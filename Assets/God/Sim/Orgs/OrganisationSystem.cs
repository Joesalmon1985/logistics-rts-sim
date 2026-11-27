using God.Sim.Learning;
using System.Collections.Generic;
using God.Sim.Comms;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// The organisation system (contracts 11.4: order 260, after the civilisation system 257; installed only while
    /// <c>GodState.Organisations</c> exists). At each day start: the snapshot of the day that ended (<see cref="OrgDay"/>), the
    /// day counters reset, status updates (dormant with no living decider for <c>DormantDays</c>, operating again once one
    /// exists), then the day slots of C7-C10. Every tick: founding meetings expire; per organisation (ascending id) requests
    /// expire, requests perceived by a keeper are written into the ring by that keeper, an appointed office-holder who does not
    /// know yet is told when the head is within gesture range; then the tick slots of C7-C10, in that fixed order.
    /// </summary>
    public sealed partial class OrganisationSystem : ISimSystem
    {
        public const int Order = 260;

        private readonly List<int> _wares = new List<int>(), _counts = new List<int>();

        public void Tick(GodState s, EventBuffer ev)
        {
            var os = s.Organisations;
            if (os == null || s.Population == null) return;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            if (os.LastDay != day)
            {
                long lastStart = (long)os.LastDay * SimCalendar.Of(s).TicksPerDay;
                if (os.LastDay >= 0) foreach (var org in os.Orgs) if (org.Status != OrgStatus.Dissolved || org.DissolvedTick >= lastStart) Snapshot(s, ev, org, os.LastDay);
                os.LastDay = day;
                foreach (var org in os.Orgs) if (org.Status != OrgStatus.Dissolved) Status(s, ev, org, day);
                DayStartC7(s, ev, day);
                DayStartC8(s, ev, day);
                DayStartC9(s, ev, day);
                DayStartC10(s, ev, day);
            }
            for (int i = os.Meetings.Count - 1; i >= 0; i--) if (os.Meetings[i].Expires < s.Tick) os.Meetings.RemoveAt(i);
            foreach (var org in os.Orgs)
            {
                if (org.Status == OrgStatus.Dissolved) continue;
                Services.ExpireDue(s, ev, org);
                CommunalStore.OpenPerceived(s, ev, org);
                TellOffices(s, ev, org);
            }
            TickC7(s, ev);
            TickC8(s, ev);
            TickC9(s, ev);
            TickC10(s, ev);
        }

        private void Snapshot(GodState s, EventBuffer ev, Organisation org, int day)
        {
            _wares.Clear(); _counts.Clear();
            foreach (int bin in org.Stores)
            {
                var b = Goods.BinById(s.Population, bin);
                if (b == null) continue;
                foreach (var l in b.Lots)
                {
                    if (!l.Owner.Same(Orgs.OwnerOf(org.Id))) continue;
                    int k = _wares.IndexOf((int)l.Ware);
                    if (k < 0) { int at = 0; while (at < _wares.Count && _wares[at] < (int)l.Ware) at++; _wares.Insert(at, (int)l.Ware); _counts.Insert(at, l.Count); }
                    else _counts[k] += l.Count;
                }
            }
            int members = 0, staff = 0, vacant = 0, open = 0, unmet = 0, decisions = 0;
            var seen = new List<int>();
            foreach (var r in org.Roles)
            {
                if (seen.Contains(r.Npc)) continue;
                seen.Add(r.Npc); members++;
                var n = s.Population.Find(r.Npc);
                // truth (analytics only): a member who is hungry with no food in hand
                if (n != null && Cognition.Thinker.Band(s.Tuning.Population, Drive.Hunger, n.DriveOf(Drive.Hunger)) >= 2 && !HoldsFood(n)) unmet++;
            }
            foreach (var o in org.Offices) { if (o.Holder != 0) staff++; else vacant++; }
            foreach (var r in org.Requests) if (r.Status == RequestStatus.Waiting || r.Status == RequestStatus.Active) open++;
            foreach (var d in org.Decisions) if (d.Tick >= (long)day * SimCalendar.Of(s).TicksPerDay) decisions++;
            ev?.Add(new OrgDay
            {
                Org = org.Id, Day = day, Status = org.Status, Members = members, Staff = staff, OfficesVacant = vacant, StoreWares = _wares.ToArray(), StoreCounts = _counts.ToArray(),
                RequestsOpen = open, Served = org.DayServed, Refused = org.DayRefused, Expired = org.DayExpired, Deposits = org.DayDeposits, Records = org.DayRecords,
                Decisions = decisions, TrueUnmet = unmet,
            });
            org.DayOpened = org.DayServed = org.DayRefused = org.DayExpired = org.DayDeposits = org.DayRecords = 0;
            org.Ledger.Day.Clear();
        }

        private static bool HoldsFood(Npc n)
        {
            foreach (var l in n.Carry) if (!l.Empty && Wares.IsFood(l.Ware) && l.Ware != Ware.Nightshade) return true;
            return false;
        }

        /// <summary>Dormant after <c>DormantDays</c> with no living office-holder with Propose; operating again once one exists.</summary>
        private static void Status(GodState s, EventBuffer ev, Organisation org, int day)
        {
            bool decider = false;
            foreach (var o in org.Offices)
            {
                if (o.Holder == 0 || (o.Powers & OrgPower.Propose) == 0) continue;
                var n = s.Population.Find(o.Holder);
                if (n != null && Bodies.IsLivingPerson(n)) { decider = true; break; }
            }
            if (decider)
            {
                org.NoDeciderSince = -1;
                if (org.Status == OrgStatus.Dormant) Orgs.SetStatus(s, ev, org, OrgStatus.Operating);
                return;
            }
            if (org.NoDeciderSince < 0) org.NoDeciderSince = day;
            if (org.Status == OrgStatus.Operating && day - org.NoDeciderSince >= s.Tuning.Orgs.C6.DormantDays) Orgs.SetStatus(s, ev, org, OrgStatus.Dormant);
        }

        /// <summary>
        /// An office-holder who does not believe it holds its office yet is told by the head (or another office-holder who knows),
        /// by a gesture within range (AskJoin, topic the organisation, Sub 100 + office): it believes it once it perceived it.
        /// </summary>
        private static void TellOffices(GodState s, EventBuffer ev, Organisation org)
        {
            if ((s.Tick + org.Id) % 30 != 0) return; // a telling is tried every 30 ticks, not every tick
            var p = s.Population;
            for (int i = 0; i < org.Offices.Count; i++)
            {
                var o = org.Offices[i];
                if (o.Holder == 0) continue;
                var h = p.Find(o.Holder);
                if (h?.Mind == null || h.Action.Asleep) continue;
                int bi = h.Mind.Orgs?.IndexOfOrg(org.Id) ?? -1;
                if (bi >= 0 && h.Mind.Orgs.Beliefs[bi].MyOffice == i) continue;
                Npc teller = null;
                foreach (var x in org.Offices)
                {
                    if (x.Holder == 0 || x.Holder == o.Holder) continue;
                    var t = p.Find(x.Holder);
                    var tm = t?.Mind?.Orgs;
                    if (t == null || !Bodies.IsLivingPerson(t) || t.Action.Asleep || tm == null || tm.IndexOfOrg(org.Id) < 0) continue;
                    if (!Orgs.Present(s, h.Id, t.Node)) continue;
                    teller = t; break;
                }
                if (teller == null) continue;
                int msg = OrgActs.Say(s, ev, teller, GestureAct.AskJoin, new MsgTopic { Kind = TopicKind.Org, Id = org.Id, Sub = 100 + i }, h.Id, MsgTime.Now);
                if (Orgs.Perceived(h, msg)) Orgs.TellFounding(s, org, h);
            }
        }

        partial void DayStartC7(GodState s, EventBuffer ev, int day);
        partial void DayStartC8(GodState s, EventBuffer ev, int day);
        partial void DayStartC9(GodState s, EventBuffer ev, int day);
        partial void DayStartC10(GodState s, EventBuffer ev, int day);
        partial void TickC7(GodState s, EventBuffer ev);
        partial void TickC8(GodState s, EventBuffer ev);
        partial void TickC9(GodState s, EventBuffer ev);
        partial void TickC10(GodState s, EventBuffer ev);
    }
}
