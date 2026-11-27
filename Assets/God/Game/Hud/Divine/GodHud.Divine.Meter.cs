using God.Sim.Divinity;
using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>U2a: the Mythic meter (balance over max, the daily regeneration and time to full, a ghost for the armed power, the Unlimited and Disabled indicators).</summary>
    public sealed partial class GodHud
    {
        /// <summary>Whole and tenths of a Mythic from milli-Mythic ("50", "12.5").</summary>
        public static string MythicText(long milli)
        {
            long whole = milli / 1000, tenth = System.Math.Abs(milli % 1000) / 100;
            return tenth == 0 ? whole.ToString() : whole + "." + tenth;
        }

        /// <summary>The meter's words: balance, max and the per-day regeneration with the time to full (R12: regeneration is per sim day).</summary>
        public string MeterText()
        {
            if (!TryGod(out var g)) return "No god";
            switch (g.Mode)
            {
                case DivinityMode.Disabled: return "Divine power disabled";
                case DivinityMode.Unlimited: return "Mythic ∞  " + MythicText(g.Mythic) + " / " + MythicText(g.MythicMax) + "  (unlimited)";
            }
            string s = "Mythic " + MythicText(g.Mythic) + " / " + MythicText(g.MythicMax) + "   +" + MythicText(g.RegenPerDay) + " a day";
            if (g.Mythic >= g.MythicMax) return s + "   full";
            if (g.RegenPerDay <= 0) return s;
            double days = (g.MythicMax - g.Mythic) / (double)g.RegenPerDay;
            return s + "   full in " + days.ToString("0.0") + " days";
        }

        /// <summary>The estimated cost of the armed power for the selection (milli-Mythic), 0 when nothing is armed.</summary>
        public long ArmedCostMilli()
        {
            if (Armed == null || Boot.Divine == null) return 0;
            var sel = Boot.Selection;
            int n = sel != null && sel.HasPersons ? sel.Ids.Count : 1;
            return DivineCatalog.CostOf(Boot.Divine.Costs, Armed, n);
        }

        private void DrawMeterPanel(HudStyle st, HudFrame f)
        {
            var mode = MythicMode;
            bool have = TryGod(out var g);
            var r = new Rect(f.Width - 452f, 6f, 440f, f.StripHeight * 0.78f - 8f);
            f.Block(r);
            if (f.Repaint) GUI.Box(r, GUIContent.none, _pill);
            GUI.Label(new Rect(r.x + 10f, r.y + 3f, r.width - 20f, 20f), have ? g.Name : "", _bold);
            GUI.Label(new Rect(r.x + 10f, r.y + 22f, r.width - 20f, 20f), MeterText(), st.Small);
            var bar = new Rect(r.x + 12f, r.y + 46f, r.width - 24f, 14f);
            if (f.Repaint && have)
            {
                bool off = mode == DivinityMode.Disabled;
                GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 0f, 0.22f), Vector4.zero, new Vector4(3, 3, 3, 3));
                float max = Mathf.Max(1f, g.MythicMax);
                float cur = Mathf.Clamp01(g.Mythic / max);
                var fill = off ? new Color(0.5f, 0.5f, 0.5f, 0.6f) : mode == DivinityMode.Unlimited ? new Color(0.95f, 0.85f, 0.45f, 0.95f) : new Color(0.55f, 0.72f, 0.95f, 0.95f);
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * cur, bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill, Vector4.zero, new Vector4(3, 3, 3, 3));
                long cost = off ? 0 : ArmedCostMilli();
                if (cost > 0)
                {
                    // the ghost: the part the armed power would take, red when it cannot be paid (Unlimited waives it)
                    bool short_ = mode == DivinityMode.Standard && cost > g.Mythic;
                    float to = Mathf.Clamp01((g.Mythic - cost) / max);
                    var ghost = short_ ? new Color(0.85f, 0.2f, 0.15f, 0.8f) : new Color(1f, 0.82f, 0.3f, 0.65f);
                    float x0 = bar.x + bar.width * (short_ ? 0f : to);
                    GUI.DrawTexture(new Rect(x0, bar.y, bar.width * cur - (x0 - bar.x), bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, ghost, Vector4.zero, Vector4.zero);
                }
            }
            if (have && mode == DivinityMode.Unlimited) GUI.Label(new Rect(bar.xMax - 30f, bar.y - 6f, 28f, 24f), "∞", _bold);
            if (Armed != null && have && mode != DivinityMode.Disabled)
            {
                long cost = ArmedCostMilli();
                string t = Armed.Label + " about " + MythicText(cost) + (mode == DivinityMode.Unlimited ? " (waived)" : mode == DivinityMode.Standard && cost > g.Mythic ? " - not enough" : "");
                GUI.Label(new Rect(r.x + 10f, bar.yMax + 2f, r.width - 20f, 18f), t, st.Small);
            }
            else if (Boot.Divine.Feed.LastBalance >= 0)
            {
                var fd = Boot.Divine.Feed;
                GUI.Label(new Rect(r.x + 10f, bar.yMax + 2f, r.width - 20f, 18f), "Last spend " + MythicText(fd.LastNominal) + (fd.LastWaived ? " (waived)" : ""), st.Small);
            }
            // the mode buttons exist in a sandbox (SetMythicMode is sandbox only)
            if (Boot.Divine.Sandbox)
            {
                float bx = r.xMax - 10f - 3 * 64f;
                var names = new[] { "Normal", "Unlimited", "Disabled" };
                var modes = new[] { DivinityMode.Standard, DivinityMode.Unlimited, DivinityMode.Disabled };
                for (int i = 0; i < 3; i++)
                    if (GUI.Button(new Rect(bx + i * 64f, r.y + 2f, 62f, 18f), new GUIContent(i == 1 ? "Unltd" : names[i], "Mythic mode: " + names[i]), mode == modes[i] ? st.BtnSel : st.Btn)) SetMythicMode(modes[i]);
            }
        }
    }
}
