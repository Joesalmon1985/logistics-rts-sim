using System;
using System.Collections.Generic;
using System.Globalization;

namespace God.Recording
{
    /// <summary>
    /// How much a run records (DATA_WAREHOUSE_DESIGN 4.1). M0 has Off, Metrics, Summary and Full; Experiment (a preset
    /// between Metrics and Summary) arrives with the SQLite warehouse (M13). Recording never changes the sim: every level
    /// gives the same hashes.
    /// </summary>
    public enum RecordingLevel
    {
        /// <summary><c>run.json</c> and the replay only.</summary>
        Off = 0,
        /// <summary>In-memory aggregates: <c>metrics.jsonl</c> (one line a sim day) and <c>summary.json</c>.</summary>
        Metrics = 1,
        /// <summary>Metrics, plus the low-volume events in full and the high-volume ones (fights, dice, production) as a
        /// deterministic 1-in-<c>sampleEvery</c> sample, plus the daily snapshots, as JSONL.</summary>
        Summary = 3,
        /// <summary>Every event and every snapshot, as JSONL (<c>events.jsonl.gz</c>, <c>snapshots.jsonl.gz</c>).</summary>
        Full = 4,
    }

    public static class RecordingLevels
    {
        public static bool TryParse(string s, out RecordingLevel level)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "off": level = RecordingLevel.Off; return true;
                case "metrics": level = RecordingLevel.Metrics; return true;
                case "summary": level = RecordingLevel.Summary; return true;
                case "full": level = RecordingLevel.Full; return true;
                default: level = RecordingLevel.Off; return false;
            }
        }

        public static string Name(RecordingLevel l) => l.ToString().ToLowerInvariant();
    }

    /// <summary>What every recorder is told about the run it records.</summary>
    public sealed class RunInfo
    {
        public string RunId;
        public ulong Seed;
        public string ScenarioName;
        public RecordingLevel Level;
        /// <summary>Per-domain overrides of the level (lower-case domain names).</summary>
        public Dictionary<string, RecordingLevel> Domains = new Dictionary<string, RecordingLevel>();
        /// <summary>Summary keeps one high-volume event in this many (by a hash of seed, tick and seq; no Rng).</summary>
        public int SampleEvery = 16;
        /// <summary>The directory the recorder writes into (null: write nothing).</summary>
        public string Dir;
    }

    /// <summary>
    /// A consumer of the sim's event stream and snapshots. Read-only: it never changes state, never draws from an Rng and
    /// never submits commands. The host calls it after every step with the drained events, and after every day end with
    /// the snapshot rows built by <c>God.Sim.SnapshotBuilder</c>.
    /// </summary>
    public interface IRunRecorder : IDisposable
    {
        void Begin(RunInfo info, God.Sim.GodSim sim);
        void OnTick(God.Sim.GodSim sim, IReadOnlyList<God.Sim.SimEvent> events);
        void OnDayEnd(God.Sim.GodSim sim, God.Sim.FactionDayRow[] rows);
        /// <summary>Flushes and closes outputs; adds its counters (bytes written, rows) to <paramref name="counters"/>.</summary>
        void End(God.Sim.GodSim sim, IDictionary<string, long> counters);
    }

    /// <summary>Small shared helpers.</summary>
    public static class Hex
    {
        public static string Of(ulong v) => v.ToString("x16", CultureInfo.InvariantCulture);

        public static bool TryParse(string s, out ulong v) =>
            ulong.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v);

        /// <summary>FNV-1a over a string (UTF-16 code units), as a 64-bit value.</summary>
        public static ulong Fnv(string s, ulong h = 14695981039346656037UL)
        {
            unchecked
            {
                foreach (char c in s ?? "") { h ^= (byte)c; h *= 1099511628211UL; h ^= (byte)(c >> 8); h *= 1099511628211UL; }
            }
            return h;
        }

        public static ulong Fnv(ulong v, ulong h)
        {
            unchecked { for (int i = 0; i < 8; i++) { h ^= (v >> (8 * i)) & 0xFF; h *= 1099511628211UL; } }
            return h;
        }

        /// <summary>The deterministic sampling key of an event (DATA_WAREHOUSE 4.1): no Rng, so it never touches sim streams.</summary>
        public static ulong SampleKey(ulong seed, long tick, int seq)
        {
            ulong h = Fnv(seed, 14695981039346656037UL);
            h = Fnv((ulong)tick, h);
            h = Fnv((ulong)seq, h);
            // final avalanche (SplitMix64 finaliser)
            h = (h ^ (h >> 30)) * 0xBF58476D1CE4E5B9UL;
            h = (h ^ (h >> 27)) * 0x94D049BB133111EBUL;
            return h ^ (h >> 31);
        }
    }
}
