using System;
using System.Collections.Generic;
using System.Linq;

namespace God.Catalogue
{
    public enum ArgType { Unused, Int, Ware, WareOrZero, Cat, Channel, Permille, Flag }

    /// <summary>The schema facts the tool checks against (UN0 enums, contracts 23.7): kinds 1-20, target kinds, source requirements, argument types.</summary>
    public static class Spec
    {
        public static readonly string[] EffectKinds =
        {
            null, "Forage", "DrinkWash", "Eat", "NeedRelief", "CollectRaw", "Deposit", "Install", "Occupy", "WorkInput", "RecipeWork", "Fuel", "PlaceSite",
            "Contribute", "Labour", "HoldOut", "Strike", "Equip", "Treat", "Relieve", "Play",
        };

        public static readonly string[] TargetKinds = { "None", "Self", "Npc", "Lot", "Node", "Facing", "Building", "Site", "Fixture", "ForagePlace", "RawNode", "WaterPoint", "Placement" };
        public static readonly string[] SourceReqs = { "EmptyHand", "Ware", "Class", "Tool", "Any" };
        public static readonly string[] PlacementRules = { "Any", "Land", "Coast", "Terrain", "InSettlement", "Settlement", "Road" };
        public static readonly string[] Capabilities = { "Storage", "Sleep", "Fixtures", "Workstation", "Collect", "Hearth", "Water", "Sanitation", "Shelter" };

        static readonly Dictionary<string, ArgType[]> Args = new Dictionary<string, ArgType[]>
        {
            ["Forage"] = new[] { ArgType.Ware, ArgType.Int, ArgType.Unused },
            ["DrinkWash"] = new[] { ArgType.Flag, ArgType.Permille, ArgType.Unused },
            ["Eat"] = new[] { ArgType.Channel, ArgType.Unused, ArgType.Unused },
            ["NeedRelief"] = new[] { ArgType.Channel, ArgType.Permille, ArgType.Unused },
            ["CollectRaw"] = new[] { ArgType.Ware, ArgType.Int, ArgType.Cat },
            ["Deposit"] = new[] { ArgType.WareOrZero, ArgType.Int, ArgType.Unused },
            ["Install"] = new[] { ArgType.Flag, ArgType.Unused, ArgType.Unused },
            ["Occupy"] = new[] { ArgType.Int, ArgType.Unused, ArgType.Unused },
            ["WorkInput"] = new[] { ArgType.WareOrZero, ArgType.Unused, ArgType.Unused },
            ["RecipeWork"] = new[] { ArgType.Int, ArgType.Unused, ArgType.Unused },
            ["Fuel"] = new[] { ArgType.Int, ArgType.Int, ArgType.Permille },
            ["PlaceSite"] = new[] { ArgType.Cat, ArgType.Int, ArgType.Unused },
            ["Contribute"] = new[] { ArgType.Ware, ArgType.Int, ArgType.Unused },
            ["Labour"] = new[] { ArgType.Int, ArgType.Unused, ArgType.Unused },
            ["HoldOut"] = new[] { ArgType.Unused, ArgType.Unused, ArgType.Unused },
            ["Strike"] = new[] { ArgType.Int, ArgType.Permille, ArgType.Unused },
            ["Equip"] = new[] { ArgType.Int, ArgType.Int, ArgType.Unused },
            ["Treat"] = new[] { ArgType.Channel, ArgType.Permille, ArgType.Int },
            ["Relieve"] = new[] { ArgType.Channel, ArgType.Permille, ArgType.Int },
            ["Play"] = new[] { ArgType.Permille, ArgType.Permille, ArgType.Unused },
        };

        public static ArgType[] ArgTypes(string kind) => Args.TryGetValue(kind, out var a) ? a : null;

        public static int KindId(string kind) => Array.IndexOf(EffectKinds, kind);

        public static int ChannelIndex(string name) => Array.IndexOf(UaContent.Channels, name);

        /// <summary>The numeric value of an argument, or an error text. Names are resolved against wares, catalogue codes and need channels.</summary>
        public static bool Resolve(object arg, ArgType type, CatalogueDoc doc, WareTable wares, out int value, out string error)
        {
            value = 0; error = null;
            string s = null; long n = 0; bool isNum = false;
            if (arg == null) { if (type == ArgType.Unused) return true; error = "missing argument"; return false; }
            if (arg is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.Number) { n = je.GetInt64(); isNum = true; }
                else if (je.ValueKind == System.Text.Json.JsonValueKind.String) s = je.GetString();
                else { error = "bad json " + je.ValueKind; return false; }
            }
            else if (arg is string str) s = str;
            else if (arg is int i) { n = i; isNum = true; }
            else if (arg is long l) { n = l; isNum = true; }
            else { error = "bad type " + arg.GetType().Name; return false; }

            switch (type)
            {
                case ArgType.Unused: error = "argument must be unused"; return false;
                case ArgType.Int: if (!isNum || n < 0 || n > short.MaxValue) { error = "int 0-32767 expected"; return false; } value = (int)n; return true;
                case ArgType.Flag: if (!isNum || n < 0 || n > 1) { error = "flag 0/1 expected"; return false; } value = (int)n; return true;
                case ArgType.Permille: if (!isNum || n < 0 || n > 1000) { error = "permille 0-1000 expected"; return false; } value = (int)n; return true;
                case ArgType.Channel:
                    if (isNum) { if (n < 0 || n > 11) { error = "channel 0-11 expected"; return false; } value = (int)n; return true; }
                    value = ChannelIndex(s); if (value < 0) { error = "unknown channel " + s; return false; } return true;
                case ArgType.Ware:
                case ArgType.WareOrZero:
                    if (isNum) { if (n == 0 && type == ArgType.WareOrZero) { value = 0; return true; } error = "ware name expected"; return false; }
                    if (!wares.Has(s)) { error = "unknown ware " + s; return false; }
                    value = wares.Id(s); return true;
                case ArgType.Cat:
                    if (isNum) { value = (int)n; return true; }
                    value = CatOf(doc, s); if (value == 0) { error = "unknown catalogue code " + s; return false; } return true;
            }
            return false;
        }

        public static int CatOf(CatalogueDoc doc, string code)
        {
            if (code == null) return 0;
            foreach (var o in doc.Objects) if (o.Id == code) return o.CatId;
            foreach (var b in doc.Buildings) if (b.Id == code) return b.CatId;
            return 0;
        }

        /// <summary>The need channels a row relieves: effect arguments of the relief kinds (the Eat effect names Hunger, Fuel relieves Warmth).</summary>
        public static IEnumerable<int> ReliefChannels(EffectData e, CatalogueDoc doc, WareTable wares)
        {
            int v;
            switch (e.Kind)
            {
                case "DrinkWash":
                    if (Resolve(e.A, ArgType.Flag, doc, wares, out v, out _)) yield return v == 0 ? 5 : 6;
                    break;
                case "NeedRelief": case "Treat": case "Relieve": case "Eat":
                    if (Resolve(e.A, ArgType.Channel, doc, wares, out v, out _)) yield return v;
                    break;
                case "Fuel": yield return 2; break;
                case "Play": yield return 9; break;
            }
        }
    }

    /// <summary>The active rows (generated content): UA-initial implemented rows and the one endowment-only definition.</summary>
    public static class Rows
    {
        public static bool IsActive(RowBase r, object def) => def != null && (r.Status == "implemented" || (r.Status == "staged" && IsEndowment(def)));

        static bool IsEndowment(object def) => def is ObjectDefData o && o.EndowmentOnly;

        public static IEnumerable<ObjectRow> ActiveObjects(CatalogueDoc d) => d.Objects.Where(o => IsActive(o, o.Def)).OrderBy(o => o.CatId);
        public static IEnumerable<BuildingRow> ActiveBuildings(CatalogueDoc d) => d.Buildings.Where(b => IsActive(b, b.Def)).OrderBy(b => b.CatId);
    }
}
