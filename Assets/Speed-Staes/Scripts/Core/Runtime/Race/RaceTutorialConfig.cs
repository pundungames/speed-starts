using UnityEngine;

namespace SpeedStars.Core.Race
{
    /// <summary>
    /// Numbers of the first race's tutorial in one asset (Data/Config/RaceTutorialConfig): how far the game slows down while a step waits
    /// for its tap, how many steps are prompted and how long the last hints stay. First values, to be tuned by feel. The field defaults are
    /// what the seeder writes once; at runtime the asset counts.
    /// </summary>
    [CreateAssetMenu(fileName = "RaceTutorialConfig", menuName = "Speed Stars/Config/Race Tutorial")]
    public sealed class RaceTutorialConfig : ScriptableObject
    {
        [Header("Prompted steps")]
        [Tooltip("How many steps are asked for one by one, left, right, left... (each waits for its tap).")]
        [Min(2)]
        [SerializeField] private int promptedSteps = 4;

        [Tooltip("Game speed while a step waits for its tap (1 = normal speed, 0 = stopped). Barely moving: the game is alive, and nobody runs away.")]
        [Range(0f, 1f)]
        [SerializeField] private float holdTimeScale = 0.02f;

        [Tooltip("How long the game takes to slow down to that when a step starts (real seconds).")]
        [Min(0f)]
        [SerializeField] private float holdBlendSeconds = 0.12f;

        [Tooltip("Game seconds from one tap to the next prompt: about when the foot in front lands, so a tap on the prompt is a step on time.")]
        [Min(0.01f)]
        [SerializeField] private float strideSeconds = 0.25f;

        [Header("Hints while running")]
        [Tooltip("The KEEP GOING hint stays until this many more taps...")]
        [Min(1)]
        [SerializeField] private int keepGoingPresses = 6;

        [Tooltip("...or this long (real seconds).")]
        [Min(0f)]
        [SerializeField] private float keepGoingMaxSeconds = 4f;

        [Tooltip("How long the rhythm hint stays (real seconds). The game runs at normal speed under it: in slow motion a steady hand would be mashing.")]
        [Min(0f)]
        [SerializeField] private float rhythmSeconds = 2.5f;

        [Header("Look")]
        [Tooltip("The TAP! word grows to this scale and back while it waits.")]
        [Min(1f)]
        [SerializeField] private float hintPulseScale = 1.18f;

        [Tooltip("How long one way of that takes (real seconds).")]
        [Min(0.01f)]
        [SerializeField] private float hintPulseSeconds = 0.35f;

        [Tooltip("The TAP! word's outline width (TextMeshPro units). Thick swallows the fill of this narrow font.")]
        [Range(0f, 1f)]
        [SerializeField] private float hintOutlineWidth = 0.2f;

        [Tooltip("The TAP! word's outline colour.")]
        [SerializeField] private Color hintOutlineColor = new Color(0.024f, 0.102f, 0.255f, 1f);

        [Tooltip("One frame never advances the tutorial by more than this (real seconds).")]
        [Min(0.01f)]
        [SerializeField] private float maxFrameSeconds = 0.1f;

        public int PromptedSteps => promptedSteps;
        public float HoldTimeScale => holdTimeScale;
        public float HoldBlendSeconds => holdBlendSeconds;
        public float StrideSeconds => strideSeconds;
        public int KeepGoingPresses => keepGoingPresses;
        public float KeepGoingMaxSeconds => keepGoingMaxSeconds;
        public float RhythmSeconds => rhythmSeconds;
        public float HintPulseScale => hintPulseScale;
        public float HintPulseSeconds => hintPulseSeconds;
        public float HintOutlineWidth => hintOutlineWidth;
        public Color HintOutlineColor => hintOutlineColor;
        public float MaxFrameSeconds => maxFrameSeconds;
    }
}
