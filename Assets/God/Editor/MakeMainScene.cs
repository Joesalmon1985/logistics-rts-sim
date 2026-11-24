using God.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace God.Editor
{
    /// <summary>Creates Assets/God/Scenes/Main.unity (camera, sun and the <see cref="GodBootstrap"/>) and lists it in the build settings.</summary>
    public static class MakeMainScene
    {
        public const string Path = "Assets/God/Scenes/Main.unity";

        [MenuItem("God/Make Main Scene")]
        public static string Run()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.78f, 0.90f);
            camGo.AddComponent<AudioListener>();
            IslandCamera.Frame(cam, 340f);

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            new GameObject("God Bootstrap").AddComponent<GodBootstrap>();

            System.IO.Directory.CreateDirectory("Assets/God/Scenes");
            bool ok = EditorSceneManager.SaveScene(scene, Path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Path, true) };
            return ok ? "Saved " + Path : "Failed to save " + Path;
        }
    }
}
