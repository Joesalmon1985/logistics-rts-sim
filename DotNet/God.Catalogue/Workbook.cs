using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace God.Catalogue
{
    /// <summary>The pinned workbook (manifest section 1) read without any dependency: a zip of XML.</summary>
    public sealed class Workbook
    {
        public const string PinnedSha256 = "f764df01de6b98aa5cc61e612b300446cf87366bda971804428544d9455405a0";

        public string Sha256;
        /// <summary>Sheet name to rows (1-based sheet row to the cell text by column letter).</summary>
        public Dictionary<string, SortedDictionary<int, Dictionary<string, string>>> Sheets = new Dictionary<string, SortedDictionary<int, Dictionary<string, string>>>();

        public static string Sha(string path)
        {
            using (var f = File.OpenRead(path)) return Hex(SHA256.HashData(f));
        }

        public static string Hex(byte[] b) { var sb = new StringBuilder(); foreach (var x in b) sb.Append(x.ToString("x2")); return sb.ToString(); }

        public static Workbook Read(string path)
        {
            var wb = new Workbook { Sha256 = Sha(path) };
            using (var zip = ZipFile.OpenRead(path))
            {
                var shared = new List<string>();
                var sst = zip.GetEntry("xl/sharedStrings.xml");
                if (sst != null)
                    using (var r = XmlReader.Create(sst.Open()))
                    {
                        var sb = new StringBuilder(); bool inSi = false, pend = false;
                        while (pend || r.Read())
                        {
                            pend = false;
                            if (r.NodeType == XmlNodeType.Element && r.LocalName == "si") { inSi = true; sb.Clear(); }
                            else if (r.NodeType == XmlNodeType.Element && r.LocalName == "t" && inSi) { sb.Append(r.ReadElementContentAsString()); pend = true; }
                            else if (r.NodeType == XmlNodeType.EndElement && r.LocalName == "si") { inSi = false; shared.Add(sb.ToString()); }
                        }
                    }
                // sheet names in workbook order; the n-th sheet is xl/worksheets/sheet{n}.xml (sheetId order)
                var names = new List<string>();
                using (var r = XmlReader.Create(zip.GetEntry("xl/workbook.xml").Open()))
                    while (r.Read()) if (r.NodeType == XmlNodeType.Element && r.LocalName == "sheet") names.Add(r.GetAttribute("name"));
                for (int i = 0; i < names.Count; i++)
                {
                    var e = zip.GetEntry("xl/worksheets/sheet" + (i + 1) + ".xml");
                    var rows = new SortedDictionary<int, Dictionary<string, string>>();
                    using (var r = XmlReader.Create(e.Open()))
                    {
                        string cell = null, type = null; StringBuilder txt = null;
                        bool pending = false;
                        while (pending || r.Read())
                        {
                            pending = false;
                            if (r.NodeType == XmlNodeType.Element && r.LocalName == "c") { cell = r.GetAttribute("r"); type = r.GetAttribute("t"); txt = new StringBuilder(); if (r.IsEmptyElement) { cell = null; } }
                            else if (r.NodeType == XmlNodeType.Element && (r.LocalName == "v" || r.LocalName == "t") && cell != null)
                            {
                                string s = r.ReadElementContentAsString();
                                txt.Append(type == "s" ? shared[int.Parse(s)] : s);
                                pending = true;
                            }
                            else if (r.NodeType == XmlNodeType.EndElement && r.LocalName == "c" && cell != null)
                            {
                                var m = Regex.Match(cell, "^([A-Z]+)([0-9]+)$");
                                string v = txt.ToString();
                                if (v.Length > 0)
                                {
                                    int row = int.Parse(m.Groups[2].Value);
                                    if (!rows.TryGetValue(row, out var d)) rows[row] = d = new Dictionary<string, string>();
                                    d[m.Groups[1].Value] = v;
                                }
                                cell = null;
                            }
                        }
                    }
                    wb.Sheets[names[i]] = rows;
                }
            }
            return wb;
        }

        public string Cell(string sheet, int row, string col)
        {
            return Sheets[sheet].TryGetValue(row, out var d) && d.TryGetValue(col, out var v) ? v : "";
        }
    }
}
