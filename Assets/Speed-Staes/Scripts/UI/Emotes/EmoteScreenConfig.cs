using UnityEngine;

namespace SpeedStars.UI.Emotes
{
    /// <summary>Numbers of the emote screen that are look and feel, not balance: the feedback of a refused purchase.</summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Emote Screen", fileName = "EmoteScreenConfig")]
    public sealed class EmoteScreenConfig : ScriptableObject
    {
        [Header("Denied feedback (not enough Cash)")]
        [Tooltip("Length of the shake (s).")]
        [Min(0f)]
        [SerializeField] private float deniedShakeDuration = 0.25f;

        [Tooltip("Shake size in canvas pixels.")]
        [Min(0f)]
        [SerializeField] private float deniedShakeStrength = 14f;

        [Tooltip("How many times the shake swings to each side.")]
        [Min(1)]
        [SerializeField] private int deniedShakeCycles = 4;

        [Tooltip("The flash colour of a denied button.")]
        [SerializeField] private Color deniedFlashColor = new Color(0.9f, 0.16f, 0.16f, 1f);

        [Tooltip("Time the flash takes to fade back (s).")]
        [Min(0f)]
        [SerializeField] private float deniedFlashDuration = 0.35f;

        [Header("Cards")]
        [Tooltip("Tint of the price text on a locked card.")]
        [SerializeField] private Color priceColor = new Color(0.24f, 0.74f, 0.36f, 1f);

        public float DeniedShakeDuration => deniedShakeDuration;
        public float DeniedShakeStrength => deniedShakeStrength;
        public int DeniedShakeCycles => deniedShakeCycles;
        public Color DeniedFlashColor => deniedFlashColor;
        public float DeniedFlashDuration => deniedFlashDuration;
        public Color PriceColor => priceColor;
    }
}
