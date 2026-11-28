using God.Sim.Unified;

namespace God.Train.Net
{
    /// <summary>
    /// One factorised choice over the <see cref="ActSchema"/> heads (<c>Act v1</c>): the option index of each head, -1 where the head
    /// is not used by the chosen control. A world turns it into a <see cref="PrimitiveControl"/> through its own percept slots.
    /// </summary>
    public struct ActionChoice
    {
        public int Control, Target, Source, Slot, Design, Gesture, R1, R2, Quantity;

        public static ActionChoice Of(Prim control)
        {
            return new ActionChoice { Control = (int)control, Target = -1, Source = -1, Slot = -1, Design = -1, Gesture = -1, R1 = -1, R2 = -1, Quantity = -1 };
        }

        public int Get(int head) => head switch
        {
            Heads.Control => Control, Heads.Target => Target, Heads.Source => Source, Heads.Slot => Slot, Heads.Design => Design,
            Heads.Gesture => Gesture, Heads.R1 => R1, Heads.R2 => R2, _ => Quantity,
        };

        public void Set(int head, int v)
        {
            switch (head)
            {
                case Heads.Control: Control = v; break;
                case Heads.Target: Target = v; break;
                case Heads.Source: Source = v; break;
                case Heads.Slot: Slot = v; break;
                case Heads.Design: Design = v; break;
                case Heads.Gesture: Gesture = v; break;
                case Heads.R1: R1 = v; break;
                case Heads.R2: R2 = v; break;
                default: Quantity = v; break;
            }
        }

        public override string ToString() => $"{(Prim)Control} t{Target} s{Source} sl{Slot} d{Design} g{Gesture} r{R1}/{R2} q{Quantity}";
    }

    /// <summary>The heads of <c>Act v1</c> in schema order, their sizes and offsets, and which heads each control uses.</summary>
    public static class Heads
    {
        public const int Control = 0, Target = 1, Source = 2, Slot = 3, Design = 4, Gesture = 5, R1 = 6, R2 = 7, Quantity = 8, Count = 9;

        public static readonly int[] Size = new int[Count];
        public static readonly int[] Offset = new int[Count];
        public static readonly int Total;

        static Heads()
        {
            int o = 0;
            for (int h = 0; h < Count; h++) { Size[h] = ActSchema.Heads[h].Length; Offset[h] = o; o += Size[h]; }
            Total = o;
        }

        /// <summary>Target option indices: 0-15 perceived entity slots, then self, facing cell, none.</summary>
        public static int TargetSelf => ObsSchema.Entities;
        public static int TargetFacing => ObsSchema.Entities + 1;
        public static int TargetNone => ObsSchema.Entities + 2;
        /// <summary>Source option indices: 0-3 carry slots, then empty hand.</summary>
        public static int SourceEmpty => ObsSchema.CarrySlots;
        public static int DesignNone => 32;

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

        /// <summary>The largest number of logits any one decision computes (control head plus the widest control's heads).</summary>
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

        public static ulong MaskOf(in ActMask m, int head) => head switch
        {
            Control => m.Control, Target => m.Target, Source => m.Source, Slot => m.Slot, Design => m.Design,
            Gesture => m.Gesture, R1 => m.Referent1, R2 => m.Referent2, _ => m.Quantity,
        };
    }
}
