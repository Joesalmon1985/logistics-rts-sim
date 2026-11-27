using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// The tutorial's pop-up card (stage 3), in the HUD's cloud look. It shows one <see cref="TutorialStep"/> of the
    /// <see cref="TutorialGuide"/> in the bottom-left corner: a title, a few short lines, a goal line that ticks off by
    /// itself, a Next (or Skip step) button and Hide tutorial. It never dims or blocks the game. Progress is read from
    /// sim state each frame; the buttons are public methods (tests call them). The guide is saved with the game.
    /// The build-bar glow and the map pointer are drawn by the HUD (<c>GodHud.Tutorial.cs</c>), which reads this.
    /// </summary>
    public sealed class TutorialCards : MonoBehaviour, ISaveExtra
    {
        private const int WinId = 9101;

        public GodBootstrap Boot { get; private set; }
        public TutorialGuide Guide { get; } = new TutorialGuide();

        private Rect _rect;
        private GUIStyle _goal, _count;

        public static TutorialCards Create(GodBootstrap boot)
        {
            var go = new GameObject("TutorialCards");
            go.transform.SetParent(boot.transform, false);
            var c = go.AddComponent<TutorialCards>();
            c.Boot = boot;
            return c;
        }

        /// <summary>The card showing now, or null (hidden, finished, watching, or the game is over).</summary>
        public TutorialStep ActiveStep
        {
            get
            {
                var hud = Boot.Hud;
                if (hud == null || hud.Watching || hud.Outcome != GameOutcome.Playing || !Guide.Visible) return null;
                return Guide.Step;
            }
        }

        // ---------------------------------------------------------------- buttons

        /// <summary>The left button: Next when the goal is done or there is none, else Skip step.</summary>
        public void Next() { Guide.Skip(); }

        /// <summary>Hide tutorial: the cards go away until tips are switched on in the Esc menu.</summary>
        public void HideTips() { Guide.Hide(); }
        public void ShowTips() { Guide.Show(); }
        public void ToggleTips() { if (Guide.Hidden) Guide.Show(); else Guide.Hide(); }

        // ---------------------------------------------------------------- progress

        private void Update()
        {
            if (Boot == null || Boot.Hud == null) return;
            var hud = Boot.Hud;
            if (hud.Watching || hud.Outcome != GameOutcome.Playing) return;
            Guide.Update(Boot.Sim.State, hud.Me);
        }

        // ---------------------------------------------------------------- drawing

        private void OnGUI()
        {
            var hud = Boot != null ? Boot.Hud : null;
            if (hud == null) return;
            var step = ActiveStep;
            if (step == null) { if (hud.External.Count > 0 && Event.current.type == EventType.Repaint) hud.External.Clear(); return; }
            var st = HudStyle.Get();
            if (_goal == null)
            {
                _goal = new GUIStyle(st.Label) { fontStyle = FontStyle.Bold, fontSize = 12 };
                _count = new GUIStyle(st.Small) { alignment = TextAnchor.MiddleRight, wordWrap = false };
            }
            float s = hud.Scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            GUI.enabled = hud.HudOn;
            float vw = Screen.width / s, vh = Screen.height / s;
            const float w = 330f;
            float h = _rect.height > 1f ? _rect.height : 200f;
            var r = new Rect(14f, Mathf.Max(10f, vh - h - 14f), w, 0f);
            _rect = GUILayout.Window(WinId, r, id => Card(st, step), GUIContent.none, st.Window, GUILayout.Width(w));
            if (Event.current.type == EventType.Repaint) { hud.External.Clear(); hud.External.Add(_rect); }
        }

        private void Card(HudStyle st, TutorialStep step)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(step.Title, st.Title);
            GUILayout.FlexibleSpace();
            GUILayout.Label((Guide.Index + 1) + " / " + Guide.StepCount, _count, GUILayout.Width(48f));
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
            foreach (var line in step.Lines) { GUILayout.Label(line, st.Label); GUILayout.Space(2f); }
            GUILayout.Space(4f);

            // The goal line: an empty box that fills green and shows a tick when the goal is done.
            GUILayout.BeginHorizontal();
            var box = GUILayoutUtility.GetRect(16f, 20f, GUILayout.Width(16f), GUILayout.Height(20f));
            if (Event.current.type == EventType.Repaint)
            {
                var b = new Rect(box.x, box.y + 3f, 14f, 14f);
                GUI.DrawTexture(b, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Guide.GoalMet ? HudStyle.Good : HudStyle.InkSoft, new Vector4(2, 2, 2, 2), new Vector4(4, 4, 4, 4));
                if (Guide.GoalMet) GUI.DrawTexture(new Rect(b.x + 3f, b.y + 3f, 8f, 8f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, HudStyle.Good, Vector4.zero, new Vector4(4, 4, 4, 4));
            }
            GUILayout.Space(4f);
            GUILayout.Label(step.Goal, _goal);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            string first = Guide.Index >= Guide.StepCount - 1 && Guide.CanContinue ? "Finish" : Guide.CanContinue ? "Next" : "Skip step";
            if (GUILayout.Button(first, Guide.CanContinue ? st.BtnSel : st.Btn, GUILayout.Height(28f))) Next();
            if (GUILayout.Button("Hide tutorial", st.Btn, GUILayout.Height(28f))) HideTips();
            GUILayout.EndHorizontal();
        }

        // ---------------------------------------------------------------- ISaveExtra

        public string SaveKey => "tutorial";
        public string SaveState() => Guide.SaveState();
        public void LoadState(string data) { Guide.LoadState(data); }
    }
}
