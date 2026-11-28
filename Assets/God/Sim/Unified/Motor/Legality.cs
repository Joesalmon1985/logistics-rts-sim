namespace God.Sim.Unified
{
    public static partial class Motor
    {
        /// <summary>Physical legality only: lands with UN2b (the percept encoder).</summary>
        public static void Legal(GodState s, int actor, in Percept p, ref ActMask mask) => throw new System.NotImplementedException("Motor.Legal: UN2b");
    }
}
