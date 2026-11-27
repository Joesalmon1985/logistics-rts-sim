using System;
using UnityEngine;

namespace God.Game
{
    /// <summary>The four named zoom bands (design section 4). They drive drawn detail only (VISUAL_WORLD_DESIGN section 14).</summary>
    public enum ZoomBand { Island, Region, Settlement, Individual }

    /// <summary>Something the follow camera can track: a person today, a vehicle later (I6). View side only.</summary>
    public interface IFollowTarget
    {
        /// <summary>The drawn position now; false when it is gone (a dead person is no longer drawn).</summary>
        bool TryGetPosition(out Vector3 world);
        int Id { get; }
    }

    /// <summary>The pose of the overhead camera, for the ui save extra.</summary>
    [Serializable]
    public struct CameraPose
    {
        public float FocusX, FocusZ, Yaw, Pitch, Distance;
    }

    /// <summary>
    /// The one camera component with modes (CC-6). U1a has the overhead mode: an orbit model (focus point on the ground, yaw,
    /// pitch, distance; the field of view stays the island camera's) that owns <see cref="GodCameraRig"/> as its overhead
    /// implementation: at yaw 0 and pitch 50 it is exactly the old view. Every move changes a target pose that the camera eases
    /// to on UNSCALED time, so the camera works while the game is paused. The sim never reads any of this (CC-3): the camera,
    /// the focus and the zoom band decide what is drawn and what can be clicked, never what happens.
    /// <para>Not driving (the stage 3 faction mode) the director does nothing and the old rig input applies.</para>
    /// </summary>
    public sealed class CameraDirector : MonoBehaviour
    {
        public Camera Cam { get; private set; }
        public GodCameraRig Rig { get; private set; }
        public ControlTuning Tuning = new ControlTuning();

        /// <summary>True once the director places the camera (god-only mode, or a test). Turning it on takes the pose the camera has.</summary>
        public bool Driving
        {
            get => _driving;
            set
            {
                if (value && !_driving) { _driving = true; SyncFromCamera(); }
                else _driving = value;
                if (Rig != null) Rig.InputEnabled = !_driving && _rigInput;
            }
        }

        private bool _driving, _rigInput = true;
        private Vector3 _focus, _tFocus;     // ground-level focus point (y is the ground height)
        private float _yaw, _tYaw, _pitch, _tPitch, _dist, _tDist;
        private float _homeDist = 400f;
        private Vector3 _homeFocus;
        private IFollowTarget _follow;
        private bool _grabbing;
        private Vector3 _grabWorld;
        private ZoomBand _band = ZoomBand.Island;

        /// <summary>Raised when the zoom band changes (U3 listens for level of detail).</summary>
        public event Action<ZoomBand> BandChanged;
        /// <summary>Raised when following ends (pan, another focus, or the target vanished).</summary>
        public event Action FollowEnded;

        /// <summary>Makes an <see cref="IFollowTarget"/> for a person id (set by the bootstrap from the NPC layer).</summary>
        public Func<int, IFollowTarget> Follower;

        public static CameraDirector Attach(Camera cam)
        {
            var d = cam.GetComponent<CameraDirector>();
            if (d == null) d = cam.gameObject.AddComponent<CameraDirector>();
            d.Cam = cam;
            d.Rig = GodCameraRig.Attach(cam);
            d.Rig.Director = d;
            d._rigInput = d.Rig.InputEnabled;
            d.Rig.Reframed -= d.OnReframed;
            d.Rig.Reframed += d.OnReframed;
            return d;
        }

        private void OnDestroy() { if (Rig != null) Rig.Reframed -= OnReframed; }

        // ---------------------------------------------------------------- read side

        public Vector3 Focus => _focus;
        public Vector3 TargetFocus => _tFocus;
        public float Yaw => _yaw;
        public float Pitch => _pitch;
        public float Distance => _dist;
        public float TargetDistance => _tDist;
        public float TargetYaw => _tYaw;
        public float TargetPitch => _tPitch;
        public float MaxDistance => _homeDist;
        public float MinDistance => Tuning.MinDistance;
        public bool Following => _follow != null;
        public int FollowId => _follow == null ? 0 : _follow.Id;
        public ZoomBand Band => BandOf(_dist);
        public bool Grabbing => _grabbing;

        public ZoomBand BandOf(float distance)
        {
            if (distance <= Tuning.IndividualDistance) return ZoomBand.Individual;
            if (distance <= Tuning.SettlementDistance) return ZoomBand.Settlement;
            if (distance <= Tuning.RegionDistance) return ZoomBand.Region;
            return ZoomBand.Island;
        }

        /// <summary>Distance of a band (the whole island is the home distance).</summary>
        public float DistanceOf(ZoomBand b)
        {
            switch (b)
            {
                case ZoomBand.Individual: return Tuning.IndividualDistance;
                case ZoomBand.Settlement: return Tuning.SettlementDistance;
                case ZoomBand.Region: return Tuning.RegionDistance;
                default: return _homeDist;
            }
        }

        public CameraPose Pose => new CameraPose { FocusX = _tFocus.x, FocusZ = _tFocus.z, Yaw = _tYaw, Pitch = _tPitch, Distance = _tDist };

        public void RestorePose(CameraPose p, bool instant = true)
        {
            _follow = null;
            _tFocus = new Vector3(p.FocusX, 0f, p.FocusZ);
            _tYaw = p.Yaw;
            _tPitch = Mathf.Clamp(p.Pitch, Tuning.MinPitch, Tuning.MaxPitch);
            _tDist = Mathf.Clamp(p.Distance <= 0f ? _homeDist : p.Distance, Tuning.MinDistance, _homeDist);
            ClampTarget();
            if (instant) Settle();
        }

        // ---------------------------------------------------------------- moves (all change the target pose)

        private void OnReframed()
        {
            // The rig has just framed the whole island (first frame, or the window changed shape): that is the home view.
            SyncFromCamera();
            _homeDist = _dist;
            _homeFocus = Rig.IslandCentre;
        }

        /// <summary>Reads the orbit state back from where the camera is now (focus, yaw, pitch, distance).</summary>
        public void SyncFromCamera()
        {
            if (Cam == null) return;
            var tr = Cam.transform; var f = tr.forward;
            _pitch = Mathf.Asin(Mathf.Clamp(-f.y, -1f, 1f)) * Mathf.Rad2Deg;
            _yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            if (!Rig.GroundHit(new Ray(tr.position, f), out var hit))
                hit = new Vector3(tr.position.x, 0f, tr.position.z) + new Vector3(f.x, 0f, f.z).normalized * tr.position.y;
            _focus = hit;
            _dist = Mathf.Max(Tuning.MinDistance, (tr.position - hit).magnitude);
            if (_homeDist < _dist) _homeDist = _dist;
            _homeFocus = Rig.IslandCentre;
            _tFocus = _focus; _tYaw = _yaw; _tPitch = _pitch; _tDist = _dist;
        }

        /// <summary>Whole island: the framed home view, north up. Gliding.</summary>
        public void Home(bool instant = false)
        {
            Unfollow();
            _tFocus = _homeFocus; _tPitch = Tuning.HomePitch; _tDist = _homeDist;
            _tYaw = Mathf.Round(_yaw / 360f) * 360f;
            if (instant) Settle();
        }

        /// <summary>Turns the camera about the focus by deltas in degrees (yaw clockwise seen from above; pitch clamped 25 to 80).</summary>
        public void Orbit(float yawDelta, float pitchDelta)
        {
            _tYaw += yawDelta;
            _tPitch = Mathf.Clamp(_tPitch + pitchDelta, Tuning.MinPitch, Tuning.MaxPitch);
        }

        /// <summary>Sets yaw and pitch outright.</summary>
        public void SetOrbit(float yaw, float pitch)
        {
            _tYaw = yaw;
            _tPitch = Mathf.Clamp(pitch, Tuning.MinPitch, Tuning.MaxPitch);
        }

        /// <summary>Zoom by wheel ticks (positive = in) towards a screen point (pixels): the ground point under it stays under it.</summary>
        public void Zoom(float ticks, Vector2 screen)
        {
            if (Mathf.Approximately(ticks, 0f)) return;
            float f = Mathf.Pow(1f - Tuning.ZoomStep, ticks);
            float nd = Mathf.Clamp(_tDist * f, Tuning.MinDistance, _homeDist);
            f = nd / Mathf.Max(0.001f, _tDist);
            if (_follow == null && Rig.GroundPoint(screen, out var p)) _tFocus = p + (_tFocus - p) * f;
            _tDist = nd;
            ClampTarget();
        }

        /// <summary>Zoom to a given distance without moving the focus.</summary>
        public void ZoomTo(float distance)
        {
            _tDist = Mathf.Clamp(distance, Tuning.MinDistance, _homeDist);
            ClampTarget();
        }

        public void ZoomToBand(ZoomBand b) => ZoomTo(DistanceOf(b));

        /// <summary>Slides the focus over the ground by a world offset (y ignored). Detaches the follow camera.</summary>
        public void PanBy(Vector3 delta)
        {
            Unfollow();
            delta.y = 0f;
            _tFocus += delta;
            ClampTarget();
        }

        /// <summary>Keyboard pan: x right, y forward, in the camera's own frame (so it follows the yaw), for dt seconds of unscaled time.</summary>
        public void Pan(Vector2 axis, float dt)
        {
            if (axis.sqrMagnitude < 1e-6f) return;
            float yaw = _tYaw * Mathf.Deg2Rad;
            var fwd = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            var right = new Vector3(fwd.z, 0f, -fwd.x);
            var dir = right * axis.x + fwd * axis.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            PanBy(dir * (_tDist * Tuning.PanSpeedPerDistance * dt));
        }

        /// <summary>Starts a grab-the-ground drag at a screen point. False when the point sees no ground.</summary>
        public bool BeginGrab(Vector2 screen)
        {
            _grabbing = Rig.GroundPoint(screen, out _grabWorld);
            return _grabbing;
        }

        /// <summary>Moves the grabbed ground point under the pointer (rigid: no easing).</summary>
        public void GrabTo(Vector2 screen)
        {
            if (!_grabbing || !Rig.GroundPoint(screen, out var now)) return;
            var delta = _grabWorld - now; delta.y = 0f;
            Unfollow();
            _focus += delta; _tFocus += delta;
            ClampBoth();
            ApplyPose();
        }

        public void EndGrab() { _grabbing = false; }

        /// <summary>Flies the focus to a ground point at a distance (null: keep the distance). Detaches the follow camera.</summary>
        public void FocusOn(Vector3 ground, float? distance = null, bool instant = false)
        {
            Unfollow();
            _tFocus = ground;
            if (distance.HasValue) _tDist = Mathf.Clamp(distance.Value, Tuning.MinDistance, _homeDist);
            ClampTarget();
            if (instant) Settle();
        }

        /// <summary>Focus on a person's drawn position, no further out than the individual band. False when the person is not drawn.</summary>
        public bool FocusOnPerson(int npcId, bool instant = false)
        {
            var t = Follower?.Invoke(npcId);
            if (t == null || !t.TryGetPosition(out var p)) return false;
            FocusOn(p, Mathf.Min(_tDist, Tuning.IndividualDistance), instant);
            return true;
        }

        /// <summary>The focus tracks a person's drawn position with the follow smoothing; a pan detaches, a death leaves the camera in place.</summary>
        public bool Follow(int npcId) => Follow(Follower?.Invoke(npcId));

        public bool Follow(IFollowTarget target)
        {
            if (target == null || !target.TryGetPosition(out _)) return false;
            _follow = target;
            return true;
        }

        public void Unfollow()
        {
            if (_follow == null) return;
            _follow = null;
            FollowEnded?.Invoke();
        }

        /// <summary>Jumps the current pose to the target pose at once (tests, restoring a save).</summary>
        public void Settle()
        {
            ClampTarget();
            _focus = _tFocus; _yaw = _tYaw; _pitch = _tPitch; _dist = _tDist;
            ApplyPose();
        }

        // ---------------------------------------------------------------- per frame

        private void LateUpdate()
        {
            if (!_driving || Cam == null) return;
            Advance(Time.unscaledDeltaTime);
        }

        /// <summary>Eases the pose towards the target by dt seconds of unscaled time, then places the camera. Public so tests can step it.</summary>
        public void Advance(float dt)
        {
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            float k = 1f - Mathf.Exp(-dt * Tuning.Stiffness);
            float kf = k;
            if (_follow != null)
            {
                if (_follow.TryGetPosition(out var p))
                {
                    _tFocus = p;
                    kf = 1f - Mathf.Exp(-dt / Mathf.Max(0.02f, Tuning.FollowSmoothing));
                }
                else Unfollow(); // the person is gone: the camera stays where it is
            }
            _focus = Vector3.Lerp(_focus, _tFocus, kf);
            _yaw = Mathf.Lerp(_yaw, _tYaw, k);
            _pitch = Mathf.Lerp(_pitch, _tPitch, k);
            _dist = Mathf.Exp(Mathf.Lerp(Mathf.Log(_dist), Mathf.Log(_tDist), k));
            ClampBoth();
            ApplyPose();
        }

        private static float Lift(float dist) => Mathf.Clamp(1.2f * (1f - dist / 40f), 0f, 1.2f);

        private void ClampTarget() => ClampFocus(ref _tFocus, _tDist);

        private void ClampBoth() { ClampFocus(ref _tFocus, _tDist); ClampFocus(ref _focus, _dist); }

        /// <summary>Keeps the looked-at point over the island: a small slack zoomed out, the whole board zoomed in; y follows the ground.</summary>
        private void ClampFocus(ref Vector3 f, float dist)
        {
            float t = Mathf.Clamp01((_homeDist - dist) / Mathf.Max(1f, _homeDist - Tuning.MinDistance));
            float allowed = 30f + (Rig.IslandRadius * 0.85f) * t;
            var c = Rig.IslandCentre;
            var flat = new Vector2(f.x - c.x, f.z - c.z);
            if (flat.magnitude > allowed) { flat *= allowed / flat.magnitude; f.x = c.x + flat.x; f.z = c.z + flat.y; }
            f.y = Rig.GroundY(f.x, f.z);
        }

        private void ApplyPose()
        {
            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            var centre = _focus + Vector3.up * Lift(_dist);
            var pos = centre - rot * Vector3.forward * _dist;
            float floor = Rig.GroundY(pos.x, pos.z) + 0.8f;
            if (pos.y < floor) pos.y = floor; // the eye stays above the ground at close zoom
            Cam.transform.SetPositionAndRotation(pos, rot);
            Cam.nearClipPlane = Mathf.Clamp(_dist * 0.08f, 0.3f, 5f);
            Cam.farClipPlane = _dist + Rig.IslandRadius * 4f + 200f;
            var b = BandOf(_dist);
            if (b != _band) { _band = b; BandChanged?.Invoke(b); }
        }
    }

    /// <summary>Follows a person through the NPC layer's drawn position.</summary>
    public sealed class NpcFollowTarget : IFollowTarget
    {
        private readonly NpcLayer _layer;
        private readonly int _id;
        public NpcFollowTarget(NpcLayer layer, int id) { _layer = layer; _id = id; }
        public int Id => _id;
        public bool TryGetPosition(out Vector3 world)
        {
            if (_layer != null && _layer.TryPosition(_id, out world)) return true;
            world = default;
            return false;
        }
    }
}
