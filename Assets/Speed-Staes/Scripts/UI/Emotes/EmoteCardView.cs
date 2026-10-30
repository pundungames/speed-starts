using System;
using DG.Tweening;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Emotes
{
    /// <summary>
    /// One emote card of the list (prefab EmoteCard, created by the screen under the list's content). Same layers as the
    /// wardrobe item card: normal and selected background, the picture, a tick while equipped, a lock while not owned; plus
    /// the emote's name and, while locked, its price. A Button: the screen hears the tap through <see cref="Tapped"/>.
    /// Binds existing objects only.
    /// </summary>
    public sealed class EmoteCardView : MonoBehaviour
    {
        [Tooltip("Card root button (tap area).")]
        [SerializeField] private Button button;

        [Tooltip("Normal background layer.")]
        [SerializeField] private Image normalBackground;

        [Tooltip("Selected background layer (red frame): shown while the emote is the selected / previewed one.")]
        [SerializeField] private Image selectedBackground;

        [Tooltip("Tick: shown while the emote is the equipped one.")]
        [SerializeField] private GameObject check;

        [Tooltip("Lock overlay: shown while the emote is not owned.")]
        [SerializeField] private GameObject lockOverlay;

        [Tooltip("Card/Name: the emote's name.")]
        [SerializeField] private Text nameText;

        [Tooltip("Card/Price: the price; shown only while the emote is locked.")]
        [SerializeField] private Text priceText;

        private Tween _punch;
        private Vector3 _baseScale = Vector3.one;
        private bool _baseKnown;

        /// <summary>The emote id this card shows.</summary>
        public string EmoteId { get; private set; } = string.Empty;

        /// <summary>The player tapped the card (emote id).</summary>
        public event Action<string> Tapped;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
            RememberScale();
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
            KillPunch();
        }

        private void OnDisable()
        {
            KillPunch();
            if (_baseKnown) transform.localScale = _baseScale;
        }

        public void Bind(EmoteInfo info, Color priceColor)
        {
            EmoteId = info.Id;
            nameText.text = info.DisplayName;
            priceText.text = info.CashPrice.ToString();
            priceText.color = priceColor;
        }

        public void SetState(bool selected, bool equipped, bool locked)
        {
            normalBackground.gameObject.SetActive(!selected);
            selectedBackground.gameObject.SetActive(selected);
            check.SetActive(equipped);
            lockOverlay.SetActive(locked);
            priceText.gameObject.SetActive(locked);
        }

        /// <summary>A short scale punch (tap, bought emote): numbers from the motion config; no motion config = no punch.</summary>
        public void Punch(UiMotionConfig motion)
        {
            if (motion == null || !isActiveAndEnabled) return;
            RememberScale();
            KillPunch();
            transform.localScale = _baseScale;
            Tween punch = transform
                .DOPunchScale(_baseScale * motion.ButtonPunchScale, motion.ButtonPunchDuration, motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
            punch.OnKill(() => { if (_punch == punch) _punch = null; });
            _punch = punch;
        }

        private void OnClick() => Tapped?.Invoke(EmoteId);

        private void RememberScale()
        {
            if (_baseKnown) return;
            _baseKnown = true;
            _baseScale = transform.localScale;
        }

        private void KillPunch()
        {
            if (_punch == null) return;
            Tween running = _punch;
            _punch = null;
            running.Kill();
        }
    }
}
