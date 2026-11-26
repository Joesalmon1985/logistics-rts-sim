using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using God.Sim;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace God.Recording
{
    /// <summary>Thrown when a replay file cannot be read (runner exit code 3).</summary>
    public sealed class ReplayException : Exception
    {
        public ReplayException(string message) : base(message) { }
        public ReplayException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Line 1 of a <c>.godr</c> file (HEADLESS_SIMULATION 3.4).</summary>
    public sealed class ReplayHeader
    {
        public const string FormatName = "god-replay";
        public const int CurrentVersion = 1;
        public const string SimVersion = "4.0.0-m0";

        public int Version = CurrentVersion;
        public string GitCommit = "unknown";
        /// <summary>"runner" or "unity".</summary>
        public string Host = "runner";
        /// <summary>The resolved scenario (all defaults filled in).</summary>
        public JObject Scenario;
        public ulong Seed;
        /// <summary>"new", or "save" with the save's SHA-256 and file name.</summary>
        public string StartKind = "new";
        public string StartSha256;
        public string StartFile;
        public long CheckpointEvery = 1200;
        public string[] HashParts = GodSim.AllHashPartNames.ToArray();

        public JObject ToJson()
        {
            var start = new JObject { ["kind"] = StartKind };
            if (StartKind == "save") { start["sha256"] = StartSha256; start["file"] = StartFile; }
            return new JObject
            {
                ["format"] = FormatName, ["version"] = Version, ["simVersion"] = SimVersion, ["saveVersion"] = SaveFile.CurrentVersion,
                ["gitCommit"] = GitCommit, ["host"] = Host, ["scenario"] = Scenario ?? new JObject(), ["seed"] = Seed,
                ["start"] = start, ["checkpointEvery"] = CheckpointEvery, ["hashParts"] = new JArray(HashParts),
            };
        }

        public static ReplayHeader FromJson(JObject o)
        {
            if (o == null || (string)o["format"] != FormatName) throw new ReplayException("Not a god-replay file.");
            int v = (int?)o["version"] ?? 0;
            if (v != CurrentVersion) throw new ReplayException("Unsupported replay version " + v + ".");
            int sv = (int?)o["saveVersion"] ?? 0;
            if (sv != SaveFile.CurrentVersion) throw new ReplayException("The replay was written for save version " + sv + "; this build has " + SaveFile.CurrentVersion + ".");
            var h = new ReplayHeader
            {
                GitCommit = (string)o["gitCommit"] ?? "unknown", Host = (string)o["host"] ?? "runner",
                Scenario = o["scenario"] as JObject, Seed = (ulong?)o["seed"] ?? 0, CheckpointEvery = (long?)o["checkpointEvery"] ?? 1200,
            };
            var start = o["start"] as JObject;
            h.StartKind = (string)start?["kind"] ?? "new";
            h.StartSha256 = (string)start?["sha256"]; h.StartFile = (string)start?["file"];
            if (o["hashParts"] is JArray parts) { var l = new List<string>(); foreach (var p in parts) l.Add((string)p); h.HashParts = l.ToArray(); }
            if (h.Scenario == null) throw new ReplayException("The replay header has no scenario.");
            if (h.CheckpointEvery <= 0) throw new ReplayException("Bad checkpointEvery in the replay header.");
            return h;
        }
    }

    /// <summary>The kinds of replay line. (1 was M0's "hold" line for a paused game's held tick, removed at M1: a paused game now steps ordinary ticks.)</summary>
    public enum ReplayEntryKind { Command = 0, Checkpoint = 2, End = 3 }

    /// <summary>One line after the header: an external command, a checkpoint or the end record.</summary>
    public sealed class ReplayEntry
    {
        public ReplayEntryKind Kind;
        /// <summary>The tick the command applies at (submitted before the step that reaches it), the checkpoint tick or the end tick.</summary>
        public long Tick;
        public string CommandType;
        public string CommandJson;
        public string Origin;
        public ulong Hash;
        public ulong[] Parts;
        public string Reason;

        public Command ToCommand()
        {
            var t = typeof(Command).Assembly.GetType(CommandType ?? "", false);
            if (t == null || !typeof(Command).IsAssignableFrom(t) || t.IsAbstract) throw new ReplayException("The replay names an unknown command: " + CommandType);
            try { return (Command)JsonConvert.DeserializeObject(CommandJson, t, SaveFile.Settings); }
            catch (Exception e) { throw new ReplayException("A replay command is damaged at tick " + Tick + ".", e); }
        }
    }

    /// <summary>Writes a <c>.godr</c> file: gzipped JSON lines, header first, then entries in tick order.</summary>
    public sealed class ReplayWriter : IDisposable
    {
        private StreamWriter _w;
        public long Lines { get; private set; }

        public ReplayWriter(string path) : this(File.Create(path)) { }

        public ReplayWriter(Stream stream)
        {
            _w = new StreamWriter(new GZipStream(stream, CompressionLevel.Optimal), new UTF8Encoding(false)) { NewLine = "\n" };
        }

        private void Line(JObject o) { _w.WriteLine(o.ToString(Formatting.None)); Lines++; }

        public void Header(ReplayHeader h) => Line(h.ToJson());

        public void Command(long tick, Command c, string origin)
        {
            Line(new JObject
            {
                ["t"] = tick,
                ["c"] = new JObject { ["type"] = c.GetType().FullName, ["json"] = JsonConvert.SerializeObject(c, SaveFile.Settings) },
                ["o"] = origin ?? "external",
            });
        }

        public void Checkpoint(long tick, ulong hash, ulong[] parts)
        {
            var p = new JArray(); foreach (var x in parts) p.Add(Hex.Of(x));
            Line(new JObject { ["t"] = tick, ["h"] = Hex.Of(hash), ["p"] = p });
        }

        public void End(long tick, ulong hash, string reason) => Line(new JObject { ["end"] = tick, ["h"] = Hex.Of(hash), ["reason"] = reason ?? "" });

        public void Dispose() { _w?.Dispose(); _w = null; }
    }

    /// <summary>A whole replay read into memory.</summary>
    public sealed class ReplayFile
    {
        public ReplayHeader Header;
        public readonly List<ReplayEntry> Entries = new List<ReplayEntry>();

        public IEnumerable<ReplayEntry> Of(ReplayEntryKind k) { foreach (var e in Entries) if (e.Kind == k) yield return e; }

        public static ReplayFile Read(string path)
        {
            byte[] bytes;
            try { bytes = File.ReadAllBytes(path); }
            catch (Exception e) { throw new ReplayException("Cannot read the replay " + path + ": " + e.Message, e); }
            return Read(bytes);
        }

        public static ReplayFile Read(byte[] bytes)
        {
            var f = new ReplayFile();
            try
            {
                Stream s = new MemoryStream(bytes);
                if (bytes.Length >= 2 && bytes[0] == 0x1f && bytes[1] == 0x8b) s = new GZipStream(s, CompressionMode.Decompress);
                using (var r = new StreamReader(s, Encoding.UTF8))
                {
                    string line; int n = 0; long lastTick = 0;
                    while ((line = r.ReadLine()) != null)
                    {
                        if (line.Length == 0) continue;
                        var o = JObject.Parse(line);
                        if (n++ == 0) { f.Header = ReplayHeader.FromJson(o); continue; }
                        var e = Entry(o);
                        if (e.Tick < lastTick) throw new ReplayException("Replay lines are out of tick order at tick " + e.Tick + ".");
                        lastTick = e.Tick;
                        f.Entries.Add(e);
                    }
                }
            }
            catch (ReplayException) { throw; }
            catch (Exception e) { throw new ReplayException("The replay is damaged: " + e.Message, e); }
            if (f.Header == null) throw new ReplayException("The replay is empty.");
            return f;
        }

        private static ReplayEntry Entry(JObject o)
        {
            if (o["end"] != null)
                return new ReplayEntry { Kind = ReplayEntryKind.End, Tick = (long)o["end"], Hash = HexOf(o["h"]), Reason = (string)o["reason"] };
            long t = (long?)o["t"] ?? throw new ReplayException("A replay line has no tick.");
            if (o["c"] is JObject c)
                return new ReplayEntry { Kind = ReplayEntryKind.Command, Tick = t, CommandType = (string)c["type"], CommandJson = (string)c["json"], Origin = (string)o["o"] };
            if (o["hold"] != null)
                throw new ReplayException("The replay has a 'hold' line (tick " + t + "): held ticks were removed at M1 (a paused game now steps ordinary ticks). Record the game again.");
            if (o["h"] != null)
            {
                var parts = new List<ulong>();
                if (o["p"] is JArray p) foreach (var x in p) parts.Add(HexOf(x));
                return new ReplayEntry { Kind = ReplayEntryKind.Checkpoint, Tick = t, Hash = HexOf(o["h"]), Parts = parts.ToArray() };
            }
            throw new ReplayException("Unknown replay line: " + o.ToString(Formatting.None));
        }

        private static ulong HexOf(JToken t)
        {
            if (!Hex.TryParse((string)t, out ulong v)) throw new ReplayException("Bad hash in the replay: " + t);
            return v;
        }
    }
}
