using System;
using UnityEngine;

namespace God.Game
{
    /// <summary>Keys the player controls use, as flags that were pressed (went down) this frame. Device independent: a touch or gamepad adapter sets the same bits.</summary>
    [Flags]
    public enum PlayerKeys : uint
    {
        None = 0,
        TogglePause = 1 << 0,
        SpeedUp = 1 << 1,
        SpeedDown = 1 << 2,
        StepTick = 1 << 3,
        ToggleAccelerated = 1 << 4,
        Home = 1 << 5,
        HouseholdOrHome = 1 << 6,
        Follow = 1 << 7,
        ZoomToIndividual = 1 << 8,
        Escape = 1 << 9,
        RosterNext = 1 << 10,
        RosterPrev = 1 << 11,
    }

    /// <summary>State of one pointer button this frame.</summary>
    public struct ButtonState
    {
        public bool Held, Pressed, Released;
    }

    /// <summary>
    /// One frame of device input as plain data (CC-1): produced by an <see cref="IInputSource"/>, read only by
    /// <see cref="PlayerController"/>. Pointer positions are pixels with the origin bottom left.
    /// </summary>
    public struct PlayerInputFrame
    {
        /// <summary>Unscaled seconds since the last frame.</summary>
        public float DeltaTime;
        public Vector2 Pointer;
        public float Wheel;
        public ButtonState Left, Right, Middle;
        public bool Shift, Ctrl, Alt;
        /// <summary>WASD or arrows: x right, y forward (-1 to 1).</summary>
        public Vector2 Move;
        /// <summary>Q and E: -1 turns the view anticlockwise, +1 clockwise.</summary>
        public float Rotate;
        /// <summary>R and T, Page Up and Down: +1 tilts towards looking straight down.</summary>
        public float Tilt;
        public PlayerKeys Keys;
        public bool Has(PlayerKeys k) => (Keys & k) != 0;
    }
}
