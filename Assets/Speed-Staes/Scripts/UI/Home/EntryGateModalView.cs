using System;
using DG.Tweening;
using SpeedStars.Core.Audio;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Rewards;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Entry gate modal (prefab EntryGateModal, a UiPopup, dressed with the daily reward art, T-050): RACE ENTRY, event name,
    /// "Tickets 2/3" with the next-ticket countdown, Timed Ticket status (with "Use Timed Ticket" and the running window's
    /// countdown), "Use Ticket" and the ways to get tickets (Cash and the rewarded ad; both closed while the inventory is
    /// full). Confirm goes through <see cref="HomeEntryFlow"/>; a refused entry shakes the window and writes a warning into
    /// the Remaining line (hidden otherwise: the count above already says it; the prefab has no separate warning text). While Timed access runs the ticket button is hidden:
    /// nothing is spent then (PDF 1.1). WATCH AD runs <see cref="EntryGateAdFlow"/>: the button shows a spinner while the
    /// ad runs; a granted ticket punches the ticket icon, anything else shakes the window. The ad rules live in the flow;
    /// this view only shows them.
    /// </summary>
    public sealed class EntryGateModalView : UiPopup
    {
        [Tooltip("Window: shaken when the entry is refused.")]
        [SerializeField] private RectTransform window;

        [Tooltip("Window/Title: RACE ENTRY on the header.")]
        [SerializeField] private Text titleText;

        [Tooltip("Window/EventName.")]
        [SerializeField] private Text eventNameText;

        [Tooltip("Window/TicketCount: 'Tickets 2/3' next to the ticket icon.")]
        [SerializeField] private Text ticketCountText;

        [Tooltip("Window/TicketIcon: the red Entry Ticket; punched when a ticket arrives from an ad.")]
        [SerializeField] private RectTransform ticketIcon;

        [Tooltip("Window/Refill: 'Next ticket in mm:ss'; hidden when the inventory is full.")]
        [SerializeField] private Text refillText;

        [Tooltip("Window/Actions/UseEntryTicket: confirm button; hidden while Timed access is active, not interactable with no ticket.")]
        [SerializeField] private Button useTicketButton;

        [Tooltip("Window/Actions/UseEntryTicket/Label.")]
        [SerializeField] private Text useTicketLabel;

        [Tooltip("USE TICKET label colour while the button works (white on the red art).")]
        [SerializeField] private Color useTicketLabelReady = Color.white;

        [Tooltip("USE TICKET label colour while the button is disabled: white would vanish on the pale grey disabled art.")]
        [SerializeField] private Color useTicketLabelDisabled = new Color(0.38f, 0.44f, 0.56f, 1f);

        [Tooltip("Window/Remaining: the refusal warning only; hidden the rest of the time (the ticket count already shows what is left).")]
        [SerializeField] private Text remainingText;

        [Tooltip("Window/TimedTicketStatus/Label: 'Timed Ticket' or 'Timed Ticket active'.")]
        [SerializeField] private Text timedLabel;

        [Tooltip("Window/TimedTicketStatus/Value: Timed Ticket stock.")]
        [SerializeField] private Text timedValue;

        [Tooltip("Window/Actions/UseTimedTicket: starts the Timed Ticket window; shown only with a ticket in stock and no window running.")]
        [SerializeField] private Button useTimedButton;

        [Tooltip("Window/Actions/UseTimedTicket/Label.")]
        [SerializeField] private Text useTimedLabel;

        [Tooltip("Window/TimedTicketStatus: punched when a Timed Ticket window starts.")]
        [SerializeField] private RectTransform timedStatus;

        [Tooltip("Window/Actions/UnlockWithCash: 'Spend 300 - unlock 1 HOUR' (premium events): buys a Timed Ticket window with Cash in one tap. Shown without a running window; not interactable without the Cash. May stay empty (old prefab).")]
        [SerializeField] private Button useTimedCashButton;

        [Tooltip("Window/Actions/UnlockWithCash/Label.")]
        [SerializeField] private Text useTimedCashLabel;

        [Tooltip("Window/GetTickets/Caption: 'GET TICKETS', or 'TICKETS FULL' while the inventory is full.")]
        [SerializeField] private Text offersCaption;

        [Tooltip("Window/GetTickets/BuyWithCash: not interactable while full, without a Cash price or without enough Cash.")]
        [SerializeField] private Button buyWithCashButton;

        [Tooltip("Window/GetTickets/BuyWithCash/Label: BUY WITH CASH.")]
        [SerializeField] private Text buyWithCashLabel;

        [Tooltip("Window/GetTickets/BuyWithCash/Price: the Cash price (empty when there is none).")]
        [SerializeField] private Text cashPriceText;

        [Tooltip("Window/GetTickets/WatchAd: not interactable while the ad offer is closed (full inventory, no ad service, ad off in the config) or an ad runs.")]
        [SerializeField] private Button watchAdButton;

        [Tooltip("Window/GetTickets/WatchAd/Label: WATCH AD; hidden while the spinner shows.")]
        [SerializeField] private Text watchAdLabel;

        [Tooltip("Window/GetTickets/WatchAd/Spinner: turns while an ad runs; inactive otherwise.")]
        [SerializeField] private RectTransform watchAdSpinner;

        [Header("Tickets Store Art")]
        [SerializeField] private Sprite storePanelArt;
        [SerializeField] private Sprite storeHeaderArt;
        [SerializeField] private Sprite storeButtonArt;
        [SerializeField] private Sprite storeWatchIcon;

        [Header("Countdown")]
        [Tooltip("Seconds between countdown redraws (Timed window, next ticket).")]
        [Min(0.1f)]
        [SerializeField] private float tickInterval = 1f;

        [Header("Ad loading")]
        [Tooltip("Seconds one full turn of the loading spinner takes.")]
        [Min(0.1f)]
        [SerializeField] private float spinnerTurnSeconds = 1f;

        [Header("Refusal shake")]
        [Tooltip("Seconds the window shakes after a refused entry.")]
        [Min(0f)]
        [SerializeField] private float shakeDuration = 0.35f;

        [Tooltip("Sideways shake size in canvas pixels.")]
        [Min(0f)]
        [SerializeField] private float shakeStrength = 24f;

        [Tooltip("How many times the shake swings.")]
        [Min(1)]
        [SerializeField] private int shakeVibrato = 18;

        private IEntryTickets _tickets;
        private IWallet _wallet;
        private HomeEntryFlow _entryFlow;
        private ScreenNavigator _navigator;
        private RaceEventDefinition _event;
        private bool _storeOnly;
        private GameObject[] _entryContent;
        private Vector2 _entryWindowSize;
        private RectTransform _storePanel;
        private Sprite _storeFrameSprite;
        private Sprite _storeVipIcon;
        private Sprite _storeDiamondIcon;
        private Sprite _storeWatchDisplay;
        private Button _raceAdOffer;
        private Button _raceCashOffer;
        private Button _vipAdOffer;
        private Button _vipCashOffer;
        private Button _activateVip;
        private Text _vipAdLabel;
        private bool _vipAdBusy;
        private IEconomyLedger _ledger;
        private Tween _shake;
        private Tween _punch;
        private Tween _ticketPunch;
        private Tween _spin;
        private Vector3 _timedBaseScale = Vector3.one;
        private Vector3 _ticketBaseScale = Vector3.one;
        private float _elapsed;
        private ITimedAccess _timedAccess;
        private IEntryTicketShop _shop;
        private EconomyConfig _economy;
        private IClock _clock;
        private UiMotionConfig _punchMotion;
        private IRewardedAds _ads;
        private IEntryTicketSource _ticketSource;
        private ProgressRepository _progress;
        private EntryGateAdFlow _adFlow;
        private ITimedTicketShop _timedShop;
        private ISfx _sfx;
        private IHaptics _haptics;
        private TimedCashUnlock _timedCashUnlock;
        private CashFlightIcon _ticketFlight;
        private ResourceBarView _resourceBar;
        private int _pendingAdTickets;
        private Action<int> _revealAdTickets;

        [Inject]
        public void ConstructEntryGate(IEntryTickets tickets, IWallet wallet, HomeEntryFlow entryFlow, ScreenNavigator navigator)
        {
            _tickets = tickets;
            _wallet = wallet;
            _entryFlow = entryFlow;
            _navigator = navigator;
        }

        [Inject]
        public void ConstructEntryGateEconomy(ITimedAccess timedAccess, IEntryTicketShop shop, EconomyConfig economy, IClock clock, UiMotionConfig motion)
        {
            _timedAccess = timedAccess;
            _shop = shop;
            _economy = economy;
            _clock = clock;
            _punchMotion = motion;
        }

        [Inject]
        public void ConstructEntryGateAds(IEntryTicketSource ticketSource, ProgressRepository progress, IEconomyLedger ledger, [InjectOptional] IRewardedAds ads)
        {
            _ticketSource = ticketSource;
            _progress = progress;
            _ledger = ledger;
            _ads = ads; // null in a build without an ad service: WATCH AD stays closed
        }

        /// <summary>Optional so a build before the Timed Ticket shop WIRING shows the gate without the Cash unlock button.</summary>
        [Inject]
        public void ConstructEntryGateTimedCash([InjectOptional] ITimedTicketShop timedShop, [InjectOptional] ISfx sfx, [InjectOptional] IHaptics haptics)
        {
            _timedShop = timedShop;
            _sfx = sfx;
            _haptics = haptics;
        }

        /// <summary>Opens the modal for <paramref name="definition"/> through the navigator's popup layer.</summary>
        public void Open(RaceEventDefinition definition) => Open(definition, false);

        public void OpenStore(RaceEventDefinition definition) => Open(definition, true);

        private void Open(RaceEventDefinition definition, bool storeOnly)
        {
            _event = definition;
            _storeOnly = storeOnly;
            ApplyShopLayout();
            Refresh();
            _navigator.OpenPopup(this);
        }

        private void ApplyShopLayout()
        {
            if (!_storePanel && !_storeOnly) return;
            if (!_storePanel)
            {
                _entryWindowSize = window.sizeDelta;
                var children = new System.Collections.Generic.List<GameObject>();
                foreach (Transform child in window)
                {
                    if (child.name != "Close" && child.gameObject.activeSelf) children.Add(child.gameObject);
                }
                _entryContent = children.ToArray();
                BuildStore();
            }

            window.sizeDelta = _storeOnly ? new Vector2(_entryWindowSize.y * 0.93f, _entryWindowSize.y) : _entryWindowSize;
            foreach (GameObject child in _entryContent) child.SetActive(!_storeOnly);
            _storePanel.gameObject.SetActive(_storeOnly);
        }

        private void BuildStore()
        {
            Color32 navy = new Color32(16, 32, 67, 255);
            Color32 race = new Color32(255, 249, 244, 255);
            Color32 vip = new Color32(255, 242, 201, 255);
            Color32 blue = new Color32(176, 232, 250, 255);
            Color32 red = new Color32(255, 39, 59, 255);
            Color32 gold = new Color32(244, 168, 23, 255);

            // Preserve the day-card sprite's corners and outline when it fills the larger store window.
            const float panelSlice = 18f;
            _storeFrameSprite = Sprite.Create(storePanelArt.texture, storePanelArt.rect, new Vector2(.5f, .5f),
                storePanelArt.pixelsPerUnit, 0, SpriteMeshType.FullRect, Vector4.one * panelSlice);
            _storePanel = StoreRect("Tickets Store", window, 0f, 0f, 1f, 1f);
            Image backdrop = StoreSurface("Background", _storePanel, 0f, -.04f, 1f, 1f, _storeFrameSprite, Color.white);
            backdrop.raycastTarget = true;
            RectTransform header = StoreSurface("Header", _storePanel, .045f, .865f, .955f, .962f, storeHeaderArt, Color.white).rectTransform;
            Text heading = StoreText("TICKETS STORE", header, 0f, 0f, 1f, 1f, 60, Color.white);
            heading.alignByGeometry = true;
            StoreSection("RACE TICKETS", .81f, red, navy);
            StoreSection("TIMED VIP TICKET", .43f, gold, navy);

            Image raceIcon = ticketIcon.GetComponentInChildren<Image>(true);
            Image vipIcon = timedStatus.Find("Icon")?.GetComponentInChildren<Image>(true);
            // These two sprites share their texture canvas with transparent padding.
            _storeVipIcon = CropStoreIcon(vipIcon ? vipIcon.sprite : null, new Rect(52f, 81f, 83f, 68f));
            _storeWatchDisplay = CropStoreIcon(storeWatchIcon, new Rect(30f, 18f, 84f, 68f));
            ResourceBarView bar = FindObjectOfType<ResourceBarView>(true);
            Image cashIcon = null;
            if (bar)
            {
                foreach (Transform item in bar.GetComponentsInChildren<Transform>(true))
                {
                    if (item.name != "Cash") continue;
                    cashIcon = item.Find("Icon")?.GetComponent<Image>();
                    break;
                }
            }
            Sprite diamond = cashIcon ? cashIcon.sprite : null;
            _storeDiamondIcon = CropStoreIcon(diamond, new Rect(23f, 15f, 78f, 68f));
            _economy.TryGetRewardedAdEntryTickets(out int raceAdCount);
            _economy.TryGetEntryTicketCashPrice(out int racePrice);
            _economy.TryGetTimedTicketCashPrice(out int vipPrice);
            _raceAdOffer = StoreCard("Race ad", .04f, .50f, race, navy, raceIcon ? raceIcon.sprite : null, $"x{raceAdCount}", "WATCH 1 AD", navy, Color.white, storeWatchIcon, OnWatchAd);
            _raceCashOffer = StoreCard("Race cash", .52f, .50f, race, navy, raceIcon ? raceIcon.sprite : null, "x1", racePrice.ToString(), blue, navy, _storeDiamondIcon ? _storeDiamondIcon : diamond, OnBuyWithCash);
            _vipAdOffer = StoreCard("VIP ad", .04f, .12f, vip, navy, _storeVipIcon, "x1", "WATCH 3 ADS", navy, Color.white, storeWatchIcon, OnWatchVipAd);
            _vipAdLabel = _vipAdOffer.GetComponentInChildren<Text>();
            _vipCashOffer = StoreCard("VIP cash", .52f, .12f, vip, navy, _storeVipIcon, "x1", vipPrice.ToString(), blue, navy, _storeDiamondIcon ? _storeDiamondIcon : diamond, OnBuyVipTicket);
            _activateVip = StoreButton("USE TIMED TICKET", _storePanel, .25f, .035f, .75f, .098f, gold, navy, null, OnUseTimed);

            Transform close = window.Find("Close");
            if (close) close.SetAsLastSibling();
        }

        private void StoreSection(string caption, float y, Color accent, Color ink)
        {
            StoreImage("Accent", _storePanel, .04f, y, .051f, y + .035f, accent);
            Text label = StoreText(caption, _storePanel, .066f, y - .005f, .40f, y + .04f, 32, ink);
            label.alignment = TextAnchor.MiddleLeft;
            StoreImage("Rule", _storePanel, .42f, y + .016f, .96f, y + .018f, new Color32(209, 200, 187, 255));
        }

        private Button StoreCard(string name, float x, float y, Color fill, Color ink, Sprite icon, string count,
            string actionLabel, Color actionColor, Color textColor, Sprite actionIcon, UnityEngine.Events.UnityAction action)
        {
            RectTransform card = StoreSurface(name, _storePanel, x, y, x + .44f, y + .29f, storePanelArt, fill).rectTransform;
            if (icon == _storeVipIcon)
                StoreIcon(icon, card, .13f, .425f, .48f, .935f);
            else
                StoreIcon(icon, card, .105f, .39f, .505f, .97f);
            StoreText(count, card, .50f, .41f, .80f, .93f, 72, ink);
            return StoreButton(actionLabel, card, .095f, .072f, .905f, .342f, actionColor, textColor, actionIcon, action);
        }

        private Button StoreButton(string caption, RectTransform parent, float left, float bottom, float right, float top,
            Color fill, Color ink, Sprite icon, UnityEngine.Events.UnityAction action)
        {
            Image face = StoreSurface(caption, parent, left, bottom, right, top, storeButtonArt, fill);
            RectTransform rect = face.rectTransform;
            face.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.onClick.AddListener(action);

            if (icon && icon == storeWatchIcon)
            {
                StoreIcon(_storeWatchDisplay ? _storeWatchDisplay : icon, rect, .09f, .23f, .25f, .77f);
                StoreText(caption, rect, .29f, .06f, .94f, .94f, 34, ink);
            }
            else if (icon)
            {
                // Keep the price group centered and size the diamond against the button height.
                float groupLeft = caption.Length > 2 ? .27f : .30f;
                float priceLeft = groupLeft + .21f;
                StoreIcon(icon, rect, groupLeft, .12f, priceLeft - .02f, .88f);
                Text price = StoreText(caption, rect, priceLeft, .06f, .81f, .94f, 48, ink);
                price.alignment = TextAnchor.MiddleLeft;
            }
            else
            {
                StoreText(caption, rect, .06f, .06f, .94f, .94f, 34, ink);
            }
            return button;
        }

        private static Image StoreSurface(string name, RectTransform parent, float left, float bottom, float right, float top,
            Sprite sprite, Color tint)
        {
            Image image = StoreImage(name, parent, left, bottom, right, top, tint);
            image.sprite = sprite;
            if (sprite && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        private static Image StoreImage(string name, RectTransform parent, float left, float bottom, float right, float top, Color color)
        {
            Image image = StoreRect(name, parent, left, bottom, right, top).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite CropStoreIcon(Sprite source, Rect crop)
        {
            if (!source || crop.xMax > source.rect.width || crop.yMax > source.rect.height) return null;
            crop.position += source.rect.position;
            Sprite icon = Sprite.Create(source.texture, crop, new Vector2(.5f, .5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            icon.name = source.name;
            return icon;
        }

        private static void StoreIcon(Sprite source, RectTransform parent, float left, float bottom, float right, float top)
        {
            if (!source) return;
            Image icon = StoreImage(source.name, parent, left, bottom, right, top, Color.white);
            icon.sprite = source;
            icon.preserveAspect = true;
        }

        private Text StoreText(string value, RectTransform parent, float left, float bottom, float right, float top, float size, Color color)
        {
            Text label = StoreRect(value, parent, left, bottom, right, top).gameObject.AddComponent<Text>();
            label.font = titleText.font;
            label.text = value;
            label.fontSize = Mathf.RoundToInt(size);
            label.resizeTextMaxSize = Mathf.RoundToInt(size);
            label.resizeTextMinSize = Mathf.RoundToInt(size * .6f);
            label.resizeTextForBestFit = true;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform StoreRect(string name, RectTransform parent, float left, float bottom, float right, float top)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void RefreshStore(EntryGateModel model, EntryGateAdFlow ad)
        {
            if (!_storeOnly || !_storePanel) return;
            _raceCashOffer.interactable = model.BuyWithCashInteractable;
            _raceAdOffer.interactable = watchAdButton.interactable && !_vipAdBusy;
            _vipCashOffer.interactable = _timedShop != null && _economy.TryGetTimedTicketCashPrice(out int price) && _wallet.Cash >= price;
            _vipAdOffer.interactable = !_vipAdBusy && !ad.IsBusy && _ads != null && _ads.IsReady;
            int watched = _progress.Current.VipStoreAdsWatched;
            _vipAdLabel.text = _vipAdBusy ? "LOADING..." : watched > 0 ? $"WATCH {3 - watched} {(watched == 2 ? "AD" : "ADS")}" : "WATCH 3 ADS";
            _activateVip.gameObject.SetActive(model.ShowUseTimedButton);
        }

        private void OnBuyVipTicket()
        {
            if (_timedShop.TryBuyWithCash(Guid.NewGuid().ToString("N")) != TimedTicketPurchaseResult.Bought) Shake();
            Refresh();
        }

        private void OnWatchVipAd()
        {
            if (_vipAdBusy || AdFlow.IsBusy || _ads == null || !_ads.IsReady) return;

            _vipAdBusy = true;
            Refresh();
            bool answered = false;
            try
            {
                _ads.Show(result =>
                {
                    if (answered) return;
                    answered = true;
                    _vipAdBusy = false;
                    if (result == AdResult.Completed)
                    {
                        PlayerProgress progress = _progress.Current;
                        int watched = progress.VipStoreAdsWatched + 1;
                        progress.VipStoreAdsWatched = watched % 3;
                        if (watched == 3) _ledger.Grant(EconomyCurrency.TimedTicket, 1, Guid.NewGuid().ToString("N"));
                        SaveProgress();
                    }
                    if (this && isActiveAndEnabled) Refresh();
                });
            }
            catch
            {
                _vipAdBusy = false;
                Refresh();
                throw;
            }
        }


        /// <summary>Built on first use (after injection); the flow outlives the window being closed, so a late ad result still pays out.</summary>
        private EntryGateAdFlow AdFlow => _adFlow ?? (_adFlow = new EntryGateAdFlow(_ads, _ticketSource, _economy, SaveProgress));

        protected override void Awake()
        {
            base.Awake();
            useTicketButton.onClick.AddListener(OnUseTicket);
            useTimedButton.onClick.AddListener(OnUseTimed);
            buyWithCashButton.onClick.AddListener(OnBuyWithCash);
            watchAdButton.onClick.AddListener(OnWatchAd);
            if (useTimedCashButton != null) useTimedCashButton.onClick.AddListener(OnUseTimedCash);
            titleText.text = HomeTexts.EntryGateTitle;
            useTicketLabel.text = HomeTexts.UseEntryTicket;
            useTimedLabel.text = HomeTexts.UseTimedTicket;
            buyWithCashLabel.text = HomeTexts.BuyWithCash;
            watchAdLabel.text = HomeTexts.WatchAd;
            _timedBaseScale = timedStatus.localScale; // the designer's scale; the punch always returns to it
            _ticketBaseScale = ticketIcon.localScale;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _tickets.Changed += Refresh;
            _wallet.Changed += Refresh;
            Refresh();
        }

        protected override void OnDisable()
        {
            _tickets.Changed -= Refresh;
            _wallet.Changed -= Refresh;
            KillShake();
            KillPunch();
            if (_ticketFlight) _ticketFlight.Cancel();
            FinishAdTicketFlight();
            KillSpin(); // an ad that is still running keeps going; Refresh restores the spinner on the next open
            base.OnDisable();
        }

        private void Update()
        {
            if (_event == null) return;

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < tickInterval) return;
            _elapsed -= tickInterval;
            Refresh(); // the countdowns (Timed window, next ticket); also flips the screen when the window runs out
        }

        protected override void OnDestroy()
        {
            if (useTicketButton != null) useTicketButton.onClick.RemoveListener(OnUseTicket);
            if (useTimedButton != null) useTimedButton.onClick.RemoveListener(OnUseTimed);
            if (buyWithCashButton != null) buyWithCashButton.onClick.RemoveListener(OnBuyWithCash);
            if (watchAdButton != null) watchAdButton.onClick.RemoveListener(OnWatchAd);
            if (useTimedCashButton != null) useTimedCashButton.onClick.RemoveListener(OnUseTimedCash);
            if (_storeFrameSprite) Destroy(_storeFrameSprite);
            if (_storeVipIcon) Destroy(_storeVipIcon);
            if (_storeDiamondIcon) Destroy(_storeDiamondIcon);
            if (_storeWatchDisplay) Destroy(_storeWatchDisplay);
            base.OnDestroy();
        }

        private void Refresh()
        {
            if (_event == null) return;

            bool timedActive = _wallet.TimedAccessActive;
            TimeSpan remaining = timedActive && _wallet.TimedAccessEndsUtc.HasValue ? _wallet.TimedAccessEndsUtc.Value - _clock.UtcNow : TimeSpan.Zero;
            int cashPrice = _economy != null && _economy.TryGetEntryTicketCashPrice(out int price) ? price : 0;
            EntryGateAdFlow ad = AdFlow;
            int timedCashPrice = _timedShop != null && _economy != null && _economy.TryGetTimedTicketCashPrice(out int timedPrice) ? timedPrice : 0;
            var state = new EntryGateState(_tickets.Count, _tickets.Cap, timedActive, _wallet.TimedTickets, remaining, cashPrice, _wallet.Cash, adAvailable: ad.IsOffered,
                timedCashPrice: timedCashPrice, accessDuration: _economy != null ? _economy.TimedAccessDuration : TimeSpan.Zero);
            EntryGateModel model = EntryGateModel.Build(state);

            titleText.text = _storeOnly ? "TICKETS STORE" : HomeTexts.EntryGateTitle;
            eventNameText.text = _storeOnly ? "RACE TICKETS" : _event.DisplayName;
            ticketCountText.text = HomeTexts.TicketsHeld(HomeTexts.TicketCount(Mathf.Max(0, _tickets.Count - _pendingAdTickets), _tickets.Cap));
            DateTime? nextTicket = _tickets.NextTicketUtc;
            refillText.gameObject.SetActive(!_storeOnly && nextTicket.HasValue);
            if (nextTicket.HasValue) refillText.text = HomeTexts.NextTicketIn(CountdownFormatter.Format(nextTicket.Value - _clock.UtcNow));
            useTimedButton.gameObject.SetActive(model.ShowUseTimedButton);
            if (useTimedCashButton != null)
            {
                useTimedCashButton.gameObject.SetActive(model.ShowTimedCashButton);
                useTimedCashButton.interactable = model.TimedCashInteractable;
                if (useTimedCashLabel != null) useTimedCashLabel.text = model.TimedCashLabel;
            }
            offersCaption.text = _storeOnly ? "REFILL TICKETS" : model.OffersCaption;
            buyWithCashButton.interactable = model.BuyWithCashInteractable;
            cashPriceText.text = model.CashPriceText;
            watchAdButton.interactable = model.WatchAdInteractable && !ad.IsBusy && _pendingAdTickets == 0;
            watchAdLabel.gameObject.SetActive(!ad.IsBusy);
            SetSpinner(ad.IsBusy);
            useTicketButton.gameObject.SetActive(!_storeOnly && model.ShowUseTicketButton);
            useTicketButton.interactable = model.UseTicketInteractable;
            remainingText.gameObject.SetActive(false); // a refusal warning (OnUseTicket) lives until the next redraw
            useTicketLabel.color = model.UseTicketInteractable ? useTicketLabelReady : useTicketLabelDisabled;
            timedLabel.text = _storeOnly ? (timedActive ? "VIP ACTIVE" : "TIMED VIP TICKET") : model.TimedStatusText;
            timedValue.text = model.TimedValueText;
            RefreshStore(model, ad);
        }

        private void OnUseTicket()
        {
            if (_event == null) return;

            EntryOutcome outcome = _entryFlow.TryEnter(_event.Id);
            if (outcome == EntryOutcome.StartRace)
            {
                _navigator.ClosePopup(this);
                return;
            }

            remainingText.text = HomeEntryRules.WarningFor(outcome);
            remainingText.gameObject.SetActive(true);
            refillText.gameObject.SetActive(false); // the warning takes the refill line's place
            Shake();
        }

        private void OnUseTimed()
        {
            TimedAccessResult result = _timedAccess.TryActivate();
            if (result == TimedAccessResult.Activated)
            {
                Refresh();
                PlayPunch();
                return;
            }
            if (result != TimedAccessResult.AlreadyActive) Shake();
        }

        /// <summary>Premium event without access: spend the Timed Ticket price and start the window in one tap.</summary>
        private void OnUseTimedCash()
        {
            if (_timedCashUnlock == null) _timedCashUnlock = new TimedCashUnlock(_timedShop, _timedAccess, () => _wallet.TimedAccessActive);

            TimedCashUnlockResult result = _timedCashUnlock.TryUnlock();
            Refresh();
            switch (result)
            {
                case TimedCashUnlockResult.Unlocked:
                    _sfx?.Play(SfxId.Purchase);
                    _haptics?.Tap();
                    PlayPunch();
                    break;
                case TimedCashUnlockResult.BoughtNotStarted: // the ticket is in stock; "Use Timed Ticket" shows now
                    _sfx?.Play(SfxId.Purchase);
                    break;
                case TimedCashUnlockResult.AlreadyActive:
                    break;
                default:
                    Shake();
                    break;
            }
        }

        private void OnBuyWithCash()
        {
            TicketPurchaseResult result = _shop.TryBuyWithCash(Guid.NewGuid().ToString("N")); // one id per tap
            if (result != TicketPurchaseResult.Bought && result != TicketPurchaseResult.AlreadyProcessed) Shake();
            Refresh();
        }

        private void OnWatchAd()
        {
            EntryGateAdFlow ad = AdFlow;
            if (!ad.IsAvailable || _vipAdBusy || _pendingAdTickets > 0) return;

            int before = _tickets.Count;
            if (ad.TryStart(outcome => OnAdFinished(outcome, before))) Refresh();
        }

        /// <summary>
        /// The ad ended, maybe after the window was closed or the scene left. The flow has already paid out by now; this
        /// only shows it, and only while the window is up.
        /// </summary>
        private void OnAdFinished(AdWatchOutcome outcome, int previousBalance)
        {
            if (this == null || !isActiveAndEnabled) return;

            if (outcome != AdWatchOutcome.Granted)
            {
                Refresh();
                Shake();
                return;
            }

            int awarded = _tickets.Count - previousBalance;
            Image art = ticketIcon ? ticketIcon.GetComponentInChildren<Image>() : null;
            if (awarded <= 0 || !art || !art.sprite)
            {
                Refresh();
                PlayTicketPunch();
                return;
            }

            if (!_ticketFlight)
            {
                var host = new GameObject("Ad Ticket Burst", typeof(RectTransform), typeof(Image), typeof(CashFlightIcon));
                host.SetActive(false);
                var rect = (RectTransform)host.transform;
                rect.SetParent(transform, false);
                rect.sizeDelta = Vector2.one * Mathf.Max(85f, art.rectTransform.rect.height);
                Image image = host.GetComponent<Image>();
                image.sprite = art.sprite;
                image.raycastTarget = false;
                _ticketFlight = host.GetComponent<CashFlightIcon>();
            }

            if (!_resourceBar) _resourceBar = FindObjectOfType<ResourceBarView>();
            if (_resourceBar) _revealAdTickets = _resourceBar.HoldReward(SevenDayRewardKind.EntryTicket, awarded);
            _pendingAdTickets = awarded;
            Refresh();

            _ticketFlight.PlayBurst(art.rectTransform, awarded, (arrived, count) =>
            {
                int portion = (int)((long)awarded * arrived / count - (long)awarded * (arrived - 1) / count);
                _pendingAdTickets -= portion;
                _revealAdTickets?.Invoke(portion);
                if (arrived == count)
                {
                    FinishAdTicketFlight();
                    if (_resourceBar) _resourceBar.SlideCounter(SevenDayRewardKind.EntryTicket);
                    PlayTicketPunch();
                }
                Refresh();
            });
        }

        private void FinishAdTicketFlight()
        {
            _pendingAdTickets = 0;
            _revealAdTickets?.Invoke(int.MaxValue);
            _revealAdTickets = null;
        }

        /// <summary>The ad ticket is written to the save right away (the grant itself does not save).</summary>
        private void SaveProgress()
        {
            _progress.Save();
        }

        private void SetSpinner(bool running)
        {
            watchAdSpinner.gameObject.SetActive(running);
            if (!running)
            {
                KillSpin();
                return;
            }
            if (_spin != null) return;

            watchAdSpinner.localRotation = Quaternion.identity;
            _spin = watchAdSpinner
                .DOLocalRotate(new Vector3(0f, 0f, -360f), spinnerTurnSeconds, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillSpin()
        {
            if (_spin == null) return;
            _spin.Kill();
            _spin = null;
        }

        /// <summary>The ticket icon punches when an ad ticket arrives (numbers from UiMotionConfig).</summary>
        private void PlayTicketPunch()
        {
            if (_punchMotion == null) return;

            if (_ticketPunch != null) _ticketPunch.Kill();
            ticketIcon.localScale = _ticketBaseScale;
            _ticketPunch = ticketIcon
                .DOPunchScale(_ticketBaseScale * _punchMotion.ButtonPunchScale, _punchMotion.ButtonPunchDuration, _punchMotion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <summary>The Timed Ticket box punches when its window starts (numbers from UiMotionConfig).</summary>
        private void PlayPunch()
        {
            if (_punchMotion == null) return;

            KillPunch();
            timedStatus.localScale = _timedBaseScale;
            _punch = timedStatus
                .DOPunchScale(_timedBaseScale * _punchMotion.ButtonPunchScale, _punchMotion.ButtonPunchDuration, _punchMotion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillPunch()
        {
            if (_punch != null)
            {
                _punch.Kill();
                _punch = null;
            }
            if (timedStatus != null) timedStatus.localScale = _timedBaseScale;
            if (_ticketPunch != null)
            {
                _ticketPunch.Kill();
                _ticketPunch = null;
            }
            if (ticketIcon != null) ticketIcon.localScale = _ticketBaseScale;
        }

        private void Shake()
        {
            KillShake();
            _shake = window.DOShakePosition(shakeDuration, new Vector3(shakeStrength, 0f, 0f), shakeVibrato, 0f, false, true)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillShake()
        {
            if (_shake == null) return;
            Tween running = _shake;
            _shake = null;
            running.Kill(true); // complete: the window ends on its designed position
        }
    }
}
