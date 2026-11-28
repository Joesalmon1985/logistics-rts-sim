using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using God.Sim;
using God.Sim.Unified;
using God.Train.Env;
using God.Train.Honesty;
using God.Train.Net;

namespace God.Train.Learn
{
    public sealed class BcConfig
    {
        public int Epochs = 20, Batch = 8, Workers = 1;
        public double Lr = 0.003, Beta1 = 0.9, Beta2 = 0.999, Eps = 1e-8;
        public ulong OrderSeed = 1;
    }

    /// <summary>
    /// Behaviour cloning from labelled teacher demonstrations: masked softmax cross-entropy on the control head and the heads the
    /// labelled control uses, truncated back-propagation through the recurrent update, Adam on the float master weights. Batches
    /// are fixed sets of sequences (epoch order keyed by <see cref="BcConfig.OrderSeed"/> and the epoch); per-sequence gradients
    /// run in parallel and are summed in sequence order, so the result does not depend on the worker count.
    /// </summary>
    public static class BehaviourCloning
    {
        public static Checkpoint Train(Checkpoint start, List<Sequence> data, BcConfig cfg, int untilEpoch, Action<int, double> log = null)
        {
            foreach (var s in data) if (s.Origin != ControlOrigin.Teacher) throw new InvalidOperationException("behaviour cloning reads labelled teacher demonstrations only");
            var shape = start.NetShape;
            var ck = new Checkpoint
            {
                Kind = "bc", Stage = start.Stage, Shape = start.Shape, World = start.World, Step = start.Kind == "bc" ? start.Step : 0,
                P = (float[])start.P.Clone(), M = start.Kind == "bc" ? (float[])start.M.Clone() : new float[shape.Count],
                V = start.Kind == "bc" ? (float[])start.V.Clone() : new float[shape.Count],
                B1Pow = start.Kind == "bc" ? start.B1Pow : 1, B2Pow = start.Kind == "bc" ? start.B2Pow : 1, SeedsText = start.SeedsText,
            };
            int nb = Math.Max(1, cfg.Batch);
            var grads = new float[nb][]; var caches = new FCache[nb]; var losses = new double[nb]; var counts = new int[nb];
            for (int j = 0; j < nb; j++) { grads[j] = new float[shape.Count]; caches[j] = new FCache(shape) { InputMap = start.InputMap }; }
            var total = new double[shape.Count];
            var po = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, cfg.Workers) };
            while (ck.Step < untilEpoch)
            {
                int epoch = ck.Step;
                var order = new List<int>(data.Count); for (int i = 0; i < data.Count; i++) order.Add(i);
                new Rng(cfg.OrderSeed).Fork("un6t-bc-order", epoch).Shuffle(order);
                double epochLoss = 0; long epochN = 0;
                for (int b0 = 0; b0 < order.Count; b0 += nb)
                {
                    int bn = Math.Min(nb, order.Count - b0);
                    var p = ck.P;
                    Parallel.For(0, bn, po, j =>
                    {
                        Array.Clear(grads[j], 0, grads[j].Length);
                        losses[j] = SequenceGrad(shape, p, data[order[b0 + j]], grads[j], caches[j], out counts[j]);
                    });
                    Array.Clear(total, 0, total.Length);
                    long n = 0;
                    for (int j = 0; j < bn; j++) { var g = grads[j]; for (int i = 0; i < g.Length; i++) total[i] += g[i]; n += counts[j]; epochLoss += losses[j]; }
                    epochN += n;
                    if (n == 0) continue;
                    ck.B1Pow *= cfg.Beta1; ck.B2Pow *= cfg.Beta2;
                    for (int i = 0; i < total.Length; i++)
                    {
                        double g = total[i] / n;
                        double m = cfg.Beta1 * ck.M[i] + (1 - cfg.Beta1) * g, v = cfg.Beta2 * ck.V[i] + (1 - cfg.Beta2) * g * g;
                        ck.M[i] = (float)m; ck.V[i] = (float)v;
                        double mh = m / (1 - ck.B1Pow), vh = v / (1 - ck.B2Pow);
                        ck.P[i] = (float)(ck.P[i] - cfg.Lr * mh / (Math.Sqrt(vh) + cfg.Eps));
                    }
                }
                ck.Step++;
                log?.Invoke(ck.Step, epochN > 0 ? epochLoss / epochN : 0);
            }
            return ck;
        }

        private static double SequenceGrad(NetShape s, float[] p, Sequence seq, float[] g, FCache c, out int count)
        {
            var h = new float[s.Rec]; double loss = 0; count = 0;
            foreach (var x in seq.Samples)
            {
                FloatNet.Recur(s, p, x.Obs, h, c);
                int ctl = x.Label.Control;
                loss += FloatNet.HeadLoss(s, p, g, Heads.Control, x.Mask.Control & ~1UL, ctl, c);
                int used = Heads.UsedBy(ctl);
                for (int hd = 1; hd < Heads.Count; hd++)
                    if ((used & (1 << hd)) != 0) loss += FloatNet.HeadLoss(s, p, g, hd, Heads.MaskOf(x.Mask, hd), x.Label.Get(hd), c);
                FloatNet.Backward(s, p, g, c);
                count++;
            }
            return loss;
        }

        /// <summary>
        /// Action agreement of the quantised network with the labels, teacher-forced along each sequence (greedy, integer
        /// inference): a decision agrees when the control and every head that control uses are equal.
        /// </summary>
        public static double Agreement(QNet net, List<Sequence> data) => Agreement(net, data, null, null);

        /// <summary>Agreement of the float master network (greedy), for the quantisation gap; never used as a result.</summary>
        public static double FloatAgreement(NetShape s, float[] p, List<Sequence> data)
        {
            var c = new FCache(s); long ok = 0, n = 0;
            foreach (var seq in data)
            {
                var h = new float[s.Rec];
                foreach (var x in seq.Samples)
                {
                    FloatNet.Recur(s, p, x.Obs, h, c);
                    int ctl = Greedy(s, p, Heads.Control, x.Mask.Control & ~1UL, c);
                    bool same = ctl == x.Label.Control;
                    int used = Heads.UsedBy(x.Label.Control);
                    for (int hd = 1; same && hd < Heads.Count; hd++) if ((used & (1 << hd)) != 0 && Greedy(s, p, hd, Heads.MaskOf(x.Mask, hd), c) != x.Label.Get(hd)) same = false;
                    if (same) ok++;
                    n++;
                }
            }
            return n == 0 ? 0 : (double)ok / n;
        }

        private static int Greedy(NetShape s, float[] p, int head, ulong legal, FCache c)
        {
            if (legal == 0) return -1;
            FloatNet.Logits(s, p, head, c);
            int best = -1; float bl = float.NegativeInfinity;
            for (int k = 0; k < Heads.Size[head]; k++) if ((legal & (1UL << k)) != 0 && c.Logit[k] > bl) { bl = c.Logit[k]; best = k; }
            return best;
        }

        /// <summary>As <see cref="Agreement(QNet, List{Sequence})"/>, also counting decisions and agreements per labelled control.</summary>
        public static double Agreement(QNet net, List<Sequence> data, long[] perControlN, long[] perControlOk)
        {
            var sc = new QScratch(net.Shape); long ok = 0, n = 0;
            foreach (var seq in data)
            {
                var h = new short[BrainState.RecurrentSize];
                foreach (var x in seq.Samples)
                {
                    var a = net.Act(x.Obs, h, in x.Mask, null, sc);
                    bool same = a.Control == x.Label.Control;
                    int used = Heads.UsedBy(x.Label.Control);
                    for (int hd = 1; same && hd < Heads.Count; hd++) if ((used & (1 << hd)) != 0 && a.Get(hd) != x.Label.Get(hd)) same = false;
                    if (same) ok++;
                    n++;
                    if (perControlN != null) { perControlN[x.Label.Control]++; if (same) perControlOk[x.Label.Control]++; }
                }
            }
            return n == 0 ? 0 : (double)ok / n;
        }
    }

    public sealed class EsConfig
    {
        public int Pairs = 8, EpisodesPerCandidate = 2, Workers = 1;
        public double Sigma = 0.005, Lr = 0.0005;
        public long Ticks = 200;
        public long[] Seeds = { 1 };
        public ulong NoiseSeed = 7;
    }

    /// <summary>
    /// Evolution-strategies fine-tuning on undiscounted whole-episode returns: antithetic pairs, noise keyed by (iteration, pair),
    /// rank-normalised fitness, plain gradient step. Every candidate is quantised to int16 and run through <see cref="QNet"/> (the
    /// game's inference) on training seeds; candidates run in parallel and are stored by index, so the result does not depend on
    /// the worker count. A checkpoint after iteration i resumes bit-exactly.
    /// </summary>
    public static class EvolutionStrategies
    {
        public static Checkpoint Train(Checkpoint start, Func<long, IWorld> world, EsConfig cfg, int untilIteration, Action<int, long, long> log = null)
        {
            SeedPolicy.CheckTraining(cfg.Seeds);
            var shape = start.NetShape;
            var ck = new Checkpoint { Kind = "es", Stage = start.Stage, Shape = start.Shape, World = start.World, Step = start.Kind == "es" ? start.Step : 0, P = (float[])start.P.Clone(), SeedsText = start.SeedsText };
            int pop = 2 * cfg.Pairs;
            var fitness = new long[pop];
            var po = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, cfg.Workers) };
            while (ck.Step < untilIteration)
            {
                int it = ck.Step; var theta = ck.P;
                Parallel.For(0, pop, po, c =>
                {
                    var eps = Noise(cfg.NoiseSeed, it, c / 2, shape.Count);
                    float sign = (c & 1) == 0 ? 1f : -1f;
                    var cand = new float[theta.Length];
                    for (int i = 0; i < cand.Length; i++) cand[i] = (float)(theta[i] + sign * cfg.Sigma * eps[i]);
                    var q = FloatNet.ToQNet(shape, cand); q.InputMap = start.InputMap;
                    fitness[c] = Fitness(q, world, cfg, it);
                });
                var idx = new int[pop]; for (int i = 0; i < pop; i++) idx[i] = i;
                Array.Sort(idx, (a, b) => fitness[a] != fitness[b] ? fitness[a].CompareTo(fitness[b]) : a.CompareTo(b));
                var u = new double[pop];
                for (int r = 0; r < pop; r++) u[idx[r]] = pop == 1 ? 0 : (double)r / (pop - 1) - 0.5;
                var step = new double[theta.Length];
                for (int k = 0; k < cfg.Pairs; k++)
                {
                    double wk = u[2 * k] - u[2 * k + 1];
                    if (wk == 0) continue;
                    var eps = Noise(cfg.NoiseSeed, it, k, shape.Count);
                    for (int i = 0; i < step.Length; i++) step[i] += wk * eps[i];
                }
                double scale = cfg.Lr / (pop * cfg.Sigma);
                for (int i = 0; i < theta.Length; i++) theta[i] = (float)(theta[i] + scale * step[i]);
                ck.Step++;
                long best = long.MinValue, sum = 0; foreach (var f in fitness) { sum += f; if (f > best) best = f; }
                log?.Invoke(ck.Step, sum / pop, best);
            }
            return ck;
        }

        public static double[] Noise(ulong seed, int iteration, int pair, int n)
        {
            var r = new Rng(seed).Fork("un6t-es-noise", iteration, pair);
            var e = new double[n];
            for (int i = 0; i < n; i++) e[i] = FloatNet.Normal(r);
            return e;
        }

        /// <summary>The episode seeds of an iteration: consecutive entries of the training seed list (wrapping).</summary>
        public static long SeedOf(EsConfig cfg, int iteration, int e) => cfg.Seeds[(int)(((long)iteration * cfg.EpisodesPerCandidate + e) % cfg.Seeds.Length)];

        private static long Fitness(QNet net, Func<long, IWorld> world, EsConfig cfg, int it)
        {
            long f = 0;
            for (int e = 0; e < cfg.EpisodesPerCandidate; e++)
            {
                long seed = SeedOf(cfg, it, e);
                var w = world(seed);
                f += Rollout.Run(w, new NetActor(net), seed, cfg.Ticks, evaluation: false).TotalReturn;
            }
            return f;
        }
    }
}
