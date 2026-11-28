namespace God.Sim.Unified
{
    /// <summary>
    /// UN6 (§D9.2 e): bounded internal planning, only with <c>Unification.Brain.Planning = 1</c> (default 0). It searches, inside the
    /// brain, the person's own perceived entities and remembered places (its observation vector, never truth) at most
    /// <c>UN6.PlanDepth</c> steps away, over the interaction kinds the person itself knows (the catalogue's physics of what a row does),
    /// at most <c>UN6.PlanExpansions</c> expansions, and writes an <b>intention</b> (a target slot and a need channel) into the brain
    /// state. It never issues a control, never paths, holds no goal table and reads no other mind.
    /// <para><c>Obs v1</c> has no intention field, so the network cannot read the intention yet: the planner's output is a recorded
    /// belief until a schema version adds the field (amendment request in the UN6 record). Training and evaluation must use the same
    /// setting.</para>
    /// </summary>
    public static class Planner
    {
        /// <summary>The expansions of the last plan (cost counter).</summary>
        public static int Plan(GodState s, BrainState b, short[] obs, BrainRuntime rt, UN6Tuning t)
        {
            int need = -1, top = -1;
            for (int c = 0; c < NeedChannels.Count; c++) { int v = obs[rt.NeedsOff + c]; if (v > top) { top = v; need = c; } }
            b.IntentSlot = -1; b.IntentNeed = (byte)(need < 0 ? 0 : need);
            if (need < 0) return 0;
            int depth = t.PlanDepth > 4 ? 4 : t.PlanDepth, budget = t.PlanExpansions > 64 ? 64 : t.PlanExpansions;
            int expansions = 0, best = -1, bestScore = 0;
            // perceived entities (slots 0-15), then remembered places (slots 16-23)
            for (int slot = 0; slot < ObsSchema.Entities + ObsSchema.Places && expansions < budget; slot++)
            {
                int cls, dist;
                if (slot < ObsSchema.Entities) { int o = rt.EntOff + slot * ObsSchema.EntityFields; cls = obs[o]; dist = obs[o + 2]; }
                else { int o = rt.PlacesOff + (slot - ObsSchema.Entities) * ObsSchema.PlaceFields; cls = obs[o]; dist = obs[o + 2]; }
                if (cls <= 0 || dist > depth) continue;
                expansions++;
                int kind = BrainDecider.InteractionKindOf(cls, 0);
                if (kind <= 0 || !Knowledge.Knows(b, KnowledgeKind.Interaction, kind) || !Relieves(kind, need)) continue;
                int score = 1000 / (1 + (dist < 0 ? 0 : dist));
                if (score > bestScore) { bestScore = score; best = slot; }
            }
            b.IntentSlot = (sbyte)best;
            return expansions;
        }

        /// <summary>The physics of an interaction kind's first effect: which need channel it can lower.</summary>
        public static bool Relieves(int kind, int need)
        {
            switch ((EffectKind)kind)
            {
                case EffectKind.Forage: case EffectKind.Eat: return need == (int)NeedChannel.Hunger;
                case EffectKind.DrinkWash: return need == (int)NeedChannel.Thirst || need == (int)NeedChannel.Hygiene;
                case EffectKind.Occupy: return need == (int)NeedChannel.Energy || need == (int)NeedChannel.Comfort || need == (int)NeedChannel.Room;
                case EffectKind.NeedRelief: case EffectKind.Relieve: return need >= NeedChannels.FirstExtension;
                case EffectKind.Play: return need == (int)NeedChannel.Fun || need == (int)NeedChannel.Social;
                case EffectKind.Treat: return need == (int)NeedChannel.Pain;
                default: return false;
            }
        }
    }
}
