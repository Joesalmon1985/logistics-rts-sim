using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Divinity;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// D6's test fixtures (OWNER: D6): a village (one community of founders with households, children and huts, a second
    /// community elsewhere) with gods on (<c>Divinity.Enabled = 1</c>, god 1 in the Human seat), and the person-level fixtures
    /// of beliefs and held commandments. Nothing scripts a person's choice: it perceives, decides and learns as on the island.
    /// </summary>
    public static class D6Fixtures
    {
        public const int Day = 1200;

        public static GodSim Village(ulong seed, Action<SimTuning> tune = null, int perFaction = 20, int factions = 2)
        {
            var t = new SimTuning();
            Set(t, "Households.Founders.Generator", 1);
            Set(t, "Households.Founders.ElderPermille", 100);
            Set(t, "Households.Founders.MaturePermille", 300);
            Set(t, "Households.Founders.ChildPermille", 300);
            Set(t, "Households.Founders.ChildMinMilli", 50);
            Set(t, "Households.Founders.ChildMaxMilli", 950);
            Set(t, "Households.Founders.CouplePermille", 800);
            Set(t, "Households.Founders.StartHuts", 1);
            Set(t, "Households.Founders.StoreFood", 10);
            t.Divinity.Enabled = 1;
            t.Divinity.Mythic.Start = 500_000;
            t.Divinity.Mythic.Max = 1_000_000;
            tune?.Invoke(t);
            var setup = new GameSetup(seed, GameSetup.NoHuman, factions) { Population = true, StartNpcsPerFaction = perFaction, Victory = VictorySpec.SandboxOnly(), HumanGod = 1 };
            return GodSim.Create(setup, t);
        }

        public static void Set(SimTuning t, string path, long v)
        {
            Assert.IsTrue(t.TrySet(path, v, out var err), path + ": " + err);
        }

        /// <summary>Test fixture: a person's five fields about a god (written into its mind's <c>GodBelief</c>).</summary>
        public static void Believe(GodSim sim, Npc npc, int god, int belief, int faith = 300, int trust = 0, int fear = 0, int loyalty = 500)
        {
            var m = Doctrine.MindOf(sim.State, npc);
            if (m.Gods == null || m.Gods.Length < sim.State.FactionCount) { var g = new GodBelief[Math.Max(2, sim.State.FactionCount)]; if (m.Gods != null) Array.Copy(m.Gods, g, m.GodCount); m.Gods = g; }
            for (int i = 0; i < m.GodCount; i++)
                if (m.Gods[i].God == god) { m.Gods[i] = new GodBelief { God = god, Belief = (short)belief, Faith = (short)faith, Trust = (short)trust, Fear = (short)fear, Loyalty = (short)loyalty }; return; }
            int at = m.GodCount;
            while (at > 0 && m.Gods[at - 1].God > god) { m.Gods[at] = m.Gods[at - 1]; at--; }
            m.Gods[at] = new GodBelief { God = god, Belief = (short)belief, Faith = (short)faith, Trust = (short)trust, Fear = (short)fear, Loyalty = (short)loyalty };
            m.GodCount++;
        }

        /// <summary>Test fixture: the person holds the commandment's current revision exactly (no misunderstanding) at this adoption and salience.</summary>
        public static void Hold(GodSim sim, Npc npc, int commandment, int adoption, int salience)
        {
            var cm = sim.State.Divinity.Doctrine.Find(commandment);
            var m = Doctrine.MindOf(sim.State, npc);
            var k = new KnownCommandment
            {
                Commandment = cm.Id, Revision = cm.Revision, God = cm.God, Importance = (byte)cm.Importance, RuleCount = (byte)cm.Rules.Length,
                Interpretation = cm.Rules[0], Interpretation2 = cm.Rules.Length > 1 ? cm.Rules[1] : default,
                Source = CommandmentSource.Revealed, Adoption = (short)adoption, Salience = (short)salience,
            };
            int i = m.IndexOfKnown(commandment);
            if (i >= 0) m.Known[i] = k; else m.AddKnown(k, out _);
            int j = m.IndexOfKnown(commandment);
            m.Known[j].Stance = Doctrine.StanceOf(sim.State.Tuning.Divinity.Doctrine, m.Known[j]);
        }

        /// <summary>Reveals a template to a target; asserts acceptance; the new commandment's id.</summary>
        public static int Reveal(GodSim sim, int template, DivineTarget to, int importance = 0, int god = 1)
        {
            var t = CommandmentTemplates.Get(template);
            int before = sim.State.Divinity.Doctrine?.NextCommandmentId ?? 1;
            var r = sim.Submit(new RevealCommandment { God = god, Template = template, Rules = CommandmentTemplates.RulesOf(template), Importance = importance > 0 ? importance : t.Importance, Recipients = to });
            Assert.IsTrue(r.Ok, r.Reason);
            sim.Step();
            return before;
        }

        public static List<Npc> People(GodSim sim, Faction f) => sim.State.Population.Npcs.Where(n => Bodies.IsLivingPerson(n) && n.Faction == f).OrderBy(n => n.Id).ToList();

        public static List<Npc> Adults(GodSim sim, Faction f) => People(sim, f).Where(n => n.Stage != LifeStage.Child).ToList();

        /// <summary>The known entry of a person (null when it holds none).</summary>
        public static KnownCommandment? Known(Npc n, int commandment)
        {
            var m = n.Mind?.Divine;
            if (m == null) return null;
            int i = m.IndexOfKnown(commandment);
            return i < 0 ? (KnownCommandment?)null : m.Known[i];
        }

        public static CommandmentStance StanceOf(GodSim sim, Npc n, int commandment)
        {
            var k = Known(n, commandment);
            return k == null ? CommandmentStance.None : Doctrine.StanceOf(sim.State.Tuning.Divinity.Doctrine, k.Value);
        }

        /// <summary>A target naming a person and everyone within <c>radius</c> nodes of it, with the smallest radius that reaches at least <paramref name="want"/> of the given people.</summary>
        public static DivineTarget Gathered(GodSim sim, Npc centre, IList<Npc> among, int want)
        {
            var g = sim.State.Territory.Grid;
            var ds = among.Select(n => g.Distance(n.Node, centre.Node)).OrderBy(d => d).ToList();
            int r = ds[Math.Min(want - 1, ds.Count - 1)];
            return new DivineTarget(DivineTargetKind.Npc, centre.Id, 0, Math.Max(0, r));
        }
    }
}
