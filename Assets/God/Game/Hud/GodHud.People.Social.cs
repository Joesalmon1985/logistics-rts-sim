using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// The relations tab of the person card (OWNER: M5). Reads only the card (the knowledge view's copy): the people this NPC
    /// knows, as its own memory holds them (class, trust, familiarity, grudge, deals and gifts each way) and what it has
    /// learned about dealing with each (its learned values keyed on that person), then its open deals and the messages it
    /// last heard.
    /// </summary>
    public sealed partial class GodHud
    {
        private void DrawRelationsTab(HudStyle st, NpcInspection c)
        {
            var card = c.Relations;
            if (card == null || card.Rows.Count == 0 && card.Lines.Count == 0) { GUILayout.Label("Nothing known yet.", st.Small); return; }
            GUILayout.Label("Knows " + card.RelCount + " of at most " + card.RelCap + " people (most important first)", st.Small);
            foreach (var r in card.Rows)
            {
                GUILayout.Label("#" + r.Other + "  " + r.Class.ToLowerInvariant(), st.Label);
                GUILayout.Label("  trust " + Signed(r.Trust) + "   familiarity " + r.Familiarity + "   affection " + Signed(r.Affection) + "   grudge " + r.Grudge, st.Small);
                GUILayout.Label("  deals done " + r.DealsDone + ", cheated me " + r.Cheated + "; gifts from " + r.GiftsIn + ", to " + r.GiftsOut, st.Small);
                if (r.Learned.Count > 0)
                    GUILayout.Label("  learned about them: " + string.Join(", ", r.Learned) + (r.LearnedSum != 0 ? "  (long-term " + Signed(r.LearnedSum) + ")" : ""), st.Small);
            }
            if (card.Lines.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("Deals and messages", st.Label);
                foreach (var line in card.Lines) GUILayout.Label(line, st.Small);
            }
        }

    }
}
