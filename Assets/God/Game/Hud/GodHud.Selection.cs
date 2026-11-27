using System.Collections.Generic;
using God.Sim;
using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// U1a: the view side of the selection. Box selection by drawn position, the household of a person (an id read from the
    /// card), the markers drawn under the selected, the rubber band, the roster chips, and the alias row of the people card.
    /// Reads only the faction knowledge view and the NPC layer's drawn positions.
    /// </summary>
    public sealed partial class GodHud
    {
        /// <summary>True while the alias text field has the keyboard (the player keys are then not read).</summary>
        public bool TypingText { get; private set; }

        /// <summary>The box being dragged, in pixels (origin bottom left), or null.</summary>
        public Rect? RubberBand;

        private string _aliasEdit = "";
        private int _aliasFor;

        /// <summary>Where a person is drawn (world, chest height), or false when unknown.</summary>
        public bool TryDrawnPoint(int id, out Vector3 world)
        {
            world = default;
            if (Boot.People != null && Boot.People.TryPosition(id, out var fp)) { world = fp + Vector3.up * 1.2f; return true; }
            var seen = View.Population.Exists ? View.Population.See(id) : null;
            if (seen == null) return false;
            world = ModelKit.Node(View.Map.Grid, seen.Value.Node, 1.2f);
            return true;
        }

        /// <summary>Screen position (pixels) of a drawn person; false when behind the camera or unknown.</summary>
        public bool DrawnScreenPoint(int id, out Vector2 screen)
        {
            screen = default;
            if (!TryDrawnPoint(id, out var w)) return false;
            var sp = Cam.WorldToScreenPoint(w);
            if (sp.z <= 0f) return false;
            screen = new Vector2(sp.x, sp.y);
            return true;
        }

        /// <summary>The ids of the people drawn inside a screen rectangle (pixels), ascending. The explicit hand-picked list of a box select.</summary>
        public void BoxSelectIds(Rect pixels, List<int> into)
        {
            into.Clear();
            if (!View.Population.Exists) return;
            View.Population.Sightings(_sightings);
            foreach (var s in _sightings)
                if (DrawnScreenPoint(s.Id, out var sp) && pixels.Contains(sp)) into.Add(s.Id);
        }

        /// <summary>The household id of a person (from the card the view gives for them), 0 for none.</summary>
        public int HouseholdOf(int npcId)
        {
            var card = View.Population.Exists && View.Population.See(npcId) != null ? InspectionOf(npcId) : null;
            return card?.Household == null ? 0 : card.Household.Household;
        }

        private void DrawSelectionMarks(HudOverlay.Soft d)
        {
            var sel = Boot.Selection;
            if (sel == null || sel.IsEmpty) return;
            var ring = new Color(1f, 0.86f, 0.40f, 0.95f);
            var mate = new Color(0.55f, 0.85f, 1f, 0.9f);
            switch (sel.Kind)
            {
                case SelectionKind.Person:
                case SelectionKind.Group:
                case SelectionKind.Household:
                    foreach (int id in sel.Kind == SelectionKind.Household ? (IReadOnlyList<int>)new[] { sel.Primary } : sel.Ids)
                    {
                        if (!TryDrawnPoint(id, out var w)) continue;
                        var p = new Vector3(w.x, w.y - 1.1f, w.z);
                        d.Ring(p, sel.Kind == SelectionKind.Household ? 2.0f : 1.3f, 0.3f, sel.Kind == SelectionKind.Household ? mate : ring, 24);
                    }
                    break;
                case SelectionKind.Area:
                    {
                        var g = View.Map.Grid;
                        var c = NodePos(sel.Area.CentreNode);
                        float r = sel.Area.RadiusSteps * (float)Lattice.Step;
                        d.Disc(c, r, new Color(0.55f, 0.85f, 1f, 0.16f), new Color(0.55f, 0.85f, 1f, 0.10f), 36);
                        d.Ring(c, r, 0.6f, mate, 48);
                        break;
                    }
            }
        }

        // ---------------------------------------------------------------- drawing

        private void DrawU1(HudStyle st, float vw, float vh, float stripH, bool repaint)
        {
            if (Boot.GodOnly)
            {
                if (RubberBand.HasValue && repaint)
                {
                    var b = RubberBand.Value;
                    var r = new Rect(b.xMin / _scale, (Screen.height - b.yMax) / _scale, b.width / _scale, b.height / _scale);
                    GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.55f, 0.85f, 1f, 0.16f), 0f, 0f);
                    GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1.5f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.55f, 0.85f, 1f, 0.9f), 0f, 0f);
                    GUI.DrawTexture(new Rect(r.x, r.yMax - 1.5f, r.width, 1.5f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.55f, 0.85f, 1f, 0.9f), 0f, 0f);
                    GUI.DrawTexture(new Rect(r.x, r.y, 1.5f, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.55f, 0.85f, 1f, 0.9f), 0f, 0f);
                    GUI.DrawTexture(new Rect(r.xMax - 1.5f, r.y, 1.5f, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.55f, 0.85f, 1f, 0.9f), 0f, 0f);
                }
                DrawSelectionLabel(st, vw, vh, repaint);
                DrawRoster(st, stripH, repaint);
            }
            DrawRegisteredPanels(st, vw, vh, stripH, repaint);
        }

        private void DrawSelectionLabel(HudStyle st, float vw, float vh, bool repaint)
        {
            var sel = Boot.Selection;
            if (sel == null || sel.IsEmpty || Boot.Ui == null) return;
            string text = sel.Describe();
            if (sel.Kind == SelectionKind.Person) text = Boot.Ui.NameOf(sel.Primary);
            if (PendingCommandCount > 0) text += "   (" + PendingCommandCount + " pending)";
            var c = new GUIContent(text);
            var size = _bold.CalcSize(c);
            var r = new Rect(vw * 0.5f - size.x * 0.5f - 14f, vh - 44f, size.x + 28f, size.y + 12f);
            GUI.Box(r, c, _pill);
            if (repaint) _ui.Add(r);
        }

        /// <summary>The chips along the left edge: favourites then followed people (at most twelve), click to jump.</summary>
        private void DrawRoster(HudStyle st, float stripH, bool repaint)
        {
            var ui = Boot.Ui; var player = Boot.Player;
            if (ui == null || player == null) return;
            var roster = ui.Roster();
            float y = stripH + 56f;
            for (int i = 0; i < roster.Count; i++)
            {
                int id = roster[i];
                bool alive = View.Population.See(id) != null;
                string mark = ui.IsFavourite(id) ? "* " : "> ";
                var r = new Rect(8f, y, 150f, 24f);
                var prev = GUI.enabled;
                GUI.enabled = prev && alive;
                if (GUI.Button(r, new GUIContent(mark + ui.NameOf(id), alive ? "Jump to them" : "No longer here"), Boot.Selection.Primary == id ? st.BtnSel : st.Btn)) player.JumpTo(id);
                GUI.enabled = prev;
                if (repaint) _ui.Add(r);
                y += 27f;
            }
        }

        /// <summary>The alias, favourite and follow row of the people card (god-only mode). Names never reach the sim.</summary>
        private void DrawPersonNameRow(HudStyle st, NpcInspection c)
        {
            var ui = Boot.Ui;
            if (ui == null) { TypingText = false; return; }
            if (_aliasFor != c.Id) { _aliasFor = c.Id; _aliasEdit = ui.HasAlias(c.Id) ? ui.NameOf(c.Id) : ""; }
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("aliasField");
            string typed = GUILayout.TextField(_aliasEdit, 40, GUILayout.Width(190f));
            TypingText = GUI.GetNameOfFocusedControl() == "aliasField";
            if (typed != _aliasEdit) { _aliasEdit = typed; ui.Rename(c.Id, typed); }
            GUILayout.Label(ui.HasAlias(c.Id) ? "(" + ui.TrueName(c.Id) + ")" : "name them", st.Small);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(ui.IsFavourite(c.Id) ? "* Fav" : "Fav", ui.IsFavourite(c.Id) ? st.BtnSel : st.Btn, GUILayout.Width(52f))) ui.ToggleFavourite(c.Id);
            if (GUILayout.Button(ui.IsFollowed(c.Id) ? "Watching" : "Watch", ui.IsFollowed(c.Id) ? st.BtnSel : st.Btn, GUILayout.Width(74f))) ui.ToggleFollow(c.Id);
            GUILayout.EndHorizontal();
        }
    }
}
