using System.Collections.Generic;
using DG.Tweening;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Cosmetics;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Emotes;
using SpeedStars.UI.Home;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// Costume editor ("Edit racer") screen: resource boxes, colour panel, stats, Random / Save, the purchase button under
    /// the pedestal and the wardrobe panel (preset drop-down, ten tabs, two-column item grid). All objects are prefab
    /// objects bound in the Inspector; item cards, skin swatches and preset rows come from prefabs under their content parents. What is
    /// worn, whether it is saved, what Save / Purchase may do lives in <see cref="CostumeEditorModel"/> (pure C#); the menu
    /// character follows <see cref="ICosmeticPreview"/>, which this screen only feeds (<c>Show</c> on every change,
    /// <c>Reset</c> on Back). Back is UiPanel's close button: unsaved changes are dropped, the character returns to the saved
    /// outfit. A locked item can be tried on; Save is off while one is worn. Tab and item texts and numbers come from the
    /// config asset. Skin tones and emotes share the wardrobe grid; tab switches keep the unsaved outfit and screen alive.
    /// </summary>
    public sealed class CostumeEditorScreenView : UiScreen
    {
        [Header("Content")]
        [Tooltip("Content asset: tab to category mapping, item pictures, stat values, texts and denied-feedback numbers.")]
        [SerializeField] private CostumeEditorConfig config;

        [Tooltip("Back: the screen's close button; here only to drop the preview at the moment of the tap.")]
        [SerializeField] private Button backButton;

        [Tooltip("Scene object drawn BEHIND the 3D character and in front of the scene (a Screen Space - Camera canvas with the dimscreen art). Scene-bound; empty = no dimming.")]
        [SerializeField] private GameObject dimBackdrop;

        [Header("Wardrobe")]
        [Tooltip("Wardrobe/Presets: the 'Custom N' drop-down.")]
        [SerializeField] private CostumePresetDropdownView presetDropdown;

        [Tooltip("Wardrobe/Tabs: the ten tabs, in column order (each view carries its own CostumeTab value).")]
        [SerializeField] private CostumeTabView[] tabs = new CostumeTabView[0];

        [Tooltip("Wardrobe/Items: the item grid's scroll list.")]
        [SerializeField] private ScrollRect itemsScroll;

        [Tooltip("Wardrobe/Items/Viewport/Content: the parent item cards are created under.")]
        [SerializeField] private Transform itemsContent;

        [Tooltip("CostumeItemCard prefab with a CostumeItemCardView on its root.")]
        [SerializeField] private CostumeItemCardView cardPrefab;

        [Tooltip("CostumeSkinSwatch prefab with a CostumeSkinSwatchView on its root: one ready-made skin tone of the Body tab, created under the same content parent.")]
        [SerializeField] private CostumeSkinSwatchView swatchPrefab;

        [Tooltip("Wardrobe/ScrollTrack: the handle that follows the item grid.")]
        [SerializeField] private ScrollIndicatorView scrollIndicator;

        [Header("Emote")]
        [Tooltip("Emote controller reused inside this wardrobe's grid; its standalone screen stays closed.")]
        [SerializeField] private UiScreen emoteScreen;

        [Header("Left side")]
        [Tooltip("ColorPanel: colour channels of the worn item of the selected tab (boxes + square + strip).")]
        [SerializeField] private CostumeColorPickerView colorPicker;

        [Tooltip("Resources: the four resource boxes.")]
        [SerializeField] private CostumeResourceView resources;

        [Tooltip("Random: wears a random outfit of owned items and palette colours (not saved).")]
        [SerializeField] private Button randomButton;

        [Tooltip("Save: writes the outfit to the selected preset. Off while a locked item is worn.")]
        [SerializeField] private Button saveButton;

        [Tooltip("Save background: flashes when a save is refused.")]
        [SerializeField] private Graphic saveBackground;

        [Header("Purchase (under the pedestal)")]
        [Tooltip("Purchase: the buy button. Shown only while the worn item of the tab is locked and has a price (Cash or Purple Gems).")]
        [SerializeField] private Button purchaseButton;

        [Tooltip("Purchase/Price: the price of that item.")]
        [SerializeField] private TMP_Text purchasePriceText;

        [Tooltip("Purchase/Icon: the Purple Gem icon, shown beside the price of a premium item. Optional.")]
        [SerializeField] private GameObject purchaseGemIcon;

        [Tooltip("Purchase/CashIcon: the Cash icon, shown beside the price of a normal item. Optional: without it a Cash price shows no icon.")]
        [SerializeField] private GameObject purchaseCashIcon;

        [Tooltip("Purchase background: flashes when the player cannot afford the item.")]
        [SerializeField] private Graphic purchaseBackground;

        [Header("Stats panel (display only)")]
        [Tooltip("Stats/PowerFill: filled image over the whole stats art; shows the bar part of it.")]
        [SerializeField] private Image powerFill;

        [Tooltip("Stats/StaminaFill: same for stamina.")]
        [SerializeField] private Image staminaFill;

        [Tooltip("Stats/Height/Label.")]
        [SerializeField] private TMP_Text heightText;

        [Tooltip("Where the bars start and end on the stats art, as fractions of its width (read off the art by the builder). x = start, y = end.")]
        [SerializeField] private Vector2 statBarSpan = new Vector2(0f, 1f);

        [Tooltip("Largest scale the wardrobe (right panel, the Emote tab too) is drawn at, from the screen's top-right corner. On a short screen it shrinks below this so the panel never runs off the bottom.")]
        [SerializeField] private float wardrobeMaxScale = 1.2f;

        [Tooltip("Room kept between the bottom of the wardrobe panel and the bottom of the screen (canvas units).")]
        [SerializeField] private float wardrobeBottomMargin = 16f;

        private ICosmeticService _service;
        private CosmeticCatalog _catalog;
        private ICosmeticPreview _preview;
        private IInstantiator _instantiator;
        private UiMotionConfig _cardMotion;
        private CostumeEditorModel _model;
        private CostumeTab _tab;
        private DeniedFeedback _saveDenied;
        private DeniedFeedback _purchaseDenied;
        private bool _openEmoteOnEnable;
        private ResourceBarView _sharedResources;
        private EmoteScreenView EmbeddedEmotes => emoteScreen as EmoteScreenView;

        public void OpenEmoteTab()
        {
            if (!gameObject.activeInHierarchy) { _openEmoteOnEnable = true; return; }
            OnTab(CostumeTab.Emote);
        }

        private void FitTabsInsidePanel()
        {
            var wardrobe = transform.Find("Wardrobe") as RectTransform;
            if (wardrobe)
            {
                wardrobe.pivot = Vector2.one;
                wardrobe.anchorMin = Vector2.zero;
                wardrobe.anchorMax = Vector2.one;
                wardrobe.offsetMin = wardrobe.offsetMax = Vector2.zero;
                wardrobe.anchoredPosition = Vector2.zero;
                FitWardrobeScale();
            }
            var panel = transform.Find("Wardrobe/Panel") as RectTransform;
            if (!panel || tabs.Length == 0) return;
            const float top = 0.87f, bottom = 0.035f, gap = 0.006f;
            float height = (top - bottom - gap * (tabs.Length - 1)) / tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
            {
                var rect = (RectTransform)tabs[i].transform;
                rect.SetParent(panel, false);
                rect.anchorMin = new Vector2(0.685f, top - i * (height + gap) - height);
                rect.anchorMax = new Vector2(0.98f, top - i * (height + gap));
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }
        }
        /// <summary>
        /// The wardrobe grows from the top-right corner up to wardrobeMaxScale, but only as far as the screen's height allows: the panel
        /// hangs from that corner, so its bottom edge is its top offset plus its height, times the scale. A long phone (19.5:9) has less
        /// canvas height than the 1.2 that was fixed here, and the tabs ran off the bottom.
        /// </summary>
        private void FitWardrobeScale()
        {
            var wardrobe = transform.Find("Wardrobe") as RectTransform;
            var panel = transform.Find("Wardrobe/Panel") as RectTransform;
            if (!wardrobe || !panel) return;
            float available = ((RectTransform)transform).rect.height - wardrobeBottomMargin;
            float needed = -panel.anchoredPosition.y + panel.rect.height;
            float scale = available > 0f && needed > 0f ? Mathf.Min(wardrobeMaxScale, available / needed) : wardrobeMaxScale;
            wardrobe.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Safe area, rotation or a resized window: the height the wardrobe may take changed.</summary>
        private void OnRectTransformDimensionsChange() => FitWardrobeScale();

        private readonly List<CostumeItemCardView> _cards = new List<CostumeItemCardView>();
        private readonly List<CostumeSkinSwatchView> _swatches = new List<CostumeSkinSwatchView>();
        private readonly List<string> _presetNames = new List<string>();
        private readonly List<string> _channelHexes = new List<string>();

        [Inject]
        public void ConstructCostumeEditor(ICosmeticService service, ICosmeticPreview preview, IInstantiator instantiator, UiMotionConfig motion,
            ScreenNavigator navigator, CosmeticCatalog catalog)
        {
            _service = service;
            _catalog = catalog;
            _preview = preview;
            _instantiator = instantiator;
            _cardMotion = motion;
            _model = new CostumeEditorModel(service, catalog.DefaultSkinColor);
        }

        protected override void Awake()
        {
            base.Awake();
            FitTabsInsidePanel();
            // Leave a full horizontal-resource column at the left, with a clear gap before the editor controls.
            ((RectTransform)colorPicker.transform).anchoredPosition += Vector2.right * 140f;
            var stats = transform.Find("Stats") as RectTransform;
            if (stats) stats.anchoredPosition += Vector2.right * 140f;
            ((RectTransform)scrollIndicator.transform).anchoredPosition += Vector2.right * 12f;
            foreach (CostumeTabView tabView in tabs) tabView.Tapped += OnTab;
            presetDropdown.PresetChosen += OnPresetChosen;
            colorPicker.ColorPicked += OnColorPicked;
            colorPicker.ChannelTapped += OnChannelTapped;
            randomButton.onClick.AddListener(OnRandom);
            saveButton.onClick.AddListener(OnSave);
            purchaseButton.onClick.AddListener(OnPurchase);
            backButton.onClick.AddListener(OnBack);
            _saveDenied = new DeniedFeedback((RectTransform)saveButton.transform, saveBackground);
            _purchaseDenied = new DeniedFeedback((RectTransform)purchaseButton.transform, purchaseBackground);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            FitWardrobeScale(); // the screen has its final (safe area) size only once it is on
            if (_model == null) return;

            _service.Changed += RefreshAll;
            _sharedResources = FindObjectOfType<ResourceBarView>();
            if (_sharedResources)
            {
                resources.gameObject.SetActive(false);
                _sharedResources.ShowInWardrobe(this, (RectTransform)backButton.transform);
            }
            if (dimBackdrop != null) dimBackdrop.SetActive(true);
            if (dimBackdrop != null)
            {
                var isolation = dimBackdrop.GetComponent<MenuBackdropIsolation>()
                    ?? dimBackdrop.AddComponent<MenuBackdropIsolation>();
                isolation.Configure();
            }

            _model.Begin();
            DrawStats();
            ShowPresets();
            if (_openEmoteOnEnable) { _openEmoteOnEnable = false; SelectTab(CostumeTab.Emote); }
            else SelectFirstAvailableTab();
            RefreshAll();
        }

        protected override void OnDisable()
        {
            if (_sharedResources) _sharedResources.LeaveWardrobe(this);
            EmbeddedEmotes?.HideEmbedded();
            if (_model != null)
            {
                _service.Changed -= RefreshAll;
                DropPreview();
            }
            if (dimBackdrop != null) dimBackdrop.SetActive(false);
            presetDropdown.Close();
            _saveDenied?.Stop();
            _purchaseDenied?.Stop();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            foreach (CostumeTabView tabView in tabs)
                if (tabView != null) tabView.Tapped -= OnTab;
            if (presetDropdown != null) presetDropdown.PresetChosen -= OnPresetChosen;
            if (colorPicker != null)
            {
                colorPicker.ColorPicked -= OnColorPicked;
                colorPicker.ChannelTapped -= OnChannelTapped;
            }
            if (randomButton != null) randomButton.onClick.RemoveListener(OnRandom);
            if (saveButton != null) saveButton.onClick.RemoveListener(OnSave);
            if (purchaseButton != null) purchaseButton.onClick.RemoveListener(OnPurchase);
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
            foreach (CostumeItemCardView card in _cards)
                if (card != null) card.Tapped -= OnItemTapped;
            foreach (CostumeSkinSwatchView swatch in _swatches)
                if (swatch != null) swatch.Tapped -= OnSwatchTapped;
            base.OnDestroy();
        }

        // ---------------------------------------------------------------- drawing

        private void DrawStats()
        {
            powerFill.fillAmount = Mathf.Lerp(statBarSpan.x, statBarSpan.y, config.PowerFill);
            staminaFill.fillAmount = Mathf.Lerp(statBarSpan.x, statBarSpan.y, config.StaminaFill);
            heightText.text = config.HeightText;
        }

        private void ShowPresets()
        {
            _presetNames.Clear();
            for (int i = 0; i < _service.PresetCount; i++) _presetNames.Add(_service.GetPresetName(i));
            presetDropdown.Show(_instantiator, _presetNames, _model.PresetIndex);
        }

        private void SelectFirstAvailableTab()
        {
            // The opening tab is the first list of items (the skin tab sits at the top of the column but is not the default).
            foreach (CostumeTabView tabView in tabs)
            {
                if (CostumeTabRules.KindOf(tabView.Tab) != CostumeTabKind.Items || !IsAvailable(tabView.Tab)) continue;
                SelectTab(tabView.Tab);
                return;
            }
        }

        /// <summary>
        /// Item tabs follow the config asset. The Body (skin) tab is always there. The Emote tab is there while the emote screen
        /// is bound; both are behaviour of the screen, so an asset saved before they existed still works.
        /// </summary>
        private bool IsAvailable(CostumeTab tab)
        {
            switch (CostumeTabRules.KindOf(tab))
            {
                case CostumeTabKind.Skin: return true;
                case CostumeTabKind.Emote: return EmbeddedEmotes != null;
                default: return config.TryGetTab(tab, out CostumeTabEntry entry) && entry.HasContent;
            }
        }

        /// <summary>Everything that follows the outfit, the ownership or the saved state (cheap: a handful of objects).</summary>
        private void RefreshAll()
        {
            foreach (CostumeTabView tabView in tabs)
            {
                tabView.SetAvailable(IsAvailable(tabView.Tab));
                tabView.SetSelected(tabView.Tab == _tab);
            }

            CosmeticSlot slot = _model.SelectedSlot;
            foreach (CostumeItemCardView card in _cards)
            {
                if (!card.gameObject.activeSelf) continue;
                bool known = _service.TryGetItem(card.CosmeticId, out CosmeticItemInfo item);
                card.SetState(!slot.IsEmpty && slot.CosmeticId == card.CosmeticId, known && item.IsLocked);
            }
            RefreshSwatches();

            bool emotes = _tab == CostumeTab.Emote;
            colorPicker.gameObject.SetActive(!emotes);
            randomButton.gameObject.SetActive(!emotes);
            saveButton.gameObject.SetActive(!emotes);
            colorPicker.SetInteractable(!emotes && _model.CanColor);
            DrawColorBoxes();

            saveButton.interactable = _model.CanSave;

            bool buyable = _model.TryGetBuyableItem(out CosmeticItemInfo buy);
            purchaseButton.gameObject.SetActive(!emotes && buyable);
            if (buyable) purchasePriceText.text = buy.Price.ToString();
            // The icon follows the currency of the selected item: a Cash price never wears the gem.
            bool cashPrice = buyable && buy.Currency == CosmeticCurrency.Cash;
            if (purchaseGemIcon != null) purchaseGemIcon.SetActive(buyable && !cashPrice);
            if (purchaseCashIcon != null) purchaseCashIcon.SetActive(cashPrice);
        }

        /// <summary>
        /// One box per colour channel of the worn item (a bare slot shows one greyed box): each shows its channel's colour,
        /// the one the player set or else the material's default; the square and strip edit the selected channel.
        /// </summary>
        private void DrawColorBoxes()
        {
            int count = Mathf.Max(1, _model.ChannelCount);
            _channelHexes.Clear();
            for (int i = 0; i < count; i++) _channelHexes.Add(_model.ChannelHex(i));

            int selected = _model.ChannelIndex;
            colorPicker.ShowChannels(_channelHexes, selected, config.EmptyColorHex);
            string current = _channelHexes[selected];
            colorPicker.SetColor(string.IsNullOrEmpty(current) ? config.EmptyColorHex : current);
        }

        /// <summary>The swatch of the worn skin shows its frame and tick; a colour picked in the panel can match one too.</summary>
        private void RefreshSwatches()
        {
            foreach (CostumeSkinSwatchView swatch in _swatches)
                if (swatch.gameObject.activeSelf) swatch.SetSelected(_model.IsSkinColor(swatch.ColorHex));
        }

        /// <summary>The Body tab's grid: one swatch per ready-made tone of the catalog (swatches are reused, extra ones are switched off).</summary>
        private void RebuildSkinTones()
        {
            foreach (CostumeItemCardView card in _cards) card.gameObject.SetActive(false);
            if (swatchPrefab == null)
            {
                Debug.LogWarning($"{name}: {nameof(swatchPrefab)} is not assigned; the Body tab shows no skin tones (the colour panel still works).", this);
                FinishGrid();
                return;
            }

            int used = 0;
            foreach (string tone in _catalog.SkinTones)
            {
                string hex = CosmeticColor.Normalize(tone);
                if (hex.Length == 0 || !CosmeticColor.TryParse(hex, out Color color)) continue;
                if (used == _swatches.Count)
                {
                    CostumeSkinSwatchView created = _instantiator.InstantiatePrefabForComponent<CostumeSkinSwatchView>(swatchPrefab.gameObject, itemsContent);
                    created.Tapped += OnSwatchTapped;
                    _swatches.Add(created);
                }
                _swatches[used].gameObject.SetActive(true);
                _swatches[used].Bind(hex, color);
                used++;
            }
            for (int i = used; i < _swatches.Count; i++) _swatches[i].gameObject.SetActive(false);

            FinishGrid();
        }

        /// <summary>The grid of the selected tab: cards are reused, extra ones are switched off.</summary>
        private void RebuildItems()
        {
            foreach (CostumeSkinSwatchView swatch in _swatches) swatch.gameObject.SetActive(false);

            IReadOnlyList<CosmeticItemInfo> items = _service.GetItems(_model.Category);
            for (int i = 0; i < items.Count; i++)
            {
                if (i == _cards.Count)
                {
                    CostumeItemCardView created = _instantiator.InstantiatePrefabForComponent<CostumeItemCardView>(cardPrefab.gameObject, itemsContent);
                    created.Tapped += OnItemTapped;
                    _cards.Add(created);
                }
                _cards[i].gameObject.SetActive(true);
                _cards[i].Bind(items[i], config.IconOf(items[i].Id));
            }
            for (int i = items.Count; i < _cards.Count; i++) _cards[i].gameObject.SetActive(false);

            FinishGrid();
        }

        private void FinishGrid()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)itemsContent);
            itemsScroll.verticalNormalizedPosition = 1f;
            scrollIndicator.Redraw();
        }

        private void SelectTab(CostumeTab tab)
        {
            if (tab != CostumeTab.Emote) EmbeddedEmotes?.HideEmbedded();
            switch (CostumeTabRules.KindOf(tab))
            {
                case CostumeTabKind.Emote:
                    if (EmbeddedEmotes == null) return;
                    _tab = tab;
                    foreach (var card in _cards) card.gameObject.SetActive(false);
                    foreach (var swatch in _swatches) swatch.gameObject.SetActive(false);
                    EmbeddedEmotes.ShowEmbedded(transform, itemsScroll, itemsContent, scrollIndicator);
                    return;
                case CostumeTabKind.Skin:
                    _tab = tab;
                    _model.SelectSkin();
                    RebuildSkinTones();
                    return;
                default:
                    if (!config.TryGetTab(tab, out CostumeTabEntry entry) || !entry.HasContent) return;
                    _tab = tab;
                    _model.SelectCategory(entry.Category);
                    RebuildItems();
                    return;
            }
        }

        // ---------------------------------------------------------------- taps

        private void OnTab(CostumeTab tab)
        {
            presetDropdown.Close();
            if (CostumeTabRules.KindOf(tab) == CostumeTabKind.Emote)
            {
                OpenEmote();
                return;
            }
            SelectTab(tab);
            RefreshAll();
        }

        /// <summary>Switches content in place without navigation or a screen transition.</summary>
        private void OpenEmote()
        {
            SelectTab(CostumeTab.Emote);
            RefreshAll();
        }

        private void OnSwatchTapped(string hex)
        {
            presetDropdown.Close();
            if (!_model.SetSkinColor(hex)) return;
            _preview.Show(_model.Outfit);
            foreach (CostumeSkinSwatchView swatch in _swatches)
                if (swatch.gameObject.activeSelf && swatch.ColorHex == hex) swatch.Punch(_cardMotion);
            RefreshAll();
        }

        private void OnPresetChosen(int index)
        {
            if (!_model.SelectPreset(index)) return;
            _preview.Reset(); // the service already follows the new preset; nothing unsaved is left to show
            ShowPresets();
            RefreshAll();
        }

        private void OnItemTapped(string cosmeticId)
        {
            presetDropdown.Close();
            if (_model.TapItem(cosmeticId) == ItemTapOutcome.None) return;
            _preview.Show(_model.Outfit);
            RefreshAll();
        }

        private void OnColorPicked(string hex)
        {
            if (!_model.SetColor(hex)) return;
            _preview.Show(_model.Outfit);
            RefreshSaveButton();
            RefreshSwatches();
        }

        private void OnChannelTapped(int index)
        {
            presetDropdown.Close();
            if (!_model.SelectChannel(index)) return;
            colorPicker.PunchChannel(index, _cardMotion);
            RefreshAll();
        }

        private void OnRandom()
        {
            presetDropdown.Close();
            _model.Randomize(new System.Random());
            _preview.Show(_model.Outfit);
            RefreshAll();
        }

        private void OnSave()
        {
            presetDropdown.Close();
            switch (_model.Save())
            {
                case CosmeticSaveResult.Saved:
                case CosmeticSaveResult.NotPersisted:
                    _preview.Reset(); // saved = what is worn; the preview is no longer a preview
                    break;
                default:
                    _saveDenied.Play(config, gameObject);
                    break;
            }
            RefreshAll();
        }

        private void OnPurchase()
        {
            presetDropdown.Close();
            string boughtId = _model.SelectedSlot.CosmeticId;
            switch (_model.Purchase())
            {
                case CosmeticPurchaseResult.Purchased:
                    PunchCard(boughtId);
                    break;
                case CosmeticPurchaseResult.NotEnough:
                    _purchaseDenied.Play(config, gameObject);
                    break;
            }
            RefreshAll();
        }

        private void RefreshSaveButton()
        {
            saveButton.interactable = _model.CanSave;
        }

        private void PunchCard(string cosmeticId)
        {
            foreach (CostumeItemCardView card in _cards)
            {
                if (card.gameObject.activeSelf && card.CosmeticId == cosmeticId)
                {
                    card.Punch(_cardMotion);
                    return;
                }
            }
        }

        /// <summary>Back leaves for good: nothing is kept for a later open.</summary>
        private void OnBack()
        {
            DropPreview();
        }

        private void DropPreview()
        {
            if (_preview != null) _preview.Reset();
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

            public void Play(CostumeEditorConfig config, GameObject link)
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
