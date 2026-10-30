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
    /// swaps to its own layer family (see <see cref="PremiumCardLayers"/>). The card is a Button: the screen hears the tap through <see cref="Tapped"/>.
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

        [Header("Premium layers (hidden on normal cards)")]
        [Tooltip("Premium plain fill under the item picture.")]
        [SerializeField] private GameObject premiumBackground;

        [Tooltip("Premium navy frame with the gold inner line, above the picture.")]
        [SerializeField] private GameObject premiumFrame;

        [Tooltip("Premium red outer frame: the item is the one shown on the character.")]
        [SerializeField] private GameObject premiumSelectedFrame;

        [Tooltip("Premium lock: the item is not owned.")]
        [SerializeField] private GameObject premiumLock;

        [Tooltip("Premium tick: the item is owned and worn.")]
        [SerializeField] private GameObject premiumCheck;

        [Tooltip("Premium veil: only while the card is really disabled (not the same as locked).")]
        [SerializeField] private GameObject premiumDisabledOverlay;

        private Tween _punch;
        private bool _premium;
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
        public void Bind(CosmeticItemInfo item, Sprite picture)
        {
            CosmeticId = item.Id;
            icon.sprite = picture;
            icon.enabled = picture != null;
            _premium = item.IsPremium;
        }

        public void SetState(bool selected, bool locked, bool disabled = false)
        {
            PremiumCardLayers layers = PremiumCardLayers.Resolve(_premium, selected, locked, disabled);
            normalBackground.gameObject.SetActive(layers.NormalBackground);
            selectedBackground.gameObject.SetActive(layers.NormalSelectedBackground);
            check.SetActive(layers.NormalCheck);
            lockOverlay.SetActive(layers.NormalLock);
            Show(premiumBackground, layers.PremiumBackground);
            Show(premiumFrame, layers.PremiumFrame);
            Show(premiumSelectedFrame, layers.PremiumSelectedFrame);
            Show(premiumLock, layers.PremiumLock);
            Show(premiumCheck, layers.PremiumCheck);
            Show(premiumDisabledOverlay, layers.PremiumDisabledOverlay);
        }

        private static void Show(GameObject layer, bool visible)
        {
            if (layer != null) layer.SetActive(visible);
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

    /// <summary>
    /// Which layers of an item card are visible (pure, tested). Normal cards keep the old look. A premium card shows
    /// background, picture, frame; locked adds the lock (the card still selects and previews); the selected one adds the
    /// red frame, and the tick only when the item is really owned (a previewed locked item is not "worn"); the veil is
    /// only for a really disabled card.
    /// </summary>
    public readonly struct PremiumCardLayers
    {
        public bool NormalBackground { get; }
        public bool NormalSelectedBackground { get; }
        public bool NormalCheck { get; }
        public bool NormalLock { get; }
        public bool PremiumBackground { get; }
        public bool PremiumFrame { get; }
        public bool PremiumSelectedFrame { get; }
        public bool PremiumLock { get; }
        public bool PremiumCheck { get; }
        public bool PremiumDisabledOverlay { get; }

        private PremiumCardLayers(bool premium, bool selected, bool locked, bool disabled)
        {
            NormalBackground = !premium && !selected;
            NormalSelectedBackground = !premium && selected;
            NormalCheck = !premium && selected;
            NormalLock = !premium && locked;
            PremiumBackground = premium;
            PremiumFrame = premium;
            PremiumSelectedFrame = premium && selected;
            PremiumLock = premium && locked;
            PremiumCheck = premium && selected && !locked;
            PremiumDisabledOverlay = premium && disabled;
        }

        public static PremiumCardLayers Resolve(bool premium, bool selected, bool locked, bool disabled)
            => new PremiumCardLayers(premium, selected, locked, disabled);
    }
}
