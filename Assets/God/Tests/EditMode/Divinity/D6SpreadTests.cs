using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Divinity;
using God.Sim.Population;
using God.Sim.Learning;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// D6 spec 14, brief §47 scenario 13: a commandment is revealed, learned and communicated person to person (T1 CR1),
    /// copied with misunderstanding along chains (T4 CR4), forgotten (T6 CR6), revised and revoked (T8). Rates over fixed
    /// seed sets are printed. OWNER: D6.
    /// </summary>
    public class D6SpreadTests
    {
        /// <summary>One run of CR1: reveal T1 to three believers of a village of 20 (telling on), 3 days. Returns (holders learned-or-better, every holder traces to the revelation).</summary>
        public static (int holders, bool traced, int recipients, int told) Spread(ulong seed, int days = 3, Action<SimTuning> tune = null)
        {
            var sim = D6Fixtures.Village(seed, tune);
            var probe = new DivineProbe(sim);
            var village = D6Fixtures.People(sim, Faction.F0);
            var adults = village.Where(n => n.Stage != LifeStage.Child).ToList();
            var centre = adults[0];
            // three believers: the centre and its two nearest adults of the village
            var g = sim.State.Territory.Grid;
            var believers = adults.OrderBy(n => g.Distance(n.Node, centre.Node)).ThenBy(n => n.Id).Take(3).ToList();
            foreach (var b in believers) D6Fixtures.Believe(sim, b, 1, 900, 600, 300, 0, 800);
            var target = D6Fixtures.Gathered(sim, centre, believers, 3);
            int cid = D6Fixtures.Reveal(sim, 1, target);
            probe.Step(days * D6Fixtures.Day);
            var learned = probe.Of<CommandmentLearned>().Where(e => e.Commandment == cid).ToList();
            var holders = sim.State.Population.Npcs.Where(n => Bodies.IsLivingPerson(n))
                .Where(n => { var st = D6Fixtures.StanceOf(sim, n, cid); return st == CommandmentStance.Learned || st == CommandmentStance.Adopted; }).ToList();
            // trace: each holder's first learning names a source that itself learned earlier, back to a revealed recipient
            var first = new Dictionary<int, CommandmentLearned>();
            foreach (var e in learned) if (!first.ContainsKey(e.Npc)) first[e.Npc] = e;
            bool traced = true;
            foreach (var h in holders)
            {
                int at = h.Id, hops = 0;
                while (hops++ < 64 && first.TryGetValue(at, out var e) && e.Source != CommandmentSource.Revealed && e.Source != CommandmentSource.Witnessed) at = e.FromNpc;
                if (!first.TryGetValue(at, out var root) || root.Source != CommandmentSource.Revealed && root.Source != CommandmentSource.Witnessed) { traced = false; break; }
            }
            int recipients = learned.Count(e => e.Source == CommandmentSource.Revealed);
            return (holders.Count, traced, recipients, probe.Of<CommandmentLearned>().Count(e => e.Source == CommandmentSource.Taught));
        }

        [Test]
        public void Revealed_Learned_Communicated()
        {
            int ok = 0, n = 30;
            var hs = new List<int>();
            for (ulong seed = 1; seed <= (ulong)n; seed++)
            {
                var r = Spread(seed);
                hs.Add(r.holders);
                Assert.IsTrue(r.traced, "seed " + seed + ": every holder traces back through FromNpc to the revelation");
                Assert.GreaterOrEqual(r.recipients, 3, "seed " + seed);
                if (r.holders >= 8) ok++;
            }
            TestContext.Progress.WriteLine("D6-T1 holders after 3 days per seed: " + string.Join(",", hs) + "; >= 8 in " + ok + "/" + n);
            Assert.GreaterOrEqual(ok, 24, ">= 8 holders in >= 80% of 30 seeds");
        }
    
        // ---------------------------------------------------------------- D6-T4 (CR4)

        [Test]
        public void Misunderstanding_MakesVariants()
        {
            var sim = D3MiracleTests.Game(4, t => { t.Divinity.Doctrine.Telling = 0; });
            int node = LearningFixtures.Glade(sim);
            var chain = Enumerable.Range(0, 5).Select(i => D3MiracleTests.Person(sim, LearningFixtures.Step(sim, node, 0, i))).ToList();
            int cid = D6Fixtures.Reveal(sim, 1, new DivineTarget(DivineTargetKind.Npc, chain[0].Id));
            var s = sim.State;
            int sight = Comms.CommsRange.Gesture(s.Tuning.Population, true);
            int chance = Doctrine.TellChance(s, sight, sight, true); // at night, at the edge of sight
            int rules = CommandmentTemplates.Get(1).Rules.Length;
            double expected = 1 - Math.Pow(1 - chance / 1000.0, rules);
            int hops = 0, altered = 0, passedAsHeld = 0, carried = 0;
            var ev = new EventBuffer();
            for (int run = 0; run < 200; run++)
            {
                foreach (var p in chain.Skip(1)) { var m = p.Mind.Divine; if (m != null) { int i = m.IndexOfKnown(cid); if (i >= 0) m.RemoveKnownAt(i); } }
                D6Fixtures.Hold(sim, chain[0], cid, 900, 900);
                for (int h = 1; h < chain.Count; h++)
                {
                    var spoken = D6Fixtures.Known(chain[h - 1], cid).Value;
                    Doctrine.TellTo(s, ev, chain[h - 1], chain[h], spoken, sight, sight, true);
                    var heard = D6Fixtures.Known(chain[h], cid).Value;
                    hops++;
                    bool differs = !heard.Interpretation.Equals(spoken.Interpretation) || !heard.Interpretation2.Equals(spoken.Interpretation2);
                    if (differs) altered++;
                    else { passedAsHeld++; if (spoken.VariantMask != 0) carried++; }
                }
                sim.Step(); // a new tick: new draws
            }
            double share = (double)altered / hops;
            TestContext.Progress.WriteLine($"D6-T4 chain of 5 at night at the edge of sight, 200 runs: field error {chance} per mille per rule, expected altered share per hop {expected:P1}, observed {share:P1} ({altered}/{hops}); unaltered hops that passed on an earlier variant as held: {carried}");
            Assert.That(Math.Abs(share - expected), Is.LessThanOrEqualTo(0.03), "within +-3 points of the computed rate");
            Assert.Greater(carried, 0, "a variant is passed on as held, not as the original");
        }

        // ---------------------------------------------------------------- D6-T6 (CR6)

        [Test]
        public void Forgetting()
        {
            var sim = D3MiracleTests.Game(6, t => { t.Divinity.Doctrine.Telling = 0; });
            int node = LearningFixtures.Glade(sim);
            var n = D3MiracleTests.Person(sim, node);
            int cid = D6Fixtures.Reveal(sim, 12, new DivineTarget(DivineTargetKind.Npc, n.Id));
            D6Fixtures.Hold(sim, n, cid, 400, 35);
            var probe = new DivineProbe(sim);
            var seen = new List<int>();
            for (int d = 0; d < 6; d++)
            {
                probe.Step(D6Fixtures.Day);
                var k = D6Fixtures.Known(n, cid);
                seen.Add(k?.Salience ?? 0);
                if (k == null) break;
            }
            TestContext.Progress.WriteLine("D6-T6 salience by day: " + string.Join(",", seen));
            for (int i = 1; i < seen.Count; i++) Assert.Less(seen[i], seen[i - 1], "salience falls daily");
            Assert.IsNull(D6Fixtures.Known(n, cid), "forgotten at 0");
            Assert.AreEqual(1, probe.Of<CommandmentForgotten>().Count(e => e.Npc == n.Id && e.Commandment == cid && !e.Evicted));
        }

        // ---------------------------------------------------------------- D6-T8

        [Test]
        public void Revise_OnlyToldUpdate_Revoke_CannotErase()
        {
            var sim = D3MiracleTests.Game(8, t => { t.Divinity.Doctrine.Telling = 0; t.Divinity.Doctrine.Witnesses = 0; });
            int node = LearningFixtures.Glade(sim);
            var told = D3MiracleTests.Person(sim, node);
            var untold = D3MiracleTests.Person(sim, LearningFixtures.Step(sim, node, 0, 6));
            D6Fixtures.Believe(sim, told, 1, 900, 600, 300, 0, 800);
            int cid = D6Fixtures.Reveal(sim, 12, new DivineTarget(DivineTargetKind.Npc, told.Id));
            D6Fixtures.Hold(sim, untold, cid, 700, 800);
            Assert.IsTrue(sim.Submit(new ReviseCommandment { God = 1, Commandment = cid, Rules = CommandmentTemplates.RulesOf(7), Importance = 4 }).Ok);
            sim.Step();
            Assert.AreEqual(2, D6Fixtures.Known(told, cid).Value.Revision, "the one the god spoke to has revision 2");
            Assert.AreEqual(1, D6Fixtures.Known(untold, cid).Value.Revision, "the untold keeps revision 1");
            Assert.AreEqual(CommandmentTemplates.Tend, D6Fixtures.Known(untold, cid).Value.Interpretation.LearningVerb);
            Assert.AreEqual(ConditionKind.TargetInjured, D6Fixtures.Known(untold, cid).Value.Interpretation.Condition, "and its old rule (it keeps following it)");
            int beforeTold = D6Fixtures.Known(told, cid).Value.Adoption, beforeUntold = D6Fixtures.Known(untold, cid).Value.Adoption;
            Assert.IsTrue(sim.Submit(new RevokeCommandment { God = 1, Commandment = cid }).Ok);
            sim.Step();
            var kt = D6Fixtures.Known(told, cid).Value; var ku = D6Fixtures.Known(untold, cid).Value;
            Assert.AreEqual(beforeTold + sim.State.Tuning.Divinity.Doctrine.RevokeShift, kt.Adoption, "told: -600");
            Assert.IsTrue(kt.ToldRevoked);
            Assert.AreEqual(beforeUntold, ku.Adoption, "untold: unchanged");
            Assert.AreEqual(CommandmentStance.Adopted, D6Fixtures.StanceOf(sim, untold, cid), "the untold go on following it");
            Assert.IsNotNull(D6Fixtures.Known(told, cid), "the god cannot erase knowledge");
        }
}
}
