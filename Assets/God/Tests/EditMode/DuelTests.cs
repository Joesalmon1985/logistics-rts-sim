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
            MilitaryTuning.Of(new Kit(weapon, shield, head, body));

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
                var s = MilitaryTuning.WeaponOf(w);
                Assert.AreEqual(new[] { dmg, pierce, hit, cd, range, bld, (int)sp }, new[] { s.Damage, s.Pierce, s.HitPct, s.CooldownTicks, s.RangeMm, s.BuildingPct, (int)s.Special }, Wares.Name(w));
            }
            W(Ware.Sword, 12, 2, 85, 8, 2000, 100, WeaponSpecial.Parry);
            W(Ware.Spear, 10, 3, 80, 10, 3500, 75, WeaponSpecial.FirstStrike);
            W(Ware.Axe, 16, 4, 75, 12, 2000, 200, WeaponSpecial.ShieldBreaker);
            W(Ware.Bow, 9, 1, 65, 14, 25000, 25, WeaponSpecial.None);
            W(Ware.Sling, 7, 0, 60, 9, 18000, 25, WeaponSpecial.Cheap);

            void D(Ware w, int block, int vsRanged, int armour, int dodge, int speed, bool crits)
            {
                var s = MilitaryTuning.DefenceOf(w);
                Assert.AreEqual(new[] { block, vsRanged, armour, dodge, speed, crits ? 1 : 0 }, new[] { s.BlockPct, s.BlockVsRangedPct, s.Armour, s.DodgePct, s.SpeedPct, s.StopsCrits ? 1 : 0 }, Wares.Name(w));
            }
            D(Ware.BigShield, 35, 55, 0, 0, -10, false);
            D(Ware.SmallShield, 25, 25, 0, 5, 0, false);
            D(Ware.Helmet, 0, 0, 2, 0, 0, true);
            D(Ware.Breastplate, 0, 0, 5, -5, -20, false);
            D(Ware.LightArmour, 0, 0, 2, 10, 0, false);

            var bare = St(Ware.Sword);
            Assert.AreEqual(100, bare.MaxHealth); Assert.AreEqual(300, bare.SpeedMmPerTick); Assert.AreEqual(5, bare.DodgePct, "base dodge");
            Assert.AreEqual(0, bare.Armour); Assert.AreEqual(0, bare.BlockPct);
            var full = St(Ware.Spear, Ware.SmallShield, Ware.Helmet, Ware.LightArmour);
            Assert.AreEqual(20, full.DodgePct); Assert.AreEqual(4, full.Armour); Assert.AreEqual(25, full.BlockPct); Assert.IsTrue(full.StopsCrits);
            Assert.AreEqual(0, St(Ware.Axe, Ware.None, Ware.None, Ware.Breastplate).DodgePct, "5 base - 5");
            Assert.AreEqual(25000, St(Ware.Bow).SightMm, "a bow sees as far as it shoots");
            Assert.AreEqual(MilitaryTuning.SightMm, St(Ware.Sword).SightMm);
            Assert.AreEqual(12, MilitaryTuning.BuildingDamage(Ware.Sword));
            Assert.AreEqual(32, MilitaryTuning.BuildingDamage(Ware.Axe));
            Assert.AreEqual(7, MilitaryTuning.BuildingDamage(Ware.Spear));
            Assert.AreEqual(2, MilitaryTuning.BuildingDamage(Ware.Bow));
            Assert.AreEqual(1, MilitaryTuning.BuildingDamage(Ware.Sling));
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
            Assert.AreEqual(12, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword), false));
            Assert.AreEqual(24, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword), true), "crit doubles");
            Assert.AreEqual(9, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword, Ware.None, Ware.None, Ware.Breastplate), false), "12 - (5 - 2)");
            Assert.AreEqual(21, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword, Ware.None, Ware.None, Ware.Breastplate), true), "24 - 3");
            Assert.AreEqual(12, Duel.HitDamage(St(Ware.Sword), St(Ware.Sword, Ware.None, Ware.Helmet), false), "armour 2 - pierce 2 = 0");
            Assert.AreEqual(13, Duel.HitDamage(St(Ware.Axe), St(Ware.Sword, Ware.None, Ware.Helmet, Ware.Breastplate), false), "16 - (7 - 4)");
            Assert.AreEqual(10, Duel.HitDamage(St(Ware.Spear), St(Ware.Sword, Ware.None, Ware.None, Ware.LightArmour), false), "pierce 3 beats armour 2: no bonus");
            Assert.AreEqual(8, Duel.HitDamage(St(Ware.Bow), St(Ware.Sword, Ware.None, Ware.None, Ware.LightArmour), false), "9 - (2 - 1)");
            Assert.AreEqual(1, Duel.HitDamage(St(Ware.Sling), St(Ware.Sword, Ware.None, Ware.Helmet, Ware.Breastplate), false), "7 - 7 = 0, at least 1");
        }

        [Test]
        public void HitChance_IsHitMinusDodge()
        {
            Assert.AreEqual(80, Duel.HitChance(St(Ware.Sword), St(Ware.Sword)));
            Assert.AreEqual(70, Duel.HitChance(St(Ware.Sword), St(Ware.Axe, Ware.None, Ware.None, Ware.LightArmour)), "85 - 15");
            Assert.AreEqual(85, Duel.HitChance(St(Ware.Sword), St(Ware.Axe, Ware.None, Ware.None, Ware.Breastplate)), "breastplate takes the base dodge away");
            Assert.AreEqual(40, Duel.HitChance(St(Ware.Sling), St(Ware.Axe, Ware.SmallShield, Ware.None, Ware.LightArmour)), "60 - 20");
            var misses = Many(St(Ware.Sling), St(Ware.Axe, Ware.SmallShield, Ware.None, Ware.LightArmour), 4000).Count(r => r.Outcome == AttackOutcome.Miss);
            Assert.That(misses, Is.InRange(2200, 2600), "about 60% miss");
        }

        [Test]
        public void Special_Parry_SwordWithoutAShieldBlocks10()
        {
            Assert.AreEqual(10, Duel.BlockChance(St(Ware.Spear), St(Ware.Sword)));
            Assert.AreEqual(0, Duel.BlockChance(St(Ware.Spear), St(Ware.Spear)), "no parry with a spear");
            Assert.AreEqual(25, Duel.BlockChance(St(Ware.Spear), St(Ware.Sword, Ware.SmallShield)), "with a shield: the shield only");
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
            Assert.AreEqual(35, Duel.BlockChance(St(Ware.Sword), big));
            Assert.AreEqual(17, Duel.BlockChance(St(Ware.Axe), big));
            var axe = Many(St(Ware.Axe), big, 4000);
            var sword = Many(St(Ware.Sword), big, 4000);
            Assert.IsTrue(axe.Where(x => x.Outcome == AttackOutcome.Blocked).All(x => x.Damage == 3), "a blocked axe still does 3");
            Assert.IsTrue(sword.Where(x => x.Outcome == AttackOutcome.Blocked).All(x => x.Damage == 0));
            int axeLanded = axe.Count(x => x.Outcome != AttackOutcome.Miss), swordLanded = sword.Count(x => x.Outcome != AttackOutcome.Miss);
            Assert.That(axe.Count(x => x.Outcome == AttackOutcome.Blocked) * 100 / axeLanded, Is.InRange(14, 20));
            Assert.That(sword.Count(x => x.Outcome == AttackOutcome.Blocked) * 100 / swordLanded, Is.InRange(31, 39));
        }

        [Test]
        public void Special_BigShield_BlocksMoreAgainstBowsAndSlings()
        {
            var big = St(Ware.Axe, Ware.BigShield);
            var small = St(Ware.Axe, Ware.SmallShield);
            Assert.AreEqual(55, Duel.BlockChance(St(Ware.Bow), big));
            Assert.AreEqual(55, Duel.BlockChance(St(Ware.Sling), big));
            Assert.AreEqual(35, Duel.BlockChance(St(Ware.Spear), big));
            Assert.AreEqual(25, Duel.BlockChance(St(Ware.Bow), small), "the small shield is the same against both");
            var r = Many(St(Ware.Bow), big, 4000);
            int landed = r.Count(x => x.Outcome != AttackOutcome.Miss);
            Assert.That(r.Count(x => x.Outcome == AttackOutcome.Blocked) * 100 / landed, Is.InRange(51, 59));
        }

        [Test]
        public void Special_Helmet_StopsCriticalHits()
        {
            var bare = Many(St(Ware.Sword), St(Ware.Spear), 4000);
            var helm = Many(St(Ware.Sword), St(Ware.Spear, Ware.None, Ware.Helmet), 4000);
            int hits = bare.Count(x => x.Outcome == AttackOutcome.Hit || x.Outcome == AttackOutcome.Crit);
            Assert.That(bare.Count(x => x.Outcome == AttackOutcome.Crit) * 100 / hits, Is.InRange(8, 12), "about 10% crits");
            Assert.IsTrue(bare.Where(x => x.Outcome == AttackOutcome.Crit).All(x => x.Damage == 24));
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
                int range = MilitaryTuning.WeaponOf(w).RangeMm;
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
            Assert.AreEqual(a.State.Military.Rng.State, loaded.State.Military.Rng.State, "the duel rolls' stream is saved");
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
        /// 200 seeded duels per pairing, starting 25 m apart (a bow's range). Prints the win-rate table (row's wins in
        /// percent against the column). Sanity bounds only; the plan's 60% target across a real game is package Q4's.
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
        }
    }
}
