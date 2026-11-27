using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Population;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// F2b (LEAD_RULINGS R61, owner D4 §2): the partner-sex preference behind <c>Households.Prefs.PartnerSexMode</c>.
    /// F2b-T1: mode 0 (unset or explicit) hashes exactly as the base (the F2-T1 hashes); F2b-T2: mode 1 with every person
    /// seeking both sexes (the sex-blind control) behaves exactly as mode 0 (only the drawn preference is new state);
    /// F2b-T3: a partnership forms only between two people who each seek the other's sex, and it is a preference, not a
    /// restriction: same-sex partnerships form when people seek them, and reproduction outside a partnership still happens;
    /// F2b-T4: a save taken with mode 1 goes on exactly as the unsaved run; F2b-T5: the draw follows the stated distribution.
    /// </summary>
    [TestFixture]
    public sealed class DemogPartnerSexTests
    {
        private const string Mode = "tuning.Households.Prefs.PartnerSexMode=";

        /// <summary>
        /// The F2-T1 base hashes (tick 12000). Re-pinned at Checkpoint B (R60, ConceptionMode default 1): were
        /// 4e097b3731e62136, 561a1f8ef03ccfc3, 55a810bf69f22ec1, 08e4eeae3e8ccbc2 (11c3e43 runner); now the re-pinned values of
        /// <c>DemogConceptionTests.Repinned</c>.
        /// </summary>
        private static readonly Dictionary<(string, ulong), ulong> Base = new Dictionary<(string, ulong), ulong>
        {
            [("m4_families", 1)] = 0x8345d448306f8459UL, [("m4_families", 2)] = 0x94ac3afc879cf416UL,
            [("demog_favourable", 1)] = 0xfd03d50ba778738aUL, [("demog_favourable", 2)] = 0xc2f7eb1255fcdba5UL,
        };

        [Test]
        public void PartnerSexMode0_IsByteIdentical_F2b_T1()
        {
            var keys = Base.Keys.ToList();
            var specs = keys.Select(k => TestUtil.Spec(k.Item1, k.Item2, 12000))
                .Concat(keys.Select(k => TestUtil.Spec(k.Item1, k.Item2, 12000, Mode + "0"))).ToList();
            var res = TestUtil.RunAll(specs);
            for (int i = 0; i < res.Length; i++)
            {
                var k = keys[i % keys.Count];
                Assert.AreEqual(0, res[i].ExitCode, res[i].Error);
                Assert.AreEqual(Base[k].ToString("x16"), res[i].FinalHash.ToString("x16"), k + (i < keys.Count ? " unset" : " explicit 0") + ": mode 0 must hash as the base");
            }
        }

        [Test]
        public void SexBlindControl_BehavesAsMode0_F2b_T2()
        {
            var off = Build("demog_f2_favourable", 1, "tuning.Households.Life.ConceptionMode=1");
            var ctl = Build("demog_f2_favourable", 1, "tuning.Households.Life.ConceptionMode=1", Mode + "1");
            Assert.IsTrue(ctl.State.Population.Npcs.Where(n => n.Life != null).All(n => n.Life.PartnerSeeks == Preferences.SeeksBoth), "everyone seeks both");
            Assert.IsTrue(off.State.Population.Npcs.Where(n => n.Life != null).All(n => n.Life.PartnerSeeks == 0), "not drawn in mode 0");
            int formed = 0;
            for (int day = 0; day < 40; day++)
            {
                for (int t = 0; t < 1200; t++)
                {
                    off.Step(); ctl.Step();
                    var a = off.DrainEvents(); var b = ctl.DrainEvents();
                    Assert.AreEqual(a.Count, b.Count, "event count at tick " + off.State.Tick);
                    formed += a.Count(e => e is PartnershipFormed);
                }
                var pa = off.State.Population.Npcs; var pb = ctl.State.Population.Npcs;
                Assert.AreEqual(pa.Count, pb.Count, "people on day " + day);
                for (int i = 0; i < pa.Count; i++)
                {
                    Assert.AreEqual(pa[i].Alive, pb[i].Alive);
                    Assert.AreEqual(pa[i].Node, pb[i].Node, "npc " + pa[i].Id + " day " + day);
                    Assert.AreEqual(pa[i].NutritionPermille, pb[i].NutritionPermille);
                    Assert.AreEqual(pa[i].Household, pb[i].Household);
                }
                Assert.AreEqual(off.State.Households.Partnerships.Count, ctl.State.Households.Partnerships.Count);
            }
            Assert.Greater(formed, 0, "partnerships formed in the window");
        }

        [Test]
        public void PartnershipsRespectBothPreferences_NotARestriction_F2b_T3()
        {
            // everyone seeks the other sex: every partnership formed during the run is a woman and a man
            var other = Run("demog_f2_favourable", 1, 60, Mode + "1", "tuning.Households.Prefs.SeekOtherSexPermille=1000");
            Assert.Greater(other.Formed.Count, 0);
            Assert.IsTrue(other.Formed.All(p => p.Item1 != p.Item2), "only mixed partnerships under 'everyone seeks the other sex'");
            // everyone seeks its own sex: only same-sex partnerships form, and children are still conceived (reproduction
            // outside a partnership is untouched by the preference; D4 §2)
            var same = Run("demog_f2_favourable", 1, 60, Mode + "1", "tuning.Households.Prefs.SeekSameSexPermille=1000", "tuning.Households.Life.ConceptionMode=1");
            Assert.Greater(same.Formed.Count, 0, "same-sex partnerships form when people seek them");
            Assert.IsTrue(same.Formed.All(p => p.Item1 == p.Item2), "only same-sex partnerships under 'everyone seeks its own sex'");
            Assert.Greater(same.UnionsOutside, 0, "reproductive relationships outside a partnership still form");
            Assert.Greater(same.Conceptions, 0, "children are still conceived");
            // a mixed distribution: every partnership formed is one both partners sought
            var mixed = Run("demog_f2_favourable", 2, 60, Mode + "1", "tuning.Households.Prefs.SeekOtherSexPermille=600", "tuning.Households.Prefs.SeekSameSexPermille=150");
            Assert.Greater(mixed.Formed.Count, 0);
            Assert.AreEqual(0, mixed.Violations, "a partnership both of whose people did not seek the other's sex");
        }

        [Test]
        public void SaveLoad_PartnerSexMode1_F2b_T4()
        {
            var sc = TestUtil.Load("demog_f2_favourable", Mode + "1", "tuning.Households.Prefs.SeekOtherSexPermille=700", "tuning.Households.Prefs.SeekSameSexPermille=100");
            var sim = SimBuilder.Create(sc.ToSetup(3), sc.ToTuning()).Sim;
            var twin = SimBuilder.Create(sc.ToSetup(3), sc.ToTuning()).Sim;
            long saveAt = 1200L * 20;
            while (sim.State.Tick < saveAt) { sim.Step(); sim.DrainEvents(); twin.Step(); twin.DrainEvents(); }
            var loaded = SimBuilder.FromSave(SaveFile.FromJson(SaveFile.From(sim).ToJson())).Sim;
            Assert.AreEqual(1, loaded.State.Tuning.Households.Prefs.PartnerSexMode, "the flag survives the save");
            Assert.IsTrue(loaded.State.Population.Npcs.Where(n => n.Life != null).Select(n => n.Life.PartnerSeeks)
                .SequenceEqual(sim.State.Population.Npcs.Where(n => n.Life != null).Select(n => n.Life.PartnerSeeks)), "preferences survive the save");
            Assert.AreEqual(twin.StateHash(), loaded.StateHash());
            for (int i = 0; i < 1200 * 5; i++)
            {
                loaded.Step(); loaded.DrainEvents(); twin.Step(); twin.DrainEvents();
                if (i % 200 == 0) Assert.AreEqual(twin.StateHash(), loaded.StateHash(), "tick " + i + " after the load");
            }
        }

        [Test]
        public void Draw_FollowsTheDistribution_F2b_T5()
        {
            var pt = new PrefTuning { PartnerSexMode = 1, SeekOtherSexPermille = 700, SeekSameSexPermille = 100 };
            Assert.AreEqual(Preferences.SeeksMen, Preferences.PartnerSex(0, 0, pt));
            Assert.AreEqual(Preferences.SeeksMen, Preferences.PartnerSex(699, 0, pt));
            Assert.AreEqual(Preferences.SeeksWomen, Preferences.PartnerSex(700, 0, pt));
            Assert.AreEqual(Preferences.SeeksWomen, Preferences.PartnerSex(799, 0, pt));
            Assert.AreEqual(Preferences.SeeksBoth, Preferences.PartnerSex(800, 0, pt));
            Assert.AreEqual(Preferences.SeeksWomen, Preferences.PartnerSex(10, 1, pt));
            Assert.AreEqual(Preferences.SeeksMen, Preferences.PartnerSex(750, 1, pt));
            Assert.AreEqual(Preferences.SeeksBoth, Preferences.PartnerSex(999, 1, pt));
            // on a large band the shares come out near the stated ones
            var sim = Build("demog_f2_favourable", 4, Mode + "1", "tuning.Households.Prefs.SeekOtherSexPermille=700", "tuning.Households.Prefs.SeekSameSexPermille=100");
            var people = sim.State.Population.Npcs.Where(n => n.Life != null).ToList();
            int otherSex = people.Count(n => n.Life.PartnerSeeks == (n.Life.Sex == 0 ? Preferences.SeeksMen : Preferences.SeeksWomen));
            int both = people.Count(n => n.Life.PartnerSeeks == Preferences.SeeksBoth);
            Assert.That(otherSex * 1000 / people.Count, Is.InRange(560, 840), "share seeking the other sex");
            Assert.That(both * 1000 / people.Count, Is.InRange(80, 330), "share seeking both");
            // compatibility is mutual
            var w = new Npc { Life = new NpcLife { Sex = 0, PartnerSeeks = Preferences.SeeksWomen } };
            var m = new Npc { Life = new NpcLife { Sex = 1, PartnerSeeks = Preferences.SeeksWomen } };
            var w2 = new Npc { Life = new NpcLife { Sex = 0, PartnerSeeks = Preferences.SeeksBoth } };
            Assert.IsFalse(Preferences.PartnerCompatible(w, m), "she does not seek men");
            Assert.IsTrue(Preferences.PartnerCompatible(w, w2));
            Assert.IsTrue(Preferences.PartnerCompatible(m, w2));
            Assert.IsTrue(Preferences.PartnerCompatible(new Npc { Life = new NpcLife { Sex = 0 } }, new Npc { Life = new NpcLife { Sex = 0 } }), "not drawn: sex-blind");
        }

        private static GodSim Build(string scenario, ulong seed, params string[] sets)
        {
            var sc = TestUtil.Load(scenario, sets);
            return SimBuilder.Create(sc.ToSetup(seed), sc.ToTuning()).Sim;
        }

        private sealed class Outcome
        {
            /// <summary>Sexes of the two people of each partnership formed during the run.</summary>
            public List<(byte, byte)> Formed = new List<(byte, byte)>();
            public int UnionsOutside, Conceptions, Violations;
        }

        private static Outcome Run(string scenario, ulong seed, int days, params string[] sets)
        {
            var sim = Build(scenario, seed, sets);
            var o = new Outcome();
            var p = sim.State.Population;
            for (long t = 0; t < 1200L * days; t++)
            {
                sim.Step();
                foreach (var e in sim.DrainEvents())
                {
                    if (e is PartnershipFormed pf && pf.Household != 0) // founders' couples (scenario data) are announced with household 0
                    {
                        var a = p.Find(pf.A); var b = p.Find(pf.B);
                        o.Formed.Add((a.Life.Sex, b.Life.Sex));
                        if (!Preferences.Seeks(a.Life, b.Life.Sex) || !Preferences.Seeks(b.Life, a.Life.Sex)) o.Violations++;
                    }
                    else if (e is ReproductiveUnionFormed uf && !uf.Partners) o.UnionsOutside++;
                    else if (e is PregnancyStarted) o.Conceptions++;
                }
            }
            return o;
        }
    }
}
