using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Civ
{
    /// <summary>
    /// F1.5 (contracts 0.1): the module entry points. Call sites (frozen): <c>GodSim.Create</c> (after
    /// <c>PopulationExtensions.StartGame</c>), <c>GodSim.InstallModules</c>, <c>PopulationModule.AddNpc</c> (through
    /// <c>PopulationExtensions.NewMind</c>), <c>Kill</c> (through the hooks' <c>OnDeath</c>) and <c>SimInvariants.Check</c>.
    /// OWNER of the bodies: C1. In F1 every body is a no-op except that <see cref="StartGame"/> creates the empty state when
    /// <c>Civ.Enabled = 1</c> and the game has a population.
    /// </summary>
    public static partial class CivModule
    {
        /// <summary>Before the first tick: the empty state when the switch is on and the game has a population.</summary>
        public static void StartGame(GodState s)
        {
            if (s.Population == null || s.Tuning.Civ.Enabled == 0) return;
            s.Civ = new CivState();
            s.Civ.Ownership = new OwnershipState(); // C1: assets, the O7 overlay and claims (hashed inside the civ part only)
        }

        /// <summary>Registers handlers and systems: C1's <see cref="CivSystem"/> (order 257), only while the state exists.</summary>
        public static void Install(GodSim sim)
        {
            if (sim.State.Civ != null) sim.AddSystem(CivSystem.Order, new CivSystem());
        }

        /// <summary>A new mind: its part of this module (none in F1: the mind slot stays null until C1 fills it).</summary>
        public static void NewMind(GodState s, Npc npc) { }

        /// <summary>A person died (called by the hooks' <c>OnDeath</c> only while the state exists).</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc) { }

        /// <summary>C1's invariants: ascending unique asset ids below the counter, each finished asset's store an AssetStore bin, at most 8 rights and 4 disputes.</summary>
        public static void Check(GodState s, List<string> bad)
        {
            var o = s.Civ.Ownership;
            if (o == null) return;
            int last = 0;
            foreach (var a in o.Assets)
            {
                if (a.Id <= last) bad.Add("CIV1: asset ids not ascending at " + a.Id);
                if (a.Id >= o.NextAssetId) bad.Add("CIV1: asset " + a.Id + " not below NextAssetId " + o.NextAssetId);
                last = a.Id;
                if (AssetTable.Get(a.Kind) == null) bad.Add("CIV1: asset " + a.Id + " of unknown kind " + a.Kind);
                if (a.Store != 0)
                {
                    var b = Goods.BinById(s.Population, a.Store);
                    if (b == null || b.Kind != BinKind.AssetStore || b.Node != a.Node) bad.Add("CIV1: asset " + a.Id + " store " + a.Store + " missing or not an AssetStore at its node");
                }
                if (a.IsFinished == (a.MaterialsIn != null)) bad.Add("CIV1: asset " + a.Id + " materials record does not match its state");
                if (a.Rights.Count > Assets.MaxRights || a.Disputes.Count > Assets.MaxDisputes) bad.Add("CIV1: asset " + a.Id + " has too many rights or disputes");
            }
            foreach (var l in o.Lost) if (l.Count <= 0 || l.Owner.IsNone) bad.Add("CIV1: lost lot " + l.Lot + " without units or owner");
            CheckC2(s, bad);
            CheckC3(s, bad);
            CheckC5(s, bad);
        }

        /// <summary>C1c: the later packages' invariants, each in its own partial file (<c>CivModule.C3.cs</c>, ...), in package order.</summary>
        static partial void CheckC2(GodState s, List<string> bad);
        static partial void CheckC3(GodState s, List<string> bad);
        static partial void CheckC5(GodState s, List<string> bad);
    }
}
