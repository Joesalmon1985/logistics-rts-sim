using System.Linq;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Headless smoke for the built player: <c>-godsmoke N</c> runs N sim ticks as fast as possible with the chain bot
    /// building the economy. It marks every enemy building as a target once units exist and logs a summary every
    /// 1000 ticks. It quits with exit code 0 if no exception was logged and the chain built at least 3 kinds at some point, otherwise 1. Use only
    /// with <c>-batchmode -nographics</c>. The view still only reads state; the smoke sends commands like a player.
    /// </summary>
    public sealed class SmokeRun : MonoBehaviour
    {
        private const int TicksPerFrame = 100;
        private GodBootstrap _boot;
        private long _endTick;
        private int _exceptions;
        private bool _marked;
        private int _maxKinds;

        public static void StartIfAsked(GodBootstrap boot)
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-godsmoke");
            if (i < 0) return;
            long ticks = 6000;
            if (i + 1 < args.Length && long.TryParse(args[i + 1], out var n)) ticks = n;
            var run = boot.gameObject.AddComponent<SmokeRun>();
            run._boot = boot;
            run._endTick = ticks;
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Exception) _exceptions++;
        }

        private void Start()
        {
            _boot.Running = false; // the smoke steps the sim itself
            _boot.DebugSample();   // starts the chain bot
            Debug.Log("SMOKE start seed " + _boot.Seed + " ticks " + _endTick);
        }

        private void Update()
        {
            var sim = _boot.Sim; var s = sim.State;
            for (int k = 0; k < TicksPerFrame && s.Tick < _endTick && !s.Military.IsGameOver; k++)
            {
                sim.Step();
                sim.DrainEvents();
                if (s.Tick % 20 == 0) _boot.Chain?.Think();
                if (!_marked && s.Military.Units.Any(u => u.Faction == Faction.Player))
                {
                    _marked = true;
                    foreach (var b in s.Territory.Buildings.Where(b => b.Faction == Faction.Enemy).ToList())
                        sim.Submit(new MarkTarget(b.Id));
                }
                if (s.Tick % 200 == 0) _maxKinds = Mathf.Max(_maxKinds, KindsBuilt(s));
                if (s.Tick % 1000 == 0) Debug.Log("SMOKE " + Summary(s));
            }
            if (s.Tick >= _endTick || s.Military.IsGameOver) Finish(s);
        }

        private static int KindsBuilt(GodState s) =>
            s.Economy.Buildings.Where(b => b.Faction == Faction.Player && b.Built && !b.IsHq).Select(b => b.Kind).Distinct().Count();

        private static string Summary(GodState s)
        {
            var e = s.Economy; var m = s.Military;
            var built = e.Buildings.Where(b => b.Faction == Faction.Player && b.Built && !b.IsHq).Select(b => b.Kind).Distinct().OrderBy(k => k);
            return "tick " + s.Tick
                + " | player kinds built: " + string.Join(",", built)
                + " | units made " + e.ProducedLog.Count(o => o.Faction == Faction.Player)
                + " | player units alive " + m.Units.Count(u => u.Faction == Faction.Player)
                + " | enemy units alive " + m.Units.Count(u => u.Faction == Faction.Enemy)
                + " | wave " + m.WaveNumber
                + " | enemy buildings " + s.Territory.Buildings.Count(b => b.Faction == Faction.Enemy)
                + " | outcome " + m.Outcome;
        }

        private void Finish(GodState s)
        {
            _maxKinds = Mathf.Max(_maxKinds, KindsBuilt(s));
            bool ok = _exceptions == 0 && _maxKinds >= 3;
            Debug.Log("SMOKE END " + Summary(s) + " | most kinds built at once " + _maxKinds + " | exceptions " + _exceptions + " | " + (ok ? "OK" : "FAIL"));
            enabled = false;
            Application.Quit(ok ? 0 : 1);
        }
    }
}
