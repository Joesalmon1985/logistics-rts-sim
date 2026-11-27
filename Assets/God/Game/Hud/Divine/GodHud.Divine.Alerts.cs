using System.Collections.Generic;
using UnityEngine;

namespace God.Game
{
    /// <summary>U2a: response chips and miracle notes, the aggregated alerts (replacing the toast feed in the god-only game) and the minimal distinct cues.</summary>
    public sealed partial class GodHud
    {
        private readonly HashSet<int> _dismissedAlerts = new HashSet<int>();

        /// <summary>The chips of the instructions sent, newest last.</summary>
        public IReadOnlyList<InstructionChip> Chips => Boot.Divine?.Feed.Chips ?? (IReadOnlyList<InstructionChip>)System.Array.Empty<InstructionChip>();

        /// <summary>The alert cards (aggregated), newest last.</summary>
        public IReadOnlyList<AlertCard> AlertCards => Boot.Divine?.Alerts.Cards ?? (IReadOnlyList<AlertCard>)System.Array.Empty<AlertCard>();

        public void DismissAlert(int id) { _dismissedAlerts.Add(id); }

        private void DrawChipsPanel(HudStyle st, HudFrame f)
        {
            var chips = Chips;
            var notes = Boot.Divine.Feed.Notes;
            int shownChips = Mathf.Min(3, chips.Count), shownNotes = Mathf.Min(2, notes.Count);
            bool refusal = !string.IsNullOrEmpty(RefusalText);
            int lines = shownChips + shownNotes + (refusal ? 1 : 0);
            if (lines == 0) return;
            float y = f.Height - 100f - 6f - lines * 24f;
            if (PopoverOpen || TemplateOpen) return;
            for (int i = chips.Count - shownChips; i < chips.Count; i++)
            {
                var c = chips[i];
                var col = c.Pending ? DivineCatalog.ColInspect : c.Kind == God.Sim.Divinity.InterventionKind.Suggestion ? DivineCatalog.ColSuggest : c.Kind == God.Sim.Divinity.InterventionKind.Command ? DivineCatalog.ColCommand : DivineCatalog.ColCompel;
                y = Chip(st, f, y, c.Text(), col);
            }
            for (int i = notes.Count - shownNotes; i < notes.Count; i++) y = Chip(st, f, y, "Miracle: " + notes[i].Text, DivineCatalog.ColHeal);
            if (refusal) y = Chip(st, f, y, RefusalText, DivineCatalog.ColSmite);
        }

        private float Chip(HudStyle st, HudFrame f, float y, string text, int colour)
        {
            var c = new GUIContent(text);
            float w = Mathf.Min(f.Width - 40f, _tiny.CalcSize(c).x + 44f);
            var r = new Rect((f.Width - w) * 0.5f, y, w, 22f);
            f.Block(r);
            GUI.Box(r, GUIContent.none, _pill);
            if (f.Repaint) { var old = GUI.color; GUI.color = Rgb(colour); GUI.DrawTexture(new Rect(r.x + 12f, r.y + 6f, 10f, 10f), Texture2D.whiteTexture); GUI.color = old; }
            GUI.Label(new Rect(r.x + 28f, r.y + 2f, r.width - 34f, 18f), c, _tiny);
            return y + 24f;
        }

        private void DrawAlertsPanel(HudStyle st, HudFrame f)
        {
            var cards = AlertCards;
            float y = f.StripHeight + 420f;
            long now = DivineTick;
            int tpd = Boot.Divine.TicksPerDay;
            for (int i = cards.Count - 1; i >= 0 && y < f.Height - 120f; i--)
            {
                var a = cards[i];
                if (_dismissedAlerts.Contains(a.Id)) continue;
                long ttl = a.Class == AlertClass.Critical ? 3L * tpd : a.Class == AlertClass.Digest ? 2L * tpd : tpd;
                if (now - a.Updated > ttl) continue;
                var c = new GUIContent(a.Text);
                float w = Mathf.Min(330f, _tiny.CalcSize(c).x + 50f);
                var r = new Rect(8f, y, w, 24f);
                f.Block(r);
                GUI.Box(r, GUIContent.none, _pill);
                int col = a.Class == AlertClass.Critical ? DivineCatalog.ColSmite : a.Class == AlertClass.Important ? DivineCatalog.ColCommand : DivineCatalog.ColInspect;
                if (f.Repaint) { var old = GUI.color; GUI.color = Rgb(col); GUI.DrawTexture(new Rect(r.x + 10f, r.y + 7f, 10f, 10f), Texture2D.whiteTexture); GUI.color = old; }
                GUI.Label(new Rect(r.x + 26f, r.y + 3f, r.width - 50f, 18f), c, _tiny);
                if (GUI.Button(new Rect(r.xMax - 22f, r.y + 3f, 18f, 18f), "x", st.Btn)) _dismissedAlerts.Add(a.Id);
                y += 27f;
            }
        }

        // ---------------------------------------------------------------- cues and the ghost (world, drawn with the overlay)

        private void DrawDivineCues(HudOverlay.Soft d)
        {
            float now = Time.unscaledTime;
            var cues = Boot.Divine.Cues;
            for (int i = 0; i < cues.Live.Count; i++)
            {
                var c = cues.Live[i];
                Vector3 p;
                if (c.Person != 0 && TryDrawnPoint(c.Person, out var w)) p = new Vector3(w.x, w.y - 1.1f, w.z);
                else if (c.Node >= 0 && c.Person == 0) p = NodePos(c.Node);
                else continue;
                float t = DivineCues.Age01(c, now), fade = 1f - t;
                var col = Rgb(DivineCues.Colour(c.Kind), 0.9f * fade);
                switch (c.Kind)
                {
                    case CueKind.Suggest: d.Ring(p + Vector3.up * (0.4f + 2.2f * t), 0.9f + 0.9f * t, 0.18f, col, 24); break;
                    case CueKind.Command: d.Ring(p + Vector3.up * 0.3f, 1.0f + 1.4f * t, 0.3f, col, 24); d.Ring(p + Vector3.up * 0.3f, 0.6f + 1.0f * t, 0.3f, col, 24); break;
                    case CueKind.Compel:
                        for (int k = 0; k < 4; k++)
                        {
                            float a = k * Mathf.PI * 0.5f + t * 0.6f;
                            var o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.1f;
                            d.Line(p + o, p + o + Vector3.up * 2.4f, 0.16f, col);
                        }
                        break;
                    case CueKind.Heal:
                        {
                            var q = p + Vector3.up * (1.2f + 1.8f * t);
                            d.Box(q, new Vector3(1.4f, 0.4f, 0.4f), col); d.Box(q, new Vector3(0.4f, 1.4f, 0.4f), col);
                            break;
                        }
                    case CueKind.Smite:
                        d.Line(p + Vector3.up * 14f + new Vector3(1.2f, 0f, 0f), p + Vector3.up * 7f - new Vector3(0.8f, 0f, 0f), 0.35f, col);
                        d.Line(p + Vector3.up * 7f - new Vector3(0.8f, 0f, 0f), p, 0.35f, col);
                        d.Disc(p, 1.5f + 2.5f * t, col, new Color(col.r, col.g, col.b, 0f), 16);
                        break;
                    case CueKind.Create:
                        d.Disc(p + Vector3.up * 0.2f, 0.8f + 1.4f * t, col, new Color(col.r, col.g, col.b, 0f), 14);
                        for (int k = 0; k < 4; k++) { float a = k * Mathf.PI * 0.5f; d.Line(p + Vector3.up * 0.3f, p + Vector3.up * (0.3f + 1.2f * t) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (0.8f + t), 0.14f, col); }
                        break;
                }
            }
            // the ghost of the armed power on the hovered target: green when the gate allows it, red when it refuses
            var g = Ghost;
            if (g.Active)
            {
                Vector3 gp;
                if (g.Person != 0 && TryDrawnPoint(g.Person, out var gw)) gp = new Vector3(gw.x, gw.y - 1.1f, gw.z);
                else if (g.Node >= 0) gp = NodePos(g.Node);
                else return;
                var gc = g.Valid ? (g.Affordable ? new Color(0.45f, 0.9f, 0.45f, 0.9f) : new Color(0.95f, 0.6f, 0.2f, 0.9f)) : new Color(0.92f, 0.3f, 0.25f, 0.9f);
                d.Ring(gp, 1.6f, 0.3f, gc, 24);
            }
        }
    }
}
