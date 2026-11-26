using God.Sim;
using God.Sim.Population;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Draws the gestures a person is making (compositional acts, COMMUNICATION 4.8) on a person's figure (F0.15 stub; OWNER: M5). Called by <see cref="NpcLayer"/> once per drawn person per
    /// frame, after the figure is placed and animated. Reads the sim; never changes it.
    /// </summary>
    public static class GestureLayer
    {
        public static void Apply(Transform figure, Npc n, GodState s, float time) { }
    }
}
