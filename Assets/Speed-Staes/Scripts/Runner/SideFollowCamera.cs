using UnityEngine;
using SpeedStars.Meta.Contracts;
using SpeedStars.Physical;
using Zenject;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Side-on race camera: sits to the runner's right and tracks along the run axis only,
    /// so step bob and pelvis sway never shake the frame. Along the track it follows the runner through two critically damped springs
    /// in a row with their lag added back (Track Follow): at a steady speed the runner sits exactly at its place in the picture (the
    /// original keeps it fixed), while the sim's speed, which jumps on every press, reaches the camera with neither its speed nor its
    /// acceleration jumping. Locked instead, each press jerked the whole picture.
    /// Framed as the original (measured from its lane lines): a wide lens (about 69 deg),
    /// level, about 0.47 m up per metre away at the nearest setting (flatter when the player sets it farther), so the zoom backs it away
    /// along one line and the angle onto the runner never changes with speed
    /// (Height Per Distance 0 keeps the older fixed height).
    /// The zoom reads the speed held over a moment (Zoom Speed Smooth), not a single press, and glides with no overshoot; a knocked
    /// hurdle shakes the camera sideways. Presses themselves never kick the picture.
    /// </summary>
    public sealed class SideFollowCamera : MonoBehaviour
    {
        [Tooltip("The runner to follow.")]
        public Transform target;
        [Tooltip("Distance from the runner standing still (m).")]
        public float distance = 4.5f;
        [Tooltip("How far the camera looks down at the runner (deg). The camera's fixed height follows from it at full zoom, so the angle is right at running speed and the height never moves with the zoom.")]
        public float pitch = 8f;
        [Tooltip("Turns the camera round the runner toward the runner's front (deg), so the near and far leg and arm can be told apart. 0 is exactly side-on. The follow stays on the run axis.")]
        public float sideYaw = 4f;
        [Tooltip("Frame the runner slightly left so more track ahead is visible (m). Only used when Runner Screen X is 0.")]
        public float leadAhead = 0.6f;
        [Tooltip("Where the runner sits across the picture (0 = left edge, 0.5 = centre, 1 = right edge), at any zoom, speed and distance setting; the original keeps it at about a third. 0 = use Lead Ahead (m) instead.")]
        [Range(0f, 0.9f)]
        public float runnerScreenX = 0.32f;
        [Tooltip("Height the camera looks at (m). Only used when Height Per Distance is 0.")]
        public float lookHeight = 0.9f;
        [Tooltip("Camera height above the runner's feet per metre of distance at distance setting 1 (0.47 = 1.69 m at 3.6 m: the original's nearest setting). The camera backs away along one line as it zooms, so the angle onto the runner never changes with speed. 0 = the older fixed height from Pitch and Look Height.")]
        [Min(0f)]
        public float heightPerDistance = 0.47f;
        [Tooltip("How much flatter that line gets as the player's distance setting grows: the slope is divided by setting ^ this. The original goes from 0.47 at its nearest to 0.29 at twice the distance, which is 0.7. 0 = the same slope at every setting.")]
        [Range(0f, 2f)]
        public float heightScalePower = 0.7f;
        [Tooltip("Vertical field of view the race camera uses (deg); the original is about 69. 0 = leave the Camera's own.")]
        [Range(0f, 120f)]
        public float fieldOfView = 69f;
        [Tooltip("How tightly the camera follows across the track and in height (where the zoom moves it). With Runner Screen X 0 it also sets how far behind it rests at speed.")]
        public float followSharpness = 12f;
        [Tooltip("How quickly (Hz) the camera follows the runner along the track: two critically damped springs in a row with their lag " +
                 "added back. At a steady speed the runner stays exactly in place in the picture; a press from standing moves it up to " +
                 "about 15 cm in the picture at 2.5 and back, with the camera's speed and acceleration never jumping (higher = tighter, " +
                 "lower = softer). 0 = locked to the runner (every surge of the sim shows as a jerk of the whole picture).")]
        [Min(0f)]
        public float trackFollow = 2.5f;
        [Tooltip("The player's distance setting: multiplies distance and leaves the zoom on top of it.")]
        public float distanceScale = 1f;

        [Header("Zoom with speed")]
        // New names (12.11): Race.unity kept speedZoomFull 10 / speedZoomDistance 1.5 from 11.11, which flattened the zoom the user wants
        // (close when slow, well back when fast); those old values are no longer read.
        [Tooltip("Speed at which the camera is fully back (m/s). It keeps coming back up to this speed, so the faster the runner the further it sits.")]
        public float zoomOutSpeed = 11f;
        [Tooltip("Distance at full zoom (the speed in Zoom Out Speed), as a multiple of the standing distance: close to the runner standing, well back at speed.")]
        public float zoomOutDistance = 2.4f;
        [Tooltip("Time (s) the speed the zoom reads takes to follow the runner's: a single press's surge barely moves the zoom, a held pace does. 0 = the raw speed.")]
        [Min(0f)]
        public float zoomSpeedSmooth = 0.8f;
        [Tooltip("How much of the full zoom each key press asks for at once (0..1).")]
        public float zoomPerPress = 0.15f;
        [Tooltip("How fast the zoom asked for by presses fades (share of the full zoom per second).")]
        public float zoomPressFade = 0.4f;
        [Tooltip("Time to flow back (s): it never jumps with a step.")]
        public float zoomFlowOutTime = 0.45f;
        [Tooltip("Time to flow in (s).")]
        public float zoomFlowInTime = 0.6f;

        [Header("Shake")]
        [Tooltip("Shake size on a knocked hurdle (m); sideways only.")]
        public float hitShake = 0.12f;
        [Tooltip("Shake length on a knocked hurdle (s).")]
        public float hitShakeTime = 0.3f;
        [Tooltip("How fast the shake moves (noise steps per second).")]
        public float shakeRate = 30f;

        /// <summary>Further off than this along the track (m) the camera closes the gap as fast as the plain follow does (a change of runner).</summary>
        const float TrackFarOff = 3f;

        RunnerBody _body;
        Vector3 _position;
        Spring _track, _trackOut;   // the two stages of the camera's place along the track, without their lag added back (m)
        float _heldSpeed;       // the runner's speed held over Zoom Speed Smooth (m/s)
        float _zoom, _zoomVelocity, _pressZoom;
        float _shakeSize, _shakeTime, _shakeStart = -10f;
        float _lastHitWeight;
        Camera _camera;

        void Awake()
        {
            // Before the opening shot reads it as the race camera's field of view.
            _camera = GetComponent<Camera>();
            if (_camera && fieldOfView > 0f) _camera.fieldOfView = fieldOfView;
        }

        void OnEnable()
        {
            _body = target ? target.GetComponent<RunnerBody>() : null;
            if (_body)
            {
                _body.RunReset += SnapToTarget;
                _body.Tapped += OnTapped;
            }
            _position = transform.position;
            if (target) SetTrack(Vector3.Dot(_position, target.forward), 0f);
        }

        void OnDisable()
        {
            if (_body)
            {
                _body.RunReset -= SnapToTarget;
                _body.Tapped -= OnTapped;
            }
            _body = null;
        }

        void Start()
        {
            var context = FindObjectOfType<SceneContext>();
            var settings = context ? context.Container.TryResolve<IPlayerSettings>() : null;
            if (settings != null) distanceScale = settings.CameraDistance;
        }

        void OnTapped(int leg, bool pressed, double simTime)
        {
            if (!pressed || _body.Sim == null) return;
            _pressZoom = Mathf.Clamp01(_pressZoom + zoomPerPress);
        }

        /// <summary>
        /// easeOutBack: starts fast, passes 1 by a little and settles on it at x = 1 (overshoot 1.70158 is the classic curve, 0 is a
        /// plain cubic ease out). x is clamped to 0..1; the result may leave 0..1 on the way. The opening shot (RaceStartCamera) uses it.
        /// </summary>
        public static float EaseOutBack(float x, float overshoot)
        {
            x = Mathf.Clamp01(x) - 1f;
            return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
        }

        /// <summary>A stronger shake running now is not cut by a smaller one.</summary>
        void Shake(float size, float time)
        {
            if (Current(out float running) > 0f && running > size) return;
            _shakeSize = size;
            _shakeTime = time;
            _shakeStart = Time.time;
        }

        /// <summary>Share of the shake left (1 at its start, 0 when over); size is what it is worth now.</summary>
        float Current(out float size)
        {
            float s = (Time.time - _shakeStart) / Mathf.Max(0.01f, _shakeTime);
            float left = s < 1f ? 1f - s : 0f;
            size = _shakeSize * left;
            return left;
        }

        public void SnapToTarget()
        {
            if (!target) return;
            _zoom = _zoomVelocity = _pressZoom = 0f;
            _shakeStart = -10f;
            _lastHitWeight = 0f;
            var sim = _body ? _body.Sim : null;
            float speed = sim != null ? sim.Speed : 0f;
            _heldSpeed = speed;
            // At speed it rests as far behind as it runs, so a snap does not start with a slide.
            _position = Desired() - target.forward * Lag();
            SetTrack(Vector3.Dot(_position, target.forward), speed);
            transform.position = _position;
            transform.rotation = Facing();
        }

        /// <summary>The lag one critically damped spring at Track Follow keeps behind a target moving at <paramref name="speed"/> (m).</summary>
        float StageLag(float speed) => trackFollow > 0f ? speed / (Mathf.PI * trackFollow) : 0f;

        /// <summary>The camera's place along the track: the second stage with both stages' lag added back, so at a steady speed it is on the runner.</summary>
        float TrackShown() => _trackOut.Value + 2f * StageLag(_trackOut.Velocity);

        /// <summary>Puts the camera at <paramref name="along"/> on the track moving at <paramref name="speed"/>, with no swing to come.</summary>
        void SetTrack(float along, float speed)
        {
            float lag = StageLag(speed);
            _track.Value = along - lag;
            _track.Velocity = speed;
            _trackOut.Value = along - 2f * lag;
            _trackOut.Velocity = speed;
        }

        /// <summary>Unit direction from the runner to the camera on the ground: the right side turned toward the front by <see cref="sideYaw"/>.</summary>
        Vector3 Side() => Quaternion.AngleAxis(-sideYaw, Vector3.up) * target.right;

        Vector3 Desired()
        {
            float back = distance * distanceScale * Mathf.Lerp(1f, zoomOutDistance, _zoom);
            Vector3 side = Side();
            // The lead is taken along the picture's own horizontal, so the runner sits the same share of the frame left at any yaw.
            Vector3 screenRight = Vector3.Cross(Vector3.up, -side);
            return target.position + screenRight * Lead(back) + side * back + Vector3.up * HeightAt(back);
        }

        /// <summary>Camera height above the runner's feet at <paramref name="back"/> metres away: on one line through the runner, or the older fixed height.</summary>
        float HeightAt(float back) => heightPerDistance > 0f
            ? back * heightPerDistance / Mathf.Pow(Mathf.Max(0.1f, distanceScale), heightScalePower)
            : StandingHeight;

        /// <summary>
        /// How far ahead of the runner the camera centres (m). With <see cref="runnerScreenX"/> set it is that share of the picture's
        /// width at the camera's distance, so the runner stays in the same place at any zoom or distance setting; otherwise <see cref="leadAhead"/>.
        /// </summary>
        float Lead(float back)
        {
            if (runnerScreenX <= 0f) return leadAhead;
            return (0.5f - runnerScreenX) * WidthAt(back);
        }

        /// <summary>How far behind the camera rests at speed (m). None when the runner is framed by a screen share: that share holds at any speed.</summary>
        float Lag() => runnerScreenX > 0f ? 0f : _heldSpeed / Mathf.Max(0.01f, followSharpness);

        /// <summary>Width of the picture (m) at <paramref name="depth"/> metres from the camera.</summary>
        float WidthAt(float depth)
        {
            if (!_camera) _camera = GetComponent<Camera>();
            if (!_camera) return 0f;
            return 2f * depth * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * _camera.aspect;
        }

        /// <summary>The camera's one fixed height (m): what the pitch gives at full zoom, so the picture at running speed is unchanged and presses never move it.</summary>
        public float StandingHeight => lookHeight + distance * distanceScale * zoomOutDistance * Mathf.Tan(pitch * Mathf.Deg2Rad);

        Quaternion Facing() => Quaternion.LookRotation(-Side(), Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);

        void LateUpdate()
        {
            if (!target) return;

            float dt = Time.deltaTime;
            var sim = _body ? _body.Sim : null;
            if (sim != null)
            {
                _pressZoom = Mathf.Max(0f, _pressZoom - zoomPressFade * dt);
                // The zoom reads the pace, not the press: a single surge passes through the smoothing almost unseen.
                _heldSpeed = zoomSpeedSmooth > 0f ? Mathf.Lerp(_heldSpeed, sim.Speed, 1f - Mathf.Exp(-dt / zoomSpeedSmooth)) : sim.Speed;
                float wanted = Mathf.Max(_pressZoom, Mathf.Clamp01(_heldSpeed / Mathf.Max(0.01f, zoomOutSpeed)));
                _zoom = Mathf.SmoothDamp(_zoom, wanted, ref _zoomVelocity, wanted > _zoom ? zoomFlowOutTime : zoomFlowInTime);
                if (sim.HitWeight > _lastHitWeight) Shake(hitShake, hitShakeTime);
                _lastHitWeight = sim.HitWeight;
            }

            float t = 1f - Mathf.Exp(-followSharpness * dt);
            Vector3 desired = Desired();
            // Only the track direction and the zoom go through this; the shake stays out of it so it ends exactly.
            if (sim != null)
            {
                // Along the track: the two springs, their steady lag added back (TrackShown), so the camera's speed bends where the runner's jumps.
                Vector3 along = target.forward;
                desired -= along * Lag();
                float want = Vector3.Dot(desired, along);
                float shown = TrackShown();
                if (trackFollow <= 0f) SetTrack(want, sim.Speed);
                // Far off (a change of runner without a snap) it closes as fast as the plain follow and takes the runner's speed.
                else if (Mathf.Abs(want - shown) > TrackFarOff) SetTrack(Mathf.Lerp(shown, want, t), sim.Speed);
                else
                {
                    _track.Step(want, trackFollow, 1f, dt);
                    _trackOut.Step(_track.Value, trackFollow, 1f, dt);
                }
                Vector3 off = desired - _position;
                _position += (off - along * Vector3.Dot(off, along)) * t;
                _position += along * (TrackShown() - Vector3.Dot(_position, along));
            }
            else _position = Vector3.Lerp(_position, desired, t);

            Current(out float size);
            float n = Time.time * shakeRate;
            Vector3 shake = transform.right * ((Mathf.PerlinNoise(n, 0.3f) - 0.5f) * 2f * size);

            transform.position = _position + shake;
            transform.rotation = Facing();
        }
    }
}
