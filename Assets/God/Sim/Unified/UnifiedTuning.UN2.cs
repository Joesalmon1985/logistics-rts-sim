namespace God.Sim.Unified
{
    /// <summary>
    /// <c>Unification.UN2</c>: OWNER UN2 (the motor, contracts 23.5, D9.4 a). Durations are in ticks and come from here (or, for USE,
    /// from the interaction row); nothing is a constant in code. Written only when changed.
    /// </summary>
    public sealed partial class UN2Tuning : IOmitWhenDefault
    {
        /// <summary>One lattice edge (D9.4 a: 6; a road edge 4 once UB1 lands roads).</summary>
        public int StepTicks = 6, RoadStepTicks = 4;
        /// <summary>One facing sector (60 degrees).</summary>
        public int TurnTicks = 1;
        /// <summary>WAIT.</summary>
        public int WaitTicks = 10;
        /// <summary>TAKE, DROP and GESTURE apply when submitted; the body is then busy for these ticks.</summary>
        public int TakeTicks = 1, DropTicks = 1, GestureTicks = 1;
        /// <summary>The perception radius in lattice steps (0 = the population's day/night sight, <c>SightDay</c>/<c>SightNight</c>).</summary>
        public int SightNodes;
        /// <summary>A body whose health is at or below this permille cannot resist a TAKE (unconscious; UN8a may refine).</summary>
        public int UnconsciousHealthPermille = 50;
        /// <summary>How long (ticks) a resisted TAKE stays in the holder's perception (an entity state bit).</summary>
        public int ResistedMemoryTicks = 60;
    }
}
