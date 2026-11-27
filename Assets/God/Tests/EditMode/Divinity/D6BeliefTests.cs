using System;
using System.Linq;
using God.Sim.Divinity;
using God.Sim.Learning;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// D6b (Tier 2, <c>Divinity.Doctrine.Beliefs = 1</c>): five separate quantities moved by perceived evidence (T12 CR9), religious
    /// affiliation that changes without touching citizenship or the community of origin (T13 CR8, I-CR-3; brief §47 scenario 16),
    /// founding religions, and D2's term reading the beliefs. OWNER: D6.
    /// </summary>
    public class D6BeliefTests
    {
        private static GodSim Game(ulong seed, Action<SimTuning> tune = null) => D3MiracleTests.Game(seed, t => { t.Divinity.Doctrine.Beliefs = 1; tune?.Invoke(t); });

        private static GodBelief Of(Npc n, int god)
        {
            var m = n.Mind.Divine;
            int i = m.IndexOfGod(god);
            Assert.GreaterOrEqual(i, 0, "a belief entry for god " + god);
            return m.Gods[i];
        }

        // ---------------------------------------------------------------- D6-T12 (CR9)

        [Test]
        public void SeparateQuantities()
        {
            var sim = Game(12);
            int node = LearningFixtures.Glade(sim);
            var n = D3MiracleTests.Person(sim, node);
            var probe = new DivineProbe(sim);
            probe.Step(1); // founding (the person is a founder of god 1's people), then the fixture's beliefs
            D6Fixtures.Believe(sim, n, 1, 600, 300, 0, 100, 300);
            for (int k = 0; k < 2; k++)
            {
                Assert.IsTrue(D3MiracleTests.Cast(sim, MiracleKind.SmitePerson, D3MiracleTests.Npc(n.Id), 100).Ok);
                probe.Step(5);
                n.Injury = 0; // test-only: heal between the strikes so the believer lives to feel the second
            }
            var g = Of(n, 1);
            TestContext.Progress.WriteLine($"D6-T12 believer smitten twice: belief {g.Belief}, faith {g.Faith}, trust {g.Trust}, fear {g.Fear}, loyalty {g.Loyalty}");
            Assert.GreaterOrEqual((int)g.Belief, 800); Assert.GreaterOrEqual((int)g.Fear, 600); Assert.LessOrEqual((int)g.Trust, -300); Assert.Less((int)g.Loyalty, 300);
            Assert.AreEqual(300, g.Faith, "faith untouched by a smiting: no single score drives all five");
            var changes = probe.Of<BeliefChanged>().Where(e => e.Npc == n.Id && e.Cause == 3).ToList();
            Assert.AreEqual(2, changes.Count, "one BeliefChanged per smiting, with its cause");
            Assert.IsTrue(changes.All(e => e.CauseRef != 0));
        }

        // ---------------------------------------------------------------- D6-T13 (CR8, I-CR-3), scenario 16

        [Test]
        public void Affiliation_Change_Keeps_Citizenship()
        {
            var sim = Game(13);
            int node = LearningFixtures.Glade(sim);
            var n = D3MiracleTests.Person(sim, node); // community F0: god 1's founding religion
            var probe = new DivineProbe(sim);
            probe.Step(1);
            var st = sim.State.Divinity.Doctrine;
            Assert.AreEqual(sim.State.Divinity.Gods.Gods.Count, st.Religions.Count, "one founding religion per god");
            Assert.IsTrue(st.Affiliations.Any(a => a.Npc == n.Id && a.Open && st.FindReligion(a.Religion).Worships(1)), "a founder of god 1's people is affiliated to its religion");
            D6Fixtures.Believe(sim, n, 1, 300, 100, 0, 0, 100); // a lukewarm founder
            var faction = n.Faction;
            int household = n.Household;
            var social = n.Mind.Social;
            ulong socialHash = SocialHash(n);
            sim.State.Divinity.Gods.Of(1).Seat = GodSeat.Ai; sim.State.Divinity.Gods.Of(2).Seat = GodSeat.Human; // test-only seat switch: god 2 acts
            for (int k = 0; k < 3; k++)
            {
                n.Injury = 500_000;
                Assert.IsTrue(D3MiracleTests.Cast(sim, MiracleKind.HealInjury, D3MiracleTests.Npc(n.Id), 400, 2).Ok);
                probe.Step(D6Fixtures.Day / 3);
            }
            probe.Step(5 * D6Fixtures.Day);
            var conv = probe.Of<AffiliationChanged>().Where(e => e.Npc == n.Id && e.Opened && e.Reason == 2).ToList();
            var g2 = Of(n, 2);
            TestContext.Progress.WriteLine($"D6-T13 healed three times by god 2: god 2 belief {g2.Belief}, trust {g2.Trust}, loyalty {g2.Loyalty}; converted {conv.Count}; community {n.Faction}, citizenship (stand-in) {Beliefs.CitizenshipOf(sim.State, n)}");
            Assert.AreEqual(1, conv.Count, "AffiliationChanged (Converted) to god 2's religion");
            Assert.AreEqual(2, conv[0].God);
            Assert.IsTrue(probe.Of<AffiliationChanged>().Any(e => e.Npc == n.Id && !e.Opened && e.Reason == 4), "it left god 1's religion");
            Assert.AreEqual(faction, n.Faction, "Npc.Faction unchanged");
            Assert.IsTrue(probe.Of<AffiliationChanged>().Where(e => e.Npc == n.Id).All(e => e.Community == (int)faction && e.Citizenship == (int)faction), "citizenship and community unchanged at every change");
            Assert.AreEqual(household, n.Household);
            Assert.AreSame(social, n.Mind.Social);
            Assert.AreEqual(socialHash, SocialHash(n), "D6 never writes SocialMind");
        }

        private static ulong SocialHash(Npc n)
        {
            var sm = n.Mind.Social;
            if (sm == null) return 0;
            ulong h = 1469598103934665603UL;
            for (int i = 0; i < sm.RelCount; i++) { h = (h ^ (ulong)sm.Rel[i].Other) * 1099511628211UL; h = (h ^ (ulong)(ushort)sm.Rel[i].Trust) * 1099511628211UL; h = (h ^ (ulong)(ushort)sm.Rel[i].Fear) * 1099511628211UL; }
            return h;
        }

        [Test]
        public void Static_NeverWritesCitizenshipFactionOrSocialMind()
        {
            foreach (var f in new[] { "Beliefs.cs", "Doctrine.cs", "DivinityMindHooks.D6.cs", "DivinityHooks.D6.cs", "DivineMind.D6.cs" })
            {
                var text = string.Join(" ", System.IO.File.ReadAllLines(System.IO.Path.Combine(GoodsTests.RepoRoot(), "Assets", "God", "Sim", "Divinity", f)).Where(l => !l.TrimStart().StartsWith("//")));
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text, @"\.Faction\s*=[^=]"), f + " never writes Npc.Faction");
                Assert.IsFalse(text.Contains(".Citizenship") || text.Contains("CivState"), f + " never touches C5's citizenship records");
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text, @"Social\.[A-Za-z\[\]\.]*\s*(=[^=]|\+\+|--|\+=|-=)"), f + " never writes SocialMind");
            }
        }

        [Test]
        public void D2Term_ReadsGodBelief()
        {
            var sim = Game(14);
            int node = LearningFixtures.Glade(sim);
            var n = D3MiracleTests.Person(sim, node);
            new DivineProbe(sim).Step(1);
            var m = n.Mind.Divine;
            var tu = sim.State.Tuning.Divinity.Instruction;
            int seeded = DivinityMindHooks.Weight(m, tu, InterventionKind.Command, 1);
            D6Fixtures.Believe(sim, n, 1, 1000, 300, 1000, 0, 500);
            int strong = DivinityMindHooks.Weight(m, tu, InterventionKind.Command, 1);
            TestContext.Progress.WriteLine($"D2 command weight from the founder seed {seeded}, at belief 1000 / trust 1000 {strong}");
            Assert.AreNotEqual(seeded, strong, "D2's term follows the person's own GodBelief");
            Assert.AreEqual(tu.AuthorityCommand + tu.CommandedPrior, strong);
        }
    }
}
