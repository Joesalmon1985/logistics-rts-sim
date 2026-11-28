namespace God.Sim.Unified
{
    /// <summary>
    /// UN6 (§D9.2 a, contracts 23.6): the heads of <c>Act v1</c> in schema order, their sizes and offsets, and which heads each control
    /// reads. Identical to the trainer's reference table (<c>DotNet/God.Train/Net/ActionChoice.cs</c> <c>Heads</c>, UN6t-a), so the
    /// artifact's head layout is the same in training and in the game. Read-only arrays only (no mutable static state).
    /// </summary>
    public static class BrainHeads
    {
        public const int Control = 0, Target = 1, Source = 2, Slot = 3, Design = 4, Gesture = 5, R1 = 6, R2 = 7, Quantity = 8, Count = 9;

        public static readonly ReadOnlyArray<int> Size = BuildSize();
        public static readonly ReadOnlyArray<int> Offset = BuildOffset();
        /// <summary>The total logits of all heads (166 in v1).</summary>
        public static readonly int Total = ActSchema.Length;
        /// <summary>The widest head (33, the design head): the logit scratch length.</summary>
        public static readonly int Widest = BuildWidest();

        private static ReadOnlyArray<int> BuildSize()
        {
            var a = new int[Count];
            for (int h = 0; h < Count; h++) a[h] = ActSchema.Heads[h].Length;
            return new ReadOnlyArray<int>(a);
        }

        private static ReadOnlyArray<int> BuildOffset()
        {
            var a = new int[Count];
            int o = 0;
            for (int h = 0; h < Count; h++) { a[h] = o; o += ActSchema.Heads[h].Length; }
            return new ReadOnlyArray<int>(a);
        }

        private static int BuildWidest()
        {
            int w = 0;
            for (int h = 0; h < Count; h++) if (ActSchema.Heads[h].Length > w) w = ActSchema.Heads[h].Length;
            return w;
        }

        /// <summary>Target options: 0-15 perceived entity slots, then self, the facing cell, none.</summary>
        public const int TargetSelf = ObsSchema.Entities, TargetFacing = ObsSchema.Entities + 1, TargetNone = ObsSchema.Entities + 2;
        /// <summary>Source options: 0-3 carry slots, then the empty hand.</summary>
        public const int SourceEmpty = ObsSchema.CarrySlots;
        public const int DesignNone = 32;

        /// <summary>Bit h set when control <paramref name="c"/> reads head h (after the control head itself).</summary>
        public static int UsedBy(int c)
        {
            switch ((Prim)c)
            {
                case Prim.Take: return 1 << Target;
                case Prim.Drop: return 1 << Source;
                case Prim.Use: return (1 << Target) | (1 << Source) | (1 << Slot) | (1 << Design);
                case Prim.Gesture: return (1 << Gesture) | (1 << Target) | (1 << R1) | (1 << R2) | (1 << Quantity);
                default: return 0;
            }
        }

        /// <summary>The largest number of logits one decision computes (the control head plus the widest control's heads): 124 in v1.</summary>
        public static int MaxLogitsPerDecision()
        {
            int best = 0;
            for (int c = 0; c < Size[Control]; c++)
            {
                int n = Size[Control], u = UsedBy(c);
                for (int h = 1; h < Count; h++) if ((u & (1 << h)) != 0) n += Size[h];
                if (n > best) best = n;
            }
            return best;
        }

        /// <summary>The legality bits of one head.</summary>
        public static ulong MaskOf(in ActMask m, int head)
        {
            switch (head)
            {
                case Control: return m.Control;
                case Target: return m.Target;
                case Source: return m.Source;
                case Slot: return m.Slot;
                case Design: return m.Design;
                case Gesture: return m.Gesture;
                case R1: return m.Referent1;
                case R2: return m.Referent2;
                default: return m.Quantity;
            }
        }
    }

    /// <summary>
    /// UN6: one factorised choice over the <see cref="ActSchema"/> heads: the option index of each head, -1 where the chosen control
    /// does not read the head. Turned into a <see cref="PrimitiveControl"/> through the percept's slots (<see cref="IBrainBody.Decode"/>).
    /// </summary>
    public struct BrainChoice
    {
        public int Control, Target, Source, Slot, Design, Gesture, R1, R2, Quantity;

        public static BrainChoice Of(Prim control) => new BrainChoice
        {
            Control = (int)control, Target = -1, Source = -1, Slot = -1, Design = -1, Gesture = -1, R1 = -1, R2 = -1, Quantity = -1,
        };

        public int Get(int head)
        {
            switch (head)
            {
                case BrainHeads.Control: return Control;
                case BrainHeads.Target: return Target;
                case BrainHeads.Source: return Source;
                case BrainHeads.Slot: return Slot;
                case BrainHeads.Design: return Design;
                case BrainHeads.Gesture: return Gesture;
                case BrainHeads.R1: return R1;
                case BrainHeads.R2: return R2;
                default: return Quantity;
            }
        }

        public void Set(int head, int v)
        {
            switch (head)
            {
                case BrainHeads.Control: Control = v; break;
                case BrainHeads.Target: Target = v; break;
                case BrainHeads.Source: Source = v; break;
                case BrainHeads.Slot: Slot = v; break;
                case BrainHeads.Design: Design = v; break;
                case BrainHeads.Gesture: Gesture = v; break;
                case BrainHeads.R1: R1 = v; break;
                case BrainHeads.R2: R2 = v; break;
                default: Quantity = v; break;
            }
        }

        public override string ToString() => ((Prim)Control) + " t" + Target + " s" + Source + " sl" + Slot + " d" + Design + " g" + Gesture + " r" + R1 + "/" + R2 + " q" + Quantity;
    }
}
