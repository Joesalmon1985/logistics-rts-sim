using God.Sim.Population;

namespace God.Sim.Comms
{
    /// <summary>
    /// Communication ranges (WORLD_ONTOLOGY 2, SHARED_CONTRACTS A7; frozen by F0): a gesture or a display is perceived by a
    /// listener within its own current sight radius (day or night) that attends the speaker. Perception, not reach: hearing
    /// or seeing something never lets anyone act on it at a distance.
    /// </summary>
    public static class CommsRange
    {
        /// <summary>The gesture and display range of a listener: its current sight radius, in nodes.</summary>
        public static int Gesture(PopulationTuning tu, bool night) => night ? tu.SightNight : tu.SightDay;
    }
}
