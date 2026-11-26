using God.Sim;
using God.Sim.Population;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Draws a person's look by life stage and its visible displays (begging, courtship, care requests) on a person's figure (F0.15 stub; OWNER: M4). Called by <see cref="NpcLayer"/> once per drawn person per
    /// frame, after the figure is placed and animated. Reads the sim; never changes it.
    /// </summary>
    public static class LifeStageLayer
    {
        public static void Apply(Transform figure, Npc n, GodState s) { }
    }
}
