using SpeedStars.Meta.Contracts;
using SpeedStars.Themes;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>Switches the showcased world and runs the camera patrol while the theme shop is open.</summary>
    public sealed class MenuThemeView : MonoBehaviour
    {
        [Tooltip("The showcase on the scene root. Its theme order must match the catalog showcase indices.")]
        [SerializeField] private MainMenuThemeShowcase showcase;

        [Tooltip("The character and plinth, hidden while the theme shop is open.")]
        [SerializeField] private GameObject characterPlatform;

        // The shorter forward leg keeps the camera clear of the shared track's open end.
        private const float BackwardDistance = 52f;
        private const float ForwardDistance = 22f;
        private const float PatrolSpan = BackwardDistance + ForwardDistance;
        private const float PatrolMidpoint = (ForwardDistance - BackwardDistance) * 0.5f;
        private const float PatrolRadius = PatrolSpan * 0.5f;
        // Keep the same cruise speed across the asymmetric route.
        private const float HalfPatrolSeconds = 18f * PatrolSpan / BackwardDistance;
        private const float EnterSeconds = 2.4f;
        private const float ReturnSeconds = 1f;
        private const float PatrolRise = 2.3f;
        private const float LookAlongTrack = 20f;

        private IThemeService _themes;
        private Transform _camera;
        private Pose _menuPose;
        private Pose _blendStart;
        private float _patrolTime;
        private float _inwardYaw;
        private float _blendTime;
        private bool _subscribed;
        private bool _browsing;
        private bool _returning;
        private bool _entering;

        [Inject]
        public void Construct(IThemeService themes)
        {
            _themes = themes;
            Initialize();
        }

        private void Awake() => Initialize();
        private void OnEnable() => Initialize();

        /// <summary>Idempotent and safe to call before injection.</summary>
        public void Initialize()
        {
            if (_themes == null || showcase == null) return;
            if (!_camera && showcase.previewCamera) _camera = showcase.previewCamera.transform;
            if (!_subscribed)
            {
                _themes.ShownChanged += ApplyTheme;
                _themes.BrowsingChanged += ApplyBrowsing;
                _subscribed = true;
            }
            ApplyTheme();
            ApplyBrowsing();
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _themes.ShownChanged -= ApplyTheme;
                _themes.BrowsingChanged -= ApplyBrowsing;
                _subscribed = false;
            }
            if (_browsing || _returning) SetCamera(_menuPose);
            _browsing = _returning = _entering = false;
        }

        private void ApplyTheme()
        {
            if (!showcase) return;
            bool preserveCamera = _camera && (_browsing || _returning);
            Pose pose = preserveCamera ? ReadCamera() : default;
            showcase.SelectTheme(_themes.ShownShowcaseIndex);
            if (preserveCamera) SetCamera(pose);
        }

        private void ApplyBrowsing()
        {
            bool browsing = _themes.IsBrowsing;
            if (characterPlatform) characterPlatform.SetActive(!browsing);
            if (!_camera || browsing == _browsing) return;

            _blendStart = ReadCamera();
            _blendTime = 0f;
            _entering = browsing;
            if (browsing && !_returning)
            {
                _menuPose = _blendStart;
                Vector3 menuForward = _menuPose.rotation * Vector3.forward;
                _inwardYaw = Vector3.SignedAngle(Vector3.ProjectOnPlane(menuForward, Vector3.up), Vector3.left, Vector3.up);
                _patrolTime = Mathf.Acos(-PatrolMidpoint / PatrolRadius) * HalfPatrolSeconds / Mathf.PI;
            }
            _browsing = browsing;
            _returning = !browsing;
        }

        private void LateUpdate()
        {
            if (!_camera || (!_browsing && !_returning)) return;
            float delta = Time.unscaledDeltaTime;
            Pose target = _menuPose;
            if (_browsing)
            {
                _patrolTime = Mathf.Repeat(_patrolTime + delta, 2f * HalfPatrolSeconds);
                float phase = Mathf.PI * _patrolTime / HalfPatrolSeconds;
                float offset = PatrolMidpoint + PatrolRadius * Mathf.Cos(phase);
                float backward = Mathf.Clamp01(-offset / BackwardDistance);
                float forward = Mathf.Clamp01(offset / ForwardDistance);
                // The cosine path's velocity is proportional to -sin(phase):
                // look ahead while in motion and face inward as the camera changes direction.
                float yaw = _inwardYaw - Mathf.Sin(phase) * LookAlongTrack;
                float rise = PatrolRise * backward + 1.4f * forward;
                target = new Pose(
                    _menuPose.position + new Vector3(0f, rise, offset),
                    Quaternion.AngleAxis(yaw, Vector3.up) * _menuPose.rotation);
            }
            if (_returning || _entering)
            {
                _blendTime += delta;
                float blendDuration = _returning ? ReturnSeconds : EnterSeconds;
                float eased = Mathf.SmoothStep(0f, 1f, _blendTime / blendDuration);
                target = new Pose(Vector3.Lerp(_blendStart.position, target.position, eased),
                    Quaternion.Slerp(_blendStart.rotation, target.rotation, eased));
                if (_blendTime >= blendDuration) _returning = _entering = false;
            }
            SetCamera(target);
        }

        private Pose ReadCamera() => new Pose(_camera.position, _camera.rotation);

        private void SetCamera(Pose pose)
        {
            if (_camera) _camera.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
