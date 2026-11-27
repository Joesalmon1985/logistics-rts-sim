using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// Deterministic SplitMix64 generator. The whole state is the public field <see cref="State"/>,
    /// so it serialises with Newtonsoft and resumes exactly after load.
    /// </summary>
    public sealed class Rng
    {
        public ulong State;

        public Rng() { }

        public Rng(ulong seed) { State = seed; }

        public ulong NextULong()
        {
            State += 0x9E3779B97F4A7C15UL;
            ulong z = State;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Uniform integer in [0, n). n must be positive. Unbiased (rejection sampling).</summary>
        public int NextInt(int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "n must be positive");
            ulong bound = (ulong)n;
            ulong limit = ulong.MaxValue - (ulong.MaxValue % bound);
            ulong x;
            do { x = NextULong(); } while (x >= limit);
            return (int)(x % bound);
        }

        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        public int NextRange(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return minInclusive + NextInt(maxExclusive - minInclusive);
        }

        /// <summary>True with probability numerator/denominator.</summary>
        public bool Chance(int numerator, int denominator) => NextInt(denominator) < numerator;

        /// <summary>Fisher-Yates shuffle in place.</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = NextInt(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        /// <summary>
        /// New independent generator derived from the current state and a label (FNV-1a hash).
        /// Does NOT advance this generator: the same state and label always give the same stream.
        /// </summary>
        public Rng Fork(string label)
        {
            ulong h = 14695981039346656037UL;
            if (label != null)
            {
                foreach (char c in label)
                {
                    h ^= c;
                    h *= 1099511628211UL;
                }
            }
            var mix = new Rng(State ^ h);
            return new Rng(mix.NextULong());
        }

        /// <summary>
        /// A stream keyed by purpose and entity: <c>Fork("npc", id)</c>, <c>Fork("dice", roll)</c>. Like
        /// <see cref="Fork(string)"/> it does not advance this generator, so the same state, label and id always give the
        /// same stream, and adding an entity never shifts another entity's draws (conventions section 3).
        /// </summary>
        public Rng Fork(string label, long id)
        {
            ulong h = Fnv(label);
            h = Mix(h, (ulong)id);
            var mix = new Rng(State ^ h);
            return new Rng(mix.NextULong());
        }

        /// <summary>A stream keyed by purpose and two ids (an entity and a tick, or a pair): <c>Fork("duel", unit, tick)</c>.</summary>
        public Rng Fork(string label, long id1, long id2)
        {
            ulong h = Fnv(label);
            h = Mix(h, (ulong)id1);
            h = Mix(h, 0xA5A5A5A5A5A5A5A5UL);
            h = Mix(h, (ulong)id2);
            var mix = new Rng(State ^ h);
            return new Rng(mix.NextULong());
        }

        private static ulong Fnv(string label)
        {
            ulong h = 14695981039346656037UL;
            if (label != null) foreach (char c in label) { h ^= c; h *= 1099511628211UL; }
            return h;
        }

        /// <summary>FNV-1a over the eight bytes of a value.</summary>
        private static ulong Mix(ulong h, ulong v)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++) { h ^= (v >> (8 * i)) & 0xFF; h *= 1099511628211UL; }
            }
            return h;
        }

        public Rng Clone() => new Rng(State);

        /// <summary>
        /// Makes this generator the stream <c>parent.Fork(label, id1, id2)</c> would give, without allocating (a per-system
        /// scratch stream reused for every decision of a tick). Does not advance <paramref name="parent"/>.
        /// </summary>
        public void SetToFork(Rng parent, string label, long id1, long id2)
        {
            ulong h = Fnv(label);
            h = Mix(h, (ulong)id1);
            h = Mix(h, 0xA5A5A5A5A5A5A5A5UL);
            h = Mix(h, (ulong)id2);
            State = parent.State ^ h;
            State = NextULong();
        }
    }
}
