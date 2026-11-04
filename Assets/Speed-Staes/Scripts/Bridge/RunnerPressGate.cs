using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Physical;
using SpeedStars.Runner;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Sits between the player's RunnerInput and RunnerBody (their execution orders are -200 and -100), so Malik's input stays as it
    /// is: a finger that came down on a button is not a step, and a press during the countdown is a false start, reported to the
    /// session; the runner then falls with both feet pressed from here, and nothing the player does counts until the countdown is
    /// back. Every other event goes back into the queue in order, with its own time. Added by RaceSessionAdapter, player only.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class RunnerPressGate : MonoBehaviour
    {
        const int LeftLeg = 0;
        const int RightLeg = 1;

        RaceSessionAdapter _session;
        readonly IgnoredTapFilter _filter = new IgnoredTapFilter();
        readonly List<TapEvent> _kept = new List<TapEvent>();

        void Awake() => _session = GetComponent<RaceSessionAdapter>();

        RunnerInput PlayerInput
        {
            get
            {
                RunnerBody body = _session ? _session.PlayerBody : null;
                return body ? body.input : null;
            }
        }

        void Update()
        {
            RunnerInput input = PlayerInput;
            if (!input) return;

            bool fall = false;
            _kept.Clear();
            while (input.TryDequeue(out TapEvent tap))
            {
                if (_filter.ShouldDrop(tap.Leg, tap.Pressed) || _session.IsFailing) continue;
                if (tap.Pressed && _session.IsCountingDown)
                {
                    fall = _session.ReportFalseStart();
                    continue;
                }
                _kept.Add(tap);
            }
            foreach (TapEvent tap in _kept) input.Inject(tap.Leg, tap.Pressed, tap.Time);
            _filter.Clear();

            // Both feet down is the runner's dive. RunnerBody runs right after this and takes them in the same frame, so they never meet this gate.
            if (!fall) return;
            input.Inject(LeftLeg, true, Time.timeAsDouble);
            input.Inject(RightLeg, true, Time.timeAsDouble);
        }

#if ENABLE_INPUT_SYSTEM
        readonly Dictionary<int, int> _buttonFingers = new Dictionary<int, int>();
        readonly List<RaycastResult> _hits = new List<RaycastResult>();
        PointerEventData _pointer;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            ETouch.onFingerDown += OnFingerDown;
            ETouch.onFingerUp += OnFingerUp;
        }

        void OnDisable()
        {
            ETouch.onFingerDown -= OnFingerDown;
            ETouch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();
            _buttonFingers.Clear();
        }

        void OnFingerDown(Finger finger)
        {
            RunnerInput input = PlayerInput;
            if (!input || !input.isActiveAndEnabled || !IsOnButton(finger.screenPosition)) return;
            int leg = finger.screenPosition.x < Screen.width * 0.5f ? LeftLeg : RightLeg; // RunnerInput's own rule for the side
            _buttonFingers[finger.index] = leg;
            _filter.IgnorePress(leg);
        }

        void OnFingerUp(Finger finger)
        {
            if (!_buttonFingers.TryGetValue(finger.index, out int leg)) return;
            _buttonFingers.Remove(finger.index);
            _filter.IgnoreRelease(leg);
        }

        /// <summary>Only what takes a press counts (the topmost hit): the HUD's plain graphics must not eat the steps under them.</summary>
        bool IsOnButton(Vector2 screenPosition)
        {
            EventSystem events = EventSystem.current;
            if (!events) return false;
            if (_pointer == null || _pointer.currentInputModule != events.currentInputModule) _pointer = new PointerEventData(events);
            _pointer.position = screenPosition;
            _hits.Clear();
            events.RaycastAll(_pointer, _hits);
            return _hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerDownHandler>(_hits[0].gameObject) != null;
        }
#endif
    }
}
