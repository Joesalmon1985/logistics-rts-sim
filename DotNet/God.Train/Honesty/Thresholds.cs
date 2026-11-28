using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace God.Train.Honesty
{
    /// <summary>One measure of the thresholds file (T1-T7) or one stage criterion: read from the file, never a constant in code.</summary>
    public sealed class MeasureSpec
    {
        public string Id = "";
        public string Kind = "";
        public string Op = ">=";
        public double Value;
        public double MinAboveUntrained;
        public int Count, Of, Population, Days;
        public bool HasCount => Of > 0;

        public bool Passes(double v) => Op == "<=" ? v <= Value + 1e-12 : v >= Value - 1e-12;
        public bool PassesCount(int c) => Op == "<=" ? c <= Count : c >= Count;
    }

    public sealed class StageSpec
    {
        public string Id = "";
        public int NeedMask, Seeds, Population, Days;
        public List<MeasureSpec> Criteria = new List<MeasureSpec>();
    }

    /// <summary>
    /// The predeclared thresholds file (<c>docs/unification/TRAINING_THRESHOLDS.md</c>): its hash (SHA-256 of the UTF-8 bytes with
    /// CR LF converted to LF) and the machine-readable block between the <c>thresholds-json-begin</c>/<c>-end</c> markers.
    /// </summary>
    public sealed class Thresholds
    {
        public const string RelativePath = "docs/unification/TRAINING_THRESHOLDS.md";

        public string Path = "";
        public string Hash = "";
        public int Version;
        public long[] TrainingSeeds, ValidationSeeds, HeldOutSeeds, Full72Seeds, SelfTestSeeds;
        public long RefuseSeedsFrom;
        public List<string> HeldOutLayouts = new List<string>();
        public List<string> Arms = new List<string>();
        public ulong UntrainedInitSeed;
        public int PainBandDefault;
        public List<string> Gate = new List<string>();
        public List<MeasureSpec> Measures = new List<MeasureSpec>();
        public List<StageSpec> Stages = new List<StageSpec>();

        public MeasureSpec Measure(string id) { foreach (var m in Measures) if (m.Id == id) return m; throw new KeyNotFoundException(id); }
        public StageSpec Stage(string id) { foreach (var s in Stages) if (s.Id == id) return s; throw new KeyNotFoundException("stage " + id); }

        /// <summary>The hash rule of the file's section 7.</summary>
        public static string HashOf(byte[] fileBytes)
        {
            var norm = new List<byte>(fileBytes.Length);
            for (int i = 0; i < fileBytes.Length; i++)
            {
                if (fileBytes[i] == (byte)'\r' && i + 1 < fileBytes.Length && fileBytes[i + 1] == (byte)'\n') continue;
                norm.Add(fileBytes[i]);
            }
            using var sha = SHA256.Create();
            return Hex(sha.ComputeHash(norm.ToArray()));
        }

        public static string Hex(byte[] b)
        {
            var sb = new StringBuilder(b.Length * 2);
            foreach (var x in b) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>The file: <paramref name="path"/>, else found by walking up from the working and the binary directory.</summary>
        public static string Locate(string path = null)
        {
            if (!string.IsNullOrEmpty(path)) return System.IO.Path.GetFullPath(path);
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var d = new DirectoryInfo(start);
                while (d != null)
                {
                    var f = System.IO.Path.Combine(d.FullName, RelativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                    if (File.Exists(f)) return f;
                    d = d.Parent;
                }
            }
            throw new FileNotFoundException("thresholds file not found: " + RelativePath);
        }

        public static Thresholds Load(string path = null)
        {
            var p = Locate(path);
            var bytes = File.ReadAllBytes(p);
            var t = Parse(Encoding.UTF8.GetString(bytes));
            t.Path = p;
            t.Hash = HashOf(bytes);
            return t;
        }

        public static Thresholds Parse(string text)
        {
            const string begin = "<!-- thresholds-json-begin -->", end = "<!-- thresholds-json-end -->";
            int b = text.IndexOf(begin, StringComparison.Ordinal), e = text.IndexOf(end, StringComparison.Ordinal);
            if (b < 0 || e < b) throw new FormatException("thresholds file: machine-readable block missing");
            var block = text.Substring(b + begin.Length, e - b - begin.Length);
            int jb = block.IndexOf('{'), je = block.LastIndexOf('}');
            using var doc = JsonDocument.Parse(block.Substring(jb, je - jb + 1));
            var r = doc.RootElement;
            var t = new Thresholds
            {
                Version = r.GetProperty("version").GetInt32(),
                TrainingSeeds = Range(r, "trainingSeeds"), ValidationSeeds = Range(r, "validationSeeds"), HeldOutSeeds = Range(r, "heldOutSeeds"),
                Full72Seeds = Range(r, "full72Seeds"), SelfTestSeeds = Range(r, "selfTestSeeds"),
                RefuseSeedsFrom = r.GetProperty("refuseSeedsFrom").GetInt64(),
                UntrainedInitSeed = (ulong)r.GetProperty("untrainedInitSeed").GetInt64(),
                PainBandDefault = r.GetProperty("painBandDefault").GetInt32(),
            };
            foreach (var x in r.GetProperty("heldOutLayouts").EnumerateArray()) t.HeldOutLayouts.Add(x.GetString());
            foreach (var x in r.GetProperty("arms").EnumerateArray()) t.Arms.Add(x.GetString());
            foreach (var x in r.GetProperty("gate").EnumerateArray()) t.Gate.Add(x.GetString());
            foreach (var x in r.GetProperty("measures").EnumerateArray()) t.Measures.Add(ParseMeasure(x));
            foreach (var x in r.GetProperty("stages").EnumerateArray())
            {
                var s = new StageSpec
                {
                    Id = x.GetProperty("id").GetString(), NeedMask = x.GetProperty("needMask").GetInt32(), Seeds = x.GetProperty("seeds").GetInt32(),
                    Population = x.GetProperty("population").GetInt32(), Days = x.GetProperty("days").GetInt32(),
                };
                foreach (var c in x.GetProperty("criteria").EnumerateArray()) s.Criteria.Add(ParseMeasure(c));
                t.Stages.Add(s);
            }
            return t;
        }

        public static MeasureSpec ParseMeasure(JsonElement x)
        {
            var m = new MeasureSpec();
            if (x.TryGetProperty("id", out var v)) m.Id = v.GetString();
            m.Kind = x.GetProperty("kind").GetString();
            m.Op = x.GetProperty("op").GetString();
            if (x.TryGetProperty("value", out v)) m.Value = v.GetDouble();
            if (x.TryGetProperty("minAboveUntrained", out v)) m.MinAboveUntrained = v.GetDouble();
            if (x.TryGetProperty("count", out v)) m.Count = v.GetInt32();
            if (x.TryGetProperty("of", out v)) m.Of = v.GetInt32();
            if (x.TryGetProperty("population", out v)) m.Population = v.GetInt32();
            if (x.TryGetProperty("days", out v)) m.Days = v.GetInt32();
            return m;
        }

        private static long[] Range(JsonElement r, string name)
        {
            var a = r.GetProperty(name);
            return new[] { a[0].GetInt64(), a[1].GetInt64() };
        }
    }

    /// <summary>Raised when an evaluation is asked to run without the committed thresholds hash.</summary>
    public sealed class ThresholdsHashMismatchException : Exception
    {
        public ThresholdsHashMismatchException(string given, string actual)
            : base($"eval refused: --thresholds-hash '{given}' does not equal the committed thresholds file's hash {actual}") { }
    }
}
