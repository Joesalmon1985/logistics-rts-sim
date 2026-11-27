using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// The god camera controls (P5): the mouse wheel zooms along the view ray under the cursor, right- or middle-drag
    /// grabs the ground and pans, WASD and the arrow keys pan. The tilt never changes. The view is clamped so the island
    /// stays in view: fully zoomed out it is the framed island, closer in it may slide a little over the whole board.
    /// View only: it moves the camera and nothing else.
    /// </summary>
    public sealed class GodCameraRig : MonoBehaviour
    {
        public const float MinHeight = 28f;
        /// <summary>Wheel step: each tick moves the camera this fraction of the way to the point under the cursor.</summary>
        public const float ZoomStep = 0.14f;
        /// <summary>A right-button press that moves less than this many pixels is a click, not a pan.</summary>
        public const float ClickSlopPixels = 6f;

        public Camera Cam { get; private set; }
        /// <summary>Ground height at a world point (the island mesh); null means flat ground at 0.</summary>
        public System.Func<float, float, float> HeightAt;
        /// <summary>Asked before a mouse press starts a pan (true = the cursor is over a HUD panel).</summary>
        public System.Func<Vector2, bool> OverUi;
        /// <summary>Turn the input off (tests drive the public methods instead).</summary>
        public bool InputEnabled = true;
        /// <summary>U1a: the director that places the camera in god-only mode (null or not driving: this rig works as in stage 3).</summary>
        public CameraDirector Director;
        /// <summary>Raised at the end of <see cref="Reframe(float, Vector3)"/>: the whole island is framed (the director's home view).</summary>
        public event System.Action Reframed;

        public float MaxHeight { get; private set; } = 400f;
        public float IslandRadius { get; private set; } = 500f;
        /// <summary>The ground point the home view looks at; panning is clamped round it.</summary>
        public Vector3 IslandCentre { get; private set; }
        /// <summary>True while the last right/middle press moved past the click slop (a pan, so no click).</summary>
        public bool Dragged { get; private set; }

        private bool _dragging;
        private Vector2 _dragStartScreen;
        private Vector3 _dragStartWorld;

        public static GodCameraRig Attach(Camera cam)
        {
            var rig = cam.GetComponent<GodCameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<GodCameraRig>();
            rig.Cam = cam;
            return rig;
        }

        /// <summary>Frames the whole island (the home view) and makes it the zoom-out limit.</summary>
        public void Reframe(float islandRadius) => Reframe(islandRadius, Vector3.zero);

        /// <summary>Frames a circle round a centre (the home view) and makes it the zoom-out limit.</summary>
        public void Reframe(float islandRadius, Vector3 centre)
        {
            IslandRadius = islandRadius;
            IslandCentre = new Vector3(centre.x, 0f, centre.z);
            IslandCamera.Frame(Cam, islandRadius, IslandCentre);
            MaxHeight = Cam.transform.position.y;
            Reframed?.Invoke();
        }

        /// <summary>Ground height used for picking: the island mesh, never below the sea level at 0.</summary>
        public float GroundY(float x, float z) => HeightAt == null ? 0f : Mathf.Max(0f, HeightAt(x, z));

        /// <summary>Where the ray through a screen point (pixels, origin bottom-left) meets the ground.</summary>
        public bool GroundPoint(Vector2 screen, out Vector3 point)
        {
            return GroundHit(Cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f)), out point);
        }

        /// <summary>Where a ray meets the ground (the island mesh height), false when it points away from it.</summary>
        public bool GroundHit(Ray ray, out Vector3 point)
        {
            point = default;
            float y = 0f;
            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(ray.direction.y) < 1e-4f) return false;
                float t = (y - ray.origin.y) / ray.direction.y;
                if (t < 0f) return false;
                point = ray.origin + ray.direction * t;
                y = GroundY(point.x, point.z);
            }
            return true;
        }

        /// <summary>Zoom by wheel ticks (positive = in) along the view ray through a screen point.</summary>
        public void Zoom(float ticks, Vector2 screen)
        {
            if (Mathf.Approximately(ticks, 0f) || !GroundPoint(screen, out var p)) return;
            _glideTo = null;
            var pos = Cam.transform.position;
            float f = Mathf.Pow(1f - ZoomStep, ticks);
            float h0 = pos.y - p.y;
            if (h0 < 1e-3f) return;
            float newY = Mathf.Clamp(p.y + h0 * f, MinHeight, MaxHeight);
            f = (newY - p.y) / h0;
            Cam.transform.position = p + (pos - p) * f;
            Clamp();
        }

        private Vector3? _glideTo;

        /// <summary>
        /// Flies the camera so the ground point is at the centre of the view, <paramref name="height"/> above it (the tilt stays).
        /// Gliding ends at once when the player pans or zooms.
        /// </summary>
        public void FocusOn(Vector3 ground, float height, bool instant = false)
        {
            if (Director != null && Director.Driving)
            {
                // the old height above the ground becomes a distance along the current view
                Director.FocusOn(ground, height / Mathf.Max(0.2f, Mathf.Sin(Director.Pitch * Mathf.Deg2Rad)), instant);
                return;
            }
            var f = Cam.transform.forward;
            float t = height / Mathf.Max(0.2f, -f.y);
            var target = new Vector3(ground.x, GroundY(ground.x, ground.z), ground.z) - f * t;
            if (instant) { Cam.transform.position = target; _glideTo = null; Clamp(); }
            else _glideTo = target;
        }

        public bool Gliding => _glideTo.HasValue;

        private void LateUpdate()
        {
            if (Cam == null || !_glideTo.HasValue) return;
            var tr = Cam.transform;
            tr.position = Vector3.Lerp(tr.position, _glideTo.Value, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            if ((tr.position - _glideTo.Value).sqrMagnitude < 0.04f) { tr.position = _glideTo.Value; _glideTo = null; }
            Clamp();
        }

        /// <summary>Slides the camera over the ground by a world offset (y ignored).</summary>
        public void PanBy(Vector3 delta)
        {
            _glideTo = null;
            delta.y = 0f;
            Cam.transform.position += delta;
            Clamp();
        }

        /// <summary>Keeps the point the camera looks at over the island: a small slack zoomed out, the whole board zoomed in.</summary>
        public void Clamp()
        {
            var tr = Cam.transform;
            var pos = tr.position;
            pos.y = Mathf.Clamp(pos.y, MinHeight, MaxHeight);
            tr.position = pos;
            if (!GroundPoint(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), out var c)) return;
            float t = Mathf.Clamp01((MaxHeight - pos.y) / Mathf.Max(1f, MaxHeight - MinHeight));
            float allowed = 30f + (IslandRadius * 0.85f) * t;
            var flat = new Vector2(c.x - IslandCentre.x, c.z - IslandCentre.z);
            if (flat.magnitude > allowed)
            {
                var shift = flat * (1f - allowed / flat.magnitude);
                tr.position = pos - new Vector3(shift.x, 0f, shift.y);
            }
        }

        private void Update()
        {
            if (Cam == null || !InputEnabled) return;
            var mouse = (Vector2)Input.mousePosition;
            bool over = OverUi != null && OverUi(mouse);

            float wheel = Input.mouseScrollDelta.y;
            if (!over && Mathf.Abs(wheel) > 0.01f) Zoom(wheel, mouse);

            bool down = Input.GetMouseButton(1) || Input.GetMouseButton(2);
            if (down && !_dragging && (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) && !over && GroundPoint(mouse, out var start))
            {
                _dragging = true; Dragged = false; _dragStartScreen = mouse; _dragStartWorld = start;
            }
            else if (!down) _dragging = false;
            if (_dragging)
            {
                if ((mouse - _dragStartScreen).magnitude > ClickSlopPixels) Dragged = true;
                if (Dragged && GroundPoint(mouse, out var now)) PanBy(_dragStartWorld - now);
            }

            float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float z = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            if (x != 0f || z != 0f)
            {
                var fwd = Cam.transform.forward; fwd.y = 0f; fwd.Normalize();
                var right = Cam.transform.right; right.y = 0f; right.Normalize();
                float speed = Cam.transform.position.y * 1.1f * Time.unscaledDeltaTime;
                PanBy((right * x + fwd * z).normalized * speed);
            }
        }
    }
}
