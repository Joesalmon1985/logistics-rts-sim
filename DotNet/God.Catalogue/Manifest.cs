using System;
using System.Collections.Generic;
using System.IO;

namespace God.Catalogue
{
    /// <summary>One row of the manifest's mapping tables 3.1 (objects) and 3.2 (buildings).</summary>
    public sealed class ManifestRow
    {
        public string Id, Name, Stage;
        public int WorkbookRow;
        public string Fix;
        public string[] Cells;
    }

    /// <summary>Parses CATALOGUE_MANIFEST.md (the reviewed mapping): a test fails on any mismatch with catalogue.json.</summary>
    public static class Manifest
    {
        public static List<ManifestRow> Parse(string path, string prefix)
        {
            var list = new List<ManifestRow>();
            foreach (var raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (!line.StartsWith("| " + prefix + "-")) continue;
                var c = line.Trim('|').Split('|');
                for (int i = 0; i < c.Length; i++) c[i] = c[i].Trim();
                if (c.Length != 9 && c.Length != 10) throw new InvalidOperationException("manifest row with " + c.Length + " cells: " + line);
                list.Add(new ManifestRow { Id = c[0], WorkbookRow = int.Parse(c[1]), Name = c[2], Stage = c[7], Fix = c[8], Cells = c });
            }
            return list;
        }

        /// <summary>Replaces the status of every row id in section 3 tables (a Status column appended by UN3a).</summary>
        public static string Normalize(string text) => text.Replace("\r\n", "\n");
    }
}
