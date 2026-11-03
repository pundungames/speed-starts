using UnityEngine;
using SpeedStars.Meta.Contracts;
using SpeedStars.Physical;
using Zenject;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Side-on race camera: sits to the runner's right and tracks along the run axis only,
    /// so step bob and pelvis sway never shake the frame. Three things come from the race itself and touch only
    /// the picture: each press sends the camera back, flowing, and the speed holds it there (it comes in as both fade), and a wrong
    /// step or a knocked hurdle shakes it.
    /// </summary>
    public sealed class SideFollowCamera : MonoBehaviour
    {
        [Tooltip("The runner to follow.")]
        public Transform target;
        [Tooltip("Distance from the runner standing still (m).")]
        public float distance = 4.5f;
        [Tooltip("How far the camera looks down at the runner (deg). The height follows from it, so the angle stays the same at any distance or zoom.")]
        public float pitch = 8f;
        [Tooltip("Frame the runner slightly left so more track ahead is visible.")]
        public float leadAhead = 0.6f;
        [Tooltip("Height the camera looks at (m).")]
        public float lookHeight = 0.9f;
        [Tooltip("How tightly the camera follows along the track.")]
        public float followSharpness = 12f;
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
        public float zoomOutTime = 0.33f;
        [Tooltip("Time to flow in (s).")]
        public float zoomInTime = 0.17f;

        [Header("Shake")]
        [Tooltip("Shake size on a wrong step, early or late (m).")]
        public float wrongStepShake = 0.03f;
        [Tooltip("Shake length on a wrong step (s).")]
        public float wrongStepShakeTime = 0.15f;
        [Tooltip("Shake size on a knocked hurdle (m).")]
        public float hitShake = 0.12f;
        [Tooltip("Shake length on a knocked hurdle (s).")]
        public float hitShakeTime = 0.3f;
        [Tooltip("How fast the shake moves (noise steps per second).")]
        public float shakeRate = 30f;

        RunnerBody _body;
        Vector3 _position;
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
            PressResult result = _body.Sim.LastResult;
            if (result == PressResult.Early || result == PressResult.Late) Shake(wrongStepShake, wrongStepShakeTime);
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
            _position = Desired();
            transform.position = _position;
            transform.rotation = Facing();
        }

        Vector3 Desired()
        {
            float back = distance * distanceScale * Mathf.Lerp(1f, zoomDistance, _zoom);
            float up = lookHeight + back * Mathf.Tan(pitch * Mathf.Deg2Rad);
            return target.position + target.forward * leadAhead + target.right * back + Vector3.up * up;
        }

        /// <summary>Camera height standing still at the normal distance (m).</summary>
        public float StandingHeight => lookHeight + distance * Mathf.Tan(pitch * Mathf.Deg2Rad);

        Quaternion Facing() => Quaternion.LookRotation(-target.right, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);

        void LateUpdate()
        {
            if (!target) return;

            var sim = _body ? _body.Sim : null;
            if (sim != null)
            {
                _pressZoom = Mathf.Max(0f, _pressZoom - zoomPressFade * Time.deltaTime);
                float wanted = Mathf.Max(_pressZoom, Mathf.Clamp01(sim.Speed / Mathf.Max(0.01f, zoomFullSpeed)));
                _zoom = Mathf.SmoothDamp(_zoom, wanted, ref _zoomVelocity, wanted > _zoom ? zoomOutTime : zoomInTime);
                if (sim.HitWeight > _lastHitWeight) Shake(hitShake, hitShakeTime);
                _lastHitWeight = sim.HitWeight;
            }

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            // Only the track direction and the zoom go through this; the shake stays out of it so it ends exactly.
            _position = Vector3.Lerp(_position, Desired(), t);

            Current(out float size);
            float n = Time.time * shakeRate;
            Vector3 shake = transform.right * ((Mathf.PerlinNoise(n, 0.3f) - 0.5f) * 2f * size)
                + Vector3.up * ((Mathf.PerlinNoise(0.7f, n) - 0.5f) * 2f * size);

            transform.position = _position + shake;
            transform.rotation = Facing();
        }
    }
}
