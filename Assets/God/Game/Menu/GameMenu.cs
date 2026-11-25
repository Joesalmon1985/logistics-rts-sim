using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>The page of the menu that is showing.</summary>
    public enum MenuPage { None, Start, Main, Save, Load }

    /// <summary>
    /// The start card and the Esc menu (P10), in the cloud style. While either shows, the game is held: the bootstrap
    /// does not step the sim and the HUD takes no clicks. Everything the buttons do is a public method (tests call
    /// them): resume, save and load a slot, a new island (same mode), watch the AIs, quit. It never changes sim state;
    /// loading and restarting reload the scene through the bootstrap.
    /// </summary>
    public sealed class GameMenu : MonoBehaviour
    {
        /// <summary>Replaces <c>Application.Quit</c> (tests). Null for the real thing.</summary>
        public static System.Action QuitHandler;

        public GodBootstrap Boot { get; private set; }
        public MenuPage Page { get; private set; } = MenuPage.None;
        /// <summary>True while the start card or the menu holds the game.</summary>
        public bool Blocking => Page != MenuPage.None;
        public bool IsOpen => Page != MenuPage.None;
        /// <summary>The text of the seed field on the start card.</summary>
        public string SeedText;
        /// <summary>A line under the buttons (saved, could not load).</summary>
        public string Message { get; private set; }
        public bool MessageIsError { get; private set; }

        private const int WinId = 9001;
        private Rect _rect;
        private GUIStyle _field, _title, _slotHead, _msg;

        public static GameMenu Create(GodBootstrap boot)
        {
            var go = new GameObject("GameMenu");
            go.transform.SetParent(boot.transform, false);
            var m = go.AddComponent<GameMenu>();
            m.Boot = boot;
            m.SeedText = boot.Seed.ToString();
            if (boot.ShowStartCard) m.Page = MenuPage.Start;
            return m;
        }

        // ---------------------------------------------------------------- actions

        /// <summary>Opens the Esc menu (not over the start card or the end card).</summary>
        public void Open()
        {
            if (Page != MenuPage.None || Boot.Sim.State.IsGameOver) return;
            Page = MenuPage.Main; Message = null;
        }

        public void Close() { Page = MenuPage.None; Message = null; _loadFromStart = false; }

        /// <summary>Esc inside the menu: a sub-page goes back to the menu, the menu closes. The start card ignores it.</summary>
        public void Back()
        {
            if (Page == MenuPage.Load && _loadFromStart) { Page = MenuPage.Start; _loadFromStart = false; Message = null; }
            else if (Page == MenuPage.Save || Page == MenuPage.Load) { Page = MenuPage.Main; Message = null; }
            else if (Page == MenuPage.Main) Close();
        }

        public void Resume() => Close();

        public void ShowSave() { Page = MenuPage.Save; Message = null; }
        public void ShowLoad() { Page = MenuPage.Load; Message = null; }
        /// <summary>The Load button of the start card: shows the slot list on the card.</summary>
        public void ShowLoadFromStart() { Page = MenuPage.Load; Message = null; _loadFromStart = true; }
        private bool _loadFromStart;

        /// <summary>Saves the running game into a slot (1 to 3). Returns false (with a message) when the file cannot be written.</summary>
        public bool Save(int slot)
        {
            try
            {
                Boot.SaveToSlot(slot);
                Say("Saved to slot " + slot + ".", false);
                return true;
            }
            catch (System.Exception e) when (e is System.IO.IOException || e is System.UnauthorizedAccessException)
            {
                Say("Could not save: " + e.Message, true);
                return false;
            }
        }

        /// <summary>Loads a slot: reloads the scene into the saved game. A damaged or empty slot is refused and the game goes on.</summary>
        public bool LoadSlot(int slot)
        {
            try { Boot.LoadSlot(slot); return true; }
            catch (SaveException e) { Say(e.Message, true); return false; }
        }

        /// <summary>A new island: another seed, same mode (play or watch).</summary>
        public void NewIsland() { Close(); Boot.Restart(true); }

        /// <summary>Restarts this island with no human: the six AIs play each other.</summary>
        public void WatchAis() { Close(); GodBootstrap.StartNew(Boot.Seed, true); }

        /// <summary>Restarts this island with you as Amber (from watch mode).</summary>
        public void PlayThisIsland() { Close(); GodBootstrap.StartNew(Boot.Seed, false); }

        /// <summary>Start card, Play: you are Amber on the seed in the field.</summary>
        public void StartPlay() => Begin(false);

        /// <summary>Start card, Watch: six AIs on the seed in the field.</summary>
        public void StartWatch() => Begin(true);

        private void Begin(bool watch)
        {
            ulong seed = ParsedSeed();
            bool sameGame = seed == Boot.Seed && watch == Boot.Hud.Watching;
            Close();
            if (sameGame) { Boot.StartAi(); Boot.Running = true; }
            else GodBootstrap.StartNew(seed, watch);
        }

        /// <summary>The seed in the field, or the current seed when the field holds no number.</summary>
        public ulong ParsedSeed() => ulong.TryParse(SeedText?.Trim(), out var s) && s > 0 ? s : Boot.Seed;

        /// <summary>The dice button: a random seed in the field.</summary>
        public void RollSeed() { SeedText = GodBootstrap.RandomSeed().ToString(); }

        public void Quit()
        {
            if (QuitHandler != null) { QuitHandler(); return; }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Say(string text, bool error) { Message = text; MessageIsError = error; }

        // ---------------------------------------------------------------- drawing

        private void OnGUI()
        {
            if (Boot == null || Page == MenuPage.None) return;
            GUI.depth = -10;
            var st = HudStyle.Get();
            if (_field == null) MakeStyles(st);
            float s = Boot.Hud != null ? Boot.Hud.Scale : 1f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float vw = Screen.width / s, vh = Screen.height / s;
            var full = new Rect(0, 0, vw, vh);
            if (Event.current.type == EventType.Repaint) GUI.DrawTexture(full, st.Dim);
            GUI.Button(full, GUIContent.none, GUIStyle.none); // eats clicks meant for the HUD below

            float w = Page == MenuPage.Start ? 380f : Page == MenuPage.Main ? 300f : 460f;
            float h = _rect.height > 1f ? _rect.height : 200f;
            var r = new Rect((vw - w) * 0.5f, Mathf.Max(10f, (vh - h) * 0.5f), w, 0f);
            _rect = GUILayout.Window(WinId, r, id => Card(st), GUIContent.none, st.Window, GUILayout.Width(w));
            GUI.BringWindowToFront(WinId);
        }

        private void MakeStyles(HudStyle st)
        {
            _title = new GUIStyle(st.Title) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            _slotHead = new GUIStyle(st.Label) { fontSize = 13, fontStyle = FontStyle.Bold, wordWrap = false };
            _msg = new GUIStyle(st.Label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            _field = new GUIStyle(st.Label)
            {
                fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip,
                border = new RectOffset(14, 14, 14, 14), padding = new RectOffset(10, 10, 5, 6),
                normal = { background = st.BtnNormal, textColor = HudStyle.Ink },
                focused = { background = st.BtnHover, textColor = HudStyle.Ink },
                hover = { background = st.BtnHover, textColor = HudStyle.Ink },
            };
        }

        private void Card(HudStyle st)
        {
            switch (Page)
            {
                case MenuPage.Start: StartCard(st); break;
                case MenuPage.Main: MainCard(st); break;
                default: SlotCard(st, Page == MenuPage.Save); break;
            }
            if (!string.IsNullOrEmpty(Message))
            {
                GUILayout.Space(6f);
                var old = _msg.normal.textColor;
                _msg.normal.textColor = MessageIsError ? HudStyle.Bad : HudStyle.Good;
                GUILayout.Label(Message, _msg);
                _msg.normal.textColor = old;
            }
        }

        private void StartCard(HudStyle st)
        {
            GUILayout.Label("God of the Island", _title);
            GUILayout.Label("Six factions, one island. First to 10 points wins.", st.Centered);
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Island seed", st.Row, GUILayout.Width(90f), GUILayout.Height(30f));
            string t = GUILayout.TextField(SeedText ?? "", 10, _field, GUILayout.Height(30f));
            if (t != SeedText) SeedText = FilterDigits(t);
            var dice = GUILayoutUtility.GetRect(34f, 30f, GUILayout.Width(34f), GUILayout.Height(30f));
            if (GUI.Button(new Rect(dice.x, dice.y, dice.width, dice.height), new GUIContent("", "A random island"), st.Btn)) RollSeed();
            if (Event.current.type == EventType.Repaint) DrawDie(dice);
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play as Amber", st.BtnSel, GUILayout.Height(40f))) StartPlay();
            if (GUILayout.Button("Watch 6 AIs", st.Btn, GUILayout.Height(40f))) StartWatch();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Load a game", st.Btn, GUILayout.Height(30f))) ShowLoadFromStart();
        }

        private static string FilterDigits(string t)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in t) if (c >= '0' && c <= '9') sb.Append(c);
            return sb.ToString();
        }

        private static void DrawDie(Rect r)
        {
            float pip = 5f, cx = r.center.x, cy = r.center.y, d = 7f;
            var ink = HudStyle.Ink;
            foreach (var o in new[] { new Vector2(-d, -d), new Vector2(d, -d), new Vector2(0, 0), new Vector2(-d, d), new Vector2(d, d) })
                GUI.DrawTexture(new Rect(cx + o.x - pip * 0.5f, cy + o.y - pip * 0.5f, pip, pip), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, ink, Vector4.zero, new Vector4(pip, pip, pip, pip));
        }

        private void MainCard(HudStyle st)
        {
            GUILayout.Label("Paused", _title);
            GUILayout.Space(8f);
            if (GUILayout.Button("Resume", st.BtnSel, GUILayout.Height(34f))) Resume();
            if (GUILayout.Button("Save", st.Btn, GUILayout.Height(30f))) ShowSave();
            if (GUILayout.Button("Load", st.Btn, GUILayout.Height(30f))) ShowLoad();
            GUILayout.Space(4f);
            if (GUILayout.Button("New island", st.Btn, GUILayout.Height(30f))) NewIsland();
            if (Boot.Hud != null && Boot.Hud.Watching)
            {
                if (GUILayout.Button("Play this island", st.Btn, GUILayout.Height(30f))) PlayThisIsland();
            }
            else if (GUILayout.Button("Watch AIs", st.Btn, GUILayout.Height(30f))) WatchAis();
            GUILayout.Space(4f);
            if (GUILayout.Button("Quit", st.Btn, GUILayout.Height(30f))) Quit();
        }

        private void SlotCard(HudStyle st, bool saving)
        {
            GUILayout.Label(saving ? "Save game" : "Load game", _title);
            GUILayout.Space(6f);
            for (int slot = 1; slot <= SaveStore.Slots; slot++)
            {
                var h = SaveStore.ReadHeader(slot);
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Label("Slot " + slot, _slotHead);
                GUILayout.Label(h == null ? (SaveStore.Exists(slot) ? "Damaged file" : "Empty") : SaveStore.Describe(h), st.Small);
                if (h != null && !string.IsNullOrEmpty(h.VpLine)) GUILayout.Label(h.VpLine, st.Small);
                GUILayout.EndVertical();
                bool enabled = saving || h != null;
                GUI.enabled = enabled;
                if (GUILayout.Button(saving ? "Save" : "Load", saving ? st.BtnSel : st.Btn, GUILayout.Width(74f), GUILayout.Height(36f)))
                {
                    if (saving) Save(slot); else LoadSlot(slot);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);
            }
            if (GUILayout.Button("Back", st.Btn, GUILayout.Height(28f))) Back();
        }
    }
}
