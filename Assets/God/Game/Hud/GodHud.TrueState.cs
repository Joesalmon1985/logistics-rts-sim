using System.Collections.Generic;
using God.Sim;
using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// The debug true-state overlay (M1, owner decision Q22): beside what the HUD's faction knows (its view), what is really
    /// there, with the gap. Only where the game allows it (developer mode, watch mode and other observation games; never
    /// a competitive game, which has no toggle at all). F8 or the small "True state" button switches it. View side only:
    /// it reads <see cref="ITrueStateOverlay"/>, which no policy receives, and never changes the sim.
    /// </summary>
    public sealed partial class GodHud
    {
        private const int WinTruth = 105;
        private Rect _truthRect;

        /// <summary>True when this game allows the overlay (the toggle exists).</summary>
        public bool TrueStateAvailable => Boot != null && Boot.TrueState != null;

        /// <summary>True while the overlay panel is shown.</summary>
        public bool TrueStateShown { get; private set; }

        /// <summary>Shows or hides the overlay; does nothing where it is not allowed.</summary>
        public void ToggleTrueState() { TrueStateShown = TrueStateAvailable && !TrueStateShown; }

        /// <summary>One row of the panel: a faction's figures as known (null: not known) and as they really are.</summary>
        public struct TruthRow
        {
            public Faction Faction;
            public int? KnownStock, KnownSoldiers, KnownBuildings;
            public int TrueStock, TrueSoldiers, TrueBuildings, TrueInTransit;
        }

        /// <summary>The panel's rows (for tests): own stock is known exactly; foreign stock is not known at all (Q21).</summary>
        public List<TruthRow> TruthRows()
        {
            var rows = new List<TruthRow>();
            var ov = Boot.TrueState;
            if (ov == null) return rows;
            var foreign = View.Foreign();
            for (int i = 0; i < View.FactionCount; i++)
            {
                var f = (Faction)i;
                var r = new TruthRow
                {
                    Faction = f, TrueStock = ov.TrueStockAll(f), TrueSoldiers = ov.TrueSoldiers(f), TrueBuildings = ov.TrueBuildings(f), TrueInTransit = ov.TrueWaresInTransit(f),
                };
                if (f == Me)
                {
                    int stock = 0; for (int w = 1; w < Wares.Count; w++) stock += View.Own.TotalStock((Ware)w);
                    int soldiers = 0; foreach (var u in View.Military.VisibleUnits) if (u.Faction == f) soldiers++;
                    int buildings = 0; foreach (var b in View.Map.Buildings) if (b.Faction == f) buildings++;
                    r.KnownStock = stock; r.KnownSoldiers = soldiers; r.KnownBuildings = buildings;
                }
                else
                    foreach (var k in foreign)
                    {
                        if (k.About != f) continue;
                        if (k.Fact == ForeignFact.VisibleSoldiers) r.KnownSoldiers = (int)k.Value;
                        if (k.Fact == ForeignFact.VisibleBuildings) r.KnownBuildings = (int)k.Value;
                    }
                rows.Add(r);
            }
            return rows;
        }

        private void HandleTrueStateKey()
        {
            if (Input.GetKeyDown(KeyCode.F8)) ToggleTrueState();
        }

        private void DrawTrueState(HudStyle st, float vw, float vh, bool repaint)
        {
            if (!TrueStateAvailable) return;
            var b = new Rect(14f, vh - 40f, 92f, 26f);
            if (GUI.Button(b, new GUIContent("True state", "Debug overlay (F8): what is really there, beside what your faction knows"), TrueStateShown ? st.BtnSel : st.Btn)) ToggleTrueState();
            if (repaint) _ui.Add(b);
            if (!TrueStateShown) return;
            float h = _truthRect.height > 1f ? _truthRect.height : 220f;
            var r = new Rect(14f, Mathf.Max(60f, vh - 48f - h), 380f, 0f);
            _truthRect = GUILayout.Window(WinTruth, r, id => TruthCard(st), GUIContent.none, st.Window, GUILayout.Width(380f));
            if (repaint) _ui.Add(_truthRect);
        }

        private static string Pair(int? known, int truth) => (known.HasValue ? known.Value.ToString() : "?") + " / " + truth + (known.HasValue && known.Value != truth ? "  (gap " + (truth - known.Value) + ")" : "");

        private void TruthCard(HudStyle st)
        {
            GUILayout.Label("True state (debug): known / true", st.Title);
            GUILayout.Label("Day " + View.Calendar.Label(View.AsOfTick) + ". Your faction knows its own stores exactly and other factions' only through what it sees.", st.Small);
            foreach (var row in TruthRows())
            {
                GUILayout.BeginHorizontal();
                Swatch(FactionLook.Colour(row.Faction));
                GUILayout.Label(FactionLook.Name(row.Faction) + (row.Faction == Me ? "*" : ""), st.Row, GUILayout.Width(58f));
                GUILayout.Label("stock " + Pair(row.KnownStock, row.TrueStock), st.Small, GUILayout.Width(120f));
                GUILayout.Label("soldiers " + Pair(row.KnownSoldiers, row.TrueSoldiers), st.Small, GUILayout.Width(90f));
                GUILayout.Label("carried " + row.TrueInTransit, st.Small);
                GUILayout.EndHorizontal();
            }
        }
    }
}
