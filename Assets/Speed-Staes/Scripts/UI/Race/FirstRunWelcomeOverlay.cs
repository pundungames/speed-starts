using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Race
{
    /// <summary>Optional race input gate used while the full-screen welcome is waiting for acknowledgement.</summary>
    public interface IRaceWelcomeInput
    {
        void HoldInputForWelcome();
    }

    internal sealed class FirstRunWelcomeOverlay : MonoBehaviour
    {
        private Action _continue;
        private float _previousTimeScale;
        private bool _paused;
        private Tween _messagePulse;

        public static bool TryShow(ResultsScreenView results, Action accepted)
        {
            Canvas canvas = results ? results.GetComponentInParent<Canvas>(true) : null;
            if (!canvas) return false;

            Canvas root = canvas.rootCanvas;
            var overlay = new GameObject("First Run Welcome", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster),
                typeof(Image), typeof(Button), typeof(FirstRunWelcomeOverlay));
            var rect = (RectTransform)overlay.transform;
            rect.SetParent(root.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var front = overlay.GetComponent<Canvas>();
            front.overrideSorting = true;
            front.sortingLayerID = root.sortingLayerID;
            front.sortingOrder = 31000;
            var mask = overlay.GetComponent<Image>();
            mask.color = new Color(0.025f, 0.055f, 0.11f, 0.87f);
            var button = overlay.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = mask;

            var labelObject = new GameObject("Welcome Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(rect, false);
            label.rectTransform.anchorMin = new Vector2(0.13f, 0.29f);
            label.rectTransform.anchorMax = new Vector2(0.87f, 0.71f);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = results.GetComponentInChildren<TMP_Text>(true)?.font;
            label.text = "Welcome to Speed Stars! Let’s start your racing game!";
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMax = 82f;
            label.fontSizeMin = 28f;
            label.raycastTarget = false;

            var controller = overlay.GetComponent<FirstRunWelcomeOverlay>();
            controller._continue = accepted;
            controller._previousTimeScale = Time.timeScale;
            controller._paused = true;
            Time.timeScale = 0f;
            label.rectTransform.localScale = Vector3.one * 0.94f;
            controller._messagePulse = label.rectTransform.DOScale(1.06f, 0.7f)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(overlay);
            button.onClick.AddListener(controller.Accept);
            return true;
        }

        private void Accept()
        {
            Action next = _continue;
            if (next == null) return;
            _continue = null;
            Resume();
            gameObject.SetActive(false);
            Destroy(gameObject);
            next();
        }

        private void OnDestroy()
        {
            _messagePulse?.Kill();
            Resume();
        }

        private void Resume()
        {
            if (!_paused) return;
            _paused = false;
            Time.timeScale = _previousTimeScale;
        }
    }
}