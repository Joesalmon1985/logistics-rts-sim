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

        /// <summary>The territory debug view (P1).</summary>
        public TerritoryView Territory { get; private set; }

        /// <summary>When true the bootstrap steps the sim at 10 Hz times <see cref="Speed"/>. Off by default; <c>-godrun</c> or F3 turns it on.</summary>
        public bool Running;
        public float Speed = 1f;

        private float _framedAspect = -1f;
        private float _acc;

        private void Awake()
        {
            Instance = this;
            seed = SeedFromCommandLine(seed);
            Sim = GodSim.Create(seed);
            BoardSpace.Topo = Sim.State.Board.Topology;
            Island = HexDressingView.Create(Sim.State.Board, transform);
            Territory = TerritoryView.Create(Sim, transform);
            ReadRunArgs();

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

        private void Update()
        {
            // Debug keys: F2 lays out the sample, F3 runs or pauses the sim, F4 doubles its speed (F5 halves it).
            if (Input.GetKeyDown(KeyCode.F2)) DebugSample();
            if (Input.GetKeyDown(KeyCode.F3)) Running = !Running;
            if (Input.GetKeyDown(KeyCode.F4)) Speed = Mathf.Min(Speed * 2f, 64f);
            if (Input.GetKeyDown(KeyCode.F5)) Speed = Mathf.Max(Speed * 0.5f, 0.25f);
            if (!Running) return;
            _acc += Time.deltaTime * Speed * GodSim.TicksPerSecond;
            int steps = Mathf.Min((int)_acc, 400);
            _acc -= (int)_acc;
            if (steps > 0) Sim.Step(steps);
            Sim.DrainEvents();
        }

        /// <summary>Dev: places the sample layout through commands. Returns the refusals (empty when all went through).</summary>
        public System.Collections.Generic.List<string> DebugSample()
        {
            var log = TerritorySample.Apply(Sim);
            foreach (var l in log) Debug.LogWarning("TerritorySample: " + l);
            return log;
        }

        private void ReadRunArgs()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-godrun") Running = true;
                else if (args[i] == "-godsample") DebugSample();
                else if (args[i] == "-godspeed" && i + 1 < args.Length && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sp)) Speed = sp;
            }
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
