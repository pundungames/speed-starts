using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Physical;
using SpeedStars.Runner;
using SpeedStars.UI.Costume;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Keeps the runner prefab instance standing still in the main menu. Switches off everything that makes it run
    /// (input, sim, replay, tuning HUD) and the pose driver: with no driver the rig targets stay at the prefab's own
    /// rest, which is the upright stance; the driver's "Ready" is the starting-block crouch, wrong for a menu.
    /// Only components on the instance are toggled; Malik's code and the prefab are untouched.
    /// Cosmetics: wears the previewed outfit (selected preset, or what the costume screen shows) through a
    /// <c>CosmeticApplier</c> on the runner and follows <c>ICosmeticPreview.Changed</c>.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class MenuCharacter : MonoBehaviour
    {
        [SerializeField, Tooltip("The runner root (HumanCharacterDummy_F instance). Empty = this object.")]
        GameObject runner;

        bool _applied;
        ICosmeticPreview _preview;
        CosmeticCatalog _catalog;
        ICosmeticService _cosmetics;
        ScreenNavigator _navigator;
        int? _lastPreset;
        CosmeticApplier _applier;
        bool _subscribed;

        const float SpinDegrees = 360f;
        const float ScenePoseHoldDuration = 0.25f;
        const float PresetMotionDuration = 2f;
        SceneFlow _flow;
        bool _flowSubscribed;
        Transform _runnerRoot;
        MenuCharacterSpin _spin;
        MenuIdlePose _idle;
        Transform _camera;
        Quaternion _runnerRest;
        Vector3 _cameraPosition;
        Quaternion _cameraRotation;
        float _motionTime;
        float _motionDuration;
        float _poseHoldDuration;
        bool _leaving;
        bool _moving;

        // Optional so a scene without the cosmetic bindings still shows the (bare) character; Start warns about it.
        [Inject]
        void Construct([InjectOptional] ICosmeticPreview preview, [InjectOptional] CosmeticCatalog catalog,
            [InjectOptional] SceneFlow flow, [InjectOptional] ICosmeticService cosmetics, [InjectOptional] ScreenNavigator navigator)
        {
            _preview = preview;
            _catalog = catalog;
            _flow = flow;
            _cosmetics = cosmetics;
            _navigator = navigator;
            if (isActiveAndEnabled) SubscribeToSceneFlow();
        }

        void Awake() => Initialize();

        void OnEnable()
        {
            Initialize();
            Subscribe();
            SubscribeToSceneFlow();
            _lastPreset = _cosmetics?.SelectedPreset;
            Wear();
        }

        void Start()
        {
            if (!_leaving) BeginMotion(leaving: false);
            if (_preview == null || _catalog == null)
            {
                Debug.LogWarning("[MenuCharacter] ICosmeticPreview / CosmeticCatalog are not bound; the outfit is not applied.", this);
                return;
            }
            Subscribe(); // injection may land after OnEnable in a runtime-instantiated menu
            Wear();
        }

        void OnDisable()
        {
            if (_flowSubscribed) _flow.TransitionChanged -= OnSceneTransition;
            _flowSubscribed = false;
            RestoreMotion();
            if (_subscribed && _preview != null) _preview.Changed -= Wear;
            _subscribed = false;
        }

        void SubscribeToSceneFlow()
        {
            if (_flow == null || _flowSubscribed) return;
            _flow.TransitionChanged += OnSceneTransition;
            _flowSubscribed = true;
        }

        void OnSceneTransition(bool transitioning)
        {
            if (transitioning) BeginMotion(leaving: true);
            else if (_leaving) RestoreMotion();
        }

        void BeginMotion(bool leaving, bool moveCamera = true)
        {
            RestoreMotion();
            _leaving = leaving;
            _runnerRoot = runner ? runner.transform : transform;
            _spin = _runnerRoot.GetComponent<MenuCharacterSpin>();
            _idle = _runnerRoot.GetComponent<MenuIdlePose>();
            _runnerRest = _runnerRoot.localRotation;

            Camera camera = moveCamera ? Camera.main : null;
            _camera = camera && camera.gameObject.scene == gameObject.scene ? camera.transform : null;
            if (_camera)
            {
                _cameraPosition = _camera.position;
                _cameraRotation = _camera.rotation;
            }

            _motionTime = 0f;
            _motionDuration = moveCamera ? UnitySceneLoader.DepartureDuration : PresetMotionDuration;
            _poseHoldDuration = moveCamera ? ScenePoseHoldDuration : 0f;
            _moving = true;
            ApplyMotion(0f);
        }

        void LateUpdate()
        {
            if (!_moving) return;
            _motionTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01((_motionTime - _poseHoldDuration) / (_motionDuration - _poseHoldDuration));
            ApplyMotion(progress);
            if (progress >= 1f && !_leaving) RestoreMotion();
        }

        void ApplyMotion(float progress)
        {
            float direction = _leaving ? 1f : -1f;
            // Show the front, profile and back at a readable speed, then ease the closing 45 degrees.
            float closing = Mathf.Clamp01((progress - 0.5f) * 2f);
            float easeOutExpo = (1f - Mathf.Pow(2f, -10f * closing)) / (1f - 1f / 1024f);
            float spin = 0.875f * Mathf.Min(progress * 2f, 1f) + 0.125f * easeOutExpo;
            float runWeight = Mathf.SmoothStep(0f, 1f, progress / 0.08f)
                * (1f - Mathf.SmoothStep(0f, 1f, (progress - 0.53f) / 0.35f));
            Quaternion turn = Quaternion.Euler(6f * runWeight, SpinDegrees * direction * spin, -4f * direction * runWeight);
            if (_spin) _spin.SetTransitionRotation(turn);
            else _runnerRoot.localRotation = _runnerRest * turn;
            if (_idle)
            {
                float phase = (2.7f * progress - 0.9f * progress * progress) * (_motionDuration - _poseHoldDuration);
                _idle.SetTransitionRun(runWeight, phase);
            }

            if (_camera)
            {
                float amount = _leaving ? spin : 1f - spin;
                Quaternion orbit = Quaternion.AngleAxis(8f * direction * amount, Vector3.up);
                Vector3 pivot = _runnerRoot.position;
                _camera.SetPositionAndRotation(pivot + orbit * (_cameraPosition - pivot), orbit * _cameraRotation);
            }
        }

        void RestoreMotion()
        {
            if (!_moving) return;
            if (_spin) _spin.SetTransitionRotation(Quaternion.identity);
            else if (_runnerRoot) _runnerRoot.localRotation = _runnerRest;
            if (_idle) _idle.SetTransitionRun(0f, 0f);
            if (_camera) _camera.SetPositionAndRotation(_cameraPosition, _cameraRotation);
            _moving = false;
            _leaving = false;
        }

        void Subscribe()
        {
            if (_subscribed || _preview == null) return;
            _preview.Changed += Wear;
            _subscribed = true;
        }

        void Wear()
        {
            if (_preview == null || _catalog == null) return;
            var root = runner ? runner : gameObject;
            if (!_applier) _applier = root.GetComponent<CosmeticApplier>() ?? root.AddComponent<CosmeticApplier>();
            _applier.Apply(_preview.Current, _catalog);

            int? preset = _cosmetics?.SelectedPreset;
            bool changedPreset = _lastPreset.HasValue && preset.HasValue && _lastPreset.Value != preset.Value;
            _lastPreset = preset;
            if (!changedPreset || _leaving || !IsPresetScreen()) return;

            BeginMotion(leaving: false, moveCamera: false);
        }

        bool IsPresetScreen() =>
            _navigator?.CurrentScreen is HomeScreenView || _navigator?.CurrentScreen is CostumeEditorScreenView;

        /// <summary>Idempotent; also callable from tests or a builder (AddComponent skips Awake in EditMode).</summary>
        public void Initialize()
        {
            if (_applied) return;
            Freeze(runner ? runner : gameObject);
            _applied = true;
        }

        /// <summary>
        /// Stops a runner copy for the menu: input, sim, replay, tuning HUD and pose driver off, relaxed arms added. Shared with the
        /// relay stage (<see cref="MenuRelayTeam"/>) so every standing runner is stopped the same way. Safe to call on an inactive copy.
        /// </summary>
        public static void Freeze(GameObject root)
        {
            // Awake of a disabled component still runs, so the driver binds the rig (arm IK off) before it is silenced.
            Disable<RunnerInput>(root);
            Disable<RunnerReplay>(root);
            Disable<RunnerBodyHUD>(root);
            Disable<RunnerPoseDriver>(root);
            var body = root.GetComponentInChildren<RunnerBody>(true);
            if (body)
            {
                body.acceptInput = false;
                body.enabled = false;
            }
            // Relaxed arms instead of the T-pose; added on the runner so no scene wiring is needed. Menu instance only.
            if (!root.GetComponent<MenuIdlePose>()) root.AddComponent<MenuIdlePose>().Initialize();
        }

        static void Disable<T>(GameObject root) where T : Behaviour
        {
            foreach (var c in root.GetComponentsInChildren<T>(true))
                if (c) c.enabled = false;
        }
    }
}
