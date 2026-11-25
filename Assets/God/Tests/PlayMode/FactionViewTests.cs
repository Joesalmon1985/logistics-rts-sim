using System.Collections;
using System.IO;
using System.Linq;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>
    /// P7: the scene with six factions. Every HQ has a model in its faction's colour; a capture repaints the captured
    /// town in the new owner's style and colour. Writes p7_island.png and p7_capture.png to <c>GOD_SHOTS</c> (else
    /// LocalData, which is not committed).
    /// </summary>
    public class FactionViewTests
    {
        private static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }

        private static bool HasColour(Mesh mesh, Color want)
        {
            var a = ModelKit.Accent(want);
            a = new Color(Mathf.Clamp01(a.r), Mathf.Clamp01(a.g), Mathf.Clamp01(a.b));
            foreach (var c in mesh.colors)
                if (Mathf.Abs(c.r - a.r) + Mathf.Abs(c.g - a.g) + Mathf.Abs(c.b - a.b) < 0.12f) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator SixFactions_EveryHqHasAModel_InItsFactionsColour()
        {
            yield return LoadScene();
            var boot = GodBootstrap.Instance;
            var s = boot.Sim.State;
            boot.Running = false;
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(6, s.FactionCount);
            Assert.AreEqual(6, s.Territory.Hqs.Count, "one HQ per faction");
            var layer = boot.Economy.Buildings;
            foreach (var f in FactionInfo.All)
            {
                var hq = s.Territory.HqsOf(f).Single();
                var model = layer.ModelOf(hq.Id);
                Assert.IsNotNull(model, FactionInfo.Name(f) + " HQ has a model");
                Assert.IsTrue(model.activeInHierarchy);
                Assert.AreEqual(f, layer.DrawnFaction(hq.Id));
                var banner = model.transform.Find("Banner");
                Assert.IsNotNull(banner, "the HQ flies a banner");
                var mesh = banner.GetComponent<MeshFilter>().sharedMesh;
                Assert.IsTrue(HasColour(mesh, FactionLook.Colour(f)), FactionInfo.Name(f) + " banner in its colour");
                foreach (var o in FactionInfo.All)
                    if (o != f) Assert.IsFalse(HasColour(mesh, FactionLook.Colour(o)), FactionInfo.Name(f) + " banner is not " + FactionInfo.Name(o));
                // The model body carries the colour on its ridge and eave band too.
                Assert.IsTrue(HasColour(model.GetComponent<MeshFilter>().sharedMesh, FactionLook.Colour(f)), FactionInfo.Name(f) + " model accent");
            }
            Assert.AreEqual(6, FactionInfo.All.Select(FactionLook.Colour).Distinct().Count());

            // The whole island at 16:9, as the home view frames it.
            var cam = boot.GodCamera;
            IslandCamera.Frame(cam, BoardSpace.FrameRadius + 40f, BoardSpace.BoardCentre);
            for (int i = 0; i < 3; i++) yield return null;
            // Every HQ is inside the 16:9 view.
            float aspect = cam.aspect; cam.aspect = 16f / 9f;
            foreach (var h in s.Territory.Hqs)
            {
                var vp = cam.WorldToViewportPoint(ModelKit.Node(s.Territory.Grid, h.Node));
                Assert.That(vp.x, Is.InRange(0.02f, 0.98f), "HQ " + h.Id + " in view across");
                Assert.That(vp.y, Is.InRange(0.02f, 0.98f), "HQ " + h.Id + " in view up and down");
            }
            cam.aspect = aspect;
            ModelViewTests.Shot(cam, "p7_island.png", 1920, 1080);
        }

        [UnityTest]
        public IEnumerator Capture_RepaintsTheTown_InTheNewOwnersStyleAndColour()
        {
            yield return LoadScene();
            var boot = GodBootstrap.Instance;
            var sim = boot.Sim; var s = sim.State; var t = s.Territory; var g = t.Grid;
            boot.Running = false;
            // Six economies grow a little first (the bots step the sim themselves).
            boot.DebugSample();
            boot.Running = false;
            while (s.Tick < 5000) { sim.Step(); if (s.Tick % 20 == 0) boot.ThinkChains(); sim.DrainEvents(); }
            yield return null;

            var hq = t.HqsOf(Faction.F0)[0];
            var x = t.Hqs.Where(h => h.Faction != Faction.F0).OrderBy(h => g.Distance(h.Node, hq.Node)).First().Faction;
            Assert.IsTrue(sim.Submit(new DeclareWar(x, Faction.F0)).Ok);
            sim.Step();
            int flag = g.FlagNodeOf(hq.Node);
            for (int i = 0; i < 4; i++) sim.Submit(new DevSpawnUnit(x, i % 2 == 0 ? UnitKind.Vehicle : UnitKind.Infantry, flag));
            sim.Submit(new MarkTarget(x, hq.Id));
            sim.Step();
            s.Military.BuildingById(hq.Id).Health = 400;
            HqCaptured cap = null;
            for (int i = 0; i < 600 && cap == null; i++)
            {
                sim.Step();
                cap = sim.DrainEvents().OfType<HqCaptured>().FirstOrDefault();
            }
            Assert.IsNotNull(cap, "the HQ fell");
            Assert.AreEqual(x, t.HqOf(hq.Id).Faction);
            var layer = boot.Economy.Buildings;
            int restyled = layer.Restyled;
            for (int i = 0; i < 4; i++) yield return null;
            Assert.Greater(layer.Restyled, restyled, "the captured models were rebuilt");
            foreach (int id in cap.BuildingIds)
            {
                if (t.BuildingById(id) == null) continue;
                Assert.AreEqual(x, layer.DrawnFaction(id), "building " + id + " drawn for its new owner");
            }
            var banner = layer.ModelOf(hq.Id).transform.Find("Banner").GetComponent<MeshFilter>().sharedMesh;
            Assert.IsTrue(HasColour(banner, FactionLook.Colour(x)), "the HQ banner is now " + FactionInfo.Name(x));
            Assert.IsFalse(HasColour(banner, FactionLook.Colour(Faction.F0)));

            // Just done: the town in its new colours, the attackers round it.
            var cam = boot.GodCamera;
            ModelViewTests.Aim(cam, ModelKit.Node(g, hq.Node) + new Vector3(0f, 0f, 8f), 125f, 55f);
            for (int i = 0; i < 3; i++) yield return null;
            ModelViewTests.Shot(cam, "p7_capture.png");
        }
    }
}
