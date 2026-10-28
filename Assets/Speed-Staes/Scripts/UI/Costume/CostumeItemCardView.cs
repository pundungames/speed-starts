using System;
using DG.Tweening;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// One item card of the wardrobe grid (prefab CostumeItemCard, created by the screen under the grid's content). Normal
    /// and selected background layers, the item picture, a tick while worn, a lock while not owned. A premium item
    /// tints its backgrounds (placeholder look). The card is a Button: the screen hears the tap through <see cref="Tapped"/>.
    /// Binds existing objects only.
    /// </summary>
    public sealed class CostumeItemCardView : MonoBehaviour
    {
        [Tooltip("Card root button (tap area).")]
        [SerializeField] private Button button;

        [Tooltip("Normal background layer.")]
        [SerializeField] private Image normalBackground;

        [Tooltip("Selected background layer (red frame): shown while the item is worn.")]
        [SerializeField] private Image selectedBackground;

        [Tooltip("Item picture; kept in its aspect inside the card.")]
        [SerializeField] private Image icon;

        [Tooltip("Tick: shown while the item is worn.")]
        [SerializeField] private GameObject check;

        [Tooltip("Lock overlay: shown while the item is not owned.")]
        [SerializeField] private GameObject lockOverlay;

        private Tween _punch;
        private Vector3 _baseScale = Vector3.one;
        private bool _baseKnown;

        /// <summary>The cosmetic id this card shows.</summary>
        public string CosmeticId { get; private set; } = string.Empty;

        /// <summary>The player tapped the card (cosmetic id).</summary>
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

        /// <summary>Fills the card for an item. <paramref name="picture"/> may be null (no picture yet: the picture is hidden).</summary>
        public void Bind(CosmeticItemInfo item, Sprite picture, Color premiumTint)
        {
            CosmeticId = item.Id;
            icon.sprite = picture;
            icon.enabled = picture != null;

            Color tint = item.IsPremium ? premiumTint : Color.white;
            normalBackground.color = tint;
            selectedBackground.color = tint;
        }

        public void SetState(bool selected, bool locked)
        {
            normalBackground.gameObject.SetActive(!selected);
            selectedBackground.gameObject.SetActive(selected);
            check.SetActive(selected);
            lockOverlay.SetActive(locked);
        }

        /// <summary>A short scale punch (a bought item): numbers from the motion config; no motion config = no punch.</summary>
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

        private void OnClick() => Tapped?.Invoke(CosmeticId);

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
