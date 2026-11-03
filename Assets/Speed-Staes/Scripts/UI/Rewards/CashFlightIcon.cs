using System;
using DG.Tweening;
using SpeedStars.UI.Framework;
using UnityEngine;

namespace SpeedStars.UI.Rewards
{
    /// <summary>
    /// The little Cash diamond that flies from where a reward was shown to the Cash counter. It sits inside the prefab of the
    /// screen that uses it (last child, so it draws on top; inactive until it flies), so nothing is created at run time. The
    /// flight only draws: the Cash is in the wallet before it starts, and a cancelled flight loses nothing. Duration, easing
    /// and the arrival punch come from <see cref="UiMotionConfig"/> (the counter numbers), no flight numbers of its own.
    /// </summary>
    public sealed class CashFlightIcon : MonoBehaviour
    {
        [Tooltip("Scale of the diamond at take-off, as a share of its designed scale (it grows to 1 on the way).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float startScale = 0.6f;

        [Tooltip("Height of the sideways-up arc as a share of the flight distance (0 = straight line).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float arcShare = 0.15f;

        private Sequence _flight;
        private Vector3 _baseScale = Vector3.one;
        private bool _scaleKnown;

        /// <summary>True from <see cref="Play"/> until the diamond landed or was cancelled.</summary>
        public bool IsFlying => _flight != null;

        /// <summary>
        /// Flies from <paramref name="from"/> to <paramref name="to"/> and calls <paramref name="landed"/> on arrival. Without a
        /// motion config, or without both ends, nothing flies and <paramref name="landed"/> runs at once (the Cash is already paid).
        /// </summary>
        public void Play(UiMotionConfig motion, RectTransform from, RectTransform to, Action landed)
        {
            Cancel();
            if (motion == null || from == null || to == null)
            {
                landed?.Invoke();
                return;
            }

            if (!_scaleKnown)
            {
                _baseScale = transform.localScale;
                _scaleKnown = true;
            }

            Vector3 start = from.TransformPoint(from.rect.center);
            Vector3 end = to.TransformPoint(to.rect.center);
            Vector3 arc = Vector3.up * ((end - start).magnitude * arcShare);
            float progress = 0f;

            gameObject.SetActive(true);
            Place(start, end, arc, 0f);

            Sequence flight = DOTween.Sequence();
            flight.Append(DOTween.To(() => progress, p =>
            {
                progress = p;
                Place(start, end, arc, p);
            }, 1f, motion.CounterDuration).SetEase(motion.CounterEase));
            flight.SetUpdate(true);
            flight.SetLink(gameObject);
            flight.OnComplete(() =>
            {
                _flight = null;
                Hide();
                landed?.Invoke();
            });
            _flight = flight;
        }

        /// <summary>Stops a running flight without calling the landing callback; the diamond disappears.</summary>
        public void Cancel()
        {
            if (_flight != null)
            {
                Sequence running = _flight;
                _flight = null;
                running.Kill();
            }
            Hide();
        }

        private void Place(Vector3 start, Vector3 end, Vector3 arc, float p)
        {
            transform.position = Vector3.Lerp(start, end, p) + arc * Mathf.Sin(p * Mathf.PI);
            transform.localScale = _baseScale * Mathf.Lerp(startScale, 1f, p);
        }

        private void Hide()
        {
            if (_scaleKnown) transform.localScale = _baseScale;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (_flight == null) return;
            Sequence running = _flight;
            _flight = null;
            running.Kill();
        }

        private void OnDestroy()
        {
            if (_flight == null) return;
            Sequence running = _flight;
            _flight = null;
            running.Kill();
        }
    }
}
