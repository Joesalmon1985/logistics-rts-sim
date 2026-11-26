using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>The relations tab of the person card (F0.15 stub; OWNER: M5). Reads only the card (the knowledge view's copy).</summary>
    public sealed partial class GodHud
    {
        private void DrawRelationsTab(HudStyle st, NpcInspection c)
        {
            var card = c.Relations;
            if (card == null || card.Lines.Count == 0) { GUILayout.Label("Nothing known yet.", st.Small); return; }
            foreach (var line in card.Lines) GUILayout.Label(line, st.Small);
        }
    }
}
