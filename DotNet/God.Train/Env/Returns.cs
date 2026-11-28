using God.Sim.Learning;

namespace God.Train.Env
{
    /// <summary>
    /// What a body experiences, and nothing else: the twelve need pressures (permille, in the order Hunger, Energy, Warmth, Safety,
    /// Pain, Thirst, Hygiene, Bladder, Social, Fun, Comfort, Room), health (permille) and whether it is alive. The return function
    /// reads only this (D9.1 d, test <c>Returns_NoGoodsNoTitle</c>).
    /// </summary>
    public sealed class BodyReading
    {
        public readonly short[] Needs = new short[12];
        public int Health;
        public bool Alive;

        public void CopyFrom(BodyReading o) { System.Array.Copy(o.Needs, Needs, 12); Health = o.Health; Alive = o.Alive; }
    }

    /// <summary>
    /// The training return (thresholds file section 6): per tick and active channel the drive-change term
    /// <c>Learner.DriveReward(1000, 250, prev, now)</c> plus the level term <c>-(pressure - 250)+ / 4</c>, plus the health change;
    /// on the tick of death, once, the level term at full pressure on every active channel for every remaining tick (dying is
    /// never better than living in need). Integer, milli units. Reads the body only.
    /// </summary>
    public static class Returns
    {
        public const int Comfort = 250, Weight = 1000, LevelDiv = 4;

        public static long Tick(BodyReading prev, BodyReading now, int needMask, long ticksRemaining)
        {
            if (!prev.Alive) return 0;
            if (!now.Alive) return DeathTerm(needMask, ticksRemaining);
            long r = now.Health - prev.Health;
            for (int c = 0; c < 12; c++)
            {
                if ((needMask & (1 << c)) == 0) continue;
                r += Learner.DriveReward(Weight, Comfort, prev.Needs[c], now.Needs[c]);
                int over = now.Needs[c] - Comfort;
                if (over > 0) r -= over / LevelDiv;
            }
            return r;
        }

        public static long DeathTerm(int needMask, long ticksRemaining)
        {
            long perTick = 0;
            for (int c = 0; c < 12; c++) if ((needMask & (1 << c)) != 0) perTick += (1000 - Comfort) / LevelDiv;
            return -perTick * (ticksRemaining + 1);
        }
    }
}
