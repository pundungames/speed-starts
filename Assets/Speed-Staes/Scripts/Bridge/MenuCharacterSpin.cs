using System.Collections.Generic;
using SpeedStars.Core.Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Drag horizontally anywhere outside the UI to turn the menu character about its own vertical axis; on release it
    /// coasts to a stop and stays at that angle. It turns the runner ROOT (the transform MenuCharacter points at), not a bone:
    /// MenuIdlePose / MenuEmotePlayer write bones in LateUpdate and work in runner space (they read the root rotation every
    /// frame), and the root is not touched by the rig, so the turn never fights the pose. The component sits on the runner,
    /// which lives under the character platform, so it is inactive while the theme screen hides the platform.
    /// </summary>
    public sealed class MenuCharacterSpin : MonoBehaviour
    {
        [SerializeField, Tooltip("Transform that turns. Empty = this object (the runner root).")]
        Transform target;

        [SerializeField, Tooltip("Yaw for a drag across the full screen width (deg). Same swipe = same turn on any screen.")]
        float degreesPerScreen = 360f;

        [SerializeField, Tooltip("Coast decay k in exp(-k t) after release. Bigger = stops sooner.")]
        float decayPerSecond = 4f;

        [SerializeField, Tooltip("Below this speed (deg/s) the coast stops at once.")]
        float stopSpeed = 4f;

        [SerializeField, Tooltip("Smoothing time (s) of the release speed, so a flick uses the last moments of the drag.")]
        float velocitySmoothing = 0.06f;

        SpinMotion _motion;
        Quaternion _restLocal;
        bool _initialized;
        bool _dragging;
        float _lastX;
        readonly List<RaycastResult> _hits = new List<RaycastResult>();
        PointerEventData _pointerData;

        void Awake() => Initialize();
        void OnEnable() => Initialize();

        /// <summary>Idempotent; callable where AddComponent skips Awake.</summary>
        public void Initialize()
        {
            if (_initialized) return;
            if (!target) target = transform;
            _restLocal = target.localRotation;
            _motion = new SpinMotion(degreesPerScreen, decayPerSecond, stopSpeed, velocitySmoothing);
            _initialized = true;
        }

        void OnDisable()
        {
            // Hidden mid-drag (theme screen): drop the grab, keep the angle.
            if (_dragging) _motion.EndDrag();
            _dragging = false;
        }

        void Update()
        {
            if (!_initialized) Initialize();
            var pointer = Pointer.current;
            if (pointer != null)
            {
                Vector2 pos = pointer.position.ReadValue();
                if (pointer.press.wasPressedThisFrame && !IsOverUi(pos))
                {
                    _dragging = true;
                    _lastX = pos.x;
                    _motion.BeginDrag();
                }
                else if (_dragging && pointer.press.wasReleasedThisFrame)
                {
                    _dragging = false;
                    _motion.EndDrag();
                }
                else if (_dragging && !pointer.press.isPressed)
                {
                    _dragging = false; // device switched or press lost
                    _motion.EndDrag();
                }
            }

            float dt = Time.unscaledDeltaTime;
            if (_dragging && pointer != null)
            {
                float x = pointer.position.ReadValue().x;
                _motion.Drag(x - _lastX, Screen.width, dt);
                _lastX = x;
            }
            else
            {
                _motion.Step(dt);
            }

            target.localRotation = _restLocal * Quaternion.AngleAxis(-_motion.Yaw, Vector3.up);
        }

        /// <summary>True when any raycast-target UI element is under the point (a button, popup, panel).</summary>
        bool IsOverUi(Vector2 screenPosition)
        {
            var system = EventSystem.current;
            if (system == null) return false;
            if (_pointerData == null) _pointerData = new PointerEventData(system);
            _pointerData.position = screenPosition;
            _hits.Clear();
            system.RaycastAll(_pointerData, _hits);
            return _hits.Count > 0;
        }
    }
}
