using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// U1a time controls: pause, the speed set, step, accelerated. Speed, pause and step are UI inputs, never sim commands: a
    /// replay is just commands at ticks, which is exact at any speed (PLAYER_UI_DESIGN section 15). In god-only mode a command
    /// sent while paused is checked at once and stays pending until the next tick that runs (contracts 11.3); the stage 3 mode
    /// keeps its one-tick-on-command rule.
    /// </summary>
    public sealed partial class GodHud
    {
        /// <summary>The speeds of god-only mode.</summary>
        public static readonly float[] GodOnlySpeeds = { 1f, 2f, 5f, 10f };
        private static readonly float[] WatchSpeeds = { 1f, 2f, 4f, 8f, 16f };
        private static readonly float[] FactionSpeeds = { 1f, 2f, 4f };

        private float[] SpeedSet() => Boot.GodOnly ? GodOnlySpeeds : Watching ? WatchSpeeds : FactionSpeeds;

        /// <summary>Accelerated mode is on: the sim runs as fast as a per-frame time budget allows.</summary>
        public bool Accelerated => Boot.Accelerated;
        /// <summary>The rate accelerated mode achieved last frame (times real time).</summary>
        public float AchievedSpeed => Boot.AchievedSpeed;
        /// <summary>Commands accepted and waiting for the next tick that runs (pending chips are drawn from this).</summary>
        public int PendingCommandCount => Boot.PendingCommandCount;

        /// <summary>Steps the paused sim by n ticks (one, or ten with Shift). Pending commands apply on the first.</summary>
        public void StepTicks(int n) { Boot.StepTicks(n); }

        public void ToggleAccelerated()
        {
            Boot.Accelerated = !Boot.Accelerated;
            if (Boot.Accelerated) Boot.Running = true;
        }

        /// <summary>After the speed buttons: step, accelerated and the queued count (god-only mode).</summary>
        private void DrawTimeExtras(HudStyle st, ref float x, float y)
        {
            if (!Boot.GodOnly) return;
            if (GUI.Button(new Rect(x, y + 8f, 40f, 30f), new GUIContent("Step", ". one tick, Shift+. one second"), st.Btn)) StepTicks(Event.current.shift ? 10 : 1);
            x += 42f;
            string label = Accelerated ? ">> " + Mathf.RoundToInt(AchievedSpeed) + "x" : ">>";
            if (GUI.Button(new Rect(x, y + 8f, Accelerated ? 62f : 38f, 30f), new GUIContent(label, "Accelerated: as fast as the frame budget allows (]). The rate achieved is shown."), Accelerated ? st.BtnSel : st.Btn)) ToggleAccelerated();
            x += Accelerated ? 64f : 40f;
            if (Paused && PendingCommandCount > 0)
            {
                GUI.Label(new Rect(x, y + 14f, 110f, 20f), PendingCommandCount + " queued", _bold);
                x += 84f;
            }
        }
    }
}
