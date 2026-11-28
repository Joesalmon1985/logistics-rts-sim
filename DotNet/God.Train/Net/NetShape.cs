using System;
using God.Sim.Unified;

namespace God.Train.Net
{
    /// <summary>
    /// The network shape (§D9.2 a, (L)): input (the <c>Obs v1</c> vector) → encoder (dense, hard-tanh) → dense (hard-tanh) →
    /// gated recurrent state (a minimal GRU: the gate and the candidate read the dense output, the state is
    /// <c>h + z (c - h)</c>) → the factorised <c>Act v1</c> heads read from the recurrent state; only the heads the chosen
    /// control uses are computed. <b>Budget (scale rule): ≤ 20,000 multiply-accumulates per decision.</b> The design's "2 dense
    /// layers of 128" cannot meet that budget with a 284-wide input (284 × 128 alone is 36,352), so the default is
    /// 284 → 24 → 32 → 64 (recurrent) → heads: 19,616 MAC worst case (recorded in the UN6t-a record).
    /// <para>Parameter layout (the artifact's weight order): W1[enc × in], B1[enc], W2[dense × enc], B2[dense], Wz[rec × dense],
    /// Bz[rec], Wc[rec × dense], Bc[rec], Wh[act × rec], Bh[act]; row-major, rows = outputs.</para>
    /// </summary>
    public sealed class NetShape
    {
        public const int MacBudget = 20000;

        public readonly int In, Enc, Dense, Rec, Act;
        public readonly int W1, B1, W2, B2, Wz, Bz, Wc, Bc, Wh, Bh, Count;

        public NetShape(int input, int enc, int dense, int rec, int act)
        {
            if (rec > BrainState.RecurrentSize) throw new ArgumentOutOfRangeException(nameof(rec), "recurrent state larger than BrainState.Recurrent");
            In = input; Enc = enc; Dense = dense; Rec = rec; Act = act;
            int o = 0;
            W1 = o; o += enc * input; B1 = o; o += enc;
            W2 = o; o += dense * enc; B2 = o; o += dense;
            Wz = o; o += rec * dense; Bz = o; o += rec;
            Wc = o; o += rec * dense; Bc = o; o += rec;
            Wh = o; o += act * rec; Bh = o; o += act;
            Count = o;
        }

        public static NetShape Default => new NetShape(ObsSchema.Length, 24, 32, 64, ActSchema.Length);

        /// <summary>Worst-case multiply-accumulates of one decision.</summary>
        public int MacPerDecision => In * Enc + Enc * Dense + 2 * Rec * Dense + Rec * Heads.MaxLogitsPerDecision();

        public string Text => $"{In},{Enc},{Dense},{Rec},{Act}";

        public static NetShape Parse(string text)
        {
            var p = text.Split(',');
            return new NetShape(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]));
        }

        public bool SameAs(NetShape o) => o != null && Text == o.Text;
    }
}
