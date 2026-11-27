using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// The household tab of the person card (OWNER: M4): life stage, household and its members as this person believes them,
    /// the store, partnership and care, then the family view (its believed kin by generation). Reads only the card (the
    /// knowledge view's copy); changes nothing.
    /// </summary>
    public sealed partial class GodHud
    {
        private void DrawHouseholdTab(HudStyle st, NpcInspection c)
        {
            var card = c.Household;
            if (card == null || card.Lines.Count == 0) { GUILayout.Label("Nothing known yet.", st.Small); return; }
            foreach (var line in card.Lines) GUILayout.Label(line, st.Small);
            if (card.Family.Count == 0) return;
            GUILayout.Space(6f);
            GUILayout.Label("Family (as it believes)", st.Label);
            foreach (var line in card.Family) GUILayout.Label(line, st.Small);
        }
    }
}
