using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace God.Runner.Warehouse
{
    /// <summary>One statement with its parameter values.</summary>
    internal struct Op
    {
        public TableDef Table;
        public object[] Values;
    }

    /// <summary>Everything one sim day (or the last partial day) adds: committed as one transaction.</summary>
    internal sealed class DayBatch
    {
        public readonly List<Op> Ops = new List<Op>(256);
        public long FirstDay = long.MaxValue, LastDay = long.MinValue;
        public int DroppedZeroSpells;
    }

    /// <summary>
    /// Opens the database file, applies the migrations and writes day batches. The migration files are the embedded
    /// <c>Warehouse/Migrations/NNNN_name.sql</c> resources, applied in order; <c>meta_schema_version</c> records each. The file
    /// is created at the current version; a file of another version is refused (the caller turns that into exit code 3).
    /// </summary>
    public sealed class WarehouseDb : IDisposable
    {
        public const string Pragmas = "journal_mode=WAL;synchronous=NORMAL;temp_store=MEMORY;cache_size=-65536";

        public SqliteConnection Conn { get; private set; }
        public string Path { get; }

        private WarehouseDb(string path, SqliteConnection c) { Path = path; Conn = c; }

        /// <summary>The newest schema version the runner knows.</summary>
        public static int CurrentVersion => Migrations().Max(m => m.version);

        /// <summary>Creates (replacing any old file) a database at <paramref name="path"/>, set up for a bulk load.</summary>
        public static WarehouseDb Create(string path, bool foreignKeys)
        {
            foreach (var f in new[] { path, path + "-wal", path + "-shm", path + "-journal" }) if (File.Exists(f)) File.Delete(f);
            var dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var c = new SqliteConnection("Data Source=" + path + ";Pooling=False");
            c.Open();
            var db = new WarehouseDb(path, c);
            db.Exec("PRAGMA page_size=8192");                       // before the first table
            foreach (var p in Pragmas.Split(';')) db.Exec("PRAGMA " + p);
            db.Exec("PRAGMA foreign_keys=" + (foreignKeys ? "ON" : "OFF"));
            db.Migrate();
            return db;
        }

        /// <summary>Opens an existing database for reading; refuses a file whose schema version this runner does not know.</summary>
        public static WarehouseDb OpenRead(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("no warehouse at " + path);
            var c = new SqliteConnection("Data Source=" + path + ";Mode=ReadOnly;Pooling=False");
            c.Open();
            var db = new WarehouseDb(path, c);
            int v = db.SchemaVersion();
            if (v < 1 || v > CurrentVersion) { db.Dispose(); throw new InvalidDataException("the warehouse " + path + " has schema version " + v + "; this runner knows up to " + CurrentVersion); }
            return db;
        }

        public int SchemaVersion()
        {
            try { return (int)(long)(Scalar("SELECT COALESCE(MAX(version), 0) FROM meta_schema_version") ?? 0L); }
            catch (SqliteException) { return 0; }
        }

        public static List<(int version, string name, string sql)> Migrations()
        {
            var asm = typeof(WarehouseDb).Assembly;
            var l = new List<(int, string, string)>();
            foreach (var n in asm.GetManifestResourceNames().OrderBy(x => x, StringComparer.Ordinal))
            {
                if (!n.StartsWith("Migrations/", StringComparison.Ordinal) || !n.EndsWith(".sql", StringComparison.Ordinal)) continue;
                string file = n.Substring("Migrations/".Length);
                int us = file.IndexOf('_');
                if (us < 1 || !int.TryParse(file.Substring(0, us), out int v)) continue;
                l.Add((v, file, ReadResource(asm, n)));
            }
            return l.OrderBy(m => m.Item1).ToList();
        }

        public static string ReadResource(Assembly asm, string name)
        {
            using (var s = asm.GetManifestResourceStream(name) ?? throw new InvalidOperationException("missing resource " + name))
            using (var r = new StreamReader(s)) return r.ReadToEnd();
        }

        private void Migrate() => ApplyMissing(Migrations());

        /// <summary>The migration versions recorded in <c>meta_schema_version</c> (empty for a new file).</summary>
        public HashSet<int> AppliedVersions()
        {
            var set = new HashSet<int>();
            try
            {
                using (var c = Conn.CreateCommand())
                {
                    c.CommandText = "SELECT version FROM meta_schema_version";
                    using (var r = c.ExecuteReader()) while (r.Read()) set.Add((int)r.GetInt64(0));
                }
            }
            catch (SqliteException) { }
            return set;
        }

        /// <summary>
        /// LEAD_RULINGS R64: applies, in number order, every migration of <paramref name="migrations"/> whose version is not
        /// yet recorded in <c>meta_schema_version</c> (set membership, not "above the current maximum": migrations merge out
        /// of number order, e.g. 0009 after 0012/0015/0026). Returns the versions applied now.
        /// </summary>
        public List<int> ApplyMissing(IEnumerable<(int version, string name, string sql)> migrations)
        {
            Exec("CREATE TABLE IF NOT EXISTS meta_schema_version (version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL, description TEXT NOT NULL)");
            var have = AppliedVersions();
            var applied = new List<int>();
            foreach (var (version, name, sql) in migrations.OrderBy(m => m.version))
            {
                if (have.Contains(version)) continue;
                applied.Add(version);
                using (var tx = Conn.BeginTransaction())
                {
                    Exec(sql, tx);
                    string first = sql.Split('\n').FirstOrDefault(l => l.StartsWith("--", StringComparison.Ordinal)) ?? name;
                    using (var cmd = Conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO meta_schema_version (version, applied_at, description) VALUES (@v, @t, @d)";
                        cmd.Parameters.AddWithValue("@v", version); cmd.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("o")); cmd.Parameters.AddWithValue("@d", first.TrimStart('-', ' '));
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
            return applied;
        }

        /// <summary>
        /// Opens (creating if absent) a database read-write and applies the migrations it lacks (R64; set membership).
        /// <paramref name="migrations"/>: the set to apply (default: every migration the runner knows; tests pass a subset).
        /// </summary>
        public static WarehouseDb OpenMigrate(string path, IEnumerable<(int version, string name, string sql)> migrations = null)
        {
            var c = new SqliteConnection("Data Source=" + path + ";Pooling=False");
            c.Open();
            var db = new WarehouseDb(path, c);
            db.LastApplied = db.ApplyMissing(migrations ?? Migrations());
            return db;
        }

        /// <summary>The versions the last <see cref="OpenMigrate"/> applied.</summary>
        public List<int> LastApplied { get; private set; } = new List<int>();

        public void Exec(string sql, SqliteTransaction tx = null)
        {
            using (var c = Conn.CreateCommand()) { c.Transaction = tx; c.CommandText = sql; c.ExecuteNonQuery(); }
        }

        public object Scalar(string sql)
        {
            using (var c = Conn.CreateCommand()) { c.CommandText = sql; var v = c.ExecuteScalar(); return v is DBNull ? null : v; }
        }

        public long Count(string table) => (long)Scalar("SELECT COUNT(*) FROM " + table);

        public List<string> TableNames()
        {
            var l = new List<string>();
            using (var c = Conn.CreateCommand())
            {
                c.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
                using (var r = c.ExecuteReader()) while (r.Read()) l.Add(r.GetString(0));
            }
            return l;
        }

        /// <summary>The indexes and ANALYZE, at the end of a load (DATA_WAREHOUSE 4.4).</summary>
        public void PostLoad()
        {
            Exec(ReadResource(typeof(WarehouseDb).Assembly, "post_load.sql"));
            Exec("ANALYZE");
            Exec("PRAGMA optimize");
        }

        public void Dispose() { Conn?.Dispose(); Conn = null; }

        // ---------------- batches ----------------

        private readonly Dictionary<TableDef, SqliteCommand> _commands = new Dictionary<TableDef, SqliteCommand>();

        /// <summary>Writes one batch in a single transaction: ops sorted by table order (dimensions first), insertion order kept inside a table.</summary>
        internal void Write(DayBatch b, Action<string, long> counted)
        {
            using (var tx = Conn.BeginTransaction())
            {
                foreach (var op in b.Ops.OrderBy(o => o.Table.Order))
                {
                    if (!_commands.TryGetValue(op.Table, out var cmd))
                    {
                        cmd = Conn.CreateCommand();
                        cmd.CommandText = op.Table.Sql;
                        for (int i = 0; i < op.Table.Columns.Length; i++) cmd.Parameters.Add(new SqliteParameter("@p" + i, null));
                        _commands[op.Table] = cmd;
                    }
                    cmd.Transaction = tx;
                    var v = op.Values;
                    if (v.Length != cmd.Parameters.Count) throw new InvalidOperationException(op.Table.Name + " takes " + cmd.Parameters.Count + " values, got " + v.Length);
                    for (int i = 0; i < v.Length; i++) cmd.Parameters[i].Value = v[i] ?? DBNull.Value;
                    cmd.ExecuteNonQuery();
                    if (op.Table.CountsAsRows) counted(op.Table.Name, 1);
                }
                tx.Commit();
            }
        }

        internal void ReleaseCommands() { foreach (var c in _commands.Values) c.Dispose(); _commands.Clear(); }
    }

    /// <summary>
    /// The writer thread (DATA_WAREHOUSE 4.2): a bounded queue of day batches (default 8 sim days); when it is full the sim
    /// thread waits, which changes speed and never results. With <c>async</c> false batches are written on the calling thread
    /// (tests, and the baseline of the overhead bench). An error on the writer thread is rethrown to the sim thread at the
    /// next call.
    /// </summary>
    internal sealed class BatchWriter : IDisposable
    {
        private readonly WarehouseDb _db;
        private readonly BlockingCollection<DayBatch> _queue;
        private readonly Thread _thread;
        private volatile Exception _error;
        private readonly object _gate = new object();
        public readonly SortedDictionary<string, long> Rows = new SortedDictionary<string, long>(StringComparer.Ordinal);
        public long QueueFullWaits;
        public double WriterSeconds;
        public int Loads;
        private readonly Stopwatch _busy = new Stopwatch();

        public BatchWriter(WarehouseDb db, bool async, int queueDays)
        {
            _db = db;
            if (!async) return;
            _queue = new BlockingCollection<DayBatch>(Math.Max(1, queueDays));
            _thread = new Thread(Loop) { IsBackground = true, Name = "warehouse-writer" };
            _thread.Start();
        }

        private void Loop()
        {
            foreach (var b in _queue.GetConsumingEnumerable())
            {
                if (_error != null) continue;
                try { WriteOne(b); } catch (Exception e) { _error = e; }
            }
        }

        private void WriteOne(DayBatch b)
        {
            _busy.Start();
            var counts = new SortedDictionary<string, long>(StringComparer.Ordinal);
            // meta_load goes in the same transaction as the day it describes; its rows_by_table is filled from this batch.
            var sw = Stopwatch.StartNew();
            _db.Write(WithLoadRow(b, counts), (t, n) => { counts.TryGetValue(t, out long v); counts[t] = v + n; });
            foreach (var kv in counts) { Rows.TryGetValue(kv.Key, out long v); Rows[kv.Key] = v + kv.Value; }
            _busy.Stop();
            lock (_gate) WriterSeconds = _busy.Elapsed.TotalSeconds;
        }

        /// <summary>Adds the meta_load row, whose rows_by_table counts this batch's inserts per table (known before writing).</summary>
        private static DayBatch WithLoadRow(DayBatch b, SortedDictionary<string, long> unused)
        {
            var counts = new SortedDictionary<string, long>(StringComparer.Ordinal);
            foreach (var op in b.Ops) if (op.Table.CountsAsRows) { counts.TryGetValue(op.Table.Name, out long v); counts[op.Table.Name] = v + 1; }
            var json = "{" + string.Join(",", counts.Select(kv => "\"" + kv.Key + "\":" + kv.Value)) + "}";
            b.Ops.Add(new Op { Table = Tables.MetaLoad, Values = new object[] { 1L, b.FirstDay == long.MaxValue ? 0L : b.FirstDay, b.LastDay == long.MinValue ? 0L : b.LastDay, json, (long)b.DroppedZeroSpells, DateTime.UtcNow.ToString("o"), 0.0 } });
            return b;
        }

        public void Submit(DayBatch b)
        {
            if (_error != null) throw new IOException("the warehouse writer failed: " + _error.Message, _error);
            Loads++;
            if (_queue == null) { WriteOne(b); return; }
            if (!_queue.TryAdd(b)) { QueueFullWaits++; _queue.Add(b); }
        }

        /// <summary>Waits until everything queued is written; rethrows a writer error.</summary>
        public void Drain()
        {
            if (_queue != null && !_queue.IsAddingCompleted)
            {
                _queue.CompleteAdding();
                _thread.Join();
            }
            if (_error != null) throw new IOException("the warehouse writer failed: " + _error.Message, _error);
        }

        public void Dispose()
        {
            if (_queue != null && !_queue.IsAddingCompleted) { _queue.CompleteAdding(); _thread.Join(); }
        }
    }
}
