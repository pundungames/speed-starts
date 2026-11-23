using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Settings
{
    /// <summary>
    /// Small yes/no window inside the settings popup (its own dim layer, so taps cannot reach the rows behind it).
    /// It shows what it is told and reports Cancel or Confirm; the question logic is <see cref="ResetConfirmFlow"/>.
    /// The danger look (red button) is for the question whose yes cannot be taken back. Binds objects of the prefab.
    /// </summary>
    public sealed class ConfirmDialogView : MonoBehaviour
    {
        [Tooltip("Window panel that pops in.")]
        [SerializeField] private RectTransform panel;

        [Tooltip("Question.")]
        [SerializeField] private Text titleText;

        [Tooltip("Explanation under the question.")]
        [SerializeField] private Text bodyText;

        [Tooltip("Left button: say no.")]
        [SerializeField] private Button cancelButton;

        [Tooltip("Left button caption.")]
        [SerializeField] private Text cancelLabel;

        [Tooltip("Right button: say yes.")]
        [SerializeField] private Button confirmButton;

        [Tooltip("Right button background Image.")]
        [SerializeField] private Image confirmImage;

        [Tooltip("Right button caption.")]
        [SerializeField] private Text confirmLabel;

        [Tooltip("Right button background for a normal yes.")]
        [SerializeField] private Sprite normalConfirmSprite;

        [Tooltip("Right button background for the yes that cannot be undone (tinted below).")]
        [SerializeField] private Sprite dangerConfirmSprite;

        [Tooltip("Tint of the danger background.")]
        [SerializeField] private Color dangerTint = new Color(0.9f, 0.16f, 0.16f, 1f);

        [Tooltip("Scale the window pops in from.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float popFromScale = 0.85f;

        [Tooltip("Seconds of the pop-in.")]
        [Min(0f)]
        [SerializeField] private float popSeconds = 0.18f;

        private Action _onConfirm;
        private Action _onCancel;
        private Tween _pop;

        public bool IsShown => gameObject.activeSelf;

        private void Awake()
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
            confirmButton.onClick.AddListener(OnConfirmClicked);
            cancelLabel.text = SettingsTexts.Cancel;
        }

        private void OnDestroy()
        {
            if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancelClicked);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
        }

        private void OnDisable() => KillPop();

        /// <summary>Shows (or re-words, when already shown) the window.</summary>
        public void Show(string title, string body, string confirmCaption, bool danger, Action onConfirm, Action onCancel)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            titleText.text = title;
            bodyText.text = body;
            confirmLabel.text = confirmCaption;
            confirmImage.sprite = danger ? dangerConfirmSprite : normalConfirmSprite;
            confirmImage.type = danger ? Image.Type.Sliced : Image.Type.Simple;
            confirmImage.color = danger ? dangerTint : Color.white;

            bool wasShown = gameObject.activeSelf;
            gameObject.SetActive(true);
            if (wasShown) return; // the second question only changes the words

            KillPop();
            panel.localScale = Vector3.one * popFromScale;
            _pop = panel.DOScale(1f, popSeconds).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        public void Hide()
        {
            KillPop();
            gameObject.SetActive(false);
        }

        private void OnCancelClicked() => _onCancel?.Invoke();

        private void OnConfirmClicked() => _onConfirm?.Invoke();

        private void KillPop()
        {
            if (_pop == null) return;
            Tween running = _pop;
            _pop = null;
            running.Kill();
            if (panel != null) panel.localScale = Vector3.one;
        }
    }
}
