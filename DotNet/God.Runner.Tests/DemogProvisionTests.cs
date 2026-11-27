using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Population;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// F2c (LEAD_RULINGS R67): provisioning of dependants behind <c>Households.Care.Provision</c>.
    /// F2c-T1: Provision 0 (explicit) hashes exactly as the base (the F2-T1 / F2b-T1 hashes): nothing new is offered, drawn or hashed.
    /// F2c-T2: with Provision 1 provisioning is a genuine act: only adults of a household with children at home choose it,
    /// it puts food in their own household's store by a deposit (more deposits and more stored food than the same run with
    /// it off), and no food is created by it (every unit it delivers was foraged from a source, and every forage and
    /// deposit is an ordinary event within reach). With it off no one ever provisions.
    /// </summary>
    [TestFixture]
    public sealed class DemogProvisionTests
    {
        private const string Flag = "tuning.Households.Care.Provision=";

        /// <summary>The F2-T1 base hashes (tick 12000), as pinned by F2b-T1.</summary>
        private static readonly Dictionary<(string, ulong), ulong> Base = new Dictionary<(string, ulong), ulong>
        {
            [("m4_families", 1)] = 0x4e097b3731e62136UL, [("m4_families", 2)] = 0x561a1f8ef03ccfc3UL,
            [("demog_favourable", 1)] = 0x55a810bf69f22ec1UL, [("demog_favourable", 2)] = 0x08e4eeae3e8ccbc2UL,
        };

        [Test]
        public void Provision0_IsByteIdentical_F2c_T1()
        {
            var keys = Base.Keys.ToList();
            var res = TestUtil.RunAll(keys.Select(k => TestUtil.Spec(k.Item1, k.Item2, 12000, Flag + "0")).ToList());
            for (int i = 0; i < res.Length; i++)
            {
                Assert.AreEqual(0, res[i].ExitCode, res[i].Error);
                Assert.AreEqual(Base[keys[i]].ToString("x16"), res[i].FinalHash.ToString("x16"), keys[i] + " explicit 0 must hash as the base");
            }
        }

        private sealed class Tally
        {
            public int Deposits, DepositUnits, ProvisionTicks, BadProvisioners, StoreFood, Foraged;
            public HashSet<int> Provisioners = new HashSet<int>();
        }

        private static Tally Run(int days, params string[] sets)
        {
            var sc = TestUtil.Load("demog_f2_favourable", sets);
            var sim = SimBuilder.Create(sc.ToSetup(1), sc.ToTuning()).Sim;
            var s = sim.State;
            var p = s.Population;
            var t = new Tally();
            for (long tick = 0; tick < 1200L * days; tick++)
            {
                sim.Step();
                foreach (var e in sim.DrainEvents())
                {
                    if (e is ItemMoved m && m.Reason == MoveReason.Deposit && m.ToKind == HolderKind.Bin) { t.Deposits++; t.DepositUnits += m.Count; }
                    else if (e is Foraged f) t.Foraged += f.Count;
                }
                if (tick % 60 != 0) continue;
                foreach (var n in p.Npcs)
                {
                    if (!n.Alive || n.Action.Verb != HVerb.Provision) continue;
                    t.ProvisionTicks++;
                    t.Provisioners.Add(n.Id);
                    // a child never provisions; one with no child at home only finishes bringing home what it holds
                    bool holds = n.Carry.Any(l => !l.Empty && Wares.IsFood(l.Ware) && l.Owner.Kind == OwnerKind.Npc && l.Owner.Id == n.Id);
                    if (n.Stage == LifeStage.Child || n.Household == 0 || !holds && HouseholdHooks.ProvisionDependants(s, n) == 0) t.BadProvisioners++;
                }
            }
            foreach (var b in p.Bins)
                if (b.Kind == BinKind.HouseholdStore)
                    foreach (var l in b.Lots) if (Wares.IsFood(l.Ware)) t.StoreFood += l.Count;
            return t;
        }

        [Test]
        public void Provisioning_IsAGenuineCareAct_F2c_T2()
        {
            const string conception = "tuning.Households.Life.ConceptionMode=1";
            var off = Run(30, conception);
            var on = Run(30, conception, Flag + "1");
            Assert.AreEqual(0, off.ProvisionTicks, "with Provision 0 no one provisions");
            Assert.Greater(on.ProvisionTicks, 0, "with Provision 1 adults provision");
            Assert.AreEqual(0, on.BadProvisioners, "only adults of a household with children at home provision");
            Assert.Greater(on.Deposits, off.Deposits, "provisioning brings food home to the store");
            Assert.Greater(on.DepositUnits, off.DepositUnits);
            Assert.Greater(on.Foraged, off.Foraged, "the food it brings home is foraged (real work)");
            int adults = 0;
            var sc = TestUtil.Load("demog_f2_favourable", conception, Flag + "1");
            var sim = SimBuilder.Create(sc.ToSetup(1), sc.ToTuning()).Sim;
            foreach (var n in sim.State.Population.Npcs) if (n.Stage != LifeStage.Child) adults++;
            TestContext.WriteLine($"off: deposits {off.Deposits} ({off.DepositUnits} units), foraged {off.Foraged}, store food at the end {off.StoreFood}; "
                + $"on: deposits {on.Deposits} ({on.DepositUnits} units), foraged {on.Foraged}, store food {on.StoreFood}, provisioners {on.Provisioners.Count} (founding adults {adults})");
        }
    }
}
