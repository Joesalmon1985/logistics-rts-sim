using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace God.Catalogue
{
    /// <summary>Reads and writes catalogue.json deterministically (invariant, LF, UTF-8 without BOM, declaration order).</summary>
    public static class Store
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
        };

        public static readonly JsonSerializerOptions Compact = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static string ToText(CatalogueDoc doc) => JsonSerializer.Serialize(doc, Options).Replace("\r\n", "\n") + "\n";

        public static CatalogueDoc FromText(string text) => JsonSerializer.Deserialize<CatalogueDoc>(text, Options);

        public static CatalogueDoc Load(string path) => FromText(File.ReadAllText(path, Encoding.UTF8));

        public static void WriteText(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text.Replace("\r\n", "\n"), new UTF8Encoding(false));
        }

        /// <summary>Reads a text file with line endings normalised to LF (git may check out CRLF).</summary>
        public static string ReadLf(string path) => File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n");

        public static string DeepCopy(CatalogueDoc doc) => JsonSerializer.Serialize(doc, Options);
        public static CatalogueDoc Clone(CatalogueDoc doc) => JsonSerializer.Deserialize<CatalogueDoc>(JsonSerializer.Serialize(doc, Options), Options);
    }
}
