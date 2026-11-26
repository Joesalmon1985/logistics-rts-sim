using System;
using System.Collections.Generic;
using System.Globalization;

namespace God.Runner
{
    /// <summary>Thrown for bad command-line arguments (exit code 2).</summary>
    public sealed class UsageException : Exception
    {
        public UsageException(string message) : base(message) { }
    }

    /// <summary>
    /// The command line after the command word: <c>--name value</c> options, <c>--flag</c> switches (from a known list),
    /// repeatable <c>--set</c>, and positional arguments. Every option must be consumed; leftovers are usage errors.
    /// </summary>
    public sealed class Args
    {
        private readonly Dictionary<string, List<string>> _opts = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly HashSet<string> _flags = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _used = new HashSet<string>(StringComparer.Ordinal);
        public readonly List<string> Positional = new List<string>();

        public Args(IList<string> argv, int start, ICollection<string> flagNames)
        {
            for (int i = start; i < argv.Count; i++)
            {
                string a = argv[i];
                if (a.StartsWith("--", StringComparison.Ordinal))
                {
                    string name = a.Substring(2);
                    int eq = name.IndexOf('=');
                    if (eq > 0 && name.Substring(0, eq) != "set") { Add(name.Substring(0, eq), name.Substring(eq + 1)); continue; }
                    if (flagNames.Contains(name)) { _flags.Add(name); continue; }
                    if (i + 1 >= argv.Count) throw new UsageException("--" + name + " needs a value");
                    Add(name, argv[++i]);
                }
                else Positional.Add(a);
            }
        }

        private void Add(string name, string value)
        {
            if (!_opts.TryGetValue(name, out var l)) _opts[name] = l = new List<string>();
            l.Add(value);
        }

        public bool Flag(string name) { _used.Add(name); return _flags.Contains(name); }

        public string Str(string name, string def = null)
        {
            _used.Add(name);
            if (!_opts.TryGetValue(name, out var l)) return def;
            if (l.Count > 1) throw new UsageException("--" + name + " given twice");
            return l[0];
        }

        public List<string> All(string name) { _used.Add(name); return _opts.TryGetValue(name, out var l) ? l : new List<string>(); }

        public long Long(string name, long def)
        {
            string s = Str(name);
            if (s == null) return def;
            if (!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v)) throw new UsageException("--" + name + " must be an integer: " + s);
            return v;
        }

        public ulong? ULong(string name)
        {
            string s = Str(name);
            if (s == null) return null;
            if (!ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong v)) throw new UsageException("--" + name + " must be a non-negative integer: " + s);
            return v;
        }

        /// <summary>Throws for any option or flag the command did not read.</summary>
        public void CheckAllUsed(int maxPositional)
        {
            foreach (var k in _opts.Keys) if (!_used.Contains(k)) throw new UsageException("unknown option --" + k);
            foreach (var k in _flags) if (!_used.Contains(k)) throw new UsageException("unknown option --" + k);
            if (Positional.Count > maxPositional) throw new UsageException("unexpected argument '" + Positional[maxPositional] + "'");
        }

        /// <summary>Seeds: <c>1-64</c>, <c>1..8</c>, <c>1,3,5-8</c>.</summary>
        public static List<ulong> ParseSeeds(string s)
        {
            var l = new List<ulong>();
            if (string.IsNullOrWhiteSpace(s)) throw new UsageException("--seeds is empty");
            foreach (var part in s.Split(','))
            {
                string p = part.Trim();
                string[] r = p.Contains("..") ? p.Split(new[] { ".." }, StringSplitOptions.None) : p.Split('-');
                if (r.Length == 1 && ulong.TryParse(r[0], out ulong one)) { l.Add(one); continue; }
                if (r.Length == 2 && ulong.TryParse(r[0], out ulong lo) && ulong.TryParse(r[1], out ulong hi) && lo <= hi && hi - lo < 100000)
                {
                    for (ulong x = lo; x <= hi; x++) l.Add(x);
                    continue;
                }
                throw new UsageException("bad seed list '" + s + "' (use 1-64, 1..8 or 1,3,5-8)");
            }
            return l;
        }
    }
}
