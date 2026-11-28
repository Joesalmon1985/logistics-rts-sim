using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace God.Catalogue
{
    /// <summary>One step of the computed production order (round 0 = the endowment and the environment).</summary>
    public sealed class Step { public int Round; public string Text; }

    /// <summary>The audits of <c>validate</c> (spec 5.6). Each returns its errors; an empty list is a pass.</summary>
    public static class Audits
    {
        public static readonly Regex Banned = new Regex(@"\b(owner\w*|permission\w*|rights?|recruit\w*|employ\w*|consent\w*|member\w*|membership)\b", RegexOptions.IgnoreCase);

        // ---------- counts, ids, aliases ----------
        public static List<string> Counts(CatalogueDoc d)
        {
            var e = new List<string>();
            if (d.Controls.Count != 12) e.Add("controls " + d.Controls.Count + " != 12");
            if (d.Needs.Count != 12) e.Add("needs " + d.Needs.Count + " != 12");
            if (d.Objects.Count != 138) e.Add("objects " + d.Objects.Count + " != 138");
            if (d.Buildings.Count != 65) e.Add("buildings " + d.Buildings.Count + " != 65");
            Expect(e, "objects UA-initial", d.Objects.Count(o => o.Stage == "UA-initial"), 29);
            Expect(e, "objects UB", d.Objects.Count(o => o.Stage == "UB"), 54);
            Expect(e, "objects Later", d.Objects.Count(o => o.Stage == "Later"), 55);
            Expect(e, "buildings UA-initial", d.Buildings.Count(o => o.Stage == "UA-initial"), 10);
            Expect(e, "buildings UB", d.Buildings.Count(o => o.Stage == "UB"), 13);
            Expect(e, "buildings Later", d.Buildings.Count(o => o.Stage == "Later"), 42);
            foreach (var r in AllRows(d))
            {
                if (string.IsNullOrEmpty(r.Id) || r.WorkbookRow <= 0 || string.IsNullOrEmpty(r.Stage) || string.IsNullOrEmpty(r.Status) || string.IsNullOrEmpty(r.Reason)) e.Add(r.Id + ": missing id, row, stage, status or reason");
                if ((r is ObjectRow || r is BuildingRow) && r.CatId == 0) e.Add(r.Id + ": no catId");
                if (r.WorkbookSha256 != d.WorkbookSha256) e.Add(r.Id + ": workbook sha differs");
                var okStatus = new[] { "implemented", "staged", "deferred", "rejected" };
                if (!okStatus.Contains(r.Status)) e.Add(r.Id + ": status " + r.Status);
                var okStage = new[] { "UA-initial", "UB", "Later", "Superseded" };
                if (!okStage.Contains(r.Stage)) e.Add(r.Id + ": stage " + r.Stage);
            }
            return e;
        }

        static void Expect(List<string> e, string what, int got, int want) { if (got != want) e.Add(what + " " + got + " != " + want); }

        public static IEnumerable<RowBase> AllRows(CatalogueDoc d) => d.Controls.Cast<RowBase>().Concat(d.Needs).Concat(d.Objects).Concat(d.Buildings);

        public static List<string> Ids(CatalogueDoc d)
        {
            var e = new List<string>();
            for (int i = 0; i < d.Objects.Count; i++)
                if (d.Objects[i].Id != "OBJ-" + (i + 1).ToString("D3") || d.Objects[i].CatId != 1001 + i) e.Add("object position " + (i + 1) + " is " + d.Objects[i].Id + "/" + d.Objects[i].CatId);
            for (int i = 0; i < d.Buildings.Count; i++)
                if (d.Buildings[i].Id != "BLD-" + (i + 1).ToString("D3") || d.Buildings[i].CatId != 2001 + i) e.Add("building position " + (i + 1) + " is " + d.Buildings[i].Id + "/" + d.Buildings[i].CatId);
            // objects and buildings are separate namespaces (OBJ-064 Pottery and BLD-056 Pottery); inside one, a name or alias resolves to exactly one id
            foreach (var rows in new[] { d.Objects.Cast<RowBase>().ToList(), d.Buildings.Cast<RowBase>().ToList() })
            {
                var seen = new Dictionary<string, string>();
                foreach (var r in rows)
                    foreach (var n in new[] { r.Name }.Concat(r.Aliases))
                    {
                        string k = n.ToLowerInvariant();
                        if (seen.TryGetValue(k, out var other) && other != r.Id) e.Add("name or alias '" + n + "' of " + r.Id + " also resolves to " + other);
                        seen[k] = r.Id;
                    }
            }
            return e;
        }

        // ---------- effects, targets, slots ----------
        static IEnumerable<(string Owner, InteractionData Row)> AllInteractions(CatalogueDoc d)
        {
            foreach (var o in d.Objects) if (o.Def != null) foreach (var i in o.Def.Interactions) yield return (o.Id, i);
            foreach (var b in d.Buildings) if (b.Def != null) foreach (var i in b.Def.Interactions) yield return (b.Id, i);
        }

        public static List<string> Effects(CatalogueDoc d, WareTable wares)
        {
            var e = new List<string>();
            foreach (var (owner, i) in AllInteractions(d))
            {
                string at = owner + " " + i.Name;
                if (Array.IndexOf(Spec.TargetKinds, i.Target) <= 0) e.Add(at + ": target kind " + i.Target);
                if (Array.IndexOf(Spec.SourceReqs, i.Source) < 0) e.Add(at + ": source requirement " + i.Source);
                if (i.Slot < 0 || i.Slot > 3) e.Add(at + ": slot " + i.Slot);
                if (i.Reach < 0 || i.Reach > 255) e.Add(at + ": reach " + i.Reach);
                if (i.Ticks < 0) e.Add(at + ": ticks " + i.Ticks);
                if (i.Effects.Count == 0) e.Add(at + ": no effect");
                bool recipe = i.Effects.Any(x => x.Kind == "RecipeWork");
                if (i.Ticks == 0 && !recipe) e.Add(at + ": zero ticks outside RecipeWork");
                if (i.TargetCat != null && Spec.CatOf(d, i.TargetCat) == 0) e.Add(at + ": target cat " + i.TargetCat);
                if (i.Source == "Ware")
                {
                    string w = Generator.Str(i.SourceArg);
                    if (w == null || !wares.Has(w)) e.Add(at + ": source ware " + w);
                }
                else if (i.Source == "Tool" || i.Source == "Class")
                {
                    string c = Generator.Str(i.SourceArg);
                    var row = d.Objects.FirstOrDefault(o => o.Id == c);
                    if (row == null || row.Def == null) e.Add(at + ": source tool " + c + " has no definition");
                    else if (i.Source == "Tool" && row.Def.ToolMultipliers.Count == 0 && row.Def.StrikeAttack == 0) e.Add(at + ": " + c + " is not a tool");
                }
                else if (i.SourceArg != null && i.Source != "Any") e.Add(at + ": a source argument without a ware or tool source");
                foreach (var f in i.Effects)
                {
                    int k = Spec.KindId(f.Kind);
                    if (k < 1 || k > 20) { e.Add(at + ": effect kind " + f.Kind + " outside 1-20"); continue; }
                    var spec = Spec.ArgTypes(f.Kind);
                    var args = new[] { f.A, f.B, f.C };
                    for (int a = 0; a < 3; a++)
                        if (!Spec.Resolve(args[a], spec[a], d, wares, out _, out string err)) e.Add(at + ": " + f.Kind + " arg " + "ABC"[a] + ": " + err);
                }
            }
            // slots distinct per (target, source): rows that could match the same USE must differ in slot
            foreach (var group in AllInteractions(d).GroupBy(x => x.Row.Target + "/" + x.Row.TargetCat))
            {
                var rows = group.ToList();
                for (int a = 0; a < rows.Count; a++)
                    for (int b = a + 1; b < rows.Count; b++)
                    {
                        var x = rows[a].Row; var y = rows[b].Row;
                        if (x.Slot != y.Slot) continue;
                        bool same = x.Source == y.Source && Generator.Str(x.SourceArg) == Generator.Str(y.SourceArg);
                        bool anyOverlap = (x.Source == "Any" && y.Source != "EmptyHand") || (y.Source == "Any" && x.Source != "EmptyHand");
                        if (same || anyOverlap) e.Add(rows[a].Owner + " " + x.Name + " and " + rows[b].Owner + " " + y.Name + ": same slot " + x.Slot + " on target " + group.Key + " with overlapping sources");
                    }
            }
            // definition data
            foreach (var o in d.Objects.Where(o => o.Def != null))
            {
                var df = o.Def;
                if (!wares.Has(df.Ware)) e.Add(o.Id + ": ware " + df.Ware);
                if (df.Weight <= 0 || df.Weight > 255) e.Add(o.Id + ": weight " + df.Weight);
                if (df.Nutrition < 0 || df.Nutrition > 1000) e.Add(o.Id + ": nutrition");
                if (df.StrikeAttack < 0 || df.Defence < 0) e.Add(o.Id + ": strike or defence");
                foreach (var t in df.ToolMultipliers)
                {
                    if (Spec.KindId(t.Effect) < 1) e.Add(o.Id + ": tool effect " + t.Effect);
                    if (t.Permille <= 0 || t.Permille > 1000) e.Add(o.Id + ": tool multiplier " + t.Permille);
                }
                if (df.Interactions.Count == 0 && df.Role != "input") e.Add(o.Id + ": no interaction and not an input-only object");
            }
            foreach (var b in d.Buildings.Where(b => b.Def != null))
            {
                var df = b.Def;
                if (Array.IndexOf(Spec.PlacementRules, df.Placement) < 0) e.Add(b.Id + ": placement " + df.Placement);
                if (df.PlacementArg != null && !wares.Terrains.ContainsKey(df.PlacementArg)) e.Add(b.Id + ": terrain " + df.PlacementArg);
                foreach (var c in df.Capabilities) if (Array.IndexOf(Spec.Capabilities, c) < 0) e.Add(b.Id + ": capability " + c);
                if (df.Levels.Count == 0) e.Add(b.Id + ": no level");
                for (int i = 0; i < df.Levels.Count; i++)
                {
                    var l = df.Levels[i];
                    if (l.Level != i + 1) e.Add(b.Id + ": level numbering");
                    if (l.Storage < 0 || l.SleepSpots < 0 || l.SleepSpots > 255 || l.FixtureSlots < 0 || l.FixtureSlots > 255 || l.CollectRadius < 0 || l.CollectRadius > 255 || l.Labour < 0) e.Add(b.Id + ": level " + l.Level + " out of range");
                    foreach (var m in l.Materials) if (!wares.Has(m.Ware) || m.Count <= 0 || m.Count > short.MaxValue) e.Add(b.Id + ": material " + m.Ware);
                    if (l.CollectWare != null && !wares.Has(l.CollectWare)) e.Add(b.Id + ": collect ware " + l.CollectWare);
                }
                foreach (var r in df.Recipes)
                {
                    if (r.Id <= 0 || r.Id > ushort.MaxValue) e.Add(b.Id + ": recipe id " + r.Id);
                    foreach (var q in r.Inputs.Concat(r.Outputs)) if (!wares.Has(q.Ware) || q.Count <= 0) e.Add(b.Id + ": recipe " + r.Name + " ware " + q.Ware);
                    if (r.LabourTicks <= 0) e.Add(b.Id + ": recipe " + r.Name + " labour");
                    if (r.Outputs.Count == 0) e.Add(b.Id + ": recipe " + r.Name + " has no output");
                }
            }
            var recipeIds = d.Buildings.Where(b => b.Def != null).SelectMany(b => b.Def.Recipes.Select(r => r.Id)).ToList();
            foreach (var g in recipeIds.GroupBy(x => x).Where(g => g.Count() > 1)) e.Add("recipe id " + g.Key + " is used twice");
            return e;
        }

        // ---------- the UA closure ----------
        static Dictionary<string, RowBase> WareOwners(CatalogueDoc d)
        {
            var m = new Dictionary<string, RowBase>();
            foreach (var o in d.Objects.Where(o => o.Def != null)) if (!m.ContainsKey(o.Def.Ware)) m[o.Def.Ware] = o;
            return m;
        }

        static bool IsUaOrEndowment(RowBase r) => r.Stage == "UA-initial" || (r is ObjectRow o && o.Def != null && o.Def.EndowmentOnly);

        public static List<string> Closure(CatalogueDoc d)
        {
            var e = new List<string>();
            var owners = WareOwners(d);
            void Check(string where, string ware)
            {
                if (!owners.TryGetValue(ware, out var row)) e.Add(where + ": input " + ware + " is no catalogue object");
                else if (!IsUaOrEndowment(row)) e.Add(where + ": input " + ware + " belongs to " + row.Id + " (" + row.Stage + ")");
            }
            foreach (var b in Rows.ActiveBuildings(d))
            {
                foreach (var l in b.Def.Levels) foreach (var m in l.Materials) Check(b.Id + " L" + l.Level, m.Ware);
                foreach (var r in b.Def.Recipes)
                {
                    foreach (var q in r.Inputs) Check(b.Id + " recipe " + r.Name, q.Ware);
                    foreach (var q in r.Outputs) Check(b.Id + " recipe " + r.Name + " output", q.Ware);
                }
            }
            return e;
        }

        // ---------- cycles and bootstrap ----------
        static readonly HashSet<string> RootRoles = new HashSet<string> { "raw", "forage", "water", "remedy", "endowment" };

        /// <summary>The set of wares producible from the roots (raws, forage, water, herbs, the endowment) through recipes and by-products.</summary>
        static HashSet<string> Producible(CatalogueDoc d)
        {
            var have = new HashSet<string>(d.Objects.Where(o => o.Def != null && o.Def.Role != null && RootRoles.Contains(o.Def.Role)).Select(o => o.Def.Ware));
            var recipes = Rows.ActiveBuildings(d).SelectMany(b => b.Def.Recipes).ToList();
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (var r in recipes)
                    if (r.Inputs.All(q => have.Contains(q.Ware))) foreach (var q in r.Outputs) grew |= have.Add(q.Ware);
                foreach (var o in d.Objects.Where(o => o.Def != null && o.Def.ByproductOf != null))
                {
                    var parent = d.Objects.First(p => p.Id == o.Def.ByproductOf);
                    if (have.Contains(parent.Def.Ware)) grew |= have.Add(o.Def.Ware);
                }
            }
            return have;
        }

        public static List<string> Cycles(CatalogueDoc d, out List<string> cyclesFound)
        {
            var e = new List<string>();
            cyclesFound = new List<string>();
            var recipes = Rows.ActiveBuildings(d).SelectMany(b => b.Def.Recipes.Select(r => (Station: b.Id, Recipe: r))).ToList();
            var edges = new Dictionary<string, HashSet<string>>();
            foreach (var (_, r) in recipes)
                foreach (var i in r.Inputs)
                    foreach (var o in r.Outputs)
                    {
                        if (!edges.TryGetValue(i.Ware, out var s)) edges[i.Ware] = s = new HashSet<string>();
                        s.Add(o.Ware);
                    }
            // Tarjan strongly connected components over wares
            var nodes = edges.Keys.Concat(edges.Values.SelectMany(x => x)).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            var index = new Dictionary<string, int>(); var low = new Dictionary<string, int>(); var onStack = new HashSet<string>(); var stack = new Stack<string>(); int idx = 0;
            var sccs = new List<List<string>>();
            void Strong(string v)
            {
                index[v] = low[v] = idx++; stack.Push(v); onStack.Add(v);
                if (edges.TryGetValue(v, out var next))
                    foreach (var w in next.OrderBy(x => x, StringComparer.Ordinal))
                    {
                        if (!index.ContainsKey(w)) { Strong(w); low[v] = Math.Min(low[v], low[w]); }
                        else if (onStack.Contains(w)) low[v] = Math.Min(low[v], index[w]);
                    }
                if (low[v] == index[v])
                {
                    var c = new List<string>(); string w;
                    do { w = stack.Pop(); onStack.Remove(w); c.Add(w); } while (w != v);
                    sccs.Add(c);
                }
            }
            foreach (var v in nodes) if (!index.ContainsKey(v)) Strong(v);
            var producible = Producible(d);
            foreach (var c in sccs)
            {
                bool cyclic = c.Count > 1 || (edges.TryGetValue(c[0], out var self) && self.Contains(c[0]));
                if (!cyclic) continue;
                c.Sort(StringComparer.Ordinal);
                cyclesFound.Add(string.Join(" -> ", c));
                foreach (var w in c) if (!producible.Contains(w)) e.Add("recipe cycle {" + string.Join(", ", c) + "} has no bootstrap path: " + w + " is not producible from the roots");
            }
            foreach (var b in d.Buildings.Where(b => b.Name.EndsWith("Mine", StringComparison.Ordinal)))
                if (b.Stage != "Later" || !b.Reason.Contains("no bootstrap path")) e.Add(b.Id + " " + b.Name + ": a mine must be Later and say 'no bootstrap path'");
            foreach (var o in d.Objects.Where(o => o.Workbook.TryGetValue("type", out var t) && t == "Mineral"))
                if (o.Stage != "Later" || !o.Reason.Contains("no bootstrap path")) e.Add(o.Id + " " + o.Name + ": a deposit must be Later and say 'no bootstrap path'");
            return e;
        }

        // ---------- reachability ----------
        public static List<string> Reachability(CatalogueDoc d, out List<Step> order)
        {
            var e = new List<string>();
            order = new List<Step>();
            var have = new HashSet<string>();
            var built = new Dictionary<string, int>();   // building id to highest level built
            var objByWare = WareOwners(d);
            int round = 0;
            // round 0: the founder endowment and the environment (forage, water, herbs)
            foreach (var b in Rows.ActiveBuildings(d).Where(b => b.Def.Founder)) { built[b.Id] = 1; order.Add(new Step { Round = 0, Text = "endowment: " + b.Id + " " + b.Name + " (founder building)" }); }
            foreach (var o in Rows.ActiveObjects(d).Where(o => o.Def.Role == "endowment")) { have.Add(o.Def.Ware); order.Add(new Step { Round = 0, Text = "endowment: " + o.Id + " " + o.Name }); }
            foreach (var o in Rows.ActiveObjects(d).Where(o => o.Def.Role == "forage" || o.Def.Role == "water" || o.Def.Role == "remedy"))
            { have.Add(o.Def.Ware); order.Add(new Step { Round = 0, Text = "environment: " + o.Id + " " + o.Name + (o.Def.Role == "water" ? " (fresh-water point)" : " (forage place)") }); }
            var buildings = Rows.ActiveBuildings(d).Where(b => !b.Def.Founder).ToList();
            for (round = 1; round < 40; round++)
            {
                var add = new List<Action>(); var texts = new List<string>();
                foreach (var b in buildings)
                {
                    int have0 = built.TryGetValue(b.Id, out var lv) ? lv : 0;
                    foreach (var l in b.Def.Levels.Where(l => l.Level == have0 + 1))
                        if (l.Materials.All(m => have.Contains(m.Ware)))
                        {
                            var bid = b.Id; int level = l.Level; var cw = l.CollectWare;
                            texts.Add("build " + b.Id + " " + b.Name + " level " + l.Level + (l.Materials.Count == 0 ? " (labour only)" : " (" + string.Join(" + ", l.Materials.Select(m => m.Count + " " + m.Ware)) + ")"));
                            add.Add(() => built[bid] = level);
                            if (cw != null && !have.Contains(cw) && level == 1) { texts.Add("collect " + cw + " at " + b.Id + " (" + objByWare[cw].Id + " " + objByWare[cw].Name + ")"); add.Add(() => have.Add(cw)); }
                        }
                }
                foreach (var b in buildings.Concat(Rows.ActiveBuildings(d).Where(x => x.Def.Founder)))
                {
                    if (!built.ContainsKey(b.Id)) continue;
                    foreach (var r in b.Def.Recipes)
                        if (r.Inputs.All(q => have.Contains(q.Ware)) && r.Outputs.Any(q => !have.Contains(q.Ware)))
                        {
                            var outs = r.Outputs.Select(q => q.Ware).ToList();
                            texts.Add("craft " + r.Name + " at " + b.Id + " (" + string.Join(" + ", r.Inputs.Select(q => q.Count + " " + q.Ware)) + ")");
                            add.Add(() => { foreach (var w in outs) have.Add(w); });
                        }
                }
                foreach (var o in Rows.ActiveObjects(d).Where(o => o.Def.ByproductOf != null && !have.Contains(o.Def.Ware)))
                {
                    var parent = d.Objects.First(p => p.Id == o.Def.ByproductOf);
                    if (have.Contains(parent.Def.Ware)) { var w = o.Def.Ware; texts.Add("by-product " + o.Id + " " + o.Name + " (of " + parent.Id + " " + parent.Name + ")"); add.Add(() => have.Add(w)); }
                }
                if (add.Count == 0) break;
                foreach (var t in texts.Distinct()) order.Add(new Step { Round = round, Text = t });
                foreach (var a in add) a();
            }
            foreach (var o in Rows.ActiveObjects(d)) if (!have.Contains(o.Def.Ware)) e.Add(o.Id + " " + o.Name + " is not reachable from labour-only L1 shelters, forage, dice raws and the endowment");
            foreach (var b in buildings)
            {
                int top = b.Def.Levels.Count;
                if (!built.TryGetValue(b.Id, out var lv) || lv < top) e.Add(b.Id + " " + b.Name + " level " + (lv + 1) + " is not reachable");
            }
            return e;
        }

        // ---------- no ownership wording and fix codes ----------
        public static List<string> Wording(CatalogueDoc d)
        {
            var e = new List<string>();
            foreach (var r in d.Objects.Cast<RowBase>().Concat(d.Buildings))
            {
                bool active = r is ObjectRow o ? Rows.IsActive(o, o.Def) : Rows.IsActive(r, ((BuildingRow)r).Def);
                if (!active) continue;
                // the workbook's own cell text is kept for traceability only; everything else is active text and field names
                string json = JsonSerializer.Serialize(r is ObjectRow oo ? (object)new { oo.Id, oo.Name, oo.Aliases, oo.Stage, oo.Fix, oo.FixApplied, oo.Corrections, oo.Status, oo.Reason, oo.Def }
                                                                         : new { r.Id, r.Name, r.Aliases, r.Stage, r.Fix, r.FixApplied, r.Corrections, r.Status, r.Reason, ((BuildingRow)r).Def }, Store.Options);
                foreach (Match m in Banned.Matches(json)) e.Add(r.Id + ": active row contains '" + m.Value + "'");
                foreach (var f in r.Fix) if (!r.FixApplied.Contains(f)) e.Add(r.Id + ": fix code " + f + " is not marked applied");
                if (r.Fix.Count != r.Corrections.Count) e.Add(r.Id + ": " + r.Fix.Count + " fix codes but " + r.Corrections.Count + " corrections recorded");
            }
            // a UA row must never have an unresolved fix
            foreach (var r in d.Objects.Cast<RowBase>().Concat(d.Buildings).Where(r => r.Stage == "UA-initial"))
                if (r.Status != "implemented") e.Add(r.Id + ": UA-initial but " + r.Status);
            return e;
        }

        // ---------- needs ----------
        public static Dictionary<int, List<string>> NeedCoverage(CatalogueDoc d, WareTable wares)
        {
            var cov = new Dictionary<int, List<string>>();
            for (int i = 0; i < 12; i++) cov[i] = new List<string>();
            void Scan(string id, IEnumerable<InteractionData> rows)
            {
                foreach (var r in rows) foreach (var f in r.Effects) foreach (var ch in Spec.ReliefChannels(f, d, wares)) if (!cov[ch].Contains(id)) cov[ch].Add(id);
            }
            foreach (var o in Rows.ActiveObjects(d)) Scan(o.Id, o.Def.Interactions);
            foreach (var b in Rows.ActiveBuildings(d)) Scan(b.Id, b.Def.Interactions);
            return cov;
        }

        public static List<string> Needs(CatalogueDoc d, WareTable wares)
        {
            var e = new List<string>();
            var cov = NeedCoverage(d, wares);
            foreach (var n in d.Needs)
                if (cov[n.Channel].Count == 0 && string.IsNullOrEmpty(n.ReliefSource)) e.Add("need " + n.Name + ": no UA relief row and no named non-catalogue source");
            return e;
        }

        // ---------- the join with the manifest ----------
        public static List<string> ManifestJoin(CatalogueDoc d, string manifestPath)
        {
            var e = new List<string>();
            var om = Manifest.Parse(manifestPath, "OBJ"); var bm = Manifest.Parse(manifestPath, "BLD");
            if (om.Count != d.Objects.Count) e.Add("manifest has " + om.Count + " objects, catalogue " + d.Objects.Count);
            if (bm.Count != d.Buildings.Count) e.Add("manifest has " + bm.Count + " buildings, catalogue " + d.Buildings.Count);
            void Join(List<ManifestRow> m, IList<RowBase> rows)
            {
                for (int i = 0; i < Math.Min(m.Count, rows.Count); i++)
                {
                    var a = m[i]; var r = rows[i];
                    if (a.Id != r.Id) e.Add(a.Id + ": id " + r.Id);
                    if (a.Name != r.Name) e.Add(a.Id + ": name '" + a.Name + "' vs '" + r.Name + "'");
                    if (a.WorkbookRow != r.WorkbookRow) e.Add(a.Id + ": workbook row " + a.WorkbookRow + " vs " + r.WorkbookRow);
                    if (a.Stage != r.Stage) e.Add(a.Id + ": stage " + a.Stage + " vs " + r.Stage);
                    if (a.Fix != string.Concat(r.Fix)) e.Add(a.Id + ": fix '" + a.Fix + "' vs '" + string.Concat(r.Fix) + "'");
                }
            }
            Join(om, d.Objects.Cast<RowBase>().ToList());
            Join(bm, d.Buildings.Cast<RowBase>().ToList());
            return e;
        }

        /// <summary>UB and Later objects whose workbook inputs name reed, seeds or deposits: the unresolved bootstrap dependencies.</summary>
        public static List<string> UnresolvedDependencies(CatalogueDoc d)
        {
            var l = new List<string>();
            foreach (var r in d.Objects.Cast<RowBase>().Concat(d.Buildings).Where(r => r.Stage != "UA-initial"))
            {
                string src = r is ObjectRow ? r.Workbook["source"] : r.Workbook["cost"];
                var miss = Importer.MissingInputs(src);
                var keys = miss.Where(m => m == "reed" || m == "wheat seed" || m == "seed plants" || m.EndsWith("deposit")).ToList();
                if (r.Workbook.TryGetValue("type", out var t) && t == "Mineral") keys.Add("finite deposit");
                if (r.Name.EndsWith("Mine", StringComparison.Ordinal)) keys.Add("finite deposit");
                if (keys.Count > 0) l.Add(r.Id + " " + r.Name + " (" + r.Stage + "): " + string.Join(", ", keys.Distinct()));
            }
            return l;
        }
    }
}
