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
    /// P4b: buildings, carriers and units drawn as models. Runs the F2 chain (a bot per faction) and then the dev fight in the real
    /// scene and checks that every building kind on the island has its model, sites and carriers show, every weapon kind is
    /// drawn, shots fly and a destroyed building leaves rubble. Writes screenshots when <c>GOD_SHOTS</c> names a folder
    /// (else to LocalData, which is not committed).
    /// </summary>
    public class ModelViewTests
    {
        private static string ShotDir()
        {
            var env = System.Environment.GetEnvironmentVariable("GOD_SHOTS");
            var dir = !string.IsNullOrEmpty(env) ? env : Path.Combine(Application.dataPath, "..", "LocalData");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>Renders the camera into a texture and saves it (works in batch mode too).</summary>
        public static void Shot(Camera cam, string name, int w = 1600, int h = 900)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var prevTarget = cam.targetTexture; var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            string file = Path.GetFullPath(Path.Combine(ShotDir(), name));
            File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = prevTarget; RenderTexture.active = prevActive;
            Object.Destroy(rt); Object.Destroy(tex);
            Debug.Log("P4b screenshot: " + file);
        }

        /// <summary>Points the god camera at a spot from the south at the god pitch, from a distance.</summary>
        public static void Aim(Camera cam, Vector3 at, float distance, float pitch = IslandCamera.Pitch, float yaw = 0f)
        {
            var q = Quaternion.Euler(pitch, yaw, 0);
            cam.transform.rotation = q;
            cam.transform.position = at - q * Vector3.forward * distance;
            cam.nearClipPlane = 1f; cam.farClipPlane = distance + 2000f;
        }

        private static Vector3 At(NodeGrid g, int node) => ModelKit.Node(g, node);

        [UnityTest]
        public IEnumerator ChainAndFight_DrawModelsSitesCarriersUnitsAndRubble()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = GodBootstrap.Instance;
            var s = boot.Sim.State; var g = s.Territory.Grid;
            var cam = boot.GodCamera;
            var eco = boot.Economy; var mil = boot.Military;

            // Every kind builds a mesh in both styles.
            foreach (var kind in BuildingModels.Kinds)
                foreach (Faction f in FactionInfo.All)
                    Assert.Greater(BuildingModels.MeshOf(kind, f).vertexCount, 200, kind + " " + f + " has a model");

            boot.DebugSample();
            boot.Speed = 24f;
            Time.captureFramerate = 30;
            int maxWalkers = 0, maxSites = 0, maxCarrying = 0, maxTools = 0;
            bool townShot = false; int townScore = 0;
            var builtBefore = new System.Collections.Generic.HashSet<int>();
            int frames = 0;
            while (s.Tick < 9000 && frames++ < 12000)
            {
                yield return null;
                maxWalkers = Mathf.Max(maxWalkers, eco.WalkerCount);
                maxSites = Mathf.Max(maxSites, eco.Buildings.SiteCount);
                maxCarrying = Mathf.Max(maxCarrying, eco.Walkers.CarryingNow);
                maxTools = Mathf.Max(maxTools, eco.Walkers.WithToolNow);
                // Every finished building has its model (one that finished during this frame's sim steps is drawn in this
                // frame's LateUpdate, after this coroutine, so only those already finished at the last check count).
                if (frames % 60 == 0)
                {
                    foreach (var b in s.Economy.Buildings.Where(b => b.Built && builtBefore.Contains(b.Id)))
                        Assert.Greater(eco.Buildings.CountBuilt(b.IsHq ? TerritoryTuning.HqKind : b.Kind, b.Faction), 0, b.Kind + " " + b.Id + " drawn");
                    builtBefore = new System.Collections.Generic.HashSet<int>(s.Economy.Buildings.Where(b => b.Built).Select(b => b.Id));
                }
                // The town picture: retaken whenever a better moment comes (a site going up, carriers loaded near it, town round it).
                if (s.Tick > 2500 && frames % 15 == 0)
                {
                    var pts = s.Economy.Buildings.Where(b => b.Faction == Faction.F0 && b.Built).Select(b => At(g, b.Node)).ToList();
                    var loaded = s.Economy.Walkers.Where(w => w.Loaded && w.CurrentNode >= 0 && w.Faction == Faction.F0).Select(w => At(g, w.CurrentNode)).ToList();
                    foreach (var x in s.Economy.Buildings.Where(b => !b.Built && !b.IsHq && b.Faction == Faction.F0))
                    {
                        var sp = At(g, x.Node);
                        int score = pts.Count(p => (p - sp).magnitude < 30f) * 2 + loaded.Count(p => (p - sp).magnitude < 30f) * 3 + (s.Economy.SiteProgressPermille(x.Id) >= 450 ? 4 : 0);
                        if (score <= townScore) continue;
                        townScore = score; townShot = true;
                        var near = pts.Where(p => (p - sp).magnitude < 30f).ToList();
                        var c = near.Count > 0 ? near.Aggregate(Vector3.zero, (q, p) => q + p) / near.Count : sp;
                        Aim(cam, Vector3.Lerp(c, sp, 0.6f), 58f, 50f);
                        yield return null;
                        Shot(cam, "p4b_town.png");
                        break;
                    }
                }
            }
            Time.captureFramerate = 0;
            Assert.Greater(maxWalkers, 0, "walkers drawn");
            Assert.Greater(maxSites, 0, "construction sites drawn");
            Assert.Greater(maxCarrying, 0, "carriers carried a ware");
            Assert.Greater(maxTools, 0, "builders or harvesters carried a tool");
            var builtKinds = s.Economy.Buildings.Where(b => b.Built && !b.IsHq).Select(b => b.Kind).Distinct().ToList();
            Debug.Log("P4b built kinds: " + string.Join(", ", builtKinds) + " at tick " + s.Tick);
            Assert.GreaterOrEqual(builtKinds.Count, 6, "the chain built most kinds");
            Assert.Greater(eco.Buildings.CountBuilt(TerritoryTuning.HqKind, Faction.F0), 0);
            Assert.Greater(eco.Buildings.CountBuilt(TerritoryTuning.HqKind, Faction.F1), 0);
            if (!townShot)
            {
                var hq0 = s.Territory.HqsOf(Faction.F0)[0];
                Aim(cam, At(g, hq0.Node), 95f, 52f);
                yield return null;
                Shot(cam, "p4b_town.png");
            }

            // The whole island, then the enemy base.
            boot.Running = false;
            IslandCamera.Frame(cam, BoardSpace.FrameRadius + 40f, BoardSpace.BoardCentre);
            for (int i = 0; i < 3; i++) yield return null;
            Shot(cam, "p4b_island.png", 1920, 1080);
            var ehq = s.Territory.HqsOf(Faction.F1)[0];
            var epts = s.Territory.Buildings.Where(b => b.Faction == Faction.F1).Select(b => At(g, b.Node)).ToList();
            Aim(cam, epts.Aggregate(Vector3.zero, (q, p) => q + p) / epts.Count, 78f, 50f);
            for (int i = 0; i < 3; i++) yield return null;
            Shot(cam, "p4b_enemy.png");

            // The fight at the nearest other faction's town.
            int firstFightId = s.Military.NextUnitId;
            boot.DebugFight();
            var foe = s.Factions.Enemies(Faction.F0)[0];
            ehq = s.Territory.HqsOf(foe)[0];
            // Stage 3 soldiers duel in melee and the siege wagons are gone: the foe's chain army (a dozen or more by now)
            // would swallow the dev raid before it razes anything. This is a drawing test, so the raid meets only the
            // three defenders it spawned, and its marked buildings are already battered.
            s.Military.Units.RemoveAll(u => u.Faction == foe && u.Id < firstFightId);
            foreach (int id in s.Military.MarkedOf(Faction.F0)) { var r = s.Military.BuildingById(id); if (r != null && !r.IsHq) r.Health = System.Math.Min(r.Health, 60); }
            boot.Running = true;
            boot.Speed = 3f;
            Time.captureFramerate = 30;
            int rubbleSeen = 0, shotsSeen = 0, barsSeen = 0, deaths = 0;
            bool pInf = false, pRanged = false, eInf = false, battleShot = false;
            frames = 0;
            while (frames++ < 9000)
            {
                yield return null;
                pInf |= mil.Units.CountOf(UnitKind.Infantry, Faction.F0) > 0;
                pRanged |= mil.Units.CountOf(Ware.Bow, Faction.F0) > 0;
                eInf |= mil.Units.CountOf(UnitKind.Infantry, foe) > 0;
                rubbleSeen = Mathf.Max(rubbleSeen, eco.Buildings.RubbleCount);
                shotsSeen = Mathf.Max(shotsSeen, mil.TracersDrawn);
                barsSeen = Mathf.Max(barsSeen, FxLayer.Instance.BarsDrawn);
                deaths = mil.Units.DeathsSoFar;
                if (!battleShot && rubbleSeen > 0 && mil.UnitsDrawn > 0 && FxLayer.Instance.ShotsDrawn > 0 && FxLayer.Instance.BarsDrawn > 0)
                {
                    battleShot = true;
                    var rb = s.Military.Rubbles[0];
                    var rp = ModelKit.OnGround(rb.X / 1000f, rb.Z / 1000f);
                    var us = s.Military.Units.Select(u => ModelKit.OnGround(u.X / 1000f, u.Z / 1000f)).Where(p => (p - rp).magnitude < 35f).ToList();
                    var c = us.Count > 0 ? us.Aggregate(Vector3.zero, (a, p) => a + p) / us.Count : rp;
                    Aim(cam, Vector3.Lerp(c, rp, 0.35f), 62f, 50f);
                    yield return null;
                    Shot(cam, "p4b_battle.png");
                }
                if (battleShot && rubbleSeen > 0 && frames > 600) break;
                if (s.IsGameOver) break;
            }
            Time.captureFramerate = 0;
            if (!battleShot) { Aim(cam, At(g, ehq.Node), 85f, 52f); yield return null; Shot(cam, "p4b_battle.png"); }
            Assert.IsTrue(pInf && pRanged && eInf, "player soldiers and archers and enemy soldiers drawn");
            Assert.Greater(shotsSeen, 0, "arrows and stones drawn");
            Assert.Greater(mil.Units.StrikesSoFar, 0, "blows and shots drawn");
            Assert.Greater(FxLayer.Instance.MissesShown + FxLayer.Instance.BlocksShown, 0, "a miss or block puff shown");
            Assert.Greater(barsSeen, 0, "health bars drawn when damaged");
            Assert.Greater(rubbleSeen, 0, "a destroyed building left rubble; tick " + s.Tick + ", units mine " + s.Military.CountUnits(Faction.F0) + " foe " + s.Military.CountUnits(foe) + ", deaths drawn " + deaths + ", strikes " + mil.Units.StrikesSoFar);
            Assert.Greater(eco.Buildings.CollapsedSoFar, 0, "a building collapsed");
            Debug.Log("P4b fight: deaths " + deaths + " rubble " + rubbleSeen + " tick " + s.Tick + " strikes " + mil.Units.StrikesSoFar + " misses " + FxLayer.Instance.MissesShown + " blocks " + FxLayer.Instance.BlocksShown + " crits " + FxLayer.Instance.CritsShown);
        }

        /// <summary>Kits shown in the gallery and the load test: every weapon, every defence item.</summary>
        internal static readonly Kit[] GalleryKits =
        {
            new Kit(Ware.Sword, Ware.SmallShield, Ware.Helmet),
            new Kit(Ware.Spear, Ware.BigShield, Ware.None, Ware.Breastplate),
            new Kit(Ware.Axe, Ware.None, Ware.Helmet, Ware.LightArmour),
            new Kit(Ware.Bow, Ware.None, Ware.Helmet, Ware.LightArmour),
            new Kit(Ware.Sling, Ware.SmallShield),
            new Kit(Ware.Sword),
        };

        /// <summary>A standing soldier figure in a kit, from the unit models (view objects only).</summary>
        internal static GameObject Soldier(Transform parent, Faction f, Kit k, Vector3 at, float armPitch)
        {
            var inf = ModelKit.Part("soldier " + k, parent, UnitModels.Body(f, k.Head, k.Body));
            inf.transform.position = at; inf.transform.localScale = Vector3.one * 1.2f; inf.transform.rotation = Quaternion.Euler(0, 200f, 0);
            var arm = ModelKit.Part("arm", inf.transform, UnitModels.ShieldArm(f, k.Shield)); arm.transform.localPosition = new Vector3(-PeopleModels.ShoulderX, PeopleModels.Shoulder, 0); arm.transform.localRotation = Quaternion.Euler(-35f, 0, 0);
            var arm2 = ModelKit.Part("arm2", inf.transform, UnitModels.WeaponArm(f, k.Weapon)); arm2.transform.localPosition = new Vector3(PeopleModels.ShoulderX, PeopleModels.Shoulder, 0); arm2.transform.localRotation = Quaternion.Euler(armPitch, 0, 0);
            foreach (int side in new[] { -1, 1 }) { var l = ModelKit.Part("leg", inf.transform, UnitModels.Leg(f)); l.transform.localPosition = new Vector3(side * PeopleModels.LegX, PeopleModels.Hip, 0); }
            return inf;
        }

        /// <summary>A gallery of every building model in both styles, sites and figures, laid out off the coast (view objects only).</summary>
        [UnityTest]
        public IEnumerator Gallery_EveryKindInBothStyles()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = GodBootstrap.Instance;
            var cam = boot.GodCamera;
            var root = new GameObject("Gallery");
            var origin = new Vector3(0, 0, -520f);
            foreach (Faction f in new[] { Faction.F0, Faction.F1 })
            {
                var o = origin + new Vector3(f == Faction.F0 ? -60f : 60f, 0, 0);
                for (int i = 0; i < BuildingModels.Kinds.Length; i++)
                {
                    var kind = BuildingModels.Kinds[i];
                    var go = ModelKit.Part(kind, root.transform, BuildingModels.MeshOf(kind, f));
                    go.transform.position = o + new Vector3((i % 4) * 17f - 25f, 0, -(i / 4) * 17f + 17f);
                    go.transform.rotation = Quaternion.Euler(0, 160f, 0);
                    var rig = BuildingModels.RigOf(kind, f);
                    var ban = ModelKit.Part("banner", go.transform, BuildingModels.BannerMesh(f));
                    ban.transform.localPosition = rig.BannerTop; ban.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                    if (rig.HasWheel) { var w = ModelKit.Part("wheel", go.transform, BuildingModels.WheelMesh(kind, f, rig.WheelR)); w.transform.localPosition = rig.Wheel; }
                }
                // Site stages along the bottom row.
                for (int k = 0; k <= 3; k++)
                {
                    var go = ModelKit.Part("site", root.transform, PropModels.SiteMesh(BuildingSize.Medium, f, k * 3 + 1));
                    go.transform.position = o + new Vector3(k * 13f - 25f, 0, -40f);
                    go.transform.rotation = Quaternion.Euler(0, 160f, 0);
                }
                // Soldiers, one per weapon, each in a different kit.
                var uo = origin + new Vector3(f == Faction.F0 ? -6f : 6f, 0, 26f);
                for (int i = 0; i < GalleryKits.Length; i++) Soldier(root.transform, f, GalleryKits[i], uo + new Vector3((i % 3) * 2.2f - 2.2f, 0, (i / 3) * 2.6f), i == 3 ? -85f : -20f);
            }
            // Raw piles.
            var mb = new MeshBuilder();
            int n = 0;
            foreach (RawKind k in new[] { RawKind.Log, RawKind.Clay, RawKind.Rock, RawKind.Grain }) PropModels.Pile(mb, origin + new Vector3(-12f + 8f * n++, 0, 40f), k, n);
            ModelKit.Part("piles", root.transform, mb.ToMesh("piles"));
            for (int i = 0; i < 3; i++) yield return null;
            Aim(cam, origin + new Vector3(-60f, 0, 0f), 120f, 50f);
            yield return null;
            Shot(cam, "p4b_gallery_player.png");
            Aim(cam, origin + new Vector3(60f, 0, 0f), 120f, 50f);
            yield return null;
            Shot(cam, "p4b_gallery_enemy.png");
            Aim(cam, origin + new Vector3(0f, 0, 32f), 45f, 50f);
            yield return null;
            Shot(cam, "p4b_gallery_units.png");
            Object.Destroy(root);
        }

        /// <summary>
        /// Load check at the size the brief asks for (about 60 buildings, 60 walkers, 40 units). The chain runs first so the
        /// town is real; then the sim is stopped and the test (not the view) adds synthetic buildings, walkers that walk
        /// lattice lines, and fighting units, and moves them each frame. Frame time is logged; the bound is loose because
        /// the editor and test runner add their own cost.
        /// </summary>
        [UnityTest]
        public IEnumerator Load_60Buildings60Walkers40Units_StaysSmooth()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = GodBootstrap.Instance;
            var s = boot.Sim.State; var t = s.Territory; var g = t.Grid; var m = s.Military;
            boot.DebugSample();
            boot.Speed = 48f;
            Time.captureFramerate = 30;
            int frames = 0;
            while (s.Tick < 6000 && frames++ < 6000) yield return null;
            Time.captureFramerate = 0;
            boot.Running = false;

            // Synthetic load (test only, the sim no longer steps).
            var kinds = BuildingModels.Kinds;
            int id = 900000;
            var used = new System.Collections.Generic.List<int>();
            foreach (var b in t.Buildings) used.Add(b.Node);
            for (int n = 0; n < g.Count && t.Buildings.Count < 64; n += 37)
            {
                if (!g.IsLand(n) || t.NodeBuilding[n] != 0 || t.NodeRoad[n] != 0 || t.NodeFlag[n] != 0) continue;
                if (used.Any(u => g.Distance(u, n) < 4)) continue;
                used.Add(n);
                var kind = kinds[1 + id % (kinds.Length - 1)];
                t.Buildings.Add(new Building { Id = id++, Faction = (Faction)(id % FactionInfo.Max), Node = n, Size = BuildingSize.Medium, Kind = kind, FlagNode = g.FlagNodeOf(n) });
            }
            var walkers = new System.Collections.Generic.List<Walker>();
            for (int n = 11; n < g.Count && walkers.Count < 60; n += 53)
            {
                var path = new System.Collections.Generic.List<int> { n };
                int dir = walkers.Count % 6;
                while (path.Count < 14) { int nx = g.Neighbour(path[path.Count - 1], dir); if (!g.IsValid(nx) || !g.IsLand(nx)) break; path.Add(nx); }
                if (path.Count < 6 || !g.IsLand(n)) continue;
                var w = new Walker { Id = id++, Kind = (WalkerKind)(walkers.Count % 2 == 0 ? 0 : 1), Faction = Faction.F0, Path = path.ToArray(), Loaded = walkers.Count % 2 == 0, JobWare = (Ware)(1 + walkers.Count % 10), Phase = WalkerPhase.Carrying };
                walkers.Add(w); s.Economy.Walkers.Add(w);
            }
            var units = new System.Collections.Generic.List<MilUnit>();
            var c0 = new Vector3((float)g.X(t.HqsOf(Faction.F0)[0].Node), 0, (float)g.Z(t.HqsOf(Faction.F0)[0].Node));
            for (int i = 0; i < 40; i++)
            {
                var u = new MilUnit { Id = id++, Faction = (Faction)(i % FactionInfo.Max), Kind = UnitKind.Infantry, Kit = GalleryKits[i % GalleryKits.Length], MaxHealth = 100, Health = 40 + i, Activity = UnitActivity.Fighting, LastOutcome = (AttackOutcome)(1 + i % 4) };
                units.Add(u); m.Units.Add(u);
            }
            for (int i = 0; i < units.Count; i++) { units[i].TargetKind = TargetKind.Unit; units[i].TargetId = units[(i + 1) % units.Count].Id; }

            var eco = boot.Economy; var mil = boot.Military;
            IslandCamera.Frame(boot.GodCamera, BoardSpace.FrameRadius + 40f, BoardSpace.BoardCentre);
            float t0 = 0f; int measured = 0; float worst = 0f;
            int maxB = 0, maxW = 0, maxU = 0, shots = 0;
            for (int f = 0; f < 360; f++)
            {
                foreach (var w in walkers)
                {
                    w.Progress++;
                    if (w.Progress >= w.StepTicks) { w.Progress = 0; w.Index = (w.Index + 1) % (w.Path.Length - 1); }
                }
                for (int i = 0; i < units.Count; i++)
                {
                    var u = units[i];
                    float a = (f * 0.01f + i * 0.157f), r = 25f + (i % 5) * 4f;
                    int nx = (int)((c0.x + Mathf.Cos(a) * r) * 1000f), nz = (int)((c0.z + Mathf.Sin(a) * r) * 1000f);
                    u.FaceX = nx - u.X; u.FaceZ = nz - u.Z; u.X = nx; u.Z = nz;
                    u.Cooldown = (f + i * 3) % 12; u.FireFlash = u.Cooldown > 9 ? 1 : 0;
                }
                float f0 = Time.realtimeSinceStartup;
                yield return null;
                if (f >= 60)
                {
                    float d = Time.realtimeSinceStartup - f0;
                    t0 += d; measured++; worst = Mathf.Max(worst, d);
                }
                maxB = Mathf.Max(maxB, eco.BuildingMarkerCount); maxW = Mathf.Max(maxW, eco.WalkerCount); maxU = Mathf.Max(maxU, mil.UnitsDrawn);
                shots = Mathf.Max(shots, mil.TracersDrawn);
            }
            float avg = t0 / measured;
            int renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r => r.enabled && r.gameObject.activeInHierarchy);
            Debug.Log($"P4b load: buildings {maxB}, walkers {maxW}, units {maxU}, shots in flight {shots}, renderers {renderers}, avg frame {avg * 1000f:0.0} ms, worst {worst * 1000f:0.0} ms, puffs {FxLayer.Instance.PuffsDrawn}");
            Assert.GreaterOrEqual(maxB, 55, "about 60 buildings drawn");
            Assert.GreaterOrEqual(maxW, 55, "about 60 walkers drawn");
            Assert.GreaterOrEqual(maxU, 40, "40 units drawn");
            Assert.Less(avg, 0.1f, "frames stay well under 100 ms in the editor");
        }

        /// <summary>
        /// Stage 3: soldiers are drawn in their kit (weapon, shield, helmet and body named on the figure), a duel between
        /// a melee and a ranged soldier draws strikes, flying missiles and miss or block puffs, and the dead fall.
        /// </summary>
        [UnityTest]
        public IEnumerator Soldiers_WearTheirKit_AndDuelsShowStrikesAndPuffs()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = GodBootstrap.Instance;
            boot.Running = false;
            var sim = boot.Sim; var s = sim.State; var g = s.Territory.Grid; var mil = boot.Military;
            var hq = s.Territory.HqsOf(Faction.F0)[0];
            var foe = s.Territory.Hqs.First(h => h.Faction != Faction.F0).Faction;
            if (!s.Factions.AtWar(Faction.F0, foe)) { Assert.IsTrue(sim.Submit(new DeclareWar(Faction.F0, foe)).Ok); sim.Step(); }
            int node = g.FlagNodeOf(hq.Node);
            int id0 = s.Military.NextUnitId;
            var kits = new[]
            {
                new Kit(Ware.Bow, Ware.None, Ware.Helmet, Ware.LightArmour),
                new Kit(Ware.Axe, Ware.BigShield, Ware.None, Ware.Breastplate),
                new Kit(Ware.Sling, Ware.SmallShield),
                new Kit(Ware.Spear, Ware.SmallShield, Ware.Helmet),
            };
            for (int i = 0; i < kits.Length; i++) Assert.IsTrue(sim.Submit(DevSpawnUnit.WithKit(i % 2 == 0 ? Faction.F0 : foe, kits[i], node)).Ok);
            sim.Step(); sim.DrainEvents();
            yield return null;
            for (int i = 0; i < kits.Length; i++)
            {
                var fig = mil.Units.FigureOf(id0 + i);
                Assert.IsNotNull(fig, "unit " + i + " drawn");
                var k = kits[i];
                Assert.IsNotNull(fig.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Weapon arm " + Wares.Name(k.Weapon)), "holds a " + Wares.Name(k.Weapon));
                Assert.IsNotNull(fig.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Shield arm " + Wares.Name(k.Shield)), "shield " + Wares.Name(k.Shield));
                Assert.IsNotNull(fig.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Torso " + Wares.Name(k.Head) + " " + Wares.Name(k.Body)), "head and body " + k);
            }
            Assert.AreEqual(2, mil.Units.CountOf(Ware.Bow, Faction.F0) + mil.Units.CountOf(Ware.Sling, Faction.F0));
            var cam = boot.GodCamera;
            Aim(cam, At(g, node), 30f, 45f);
            int strikes0 = mil.Units.StrikesSoFar, puffs0 = FxLayer.Instance.MissesShown + FxLayer.Instance.BlocksShown, maxShots = 0, deaths0 = mil.Units.DeathsSoFar;
            bool shot = false;
            for (int f = 0; f < 1200 && s.Military.Units.Count(u => u.Id >= id0) > 2; f++)
            {
                sim.Step(); sim.DrainEvents();
                yield return null;
                maxShots = Mathf.Max(maxShots, FxLayer.Instance.ShotsDrawn);
                if (!shot && FxLayer.Instance.ShotsDrawn > 0 && mil.Units.StrikesSoFar > strikes0 + 4) { shot = true; Shot(cam, "q2_duel.png"); }
            }
            for (int i = 0; i < 5; i++) yield return null;
            Assert.Greater(mil.Units.StrikesSoFar, strikes0 + 4, "strikes drawn");
            Assert.Greater(maxShots, 0, "an arrow or stone in flight");
            Assert.Greater(FxLayer.Instance.MissesShown + FxLayer.Instance.BlocksShown, puffs0, "a miss or block puff");
            Assert.Greater(mil.Units.DeathsSoFar, deaths0, "someone fell");
            Debug.Log("Q2 duel: strikes " + (mil.Units.StrikesSoFar - strikes0) + " misses " + FxLayer.Instance.MissesShown + " blocks " + FxLayer.Instance.BlocksShown + " crits " + FxLayer.Instance.CritsShown + " tick " + s.Tick);
        }

        [UnityTest]
        public IEnumerator BuildableGlow_IsOffUntilAsked()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = GodBootstrap.Instance;
            var tv = boot.Territory;
            Assert.IsFalse(tv.BuildableShown);
            Assert.IsFalse(tv.ShowDebugOverlay);
            tv.ShowBuildable(true);
            yield return null;
            Assert.IsTrue(tv.BuildableShown);
            Assert.Greater(tv.Ground.GlowSpots, 0, "buildable spots glow in placing mode");
            tv.ShowBuildable(false);
            yield return null;
            Assert.IsFalse(tv.BuildableShown);
        }
    }
}
