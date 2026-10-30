using DG.Tweening;
using UnityEngine;

namespace SpeedStars.UI.Rewards
{
    /// <summary>
    /// Look and feel numbers of the daily gift ticket flight (not balance): how long the ticket flies, how high its arc bends,
    /// and how its size changes on the way. Seeded by the reward prefab builder;
    /// the popup and the flight read this asset, the code holds no numbers. The target punch uses the shared button punch of
    /// UiMotionConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/UI/Reward UI Config", fileName = "RewardUiConfig")]
    public sealed class RewardUiConfig : ScriptableObject
    {
        [Header("Ticket flight")]
        [Tooltip("Seconds the ticket takes from the screen centre to the Entry Ticket box.")]
        [Min(0.05f)]
        [SerializeField] private float flightDuration = 0.4f;

        [Tooltip("Easing of the flight along its path.")]
        [SerializeField] private Ease flightEase = Ease.InOutSine;

        [Tooltip("How far the path bends upward in the middle, in canvas pixels (a slight arc, not a straight line).")]
        [Min(0f)]
        [SerializeField] private float arcHeight = 140f;

        [Tooltip("Size of the flying ticket at the start, times its designed size.")]
        [Min(0.01f)]
        [SerializeField] private float startScale = 1f;

        [Tooltip("Size of the flying ticket on arrival, times its designed size.")]
        [Min(0.01f)]
        [SerializeField] private float endScale = 0.6f;

        public float FlightDuration => flightDuration;
        public Ease FlightEase => flightEase;
        public float ArcHeight => arcHeight;
        public float StartScale => startScale;
        public float EndScale => endScale;
    }
}
