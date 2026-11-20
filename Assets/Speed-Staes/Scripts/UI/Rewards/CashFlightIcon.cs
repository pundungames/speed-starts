using System;
using DG.Tweening;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;

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

        /// <summary>Reuses the Cash art for a single burst starting at the screen center.</summary>
        public void PlayBurst(RectTransform target, int amount, Action<int, int> landed)
        {
            Cancel();
            Image template = GetComponent<Image>();
            Canvas canvas = GetComponentInParent<Canvas>();
            if (!target || !template || !template.sprite || !canvas || amount <= 0)
            {
                landed?.Invoke(1, 1);
                return;
            }

            RectTransform area = (RectTransform)canvas.rootCanvas.transform;
            RectTransform origin = (RectTransform)transform;
            Vector3 center = area.TransformPoint(area.rect.center);
            Vector3 arc = origin.TransformVector(Vector3.up * 52f);
            RectTransform targetIcon = target.Find("Icon") as RectTransform;
            if (!targetIcon) targetIcon = target;

            int count = Mathf.Clamp(amount, 3, 9);
            var icons = new GameObject[count];
            gameObject.SetActive(true);
            template.enabled = false;
            Sequence flight = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            for (int i = 0; i < count; i++)
            {
                const float spreadTime = 0.20f;
                float travel = 0.58f + i * 0.032f;
                // The golden angle spreads icons into a cluster without forming radial rows.
                float angle = i * 2.399963f;
                float radius = i == 0 ? 0f : 1.85f * Mathf.Sqrt((i + 1f) / count);
                Vector3 offset = origin.TransformVector(new Vector3(
                    Mathf.Cos(angle) * origin.rect.width * radius,
                    Mathf.Sin(angle) * origin.rect.height * radius, 0f));
                Vector3 start = center + offset * 0.55f;
                Vector3 scattered = center + offset;
                int arrival = i + 1;
                var clip = new GameObject("Race Reward Diamond", typeof(RectTransform), typeof(Image));
                icons[i] = clip;
                var rect = (RectTransform)clip.transform;
                rect.SetParent(origin, false);
                rect.sizeDelta = origin.rect.size * 1.35f;
                rect.position = start;
                rect.localScale = Vector3.one * 0.72f;
                Image image = clip.GetComponent<Image>();
                image.sprite = template.sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                flight.Insert(0f, DOVirtual.Float(0f, 1f, spreadTime, progress =>
                {
                    rect.position = Vector3.Lerp(start, scattered, progress);
                    rect.localScale = Vector3.one * Mathf.Lerp(0.72f, 1.18f, progress);
                }).SetEase(Ease.OutCubic));
                flight.Insert(spreadTime, DOVirtual.Float(0f, 1f, travel, progress =>
                {
                    if (!targetIcon) return;
                    Vector3 end = targetIcon.TransformPoint(targetIcon.rect.center);
                    rect.position = Vector3.Lerp(scattered, end, progress) + arc * Mathf.Sin(Mathf.PI * progress);
                    rect.localScale = Vector3.one * Mathf.Lerp(1.18f, 0.30f, progress);
                }).SetEase(Ease.InOutSine));
                flight.InsertCallback(spreadTime + travel, () =>
                {
                    clip.SetActive(false);
                    landed?.Invoke(arrival, count);
                });
            }
            flight.OnComplete(() =>
            {
                _flight = null;
                Hide();
            });
            flight.OnKill(() =>
            {
                if (template) template.enabled = true;
                foreach (GameObject clip in icons)
                    if (clip) Destroy(clip);
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
