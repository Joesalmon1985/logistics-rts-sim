using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using God.Train.Env;
using God.Train.Honesty;

namespace God.Train.Curriculum
{
    /// <summary>One curriculum stage data file (<c>Curriculum/S1.json</c> ... <c>S8.json</c>).</summary>
    public sealed class Stage
    {
        public string Id = "", CurriculumId = "", Content = "", Generator = "";
        public int Population, NeedMask, Days, ValidationSeeds;
        public long ValidationFrom;
        public bool StubSupported;
        public List<string> WaitsFor = new List<string>();
        public List<MeasureSpec> Criteria = new List<MeasureSpec>();
        public int DemoSeeds, BcEpochs, EsIterations, EsPairs, EsEpisodes;

        public long[] ValidationSeedList()
        {
            var a = new long[ValidationSeeds];
            for (int i = 0; i < a.Length; i++) a[i] = ValidationFrom + i;
            return a;
        }
    }

    public sealed class CriterionResult
    {
        public string Kind = ""; public string Op = ""; public double Threshold; public double Value; public bool Evaluable; public bool Met;
    }

    /// <summary>The curriculum S1-S8 (thresholds file section 5) and the stage-criteria check on validation episodes.</summary>
    public static class CurriculumData
    {
        public static readonly string[] Order = { "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8" };

        public static string Dir()
        {
            var d = Path.Combine(AppContext.BaseDirectory, "Curriculum");
            if (Directory.Exists(d)) return d;
            throw new DirectoryNotFoundException("curriculum data files not found next to the trainer");
        }

        public static Stage Load(string id, string dir = null)
        {
            var path = Path.Combine(dir ?? Dir(), id + ".json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var r = doc.RootElement;
            var s = new Stage
            {
                Id = r.GetProperty("id").GetString(), CurriculumId = r.GetProperty("curriculum").GetString(), Content = r.GetProperty("content").GetString(),
                Generator = r.GetProperty("generator").GetString(), Population = r.GetProperty("population").GetInt32(), NeedMask = r.GetProperty("needMask").GetInt32(),
                Days = r.GetProperty("days").GetInt32(), StubSupported = r.GetProperty("stubSupported").GetBoolean(),
                ValidationSeeds = r.GetProperty("validation").GetProperty("seeds").GetInt32(), ValidationFrom = r.GetProperty("validation").GetProperty("from").GetInt64(),
            };
            foreach (var x in r.GetProperty("waitsFor").EnumerateArray()) s.WaitsFor.Add(x.GetString());
            foreach (var x in r.GetProperty("criteria").EnumerateArray()) s.Criteria.Add(Thresholds.ParseMeasure(x));
            var b = r.GetProperty("budget");
            s.DemoSeeds = b.GetProperty("demoSeeds").GetInt32(); s.BcEpochs = b.GetProperty("bcEpochs").GetInt32();
            s.EsIterations = b.GetProperty("esIterations").GetInt32(); s.EsPairs = b.GetProperty("esPairs").GetInt32(); s.EsEpisodes = b.GetProperty("esEpisodes").GetInt32();
            if (s.ValidationFrom < 500000 || s.ValidationFrom + s.ValidationSeeds > SeedPolicy.RefuseFrom) throw new InvalidDataException(id + ": validation seeds must lie in 500000-899999");
            return s;
        }

        /// <summary>Evaluates the stage criteria on validation episodes. A criterion the world cannot measure is not met.</summary>
        public static List<CriterionResult> Check(Stage st, List<EpisodeStats> eps)
        {
            var res = new List<CriterionResult>();
            foreach (var c in st.Criteria)
            {
                var r = new CriterionResult { Kind = c.Kind, Op = c.Op, Threshold = c.Value, Evaluable = true };
                long horizon = 0; foreach (var e in eps) horizon = Math.Max(horizon, e.Ticks);
                switch (c.Kind)
                {
                    case "accepted_share":
                    {
                        long a = 0, n = 0; foreach (var e in eps) { a += e.Facts.Accepted; n += e.Facts.Submissions; }
                        r.Value = n == 0 ? 0 : (double)a / n; break;
                    }
                    case "mean_distinct_cells":
                    {
                        long a = 0, n = 0; foreach (var e in eps) foreach (var p in e.People) { a += p.CellsVisited; n++; }
                        r.Value = n == 0 ? 0 : (double)a / n; break;
                    }
                    case "adults_alive_share_pooled":
                    {
                        long a = 0, n = 0; foreach (var e in eps) { a += e.AdultsAliveAt(e.Ticks); n += e.AdultsAtStart(); }
                        r.Value = n == 0 ? 0 : (double)a / n; break;
                    }
                    case "pain_band_share_hunger_or_thirst":
                    case "pain_band_share_any_active":
                    {
                        long a = 0, n = 0;
                        foreach (var e in eps) foreach (var p in e.People) { a += c.Kind.EndsWith("thirst") ? p.PainTicksHungerThirst : p.PainTicksAnyActive; n += p.PersonTicks; }
                        r.Value = n == 0 ? 1 : (double)a / n; break;
                    }
                    default:
                        r.Value = SeedShare(c.Kind, eps, out bool ok); r.Evaluable = ok; break;
                }
                r.Met = r.Evaluable && c.Passes(r.Value);
                res.Add(r);
            }
            return res;
        }

        private static double SeedShare(string kind, List<EpisodeStats> eps, out bool evaluable)
        {
            evaluable = eps.Count > 0; int k = 0;
            foreach (var e in eps)
            {
                var f = e.Facts;
                bool can, hit;
                switch (kind)
                {
                    case "seeds_with_l1_collection_shelter_share": can = f.CanBuild; hit = f.FirstShelterTick >= 0; break;
                    case "seeds_with_raw_collected_deposited_share": can = f.CanCollect; hit = f.FirstRawDepositTick >= 0; break;
                    case "seeds_with_recipe_share": can = f.CanRecipe; hit = f.FirstRecipeTick >= 0; break;
                    case "seeds_with_handover_share": can = f.CanHandOver; hit = f.FirstHandoverTick >= 0; break;
                    case "seeds_with_deposit_and_withdrawal_share": can = f.CanStore; hit = f.Deposits > 0 && f.Withdrawals > 0; break;
                    default: throw new InvalidDataException("unknown criterion kind " + kind);
                }
                if (!can) evaluable = false;
                if (hit) k++;
            }
            return eps.Count == 0 ? 0 : (double)k / eps.Count;
        }
    }
}
