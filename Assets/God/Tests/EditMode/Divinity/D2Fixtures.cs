using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Divinity;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Tests
{
    /// <summary>
    /// D2's test fixtures (OWNER: D2): a quiet population game with gods (<c>Divinity.Enabled = 1</c>, god 1 in the Human
    /// seat), a glade, one person and the things around it placed through the ordinary module calls. Nothing scripts a
    /// person's choice: it perceives, decides and learns as on the island. Belief and trust are set as a test fixture
    /// (written into the mind's <c>GodBelief</c>) until D6 lands them.
    /// </summary>
    public static class D2Fixtures
    {
        public const int Day = 1200;

        public static GodSim Game(ulong seed, Action<SimTuning> tune = null)
        {
            var t = new SimTuning();
            t.Population.HandGatherPiles = false;
            t.Population.FounderAxePermille = 0;
            t.Ecology.BerriesPerForest = 0; t.Ecology.BerriesPerMeadow = 0; t.Ecology.RootsPerHills = 0; t.Ecology.RootsPerFields = 0;
            t.Ecology.WoodPerForest = 0; t.Ecology.FishPerCoast = 0; t.Ecology.NightshadePerForest = 0;
            t.Divinity.Enabled = 1;
            t.Divinity.Mythic.Start = 100_000;
            tune?.Invoke(t);
            var setup = new GameSetup(seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 0, Victory = VictorySpec.SandboxOnly(), HumanGod = 1 };
            return GodSim.Create(setup, t);
        }

        /// <summary>The scene of DP1: a person at the glade centre, a berry bush 2 nodes away, a wood stand 3 nodes the other way.</summary>
        public sealed class Scene
        {
            public GodSim Sim;
            public Npc Npc;
            public int Centre, BushNode, WoodNode;
            public DivineProbe Probe;
        }

        public static Scene Glade(ulong seed, InstinctSet instincts, int hunger, Action<SimTuning> tune = null, LearningMode mode = LearningMode.On, int experienceDays = 0, int gladeRadius = 8)
        {
            var sim = Game(seed, tune);
            int c = LearningFixtures.Glade(sim, gladeRadius);
            var sc = new Scene { Sim = sim, Centre = c, BushNode = LearningFixtures.Step(sim, c, 0, 2), WoodNode = LearningFixtures.Step(sim, c, 3, 3) };
            LearningFixtures.Bush(sim, SourceKind.Berries, sc.BushNode);
            sc.Npc = LearningFixtures.Npc(sim, c, instincts, mode, hunger, 150);
            sc.Probe = new DivineProbe(sim);
            if (experienceDays > 0)
            {
                // "normal values": the person first lives by the bush for some days and learns what eating is worth to it
                sc.Probe.Step(experienceDays * Day);
                sc.Npc.Node = c; sc.Npc.Path = null; sc.Npc.PathGoal = -1; // test-only: back to the centre
                sc.Npc.Drives[Drive.Hunger] = hunger * 1000;
                Array.Copy(sc.Npc.Drives, sc.Npc.RewardSnap, Drive.Count);
            }
            LearningFixtures.Bush(sim, SourceKind.Wood, sc.WoodNode);
            return sc;
        }

        /// <summary>Test fixture until D6: the person's belief, trust and fear about a god.</summary>
        public static void Believe(GodSim sim, Npc npc, int god, int belief, int trust, int fear = 0)
        {
            var m = npc.Mind.Divine ?? (npc.Mind.Divine = new DivineMind { Tuning = sim.State.Tuning.Divinity.Instruction });
            if (m.Gods == null || m.Gods.Length < sim.State.FactionCount) { var g = new GodBelief[sim.State.FactionCount]; if (m.Gods != null) Array.Copy(m.Gods, g, m.GodCount); m.Gods = g; }
            for (int i = 0; i < m.GodCount; i++) if (m.Gods[i].God == god) { m.Gods[i].Belief = (short)belief; m.Gods[i].Trust = (short)trust; m.Gods[i].Fear = (short)fear; return; }
            m.Gods[m.GodCount++] = new GodBelief { God = god, Belief = (short)belief, Trust = (short)trust, Fear = (short)fear };
        }

        public static DivineTarget Person(int npc, int node = 0, int obj = 0) => new DivineTarget(DivineTargetKind.Npc, npc, node, obj);

        /// <summary>Steps until the person's one response arrives (or the limit); returns it (None when none).</summary>
        public static CommandResponded StepToResponse(Scene sc, int limit = 200)
        {
            for (int i = 0; i < limit; i++)
            {
                sc.Probe.Step(1);
                var r = sc.Probe.Of<CommandResponded>().FirstOrDefault(e => e.Npc == sc.Npc.Id);
                if (r != null) return r;
            }
            return null;
        }
    }
}
