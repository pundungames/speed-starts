using UnityEngine;
using SpeedStars.Meta.Contracts;
using SpeedStars.Physical;
using Zenject;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Side-on race camera: sits to the runner's right and tracks along the run axis only,
    /// so step bob and pelvis sway never shake the frame. Its height never changes (the zoom moves it only away from the runner).
    /// Two things come from the race itself and touch only the picture: each press sends the camera back, flowing, and the speed
    /// holds it there (it comes in as both fade), and a knocked hurdle shakes it sideways.
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
        public float sideYaw = 12f;
        [Tooltip("Frame the runner slightly left so more track ahead is visible.")]
        public float leadAhead = 0.6f;
        [Tooltip("Height the camera looks at (m).")]
        public float lookHeight = 0.9f;
        [Tooltip("How tightly the camera follows along the track.")]
        public float followSharpness = 12f;
        [Tooltip("Time (s) the camera's own speed along the track takes to follow the runner's, so a press's surge does not move the picture.")]
        public float trackSpeedSmooth = 0.35f;
        [Tooltip("How strongly (1/s) the camera makes up what it is off along the track.")]
        public float trackPull = 2.5f;
        [Tooltip("The player's distance setting: multiplies distance and leaves the zoom on top of it.")]
        public float distanceScale = 1f;

        [Header("Zoom with speed")]
        [Tooltip("Speed at which the camera is fully back (m/s).")]
        public float zoomFullSpeed = 5.8f;
        [Tooltip("Distance at full zoom, as a multiple of the standing distance.")]
        public float zoomDistance = 1.7f;
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
        float _trackSpeed;
        float _zoom, _zoomVelocity, _pressZoom;
        float _shakeSize, _shakeTime, _shakeStart = -10f;
        float _lastHitWeight;

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
            _trackSpeed = sim != null ? sim.Speed : 0f;
            // At speed it rests as far behind as it runs, so a snap does not start with a slide.
            _position = Desired() - target.forward * (_trackSpeed / Mathf.Max(0.01f, followSharpness));
            transform.position = _position;
            transform.rotation = Facing();
        }

        /// <summary>Unit direction from the runner to the camera on the ground: the right side turned toward the front by <see cref="sideYaw"/>.</summary>
        Vector3 Side() => Quaternion.AngleAxis(-sideYaw, Vector3.up) * target.right;

        Vector3 Desired()
        {
            float back = distance * distanceScale * Mathf.Lerp(1f, zoomDistance, _zoom);
            Vector3 side = Side();
            // The lead is taken along the picture's own horizontal, so the runner sits the same share of the frame left at any yaw.
            Vector3 screenRight = Vector3.Cross(Vector3.up, -side);
            return target.position + screenRight * leadAhead + side * back + Vector3.up * StandingHeight;
        }

        /// <summary>The camera's one fixed height (m): what the pitch gives at full zoom, so the picture at running speed is unchanged and presses never move it.</summary>
        public float StandingHeight => lookHeight + distance * distanceScale * zoomDistance * Mathf.Tan(pitch * Mathf.Deg2Rad);

        Quaternion Facing() => Quaternion.LookRotation(-Side(), Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);

        void LateUpdate()
        {
            if (!target) return;

            float dt = Time.deltaTime;
            var sim = _body ? _body.Sim : null;
            if (sim != null)
            {
                _pressZoom = Mathf.Max(0f, _pressZoom - zoomPressFade * dt);
                float wanted = Mathf.Max(_pressZoom, Mathf.Clamp01(sim.Speed / Mathf.Max(0.01f, zoomFullSpeed)));
                _zoom = Mathf.SmoothDamp(_zoom, wanted, ref _zoomVelocity, wanted > _zoom ? zoomFlowOutTime : zoomFlowInTime);
                if (sim.HitWeight > _lastHitWeight) Shake(hitShake, hitShakeTime);
                _lastHitWeight = sim.HitWeight;
            }

            float t = 1f - Mathf.Exp(-followSharpness * dt);
            Vector3 desired = Desired();
            // Only the track direction and the zoom go through this; the shake stays out of it so it ends exactly.
            if (sim != null)
            {
                // Along the track the camera runs at the runner's smoothed speed, moving by exactly what it moved (no lag that depends on the
                // frame time), and only slowly makes up what it is off. It stays as far behind as it always did at speed
                // (speed / followSharpness), so the framing is the same.
                Vector3 along = target.forward;
                _trackSpeed = Mathf.Lerp(_trackSpeed, sim.Speed, 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, trackSpeedSmooth)));
                desired -= along * (_trackSpeed / Mathf.Max(0.01f, followSharpness));
                Vector3 off = desired - _position;
                float offAlong = Vector3.Dot(off, along);
                // Far off (a change of runner without a snap) it closes as fast as it always did.
                float pull = Mathf.Abs(offAlong) > TrackFarOff ? t : 1f - Mathf.Exp(-trackPull * dt);
                _position += (off - along * offAlong) * t + along * (_trackSpeed * dt + offAlong * pull);
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
