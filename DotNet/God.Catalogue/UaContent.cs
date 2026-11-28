using System.Collections.Generic;

namespace God.Catalogue
{
    /// <summary>
    /// The authored definition data of the 39 UA-initial rows (manifest 3.3) with the corrections N1, A-I applied and the D9.4 b / D9.5
    /// numbers. Used once by <c>import-workbook</c>; afterwards catalogue.json is the source and this file is history.
    /// Every number is a configurable default. No ownership, permission, membership, profession or consent field exists.
    /// </summary>
    public static class UaContent
    {
        // ---- the twelve need channels in the unified order (contracts 23.6) ----
        public static readonly string[] Channels = { "Hunger", "Energy", "Warmth", "Safety", "Pain", "Thirst", "Hygiene", "Bladder", "Social", "Fun", "Comfort", "Room" };

        static EffectData E(string kind, object a = null, object b = null, object c = null) => new EffectData { Kind = kind, A = a, B = b, C = c };

        static InteractionData I(string name, string target, string targetCat, int slot, string source, object sourceArg, int reach, int ticks, params EffectData[] fx)
        {
            var i = new InteractionData { Name = name, Target = target, TargetCat = targetCat, Slot = slot, Source = source, SourceArg = sourceArg, Reach = reach, Ticks = ticks };
            i.Effects.AddRange(fx);
            return i;
        }

        static WareQtyData Q(string ware, int n) => new WareQtyData { Ware = ware, Count = n };

        static ObjectDefData O(string ware, int weight, int nutrition = 0, string role = null, params InteractionData[] rows)
        {
            var d = new ObjectDefData { Ware = ware, Weight = weight, Nutrition = nutrition, Role = role };
            d.Interactions.AddRange(rows);
            return d;
        }

        // ---------- objects ----------
        static InteractionData Collect(string cat, string ware, int ticks, string building) => I("collect", "RawNode", cat, 1, "EmptyHand", null, 1, ticks, E("CollectRaw", ware, 1, building));
        static InteractionData Forage(string cat, string ware, int ticks) => I("forage", "ForagePlace", cat, 1, "EmptyHand", null, 1, ticks, E("Forage", ware, 1));
        static InteractionData Eat(string ware) => I("eat", "Self", null, 1, "Ware", ware, 0, 20, E("Eat", "Hunger"));
        static InteractionData Uninstall(string cat) => I("uninstall", "Fixture", cat, 2, "EmptyHand", null, 1, 15, E("Install", 1));

        static ObjectDefData Seat(string cat, string ware, int weight, int spots, int energy, int comfort)
        {
            var o = O(ware, weight, 0, "furnishing");
            var fx = new List<EffectData> { E("Occupy", spots) };
            if (energy > 0) fx.Add(E("NeedRelief", "Energy", energy));
            if (comfort > 0) fx.Add(E("NeedRelief", "Comfort", comfort));
            o.Interactions.Add(I(energy > 0 ? "occupy" : "sit", "Fixture", cat, 1, "EmptyHand", null, 1, 100, fx.ToArray()));
            o.Interactions.Add(Uninstall(cat));
            return o;
        }

        public static Dictionary<string, ObjectDefData> Objects()
        {
            var d = new Dictionary<string, ObjectDefData>();
            // raws from the six collection shelters (dice stock): no tool gates a collection (G13)
            d["OBJ-012"] = O("Fish", 2, 0, "raw", Collect("OBJ-012", "Fish", 60, "BLD-002"));
            d["OBJ-016"] = O("Stone", 4, 0, "raw", Collect("OBJ-016", "Stone", 90, "BLD-006"));
            d["OBJ-024"] = O("Log", 3, 0, "raw", Collect("OBJ-024", "Log", 60, "BLD-005"),
                I("collect-tool", "RawNode", "OBJ-024", 1, "Tool", "OBJ-069", 1, 60, E("CollectRaw", "Log", 1, "BLD-005")));
            d["OBJ-041"] = O("Clay", 3, 0, "raw", Collect("OBJ-041", "Clay", 60, "BLD-051"));
            d["OBJ-042"] = O("Sheep", 10, 0, "raw", Collect("OBJ-042", "Sheep", 120, "BLD-052"));
            d["OBJ-043"] = O("Hay", 1, 0, "raw", Collect("OBJ-043", "Hay", 60, "BLD-053"));
            // water: a fresh-water point (natural or well); drink and wash are separate slots (E, G3)
            d["OBJ-039"] = O("Water", 1, 0, "water",
                I("drink", "WaterPoint", "OBJ-039", 1, "EmptyHand", null, 1, 20, E("DrinkWash", "Thirst", 800, 1)),
                I("wash", "WaterPoint", "OBJ-039", 2, "EmptyHand", null, 1, 20, E("DrinkWash", "Hygiene", 800, 0)));
            // the seven forages: infinite, time-limited, edible raw; offers are gestures (C)
            d["OBJ-044"] = O("Mushroom", 1, 150, "forage", Forage("OBJ-044", "Mushroom", 60), Eat("Mushroom"));
            d["OBJ-045"] = O("Roots", 1, 200, "forage", Forage("OBJ-045", "Roots", 90), Eat("Roots"));
            d["OBJ-046"] = O("WildEgg", 1, 250, "forage", Forage("OBJ-046", "WildEgg", 60), Eat("WildEgg"));
            d["OBJ-047"] = O("Rabbit", 2, 350, "forage", Forage("OBJ-047", "Rabbit", 180), Eat("Rabbit"));
            d["OBJ-048"] = O("WildVeg", 1, 150, "forage", Forage("OBJ-048", "WildVeg", 60), Eat("WildVeg"));
            d["OBJ-049"] = O("CactusFruit", 1, 120, "forage", Forage("OBJ-049", "CactusFruit", 60), Eat("CactusFruit"));
            d["OBJ-050"] = O("Seaweed", 1, 100, "forage", Forage("OBJ-050", "Seaweed", 60), Eat("Seaweed"));
            d["OBJ-054"] = O("CookedFish", 1, 400, "food", Eat("CookedFish"));
            // herbs: forage-like source (G5); a remedy used on a reachable person or on self; no flag, the person may walk away (D)
            d["OBJ-075"] = O("Herbs", 1, 0, "remedy", Forage("OBJ-075", "Herbs", 90),
                I("treat", "Npc", null, 3, "Ware", "Herbs", 1, 60, E("Treat", "Pain", 500, 1)),
                I("treat-self", "Self", null, 3, "Ware", "Herbs", 0, 60, E("Treat", "Pain", 500, 1)));
            // processed inputs (recipe inputs only)
            d["OBJ-063"] = O("Plank", 3, 0, "input");
            d["OBJ-084"] = O("Bedding", 2, 0, "input");
            // tools: speed multipliers (permille of the base duration) and strike-capable objects (E, G4, G13)
            var hammer = O("StoneHammer", 3, 0, "tool", I("strike", "Npc", null, 3, "Ware", "StoneHammer", 1, 12, E("Strike", 2, 100)));
            hammer.StrikeAttack = 2; hammer.ToolMultipliers.Add(new ToolMulData { Effect = "Labour", Permille = 700 });
            d["OBJ-068"] = hammer;
            var axe = O("Axe", 3, 0, "tool", I("strike", "Npc", null, 3, "Ware", "Axe", 1, 12, E("Strike", 2, 100)));
            axe.StrikeAttack = 2; axe.ToolMultipliers.Add(new ToolMulData { Effect = "CollectRaw", Permille = 600 });
            d["OBJ-069"] = axe;
            // furnishings: any able person may use one that is physically free (A); occupancy is physical
            d["OBJ-092"] = Seat("OBJ-092", "Bed", 12, 1, 300, 150);
            d["OBJ-097"] = Seat("OBJ-097", "Stool", 4, 1, 0, 100);
            d["OBJ-127"] = Seat("OBJ-127", "Bench", 10, 3, 0, 100);
            d["OBJ-103"] = Seat("OBJ-103", "Cradle", 8, 1, 250, 150);
            d["OBJ-107"] = O("StorageChest", 12, 0, "furnishing",
                I("deposit", "Fixture", "OBJ-107", 1, "Any", null, 1, 10, E("Deposit", 0, 30), E("NeedRelief", "Room", 80)), Uninstall("OBJ-107"));
            d["OBJ-108"] = O("CarvedOrnament", 1, 0, "furnishing",
                I("admire", "Self", null, 1, "Ware", "CarvedOrnament", 0, 30, E("NeedRelief", "Room", 150)),
                I("admire-installed", "Fixture", "OBJ-108", 1, "EmptyHand", null, 1, 30, E("NeedRelief", "Room", 150)), Uninstall("OBJ-108"));
            d["OBJ-132"] = O("Toy", 1, 0, "toy", I("play", "Self", null, 1, "Ware", "Toy", 0, 60, E("Play", 300, 0)));
            // latrine (a fixture object, G8): relieve slot; stores up to 20 Waste; Waste handling is a deposit into it
            d["OBJ-136"] = O("Latrine", 15, 0, "furnishing",
                I("relieve", "Fixture", "OBJ-136", 3, "EmptyHand", null, 1, 30, E("Relieve", "Bladder", 1000, 20)), Uninstall("OBJ-136"));
            var waste = O("Waste", 1, 0, "byproduct", I("dispose", "Fixture", "OBJ-136", 1, "Ware", "Waste", 1, 20, E("Deposit", "Waste", 20), E("NeedRelief", "Room", 200)));
            waste.ByproductOf = "OBJ-136";
            d["OBJ-114"] = waste;
            // the founder endowment only (hall fixtures): a mat needs reed (UB), so it has no recipe and appears only through the endowment (G6, G7)
            var mat = Seat("OBJ-093", "SleepingMat", 3, 1, 200, 100);
            mat.EndowmentOnly = true; mat.Role = "endowment";
            d["OBJ-093"] = mat;
            return d;
        }

        // ---------- buildings ----------
        static LevelData L(int level, int labour, int storage, int spots, int fixtures, string collect, int radius, params WareQtyData[] mats)
        {
            var l = new LevelData { Level = level, Labour = labour, Storage = storage, SleepSpots = spots, FixtureSlots = fixtures, CollectWare = collect, CollectRadius = radius };
            l.Materials.AddRange(mats);
            return l;
        }

        /// <summary>The rows that build a design: placement, upgrade, delivery of each material, labour (hand or stone hammer).</summary>
        static void Construction(BuildingDefData b, string cat, string[] materials, bool upgrades)
        {
            b.Interactions.Add(I("place", "Placement", cat, 1, "EmptyHand", null, 1, 10, E("PlaceSite", cat, 1)));
            if (upgrades)
            {
                b.Interactions.Add(I("upgrade-l2", "Building", cat, 2, "EmptyHand", null, 1, 10, E("PlaceSite", cat, 2)));
                b.Interactions.Add(I("upgrade-l3", "Building", cat, 3, "EmptyHand", null, 1, 10, E("PlaceSite", cat, 3)));
            }
            foreach (var m in materials) b.Interactions.Add(I("contribute-" + m.ToLowerInvariant(), "Site", cat, 1, "Ware", m, 1, 10, E("Contribute", m, 0)));
            b.Interactions.Add(I("labour", "Site", cat, 1, "EmptyHand", null, 1, 30, E("Labour", 0)));
            b.Interactions.Add(I("labour-tool", "Site", cat, 1, "Tool", "OBJ-068", 1, 30, E("Labour", 0)));
        }

        static BuildingDefData Shelter(string cat, string ware, string rule, string ruleArg)
        {
            var b = new BuildingDefData { Placement = rule, PlacementArg = ruleArg };
            b.Capabilities.AddRange(new[] { "Storage", "Sleep", "Fixtures", "Collect", "Shelter" });
            // D9.4 b: L1 labour only 600 ticks, storage 10, 2 sleeping spots; L2 10 wood + 300 ticks, 30, 4; L3 20 wood + 10 stone + 600 ticks, 60, 6
            b.Levels.Add(L(1, 600, 10, 2, 0, ware, 2));
            b.Levels.Add(L(2, 300, 30, 4, 2, ware, 2, Q("Log", 10)));
            b.Levels.Add(L(3, 600, 60, 6, 4, ware, 2, Q("Log", 20), Q("Stone", 10)));
            b.Interactions.Add(I("deposit", "Building", cat, 1, "Any", null, 1, 10, E("Deposit", 0, 0)));
            b.Interactions.Add(I("install", "Building", cat, 2, "Any", null, 1, 10, E("Install", 0)));
            b.Interactions.Add(I("occupy", "Building", cat, 1, "EmptyHand", null, 1, 100, E("Occupy", 0), E("NeedRelief", "Energy", 120), E("NeedRelief", "Warmth", 40), E("NeedRelief", "Comfort", 30)));
            Construction(b, cat, new[] { "Log", "Stone" }, true);
            return b;
        }

        static RecipeData R(int id, string name, int ticks, WareQtyData[] ins, params WareQtyData[] outs)
        {
            var r = new RecipeData { Id = id, Name = name, LabourTicks = ticks, ToolBonus = 0 };
            r.Inputs.AddRange(ins); r.Outputs.AddRange(outs);
            return r;
        }

        public static Dictionary<string, BuildingDefData> Buildings()
        {
            var d = new Dictionary<string, BuildingDefData>();

            // Communal hall: founder building (cost TBD, so no placement or site rows, F); storage 40, 12 sleeping spots, integral hearth (D9.5 G6)
            var hall = new BuildingDefData { Founder = true, Placement = "InSettlement" };
            hall.Capabilities.AddRange(new[] { "Storage", "Sleep", "Fixtures", "Hearth", "Shelter" });
            hall.Levels.Add(L(1, 0, 40, 12, 15, null, 0));
            hall.Endowment.AddRange(new[] { "OBJ-093 x12", "OBJ-127 x2", "OBJ-107 x1" });
            hall.Interactions.Add(I("deposit", "Building", "BLD-001", 1, "Any", null, 1, 10, E("Deposit", 0, 0)));
            hall.Interactions.Add(I("install", "Building", "BLD-001", 2, "Any", null, 1, 10, E("Install", 0)));
            hall.Interactions.Add(I("fuel", "Building", "BLD-001", 3, "Ware", "Log", 1, 10, E("Fuel", 1200, 2, 100)));
            hall.Interactions.Add(I("occupy", "Building", "BLD-001", 1, "EmptyHand", null, 1, 100, E("Occupy", 0), E("NeedRelief", "Energy", 120), E("NeedRelief", "Warmth", 40), E("NeedRelief", "Comfort", 30)));
            hall.Interactions.Add(I("cook", "Building", "BLD-001", 2, "EmptyHand", null, 1, 0, E("RecipeWork", 0)));
            hall.Recipes.Add(R(11054, "Cooked fish at the hall hearth", 60, new[] { Q("Fish", 1), Q("Log", 1) }, Q("CookedFish", 1)));
            d["BLD-001"] = hall;

            d["BLD-002"] = Shelter("BLD-002", "Fish", "Coast", null);
            d["BLD-005"] = Shelter("BLD-005", "Log", "Terrain", "Forest");
            d["BLD-006"] = Shelter("BLD-006", "Stone", "Terrain", "Mountain");
            d["BLD-051"] = Shelter("BLD-051", "Clay", "Terrain", "Hills");
            d["BLD-052"] = Shelter("BLD-052", "Sheep", "Terrain", "Pasture");
            d["BLD-053"] = Shelter("BLD-053", "Hay", "Terrain", "Fields");

            // Well: 4 wood + 300 ticks; creates a fresh-water point where it stands (UN3b)
            var well = new BuildingDefData { Placement = "InSettlement" };
            well.Capabilities.Add("Water");
            well.Levels.Add(L(1, 300, 0, 0, 0, null, 0, Q("Log", 4)));
            Construction(well, "BLD-010", new[] { "Log" }, false);
            d["BLD-010"] = well;

            // Primitive hearth: 1 stone + 120 ticks; input (slot 1), wood as fuel (slot 2), cook slot 60 ticks; warms within 2 nodes
            var hearth = new BuildingDefData { Placement = "InSettlement" };
            hearth.Capabilities.AddRange(new[] { "Workstation", "Hearth" });
            hearth.Levels.Add(L(1, 120, 0, 0, 0, null, 0, Q("Stone", 1)));
            Construction(hearth, "BLD-054", new[] { "Stone" }, false);
            hearth.Interactions.Add(I("input", "Building", "BLD-054", 1, "Any", null, 1, 10, E("WorkInput", 0)));
            hearth.Interactions.Add(I("fuel", "Building", "BLD-054", 2, "Ware", "Log", 1, 10, E("Fuel", 1200, 2, 100)));
            hearth.Interactions.Add(I("cook", "Building", "BLD-054", 3, "EmptyHand", null, 1, 0, E("RecipeWork", 0)));
            hearth.Recipes.Add(R(1054, "Cooked fish", 60, new[] { Q("Fish", 1), Q("Log", 1) }, Q("CookedFish", 1)));
            d["BLD-054"] = hearth;

            // Carpenter: 3 wood + 1 stone; every product is a separate recipe (F); the recipe is chosen by the control's design id (output CatId)
            var carp = new BuildingDefData { Placement = "InSettlement" };
            carp.Capabilities.Add("Workstation");
            carp.Levels.Add(L(1, 300, 0, 0, 0, null, 0, Q("Log", 3), Q("Stone", 1)));
            Construction(carp, "BLD-055", new[] { "Log", "Stone" }, false);
            carp.Interactions.Add(I("input", "Building", "BLD-055", 1, "Any", null, 1, 10, E("WorkInput", 0)));
            carp.Interactions.Add(I("work", "Building", "BLD-055", 3, "EmptyHand", null, 1, 0, E("RecipeWork", 0)));
            carp.Recipes.Add(R(1063, "Planks", 60, new[] { Q("Log", 1) }, Q("Plank", 2)));
            carp.Recipes.Add(R(1084, "Bedding", 60, new[] { Q("Hay", 2) }, Q("Bedding", 1)));
            carp.Recipes.Add(R(1092, "Bed", 180, new[] { Q("Log", 3), Q("Bedding", 1) }, Q("Bed", 1)));
            carp.Recipes.Add(R(1097, "Stool", 90, new[] { Q("Log", 2) }, Q("Stool", 1)));
            carp.Recipes.Add(R(1127, "Bench", 120, new[] { Q("Log", 3) }, Q("Bench", 1)));
            carp.Recipes.Add(R(1132, "Toy", 60, new[] { Q("Log", 1) }, Q("Toy", 1)));
            carp.Recipes.Add(R(1108, "Carved ornament", 90, new[] { Q("Log", 1) }, Q("CarvedOrnament", 1)));
            carp.Recipes.Add(R(1107, "Storage chest", 120, new[] { Q("Plank", 2) }, Q("StorageChest", 1)));
            carp.Recipes.Add(R(1103, "Cradle", 150, new[] { Q("Log", 2), Q("Bedding", 1) }, Q("Cradle", 1)));
            carp.Recipes.Add(R(1136, "Latrine", 180, new[] { Q("Log", 2), Q("Clay", 1) }, Q("Latrine", 1)));
            carp.Recipes.Add(R(1068, "Stone hammer", 120, new[] { Q("Log", 1), Q("Stone", 1) }, Q("StoneHammer", 1)));
            carp.Recipes.Add(R(1069, "Stone axe", 120, new[] { Q("Log", 1), Q("Stone", 1) }, Q("Axe", 1)));
            d["BLD-055"] = carp;
            return d;
        }

        // ---------- reasons and corrections of the 39 UA rows ----------
        public static readonly Dictionary<string, string> FixText = new Dictionary<string, string>
        {
            ["A"] = "A: physically free to any able person; occupancy and capacity are physical, first come first served; a founder is provenance only",
            ["C"] = "C: an offer is a gesture; a hand-over is a separate physical transfer; foods differ only by edibility",
            ["D"] = "D: no flag gates the remedy; the treated person may move away; knowledge is the brain's, not a gate",
            ["E"] = "E: explicit interaction slots; tools only change durations; raw fish gives no nutrition",
            ["F"] = "F: alternatives are separate recipes; TBD costs stay inactive; L2 and L3 numbers are configurable defaults",
            ["G"] = "G: names normalised by alias; L1 shelters have basic sleeping spots only; listed furnishings are craft options",
        };

        public static readonly Dictionary<string, string> UaReason = new Dictionary<string, string>
        {
            ["OBJ-012"] = "Raw fish from the fisher's shelter (sea dice stock); nutrition 0 until cooked at a hearth (G2)",
            ["OBJ-016"] = "Raw stone from the stonecutter's shelter (mountain dice stock); the granite mine is Later (no bootstrap path)",
            ["OBJ-024"] = "Raw wood from the woodcutter's shelter (forest dice stock); the stone axe only shortens the collection (G13)",
            ["OBJ-039"] = "Water: natural fresh-water points plus the well; drink and wash are separate slots, no lot is created (G3)",
            ["OBJ-041"] = "Raw clay from the clay gatherer's shelter (wetland dice stock; legacy terrain Hills until UN1 adds wetland)",
            ["OBJ-042"] = "Sheep from the shepherd's shelter (pasture dice stock); a unique live animal, never a stack",
            ["OBJ-043"] = "Raw hay from the hay gatherer's shelter (fields dice stock)",
            ["OBJ-044"] = "Forest forage: infinite, time-limited, edible raw",
            ["OBJ-045"] = "Mountain forage: infinite, time-limited, edible raw",
            ["OBJ-046"] = "Wetland forage: infinite, time-limited, edible raw",
            ["OBJ-047"] = "Pasture forage: three times the mushroom forage time, edible raw; the hunter's hut is UB (G9)",
            ["OBJ-048"] = "Fields forage: infinite, time-limited, edible raw",
            ["OBJ-049"] = "Desert forage: infinite, time-limited, edible raw",
            ["OBJ-050"] = "Coast forage (shore nodes only, G1): infinite, time-limited, edible raw",
            ["OBJ-054"] = "Cooked at the primitive hearth: 1 fish + 1 wood, 60 ticks (G2)",
            ["OBJ-063"] = "Carpenter recipe: 1 wood gives 2 planks; input of the chest",
            ["OBJ-068"] = "Carpenter recipe: wood + stone; construction labour multiplier 0.7; strike attack 2",
            ["OBJ-069"] = "Carpenter recipe: wood + stone; wood collection multiplier 0.6; strike attack 2",
            ["OBJ-075"] = "Environmental forage-like source on mountain and forest (G5); treats pain on a reachable person or on self",
            ["OBJ-084"] = "Carpenter recipe: 2 hay gives 1 bedding; input of the bed and the cradle",
            ["OBJ-092"] = "Carpenter recipe: 3 wood + 1 bedding; one sleeping place, free to any able person, energy and comfort relief",
            ["OBJ-097"] = "Carpenter recipe: 2 wood; one seat, comfort relief",
            ["OBJ-103"] = "Carpenter recipe: 2 wood + 1 bedding; one resting place, energy and comfort relief",
            ["OBJ-107"] = "Carpenter recipe: 2 planks; a finite container, deposit slot, room relief",
            ["OBJ-108"] = "Carpenter recipe: 1 wood; a display that gives room relief",
            ["OBJ-114"] = "By-product of the latrine; disposed of by a deposit into a latrine (G8); room relief",
            ["OBJ-127"] = "Carpenter recipe: 3 wood; three seats, comfort relief",
            ["OBJ-132"] = "Carpenter recipe: 1 wood; solo play gives fun relief; shared play needs a second person's actions",
            ["OBJ-136"] = "Carpenter recipe: 2 wood + 1 clay; stores up to 20 waste, slot relieve (G8)",
            ["BLD-001"] = "Founder building: storage 40, 12 sleeping spots, integral hearth, 15 fixture slots; later construction TBD (inactive); the endowment ledger is UN5's (G6)",
            ["BLD-002"] = "Collection shelter L1-L3 for fish; stands on a coast node (G1); 600 / 300 / 600 labour ticks, storage 10/30/60, sleeping spots 2/4/6, radius 2",
            ["BLD-005"] = "Collection shelter L1-L3 for wood; stands on forest; same numbers as the other five",
            ["BLD-006"] = "Collection shelter L1-L3 for stone; stands on mountain; same numbers as the other five",
            ["BLD-010"] = "Well: 4 wood + 300 ticks; creates a fresh-water point where it stands (G3)",
            ["BLD-051"] = "Collection shelter L1-L3 for clay; stands on wetland (legacy terrain Hills); same numbers as the other five",
            ["BLD-052"] = "Collection shelter L1-L3 for sheep; stands on pasture; same numbers as the other five",
            ["BLD-053"] = "Collection shelter L1-L3 for hay; stands on fields; same numbers as the other five",
            ["BLD-054"] = "Primitive hearth: 1 stone + 120 ticks; cooked fish only in UA (meat, charcoal, mortar, infusion, flour are UB); fuel 1 wood per 1,200 ticks, warms within 2 nodes",
            ["BLD-055"] = "Carpenter: 3 wood + 1 stone; the twelve UA products are separate recipes; the other products are UB or Later",
        };

        public static readonly Dictionary<string, string[]> Aliases = new Dictionary<string, string[]>
        {
            ["OBJ-093"] = new[] { "Bedding mat" },
            ["OBJ-091"] = new[] { "Hoop", "Ball target", "Target ball" },
            ["OBJ-105"] = new[] { "Jump platform" },
            ["OBJ-126"] = new[] { "Cool storage" },
            ["OBJ-128"] = new[] { "Fire watch" },
            ["OBJ-085"] = new[] { "Painting board", "Easel" },
            ["OBJ-099"] = new[] { "Sundial" },
            ["OBJ-112"] = new[] { "Water basin" },
        };
    }
}
