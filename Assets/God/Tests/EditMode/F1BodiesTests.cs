using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using God.Sim.Divinity;
using God.Sim.Information;
using God.Sim.Population;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// F1b (F1 spec 13): person-semantics loops (T4, T5), biological age (T6), the control input model (T8) and procedural
    /// names (T13). OWNER: F1.
    /// </summary>
    public class F1BodiesTests
    {
        // ---------------------------------------------------------------- F1-T4

        /// <summary>
        /// Loops over every body whose meaning is not "people", each with its reason (contracts 4.3: hashing, Find, save,
        /// goods physics, perception of bodies). Key: file name + the loop line's text (trimmed).
        /// </summary>
        public static readonly Dictionary<string, string> PersonLoopAllowList = new Dictionary<string, string>
        {
            ["People.cs|foreach (var n in p.Npcs)"] = "the people snapshot holds every manifested body: people see an avatar as a body in sight (contracts 4.2); recognition is D6",
            ["Messages.cs|foreach (var x in p.Npcs)"] = "bodies in sight competing for a listener's attention: an avatar in sight is seen like a person (4.2)",
            ["HouseholdInvariants.cs|foreach (var n in p.Npcs) if (n.Alive) foreach (var l in n.Carry) if (!l.Empty) Lot(l, \"a carry\");"] = "goods conservation counts every carrier's lots (an avatar's carry too)",
            ["PopulationInvariants.cs|foreach (var n in p.Npcs) foreach (var l in n.Carry) if (!l.Empty && l.Owner.Kind == OwnerKind.Npc && l.Owner.Id == dead) bad.Add(\"C4: lot \" + l.Id + \" is still owned by npc \" + dead + \", dead for over 3 days\");"] = "goods ownership invariant over every carrier",
            ["PopulationModule.cs|p.Stats.Founders = p.Npcs.Count;"] = "the founders at StartGame (no avatar exists before the first tick)",
            ["PopulationModule.cs|int at = p.Npcs.Count;"] = "sorted insertion by id (Find)",
            ["PopulationModule.cs|foreach (var n in p.Npcs)"] = "hashing: every record (HashPopulation)",
            ["PopulationModule.cs|foreach (var n in s.Population.Npcs) { h.Add(n.Id); if (n.Mind != null) n.Mind.AddTo(ref h); else h.Add(-1); }"] = "hashing: every mind",
            ["PopulationModule.cs|foreach (var n in s.Population.Npcs) { h.Add(n.Id); if (n.Mind?.Store != null) n.Mind.Store.AddTo(ref h); else h.Add(-1); }"] = "hashing: every learning store",
            ["PopulationState.cs|int lo = 0, hi = Npcs.Count - 1;"] = "Find (binary search over every record)",
            ["DivineActs.cs|foreach (var n in p.Npcs)"] = "D2: where a named lot is now: any carrier's carry (an avatar's too)",
            ["PopulationSystem.cs|foreach (var npc in p.Npcs)"] = "goods physics: carried lots spoil whoever carries them (DayStart spoiling)",
        };

        private static readonly Regex LoopRe = new Regex(@"foreach\s*\(\s*var\s+\w+\s+in\s+[\w.?]*\bNpcs\s*\)|\bfor\s*\(.*\bNpcs\.Count|=\s*[\w.]*\bNpcs\.Count\s*[;-]", RegexOptions.Compiled);

        public static List<string> UnguardedPersonLoops(IEnumerable<(string file, string text)> sources, out HashSet<string> usedAllow)
        {
            var bad = new List<string>();
            usedAllow = new HashSet<string>();
            foreach (var (file, text) in sources)
            {
                var lines = text.Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string t = lines[i].Trim();
                    if (t.StartsWith("//") || !LoopRe.IsMatch(lines[i])) continue;
                    bool guarded = false;
                    for (int k = i; k < Math.Min(lines.Length, i + 6); k++) if (lines[k].Contains("IsPerson(") || lines[k].Contains("IsLivingPerson(")) { guarded = true; break; }
                    if (guarded) continue;
                    string key = Path.GetFileName(file) + "|" + t;
                    if (PersonLoopAllowList.ContainsKey(key)) { usedAllow.Add(key); continue; }
                    bad.Add(Path.GetFileName(file) + ":" + (i + 1) + ": " + t);
                }
            }
            return bad;
        }

        [Test]
        public void Static_PersonLoopsUseIsPerson()
        {
            string root = GoodsTests.RepoRoot();
            Assert.IsNotNull(root);
            var dir = Path.Combine(root, "Assets", "God", "Sim");
            var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal).Select(f => (f, File.ReadAllText(f))).ToList();
            Assert.Greater(files.Count, 100);
            var bad = UnguardedPersonLoops(files, out var used);
            Assert.IsEmpty(bad, "loops over PopulationState.Npcs need Bodies.IsPerson / IsLivingPerson or an allow-list entry with a reason:\n" + string.Join("\n", bad));
            foreach (var kv in PersonLoopAllowList) Assert.IsFalse(string.IsNullOrWhiteSpace(kv.Value), kv.Key);
            var stale = PersonLoopAllowList.Keys.Where(k => !used.Contains(k)).ToList();
            Assert.IsEmpty(stale, "allow-list entries that match no loop any more (remove them):\n" + string.Join("\n", stale));
            // the scanner catches an unguarded loop
            Assert.AreEqual(1, UnguardedPersonLoops(new[] { ("X.cs", "foreach (var n in p.Npcs)\n{\n    if (!n.Alive) continue;\n}") }, out _).Count);
        }

        // ---------------------------------------------------------------- F1-T5

        /// <summary>The land-or-sea node farthest from every living person (a place no one sees).</summary>
        private static int FarNode(GodSim sim)
        {
            var g = sim.State.Territory.Grid;
            var people = sim.State.Population.Npcs.Where(Bodies.IsLivingPerson).Select(n => n.Node).ToList();
            int best = 0, bestD = -1;
            for (int v = 0; v < g.Count; v++)
            {
                int d = int.MaxValue;
                foreach (int p in people) d = Math.Min(d, g.Distance(v, p));
                if (d > bestD) { bestD = d; best = v; }
            }
            return best;
        }

        [Test]
        public void AvatarBody_SkippedByPersonLoops()
        {
            var a = F1FoundationTests.Game(21, divinity: 1);
            var b = F1FoundationTests.Game(21, divinity: 1);
            int node = FarNode(a);
            var av = F1FoundationTests.Avatar(a, 1, node);
            av.Mind = new Cognition.NpcMind(a.State.Tuning.Learning.Caps, 8, Learning.LearningMode.Off);
            av.Drives[Learning.Drive.Hunger] = 900000;
            Assert.AreEqual(b.State.Population.AliveCount, a.State.Population.AliveCount, "AliveCount counts people");
            Assert.AreEqual(b.State.Population.AliveOf(Faction.F0), a.State.Population.AliveOf(Faction.F0));
            var probe = new EventProbe(a);
            int tpd = a.State.Setup.TicksPerDay;
            for (int i = 0; i < tpd + 5; i++) { probe.Step(); b.Step(); }
            Assert.IsFalse(probe.Events.OfType<NpcDay>().Any(d => d.Npc == av.Id), "no person-day row for an avatar body");
            Assert.IsFalse(probe.Events.OfType<NpcDied>().Any(d => d.Npc == av.Id));
            Assert.IsTrue(av.Alive); Assert.AreEqual(node, av.Node, "an uncontrolled avatar body does not act");
            Assert.AreEqual(900000, av.Drives[Learning.Drive.Hunger], "no drives for an avatar body");
            Assert.AreEqual(LifeStage.Adult, av.Stage, "no ageing");
            Assert.IsEmpty(SimInvariants.Check(a.State), "invariants ignore the avatar body");
            a.State.Population.Npcs.Remove(av);
            Assert.AreEqual(b.StateHash(), a.StateHash(), "removing the avatar body restores the hash: nothing else changed");
            Assert.Throws<InvalidOperationException>(() => PopulationModule.Kill(a.State, new EventBuffer(), av, DeathCause.Wound), "an avatar is destroyed, never killed");
        }

        // ---------------------------------------------------------------- F1-T6

        [Test]
        public void AgeTicks_OffsetZero_Neutral()
        {
            var sim = F1FoundationTests.Game(22);
            for (int i = 0; i < 50; i++) sim.Step();
            var cal = SimCalendar.Of(sim.State);
            foreach (var n in sim.State.Population.Npcs)
            {
                Assert.AreEqual(0, n.BioAgeOffset);
                Assert.AreEqual(sim.State.Tick - n.BornTick, Bodies.AgeTicks(n, sim.State.Tick));
                Assert.AreEqual(cal.AgeMilli(n.BornTick, sim.State.Tick), Bodies.AgeMilli(cal, n, sim.State.Tick));
            }
        }

        private static long ElderTick(long offset)
        {
            var sim = F1FoundationTests.Game(23, tune: t => { t.Households.Life.MaxAgeBaseMilli = 6000; });
            sim.State.Setup.Disease = DiseaseSetting.Off;
            var cal = SimCalendar.Of(sim.State);
            var npc = HouseholdFixtures.Person(sim, sim.State.Population.Npcs.First(Bodies.IsLivingPerson).Node, 0, SimCalendar.ElderFromMilli - 30, hunger: 0);
            npc.BioAgeOffset = offset;
            long limit = sim.State.Tick + cal.TicksPerYear + 4 * cal.TicksPerDay;
            while (sim.State.Tick < limit)
            {
                sim.Step();
                if (!npc.Alive) Assert.Fail("the fixture died at tick " + sim.State.Tick + " (" + npc.DeathCause + ")");
                if (npc.Stage == LifeStage.Elder) return sim.State.Tick;
                npc.Drives[Learning.Drive.Hunger] = 0; npc.Nutrition = 800000; // keep the fixture fed (test-only)
            }
            return -1;
        }

        [Test]
        public void AgeTicks_Offset_DelaysAgeing()
        {
            long t0 = ElderTick(0);
            Assert.Greater(t0, 0);
            var cal = SimCalendar.Of(F1FoundationTests.Game(23).State);
            long t1 = ElderTick(cal.TicksPerYear);
            Assert.AreEqual(t0 + cal.TicksPerYear, t1, "a BioAgeOffset of one year reaches the elder stage exactly one year later");
        }

        // ---------------------------------------------------------------- F1-T8

        [Test]
        public void ControlInput_Lease_Quantisation()
        {
            foreach (int k in new[] { 4, 8, 16, 32, 64 }) Assert.IsTrue(Control.ValidSectors(k));
            foreach (int k in new[] { 0, 1, 2, 3, 5, 6, 12, 24, 63, 65, 128 }) Assert.IsFalse(Control.ValidSectors(k), k.ToString());
            var sim = F1FoundationTests.Game(24, divinity: 1);
            var s = sim.State; var ev = new EventBuffer();
            var person = s.Population.Npcs.First(Bodies.IsLivingPerson);
            Assert.IsTrue(Control.TryAcquire(s, ev, new ControlEntry { Kind = ControlKind.Possessed, God = 1, Body = person.Id, Intervention = 1 }, out var why), why);
            var h = new DivineControlInputHandler();
            Assert.IsTrue(sim.HasHandler(typeof(DivineControlInput)), "registered by D4 (D4 adjustment: F1 left it unregistered)");
            Assert.AreEqual(DivineReasons.BadTarget, h.Validate(s, new DivineControlInput { God = 2, Body = person.Id, Kind = ControlInputKind.Heading, Heading = 0, Gait = 1 }).Reason, "another god's body");
            Assert.AreEqual(DivineReasons.BadTarget, h.Validate(s, new DivineControlInput { God = 1, Body = person.Id, Kind = ControlInputKind.Heading, Heading = 16, Gait = 1 }).Reason, "sector out of range at 16");
            Assert.IsTrue(h.Validate(s, new DivineControlInput { God = 1, Body = person.Id, Kind = ControlInputKind.Heading, Heading = 15, Gait = 1 }).Ok);
            s.Tuning.Divinity.Control.HeadingSectors = 12;
            Assert.AreEqual(DivineReasons.BadTarget, h.Validate(s, new DivineControlInput { God = 1, Body = person.Id, Kind = ControlInputKind.Heading, Heading = 1, Gait = 1 }).Reason, "12 sectors refused");
            s.Tuning.Divinity.Control.HeadingSectors = 16;

            // lease: applied at tick t, holds t .. t + LeaseTicks - 1
            long t = s.Tick;
            Assert.IsTrue(h.Apply(s, new DivineControlInput { God = 1, Body = person.Id, Kind = ControlInputKind.Heading, Heading = 4, Gait = 1 }, ev).Ok);
            var e = Control.EntryOf(s, person.Id);
            Assert.AreEqual(t + s.Tuning.Divinity.Control.LeaseTicks - 1, e.LeaseUntil);
            Assert.IsTrue(Control.LeaseHolds(in e, t + 2)); Assert.IsFalse(Control.LeaseHolds(in e, t + 3), "a 3-tick lease lapses on the fourth tick");
            Control.ResolveMovement(s);
            var g = s.Territory.Grid;
            Assert.AreEqual(Math.Max(0, Control.NextNode(g, person.Node, 4, 16)), Control.EntryOf(s, person.Id).MoveNode);
            s.Tick = t + 3; Control.ResolveMovement(s);
            Assert.AreEqual(ControlInputKind.Stop, Control.EntryOf(s, person.Id).Input, "a lapsed lease halts");
            s.Tick = t;

            // the integer-angle choice: sector 0 of 4 is north (direction 0, +J); a quarter turn picks between 60 and 120 degrees by the lower id
            int c = g.Id(0, 0);
            Assert.AreEqual(Pick(g, c, 0), Control.NextNode(g, c, 0, 4));
            Assert.AreEqual(Pick(g, c, 1800), Control.NextNode(g, c, 2, 4));
            int east = Control.NextNode(g, c, 1, 4), n60 = g.Neighbour(c, 1), n120 = g.Neighbour(c, 2);
            Assert.AreEqual(Math.Min(Land(g, n60), Land(g, n120)), east, "90 degrees is equidistant from 60 and 120: the lower node id wins");
            // same input sequence twice gives the same node sequence
            var seq = new[] { 0, 3, 7, 12, 15, 9 };
            CollectionAssert.AreEqual(Walk(g, c, seq), Walk(g, c, seq));
        }

        private static int Land(NodeGrid g, int v) => v >= 0 && g.IsLand(v) ? v : int.MaxValue;

        private static int Pick(NodeGrid g, int node, int angle)
        {
            for (int dir = 0; dir < 6; dir++) if (Control.DirectionAngle[dir] == angle && g.IsLand(g.Neighbour(node, dir))) return g.Neighbour(node, dir);
            return Control.NextNode(g, node, angle * 16 / Control.FullTurn, 16);
        }

        private static List<int> Walk(NodeGrid g, int start, int[] headings)
        {
            var l = new List<int>(); int n = start;
            foreach (int h in headings) { int v = Control.NextNode(g, n, h, 16); if (v >= 0) n = v; l.Add(n); }
            return l;
        }

        // ---------------------------------------------------------------- F1-T13

        [Test]
        public void Names_ArePure()
        {
            Assert.AreEqual(Names.Person(7, 12), Names.Person(7, 12));
            Assert.AreEqual(Names.God(7, 2), Names.God(7, 2));
            Assert.AreEqual(Names.Org(7, 5, 1), Names.Org(7, 5, 1));
            Assert.IsNotEmpty(Names.Person(1, 1)); Assert.IsNotEmpty(Names.God(1, 1)); Assert.IsNotEmpty(Names.Org(1, 1, 0));
            var gods = Enumerable.Range(1, 6).Select(i => Names.God(42, i)).ToList();
            Assert.Greater(gods.Distinct().Count(), 3, "different gods mostly get different names");
            var sim = F1FoundationTests.Game(25);
            ulong h = sim.StateHash();
            foreach (var n in sim.State.Population.Npcs) Names.Person(sim.State.Seed, n.Id);
            Assert.AreEqual(h, sim.StateHash(), "calling them changes no hash");
        }

        // ---------------------------------------------------------------- F1.14 stubs

        [Test]
        public void InspectionCards_Stubs()
        {
            Assert.AreEqual(9, (int)KnowledgeLayer.Analytics);
            var card = new NpcInspection();
            Assert.IsNotNull(card.Divine); Assert.IsNotNull(card.Orgs); Assert.IsNotNull(card.Economy);
            Assert.IsEmpty(card.Divine.Lines);
        }
    }
}
