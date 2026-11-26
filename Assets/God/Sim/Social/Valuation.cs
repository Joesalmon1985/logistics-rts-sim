using System;
using God.Sim.Learning;

namespace God.Sim.Social
{
    /// <summary>
    /// Subjective valuation and price beliefs (ECONOMY 4.5), the NPC's own belief in worth units (here: drive points of
    /// hunger relief times urgency). Used only for exchange decisions and telemetry, never as the learning goods channel
    /// (SHARED_CONTRACTS 13.6: <c>GoodsWorth.Of</c> stays the reward). Integers; it reads only what the mind knows of
    /// itself (hunger, its carry, its bed and tool) and its own price beliefs.
    /// </summary>
    public static class Valuation
    {
        /// <summary>Units of one food the planning horizon needs; beyond them a unit has no consume use, only exchange use.</summary>
        public const int FoodHorizon = 4;

        /// <summary>The believed nutrition of a food (ECONOMY 4.1: berries 150, roots 200, fish 300; nightshade little), 0 otherwise.</summary>
        public static int Nutrition(Ware w)
        {
            switch (w)
            {
                case Ware.Berries: return 150;
                case Ware.Roots: return 200;
                case Ware.Fish: return 300;
                case Ware.Nightshade: return 30;
                default: return 0;
            }
        }

        /// <summary>
        /// The worth of one more unit of <paramref name="w"/> when <paramref name="held"/> are held: the largest of its
        /// consume use (nutrition x urgency x the variety rule, within the horizon), production use (logs while it has no
        /// bed), tool use (an axe without one) and exchange use (price belief x 9/10).
        /// </summary>
        public static int Marginal(Ware w, int held, int hunger, bool hasBed, bool hasTool, short[] price)
        {
            int consume = 0, prod = 0, tool = 0;
            if (Wares.IsFood(w) && held < FoodHorizon)
                consume = Nutrition(w) * (500 + Fx.Clamp(hunger, 0, 1000)) / 1000 * 1000 / (1000 + 250 * held);
            if (w == Ware.Log) prod = hasBed ? (held < 1 ? 60 : 20) : 250;
            if (w == Ware.Axe) tool = hasTool ? 0 : 200;
            int exch = price != null && (int)w < price.Length ? price[(int)w] * 9 / 10 : 0;
            return Math.Max(Math.Max(consume, prod), Math.Max(tool, exch));
        }

        /// <summary>The worth of receiving <paramref name="count"/> units on top of <paramref name="held"/>.</summary>
        public static int Gain(Ware w, int count, int held, int hunger, bool hasBed, bool hasTool, short[] price)
        {
            int v = 0;
            for (int k = 0; k < count; k++) v += Marginal(w, held + k, hunger, hasBed, hasTool, price);
            return v;
        }

        /// <summary>The worth of giving away <paramref name="count"/> of the <paramref name="held"/> units (the last units first).</summary>
        public static int Loss(Ware w, int count, int held, int hunger, bool hasBed, bool hasTool, short[] price)
        {
            int v = 0;
            for (int k = 0; k < count && held - 1 - k >= 0; k++) v += Marginal(w, held - 1 - k, hunger, hasBed, hasTool, price);
            return v;
        }

        /// <summary>The price belief of a ware, or its own consume or production use at a moderate need when it has none yet.</summary>
        public static int PriceOf(short[] price, Ware w)
        {
            int p = price[(int)w];
            if (p > 0) return p;
            return Math.Max(1, Marginal(w, 0, 300, true, true, null));
        }

        /// <summary>
        /// A trade it made (or saw): <c>a</c> of A for <c>b</c> of B implies <c>P(A) = b P(B) / a</c> and <c>P(B) = a P(A) / b</c>.
        /// Each belief moves towards the implied value by 1/<paramref name="towards"/> (4 for its own trade, 8 for one seen).
        /// </summary>
        public static void Observe(short[] price, Ware a, int countA, Ware b, int countB, int towards)
        {
            if (countA <= 0 || countB <= 0 || a == b) return;
            int pa = PriceOf(price, a), pb = PriceOf(price, b);
            int impliedA = countB * pb / countA, impliedB = countA * pa / countB;
            price[(int)a] = (short)Fx.Clamp(pa + (impliedA - pa) / towards, 1, 30000);
            price[(int)b] = (short)Fx.Clamp(pb + (impliedB - pb) / towards, 1, 30000);
        }

        /// <summary>Worth to value units of social memory (SOCIAL 4.2: worth / 10).</summary>
        public static int ValueUnits(int worth) => worth / 10;
    }
}
