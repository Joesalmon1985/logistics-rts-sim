using UnityEngine;

namespace God.Game
{
    public enum PointerGesture { None, Click, DoubleClick, BoxSelect, BrushBegin, BrushMove, BrushEnd, SecondaryClick }

    public enum GrabPhase { None, Begin, Move, End }

    /// <summary>
    /// What the player means this frame, independent of the device (CC-1, D5.23): one plain struct. The camera, the selection,
    /// the time controls and (through <see cref="PlayerCommandBuilder"/>) the commands are driven from it. A touch or gamepad
    /// adapter would fill the same fields.
    /// </summary>
    public struct PlayerIntent
    {
        public float Dt;
        // ---- camera
        /// <summary>Keyboard pan in the camera's own frame (x right, y forward).</summary>
        public Vector2 Pan;
        /// <summary>Key rotate and tilt, -1 to 1 (scaled by Dt when applied).</summary>
        public float Rotate, Tilt;
        public float ZoomTicks;
        public Vector2 ZoomAt;
        /// <summary>Alt plus wheel ticks: tilt.</summary>
        public float TiltWheelTicks;
        public GrabPhase Grab;
        public Vector2 GrabAt;
        /// <summary>Middle-drag movement in pixels this frame: orbit.</summary>
        public Vector2 OrbitPixels;
        public bool Home, ToggleFollow, ZoomToIndividual, HouseholdOrHome;
        // ---- selection
        public PointerGesture Gesture;
        public Vector2 GesturePos;
        /// <summary>Box select: the rectangle in pixels (origin bottom left).</summary>
        public Rect Box;
        /// <summary>Shift or Ctrl held: add to or toggle in the selection.</summary>
        public bool Additive;
        /// <summary>Wheel ticks while the area brush is down: radius.</summary>
        public float BrushWheelTicks;
        public bool Escape, RosterNext, RosterPrev;
        // ---- time (UI only, never commands)
        public bool TogglePause, ToggleAccelerated;
        public int SpeedStep, StepTicks;
    }
}
