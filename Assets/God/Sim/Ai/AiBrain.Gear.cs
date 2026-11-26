using System;
using System.Collections.Generic;

namespace God.Sim
{
    public sealed partial class AiBrain
    {
        // ---------------- the gear plan (Q4) ----------------

        /// <summary>Planned weapons, most wanted first: a melee weapon, a ranged one, later a second melee weapon.</summary>
        private readonly List<Ware> _weapons = new List<Ware>();
        /// <summary>Planned defence items, most wanted first (none until the weapons run).</summary>
        private readonly List<Ware> _defence = new List<Ware>();
        /// <summary>The faction's soldiers by weapon (index in <see cref="Gear.Attack"/>).</summary>
        private readonly int[] _byWeapon = new int[5];

        /// <summary>The planned weapons as of the last think (for tests and logs).</summary>
        public IReadOnlyList<Ware> PlannedWeapons => _weapons;
        /// <summary>The planned defence items as of the last think.</summary>
        public IReadOnlyList<Ware> PlannedDefence => _defence;

        /// <summary>The armoury kind that makes a piece of gear.</summary>
        public static string ArmouryOf(Ware gear) => EconomyDefs.MakerOf(gear)?.Kind;

        /// <summary>Raws the chain of a ware needs (a raw itself; a processed ware its two raws; gear the raws of its two inputs).</summary>
        private static void RawsOf(Ware w, List<Ware> into)
        {
            if (Wares.IsRaw(w)) { if (!into.Contains(w)) into.Add(w); return; }
            var maker = EconomyDefs.MakerOf(w);
            if (maker == null) return;
            for (int i = 1; i < Wares.Count; i++) if (maker.In[i] > 0) RawsOf((Ware)i, into);
        }

        private readonly List<Ware> _rawScratch = new List<Ware>();

        /// <summary>
        /// How much the faction wants a piece of gear now: land for every raw of its chain, an armoury or input processors
        /// already standing (a plan sticks once started), and a fixed per-faction taste so factions on similar land still
        /// arm differently. Stateless: a function of the game seed, the faction and the state.
        /// </summary>
        private int GearScore(Ware g)
        {
            int score = 0;
            _rawScratch.Clear();
            RawsOf(g, _rawScratch);
            foreach (var r in _rawScratch) score += _lands.Contains(LandOf(r)) ? 0 : -Tune.GearMissingLandPenalty;
            var arm = EconomyDefs.MakerOf(g);
            if (Have(arm.Kind) > 0) score += Tune.GearArmouryBonus;
            for (int i = 1; i < Wares.Count; i++)
                if (arm.In[i] > 0) { var p = EconomyDefs.MakerOf((Ware)i); if (p != null && Have(p.Kind) > 0) score += Tune.GearProcessorBonus; }
            score += new Rng(S.Seed).Fork("gear." + (int)Side + "." + (int)g).NextInt(Tune.GearTaste);
            return score;
        }

        private Ware Best(IEnumerable<Ware> pool, ICollection<Ware> except)
        {
            Ware best = Ware.None; int bs = int.MinValue;
            foreach (var g in pool)
            {
                if (except.Contains(g)) continue;
                int sc = GearScore(g);
                if (sc > bs) { bs = sc; best = g; }
            }
            return best;
        }

        private static readonly ReadOnlyArray<Ware> Melee = new ReadOnlyArray<Ware>(Ware.Sword, Ware.Spear, Ware.Axe);
        private static readonly ReadOnlyArray<Ware> RangedWeapons = new ReadOnlyArray<Ware>(Ware.Bow, Ware.Sling);

        /// <summary>
        /// The plan: the best melee weapon and the best ranged one; a second melee weapon once both armouries stand and the
        /// faction has <see cref="AiTuning.ThirdWeaponFromHqs"/> HQs; a defence item once the weapons are made; a second one
        /// with <see cref="AiTuning.SecondDefenceFromHqs"/> HQs.
        /// </summary>
        private void PlanGear()
        {
            _weapons.Clear(); _defence.Clear();
            Array.Clear(_byWeapon, 0, _byWeapon.Length);
            foreach (var u in _mine) { int i = Gear.Attack.IndexOf(MilitaryTuning.Normalize(u.Kit).Weapon); if (i >= 0) _byWeapon[i]++; }
            _weapons.Add(Best(Melee, _weapons));
            _weapons.Add(Best(RangedWeapons, _weapons));
            bool armed = Built(ArmouryOf(_weapons[0])) > 0 && Built(ArmouryOf(_weapons[1])) > 0;
            if (armed && _hqs.Count >= Tune.ThirdWeaponFromHqs) _weapons.Add(Best(Melee, _weapons));
            if (!armed) return;
            _defence.Add(Best(Gear.Defence, _defence));
            if (_hqs.Count >= Tune.SecondDefenceFromHqs && Built(ArmouryOf(_defence[0])) > 0) _defence.Add(Best(Gear.Defence, _defence));
        }

        /// <summary>
        /// Wants the whole chain of one piece of gear: a harvester for every raw on the faction's own land, a processor for
        /// every input whose raws it can get (own land or traded stock; the rest it trades for ready-made), then the armoury.
        /// </summary>
        private void WantGear(Ware g, int maxSites)
        {
            var arm = EconomyDefs.MakerOf(g);
            for (int i = 1; i < Wares.Count; i++)
            {
                if (arm.In[i] <= 0) continue;
                var p = EconomyDefs.MakerOf((Ware)i);
                if (p == null) continue;
                if (!OwnLandFor(p.Kind)) continue;
                for (int r = 1; r < Wares.Count; r++) if (p.In[r] > 0 && _lands.Contains(LandOf((Ware)r))) Want(HarvesterOf((Ware)r), 1, maxSites);
                Want(p.Kind, 1, maxSites);
            }
            Want(arm.Kind, 1, maxSites);
        }

        /// <summary>
        /// Steers the barracks: the planned weapon furthest below its share of the army (melee and ranged by
        /// <see cref="AiTuning.RangedSharePercent"/>) gets High, and while it is in stock the other weapons get Stop so the
        /// barracks holds the wanted one most. Defence items stay Normal: the barracks fits whatever it has.
        /// </summary>
        private void SteerBarracks(EcoBuilding b)
        {
            int total = 0; foreach (int n in _byWeapon) total += n;
            Ware want = Ware.None; long bestGap = long.MinValue;
            int ranged = 0, melee = 0;
            foreach (var w in _weapons) if (Gear.IsRanged(w)) ranged++; else melee++;
            foreach (var w in _weapons)
            {
                int share = Gear.IsRanged(w) ? Tune.RangedSharePercent / Math.Max(1, ranged) : (100 - Tune.RangedSharePercent) / Math.Max(1, melee);
                long gap = (long)share * (total + 1) - 100L * _byWeapon[Gear.Attack.IndexOf(w)];
                if (gap > bestGap) { bestGap = gap; want = w; }
            }
            bool inStock = want != Ware.None && (_free[(int)want] > 0 || b.In[(int)want] > 0 || OutOf(want) > 0);
            foreach (var w in Gear.Attack)
            {
                Priority p = w == want ? Priority.High : inStock ? Priority.Stop : Priority.Normal;
                SetPrio(b, w, p);
            }
        }

        /// <summary>Units of a ware waiting at the faction's workshops (made, not yet carried off).</summary>
        private int OutOf(Ware w)
        {
            int n = 0;
            foreach (var b in E.Buildings) if (b.Faction == Side && !b.IsHq && b.Built) n += b.Out[(int)w];
            return n;
        }

        // ---------------- kit-aware strength ----------------

        /// <summary>
        /// Fighting worth of a kit in the AI's sums: <see cref="AiTuning.InfantryStrength"/> for a bare sword, scaled by
        /// the square root of (its damage per tick against a bare sword) times (the hits a bare sword needs to fell it), so
        /// a full kit counts about twice a bare weapon. Integers only.
        /// </summary>
        public static int KitStrength(Kit kit, int baseStrength) => KitStrength(new MilitaryTuning(), kit, baseStrength);

        /// <summary>As <see cref="KitStrength(Kit, int)"/> under the game's tuning.</summary>
        public static int KitStrength(MilitaryTuning tu, Kit kit, int baseStrength)
        {
            var bare = tu.Of(new Kit(Ware.Sword));
            var me = tu.Of(kit);
            long off = Offence(tu, me, bare), offRef = Offence(tu, bare, bare);
            long tough = Toughness(tu, me, bare), toughRef = Toughness(tu, bare, bare);
            // per = 100 * sqrt(off / offRef * tough / toughRef)
            int per = Pathing.ISqrt(off * tough * 10000L / Math.Max(1, offRef * toughRef));
            return Math.Max(1, (baseStrength * per + 50) / 100);
        }

        /// <summary>Expected damage per 1000 ticks of <paramref name="a"/> against <paramref name="t"/> (no crits).</summary>
        private static long Offence(MilitaryTuning tu, in SoldierStats a, in SoldierStats t)
        {
            long hit = Duel.HitChance(tu, a, t), block = Duel.BlockChance(tu, a, t);
            long dmg = Duel.HitDamage(tu, a, t, false);
            long blocked = a.W.Special == WeaponSpecial.ShieldBreaker ? tu.AxeChipDamage : 0;
            return hit * ((100 - block) * dmg + block * blocked) * 1000 / (10000L * Math.Max(1, a.CooldownTicks)) + 1;
        }

        /// <summary>Health over the damage per attack a bare sword deals to <paramref name="t"/> (times 100).</summary>
        private static long Toughness(MilitaryTuning tu, in SoldierStats t, in SoldierStats sword)
        {
            long hit = Duel.HitChance(tu, sword, t), block = Duel.BlockChance(tu, sword, t), dmg = Duel.HitDamage(tu, sword, t, false);
            long perAttack = hit * (100 - block) * dmg; // times 10000
            return t.MaxHealth * 1000000L / Math.Max(1, perAttack);
        }
    }
}
