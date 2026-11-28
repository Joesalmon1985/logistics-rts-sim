using System;

namespace God.Train.Honesty
{
    /// <summary>The layouts an environment can open. Only <see cref="Train"/> is visible to the trainer; the rest are held out.</summary>
    public enum Layout : byte { Train = 0, Mirrored = 1, Rotated = 2, Regenerated = 3, UnfamiliarStart = 4 }

    public static class Layouts
    {
        public static readonly Layout[] HeldOut = { Layout.Mirrored, Layout.Rotated, Layout.Regenerated, Layout.UnfamiliarStart };

        public static string Name(Layout l) => l switch
        {
            Layout.Train => "train", Layout.Mirrored => "mirrored", Layout.Rotated => "rotated",
            Layout.Regenerated => "regenerated", _ => "unfamiliar_start",
        };

        /// <summary>The held-out layout of evaluation seed <paramref name="seed"/>: the four variants in turn (seed order).</summary>
        public static Layout ForEvalSeed(long seed) => HeldOut[(int)(seed % HeldOut.Length)];
    }

    /// <summary>
    /// The right to open held-out seeds and layouts. Its constructor is internal: outside this assembly only <c>Evaluator</c> hands
    /// one out, and only after the thresholds hash matched. The trainer (demo, bc, es) never holds one.
    /// </summary>
    public sealed class EvalAccess
    {
        public readonly string ThresholdsHash;
        internal EvalAccess(string hash) { ThresholdsHash = hash; }
    }

    public sealed class HeldOutRefusedException : Exception
    {
        public HeldOutRefusedException(string what) : base("refused: " + what + " is held out (seeds >= 900000 and held-out layouts are evaluation-only)") { }
    }

    /// <summary>The seed split (thresholds file section 3), enforced at every environment construction.</summary>
    public static class SeedPolicy
    {
        /// <summary>Seeds at or above this are never visible to training (the file's <c>refuseSeedsFrom</c>, checked by a test).</summary>
        public const long RefuseFrom = 900000;

        public static bool IsHeldOut(long seed) => seed >= RefuseFrom;

        /// <summary>Throws unless the seed and layout are trainer-visible, or <paramref name="access"/> is an evaluation right.</summary>
        public static void Check(long seed, Layout layout, EvalAccess access)
        {
            if (access != null) return;
            if (seed < 0) throw new ArgumentOutOfRangeException(nameof(seed));
            if (IsHeldOut(seed)) throw new HeldOutRefusedException("seed " + seed);
            if (layout != Layout.Train) throw new HeldOutRefusedException("layout " + Layouts.Name(layout));
        }

        /// <summary>Parses "a-b" or "a" or "a,b,c" into ascending seeds.</summary>
        public static long[] ParseSeeds(string text)
        {
            var list = new System.Collections.Generic.List<long>();
            foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                int dash = part.IndexOf('-');
                if (dash > 0)
                {
                    long a = long.Parse(part.Substring(0, dash)), b = long.Parse(part.Substring(dash + 1));
                    for (long s = a; s <= b; s++) list.Add(s);
                }
                else list.Add(long.Parse(part));
            }
            list.Sort();
            return list.ToArray();
        }

        /// <summary>Training-side guard for a whole seed list (bc, es, demo).</summary>
        public static void CheckTraining(long[] seeds)
        {
            foreach (var s in seeds) Check(s, Layout.Train, null);
        }
    }
}
