using System.Linq;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Headless smoke for the built player: <c>-godsmoke N</c> plays a watch game (six AI brains, see <see cref="AiHost"/>)
    /// as fast as possible for up to N sim ticks, and logs the standings every 1000 ticks: VP, HQs, Citadels and units per
    /// faction, the wars, captures and eliminations so far, and the trades (offered, accepted, refused, envoys killed). It quits with exit code 0 when no exception was logged, the
    /// invariants hold, and either someone won or the game passed the health checks (at least 3 factions expanded, at least
    /// one war, at least one capture); otherwise 1. Use only with <c>-batchmode -nographics</c>. The view still only reads
    /// state; the brains send commands.
    /// </summary>
    public sealed class SmokeRun : MonoBehaviour
    {
        private const int TicksPerFrame = 200;
        private GodBootstrap _boot;
        private long _endTick;
        private bool _quit = true;
        private int _exceptions, _wars, _captures, _eliminations, _invariantFailures, _offered, _accepted, _refused, _killed;
        private readonly int[] _maxHqs = new int[FactionInfo.Max];
        private string _invariantNote = "ok";

        /// <summary>The verdict once finished: null while running.</summary>
        public bool? Ok { get; private set; }
        /// <summary>The last standings line and the end line (for tests).</summary>
        public string LastLine { get; private set; }
        public int Wars => _wars;
        public int Captures => _captures;
        /// <summary>Trade offers sent and accepted so far (P11).</summary>
        public int TradesOffered => _offered;
        public int TradesAccepted => _accepted;

        public static void StartIfAsked(GodBootstrap boot)
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-godsmoke");
            if (i < 0) return;
            long ticks = 40000;
            if (i + 1 < args.Length && long.TryParse(args[i + 1], out var n)) ticks = n;
            Begin(boot, ticks, true);
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
            if (type == LogType.Exception) _exceptions++;
        }

        private void Start()
        {
            _boot.Running = false; // the smoke steps the sim itself; the AI thinks inside each step
            var s = _boot.Sim.State;
            Debug.Log("SMOKE start seed " + _boot.Seed + " ticks " + _endTick + " factions " + s.FactionCount
                + " brains " + (AiHost.Director != null && AiHost.Director.Sim == _boot.Sim ? AiHost.Director.Brains.Count : 0)
                + (s.Setup.HasHuman ? " human " + FactionInfo.Name(s.Setup.HumanFaction) : " watch"));
        }

        private void Update()
        {
            if (Ok.HasValue) return;
            var sim = _boot.Sim; var s = sim.State;
            for (int k = 0; k < TicksPerFrame && s.Tick < _endTick && !s.IsGameOver; k++)
            {
                sim.Step();
                foreach (var ev in sim.DrainEvents())
                {
                    if (ev is WarDeclared) _wars++;
                    else if (ev is HqCaptured) _captures++;
                    else if (ev is FactionEliminated) _eliminations++;
                    else if (ev is TradeOffered) _offered++;
                    else if (ev is TradeAccepted) _accepted++;
                    else if (ev is TradeRefused) _refused++;
                    else if (ev is EnvoyKilled) _killed++;
                }
                if (s.Tick % 200 == 0) foreach (var f in s.Setup.Factions) _maxHqs[(int)f] = Mathf.Max(_maxHqs[(int)f], s.Territory.HqsOf(f).Count);
                if (s.Tick % 500 == 0) CheckInvariants(s);
                if (s.Tick % 1000 == 0) Debug.Log(LastLine = "SMOKE " + Summary(s));
            }
            if (s.Tick >= _endTick || s.IsGameOver) Finish(s);
        }

        private void CheckInvariants(GodState s)
        {
            var bad = SimInvariants.Check(s);
            if (bad.Count == 0) return;
            _invariantFailures++;
            _invariantNote = "tick " + s.Tick + ": " + string.Join("; ", bad);
            Debug.LogWarning("SMOKE invariants " + _invariantNote);
        }

        private string Summary(GodState s)
        {
            var parts = FactionRules.Standings(s).Select(st => FactionInfo.Name(st.Faction) + " " + st.VictoryPoints + " VP " + st.Hqs + " HQ "
                + st.Citadels + " Cit " + s.Military.CountUnits(st.Faction) + " units"
                + (st.Eliminated ? " (out)" : s.Factions.Enemies(st.Faction).Count > 0 ? " war " + string.Join("/", s.Factions.Enemies(st.Faction).Select(FactionInfo.Name)) : ""));
            return "tick " + s.Tick + " min " + (s.Tick / 600.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " | " + string.Join(" | ", parts)
                + " | soldiers made " + s.Economy.UnitsProduced + " harvester upgrades " + s.Economy.Buildings.Count(b => b.Level > 1)
                + " | wars " + _wars + " captures " + _captures + " eliminations " + _eliminations
                + " | trades offered " + _offered + " accepted " + _accepted + " refused " + _refused + " killed " + _killed;
        }

        private void Finish(GodState s)
        {
            foreach (var f in s.Setup.Factions) _maxHqs[(int)f] = Mathf.Max(_maxHqs[(int)f], s.Territory.HqsOf(f).Count);
            CheckInvariants(s);
            int expanded = s.Setup.Factions.Count(f => _maxHqs[(int)f] >= 2);
            bool won = s.IsGameOver && s.Factions.Winner >= 0;
            bool healthy = expanded >= 3 && _wars >= 1 && _captures >= 1;
            bool ok = _exceptions == 0 && _invariantFailures == 0 && (won || healthy);
            string over = s.IsGameOver
                ? "GAME OVER at tick " + s.Factions.GameOverTick + " (minute " + (s.Factions.GameOverTick / 600.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "): "
                  + (s.Factions.Winner >= 0 ? FactionInfo.Name((Faction)s.Factions.Winner) : "nobody") + " wins by " + s.Factions.EndReason
                : "no game over by tick " + s.Tick;
            Debug.Log("SMOKE " + over);
            LastLine = "SMOKE END " + Summary(s) + " | " + over + " | factions expanded " + expanded + " | invariants " + _invariantNote
                + " | exceptions " + _exceptions + " | " + (ok ? "OK" : "FAIL");
            Debug.Log(LastLine);
            Ok = ok;
            enabled = false;
            if (_quit) Application.Quit(ok ? 0 : 1);
        }
    }
}
