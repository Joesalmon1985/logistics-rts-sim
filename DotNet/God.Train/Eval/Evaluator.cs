using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using God.Train.Env;
using God.Train.Honesty;

namespace God.Train.Eval
{
    /// <summary>One arm to evaluate: its name, what brain it is, and a fresh actor per episode.</summary>
    public sealed class ArmSpec
    {
        public string Name = "";
        public string Brain = "";
        public string WeightsSha256 = "";
        public string ArtifactStatus = "";
        public Func<IActor> Actor;
    }

    public sealed class EvalSetup
    {
        /// <summary>"stub" (harness self-test) or "unified".</summary>
        public string World = "stub";
        public long[] Seeds;
        public int NeedMask = 0x033;
        public int Workers = 1;
        public StubConfig Stub = new StubConfig();
        public List<ArmSpec> Arms = new List<ArmSpec>();
    }

    /// <summary>
    /// The held-out evaluation (thresholds file sections 1-4): refuses to run unless the given thresholds hash equals the committed
    /// file's; teacher-free (a teacher submission aborts the evaluation, which is marked invalid), divinity off, no path oracle,
    /// the training need mask; held-out layouts; computes T1-T7 exactly as the file defines them and writes the hash into the
    /// result. On the stub world it runs only the self-test seeds and labels everything HARNESS SELF-TEST.
    /// </summary>
    public static class Evaluator
    {
        public const string SelfTestLabel = "HARNESS SELF-TEST, not a result";

        public static EvalAccess Authorise(Thresholds t, string givenHash)
        {
            if (string.IsNullOrEmpty(givenHash) || !string.Equals(givenHash.Trim(), t.Hash, StringComparison.Ordinal))
                throw new ThresholdsHashMismatchException(givenHash ?? "", t.Hash);
            return new EvalAccess(t.Hash);
        }

        public static EvalResults Run(Thresholds t, string givenHash, EvalSetup setup)
        {
            var access = Authorise(t, givenHash);
            var m1 = t.Measure("T1");
            int days = 0;
            foreach (var id in new[] { "T1", "T2", "T3", "T4", "T7" }) days = Math.Max(days, t.Measure(id).Days);
            var res = new EvalResults
            {
                ThresholdsHash = t.Hash, ThresholdsVersion = t.Version, World = setup.World, NeedMask = setup.NeedMask,
                Population = m1.Population, Days = days, Gate = new List<string>(t.Gate),
            };
            if (setup.World == "stub")
            {
                foreach (var s in setup.Seeds)
                    if (s < t.SelfTestSeeds[0] || s > t.SelfTestSeeds[1])
                        throw new HeldOutRefusedException($"seed {s} on the stub world (the stub runs only the self-test seeds {t.SelfTestSeeds[0]}-{t.SelfTestSeeds[1]}; held-out seeds are for the real world)");
                res.SelfTest = true; res.Label = SelfTestLabel;
            }
            else
            {
                var why = UnifiedWorld.Unavailable(1);
                if (why != null) throw new InvalidOperationException("the unified world cannot be evaluated yet: " + why);
                if (setup.Seeds.Length != t.HeldOutSeeds[1] - t.HeldOutSeeds[0] + 1 || setup.Seeds[0] != t.HeldOutSeeds[0])
                    throw new InvalidOperationException($"a held-out evaluation runs exactly the seeds {t.HeldOutSeeds[0]}-{t.HeldOutSeeds[1]} (no subset, no cherry-picking)");
                res.Label = "held-out evaluation";
            }
            res.Seeds.AddRange(setup.Seeds);

            IWorld Make(long seed)
            {
                var cfg = new StubConfig
                {
                    Radius = setup.Stub.Radius, Persons = m1.Population, NeedMask = setup.NeedMask, TicksPerDay = setup.Stub.TicksPerDay,
                    Forages = setup.Stub.Forages, Waters = setup.Stub.Waters, Rocks = setup.Stub.Rocks, Sight = setup.Stub.Sight, PainBand = t.PainBandDefault,
                };
                return new StubWorld(cfg, seed, Layouts.ForEvalSeed(seed), access);
            }

            int tpd = setup.Stub.TicksPerDay;
            res.TicksPerDay = tpd; res.PainBand = t.PainBandDefault;
            var jobs = new List<(int arm, int seed)>();
            for (int a = 0; a < setup.Arms.Count; a++) for (int s = 0; s < setup.Seeds.Length; s++) jobs.Add((a, s));
            var stats = new EpisodeStats[jobs.Count];
            var sw = Stopwatch.StartNew();
            string invalid = null;
            try
            {
                Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, setup.Workers) }, j =>
                {
                    var (a, s) = jobs[j];
                    long seed = setup.Seeds[s];
                    var w = Make(seed);
                    var st = Rollout.Run(w, setup.Arms[a].Actor(), seed, (long)days * tpd, evaluation: true);
                    st.Layout = Layouts.Name(Layouts.ForEvalSeed(seed));
                    stats[j] = st;
                });
            }
            catch (AggregateException e) when (e.InnerException is EvaluationInvalidException)
            {
                invalid = e.InnerException.Message;
            }
            catch (EvaluationInvalidException e) { invalid = e.Message; }
            res.RolloutSecondsTotal = sw.Elapsed.TotalSeconds;
            if (invalid != null)
            {
                res.Invalid = true; res.InvalidReason = invalid;
                res.Verdict = Verdict.Compute(res, t);
                throw new EvaluationAbortedException(res);
            }
            long ticksAll = 0;
            for (int a = 0; a < setup.Arms.Count; a++)
            {
                var spec = setup.Arms[a];
                var arm = new ArmResult { Name = spec.Name, Brain = spec.Brain, WeightsSha256 = spec.WeightsSha256, ArtifactStatus = spec.ArtifactStatus };
                var eps = new List<EpisodeStats>();
                for (int s = 0; s < setup.Seeds.Length; s++)
                {
                    var st = stats[a * setup.Seeds.Length + s];
                    eps.Add(st);
                    ticksAll += st.Ticks;
                    var sr = new SeedResult
                    {
                        Seed = st.Seed, Layout = st.Layout, Adults0 = st.AdultsAtStart(), AdultsAlive = st.AdultsAliveAt((long)m1.Days * tpd),
                        ShelterTick = st.Facts.FirstShelterTick, RawDepositTick = st.Facts.FirstRawDepositTick, HandoverTick = st.Facts.FirstHandoverTick,
                        Return = st.TotalReturn, Decisions = st.Decisions, TeacherSubmissions = st.TeacherSubmissions, FinalHash = st.FinalHash.ToString("x16"),
                    };
                    foreach (var p in st.People) { sr.PersonTicks += p.PersonTicks; sr.PainTicksHungerThirst += p.PainTicksHungerThirst; }
                    arm.PerSeed.Add(sr);
                }
                arm.Measures = Measures(t, eps, tpd, setup.World == "stub");
                res.Arms.Add(arm);
            }
            res.TicksPerSecondPerWorker = res.RolloutSecondsTotal > 0 ? ticksAll / res.RolloutSecondsTotal / Math.Max(1, Math.Min(setup.Workers, jobs.Count)) : 0;
            // T1's comparison with the untrained arm
            var tr = res.Arm("trained"); var un = res.Arm("untrained");
            if (tr != null)
            {
                var t1 = tr.Measures.Find(x => x.Id == "T1");
                if (un == null) { t1.Met = false; t1.Note = "no untrained arm: the comparison of T1 cannot be made"; }
                else
                {
                    var u1 = un.Measures.Find(x => x.Id == "T1");
                    bool above = t1.Value - u1.Value >= m1.MinAboveUntrained - 1e-12;
                    t1.Note = $"trained - untrained = {t1.Value - u1.Value:0.000} (required >= {m1.MinAboveUntrained:0.00})";
                    t1.Met = t1.Met && above;
                }
            }
            res.Verdict = Verdict.Compute(res, t);
            return res;
        }

        /// <summary>T1-T7 for one arm from its episodes (thresholds file section 2).</summary>
        public static List<MeasureResult> Measures(Thresholds t, List<EpisodeStats> eps, int tpd, bool stub)
        {
            var list = new List<MeasureResult>();
            foreach (var m in t.Measures)
            {
                var r = new MeasureResult { Id = m.Id, Kind = m.Kind, Gate = t.Gate.Contains(m.Id), Of = eps.Count, Evaluable = true };
                long by = (long)m.Days * tpd;
                switch (m.Kind)
                {
                    case "adults_alive_share_pooled":
                    {
                        long a = 0, n = 0; foreach (var e in eps) { a += e.AdultsAliveAt(by); n += e.AdultsAtStart(); }
                        r.Value = n == 0 ? 0 : (double)a / n; r.Count = (int)a; r.Of = (int)n; r.Met = m.Passes(r.Value); break;
                    }
                    case "pain_band_share_hunger_or_thirst":
                    {
                        long a = 0, n = 0; foreach (var e in eps) foreach (var p in e.People) { a += p.PainTicksHungerThirst; n += p.PersonTicks; }
                        r.Value = n == 0 ? 1 : (double)a / n; r.Met = n > 0 && m.Passes(r.Value); break;
                    }
                    case "seeds_with_l1_collection_shelter":
                        Count(r, m, eps, f => f.CanBuild, f => f.FirstShelterTick >= 0 && f.FirstShelterTick <= by); break;
                    case "seeds_with_raw_collected_deposited":
                        Count(r, m, eps, f => f.CanCollect, f => f.FirstRawDepositTick >= 0 && f.FirstRawDepositTick <= by); break;
                    case "seeds_with_handover":
                        Count(r, m, eps, f => f.CanHandOver, f => f.FirstHandoverTick >= 0 && f.FirstHandoverTick <= by); break;
                    case "interrupted_sites_completed_share":
                        r.Evaluable = false; r.Met = false;
                        r.Note = stub ? "not evaluable on the stub world (no construction)" : "the interruption fixture needs UN4 (construction) and UN2's fixture harness";
                        break;
                    case "adults_alive_share_each_seed":
                        r.Evaluable = false; r.Met = false;
                        r.Note = stub ? "not evaluable on the stub world (needs the 72-person unified start)" : "run with --full72 on seeds 900001-900003 (UN5)";
                        break;
                    default: throw new InvalidOperationException("unknown measure kind " + m.Kind);
                }
                list.Add(r);
            }
            return list;
        }

        private static void Count(MeasureResult r, MeasureSpec m, List<EpisodeStats> eps, Func<WorldFacts, bool> can, Func<WorldFacts, bool> hit)
        {
            int k = 0; bool ok = eps.Count > 0;
            foreach (var e in eps) { if (!can(e.Facts)) ok = false; if (hit(e.Facts)) k++; }
            r.Count = k; r.Of = eps.Count; r.Evaluable = ok;
            r.Value = eps.Count == 0 ? 0 : (double)k / eps.Count;
            // the file states "count of 20"; with fewer seeds (a self-test subset) the count scales to the same share
            double need = m.Of > 0 ? (double)m.Count / m.Of * eps.Count : m.Count;
            r.Met = ok && k >= need - 1e-9;
            if (!ok) r.Note = "not evaluable on this world";
        }
    }

    public sealed class EvaluationAbortedException : Exception
    {
        public readonly EvalResults Results;
        public EvaluationAbortedException(EvalResults r) : base("evaluation aborted and marked invalid: " + r.InvalidReason) { Results = r; }
    }

    /// <summary>
    /// The TR-01 verdict, computed from the results and the thresholds file only. Passed only on the real unified world, for a
    /// valid evaluation, with a trained arm whose brain is "trained" (never a fallback), when every gate measure (T1, T2, T3, T4,
    /// T6 per the file) is met. Otherwise "TR-01: not passed; UA-learned: not green" with the failed measures named and
    /// <c>Brain.Kind = 2</c> (labelled fallback) recommended.
    /// </summary>
    public static class Verdict
    {
        public const string Failed = "TR-01: not passed; UA-learned: not green";
        public const string Passed = "TR-01: passed; UA-learned: green";

        public static VerdictResult Compute(EvalResults r, Thresholds t)
        {
            var v = new VerdictResult();
            var gate = t != null ? t.Gate : r.Gate;
            var tr = r.Arm("trained");
            if (tr == null) v.FailedGate.Add("no trained arm");
            else
            {
                foreach (var id in gate)
                {
                    var m = tr.Measures.Find(x => x.Id == id);
                    if (m == null || !m.Met) v.FailedGate.Add(id + (m == null ? " (not run)" : !m.Evaluable ? " (not evaluable)" : ""));
                }
                foreach (var m in tr.Measures) if (!gate.Contains(m.Id)) v.ReportedNotGate.Add(m.Id + (m.Met ? ": met" : m.Evaluable ? ": not met" : ": not evaluable"));
                if (tr.Brain != "trained") v.FailedGate.Add($"the trained arm's brain is '{tr.Brain}', never counted as trained");
            }
            if (r.Invalid) v.FailedGate.Add("evaluation invalid: " + r.InvalidReason);
            if (r.SelfTest || r.World != "unified") v.FailedGate.Add("stub world: " + Evaluator.SelfTestLabel);
            v.Tr01Passed = v.FailedGate.Count == 0;
            v.UaLearnedGreen = v.Tr01Passed;
            v.Headline = v.Tr01Passed ? Passed : Failed;
            v.Recommendation = v.Tr01Passed
                ? "Brain.Kind = 1 (Trained) with this artifact"
                : "Brain.Kind = 2 (labelled Fallback); keep the artifact, metrics and cause; mark the artifact FAILED-TR01";
            return v;
        }
    }
}
