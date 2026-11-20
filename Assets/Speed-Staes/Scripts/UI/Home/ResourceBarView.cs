using System;
using System.Collections.Generic;
using DG.Tweening;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Resource bar (prefab ResourceBar, on the overlay canvas): Purple Gems, Entry Tickets with the regen countdown,
    /// Timed Tickets and Cash. Visible while the Home screen or one of <c>alsoVisibleOn</c> (theme select, emote) is the top
    /// screen, so those screens show this same bar instead of a copy; other full screens have their own columns. Popups do not hide it. Redraws when the wallet or tickets change; about once a second it calls
    /// <c>IEntryTickets.Refresh</c> (regen is lazy) and updates the countdown. Draws claimed rewards on the shared overlay.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ResourceBarView : MonoBehaviour
    {
        [Tooltip("PurpleGem/Value.")]
        [SerializeField] private TMP_Text purpleGemText;

        [Tooltip("EntryTicket/Value: 'count/cap'.")]
        [SerializeField] private TMP_Text entryTicketText;

        [Tooltip("TicketTimer (whole chip): hidden when the tickets are full.")]
        [SerializeField] private GameObject entryTimerRoot;

        [Tooltip("TicketTimer/Value: time to the next ticket.")]
        [SerializeField] private TMP_Text entryTimerText;

        [Tooltip("TimedTicket/Value: unused Timed Tickets, or the time left while the window runs.")]
        [SerializeField] private TMP_Text timedTicketText;

        [Tooltip("Cash/Value.")]
        [SerializeField] private TMP_Text cashText;

        [Tooltip("Cash/Plus: opens the Cash offer window (watch an ad for Cash). Not interactable while no window is bound. May stay empty (old prefab).")]
        [SerializeField] private Button cashPlusButton;

        [Tooltip("The Cash offer window (CashOfferModal prefab instance, starts inactive). Scene-bound; SceneWiring fills it.")]
        [SerializeField] private CashOfferModalView cashOfferModal;

        [Tooltip("EntryTicket (the whole box): where the daily gift ticket flies to and punches. Empty = the parent of the ticket value text.")]
        [SerializeField] private RectTransform entryTicketBox;

        [Tooltip("The Home screen (root of the stack): the bar is shown only while it is the top screen. Empty = always shown.")]
        [SerializeField] private UiScreen homeScreen;

        [Tooltip("Other screens that keep the bar visible (theme select, emote). Scene-bound; SceneWiring fills it.")]
        [SerializeField] private UiScreen[] alsoVisibleOn;

        [Tooltip("Seconds between ticket refresh ticks.")]
        [Min(0.1f)]
        [SerializeField] private float tickInterval = 1f;

        private IWallet _wallet;
        private IEntryTickets _tickets;
        private IClock _clock;
        private ScreenNavigator _navigator;
        private CanvasGroup _group;
        private float _elapsed;
        private readonly Dictionary<SevenDayRewardKind, int> _inFlight = new Dictionary<SevenDayRewardKind, int>();
        private UiScreen _wardrobeOwner;
        private Vector2 _homePosition, _wardrobePosition;

        /// <summary>The same live Home resource boxes, placed below the wardrobe Back button.</summary>
        public void ShowInWardrobe(UiScreen owner, RectTransform backButton)
        {
            var rect = (RectTransform)transform;
            if (_wardrobeOwner == null) _homePosition = rect.anchoredPosition;
            _wardrobeOwner = owner;
            var parent = (RectTransform)rect.parent;
            var corners = new Vector3[4];
            backButton.GetWorldCorners(corners);
            float belowBack = parent.rect.yMax - parent.InverseTransformPoint(corners[0]).y + 16f;
            _wardrobePosition = new Vector2(_homePosition.x, -belowBack);
            ApplyVisibility();
        }

        public void LeaveWardrobe(UiScreen owner)
        {
            if (_wardrobeOwner != owner) return;
            ((RectTransform)transform).anchoredPosition = _homePosition;
            _wardrobeOwner = null;
            ApplyVisibility();
        }

        /// <summary>The Entry Ticket box of the bar: the target of the daily gift ticket flight.</summary>
        public RectTransform EntryTicketBox => entryTicketBox != null ? entryTicketBox : entryTicketText.transform.parent as RectTransform;

        [Inject]
        public void ConstructResourceBar(IWallet wallet, IEntryTickets tickets, IClock clock)
        {
            _wallet = wallet;
            _tickets = tickets;
            _clock = clock;
        }

        /// <summary>Optional so a scene without a navigator keeps the bar always shown.</summary>
        [Inject]
        public void ConstructResourceBarNavigation([InjectOptional] ScreenNavigator navigator)
        {
            _navigator = navigator;
        }

        private void OnEnable()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_navigator != null) _navigator.CurrentScreenChanged += OnScreenChanged;
            if (cashPlusButton != null)
            {
                cashPlusButton.interactable = cashOfferModal != null;
                cashPlusButton.onClick.AddListener(OnCashPlus);
            }
            ApplyVisibility();
            _wallet.Changed += Redraw;
            _tickets.Changed += Redraw;
            _elapsed = 0f;
            _tickets.Refresh();
            Redraw();
        }

        private void OnDisable()
        {
            if (_navigator != null) _navigator.CurrentScreenChanged -= OnScreenChanged;
            if (cashPlusButton != null) cashPlusButton.onClick.RemoveListener(OnCashPlus);
            _wallet.Changed -= Redraw;
            _tickets.Changed -= Redraw;
            DOTween.Kill(this);
            _inFlight.Clear();
        }

        private void OnScreenChanged(IUiView top) => ApplyVisibility();

        private void OnCashPlus()
        {
            if (cashOfferModal != null) cashOfferModal.Open();
        }

        /// <summary>Shown before the first screen opens and while Home is on top; a CanvasGroup so the bar keeps ticking.</summary>
        private void ApplyVisibility()
        {
            IUiView top = _navigator?.CurrentScreen;
            bool wardrobe = _wardrobeOwner != null && ReferenceEquals(top, _wardrobeOwner);
            if (_wardrobeOwner != null) ((RectTransform)transform).anchoredPosition = wardrobe ? _wardrobePosition : _homePosition;
            bool visible = wardrobe || homeScreen == null || top == null || ReferenceEquals(top, homeScreen) || IsAlsoVisibleOn(top);
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
            _group.interactable = visible;
        }

        private bool IsAlsoVisibleOn(IUiView top)
        {
            if (alsoVisibleOn == null) return false;
            foreach (UiScreen screen in alsoVisibleOn)
            {
                if (screen != null && ReferenceEquals(top, screen)) return true;
            }
            return false;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < tickInterval) return;

            _elapsed -= tickInterval;
            _tickets.Refresh(); // may raise Changed, which redraws
            RedrawCountdown();
            RedrawTimed();
        }

        private void Redraw()
        {
            purpleGemText.text = _wallet.PurpleGems.ToString();
            cashText.text = VisibleBalance(SevenDayRewardKind.Cash).ToString();
            RedrawTimed();
            entryTicketText.text = HomeTexts.TicketCount(VisibleBalance(SevenDayRewardKind.EntryTicket), _tickets.Cap);
            RedrawCountdown();
        }

        /// <summary>Stock normally; the countdown while a Timed Ticket window runs (the once-a-second tick keeps it moving).</summary>
        private void RedrawTimed()
        {
            DateTime? ends = _wallet.TimedAccessEndsUtc;
            timedTicketText.text = _wallet.TimedAccessActive && ends.HasValue
                ? CountdownFormatter.Format(ends.Value - _clock.UtcNow)
                : VisibleBalance(SevenDayRewardKind.TimedTicket).ToString();
        }

        internal int Balance(SevenDayRewardKind kind)
        {
            switch (kind)
            {
                case SevenDayRewardKind.Cash: return _wallet.Cash;
                case SevenDayRewardKind.EntryTicket: return _tickets.Count;
                case SevenDayRewardKind.TimedTicket: return _wallet.TimedTickets;
                default: return 0;
            }
        }

        private int Pending(SevenDayRewardKind kind) => _inFlight.TryGetValue(kind, out int amount) ? amount : 0;
        private int VisibleBalance(SevenDayRewardKind kind) => Mathf.Max(0, Balance(kind) - Pending(kind));

        // The reward is already saved. Hold only its presentation so unrelated wallet changes remain live.
        internal Action<int> HoldReward(SevenDayRewardKind kind, int amount)
        {
            int remaining = amount;
            _inFlight[kind] = Pending(kind) + remaining;
            Redraw();
            return count =>
            {
                int revealed = Mathf.Min(remaining, count);
                if (revealed <= 0) return;
                remaining -= revealed;
                int pending = Pending(kind) - revealed;
                if (pending > 0) _inFlight[kind] = pending;
                else _inFlight.Remove(kind);
                if (isActiveAndEnabled) Redraw();
            };
        }

        internal void FlyReward(SevenDayRewardKind kind, int previousBalance, RectTransform source)
        {
            int amount = Balance(kind) - previousBalance;
            TMP_Text label = RewardLabel(kind);
            if (amount <= 0 || !source || !label || !isActiveAndEnabled || _group.alpha <= 0f) return;

            RectTransform box = label.rectTransform.parent as RectTransform;
            Image target = box ? box.Find("Icon")?.GetComponent<Image>() : null;
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (!target || !target.sprite || !parentCanvas) return;

            Canvas root = parentCanvas.rootCanvas;
            var layer = new GameObject("Reward Attraction", typeof(RectTransform), typeof(Canvas));
            var area = (RectTransform)layer.transform;
            area.SetParent(root.transform, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;
            var overlay = layer.GetComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingLayerID = root.sortingLayerID;
            overlay.sortingOrder = 30000;

            Vector2 center = area.rect.center;
            Vector2 artCenter = RewardArtCenter(kind);
            Action<int> reveal = HoldReward(kind, amount);
            var flight = DOTween.Sequence().SetUpdate(true).SetTarget(this).SetLink(gameObject);
            const int iconsPerWave = 3;
            int icons = Mathf.Clamp(amount, iconsPerWave, 12);
            float size = Mathf.Clamp(source.rect.height * 0.65f, area.rect.height * 0.17f, area.rect.height * 0.21f);
            float spriteAspect = target.sprite.rect.width / target.sprite.rect.height;
            Vector2 iconSize = new Vector2(size * Mathf.Max(1f, spriteAspect), size);
            for (int i = 0; i < icons; i++)
            {
                int portion = (int)((long)amount * (i + 1) / icons - (long)amount * i / icons);
                int lane = i % iconsPerWave;
                int groupSize = Mathf.Min(iconsPerWave, icons - i + lane);
                Vector2 start = center + Vector2.right * ((lane - (groupSize - 1) * 0.5f) * size * 0.95f);
                float launch = (i / iconsPerWave) * 0.34f + lane * 0.035f;
                const float centerHold = 0.12f;
                const float travelSeconds = 0.64f;
                var icon = new GameObject("Reward Icon", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)icon.transform;
                rect.SetParent(area, false);
                rect.pivot = artCenter;
                rect.sizeDelta = iconSize;
                rect.anchoredPosition = start;
                var image = icon.GetComponent<Image>();
                image.sprite = target.sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                icon.SetActive(false);

                flight.InsertCallback(launch, () => icon.SetActive(true));
                flight.Insert(launch + centerHold, DOVirtual.Float(0f, 1f, travelSeconds, progress =>
                {
                    if (!target) return;
                    Vector2 end = InFlightLayer(target, artCenter, area, root);
                    float arc = Mathf.Min(Vector2.Distance(start, end) * 0.22f, 125f);
                    rect.anchoredPosition = Vector2.Lerp(start, end, progress) + Vector2.up * (arc * Mathf.Sin(Mathf.PI * progress));
                    rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.42f, progress);
                }).SetEase(Ease.InOutSine));
                flight.InsertCallback(launch + centerHold + travelSeconds, () =>
                {
                    icon.SetActive(false);
                    reveal(portion);
                    if (portion > 0) SlideCounter(label);
                });
            }
            flight.OnKill(() =>
            {
                reveal(int.MaxValue);
                if (layer) Destroy(layer);
            });
        }

        private TMP_Text RewardLabel(SevenDayRewardKind kind)
        {
            switch (kind)
            {
                case SevenDayRewardKind.Cash: return cashText;
                case SevenDayRewardKind.EntryTicket: return entryTicketText;
                case SevenDayRewardKind.TimedTicket: return timedTicketText;
                default: return null;
            }
        }

        internal void SlideCounter(SevenDayRewardKind kind) => SlideCounter(RewardLabel(kind));

        private void SlideCounter(TMP_Text label)
        {
            if (!label || DOTween.IsTweening(label.rectTransform)) return;
            RectTransform rect = label.rectTransform;
            Vector2 rest = rect.anchoredPosition;
            DOVirtual.Float(0f, 1f, 0.15f, t => rect.anchoredPosition = rest + Vector2.up * (6f * Mathf.Sin(Mathf.PI * t)))
                .SetEase(Ease.Linear).SetUpdate(true).SetTarget(rect).SetLink(gameObject)
                .OnKill(() => { if (rect) rect.anchoredPosition = rest; });
        }

        // The shared HUD art includes transparent padding. Use the center of its visible pixels.
        private static Vector2 RewardArtCenter(SevenDayRewardKind kind)
        {
            switch (kind)
            {
                case SevenDayRewardKind.Cash: return new Vector2(62f / 243f, 49f / 102f);
                case SevenDayRewardKind.TimedTicket: return new Vector2(61.5f / 243f, 47.5f / 102f);
                default: return new Vector2(0.5f, 0.5f);
            }
        }

        private static Vector2 InFlightLayer(Image image, Vector2 artCenter, RectTransform area, Canvas overlayRoot)
        {
            RectTransform rect = image.rectTransform;
            Rect drawing = image.GetPixelAdjustedRect();
            if (image.preserveAspect)
            {
                Vector2 spriteSize = image.sprite.rect.size;
                Vector2 fit = spriteSize * Mathf.Min(drawing.width / spriteSize.x, drawing.height / spriteSize.y);
                drawing.position += Vector2.Scale(drawing.size - fit, rect.pivot);
                drawing.size = fit;
            }
            Vector2 center = drawing.position + Vector2.Scale(drawing.size, artCenter);
            Canvas sourceRoot = image.canvas.rootCanvas;
            Camera sourceCamera = sourceRoot.renderMode == RenderMode.ScreenSpaceOverlay ? null : sourceRoot.worldCamera;
            Camera overlayCamera = overlayRoot.renderMode == RenderMode.ScreenSpaceOverlay ? null : overlayRoot.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, rect.TransformPoint(center));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, overlayCamera, out Vector2 position);
            return position;
        }

        private void RedrawCountdown()
        {
            DateTime? next = _tickets.NextTicketUtc;
            bool running = !_tickets.IsFull && next.HasValue;

            entryTimerRoot.SetActive(running);
            if (running) entryTimerText.text = CountdownFormatter.Format(next.Value - _clock.UtcNow);
        }
    }
}
