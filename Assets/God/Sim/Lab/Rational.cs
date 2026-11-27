using System.Numerics;

namespace God.Sim.Lab
{
    /// <summary>
    /// An exact rational number (big-integer numerator and positive denominator, always reduced). Used only by the
    /// exact equilibrium analysis (<see cref="ContestStudy"/>), where Q30 rounding would blur ties such as Dove against
    /// Retaliator (15 = 15) that decide whether a state is an ESS. No floating point.
    /// </summary>
    public readonly struct Rational
    {
        public readonly BigInteger Num;
        public readonly BigInteger Den;

        public Rational(BigInteger num, BigInteger den)
        {
            if (den.IsZero) throw new System.DivideByZeroException();
            if (den.Sign < 0) { num = -num; den = -den; }
            var g = BigInteger.GreatestCommonDivisor(num, den);
            if (!g.IsZero && !g.IsOne) { num /= g; den /= g; }
            Num = num;
            Den = den.IsZero ? BigInteger.One : den;
        }

        public static Rational Of(long v) => new Rational(v, 1);
        public static Rational Of(long num, long den) => new Rational(num, den);
        public static Rational Zero => new Rational(0, 1);
        public static Rational One => new Rational(1, 1);

        public int Sign => Num.Sign;
        public bool IsZero => Num.IsZero;

        public static Rational operator +(Rational a, Rational b) => new Rational(a.Num * b.Den + b.Num * a.Den, a.Den * b.Den);
        public static Rational operator -(Rational a, Rational b) => new Rational(a.Num * b.Den - b.Num * a.Den, a.Den * b.Den);
        public static Rational operator -(Rational a) => new Rational(-a.Num, a.Den);
        public static Rational operator *(Rational a, Rational b) => new Rational(a.Num * b.Num, a.Den * b.Den);
        public static Rational operator /(Rational a, Rational b) => new Rational(a.Num * b.Den, a.Den * b.Num);

        public int CompareTo(Rational b) => (Num * b.Den).CompareTo(b.Num * Den);
        public bool Equals(Rational b) => Num == b.Num && Den == b.Den;

        /// <summary>Rounded to the nearest Q30 value (halves away from zero).</summary>
        public long ToQ30() => RoundScaled(Fx.One);

        /// <summary>Rounded to millionths.</summary>
        public long ToMicro() => RoundScaled(1000000);

        private long RoundScaled(long scale)
        {
            var n = Num * scale;
            var q = BigInteger.DivRem(BigInteger.Abs(n) * 2 + Den, Den * 2, out _);
            return (long)(n.Sign < 0 ? -q : q);
        }

        /// <summary>Decimal text with the given number of decimals (rounded).</summary>
        public string Format(int decimals) => Fx.FormatScaled((long)RoundTo(decimals), decimals);

        private BigInteger RoundTo(int decimals)
        {
            var n = Num * BigInteger.Pow(10, decimals);
            var q = (BigInteger.Abs(n) * 2 + Den) / (Den * 2);
            return n.Sign < 0 ? -q : q;
        }

        /// <summary>"a/b", or "a" for an integer.</summary>
        public override string ToString() => Den.IsOne ? Num.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : Num.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + Den.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
