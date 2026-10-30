using DG.Tweening;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Rewards
{
    /// <summary>
    /// One day card of the 7-day panel (prefabs RewardDayCard for days 1-6 and RewardPremiumCard for day 7; one component for
    /// both, the premium card simply leaves the optional references empty). It only shows what
    /// <see cref="RewardPanelsModel.DayCard"/> returned: the look layers (available / claimed / locked), lock, tick, the
    /// footers, the reward picture and the texts. The card is not a button; the panel's CLAIM takes the reward. Binds
    /// existing objects only.
    /// </summary>
    public sealed class RewardDayCardView : MonoBehaviour
    {
        [Header("Look layers (one on at a time)")]
        [Tooltip("Card/Available: red outline.")]
        [SerializeField] private GameObject availableLook;

        [Tooltip("Card/Claimed: dimmed fill.")]
        [SerializeField] private GameObject claimedLook;

        [Tooltip("Card/Locked: the plain fill of a day that is not reachable yet.")]
        [SerializeField] private GameObject lockedLook;

        [Header("Icons and footers")]
        [Tooltip("Card/Lock: shown on a locked day.")]
        [SerializeField] private GameObject lockIcon;

        [Tooltip("Card/ClaimedFooter (dark bar, tick and its text): shown on a claimed day.")]
        [SerializeField] private GameObject claimedFooter;

        [Tooltip("Card/AvailableFooter (red bar and its text): shown on the available day. Empty on the premium card.")]
        [SerializeField] private GameObject availableFooter;

        [Header("Reward")]
        [Tooltip("Card/Diamond: the cyan Cash diamond. Empty on the premium card.")]
        [SerializeField] private GameObject diamondIcon;

        [Tooltip("Card/TimedTicket: the gold Timed Ticket. Empty on the premium card.")]
        [SerializeField] private GameObject timedTicketIcon;

        [Tooltip("Card/Item: the wearable's own picture (premium card only); aspect kept.")]
        [SerializeField] private Image itemIcon;

        [Header("Texts")]
        [Tooltip("Card/Day: 'DAY 3'.")]
        [SerializeField] private Text dayText;

        [Tooltip("Card/Amount: '100' or 'x1'. Empty on the premium card.")]
        [SerializeField] private Text amountText;

        [Tooltip("Card/Caption: the wearable's name (premium card only); hidden once the day is claimed (the tick row takes its place).")]
        [SerializeField] private Text captionText;

        private Tween _punch;
        private Vector3 _baseScale = Vector3.one;
        private bool _baseKnown;

        /// <summary>Shows one state. <paramref name="itemPicture"/> and <paramref name="caption"/> only matter on the premium card.</summary>
        public void Apply(DayCardLayout layout, Sprite itemPicture, string caption)
        {
            dayText.text = layout.DayText;
            availableLook.SetActive(layout.ShowAvailableLook);
            claimedLook.SetActive(layout.ShowClaimedLook);
            lockedLook.SetActive(layout.ShowLockedLook);
            lockIcon.SetActive(layout.ShowLock);
            claimedFooter.SetActive(layout.ShowClaimedFooter);
            if (availableFooter != null) availableFooter.SetActive(layout.ShowAvailableFooter);

            if (diamondIcon != null) diamondIcon.SetActive(layout.Icon == RewardIcon.Diamond);
            if (timedTicketIcon != null) timedTicketIcon.SetActive(layout.Icon == RewardIcon.TimedTicket);
            if (amountText != null) amountText.text = layout.AmountText;

            if (itemIcon != null)
            {
                itemIcon.sprite = itemPicture;
                itemIcon.enabled = itemPicture != null;
            }
            if (captionText != null)
            {
                captionText.text = caption ?? string.Empty;
                captionText.gameObject.SetActive(!layout.ShowClaimedFooter);
            }
        }

        /// <summary>A short scale punch when the card turns claimed: numbers from the motion config; no config = no punch.</summary>
        public void Punch(UiMotionConfig motion)
        {
            if (motion == null || !isActiveAndEnabled) return;
            if (!_baseKnown)
            {
                _baseKnown = true;
                _baseScale = transform.localScale;
            }
            KillPunch();
            transform.localScale = _baseScale;
            Tween punch = transform
                .DOPunchScale(_baseScale * motion.ButtonPunchScale, motion.ButtonPunchDuration, motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
            punch.OnKill(() => { if (_punch == punch) _punch = null; });
            _punch = punch;
        }

        private void OnDisable()
        {
            KillPunch();
            if (_baseKnown) transform.localScale = _baseScale;
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
