using System.Collections.Generic;
using System.Text;
using God.Sim;
using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// M3: the NPC inspector card (POPULATION 6, NPC_LEARNING 6, COMMUNICATION 6). Click a person in the world: the card
    /// shows that person's own view, read through the faction knowledge view (never the state): drives and body, what it
    /// carries, its home, what it is doing, its last decision with the term breakdown (instinct, learned, personality,
    /// cost, explore) and the associations that carried it, what it attends to, its strongest beliefs, and its learned
    /// habits. Only the viewer's own people have a card; in watch mode (an observation game) each faction's own records
    /// answer for its people.
    /// </summary>
    public sealed partial class GodHud
    {
        private const int WinPerson = 107;
        private readonly List<NpcSighting> _sightings = new List<NpcSighting>();
        private Rect _personRect;
        private Vector2 _personScroll;
        /// <summary>F0.15: the card's tab: 0 mind (M3), 1 household (M4), 2 relations (M5).</summary>
        private int _personTab;

        /// <summary>The tab shown (tests): an index into the registered tabs (0 mind, 1 household, 2 relations, then the rest by order).</summary>
        public int PersonTab { get => _personTab; set => _personTab = Mathf.Clamp(value, 0, Mathf.Max(0, _personTabs.Count - 1)); }

        /// <summary>The person whose card is open, 0 for none.</summary>
        public int SelectedNpc { get; private set; }

        /// <summary>The card's content as last drawn (tests).</summary>
        public NpcInspection PersonCard { get; private set; }

        public void SelectNpc(int id)
        {
            ClearSelection(); ClearEnvoy();
            SelectedNpc = id;
            PersonCard = InspectionOf(id);
            // U1a: the selection model is the truth, the card follows it; a card opened by hand selects that person
            var sel = Boot.Selection;
            if (sel != null && id != 0 && !((sel.HasPersons || sel.Kind == SelectionKind.Household) && sel.Primary == id)) sel.SetPerson(id);
        }

        public void ClearNpc()
        {
            SelectedNpc = 0; PersonCard = null; TypingText = false;
            var sel = Boot.Selection;
            if (sel != null && (sel.HasPersons || sel.Kind == SelectionKind.Household)) sel.Clear();
        }

        /// <summary>The view that has a card for this person: the viewer's own, or in watch mode the person's own faction's records.</summary>
        private NpcInspection InspectionOf(int id)
        {
            var seen = View.Population.See(id);
            if (seen == null) return null;
            var card = View.Population.Inspect(id);
            if (card == null && Watching) card = Boot.ViewOf(seen.Value.Faction)?.Population.Inspect(id);
            return card;
        }

        /// <summary>The person drawn nearest the cursor (judged on screen), or 0.</summary>
        public int PickNpc(Vector2 mouse)
        {
            if (!View.Population.Exists) return 0;
            View.Population.Sightings(_sightings);
            var g = View.Map.Grid;
            int best = 0; float bestD = float.MaxValue;
            foreach (var s in _sightings)
            {
                Vector3 p = Boot.People != null && Boot.People.TryPosition(s.Id, out var fp) ? fp + Vector3.up * 1.2f : ModelKit.Node(g, s.Node, 1.2f);
                var sp = Cam.WorldToScreenPoint(p);
                if (sp.z <= 0f) continue;
                float rad = Screen.height * 0.025f;
                float d = Vector2.Distance(mouse, new Vector2(sp.x, sp.y));
                if (d < rad && d < bestD) { bestD = d; best = s.Id; }
            }
            return best;
        }

        private void DrawPeople(HudStyle st, float vw, float vh, float stripH, bool repaint)
        {
            if (SelectedNpc == 0) return;
            PersonCard = InspectionOf(SelectedNpc);
            if (PersonCard == null || !PersonCard.Alive) { ClearNpc(); return; }
            var r = new Rect(vw - 432f, stripH + 8f, 420f, Mathf.Min(vh - stripH - 24f, 640f));
            _personRect = GUILayout.Window(WinPerson, r, id => PersonPanel(st, r.height), GUIContent.none, st.Window, GUILayout.Width(420f), GUILayout.Height(r.height));
            if (repaint) _ui.Add(_personRect);
        }

        private void PersonPanel(HudStyle st, float height)
        {
            var c = PersonCard;
            GUILayout.BeginHorizontal();
            GUILayout.Label(FactionInfo.Name(c.Faction) + " person #" + c.Id, st.Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("x", st.Btn, GUILayout.Width(28f))) { ClearNpc(); GUILayout.EndHorizontal(); return; }
            GUILayout.EndHorizontal();
            if (Boot.GodOnly) DrawPersonNameRow(st, c);
            var tabs = VisiblePersonTabs(c);
            if (_personTab >= tabs.Count) _personTab = 0;
            GUILayout.BeginHorizontal();
            for (int t = 0; t < tabs.Count; t++)
                if (GUILayout.Button(tabs[t].Title, t == _personTab ? st.BtnSel : st.Btn)) _personTab = t;
            GUILayout.EndHorizontal();
            // vertical scrolling only: the content is laid out to the card's width (wrapped labels, flexible drive bars)
            _personScroll = GUILayout.BeginScrollView(_personScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(height - (Boot.GodOnly ? 120f : 88f)));
            tabs[_personTab].Draw(st, c);
            GUILayout.EndScrollView();
        }

        private void DrawMindTab(HudStyle st, NpcInspection c)
        {
            GUILayout.Label((c.Founder ? "Founder, age " : "Age ") + c.HudAge, st.Small);
            GUILayout.Label("Doing: " + (c.Doing.Length > 0 ? c.Doing : "nothing"), st.Label);
            GUILayout.Label("Health " + c.Health / 10 + "%   nutrition " + c.Nutrition / 10 + "%" + (c.Injury > 0 ? "   injury " + c.Injury / 10 + "%" : "")
                + "   " + (c.HomeHut != 0 ? "has a bed" : c.SiteHut != 0 ? "building a hut" : "no bed"), st.Small);
            Section(st, "Drives (how it feels)");
            string[] names = { "Hunger", "Tiredness", "Cold", "Fear", "Pain" };
            for (int d = 0; d < c.Drives.Length; d++) DriveBar(st, names[d], c.Drives[d]);
            Section(st, "Carrying");
            GUILayout.Label(c.Carry.Count == 0 ? "nothing" : string.Join(", ", c.Carry), st.Small);
            Section(st, "Last decision and why");
            GUILayout.Label(c.LastDecision.Length > 0 ? c.LastDecision + " (of " + c.Candidates + " options)" : "none yet", st.Label);
            GUILayout.Label("need " + Signed(c.Need) + "   instinct " + Signed(c.Instinct - c.Need) + "   learned " + Signed(c.Learned) + "   personality " + Signed(c.Personality)
                + "   cost " + Signed(-c.Cost) + "   explore " + Signed(c.Explore) + "   = " + c.Total, st.Small);
            if (c.LearnedChangedWinner) GUILayout.Label("What it has learned changed this choice.", st.Small);
            if (c.ExploreWon) GUILayout.Label("Curiosity tipped this choice (exploration).", st.Small);
            if (c.RunnerUp.Length > 0) GUILayout.Label("Runner-up: " + c.RunnerUp, st.Small);
            foreach (var h in c.Why) GUILayout.Label("  because: " + h.Text, st.Small);
            Section(st, "Options it weighed");
            if (c.Opportunities.Count == 0) GUILayout.Label("none yet", st.Small);
            foreach (var o in c.Opportunities) GUILayout.Label((o.Chosen ? "> " : "  ") + o.Breakdown, st.Small);
            Section(st, "Recent experiences");
            if (c.Experiences.Count == 0) GUILayout.Label("none yet", st.Small);
            foreach (var e in c.Experiences) GUILayout.Label("  " + e.Text, st.Small);
            Section(st, "Attending to");
            GUILayout.Label(c.Attending.Count == 0 ? "nothing in view" : string.Join(", ", c.Attending), st.Small);
            Section(st, "Believes");
            if (c.Beliefs.Count == 0) GUILayout.Label("nothing yet", st.Small);
            foreach (var b in c.Beliefs) GUILayout.Label("  " + b, st.Small);
            Section(st, "Habits learned (strongest)");
            if (c.HabitsPositive.Count + c.HabitsNegative.Count == 0) GUILayout.Label("none yet", st.Small);
            foreach (var h in c.HabitsPositive) GUILayout.Label("  + " + h.Text, st.Small);
            foreach (var h in c.HabitsNegative) GUILayout.Label("  - " + h.Text, st.Small);
            GUILayout.Label(c.Entries + " associations, " + c.Conjunctions + " learned combinations; learning " + c.Mode
                + "; learning changed " + c.LearnedSharePermille / 10 + "% of today's choices", st.Small);
        }

        private static string Signed(int v) => v >= 0 ? "+" + v : v.ToString();

        private static void Section(HudStyle st, string title)
        {
            GUILayout.Space(4f);
            GUILayout.Label(title, st.Label);
        }

        private static void DriveBar(HudStyle st, string name, int permille)
        {
            // as wide as the card allows (a fixed 380 px rect was wider than the scroll view and showed a horizontal scrollbar)
            var r = GUILayoutUtility.GetRect(GUIContent.none, st.Small, GUILayout.Height(16f), GUILayout.ExpandWidth(true));
            GUI.Label(new Rect(r.x, r.y - 1f, 90f, 18f), name, st.Small);
            var bar = new Rect(r.x + 92f, r.y + 3f, Mathf.Max(60f, r.width - 92f - 56f), 10f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.15f), 0, 0);
            var fill = new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(permille / 1000f), bar.height);
            var col = permille < 300 ? HudStyle.Good : permille < 550 ? HudStyle.Gold : HudStyle.Bad;
            GUI.DrawTexture(fill, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, col, 0, 0);
            GUI.Label(new Rect(bar.xMax + 6f, r.y - 1f, 50f, 18f), (permille / 10) + "%", st.Small);
        }
    }
}
