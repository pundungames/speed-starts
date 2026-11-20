using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Rewards
{
    /// <summary>The reward burst hosted by a screen-owned image. It only animates the display.</summary>
    public sealed class CashFlightIcon : MonoBehaviour
    {
        private Sequence _flight;

        public bool IsFlying => _flight != null;

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
            RectTransform targetIcon = target.Find("Icon") as RectTransform;
            if (!targetIcon) targetIcon = target;

            int count = Mathf.Clamp(amount, 3, 9);
            var icons = new GameObject[count];
            gameObject.SetActive(true);
            template.enabled = false;
            Sequence flight = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            for (int i = 0; i < count; i++)
            {
                int arrival = i + 1;
                var clip = new GameObject("Race Reward Diamond", typeof(RectTransform), typeof(Image));
                icons[i] = clip;
                var rect = (RectTransform)clip.transform;
                rect.SetParent(origin, false);
                rect.sizeDelta = origin.rect.size * 1.35f;
                Image image = clip.GetComponent<Image>();
                image.sprite = template.sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                AddBurstIcon(flight, rect, origin, center, origin.rect.size,
                    () => targetIcon ? targetIcon.TransformPoint(targetIcon.rect.center) : center,
                    i, count, 0f, () => landed?.Invoke(arrival, count));
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

        internal static void AddBurstIcon(Sequence flight, RectTransform icon, RectTransform space, Vector3 center,
            Vector2 scatterSize, Func<Vector3> destination, int index, int count, float delay, Action landed)
        {
            const float spreadTime = 0.20f;
            float travel = 0.58f + index * 0.032f;
            float angle = index * 2.399963f;
            float radius = index == 0 ? 0f : 1.85f * Mathf.Sqrt((index + 1f) / count);
            Vector3 offset = space.TransformVector(new Vector3(
                Mathf.Cos(angle) * scatterSize.x * radius, Mathf.Sin(angle) * scatterSize.y * radius, 0f));
            Vector3 start = center + offset * 0.55f;
            Vector3 scattered = center + offset;
            Vector3 arc = space.TransformVector(Vector3.up * 52f);
            Vector3 restScale = icon.localScale;
            icon.position = start;
            icon.localScale = restScale * 0.72f;
            icon.gameObject.SetActive(false);
            flight.InsertCallback(delay, () => icon.gameObject.SetActive(true));
            flight.Insert(delay, DOVirtual.Float(0f, 1f, spreadTime, progress =>
            {
                icon.position = Vector3.Lerp(start, scattered, progress);
                icon.localScale = restScale * Mathf.Lerp(0.72f, 1.18f, progress);
            }).SetEase(Ease.OutCubic));
            flight.Insert(delay + spreadTime, DOVirtual.Float(0f, 1f, travel, progress =>
            {
                icon.position = Vector3.Lerp(scattered, destination(), progress) + arc * Mathf.Sin(Mathf.PI * progress);
                icon.localScale = restScale * Mathf.Lerp(1.18f, 0.30f, progress);
            }).SetEase(Ease.InOutSine));
            flight.InsertCallback(delay + spreadTime + travel, () =>
            {
                icon.gameObject.SetActive(false);
                icon.localScale = restScale;
                landed?.Invoke();
            });
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

        private void Hide()
        {
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
