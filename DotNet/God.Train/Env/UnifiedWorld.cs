using System;
using God.Sim;
using God.Sim.Unified;
using God.Train.Honesty;

namespace God.Train.Env
{
    /// <summary>
    /// The real environment: a <c>GodSim</c> with <c>Unification.Enabled = 1</c>, the same catalogue, needs, executor and encoder
    /// as the game. It is built against the contracts' surfaces (23.5 <c>Motor</c>, 23.6 <c>Percept</c>, <c>IPrimitivePolicy</c>)
    /// and becomes available when UN1 (world, needs), UN2 (executor, encoder) and UN6 (brain runtime) have merged.
    /// <para><b>Seams UN6t-b needs from the merged packages (amendment requests to the lead, listed in the UN6t-a record):</b>
    /// (1) UN2: <c>Percept.Encode(s, actor, ref p)</c> and <c>Percept.ToVector(in p, obs)</c> plus the percept's entity-slot →
    /// <see cref="TargetRef"/> table, so a head choice can be decoded into a <see cref="PrimitiveControl"/>; (2) UN6: a per-<c>GodSim</c>
    /// (not static) policy injection, so the brain runtime's own decision site (<c>PersonTickUN6</c>) runs the trainer's network and
    /// the teacher (origin <see cref="ControlOrigin.Teacher"/>) in exactly the game's decision order; (3) the effect events of
    /// UN3b/UN4/UN7 for the evaluation facts (shelter completed, raw collected then deposited, hand-over).</para>
    /// </summary>
    public static class UnifiedWorld
    {
        /// <summary>
        /// Probes the merged simulation: builds a unified game and asks the executor for a legality mask. Returns null when every
        /// surface answers, else the reason it cannot run yet (the trainer then refuses to produce any result on it).
        /// </summary>
        public static string Unavailable(ulong seed)
        {
            SeedPolicy.Check((long)seed, Layout.Train, null);
            try
            {
                var tuning = new SimTuning();
                tuning.Unification.Enabled = 1;
                var sim = GodSim.Create(new GameSetup(seed), tuning);
                if (sim.State.Unification == null) return "GodSim.Create did not create the unified state";
                if (sim.State.Unification.People.Count == 0) return "the unified start has no persons (UN5/UN1 not merged)";
                var p = default(Percept); var m = default(ActMask);
                Motor.Legal(sim.State, sim.State.Unification.People[0].Npc, in p, ref m);
                return "Percept.Encode/ToVector and the UN6 policy-injection seam are not declared yet (UN2/UN6 not merged)";
            }
            catch (NotImplementedException e) { return "not merged: " + e.Message; }
            catch (Exception e) { return "unified game failed to start: " + e.GetType().Name + ": " + e.Message; }
        }
    }
}
