using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// <c>batch.sqlite</c> (HEADLESS_SIMULATION 4.6): the batch's index, merged at the end in seed order so finishing order never
    /// shows. It holds <c>dim_run</c> (run_key = position in the batch, 1 up) and <c>fact_run_metric</c> of every run that wrote
    /// a <c>warehouse.sqlite</c>; the rest of the schema is present but empty (same migrations). Per-run facts stay in the
    /// per-run files, so there is no write contention between runs.
    /// </summary>
    public static class BatchIndex
    {
        public static int Write(string path, IList<string> runDirs)
        {
            int merged = 0;
            using (var db = WarehouseDb.Create(path, false))
            {
                foreach (var dir in runDirs)
                {
                    string file = dir == null ? null : Path.Combine(dir, "warehouse.sqlite");
                    if (file == null || !File.Exists(file)) continue;
                    int key = merged + 1;
                    string q = file.Replace("'", "''");
                    db.Exec("ATTACH DATABASE '" + q + "' AS r");
                    try
                    {
                        var cols = WarehouseExport.Columns(db, "dim_run").Select(c => c.Name).Where(n => n != "run_key").ToList();
                        string list = string.Join(", ", cols);
                        db.Exec("INSERT INTO main.dim_run (run_key, " + list + ") SELECT " + key + ", " + list + " FROM r.dim_run");
                        db.Exec("INSERT INTO main.fact_run_metric (run_key, metric, source, value, deterministic) SELECT " + key + ", metric, source, value, deterministic FROM r.fact_run_metric ");
                    }
                    finally { db.Exec("DETACH DATABASE r"); }
                    merged++;
                }
                db.Exec("PRAGMA wal_checkpoint(TRUNCATE)");
                db.Exec("PRAGMA journal_mode=DELETE");
            }
            return merged;
        }
    }
}
