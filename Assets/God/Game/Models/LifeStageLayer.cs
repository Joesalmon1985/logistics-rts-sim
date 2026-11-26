using God.Sim;
using God.Sim.Population;
using God.Sim.Population.Households;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Draws a person's look by life stage and its visible displays on its figure (OWNER: M4; POPULATION 6, HOUSEHOLDS 6).
    /// Called by <see cref="NpcLayer"/> once per drawn person per frame, after the figure is placed and animated: a child is
    /// drawn smaller by its growth (half size at birth, full at adulthood), an elder a little shorter and stooped, an expecting
    /// mother a little wider, a begging child bobs by its display's intensity. Reads the sim; never changes it.
    /// </summary>
    public static class LifeStageLayer
    {
        public static void Apply(Transform figure, Npc n, GodState s)
        {
            float k = NpcLayer.Scale;
            var l = n.Life;
            if (l == null) { figure.localScale = Vector3.one * k; return; }
            if (n.Stage == LifeStage.Child)
            {
                float g = 0.5f + 0.5f * Mathf.Clamp01(l.Growth / 1000f);
                figure.localScale = Vector3.one * (k * g);
                if (l.Display == DisplayKind.Beg && l.DisplayUntil >= s.Tick && !n.Action.Asleep)
                {
                    float bob = 0.04f * l.DisplayLevel * Mathf.Abs(Mathf.Sin(Time.time * 9f));
                    figure.position += Vector3.up * bob;
                }
                return;
            }
            var scale = Vector3.one * k;
            if (n.Stage == LifeStage.Elder) scale.y *= 0.93f;
            if (l.PregFather != 0) { scale.x *= 1.08f; scale.z *= 1.08f; }
            figure.localScale = scale;
            if (n.Stage == LifeStage.Elder && !n.Action.Asleep) figure.rotation *= Quaternion.Euler(8f, 0f, 0f);
        }
    }
}
