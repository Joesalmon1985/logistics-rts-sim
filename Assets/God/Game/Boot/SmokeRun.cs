using System.Collections.Generic;
using God.Recording;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Headless smoke for the built player: <c>-godsmoke N</c> plays a watch game (six AI brains, see <see cref="AiHost"/>)
    /// as fast as possible for up to N sim ticks, and logs the standings every 1000 ticks. The judgement (counting wars,
    /// captures, eliminations and trades, the invariants, the verdict) is <see cref="SmokeVerdict"/>, shared with the
    /// headless runner's <c>run --check smoke</c> (M0). It quits with exit code 0 when the verdict is OK, otherwise 1.
    /// Use only with <c>-batchmode -nographics</c>.
    /// <para>M0: at every sim day (1200 ticks) and at the end it logs <c>SMOKE HASH &lt;tick&gt; &lt;hex&gt;</c>, the same
    /// hash the runner prints for the same seed and ticks; <c>-godrecord FILE</c> also writes a replay (<c>.godr</c>) that
    /// <c>God.Runner verify --against-unity FILE</c> checks at every checkpoint.</para>
    /// </summary>
    public sealed class SmokeRun : MonoBehaviour
    {
        private const int TicksPerFrame = 200;
        private GodBootstrap _boot;
        private long _endTick;
        private bool _quit = true;
        private string _recordPath;
        private ReplayWriter _replay;
        private long _checkpointEvery = SimCalendar.DefaultTicksPerDay;
        private readonly SmokeVerdict _verdict = new SmokeVerdict();
        private readonly List<SimEvent> _events = new List<SimEvent>(256);

        /// <summary>The verdict once finished: null while running.</summary>
        public bool? Ok => _verdict.Ok;
        /// <summary>The last standings line and the end line (for tests).</summary>
        public string LastLine => _verdict.LastLine;
        public int Wars => _verdict.Wars;
        public int Captures => _verdict.Captures;
        /// <summary>Trade offers sent and accepted so far (P11).</summary>
        public int TradesOffered => _verdict.Offered;
        public int TradesAccepted => _verdict.Accepted;
        /// <summary>The last "SMOKE HASH" line.</summary>
        public string LastHashLine { get; private set; }

        public static void StartIfAsked(GodBootstrap boot)
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-godsmoke");
            if (i < 0) return;
            long ticks = 40000;
            if (i + 1 < args.Length && long.TryParse(args[i + 1], out var n)) ticks = n;
            var run = Begin(boot, ticks, true);
            int r = System.Array.IndexOf(args, "-godrecord");
            if (r >= 0 && r + 1 < args.Length) run._recordPath = args[r + 1];
        }

        /// <summary>Starts a smoke on a loaded game (the PlayMode test calls this; the player uses <c>-godsmoke</c>). <paramref name="quit"/>: quit the application with the verdict.</summary>
        public static SmokeRun Begin(GodBootstrap boot, long ticks, bool quit)
        {
            var run = boot.gameObject.AddComponent<SmokeRun>();
            run._boot = boot;
            run._endTick = ticks;
            run._quit = quit;
            return run;
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Exception) _verdict.CountException();
        }

        private void Start()
        {
            _boot.Running = false; // the smoke steps the sim itself; the AI thinks inside each step
            _boot.DrivenExternally = true; // and the bootstrap never steps it (no held steps between frames)
            var s = _boot.Sim.State;
            _checkpointEvery = s.Setup.TicksPerDay;
            Debug.Log("SMOKE start seed " + _boot.Seed + " ticks " + _endTick + " factions " + s.FactionCount
                + " brains " + (AiHost.Director != null && AiHost.Director.Sim == _boot.Sim ? AiHost.Director.Brains.Count : 0)
                + (s.Setup.HasHuman ? " human " + FactionInfo.Name(s.Setup.HumanFaction) : " watch"));
            if (_recordPath != null) OpenReplay(s);
        }

        /// <summary>
        /// The replay of this smoke: the same resolved scenario the runner's <c>watch6</c> gives (six factions, watch, stage 3
        /// victory, this length), so the runner can rebuild the game and check every checkpoint.
        /// </summary>
        private void OpenReplay(GodState s)
        {
            try
            {
                var sc = Scenario.Watch6(_endTick);
                sc.FactionCount = s.FactionCount; sc.Human = s.Setup.Human; sc.CheckpointEvery = _checkpointEvery;
                _replay = new ReplayWriter(_recordPath);
                _replay.Header(new ReplayHeader { Host = "unity", Scenario = sc.ToJson(), Seed = s.Seed, CheckpointEvery = _checkpointEvery });
                Debug.Log("SMOKE recording the replay to " + _recordPath);
            }
            catch (System.Exception e) { Debug.LogWarning("SMOKE cannot write the replay: " + e.Message); _replay = null; }
        }

        private void Update()
        {
            if (Ok.HasValue) return;
            var sim = _boot.Sim; var s = sim.State;
            for (int k = 0; k < TicksPerFrame && s.Tick < _endTick && !s.IsGameOver; k++)
            {
                sim.Step();
                _events.Clear();
                sim.DrainInto(_events);
                _verdict.OnEvents(_events);
                _verdict.AfterTick(s, out var info, out var warning);
                if (warning != null) Debug.LogWarning(warning);
                if (info != null) Debug.Log(info);
                if (s.Tick % _checkpointEvery == 0) Checkpoint(sim);
            }
            if (s.Tick >= _endTick || s.IsGameOver) Finish(sim);
        }

        private void Checkpoint(GodSim sim)
        {
            var parts = sim.StateHashParts();
            ulong h = GodSim.Fold(parts);
            Debug.Log(LastHashLine = "SMOKE HASH " + sim.State.Tick + " " + Hex.Of(h));
            _replay?.Checkpoint(sim.State.Tick, h, parts);
        }

        private void Finish(GodSim sim)
        {
            var s = sim.State;
            var over = _verdict.Finish(s, out var warning);
            if (warning != null) Debug.LogWarning(warning);
            Debug.Log(over);
            Debug.Log(LastHashLine = "SMOKE HASH END " + s.Tick + " " + Hex.Of(sim.StateHash()));
            Debug.Log(_verdict.LastLine);
            if (_replay != null) { _replay.End(s.Tick, sim.StateHash(), s.IsGameOver ? "gameOver" : "maxTicks"); _replay.Dispose(); _replay = null; }
            enabled = false;
            if (_quit) Application.Quit(_verdict.Ok == true ? 0 : 1);
        }

        private void OnDestroy() { _replay?.Dispose(); _replay = null; }
    }
}
