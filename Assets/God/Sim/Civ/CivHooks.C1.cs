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
                if (n < near.Length && tu.TakeOutEnabled != 0) n = SenseStore(s, npc, best, def, bestD, near, n);
            }
            if (n < near.Length && tu.BuildEnabled != 0 && CarriesAnyMaterial(npc) && Work.CanFound(s, npc, npc.Node))
                near[n++] = new Sensed { Class = CivClass.Plot, Category = ObjCategory.Workplace, State = 0, Kind = IntentTarget.Node, Node = npc.Node, Dist = 0, Ref = 0 };
        }

        /// <summary>
        /// C1c, the asset's store as a bin (the I-BS bin seam pattern, contracts 6): perceived by a person who truly may take
        /// from it (the owner, an adult of the owning household, a holder of a Store right), as <see cref="CivClass.AssetStore"/>
        /// with the band of the output it sees there that it may take (Full: no room left for another cycle), category Food
        /// when that output is food. Others' stores are not offered to the mind (no permission; theft is a later package's).
        /// Always offered to those people, empty too, so a store belief stays current.
        /// </summary>
        private static int SenseStore(GodState s, Npc npc, ProductiveAsset a, AssetDef def, int dist, Sensed[] near, int n)
        {
            if (!a.IsFinished || a.Store == 0) return n;
            var bin = Goods.BinById(s.Population, a.Store);
            if (bin == null || !Work.StoreView(s, npc, a, def, bin, out int units, out bool food)) return n;
            bool full = units > 0 && def.Recipe != null && def.WorkTicks > 0 && !Work.HasRoom(bin, def);
            near[n++] = new Sensed
            {
                Class = CivClass.AssetStore, Category = (byte)(food ? ObjCategory.Food : ObjCategory.Store),
                State = (byte)(units == 0 ? ObjState.StockEmpty : full ? ObjState.Full : units < 4 ? ObjState.StockLow : ObjState.StockHigh),
                Kind = IntentTarget.Bin, Node = bin.Node, Dist = dist, Ref = bin.Id,
            };
            return n;
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
