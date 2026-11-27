using System;
using System.Collections.Generic;
using God.Sim;
using NUnit.Framework;

namespace God.Game.Tests
{
    /// <summary>
    /// U1-T7, U1-T8 (pure part): the command builder has no scene. The sim's divine command types arrive with F1a, so these tests
    /// use a stand-in command and factory behind <see cref="IDivineCommandFactory"/>; the builder code is the shipped code.
    /// </summary>
    public class PlayerCommandBuilderTests
    {
        private sealed class StandInCommand : Command
        {
            public DivineRequest Request;
        }

        private sealed class StandInFactory : IDivineCommandFactory
        {
            public Command Create(DivineRequest r) => new StandInCommand { PlayerId = r.God, Request = r };
        }

        /// <summary>A gate that behaves like the sim's authorship rule: refuses a command whose god is not the Human seat holder.</summary>
        private sealed class SeatGate : ICommandGate
        {
            public int Seat = 1;
            public readonly List<Command> Queue = new List<Command>();
            public int Checks;
            public CommandResult Check(Command c)
            {
                Checks++;
                return ((StandInCommand)c).Request.God == Seat ? CommandResult.Accept() : CommandResult.Reject("not_your_god", "That is not your god.");
            }
            public CommandResult Submit(Command c) { var r = Check(c); if (r.Ok) Queue.Add(c); return r; }
        }

        private static SelectionSnapshot Select(Action<SelectionModel> pick) { var m = new SelectionModel(); pick(m); return m.Snapshot(); }

        [Test]
        public void CommandBuilder_Pure_StampsTheHumanSeatGod_AndIsDeterministic()
        {
            var seat = new SeatGate { Seat = 3 };
            int god = 3;
            var b = new PlayerCommandBuilder(() => god, seat, new StandInFactory());
            var sel = Select(m => m.SetGroup(new[] { 9, 4, 4, 7 }));
            var a = (StandInCommand)b.Build(new DivineIntent("suggest", 2), sel);
            var c = (StandInCommand)b.Build(new DivineIntent("suggest", 2), sel);
            Assert.AreEqual(3, a.Request.God, "stamped with the holder of the Human seat");
            Assert.IsTrue(a.Request.SameAs(c.Request), "identical intents and selection give identical commands");
            CollectionAssert.AreEqual(new[] { 4, 7, 9 }, a.Request.Persons, "hand-picked ids: explicit, ascending, no repeats");
        }

        [Test]
        public void CommandBuilder_NeverIncludesAListForHouseholdOrAreaScopes()
        {
            var b = new PlayerCommandBuilder(() => 1, new SeatGate(), new StandInFactory());
            var hh = (StandInCommand)b.Build(new DivineIntent("bless"), Select(m => m.SetHousehold(12, 5)));
            Assert.AreEqual(TargetScope.Household, hh.Request.Scope);
            Assert.AreEqual(12, hh.Request.Id);
            Assert.IsNull(hh.Request.Persons, "the sim resolves the members at the application tick");
            var area = (StandInCommand)b.Build(new DivineIntent("bless"), Select(m => m.SetArea(new AreaSpec(77, 4))));
            Assert.AreEqual(TargetScope.Area, area.Request.Scope);
            Assert.AreEqual(new AreaSpec(77, 4), area.Request.Area, "centre node and radius in lattice steps");
            Assert.IsNull(area.Request.Persons, "the sim resolves who is inside");
            var loc = (StandInCommand)b.Build(new DivineIntent("go"), Select(m => m.SetLocation(31)));
            Assert.AreEqual(31, loc.Request.Node);
            Assert.IsNull(b.Build(new DivineIntent("go"), Select(m => { })), "nothing selected, nothing built");
        }

        [Test]
        public void Builder_NotYourGod_StampsTheNewGod_AndAStaleGodIsRefusedNotSentSilently()
        {
            var seat = new SeatGate { Seat = 1 };
            int god = 1;
            var b = new PlayerCommandBuilder(() => god, seat, new StandInFactory());
            var sel = Select(m => m.SetPerson(5));
            var stale = (StandInCommand)b.Build(new DivineIntent("suggest"), sel);
            Assert.AreEqual(1, stale.Request.God);
            // SwitchGod in the sandbox: the Human seat moves to god 2
            seat.Seat = 2; god = 2;
            var fresh = b.Issue(new DivineIntent("suggest"), sel);
            Assert.IsTrue(fresh.Submitted, "the new god's command is accepted");
            Assert.AreEqual(2, ((StandInCommand)fresh.Command).Request.God, "stamped with the new god");
            var late = b.Send(null, stale);
            Assert.IsFalse(late.Submitted, "the stale god is not sent");
            Assert.AreEqual("not_your_god", late.Result.Reason, "the refusal comes back with its reason");
            Assert.AreEqual(1, seat.Queue.Count, "only the new god's command is queued");
        }

        [Test]
        public void Builder_WithoutFactory_RefusesPlainly()
        {
            var seat = new SeatGate();
            var b = new PlayerCommandBuilder(() => 1, seat, null);
            var r = b.Issue(new DivineIntent("suggest"), Select(m => m.SetPerson(5)));
            Assert.IsFalse(r.Submitted);
            Assert.AreEqual(Reasons.Disabled, r.Result.Reason);
            Assert.AreEqual(0, seat.Queue.Count);
        }
    }

    public class SelectionModelTests
    {
        [Test]
        public void ClickToggleBoxAndKinds()
        {
            var m = new SelectionModel();
            int changes = 0; m.Changed += () => changes++;
            m.SetPerson(5);
            Assert.AreEqual(SelectionKind.Person, m.Kind);
            m.Toggle(8);
            Assert.AreEqual(SelectionKind.Group, m.Kind); CollectionAssert.AreEqual(new[] { 5, 8 }, m.Ids);
            m.Toggle(5);
            Assert.AreEqual(SelectionKind.Person, m.Kind); Assert.AreEqual(8, m.Primary);
            m.Toggle(8);
            Assert.IsTrue(m.IsEmpty);
            m.SetGroup(new[] { 3, 1, 2 });
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, m.Ids);
            m.SetArea(new AreaSpec(10, 4));
            Assert.AreEqual(0, m.Ids.Count, "an area holds no id list");
            m.SetHousehold(7, 2);
            Assert.AreEqual(SelectionKind.Household, m.Kind); Assert.AreEqual(2, m.Primary);
            m.Clear();
            Assert.IsTrue(changes >= 6, "Changed is raised");
        }

        [Test]
        public void NewKinds_AreDescribedByRegistration()
        {
            var m = new SelectionModel();
            m.RegisterDescriber(SelectionKind.Object, s => "vehicle " + s.Primary);
            m.SetObject((ObjectKind)9, 4);
            Assert.AreEqual("vehicle 4", m.Describe(), "a later workstream adds an object subtype and a describer without changing the core");
        }

        [Test]
        public void AreaSteps_AreLatticeSteps()
        {
            Assert.AreEqual(4, AreaSpec.StepsOf(20f));
            Assert.AreEqual(1, AreaSpec.StepsOf(2f));
        }
    }

    public class UiExtraTests
    {
        [Test]
        public void AliasesFavouritesFollows_RoundTrip_AndAliasShowsOverTrueName()
        {
            var a = new UiExtra { TrueName = id => "Amber #" + id };
            a.Rename(5, "  Old Wren ");
            a.ToggleFavourite(5); a.ToggleFollow(9); a.SetNote(5, "likes the river");
            Assert.AreEqual("Old Wren", a.NameOf(5));
            Assert.AreEqual("Amber #6", a.NameOf(6));
            var sel = new SelectionModel(); sel.SetGroup(new[] { 2, 3 });
            a.CaptureSelection = () => sel;
            a.CaptureCamera = () => new CameraPose { FocusX = 10, FocusZ = 20, Yaw = 90, Pitch = 40, Distance = 55 };
            string json = a.SaveState();
            var b = new UiExtra();
            b.LoadState(json);
            Assert.AreEqual("Old Wren", b.State.Aliases[5]);
            CollectionAssert.AreEqual(new[] { 5, 9 }, b.Roster());
            Assert.AreEqual("likes the river", b.State.Notes[5]);
            Assert.AreEqual(90f, b.State.Camera.Value.Yaw);
            var restored = new SelectionModel();
            UiExtra.Restore(b.State.Selection, restored);
            CollectionAssert.AreEqual(new[] { 2, 3 }, restored.Ids);
        }

        [Test]
        public void Roster_IsCappedAtTwelve()
        {
            var a = new UiExtra();
            for (int i = 1; i <= 20; i++) a.ToggleFavourite(i);
            Assert.AreEqual(UiExtra.MaxRoster, a.Roster().Count);
        }
    }
}
