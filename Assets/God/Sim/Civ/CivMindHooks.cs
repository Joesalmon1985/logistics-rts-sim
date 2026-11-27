using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;

namespace God.Sim.Civ
{
    /// <summary>
    /// C1's mind-side slots (OWNER: C1): the verb rows, instincts and candidates of the physical economy. Like every mind
    /// hook they read only the think scratch (what is sensed now, the mind's own beliefs, its carry): never the true state,
    /// never <c>Permits.Truth</c> (a static test). Permission is <see cref="Permits.BelievedAsset"/>, a belief.
    /// </summary>
    public static partial class CivMindHooks
    {
        static partial void VerbsC1(List<VerbInfo> into)
        {
            into.Add(new VerbInfo(Work.VerbWork, "work", VerbGroup.Production, Work.IntentWork));
            into.Add(new VerbInfo(Work.VerbBuild, "build an asset", VerbGroup.Production, Work.IntentBuild));
            // C1c: deferred, so taking food out of a store waits for that lot's fate (eaten or spoiled) as a picked-up lot does
            into.Add(new VerbInfo(Work.VerbHaul, "haul", VerbGroup.Production, Work.IntentHaul, deferred: true));
        }

        /// <summary>
        /// Innate priors (only matter for C1's own verbs, which exist only while Civ is on): a log in hand at a free plot or
        /// at one's own unfinished site draws building; a site with its materials in draws its finishing work; a field, or a
        /// stocked workshop, draws work; a log in hand near one's own workshop draws hauling.
        /// </summary>
        static partial void InstinctsC1(List<Instinct> into)
        {
            uint log = Feature.Self(SelfFeature.CarryingLog);
            into.Add(new Instinct(log, Feature.Class(CivClass.Plot), Work.VerbBuild, 200));
            into.Add(new Instinct(log, Feature.State(ObjState.Unfinished), Work.VerbBuild, 300));
            into.Add(new Instinct(Feature.State(ObjState.Full), Feature.Category(ObjCategory.Workplace), Work.VerbBuild, 300));
            into.Add(new Instinct(Feature.Class(CivClass.Field), 0, Work.VerbWork, 200));
            into.Add(new Instinct(Feature.Class(CivClass.Workshop), Feature.State(ObjState.StockHigh), Work.VerbWork, 200));
            into.Add(new Instinct(Feature.Class(CivClass.Workshop), Feature.State(ObjState.StockLow), Work.VerbWork, 150));
            into.Add(new Instinct(log, Feature.Class(CivClass.Workshop), Work.VerbHaul, 250));
            // C1c: hunger draws food out of one's own store (as M4's household store does); a store with no room left for
            // another cycle, or well stocked, draws its output out (to carry home, pay or trade)
            uint store = Feature.Class(CivClass.AssetStore);
            into.Add(new Instinct(Feature.DriveActive(Drive.Hunger), Feature.Category(ObjCategory.Food), Work.VerbHaul, 450));
            into.Add(new Instinct(Feature.DriveBand(Drive.Hunger, 1), Feature.Category(ObjCategory.Food), Work.VerbHaul, 200));
            into.Add(new Instinct(Feature.State(ObjState.Full), store, Work.VerbHaul, 350));
            into.Add(new Instinct(Feature.State(ObjState.StockHigh), store, Work.VerbHaul, 150));
        }

        /// <summary>
        /// Candidates (quota 4): build at a free plot it perceives (one kind it does not believe it owns yet, two at most);
        /// build or finish its own site; work an asset it believes it may work (seen now, else remembered); haul a log to its
        /// own workshop when that looks short of inputs.
        /// </summary>
        static partial void AddCandidatesC1(ThinkScratch sc)
        {
            if (sc.Mind == null) return;
            var self = sc.Self;
            bool workCand = false;
            TakeOutCandidate(sc);
            for (int i = 0; i < sc.SeenCount; i++)
            {
                var t = sc.Seen[i];
                if (t.Kind == IntentTarget.Node && t.Class == CivClass.Plot)
                {
                    int offered = 0;
                    foreach (var d in AssetTable.Rows)
                    {
                        if (offered >= 2) break;
                        if (OwnsKindBelief(sc, d.ObjClass)) continue;
                        int at = Thinker.AddCandidate(sc, Work.VerbBuild, IntentTarget.Node, 0, t.Node, (byte)d.ObjClass, 0);
                        if (at < 0) return;
                        Thinker.TargetFeatures(sc, at, Feature.Class(CivClass.Plot), Feature.Class(d.ObjClass), Feature.Category(ObjCategory.Workplace), Feature.PlaceRef(t.Node));
                        offered++;
                    }
                    continue;
                }
                if (t.Kind != IntentTarget.Building || !CivClass.IsAsset(t.Class) || t.Ref == 0) continue;
                var own = Permits.BelievedAsset(sc, t.Ref, UseKind.Work);
                bool owner = own.Allowed && own.Basis != PermissionBasis.UseRight;
                if (t.State == ObjState.Unfinished || t.State == ObjState.Full)
                {
                    if (owner && (t.State == ObjState.Full || self.CarryLog)) Cand(sc, Work.VerbBuild, t.Ref, t.Node, t.Class, t.State, t.Dist, false);
                    continue;
                }
                var def = AssetTable.OfClass(t.Class);
                if (def == null || def.WorkTicks <= 0) continue;
                if (own.Allowed && (t.State != ObjState.StockEmpty || self.CarryLog)) { Cand(sc, Work.VerbWork, t.Ref, t.Node, t.Class, t.State, t.Dist, false); workCand = true; }
                if (owner && self.CarryLog && t.State != ObjState.StockHigh && def.Recipe != null && def.Recipe.Inputs.Count > 0)
                    Cand(sc, Work.VerbHaul, t.Ref, t.Node, t.Class, t.State, t.Dist, false);
            }
            if (workCand) return;
            // remembered: the nearest asset out of sight it believes it may work, within recall
            var cm = sc.Mind.Civ;
            if (cm == null) return;
            int best = -1, bestD = int.MaxValue;
            for (int i = 0; i < cm.Own.Count; i++)
            {
                var b = cm.Own[i];
                var def = AssetTable.OfClass(b.Class);
                if (def == null || def.WorkTicks <= 0 || !Permits.BelievedAsset(sc, b.Asset, UseKind.Work).Allowed) continue;
                bool seen = false;
                for (int k = 0; k < sc.SeenCount; k++) if (sc.Seen[k].Kind == IntentTarget.Building && sc.Seen[k].Ref == b.Asset) { seen = true; break; }
                if (seen) continue;
                int d = sc.Grid.Distance(self.Node, b.Node);
                if (d > sc.Tuning.RecallNodes || d >= bestD) continue;
                best = i; bestD = d;
            }
            if (best >= 0) Cand(sc, Work.VerbWork, cm.Own[best].Asset, cm.Own[best].Node, cm.Own[best].Class, 0, bestD, true);
        }

        /// <summary>
        /// C1c: take output out of an asset's store: the store it sees now (the Sense seam: a bin of class
        /// <see cref="CivClass.AssetStore"/>), else one it remembers (a store belief within recall), with something there it
        /// may take (the band is not empty), when it believes the asset its own, its household's, or that it holds a Store
        /// right (<see cref="Permits.BelievedAsset"/>: a belief, never the truth). One candidate at most, the nearest.
        /// </summary>
        private static void TakeOutCandidate(ThinkScratch sc)
        {
            int best = -1, bestD = int.MaxValue;
            for (int i = 0; i < sc.SeenCount; i++)
            {
                var t = sc.Seen[i];
                if (t.Kind != IntentTarget.Bin || t.Class != CivClass.AssetStore || t.Ref == 0 || t.State == ObjState.StockEmpty) continue;
                if (t.Dist >= bestD || MayTakeBelieved(sc, t.Node) == 0) continue;
                best = i; bestD = t.Dist;
            }
            if (best >= 0)
            {
                var t = sc.Seen[best];
                StoreCand(sc, t.Ref, t.Node, t.State, t.Category, t.Dist, false);
                return;
            }
            var b = sc.Mind.Beliefs;
            int bi = -1;
            for (int i = 0; i < b.Count; i++)
            {
                var bel = b.Slots[i];
                if (bel.Kind != BeliefKind.Store || bel.Class != CivClass.AssetStore || bel.Ref == 0 || bel.Stock == ObjState.StockEmpty) continue;
                bool seenNow = false;
                for (int k = 0; k < sc.SeenCount; k++) if (sc.Seen[k].Kind == IntentTarget.Bin && sc.Seen[k].Ref == bel.Ref) { seenNow = true; break; }
                if (seenNow) continue;
                int d = sc.Grid.Distance(sc.Self.Node, bel.Subject);
                if (d > sc.Tuning.RecallNodes || d >= bestD || MayTakeBelieved(sc, bel.Subject) == 0) continue;
                bi = i; bestD = d;
            }
            if (bi < 0) return;
            // remembered: what it knows of the asset's kind says whether its output is food
            var def = AssetTable.OfClass(MayTakeBelieved(sc, b.Slots[bi].Subject));
            bool food = false;
            if (def?.Recipe != null) foreach (var o in def.Recipe.Outputs) if (Wares.IsFood(o.Ware)) food = true;
            StoreCand(sc, b.Slots[bi].Ref, b.Slots[bi].Subject, b.Slots[bi].Stock, food ? ObjCategory.Food : ObjCategory.Store, bestD, true);
        }

        /// <summary>The class of the asset at a node whose store it believes it may take from (owner, household or a Store right, from its ownership beliefs); 0 none.</summary>
        private static int MayTakeBelieved(ThinkScratch sc, int node)
        {
            var cm = sc.Mind.Civ;
            if (cm == null) return 0;
            foreach (var o in cm.Own)
                if (o.Node == node && Permits.BelievedAsset(sc, o.Asset, UseKind.Store).Allowed) return o.Class;
            return 0;
        }

        private static void StoreCand(ThinkScratch sc, int bin, int node, int state, int category, int dist, bool fromBelief)
        {
            int at = Thinker.AddCandidate(sc, Work.VerbHaul, IntentTarget.Bin, bin, node, CivClass.AssetStore, dist, fromBelief);
            if (at < 0) return;
            sc.Cand[at].Intent = Work.IntentTakeOut; // the learning verb is shared with delivering; the bin target and its class tell them apart
            Thinker.TargetFeatures(sc, at, Feature.Class(CivClass.AssetStore), category != 0 ? Feature.Category(category) : Feature.Category(ObjCategory.Store),
                state != 0 ? Feature.State(state) : 0, Feature.PlaceRef(node));
        }

        private static bool OwnsKindBelief(ThinkScratch sc, int cls)
        {
            var cm = sc.Mind.Civ;
            if (cm == null) return false;
            foreach (var b in cm.Own)
                if (b.Class == cls) { var p = Permits.Believed(sc, b.BelievedOwner); if (p.Allowed && p.Basis != PermissionBasis.Ownerless) return true; }
            return false;
        }

        private static void Cand(ThinkScratch sc, byte verb, int asset, int node, int cls, int state, int dist, bool fromBelief)
        {
            int at = Thinker.AddCandidate(sc, verb, IntentTarget.Building, asset, node, (byte)cls, dist, fromBelief);
            if (at < 0) return;
            Thinker.TargetFeatures(sc, at, Feature.Class(cls), Feature.Category(ObjCategory.Workplace), state != 0 ? Feature.State(state) : 0, Feature.PlaceRef(node));
        }

        static partial void DescribeC1(uint f, ref string text)
        {
            if (text != null || (f >> 24) != (uint)FeatureKind.ObjClass) return;
            switch ((int)(f & 0xFFFFFF))
            {
                case CivClass.Field: text = "a field"; break;
                case CivClass.Workshop: text = "a workshop"; break;
                case CivClass.Store: text = "a store building"; break;
                case CivClass.Plot: text = "a free plot"; break;
                case CivClass.AssetStore: text = "the store of an asset"; break;
            }
        }
    }
}
