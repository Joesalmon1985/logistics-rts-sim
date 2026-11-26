using System;

namespace God.Sim.Lab
{
    /// <summary>
    /// Fixed-point arithmetic for the lab (STRATEGY_BENCHMARKS 3, 9: no floating point). A value is a <c>long</c> with
    /// <see cref="Bits"/> fractional bits, the same scale as a population share (<see cref="One"/> = 2^30 = share 1).
    /// Products and quotients that could overflow go through an exact 128-bit intermediate. Every function is a pure
    /// function of its integer inputs, so the results are identical on every host.
    /// </summary>
    public static class Fx
    {
        public const int Bits = 30;
        public const long One = 1L << Bits;
        public const long Half = One >> 1;
        /// <summary>ln 2 in Q30 (744261117.95, rounded).</summary>
        public const long Ln2 = 744261118;

        public static long FromInt(long v) => v << Bits;
        public static long FromPermille(long p) => MulDiv(One, p, 1000);
        public static long FromRatio(long num, long den) => MulDiv(One, num, den);
        /// <summary>A value from micro units (millionths).</summary>
        public static long FromMicro(long micro) => MulDiv(One, micro, 1000000);
        public static long ToMicro(long fx) => MulDiv(fx, 1000000, One);
        public static long ToPermille(long fx) => MulDiv(fx, 1000, One);

        /// <summary>a × b in Q30, rounded half away from zero; exact 128-bit intermediate.</summary>
        public static long Mul(long a, long b)
        {
            if (a > -2147483648L && a < 2147483648L && b > -2147483648L && b < 2147483648L)
            {
                long p = a * b;
                return p >= 0 ? (p + Half) >> Bits : -((-p + Half) >> Bits);
            }
            return MulDiv(a, b, One);
        }

        /// <summary>a / b in Q30.</summary>
        public static long Div(long a, long b) => MulDiv(a, One, b);

        /// <summary>a × b / c, rounded half away from zero, with an exact 128-bit product. Throws if the result overflows.</summary>
        public static long MulDiv(long a, long b, long c)
        {
            if (c == 0) throw new DivideByZeroException();
            bool neg = (a < 0) ^ (b < 0) ^ (c < 0);
            ulong ua = Abs(a), ub = Abs(b), uc = Abs(c);
            MulU64(ua, ub, out ulong hi, out ulong lo);
            // add c/2 for rounding
            ulong half = uc >> 1;
            ulong lo2 = lo + half;
            if (lo2 < lo) hi++;
            if (hi >= uc) throw new OverflowException("Fx.MulDiv overflow");
            ulong q = DivU128(hi, lo2, uc);
            if (q > long.MaxValue) throw new OverflowException("Fx.MulDiv overflow");
            return neg ? -(long)q : (long)q;
        }

        /// <summary>floor(a × b / c) for non-negative a, b and positive c.</summary>
        public static long MulDivFloor(long a, long b, long c)
        {
            if (a < 0 || b < 0 || c <= 0) throw new ArgumentOutOfRangeException(nameof(a), "MulDivFloor takes non-negative inputs");
            MulU64((ulong)a, (ulong)b, out ulong hi, out ulong lo);
            if (hi >= (ulong)c) throw new OverflowException("Fx.MulDivFloor overflow");
            ulong q = DivU128(hi, lo, (ulong)c);
            if (q > long.MaxValue) throw new OverflowException("Fx.MulDivFloor overflow");
            return (long)q;
        }

        private static ulong Abs(long v) => v < 0 ? (ulong)(-(v + 1)) + 1UL : (ulong)v;

        /// <summary>Full 64 × 64 → 128-bit unsigned product.</summary>
        public static void MulU64(ulong a, ulong b, out ulong hi, out ulong lo)
        {
            ulong aL = (uint)a, aH = a >> 32, bL = (uint)b, bH = b >> 32;
            ulong ll = aL * bL, lh = aL * bH, hl = aH * bL, hh = aH * bH;
            ulong mid = (ll >> 32) + (uint)lh + (uint)hl;
            lo = (mid << 32) | (uint)ll;
            hi = hh + (lh >> 32) + (hl >> 32) + (mid >> 32);
        }

        /// <summary>(hi:lo) / d for hi &lt; d (so the quotient fits 64 bits); restoring binary division.</summary>
        private static ulong DivU128(ulong hi, ulong lo, ulong d)
        {
            if (hi == 0) return lo / d;
            ulong rem = hi, q = 0;
            for (int i = 0; i < 64; i++)
            {
                ulong carry = rem >> 63;
                rem = (rem << 1) | (lo >> 63);
                lo <<= 1;
                q <<= 1;
                if (carry != 0 || rem >= d) { rem -= d; q |= 1; }
            }
            return q;
        }

        private static int FloorLog2(ulong v)
        {
            int n = -1;
            while (v != 0) { v >>= 1; n++; }
            return n;
        }

        /// <summary>Natural logarithm of a positive Q30 value (atanh series; error below 1e-8).</summary>
        public static long Ln(long x)
        {
            if (x <= 0) throw new ArgumentOutOfRangeException(nameof(x), "Ln of a non-positive value");
            int k = FloorLog2((ulong)x) - Bits;
            long m = k >= 0 ? x >> k : x << -k; // m in [1, 2) in Q30
            long s = ((m - One) << Bits) / (m + One); // in [0, 1/3]
            long s2 = (s * s + Half) >> Bits;
            long term = s, sum = s;
            for (int n = 3; n < 64; n += 2)
            {
                term = (term * s2 + Half) >> Bits;
                if (term == 0) break;
                sum += term / n;
            }
            return 2 * sum + k * Ln2;
        }

        /// <summary>e^x for a Q30 value. Results below one unit return 0; x above 21 throws (the result would overflow).</summary>
        public static long Exp(long x)
        {
            if (x > 21 * One) throw new OverflowException("Fx.Exp argument too large");
            if (x < -22 * One) return 0;
            long k = x >= 0 ? x / Ln2 : -((-x + Ln2 - 1) / Ln2);
            long r = x - k * Ln2; // [0, ln 2)
            long term = One, sum = One;
            for (int n = 1; n < 30; n++)
            {
                term = (term * r + Half) >> Bits;
                term = (term + n / 2) / n;
                if (term == 0) break;
                sum += term;
            }
            if (k >= 0) return sum << (int)k;
            int sh = (int)-k;
            if (sh >= 62) return 0;
            return (sum + (1L << (sh - 1))) >> sh;
        }

        /// <summary>Integer square root (floor).</summary>
        public static ulong ISqrt(ulong n)
        {
            if (n < 2) return n;
            ulong x = 1UL << ((FloorLog2(n) >> 1) + 1);
            while (true)
            {
                ulong y = (x + n / x) >> 1;
                if (y >= x) return x;
                x = y;
            }
        }

        /// <summary>Square root of a non-negative Q30 value.</summary>
        public static long Sqrt(long x)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));
            if (x < (1L << 33)) return (long)ISqrt((ulong)x << Bits);
            return (long)ISqrt((ulong)x) << (Bits / 2);
        }

        /// <summary>A Q30 value as a decimal string with the given number of decimals (rounded), invariant culture.</summary>
        public static string Format(long fx, int decimals) => FormatScaled(MulDiv(fx, Pow10(decimals), One), decimals);

        /// <summary>An integer v / 10^decimals as a decimal string.</summary>
        public static string FormatScaled(long v, int decimals)
        {
            bool neg = v < 0;
            ulong a = Abs(v);
            ulong p = (ulong)Pow10(decimals);
            string s = (a / p).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (decimals > 0) s += "." + (a % p).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(decimals, '0');
            return neg ? "-" + s : s;
        }

        public static long Pow10(int n)
        {
            long p = 1;
            for (int i = 0; i < n; i++) p *= 10;
            return p;
        }
    }
}
