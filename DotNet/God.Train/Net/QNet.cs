using System;
using God.Sim;
using God.Sim.Unified;

namespace God.Train.Net
{
    /// <summary>Per-thread buffers of one forward pass (no allocation per decision).</summary>
    public sealed class QScratch
    {
        public readonly int[] X, A1, A2, Logits;
        public QScratch(NetShape s) { X = new int[s.In]; A1 = new int[s.Enc]; A2 = new int[s.Dense]; Logits = new int[64]; }
    }

    /// <summary>
    /// The reference integer inference (the definition UN6's run-time inference must match bit-exactly, test
    /// <c>Quantisation_Parity</c>). Weights int16 in Q12 (1.0 = 4096); activations and observations in milli units (1.0 = 1000);
    /// accumulation in 64-bit integers, saturated to int32, then an arithmetic right shift by 12 (floor). Activations: hard-tanh
    /// clamp to [-1000, 1000]; gate: hard-sigmoid <c>clamp((v &gt;&gt; 2) + 500, 0, 1000)</c>; recurrent update
    /// <c>h += z (c - h) / 1000</c> (C# integer division, toward zero). Observations are clamped to [-2000, 2000]. Action selection:
    /// greedy (argmax over legal options, lowest index on ties) or sampled from an integer softmax over legal options: weight =
    /// <see cref="Exp"/>[(max - logit) &gt;&gt; 4] (a table of e^(-k × 0.016) in Q16 built by integer recurrence, so no
    /// floating point anywhere), drawn with <see cref="Rng.NextInt"/>.
    /// </summary>
    public sealed class QNet
    {
        public const int WeightShift = 12, ObsClamp = 2000, ActClamp = 1000;
        public const int ExpSteps = 1024;
        /// <summary>e^(-0.016) in Q16, rounded: 0.984127 × 65536.</summary>
        public const int ExpRatioQ16 = 64496;
        public static readonly int[] Exp = BuildExp();

        public readonly NetShape Shape;
        public readonly short[] W;

        public QNet(NetShape shape, short[] weights)
        {
            if (weights.Length != shape.Count) throw new ArgumentException($"weights {weights.Length} != shape {shape.Count}");
            Shape = shape; W = weights;
        }

        private static int[] BuildExp()
        {
            var e = new int[ExpSteps];
            e[0] = 65536;
            for (int i = 1; i < ExpSteps; i++) e[i] = (int)(((long)e[i - 1] * ExpRatioQ16 + 32768) >> 16);
            return e;
        }

        private static int Sat(long v) => v > int.MaxValue ? int.MaxValue : v < int.MinValue ? int.MinValue : (int)v;
        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;

        /// <summary>The encoder, dense layer and recurrent update: <paramref name="h"/> (the person's recurrent state) changes in place.</summary>
        public void Recur(short[] obs, short[] h, QScratch sc)
        {
            var s = Shape; var w = W;
            for (int i = 0; i < s.In; i++) sc.X[i] = Clamp(obs[i], -ObsClamp, ObsClamp);
            for (int j = 0; j < s.Enc; j++)
            {
                long acc = (long)w[s.B1 + j] * 1000; int row = s.W1 + j * s.In;
                for (int i = 0; i < s.In; i++) acc += w[row + i] * sc.X[i];
                sc.A1[j] = Clamp(Sat(acc) >> WeightShift, -ActClamp, ActClamp);
            }
            for (int j = 0; j < s.Dense; j++)
            {
                long acc = (long)w[s.B2 + j] * 1000; int row = s.W2 + j * s.Enc;
                for (int i = 0; i < s.Enc; i++) acc += w[row + i] * sc.A1[i];
                sc.A2[j] = Clamp(Sat(acc) >> WeightShift, -ActClamp, ActClamp);
            }
            for (int j = 0; j < s.Rec; j++)
            {
                long az = (long)w[s.Bz + j] * 1000, ac = (long)w[s.Bc + j] * 1000;
                int rz = s.Wz + j * s.Dense, rc = s.Wc + j * s.Dense;
                for (int i = 0; i < s.Dense; i++) { az += w[rz + i] * sc.A2[i]; ac += w[rc + i] * sc.A2[i]; }
                int z = Clamp(((Sat(az) >> WeightShift) >> 2) + 500, 0, 1000);
                int c = Clamp(Sat(ac) >> WeightShift, -ActClamp, ActClamp);
                int hj = h[j];
                h[j] = (short)Clamp(hj + z * (c - hj) / 1000, -ActClamp, ActClamp);
            }
        }

        /// <summary>The logits of one head from the recurrent state, into <c>sc.Logits[0..size)</c>.</summary>
        public void Logits(int head, short[] h, QScratch sc)
        {
            var s = Shape; var w = W;
            int off = Heads.Offset[head], n = Heads.Size[head];
            for (int k = 0; k < n; k++)
            {
                int o = off + k;
                long acc = (long)w[s.Bh + o] * 1000; int row = s.Wh + o * s.Rec;
                for (int j = 0; j < s.Rec; j++) acc += w[row + j] * h[j];
                sc.Logits[k] = Sat(acc) >> WeightShift;
            }
        }

        /// <summary>Chooses an option of <paramref name="head"/> among the legal ones (-1 when none is legal).</summary>
        public int Choose(int head, short[] h, ulong legal, Rng rng, QScratch sc)
        {
            if (legal == 0) return -1;
            Logits(head, h, sc);
            int n = Heads.Size[head], best = -1, bestL = int.MinValue;
            for (int k = 0; k < n; k++) if ((legal & (1UL << k)) != 0 && sc.Logits[k] > bestL) { bestL = sc.Logits[k]; best = k; }
            if (rng == null) return best;
            long total = 0;
            for (int k = 0; k < n; k++) if ((legal & (1UL << k)) != 0) total += Weight(bestL - sc.Logits[k]);
            long r = rng.NextInt((int)Math.Min(total, int.MaxValue));
            for (int k = 0; k < n; k++)
            {
                if ((legal & (1UL << k)) == 0) continue;
                r -= Weight(bestL - sc.Logits[k]);
                if (r < 0) return k;
            }
            return best;
        }

        private static int Weight(long d) { long i = d >> 4; return i >= ExpSteps ? 0 : Exp[i]; }

        /// <summary>
        /// One decision: recurrent update from <paramref name="obs"/>, then the control head and the heads that control uses, each
        /// masked by physical legality only. <paramref name="rng"/> null = greedy.
        /// </summary>
        public ActionChoice Act(short[] obs, short[] h, in ActMask m, Rng rng, QScratch sc)
        {
            Recur(obs, h, sc);
            var a = ActionChoice.Of(Prim.None);
            ulong cm = m.Control & ~1UL; // option 0 (Prim.None) is never a decision
            int c = Choose(Heads.Control, h, cm, rng, sc);
            if (c < 0) c = (int)Prim.Wait;
            a.Control = c;
            int used = Heads.UsedBy(c);
            for (int hd = 1; hd < Heads.Count; hd++)
                if ((used & (1 << hd)) != 0) a.Set(hd, Choose(hd, h, Heads.MaskOf(m, hd), rng, sc));
            return a;
        }

        /// <summary>The weights as little-endian bytes (the artifact's hashed payload).</summary>
        public byte[] WeightBytes()
        {
            var b = new byte[W.Length * 2];
            for (int i = 0; i < W.Length; i++) { b[2 * i] = (byte)(W[i] & 0xFF); b[2 * i + 1] = (byte)((W[i] >> 8) & 0xFF); }
            return b;
        }

        public static short[] FromBytes(byte[] b)
        {
            var w = new short[b.Length / 2];
            for (int i = 0; i < w.Length; i++) w[i] = (short)(b[2 * i] | (b[2 * i + 1] << 8));
            return w;
        }

        public string WeightsSha256()
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Honesty.Thresholds.Hex(sha.ComputeHash(WeightBytes()));
        }
    }

    /// <summary>A world-specific mapping from a head choice to the primitive control (percept slot → target reference).</summary>
    public delegate PrimitiveControl ChoiceDecoder(in Percept p, in ActionChoice a);

    /// <summary>
    /// <see cref="QNet"/> behind the UN0 <see cref="IPrimitivePolicy"/> interface: the recurrent state is
    /// <see cref="BrainState.Recurrent"/>; the decision is sampled with the person's keyed policy stream.
    /// </summary>
    public sealed class QNetPolicy : IPrimitivePolicy
    {
        private readonly QNet _net; private readonly ChoiceDecoder _decode; private readonly QScratch _sc;
        public ActionChoice Last;

        public QNetPolicy(QNet net, ChoiceDecoder decode) { _net = net; _decode = decode; _sc = new QScratch(net.Shape); }

        public PrimitiveControl Decide(in Percept p, short[] obs, ref BrainState b, in ActMask m, ref Rng rng)
        {
            Last = _net.Act(obs, b.Recurrent, in m, rng, _sc);
            return _decode(in p, in Last);
        }
    }
}
