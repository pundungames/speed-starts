using System.Collections.Generic;
using SpeedStars.Core.Audio;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Costume;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Rewards
{
    /// <summary>
    /// The 7 day rewards panel (prefab SevenDayCheckInPanel, a popup opened by Home's calendar button): the title, the X
    /// (UiPanel's close button), six small day cards, the big day 7 card with the real Premium 02 hat picture, and CLAIM.
    /// CLAIM works only while a card is available and takes exactly that card through
    /// <see cref="ISevenDayRewards.Claim"/>; the card then turns CLAIMED with a punch and the next day unlocks. The cards
    /// are prefab instances placed in the panel (always seven, so they are visible and editable in the Inspector); the
    /// hat picture comes from the wardrobe's own cosmetic-id mapping (<see cref="CostumeEditorConfig"/>), so the icon and the
    /// item given are the same record. Every decision is <see cref="RewardPanelsModel"/>; this view binds and fills.
    /// </summary>
    public sealed class SevenDayPanelView : UiPopup
    {
        [Tooltip("Window/Title: 7 DAY REWARDS.")]
        [SerializeField] private Text titleText;

        [Tooltip("The seven day cards, element 0 = day 1 ... element 6 = day 7 (the big premium card).")]
        [SerializeField] private RewardDayCardView[] dayCards = new RewardDayCardView[0];

        [Header("CLAIM")]
        [Tooltip("Window/Claim: takes the available day's reward; dead while no day is available.")]
        [SerializeField] private Button claimButton;

        [Tooltip("Window/Claim/Label: CLAIM.")]
        [SerializeField] private Text claimLabel;

        [Header("Items")]
        [Tooltip("The wardrobe's content asset: cosmetic id to picture (the same mapping the costume screen uses).")]
        [SerializeField] private CostumeEditorConfig costumeConfig;

        private ISevenDayRewards _rewards;
        private ICosmeticService _cosmetics;
        private UiMotionConfig _cardMotion;
        private ISfx _sfx;
        private bool _claiming;

        [Inject]
        public void ConstructSevenDay(ISevenDayRewards rewards, UiMotionConfig motion, [InjectOptional] ICosmeticService cosmetics)
        {
            _rewards = rewards;
            _cardMotion = motion;
            _cosmetics = cosmetics;
        }

        /// <summary>Optional: without a bound sound system the claim is silent.</summary>
        [Inject]
        public void InjectAudio([InjectOptional] ISfx sfx)
        {
            _sfx = sfx;
        }

        protected override void Awake()
        {
            base.Awake();
            claimButton.onClick.AddListener(OnClaim);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_rewards == null) return;

            titleText.text = RewardTexts.SevenDayTitle;
            claimLabel.text = RewardTexts.Claim;
            _claiming = false;
            _rewards.Changed += Redraw;
            Redraw();
        }

        protected override void OnDisable()
        {
            if (_rewards != null) _rewards.Changed -= Redraw;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (claimButton != null) claimButton.onClick.RemoveListener(OnClaim);
            base.OnDestroy();
        }

        // ---------------------------------------------------------------- drawing

        private void Redraw()
        {
            IReadOnlyList<SevenDayCard> cards = _rewards.Cards;
            var states = new List<SevenDayCardState>(cards.Count);
            for (int i = 0; i < cards.Count; i++)
            {
                SevenDayCard card = cards[i];
                states.Add(card.State);

                int index = card.DayNumber - 1;
                if (index < 0 || index >= dayCards.Length || dayCards[index] == null) continue;

                RewardIcon icon = RewardPanelsModel.IconOf(card.Kind.ToString());
                DayCardLayout layout = RewardPanelsModel.DayCard(card.DayNumber, card.State, icon, card.Amount);
                Sprite picture = null;
                string caption = string.Empty;
                if (icon == RewardIcon.Cosmetic)
                {
                    picture = costumeConfig != null ? costumeConfig.IconOf(card.CosmeticId) : null;
                    caption = CaptionOf(card.CosmeticId);
                }
                dayCards[index].Apply(layout, picture, caption);
            }
            claimButton.interactable = RewardPanelsModel.SevenDayClaimEnabled(states, _claiming);
        }

        /// <summary>The wearable's caption from its own record ("PREMIUM 02 HEADWEAR"); empty when the item is unknown.</summary>
        private string CaptionOf(string cosmeticId)
        {
            if (_cosmetics == null || !_cosmetics.TryGetItem(cosmeticId, out CosmeticItemInfo item)) return string.Empty;
            return RewardPanelsModel.CosmeticCaption(item.Name, item.Category.ToString(), item.IsPremium);
        }

        // ---------------------------------------------------------------- taps

        private void OnClaim()
        {
            if (_claiming) return;

            var states = new List<SevenDayCardState>();
            foreach (SevenDayCard card in _rewards.Cards) states.Add(card.State);
            int available = RewardPanelsModel.AvailableIndex(states);
            if (available < 0) return;

            int dayIndex = _rewards.Cards[available].DayNumber - 1;
            _claiming = true;
            claimButton.interactable = false;
            SevenDayClaimResult result = _rewards.Claim(); // raises Changed: the cards redraw (and Home's red dot follows)
            _claiming = false;

            if (result == SevenDayClaimResult.Claimed) _sfx?.Play(SfxId.Reward);
            if (result == SevenDayClaimResult.Claimed && dayIndex >= 0 && dayIndex < dayCards.Length && dayCards[dayIndex] != null)
                dayCards[dayIndex].Punch(_cardMotion);
            Redraw();
        }
    }
}
