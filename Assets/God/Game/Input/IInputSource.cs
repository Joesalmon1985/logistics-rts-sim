using UnityEngine;

namespace God.Game
{
    /// <summary>A device (mouse and keyboard, later touch or gamepad, or a script) that yields one <see cref="PlayerInputFrame"/> per frame.</summary>
    public interface IInputSource
    {
        PlayerInputFrame Poll(float unscaledDeltaTime);
    }

    /// <summary>
    /// Mouse and keyboard. The only class of the new controls that reads <c>UnityEngine.Input</c>.
    /// Keys: Space pause, + and - speed, . step (Shift+. ten ticks), ] accelerated, Home whole island, H household or home, F follow,
    /// Z zoom to the person, Esc, Tab and Shift+Tab roster, WASD or arrows move, Q and E rotate, R and T (or Page Up and Down) tilt.
    /// </summary>
    public sealed class UnityInputSource : IInputSource
    {
        /// <summary>Tests and screenshots: a cursor used instead of the mouse.</summary>
        public Vector2? DebugCursor;
        /// <summary>True while a text field has the keyboard: the keys are not read.</summary>
        public System.Func<bool> TextFocus;

        private static ButtonState Btn(int b) => new ButtonState { Held = Input.GetMouseButton(b), Pressed = Input.GetMouseButtonDown(b), Released = Input.GetMouseButtonUp(b) };

        public PlayerInputFrame Poll(float dt)
        {
            var f = new PlayerInputFrame
            {
                DeltaTime = dt,
                Pointer = DebugCursor ?? (Vector2)Input.mousePosition,
                Wheel = Input.mouseScrollDelta.y,
                Left = Btn(0), Right = Btn(1), Middle = Btn(2),
                Shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                Ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                Alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
            };
            if (TextFocus != null && TextFocus()) return f;
            float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float z = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            f.Move = new Vector2(x, z);
            f.Rotate = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            f.Tilt = (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp) ? 1f : 0f) - (Input.GetKey(KeyCode.T) || Input.GetKey(KeyCode.PageDown) ? 1f : 0f);
            var k = PlayerKeys.None;
            if (Input.GetKeyDown(KeyCode.Space)) k |= PlayerKeys.TogglePause;
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus)) k |= PlayerKeys.SpeedUp;
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) k |= PlayerKeys.SpeedDown;
            if (Input.GetKeyDown(KeyCode.Period)) k |= PlayerKeys.StepTick;
            if (Input.GetKeyDown(KeyCode.RightBracket)) k |= PlayerKeys.ToggleAccelerated;
            if (Input.GetKeyDown(KeyCode.Home)) k |= PlayerKeys.Home;
            if (Input.GetKeyDown(KeyCode.H)) k |= PlayerKeys.HouseholdOrHome;
            if (Input.GetKeyDown(KeyCode.F)) k |= PlayerKeys.Follow;
            if (Input.GetKeyDown(KeyCode.Z)) k |= PlayerKeys.ZoomToIndividual;
            if (Input.GetKeyDown(KeyCode.Escape)) k |= PlayerKeys.Escape;
            if (Input.GetKeyDown(KeyCode.Tab)) k |= f.Shift ? PlayerKeys.RosterPrev : PlayerKeys.RosterNext;
            f.Keys = k;
            return f;
        }
    }

    /// <summary>
    /// A scripted device for tests and the AI-god harness (CC-1): set the pointer, press buttons and keys, and every
    /// <see cref="Poll"/> hands out the state with the one-frame edges (pressed, released, key presses) cleared after it.
    /// </summary>
    public sealed class ScriptedInput : IInputSource
    {
        public Vector2 Pointer;
        public bool Shift, Ctrl, Alt;
        public Vector2 Move;
        public float Rotate, Tilt;
        private bool _left, _right, _middle;
        private bool _leftDown, _leftUp, _rightDown, _rightUp, _middleDown, _middleUp;
        private float _wheel;
        private PlayerKeys _keys;

        public void LeftDown() { _left = true; _leftDown = true; }
        public void LeftUp() { _left = false; _leftUp = true; }
        public void RightDown() { _right = true; _rightDown = true; }
        public void RightUp() { _right = false; _rightUp = true; }
        public void MiddleDown() { _middle = true; _middleDown = true; }
        public void MiddleUp() { _middle = false; _middleUp = true; }
        public void Scroll(float ticks) { _wheel += ticks; }
        public void Press(PlayerKeys k) { _keys |= k; }

        public PlayerInputFrame Poll(float dt)
        {
            var f = new PlayerInputFrame
            {
                DeltaTime = dt, Pointer = Pointer, Wheel = _wheel, Shift = Shift, Ctrl = Ctrl, Alt = Alt, Move = Move, Rotate = Rotate, Tilt = Tilt, Keys = _keys,
                Left = new ButtonState { Held = _left, Pressed = _leftDown, Released = _leftUp },
                Right = new ButtonState { Held = _right, Pressed = _rightDown, Released = _rightUp },
                Middle = new ButtonState { Held = _middle, Pressed = _middleDown, Released = _middleUp },
            };
            _wheel = 0f; _keys = PlayerKeys.None;
            _leftDown = _leftUp = _rightDown = _rightUp = _middleDown = _middleUp = false;
            return f;
        }
    }
}
