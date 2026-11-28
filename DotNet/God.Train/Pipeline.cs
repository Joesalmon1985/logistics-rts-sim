using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using God.Train.Curriculum;
using God.Train.Env;
using God.Train.Honesty;
using God.Train.Learn;
using God.Train.Net;
using God.Train.Teacher;

namespace God.Train
{
    /// <summary>The steps the commands share (demonstrations, behaviour cloning, ES, validation), usable from tests.</summary>
    public static class Pipeline
    {
        /// <summary>A training-side stub world of a stage (train layout, trainer-visible seeds only).</summary>
        public static StubWorld Stub(Stage st, long seed, StubConfig baseCfg = null)
        {
            var b = baseCfg ?? new StubConfig();
            var cfg = new StubConfig
            {
                Radius = b.Radius, Persons = st.Population, NeedMask = st.NeedMask, TicksPerDay = b.TicksPerDay, Forages = b.Forages, Waters = b.Waters,
                Rocks = b.Rocks, Sight = b.Sight, PainBand = b.PainBand, HungerPerTick = b.HungerPerTick, ThirstPerTick = b.ThirstPerTick, EnergyPerTick = b.EnergyPerTick,
            };
            return new StubWorld(cfg, seed, Layout.Train, null);
        }

        public static long Ticks(Stage st, StubConfig cfg = null) => (long)st.Days * (cfg ?? new StubConfig()).TicksPerDay;

        /// <summary>Teacher demonstrations on training seeds (labelled <c>Teacher</c>), parallel by seed, merged in seed order.</summary>
        public static List<Sequence> Demos(Stage st, long[] seeds, int workers, long ticks = -1)
        {
            SeedPolicy.CheckTraining(seeds);
            var per = new List<Sequence>[seeds.Length];
            long n = ticks > 0 ? ticks : Ticks(st);
            Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, workers) }, i =>
            {
                var rec = new List<Sequence>();
                Rollout.Run(Stub(st, seeds[i]), new StubTeacher(), seeds[i], n, evaluation: false, record: rec);
                per[i] = rec;
            });
            var all = new List<Sequence>();
            foreach (var p in per) all.AddRange(p);
            return all;
        }

        /// <summary>Validation episodes of a stage with a network (stochastic, the game's sampling), parallel, in seed order.</summary>
        public static List<EpisodeStats> Validate(Stage st, QNet net, int workers, long[] seeds = null)
        {
            var vs = seeds ?? st.ValidationSeedList();
            var res = new EpisodeStats[vs.Length];
            Parallel.For(0, vs.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, workers) }, i =>
                res[i] = Rollout.Run(Stub(st, vs[i]), new NetActor(net), vs[i], Ticks(st), evaluation: false));
            return new List<EpisodeStats>(res);
        }

        public static string CriteriaJson(Stage st, List<CriterionResult> cr, double agreement = double.NaN, long meanReturn = 0)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder("{\"stage\":\"").Append(st.Id).Append("\",\"criteria\":[");
            bool all = true;
            for (int i = 0; i < cr.Count; i++)
            {
                var c = cr[i]; all &= c.Met;
                sb.Append(i > 0 ? "," : "").Append("{\"kind\":\"").Append(c.Kind).Append("\",\"value\":").Append(c.Value.ToString("0.0000", inv))
                  .Append(",\"op\":\"").Append(c.Op).Append("\",\"threshold\":").Append(c.Threshold.ToString("0.0000", inv)).Append(",\"met\":").Append(c.Met ? "true" : "false").Append('}');
            }
            sb.Append("],\"stageCriteria\":\"").Append(all ? "met" : "criteria not met").Append('"');
            if (!double.IsNaN(agreement)) sb.Append(",\"bcAgreement\":").Append(agreement.ToString("0.0000", inv));
            sb.Append(",\"validationMeanReturn\":").Append(meanReturn);
            sb.Append(",\"world\":\"stub\",\"label\":\"HARNESS SELF-TEST, not a result\"}");
            return sb.ToString();
        }

        public static long MeanReturn(List<EpisodeStats> eps) { long s = 0; foreach (var e in eps) s += e.TotalReturn; return eps.Count == 0 ? 0 : s / eps.Count; }
    }
}
