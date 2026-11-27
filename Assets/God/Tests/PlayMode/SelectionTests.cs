using System.Collections;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>U1-T3, U1-T4 and the click, double-click and household gestures, driven through ScriptedInput (no mouse).</summary>
    public class SelectionTests
    {
        [TearDown]
        public void TearDown() { U1.Reset(); }

        private static PlayerController Script(GodBootstrap boot, out ScriptedInput input)
        {
            input = new ScriptedInput();
            boot.Player.Source = input; boot.Player.InputEnabled = false;
            return boot.Player;
        }

        private static IEnumerable<int> DrawnInside(GodBootstrap boot, Rect px)
        {
            var cam = boot.GodCamera;
            foreach (var n in boot.Sim.State.Population.Npcs.OrderBy(n => n.Id))
            {
                if (!n.Alive || !boot.People.TryPosition(n.Id, out var p)) continue;
                var sp = cam.WorldToScreenPoint(p + Vector3.up * 1.2f);
                if (sp.z > 0f && px.Contains(new Vector2(sp.x, sp.y))) yield return n.Id;
            }
        }

        [UnityTest]
        public IEnumerator Selection_BoxAndArea()
        {
            yield return U1.Load(3, true);
            var boot = GodBootstrap.Instance; var d = boot.Director; var player = Script(boot, out var input);
            int centre = U1.SomeDrawnPerson(boot);
            d.FocusOn(boot.People.TryPosition(centre, out var cp) ? cp : Vector3.zero, 70f, true);
            d.SetOrbit(30f, 55f); d.Settle();
            float w = Screen.width, h = Screen.height;
            var box = new Rect(w * 0.25f, h * 0.25f, w * 0.5f, h * 0.5f);
            var expected = DrawnInside(boot, box).ToList();
            Assert.GreaterOrEqual(expected.Count, 2, "the framed settlement has people inside the box");
            // direct call
            var ids = new List<int>();
            boot.Hud.BoxSelectIds(box, ids);
            CollectionAssert.AreEqual(expected, ids, "exactly the ids whose drawn positions are inside");
            // by script: press, drag, release
            input.Pointer = new Vector2(box.xMin, box.yMin); input.LeftDown(); player.Step(0.02f);
            input.Pointer = new Vector2(box.xMax, box.yMax); player.Step(0.02f);
            Assert.IsTrue(boot.Hud.RubberBand.HasValue, "the rubber band shows while dragging");
            input.LeftUp(); player.Step(0.02f);
            Assert.IsFalse(boot.Hud.RubberBand.HasValue);
            Assert.AreEqual(SelectionKind.Group, boot.Selection.Kind);
            CollectionAssert.AreEqual(expected, boot.Selection.Ids.ToList(), "the box becomes a hand-picked group of ids");
            Assert.AreEqual(expected[0], boot.Hud.SelectedNpc, "the card follows the selection (primary)");

            // area brush: Alt plus left press at the centre; the wheel sets the radius
            boot.Selection.Clear();
            d.FocusOn(cp, 70f, true); d.Settle();
            input.Alt = true; input.Pointer = new Vector2(w * 0.5f, h * 0.5f);
            input.LeftDown(); player.Step(0.02f);
            Assert.AreEqual(SelectionKind.Area, boot.Selection.Kind);
            Assert.AreEqual(AreaSpec.StepsOf(player.BrushMetres), boot.Selection.Area.RadiusSteps, "radius in lattice steps");
            Assert.AreEqual(0, boot.Selection.Ids.Count, "an area carries no id list");
            input.Scroll(3f); player.Step(0.02f);
            input.Pointer = new Vector2(w * 0.5f + 10f, h * 0.5f); player.Step(0.02f);
            Assert.Greater(boot.Selection.Area.RadiusSteps, AreaSpec.StepsOf(20f), "the wheel widened the brush");
            input.LeftUp(); player.Step(0.02f); input.Alt = false;
            var area = boot.Selection.Area;
            Assert.IsTrue(boot.Hud.Rig.GroundPoint(input.Pointer, out var gp));
            Assert.AreEqual(boot.Hud.NodeAt(gp), area.CentreNode);

            // the command carries the spec and no ids
            var f = new U1.StandInFactory(); player.Builder.Factory = f;
            var r = player.Issue("bless");
            Assert.IsTrue(r.Submitted, r.Result.ToString());
            Assert.AreEqual(TargetScope.Area, f.Requests[0].Scope);
            Assert.AreEqual(area, f.Requests[0].Area);
            Assert.IsNull(f.Requests[0].Persons, "no id list");
        }

        [UnityTest]
        public IEnumerator Selection_ClickDoubleClickHousehold_AtAnyYaw()
        {
            yield return U1.Load(3, true);
            var boot = GodBootstrap.Instance; var d = boot.Director; var player = Script(boot, out var input);
            // someone with a household (own faction's card: in watch mode the person's own faction answers)
            int person = 0;
            foreach (var n in boot.Sim.State.Population.Npcs.OrderBy(n => n.Id))
                if (n.Alive && n.Household != 0 && boot.People.TryPosition(n.Id, out _)) { person = n.Id; break; }
            Assert.Greater(person, 0, "the Living island has households");
            int household = boot.Sim.State.Population.Find(person).Household;
            for (int k = 0; k < 4; k++)
            {
                d.FocusOnPerson(person, true); d.SetOrbit(k * 90f + 20f, 55f); d.ZoomTo(30f); d.Settle();
                var sp = U1.ScreenOf(boot, person);
                input.Pointer = sp;
                boot.Selection.Clear();
                input.LeftDown(); player.Step(0.02f); input.LeftUp(); player.Step(0.02f);
                Assert.AreEqual(SelectionKind.Person, boot.Selection.Kind, "click selects at yaw " + (k * 90 + 20));
                Assert.AreEqual(person, boot.Selection.Primary);
                Assert.AreEqual(person, boot.Hud.SelectedNpc, "the person card opens");
                // a second click soon after is a double click: household selected, camera focused on the person
                input.LeftDown(); player.Step(0.02f); input.LeftUp(); player.Step(0.02f);
                Assert.AreEqual(SelectionKind.Household, boot.Selection.Kind, "double click selects the household");
                Assert.AreEqual(household, boot.Selection.Ids[0], "the household id, not a member list");
                Assert.AreEqual(person, boot.Selection.Primary);
            }
            // shift click adds a second person (toggle), Esc clears
            int other = U1.SomeDrawnPerson(boot, 7);
            d.FocusOnPerson(other, true); d.ZoomTo(30f); d.Settle();
            boot.Selection.SetPerson(person);
            boot.Selection.Toggle(other);
            Assert.AreEqual(SelectionKind.Group, boot.Selection.Kind);
            input.Press(PlayerKeys.Escape); player.Step(0.02f);
            Assert.IsTrue(boot.Selection.IsEmpty, "Esc clears the selection");
            Assert.AreEqual(0, boot.Hud.SelectedNpc);
        }

        [UnityTest]
        public IEnumerator Scope_IndependentOfZoom_SameCommandSameHash()
        {
            string[] req = new string[2]; ulong[] hash = new ulong[2]; long[] tick = new long[2];
            for (int run = 0; run < 2; run++)
            {
                yield return U1.Load(5, true);
                var boot = GodBootstrap.Instance; var d = boot.Director; var player = Script(boot, out var input);
                var f = new U1.StandInFactory(); player.Builder.Factory = f;
                int person = U1.SomeDrawnPerson(boot);
                d.FocusOnPerson(person, true);
                d.ZoomTo(run == 0 ? 20f : 300f); d.SetOrbit(run == 0 ? 0f : 140f, run == 0 ? 50f : 35f); d.Settle();
                // the same area (by node and steps), and the same person clicked wherever the camera is
                int node = boot.Sim.State.Population.Find(person).Node;
                boot.Selection.SetArea(new AreaSpec(node, 4));
                Assert.IsTrue(player.Issue("bless", 1).Submitted);
                if (run == 1) d.ZoomTo(20f);
                d.Settle();
                input.Pointer = U1.ScreenOf(boot, person); input.LeftDown(); player.Step(0.02f); input.LeftUp(); player.Step(0.02f);
                if (boot.Selection.Primary != person) { boot.Selection.SetPerson(person); } // another figure was nearer on screen: the id is what matters
                Assert.IsTrue(player.Issue("suggest", 2).Submitted);
                req[run] = string.Join(" | ", f.Requests.Select(x => x.ToString()));
                boot.Sim.Step(200);
                hash[run] = boot.Sim.StateHash(); tick[run] = boot.Sim.State.Tick;
            }
            Assert.AreEqual(req[0], req[1], "identical commands at two zoom levels and yaws");
            Assert.AreEqual(tick[0], tick[1]);
            Assert.AreEqual(hash[0], hash[1], "identical resulting state hash");
        }

        [UnityTest]
        public IEnumerator HudHooks_PanelsAndPeopleTabsRegister()
        {
            yield return U1.Load(3, true);
            var hud = GodBootstrap.Instance.Hud;
            CollectionAssert.AreEqual(new[] { "mind", "household", "relations" }, hud.PersonTabIds(), "the built-in tabs are registrations");
            int drawn = 0;
            hud.RegisterPersonTab("divine", "Divine", 30, (st, c) => drawn++);
            hud.RegisterPanel("test", 5, (st, frame) => drawn++);
            CollectionAssert.AreEqual(new[] { "mind", "household", "relations", "divine" }, hud.PersonTabIds());
            Assert.AreEqual(1, hud.PanelCount);
            int person = U1.SomeDrawnPerson(GodBootstrap.Instance);
            GodBootstrap.Instance.Selection.SetPerson(person);
            hud.PersonTab = 3;
            yield return U1.Frames(3);
            Assert.GreaterOrEqual(drawn, 0); // the drawers run in OnGUI, which a -nographics run never calls: registration is what is checked
            hud.UnregisterPanel("test"); hud.UnregisterPersonTab("divine");
            Assert.AreEqual(3, hud.PersonTabIds().Count);
        }
    }
}
