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
        /// <summary>The organic island look (P4a): the hexes stay in the sim, the player sees a natural island.</summary>
        public IslandView Island { get; private set; }
        /// <summary>The old hex-tile look, built only with <see cref="legacyHexLook"/> or <c>-godhexlook</c> (kept until the P2/P3 merge).</summary>
        public HexDressingView LegacyIsland { get; private set; }

        [SerializeField] private bool legacyHexLook = false;
        public Camera GodCamera { get; private set; }

        /// <summary>The territory debug view (P1).</summary>
        public TerritoryView Territory { get; private set; }

        /// <summary>The military debug view (P3).</summary>
        public MilitaryView Military { get; private set; }

        /// <summary>The economy debug view (P2).</summary>
        public EconomyView Economy { get; private set; }

        /// <summary>When true the bootstrap steps the sim at 10 Hz times <see cref="Speed"/>. Off by default; <c>-godrun</c> or F3 turns it on.</summary>
        public bool Running = true;
        /// <summary>The god HUD and input (P5).</summary>
        public GodHud Hud { get; private set; }
        /// <summary>The god camera controls (P5).</summary>
        public GodCameraRig Rig { get; private set; }
        /// <summary>Seed for the next scene load (set by the end cards); consumed by the next bootstrap.</summary>
        public static ulong? PendingSeed;
        /// <summary>The seed the island was dealt (and dressed) from.</summary>
        public ulong Seed => seed;

        /// <summary>The scripted builder started by F2 (dev): it sends the commands of the full economy chain.</summary>
        public ChainBot Chain { get; private set; }
        private long _lastThink;
        public float Speed = 1f;

        private float _framedAspect = -1f;
        private float _acc;

        private void Awake()
        {
            Instance = this;
            seed = SeedFromCommandLine(seed);
            if (PendingSeed.HasValue) { seed = PendingSeed.Value; PendingSeed = null; }
            Sim = GodSim.Create(seed);
            BoardSpace.Topo = Sim.State.Board.Topology;
            if (legacyHexLook || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-godhexlook") >= 0)
                LegacyIsland = HexDressingView.Create(Sim.State.Board, transform);
            else
                Island = IslandView.Create(Sim.State.Board, seed, Sim, transform);
            Territory = TerritoryView.Create(Sim, transform);
            Territory.Built = () => { int n = 0; foreach (var eb in Sim.State.Economy.Buildings) if (eb.Built) n++; return n; };
            Economy = EconomyView.Create(Sim, transform);
            Military = MilitaryView.Create(Sim, transform);
            ReadRunArgs();
            SmokeRun.StartIfAsked(this);

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

            Rig = GodCameraRig.Attach(GodCamera);
            if (Island != null) Rig.HeightAt = (x, z) => Island.HeightAt(new Vector3(x, 0f, z));
            Hud = GodHud.Create(this, Rig);
        }

        /// <summary>The seed the next game uses: the same one, or a fresh one for a new island. Sets <see cref="PendingSeed"/>.</summary>
        public ulong PrepareRestart(bool newIsland)
        {
            ulong next = seed;
            if (newIsland)
            {
                // View-side only (not sim state): any fresh seed will do.
                next = (ulong)System.DateTime.UtcNow.Ticks * 6364136223846793005UL + 1442695040888963407UL + seed;
                next %= 1000000000UL; if (next == seed) next++;
            }
            PendingSeed = next;
            return next;
        }

        /// <summary>Reloads the scene: the same island ("Play again") or a new one.</summary>
        public void Restart(bool newIsland)
        {
            PrepareRestart(newIsland);
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        }

        private void Update()
        {
            // Debug keys: F2 starts the full economy chain script, F3 runs or pauses the sim, F4 doubles its speed (F5 halves it),
            // F10 lays out the P1 territory sample; F6-F9 are the military keys.
            if (Input.GetKeyDown(KeyCode.F2)) DebugSample();
            if (Input.GetKeyDown(KeyCode.F10)) TerritorySample.Apply(Sim);
            if (Input.GetKeyDown(KeyCode.F3)) Running = !Running;
            if (Input.GetKeyDown(KeyCode.F4)) Speed = Mathf.Min(Speed * 2f, 64f);
            if (Input.GetKeyDown(KeyCode.F5)) Speed = Mathf.Max(Speed * 0.5f, 0.25f);
            if (Input.GetKeyDown(KeyCode.F6)) DebugSpawnSquad();
            if (Input.GetKeyDown(KeyCode.F7)) DebugMarkNearestEnemy();
            if (Input.GetKeyDown(KeyCode.F8)) DebugDefendHq();
            if (Input.GetKeyDown(KeyCode.F9)) DebugFight();
            if (Sim.State.Military != null && Sim.State.Military.IsGameOver) return;
            // Paused: still apply a queued command (a placement) so it shows at once. One tick, no more.
            if (!Running) { if (Sim.PendingCommands.Count > 0) { Sim.Step(); Sim.DrainEvents(); } return; }
            _acc += Time.deltaTime * Speed * GodSim.TicksPerSecond;
            int steps = Mathf.Min((int)_acc, 400);
            _acc -= (int)_acc;
            if (steps > 0) Sim.Step(steps);
            if (Chain != null && Sim.State.Tick - _lastThink >= 20) { _lastThink = Sim.State.Tick; Chain.Think(); }
            Sim.DrainEvents();
        }

        /// <summary>
        /// Dev: starts the scripted full chain (sawmill, quarry, woodcutter, kiln, clay pit, mine, smelter, farm, barracks,
        /// vehicle workshop, with HQs and roads as needed), sending the same commands a player would, and runs the sim.
        /// Returns the bot's log (empty at the start).
        /// </summary>
        public System.Collections.Generic.List<string> DebugSample()
        {
            Chain = new ChainBot(Sim);
            _lastThink = Sim.State.Tick;
            Running = true;
            return Chain.Log;
        }

        /// <summary>Dev (F6): three infantry and two vehicles for the player, at the player HQ.</summary>
        public void DebugSpawnSquad()
        {
            var hq = Sim.State.Territory.HqsOf(Faction.Player)[0];
            for (int i = 0; i < 3; i++) Sim.Submit(new DevSpawnUnit(Faction.Player, UnitKind.Infantry, hq.Node));
            for (int i = 0; i < 2; i++) Sim.Submit(new DevSpawnUnit(Faction.Player, UnitKind.Vehicle, hq.Node));
        }

        /// <summary>Dev (F7): marks the enemy building nearest the player HQ, after the ones already marked.</summary>
        public void DebugMarkNearestEnemy()
        {
            var t = Sim.State.Territory; var m = Sim.State.Military; var g = t.Grid;
            var hq = t.HqsOf(Faction.Player)[0];
            Building best = null; int bd = int.MaxValue;
            foreach (var b in t.Buildings)
            {
                if (b.Faction != Faction.Enemy || m.Marked.Contains(b.Id)) continue;
                int d = g.Distance(b.Node, hq.Node);
                if (d < bd) { bd = d; best = b; }
            }
            if (best != null) Sim.Submit(new MarkTarget(best.Id));
        }

        /// <summary>Dev (F8): a defend flag with two guards on the player HQ.</summary>
        public void DebugDefendHq()
        {
            Sim.Submit(SetDefend.OnBuilding(Sim.State.Territory.HqsOf(Faction.Player)[0].Id, 2));
        }

        /// <summary>Dev (F9): a ready-made fight at the enemy base: five player units, three enemy infantry, all enemy buildings marked.</summary>
        public void DebugFight()
        {
            var t = Sim.State.Territory; var g = t.Grid;
            var eh = t.HqsOf(Faction.Enemy)[0];
            int at(int di, int dj) { int n = g.Id(g.I(eh.Node) + di, g.J(eh.Node) + dj); return g.IsLand(n) ? n : eh.Node; }
            for (int i = 0; i < 3; i++) Sim.Submit(new DevSpawnUnit(Faction.Player, UnitKind.Infantry, at(-5 + i, 2)));
            for (int i = 0; i < 2; i++) Sim.Submit(new DevSpawnUnit(Faction.Player, UnitKind.Vehicle, at(-6, i)));
            for (int i = 0; i < 3; i++) Sim.Submit(new DevSpawnUnit(Faction.Enemy, UnitKind.Infantry, at(-1 - i, 3)));
            foreach (var b in t.Buildings) if (b.Faction == Faction.Enemy) Sim.Submit(new MarkTarget(b.Id));
            DebugDefendHq();
        }

        private void ReadRunArgs()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-godrun") Running = true;
                else if (args[i] == "-godsample") DebugSample();
                else if (args[i] == "-godfight") DebugFight();
                else if (args[i] == "-godspeed" && i + 1 < args.Length && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sp)) Speed = sp;
            }
        }

        private void LateUpdate()
        {
            if (GodCamera != null && !Mathf.Approximately(_framedAspect, GodCamera.aspect))
            {
                _framedAspect = GodCamera.aspect;
                if (Rig != null) Rig.Reframe(BoardSpace.BoardRadius + 40f);
                else IslandCamera.Frame(GodCamera, BoardSpace.BoardRadius + 40f);
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
