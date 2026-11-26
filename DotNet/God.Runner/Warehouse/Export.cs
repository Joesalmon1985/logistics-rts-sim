using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace God.Runner.Warehouse
{
    /// <summary>One column of a table as SQLite declares it.</summary>
    public sealed class ColumnInfo
    {
        public string Name, Type; public bool NotNull;
        public bool IsInteger => Type.IndexOf("INT", StringComparison.OrdinalIgnoreCase) >= 0;
        public bool IsReal => Type.IndexOf("REAL", StringComparison.OrdinalIgnoreCase) >= 0 || Type.IndexOf("FLOA", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Reading helpers over a finished warehouse: the content hash used for the determinism checks (an ordered dump of every
    /// table, hashed, without the wall-clock columns), and the export to CSV and to a hive-partitioned Parquet lake
    /// (DATA_WAREHOUSE 4.7). Nothing here is used by the sim or the recorder.
    /// </summary>
    public static class WarehouseExport
    {
        /// <summary>Columns that hold wall-clock time and are left out of the content hash (DATA_WAREHOUSE 10.8).</summary>
        public static readonly IReadOnlyDictionary<string, string[]> WallClock = new Dictionary<string, string[]>
        {
            ["dim_run"] = new[] { "wall_started", "wall_seconds" },
            ["meta_load"] = new[] { "loaded_at", "wall_seconds" },
            ["meta_schema_version"] = new[] { "applied_at" },
        };

        /// <summary>Tables that exist once per schema version, not per run (exported once under <c>_global</c>).</summary>
        public static readonly IReadOnlyCollection<string> GlobalTables = new[] { "dim_ware", "dim_code", "dim_kpi", "meta_event_type", "meta_event_map", "meta_schema_version" };

        public static List<ColumnInfo> Columns(WarehouseDb db, string table)
        {
            var l = new List<ColumnInfo>();
            using (var c = db.Conn.CreateCommand())
            {
                c.CommandText = "PRAGMA table_info(" + table + ")";
                using (var r = c.ExecuteReader()) while (r.Read()) l.Add(new ColumnInfo { Name = r.GetString(1), Type = r.GetString(2), NotNull = r.GetInt64(3) != 0 });
            }
            return l;
        }

        /// <summary>The names of the tables (not views), sorted.</summary>
        public static List<string> Tables(WarehouseDb db) => db.TableNames();

        /// <summary>Hash of each table's ordered content, wall-clock columns and non-deterministic run metrics excluded.</summary>
        public static SortedDictionary<string, string> TableHashes(string path)
        {
            var res = new SortedDictionary<string, string>(StringComparer.Ordinal);
            using (var db = WarehouseDb.OpenRead(path))
            {
                foreach (var t in db.TableNames())
                {
                    var cols = Columns(db, t).Where(c => !(WallClock.TryGetValue(t, out var skip) && skip.Contains(c.Name))).ToList();
                    string where = t == "fact_run_metric" ? " WHERE deterministic = 1" : "";
                    string order = string.Join(", ", Enumerable.Range(1, cols.Count));
                    using (var sha = SHA256.Create())
                    using (var c = db.Conn.CreateCommand())
                    {
                        c.CommandText = "SELECT " + string.Join(", ", cols.Select(x => x.Name)) + " FROM " + t + where + " ORDER BY " + order;
                        var sb = new StringBuilder();
                        using (var r = c.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                sb.Clear();
                                for (int i = 0; i < cols.Count; i++)
                                {
                                    if (r.IsDBNull(i)) sb.Append('~');
                                    else if (cols[i].IsReal) sb.Append(r.GetDouble(i).ToString("R", CultureInfo.InvariantCulture));
                                    else sb.Append(Convert.ToString(r.GetValue(i), CultureInfo.InvariantCulture));
                                    sb.Append('\u001f');
                                }
                                sb.Append('\n');
                                var b = Encoding.UTF8.GetBytes(sb.ToString());
                                sha.TransformBlock(b, 0, b.Length, null, 0);
                            }
                        }
                        sha.TransformFinalBlock(new byte[0], 0, 0);
                        res[t] = BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            return res;
        }

        /// <summary>One hash over every table's hash: equal for two runs with equal warehouse content.</summary>
        public static string ContentHash(string path)
        {
            var sb = new StringBuilder();
            foreach (var kv in TableHashes(path)) sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()))).Replace("-", "").ToLowerInvariant();
        }

        // ---------------- CSV ----------------

        /// <summary>Writes one <c>TABLE.csv</c> per table (header row; NULL is an empty field; text quoted when needed). Returns the rows written per table.</summary>
        public static SortedDictionary<string, long> ToCsv(string dbPath, string outDir, ICollection<string> only = null)
        {
            Directory.CreateDirectory(outDir);
            var counts = new SortedDictionary<string, long>(StringComparer.Ordinal);
            using (var db = WarehouseDb.OpenRead(dbPath))
            {
                foreach (var t in db.TableNames())
                {
                    if (only != null && only.Count > 0 && !only.Contains(t)) continue;
                    var cols = Columns(db, t);
                    long n = 0;
                    using (var w = new StreamWriter(Path.Combine(outDir, t + ".csv"), false, new UTF8Encoding(false)) { NewLine = "\n" })
                    using (var c = db.Conn.CreateCommand())
                    {
                        w.WriteLine(string.Join(",", cols.Select(x => x.Name)));
                        c.CommandText = "SELECT * FROM " + t + " ORDER BY " + string.Join(", ", Enumerable.Range(1, cols.Count));
                        using (var r = c.ExecuteReader())
                            while (r.Read())
                            {
                                var f = new string[cols.Count];
                                for (int i = 0; i < f.Length; i++)
                                    f[i] = r.IsDBNull(i) ? "" : cols[i].IsReal ? r.GetDouble(i).ToString("R", CultureInfo.InvariantCulture) : CsvQuote(Convert.ToString(r.GetValue(i), CultureInfo.InvariantCulture));
                                w.WriteLine(string.Join(",", f)); n++;
                            }
                    }
                    counts[t] = n;
                }
            }
            return counts;
        }

        private static string CsvQuote(string s) => s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

        // ---------------- Parquet ----------------

        /// <summary>
        /// Writes the hive-partitioned lake of one run (DATA_WAREHOUSE 4.7):
        /// <c>lake/TABLE/experiment=E/scenario=HASH8/seed=N/run=ID.parquet</c>, and global tables once under
        /// <c>lake/_global/TABLE/schema=V.parquet</c>. Every per-run file gets a <c>run_id</c> column. Returns the files written.
        /// </summary>
        public static List<string> ToParquet(string dbPath, string lakeDir, ICollection<string> only = null)
        {
            var files = new List<string>();
            using (var db = WarehouseDb.OpenRead(dbPath))
            {
                string runId, scenarioHash, experiment; long seed;
                using (var c = db.Conn.CreateCommand())
                {
                    c.CommandText = "SELECT run_id, scenario_hash, COALESCE(experiment_id, 'adhoc'), seed FROM dim_run ORDER BY run_key LIMIT 1";
                    using (var r = c.ExecuteReader())
                    {
                        if (!r.Read()) throw new InvalidDataException("the warehouse has no dim_run row");
                        runId = r.GetString(0); scenarioHash = r.GetString(1); experiment = r.GetString(2); seed = r.GetInt64(3);
                    }
                }
                int version = db.SchemaVersion();
                foreach (var t in db.TableNames())
                {
                    if (only != null && only.Count > 0 && !only.Contains(t)) continue;
                    bool global = GlobalTables.Contains(t);
                    string file = global
                        ? Path.Combine(lakeDir, "_global", t, "schema=" + version + ".parquet")
                        : Path.Combine(lakeDir, t, "experiment=" + experiment, "scenario=" + scenarioHash.Substring(0, Math.Min(8, scenarioHash.Length)), "seed=" + seed, "run=" + runId + ".parquet");
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    WriteParquet(db, t, file, global ? null : runId);
                    files.Add(file);
                }
            }
            return files;
        }

        private static void WriteParquet(WarehouseDb db, string table, string file, string runId)
        {
            var cols = Columns(db, table);
            var data = new List<object>[cols.Count];
            for (int i = 0; i < cols.Count; i++) data[i] = new List<object>();
            using (var c = db.Conn.CreateCommand())
            {
                c.CommandText = "SELECT * FROM " + table + " ORDER BY " + string.Join(", ", Enumerable.Range(1, cols.Count));
                using (var r = c.ExecuteReader())
                    while (r.Read())
                        for (int i = 0; i < cols.Count; i++) data[i].Add(r.IsDBNull(i) ? null : cols[i].IsInteger ? (object)r.GetInt64(i) : cols[i].IsReal ? r.GetDouble(i) : r.GetString(i));
            }
            int rows = data.Length > 0 ? data[0].Count : 0;
            var fields = new List<DataField>(); var writes = new List<Func<ParquetRowGroupWriter, DataField, Task>>();
            for (int i = 0; i < cols.Count; i++)
            {
                var d = data[i];
                if (cols[i].IsInteger) { fields.Add(new DataField<long?>(cols[i].Name)); var a = d.Select(x => (long?)x).ToArray(); writes.Add((g, f) => g.WriteAsync<long>(f, new ReadOnlyMemory<long?>(a))); }
                else if (cols[i].IsReal) { fields.Add(new DataField<double?>(cols[i].Name)); var a = d.Select(x => (double?)x).ToArray(); writes.Add((g, f) => g.WriteAsync<double>(f, new ReadOnlyMemory<double?>(a))); }
                else { fields.Add(new DataField<string>(cols[i].Name)); var a = d.Select(x => (string)x).ToArray(); writes.Add((g, f) => g.WriteAsync(f, a)); }
            }
            if (runId != null)
            {
                fields.Add(new DataField<string>("run_id"));
                var a = Enumerable.Repeat(runId, rows).ToArray();
                writes.Add((g, f) => g.WriteAsync(f, a));
            }
            var schema = new ParquetSchema(fields.Cast<Field>().ToList());
            using (var fs = File.Create(file))
            {
                var w = ParquetWriter.CreateAsync(schema, fs).GetAwaiter().GetResult();
                try
                {
                    using (var g = w.CreateRowGroup())
                        for (int i = 0; i < fields.Count; i++) writes[i](g, fields[i]).GetAwaiter().GetResult();
                }
                finally { w.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
            }
        }

        /// <summary>Row count and the sum of every integer column of a Parquet file (the round-trip check).</summary>
        public static (long rows, SortedDictionary<string, long> sums) ReadParquetTotals(string file)
        {
            var sums = new SortedDictionary<string, long>(StringComparer.Ordinal);
            long rows = 0;
            using (var fs = File.OpenRead(file))
            {
                var rd = ParquetReader.CreateAsync(fs).GetAwaiter().GetResult();
                try
                {
                    for (int g = 0; g < rd.RowGroupCount; g++)
                        using (var rg = rd.OpenRowGroupReader(g))
                        {
                            rows += rg.RowCount;
                            foreach (var f in rd.Schema.DataFields)
                            {
                                if (f.ClrType != typeof(long)) continue;
                                var buf = new long?[rg.RowCount];
                                rg.ReadAsync<long>(f, new Memory<long?>(buf)).GetAwaiter().GetResult();
                                long s = 0; foreach (var v in buf) s += v ?? 0;
                                sums.TryGetValue(f.Name, out long old); sums[f.Name] = old + s;
                            }
                        }
                }
                finally { rd.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
            }
            return (rows, sums);
        }
    }
}
