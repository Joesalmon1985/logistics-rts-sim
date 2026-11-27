namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 1: the only allocator of <c>InterventionId</c> (<see cref="DivinityState.NextInterventionId"/>), called only
    /// from a divine command's <c>Apply</c>, after <see cref="Mythic.TrySpend"/> succeeded. One <see cref="InterventionRecord"/>
    /// and one <see cref="DivineActionAccepted"/> per accepted divine action of any kind. OWNER: D1.
    /// </summary>
    public static class Interventions
    {
        /// <summary>The id the next <see cref="Open"/> will return (what <see cref="Mythic.TrySpend"/> records on its event).</summary>
        public static int Peek(GodState s) => s.Divinity?.NextInterventionId ?? 0;

        /// <summary>Opens an intervention: takes the next id, records it and emits <see cref="DivineActionAccepted"/>.</summary>
        public static int Open(GodState s, EventBuffer ev, int god, InterventionKind kind, DivineAct act, DivineTarget target, DivineScope scope, long costMilli)
        {
            var d = s.Divinity ?? throw new System.InvalidOperationException("Interventions.Open without divinity state");
            if (d.Interventions == null) d.Interventions = new InterventionRegistry();
            long charged = Mythic.ModeOf(s) == DivinityMode.Unlimited ? 0 : costMilli;
            int id = DivinityIds.Take(ref d.NextInterventionId);
            d.Interventions.Items.Add(new InterventionRecord
            {
                Id = id, God = god, Kind = kind, Act = act, Target = target, Scope = scope, Cost = costMilli, Charged = charged, Tick = s.Tick,
            });
            ev.Add(new DivineActionAccepted
            {
                Intervention = id, God = god, Kind = kind, Act = act, TargetKind = target.Kind, TargetId = target.Id, TargetNode = target.Node,
                TargetRadius = target.Radius, Scope = scope, Cost = costMilli, Charged = charged,
            });
            return id;
        }

        /// <summary>The record of an intervention (false when unknown).</summary>
        public static bool TryGet(GodState s, int id, out InterventionRecord r)
        {
            var reg = s.Divinity?.Interventions;
            if (reg == null) { r = default; return false; }
            return reg.TryGet(id, out r);
        }
    }
}
