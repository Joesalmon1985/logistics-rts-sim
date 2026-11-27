using System;
using System.Collections.Generic;
using System.Text;

namespace God.Runner.Sql
{
    /// <summary>
    /// The read-only gate of the <c>sql</c> command: exactly one statement, starting with SELECT or WITH, containing no
    /// write, schema, attach or pragma keyword outside quotes and comments. Defence in depth: the connection is also opened
    /// <c>Mode=ReadOnly</c> with <c>PRAGMA query_only = ON</c>. A false refusal (an unquoted column named like a keyword) is
    /// accepted; quote the identifier.
    /// </summary>
    public static class SqlGuard
    {
        private static readonly HashSet<string> Forbidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "ATTACH", "DETACH", "PRAGMA", "VACUUM", "REINDEX", "ANALYZE",
            "BEGIN", "COMMIT", "ROLLBACK", "SAVEPOINT", "RELEASE", "UPSERT", "LOAD_EXTENSION",
        };

        /// <summary>Returns null when the text is one read-only statement, else the reason it is refused. <paramref name="statement"/> is the text without comments and a trailing semicolon.</summary>
        public static string Check(string sql, out string statement)
        {
            statement = null;
            if (string.IsNullOrWhiteSpace(sql)) return "empty statement";
            var clean = new StringBuilder();
            var words = new List<(string w, bool call)>();
            int i = 0, n = sql.Length;
            int semis = 0; bool contentAfterSemi = false;
            while (i < n)
            {
                char c = sql[i];
                if (c == '-' && i + 1 < n && sql[i + 1] == '-') { while (i < n && sql[i] != '\n') i++; clean.Append(' '); continue; }
                if (c == '/' && i + 1 < n && sql[i + 1] == '*')
                {
                    int e = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    if (e < 0) return "unterminated comment";
                    i = e + 2; clean.Append(' '); continue;
                }
                if (c == '\'' || c == '"' || c == '`' || c == '[')
                {
                    char close = c == '[' ? ']' : c;
                    int j = i + 1;
                    while (true)
                    {
                        if (j >= n) return "unterminated quote";
                        if (sql[j] == close) { if (close != ']' && j + 1 < n && sql[j + 1] == close) { j += 2; continue; } break; }
                        j++;
                    }
                    clean.Append(sql, i, j - i + 1); i = j + 1;
                    if (semis > 0) contentAfterSemi = true;
                    continue;
                }
                if (c == ';') { semis++; clean.Append(c); i++; continue; }
                if (char.IsLetter(c) || c == '_')
                {
                    int j = i; while (j < n && (char.IsLetterOrDigit(sql[j]) || sql[j] == '_')) j++;
                    int k = j; while (k < n && char.IsWhiteSpace(sql[k])) k++;
                    words.Add((sql.Substring(i, j - i), k < n && sql[k] == '('));
                    clean.Append(sql, i, j - i); i = j;
                    if (semis > 0) contentAfterSemi = true;
                    continue;
                }
                if (!char.IsWhiteSpace(c) && semis > 0) contentAfterSemi = true;
                clean.Append(c); i++;
            }
            if (semis > 1 || contentAfterSemi) return "more than one statement";
            if (words.Count == 0) return "empty statement";
            string first = words[0].w;
            if (!first.Equals("SELECT", StringComparison.OrdinalIgnoreCase) && !first.Equals("WITH", StringComparison.OrdinalIgnoreCase)) return "only SELECT or WITH ... SELECT is allowed, not " + first.ToUpperInvariant();
            foreach (var (w, call) in words)
            {
                if (Forbidden.Contains(w)) return "'" + w.ToUpperInvariant() + "' is not allowed in a read-only query";
                if (w.Equals("REPLACE", StringComparison.OrdinalIgnoreCase) && !call) return "'REPLACE' is not allowed in a read-only query";
            }
            statement = clean.ToString().Trim().TrimEnd(';').Trim();
            return null;
        }
    }
}
