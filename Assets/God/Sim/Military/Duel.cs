namespace God.Sim
{
    /// <summary>How one attack on a soldier ended.</summary>
    public enum AttackOutcome
    {
        None = 0,
        Miss = 1,
        /// <summary>Stopped by a shield or a parry (an axe still chips through).</summary>
        Blocked = 2,
        Hit = 3,
        /// <summary>A critical hit: double damage.</summary>
        Crit = 4,
        /// <summary>A strike on a building (no rolls).</summary>
        Building = 5,
    }

    public readonly struct AttackResult
    {
        public readonly AttackOutcome Outcome;
        public readonly int Damage;
        public AttackResult(AttackOutcome o, int d) { Outcome = o; Damage = d; }
        public override string ToString() => Outcome + " " + Damage;
    }

    /// <summary>
    /// One attack of a duel (plan stage 3, "Combat: live duels in rounds"). Pure: stats in, rolls from the given
    /// <see cref="Rng"/>, result out. Rolls are taken in a fixed order and only when they can matter (hit; block when the
    /// target has any block; crit unless the target stops crits), so a game replays exactly.
    /// </summary>
    public static class Duel
    {
        /// <summary>Hit chance in percent: the weapon's hit % minus the target's dodge, clamped to 0..100.</summary>
        public static int HitChance(in SoldierStats a, in SoldierStats t) => Clamp(a.W.HitPct - t.DodgePct);

        /// <summary>
        /// Block chance in percent: the target's shield block (its ranged block against a bow or sling), plus the sword
        /// parry when it holds a sword and no shield, then halved by an axe.
        /// </summary>
        public static int BlockChance(in SoldierStats a, in SoldierStats t)
        {
            int block = a.Ranged ? t.BlockVsRangedPct : t.BlockPct;
            if (t.W.Special == WeaponSpecial.Parry && !t.HasShield) block += MilitaryTuning.ParryBonusPct;
            if (a.W.Special == WeaponSpecial.ShieldBreaker) block /= MilitaryTuning.ShieldBreakerDivisor;
            return Clamp(block);
        }

        /// <summary>Damage of a blow that lands: doubled on a crit, minus <c>max(0, armour - pierce)</c>, at least 1.</summary>
        public static int HitDamage(in SoldierStats a, in SoldierStats t, bool crit)
        {
            int d = a.W.Damage * (crit ? MilitaryTuning.CritMultiplier : 1);
            int cut = t.Armour - a.W.Pierce;
            if (cut > 0) d -= cut;
            return d < MilitaryTuning.MinDamage ? MilitaryTuning.MinDamage : d;
        }

        public static AttackResult Resolve(in SoldierStats a, in SoldierStats t, Rng rng)
        {
            if (rng.NextInt(100) >= HitChance(a, t)) return new AttackResult(AttackOutcome.Miss, 0);
            int block = BlockChance(a, t);
            if (block > 0 && rng.NextInt(100) < block)
                return new AttackResult(AttackOutcome.Blocked, a.W.Special == WeaponSpecial.ShieldBreaker ? MilitaryTuning.AxeChipDamage : 0);
            bool crit = !t.StopsCrits && MilitaryTuning.CritPct > 0 && rng.NextInt(100) < MilitaryTuning.CritPct;
            return new AttackResult(crit ? AttackOutcome.Crit : AttackOutcome.Hit, HitDamage(a, t, crit));
        }

        public static AttackResult Resolve(Kit attacker, Kit target, Rng rng) => Resolve(MilitaryTuning.Of(attacker), MilitaryTuning.Of(target), rng);

        private static int Clamp(int p) => p < 0 ? 0 : p > 100 ? 100 : p;
    }
}
