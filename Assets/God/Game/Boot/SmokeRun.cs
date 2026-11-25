using System.Linq;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Headless smoke for the built player: <c>-godsmoke N</c> runs N sim ticks as fast as possible with a chain bot for
    /// every faction building its economy, and logs a summary every 1000 ticks. (There is no military AI yet: that is P9.)
    /// It quits with exit code 0 if no exception was logged and every faction built at least 3 kinds at some point,
    /// otherwise 1. Use only with <c>-batchmode -nographics</c>. The view still only reads state; the bots send commands.
    /// </summary>
    public sealed class SmokeRun : MonoBehaviour
    {
        private const int TicksPerFrame = 100;
        private GodBootstrap _boot;
        private long _endTick;
        private int _exceptions;
        private readonly int[] _maxKinds = new int[FactionInfo.Max];

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
            _boot.DebugSample();   // starts a chain bot per faction
            Debug.Log("SMOKE start seed " + _boot.Seed + " ticks " + _endTick + " factions " + _boot.Sim.State.FactionCount);
        }

        private void Update()
        {
            var sim = _boot.Sim; var s = sim.State;
            for (int k = 0; k < TicksPerFrame && s.Tick < _endTick && !s.IsGameOver; k++)
            {
                sim.Step();
                sim.DrainEvents();
                if (s.Tick % 20 == 0) _boot.ThinkChains();
                if (s.Tick % 200 == 0) foreach (var f in s.Setup.Factions) _maxKinds[(int)f] = Mathf.Max(_maxKinds[(int)f], KindsBuilt(s, f));
                if (s.Tick % 1000 == 0) Debug.Log("SMOKE " + Summary(s));
            }
            if (s.Tick >= _endTick || s.IsGameOver) Finish(s);
        }

        private static int KindsBuilt(GodState s, Faction f) =>
            s.Economy.Buildings.Where(b => b.Faction == f && b.Built && !b.IsHq).Select(b => b.Kind).Distinct().Count();

        private static string Summary(GodState s)
        {
            var e = s.Economy;
            var parts = s.Setup.Factions.Select(f => FactionInfo.Name(f) + " " + KindsBuilt(s, f) + " kinds, "
                + e.ProducedLog.Count(o => o.Faction == f) + " units made, " + FactionRules.VictoryPoints(s, f) + " VP");
            return "tick " + s.Tick + " | " + string.Join(" | ", parts) + " | over " + s.IsGameOver;
        }

        private void Finish(GodState s)
        {
            foreach (var f in s.Setup.Factions) _maxKinds[(int)f] = Mathf.Max(_maxKinds[(int)f], KindsBuilt(s, f));
            int worst = s.Setup.Factions.Min(f => _maxKinds[(int)f]);
            var bad = SimInvariants.Check(s);
            bool ok = _exceptions == 0 && worst >= 3 && bad.Count == 0;
            Debug.Log("SMOKE END " + Summary(s) + " | fewest kinds built at once " + worst + " | invariants " + (bad.Count == 0 ? "ok" : string.Join("; ", bad))
                + " | exceptions " + _exceptions + " | " + (ok ? "OK" : "FAIL"));
            enabled = false;
            Application.Quit(ok ? 0 : 1);
        }
    }
}
