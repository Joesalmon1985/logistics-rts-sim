using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Civ
{
    /// <summary>C1's sim-side hook slots (OWNER: C1): its acts, the Sense seam for assets and free plots, deaths, time use.</summary>
    public static partial class CivHooks
    {
        static partial void TryStartC1(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled) => Work.Start(s, ev, npc, in c, day, ref handled);

        static partial void TryActC1(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled) => Work.Act(s, ev, npc, day, ref handled);

        static partial void ActivityOfC1(byte verb, ref int activity)
        {
            if (activity != Activity.Other) return;
            if (verb == Work.VerbBuild) activity = Activity.Build;
            else if (verb == Work.VerbWork) activity = Activity.Gather;
            else if (verb == Work.VerbHaul) activity = Activity.Carry;
        }

        /// <summary>A dead person's use rights end (its assets stay on the record: inheritance of assets is a later package's).</summary>
        static partial void OnDeathC1(GodState s, EventBuffer ev, Npc npc)
        {
            var o = s.Civ.Ownership;
            if (o == null) return;
            var me = Owner.Npc(npc.Id);
            foreach (var a in o.Assets)
                for (int i = a.Rights.Count - 1; i >= 0; i--)
                    if (a.Rights[i].Holder.Same(me)) Assets.EndAt(s, ev, a, i, UseRightEnded.HolderDied);
        }

        /// <summary>
        /// The Sense seam (contracts 6, spec 5.2): the nearest assets in sight (at most <c>Civ.C1.SenseAssets</c>), as an object
        /// class 90-92 with what is visible of it (unfinished and missing materials, materials in, or how stocked with inputs
        /// its store looks), and the asset id as a thing it can name (like a hut). A free plot where it stands, while it
        /// carries a material of its own and founding is possible here (a place, Ref 0).
        /// </summary>
        static partial void SenseExtraC1(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n)
        {
            var o = s.Civ.Ownership;
            if (o == null) return;
            var tu = s.Tuning.Civ.C1;
            int taken = 0;
            int lastD = -1, lastId = 0;
            while (taken < tu.SenseAssets && n < near.Length)
            {
                // the next nearest (distance, then id) after the last one taken: no allocation, a few assets in sight
                ProductiveAsset best = null; int bestD = int.MaxValue;
                foreach (var a in o.Assets)
                {
                    if (a.Node <= 0) continue;
                    int d = g.Distance(npc.Node, a.Node);
                    if (d > sight) continue;
                    if (d < lastD || d == lastD && a.Id <= lastId) continue;
                    if (d < bestD || d == bestD && a.Id < best.Id) { best = a; bestD = d; }
                }
                if (best == null) break;
                lastD = bestD; lastId = best.Id; taken++;
                var def = AssetTable.Get(best.Kind);
                near[n++] = new Sensed
                {
                    Class = (byte)def.ObjClass, Category = (byte)(def.Recipe == null ? ObjCategory.Store : ObjCategory.Workplace),
                    State = (byte)StateOf(s, best, def), Kind = IntentTarget.Building, Node = best.Node, Dist = bestD, Ref = best.Id,
                };
            }
            if (n < near.Length && tu.BuildEnabled != 0 && CarriesAnyMaterial(npc) && Work.CanFound(s, npc, npc.Node))
                near[n++] = new Sensed { Class = CivClass.Plot, Category = ObjCategory.Workplace, State = 0, Kind = IntentTarget.Node, Node = npc.Node, Dist = 0, Ref = 0 };
        }

        private static bool CarriesAnyMaterial(Npc npc)
        {
            foreach (var d in AssetTable.Rows) if (Work.CarriesMaterial(npc, d)) return true;
            return false;
        }

        /// <summary>What can be seen of an asset: Unfinished (materials missing), Full (materials in, work left), else the input stock band.</summary>
        private static int StateOf(GodState s, ProductiveAsset a, AssetDef def)
        {
            if (!a.IsFinished)
            {
                for (int i = 0; i < def.BuildCost.Count; i++) if (a.MaterialsIn[i] < def.BuildCost[i].Count) return ObjState.Unfinished;
                return ObjState.Full;
            }
            if (def.Recipe == null || def.Recipe.Inputs.Count == 0) return ObjState.StockHigh;
            var bin = Goods.BinById(s.Population, a.Store);
            if (bin == null) return ObjState.StockEmpty;
            int cycles = int.MaxValue;
            foreach (var i in def.Recipe.Inputs)
            {
                int have = 0;
                foreach (var l in bin.Lots) if (l.Ware == i.Ware) have += l.Count;
                cycles = System.Math.Min(cycles, have / System.Math.Max(1, i.Count));
            }
            return cycles == 0 ? ObjState.StockEmpty : cycles < 2 ? ObjState.StockLow : ObjState.StockHigh;
        }
    }
}
