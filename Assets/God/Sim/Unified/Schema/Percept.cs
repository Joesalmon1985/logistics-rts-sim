namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.6): one person's encoded perception, the input of <see cref="IPrimitivePolicy"/>. Shell: UN2 adds its fields
    /// and <c>Encode</c>/<c>ToVector</c> (<see cref="ObsSchema"/>) in its own partial file. Local only; no owner field, no true-state
    /// field, no hidden path.
    /// </summary>
    public partial struct Percept
    {
        /// <summary>The perceiving person.</summary>
        public int Actor;
        public long Tick;
    }
}
