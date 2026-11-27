using System;
using System.IO;
using System.Linq;
using God.Sim.Cognition;
using God.Sim.Divinity;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// D6 spec 14: structure and invariants: validation and templates, never a restriction (T3 static part, I-CR-1), structured
    /// only (T9, I-CR-4), the term bound (T10, I-CR-2), idle neutrality (P2), save/load parity (T15), and the conditions read
    /// only the scratch. OWNER: D6.
    /// </summary>
    public class D6StructureTests
    {
        private static GodSim Small(ulong seed, Action<SimTuning> tune = null) => D3MiracleTests.Game(seed, tune);

        // ---------------------------------------------------------------- validation and templates

        [Test]
        public void Templates_Tier1SetAndValid()
        {
            foreach (int id in new[] { 1, 3, 4, 6, 7, 12 }) Assert.AreEqual(1, CommandmentTemplates.Get(id).Tier, "T" + id + " is Tier 1 (R5)");
            Assert.AreEqual(2, CommandmentTemplates.Get(11).Tier, "T11 is Tier 2 (R5)");
            foreach (int id in new[] { 2, 5, 8, 9, 10 }) Assert.AreEqual(0, CommandmentTemplates.Get(id).Tier, "T" + id + " is data only");
            for (int i = 0; i < CommandmentTemplates.All.Count; i++)
            {
                var t = CommandmentTemplates.All[i];
                Assert.IsNull(CommandmentRules.Validate(CommandmentTemplates.RulesOf(t.Id), t.Importance, t.Id), t.Name);
            }
            Assert.AreNotSame(CommandmentTemplates.RulesOf(1), CommandmentTemplates.RulesOf(1), "commands carry their own copy");
        }

        [Test]
        public void Validate_RejectsEmptyOversizedUnknown()
        {
            var ok = CommandmentTemplates.RulesOf(4);
            Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(null, 3, 0));
            Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(new CommandmentRule[0], 3, 0));
            Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(new[] { ok[0], ok[0], ok[0] }, 3, 0), "oversized");
            Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(ok, 0, 0), "importance 1-5");
            Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(ok, 6, 0));
            Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(ok, 3, 99), "unknown template");
            var r = ok[0]; r.LearningVerb = 23; Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(new[] { r }, 3, 0), "unknown verb");
            r = ok[0]; r.Modality = Modality.None; Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(new[] { r }, 3, 0));
            r = ok[0]; r.Condition = (ConditionKind)200; Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(new[] { r }, 3, 0));
            r = ok[0]; r.Applies = (ApplySelector)99; Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(new[] { r }, 3, 0));
            r = ok[0]; r.Scope = new DivineTarget(DivineTargetKind.Lot, 3); Assert.AreEqual(DivineReasons.BadTarget, CommandmentRules.Validate(new[] { r }, 3, 0));
            Assert.IsNull(CommandmentRules.Validate(ok, 3, 0), "a composed rule set (template 0) is valid");
        }

        [Test]
        public void Reveal_RefusedWithoutMutation_AndCosts()
        {
            var sim = Small(3);
            int node = LearningFixtures.Glade(sim);
            var n = D3MiracleTests.Person(sim, node);
            ulong h = sim.StateHash();
            var bad = new RevealCommandment { God = 1, Template = 4, Rules = new CommandmentRule[0], Importance = 3, Recipients = new DivineTarget(DivineTargetKind.Npc, n.Id) };
            Assert.AreEqual(DivineReasons.BadTarget, sim.Check(bad).Reason);
            Assert.AreEqual(h, sim.StateHash(), "validation never mutates");
            long before = sim.State.Divinity.Gods.Of(1).Mythic;
            int cid = D6Fixtures.Reveal(sim, 4, new DivineTarget(DivineTargetKind.Npc, n.Id));
            Assert.AreEqual(before - sim.State.Tuning.Divinity.Mythic.Costs.RevealCommandment, sim.State.Divinity.Gods.Of(1).Mythic, "25 Mythic");
            Assert.AreEqual(1, cid);
            Assert.IsNotNull(D6Fixtures.Known(n, cid), "the recipient learned it");
            Assert.IsTrue(sim.Submit(new ReviseCommandment { God = 1, Commandment = cid, Rules = CommandmentTemplates.RulesOf(12), Importance = 2 }).Ok);
            sim.Step();
            Assert.AreEqual(before - 35_000, sim.State.Divinity.Gods.Of(1).Mythic, "revise 10");
            Assert.IsTrue(sim.Submit(new RevokeCommandment { God = 1, Commandment = cid }).Ok);
            sim.Step();
            Assert.AreEqual(before - 40_000, sim.State.Divinity.Gods.Of(1).Mythic, "revoke 5");
            Assert.IsFalse(sim.Check(new RevokeCommandment { God = 1, Commandment = cid }).Ok, "already revoked");
            var cm = sim.State.Divinity.Doctrine.Find(cid);
            Assert.AreEqual(2, cm.Revision); Assert.IsTrue(cm.Revoked); Assert.AreEqual(2, cm.History.Count);
        }

        // ---------------------------------------------------------------- D6-T3 static part (I-CR-1), conditions from the scratch only

        private static string Src(string rel) => File.ReadAllText(Path.Combine(GoodsTests.RepoRoot(), "Assets", "God", "Sim", rel));

        [Test]
        public void Static_NeverRemovesOrFiltersCandidates()
        {
            foreach (var f in new[] { "Divinity/DivinityMindHooks.D6.cs", "Divinity/Doctrine.cs", "Divinity/Beliefs.cs", "Divinity/DivinityHooks.D6.cs", "Divinity/CommandmentTemplates.cs" })
            {
                var lines = Src(f).Split('\n').Where(l => !l.TrimStart().StartsWith("//") && !l.TrimStart().StartsWith("///")).ToList();
                Assert.IsFalse(lines.Any(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"CandCount\s*(=[^=]|--|-=)")), f + ": never shrinks or resets the candidate list");
                Assert.IsFalse(lines.Any(l => l.Contains("ControlCand")), f + ": never marks a controlled candidate");
                Assert.IsFalse(lines.Any(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"sc\.Cand\[[^\]]*\]\s*=[^=]")), f + ": never overwrites a candidate");
                Assert.IsFalse(lines.Any(l => l.Contains("Frustrat") || l.Contains("Refuse")), f + ": never refuses an act");
            }
            var hooks = string.Join("\n", Src("Divinity/DivinityMindHooks.D6.cs").Split('\n').Where(l => !l.TrimStart().StartsWith("//")));
            Assert.IsFalse(hooks.Contains("GodState"), "the D6 mind hooks never reference the true state");
            Assert.IsFalse(hooks.Contains("s.Population") || hooks.Contains("Npc npc"), "conditions read the think scratch only");
        }

        // ---------------------------------------------------------------- D6-T9 (CR12, I-CR-4)

        [Test]
        public void StructuredOnly_RoundTripAndNoTextInSim()
        {
            var root = Path.Combine(GoodsTests.RepoRoot(), "Assets", "God", "Sim");
            var users = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Where(f => File.ReadAllText(f).Contains("CommandmentText")).ToList();
            Assert.IsEmpty(users, "no sim code uses CommandmentText (R6/R47: it lives in the Game layer)");
            Assert.IsFalse(typeof(RevealCommandment).GetFields().Any(f => f.FieldType == typeof(string)), "the command carries rules, never text");
            Assert.IsFalse(typeof(CommandmentRule).GetFields().Any(f => f.FieldType == typeof(string)));
            var sim = Small(9);
            int node = LearningFixtures.Glade(sim);
            var n = D3MiracleTests.Person(sim, node);
            var rules = CommandmentTemplates.RulesOf(1);
            rules[0].Scope = new DivineTarget(DivineTargetKind.Location, 0, node, 5);
            rules[1].ConditionParam = (int)ConditionKind.CarryingFood | (3 << 8);
            rules[1].Applies = ApplySelector.Community;
            Assert.IsTrue(sim.Submit(new RevealCommandment { God = 1, Template = 0, Rules = rules, Importance = 4, Recipients = new DivineTarget(DivineTargetKind.Npc, n.Id) }).Ok);
            sim.Step();
            var loaded = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            Assert.AreEqual(sim.StateHash(), loaded.StateHash());
            var a = sim.State.Divinity.Doctrine.Find(1).Rules; var b = loaded.State.Divinity.Doctrine.Find(1).Rules;
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++) Assert.AreEqual(a[i], b[i], "rule " + i + " round-trips identically");
        }

        // ---------------------------------------------------------------- D6-T10 (I-CR-2)

        [Test]
        public void TermBounded()
        {
            var sim = Small(10);
            int node = LearningFixtures.Glade(sim);
            var n = D3MiracleTests.Person(sim, node);
            int cid = D6Fixtures.Reveal(sim, 4, new DivineTarget(DivineTargetKind.Npc, n.Id), 5);
            var m = n.Mind.Divine;
            // eight entries of "never gather from a low wood stand" at importance 5, full adoption and salience
            var k = m.Known[0];
            m.KnownCount = 0;
            for (int i = 0; i < DivineMind.KnownCap; i++) { var x = k; x.Commandment = 100 + i; x.Adoption = 1000; x.Salience = 1000; x.Importance = 5; m.AddKnown(x, out _); }
            var sc = new ThinkScratch { Mind = n.Mind, Traits = new int[Trait.Count], Grid = sim.State.Territory.Grid };
            sc.Traits[Trait.Conformity] = 100;
            sc.Self.Stage = LifeStage.Adult;
            sc.CandCount = 1;
            sc.Cand[0] = new Candidate { Verb = Verb.Gather, Kind = Policy.IntentTarget.Node, TargetNode = node, Class = (byte)ObjClass.WoodStand, TargetOffset = 0 };
            Thinker.TargetFeatures(sc, 0, Feature.Class(ObjClass.WoodStand), Feature.State(ObjState.StockLow));
            int term = DivinityMindHooks.DivineTerm(sc, 0);
            Assert.AreEqual(-2000, term, "eight maximal prohibitions are clamped to -2000");
            Assert.Less(Math.Abs(DivinityMindHooks.RuleTerm(sc, sim.State.Tuning.Divinity.Doctrine, m.Known[0], m.Known[0].Interpretation)), 2000);
            sc.Cand[0].TargetCount = 1; // the stand no longer looks low: the rule's condition fails
            Assert.AreEqual(0, DivinityMindHooks.DivineTerm(sc, 0), "a condition the person does not perceive never holds");
        }

        // ---------------------------------------------------------------- pin P2 and save/load parity

        [Test]
        public void Idle_NoDoctrine_TermZero()
        {
            var sim = Small(12);
            int node = LearningFixtures.Glade(sim);
            D3MiracleTests.Person(sim, node);
            new DivineProbe(sim).Step(2 * D6Fixtures.Day);
            Assert.IsNull(sim.State.Divinity.Doctrine, "no reveal, Tier 2 off: no doctrine state");
            Assert.IsTrue(sim.State.Population.Npcs.All(x => x.Mind?.Divine == null), "no divine mind");
        }

        [Test]
        public void D6_SaveLoadParity()
        {
            var sim = D6Fixtures.Village(21);
            var adults = D6Fixtures.Adults(sim, Faction.F0);
            foreach (var b in adults.Take(3)) D6Fixtures.Believe(sim, b, 1, 900, 600, 300, 0, 800);
            int cid = D6Fixtures.Reveal(sim, 1, D6Fixtures.Gathered(sim, adults[0], adults, 3));
            for (int i = 0; i < D6Fixtures.Day / 2; i++) sim.Step();
            Assert.IsTrue(sim.Submit(new ReviseCommandment { God = 1, Commandment = cid, Rules = CommandmentTemplates.RulesOf(7), Importance = 4 }).Ok);
            sim.Step();
            for (int i = 0; i < 37; i++) sim.Step(); // telling in progress
            var copy = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            Assert.AreEqual(sim.StateHash(), copy.StateHash());
            for (int c = 0; c < 30; c++)
            {
                for (int i = 0; i < 40; i++) { sim.Step(); copy.Step(); }
                Assert.AreEqual(sim.StateHash(), copy.StateHash(), "checkpoint " + c);
            }
            Assert.Greater(sim.State.Divinity.Doctrine.Told, 0, "people told it during the run");
        }
    }
}
