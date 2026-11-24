using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P0 proof that sim to view works: deals the island from a seed in <see cref="GodSim"/>, builds it with the
    /// copied terrain view and shows it from the tilted god camera, in day light. It only reads the sim.
    /// Pass <c>-godseed N</c> on the command line to deal another island.
    /// </summary>
    public sealed class GodBootstrap : MonoBehaviour
    {
        public const ulong DefaultSeed = 20261124UL;

        [SerializeField] private ulong seed = DefaultSeed;

        public static GodBootstrap Instance { get; private set; }
        public GodSim Sim { get; private set; }
        public HexDressingView Island { get; private set; }
        public Camera GodCamera { get; private set; }

        private float _framedAspect = -1f;

        private void Awake()
        {
            Instance = this;
            seed = SeedFromCommandLine(seed);
            Sim = GodSim.Create(seed);
            BoardSpace.Topo = Sim.State.Board.Topology;
            Island = HexDressingView.Create(Sim.State.Board, transform);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.54f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.32f, 0.28f);

            GodCamera = Camera.main;
            if (GodCamera == null)
            {
                var go = new GameObject("God Camera") { tag = "MainCamera" };
                GodCamera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            GodCamera.clearFlags = CameraClearFlags.SolidColor;
            GodCamera.backgroundColor = new Color(0.62f, 0.78f, 0.90f);
            EnsureSun();
        }

        private void LateUpdate()
        {
            if (GodCamera != null && !Mathf.Approximately(_framedAspect, GodCamera.aspect))
            {
                _framedAspect = GodCamera.aspect;
                IslandCamera.Frame(GodCamera, BoardSpace.BoardRadius + 40f);
            }
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private static void EnsureSun()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) return;
            var go = new GameObject("Sun");
            var sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        }

        private static ulong SeedFromCommandLine(ulong fallback)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-godseed" && ulong.TryParse(args[i + 1], out var s)) return s;
            return fallback;
        }
    }
}
