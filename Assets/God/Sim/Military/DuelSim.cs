using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// A one-on-one duel on a straight line, off the map, with the same rules as the military tick: the soldiers walk
    /// towards each other at their kit's speed until each is within its own reach (ranged ones stop there), a spear's wait
    /// is cut to 0 when a fight starts within its reach, attacks resolve with <see cref="Duel.Resolve(in SoldierStats, in SoldierStats, Rng, bool)"/> (a bow or sling within close quarters at half its hit chance)
    /// in turn order (longer reach first, then the first soldier), and a soldier killed earlier in the tick does not strike.
    /// For balancing (package Q4) and tests; the game itself never calls it.
    /// </summary>
    public static class DuelSim
    {
        public enum Winner { Draw = 0, A = 1, B = 2 }

        public const int MaxTicks = 3000;

        private sealed class Side { public SoldierStats St; public int Health, Cooldown, Pos, Engaged; public bool A; }

        /// <summary>Fights one duel starting <paramref name="gapMm"/> apart. Draw when nobody falls within <see cref="MaxTicks"/>.</summary>
        public static Winner Fight(Kit a, Kit b, int gapMm, Rng rng, out int ticks) => Fight(new MilitaryTuning(), a, b, gapMm, rng, out ticks);

        /// <summary>As <see cref="Fight(Kit, Kit, int, Rng, out int)"/> under a given tuning.</summary>
        public static Winner Fight(MilitaryTuning tu, Kit a, Kit b, int gapMm, Rng rng, out int ticks)
        {
            var sa = new Side { St = tu.Of(a), Pos = 0, A = true };
            var sb = new Side { St = tu.Of(b), Pos = gapMm };
            sa.Health = sa.St.MaxHealth; sb.Health = sb.St.MaxHealth;
            var order = new List<Side>(2);
            for (ticks = 1; ticks <= MaxTicks; ticks++)
            {
                order.Clear();
                Think(sa, sb, order);
                Think(sb, sa, order);
                if (order.Count == 2 && order[1].St.RangeMm > order[0].St.RangeMm) { var t = order[0]; order[0] = order[1]; order[1] = t; }
                foreach (var s in order)
                {
                    var o = s == sa ? sb : sa;
                    if (s.Health <= 0 || o.Health <= 0) continue;
                    var r = Duel.Resolve(tu, s.St, o.St, rng, System.Math.Abs(o.Pos - s.Pos) <= tu.CloseQuartersMm);
                    o.Health -= r.Damage;
                    s.Cooldown = s.St.CooldownTicks;
                }
                if (sa.Health <= 0) return Winner.B;
                if (sb.Health <= 0) return Winner.A;
            }
            return Winner.Draw;
        }

        private static void Think(Side s, Side o, List<Side> order)
        {
            if (s.Cooldown > 0) s.Cooldown--;
            int gap = System.Math.Abs(o.Pos - s.Pos);
            if (gap <= s.St.RangeMm)
            {
                if (s.Engaged == 0 && !s.St.Ranged && s.St.W.Special == WeaponSpecial.FirstStrike) s.Cooldown = 0;
                s.Engaged = 1;
                if (s.Cooldown == 0) order.Add(s);
                return;
            }
            s.Engaged = 0;
            int step = System.Math.Min(s.St.SpeedMmPerTick, gap);
            s.Pos += o.Pos > s.Pos ? step : -step;
        }

        /// <summary>Wins of A, wins of B and draws over <paramref name="count"/> duels, each from its own seed (seed, seed+1, ...), sides swapped every other duel so neither goes first by id.</summary>
        public static (int aWins, int bWins, int draws) Series(Kit a, Kit b, int count, ulong seed, int gapMm) => Series(new MilitaryTuning(), a, b, count, seed, gapMm);

        /// <summary>As <see cref="Series(Kit, Kit, int, ulong, int)"/> under a given tuning.</summary>
        public static (int aWins, int bWins, int draws) Series(MilitaryTuning tu, Kit a, Kit b, int count, ulong seed, int gapMm)
        {
            int wa = 0, wb = 0, d = 0;
            for (int i = 0; i < count; i++)
            {
                var rng = new Rng(seed + (ulong)i).Fork("duel");
                bool swap = (i & 1) == 1;
                var w = swap ? Fight(tu, b, a, gapMm, rng, out _) : Fight(tu, a, b, gapMm, rng, out _);
                if (w == Winner.Draw) d++;
                else if ((w == Winner.A) != swap) wa++;
                else wb++;
            }
            return (wa, wb, d);
        }
    }
}
