namespace God.Game
{
    /// <summary>
    /// The default numbers of the player controls (CAMERA_AND_CONTROLS_DESIGN CC-7). Plain data: a test or a later options
    /// screen may change a copy. View side only; the sim never sees any of it.
    /// </summary>
    public sealed class ControlTuning
    {
        // ---- camera distances (metres from the focus point): the four zoom bands (design section 4)
        public float MinDistance = 4f;
        public float IndividualDistance = 14f;
        public float SettlementDistance = 90f;
        public float RegionDistance = 220f;

        // ---- orbit
        public float MinPitch = 25f;
        public float MaxPitch = 80f;
        public float HomePitch = 50f;
        public float OrbitDegreesPerPixel = 0.25f;
        public float KeyRotateDegreesPerSecond = 90f;
        public float KeyTiltDegreesPerSecond = 45f;
        public float WheelTiltDegrees = 4f;

        // ---- feel
        /// <summary>Wheel step: each tick moves the camera this fraction of the way to the point under the cursor.</summary>
        public float ZoomStep = 0.14f;
        /// <summary>How fast the camera eases to its target pose (1/s, on unscaled time so it works while paused).</summary>
        public float Stiffness = 14f;
        /// <summary>Time constant of the follow camera's focus smoothing (seconds).</summary>
        public float FollowSmoothing = 0.25f;
        /// <summary>Keyboard pan speed: world units per second per metre of camera distance.</summary>
        public float PanSpeedPerDistance = 1.1f;

        // ---- pointer
        public float DragSlopPixels = 6f;
        public float DoubleClickSeconds = 0.32f;
        public float BrushDefaultMetres = 20f;
        public float BrushMinMetres = 6f;
        public float BrushMaxMetres = 120f;
        public float BrushWheelFactor = 1.12f;

        // ---- time
        /// <summary>Accelerated mode: milliseconds of sim work per frame, and the highest rate (times real time).</summary>
        public float AcceleratedBudgetMs = 8f;
        public float AcceleratedMaxSpeed = 50f;
    }
}
