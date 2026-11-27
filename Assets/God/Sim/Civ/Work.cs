using System;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using God.Sim.Population.Households;

namespace God.Sim.Civ
{
    /// <summary>
    /// The physical side of C1's intents (sim code, called through <c>CivHooks.TryStart/TryAct</c>): build an asset from real
    /// materials, work it (the recipe runs; the output belongs to the asset's owner), and haul inputs to its store. Each act
    /// walks within its reach first and acts only through <see cref="Goods"/>; a failed act ends with the M3 frustration
    /// signal. Nothing here makes anyone act, and nothing here branches on the kind of owner (C1-T11).
    /// </summary>
    public static class Work
    {
        /// <summary>Learning verbs: Work (contracts 14), build an asset (C1 takes the Civ spare 102: M3's Act owns verb 12 for huts), haul (11).</summary>
        public const byte VerbWork = 96, VerbBuild = 102, VerbHaul = Verb.Haul;
        /// <summary>Intent verbs (contracts 14 Civ block 170-178).</summary>
        public const NpcVerb IntentWork = (NpcVerb)170, IntentBuild = (NpcVerb)171, IntentHaul = (NpcVerb)172;

        public static bool IsMine(byte v) => v == VerbWork || v == VerbBuild || v == VerbHaul;

        /// <summary>
        /// ECONOMY 4.2 work speed (permille of a work tick per tick): <c>(BaseWorkSpeed + 3 x skill) x tool x fatigue</c>. No
        /// skill record exists in C1 (C7/K1 own the skill store, R42), so skill is 0 and the tool factor 1000; a tired worker
        /// slows to 80 % (fatigue above 600). The industrial runner calls this same function.
        /// </summary>
        public static int Speed(GodState s, Npc npc, ProductiveAsset a, int skill = 0, int toolPermille = 1000)
        {
            int b = s.Tuning.Civ.C1.BaseWorkSpeed + 3 * Math.Max(0, skill);
            int fatigue = npc.DriveOf(Drive.Fatigue);
            int fat = 1000 - Math.Min(200, Math.Max(0, fatigue - 600) / 2);
            return Fx.MulDiv(Fx.MulDiv(b, toolPermille, 1000), fat, 1000);
        }

        // ---------------- start ----------------

        public static void Start(GodState s, EventBuffer ev, Npc npc, in Cognition.Candidate c, int day, ref bool handled)
        {
            if (handled || !IsMine(c.Verb) || Assets.StateOf(s) == null) return;
            handled = true;
            if (c.Verb == VerbBuild && c.TargetId == 0) FoundSite(s, ev, npc, c.Class);
        }

        /// <summary>A free plot here: the site is founded (an unfinished asset), owned by the founder's household or the founder.</summary>
        private static void FoundSite(GodState s, EventBuffer ev, Npc npc, int cls)
        {
            var def = AssetTable.OfClass(cls);
            var owner = OwnerOf(npc);
            if (def == null || !CanFound(s, npc, npc.Node) || !CarriesMaterial(npc, def)) { Frustrate(s, npc); return; }
            var a = Assets.Create(s, ev, def.Kind, npc.Node, owner, OwnershipBasis.Built, npc.Id, false);
            npc.Action = new NpcAction { Verb = VerbBuild, TargetKind = IntentTarget.Building, TargetId = a.Id, TargetNode = a.Node, Since = s.Tick };
            Ownership.Witness(s, a, npc, s.Tuning.Civ.C1.SeenBuiltConfidence);
        }

        /// <summary>Who a person's new asset belongs to (spec 5.3): its household when it has one, else itself.</summary>
        public static Owner OwnerOf(Npc npc) => npc.Household != 0 ? Owner.Household(npc.Household) : Owner.Npc(npc.Id);

        /// <summary>A free plot (sim truth): building on, land, no building, flag or road, outside every HQ circle, no hut, asset or source within 1 node.</summary>
        public static bool CanFound(GodState s, Npc npc, int node)
        {
            var t = s.Territory; var g = t.Grid; var tu = s.Tuning.Civ.C1;
            if (tu.BuildEnabled == 0 || node < 0 || !g.IsLand(node) || t.NodeBuilding[node] != 0 || t.NodeFlag[node] != 0 || t.NodeRoad[node] != 0) return false;
            if (Assets.CountOwnedBy(s, OwnerOf(npc)) >= tu.MaxAssetsPerOwner) return false;
            foreach (var hq in t.Hqs) if (g.InCircle(hq.Node, node)) return false;
            foreach (var h in s.Population.Huts) if (g.Distance(h.Node, node) <= 1) return false;
            foreach (var src in s.Population.Sources) if (g.Distance(src.Node, node) <= 1) return false;
            foreach (var a in Assets.StateOf(s).Assets) if (a.Node > 0 && g.Distance(a.Node, node) <= 1) return false;
            return true;
        }

        /// <summary>It carries a lot of its own (or its household's) of a ware the kind's build cost needs.</summary>
        public static bool CarriesMaterial(Npc npc, AssetDef def) => MaterialSlot(npc, def, null) >= 0;

        private static int MaterialSlot(Npc npc, AssetDef def, int[] have)
        {
            for (int line = 0; line < def.BuildCost.Count; line++)
            {
                if (have != null && have[line] >= def.BuildCost[line].Count) continue;
                for (int i = 0; i < npc.Carry.Length; i++)
                {
                    var l = npc.Carry[i];
                    if (!l.Empty && l.Ware == def.BuildCost[line].Ware && Ownership.OwnerRelation(npc, l.Owner) != Ownership.RightNone) return i;
                }
            }
            return -1;
        }

        // ---------------- act ----------------

        public static void Act(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled)
        {
            if (handled || !IsMine(npc.Action.Verb) || Assets.StateOf(s) == null) return;
            handled = true;
            switch (npc.Action.Verb)
            {
                case VerbBuild: Build(s, ev, npc); break;
                case VerbWork: Operate(s, ev, npc); break;
                case VerbHaul: Haul(s, ev, npc); break;
            }
        }

        private static void Build(GodState s, EventBuffer ev, Npc npc)
        {
            var a = Assets.Find(s, npc.Action.TargetId);
            if (a == null) { Frustrate(s, npc); return; }
            if (a.IsFinished) { End(npc); return; }
            if (!WalkTo(s, npc, a.Node, Reach.Touch)) { if (npc.Path == null) Frustrate(s, npc); return; }
            if (Ownership.OwnerRelation(npc, a.Owner) == Ownership.RightNone) { Frustrate(s, npc); return; } // only the owners build their site
            if (npc.Life != null && !HouseholdHooks.CanWork(s, npc)) { Frustrate(s, npc); return; }
            var def = AssetTable.Get(a.Kind);
            for (int line = 0; line < def.BuildCost.Count; line++)
            {
                if (a.MaterialsIn[line] >= def.BuildCost[line].Count) continue;
                // a material is still missing: put one in from the carry (one per trip, like a hut's logs)
                int slot = MaterialSlot(npc, def, a.MaterialsIn);
                if (slot < 0) { Frustrate(s, npc); return; }
                var lot = npc.Carry[slot];
                int need = -1;
                for (int k = 0; k < def.BuildCost.Count; k++) if (a.MaterialsIn[k] < def.BuildCost[k].Count && def.BuildCost[k].Ware == lot.Ware) { need = k; break; }
                if (!Goods.Consume(s, ev, Holder.Carry(npc.Id), lot.Id, 1, ConsumeReason.Built, npc.Id, a.Id)) { Frustrate(s, npc); return; }
                a.MaterialsIn[need]++;
                End(npc);
                return;
            }
            if (npc.Life != null && !HouseholdHooks.WorkTick(s, npc)) return;
            npc.Action.Progress = Math.Max(npc.Action.Progress, 1);
            if (++a.BuildWork < def.BuildTicks) return;
            Assets.Finish(s, ev, a, npc.Id);
            Ownership.Witness(s, a, npc, s.Tuning.Civ.C1.SeenBuiltConfidence);
            End(npc);
        }

        /// <summary>
        /// The Work verb: the worker within touch works the asset; each completed cycle consumes the recipe's inputs (the
        /// store first, then its own carry) and produces the outputs into the store, owned by the asset's owner, actor the
        /// worker. No inputs: nothing (no goods from nothing). A worker without any true right still works physically; the
        /// sim classifies it as a use theft (truth, analytics only).
        /// </summary>
        private static void Operate(GodState s, EventBuffer ev, Npc npc)
        {
            var a = Assets.Find(s, npc.Action.TargetId);
            var def = a != null ? AssetTable.Get(a.Kind) : null;
            if (a == null || !a.IsFinished || def.WorkTicks <= 0 || def.Recipe == null || a.Store == 0) { Frustrate(s, npc); return; }
            if (!WalkTo(s, npc, a.Node, Reach.Touch)) { if (npc.Path == null) Frustrate(s, npc); return; }
            if (npc.Life != null && !HouseholdHooks.CanWork(s, npc)) { Frustrate(s, npc); return; }
            var bin = Goods.BinById(s.Population, a.Store);
            if (bin == null || !HasInputs(npc, bin, def.Recipe)) { Frustrate(s, npc); return; }
            if (npc.Life != null && !HouseholdHooks.WorkTick(s, npc)) return;
            npc.Action.Progress = Math.Max(npc.Action.Progress, 1);
            a.WorkProgress += Speed(s, npc, a);
            if (a.WorkProgress < def.WorkTicks * 1000) return;
            if (!HasRoom(bin, def)) { Frustrate(s, npc); return; } // a full store: the cycle waits
            a.WorkProgress -= def.WorkTicks * 1000;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            // outputs first (room checked), then the inputs: both inside this act, so conservation holds at every boundary
            Ware outWare = Ware.None; int outCount = 0;
            foreach (var o in def.Recipe.Outputs)
            {
                int spoil = Wares.IsFood(o.Ware) ? day + PopulationModule.SpoilDays(s.Tuning.Ecology, o.Ware) : -1;
                if (Goods.Produce(s, ev, Holder.Bin(bin.Id), o.Ware, o.Count, spoil, 0, a.Owner, def.Recipe.Reason, npc.Id, 0) == 0)
                    throw new InvalidOperationException("work: no room after the room check");
                if (outWare == Ware.None) { outWare = o.Ware; outCount = o.Count; }
            }
            foreach (var i in def.Recipe.Inputs) ConsumeInput(s, ev, npc, bin, i, a.Id);
            a.Units++;
            Assets.StateOf(s).WorkUnits++;
            byte right = Ownership.RightOn(s, npc, a, UseKind.Work);
            byte control = s.Divinity != null ? (byte)Divinity.Control.Of(s, npc.Id) : (byte)0;
            ev.Add(new AssetUsed { Asset = a.Id, Worker = npc.Id, OwnerKind = a.Owner.Kind, OwnerId = a.Owner.Id, Control = control, OutputWare = outWare, OutputCount = outCount, Right = right });
            if (right == Ownership.RightNone)
            {
                Assets.StateOf(s).Thefts++;
                ev.Add(new TheftCommitted { Kind = TheftCommitted.Use, Asset = a.Id, Ware = outWare, Count = outCount, OwnerKind = a.Owner.Kind, OwnerId = a.Owner.Id,
                    Taker = npc.Id, Node = a.Node, FromKind = HolderKind.Bin, FromHolder = bin.Id });
            }
            Ownership.Witness(s, a, npc, s.Tuning.Civ.C1.SeenUsedConfidence);
            End(npc);
        }

        /// <summary>Every input of one cycle is there: in the store (any lot), or carried and the worker's own or its household's.</summary>
        public static bool HasInputs(Npc npc, LotBin bin, Recipe r)
        {
            foreach (var i in r.Inputs)
            {
                int have = 0;
                foreach (var l in bin.Lots) if (l.Ware == i.Ware) have += l.Count;
                foreach (var l in npc.Carry) if (!l.Empty && l.Ware == i.Ware && Ownership.OwnerRelation(npc, l.Owner) != Ownership.RightNone) have += l.Count;
                if (have < i.Count) return false;
            }
            return true;
        }

        private static bool HasRoom(LotBin bin, AssetDef def)
        {
            if (bin.WeightCap <= 0) return true;
            int w = bin.Weight;
            foreach (var i in def.Recipe.Inputs)
            {
                // inputs taken from the store free their weight
                int inStore = 0;
                foreach (var l in bin.Lots) if (l.Ware == i.Ware) inStore += l.Count;
                w -= PopulationModule.Weight(i.Ware) * Math.Min(inStore, i.Count);
            }
            foreach (var o in def.Recipe.Outputs) w += PopulationModule.Weight(o.Ware) * o.Count;
            return w <= bin.WeightCap;
        }

        private static void ConsumeInput(GodState s, EventBuffer ev, Npc npc, LotBin bin, WareCount i, int assetId)
        {
            int left = i.Count;
            for (int k = 0; k < bin.Lots.Count && left > 0;)
            {
                var l = bin.Lots[k];
                if (l.Ware != i.Ware) { k++; continue; }
                int n = Math.Min(left, l.Count);
                bool whole = n == l.Count;
                if (!Goods.Consume(s, ev, Holder.Bin(bin.Id), l.Id, n, ConsumeReason.Built, npc.Id, assetId)) throw new InvalidOperationException("work: input vanished");
                left -= n;
                if (!whole) k++;
            }
            for (int k = 0; k < npc.Carry.Length && left > 0; k++)
            {
                var l = npc.Carry[k];
                if (l.Empty || l.Ware != i.Ware || Ownership.OwnerRelation(npc, l.Owner) == Ownership.RightNone) continue;
                int n = Math.Min(left, l.Count);
                if (!Goods.Consume(s, ev, Holder.Carry(npc.Id), l.Id, n, ConsumeReason.Built, npc.Id, assetId)) throw new InvalidOperationException("work: input vanished");
                left -= n;
            }
            if (left > 0) throw new InvalidOperationException("work: inputs short after the input check");
        }

        /// <summary>
        /// Haul (<see cref="MoveReason.Deliver"/>): the carrier, within <see cref="Reach.Store"/> of the store (walked to first:
        /// Goods does not check reach for this reason, so this act does), puts the recipe inputs it carries (its own or its
        /// household's) into the asset's store; the lots keep their owner. Only the owner's people or a Store right holder.
        /// </summary>
        private static void Haul(GodState s, EventBuffer ev, Npc npc)
        {
            var a = Assets.Find(s, npc.Action.TargetId);
            var def = a != null ? AssetTable.Get(a.Kind) : null;
            if (a == null || !a.IsFinished || a.Store == 0) { Frustrate(s, npc); return; }
            if (!WalkTo(s, npc, a.Node, Reach.Store)) { if (npc.Path == null) Frustrate(s, npc); return; }
            if (Ownership.RightOn(s, npc, a, UseKind.Store) == Ownership.RightNone) { Frustrate(s, npc); return; }
            if (s.Territory.Grid.Distance(npc.Node, a.Node) > Reach.Store) { Frustrate(s, npc); return; } // no remote transfer
            int put = 0;
            for (int i = 0; i < npc.Carry.Length; i++)
            {
                var l = npc.Carry[i];
                if (l.Empty || Ownership.OwnerRelation(npc, l.Owner) == Ownership.RightNone || !Accepts(def, l.Ware)) continue;
                var r = Goods.Move(s, ev, Holder.Carry(npc.Id), l.Id, l.Count, Holder.Bin(a.Store), MoveReason.Deliver, npc.Id, OwnerRule.Keep);
                if (r.Ok) put += l.Count;
            }
            if (put == 0) Frustrate(s, npc); else End(npc);
        }

        /// <summary>What a store accepts: a recipe input; a store-only kind accepts any goods.</summary>
        public static bool Accepts(AssetDef def, Ware w)
        {
            if (def.Recipe == null) return true;
            foreach (var i in def.Recipe.Inputs) if (i.Ware == w) return true;
            return false;
        }

        // ---------------- helpers ----------------

        private static void End(Npc npc)
        {
            npc.Action = new NpcAction { Since = npc.Action.Since };
            npc.Path = null; npc.PathGoal = -1;
            if (npc.Mind != null) Learner.EndOngoing(npc.Mind.Store);
        }

        private static void Frustrate(GodState s, Npc npc)
        {
            if (npc.Mind != null) Learner.AddReward(npc.Mind.Store, s.Tuning.Learning.Frustration);
            End(npc);
        }

        /// <summary>One step towards a node (M3's walking rule, C1's own path cache): true when within <paramref name="reach"/>.</summary>
        public static bool WalkTo(GodState s, Npc npc, int goal, int reach)
        {
            var g = s.Territory.Grid;
            if (g.Distance(npc.Node, goal) <= reach) { npc.Path = null; npc.PathGoal = -1; npc.StepTicks = 0; return true; }
            var tu = s.Tuning.Population;
            var o = Assets.StateOf(s);
            if (o.Pather == null) o.Pather = new NodePather(g);
            if (npc.Path == null || npc.PathGoal != goal || npc.PathIdx >= npc.Path.Length)
            {
                npc.Path = o.Pather.Find(npc.Node, goal, s.Territory.NodeRoad, tu.RoadStepTicks, tu.StepTicks, tu.PathMaxExpand);
                npc.PathIdx = 0; npc.PathGoal = goal; npc.StepTicks = 0;
                if (npc.Path == null || npc.Path.Length == 0) { npc.Path = null; return false; }
            }
            int next = npc.Path[npc.PathIdx];
            int cost = s.Territory.NodeRoad[next] != 0 ? tu.RoadStepTicks : tu.StepTicks;
            if (++npc.StepTicks >= cost) { npc.StepTicks = 0; npc.Node = next; npc.PathIdx++; }
            return g.Distance(npc.Node, goal) <= reach;
        }
    }
}
