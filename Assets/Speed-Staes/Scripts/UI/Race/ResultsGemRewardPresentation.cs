using System;
using System.Globalization;
using DG.Tweening;
using SpeedStars.UI.Rewards;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Race
{
    /// <summary>Displays a gem payout that has already been credited to the wallet.</summary>
    internal sealed class ResultsGemRewardPresentation
    {
        private readonly RectTransform _screen;
        private readonly Texture2D _gem;
        private readonly Rect _gemUv;
        private readonly Sprite _counterBackground;
        private readonly RectTransform _badgeIcon;
        private readonly Font _font;
        private Sequence _settlement;

        public ResultsGemRewardPresentation(RectTransform screen, Text rewardText, Texture2D gem, Rect gemUv, Sprite counterBackground)
        {
            _screen = screen;
            _gem = gem;
            _gemUv = gemUv;
            _counterBackground = counterBackground;
            _font = rewardText.font;

            RectTransform badge = rewardText.rectTransform.parent as RectTransform;
            _badgeIcon = MakeGem("Reward Icon", badge, 56f);
            Place(_badgeIcon, new Vector2(0.5f, 0.5f), new Vector2(-163f, 0f), _badgeIcon.sizeDelta);
            RectTransform label = rewardText.rectTransform;
            Place(label, new Vector2(0.5f, 0.5f), new Vector2(52f, 0f), new Vector2(360f, 72f));
            rewardText.alignment = TextAnchor.MiddleLeft;
        }

        public void Play(int amount, int balance, RectTransform cashBox, Action settled)
        {
            Stop();
            if (amount <= 0 || !_counterBackground) return;

            var layer = new GameObject("Purple Gem Settlement", typeof(RectTransform));
            var area = (RectTransform)layer.transform;
            area.SetParent(_screen, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;

            var counterObject = new GameObject("Purple Gem Counter", typeof(RectTransform), typeof(Image));
            var counter = (RectTransform)counterObject.transform;
            counter.SetParent(area, false);
            Image background = counterObject.GetComponent<Image>();
            background.sprite = _counterBackground;
            background.raycastTarget = false;

            var counterSize = new Vector2(198f, 83f);
            var visiblePosition = new Vector2(444f, -114f);
            if (cashBox)
            {
                Vector2 cashRight = area.InverseTransformPoint(cashBox.TransformPoint(new Vector3(cashBox.rect.xMax, cashBox.rect.center.y)));
                visiblePosition = new Vector2(cashRight.x - area.rect.xMin + counterSize.x * 0.5f + 12f, cashRight.y - area.rect.yMax);
            }
            var hiddenPosition = new Vector2(-counterSize.x * 0.5f - 8f, visiblePosition.y);
            Place(counter, new Vector2(0f, 1f), hiddenPosition, counterSize);

            RectTransform destination = MakeGem("Counter Icon", counter, 47f);
            Place(destination, new Vector2(0.5f, 0.5f), new Vector2(-56f, 0f), destination.sizeDelta);
            var valueObject = new GameObject("Balance", typeof(RectTransform), typeof(Text));
            var value = valueObject.GetComponent<Text>();
            value.rectTransform.SetParent(counter, false);
            Place(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(128f, 58f));
            value.font = _font;
            value.fontSize = value.resizeTextMaxSize = 39;
            value.resizeTextMinSize = 20;
            value.resizeTextForBestFit = true;
            value.color = new Color32(10, 31, 74, 255);
            value.alignment = TextAnchor.MiddleCenter;
            value.raycastTarget = false;

            balance = Mathf.Max(0, balance);
            int before = Mathf.Max(0, balance - amount);
            value.text = before.ToString(CultureInfo.InvariantCulture);

            const float slideSeconds = 0.34f;
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(layer);
            sequence.Append(DOTween.To(() => counter.anchoredPosition, position => counter.anchoredPosition = position, visiblePosition, slideSeconds).SetEase(Ease.OutCubic));

            Vector3 source = _badgeIcon.TransformPoint(_badgeIcon.rect.center);
            int count = Mathf.Clamp(amount, 3, 8);
            for (int i = 0; i < count; i++)
            {
                int arrival = i + 1;
                RectTransform gem = MakeGem("Flying Purple Gem", area, 52f);
                CashFlightIcon.AddBurstIcon(sequence, gem, area, source, new Vector2(35f, 26f),
                    () => destination.TransformPoint(destination.rect.center), i, count, slideSeconds + i * 0.04f, () =>
                    {
                        int current = before + (int)((long)(balance - before) * arrival / count);
                        value.text = current.ToString(CultureInfo.InvariantCulture);
                        if (arrival == count) settled?.Invoke();
                    });
            }

            sequence.Append(counter.DOPunchScale(Vector3.one * 0.07f, 0.24f, 4));
            sequence.AppendInterval(0.35f);
            sequence.Append(DOTween.To(() => counter.anchoredPosition, position => counter.anchoredPosition = position, hiddenPosition, slideSeconds).SetEase(Ease.InCubic));
            sequence.OnKill(() =>
            {
                if (_settlement == sequence) _settlement = null;
                if (layer)
                {
                    layer.SetActive(false);
                    UnityEngine.Object.Destroy(layer);
                }
            });
            _settlement = sequence;
        }

        public void Stop()
        {
            _settlement?.Kill();
        }

        private RectTransform MakeGem(string name, RectTransform parent, float width)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            var image = item.GetComponent<RawImage>();
            image.texture = _gem;
            image.uvRect = _gemUv;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.SetParent(parent, false);
            float aspect = _gemUv.width * _gem.width / (_gemUv.height * _gem.height);
            rect.sizeDelta = new Vector2(width, width / aspect);
            return rect;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
