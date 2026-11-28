using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace God.Catalogue
{
    /// <summary>The ware enum of Economy/Wares.cs (and the Terrain enum of Board/HexCoord.cs) read as text, so the generator names only existing ids.</summary>
    public sealed class WareTable
    {
        public readonly Dictionary<string, int> Ids = new Dictionary<string, int>();
        public readonly HashSet<string> Unique = new HashSet<string>();
        public readonly Dictionary<string, int> Terrains = new Dictionary<string, int>();

        public static WareTable Load(string waresCsPath, string hexCoordCsPath = null)
        {
            var t = new WareTable();
            string text = File.ReadAllText(waresCsPath);
            int s = text.IndexOf("public enum Ware"); int e = text.IndexOf("}", s);
            foreach (Match m in Regex.Matches(text.Substring(s, e - s), @"^\s*([A-Za-z0-9_]+)\s*=\s*(\d+)\s*,", RegexOptions.Multiline))
                t.Ids[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
            int u = text.IndexOf("public static bool IsUnique"); int ue = text.IndexOf("default:", u);
            foreach (Match m in Regex.Matches(text.Substring(u, ue - u), @"case Ware\.([A-Za-z0-9_]+):")) t.Unique.Add(m.Groups[1].Value);
            if (hexCoordCsPath != null)
            {
                string h = File.ReadAllText(hexCoordCsPath);
                int ts = h.IndexOf("public enum Terrain"); int te = h.IndexOf("}", ts);
                foreach (Match m in Regex.Matches(h.Substring(ts, te - ts), @"^\s*([A-Za-z0-9_]+)\s*=\s*(\d+)\s*,", RegexOptions.Multiline))
                    t.Terrains[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
            }
            return t;
        }

        public bool Has(string name) => Ids.ContainsKey(name);
        public int Id(string name) => Ids[name];
    }
}
