namespace God.Sim.Policy
{
    /// <summary>
    /// The stage 3 soldier rules as an <see cref="IMilitaryPolicy"/> (MILITARY_DESIGN: the benchmark policy; the default
    /// until soldiers are NPCs at M9). Exactly the old <c>MilitarySystem.Think</c>/<c>Acquire</c> order, from the soldier's
    /// own observation:
    /// <list type="number">
    /// <item>an enemy unit in sight: the nearest (first in the sim's order on a tie): <see cref="NpcVerb.Attack"/> when in
    /// reach, else <see cref="NpcVerb.Charge"/> at it;</item>
    /// <item>else an enemy building in sight: the first marked one (marking order), else the nearest edge: attack or charge;</item>
    /// <item>else on guard: <see cref="NpcVerb.HoldPost"/> (the guard post);</item>
    /// <item>else a marked target stands: <see cref="NpcVerb.Charge"/> to its point (the march);</item>
    /// <item>else <see cref="NpcVerb.HoldPost"/> with no post: wait at the base.</item>
    /// </list>
    /// Never retreats, deserts or surrenders; draws no random numbers; reads no mind.
    /// </summary>
    public sealed class Stage3MilitaryPolicy : IMilitaryPolicy
    {
        public string Name => PolicyAssignment.StageThreeMilitary;
        public PolicyLevel Level => PolicyLevel.Military;

        public NpcIntent Decide(in SoldierObservation obs, MindView mind, ref Rng rng)
        {
            var enemies = obs.Enemies;
            int best = -1; long bestD = long.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i].Dist2 < bestD) { bestD = enemies[i].Dist2; best = i; }
            if (best >= 0)
            {
                var e = enemies[best];
                return new NpcIntent(e.Edge <= obs.RangeMm ? NpcVerb.Attack : NpcVerb.Charge, IntentTarget.Unit, e.Id, e.X, e.Z);
            }

            var seen = obs.EnemyBuildings;
            int pick = -1;
            for (int m = 0; m < obs.Marked.Count && pick < 0; m++)
                for (int i = 0; i < seen.Count; i++)
                    if (seen[i].Id == obs.Marked[m]) { pick = i; break; }
            if (pick < 0)
            {
                int bestEdge = int.MaxValue;
                for (int i = 0; i < seen.Count; i++) if (seen[i].Edge < bestEdge) { bestEdge = seen[i].Edge; pick = i; }
            }
            if (pick >= 0)
            {
                var b = seen[pick];
                return new NpcIntent(b.Edge <= obs.RangeMm ? NpcVerb.Attack : NpcVerb.Charge, IntentTarget.Building, b.Id, b.X, b.Z);
            }

            if (obs.OnGuard) return new NpcIntent(NpcVerb.HoldPost);
            if (obs.HasMarchTarget) return new NpcIntent(NpcVerb.Charge, IntentTarget.Point, 0, obs.MarchX, obs.MarchZ);
            return new NpcIntent(NpcVerb.HoldPost);
        }
    }
}
