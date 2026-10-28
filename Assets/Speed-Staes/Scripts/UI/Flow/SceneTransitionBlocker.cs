using UnityEngine;
using Zenject;

namespace SpeedStars.UI.Flow
{
    /// <summary>
    /// Locks this scene's UI input while SceneFlow moves between scenes, by switching on a full-screen raycast
    /// target that lies above every canvas (built in the scene, not by code). It reads the state when enabled too,
    /// so the new scene's blocker starts closed while the move is still finishing and opens when it ends. One per
    /// scene, on an always-active object such as UI Root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneTransitionBlocker : MonoBehaviour
    {
        [Tooltip("Full-screen object with a raycast-target Graphic (e.g. an Image with alpha 0) on the top-most canvas. Keep it inactive in the scene; it is active only while a scene move runs.")]
        [SerializeField] private GameObject blocker = null;

        private SceneFlow _flow;
        private bool _subscribed;
        private bool _warnedNoBlocker;

        [Inject]
        public void Construct(SceneFlow flow)
        {
            _flow = flow;
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (!_subscribed) return;
            _flow.TransitionChanged -= Apply;
            _subscribed = false;
        }

        private void Subscribe()
        {
            if (_flow == null || _subscribed) return;
            _flow.TransitionChanged += Apply;
            _subscribed = true;
            Apply(_flow.IsTransitioning);
        }

        private void Apply(bool transitioning)
        {
            if (blocker != null)
            {
                blocker.SetActive(transitioning);
                return;
            }
            if (!_warnedNoBlocker)
            {
                _warnedNoBlocker = true;
                Debug.LogError($"{name}: SceneTransitionBlocker has no blocker object; UI stays tappable during scene moves.", this);
            }
        }
    }
}
