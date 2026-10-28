using System;
using UnityEngine;

namespace SpeedStars.UI.Home
{
    /// <summary>One category card's content: its title and the races behind its distance buttons (button order).</summary>
    [Serializable]
    public sealed class EventCategoryEntry
    {
        [Tooltip("Which card this is.")]
        [SerializeField] private EventCategory category;

        [Tooltip("Text on the card's header.")]
        [SerializeField] private string title = string.Empty;

        [Tooltip("Race event ids (RaceEventCatalog) behind the distance buttons, left to right. Fewer than 3 hides the extra buttons.")]
        [SerializeField] private string[] eventIds = new string[0];

        public EventCategory Category => category;
        public string Title => title ?? string.Empty;
        public string[] EventIds => eventIds ?? new string[0];
    }

    /// <summary>
    /// Content and tuning of the event select screen: which race sits behind which card / button, and the fixed texts of the
    /// art. Labels of the distance buttons are not here: they come from each race's own DisplayName. Seeded by
    /// EventSelectConfigSeed; the screen reads this asset (nothing is hard-coded in the view).
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/UI/Event Select Layout", fileName = "EventSelectLayout")]
    public sealed class EventSelectLayout : ScriptableObject
    {
        [Header("Texts")]
        [Tooltip("Heading above the cards.")]
        [SerializeField] private string headingText = string.Empty;

        [Tooltip("Label of the Continue button.")]
        [SerializeField] private string continueLabel = string.Empty;

        [Header("Categories (card order)")]
        [Tooltip("One entry per card, in the same order as the cards in the prefab.")]
        [SerializeField] private EventCategoryEntry[] categories = new EventCategoryEntry[0];

        [Header("Bottom buttons")]
        [Tooltip("Race event id behind the Race of the Day button.")]
        [SerializeField] private string raceOfTheDayId = string.Empty;

        [Tooltip("Label of the Race of the Day button (fixed text of the art).")]
        [SerializeField] private string raceOfTheDayLabel = string.Empty;

        [Tooltip("Race event id behind the Practice button.")]
        [SerializeField] private string practiceId = string.Empty;

        [Tooltip("Label of the Practice button (fixed text; the race's own display name is not used).")]
        [SerializeField] private string practiceLabel = string.Empty;

        [Header("Denied Continue (shake)")]
        [Tooltip("Length of the shake when Continue is pressed on a race that is not playable (s).")]
        [Min(0f)]
        [SerializeField] private float deniedShakeDuration = 0.25f;

        [Tooltip("Shake size in canvas pixels.")]
        [Min(0f)]
        [SerializeField] private float deniedShakeStrength = 14f;

        [Tooltip("How many times the shake wobbles.")]
        [Min(1)]
        [SerializeField] private int deniedShakeVibrato = 14;

        public string HeadingText => headingText ?? string.Empty;
        public string ContinueLabel => continueLabel ?? string.Empty;
        public EventCategoryEntry[] Categories => categories ?? new EventCategoryEntry[0];
        public string RaceOfTheDayId => raceOfTheDayId ?? string.Empty;
        public string RaceOfTheDayLabel => raceOfTheDayLabel ?? string.Empty;
        public string PracticeId => practiceId ?? string.Empty;
        public string PracticeLabel => practiceLabel ?? string.Empty;
        public float DeniedShakeDuration => deniedShakeDuration;
        public float DeniedShakeStrength => deniedShakeStrength;
        public int DeniedShakeVibrato => deniedShakeVibrato;
    }
}
