using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>One line of the standings panel.</summary>
    public struct StandingRow
    {
        public Faction Faction;
        public int Vp, Hqs, Citadels;
        public bool Eliminated;
        /// <summary>The factions this one is at war with.</summary>
        public List<Faction> AtWarWith;
    }

    /// <summary>The standings panel (right, under the cloud) and the watch-mode jump buttons.</summary>
    public sealed partial class GodHud
    {
        /// <summary>Panel size in HUD units and the row height.</summary>
        private const float StandW = 262f, StandRowH = 21f;

        /// <summary>One row per faction in the game, in rank order (the sim's standings), with the war lines.</summary>
        public List<StandingRow> StandingRows()
        {
            var s = Sim.State;
            var rows = new List<StandingRow>();
            foreach (var st in FactionRules.Standings(s))
                rows.Add(new StandingRow
                {
                    Faction = st.Faction, Vp = st.VictoryPoints, Hqs = st.Hqs, Citadels = st.Citadels, Eliminated = st.Eliminated,
                    AtWarWith = s.Factions.Enemies(st.Faction),
                });
            return rows;
        }

        private void DrawStandings(float y, bool repaint)
        {
            var rows = StandingRows();
            var st = HudStyle.Get();
            float vw = Screen.width / _scale;
            float x = vw - StandW - 14f;
            var box = new Rect(x, y, StandW, 14f + StandRowH * rows.Count);
            GUI.Box(box, GUIContent.none, _pill);
            if (repaint) _ui.Add(box);
            var tip = GUI.tooltip;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                float ry = y + 7f + i * StandRowH;
                var old = GUI.color;
                if (row.Eliminated) GUI.color = new Color(1f, 1f, 1f, 0.38f);
                var col = FactionLook.Colour(row.Faction);
                if (repaint) Disc(new Rect(x + 12f, ry + 5f, 11f, 11f), col);
                string me = !Watching && row.Faction == Me ? "*" : "";
                GUI.Label(new Rect(x + 28f, ry, 62f, StandRowH), new GUIContent(FactionLook.Name(row.Faction) + me, RowTip(row)), row.Eliminated ? st.RowSmall : st.Row);
                if (repaint)
                {
                    // VP as ten pips against the goal.
                    for (int p = 0; p < FactionTuning.VictoryPoints; p++)
                    {
                        var pr = new Rect(x + 92f + p * 8.5f, ry + 7f, 6.5f, 6.5f);
                        if (p < row.Vp) Disc(pr, col); else Disc(pr, new Color(0.27f, 0.19f, 0.09f, 0.20f));
                    }
                    // HQs and Citadels: a small house and a small gold tower, each with its count.
                    var ink = HudStyle.Ink;
                    GUI.DrawTexture(new Rect(x + 182f, ry + 6f, 9f, 8f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, ink, Vector4.zero, new Vector4(2, 2, 2, 2));
                    GUI.DrawTexture(new Rect(x + 207f, ry + 4f, 8f, 12f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.88f, 0.62f, 0.10f), Vector4.zero, new Vector4(2, 2, 2, 2));
                }
                GUI.Label(new Rect(x + 193f, ry, 14f, StandRowH), row.Hqs.ToString(), st.RowSmall);
                GUI.Label(new Rect(x + 217f, ry, 14f, StandRowH), row.Citadels.ToString(), st.RowSmall);
                if (repaint && !row.Eliminated)
                    for (int w = 0; w < row.AtWarWith.Count; w++) WarIcon(new Vector2(x + 240f - (row.AtWarWith.Count - 1) * 5f + w * 10f, ry + 10.5f), FactionLook.Colour(row.AtWarWith[w]), row.AtWarWith.Count);
                GUI.color = old;
            }
            if (!string.IsNullOrEmpty(GUI.tooltip) && GUI.tooltip != tip) { /* drawn by DrawTooltip */ }
        }

        private static string RowTip(StandingRow r)
        {
            string s = FactionLook.Name(r.Faction) + ": " + r.Vp + " of " + FactionTuning.VictoryPoints + " points, " + r.Hqs + (r.Hqs == 1 ? " HQ" : " HQs")
                + (r.Citadels > 0 ? ", " + r.Citadels + (r.Citadels == 1 ? " Citadel" : " Citadels") : "");
            if (r.Eliminated) return s + ". Out of the game.";
            if (r.AtWarWith.Count == 0) return s + ". At peace.";
            return s + ". At war with " + string.Join(", ", r.AtWarWith.ConvertAll(FactionLook.Name)) + ".";
        }

        private static void Disc(Rect r, Color c) =>
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, new Vector4(r.width, r.width, r.width, r.width));

        /// <summary>Crossed red blades over a dot in the enemy's colour. Several wars shrink the icons.</summary>
        private static void WarIcon(Vector2 c, Color enemy, int count)
        {
            float k = count > 3 ? 0.8f : 1f;
            var red = new Color(0.82f, 0.18f, 0.14f);
            var m = GUI.matrix;
            foreach (float a in new[] { 45f, -45f })
            {
                // Rotate about the icon centre in HUD units (the matrix already carries the HUD scale).
                GUI.matrix = m * Matrix4x4.TRS(new Vector3(c.x, c.y, 0f), Quaternion.Euler(0f, 0f, a), Vector3.one) * Matrix4x4.TRS(new Vector3(-c.x, -c.y, 0f), Quaternion.identity, Vector3.one);
                GUI.DrawTexture(new Rect(c.x - 1.2f, c.y - 6.5f * k, 2.4f, 13f * k), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, red, 0f, 0f);
                GUI.matrix = m;
            }
            Disc(new Rect(c.x - 2.5f * k, c.y - 2.5f * k, 5f * k, 5f * k), enemy);
        }

        // ---------------------------------------------------------------- watch mode

        /// <summary>Six small buttons that fly the camera to a faction's HQ (also keys 1 to 6), and one for the next fight (Tab).</summary>
        private void DrawWatchJumps(HudStyle st, float x, float y, bool repaint)
        {
            var wc = Boot.WatchCam;
            int n = Sim.State.FactionCount;
            for (int i = 0; i < n; i++)
            {
                var f = (Faction)i;
                var r = new Rect(x + i * 40f, y + 4f, 36f, 38f);
                bool out_ = Sim.State.Factions.IsEliminated(f);
                GUI.enabled = !out_;
                if (GUI.Button(r, new GUIContent((i + 1).ToString(), "Fly to " + FactionLook.Name(f) + "'s HQ (key " + (i + 1) + ")"), st.Tile) && wc != null) wc.JumpToFaction(f);
                GUI.enabled = true;
                if (repaint) GUI.DrawTexture(new Rect(r.x + 7f, r.y + 5f, r.width - 14f, 13f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, FactionLook.Colour(f) * (out_ ? 0.4f : 1f), Vector4.zero, new Vector4(5, 5, 5, 5));
            }
            if (GUI.Button(new Rect(x + n * 40f + 4f, y + 8f, 50f, 30f), new GUIContent("Fight", "Fly to the latest capture or fight (Tab)"), st.Btn) && wc != null) wc.NextHotSpot();
        }
    }
}
