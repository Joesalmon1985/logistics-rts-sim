using System.Security.Cryptography;
using System.Text;

namespace God.Sim.Unified
{
    /// <summary>One field of a schema: its name and length (in int16 values).</summary>
    public readonly struct SchemaField
    {
        public readonly string Name;
        public readonly int Length;
        public SchemaField(string name, int length) { Name = name; Length = length; }
    }

    /// <summary>Shared helpers of the observation and action schemas: offsets, the canonical layout text and its SHA-256.</summary>
    public static class SchemaText
    {
        public static int Total(ReadOnlyArray<SchemaField> f) { int n = 0; foreach (var x in f) n += x.Length; return n; }

        public static int OffsetOf(ReadOnlyArray<SchemaField> f, string name)
        {
            int n = 0;
            foreach (var x in f) { if (x.Name == name) return n; n += x.Length; }
            return -1;
        }

        /// <summary>"name version scale\nfield length\n..." with "\n" line ends: the text the hash is taken of.</summary>
        public static string Canonical(string name, int version, int scale, ReadOnlyArray<SchemaField> f)
        {
            var sb = new StringBuilder();
            sb.Append(name).Append(' ').Append(version).Append(' ').Append(scale).Append('\n');
            foreach (var x in f) sb.Append(x.Name).Append(' ').Append(x.Length).Append('\n');
            return sb.ToString();
        }

        /// <summary>Lower-case hex SHA-256 of the UTF-8 text.</summary>
        public static string Sha256(string text)
        {
            using (var sha = SHA256.Create())
            {
                var b = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(64);
                foreach (var x in b) sb.Append(x.ToString("x2"));
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// UN0 (contracts 23.6, §D9.2 a): the observation schema <c>Obs v1</c>, frozen. Integers in a <c>short[]</c> of
    /// <see cref="Length"/> values, fixed-point x <see cref="Scale"/> where a field is a fraction, in exactly the field order below.
    /// Filled by UN2's <c>Percept.ToVector</c>; local information only, no true-state field, no hidden path, no owner field. A change
    /// takes a new version and a new <see cref="SchemaHash"/> and invalidates every trained artifact.
    /// <para>Groups: proprioception (facing one-hot 6, current control, progress, last result, asleep, occupying, equipped); body (12
    /// need pressures in the order of <see cref="NeedChannel"/>, health, injury, age band, 12 temperament traits); carry (4 slots x
    /// class, count, condition); the 6 neighbour cells (passable, terrain, water, forage); the 16 nearest perceived entities (sorted by
    /// distance then id: class, relative sector, distance, state bits, kin, familiarity, trust band, aggression seen); the last 4
    /// gestures heard or seen (speaker slot, act, referent class, quantity); memory (8 places: class, sector, distance; 4 people: slot,
    /// sector, distance, trust; claims summary 4); knowledge bit words (designs 5, recipes 4, interactions 8: 16 bits per value);
    /// divine inputs (4; zero when divinity is off).</para>
    /// </summary>
    public static class ObsSchema
    {
        public const string Name = "Obs";
        public const int Version = 1;
        public const int Scale = 1000;
        public const int Entities = 16, EntityFields = 8, CarrySlots = 4, CarryFields = 3, Cells = 6, CellFields = 4,
            Gestures = 4, GestureFields = 4, Places = 8, PlaceFields = 3, People = 4, PeopleFields = 4;

        public static readonly ReadOnlyArray<SchemaField> Fields = new ReadOnlyArray<SchemaField>(
            new SchemaField("facing", 6),
            new SchemaField("control", 1),
            new SchemaField("progress", 1),
            new SchemaField("last_result", 1),
            new SchemaField("asleep", 1),
            new SchemaField("occupying", 1),
            new SchemaField("equipped", 1),
            new SchemaField("needs", NeedChannels.Count),
            new SchemaField("health", 1),
            new SchemaField("injury", 1),
            new SchemaField("age_band", 1),
            new SchemaField("traits", 12),
            new SchemaField("carry", CarrySlots * CarryFields),
            new SchemaField("cells", Cells * CellFields),
            new SchemaField("entities", Entities * EntityFields),
            new SchemaField("gestures", Gestures * GestureFields),
            new SchemaField("places", Places * PlaceFields),
            new SchemaField("people", People * PeopleFields),
            new SchemaField("claims", 4),
            new SchemaField("know_designs", 5),
            new SchemaField("know_recipes", 4),
            new SchemaField("know_interactions", 8),
            new SchemaField("divine", 4));

        /// <summary>The vector length (284 in v1).</summary>
        public static readonly int Length = SchemaText.Total(Fields);

        /// <summary>The canonical layout text.</summary>
        public static readonly string Canonical = SchemaText.Canonical(Name, Version, Scale, Fields);

        /// <summary>SHA-256 of <see cref="Canonical"/> (lower-case hex): an artifact names the schema it was trained on by this.</summary>
        public static readonly string SchemaHash = SchemaText.Sha256(Canonical);

        public static int OffsetOf(string field) => SchemaText.OffsetOf(Fields, field);
    }
}
