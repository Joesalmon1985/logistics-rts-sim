using System.Collections.Generic;
using God.Sim.Learning;
using Newtonsoft.Json;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN6: which brain this game runs, saved and hashed (contracts 23.14: the artifact version and hash are in the save, the weights
    /// never). <see cref="AttachedTick"/> is the tick <c>BrainAttached</c> (1320) was emitted (-1 not yet), so a load never emits it twice.
    /// </summary>
    public sealed class BrainRecord
    {
        public int Kind;
        public int ArtifactVersion;
        /// <summary>SHA-256 of the weights in use ("" for the fallback; the untrained weights' hash for Kind 0).</summary>
        public string WeightsSha256 = "";
        /// <summary>"UNTRAINED", "TRAINED", "FALLBACK" or "ARTIFACT vN STATUS" (the honest label).</summary>
        public string Label = "";
        public long AttachedTick = -1;

        public void AddTo(ref StateHasher h) { h.Add(Kind); h.Add(ArtifactVersion); h.Add(WeightsSha256); h.Add(Label); h.Add(AttachedTick); }
    }

    /// <summary>UN6: one person's associative overlay store (§D9.2 d), bounded by <c>UN6.OverlayEntries</c>; saved as the store's blob.</summary>
    public sealed class BrainOverlay
    {
        public int Npc;
        public AssociationStore Store;

        public void AddTo(ref StateHasher h) { h.Add(Npc); Store.AddTo(ref h); }
    }

    /// <summary>UN6's unified state (spec 5.4): the brain record and the overlay stores (sorted by NpcId), and the run-time cache (never saved).</summary>
    public sealed partial class UnifiedState
    {
        public BrainRecord Brain = new BrainRecord();
        public List<BrainOverlay> Overlays = new List<BrainOverlay>();

        /// <summary>The brain runtime of this game (policy, scratch, counters): rebuilt on create and load, never saved or hashed.</summary>
        [JsonIgnore] public BrainRuntime BrainRuntime;

        /// <summary>The overlay store of a person, or null (binary search).</summary>
        public AssociationStore OverlayOf(int npc)
        {
            int lo = 0, hi = Overlays.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = Overlays[mid].Npc;
                if (v == npc) return Overlays[mid].Store;
                if (v < npc) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        internal BrainOverlay OverlayOrNew(int npc, LearningCaps caps)
        {
            int lo = 0, hi = Overlays.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = Overlays[mid].Npc;
                if (v == npc) return Overlays[mid];
                if (v < npc) lo = mid + 1; else hi = mid - 1;
            }
            var o = new BrainOverlay { Npc = npc, Store = new AssociationStore(caps) { Mode = LearningMode.On } };
            Overlays.Insert(lo, o);
            return o;
        }

        partial void AddToUN6(ref StateHasher h)
        {
            Brain.AddTo(ref h);
            h.Add(Overlays.Count); foreach (var o in Overlays) o.AddTo(ref h);
        }
    }
}
