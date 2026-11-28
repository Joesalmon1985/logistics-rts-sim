using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace God.Catalogue
{
    /// <summary>One free-text note (conventions, defaults); a list keeps the file order stable.</summary>
    public sealed class Note { public string Key { get; set; } public string Text { get; set; } }

    /// <summary>catalogue.json: the reviewed, versioned content source (UN3a). The workbook is the authoring backlog.</summary>
    public sealed class CatalogueDoc
    {
        public string Format { get; set; }
        public string WorkbookFile { get; set; }
        public string WorkbookSha256 { get; set; }
        public string Manifest { get; set; }
        public List<Note> Conventions { get; set; } = new List<Note>();
        public List<Note> Defaults { get; set; } = new List<Note>();
        public List<ControlRow> Controls { get; set; } = new List<ControlRow>();
        public List<NeedRow> Needs { get; set; } = new List<NeedRow>();
        public List<ObjectRow> Objects { get; set; } = new List<ObjectRow>();
        public List<BuildingRow> Buildings { get; set; } = new List<BuildingRow>();
        public List<WareRequest> WareRequests { get; set; } = new List<WareRequest>();
    }

    public class RowBase
    {
        public string Id { get; set; }
        public int CatId { get; set; }
        public string Name { get; set; }
        public List<string> Aliases { get; set; } = new List<string>();
        public int WorkbookRow { get; set; }
        public string WorkbookSha256 { get; set; }
        public string Stage { get; set; }
        public List<string> Fix { get; set; } = new List<string>();
        public List<string> FixApplied { get; set; } = new List<string>();
        public List<string> Corrections { get; set; } = new List<string>();
        public string Status { get; set; }
        public string Reason { get; set; }
        /// <summary>The workbook's own cell text (superseded wording stays here for traceability; never active content).</summary>
        [JsonPropertyOrder(60)] public Dictionary<string, string> Workbook { get; set; } = new Dictionary<string, string>();
    }

    public sealed class ControlRow : RowBase { [JsonPropertyOrder(1)] public int Prim { get; set; } }
    public sealed class NeedRow : RowBase { [JsonPropertyOrder(1)] public int Channel { get; set; } [JsonPropertyOrder(2)] public string ReliefSource { get; set; } }
    public sealed class ObjectRow : RowBase { [JsonPropertyOrder(50)] public ObjectDefData Def { get; set; } }
    public sealed class BuildingRow : RowBase { [JsonPropertyOrder(50)] public BuildingDefData Def { get; set; } }

    public sealed class WareRequest
    {
        public string ObjectId { get; set; }
        public string Name { get; set; }
        /// <summary>The proposed ware id in the 94-159 block, or the existing id when "reuse" is true.</summary>
        public int ProposedId { get; set; }
        public bool Reuse { get; set; }
        public string ExistingWare { get; set; }
    }

    public sealed class ObjectDefData
    {
        public string Ware { get; set; }
        public int Weight { get; set; }
        public int Nutrition { get; set; }
        public int StrikeAttack { get; set; }
        public int Defence { get; set; }
        public string Role { get; set; }
        public string ByproductOf { get; set; }
        public bool EndowmentOnly { get; set; }
        public List<ToolMulData> ToolMultipliers { get; set; } = new List<ToolMulData>();
        public List<InteractionData> Interactions { get; set; } = new List<InteractionData>();
    }

    public sealed class ToolMulData { public string Effect { get; set; } public int Permille { get; set; } }

    public sealed class InteractionData
    {
        public string Name { get; set; }
        public string Target { get; set; }
        public string TargetCat { get; set; }
        public int Slot { get; set; }
        public string Source { get; set; }
        public object SourceArg { get; set; }
        public int Reach { get; set; }
        public int Ticks { get; set; }
        public List<EffectData> Effects { get; set; } = new List<EffectData>();
    }

    public sealed class EffectData
    {
        public string Kind { get; set; }
        public object A { get; set; }
        public object B { get; set; }
        public object C { get; set; }
    }

    public sealed class WareQtyData { public string Ware { get; set; } public int Count { get; set; } }

    public sealed class LevelData
    {
        public int Level { get; set; }
        public int Labour { get; set; }
        public List<WareQtyData> Materials { get; set; } = new List<WareQtyData>();
        public int Storage { get; set; }
        public int SleepSpots { get; set; }
        public int FixtureSlots { get; set; }
        public string CollectWare { get; set; }
        public int CollectRadius { get; set; }
    }

    public sealed class RecipeData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<WareQtyData> Inputs { get; set; } = new List<WareQtyData>();
        public List<WareQtyData> Outputs { get; set; } = new List<WareQtyData>();
        public int LabourTicks { get; set; }
        public int ToolBonus { get; set; }
    }

    public sealed class BuildingDefData
    {
        public bool Founder { get; set; }
        public string Placement { get; set; }
        public string PlacementArg { get; set; }
        public List<string> Capabilities { get; set; } = new List<string>();
        public List<LevelData> Levels { get; set; } = new List<LevelData>();
        public List<InteractionData> Interactions { get; set; } = new List<InteractionData>();
        public List<RecipeData> Recipes { get; set; } = new List<RecipeData>();
        public List<string> Endowment { get; set; } = new List<string>();
    }
}
