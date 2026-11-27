using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace God.Runner.Kpi
{
    /// <summary>The outcome of one KPI code: how many rows were written and, for a Missing KPI, why.</summary>
    public sealed class KpiLine
    {
        public string Code;
        public int Status;      // dim_code 'kpi_status': 0 ok, 1 source absent, 2 empty source, 3 min cell
        public string Reason;
        public int Rows;        // fact_kpi rows with a value
        public int MissingRows; // fact_kpi rows with a NULL actual_value
    }

    /// <summary>
    /// The KPI engine (Q1): after a run, copies the rows of each reference view <c>v_kpi_&lt;id&gt;</c> into <c>fact_kpi</c>
    /// (<c>actual_value</c> equals the view exactly, K-D5-1). A KPI whose view is not in this schema, or whose view returns
    /// no row, or whose cell is below the minimum, gets a row with a NULL <c>actual_value</c> and the status in
    /// <c>method_code</c>: Missing, never 0 (K-D5-5). Each series stands alone; no code is combined with another (K-D5-3).
    /// Runs after the sim, on the warehouse file; nothing in the sim reads it.
    /// </summary>
    public static class KpiEngine
    {
        public const int StatusOk = 0, StatusSourceAbsent = 1, StatusEmpty = 2, StatusMinCell = 3;

        /// <summary>The view name of a KPI code: HLT-10.median -> v_kpi_hlt10.</summary>
        public static string ViewName(string code)
        {
            int dot = code.IndexOf('.');
            string baseId = dot >= 0 ? code.Substring(0, dot) : code;
            return "v_kpi_" + baseId.Replace("-", "").ToLowerInvariant();
        }

        /// <summary>Fills fact_kpi of every run in the file (replacing earlier rows of the same runs). Needs a writable connection.</summary>
        public static List<KpiLine> Compute(SqliteConnection c)
        {
            var lines = new List<KpiLine>();
            var runs = new List<long>();
            Read(c, "SELECT run_key FROM dim_run ORDER BY run_key", r => runs.Add(r.GetInt64(0)));
            var kpis = new List<(long key, string code, int version)>();
            Read(c, "SELECT kpi_key, code, version FROM dim_kpi ORDER BY kpi_key", r => kpis.Add((r.GetInt64(0), r.GetString(1), r.GetInt32(2))));
            var views = new HashSet<string>();
            Read(c, "SELECT name FROM sqlite_master WHERE type = 'view'", r => views.Add(r.GetString(0)));
            using (var tx = c.BeginTransaction())
            {
                foreach (long run in runs)
                {
                    Exec(c, tx, "DELETE FROM fact_kpi WHERE run_key = " + run);
                    long first = 0, end = 1;
                    using (var q = c.CreateCommand())
                    {
                        q.Transaction = tx; q.CommandText = "SELECT COALESCE(MIN(date_key), 0), COALESCE(MAX(date_key) + 1, 1) FROM dim_date WHERE run_key = " + run;
                        using (var r = q.ExecuteReader()) if (r.Read()) { first = r.GetInt64(0); end = r.GetInt64(1); }
                    }
                    foreach (var (key, code, version) in kpis)
                    {
                        var line = new KpiLine { Code = code };
                        string view = ViewName(code);
                        if (!views.Contains(view))
                        {
                            line.Status = StatusSourceAbsent; line.Reason = "no view " + view + " in this schema (its source package has not merged)";
                            Insert(c, tx, run, key, version, null, first, end, null, null, StatusSourceAbsent);
                            line.MissingRows = 1;
                        }
                        else
                        {
                            var rows = new List<(long? faction, long from, long to, long? value, long? n, string why)>();
                            using (var q = c.CreateCommand())
                            {
                                q.Transaction = tx;
                                q.CommandText = "SELECT faction_key, period_from_day, period_to_day, actual_value, sample_n, missing_reason FROM " + view +
                                                " WHERE run_key = @r AND kpi_code = @c ORDER BY period_from_day, COALESCE(faction_key, -1)";
                                q.Parameters.AddWithValue("@r", run); q.Parameters.AddWithValue("@c", code);
                                using (var r = q.ExecuteReader())
                                    while (r.Read())
                                        rows.Add((r.IsDBNull(0) ? (long?)null : r.GetInt64(0), r.GetInt64(1), r.GetInt64(2), r.IsDBNull(3) ? (long?)null : r.GetInt64(3), r.IsDBNull(4) ? (long?)null : r.GetInt64(4), r.IsDBNull(5) ? null : r.GetString(5)));
                            }
                            if (rows.Count == 0)
                            {
                                line.Status = StatusEmpty; line.Reason = "the view returned no row (no eligible data in the run)";
                                Insert(c, tx, run, key, version, null, first, end, null, null, StatusEmpty);
                                line.MissingRows = 1;
                            }
                            else
                            {
                                foreach (var x in rows)
                                {
                                    bool missing = x.value == null;
                                    Insert(c, tx, run, key, version, x.faction, x.from, x.to, x.value, x.n, missing ? StatusMinCell : StatusOk);
                                    if (missing) line.MissingRows++; else line.Rows++;
                                }
                                if (line.Rows == 0) { line.Status = StatusMinCell; line.Reason = "every cell is below the minimum"; }
                            }
                        }
                        if (run == runs[0]) lines.Add(line);
                    }
                }
                tx.Commit();
            }
            return lines;
        }

        private static void Insert(SqliteConnection c, SqliteTransaction tx, long run, long kpi, int version, long? faction, long from, long to, long? value, long? n, int status)
        {
            using (var q = c.CreateCommand())
            {
                q.Transaction = tx;
                q.CommandText = "INSERT INTO fact_kpi (run_key, kpi_key, faction_key, period_from_day, period_to_day, actual_value, method_code, kpi_version, sample_n) VALUES (@r, @k, @f, @a, @b, @v, @m, @ver, @n)";
                q.Parameters.AddWithValue("@r", run); q.Parameters.AddWithValue("@k", kpi);
                q.Parameters.AddWithValue("@f", (object)faction ?? DBNull.Value); q.Parameters.AddWithValue("@a", from); q.Parameters.AddWithValue("@b", to);
                q.Parameters.AddWithValue("@v", (object)value ?? DBNull.Value); q.Parameters.AddWithValue("@m", status);
                q.Parameters.AddWithValue("@ver", version); q.Parameters.AddWithValue("@n", (object)n ?? DBNull.Value);
                q.ExecuteNonQuery();
            }
        }

        private static void Exec(SqliteConnection c, SqliteTransaction tx, string sql)
        {
            using (var q = c.CreateCommand()) { q.Transaction = tx; q.CommandText = sql; q.ExecuteNonQuery(); }
        }

        private static void Read(SqliteConnection c, string sql, Action<SqliteDataReader> row)
        {
            using (var q = c.CreateCommand()) { q.CommandText = sql; using (var r = q.ExecuteReader()) while (r.Read()) row(r); }
        }

        /// <summary>Opens the warehouse file read-write, fills fact_kpi and writes a one-line-per-KPI report.</summary>
        public static int Run(string db, TextWriter output)
        {
            if (!File.Exists(db)) throw new FileNotFoundException("no warehouse at " + db);
            List<KpiLine> lines;
            using (var c = new SqliteConnection("Data Source=" + db + ";Mode=ReadWrite;Pooling=False"))
            {
                c.Open();
                lines = Compute(c);
            }
            foreach (var l in lines)
                output.WriteLine("kpi " + l.Code.PadRight(20) + (l.Rows > 0 ? "ok     " + l.Rows + " rows" + (l.MissingRows > 0 ? ", " + l.MissingRows + " Missing" : "") : "MISSING " + l.Reason));
            return 0;
        }
    }
}
