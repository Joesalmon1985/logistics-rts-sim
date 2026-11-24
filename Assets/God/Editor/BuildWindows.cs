using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace God.Editor
{
    /// <summary>
    /// Windows player build of the Main scene to Builds/Windows/GodOfTheIsland.exe (release build: no development
    /// watermark). Fullscreen window at the desktop's native resolution (Alt+Enter or the -screen-fullscreen 0 flag
    /// gives a window).
    /// Run: unity command script run --file AgentScripts/BuildWindows.cs --entry BuildWindows.Run --timeout 900
    /// or:  unity build . --target StandaloneWindows64 --execute-method God.Editor.BuildWindows.RunBatch
    /// </summary>
    public static class BuildWindows
    {
        public const string Output = "Builds/Windows/GodOfTheIsland.exe";
        public const string Scene = "Assets/God/Scenes/Main.unity";

        /// <summary>Builds and returns a one-line result.</summary>
        public static string Run()
        {
            PlayerSettings.productName = "God of the Island";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.usePlayerLog = true;
            EditorUserBuildSettings.development = false;

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var r = report.summary;
            // The Burst debug folder is not shipped.
            string burst = Path.Combine(Path.GetDirectoryName(Output), Path.GetFileNameWithoutExtension(Output) + "_BurstDebugInformation_DoNotShip");
            if (Directory.Exists(burst)) Directory.Delete(burst, true);
            return $"{r.result} size={r.totalSize / (1024 * 1024)}MB errors={r.totalErrors} warnings={r.totalWarnings} time={r.totalTime} ended={r.buildEndedAt:u}";
        }

        /// <summary>Entry for <c>-executeMethod</c> in batch mode: exits non-zero when the build fails.</summary>
        public static void RunBatch()
        {
            string result = Run();
            Debug.Log("[BuildWindows] " + result);
            if (!result.StartsWith("Succeeded")) EditorApplication.Exit(1);
        }
    }
}
