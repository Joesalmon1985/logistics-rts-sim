using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>What a record says (append-only).</summary>
    public enum RecordKind : byte { None = 0, Deposit = 1, Withdrawal = 2, Ration = 3, Refusal = 4, Request = 5, Outcome = 6, MemberJoined = 7, MemberLeft = 8, Decision = 9, StockCount = 10 }

    /// <summary>
    /// One institutional record (layer 4, INSTITUTIONAL_STATE <c>RecordEntry</c>): written by a role-holder, saying what it
    /// knows (saw, was told, counted). It can be late, incomplete or wrong. <see cref="Falsified"/> is analytics-only truth
    /// (whether the writer wrote other than it saw), never read by a rule or policy.
    /// </summary>
    public struct RecordEntry
    {
        public int Id;
        public long Tick;
        public int Recorder;
        public RecordKind Kind;
        /// <summary>What it is about: a person, a request, a decision.</summary>
        public int Ref;
        public Ware Ware;
        public int Count;
        /// <summary>When it happened as the writer knows it.</summary>
        public int SeenTick;
        public bool Falsified;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add(Tick); h.Add(Recorder); h.Add((int)Kind); h.Add(Ref); h.Add((int)Ware); h.Add(Count); h.Add(SeenTick); h.Add(Falsified ? 1 : 0);
        }
    }

    /// <summary>
    /// Records (ORGANISATIONS 4.7): written only by <see cref="Write"/>, called by the <c>WriteRecord</c> act of a role-holder
    /// whose office has <see cref="OrgPower.KeepRecords"/>, standing at the organisation's place (within <see cref="Reach.Store"/>
    /// of it). It writes the things it witnessed (its own mind's buffer), so a deposit it did not see is never written and the
    /// organisation's recorded stock goes stale or wrong. With no recorder nothing is written. Readers: office-holders
    /// (<see cref="InstitutionReadout"/>); never other organisations, never a god (D5.28).
    /// </summary>
    public static class OrgRecords
    {
        public static bool MayWrite(Organisation org, int npc)
        {
            foreach (var o in org.Offices) if (o.Holder == npc && (o.Powers & OrgPower.KeepRecords) != 0) return true;
            return false;
        }

        /// <summary>The writer at the place writes up to <c>RecordsPerAct</c> things it witnessed for this organisation. Returns how many.</summary>
        public static int Write(GodState s, EventBuffer ev, Organisation org, Npc writer)
        {
            var m = writer.Mind?.Orgs;
            if (m == null || org.Status == OrgStatus.Dissolved || !MayWrite(org, writer.Id)) return 0;
            if (org.Node >= 0 && s.Territory.Grid.Distance(writer.Node, org.Node) > Reach.Store) return 0;
            int max = s.Tuning.Orgs.C6.RecordsPerAct, n = 0;
            for (int i = 0; i < m.WitnessCount && n < max;)
            {
                var w = m.Witness[i];
                if (w.Org != org.Id) { i++; continue; }
                Add(s, ev, org, writer.Id, w.Kind, w.Ref, w.Ware, w.Count, w.Tick, false);
                m.RemoveWitness(i);
                n++;
            }
            return n;
        }

        /// <summary>Appends one entry (ring: the oldest gives way) and emits <see cref="RecordWritten"/>.</summary>
        internal static void Add(GodState s, EventBuffer ev, Organisation org, int recorder, RecordKind kind, int reference, Ware w, int count, int seenTick, bool falsified)
        {
            var e = new RecordEntry { Id = org.NextRecordId++, Tick = s.Tick, Recorder = recorder, Kind = kind, Ref = reference, Ware = w, Count = count, SeenTick = seenTick, Falsified = falsified };
            if (org.Records.Count >= Organisation.MaxRecords) org.Records.RemoveAt(0);
            org.Records.Add(e);
            org.DayRecords++;
            if (kind == RecordKind.Outcome)
            {
                int ri = Services.IndexOf(org, reference);
                if (ri >= 0) { var r = org.Requests[ri]; r.RecordedOutcome = (short)count; org.Requests[ri] = r; ev?.Add(new ServiceOutcomeRecorded { Request = reference, Org = org.Id, Recorder = recorder, RecordedOutcome = count, TrueOutcome = r.Status == RequestStatus.Done ? 1 : 0 }); }
            }
            ev?.Add(new RecordWritten { Org = org.Id, Recorder = recorder, Kind = kind, Ref = reference, Ware = w, Count = count, Falsified = falsified });
        }

        /// <summary>
        /// The organisation's stock as its records say (layer 4): recorded deposits minus recorded withdrawals and rations, per
        /// ware, in <paramref name="into"/> (ware-indexed, length at least <c>Wares.PersonalCount</c>). Never the bin itself.
        /// </summary>
        public static void RecordedStock(Organisation org, int[] into)
        {
            System.Array.Clear(into, 0, into.Length);
            foreach (var r in org.Records)
            {
                int w = (int)r.Ware;
                if (w <= 0 || w >= into.Length) continue;
                if (r.Kind == RecordKind.Deposit) into[w] += r.Count;
                else if (r.Kind == RecordKind.Withdrawal || r.Kind == RecordKind.Ration) into[w] -= r.Count;
                else if (r.Kind == RecordKind.StockCount) into[w] = r.Count;
            }
        }

        /// <summary>
        /// A role-holder with <see cref="OrgPower.KeepRecords"/> who is awake, alive and within its sight of <paramref name="node"/>
        /// sees what happened there (its mind's witness buffer). Called by the acts that move the organisation's goods.
        /// </summary>
        public static void Witness(GodState s, Organisation org, int node, RecordKind kind, int reference, Ware w, int count, int actor)
        {
            var p = s.Population; var g = s.Territory.Grid;
            bool night = Population.PopulationSystem.TimeOf(SimCalendar.Of(s), s.Tick) == TimeOfDay.Night;
            int sight = Comms.CommsRange.Gesture(s.Tuning.Population, night);
            foreach (var o in org.Offices)
            {
                if (o.Holder == 0 || (o.Powers & OrgPower.KeepRecords) == 0) continue;
                var n = p.Find(o.Holder);
                if (n == null || !Bodies.IsLivingPerson(n) || n.Action.Asleep || n.Mind == null) continue;
                if (n.Id != actor && g.Distance(n.Node, node) > sight) continue;
                if (n.Mind.Orgs == null) n.Mind.Orgs = Orgs.NewMind(s);
                n.Mind.Orgs.Remember(new Witnessed { Org = org.Id, Kind = kind, Ref = reference, Ware = w, Count = count, Tick = (int)s.Tick });
            }
        }
    }
}
