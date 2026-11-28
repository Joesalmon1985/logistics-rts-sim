using System;
using System.Reflection;
using System.Text;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN6 (§D9.2 b, contracts 23.14): a founder policy artifact as the game holds it: the header written by UN6t's
    /// <c>god-train artifact</c> (the loader contract proposed in UN6t-a's thresholds commit <c>b441f57</c> and agreed by UN6) and the
    /// int16 weights. Weights are never saved with a game; the version and <see cref="WeightsSha256"/> are.
    /// </summary>
    public sealed class BrainArtifact
    {
        public int Version;
        /// <summary>"TRAINED", "FAILED-TR01", "INTERIM" or "SELF-TEST". Only "TRAINED" may ever be reported as a trained brain.</summary>
        public string Status = "";
        public int ObsSchemaVersion, ActSchemaVersion;
        public string ObsSchemaHash = "", ActSchemaHash = "", CatalogueHash = "", TuningHash = "", CurriculumId = "", StagesTrained = "",
            StagesNotTrained = "", TrainingSeeds = "", TrainerCommit = "", ThresholdsHash = "", TrainingMetricsJson = "", NetShape = "", WeightsSha256 = "";
        public int WeightScaleShift, WeightCount;
        public short[] Weights;

        public const string StatusTrained = "TRAINED";
        public bool StatusIsTrained => Status == StatusTrained;

        /// <summary>A deep copy (tests mutate copies to check refusal).</summary>
        public BrainArtifact Copy()
        {
            var c = (BrainArtifact)MemberwiseClone();
            c.Weights = Weights == null ? null : (short[])Weights.Clone();
            return c;
        }
    }

    /// <summary>
    /// UN6 (§D9.2 b, spec 5.2): finds and verifies the generated artifact class <c>God.Sim.Unified.FounderPolicyV&lt;n&gt;</c>
    /// (<c>Unified/Brain/Artifacts/FounderPolicyV&lt;n&gt;.g.cs</c>, written by UN6t-b). The class's constants are read by reflection
    /// once, when the game is created or loaded (never per decision); the weight chunks are the constants <c>Chunk0..ChunkK</c> joined
    /// in order (so a generated artifact needs no mutable static array). Verification: the SHA-256 of the weights, the weight count
    /// against the shape, the <c>Obs</c>/<c>Act</c> schema versions and hashes, and the catalogue hash. With <c>Brain.Kind = 1</c> any
    /// failure refuses to start the game (<see cref="BrainRefused"/>); there is no silent downgrade.
    /// <para>Versions <see cref="TestFirst"/>-<see cref="TestLast"/> are test artifacts: they are also looked up in the other loaded
    /// assemblies (UN6's tests compile their tiny generated artifacts there) and can never carry the status "TRAINED".</para>
    /// </summary>
    public static class ArtifactLoader
    {
        public const string Namespace = "God.Sim.Unified";
        public const int TestFirst = 9000, TestLast = 9999;

        public static string ClassName(int version) => "FounderPolicyV" + version.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>The catalogue content id an artifact must name (UN3a's <see cref="Catalogue.WorkbookSha"/>, "empty-catalogue" while empty).</summary>
        public static string CurrentCatalogueHash() => string.IsNullOrEmpty(Catalogue.WorkbookSha) ? "empty-catalogue" : Catalogue.WorkbookSha;

        /// <summary>The artifact class of <paramref name="version"/>, or null.</summary>
        public static Type Find(int version)
        {
            string name = Namespace + "." + ClassName(version);
            var t = typeof(ArtifactLoader).Assembly.GetType(name, false);
            if (t != null || version < TestFirst || version > TestLast) return t;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a == typeof(ArtifactLoader).Assembly) continue;
                t = a.GetType(name, false);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>Reads an artifact class's header and weights (null and a reason when it is malformed or missing).</summary>
        public static BrainArtifact Read(int version, out string error)
        {
            error = null;
            var t = Find(version);
            if (t == null) { error = "artifact " + ClassName(version) + " not found"; return null; }
            try
            {
                var a = new BrainArtifact
                {
                    Version = Int(t, "Version"), Status = Str(t, "Status"),
                    ObsSchemaVersion = Int(t, "ObsSchemaVersion"), ObsSchemaHash = Str(t, "ObsSchemaHash"),
                    ActSchemaVersion = Int(t, "ActSchemaVersion"), ActSchemaHash = Str(t, "ActSchemaHash"),
                    CatalogueHash = Str(t, "CatalogueHash"), TuningHash = Str(t, "TuningHash"), CurriculumId = Str(t, "CurriculumId"),
                    StagesTrained = Str(t, "StagesTrained"), StagesNotTrained = Str(t, "StagesNotTrained"), TrainingSeeds = Str(t, "TrainingSeeds"),
                    TrainerCommit = Str(t, "TrainerCommit"), ThresholdsHash = Str(t, "ThresholdsHash"), TrainingMetricsJson = Str(t, "TrainingMetricsJson"),
                    NetShape = Str(t, "NetShape"), WeightScaleShift = Int(t, "WeightScaleShift"), WeightCount = Int(t, "WeightCount"),
                    WeightsSha256 = Str(t, "WeightsSha256"),
                };
                var b64 = new StringBuilder();
                for (int i = 0; ; i++)
                {
                    var f = t.GetField("Chunk" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f == null) break;
                    b64.Append((string)(f.IsLiteral ? f.GetRawConstantValue() : f.GetValue(null)));
                }
                a.Weights = BrainNet.FromBytes(Convert.FromBase64String(b64.ToString()));
                if (a.Version != version) { error = "artifact " + ClassName(version) + " names version " + a.Version; return null; }
                return a;
            }
            catch (Exception e) when (e is FormatException || e is InvalidCastException || e is ArgumentException || e is MissingFieldException)
            {
                error = "artifact " + ClassName(version) + " malformed: " + e.Message;
                return null;
            }
        }

        private static FieldInfo Field(Type t, string name)
        {
            var f = t.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) throw new MissingFieldException(t.Name, name);
            return f;
        }

        private static object Value(Type t, string name) { var f = Field(t, name); return f.IsLiteral ? f.GetRawConstantValue() : f.GetValue(null); }
        private static int Int(Type t, string name) => (int)Value(t, name);
        private static string Str(Type t, string name) => (string)Value(t, name) ?? "";

        /// <summary>
        /// Null when the artifact may run against the current schemas and catalogue; else the reason it may not (changed schema,
        /// changed catalogue, malformed shape, weight count or weight hash mismatch, a test artifact claiming "TRAINED").
        /// </summary>
        public static string Verify(BrainArtifact a, string catalogueHash)
        {
            if (a == null) return "no artifact";
            if (a.ObsSchemaVersion != ObsSchema.Version || a.ObsSchemaHash != ObsSchema.SchemaHash) return "Obs schema changed";
            if (a.ActSchemaVersion != ActSchema.Version || a.ActSchemaHash != ActSchema.SchemaHash) return "Act schema changed";
            if (catalogueHash != null && a.CatalogueHash != catalogueHash) return "catalogue changed";
            if (a.WeightScaleShift != BrainNet.WeightShift) return "weight scale shift " + a.WeightScaleShift + " is not " + BrainNet.WeightShift;
            BrainShape shape;
            try { shape = BrainShape.Parse(a.NetShape); }
            catch (Exception e) when (e is FormatException || e is ArgumentException) { return "net shape: " + e.Message; }
            if (a.Weights == null || a.Weights.Length != a.WeightCount || shape.Count != a.Weights.Length) return "weight count mismatch";
            if (BrainNet.WeightsSha256(a.Weights) != a.WeightsSha256) return "weights hash mismatch";
            if (a.Version >= TestFirst && a.Version <= TestLast && a.StatusIsTrained) return "a test artifact cannot be TRAINED";
            return null;
        }

        /// <summary>Reads and verifies <paramref name="version"/>; throws <see cref="BrainRefused"/> on any failure (Kind = 1 only).</summary>
        public static BrainArtifact LoadVerified(int version)
        {
            var a = Read(version, out var error);
            if (a != null) error = Verify(a, CurrentCatalogueHash());
            if (error != null) throw new BrainRefused("Brain.Kind = 1 (Trained) refused to start: " + error + ". No silent downgrade: set Unification.Brain.Kind to 0 (Untrained) or 2 (Fallback, labelled) to run without a verified artifact.");
            return a;
        }
    }

    /// <summary>A trained brain was asked for and its artifact did not verify: the game does not start (TR-01 honesty).</summary>
    public sealed class BrainRefused : InvalidOperationException
    {
        public BrainRefused(string message) : base(message) { }
    }
}
