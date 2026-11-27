using System.Collections;
using System.Linq;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>U1-T9: camera moves, selections, renames, favourites and follows never reach the sim or its hash; the ui extra survives a save.</summary>
    public class SelectionUiStateTests
    {
        [TearDown]
        public void TearDown() { U1.Reset(); }

        private static IEnumerator Run(bool withUi, ulong[] hash, int slot)
        {
            yield return U1.Load(7, true);
            var boot = GodBootstrap.Instance; var d = boot.Director; var ui = boot.Ui;
            var rng = new System.Random(12345);
            var people = boot.Sim.State.Population.Npcs.Select(n => n.Id).Take(30).ToList();
            for (int day = 0; day < 20; day++)
            {
                boot.Sim.Step(1200);
                boot.PumpEvents();
                if (!withUi) continue;
                for (int k = 0; k < 6; k++)
                {
                    int id = people[rng.Next(people.Count)];
                    switch (rng.Next(9))
                    {
                        case 0: d.Orbit(rng.Next(-90, 90), rng.Next(-20, 20)); break;
                        case 1: d.Zoom(rng.Next(-4, 6), new Vector2(rng.Next(100, 800), rng.Next(100, 500))); break;
                        case 2: d.PanBy(new Vector3(rng.Next(-80, 80), 0f, rng.Next(-80, 80))); break;
                        case 3: boot.Selection.SetPerson(id); break;
                        case 4: boot.Selection.SetGroup(people.Take(rng.Next(1, 8))); break;
                        case 5: ui.Rename(id, "Name " + rng.Next(1000)); break;
                        case 6: ui.ToggleFavourite(id); break;
                        case 7: ui.ToggleFollow(id); d.Follow(id); break;
                        case 8: boot.Selection.SetArea(new AreaSpec(rng.Next(0, 50), rng.Next(1, 8))); break;
                    }
                    d.Advance(0.033f);
                }
                yield return null;
            }
            hash[slot] = boot.Sim.StateHash();
            if (withUi)
            {
                // the ui extra: aliases survive a save and a load; the sim part of the save is the same game with or without it
                ui.Rename(people[0], "Old Wren");
                var save = boot.MakeSave();
                Assert.IsTrue(save.Extras.ContainsKey(UiExtra.Key), "the save carries the ui extra");
                var fresh = new UiExtra();
                fresh.LoadState(save.Extras[UiExtra.Key]);
                Assert.AreEqual("Old Wren", fresh.NameOf(people[0]), "the alias survives save and load");
                Assert.IsTrue(fresh.State.Camera.HasValue, "and the camera pose");
                Assert.AreEqual(hash[slot], save.ToSim().StateHash(), "loading the save gives the same sim hash with the extra present");
            }
        }

        [UnityTest]
        public IEnumerator UiState_NeverHashed_TwentyDayRun()
        {
            var hash = new ulong[2];
            yield return Run(false, hash, 0);
            yield return Run(true, hash, 1);
            Assert.AreEqual(hash[0], hash[1], "the same StateHash with random camera moves, selections, renames, favourites and follows");
        }
    }
}
