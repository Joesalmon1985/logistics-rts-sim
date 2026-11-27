using System.Collections;
using System.Linq;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>
    /// M5 views: gestures are drawn from the sim's GestureMade events only (a label with the composed message above the
    /// speaker, a line to the addressee), and the person card's relations tab lists known people with trust, familiarity,
    /// grudge and what was learned about them. The views read the sim and decide nothing.
    /// </summary>
    public class SocialViewTests
    {
        private const string Scene = "Assets/God/Scenes/Main.unity";

        [TearDown]
        public void TearDown() { DevOverlays.Reset(); Time.captureFramerate = 0; GodBootstrap.SkipStartCard = true; GodBootstrap.PendingSeed = null; GodBootstrap.PendingSetup = null; GodBootstrap.PendingStartAi = false; }

        [UnityTest]
        public IEnumerator Gestures_AreDrawnFromEvents_AndTheRelationsTabShowsKnownPeople()
        {
            GodBootstrap.SkipStartCard = true;
            GodBootstrap.PendingSeed = 3;
            GodBootstrap.PendingSetup = new GameSetup(3, 0) { Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly() };
            GodBootstrap.PendingStartAi = true;
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = GodBootstrap.Instance;
            DevOverlays.DecodedMessages = true; // M5.18: the composed text is a developer overlay
            Assert.IsNotNull(boot.Sim.State.Social, "the social module runs on the living island");
            boot.Speed = 16f;
            Time.captureFramerate = 30;
            int before = GestureLayer.Received, frames = 0, labelled = 0;
            while (boot.Sim.State.Tick < 1500 && frames++ < 3000)
            {
                yield return null;
                foreach (var t in boot.People.GetComponentsInChildren<Transform>(false))
                    if (t.name == "GestureLabel" && t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.GetComponent<TextMesh>().text)) labelled++;
            }
            Time.captureFramerate = 0;
            boot.Running = false;
            yield return null;
            Assert.Greater(GestureLayer.Received - before, 0, "gesture events reached the layer");
            Assert.Greater(labelled, 0, "a composed message was drawn above a speaker");

            // the relations tab: someone of the player's faction knows people
            var p = boot.Sim.State.Population;
            var me = boot.View.Faction;
            var person = p.Npcs.Where(n => n.Alive && n.Faction == me && n.Mind?.Social != null).OrderByDescending(n => n.Mind.Social.RelCount).ThenBy(n => n.Id).First();
            boot.Hud.SelectNpc(person.Id);
            for (int i = 0; i < 3; i++) yield return null;
            var card = boot.Hud.PersonCard;
            Assert.IsNotNull(card);
            Assert.Greater(card.Relations.Rows.Count, 0, "known people are listed");
            var row = card.Relations.Rows[0];
            Assert.IsNotEmpty(row.Class);
            Assert.That(row.Familiarity, Is.InRange(0, 1000));
        }
    }
}
