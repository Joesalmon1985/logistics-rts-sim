using System;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN6 (§D9.2 a): the network shape <c>in,enc,dense,rec,act</c> and the artifact's weight order: W1[enc x in], B1[enc],
    /// W2[dense x enc], B2[dense], Wz[rec x dense], Bz[rec], Wc[rec x dense], Bc[rec], Wh[act x rec], Bh[act] (row-major, rows =
    /// outputs). Same layout as the trainer's <c>NetShape</c> (UN6t-a). The default <c>284,24,32,64,166</c> costs 19,616
    /// multiply-accumulates in the worst case (budget 20,000; "2 dense layers of 128" cannot meet the budget with a 284-wide input).
    /// </summary>
    public sealed class BrainShape
    {
        public const int MacBudget = 20000;
        public const string DefaultText = "284,24,32,64,166";

        public readonly int In, Enc, Dense, Rec, Act;
        public readonly int W1, B1, W2, B2, Wz, Bz, Wc, Bc, Wh, Bh, Count;

        public BrainShape(int input, int enc, int dense, int rec, int act)
        {
            if (input != ObsSchema.Length) throw new ArgumentException("brain shape: input " + input + " is not the Obs v1 length " + ObsSchema.Length);
            if (act != ActSchema.Length) throw new ArgumentException("brain shape: heads " + act + " are not the Act v1 length " + ActSchema.Length);
            if (rec < 1 || rec > BrainState.RecurrentSize) throw new ArgumentException("brain shape: recurrent state " + rec + " outside 1-" + BrainState.RecurrentSize);
            if (enc < 1 || dense < 1) throw new ArgumentException("brain shape: empty layer");
            In = input; Enc = enc; Dense = dense; Rec = rec; Act = act;
            int o = 0;
            W1 = o; o += enc * input; B1 = o; o += enc;
            W2 = o; o += dense * enc; B2 = o; o += dense;
            Wz = o; o += rec * dense; Bz = o; o += rec;
            Wc = o; o += rec * dense; Bc = o; o += rec;
            Wh = o; o += act * rec; Bh = o; o += act;
            Count = o;
        }

        public static BrainShape Default => Parse(DefaultText);

        /// <summary>The multiply-accumulates of the recurrent part of one decision (encoder, dense, gate and candidate).</summary>
        public int RecurMacs => In * Enc + Enc * Dense + 2 * Rec * Dense;

        /// <summary>Worst-case multiply-accumulates of one decision.</summary>
        public int MacPerDecision => RecurMacs + Rec * BrainHeads.MaxLogitsPerDecision();

        public string Text => In + "," + Enc + "," + Dense + "," + Rec + "," + Act;

        public static BrainShape Parse(string text)
        {
            var p = (text ?? "").Split(',');
            if (p.Length != 5) throw new FormatException("brain shape: '" + text + "' is not in,enc,dense,rec,act");
            var v = new int[5];
            for (int i = 0; i < 5; i++) v[i] = int.Parse(p[i].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            return new BrainShape(v[0], v[1], v[2], v[3], v[4]);
        }
    }

    /// <summary>Per-runtime buffers of one forward pass (made once; no allocation per decision).</summary>
    public sealed class BrainScratch
    {
        public readonly int[] X, A1, A2, Logits;
        public BrainScratch(BrainShape s) { X = new int[s.In]; A1 = new int[s.Enc]; A2 = new int[s.Dense]; Logits = new int[64]; }
    }

    /// <summary>
    /// UN6 (§D9.2 a): the run-time integer inference of the shared frozen policy. Bit-exact with the trainer's reference
    /// <c>DotNet/God.Train/Net/QNet.cs</c> (UN6t-a) whenever the temperature is 1000 and every logit bias is 0: int16 weights in Q12
    /// (1.0 = 4096), observations and activations in milli units, accumulation in 64-bit integers saturated to int32 then an
    /// arithmetic right shift by 12; hard-tanh clamp to +-1000; gate hard-sigmoid <c>clamp((v &gt;&gt; 2) + 500, 0, 1000)</c>; state
    /// update <c>h += z (c - h) / 1000</c>; observations clamped to +-2000. Action choice: greedy (argmax over legal options, lowest
    /// index on ties) or sampled from an integer softmax over legal options, weight = <see cref="Exp"/>[(d x 1000 / T) &gt;&gt; 4] with
    /// d = max - logit and T the temperature (permille; 1000 = the trainer's), drawn with <see cref="Rng.NextInt"/>.
    /// <para>Run-time additions (UN6, not in the trainer): <see cref="Bias"/> adds the person's associative overlay to the control head
    /// (bounded logit biases, milli-nats) and <see cref="Temperature"/> comes from temperament. No floating point, no <c>Math</c>,
    /// no allocation in <see cref="Act"/>. <see cref="Macs"/> counts the multiply-accumulates of the last decision.</para>
    /// </summary>
    public sealed class BrainNet
    {
        public const int WeightShift = 12, ObsClamp = 2000, ActClamp = 1000;
        public const int ExpSteps = 1024;
        /// <summary>e^(-0.016) in Q16, rounded: 0.984127 x 65536.</summary>
        public const int ExpRatioQ16 = 64496;

        public readonly BrainShape Shape;
        public readonly short[] W;
        /// <summary>e^(-k x 0.016) in Q16 for k = 0..1023, built by integer recurrence (an instance table: no mutable static).</summary>
        public readonly int[] Exp;
        private readonly BrainScratch _sc;

        /// <summary>Per-decision inputs set by the caller: a bias per control option (null = none) and the temperature (permille).</summary>
        public int[] Bias;
        public int Temperature = 1000;
        /// <summary>The input map (<see cref="BrainInput"/>): raw by default; the game's untrained brain, the trainer's candidates and
        /// an artifact that names map 1 set <see cref="BrainInput.Current"/>.</summary>
        public int InputMap = BrainInput.MapRaw;
        /// <summary>The multiply-accumulates of the last <see cref="Act"/> (cost counter for UNP and UN11a).</summary>
        public int Macs;

        public BrainNet(BrainShape shape, short[] weights)
        {
            if (weights == null || weights.Length != shape.Count) throw new ArgumentException("brain: weights " + (weights?.Length ?? 0) + " != shape " + shape.Count);
            Shape = shape; W = weights;
            Exp = BuildExp();
            _sc = new BrainScratch(shape);
        }

        public BrainScratch Scratch => _sc;

        public static int[] BuildExp()
        {
            var e = new int[ExpSteps];
            e[0] = 65536;
            for (int i = 1; i < ExpSteps; i++) e[i] = (int)(((long)e[i - 1] * ExpRatioQ16 + 32768) >> 16);
            return e;
        }

        private static int Sat(long v) => v > int.MaxValue ? int.MaxValue : v < int.MinValue ? int.MinValue : (int)v;
        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;

        /// <summary>Encoder, dense layer and recurrent update: <paramref name="h"/> (the person's recurrent state) changes in place.</summary>
        public void Recur(short[] obs, short[] h)
        {
            var s = Shape; var w = W; var sc = _sc;
            int[] x = sc.X, a1 = sc.A1, a2 = sc.A2;
            int nIn = s.In, nEnc = s.Enc, nDense = s.Dense;
            int map = InputMap;
            for (int i = 0; i < nIn; i++) x[i] = BrainInput.Apply(map, i, obs[i], ObsClamp);
            var ws = new ReadOnlySpan<short>(w);
            var xs = new ReadOnlySpan<int>(x, 0, nIn);
            for (int j = 0; j < nEnc; j++)
            {
                long acc = (long)w[s.B1 + j] * 1000;
                var row = ws.Slice(s.W1 + j * nIn, nIn);
                for (int i = 0; i < row.Length; i++) acc += row[i] * xs[i];
                a1[j] = Clamp(Sat(acc) >> WeightShift, -ActClamp, ActClamp);
            }
            var a1s = new ReadOnlySpan<int>(a1, 0, nEnc);
            for (int j = 0; j < nDense; j++)
            {
                long acc = (long)w[s.B2 + j] * 1000;
                var row = ws.Slice(s.W2 + j * nEnc, nEnc);
                for (int i = 0; i < row.Length; i++) acc += row[i] * a1s[i];
                a2[j] = Clamp(Sat(acc) >> WeightShift, -ActClamp, ActClamp);
            }
            var a2s = new ReadOnlySpan<int>(a2, 0, nDense);
            for (int j = 0; j < s.Rec; j++)
            {
                long az = (long)w[s.Bz + j] * 1000, ac = (long)w[s.Bc + j] * 1000;
                var rz = ws.Slice(s.Wz + j * nDense, nDense); var rc = ws.Slice(s.Wc + j * nDense, nDense);
                for (int i = 0; i < a2s.Length; i++) { int v = a2s[i]; az += rz[i] * v; ac += rc[i] * v; }
                int z = Clamp(((Sat(az) >> WeightShift) >> 2) + 500, 0, 1000);
                int c = Clamp(Sat(ac) >> WeightShift, -ActClamp, ActClamp);
                int hj = h[j];
                h[j] = (short)Clamp(hj + z * (c - hj) / 1000, -ActClamp, ActClamp);
            }
            Macs += s.RecurMacs;
        }

        /// <summary>The logits of one head from the recurrent state, into <c>Scratch.Logits[0..size)</c> (the control head plus <see cref="Bias"/>).</summary>
        public void Logits(int head, short[] h)
        {
            var s = Shape; var w = W; var sc = _sc;
            int off = BrainHeads.Offset[head], n = BrainHeads.Size[head], rec = s.Rec;
            var ws = new ReadOnlySpan<short>(w);
            var hs = new ReadOnlySpan<short>(h, 0, rec);
            for (int k = 0; k < n; k++)
            {
                int o = off + k;
                long acc = (long)w[s.Bh + o] * 1000;
                var row = ws.Slice(s.Wh + o * rec, rec);
                for (int j = 0; j < row.Length; j++) acc += row[j] * hs[j];
                sc.Logits[k] = Sat(acc) >> WeightShift;
            }
            if (head == BrainHeads.Control && Bias != null)
                for (int k = 0; k < n && k < Bias.Length; k++) sc.Logits[k] = Sat((long)sc.Logits[k] + Bias[k]);
            Macs += n * s.Rec;
        }

        /// <summary>Chooses an option of <paramref name="head"/> among the legal ones (-1 when none is legal); <paramref name="rng"/> null = greedy.</summary>
        public int Choose(int head, short[] h, ulong legal, Rng rng)
        {
            if (legal == 0) return -1;
            Logits(head, h);
            var sc = _sc;
            int n = BrainHeads.Size[head], best = -1, bestL = int.MinValue;
            for (int k = 0; k < n; k++) if ((legal & (1UL << k)) != 0 && sc.Logits[k] > bestL) { bestL = sc.Logits[k]; best = k; }
            if (rng == null) return best;
            long total = 0;
            for (int k = 0; k < n; k++) if ((legal & (1UL << k)) != 0) total += Weight((long)bestL - sc.Logits[k]);
            long r = rng.NextInt((int)(total < int.MaxValue ? total : int.MaxValue));
            for (int k = 0; k < n; k++)
            {
                if ((legal & (1UL << k)) == 0) continue;
                r -= Weight((long)bestL - sc.Logits[k]);
                if (r < 0) return k;
            }
            return best;
        }

        private int Weight(long d)
        {
            if (Temperature != 1000) d = d * 1000 / (Temperature < 100 ? 100 : Temperature);
            long i = d >> 4;
            return i >= ExpSteps ? 0 : Exp[i];
        }

        /// <summary>
        /// One decision: the recurrent update from <paramref name="obs"/>, then the control head and the heads that control reads, each
        /// masked by physical legality only. Option 0 of the control head (<see cref="Prim.None"/>) is never a decision; with no legal
        /// control the choice is <see cref="Prim.Wait"/>.
        /// </summary>
        public BrainChoice Act(short[] obs, short[] h, in ActMask m, Rng rng)
        {
            Macs = 0;
            Recur(obs, h);
            var a = BrainChoice.Of(Prim.None);
            ulong cm = m.Control & ~1UL;
            int c = Choose(BrainHeads.Control, h, cm, rng);
            if (c < 0) c = (int)Prim.Wait;
            a.Control = c;
            int used = BrainHeads.UsedBy(c);
            for (int hd = 1; hd < BrainHeads.Count; hd++)
                if ((used & (1 << hd)) != 0) a.Set(hd, Choose(hd, h, BrainHeads.MaskOf(m, hd), rng));
            return a;
        }

        // ---------------- weights ----------------

        /// <summary>The weights as little-endian bytes (the artifact's hashed payload).</summary>
        public static byte[] WeightBytes(short[] w)
        {
            var b = new byte[w.Length * 2];
            for (int i = 0; i < w.Length; i++) { b[2 * i] = (byte)(w[i] & 0xFF); b[2 * i + 1] = (byte)((w[i] >> 8) & 0xFF); }
            return b;
        }

        public static short[] FromBytes(byte[] b)
        {
            var w = new short[b.Length / 2];
            for (int i = 0; i < w.Length; i++) w[i] = (short)(b[2 * i] | (b[2 * i + 1] << 8));
            return w;
        }

        /// <summary>Lower-case hex SHA-256 of the little-endian weight bytes.</summary>
        public static string WeightsSha256(short[] w)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var d = sha.ComputeHash(WeightBytes(w));
                var sb = new System.Text.StringBuilder(64);
                foreach (var x in d) sb.Append(x.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// The untrained network's weights (<see cref="BrainKind.Untrained"/>): deterministic, from <paramref name="seed"/> only; each
        /// weight uniform in +-(2 x 4096 / isqrt(fan-in)) (Q12), biases 0. Valid but incompetent behaviour.
        /// </summary>
        public static short[] UntrainedWeights(BrainShape s, ulong seed)
        {
            var w = new short[s.Count];
            var rng = new Rng(seed);
            Fill(w, s.W1, s.Enc * s.In, s.In, rng);
            Fill(w, s.W2, s.Dense * s.Enc, s.Enc, rng);
            Fill(w, s.Wz, s.Rec * s.Dense, s.Dense, rng);
            Fill(w, s.Wc, s.Rec * s.Dense, s.Dense, rng);
            Fill(w, s.Wh, s.Act * s.Rec, s.Rec, rng);
            return w;
        }

        private static void Fill(short[] w, int at, int n, int fanIn, Rng rng)
        {
            int r = 2 * 4096 / ISqrt(fanIn < 1 ? 1 : fanIn);
            if (r > 32767) r = 32767;
            for (int i = 0; i < n; i++) w[at + i] = (short)(rng.NextInt(2 * r + 1) - r);
        }

        /// <summary>Integer square root (floor).</summary>
        public static int ISqrt(int v)
        {
            if (v <= 0) return 0;
            int x = 0;
            while ((long)(x + 1) * (x + 1) <= v) x++;
            return x;
        }
    }
}
