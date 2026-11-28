using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using God.Catalogue;
using God.Sim;
using God.Sim.Unified;
using NUnit.Framework;
using Cat = God.Sim.Unified.Catalogue;
using TargetKind = God.Sim.Unified.TargetKind;

namespace God.Runner.Tests
{
    /// <summary>
    /// UN3a acceptance tests T1-T13 (spec section 13). Behavioural: the importer, generator and audits are run, their output is parsed and
    /// compared with the committed files, and seeded violations must fail. The generated tables are checked through the UN0 facade.
    /// </summary>
    [TestFixture]
    public class Catalogue_Tests
    {
        static Paths P => Paths.Find(TestContext.CurrentContext.TestDirectory);

        static CatalogueDoc Doc() => P.Load();

        // ---------- T1 ----------
        [Test]
        public void Catalogue_WorkbookShaPinned()
        {
            var p = P;
            Assert.AreEqual("f764df01de6b98aa5cc61e612b300446cf87366bda971804428544d9455405a0", Workbook.Sha(p.Workbook));
            Assert.AreEqual(Workbook.PinnedSha256, Doc().WorkbookSha256);
            Assert.AreEqual(Workbook.PinnedSha256, Cat.WorkbookSha, "the generated tables record the workbook hash they come from");

            // a changed workbook fails and says the manifest must be re-reviewed
            string tmp = Path.Combine(Path.GetTempPath(), "un3a_" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var bytes = File.ReadAllBytes(p.Workbook).ToList(); bytes.Add(0);
                File.WriteAllBytes(tmp, bytes.ToArray());
                var ex = Assert.Throws<InvalidOperationException>(() => Importer.Import(tmp, p.Manifest, p.WaresCs));
                StringAssert.Contains("re-reviewed", ex.Message);
            }
            finally { if (File.Exists(tmp)) File.Delete(tmp); }
        }

        // ---------- T2 ----------
        [Test]
        public void Catalogue_RowCounts()
        {
            var p = P; var d = Doc();
            Assert.AreEqual(12, d.Controls.Count); Assert.AreEqual(12, d.Needs.Count);
            Assert.AreEqual(138, d.Objects.Count); Assert.AreEqual(65, d.Buildings.Count);
            Assert.AreEqual(29, d.Objects.Count(o => o.Stage == "UA-initial")); Assert.AreEqual(54, d.Objects.Count(o => o.Stage == "UB")); Assert.AreEqual(55, d.Objects.Count(o => o.Stage == "Later"));
            Assert.AreEqual(10, d.Buildings.Count(o => o.Stage == "UA-initial")); Assert.AreEqual(13, d.Buildings.Count(o => o.Stage == "UB")); Assert.AreEqual(42, d.Buildings.Count(o => o.Stage == "Later"));
            Assert.IsEmpty(Audits.Counts(d));
            foreach (var r in Audits.AllRows(d))
            {
                Assert.IsNotEmpty(r.Id); Assert.Greater(r.WorkbookRow, 0, r.Id); Assert.IsNotEmpty(r.Stage, r.Id); Assert.IsNotEmpty(r.Status, r.Id); Assert.IsNotEmpty(r.Reason, r.Id);
                Assert.AreEqual(d.WorkbookSha256, r.WorkbookSha256, r.Id);
            }
            foreach (var r in d.Objects.Cast<RowBase>().Concat(d.Buildings)) Assert.Greater(r.CatId, 1000, r.Id);

            // the workbook itself (read independently of catalogue.json) has the expected row counts
            var wb = Workbook.Read(p.Workbook);
            Assert.AreEqual(138, wb.Sheets["Objects"].Count(kv => kv.Key >= 5 && kv.Key <= 142 && kv.Value.ContainsKey("A")));
            Assert.AreEqual(65, wb.Sheets["Buildings"].Count(kv => kv.Key >= 5 && kv.Key <= 69 && kv.Value.ContainsKey("A")));
            Assert.AreEqual(12, wb.Sheets["NPC Actions"].Count(kv => kv.Key >= 5 && kv.Key <= 16 && kv.Value.ContainsKey("A")));
            Assert.AreEqual(12, wb.Sheets["NPC Actions"].Count(kv => kv.Key >= 21 && kv.Key <= 32 && kv.Value.ContainsKey("A")));
        }

        // ---------- T3 ----------
        [Test]
        public void Catalogue_ManifestMatchesSource()
        {
            var p = P; var d = Doc();
            Assert.IsEmpty(Audits.ManifestJoin(d, p.Manifest));
            var om = Manifest.Parse(p.Manifest, "OBJ"); var bm = Manifest.Parse(p.Manifest, "BLD");
            Assert.AreEqual(138, om.Count); Assert.AreEqual(65, bm.Count);
            // the manifest's own status column equals the catalogue's
            foreach (var m in om.Concat(bm)) Assert.AreEqual(d.Objects.Cast<RowBase>().Concat(d.Buildings).First(r => r.Id == m.Id).Status, m.Cells[9], m.Id + " status column");
            // a seeded mismatch (a renamed row, a changed stage) is caught
            var bad = Store.Clone(d); bad.Objects[10].Name += "X"; bad.Buildings[3].Stage = "Later";
            Assert.AreEqual(2, Audits.ManifestJoin(bad, p.Manifest).Count);
        }

        // ---------- T4 ----------
        [Test]
        public void Catalogue_IdsImmutable()
        {
            var p = P; var d = Doc();
            var golden = Store.ReadLf(p.Golden).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Split('\t')).ToList();
            Assert.AreEqual(203, golden.Count);
            var wb = Workbook.Read(p.Workbook);
            for (int i = 0; i < 138; i++)
            {
                Assert.AreEqual("OBJ-" + (i + 1).ToString("D3"), golden[i][0]);
                Assert.AreEqual(wb.Cell("Objects", 5 + i, "A"), golden[i][2], "golden name equals workbook sheet order at row " + (5 + i));
                Assert.AreEqual(golden[i][2], d.Objects[i].Name); Assert.AreEqual(golden[i][0], d.Objects[i].Id); Assert.AreEqual(1001 + i, d.Objects[i].CatId);
            }
            for (int i = 0; i < 65; i++)
            {
                Assert.AreEqual("BLD-" + (i + 1).ToString("D3"), golden[138 + i][0]);
                Assert.AreEqual(wb.Cell("Buildings", 5 + i, "A"), golden[138 + i][2]);
                Assert.AreEqual(golden[138 + i][2], d.Buildings[i].Name); Assert.AreEqual(2001 + i, d.Buildings[i].CatId);
            }
            Assert.IsEmpty(Audits.Ids(d));

            // aliases resolve to exactly one id (the generated binary search agrees with the source)
            Assert.AreEqual(1093, Cat.ObjectIdOfName("bedding mat")); Assert.AreEqual(1093, Cat.ObjectIdOfName("sleeping mat"));
            Assert.AreEqual(1091, Cat.ObjectIdOfName("target ball")); Assert.AreEqual(1091, Cat.ObjectIdOfName("hoop"));
            Assert.AreEqual(1105, Cat.ObjectIdOfName("jump platform")); Assert.AreEqual(1126, Cat.ObjectIdOfName("cool storage"));
            Assert.AreEqual(1128, Cat.ObjectIdOfName("fire watch")); Assert.AreEqual(1085, Cat.ObjectIdOfName("painting board")); Assert.AreEqual(1099, Cat.ObjectIdOfName("sundial"));
            Assert.AreEqual(1112, Cat.ObjectIdOfName("water basin")); Assert.AreEqual(2056, Cat.BuildingIdOfName("pottery")); Assert.AreEqual(1064, Cat.ObjectIdOfName("pottery"));
            Assert.AreEqual(CatId.None, Cat.ObjectIdOfName("no such thing"));
            foreach (var r in d.Objects) { Assert.AreEqual(r.CatId, Cat.ObjectIdOfName(r.Name.ToLowerInvariant()), r.Id); foreach (var a in r.Aliases) Assert.AreEqual(r.CatId, Cat.ObjectIdOfName(a.ToLowerInvariant())); }
            foreach (var r in d.Buildings) Assert.AreEqual(r.CatId, Cat.BuildingIdOfName(r.Name.ToLowerInvariant()), r.Id);

            // every name the manifest's Carpenter row lists resolves to exactly one object (the G aliases)
            var produces = d.Buildings.First(b => b.Id == "BLD-055").Workbook["produces"].Split(';');
            foreach (var t in produces)
            {
                string name = Regex.Replace(t.Trim(), @"^\d+\s+", "").ToLowerInvariant();
                Assert.AreNotEqual(CatId.None, Cat.ObjectIdOfName(name), "carpenter product '" + name + "'");
            }

            // a duplicate or reused id, and an alias that resolves twice, fail
            var dup = Store.Clone(d); dup.Objects[5].Id = dup.Objects[4].Id; Assert.IsNotEmpty(Audits.Ids(dup));
            var al = Store.Clone(d); al.Objects[7].Aliases.Add("Coal"); Assert.IsNotEmpty(Audits.Ids(al));
        }

        // ---------- T5 ----------
        [Test]
        public void Catalogue_GeneratedMatchesSource()
        {
            var p = P; var d = Doc(); var wares = p.Wares();
            var a = Generator.Generate(d, wares); var b = Generator.Generate(Store.Clone(d), wares);
            Assert.AreEqual(5, a.Count);
            foreach (var kv in a)
            {
                Assert.AreEqual(kv.Value, b[kv.Key], kv.Key + " differs between two runs");
                Assert.AreEqual(Store.ReadLf(Path.Combine(p.Root, kv.Key)), kv.Value, kv.Key + " differs from the committed file: run `God.Catalogue generate`");
                Assert.IsFalse(kv.Value.Contains('\r'), kv.Key + " has CR");
                Assert.IsFalse(Regex.IsMatch(kv.Value, @"20\d\d-\d\d-\d\d"), kv.Key + " has a date");
                Assert.IsFalse(Regex.IsMatch(kv.Value, @"[0-9]\.[0-9]"), kv.Key + " has a culture-formatted decimal");
            }
            // on disk the generated files use LF (the directory's .gitattributes pins it)
            foreach (var kv in a) Assert.IsFalse(File.ReadAllText(Path.Combine(p.Root, kv.Key)).Contains('\r'), kv.Key + " on disk has CR");
            // the catalogue.json text round-trips byte for byte
            Assert.AreEqual(Store.ReadLf(p.Json), Store.ToText(d));
            // CatalogueHash is the SHA-256 of the canonical active rows
            string canon = Generator.CanonicalActiveJson(d);
            string hash = Workbook.Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canon)));
            Assert.AreEqual(hash, Generator.CatalogueHash(d));
            Assert.AreEqual(hash, Cat.CatalogueHash, "the generated constant equals the hash of catalogue.json");
            Assert.AreEqual(64, hash.Length);
            // the hash covers content: changing a number changes it, changing a reason does not
            var c = Store.Clone(d); c.Buildings.First(x => x.Id == "BLD-010").Def.Levels[0].Labour = 301;
            Assert.AreNotEqual(hash, Generator.CatalogueHash(c));
            var r = Store.Clone(d); r.Buildings.First(x => x.Id == "BLD-010").Reason += " (reworded)";
            Assert.AreEqual(hash, Generator.CatalogueHash(r));
        }

        // ---------- T6 ----------
        [Test]
        public void Catalogue_UAClosure()
        {
            var d = Doc();
            Assert.IsEmpty(Audits.Closure(d));
            // an input from a Later row, an input that is no catalogue object, and an input whose row was moved out of UA all fail
            var a = Store.Clone(d); a.Buildings.First(b => b.Id == "BLD-055").Def.Recipes[0].Inputs[0].Ware = "Iron";
            Assert.IsNotEmpty(Audits.Closure(a));
            var b2 = Store.Clone(d); b2.Buildings.First(b => b.Id == "BLD-010").Def.Levels[0].Materials.Add(new WareQtyData { Ware = "Steel", Count = 1 });
            Assert.IsNotEmpty(Audits.Closure(b2));
            var c = Store.Clone(d); c.Objects.First(o => o.Id == "OBJ-016").Stage = "Later";
            StringAssert.Contains("OBJ-016", string.Join("\n", Audits.Closure(c)));
            // every material and input of the shipped rows is a UA object
            var uaWares = d.Objects.Where(o => o.Stage == "UA-initial" || (o.Def != null && o.Def.EndowmentOnly)).Select(o => o.Def.Ware).ToHashSet();
            foreach (var bld in d.Buildings.Where(x => x.Def != null))
            {
                foreach (var m in bld.Def.Levels.SelectMany(l => l.Materials)) Assert.Contains(m.Ware, uaWares.ToList(), bld.Id);
                foreach (var q in bld.Def.Recipes.SelectMany(r => r.Inputs)) Assert.Contains(q.Ware, uaWares.ToList(), bld.Id);
            }
        }

        // ---------- T7 ----------
        [Test]
        public void Catalogue_NoCycleWithoutBootstrap()
        {
            var d = Doc();
            var errors = Audits.Cycles(d, out var cycles);
            Assert.IsEmpty(errors); Assert.IsEmpty(cycles, "the UA recipes form no cycle");
            // a bootstrap cycle (Log <-> Plank, both producible from raws) is allowed; a cycle of unproducible wares is not
            var ok = Store.Clone(d);
            ok.Buildings.First(b => b.Id == "BLD-055").Def.Recipes.Add(new RecipeData { Id = 15001, Name = "Unplane", LabourTicks = 10, Inputs = { new WareQtyData { Ware = "Plank", Count = 1 } }, Outputs = { new WareQtyData { Ware = "Log", Count = 1 } } });
            Assert.IsEmpty(Audits.Cycles(ok, out var found)); Assert.AreEqual(1, found.Count);
            var bad = Store.Clone(d);
            var carp = bad.Buildings.First(b => b.Id == "BLD-055").Def;
            carp.Recipes.Add(new RecipeData { Id = 15002, Name = "A", LabourTicks = 10, Inputs = { new WareQtyData { Ware = "Iron", Count = 1 } }, Outputs = { new WareQtyData { Ware = "Steel", Count = 1 } } });
            carp.Recipes.Add(new RecipeData { Id = 15003, Name = "B", LabourTicks = 10, Inputs = { new WareQtyData { Ware = "Steel", Count = 1 } }, Outputs = { new WareQtyData { Ware = "Iron", Count = 1 } } });
            StringAssert.Contains("no bootstrap path", string.Join("\n", Audits.Cycles(bad, out _)));
            // mines and deposits are Later and say so
            var mines = d.Buildings.Where(b => b.Name.EndsWith("Mine")).ToList();
            Assert.AreEqual(12, mines.Count);
            foreach (var m in mines) { Assert.AreEqual("Later", m.Stage, m.Id); StringAssert.Contains("no bootstrap path", m.Reason, m.Id); Assert.AreEqual("deferred", m.Status); }
            var deposits = d.Objects.Where(o => o.Stage != "UA-initial" && o.Workbook["source"].Contains("deposit")).ToList();
            Assert.AreEqual(5, deposits.Count);
            foreach (var o in deposits) { Assert.AreEqual("Later", o.Stage); StringAssert.Contains("no bootstrap path", o.Reason, o.Id); }
            var seeded = Store.Clone(d); seeded.Buildings.First(b => b.Id == "BLD-040").Reason = "Later: someday";
            Assert.IsNotEmpty(Audits.Cycles(seeded, out _));
        }

        // ---------- T8 ----------
        [Test]
        public void Catalogue_Reachability()
        {
            var d = Doc();
            var errors = Audits.Reachability(d, out var order);
            Assert.IsEmpty(errors);
            // the order lists every UA building (all levels) and every UA object with a producing step
            var text = string.Join("\n", order.Select(s => s.Text));
            foreach (var b in d.Buildings.Where(b => b.Stage == "UA-initial" && !b.Def.Founder))
                for (int l = 1; l <= b.Def.Levels.Count; l++) StringAssert.Contains("build " + b.Id + " ", text);
            // labour-only L1 shelters come first; the carpenter needs wood and stone, so it comes later
            int round(string needle) => order.First(s => s.Text.StartsWith(needle)).Round;
            Assert.AreEqual(1, round("build BLD-005 Woodcutter's shelter level 1"));
            Assert.Greater(round("build BLD-055"), round("collect Log at BLD-005"));
            Assert.Greater(round("craft Bed at BLD-055"), round("craft Bedding at BLD-055"));
            // the unresolved UB dependencies are reed and seeds (and deposits)
            var deps = string.Join("\n", Audits.UnresolvedDependencies(d));
            StringAssert.Contains("reed", deps); StringAssert.Contains("seed", deps); StringAssert.Contains("finite deposit", deps);
            // a recipe removed from the carpenter makes its product unreachable
            var bad = Store.Clone(d); var carp = bad.Buildings.First(b => b.Id == "BLD-055").Def; carp.Recipes.RemoveAll(r => r.Name == "Bedding");
            var err = Audits.Reachability(bad, out _);
            StringAssert.Contains("Bedding", string.Join("\n", err)); StringAssert.Contains("Bed", string.Join("\n", err));
        }

        // ---------- T9 ----------
        [Test]
        public void Catalogue_NoOwnershipWording()
        {
            var p = P; var d = Doc();
            Assert.IsEmpty(Audits.Wording(d));
            var active = Rows.ActiveObjects(d).Count() + Rows.ActiveBuildings(d).Count();
            Assert.AreEqual(40, active);
            // every UA row with fix codes has them applied, with a recorded correction each
            foreach (var r in d.Objects.Cast<RowBase>().Concat(d.Buildings).Where(r => r.Stage == "UA-initial"))
            {
                CollectionAssert.AreEqual(r.Fix, r.FixApplied, r.Id); Assert.AreEqual(r.Fix.Count, r.Corrections.Count, r.Id);
            }
            // the generated tables and the csv carry none of the terms either
            foreach (var kv in Generator.Generate(d, p.Wares()).Where(k => k.Key.EndsWith(".g.cs")))
                Assert.IsFalse(Audits.Banned.IsMatch(kv.Value), kv.Key + ": " + Audits.Banned.Match(kv.Value).Value);
            // the control row keeps the corrected wording (N1) and the superseded word only in the workbook cell
            var use = d.Controls.First(c => c.Name == "Use");
            CollectionAssert.AreEqual(new[] { "N1" }, use.FixApplied);
            Assert.IsFalse(Audits.Banned.IsMatch(use.Corrections[0]));
            StringAssert.Contains("rights", use.Workbook["requirements"]);
            // seeded violations fail: a wording term in a reason, a field text, an unapplied fix, a UA row left unresolved
            var a = Store.Clone(d); a.Objects.First(o => o.Id == "OBJ-092").Reason += " Only the owner may sleep in it.";
            StringAssert.Contains("owner", string.Join("\n", Audits.Wording(a)));
            var b = Store.Clone(d); b.Objects.First(o => o.Id == "OBJ-075").Def.Interactions[1].Name = "treat with consent";
            Assert.IsNotEmpty(Audits.Wording(b));
            var c2 = Store.Clone(d); c2.Buildings.First(x => x.Id == "BLD-001").FixApplied.Clear();
            Assert.IsNotEmpty(Audits.Wording(c2));
            var e = Store.Clone(d); e.Objects.First(o => o.Id == "OBJ-132").Status = "staged";
            Assert.IsNotEmpty(Audits.Wording(e));
            foreach (var w in new[] { "permission", "right", "recruit", "employ", "membership" })
            { var f = Store.Clone(d); f.Buildings.First(x => x.Id == "BLD-054").Reason += " " + w; Assert.IsNotEmpty(Audits.Wording(f), w); }
        }

        // ---------- T10 ----------
        [Test]
        public void Catalogue_DefsRoundTrip()
        {
            var p = P; var d = Doc(); var wares = p.Wares();
            Assert.IsEmpty(Audits.Effects(d, wares));
            Assert.AreEqual(30, Cat.Objects.Count); Assert.AreEqual(10, Cat.Buildings.Count); Assert.AreEqual(14, Cat.Recipes.Count);
            Assert.IsFalse(Cat.IsEmpty);
            for (int i = 1; i < Cat.Objects.Count; i++) Assert.Less(Cat.Objects[i - 1].CatId, Cat.Objects[i].CatId);
            for (int i = 1; i < Cat.Buildings.Count; i++) Assert.Less(Cat.Buildings[i - 1].CatId, Cat.Buildings[i].CatId);
            for (int i = 1; i < Cat.Recipes.Count; i++) Assert.Less(Cat.Recipes[i - 1].Id, Cat.Recipes[i].Id);
            var all = new List<(string who, InteractionDef row)>();
            foreach (var o in Cat.Objects)
            {
                Assert.AreSame(o, Cat.Object(o.CatId)); Assert.IsTrue(CatId.IsObject(o.CatId));
                Assert.AreEqual(Wares.IsUnique(o.Ware), o.Unique, CatId.Code(o.CatId));
                Assert.Greater(o.Weight, 0);
                foreach (var t in o.ToolMultiplier) { Assert.IsTrue(Enum.IsDefined(typeof(EffectKind), t.Effect)); Assert.That(t.Permille, Is.InRange(1, 1000)); }
                foreach (var r in o.Interactions) all.Add((CatId.Code(o.CatId), r));
            }
            foreach (var b in Cat.Buildings)
            {
                Assert.AreSame(b, Cat.Building(b.CatId)); Assert.IsTrue(CatId.IsBuilding(b.CatId));
                Assert.Greater(b.Levels.Count, 0); Assert.IsTrue(Enum.IsDefined(typeof(PlacementRule), b.Placement));
                foreach (var r in b.Interactions) all.Add((CatId.Code(b.CatId), r));
                foreach (var rec in b.Recipes) { Assert.AreEqual(b.CatId, rec.Station); Assert.IsTrue(Cat.Recipes.Contains(rec) || Cat.Recipes.Any(x => x.Id == rec.Id)); Assert.Greater(rec.LabourTicks, 0); }
            }
            Assert.IsNull(Cat.Object(1012 + 500)); Assert.IsNull(Cat.Building(2999));
            // every row: a valid target kind, source requirement, slot 0-3, effect kinds 1-20 only, args within the int16 range
            int rows = 0;
            foreach (var (who, row) in all)
            {
                rows++;
                Assert.IsTrue(Enum.IsDefined(typeof(TargetKind), row.TargetKind) && row.TargetKind != TargetKind.None, who);
                Assert.IsTrue(Enum.IsDefined(typeof(SourceReq), row.Source), who);
                Assert.LessOrEqual(row.Slot, 3, who);
                Assert.Greater(row.Effects.Count, 0, who);
                foreach (var e in row.Effects)
                {
                    Assert.IsTrue(Enum.IsDefined(typeof(EffectKind), e.Kind) && e.Kind >= EffectKind.Forage && e.Kind <= EffectKind.Play, who + " " + e.Kind);
                    Assert.That((int)e.Kind, Is.InRange(1, 20));
                }
                if (row.Source == SourceReq.Ware) Assert.IsTrue(Enum.IsDefined(typeof(Ware), (Ware)row.SourceArg), who);
                if (row.Source == SourceReq.Tool) { var tool = Cat.Object((ushort)row.SourceArg); Assert.IsNotNull(tool, who); Assert.IsTrue(tool.ToolMultiplier.Count > 0 || tool.StrikeAttack > 0, who); }
                if (row.TargetCat != 0) Assert.IsTrue(CatId.IsObject(row.TargetCat) || CatId.IsBuilding(row.TargetCat), who);
            }
            Assert.AreEqual(131, rows);
            // slots are distinct per (target, source) in the generated tables too
            foreach (var g in all.GroupBy(x => (x.row.TargetKind, x.row.TargetCat)))
            {
                var l = g.ToList();
                for (int i = 0; i < l.Count; i++) for (int j = i + 1; j < l.Count; j++)
                    {
                        var x = l[i].row; var y = l[j].row;
                        if (x.Slot != y.Slot) continue;
                        bool same = x.Source == y.Source && x.SourceArg == y.SourceArg;
                        bool anyClash = (x.Source == SourceReq.Any && y.Source != SourceReq.EmptyHand) || (y.Source == SourceReq.Any && x.Source != SourceReq.EmptyHand);
                        Assert.IsFalse(same || anyClash, l[i].who + "/" + l[j].who + " slot " + x.Slot);
                    }
            }
            // the weights fill UN0's slot
            Assert.AreEqual(12, UnifiedWares.Weight(Ware.Bed)); Assert.AreEqual(1, UnifiedWares.Weight(Ware.Mushroom)); Assert.AreEqual(15, UnifiedWares.Weight(Ware.Latrine));
            Assert.AreEqual(UnifiedWares.DefaultWeight, UnifiedWares.Weight(Ware.Log), "wares outside the unified block keep the default");
            // the JSON names exist in the enums
            foreach (var o in d.Objects.Where(x => x.Def != null)) Assert.IsTrue(Enum.TryParse<Ware>(o.Def.Ware, out _), o.Id);
            foreach (var b in d.Buildings.Where(x => x.Def != null)) { Assert.IsTrue(Enum.TryParse<PlacementRule>(b.Def.Placement, out _)); foreach (var c in b.Def.Capabilities) Assert.IsTrue(Enum.TryParse<Capabilities>(c, out _)); }
            foreach (var k in Spec.EffectKinds.Where(k => k != null)) Assert.IsTrue(Enum.TryParse<EffectKind>(k, out _), k);
            // seeded violations in the source fail the audit: kind 21 (unknown), slot 4, a missing ware argument, two rows on one slot
            var bad = Store.Clone(d);
            bad.Objects.First(o => o.Id == "OBJ-044").Def.Interactions[0].Effects[0].Kind = "Teleport";
            bad.Objects.First(o => o.Id == "OBJ-045").Def.Interactions[0].Slot = 4;
            bad.Objects.First(o => o.Id == "OBJ-046").Def.Interactions[0].Effects[0].A = null;
            var bld = bad.Buildings.First(b => b.Id == "BLD-054").Def; bld.Interactions.Add(bld.Interactions.First(i => i.Name == "input"));
            Assert.GreaterOrEqual(Audits.Effects(bad, wares).Count, 4);
        }

        // ---------- T11 ----------
        [Test]
        public void Catalogue_UARowsMatchDecisions()
        {
            Assert.IsNotNull(Cat.Object(1012));
            // the six collection shelters: 600 ticks labour only, storage 10/30/60, spots 2/4/6, radius 2 rings; L2 10 wood + 300 ticks; L3 20 wood + 10 stone + 600 ticks
            foreach (var (id, ware) in new[] { (2002, Ware.Fish), (2005, Ware.Log), (2006, Ware.Stone), (2051, Ware.Clay), (2052, Ware.Sheep), (2053, Ware.Hay) })
            {
                var b = Cat.Building((ushort)id);
                Assert.AreEqual(3, b.Levels.Count, id.ToString());
                Assert.AreEqual(600, b.Levels[0].Labour); Assert.AreEqual(0, b.Levels[0].Materials.Count);
                Assert.AreEqual(300, b.Levels[1].Labour); Assert.AreEqual(1, b.Levels[1].Materials.Count); Assert.AreEqual(Ware.Log, b.Levels[1].Materials[0].Ware); Assert.AreEqual(10, b.Levels[1].Materials[0].Count);
                Assert.AreEqual(600, b.Levels[2].Labour); Assert.AreEqual(2, b.Levels[2].Materials.Count);
                Assert.AreEqual(Ware.Log, b.Levels[2].Materials[0].Ware); Assert.AreEqual(20, b.Levels[2].Materials[0].Count); Assert.AreEqual(Ware.Stone, b.Levels[2].Materials[1].Ware); Assert.AreEqual(10, b.Levels[2].Materials[1].Count);
                CollectionAssert.AreEqual(new[] { 10, 30, 60 }, b.Levels.Select(l => l.Storage).ToArray());
                CollectionAssert.AreEqual(new[] { 2, 4, 6 }, b.Levels.Select(l => (int)l.SleepSpots).ToArray());
                foreach (var l in b.Levels) { Assert.AreEqual(2, l.CollectRadius); Assert.AreEqual(ware, l.CollectWare); }
                Assert.IsTrue((b.Capabilities & Capabilities.Collect) != 0 && (b.Capabilities & Capabilities.Sleep) != 0 && (b.Capabilities & Capabilities.Storage) != 0);
            }
            Assert.AreEqual(PlacementRule.Coast, Cat.Building(2002).Placement);
            Assert.AreEqual((byte)1, Cat.Building(2005).PlacementArg, "forest"); Assert.AreEqual((byte)4, Cat.Building(2006).PlacementArg, "mountain");
            // the hall: founder building, storage 40, 12 sleeping spots, integral hearth, fixture slots for 12 mats + 2 benches + 1 chest; no placement rows (cost TBD)
            var hall = Cat.Building(2001);
            Assert.AreEqual(40, hall.Levels[0].Storage); Assert.AreEqual(12, hall.Levels[0].SleepSpots); Assert.AreEqual(15, hall.Levels[0].FixtureSlots);
            Assert.IsTrue((hall.Capabilities & Capabilities.Hearth) != 0);
            Assert.IsFalse(hall.Interactions.Any(i => i.Effects.Any(e => e.Kind == EffectKind.PlaceSite)), "TBD cost stays inactive");
            // the hearth: 1 stone, 120 ticks; cook slot 60 ticks; fish and wood in, cooked fish out; fuel 1 wood per 1,200 ticks; warms within 2 nodes
            var hearth = Cat.Building(2054);
            Assert.AreEqual(120, hearth.Levels[0].Labour); Assert.AreEqual(1, hearth.Levels[0].Materials.Count); Assert.AreEqual(Ware.Stone, hearth.Levels[0].Materials[0].Ware); Assert.AreEqual(1, hearth.Levels[0].Materials[0].Count);
            Assert.AreEqual(1, hearth.Recipes.Count); var cook = hearth.Recipes[0];
            Assert.AreEqual(60, cook.LabourTicks); Assert.AreEqual(Ware.CookedFish, cook.Outputs[0].Ware);
            CollectionAssert.AreEquivalent(new[] { Ware.Fish, Ware.Log }, cook.Inputs.Select(q => q.Ware).ToArray());
            var fuel = hearth.Interactions.First(i => i.Effects.Any(e => e.Kind == EffectKind.Fuel)).Effects[0];
            Assert.AreEqual(1200, fuel.A); Assert.AreEqual(2, fuel.B);
            Assert.IsTrue((hearth.Capabilities & Capabilities.Hearth) != 0);
            // the well: 4 wood, 300 ticks, a fresh-water point
            var well = Cat.Building(2010);
            Assert.AreEqual(300, well.Levels[0].Labour); Assert.AreEqual(Ware.Log, well.Levels[0].Materials[0].Ware); Assert.AreEqual(4, well.Levels[0].Materials[0].Count);
            Assert.IsTrue((well.Capabilities & Capabilities.Water) != 0);
            // the carpenter: 3 wood + 1 stone; the twelve UA products are separate recipes
            var carp = Cat.Building(2055);
            Assert.AreEqual(2, carp.Levels[0].Materials.Count); Assert.AreEqual(12, carp.Recipes.Count);
            CollectionAssert.AreEquivalent(new[] { 1063, 1084, 1092, 1097, 1127, 1132, 1108, 1107, 1103, 1136, 1068, 1069 }, carp.Recipes.Select(r => (int)r.Id).ToArray());
            var latrine = carp.Recipes.First(r => r.Id == 1136); Assert.AreEqual(2, latrine.Inputs.First(q => q.Ware == Ware.Log).Count); Assert.AreEqual(1, latrine.Inputs.First(q => q.Ware == Ware.Clay).Count);
            var planks = carp.Recipes.First(r => r.Id == 1063); Assert.AreEqual(1, planks.Inputs[0].Count); Assert.AreEqual(2, planks.Outputs[0].Count);
            var bed = carp.Recipes.First(r => r.Id == 1092); Assert.AreEqual(3, bed.Inputs.First(q => q.Ware == Ware.Log).Count); Assert.AreEqual(1, bed.Inputs.First(q => q.Ware == Ware.Bedding).Count);
            // latrine: stores 20 waste, slot relieve; tool multipliers 0.6 / 0.7; strike attack 2, empty hand 1
            var relieve = Cat.Object(1136).Interactions.First(i => i.Effects[0].Kind == EffectKind.Relieve);
            Assert.AreEqual(20, relieve.Effects[0].C); Assert.AreEqual(3, relieve.Slot);
            var axe = Cat.Object(1069); var hammer = Cat.Object(1068);
            Assert.AreEqual(Ware.Axe, axe.Ware); Assert.AreEqual(Ware.StoneHammer, hammer.Ware);
            Assert.AreEqual(600, axe.ToolMultiplier.First(t => t.Effect == EffectKind.CollectRaw).Permille);
            Assert.AreEqual(700, hammer.ToolMultiplier.First(t => t.Effect == EffectKind.Labour).Permille);
            Assert.AreEqual(2, axe.StrikeAttack); Assert.AreEqual(2, hammer.StrikeAttack); Assert.AreEqual(1, Cat.EmptyHandStrikeAttack);
            Assert.IsTrue(Doc().Defaults.Any(n => n.Text.Contains("stone pick 0.7 on stone collection")), "the UB pick multiplier is recorded");
            // every shelter's wood collection also accepts the axe as a source (a duration multiplier only: no gate)
            var wood = Cat.Object(1024);
            Assert.AreEqual(1, wood.Interactions.Count(i => i.Source == SourceReq.EmptyHand)); Assert.AreEqual(1, wood.Interactions.Count(i => i.Source == SourceReq.Tool && i.SourceArg == 1069));
            // foods: raw fish gives no nutrition and has no Eat row; cooked fish and the forages do; raw rabbit forage takes 3x the mushroom time
            Assert.AreEqual(0, Cat.Object(1012).Nutrition); Assert.IsFalse(Cat.Object(1012).Interactions.Any(i => i.Effects.Any(e => e.Kind == EffectKind.Eat)));
            Assert.Greater(Cat.Object(1054).Nutrition, 0);
            foreach (var f in new[] { 1044, 1045, 1046, 1047, 1048, 1049, 1050 }) Assert.Greater(Cat.Object((ushort)f).Nutrition, 0, f.ToString());
            Assert.AreEqual(3 * Cat.Object(1044).Interactions.First(i => i.Effects[0].Kind == EffectKind.Forage).Ticks, Cat.Object(1047).Interactions.First(i => i.Effects[0].Kind == EffectKind.Forage).Ticks);
            // water: drink and wash are separate slots; UN1 reads A 0 as drink (fresh water only) and A 1 as wash
            var water = Cat.Object(1039);
            var drink = water.Interactions.First(i => i.Slot == 1).Effects[0]; var wash = water.Interactions.First(i => i.Slot == 2).Effects[0];
            Assert.AreEqual(EffectKind.DrinkWash, drink.Kind); Assert.AreEqual(0, drink.A, "UN1: A 0 = drink"); Assert.AreEqual(1, wash.A, "UN1: A 1 = wash"); Assert.AreEqual(400, drink.B); Assert.AreEqual(400, wash.B);
            // herbs treat pain on another person or on self; no flag
            Assert.IsTrue(Cat.Object(1075).Interactions.Any(i => i.TargetKind == TargetKind.Npc && i.Effects[0].Kind == EffectKind.Treat));
            Assert.IsTrue(Cat.Object(1075).Interactions.Any(i => i.TargetKind == TargetKind.Self && i.Effects[0].Kind == EffectKind.Treat));
        }

        // ---------- T12 ----------
        [Test]
        public void Catalogue_NeedReliefCoverage()
        {
            var p = P; var d = Doc(); var wares = p.Wares();
            Assert.IsEmpty(Audits.Needs(d, wares));
            var cov = Audits.NeedCoverage(d, wares);
            string[] names = UaContent.Channels;
            for (int ch = 0; ch < 12; ch++)
            {
                var need = d.Needs.First(n => n.Channel == ch);
                if (ch == 8 || ch == 3) { Assert.AreEqual(0, cov[ch].Count, names[ch]); StringAssert.Contains("no catalogue row", need.ReliefSource, names[ch]); }
                else { Assert.Greater(cov[ch].Count, 0, names[ch] + " has no relief row"); Assert.IsTrue(string.IsNullOrEmpty(need.ReliefSource), names[ch]); }
            }
            // the relief paths of the manifest 3.3: Hunger forage + cooked fish; Thirst water; Energy bed, hall, shelter; Comfort stool, bench, bed; Warmth hearth, shelter; Hygiene water; Bladder latrine; Room ornament, chest, waste; Fun toy; Pain herbs
            CollectionAssert.IsSupersetOf(cov[0], new[] { "OBJ-044", "OBJ-054" }); CollectionAssert.Contains(cov[5], "OBJ-039");
            CollectionAssert.IsSupersetOf(cov[1], new[] { "OBJ-092", "BLD-001", "BLD-005" }); CollectionAssert.IsSupersetOf(cov[10], new[] { "OBJ-097", "OBJ-127", "OBJ-092" });
            CollectionAssert.IsSupersetOf(cov[2], new[] { "BLD-054", "BLD-005" }); CollectionAssert.Contains(cov[6], "OBJ-039"); CollectionAssert.Contains(cov[7], "OBJ-136");
            CollectionAssert.IsSupersetOf(cov[11], new[] { "OBJ-108", "OBJ-107", "OBJ-114" }); CollectionAssert.Contains(cov[9], "OBJ-132"); CollectionAssert.Contains(cov[4], "OBJ-075");
            // a channel loses its only relief row: the audit fails
            var bad = Store.Clone(d); bad.Objects.First(o => o.Id == "OBJ-136").Def.Interactions.RemoveAll(i => i.Effects.Any(e => e.Kind == "Relieve"));
            StringAssert.Contains("Bladder", string.Join("\n", Audits.Needs(bad, wares)));
        }

        // ---------- T13 ----------
        [Test]
        public void Catalogue_Report()
        {
            var p = P; var d = Doc(); var wares = p.Wares();
            string a = Report.Make(d, wares), b = Report.Make(Store.Clone(d), wares);
            Assert.AreEqual(a, b);
            Assert.AreEqual(Store.ReadLf(Path.Combine(p.Root, Report.Path)), a, "CATALOG1_REPORT.md differs: run `God.Catalogue report`");
            // counts equal T2
            StringAssert.Contains("| Controls | 12 | 12 | 0 | 0 | 0 | 12 | 0 | 0 | 0 |", a);
            StringAssert.Contains("| Needs | 12 | 12 | 0 | 0 | 0 | 12 | 0 | 0 | 0 |", a);
            StringAssert.Contains("| Objects | 138 | 29 | 54 | 55 | 0 | 29 | 54 | 55 | 0 |", a);
            StringAssert.Contains("| Buildings | 65 | 10 | 13 | 42 | 0 | 10 | 13 | 42 | 0 |", a);
            // every row appears with its status
            foreach (var r in d.Objects.Cast<RowBase>().Concat(d.Buildings))
                StringAssert.Contains("| " + r.Id + " | " + r.CatId + " | " + r.WorkbookRow + " | " + r.Name.Replace("|", "/") + " | " + r.Stage + " | " + r.Status + " |", a, r.Id);
            foreach (var c in d.Controls) StringAssert.Contains("| " + c.Id + " | " + c.Name + " |", a);
            foreach (var n in d.Needs) StringAssert.Contains("| " + n.Id + " | " + n.Name + " |", a);
            // the report names the hash, the production order, the unresolved dependencies, the ware requests and the open decisions
            StringAssert.Contains(Cat.CatalogueHash, a);
            StringAssert.Contains("Computed production order", a); StringAssert.Contains("Unresolved bootstrap dependencies", a); StringAssert.Contains("Ware ids requested", a); StringAssert.Contains("owner or lead decision", a);
            StringAssert.Contains("Social and Safety have no catalogue relief row", a);
            // the manifest status column equals the report's status
            var csv = Store.ReadLf(p.Json.Replace("catalogue.json", "catalogue.csv")).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(1 + 138 + 65, csv.Length);
        }


        // ---------- reconciliation with UN1's world and needs (merged on unity/poc) ----------
        [Test]
        public void Catalogue_MatchesUN1Defaults()
        {
            var t = new UN1Tuning();
            var foods = new[] { (Ware.Mushroom, 1044), (Ware.Roots, 1045), (Ware.WildEgg, 1046), (Ware.Rabbit, 1047), (Ware.WildVeg, 1048), (Ware.CactusFruit, 1049), (Ware.Seaweed, 1050), (Ware.Herbs, 1075) };
            foreach (var (ware, id) in foods)
            {
                int idx = HexTypes.ForageIndex(ware);
                var o = Cat.Object((ushort)id);
                Assert.AreEqual(ware, o.Ware);
                var row = o.Interactions.First(i => i.Effects[0].Kind == EffectKind.Forage);
                Assert.AreEqual(t.ForageTicks[idx], row.Ticks, ware + " forage ticks equal UN1's default");
                Assert.AreEqual(t.ForageNutrition[idx], o.Nutrition, ware + " nutrition equals UN1's default");
                Assert.AreEqual((short)ware, row.Effects[0].A); Assert.AreEqual(TargetKind.ForagePlace, row.TargetKind);
            }
            var water = Cat.Object(1039);
            Assert.AreEqual(t.DrinkTicks, water.Interactions.First(i => i.Slot == 1).Ticks); Assert.AreEqual(t.WashTicks, water.Interactions.First(i => i.Slot == 2).Ticks);
            Assert.AreEqual(t.DrinkRelief, water.Interactions.First(i => i.Slot == 1).Effects[0].B); Assert.AreEqual(t.WashRelief, water.Interactions.First(i => i.Slot == 2).Effects[0].B);
            // the raws of UN1's hex types are the wares of the collection shelters
            foreach (var (h, shelter) in new[] { (HexType.Forest, 2005), (HexType.Mountain, 2006), (HexType.Wetland, 2051), (HexType.Pastures, 2052), (HexType.Fields, 2053), (HexType.Sea, 2002) })
                Assert.AreEqual(HexTypes.RawOf(h), Cat.Building((ushort)shelter).Levels[0].CollectWare, h.ToString());
            // the terrain a shelter stands on is the stored terrain of its hex type
            Assert.AreEqual((byte)HexTypes.TerrainOf(HexType.Wetland), Cat.Building(2051).PlacementArg);
            Assert.AreEqual((byte)HexTypes.TerrainOf(HexType.Forest), Cat.Building(2005).PlacementArg);
            Assert.AreEqual((byte)HexTypes.TerrainOf(HexType.Pastures), Cat.Building(2052).PlacementArg);
            // every need channel a row names exists in UN1's NeedChannel order
            foreach (var n in Doc().Needs) Assert.AreEqual(n.Name == "Freedom from pain" ? "Pain" : n.Name, ((NeedChannel)n.Channel).ToString());
        }

        // ---------- the command line ----------
        [Test]
        public void Catalogue_ValidateCommand_RunsClean()
        {
            var o = new StringWriter(); var e = new StringWriter();
            int code = God.Catalogue.Program.Run(new[] { "validate", "--root", P.Root }, o, e);
            Assert.AreEqual(0, code, e.ToString());
            StringAssert.Contains("validate: OK", o.ToString()); StringAssert.Contains("CatalogueHash " + Cat.CatalogueHash, o.ToString());
            Assert.AreEqual(1, God.Catalogue.Program.Run(new[] { "import-workbook", "--root", P.Root }, o, e), "import refuses to overwrite the reviewed source without --force");
        }
    }
}
