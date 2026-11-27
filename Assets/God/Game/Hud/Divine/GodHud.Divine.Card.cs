using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>U2a: the person card for anyone (the divine view, not the faction view) and its Divine tab (issued, perceived, understood, answered), each row layer-tagged.</summary>
    public sealed partial class GodHud
    {
        /// <summary>
        /// The card of any living person in the god-only game: the divine view says who exists, the card is built from that person's own
        /// faction's records (as in watch mode), so a person of another community has a card too.
        /// </summary>
        private NpcInspection DivineInspectionOf(int id)
        {
            var v = Boot.Divine?.View;
            if (v == null || !v.TryPerson(id, out var p) || !p.Alive) return null;
            return Boot.ViewOf(p.Faction)?.Population.Inspect(id) ?? View.Population.Inspect(id);
        }

        /// <summary>The rows of the Divine tab for a person, each with its knowledge layer (U2 risk row: a row without a layer is a bug).</summary>
        public System.Collections.Generic.List<DivineRow> DivineRowsOf(NpcInspection c)
        {
            var rows = new System.Collections.Generic.List<DivineRow>();
            if (c == null) return rows;
            if (c.Divine != null)
                foreach (var line in c.Divine.Lines) rows.Add(new DivineRow(c.Divine.Layer, "Understood", line));
            if (Boot.Divine != null) foreach (var r in Boot.Divine.Feed.RowsOf(c.Id)) rows.Add(r);
            return rows;
        }

        private void DrawDivineTab(HudStyle st, NpcInspection c)
        {
            GUILayout.Label("What the gods said to this person, what they heard, what they understood and what they did.", st.Small);
            var rows = DivineRowsOf(c);
            if (rows.Count == 0) GUILayout.Label("Nothing issued to them yet. Choose Suggest or Command on the toolbar and click them.", st.Small);
            foreach (var r in rows)
                GUILayout.Label("[" + DivineRow.LayerLabel(r.Layer) + "] " + r.Head + ": " + r.Text, st.Small);
            Section(st, "Why they act (the Divine term)");
            bool any = false;
            foreach (var h in c.Why) if (h.Text != null && h.Text.IndexOf("divine", System.StringComparison.OrdinalIgnoreCase) >= 0) { GUILayout.Label("  " + h.Text, st.Small); any = true; }
            if (!any) GUILayout.Label("No divine term in their last decision.", st.Small);
        }
    }
}
