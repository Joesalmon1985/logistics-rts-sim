using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace God.Catalogue
{
    /// <summary>
    /// <c>import-workbook</c>: reads the pinned workbook, verifies its SHA-256, joins it with the manifest tables (a mismatch of id, name,
    /// workbook row or stage throws) and writes the skeleton of all 12 controls, 12 needs, 138 objects and 65 buildings, with the
    /// authored UA definitions of <see cref="UaContent"/> filled in. A human reviews the diff of the result.
    /// </summary>
    public static class Importer
    {
        public const int ControlsFirstRow = 5, NeedsFirstRow = 21, ObjectsFirstRow = 5, BuildingsFirstRow = 5;
        public const int ObjectCount = 138, BuildingCount = 65;

        static readonly HashSet<string> UaNames = new HashSet<string>(new[]
        {
            "wood", "stone", "clay", "hay", "fish", "sheep", "planks", "bedding", "water", "herbs", "cooked fish", "mushrooms", "roots", "wild eggs",
            "rabbit", "wild vegetables", "cactus fruit", "seaweed", "bed", "stool", "bench", "toy", "stone hammer", "stone axe",
        });

        // objects that reuse an existing ware id (legacy twins); the lead confirms each reuse
        static readonly Dictionary<string, string> LegacyTwin = new Dictionary<string, string>
        {
            ["Cloth"] = "Cloth", ["Mortar"] = "Mortar", ["Pottery"] = "Pottery", ["Brick"] = "Brick", ["Leather"] = "Leather", ["Bow"] = "Bow",
            ["Sling"] = "Sling", ["Small shield"] = "SmallShield", ["Light armour"] = "LightArmour", ["Sleeping mat"] = "SleepingMat",
        };

        static List<string> FixCodes(string s)
        {
            var l = new List<string>();
            foreach (char ch in s) if (ch >= 'A' && ch <= 'I') l.Add(ch.ToString());
            return l;
        }

        public static CatalogueDoc Import(string workbookPath, string manifestPath, string waresCsPath)
        {
            var wb = Workbook.Read(workbookPath);
            if (wb.Sha256 != Workbook.PinnedSha256)
                throw new InvalidOperationException("workbook SHA-256 " + wb.Sha256 + " differs from the pinned " + Workbook.PinnedSha256 + ": the manifest must be re-reviewed");
            var wares = WareTable.Load(waresCsPath);
            var objM = Manifest.Parse(manifestPath, "OBJ");
            var bldM = Manifest.Parse(manifestPath, "BLD");
            if (objM.Count != ObjectCount || bldM.Count != BuildingCount) throw new InvalidOperationException("manifest has " + objM.Count + "/" + bldM.Count + " rows");

            var doc = new CatalogueDoc
            {
                Format = "god-catalogue/1",
                WorkbookFile = "docs/unification/God_of_the_Island_Core_Design.xlsx",
                WorkbookSha256 = wb.Sha256,
                Manifest = "docs/unification/CATALOGUE_MANIFEST.md",
            };
            AddNotes(doc);

            // ---- controls ----
            for (int i = 0; i < 12; i++)
            {
                int row = ControlsFirstRow + i;
                var c = new ControlRow
                {
                    Id = "CTL-" + (i + 1).ToString("D3"), CatId = 0, Name = wb.Cell("NPC Actions", row, "A"), WorkbookRow = row, WorkbookSha256 = wb.Sha256,
                    Stage = "UA-initial", Status = "implemented", Prim = i + 1,
                    Reason = "Primitive " + (i + 1) + " of the twelve controls (Prim enum, UN0); the only way a body acts",
                };
                c.Workbook["target"] = wb.Cell("NPC Actions", row, "B"); c.Workbook["effect"] = wb.Cell("NPC Actions", row, "C"); c.Workbook["requirements"] = wb.Cell("NPC Actions", row, "D");
                if (c.Name == "Use")
                {
                    c.Fix.Add("N1"); c.FixApplied.Add("N1");
                    c.Corrections.Add("N1: the access-condition wording is deleted from the Use requirement; USE is valid on reach, inputs, capacity, barriers, occupancy and physical validity only");
                }
                doc.Controls.Add(c);
            }

            // ---- needs (workbook order; Channel = index in the unified order) ----
            string[] reliefSource = new string[12];
            reliefSource[8] = "gestures and shared play (no catalogue row)";
            reliefSource[3] = "movement away from a threat (no catalogue row)";
            for (int i = 0; i < 12; i++)
            {
                int row = NeedsFirstRow + i;
                string name = wb.Cell("NPC Actions", row, "A");
                string key = name == "Freedom from pain" ? "Pain" : name;
                int ch = Array.IndexOf(UaContent.Channels, key);
                if (ch < 0) throw new InvalidOperationException("unknown need " + name);
                var n = new NeedRow
                {
                    Id = "NEED-" + (i + 1).ToString("D3"), CatId = 0, Name = name, WorkbookRow = row, WorkbookSha256 = wb.Sha256, Stage = "UA-initial",
                    Status = "implemented", Channel = ch, ReliefSource = reliefSource[ch],
                    Reason = "Channel " + ch + " of the twelve (unified order Hunger, Energy, Warmth, Safety, Pain, Thirst, Hygiene, Bladder, Social, Fun, Comfort, Room); rates and magnitudes are tuning (UN1)",
                };
                n.Workbook["codeStatus"] = wb.Cell("NPC Actions", row, "B"); n.Workbook["changedBy"] = wb.Cell("NPC Actions", row, "C"); n.Workbook["relievedBy"] = wb.Cell("NPC Actions", row, "D");
                doc.Needs.Add(n);
            }

            var uaObj = UaContent.Objects();
            var uaBld = UaContent.Buildings();

            // ---- objects ----
            var objNames = new List<string>();
            for (int i = 0; i < ObjectCount; i++)
            {
                int row = ObjectsFirstRow + i; string id = "OBJ-" + (i + 1).ToString("D3");
                var m = objM[i];
                string name = wb.Cell("Objects", row, "A");
                if (m.Id != id || m.WorkbookRow != row || m.Name != name) throw new InvalidOperationException("manifest mismatch at " + id + ": manifest '" + m.Name + "' row " + m.WorkbookRow + ", workbook '" + name + "' row " + row);
                var r = new ObjectRow { Id = id, CatId = 1000 + i + 1, Name = name, WorkbookRow = row, WorkbookSha256 = wb.Sha256, Stage = m.Stage, Fix = FixCodes(m.Fix) };
                r.Workbook["type"] = wb.Cell("Objects", row, "B"); r.Workbook["source"] = wb.Cell("Objects", row, "C"); r.Workbook["use"] = wb.Cell("Objects", row, "D");
                r.Workbook["effect"] = wb.Cell("Objects", row, "E"); r.Workbook["need"] = wb.Cell("Objects", row, "F");
                if (UaContent.Aliases.TryGetValue(id, out var al)) r.Aliases.AddRange(al);
                Classify(r, uaObj.ContainsKey(id) ? uaObj[id] : null, r.Workbook["source"], null);
                doc.Objects.Add(r);
                objNames.Add(name);
            }

            // ---- buildings ----
            for (int i = 0; i < BuildingCount; i++)
            {
                int row = BuildingsFirstRow + i; string id = "BLD-" + (i + 1).ToString("D3");
                var m = bldM[i];
                string name = wb.Cell("Buildings", row, "A");
                if (m.Id != id || m.WorkbookRow != row || m.Name != name) throw new InvalidOperationException("manifest mismatch at " + id + ": manifest '" + m.Name + "' row " + m.WorkbookRow + ", workbook '" + name + "' row " + row);
                var r = new BuildingRow { Id = id, CatId = 2000 + i + 1, Name = name, WorkbookRow = row, WorkbookSha256 = wb.Sha256, Stage = m.Stage, Fix = FixCodes(m.Fix) };
                r.Workbook["cost"] = wb.Cell("Buildings", row, "B"); r.Workbook["upgradeFrom"] = wb.Cell("Buildings", row, "C"); r.Workbook["produces"] = wb.Cell("Buildings", row, "D");
                r.Workbook["furnishings"] = wb.Cell("Buildings", row, "E"); r.Workbook["function"] = wb.Cell("Buildings", row, "F");
                Classify(r, null, r.Workbook["cost"], uaBld.ContainsKey(id) ? uaBld[id] : null);
                doc.Buildings.Add(r);
            }

            // ware requests for the UB objects (UN3a does not edit Wares.cs; the lead appends the names)
            int next = 94;
            foreach (var r in doc.Objects.Where(o => o.Stage == "UB"))
            {
                if (LegacyTwin.TryGetValue(r.Name, out var ex))
                {
                    doc.WareRequests.Add(new WareRequest { ObjectId = r.Id, Name = r.Name, ProposedId = wares.Id(ex), Reuse = true, ExistingWare = ex });
                }
                else
                {
                    doc.WareRequests.Add(new WareRequest { ObjectId = r.Id, Name = r.Name, ProposedId = next++, Reuse = false });
                }
            }
            if (next - 1 > Wares159) throw new InvalidOperationException("ware block 94-159 exhausted: " + (next - 94) + " requested");
            return doc;
        }

        const int Wares159 = 159;

        static void AddNotes(CatalogueDoc doc)
        {
            void N(List<Note> l, string k, string t) => l.Add(new Note { Key = k, Text = t });
            var c = doc.Conventions;
            N(c, "ids", "OBJ-nnn and BLD-nnn are the sheet position at the pinned workbook hash (nnn from 001); catId is 1000+nnn for objects and 2000+nnn for buildings; ids are immutable and a rename adds an alias");
            N(c, "status", "implemented: active generated content (UA-initial with its corrections applied); staged: UB, defined later, needs ware ids or inputs beyond UA; deferred: Later; rejected: none yet");
            N(c, "needChannels", "Effect arguments name a need channel by name: Hunger 0, Energy 1, Warmth 2, Safety 3, Pain 4, Thirst 5, Hygiene 6, Bladder 7, Social 8, Fun 9, Comfort 10, Room 11 (contracts 23.6). Relief amounts are permille of the channel range");
            N(c, "slots", "Slot 0 means the first matching row; 1-3 disambiguate rows with the same target and source. Names used: deposit/install/input/occupy/drink/place/work 1-3 as listed per row; the slot only selects the row, the effect kind defines the behaviour");
            N(c, "sourceArg", "Source Ware: SourceArg is the ware (name in this file, enum value in the table); Source Tool: SourceArg is the tool's object CatId; Source Any: any carried lot; EmptyHand: SourceArg unused");
            N(c, "targetCat", "ForagePlace, RawNode and WaterPoint rows carry the CatId of the object they yield; Fixture rows the CatId of the furnishing; Site, Placement and Building rows the CatId of the building design; Self and Npc rows carry none");
            N(c, "Forage", "A ware, B units");
            N(c, "DrinkWash", "A channel relieved, B relief permille, C 1 = fresh water only (drink), 0 = any water (wash)");
            N(c, "Eat", "A channel (Hunger); the amount is ObjectDef.Nutrition of the eaten lot (permille of the Hunger range)");
            N(c, "NeedRelief", "A channel, B relief permille; in a row with Occupy it applies every Ticks of occupancy, otherwise once at completion");
            N(c, "CollectRaw", "A ware, B units, C the CatId of the collection building that must stand within its radius of the node; a Tool source only shortens Ticks by ToolMultiplier");
            N(c, "Deposit", "A ware (0 any), B capacity override of the target container (0 = the building's storage bin)");
            N(c, "Install", "A 0 install into the Fixtures bin, 1 uninstall");
            N(c, "Occupy", "A sleeping or seating places offered (0 = the building's SleepSpots)");
            N(c, "WorkInput", "A ware (0 any); moves the lot into the WorkIn bin");
            N(c, "RecipeWork", "A recipe id (0 = the RecipeDef of the building whose Id equals the control's design id, which is the output object's CatId); duration is the recipe LabourTicks (row Ticks 0); the hall hearth reads its inputs from the Storage bin");
            N(c, "Fuel", "A ticks one wood burns, B warming radius in nodes, C warmth relief permille per 100 ticks");
            N(c, "PlaceSite", "A design CatId, B level to build (1 new, 2 or 3 upgrade)");
            N(c, "Contribute", "A ware, B units per use (0 = as many as the site still needs)");
            N(c, "Labour", "A labour ticks credited per use (0 = the row Ticks); a Tool source applies the tool's Labour multiplier");
            N(c, "Treat", "A channel (Pain), B relief permille, C injury steps healed");
            N(c, "Strike", "A attack of the held object, B fatigue permille; an empty hand strikes with Catalogue.EmptyHandStrikeAttack (1)");
            N(c, "Relieve", "A channel (Bladder), B relief permille, C waste units the fixture can hold");
            N(c, "Play", "A fun relief permille, B 0 (social relief of shared play is tuning, UN3b)");
            N(c, "recipeIds", "RecipeDef.Id is the output object's CatId; a second station making the same output uses 10000 x k + CatId (k = station ordinal)");
            var d = doc.Defaults;
            N(d, "provisional", "Every number in this file is a configurable provisional default, not an owner fact (brief Part C); D9.4 b and D9.5 values are marked in the manifest");
            N(d, "fixtureSlots", "Collection shelter fixture slots 0/2/4 at L1/L2/L3 (L1 has basic sleeping spots only, G14); hall 15 (12 mats, 2 benches, 1 chest)");
            N(d, "carpenterLabour", "Carpenter labour 300 ticks (the workbook gives no number); recipe labour 60-180 ticks per product");
            N(d, "durations", "Forage 60 ticks (rabbit 180 = 3x, G9; roots and herbs 90), collect 60 (stone 90, sheep 120), drink and wash 20, eat 20, strike 12, labour chunk 30, deliver 10");
            N(d, "relief", "Water 800 permille, bed energy 300 + comfort 150 per 100 ticks, mat 200 + 100, shelter and hall floor energy 120 + warmth 40 + comfort 30, stool and bench comfort 100, cradle 250 + 150, ornament room 150, chest room 80, toy fun 300, herbs pain 500");
            N(d, "weights", "Carry weights: forage and hay 1, fish 2, wood and clay 3, stone 4, sheep 10, bed and chest 12, latrine 15 (the carry limit is UN3b/Goods)");
            N(d, "toolMultipliers", "Tools are duration multipliers, never gates (G13): stone axe 0.6 on wood collection, stone hammer 0.7 on construction labour, stone pick 0.7 on stone collection (the pick is UB: defined with OBJ-070); strike attack 2 for held stone tools, 1 for an empty hand");
            N(d, "nutrition", "Hunger relief permille: mushroom 150, roots 200, wild egg 250, rabbit 350, wild vegetable 150, cactus fruit 120, seaweed 100, cooked fish 400, raw fish 0");
            N(d, "terrainWetland", "Clay shelter and clay raw use the legacy terrain Hills (the legacy clay terrain) until UN1 adds a wetland terrain");
            N(d, "endowment", "OBJ-093 Sleeping mat stays stage UB (reed, G7) and is generated as an endowment-only definition for the founder hall (G6)");
        }

        /// <summary>Sets status and reason; attaches the authored definition to a UA row.</summary>
        static void Classify(RowBase row, ObjectDefData oDef, string inputsText, BuildingDefData bDef)
        {
            bool isObj = row is ObjectRow;
            string stage = row.Stage;
            if (stage == "UA-initial")
            {
                row.Status = "implemented";
                row.FixApplied.AddRange(row.Fix);
                foreach (var f in row.Fix) if (UaContent.FixText.TryGetValue(f, out var t)) row.Corrections.Add(t);
                row.Reason = UaContent.UaReason[row.Id];
                if (isObj) ((ObjectRow)row).Def = oDef ?? throw new InvalidOperationException("no UA definition for " + row.Id);
                else ((BuildingRow)row).Def = bDef ?? throw new InvalidOperationException("no UA definition for " + row.Id);
                return;
            }
            row.Status = stage == "UB" ? "staged" : stage == "Later" ? "deferred" : stage == "Superseded" ? "rejected" : throw new InvalidOperationException("stage " + stage);
            var missing = MissingInputs(inputsText);
            var sb = new StringBuilder(stage + ": ");
            bool mine = Regex.IsMatch(row.Name, @"Mine$") || (isObj && row.Workbook["source"].Contains("deposit"));
            if (mine) sb.Append("finite deposits have no placement data and no bootstrap path (G10)");
            else if (inputsText.Contains("TBD")) sb.Append("cost TBD stays inactive until set (F)");
            else if (missing.Count > 0) sb.Append("needs " + string.Join(", ", missing) + " (outside the UA closure)");
            else sb.Append("no UA need and no UA closure role; defined after the first tier");
            if (missing.Contains("reed") || missing.Contains("wheat seed")) sb.Append("; reed and seed are UB (G7)");
            var open = row.Fix.Where(f => f != "G" || true).ToList();
            if (open.Count > 0) sb.Append("; corrections " + string.Join("", open) + " to apply before activation");
            if (isObj && row.Name == "Sleeping mat")
            {
                var uaObj = UaContent.Objects();
                ((ObjectRow)row).Def = uaObj["OBJ-093"];
                row.FixApplied.AddRange(row.Fix);
                foreach (var f in row.Fix) if (UaContent.FixText.TryGetValue(f, out var t)) row.Corrections.Add(t);
                sb.Append("; endowment-only definition for the founder hall (G6), no recipe");
            }
            row.Reason = sb.ToString();
        }

        /// <summary>Input names of a workbook recipe or cost text that are outside the UA set (alternatives split on '/', parts on '+').</summary>
        public static List<string> MissingInputs(string text)
        {
            var miss = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return miss;
            foreach (var alt in text.Split('/'))
            {
                string s = alt;
                int colon = s.IndexOf(':'); if (colon >= 0) s = s.Substring(colon + 1);
                int arrow = s.IndexOf('→'); if (arrow >= 0) s = s.Substring(0, arrow);
                if (s.Contains("TBD") || s.Contains("deposit") || s.Contains("stock") || s.Contains("labour") || s.Contains("Founder") || s.Contains("forage")
                    || s.Contains("habitat") || s.Contains("cultivation") || s.Contains("plants") || s.Contains("Consumption") || s.Contains("route") || s.Contains("animals")
                    || s.Contains("population") || s.Contains("L1") || s.Contains("live")) { if (s.Contains("cultivation")) AddOnce(miss, "wetland cultivation"); if (s.Contains("plants")) AddOnce(miss, "seed plants"); continue; }
                foreach (var part in s.Split('+'))
                {
                    string p = Regex.Replace(part.Trim(), @"^\d+\s+", "").ToLowerInvariant();
                    if (p.Length == 0 || UaNames.Contains(p)) continue;
                    AddOnce(miss, p);
                }
            }
            return miss;
        }

        static void AddOnce(List<string> l, string s) { if (!l.Contains(s)) l.Add(s); }

        public static string GoldenIds(CatalogueDoc doc)
        {
            var sb = new StringBuilder();
            foreach (var o in doc.Objects) sb.Append(o.Id).Append('\t').Append(o.WorkbookRow).Append('\t').Append(o.Name).Append('\n');
            foreach (var b in doc.Buildings) sb.Append(b.Id).Append('\t').Append(b.WorkbookRow).Append('\t').Append(b.Name).Append('\n');
            return sb.ToString();
        }
    }
}
