using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace God.Catalogue
{
    /// <summary>The repository paths the tool reads and writes (the root is found by walking up to the workbook).</summary>
    public sealed class Paths
    {
        public string Root;
        public string Workbook => Path.Combine(Root, "docs/unification/God_of_the_Island_Core_Design.xlsx");
        public string Manifest => Path.Combine(Root, "docs/unification/CATALOGUE_MANIFEST.md");
        public string Json => Path.Combine(Root, "docs/unification/catalogue/catalogue.json");
        public string Golden => Path.Combine(Root, "docs/unification/catalogue/ids.golden.tsv");
        public string WaresCs => Path.Combine(Root, "Assets/God/Sim/Economy/Wares.cs");
        public string HexCoordCs => Path.Combine(Root, "Assets/God/Sim/Board/HexCoord.cs");

        public static Paths Find(string start = null)
        {
            string dir = start ?? Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "docs/unification/God_of_the_Island_Core_Design.xlsx"))) return new Paths { Root = dir };
                dir = Path.GetDirectoryName(dir);
            }
            throw new InvalidOperationException("repository root not found (docs/unification/God_of_the_Island_Core_Design.xlsx)");
        }

        public WareTable Wares() => WareTable.Load(WaresCs, HexCoordCs);
        public CatalogueDoc Load() => Store.Load(Json);
    }

    public sealed class ValidationResult
    {
        public List<string> Errors = new List<string>();
        public List<string> Summary = new List<string>();
        public List<Step> Order = new List<Step>();
        public string Hash;
        public bool Ok => Errors.Count == 0;
    }

    public static class Program
    {
        public static int Main(string[] args)
        {
            try { return Run(args, Console.Out, Console.Error); }
            catch (Exception ex) { Console.Error.WriteLine("error: " + ex.Message); return 2; }
        }

        public static int Run(string[] args, TextWriter o, TextWriter err)
        {
            string cmd = args.Length > 0 ? args[0] : "";
            string rootArg = null; bool force = false;
            for (int i = 1; i < args.Length; i++) { if (args[i] == "--root") rootArg = args[++i]; else if (args[i] == "--force") force = true; }
            var p = Paths.Find(rootArg);
            switch (cmd)
            {
                case "import-workbook":
                {
                    if (File.Exists(p.Json) && !force) { err.WriteLine("catalogue.json exists: it is the reviewed source and is not overwritten without --force"); return 1; }
                    var doc = Importer.Import(p.Workbook, p.Manifest, p.WaresCs);
                    Store.WriteText(p.Json, Store.ToText(doc));
                    Store.WriteText(p.Golden, Importer.GoldenIds(doc));
                    o.WriteLine("imported " + doc.Controls.Count + " controls, " + doc.Needs.Count + " needs, " + doc.Objects.Count + " objects, " + doc.Buildings.Count + " buildings; workbook sha " + doc.WorkbookSha256);
                    return 0;
                }
                case "generate":
                {
                    var doc = p.Load(); var wares = p.Wares();
                    foreach (var kv in Generator.Generate(doc, wares)) { Store.WriteText(Path.Combine(p.Root, kv.Key), kv.Value); o.WriteLine("wrote " + kv.Key); }
                    o.WriteLine("CatalogueHash " + Generator.CatalogueHash(doc));
                    return 0;
                }
                case "report":
                {
                    var doc = p.Load();
                    Store.WriteText(Path.Combine(p.Root, Report.Path), Report.Make(doc, p.Wares()));
                    o.WriteLine("wrote " + Report.Path);
                    return 0;
                }
                case "manifest-status":
                {
                    ManifestStatus.Update(p.Manifest, p.Load());
                    o.WriteLine("updated the status columns of " + p.Manifest);
                    return 0;
                }
                case "validate":
                {
                    var r = Validate(p);
                    foreach (var s in r.Summary) o.WriteLine(s);
                    o.WriteLine("production order (" + r.Order.Count + " steps):");
                    foreach (var s in r.Order) o.WriteLine("  round " + s.Round + ": " + s.Text);
                    o.WriteLine("CatalogueHash " + r.Hash);
                    foreach (var e in r.Errors) err.WriteLine("FAIL " + e);
                    o.WriteLine(r.Ok ? "validate: OK" : "validate: " + r.Errors.Count + " failure(s)");
                    return r.Ok ? 0 : 1;
                }
                default:
                    err.WriteLine("usage: God.Catalogue import-workbook|generate|validate|report|manifest-status [--root <repo>] [--force]");
                    return 64;
            }
        }

        public static ValidationResult Validate(Paths p)
        {
            var doc = p.Load(); var wares = p.Wares();
            var r = new ValidationResult();
            r.Errors.AddRange(Audits.Counts(doc));
            if (doc.WorkbookSha256 != Workbook.PinnedSha256) r.Errors.Add("catalogue.json records a different workbook SHA-256");
            string sha = Workbook.Sha(p.Workbook);
            if (sha != Workbook.PinnedSha256) r.Errors.Add("workbook SHA-256 " + sha + " differs from the pinned one: the manifest must be re-reviewed");
            r.Errors.AddRange(Audits.ManifestJoin(doc, p.Manifest));
            r.Errors.AddRange(Audits.Ids(doc));
            r.Errors.AddRange(Audits.Effects(doc, wares));
            r.Errors.AddRange(Audits.Wording(doc));
            r.Errors.AddRange(Audits.Closure(doc));
            r.Errors.AddRange(Audits.Cycles(doc, out var cycles));
            r.Errors.AddRange(Audits.Reachability(doc, out var order));
            r.Errors.AddRange(Audits.Needs(doc, wares));
            r.Order = order;
            r.Hash = Generator.CatalogueHash(doc);
            r.Summary.Add("rows: " + doc.Controls.Count + " controls, " + doc.Needs.Count + " needs, " + doc.Objects.Count + " objects, " + doc.Buildings.Count + " buildings; workbook sha " + sha);
            r.Summary.Add("objects: UA " + doc.Objects.Count(x => x.Stage == "UA-initial") + ", UB " + doc.Objects.Count(x => x.Stage == "UB") + ", Later " + doc.Objects.Count(x => x.Stage == "Later")
                          + "; buildings: UA " + doc.Buildings.Count(x => x.Stage == "UA-initial") + ", UB " + doc.Buildings.Count(x => x.Stage == "UB") + ", Later " + doc.Buildings.Count(x => x.Stage == "Later"));
            r.Summary.Add("active definitions: " + Rows.ActiveObjects(doc).Count() + " objects, " + Rows.ActiveBuildings(doc).Count() + " buildings, "
                          + Rows.ActiveBuildings(doc).Sum(b => b.Def.Recipes.Count) + " recipes, " + (Rows.ActiveObjects(doc).Sum(x => x.Def.Interactions.Count) + Rows.ActiveBuildings(doc).Sum(b => b.Def.Interactions.Count)) + " interaction rows");
            r.Summary.Add("audits: counts, manifest join, ids and aliases, effects (kinds 1-20, slots 0-3, args), no ownership wording, UA closure, cycles (" + cycles.Count + " found), reachability (" + order.Count + " steps), need relief");
            return r;
        }
    }

    /// <summary>Appends a Status column to the manifest's mapping tables 3.1 and 3.2 (idempotent; line endings preserved).</summary>
    public static class ManifestStatus
    {
        public static void Update(string path, CatalogueDoc doc)
        {
            string text = File.ReadAllText(path, Encoding.UTF8);
            string nl = text.Contains("\r\n") ? "\r\n" : "\n";
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var status = doc.Objects.Cast<RowBase>().Concat(doc.Buildings).ToDictionary(r => r.Id, r => r.Status);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("| ID | WB row | Name | Type |") || line.StartsWith("| ID | WB row | Building | Cost |"))
                {
                    if (!line.Contains("| Status |")) { lines[i] = line + " Status |"; lines[i + 1] = lines[i + 1] + "---|"; }
                    continue;
                }
                if (!(line.StartsWith("| OBJ-") || line.StartsWith("| BLD-"))) continue;
                var cells = line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToList();
                if (cells.Count != 9 && cells.Count != 10) continue;
                string st = status[cells[0]];
                string head = "| " + string.Join(" | ", cells.Take(9)) + " |";
                if (cells[8].Length == 0) head = "| " + string.Join(" | ", cells.Take(8)) + " |  |";
                lines[i] = head + " " + st + " |";
            }
            File.WriteAllText(path, string.Join(nl, lines), new UTF8Encoding(false));
        }
    }
}
