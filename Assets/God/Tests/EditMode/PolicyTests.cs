using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using God.Sim.Information;
using God.Sim.Policy;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// M1: the policy seams (DESIGN_CONVENTIONS §7), the registry (M1.6) and the reflection gates M1.1 / G4.1 (no policy
    /// reads GodState) and the policy half of M1.2 (policies reach only the view), with the overlay unreachable (M1.8).
    /// </summary>
    public class PolicyTests
    {
        /// <summary>GodState and every module state, plus the sim object (its State is the back door).</summary>
        public static readonly Type[] StateTypes =
        {
            typeof(GodState), typeof(EconomyState), typeof(TerritoryState), typeof(MilitaryState), typeof(TradeState), typeof(FactionsState), typeof(GodSim),
            typeof(Population.PopulationState), typeof(Population.PopulationSystem), typeof(Population.Npc),
        };

        /// <summary>The debug overlay and the adapter that builds views and overlays: never reachable from a policy.</summary>
        public static Type[] OverlayTypes() => new[]
        {
            typeof(ITrueStateOverlay), typeof(ViewFiller), typeof(GodSim).Assembly.GetType("God.Sim.Information.TrueStateOverlay", true),
        };

        private static readonly Type[] PolicyInterfaces = { typeof(INpcPolicy), typeof(IMilitaryPolicy), typeof(IInstitutionPolicy), typeof(IFactionPolicy), typeof(IGodPolicy) }; // F1-T11: the god level

        /// <summary>Every type of the policy namespaces, and every implementation of a policy interface wherever it lives.</summary>
        public static List<Type> PolicyTypes()
        {
            var l = new List<Type>();
            foreach (var t in typeof(GodSim).Assembly.GetTypes())
            {
                if (t.IsNested) continue; // nested types (closures, lambdas) are scanned with their parent
                bool inNs = t.Namespace == "God.Sim.Policy" || t.Namespace == "God.Sim.Cognition" || t.Namespace == "God.Sim.Learning"; // M3: the mind (G4.1)
                bool impl = PolicyInterfaces.Any(i => i.IsAssignableFrom(t));
                if (inNs || impl) l.Add(t);
            }
            return l;
        }

        /// <summary>
        /// Types outside the policy namespaces that policies may refer to although they read state, each with its reason
        /// (the explicit allow-list of M1.1; it only shrinks).
        /// </summary>
        public static readonly Dictionary<string, string> Frontier = new Dictionary<string, string>
        {
            ["God.Sim.AiBrain"] = "TEMPORARY (removal M14): the stage 3 brain behind AiBrainFactionPolicy still reads GodState itself (DEV_LOG temporary mechanisms).",
            ["God.Sim.Information.FactionKnowledgeView"] = "The view: the only reader of truth on the player side, answering filtered (INFORMATION 4.8).",
            ["God.Sim.Information.MapKnowledge"] = "Part of the view (the island as seen).",
            ["God.Sim.Information.OwnBooks"] = "Part of the view (own books).",
            ["God.Sim.Information.MilitaryKnowledge"] = "Part of the view (soldiers seen, own flags and marks).",
            ["God.Sim.Information.DiplomacyKnowledge"] = "Part of the view (declarations and standings).",
            ["God.Sim.Information.TradeKnowledge"] = "Part of the view (own trade, envoys seen).",
            ["God.Sim.Information.PopulationKnowledge"] = "Part of the view (M3: own citizens' inspection cards, sightings of others).",
            ["God.Sim.FactionCommand"] = "Command data; its ValidIn(GodState) helper is called by the handlers, never by a policy.",
        };

        // ---------------------------------------------------------------- M1.1 / G4.1

        [Test]
        public void Policies_DoNotReferenceGodState()
        {
            var types = PolicyTypes();
            Assert.That(types.Select(t => t.Name), Is.SupersetOf(new[] { "IFactionPolicy", "IMilitaryPolicy", "IInstitutionPolicy", "INpcPolicy", "AiBrainFactionPolicy", "Stage3MilitaryPolicy", "PolicyRegistry",
                "AssociativeNpcPolicy", "MindView", "NpcObservation", "Thinker", "ThinkScratch", "NpcMind", "AssociationStore", "Learner" }));
            var bad = new List<string>();
            var forbidden = new HashSet<Type>(StateTypes.Concat(OverlayTypes()));
            foreach (var t in types) bad.AddRange(TypeReferenceScanner.Forbidden(t, forbidden).Select(r => r.ToString()));
            Assert.IsEmpty(bad, "policy types refer to true state or the overlay:\n" + string.Join("\n", bad.Distinct()));
        }

        [Test]
        public void Policies_ReachStateOnlyThroughTheAllowList()
        {
            // Types the policies refer to that themselves read state: each must be on the frontier allow-list with a reason.
            var forbidden = new HashSet<Type>(StateTypes);
            var asm = typeof(GodSim).Assembly;
            var found = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var t in PolicyTypes())
                foreach (var r in TypeReferenceScanner.TypesOf(t))
                {
                    if (r.Assembly != asm || r.Namespace == "God.Sim.Policy" || forbidden.Contains(r)) continue;
                    var outer = r; while (outer.IsNested) outer = outer.DeclaringType;
                    if (TypeReferenceScanner.Forbidden(outer, forbidden).Count > 0) found.Add(outer.FullName);
                }
            var unlisted = found.Where(n => !Frontier.ContainsKey(n)).ToList();
            Assert.IsEmpty(unlisted, "policies reach state through types not on the allow-list: " + string.Join(", ", unlisted));
            foreach (var kv in Frontier) Assert.IsFalse(string.IsNullOrWhiteSpace(kv.Value), kv.Key + " has no reason");
            CollectionAssert.Contains(found, "God.Sim.AiBrain", "the temporary AiBrain reference is still there (remove its allow-list row when it goes)");
        }

        [Test]
        public void Scanner_CatchesAGodStateReference()
        {
            var refs = TypeReferenceScanner.Forbidden(typeof(BadPolicy), new HashSet<Type>(StateTypes));
            Assert.IsNotEmpty(refs, "the scanner sees a policy that reads the sim through a field and a call");
            Assert.That(refs.Select(r => r.Type), Has.Member(typeof(GodState)));
        }

        private sealed class BadPolicy : IFactionPolicy
        {
            private GodSim _sim = null;
            public string Name => "bad";
            public PolicyLevel Level => PolicyLevel.Faction;
            public void Decide(in FactionKnowledgeView view, ref Rng rng, List<Command> outCommands) { if (_sim.State.Tick > 0) outCommands.Clear(); }
        }

        // ---------------------------------------------------------------- M1.8 (policy half): no policy can receive the overlay

        [Test]
        public void TheOverlay_IsUnreachableFromPoliciesAndTheView()
        {
            // No policy interface method takes it, and nothing the view answers returns or takes it (or the state).
            var overlay = new HashSet<Type>(OverlayTypes());
            foreach (var i in PolicyInterfaces)
                foreach (var m in i.GetMethods())
                    foreach (var p in m.GetParameters()) Assert.IsFalse(overlay.Contains(p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType), i.Name + "." + m.Name);
            var forbidden = new HashSet<Type>(StateTypes.Concat(overlay));
            foreach (var vt in ViewTypes())
                foreach (var m in vt.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    var exposed = new List<Type>();
                    if (m is MethodInfo mi) { exposed.Add(mi.ReturnType); exposed.AddRange(mi.GetParameters().Select(p => p.ParameterType)); }
                    if (m is PropertyInfo pi) exposed.Add(pi.PropertyType);
                    if (m is FieldInfo fi) exposed.Add(fi.FieldType);
                    foreach (var e in exposed) Assert.IsFalse(forbidden.Contains(e), vt.Name + "." + m.Name + " exposes " + e.Name);
                }
        }

        public static Type[] ViewTypes() => new[] { typeof(FactionKnowledgeView), typeof(MapKnowledge), typeof(OwnBooks), typeof(MilitaryKnowledge), typeof(DiplomacyKnowledge), typeof(TradeKnowledge), typeof(PopulationKnowledge), typeof(NpcInspection) };

        // ---------------------------------------------------------------- M1.6 registry

        [Test]
        public void Registry_ResolvesNames_RefusesUnknownExternalAndTestOnly()
        {
            var sim = GodSim.Create(GameSetup.Watch(3));
            var r = StandardPolicies.Registry(sim);
            CollectionAssert.AreEqual(new[] { "none", "AiBrain" }, r.Names(PolicyLevel.Faction));
            CollectionAssert.AreEqual(new[] { "none", "Stage3MilitaryPolicy" }, r.Names(PolicyLevel.Military));
            CollectionAssert.AreEqual(new[] { "none", "AssociativeNpcPolicy" }, r.Names(PolicyLevel.Civilian), "M3: the default civilian policy");
            Assert.IsInstanceOf<AssociativeNpcPolicy>(r.Create(PolicyLevel.Civilian, "AssociativeNpcPolicy", -1));
            var p = r.CreateFaction("AiBrain", Faction.F2);
            Assert.IsInstanceOf<AiBrainFactionPolicy>(p);
            Assert.AreEqual(Faction.F2, ((AiBrainFactionPolicy)p).Side);
            Assert.AreEqual("AiBrain", p.Name);
            Assert.IsNull(r.CreateFaction("none", Faction.F1), "none: the slot decides nothing");
            Assert.IsInstanceOf<Stage3MilitaryPolicy>(r.CreateMilitary("Stage3MilitaryPolicy", 0));

            var unknown = Assert.Throws<PolicyException>(() => r.CreateFaction("AiBrian", Faction.F1));
            StringAssert.Contains("unknown policy 'AiBrian'", unknown.Message);
            StringAssert.Contains("M15", Assert.Throws<PolicyException>(() => r.Validate(PolicyLevel.Faction, "external")).Message);
            Assert.Throws<PolicyException>(() => r.Validate(PolicyLevel.Civilian, "AiBrain"), "names are per level");

            r.Register(PolicyLevel.Faction, "truth.cheat", ctx => new IdleFactionPolicy(), "test only", testOnly: true);
            StringAssert.Contains("test-only", Assert.Throws<PolicyException>(() => r.Validate(PolicyLevel.Faction, "truth.cheat")).Message);
            Assert.DoesNotThrow(() => r.Validate(PolicyLevel.Faction, "truth.cheat", allowTestOnly: true));
            CollectionAssert.DoesNotContain(r.Names(PolicyLevel.Faction), "truth.cheat", "test-only policies are not listed for normal games");
            Assert.Throws<ArgumentException>(() => r.Register(PolicyLevel.Faction, "AiBrain", ctx => new IdleFactionPolicy(), "twice"));
            Assert.Throws<ArgumentException>(() => r.Register(PolicyLevel.Faction, "none", ctx => new IdleFactionPolicy(), "reserved"));
            r.Register(PolicyLevel.Faction, "wrong.level", ctx => new Stage3MilitaryPolicy() as IPolicy, "builds a military policy");
            Assert.Throws<PolicyException>(() => r.CreateFaction("wrong.level", Faction.F0));
        }

        [Test]
        public void Registry_ConstructionIsDeterministic_TwoSimsSameAssignment_SameHashes()
        {
            ulong Run()
            {
                var b = SimBuilder.Create(GameSetup.Watch(21), null, true, Assignment(("faction", 3, "none")));
                b.Sim.Step(3000); b.Sim.DrainEvents();
                return b.Sim.StateHash();
            }
            Assert.AreEqual(Run(), Run());
        }

        public static PolicyAssignment Assignment(params (string level, int scope, string name)[] items)
        {
            var a = PolicyAssignment.Stage3Default();
            foreach (var (level, scope, name) in items)
            {
                Assert.IsTrue(PolicyRegistry.TryParseLevel(level, out var l));
                if (scope < 0) a.SetDefault(l, name); else a.Set(l, scope, name);
            }
            return a;
        }

        [Test]
        public void Assignment_PerFaction_SeatsTheNamedPolicies()
        {
            var b = SimBuilder.Create(GameSetup.Watch(4), null, true, Assignment(("faction", 1, "none"), ("faction", 4, "none")));
            var d = b.Director;
            CollectionAssert.AreEqual(new[] { Faction.F0, Faction.F2, Faction.F3, Faction.F5 }, d.Seats.Select(s => s.Side).ToArray());
            Assert.IsNull(d.PolicyOf(Faction.F1));
            Assert.IsInstanceOf<AiBrainFactionPolicy>(d.PolicyOf(Faction.F0));
            CollectionAssert.AreEqual(d.Seats.Select(s => s.Side).ToArray(), d.Brains.Select(x => x.Side).ToArray());
            foreach (var s in d.Seats) Assert.AreEqual(s.Side, s.View.Faction, "each seat reads its own faction's view");
            b.Sim.Step(2000); b.Sim.DrainEvents();
            Assert.AreEqual(1, b.Sim.State.Territory.Buildings.Count(x => x.Faction == Faction.F1), "an idle faction has only its HQ");
            Assert.Greater(b.Sim.State.Territory.Buildings.Count(x => x.Faction == Faction.F2), 1, "a brain builds");

            Assert.IsNull(SimBuilder.Create(GameSetup.Watch(4), null, true, Assignment(("faction", -1, "none"))).Director, "all none: no director, as before M1");
            Assert.Throws<PolicyException>(() => SimBuilder.Create(GameSetup.Watch(4), null, true, Assignment(("faction", 2, "Nobody"))));
            Assert.Throws<PolicyException>(() => SimBuilder.Create(GameSetup.Watch(4), null, true, Assignment(("military", -1, "none"))));
            Assert.Throws<PolicyException>(() => SimBuilder.Create(GameSetup.Watch(4), null, true, Assignment(("civilian", -1, "Stage3MilitaryPolicy"))));
        }

        [Test]
        public void ExplicitStage3Assignment_EqualsNoAssignment()
        {
            ulong Run(PolicyAssignment a)
            {
                var b = SimBuilder.Create(GameSetup.Watch(8), null, true, a);
                for (int i = 0; i < 6000 && !b.Sim.State.IsGameOver; i++) { b.Sim.Step(); b.Sim.DrainEvents(); }
                return b.Sim.StateHash();
            }
            ulong plain = Run(null);
            Assert.AreEqual(plain, Run(Assignment(("faction", -1, "AiBrain"), ("military", -1, "Stage3MilitaryPolicy"), ("faction", 2, "AiBrain"), ("military", 3, "Stage3MilitaryPolicy"))));
        }

        // ---------------------------------------------------------------- intents and commands (M1.6 derived, M1.3 policy half)

        [Test]
        public void Intents_ALevelCannotNameAVerbOutsideItsInterface()
        {
            Assert.IsTrue(IntentRules.Allowed(PolicyLevel.Military, NpcVerb.Attack));
            Assert.IsTrue(IntentRules.Allowed(PolicyLevel.Military, NpcVerb.None));
            Assert.IsFalse(IntentRules.Allowed(PolicyLevel.Military, NpcVerb.Eat));
            Assert.IsFalse(IntentRules.Allowed(PolicyLevel.Civilian, NpcVerb.Desert));
            Assert.IsFalse(IntentRules.Allowed(PolicyLevel.Faction, NpcVerb.Move), "faction policies act through commands only");
            Assert.Throws<PolicyException>(() => IntentRules.Require(PolicyLevel.Military, "x", new NpcIntent(NpcVerb.Work)));

            // A military policy that returns a civilian verb is refused by the sim, not carried out.
            var sim = GodSim.Create(GameSetup.Watch(5));
            SimBuilder.MilitaryOf(sim).SetPolicy(Faction.F0, new RoguePolicy());
            Assert.IsTrue(sim.Submit(new DevSpawnUnit(Faction.F0, UnitKind.Infantry, sim.State.Economy.HqsOf(Faction.F0)[0].Node + 0)).Ok, "spawn");
            Assert.Throws<PolicyException>(() => sim.Step(), "the new unit's policy named an intent its level may not (it thinks on the tick it appears)");
        }

        private sealed class RoguePolicy : IMilitaryPolicy
        {
            public string Name => "rogue";
            public PolicyLevel Level => PolicyLevel.Military;
            public NpcIntent Decide(in SoldierObservation obs, MindView mind, ref Rng rng) => new NpcIntent(NpcVerb.Eat);
        }

        [Test]
        public void FactionCommandSet_IsEveryFactionCommand_AndTheDirectorDropsTheRest()
        {
            var names = FactionCommandSet.Types().Select(t => t.Name).ToList();
            CollectionAssert.AreEquivalent(new[]
            {
                "AnswerTrade", "BuildRoad", "DeclareWar", "MarkTarget", "OfferPeace", "OfferTrade", "PlaceBuilding", "PlaceFlag", "PlaceHq",
                "RemoveBuilding", "RemoveDefend", "RemoveRoad", "SetDefend", "SetInputPriority", "SetPaused", "UnmarkTarget", "UpgradeBuilding", "UpgradeToCitadel",
            }, names);
            Assert.IsFalse(names.Contains("DevSpawnUnit"));

            var sim = GodSim.Create(GameSetup.Watch(6));
            var reg = new PolicyRegistry();
            reg.Register(PolicyLevel.Faction, "rogue", ctx => new ImpostorPolicy(sim), "sends commands it may not");
            var d = AiDirector.Attach(sim, new[] { Faction.F0 }, Assignment(("faction", -1, "rogue")), reg);
            for (int i = 0; i < 40; i++) { sim.Step(); sim.DrainEvents(); }
            Assert.Greater(d.Inadmissible, 0);
            Assert.IsFalse(sim.State.Factions.AtWar(Faction.F1, Faction.F2), "a command in another faction's name was dropped");
            Assert.AreEqual(0, sim.State.Military.Units.Count, "a developer command from a policy was dropped");
            Assert.IsTrue(sim.State.Factions.AtWar(Faction.F0, Faction.F3), "its own faction command went through");
        }

        private sealed class ImpostorPolicy : IFactionPolicy
        {
            private readonly int _hq;
            public ImpostorPolicy(GodSim sim) { _hq = sim.State.Economy.HqsOf(Faction.F0)[0].Node; }
            public string Name => "rogue";
            public PolicyLevel Level => PolicyLevel.Faction;
            public void Decide(in FactionKnowledgeView view, ref Rng rng, List<Command> outCommands)
            {
                outCommands.Add(new DeclareWar(Faction.F1, Faction.F2));
                outCommands.Add(new DevSpawnUnit(Faction.F0, UnitKind.Infantry, _hq));
                outCommands.Add(new DeclareWar(Faction.F0, Faction.F3));
            }
        }

        // ---------------------------------------------------------------- Stage3MilitaryPolicy

        private static SoldierObservation Obs(SeenUnit[] units = null, SeenBuilding[] bs = null, int[] marked = null, bool guard = false, bool march = false) =>
            new SoldierObservation(1, Faction.F0, 0, 0, 10, 2000, 20000, false, guard, units ?? new SeenUnit[0], bs ?? new SeenBuilding[0], marked ?? new int[0], march, 7000, 8000);

        [Test]
        public void Stage3MilitaryPolicy_FollowsRules1To4()
        {
            var p = new Stage3MilitaryPolicy();
            Rng rng = null;
            var m = MindView.Empty;
            // rule 1: nearest unit (first on a tie), attack in reach, charge otherwise
            var i1 = p.Decide(Obs(new[] { new SeenUnit(5, 9000, 0, 81000000, 9000), new SeenUnit(3, 1500, 0, 2250000, 1500), new SeenUnit(4, 0, 1500, 2250000, 1500) }), m, ref rng);
            Assert.AreEqual((NpcVerb.Attack, IntentTarget.Unit, 3), (i1.Verb, i1.Target, i1.TargetId));
            var i2 = p.Decide(Obs(new[] { new SeenUnit(5, 9000, 0, 81000000, 9000) }), m, ref rng);
            Assert.AreEqual((NpcVerb.Charge, 5, 9000), (i2.Verb, i2.TargetId, i2.X));
            // units before buildings; buildings: a marked one in marking order first, else the nearest edge
            var bs = new[] { new SeenBuilding(20, 0, 0, 1000), new SeenBuilding(21, 0, 0, 500), new SeenBuilding(22, 0, 0, 9000) };
            Assert.AreEqual(21, p.Decide(Obs(null, bs), m, ref rng).TargetId);
            var i3 = p.Decide(Obs(null, bs, new[] { 99, 22, 20 }), m, ref rng);
            Assert.AreEqual((NpcVerb.Charge, IntentTarget.Building, 22), (i3.Verb, i3.Target, i3.TargetId));
            // rule 2 guard, rule 3 march, rule 4 wait
            Assert.AreEqual(NpcVerb.HoldPost, p.Decide(Obs(guard: true, march: true), m, ref rng).Verb);
            var i4 = p.Decide(Obs(march: true), m, ref rng);
            Assert.AreEqual((NpcVerb.Charge, IntentTarget.Point, 7000, 8000), (i4.Verb, i4.Target, i4.X, i4.Z));
            var i5 = p.Decide(Obs(), m, ref rng);
            Assert.AreEqual((NpcVerb.HoldPost, IntentTarget.None), (i5.Verb, i5.Target));
            foreach (var i in new[] { i1, i2, i3, i4, i5 }) Assert.IsTrue(IntentRules.Allowed(PolicyLevel.Military, i.Verb));
        }

        [Test]
        public void Rng_SetToFork_EqualsFork()
        {
            var root = new Rng(12345).Fork("sim");
            var a = root.Fork("soldier", 17, 400);
            var b = new Rng(); b.SetToFork(root, "soldier", 17, 400);
            Assert.AreEqual(a.State, b.State);
            Assert.AreEqual(a.NextULong(), b.NextULong());
        }
    }
}
