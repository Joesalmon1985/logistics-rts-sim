using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN6's slots in <see cref="UnifiedHooks"/> (spec 5.5): the decision site of <c>PersonTick</c> (a person decides only when its
    /// primitive completed, failed or was interrupted; O(1) otherwise), the brain of a new person (innate start state, R83) and the
    /// brain invariants. OWNER: UN6.
    /// </summary>
    public static partial class UnifiedHooks
    {
        static partial void PersonTickUN6(GodState s, EventBuffer ev, Npc npc)
        {
            var rt = s.Unification?.BrainRuntime;
            if (rt == null) return;
            var rec = s.Unification.Brain;
            if (rec.AttachedTick < 0)
            {
                rec.AttachedTick = s.Tick;
                ev?.Add(new BrainAttached { Kind = rec.Kind, ArtifactVersion = rec.ArtifactVersion, WeightsSha256 = rec.WeightsSha256, Label = rec.Label });
            }
            var u = s.Unification.PersonOf(npc.Id);
            if (!BrainDecider.Due(s, npc, u) || !rt.Body.Available(s)) { rt.Skipped++; return; }
            BrainDecider.Decide(s, ev, npc, u, rt);
        }

        static partial void NewPersonUN6(GodState s, Npc npc)
        {
            var u = s.Unification.PersonOf(npc.Id);
            if (u != null) BrainDecider.Init(s, npc, u);
        }

        static partial void CheckUN6(GodState s, List<string> bad)
        {
            var un = s.Unification;
            int cap = s.Tuning.Unification.UN6.OverlayEntries;
            foreach (var p in un.People)
            {
                var b = p.Brain;
                if (b == null) { bad.Add("UN6: person " + p.Npc + " has no brain state"); continue; }
                if (b.Recurrent.Length != BrainState.RecurrentSize || b.Places.Length != BrainState.PlaceSlots || b.People.Length != BrainState.PeopleSlots
                    || b.Claims.Length != BrainState.ClaimSlots || b.PrevNeeds.Length != NeedChannels.Count)
                    bad.Add("UN6: person " + p.Npc + " brain arrays resized");
                if (b.Bytes > BrainState.MaxBytes) bad.Add("UN6: person " + p.Npc + " brain state " + b.Bytes + " bytes > " + BrainState.MaxBytes);
                if (b.Overlay != 0 && (b.Overlay != p.Npc || un.OverlayOf(b.Overlay) == null)) bad.Add("UN6: person " + p.Npc + " names overlay " + b.Overlay + " it does not own");
            }
            for (int i = 0; i < un.Overlays.Count; i++)
            {
                var o = un.Overlays[i];
                if (i > 0 && un.Overlays[i - 1].Npc >= o.Npc) bad.Add("UN6: overlays not sorted by NpcId at " + o.Npc);
                if (o.Store == null || o.Store.Count > o.Store.EntryCap || o.Store.EntryCap > (cap < 1 ? 1 : cap)) bad.Add("UN6: overlay of " + o.Npc + " over its bound");
            }
        }
    }

    /// <summary>UN6's slots in <see cref="UnifiedModule"/>: every person's brain at the start; the runtime on create and load.</summary>
    public static partial class UnifiedModule
    {
        static partial void StartUN6(GodState s)
        {
            if (s.Unification.BrainRuntime == null) s.Unification.BrainRuntime = BrainRuntime.Build(s);
            foreach (var n in s.Population.Npcs)
            {
                if (!Bodies.IsPerson(n)) continue;
                var u = s.Unification.PersonOf(n.Id);
                if (u != null) BrainDecider.Init(s, n, u);
            }
        }

        static partial void InstallUN6(GodSim sim)
        {
            var s = sim.State;
            if (s.Unification.BrainRuntime == null) s.Unification.BrainRuntime = BrainRuntime.Build(s);
        }
    }
}
