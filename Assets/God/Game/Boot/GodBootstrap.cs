using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P0 proof that sim to view works: deals the island from a seed in <see cref="GodSim"/>, builds it with the
    /// copied terrain view and shows it from the tilted god camera, in day light. It only reads the sim.
    /// Pass <c>-godseed N</c> on the command line to deal another island, <c>-godwatch</c> for six bots and no human.
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

        /// <summary>The scripted builders started by F2 (dev): one per faction, each sending the commands of the full economy chain.</summary>
        public readonly System.Collections.Generic.List<ChainBot> Chains = new System.Collections.Generic.List<ChainBot>();
        /// <summary>The human faction's chain bot (F0's in watch mode), or null before F2.</summary>
        public ChainBot Chain => Chains.Find(c => c.Side == FactionLook.Viewer(Sim.State));
        /// <summary>The setup for the next scene load (watch mode, another human faction); consumed by the next bootstrap.</summary>
        public static GameSetup PendingSetup;
        /// <summary>
        /// A game read from a save, for the next scene load (set by <see cref="LoadSlot"/>); consumed by the next bootstrap, which
        /// uses its sim as it is instead of dealing a new island.
        /// </summary>
        public static LoadedGame PendingLoad;
        /// <summary>Set by restarts and loads: the next bootstrap skips the start card and starts the AIs.</summary>
        public static bool PendingStartAi;
        /// <summary>Tests: never show the start card (it also pauses the game until a choice is made).</summary>
        public static bool SkipStartCard;
        /// <summary>True when this bootstrap shows the start card first.</summary>
        public bool ShowStartCard { get; private set; }
        /// <summary>The tutorial cards (null unless this is a tutorial game).</summary>
        public TutorialCards Tutorial { get; private set; }
        /// <summary>Esc menu, start card and watch-mode controls (P10).</summary>
        public GameMenu Menu { get; private set; }
        /// <summary>Toasts for big events (P10).</summary>
        public ToastFeed Toasts { get; private set; }
        /// <summary>Tall faction standards over every HQ and Citadel (P10).</summary>
        public StandardLayer Standards { get; private set; }
        /// <summary>Watch-mode camera keys (P10).</summary>
        public WatchCamera WatchCam { get; private set; }
        /// <summary>Raised for every sim event the bootstrap drains each frame (toasts, effects). Tests may call <see cref="PumpEvents"/>.</summary>
        public event System.Action<SimEvent> SimEventRaised;
        private readonly System.Collections.Generic.List<ISaveExtra> _saveExtras = new System.Collections.Generic.List<ISaveExtra>();

        /// <summary>A save read and turned into a sim, waiting for the scene to reload.</summary>
        public sealed class LoadedGame
        {
            public SaveFile File;
            public GodSim Sim;
        }

        /// <summary>The human faction, or null in watch mode (<c>-godwatch</c>).</summary>
        public Faction? Human => FactionLook.Human(Sim.State);
        private long _lastThink;
        public float Speed = 1f;

        private float _framedAspect = -1f;
        private float _acc;

        private static bool HasArg(string name) => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), name) >= 0;

        /// <summary>Command-line flags that skip the start card: the player (or a script) has already chosen.</summary>
        private static readonly string[] SkipCardFlags = { "-godseed", "-godwatch", "-godplay", "-godsmoke", "-godsample", "-godfight", "-godrun" };

        private void Awake()
        {
            Instance = this;
            bool flagged = false;
            foreach (var f in SkipCardFlags) if (HasArg(f)) flagged = true;
            var loaded = PendingLoad; PendingLoad = null;
            bool startAi = PendingStartAi; PendingStartAi = false;
            if (loaded != null)
            {
                Sim = loaded.Sim;
                seed = Sim.State.Seed;
                startAi = true;
            }
            else
            {
                seed = SeedFromCommandLine(seed);
                if (PendingSeed.HasValue) { seed = PendingSeed.Value; PendingSeed = null; }
                var setup = PendingSetup ?? new GameSetup(seed, HasArg("-godwatch") || HasArg("-godsmoke") ? GameSetup.NoHuman : 0);
                PendingSetup = null;
                setup.Seed = seed;
                Sim = GodSim.Create(setup);
                // A script or a flag chose already: the AIs play (the smoke too: it watches six brains; not the dev runs).
                if (flagged && !HasArg("-godsample") && !HasArg("-godfight")) startAi = true;
            }
            if (SkipStartCard && loaded == null) startAi = true; // tests skip the card: the game starts as after Play
            ShowStartCard = !SkipStartCard && !flagged && !startAi && loaded == null;
            BoardSpace.Topo = Sim.State.Board.Topology;
            if (legacyHexLook || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-godhexlook") >= 0)
                LegacyIsland = HexDressingView.Create(Sim.State.Board, transform);
            else
                Island = IslandView.Create(Sim.State.Board, seed, Sim, transform);
            Territory = TerritoryView.Create(Sim, transform);
            Territory.Built = () => { int n = 0; foreach (var eb in Sim.State.Economy.Buildings) if (eb.Built) n++; return n; };
            Economy = EconomyView.Create(Sim, transform);
            Military = MilitaryView.Create(Sim, transform);
            Standards = StandardLayer.Create(Sim, transform);
            ReadRunArgs();
            SmokeRun.StartIfAsked(this);
            _saveExtras.Add(new ChainBotsSave(this));
            if (Sim.State.Setup.Tutorial) { Tutorial = TutorialCards.Create(this); _saveExtras.Add(Tutorial); }
            if (startAi) StartAi();
            if (loaded != null)
            {
                // A loaded game waits for the player: Space (or Play) goes on.
                foreach (var x in _saveExtras) if (loaded.File.Extras.TryGetValue(x.SaveKey, out var data)) x.LoadState(data);
                Running = false;
            }

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
            Toasts = ToastFeed.Create(this);
            Menu = GameMenu.Create(this);
            WatchCam = WatchCamera.Create(this);
            if (loaded != null) Hud.Toast("Game loaded. Press Space to go on.");
        }

        // ---------------------------------------------------------------- save, load, bots (P10)

        /// <summary>Systems outside the sim that put their state into a save (see <see cref="ISaveExtra"/>). The faction AI registers here.</summary>
        public System.Collections.Generic.IReadOnlyList<ISaveExtra> SaveExtras => _saveExtras;

        public void RegisterSaveExtra(ISaveExtra extra)
        {
            if (extra == null) return;
            _saveExtras.RemoveAll(x => x.SaveKey == extra.SaveKey);
            _saveExtras.Add(extra);
        }

        /// <summary>
        /// Starts the AI for every faction without a human (all six in watch mode): for now the scripted economy builders;
        /// the faction AI replaces them here. Called for a new game and after every load, so a loaded game's bots play on.
        /// </summary>
        public void StartAi()
        {
            // P11: the faction AI (P9 brains) plays every non-human faction. Brains rebuild from state, so a loaded game goes on.
            Chains.Clear();
            if (AiHost.Director == null || AiHost.Director.Sim != Sim) AiHost.StartForGame(Sim);
            _lastThink = Sim.State.Tick;
        }

        /// <summary>Snapshot of the running game: the sim, what is queued, and every registered extra.</summary>
        public SaveFile MakeSave() => SaveFile.From(Sim, System.DateTime.UtcNow.ToString("o"), _saveExtras);

        public void SaveToSlot(int slot) { SaveStore.Write(slot, MakeSave()); }

        /// <summary>Reads a slot and reloads the scene into it. Throws <see cref="SaveException"/> when it cannot be read; the game goes on then.</summary>
        public void LoadSlot(int slot) { LoadGame(SaveStore.Read(slot)); }

        public void LoadGame(SaveFile file)
        {
            var sim = file.ToSim(); // throws before anything changes
            PendingLoad = new LoadedGame { File = file, Sim = sim };
            ReloadScene();
        }

        private static void ReloadScene() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);

        /// <summary>A fresh seed for a new island (view side: any value will do).</summary>
        public static ulong RandomSeed() => (ulong)(System.Guid.NewGuid().GetHashCode() & 0x7FFFFFFF) % 1000000000UL + 1UL;

        /// <summary>Starts another game on the next scene load: a seed, play (human faction 0) or watch.</summary>
        public static void StartNew(ulong seed, bool watch)
        {
            PendingSeed = seed;
            PendingSetup = new GameSetup(seed, watch ? GameSetup.NoHuman : 0);
            PendingStartAi = true;
            ReloadScene();
        }

        /// <summary>
        /// Starts the tutorial on the next scene load: the fixed island (<see cref="TutorialIsland.Seed"/>), you as Amber,
        /// the AIs at peace with you, and the pop-up cards. The setup carries <c>Tutorial</c>, so a save keeps it.
        /// </summary>
        public static void StartTutorial()
        {
            PendingSeed = TutorialIsland.Seed;
            PendingSetup = TutorialIsland.Setup();
            PendingStartAi = true;
            ReloadScene();
        }

        /// <summary>Hands the frame's sim events to listeners (toasts). The bootstrap calls it every frame; tests may too.</summary>
        public void PumpEvents()
        {
            var list = Sim.DrainEvents();
            if (SimEventRaised != null) foreach (var e in list) SimEventRaised(e);
        }

        /// <summary>The seed the next game uses: the same one, or a fresh one for a new island. Sets <see cref="PendingSeed"/>.</summary>
        public ulong PrepareRestart(bool newIsland)
        {
            ulong next = seed;
            bool tutorial = Sim.State.Setup.Tutorial; // the tutorial island never changes
            if (newIsland && !tutorial)
            {
                // View-side only (not sim state): any fresh seed will do.
                next = (ulong)System.DateTime.UtcNow.Ticks * 6364136223846793005UL + 1442695040888963407UL + seed;
                next %= 1000000000UL; if (next == seed) next++;
            }
            PendingSeed = next;
            PendingSetup = new GameSetup(next, Sim.State.Setup.Human, Sim.State.Setup.FactionCount) { Tutorial = tutorial };
            PendingStartAi = true;
            return next;
        }

        /// <summary>Reloads the scene: the same island ("Play again") or a new one.</summary>
        public void Restart(bool newIsland)
        {
            PrepareRestart(newIsland);
            ReloadScene();
        }

        private void Update()
        {
            // Dev keys: F2 starts a chain bot for every faction, F3 runs or pauses the sim, F4 doubles its speed (F5 halves
            // it), F10 lays out the P1 territory sample for the human faction. (The old F6-F9 military keys are gone.)
            if (Input.GetKeyDown(KeyCode.F2)) DebugSample();
            if (Input.GetKeyDown(KeyCode.F10)) TerritorySample.Apply(Sim);
            if (Input.GetKeyDown(KeyCode.F3)) Running = !Running;
            if (Input.GetKeyDown(KeyCode.F4)) Speed = Mathf.Min(Speed * 2f, 64f);
            if (Input.GetKeyDown(KeyCode.F5)) Speed = Mathf.Max(Speed * 0.5f, 0.25f);
            if (Menu != null && Menu.Blocking) return; // the menu or the start card holds the game
            if (Sim.State.IsGameOver) { PumpEvents(); return; }
            // Paused: still apply a queued command (a placement) so it shows at once. One tick, no more.
            if (!Running) { if (Sim.PendingCommands.Count > 0) { AiHost.StepHeld(Sim); PumpEvents(); } return; }
            _acc += Time.deltaTime * Speed * GodSim.TicksPerSecond;
            int steps = Mathf.Min((int)_acc, 400);
            _acc -= (int)_acc;
            if (steps > 0) Sim.Step(steps);
            if (Chains.Count > 0 && Sim.State.Tick - _lastThink >= 20) { _lastThink = Sim.State.Tick; ThinkChains(); }
            PumpEvents();
        }

        /// <summary>One decision of every chain bot (the eliminated ones say nothing).</summary>
        public void ThinkChains()
        {
            foreach (var c in Chains) if (!Sim.State.IsGameOver && !Sim.State.Factions.IsEliminated(c.Side)) c.Think();
        }

        /// <summary>
        /// Dev (F2): starts the scripted economy chain (sawmill, quarry, woodcutter, kiln, clay pit, mine, smelter, farm,
        /// barracks, vehicle workshop, with HQs and roads as needed) for every faction no AI brain plays (the human one),
        /// each sending the same commands a player would, and runs the sim. Returns the log of the human faction's bot (F0's in watch mode).
        /// </summary>
        public System.Collections.Generic.List<string> DebugSample()
        {
            Chains.Clear();
            foreach (var f in Sim.State.Setup.Factions) if (!Sim.State.Factions.IsEliminated(f) && !AiHost.Drives(Sim, f)) Chains.Add(new ChainBot(Sim, f));
            _lastThink = Sim.State.Tick;
            Running = true;
            return Chain?.Log ?? new System.Collections.Generic.List<string>();
        }

        /// <summary>
        /// Dev (-godfight): a ready-made fight. The human faction (F0 in watch mode) declares war on the faction whose HQ is
        /// nearest, spawns seven kitted soldiers (two archers, two heavy axemen) by that HQ, marks the buildings round it and then the HQ, and puts two guards on its own HQ; the enemy gets three infantry.
        /// </summary>
        public void DebugFight()
        {
            var s = Sim.State; var t = s.Territory; var g = t.Grid;
            var me = FactionLook.Viewer(s);
            var mine = t.HqsOf(me)[0];
            Hq eh = null; int best = int.MaxValue;
            foreach (var h in t.Hqs)
                if (h.Faction != me && !h.Site && g.Distance(h.Node, mine.Node) < best) { best = g.Distance(h.Node, mine.Node); eh = h; }
            if (eh == null) return;
            if (!s.Factions.AtWar(me, eh.Faction)) { Sim.Submit(new DeclareWar(me, eh.Faction)); Sim.Step(); Sim.DrainEvents(); }
            int at(int di, int dj) { int n = g.Id(g.I(eh.Node) + di, g.J(eh.Node) + dj); return n >= 0 && g.IsLand(n) ? n : eh.Node; }
            var mineKits = new[] { new Kit(Ware.Sword, Ware.SmallShield, Ware.Helmet, Ware.LightArmour), new Kit(Ware.Spear, Ware.BigShield, Ware.Helmet, Ware.Breastplate), new Kit(Ware.Axe, Ware.SmallShield, Ware.Helmet, Ware.Breastplate) };
            var foeKits = new[] { new Kit(Ware.Sword), new Kit(Ware.Bow, Ware.None, Ware.Helmet), new Kit(Ware.Sling, Ware.SmallShield) };
            for (int i = 0; i < 3; i++) Sim.Submit(DevSpawnUnit.WithKit(me, mineKits[i], at(-5 + i, 2)));
            for (int i = 0; i < 2; i++) Sim.Submit(DevSpawnUnit.WithKit(me, new Kit(Ware.Bow, Ware.None, Ware.Helmet, Ware.LightArmour), at(-6, i)));
            for (int i = 0; i < 2; i++) Sim.Submit(DevSpawnUnit.WithKit(me, new Kit(Ware.Axe, Ware.BigShield, Ware.Helmet, Ware.Breastplate), at(-5 + i, 1)));
            for (int i = 0; i < 3; i++) Sim.Submit(DevSpawnUnit.WithKit(eh.Faction, foeKits[i], at(-1 - i, 3)));
            foreach (var b in t.Buildings) if (b.Faction == eh.Faction && b.Size != BuildingSize.Hq && g.Distance(b.Node, eh.Node) <= g.CircleRadiusSteps) Sim.Submit(new MarkTarget(me, b.Id));
            Sim.Submit(new MarkTarget(me, eh.Id));
            Sim.Submit(SetDefend.OnBuilding(me, mine.Id, 2));
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
                if (Rig != null) Rig.Reframe(BoardSpace.FrameRadius + 40f, BoardSpace.BoardCentre);
                else IslandCamera.Frame(GodCamera, BoardSpace.FrameRadius + 40f, BoardSpace.BoardCentre);
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
