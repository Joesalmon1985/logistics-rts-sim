using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>Stage 3 combat: gear stats, the attack rolls and their specials, turn order, ranged stops, determinism, matchups.</summary>
    public class DuelTests
    {
        private static SoldierStats St(Ware weapon, Ware shield = Ware.None, Ware head = Ware.None, Ware body = Ware.None) =>
            new MilitaryTuning().Of(new Kit(weapon, shield, head, body));

        private static List<AttackResult> Many(SoldierStats a, SoldierStats t, int n, ulong seed = 11)
        {
            var rng = new Rng(seed);
            var list = new List<AttackResult>(n);
            for (int i = 0; i < n; i++) list.Add(Duel.Resolve(a, t, rng));
            return list;
        }

        // ---------------- tables ----------------

        [Test]
        public void GearStats_FollowThePlanTables()
        {
            void W(Ware w, int dmg, int pierce, int hit, int cd, int range, int bld, WeaponSpecial sp)
            {
                var s = new MilitaryTuning().WeaponOf(w);
                Assert.AreEqual(new[] { dmg, pierce, hit, cd, range, bld, (int)sp }, new[] { s.Damage, s.Pierce, s.HitPct, s.CooldownTicks, s.RangeMm, s.BuildingPct, (int)s.Special }, Wares.Name(w));
            }
            // Q4 balance (plan stage 3, "Combat" table as tuned): fast parrying sword, piercing first-strike spear, heavy
            // shield-breaking axe, long-range bow, quick cheap sling.
            W(Ware.Sword, 11, 1, 80, 9, 2000, 100, WeaponSpecial.Parry);
            W(Ware.Spear, 11, 4, 90, 10, 3500, 75, WeaponSpecial.FirstStrike);
            W(Ware.Axe, 19, 3, 75, 13, 2000, 200, WeaponSpecial.ShieldBreaker);
            W(Ware.Bow, 8, 2, 65, 13, 23000, 25, WeaponSpecial.None);
            W(Ware.Sling, 6, 1, 75, 8, 16000, 25, WeaponSpecial.Cheap);

            void D(Ware w, int block, int vsRanged, int armour, int dodge, int speed, bool crits)
            {
                var s = new MilitaryTuning().DefenceOf(w);
                Assert.AreEqual(new[] { block, vsRanged, armour, dodge, speed, crits ? 1 : 0 }, new[] { s.BlockPct, s.BlockVsRangedPct, s.Armour, s.DodgePct, s.SpeedPct, s.StopsCrits ? 1 : 0 }, Wares.Name(w));
            }
            D(Ware.BigShield, 25, 45, 0, 0, -10, false);
            D(Ware.SmallShield, 15, 15, 0, 3, 0, false);
            D(Ware.Helmet, 0, 0, 2, 0, 0, true);
            D(Ware.Breastplate, 0, 0, 4, -5, -15, false);
            D(Ware.LightArmour, 0, 0, 1, 5, 0, false);

            var bare = St(Ware.Sword);
            Assert.AreEqual(100, bare.MaxHealth); Assert.AreEqual(300, bare.SpeedMmPerTick); Assert.AreEqual(5, bare.DodgePct, "base dodge");
            Assert.AreEqual(0, bare.Armour); Assert.AreEqual(0, bare.BlockPct);
            var full = St(Ware.Spear, Ware.SmallShield, Ware.Helmet, Ware.LightArmour);
            Assert.AreEqual(13, full.DodgePct); Assert.AreEqual(3, full.Armour); Assert.AreEqual(15, full.BlockPct); Assert.IsTrue(full.StopsCrits);
            Assert.AreEqual(0, St(Ware.Axe, Ware.None, Ware.None, Ware.Breastplate).DodgePct, "5 base - 5");
            Assert.AreEqual(23000, St(Ware.Bow).SightMm, "a bow sees as far as it shoots");
            Assert.AreEqual(23000, MilitaryTuning.SightMm, "and so does everyone: no one is shot by a bow it cannot see");
            Assert.AreEqual(MilitaryTuning.SightMm, St(Ware.Sword).SightMm);
            Assert.AreEqual(11, new MilitaryTuning().BuildingDamage(Ware.Sword));
            Assert.AreEqual(38, new MilitaryTuning().BuildingDamage(Ware.Axe));
            Assert.AreEqual(8, new MilitaryTuning().BuildingDamage(Ware.Spear));
            Assert.AreEqual(2, new MilitaryTuning().BuildingDamage(Ware.Bow));
            Assert.AreEqual(1, new MilitaryTuning().BuildingDamage(Ware.Sling));
        }

        [Test]
        public void Kit_IsNormalised_NoWeaponIsABareSword_ABowDropsItsShield()
        {
            Assert.AreEqual(new Kit(Ware.Sword), MilitaryTuning.Normalize(default));
            Assert.AreEqual(new Kit(Ware.Sword), MilitaryTuning.Normalize(new Kit(Ware.None, Ware.BigShield, Ware.Helmet)), "no weapon: no defence either");
            Assert.AreEqual(new Kit(Ware.Bow, Ware.None, Ware.Helmet), MilitaryTuning.Normalize(new Kit(Ware.Bow, Ware.SmallShield, Ware.Helmet)));
            Assert.AreEqual(new Kit(Ware.Axe), MilitaryTuning.Normalize(new Kit(Ware.Axe, Ware.Helmet, Ware.BigShield, Ware.Sword)), "items in the wrong slot are dropped");
            Assert.AreEqual(2, MilitaryTuning.DefencePieces(new Kit(Ware.Sling, Ware.SmallShield, Ware.None, Ware.LightArmour)));
        }

        // ---------------- the attack ----------------

        [Test]
        public void ArmourAndPierce_Math()
        {
            Assert.AreEqual(11, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword), false));
            Assert.AreEqual(22, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword), true), "crit doubles");
            Assert.AreEqual(8, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword, Ware.None, Ware.None, Ware.Breastplate), false), "11 - (4 - 1)");
            Assert.AreEqual(19, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword, Ware.None, Ware.None, Ware.Breastplate), true), "22 - 3");
            Assert.AreEqual(10, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword, Ware.None, Ware.Helmet), false), "armour 2 - pierce 1 = 1");
            Assert.AreEqual(16, Duel.HitDamage(St(Ware.Axe), St(Ware.Sword, Ware.None, Ware.Helmet, Ware.Breastplate), false), "19 - (6 - 3)");
            Assert.AreEqual(9, Duel.HitDamage(St(Ware.Spear), St(Ware.Sword, Ware.None, Ware.Helmet, Ware.Breastplate), false), "11 - (6 - 4): the spear pierces armour best");
            Assert.AreEqual(6, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword, Ware.None, Ware.Helmet, Ware.Breastplate), false), "11 - (6 - 1): the sword does not");
            Assert.AreEqual(8, Duel.HitDamage(St(Ware.Bow), St(Ware.Sword, Ware.None, Ware.None, Ware.LightArmour), false), "pierce 2 beats armour 1: no bonus");
            Assert.AreEqual(1, Duel.HitDamage(St(Ware.Sling), St(Ware.Sword, Ware.None, Ware.Helmet, Ware.Breastplate), false), "6 - (6 - 1)");
            // M0: tuning is per sim (SimTuning), so the test changes its own copy instead of a shared static.
            var tuned = new MilitaryTuning { MinDamage = 3 };
            Assert.AreEqual(3, Duel.HitDamage(tuned, St(Ware.Sling), St(Ware.Sword, Ware.None, Ware.Helmet, Ware.Breastplate), false), "at least the minimum");
            Assert.AreEqual(1, Duel.HitDamage(St(Ware.Sling), St(Ware.Sword, Ware.None, Ware.Helmet, Ware.Breastplate), false), "the default tuning is untouched");
        }

        [Test]
        public void HitChance_IsHitMinusDodge()
        {
            Assert.AreEqual(75, Duel.HitChance(St(Ware.Sword), St(Ware.Sword)));
            Assert.AreEqual(70, Duel.HitChance(St(Ware.Sword), St(Ware.Axe, Ware.None, Ware.None, Ware.LightArmour)), "80 - 10");
            Assert.AreEqual(80, Duel.HitChance(St(Ware.Sword), St(Ware.Axe, Ware.None, Ware.None, Ware.Breastplate)), "breastplate takes the base dodge away");
            Assert.AreEqual(62, Duel.HitChance(St(Ware.Sling), St(Ware.Axe, Ware.SmallShield, Ware.None, Ware.LightArmour)), "75 - 13");
            var misses = Many(St(Ware.Sling), St(Ware.Axe, Ware.SmallShield, Ware.None, Ware.LightArmour), 4000).Count(r => r.Outcome == AttackOutcome.Miss);
            Assert.That(misses, Is.InRange(1380, 1660), "about 38% miss");
        }

        [Test]
        public void CloseQuarters_ABowOrSlingWithAFoeAtHandHitsHalfAsOften()
        {
            Assert.AreEqual(70, Duel.HitChance(St(Ware.Sling), St(Ware.Axe)));
            Assert.AreEqual(35, Duel.HitChance(St(Ware.Sling), St(Ware.Axe), true), "half in close quarters");
            Assert.AreEqual(30, Duel.HitChance(St(Ware.Bow), St(Ware.Axe), true), "60 / 2");
            Assert.AreEqual(75, Duel.HitChance(St(Ware.Sword), St(Ware.Axe), true), "melee is not affected");
            var far = Many(St(Ware.Bow), St(Ware.Axe), 4000).Count(r => r.Outcome != AttackOutcome.Miss);
            var rng = new Rng(11);
            int near = 0;
            for (int i = 0; i < 4000; i++) if (Duel.Resolve(St(Ware.Bow), St(Ware.Axe), rng, true).Outcome != AttackOutcome.Miss) near++;
            Assert.That(near * 100 / far, Is.InRange(42, 58), "about half the arrows land at close quarters");

            // On the map: a bow shooting an axe that stands next to it uses the close-quarters chance.
            var b = new Bed();
            var (start, _) = b.LandLine(b.PlayerHq, 1);
            var bow = b.Spawn(Faction.F0, new Kit(Ware.Bow), start);
            var axe = b.Spawn(Faction.F1, new Kit(Ware.Axe), start);
            axe.Health = 100000; bow.Health = 100000;
            b.Log.Clear();
            b.Step(1300);
            var shots = b.Events<UnitFired>().Where(e => e.AttackerId == bow.Id).ToList();
            Assert.Greater(shots.Count, 80);
            int landed = shots.Count(e => e.Outcome != AttackOutcome.Miss);
            Assert.That(landed * 100 / shots.Count, Is.InRange(18, 42), "about 30% land (60% halved)");
        }

        [Test]
        public void Special_Parry_SwordWithoutAShieldBlocks10()
        {
            Assert.AreEqual(10, Duel.BlockChance(St(Ware.Spear), St(Ware.Sword)));
            Assert.AreEqual(0, Duel.BlockChance(St(Ware.Spear), St(Ware.Spear)), "no parry with a spear");
            Assert.AreEqual(15, Duel.BlockChance(St(Ware.Spear), St(Ware.Sword, Ware.SmallShield)), "with a shield: the shield only");
            Assert.AreEqual(5, Duel.BlockChance(St(Ware.Axe), St(Ware.Sword)), "an axe halves the parry too");
            var r = Many(St(Ware.Spear), St(Ware.Sword), 4000);
            int landed = r.Count(x => x.Outcome != AttackOutcome.Miss);
            int blocked = r.Count(x => x.Outcome == AttackOutcome.Blocked);
            Assert.That(blocked * 100 / landed, Is.InRange(8, 12), "about 10% of blows that land are parried");
            Assert.IsTrue(r.Where(x => x.Outcome == AttackOutcome.Blocked).All(x => x.Damage == 0));
        }

        [Test]
        public void Special_ShieldBreaker_HalvesBlock_AndChipsThrough()
        {
            var big = St(Ware.Sword, Ware.BigShield);
            Assert.AreEqual(25, Duel.BlockChance(St(Ware.Sword), big));
            Assert.AreEqual(12, Duel.BlockChance(St(Ware.Axe), big));
            var axe = Many(St(Ware.Axe), big, 4000);
            var sword = Many(St(Ware.Sword), big, 4000);
            Assert.IsTrue(axe.Where(x => x.Outcome == AttackOutcome.Blocked).All(x => x.Damage == 3), "a blocked axe still does 3");
            Assert.IsTrue(sword.Where(x => x.Outcome == AttackOutcome.Blocked).All(x => x.Damage == 0));
            int axeLanded = axe.Count(x => x.Outcome != AttackOutcome.Miss), swordLanded = sword.Count(x => x.Outcome != AttackOutcome.Miss);
            Assert.That(axe.Count(x => x.Outcome == AttackOutcome.Blocked) * 100 / axeLanded, Is.InRange(9, 15));
            Assert.That(sword.Count(x => x.Outcome == AttackOutcome.Blocked) * 100 / swordLanded, Is.InRange(21, 29));
        }

        [Test]
        public void Special_BigShield_BlocksMoreAgainstBowsAndSlings()
        {
            var big = St(Ware.Axe, Ware.BigShield);
            var small = St(Ware.Axe, Ware.SmallShield);
            Assert.AreEqual(45, Duel.BlockChance(St(Ware.Bow), big));
            Assert.AreEqual(45, Duel.BlockChance(St(Ware.Sling), big));
            Assert.AreEqual(25, Duel.BlockChance(St(Ware.Spear), big));
            Assert.AreEqual(15, Duel.BlockChance(St(Ware.Bow), small), "the small shield is the same against both");
            var r = Many(St(Ware.Bow), big, 4000);
            int landed = r.Count(x => x.Outcome != AttackOutcome.Miss);
            Assert.That(r.Count(x => x.Outcome == AttackOutcome.Blocked) * 100 / landed, Is.InRange(41, 49));
        }

        [Test]
        public void Special_Helmet_StopsCriticalHits()
        {
            var bare = Many(St(Ware.Sword), St(Ware.Spear), 4000);
            var helm = Many(St(Ware.Sword), St(Ware.Spear, Ware.None, Ware.Helmet), 4000);
            int hits = bare.Count(x => x.Outcome == AttackOutcome.Hit || x.Outcome == AttackOutcome.Crit);
            Assert.That(bare.Count(x => x.Outcome == AttackOutcome.Crit) * 100 / hits, Is.InRange(8, 12), "about 10% crits");
            Assert.IsTrue(bare.Where(x => x.Outcome == AttackOutcome.Crit).All(x => x.Damage == 22));
            Assert.AreEqual(0, helm.Count(x => x.Outcome == AttackOutcome.Crit), "never a crit against a helmet");
            Assert.Greater(helm.Count(x => x.Outcome == AttackOutcome.Hit), 2000);
        }

        [Test]
        public void Resolve_IsDeterministic_SameSeedSameResults()
        {
            var a = St(Ware.Axe, Ware.SmallShield); var t = St(Ware.Sword, Ware.None, Ware.None, Ware.LightArmour);
            var x = Many(a, t, 500, 99).Select(r => r.ToString()).ToList();
            var y = Many(a, t, 500, 99).Select(r => r.ToString()).ToList();
            var z = Many(a, t, 500, 98).Select(r => r.ToString()).ToList();
            CollectionAssert.AreEqual(x, y);
            CollectionAssert.AreNotEqual(x, z);
            Assert.AreEqual(DuelSim.Series(new Kit(Ware.Spear), new Kit(Ware.Bow), 50, 7, 25000), DuelSim.Series(new Kit(Ware.Spear), new Kit(Ware.Bow), 50, 7, 25000));
        }

        // ---------------- on the map ----------------

        [Test]
        public void Special_FirstStrike_ASpearIsReadyWhenAMeleeFightStarts()
        {
            var b = new Bed();
            var (start, dir) = b.LandLine(b.PlayerHq, 4);
            var sword = b.Spawn(Faction.F0, new Kit(Ware.Sword), start);
            var spear = b.Spawn(Faction.F1, new Kit(Ware.Spear), b.Walk(start, dir, 4));
            b.Log.Clear();
            // Both just struck at something else: neither weapon is ready.
            sword.Cooldown = 7; spear.Cooldown = 9;
            Assert.IsTrue(b.RunUntil(200, () => b.Events<UnitFired>().Any()));
            var first = b.Events<UnitFired>().First();
            Assert.AreEqual(spear.Id, first.AttackerId, "the spear strikes first");
            Assert.LessOrEqual(Pathing.Dist(first.FromX, first.FromZ, first.ToX, first.ToZ), 3500, "from its 3.5 m reach");
            Assert.AreEqual(1, b.Events<UnitFired>().Count(e => e.Tick == first.Tick), "alone in that tick");

            // Only when a fight starts: while the same fight goes on, the spear waits like anyone else.
            var c = new Bed();
            var (s2, d2) = c.LandLine(c.PlayerHq, 1);
            var a1 = c.Spawn(Faction.F0, new Kit(Ware.Sword), s2);
            var a2 = c.Spawn(Faction.F1, new Kit(Ware.Spear), s2);
            c.Step(30);
            int engaged = c.Events<UnitFired>().Count(e => e.AttackerId == a2.Id);
            a2.Cooldown = 9;
            c.Log.Clear();
            c.Step();
            Assert.AreEqual(0, c.Events<UnitFired>().Count(e => e.AttackerId == a2.Id), "no reset while the same fight goes on");
            Assert.Greater(engaged, 0);
        }

        [Test]
        public void Ranged_StopsAtItsRange_MeleeClosesIn()
        {
            foreach (var w in new[] { Ware.Bow, Ware.Sling, Ware.Sword })
            {
                var b = new Bed();
                var target = b.EnemySmall(0);
                foreach (var r in b.M.Buildings) r.Health = r.MaxHealth = 1000000;
                b.Do(new MarkTarget(Faction.F0, target.Id));
                var u = b.Spawn(Faction.F0, new Kit(w), b.PlayerHq.Node);
                b.Log.Clear();
                Assert.IsTrue(b.RunUntil(3000, () => b.Events<UnitFired>().Any()), Wares.Name(w) + " reaches something");
                var shot = b.Events<UnitFired>().First();
                Assert.AreEqual(TargetKind.Building, shot.TargetKind);
                var rec = b.M.BuildingById(shot.TargetId);
                int edge = Math.Max(0, Pathing.Dist(u.X, u.Z, rec.X, rec.Z) - MilitaryTuning.FootprintMm(rec.Size));
                int range = new MilitaryTuning().WeaponOf(w).RangeMm;
                Assert.LessOrEqual(edge, range, Wares.Name(w));
                Assert.Greater(edge, range - 400, Wares.Name(w) + " stopped as soon as it was in range");
                int x = u.X, z = u.Z;
                b.Step(40);
                Assert.AreEqual(x, u.X); Assert.AreEqual(z, u.Z);
                Assert.Greater(b.Events<UnitFired>().Count(e => e.AttackerId == u.Id), 2, "and keeps shooting from there");
            }
        }

        [Test]
        public void Ranged_ShootsAMeleeSoldier_WhileItClosesIn()
        {
            var b = new Bed();
            var (start, dir) = b.LandLine(b.PlayerHq, 4);
            var bow = b.Spawn(Faction.F0, new Kit(Ware.Bow), start);
            var axe = b.Spawn(Faction.F1, new Kit(Ware.Axe), b.Walk(start, dir, 4));
            Assert.IsTrue(b.RunUntil(300, () => b.Events<UnitFired>().Any(e => e.AttackerId == axe.Id) || !b.M.Units.Contains(axe)));
            var bowShots = b.Events<UnitFired>().Where(e => e.AttackerId == bow.Id).ToList();
            Assert.Greater(bowShots.Count, 0);
            Assert.IsTrue(bowShots.All(e => e.Ranged && e.Weapon == Ware.Bow));
            Assert.Greater(Pathing.Dist(bowShots[0].FromX, bowShots[0].FromZ, bowShots[0].ToX, bowShots[0].ToZ), 2000, "the first arrow flies before the axe is close");
        }

        [Test]
        public void Combat_IsDeterministic_AndSurvivesASaveMidFight()
        {
            GodSim Fight(ulong seed)
            {
                var b = new Bed(seed);
                var (start, dir) = b.LandLine(b.PlayerHq, 3);
                b.Spawn(Faction.F0, new Kit(Ware.Axe, Ware.SmallShield, Ware.Helmet), start);
                b.Spawn(Faction.F0, new Kit(Ware.Bow, Ware.None, Ware.None, Ware.LightArmour), start);
                b.Spawn(Faction.F1, new Kit(Ware.Spear, Ware.BigShield, Ware.None, Ware.Breastplate), b.Walk(start, dir, 3));
                b.Spawn(Faction.F1, new Kit(Ware.Sword), b.Walk(start, dir, 3));
                b.Step(25);
                return b.Sim;
            }
            var a = Fight(5); var c = Fight(5);
            Assert.AreEqual(a.StateHash(), c.StateHash());
            Assert.Greater(a.State.Military.Units.Count(u => u.Health < 100), 0, "mid-fight");

            var loaded = SaveFile.FromJson(SaveFile.From(c).ToJson()).ToSim();
            Assert.AreEqual(a.StateHash(), loaded.StateHash(), "same hash straight after the load");
            Assert.AreEqual(a.State.Rng.State, loaded.State.Rng.State, "the root stream the duel rolls are keyed from (M0: Rng.Fork(\"duel\", unit, tick)) is saved");
            for (int i = 0; i < 200; i++) { a.Step(); loaded.Step(); }
            Assert.AreEqual(a.StateHash(), loaded.StateHash(), "same fight after the load");
            Assert.AreNotEqual(Fight(5).StateHash(), Fight(6).StateHash());
        }

        // ---------------- matchups ----------------

        private static readonly (string name, Kit kit)[] Kits =
        {
            ("sword", new Kit(Ware.Sword)),
            ("spear", new Kit(Ware.Spear)),
            ("axe", new Kit(Ware.Axe)),
            ("bow", new Kit(Ware.Bow)),
            ("sling", new Kit(Ware.Sling)),
            ("sword+small+helm+light", new Kit(Ware.Sword, Ware.SmallShield, Ware.Helmet, Ware.LightArmour)),
            ("spear+big+breast", new Kit(Ware.Spear, Ware.BigShield, Ware.None, Ware.Breastplate)),
            ("axe+helm+breast", new Kit(Ware.Axe, Ware.None, Ware.Helmet, Ware.Breastplate)),
            ("bow+helm+light", new Kit(Ware.Bow, Ware.None, Ware.Helmet, Ware.LightArmour)),
            ("sling+small+light", new Kit(Ware.Sling, Ware.SmallShield, Ware.None, Ware.LightArmour)),
        };

        /// <summary>
        /// 200 seeded duels per pairing, starting 25 m apart (just beyond a bow's range). Prints the win-rate table (row's
        /// wins in percent against the column). Sanity bounds; the plan's 60% target is about real games (see the Q4 balance
        /// report in the play guide), where soldiers fight in crowds and ranged ones stand behind the melee.
        /// </summary>
        [Test]
        public void MatchupTable_NoKitBeatsEveryOtherByAWideMargin()
        {
            const int N = 200;
            int k = Kits.Length;
            var pct = new int[k, k];
            for (int i = 0; i < k; i++)
                for (int j = i + 1; j < k; j++)
                {
                    var (a, c, d) = DuelSim.Series(Kits[i].kit, Kits[j].kit, N, 1000UL + (ulong)(i * 31 + j), 25000);
                    pct[i, j] = (a * 100 + d * 50) / N; pct[j, i] = (c * 100 + d * 50) / N;
                }
            var sb = new StringBuilder();
            sb.AppendLine("Win % of the row against the column (" + N + " duels each, start 25 m apart; draws count half):");
            sb.Append("".PadRight(24));
            for (int j = 0; j < k; j++) sb.Append(("#" + j).PadLeft(5));
            sb.AppendLine("   avg");
            var avg = new int[k];
            for (int i = 0; i < k; i++)
            {
                sb.Append(("#" + i + " " + Kits[i].name).PadRight(24));
                int sum = 0;
                for (int j = 0; j < k; j++) { sb.Append((i == j ? "-" : pct[i, j].ToString()).PadLeft(5)); if (i != j) sum += pct[i, j]; }
                avg[i] = sum / (k - 1);
                sb.AppendLine(avg[i].ToString().PadLeft(6));
            }
            TestContext.WriteLine(sb.ToString());

            // No kit crushes every other kit (90% or more against all of them).
            for (int i = 0; i < k; i++)
            {
                int crushed = 0;
                for (int j = 0; j < k; j++) if (i != j && pct[i, j] >= 90) crushed++;
                Assert.Less(crushed, k - 1, Kits[i].name + " beats every other kit by 90% or more");
            }
            // Among the bare weapons (same gear count) none wins 95% or more against all the others.
            for (int i = 0; i < 5; i++)
            {
                int crushed = 0;
                for (int j = 0; j < 5; j++) if (i != j && pct[i, j] >= 95) crushed++;
                Assert.Less(crushed, 4, Kits[i].name + " beats every other bare weapon by 95% or more");
            }
            // Defence gear is worth making: each kitted soldier beats the bare one with its weapon.
            for (int i = 0; i < 5; i++) Assert.Greater(pct[i + 5, i], 50, Kits[i + 5].name + " against " + Kits[i].name);
            // Among the bare melee weapons none dominates: each wins 30 to 70% against the other two.
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) if (i != j) Assert.That(pct[i, j], Is.InRange(30, 70), Kits[i].name + " against " + Kits[j].name);
            // One on one, a bare bow or sling loses to bare melee: ranged soldiers belong behind a melee line.
            for (int i = 0; i < 3; i++) for (int j = 3; j < 5; j++) Assert.Less(pct[j, i], 50, Kits[j].name + " against " + Kits[i].name);
        }

        // ---------------- group battles ----------------

        private static Kit[] Rep(Ware w, int n) => Enumerable.Repeat(new Kit(w), n).ToArray();
        private static Kit[] Mix(Ware melee, int m, Ware ranged, int r) => Rep(melee, m).Concat(Rep(ranged, r)).ToArray();

        /// <summary>
        /// Five against five on a real island with the real military tick (<see cref="GroupBattle"/>): melee in front, ranged
        /// one step behind. Prints the table. Bounds: no bare melee weapon routs another (20 to 80%); a mixed line (three
        /// melee, two ranged) against five of the same melee still costs the five at least one and a half soldiers on
        /// average. In a straight line-up of equal numbers the all-melee side wins most battles: bows and slings earn their
        /// keep in real games, where they shoot from behind guards and busy melee (the Q4 balance report counts them as
        /// the kits with the best duel records there).
        /// </summary>
        [Test]
        public void GroupBattles_FiveAgainstFive_RangedBehindMelee()
        {
            const int N = 24;
            var sb = new StringBuilder();
            sb.AppendLine("Group battles, " + N + " each on islands 100.." + (100 + N - 1) + ": A's wins / B's wins / draws, soldiers left A / B (sum)");
            (int pa, double lostA) Run(string name, Kit[] a, Kit[] b)
            {
                var r = GroupBattle.Series(a, b, N, 100);
                sb.AppendLine("  " + name.PadRight(34) + (r.aWins * 100 / N).ToString().PadLeft(4) + "% /" + (r.bWins * 100 / N).ToString().PadLeft(4) + "% / " + r.draws
                    + "   left " + r.leftA + " / " + r.leftB);
                return ((r.aWins * 100 + r.draws * 50) / N, a.Length - (double)r.leftA / N);
            }
            var melee = new[] { Ware.Sword, Ware.Spear, Ware.Axe };
            var ranged = new[] { Ware.Bow, Ware.Sling };
            var duel = new List<(string, int)>();
            for (int i = 0; i < 3; i++) for (int j = i + 1; j < 3; j++)
                duel.Add(("5 " + melee[i] + " vs 5 " + melee[j], Run("5 " + melee[i] + " vs 5 " + melee[j], Rep(melee[i], 5), Rep(melee[j], 5)).pa));
            var cost = new List<double>();
            foreach (var m in melee) foreach (var r in ranged)
            {
                cost.Add(Run("5 " + m + " vs 3 " + m + " + 2 " + r, Rep(m, 5), Mix(m, 3, r, 2)).lostA);
                Run("5 " + m + " vs 5 " + r, Rep(m, 5), Rep(r, 5));
            }
            Run("5 bow vs 5 sling", Rep(Ware.Bow, 5), Rep(Ware.Sling, 5));
            TestContext.WriteLine(sb.ToString());
            foreach (var (name, pa) in duel) Assert.That(pa, Is.InRange(20, 80), name);
            Assert.GreaterOrEqual(cost.Average(), 1.5, "a mixed line costs five melee soldiers at least one and a half on average");
        }

        [Test]
        public void GroupBattle_IsDeterministic()
        {
            var a = GroupBattle.Fight(Mix(Ware.Axe, 3, Ware.Bow, 2), Rep(Ware.Spear, 5), 7);
            var b = GroupBattle.Fight(Mix(Ware.Axe, 3, Ware.Bow, 2), Rep(Ware.Spear, 5), 7);
            Assert.AreEqual(a.Winner, b.Winner); Assert.AreEqual(a.Ticks, b.Ticks); Assert.AreEqual(a.LeftA, b.LeftA); Assert.AreEqual(a.LeftB, b.LeftB);
            Assert.AreNotEqual(GroupBattle.Winner.Draw, a.Winner, "someone wins a five-a-side");
        }
    }
}
