using System.Collections.Generic;
using DG.Tweening;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Costume;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Emotes
{
    /// <summary>
    /// Emote screen: a two-column list of emote cards on the wardrobe panel, the menu character on the pedestal playing the
    /// selected emote, and one action under the pedestal: Equip (owned), or the price button (locked). Every
    /// object is a prefab object bound in the Inspector; the cards come from a prefab under the list's content. What is
    /// selected and what the action does lives in <see cref="EmoteScreenModel"/> (pure C#); ownership, Cash and the saved
    /// emote live in <see cref="IEmoteService"/>; the character follows <see cref="IEmotePreview"/>, which this screen only
    /// asks. A tap on any card (locked ones too) selects it and plays it. Back is UiPanel's close button.
    /// </summary>
    public sealed class EmoteScreenView : UiScreen
    {
        [Tooltip("Look-and-feel numbers: denied shake and flash, price colour.")]
        [SerializeField] private EmoteScreenConfig config;

        [Tooltip("Back: the screen's close button; here only to stop the preview at the moment of the tap.")]
        [SerializeField] private Button backButton;

        [Tooltip("Scene object drawn BEHIND the 3D character and in front of the scene (the same dimscreen the costume editor uses). Scene-bound; empty = no dimming.")]
        [SerializeField] private GameObject dimBackdrop;

        [Header("List")]
        [Tooltip("Wardrobe/Items: the list's scroll rect.")]
        [SerializeField] private ScrollRect itemsScroll;

        [Tooltip("Wardrobe/Items/Viewport/Content: the parent emote cards are created under.")]
        [SerializeField] private Transform itemsContent;

        [Tooltip("EmoteCard prefab with an EmoteCardView on its root.")]
        [SerializeField] private EmoteCardView cardPrefab;

        [Tooltip("Wardrobe/ScrollTrack: the handle that follows the list.")]
        [SerializeField] private ScrollIndicatorView scrollIndicator;

        [Header("Actions")]
        [Tooltip("Equip: equips the selected owned emote.")]
        [SerializeField] private Button equipButton;

        [Tooltip("Equip/Label: 'Equip' or 'Equipped'.")]
        [SerializeField] private Text equipLabel;

        [Tooltip("Purchase: buys the selected locked emote. Shown only while it is locked.")]
        [SerializeField] private Button purchaseButton;

        [Tooltip("Purchase/Price: the price of the selected emote.")]
        [SerializeField] private Text purchasePriceText;

        [Tooltip("Purchase background: flashes when the player cannot afford the emote.")]
        [SerializeField] private Graphic purchaseBackground;

        private IEmoteService _service;
        private IEmotePreview _preview;
        private IInstantiator _instantiator;
        private UiMotionConfig _cardMotion;
        private EmoteScreenModel _model;
        private DeniedFeedback _purchaseDenied;
        private readonly List<EmoteCardView> _cards = new List<EmoteCardView>();

        [Inject]
        public void ConstructEmotes(IEmoteService service, IEmotePreview preview, IWallet wallet, IInstantiator instantiator, UiMotionConfig motion)
        {
            _service = service;
            _preview = preview;
            _instantiator = instantiator;
            _cardMotion = motion;
            _model = new EmoteScreenModel(service);
        }

        protected override void Awake()
        {
            base.Awake();
            equipButton.onClick.AddListener(OnEquip);
            purchaseButton.onClick.AddListener(OnPurchase);
            backButton.onClick.AddListener(StopPreview);
            _purchaseDenied = new DeniedFeedback((RectTransform)purchaseButton.transform, purchaseBackground);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_model == null) return;

            _service.Changed += RefreshAll;
            if (dimBackdrop != null) dimBackdrop.SetActive(true);

            _model.Begin();
            BuildCards();
            RefreshAll();
        }

        protected override void OnDisable()
        {
            if (_model != null)
            {
                _service.Changed -= RefreshAll;
                StopPreview();
            }
            if (dimBackdrop != null) dimBackdrop.SetActive(false);
            _purchaseDenied?.Stop();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (equipButton != null) equipButton.onClick.RemoveListener(OnEquip);
            if (purchaseButton != null) purchaseButton.onClick.RemoveListener(OnPurchase);
            if (backButton != null) backButton.onClick.RemoveListener(StopPreview);
            foreach (EmoteCardView card in _cards)
                if (card != null) card.Tapped -= OnCardTapped;
            base.OnDestroy();
        }

        // ---------------------------------------------------------------- drawing

        /// <summary>One card per emote of the service; cards are reused, extra ones are switched off.</summary>
        private void BuildCards()
        {
            IReadOnlyList<EmoteInfo> emotes = _service.Emotes;
            for (int i = 0; i < emotes.Count; i++)
            {
                if (i == _cards.Count)
                {
                    EmoteCardView created = _instantiator.InstantiatePrefabForComponent<EmoteCardView>(cardPrefab.gameObject, itemsContent);
                    created.Tapped += OnCardTapped;
                    _cards.Add(created);
                }
                _cards[i].gameObject.SetActive(true);
                _cards[i].Bind(emotes[i], config.PriceColor);
            }
            for (int i = emotes.Count; i < _cards.Count; i++) _cards[i].gameObject.SetActive(false);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)itemsContent);
            itemsScroll.verticalNormalizedPosition = 1f;
            scrollIndicator.Redraw();
        }

        /// <summary>Everything that follows the selection, the ownership or the equipped emote (a handful of objects).</summary>
        private void RefreshAll()
        {
            string equipped = _service.EquippedId;
            foreach (EmoteCardView card in _cards)
            {
                if (!card.gameObject.activeSelf) continue;
                card.SetState(card.EmoteId == _model.SelectedId, card.EmoteId == equipped, !_service.IsOwned(card.EmoteId));
            }

            EmoteAction action = _model.Action;
            purchaseButton.gameObject.SetActive(action == EmoteAction.Buy);
            equipButton.gameObject.SetActive(action == EmoteAction.Equip || action == EmoteAction.Equipped);
            equipButton.interactable = action == EmoteAction.Equip;
            equipLabel.text = action == EmoteAction.Equipped ? EmoteTexts.Equipped : EmoteTexts.Equip;
            if (action == EmoteAction.Buy) purchasePriceText.text = _model.SelectedPrice.ToString();
        }

        // ---------------------------------------------------------------- taps

        private void OnCardTapped(string emoteId)
        {
            if (!_model.Select(emoteId)) return;
            _preview.Play(emoteId);
            PunchCard(emoteId);
            RefreshAll();
        }

        private void OnEquip()
        {
            string id = _model.SelectedId;
            if (!_model.Equip()) return;
            PunchCard(id);
            RefreshAll();
        }

        private void OnPurchase()
        {
            string id = _model.SelectedId;
            switch (_model.Purchase())
            {
                case EmotePurchaseResult.Purchased:
                    PunchCard(id);
                    break;
                case EmotePurchaseResult.NotEnoughCash:
                    _purchaseDenied.Play(config, gameObject);
                    break;
            }
            RefreshAll();
        }

        private void PunchCard(string emoteId)
        {
            foreach (EmoteCardView card in _cards)
            {
                if (card.gameObject.activeSelf && card.EmoteId == emoteId)
                {
                    card.Punch(_cardMotion);
                    return;
                }
            }
        }

        private void StopPreview()
        {
            if (_preview != null) _preview.Stop();
        }

        /// <summary>
        /// Shake along X plus a flash colour that fades back, on one button. The shake always returns to the position the
        /// designer set (captured on the first play); the DOTween UI shortcuts are not visible to this assembly, so both go
        /// through DOTween.To.
        /// </summary>
        private sealed class DeniedFeedback
        {
            private readonly RectTransform _rect;
            private readonly Graphic _flashTarget;
            private Tween _shake;
            private Tween _flash;
            private Vector2 _basePosition;
            private Color _baseColor;
            private bool _baseKnown;

            public DeniedFeedback(RectTransform rect, Graphic flashTarget)
            {
                _rect = rect;
                _flashTarget = flashTarget;
            }

            public void Play(EmoteScreenConfig config, GameObject link)
            {
                Stop();
                if (!_baseKnown)
                {
                    _baseKnown = true;
                    _basePosition = _rect.anchoredPosition;
                    if (_flashTarget != null) _baseColor = _flashTarget.color;
                }

                float strength = config.DeniedShakeStrength;
                int cycles = config.DeniedShakeCycles;
                float progress = 0f;
                _shake = DOTween.To(() => progress, p =>
                    {
                        progress = p;
                        _rect.anchoredPosition = _basePosition + new Vector2(ShakeCurve.Offset(p, strength, cycles), 0f);
                    }, 1f, config.DeniedShakeDuration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .SetLink(link);

                if (_flashTarget == null) return;
                _flashTarget.color = config.DeniedFlashColor;
                _flash = DOTween.To(() => _flashTarget.color, c => _flashTarget.color = c, _baseColor, config.DeniedFlashDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetLink(link);
            }

            /// <summary>Stops both tweens and puts the button back where and how the designer left it.</summary>
            public void Stop()
            {
                if (_shake != null)
                {
                    _shake.Kill();
                    _shake = null;
                }
                if (_flash != null)
                {
                    _flash.Kill();
                    _flash = null;
                }
                if (!_baseKnown) return;
                if (_rect != null) _rect.anchoredPosition = _basePosition;
                if (_flashTarget != null) _flashTarget.color = _baseColor;
            }
        }
    }
}
