using System;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;

namespace God.Runner.Sql
{
    /// <summary>
    /// <c>God.Runner sql --db FILE [--query TEXT | --file FILE] [--csv]</c>: one read-only SELECT over a warehouse. Never writes
    /// (SqlGuard, <c>Mode=ReadOnly</c>, <c>PRAGMA query_only</c>); the result is for people and analysts, never read by the sim
    /// (INFORMATION_AND_MEDIA 12). Exit 3 on a refused statement, a missing or foreign file or a failing query.
    /// </summary>
    public static class SqlCommand
    {
        public static int Run(Args a, TextWriter output, TextWriter error)
        {
            string db = a.Str("db") ?? throw new UsageException("--db FILE is required");
            string q = a.Str("query"), file = a.Str("file");
            bool csv = a.Flag("csv");
            a.CheckAllUsed(0);
            if ((q == null) == (file == null)) throw new UsageException("give exactly one of --query TEXT and --file FILE");
            if (file != null)
            {
                try { q = File.ReadAllText(file); } catch (Exception e) { error.WriteLine("invalid input: cannot read " + file + ": " + e.Message); return ExitCodes.InvalidInput; }
            }
            string why = SqlGuard.Check(q, out string stmt);
            if (why != null) { error.WriteLine("invalid input: the sql command is read-only: " + why); return ExitCodes.InvalidInput; }
            if (!File.Exists(db)) { error.WriteLine("invalid input: no database at " + db); return ExitCodes.InvalidInput; }
            try
            {
                using (var c = new SqliteConnection("Data Source=" + db + ";Mode=ReadOnly;Pooling=False"))
                {
                    c.Open();
                    using (var p = c.CreateCommand()) { p.CommandText = "PRAGMA query_only = ON"; p.ExecuteNonQuery(); }
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = stmt;
                        using (var r = cmd.ExecuteReader())
                        {
                            var sb = new StringBuilder();
                            for (int i = 0; i < r.FieldCount; i++) { if (i > 0) sb.Append(csv ? ',' : '\t'); sb.Append(csv ? Csv(r.GetName(i)) : r.GetName(i)); }
                            output.WriteLine(sb.ToString());
                            while (r.Read())
                            {
                                sb.Clear();
                                for (int i = 0; i < r.FieldCount; i++)
                                {
                                    if (i > 0) sb.Append(csv ? ',' : '\t');
                                    string s = Cell(r, i);
                                    sb.Append(csv ? Csv(s) : s.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' '));
                                }
                                output.WriteLine(sb.ToString());
                            }
                        }
                    }
                }
            }
            catch (SqliteException e) { error.WriteLine("invalid input: " + e.Message); return ExitCodes.InvalidInput; }
            return ExitCodes.Ok;
        }

        private static string Cell(SqliteDataReader r, int i)
        {
            if (r.IsDBNull(i)) return "";
            object v = r.GetValue(i);
            if (v is byte[] b) return "0x" + BitConverter.ToString(b).Replace("-", "");
            return Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string Csv(string s) => s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }
}
