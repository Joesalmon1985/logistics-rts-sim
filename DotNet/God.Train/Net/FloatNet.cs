using System;
using God.Sim;
using God.Sim.Unified;

namespace God.Train.Net
{
    /// <summary>Per-sample buffers of the float forward pass (kept for the backward pass).</summary>
    public sealed class FCache
    {
        public readonly float[] X, V1, A1, V2, A2, Vz, Z, Vc, C, HPrev, H, DH, DA1, DA2, Logit;
        /// <summary>The input map of the network being trained (God.Sim's <c>BrainInput</c>).</summary>
        public int InputMap = BrainInput.Current;
        private int[] _xi;
        internal int[] XI(int n) => _xi != null && _xi.Length == n ? _xi : (_xi = new int[n]);
        public FCache(NetShape s)
        {
            X = new float[s.In]; V1 = new float[s.Enc]; A1 = new float[s.Enc]; V2 = new float[s.Dense]; A2 = new float[s.Dense];
            Vz = new float[s.Rec]; Z = new float[s.Rec]; Vc = new float[s.Rec]; C = new float[s.Rec]; HPrev = new float[s.Rec]; H = new float[s.Rec];
            DH = new float[s.Rec]; DA1 = new float[s.Enc]; DA2 = new float[s.Dense]; Logit = new float[64];
        }
    }

    /// <summary>
    /// The float master network of the trainer: the same shape and functions as <see cref="QNet"/> in real units (weights 1.0 =
    /// 4096 in the artifact, activations 1.0 = 1000), so quantising is a scale and a round. Behaviour cloning trains it with
    /// truncated back-propagation (the previous recurrent state is an input, its gradient is dropped); every evaluation runs the
    /// quantised <see cref="QNet"/>.
    /// </summary>
    public static class FloatNet
    {
        /// <summary>Glorot-uniform weights, zero biases, heads scaled down; keyed by <paramref name="seed"/> only.</summary>
        public static float[] Init(NetShape s, ulong seed)
        {
            var p = new float[s.Count];
            var rng = new Rng(seed).Fork("un6t-init");
            void Fill(int at, int rows, int cols, double scale)
            {
                double lim = scale * Math.Sqrt(6.0 / (rows + cols));
                for (int i = 0; i < rows * cols; i++) p[at + i] = (float)((Uniform(rng) * 2 - 1) * lim);
            }
            Fill(s.W1, s.Enc, s.In, 1); Fill(s.W2, s.Dense, s.Enc, 1); Fill(s.Wz, s.Rec, s.Dense, 1); Fill(s.Wc, s.Rec, s.Dense, 1);
            Fill(s.Wh, s.Act, s.Rec, 0.5);
            return p;
        }

        /// <summary>A uniform double in [0, 1) from 53 bits of the integer stream (portable: no transcendental function).</summary>
        public static double Uniform(Rng r) => (r.NextULong() >> 11) * (1.0 / 9007199254740992.0);

        /// <summary>An approximately standard normal value: Irwin-Hall sum of four uniforms, rescaled (portable).</summary>
        public static double Normal(Rng r) => (Uniform(r) + Uniform(r) + Uniform(r) + Uniform(r) - 2.0) * 1.7320508075688772;

        /// <summary>Exact int16 quantisation: round half away from zero of <c>p × 4096</c>, saturated.</summary>
        public static short[] Quantise(float[] p)
        {
            var q = new short[p.Length];
            for (int i = 0; i < p.Length; i++)
            {
                double v = Math.Round((double)p[i] * 4096.0, MidpointRounding.AwayFromZero);
                q[i] = (short)(v > short.MaxValue ? short.MaxValue : v < short.MinValue ? short.MinValue : v);
            }
            return q;
        }

        public static QNet ToQNet(NetShape s, float[] p) => new QNet(s, Quantise(p));

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;

        /// <summary>Forward of one decision; <paramref name="h"/> (float recurrent state) is updated in place.</summary>
        public static void Recur(NetShape s, float[] p, short[] obs, float[] h, FCache c)
        {
            var xi = c.XI(s.In);
            BrainInput.Fill(c.InputMap, obs, xi, s.In, QNet.ObsClamp);
            for (int i = 0; i < s.In; i++) c.X[i] = xi[i] / 1000f;
            for (int j = 0; j < s.Enc; j++)
            {
                double v = p[s.B1 + j]; int row = s.W1 + j * s.In;
                for (int i = 0; i < s.In; i++) v += p[row + i] * c.X[i];
                c.V1[j] = (float)v; c.A1[j] = Clamp((float)v, -1, 1);
            }
            for (int j = 0; j < s.Dense; j++)
            {
                double v = p[s.B2 + j]; int row = s.W2 + j * s.Enc;
                for (int i = 0; i < s.Enc; i++) v += p[row + i] * c.A1[i];
                c.V2[j] = (float)v; c.A2[j] = Clamp((float)v, -1, 1);
            }
            for (int j = 0; j < s.Rec; j++)
            {
                double vz = p[s.Bz + j], vc = p[s.Bc + j]; int rz = s.Wz + j * s.Dense, rc = s.Wc + j * s.Dense;
                for (int i = 0; i < s.Dense; i++) { vz += p[rz + i] * c.A2[i]; vc += p[rc + i] * c.A2[i]; }
                c.Vz[j] = (float)vz; c.Z[j] = Clamp((float)vz / 4f + 0.5f, 0, 1);
                c.Vc[j] = (float)vc; c.C[j] = Clamp((float)vc, -1, 1);
                c.HPrev[j] = h[j];
                h[j] = Clamp(h[j] + c.Z[j] * (c.C[j] - h[j]), -1, 1);
                c.H[j] = h[j];
            }
        }

        public static void Logits(NetShape s, float[] p, int head, FCache c)
        {
            int off = Heads.Offset[head], n = Heads.Size[head];
            for (int k = 0; k < n; k++)
            {
                int o = off + k; double v = p[s.Bh + o]; int row = s.Wh + o * s.Rec;
                for (int j = 0; j < s.Rec; j++) v += p[row + j] * c.H[j];
                c.Logit[k] = (float)v;
            }
        }

        /// <summary>
        /// Masked softmax cross-entropy of one head against the label; adds the head's gradient into <paramref name="g"/> and the
        /// state gradient into <c>c.DH</c>. Returns the loss (0 when the label is not legal or the head has one legal option).
        /// </summary>
        public static double HeadLoss(NetShape s, float[] p, float[] g, int head, ulong legal, int label, FCache c)
        {
            if (label < 0 || (legal & (1UL << label)) == 0) return 0;
            Logits(s, p, head, c);
            int off = Heads.Offset[head], n = Heads.Size[head];
            double max = double.NegativeInfinity;
            for (int k = 0; k < n; k++) if ((legal & (1UL << k)) != 0 && c.Logit[k] > max) max = c.Logit[k];
            double z = 0;
            for (int k = 0; k < n; k++) if ((legal & (1UL << k)) != 0) z += Math.Exp(c.Logit[k] - max);
            double loss = -(c.Logit[label] - max - Math.Log(z));
            for (int k = 0; k < n; k++)
            {
                if ((legal & (1UL << k)) == 0) continue;
                float d = (float)(Math.Exp(c.Logit[k] - max) / z - (k == label ? 1 : 0));
                int o = off + k, row = s.Wh + o * s.Rec;
                g[s.Bh + o] += d;
                for (int j = 0; j < s.Rec; j++) { g[row + j] += d * c.H[j]; c.DH[j] += d * p[row + j]; }
            }
            return loss;
        }

        /// <summary>Back-propagates <c>c.DH</c> through the recurrent update, the dense layer and the encoder (truncated at h_prev).</summary>
        public static void Backward(NetShape s, float[] p, float[] g, FCache c)
        {
            Array.Clear(c.DA2, 0, c.DA2.Length); Array.Clear(c.DA1, 0, c.DA1.Length);
            for (int j = 0; j < s.Rec; j++)
            {
                float dh = c.DH[j];
                if (dh == 0) continue;
                float raw = c.HPrev[j] + c.Z[j] * (c.C[j] - c.HPrev[j]);
                if (raw <= -1 || raw >= 1) continue;
                float dz = dh * (c.C[j] - c.HPrev[j]), dc = dh * c.Z[j];
                float vz = c.Vz[j] / 4f + 0.5f; float dvz = vz > 0 && vz < 1 ? dz * 0.25f : 0;
                float dvc = c.Vc[j] > -1 && c.Vc[j] < 1 ? dc : 0;
                int rz = s.Wz + j * s.Dense, rc = s.Wc + j * s.Dense;
                g[s.Bz + j] += dvz; g[s.Bc + j] += dvc;
                for (int i = 0; i < s.Dense; i++)
                {
                    g[rz + i] += dvz * c.A2[i]; g[rc + i] += dvc * c.A2[i];
                    c.DA2[i] += dvz * p[rz + i] + dvc * p[rc + i];
                }
            }
            for (int j = 0; j < s.Dense; j++)
            {
                float d = c.V2[j] > -1 && c.V2[j] < 1 ? c.DA2[j] : 0;
                if (d == 0) continue;
                int row = s.W2 + j * s.Enc;
                g[s.B2 + j] += d;
                for (int i = 0; i < s.Enc; i++) { g[row + i] += d * c.A1[i]; c.DA1[i] += d * p[row + i]; }
            }
            for (int j = 0; j < s.Enc; j++)
            {
                float d = c.V1[j] > -1 && c.V1[j] < 1 ? c.DA1[j] : 0;
                if (d == 0) continue;
                int row = s.W1 + j * s.In;
                g[s.B1 + j] += d;
                for (int i = 0; i < s.In; i++) g[row + i] += d * c.X[i];
            }
            Array.Clear(c.DH, 0, c.DH.Length);
        }
    }
}
