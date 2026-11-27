using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using God.Sim.Civ;
using God.Sim.Cognition;
using God.Sim.Divinity;
using God.Sim.Learning;
using God.Sim.Orgs;
using God.Sim.Policy;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// F1 (contracts 0, F1 spec 13): the behaviour-neutral foundation. Commands without handlers, the god-only and authorship
    /// gates, the control precedence table, the ledger hash layout, the reflection gates over the new mind-side types, the
    /// policy manifest and the v5 save. OWNER: F1.
    /// </summary>
    public class F1FoundationTests
    {
        // ---------------------------------------------------------------- fixtures

        /// <summary>A two-faction population game with the given switches; <paramref name="humanGod"/> is the setup's seat.</summary>
        internal static GodSim Game(ulong seed, int divinity = 0, int orgs = 0, int civ = 0, int humanGod = 0, Action<SimTuning> tune = null)
        {
            var t = new SimTuning();
            t.Divinity.Enabled = divinity; t.Orgs.Enabled = orgs; t.Civ.Enabled = civ;
            tune?.Invoke(t);
            var setup = new GameSetup(seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 3, Victory = VictorySpec.SandboxOnly(), HumanGod = humanGod };
            return GodSim.Create(setup, t);
        }

        /// <summary>Every concrete divine command type, each with plain default data.</summary>
        internal static List<Type> DivineCommandTypes() =>
            typeof(DivineCommand).Assembly.GetTypes().Where(t => typeof(DivineCommand).IsAssignableFrom(t) && !t.IsAbstract).OrderBy(t => t.Name, StringComparer.Ordinal).ToList();

        private static DivineCommand Make(Type t, int god)
        {
            var c = (DivineCommand)Activator.CreateInstance(t);
            c.God = god;
            return c;
        }

        /// <summary>Inserts a manifested avatar body for a god (test-only state write; D5 makes the real one).</summary>
        internal static Npc Avatar(GodSim sim, int god, int node)
        {
            var p = sim.State.Population;
            var n = new Npc
            {
                Id = DivinityIds.AvatarBodyId(god), Kind = BodyKind.Avatar, God = god, Faction = DivinityIds.OriginFactionOf(god), Node = node, Alive = true,
                Carry = new ItemLot[sim.State.Tuning.Population.CarrySlots], Policy = PolicyRegistry.None,
            };
            int at = p.Npcs.Count;
            while (at > 0 && p.Npcs[at - 1].Id > n.Id) at--;
            p.Npcs.Insert(at, n);
            return n;
        }

        // ---------------------------------------------------------------- F1-T1

        [Test]
        public void DivineCommands_NoHandler_Refused()
        {
            var types = DivineCommandTypes();
            Assert.GreaterOrEqual(types.Count, 18, "every concrete command of contracts 10.2");
            var sim = Game(11, divinity: 1, humanGod: 1);
            Assert.IsNotNull(sim.State.Divinity, "stub state with the switch on");
            ulong before = sim.StateHash();
            foreach (var t in types)
            {
                Assert.IsFalse(sim.HasHandler(t), t.Name + " has no handler in F1");
                var r = sim.Submit(Make(t, 1));
                Assert.IsFalse(r.Ok, t.Name);
                Assert.AreEqual(Reasons.Disabled, r.Reason, t.Name);
            }
            Assert.AreEqual(before, sim.StateHash(), "a refused command changes nothing (not even pending)");
            var off = Game(11);
            Assert.IsNull(off.State.Divinity);
            foreach (var t in types) Assert.AreEqual(DivineReasons.DivinityOff, off.Submit(Make(t, 1)).Reason, t.Name + " with the switch off");
        }

        // ---------------------------------------------------------------- F1-T2

        private sealed class InStepSubmitter : ISimSystem
        {
            public GodSim Sim; public Command Command; public CommandResult Result; public bool Done;
            public void Tick(GodState s, EventBuffer ev) { if (Done) return; Done = true; Result = Sim.Submit(Command); }
        }

        private static PlaceBuilding SomeFlagSite(GodSim sim, Faction f)
        {
            // any node: the gate refuses before the handler looks at it
            return new PlaceBuilding(f, 0, BuildingSize.Small);
        }

        [Test]
        public void GodOnly_RefusesExternalFactionCommand()
        {
            var t = new SimTuning(); t.Divinity.Enabled = 1;
            var setup = new GameSetup(12, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 2, Victory = VictorySpec.SandboxOnly(), Control = PlayerControl.GodOnly };
            var sim = GodSim.Create(setup, t);
            var r = sim.Submit(SomeFlagSite(sim, Faction.F0));
            Assert.AreEqual(DivineReasons.GodOnly, r.Reason, "an external faction command in god-only mode");
            // a faction policy submits inside Step: not gated (it reaches the handler, whatever the handler says)
            var sys = new InStepSubmitter { Sim = sim, Command = SomeFlagSite(sim, Faction.F0) };
            sim.AddSystem(899, sys);
            sim.Step();
            Assert.IsTrue(sys.Done);
            Assert.AreNotEqual(DivineReasons.GodOnly, sys.Result.Reason, "in-step faction commands are not refused by the gate");
            // the same run without the external command has the same hash (a refused command leaves no trace)
            var a = GodSim.Create(setup, t); var b = GodSim.Create(setup, t);
            a.Submit(SomeFlagSite(a, Faction.F1));
            for (int i = 0; i < 5; i++) { a.Step(); b.Step(); }
            Assert.AreEqual(b.StateHash(), a.StateHash());
            // god-only with a human faction, or with Divinity off, cannot be created
            Assert.Throws<ArgumentOutOfRangeException>(() => GodSim.Create(new GameSetup(12, 0, 2) { Population = true, Control = PlayerControl.GodOnly, Victory = VictorySpec.SandboxOnly() }, t));
            Assert.Throws<ArgumentOutOfRangeException>(() => GodSim.Create(new GameSetup(12, GameSetup.NoHuman, 2) { Population = true, Control = PlayerControl.GodOnly, Victory = VictorySpec.SandboxOnly() }, new SimTuning()));
            // stage 3 mode: external faction commands are not gated
            var s3 = Game(12, divinity: 1);
            Assert.AreNotEqual(DivineReasons.GodOnly, s3.Submit(SomeFlagSite(s3, Faction.F0)).Reason);
        }

        // ---------------------------------------------------------------- F1-T3

        [Test]
        public void Authorship_ExternalNeedsHumanSeat()
        {
            var sim = Game(13, divinity: 1, humanGod: 1);
            Assert.AreEqual(DivineReasons.NotYourGod, sim.Submit(new IssueSuggestion { God = 2 }).Reason, "god 2 is not the human's");
            Assert.AreEqual(DivineReasons.NotYourGod, sim.Submit(new IssueSuggestion { God = 0 }).Reason);
            Assert.AreEqual(Reasons.Disabled, sim.Submit(new IssueSuggestion { God = 1 }).Reason, "the human's god passes the gate (no handler yet)");
            var watch = Game(13, divinity: 1, humanGod: 0);
            Assert.AreEqual(DivineReasons.NotYourGod, watch.Submit(new IssueSuggestion { God = 1 }).Reason, "watch mode: no god holds the Human seat");
            // in-step (an AI god through the director, D1): not refused by the authorship gate
            var sys = new InStepSubmitter { Sim = sim, Command = new IssueSuggestion { God = 2 } };
            sim.AddSystem(910, sys);
            sim.Step();
            Assert.AreEqual(Reasons.Disabled, sys.Result.Reason);
            Assert.Throws<ArgumentOutOfRangeException>(() => Game(13, divinity: 1, humanGod: 3));
        }

        // ---------------------------------------------------------------- F1-T7

        private static ControlEntry Spell(ControlKind k, int god, int body, long expires = 0) =>
            new ControlEntry { Kind = k, God = god, Body = body, ExpiresTick = expires, Intervention = 1 };

        [Test]
        public void ControlTable_Precedence()
        {
            var sim = Game(14, divinity: 1);
            var s = sim.State; var ev = new EventBuffer();
            var people = s.Population.Npcs.Where(Bodies.IsLivingPerson).ToList();
            Assert.GreaterOrEqual(people.Count, 3);
            int a = people[0].Id, b = people[1].Id, c = people[2].Id;
            Assert.AreEqual(ControlKind.Self, Control.Of(s, a));
            Assert.IsNull(s.Divinity.Control, "no table until a spell opens");

            Assert.IsTrue(Control.TryAcquire(s, ev, Spell(ControlKind.Compelled, 1, a), out var why), why);
            Assert.IsFalse(Control.TryAcquire(s, ev, Spell(ControlKind.Compelled, 2, a), out why)); Assert.AreEqual(DivineReasons.AlreadyControlled, why, "a second god's compulsion");
            Assert.IsTrue(Control.TryAcquire(s, ev, Spell(ControlKind.Compelled, 1, a, 99), out why), "the same god's compulsion replaces its own (Superseded)");
            Assert.AreEqual(1, s.Divinity.Control.Entries.Count); Assert.AreEqual(99, Control.EntryOf(s, a).ExpiresTick);

            Assert.IsTrue(Control.TryAcquire(s, ev, Spell(ControlKind.Possessed, 2, a), out why), "possession over compulsion: ends it");
            Assert.AreEqual(ControlKind.Possessed, Control.Of(s, a)); Assert.AreEqual(2, Control.EntryOf(s, a).God);
            Assert.IsFalse(Control.TryAcquire(s, ev, Spell(ControlKind.Compelled, 1, a), out why)); Assert.AreEqual(DivineReasons.AlreadyControlled, why, "no compulsion of a possessed body");
            Assert.IsFalse(Control.TryAcquire(s, ev, Spell(ControlKind.Possessed, 1, a), out why)); Assert.AreEqual(DivineReasons.AlreadyControlled, why, "another god's possession");
            Assert.IsFalse(Control.TryAcquire(s, ev, Spell(ControlKind.Possessed, 2, b), out why)); Assert.AreEqual(DivineReasons.ControlLimit, why, "a second possession by one god");

            var av2 = Avatar(sim, 2, people[0].Node);
            Assert.IsFalse(Control.TryAcquire(s, ev, Spell(ControlKind.AvatarBody, 1, av2.Id), out why)); Assert.AreEqual(DivineReasons.BadTarget, why, "only its own god drives an avatar");
            Assert.IsFalse(Control.TryAcquire(s, ev, Spell(ControlKind.Compelled, 1, av2.Id), out why)); Assert.AreEqual(DivineReasons.NotAPerson, why);
            Assert.IsTrue(Control.TryAcquire(s, ev, Spell(ControlKind.AvatarBody, 2, av2.Id), out why), "manifestation ends the god's possession (one embodied focus)");
            Assert.AreEqual(ControlKind.Self, Control.Of(s, a)); Assert.AreEqual(ControlKind.AvatarBody, Control.Of(s, av2.Id));
            Assert.IsTrue(Control.TryAcquire(s, ev, Spell(ControlKind.Possessed, 2, b), out why), "possession dismisses the god's avatar spell");
            Assert.AreEqual(ControlKind.Self, Control.Of(s, av2.Id)); Assert.AreEqual(ControlKind.Possessed, Control.Of(s, b));

            // CT1: exactly one end per spell
            Assert.IsTrue(Control.Release(s, ev, b, ControlEndReason.Cancelled));
            Assert.IsFalse(Control.Release(s, ev, b, ControlEndReason.Cancelled), "a spell ends once");

            // Sweep: expiry and death
            Assert.IsTrue(Control.TryAcquire(s, ev, Spell(ControlKind.Compelled, 1, c, s.Tick + 1), out why));
            Control.Sweep(s, ev); Assert.AreEqual(ControlKind.Compelled, Control.Of(s, c));
            s.Tick += 1; Control.Sweep(s, ev); Assert.AreEqual(ControlKind.Self, Control.Of(s, c), "Timeout at ExpiresTick");
            Assert.IsTrue(Control.TryAcquire(s, ev, Spell(ControlKind.Compelled, 1, c), out why));
            var pc = s.Population.Find(c);
            PopulationModule.Kill(s, ev, pc, DeathCause.Wound);
            Assert.AreEqual(ControlKind.Self, Control.Of(s, c), "death releases control (Kill → DivinityHooks.OnDeath)");
            Assert.IsFalse(Control.TryAcquire(s, ev, Spell(ControlKind.Compelled, 1, c), out why)); Assert.AreEqual(DivineReasons.TargetDead, why);
            Assert.IsTrue(Control.TryAcquire(s, ev, Spell(ControlKind.Compelled, 1, b), out why));
            s.Population.Find(b).Alive = false; // a dead body still in the table is swept
            Control.Sweep(s, ev); Assert.AreEqual(ControlKind.Self, Control.Of(s, b));
            Assert.IsTrue(s.Divinity.Control.Entries.Select(e => e.Body).SequenceEqual(s.Divinity.Control.Entries.Select(e => e.Body).OrderBy(x => x)), "sorted by body");
        }

        // ---------------------------------------------------------------- F1-T9

        [Test]
        public void Herbs_LedgerHashLayout()
        {
            Assert.AreEqual(32, (int)Ware.Herbs);
            Assert.AreEqual(40, Wares.PersonalCount);
            var sim = Game(15);
            var s = sim.State;
            ulong h0 = PopulationModule.HashPopulation(s);
            var l = s.Population.Ledger;
            Assert.AreEqual(GoodsLedger.ProduceReasons * Wares.PersonalCount, l.Produced.Length);
            l.Produce(ProduceReason.Placed, Ware.Herbs, 1);
            ulong h1 = PopulationModule.HashPopulation(s);
            Assert.AreNotEqual(h0, h1, "a Herbs count enters the population part");
            l.Produced[(int)ProduceReason.Placed * Wares.PersonalCount + (int)Ware.Herbs] = 0;
            Assert.AreEqual(h0, PopulationModule.HashPopulation(s), "all wares 32-39 zero: the old layout exactly");
            // a real Herbs lot round-trips in save
            var npc = s.Population.Npcs.First(Bodies.IsLivingPerson);
            Goods.Produce(s, new EventBuffer(), Holder.Ground(npc.Node), Ware.Herbs, 2, 0, 0, Owner.None, ProduceReason.Placed, 0, 0);
            ulong h2 = sim.StateHash();
            var loaded = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            Assert.AreEqual(h2, loaded.StateHash());
            Assert.AreEqual(2, loaded.State.Population.Ledger.ProducedOf(Ware.Herbs));
        }

        // ---------------------------------------------------------------- F1-T10 (unit part; the scenario part is F1NeutralityTests)

        [Test]
        public void MaxCand36_Quotas()
        {
            Assert.AreEqual(36, ThinkScratch.MaxCand);
            Assert.AreEqual(ThinkScratch.M3Cand + 5 * Cognition.CognitionExtensions.CandidateQuota, ThinkScratch.MaxCand, "M3 16 + M4, M5, Civ, Orgs, Divinity 4 each");
            Assert.AreEqual(ThinkScratch.MaxCand * AssociationStore.MaxKeys, new ThinkScratch().Keys.Length);
        }

        // ---------------------------------------------------------------- F1-T11

        [Test]
        public void MindHooks_And_NewMindParts_DoNotReferenceState()
        {
            var forbidden = new HashSet<Type>(PolicyTests.StateTypes) { typeof(DivinityState), typeof(OrgState), typeof(CivState), typeof(ControlTable) };
            var mindSide = new[] { typeof(CivMindHooks), typeof(OrgMindHooks), typeof(DivinityMindHooks), typeof(DivineMind), typeof(OrgMind), typeof(CivMind), typeof(DecodedDivine), typeof(Information.IDivineView), typeof(IGodPolicy), typeof(IdleGod) };
            var bad = new List<string>();
            foreach (var t in mindSide) bad.AddRange(TypeReferenceScanner.Forbidden(t, forbidden).Select(r => r.ToString()));
            Assert.IsEmpty(bad, string.Join("\n", bad.Distinct()));
        }

        [Test]
        public void NewSavedState_HasNoDictionaryOrHashSet()
        {
            var roots = new[] { typeof(DivinityState), typeof(OrgState), typeof(CivState), typeof(DivineMind), typeof(OrgMind), typeof(CivMind), typeof(ControlTable),
                typeof(Divinity.DivinityTuning), typeof(Orgs.OrgTuning), typeof(Civ.CivTuning) };
            var seen = new HashSet<Type>(); var bad = new List<string>();
            void Walk(Type t, string path)
            {
                if (t.IsArray) { Walk(t.GetElementType(), path + "[]"); return; }
                if (t.IsGenericType)
                {
                    var g = t.GetGenericTypeDefinition();
                    if (g == typeof(Dictionary<,>) || g == typeof(HashSet<>) || g == typeof(SortedDictionary<,>) || g == typeof(SortedSet<>)) bad.Add(path + ": " + t.Name);
                    foreach (var a in t.GetGenericArguments()) Walk(a, path + "<>");
                    return;
                }
                if (t.Assembly != typeof(GodState).Assembly || !seen.Add(t)) return;
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance)) Walk(f.FieldType, path + "." + f.Name);
            }
            foreach (var r in roots) Walk(r, r.Name);
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        // ---------------------------------------------------------------- F1-T12

        [Test]
        public void PolicyAssignment_ManifestUnchanged()
        {
            var a = PolicyAssignment.Stage3Default();
            Assert.AreEqual("civilian=none; military=Stage3MilitaryPolicy; institution=none; faction=AiBrain", a.ToString(), "the god level is omitted while none");
            Assert.IsTrue(a.IsStage3Default);
            Assert.IsTrue(PolicyRegistry.TryParseLevel("god", out var lvl)); Assert.AreEqual(PolicyLevel.God, lvl);
            a.SetDefault(PolicyLevel.God, GodPolicies.Idle);
            Assert.AreEqual("civilian=none; military=Stage3MilitaryPolicy; institution=none; faction=AiBrain; god=IdleGod", a.ToString());
            Assert.IsFalse(a.IsStage3Default);
            var reg = StandardPolicies.ForValidation();
            CollectionAssert.AreEqual(new[] { "none", "IdleGod" }, reg.Names(PolicyLevel.God));
            a.Validate(reg);
            Assert.IsInstanceOf<IdleGod>(reg.Create(PolicyLevel.God, GodPolicies.Idle, 1));
            Assert.IsFalse(IntentRules.Allowed(PolicyLevel.God, NpcVerb.Eat), "gods act through commands, never intents");
        }

        // ---------------------------------------------------------------- save v5

        private static void StepN(GodSim sim, int n) { for (int i = 0; i < n; i++) sim.Step(); }

        [Test]
        public void Save_V5_RoundTrip_AllSwitchesOff()
        {
            Assert.AreEqual(5, SaveFile.CurrentVersion);
            var sim = Game(16);
            StepN(sim, 40);
            string j1 = SaveFile.From(sim).ToJson();
            Assert.IsFalse(j1.Contains("\"Divinity\":") || j1.Contains("\"Organisations\":") || j1.Contains("\"Civ\":"), "null slots and default tuning blocks are not written");
            Assert.IsFalse(j1.Contains("\"BioAgeOffset\"") || j1.Contains("\"HumanGod\""), "default F1 Npc and setup fields are not written");
            var back = SaveFile.FromJson(j1).ToSim();
            Assert.AreEqual(sim.StateHash(), back.StateHash());
            Assert.AreEqual(j1, SaveFile.From(back).ToJson(), "the second save is byte-identical");
            StepN(sim, 20); StepN(back, 20);
            Assert.AreEqual(sim.StateHash(), back.StateHash());
        }

        [Test]
        public void Save_V5_RoundTrip_StubStateOn()
        {
            foreach (var which in new[] { "civ", "organisations", "divine" })
            {
                var sim = Game(17, divinity: which == "divine" ? 1 : 0, orgs: which == "organisations" ? 1 : 0, civ: which == "civ" ? 1 : 0);
                CollectionAssert.Contains(GodSim.PartNamesOf(sim.State), which);
                Assert.AreEqual(GodSim.PartNamesOf(sim.State).Length, sim.StateHashParts().Length);
                Assert.AreEqual(1, GodSim.PartNamesOf(sim.State).Count(n => n == "civ" || n == "organisations" || n == "divine"), which + " alone");
                StepN(sim, 30);
                string j = SaveFile.From(sim).ToJson();
                var back = SaveFile.FromJson(j).ToSim();
                Assert.AreEqual(sim.StateHash(), back.StateHash(), which);
                Assert.AreEqual(j, SaveFile.From(back).ToJson(), which);
                // every other part equals the switches-off run (core differs: the tuning is part of it)
                var off = Game(17); StepN(off, 30);
                var on = sim.StateHashParts(); var offParts = off.StateHashParts(); var names = GodSim.PartNamesOf(sim.State);
                for (int i = 0; i < offParts.Length; i++) if (names[i] != "core") Assert.AreEqual(offParts[i], on[i], which + ": part " + names[i]);
            }
        }

        [Test]
        public void Save_RefusesV4()
        {
            var sim = Game(18);
            var f = SaveFile.From(sim);
            f.Version = 4;
            var ex = Assert.Throws<SaveException>(() => SaveFile.FromJson(f.ToJson()));
            StringAssert.Contains("older version", ex.Message);
        }

        // ---------------------------------------------------------------- catalogue and ranges

        [Test]
        public void Events_NewDomainsAndRanges_NoRows()
        {
            Assert.AreEqual(0, DivinityEvents.Catalog.Count, "F1 adds no event rows"); // C1 (flagged edit): Civ rows 270-277 landed; C6 (flagged edit): Orgs rows below
            foreach (var e in OrgEvents.Catalog) Assert.IsTrue(e.Code >= 500 && e.Code <= 519 || e.Code == 551, "C6 rows stay in C6's block (and the welfare slot 551): " + e.Name);
            foreach (var e in CivEvents.Catalog) Assert.IsTrue(e.Code >= 270 && e.Code <= 289, "C1 rows stay in C1's block: " + e.Name);
            foreach (var d in new[] { TelemetryDomain.Lending, TelemetryDomain.Organisation, TelemetryDomain.Service }) Assert.AreEqual((500, 599), EventCatalog.RangeOf(d, false), d.ToString());
            for (int d = 60; d <= 66; d++) Assert.AreEqual((800, 899), EventCatalog.RangeOf((TelemetryDomain)d, false), ((TelemetryDomain)d).ToString());
            Assert.AreEqual(2, EventCatalog.Of(typeof(Social.GestureMade)).Version, "GestureMade v2 (DurationTicks)");
        }

        [Test]
        public void IdRanges_AsContracts14()
        {
            Assert.AreEqual(179, (int)NpcVerb.CivReserved); Assert.AreEqual(189, (int)NpcVerb.OrgsReserved); Assert.AreEqual(199, (int)NpcVerb.DivinityReserved);
            Assert.AreEqual(17, (int)FeatureKind.Divine); Assert.AreEqual(18, (int)FeatureKind.Org);
            Assert.AreEqual(7, (int)BeliefKind.Shrine); Assert.AreEqual(8, (int)BeliefKind.OrgPlace); Assert.AreEqual(9, (int)BeliefKind.Asset);
            Assert.AreEqual(7, (int)OwnerKind.Org); Assert.AreEqual(6, (int)BinKind.Shrine); Assert.AreEqual(12, (int)MoveReason.Confiscate); Assert.AreEqual(24, (int)OwnershipBasis.Confiscated);
            Assert.AreEqual(7, (int)ConsumeReason.Offering); Assert.Less((int)ConsumeReason.Offering, GoodsLedger.ConsumeReasons); Assert.Less((int)ProduceReason.Crafted, GoodsLedger.ProduceReasons);
            Assert.AreEqual(27, (int)Comms.GestureAct.Alarm); Assert.AreEqual(10, (int)Comms.TopicKind.Service);
            Assert.AreEqual(0x4000_0001, DivinityIds.AvatarBodyId(1));
            Assert.AreEqual(35, EpisodeKey.Possession);
            Assert.AreEqual(VerbTable.Rows.Count, VerbTable.Rows.Select(r => r.Id).Distinct().Count());
            Assert.IsFalse(VerbTable.Rows.Any(r => r.Id >= 96 && !Verb.IsCiv(r.Id) && !Verb.IsOrgs(r.Id)), "rows 96-127 stay reserved until their packages"); // C1 (flagged edit): Civ rows 96, 102; C6 (flagged edit): Orgs rows 104-106, 114, 115
        }
    }
}
