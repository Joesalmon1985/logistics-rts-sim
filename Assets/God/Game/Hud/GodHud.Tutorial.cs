using UnityEngine;
using God.Sim;

namespace God.Game
{
    /// <summary>
    /// The tutorial's pointers, drawn by the HUD: a pulsing gold frame and a bobbing diamond on the build-bar button (or
    /// its tab) a step wants, and a ring on the map spot. They show only while a card is up and its goal is not yet done.
    /// </summary>
    public sealed partial class GodHud
    {
        private int _spotStep = -1;
        private int _spotCorner = -1;

        /// <summary>The step being pointed at, or null (no tutorial, hidden, goal done, or watching).</summary>
        private TutorialStep PointedStep
        {
            get
            {
                var tc = Boot != null ? Boot.Tutorial : null;
                if (tc == null || Watching || Outcome != GameOutcome.Playing) return null;
                var step = tc.ActiveStep;
                return step != null && !tc.Guide.GoalMet ? step : null;
            }
        }

        /// <summary>The build-bar button key the tutorial points at now ("" for none).</summary>
        public string TutorialButtonKey => PointedStep?.Button ?? "";

        private bool TutorialPointsAtTab(BuildTab t)
        {
            string key = TutorialButtonKey;
            return key.Length > 0 && TabOf(key) == t && Tab != t;
        }

        private static float Pulse() => 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);

        private void DrawTutorialButtonGlow(string key, Rect r, bool tab)
        {
            string want = TutorialButtonKey;
            if (want.Length == 0 || (!tab && key != want)) return;
            if (!tab && TabOf(want) != Tab) return;
            float a = Pulse();
            var gold = new Color(1f, 0.82f, 0.25f, 0.55f + 0.45f * a);
            var fr = new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f);
            GUI.DrawTexture(fr, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, gold, new Vector4(3, 3, 3, 3), new Vector4(9, 9, 9, 9));
            DrawArrow(new Vector2(r.center.x, r.yMax + 8f), false);
        }

        /// <summary>A bobbing diamond (a turned square: IMGUI has no triangles) over or under a point, in HUD units.</summary>
        private static void DrawArrow(Vector2 tip, bool above)
        {
            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 5f;
            var c = new Vector2(tip.x, tip.y + (above ? -bob : bob));
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, c);
            GUI.DrawTexture(new Rect(c.x - 6f, c.y - 6f, 12f, 12f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(1f, 0.82f, 0.25f, 0.95f), new Vector4(2, 2, 2, 2), Vector4.zero);
            GUI.matrix = m;
        }

        /// <summary>The ring on the map spot of a step (your HQ, or the corner a second HQ can take beside fields).</summary>
        private void DrawTutorialPointers(float vw, float vh, bool repaint)
        {
            if (!repaint) return;
            var step = PointedStep;
            if (step == null || step.Spot == TutorialSpot.None) return;
            var t = Sim.State.Territory; var g = t.Grid;
            int node = -1;
            if (step.Spot == TutorialSpot.Hq)
            {
                var l = t.HqsOf(Me);
                if (l.Count > 0) node = l[0].Node;
            }
            else
            {
                int idx = Boot.Tutorial.Guide.Index;
                if (_spotStep != idx || _spotCorner < 0)
                {
                    _spotStep = idx;
                    _spotCorner = TutorialIsland.SecondHqCornerTouching(Sim, Me, God.Sim.Terrain.Fields);
                }
                if (_spotCorner >= 0) node = g.CornerNode(_spotCorner);
            }
            if (node < 0) return;
            var p = ToHud(new Vector3((float)g.X(node), 2f, (float)g.Z(node)));
            if (p.x < 0 || p.y < 0 || p.x > vw || p.y > vh) return;
            float a = Pulse();
            float size = 70f + 14f * a;
            GUI.DrawTexture(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f,
                new Color(1f, 0.82f, 0.25f, 0.35f + 0.5f * a), new Vector4(4, 4, 4, 4), new Vector4(size, size, size, size));
            DrawArrow(new Vector2(p.x, p.y - size * 0.5f - 12f), true);
        }
    }
}
