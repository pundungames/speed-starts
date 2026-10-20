using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace SpeedStars.Runner
{
    public struct TapEvent
    {
        /// <summary>0 = left foot, 1 = right foot.</summary>
        public int Leg;
        /// <summary>True = pressed, false = released.</summary>
        public bool Pressed;
        /// <summary>Game time (Time.timeAsDouble timeline) of the press/release itself, not of the frame.</summary>
        public double Time;
    }

    /// <summary>
    /// Collects left/right press and release events with their own timestamps.
    /// Keyboard (← left, → right) and touch (left/right half of the screen; a finger keeps
    /// the side it started on until it lifts). Nothing here judges or moves anything.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class RunnerInput : MonoBehaviour
    {
        readonly Queue<TapEvent> _queue = new Queue<TapEvent>();

        public bool TryDequeue(out TapEvent tap)
        {
            if (_queue.Count > 0)
            {
                tap = _queue.Dequeue();
                return true;
            }
            tap = default;
            return false;
        }

        public void Clear() => _queue.Clear();

#if ENABLE_INPUT_SYSTEM
        InputAction _left, _right;
        readonly Dictionary<int, int> _fingerLeg = new Dictionary<int, int>();

        void OnEnable()
        {
            _left = new InputAction("LeftFoot", InputActionType.Button);
            _left.AddBinding("<Keyboard>/leftArrow");
            _left.performed += ctx => Enqueue(0, true, ctx.time);
            _left.canceled += ctx => Enqueue(0, false, ctx.time);
            _left.Enable();

            _right = new InputAction("RightFoot", InputActionType.Button);
            _right.AddBinding("<Keyboard>/rightArrow");
            _right.performed += ctx => Enqueue(1, true, ctx.time);
            _right.canceled += ctx => Enqueue(1, false, ctx.time);
            _right.Enable();

            EnhancedTouchSupport.Enable();
            ETouch.onFingerDown += OnFingerDown;
            ETouch.onFingerUp += OnFingerUp;
        }

        void OnDisable()
        {
            ETouch.onFingerDown -= OnFingerDown;
            ETouch.onFingerUp -= OnFingerUp;
            _left?.Dispose();
            _right?.Dispose();
            _fingerLeg.Clear();
        }

        void OnFingerDown(Finger finger)
        {
            int leg = finger.screenPosition.x < Screen.width * 0.5f ? 0 : 1;
            _fingerLeg[finger.index] = leg;
            Enqueue(leg, true, finger.currentTouch.startTime);
        }

        void OnFingerUp(Finger finger)
        {
            if (!_fingerLeg.TryGetValue(finger.index, out int leg)) return;
            _fingerLeg.Remove(finger.index);
            Enqueue(leg, false, finger.currentTouch.time);
        }

        /// <summary>Input System times are on the realtimeSinceStartup timeline; move them onto game time.</summary>
        void Enqueue(int leg, bool pressed, double realtime)
        {
            double age = System.Math.Max(0.0, Time.realtimeSinceStartupAsDouble - realtime);
            _queue.Enqueue(new TapEvent { Leg = leg, Pressed = pressed, Time = Time.timeAsDouble - age });
        }
#else
        readonly Dictionary<int, int> _fingerLeg = new Dictionary<int, int>();

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) Enqueue(0, true);
            if (Input.GetKeyUp(KeyCode.LeftArrow)) Enqueue(0, false);
            if (Input.GetKeyDown(KeyCode.RightArrow)) Enqueue(1, true);
            if (Input.GetKeyUp(KeyCode.RightArrow)) Enqueue(1, false);
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    int leg = t.position.x < Screen.width * 0.5f ? 0 : 1;
                    _fingerLeg[t.fingerId] = leg;
                    Enqueue(leg, true);
                }
                else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && _fingerLeg.TryGetValue(t.fingerId, out int upLeg))
                {
                    _fingerLeg.Remove(t.fingerId);
                    Enqueue(upLeg, false);
                }
            }
        }

        // Legacy input has no event time: frame-quantized.
        void Enqueue(int leg, bool pressed) => _queue.Enqueue(new TapEvent { Leg = leg, Pressed = pressed, Time = Time.timeAsDouble });
#endif

        /// <summary>Inject a press or release (tests / bots).</summary>
        public void Inject(int leg, bool pressed, double gameTime) =>
            _queue.Enqueue(new TapEvent { Leg = leg, Pressed = pressed, Time = gameTime });
    }
}
