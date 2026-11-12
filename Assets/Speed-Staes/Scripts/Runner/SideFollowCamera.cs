using UnityEngine;
using SpeedStars.Meta.Contracts;
using SpeedStars.Physical;
using Zenject;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Side-on race camera: sits to the runner's right and tracks along the run axis only,
    /// so step bob and pelvis sway never shake the frame. Along the track it is locked to the runner (Lock Along Track), as in the
    /// original: a press's surge and the slowing between presses never leave the camera behind or let it run on past the runner.
    /// Framed as the original (measured from its lane lines): a wide lens (about 69 deg),
    /// level, about 0.47 m up per metre away at the nearest setting (flatter when the player sets it farther), so the zoom backs it away
    /// along one line and the angle onto the runner never changes with speed
    /// (Height Per Distance 0 keeps the older fixed height).
    /// Two things come from the race itself and touch only the picture: each press sends the camera back, flowing, and the speed
    /// holds it there (it comes in as both fade), and a knocked hurdle shakes it sideways. Each press also lets the runner get a little
    /// ahead in the picture before the camera catches up (the step nudge, as in the original), which makes the steps felt.
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
        [Tooltip("How tightly the camera follows along the track.")]
        public float followSharpness = 12f;
        [Tooltip("Along the track the camera moves exactly with the runner, so the runner never slides in the picture (the original). " +
                 "Off: it runs at the runner's smoothed speed (Track Speed Smooth, Track Pull), which lags a press's surge and then runs on " +
                 "past the runner as it slows between presses (the camera seems to throw itself forward).")]
        public bool lockAlongTrack = true;
        [Tooltip("Time (s) the camera's own speed along the track takes to follow the runner's (Lock Along Track off). With Runner Screen X 0 " +
                 "it also smooths how far behind the camera rests at speed.")]
        public float trackSpeedSmooth = 0.35f;
        [Tooltip("How strongly (1/s) the camera makes up what it is off along the track. Only with Lock Along Track off.")]
        public float trackPull = 2.5f;
        [Tooltip("The player's distance setting: multiplies distance and leaves the zoom on top of it.")]
        public float distanceScale = 1f;

        [Header("Zoom with speed")]
        [Tooltip("Speed at which the camera is fully back (m/s). It keeps coming back up to this speed, so the faster the runner the further it sits.")]
        public float speedZoomFull = 11f;
        [Tooltip("Distance at full zoom (the speed in Speed Zoom Full), as a multiple of the standing distance.")]
        public float speedZoomDistance = 2.4f;
        [Tooltip("How much of the full zoom each key press asks for at once (0..1).")]
        public float zoomPerPress = 0.15f;
        [Tooltip("How fast the zoom asked for by presses fades (share of the full zoom per second).")]
        public float zoomPressFade = 0.4f;
        [Tooltip("Time to flow back (s): it never jumps with a step.")]
        public float zoomFlowOutTime = 0.45f;
        [Tooltip("Time to flow in (s).")]
        public float zoomFlowInTime = 0.6f;

        [Header("Step nudge (each press, like the original)")]
        [Tooltip("How far the runner gets ahead in the picture on each press before the camera catches up, as a share of the picture's width at distance setting 1 (0.02 = 2%; a farther player setting shows less, as in the original). 0 = off.")]
        [Range(0f, 0.1f)]
        public float stepNudge = 0.02f;
        [Tooltip("Time (s) the runner takes to get ahead after a press (the camera lagging).")]
        [Min(0.01f)]
        public float stepNudgeRise = 0.28f;
        [Tooltip("Time (s) the camera then takes to catch up.")]
        [Min(0.01f)]
        public float stepNudgeFall = 0.15f;
        [Tooltip("On the same press the camera also backs away along its line of sight by this share of its distance and comes back (0.014 = 1.4%, measured from the original's lane lines). 0 = off.")]
        [Range(0f, 0.1f)]
        public float stepZoomPulse = 0.014f;
        [Tooltip("easeOutBack overshoot of the step nudge and pulse: each move shoots a little past its mark and settles back, instead of a flat glide. 1.70158 is the classic easeOutBack; 0 = no overshoot (a plain ease).")]
        [Range(0f, 4f)]
        public float stepNudgeOvershoot = 1.70158f;

        [Header("Zoom bounce")]
        [Tooltip("How the zoom settles: 1 = glides in with no overshoot (the older feel), lower = it passes its mark and swings back, like easeOutBack (0.55 overshoots about 13%).")]
        [Range(0.2f, 1f)]
        public float zoomDamping = 0.55f;

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
        float _trackSpeed;
        float _zoom, _zoomVelocity, _pressZoom;
        float _shakeSize, _shakeTime, _shakeStart = -10f;
        float _lastHitWeight;
        float _nudgeFrom, _nudgeStart = -10f;
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
            // A press restarts the nudge from where it is now, so quick presses never jump the picture.
            _nudgeFrom = Nudge();
            _nudgeStart = Time.time;
        }

        /// <summary>
        /// The step nudge now, 0..1: rises to 1 over <see cref="stepNudgeRise"/> (the runner gets ahead), then back to 0 over
        /// <see cref="stepNudgeFall"/> (the camera catches up). Only the picture moves; the follow itself is untouched.
        /// </summary>
        float Nudge()
        {
            float t = Time.time - _nudgeStart;
            if (t < stepNudgeRise) return Mathf.LerpUnclamped(_nudgeFrom, 1f, EaseOutBack(t / stepNudgeRise, stepNudgeOvershoot));
            t -= stepNudgeRise;
            return t < stepNudgeFall ? 1f - EaseOutBack(t / stepNudgeFall, stepNudgeOvershoot) : 0f;
        }

        /// <summary>
        /// easeOutBack: starts fast, passes 1 by a little and settles on it at x = 1 (overshoot 1.70158 is the classic curve, 0 is a
        /// plain cubic ease out). x is clamped to 0..1; the result may leave 0..1 on the way.
        /// </summary>
        public static float EaseOutBack(float x, float overshoot)
        {
            x = Mathf.Clamp01(x) - 1f;
            return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
        }

        /// <summary>Width of the picture at the runner's distance (m), so the nudge is the same share of the screen at any zoom.</summary>
        float ViewWidth() => WidthAt(Vector3.Distance(_position, target.position + Vector3.up * lookHeight));

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
            _nudgeFrom = 0f;
            _nudgeStart = -10f;
            var sim = _body ? _body.Sim : null;
            _trackSpeed = sim != null ? sim.Speed : 0f;
            // At speed it rests as far behind as it runs, so a snap does not start with a slide.
            _position = Desired() - target.forward * Lag();
            transform.position = _position;
            transform.rotation = Facing();
        }

        /// <summary>Unit direction from the runner to the camera on the ground: the right side turned toward the front by <see cref="sideYaw"/>.</summary>
        Vector3 Side() => Quaternion.AngleAxis(-sideYaw, Vector3.up) * target.right;

        Vector3 Desired()
        {
            float back = distance * distanceScale * Mathf.Lerp(1f, speedZoomDistance, _zoom);
            Vector3 side = Side();
            // The lead is taken along the picture's own horizontal, so the runner sits the same share of the frame left at any yaw.
            Vector3 screenRight = Vector3.Cross(Vector3.up, -side);
            return target.position + screenRight * Lead(back) + side * back + Vector3.up * HeightAt(back);
        }

        /// <summary>Camera height above the runner's feet at <paramref name="back"/> metres away: on one line through the runner, or the older fixed height.</summary>
        float HeightAt(float back) => heightPerDistance > 0f
            ? back * heightPerDistance / Mathf.Pow(Mathf.Max(0.1f, distanceScale), heightScalePower)
            : StandingHeight;

        /// <summary>The camera's offset from the runner without the lead (m): what the step zoom pulse scales.</summary>
        Vector3 ViewOffset()
        {
            float back = distance * distanceScale * Mathf.Lerp(1f, speedZoomDistance, _zoom);
            return Side() * back + Vector3.up * HeightAt(back);
        }

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
        float Lag() => runnerScreenX > 0f ? 0f : _trackSpeed / Mathf.Max(0.01f, followSharpness);

        /// <summary>Width of the picture (m) at <paramref name="depth"/> metres from the camera.</summary>
        float WidthAt(float depth)
        {
            if (!_camera) _camera = GetComponent<Camera>();
            if (!_camera) return 0f;
            return 2f * depth * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * _camera.aspect;
        }

        /// <summary>The camera's one fixed height (m): what the pitch gives at full zoom, so the picture at running speed is unchanged and presses never move it.</summary>
        public float StandingHeight => lookHeight + distance * distanceScale * speedZoomDistance * Mathf.Tan(pitch * Mathf.Deg2Rad);

        Quaternion Facing() => Quaternion.LookRotation(-Side(), Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);

        void LateUpdate()
        {
            if (!target) return;

            float dt = Time.deltaTime;
            var sim = _body ? _body.Sim : null;
            if (sim != null)
            {
                _pressZoom = Mathf.Max(0f, _pressZoom - zoomPressFade * dt);
                float wanted = Mathf.Max(_pressZoom, Mathf.Clamp01(sim.Speed / Mathf.Max(0.01f, speedZoomFull)));
                float flow = wanted > _zoom ? zoomFlowOutTime : zoomFlowInTime;
                if (zoomDamping >= 0.999f) _zoom = Mathf.SmoothDamp(_zoom, wanted, ref _zoomVelocity, flow);
                else
                {
                    // A spring that settles in about the same time but passes its mark and swings back (the easeOutBack feel).
                    float w = 3f / Mathf.Max(0.05f, flow);
                    float step = Mathf.Min(dt, 0.05f); // a load hitch must not throw the spring
                    _zoomVelocity += (w * w * (wanted - _zoom) - 2f * zoomDamping * w * _zoomVelocity) * step;
                    _zoom += _zoomVelocity * step;
                }
                if (sim.HitWeight > _lastHitWeight) Shake(hitShake, hitShakeTime);
                _lastHitWeight = sim.HitWeight;
            }

            float t = 1f - Mathf.Exp(-followSharpness * dt);
            Vector3 desired = Desired();
            // Only the track direction and the zoom go through this; the shake stays out of it so it ends exactly.
            if (sim != null)
            {
                // Locked, the camera sits exactly where the framing puts it along the track every frame. Unlocked, it runs at the runner's
                // smoothed speed, moving by exactly what it moved (no lag that depends on the frame time), and only slowly makes up what it
                // is off. Framed by a screen share it rests on that share at speed; otherwise it rests behind by the smoothed speed /
                // followSharpness (the older framing; the smoothing keeps that offset from jumping with each press).
                Vector3 along = target.forward;
                _trackSpeed = Mathf.Lerp(_trackSpeed, sim.Speed, 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, trackSpeedSmooth)));
                desired -= along * Lag();
                Vector3 off = desired - _position;
                float offAlong = Vector3.Dot(off, along);
                if (lockAlongTrack) _position += (off - along * offAlong) * t + along * offAlong;
                else
                {
                    // Far off (a change of runner without a snap) it closes as fast as it always did.
                    float pull = Mathf.Abs(offAlong) > TrackFarOff ? t : 1f - Mathf.Exp(-trackPull * dt);
                    _position += (off - along * offAlong) * t + along * (_trackSpeed * dt + offAlong * pull);
                }
            }
            else _position = Vector3.Lerp(_position, desired, t);

            Current(out float size);
            float n = Time.time * shakeRate;
            Vector3 shake = transform.right * ((Mathf.PerlinNoise(n, 0.3f) - 0.5f) * 2f * size);

            // The camera lags behind along the track by the nudge, so the runner moves ahead in the picture and comes back.
            // Measured in the original: the lag is the same in metres at any distance setting, so a farther player setting shows a
            // smaller share (2% at 8/10, ~1.3% at 10/10); dividing by the player's scale does the same here.
            float share = stepNudge / Mathf.Max(0.1f, distanceScale);
            Vector3 nudge = sim != null && stepNudge > 0f ? -target.forward * (share * ViewWidth() * Nudge()) : Vector3.zero;

            // The same press also backs the camera away along its line of sight a little and lets it come back.
            Vector3 pulse = sim != null && stepZoomPulse > 0f ? ViewOffset() * (stepZoomPulse * Nudge()) : Vector3.zero;

            transform.position = _position + shake + nudge + pulse;
            transform.rotation = Facing();
        }
    }
}
