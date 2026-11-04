using DG.Tweening;
using SpeedStars.Core.Race;
using SpeedStars.UI.Framework;
using TMPro;
using UnityEngine;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>The player-facing sentences of the first race's tutorial, one per step.</summary>
    public static class RaceTutorialTexts
    {
        public const string WaitForGo = "WAIT FOR GO!";
        public const string TapLeftFirst = "TAP THE LEFT SIDE: LEFT FOOT";
        public const string TapRightFirst = "TAP THE RIGHT SIDE: RIGHT FOOT";
        public const string TapLeft = "LEFT!";
        public const string TapRight = "RIGHT!";
        public const string KeepGoing = "KEEP GOING: LEFT, RIGHT, LEFT, RIGHT";
        public const string Rhythm = "DON'T MASH! TAP IN RHYTHM";

        /// <summary>The sentence of a step; empty when the step has none. The first prompt of each side explains, the others only name it.</summary>
        public static string For(RaceTutorialStep step, int promptIndex)
        {
            switch (step)
            {
                case RaceTutorialStep.WaitForGo: return WaitForGo;
                case RaceTutorialStep.TapLeft: return promptIndex == 0 ? TapLeftFirst : TapLeft;
                case RaceTutorialStep.TapRight: return promptIndex == 1 ? TapRightFirst : TapRight;
                case RaceTutorialStep.KeepGoing: return KeepGoing;
                case RaceTutorialStep.Rhythm: return Rhythm;
                default: return string.Empty;
            }
        }
    }

    /// <summary>
    /// The tutorial over the race: one sentence on a banner, and while a step waits for its tap the other half of the screen goes dark and
    /// a TAP! word pulses on the side to press. The halves are RunnerInput's own: left of the middle is the left foot. It only shows what
    /// it is told; nothing here catches a touch (raycast off everywhere, and the CanvasGroup blocks nothing). A prefab placed over the HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceTutorialView : MonoBehaviour
    {
        [Tooltip("RaceTutorialConfig asset: the TAP! word's pulse.")]
        [SerializeField] private RaceTutorialConfig config;

        [Tooltip("Dark layer over the left half: on while the right side is asked for.")]
        [SerializeField] private GameObject leftShade;

        [Tooltip("Dark layer over the right half: on while the left side is asked for.")]
        [SerializeField] private GameObject rightShade;

        [Tooltip("The TAP! word on the left half.")]
        [SerializeField] private RectTransform leftHint;

        [Tooltip("The TAP! word on the right half.")]
        [SerializeField] private RectTransform rightHint;

        [Tooltip("The banner under the sentence; it shows and hides with it.")]
        [SerializeField] private RectTransform textBox;

        [Tooltip("The sentence (TextMeshPro).")]
        [SerializeField] private TMP_Text sentence;

        private UiMotionConfig _motion;
        private Vector3 _boxScale = Vector3.one;
        private Vector3 _hintScale = Vector3.one;
        private RectTransform _pulsing;

        /// <summary>Optional: without the motion config the banner changes without its pop.</summary>
        [Inject]
        public void Construct([InjectOptional] UiMotionConfig motion) => _motion = motion;

        private void Awake()
        {
            if (textBox != null) _boxScale = textBox.localScale;
            if (leftHint != null) _hintScale = leftHint.localScale;
            Outline(leftHint);
            Outline(rightHint);
            Show(RaceTutorialStep.None, 0);
        }

        /// <summary>The outline on the word's own material instance, never on the shared font material.</summary>
        private void Outline(RectTransform hint)
        {
            TMP_Text word = hint != null ? hint.GetComponentInChildren<TMP_Text>(true) : null;
            if (word == null || config == null) return;
            word.fontMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            word.outlineWidth = config.HintOutlineWidth;
            word.outlineColor = config.HintOutlineColor;
        }

        private void OnDisable() => StopPulse();

        /// <summary>Shows a step. The moment between two prompts keeps the last sentence, so the banner does not blink.</summary>
        public void Show(RaceTutorialStep step, int promptIndex)
        {
            bool left = step == RaceTutorialStep.TapLeft;
            bool right = step == RaceTutorialStep.TapRight;
            if (leftShade != null) leftShade.SetActive(right);
            if (rightShade != null) rightShade.SetActive(left);
            Pulse(left ? leftHint : right ? rightHint : null);

            if (step == RaceTutorialStep.Stride || textBox == null || sentence == null) return;
            string text = RaceTutorialTexts.For(step, promptIndex);
            textBox.gameObject.SetActive(text.Length > 0);
            if (text.Length == 0 || text == sentence.text) return;
            sentence.text = text;
            UiPunch.Play(textBox, _boxScale, _motion, gameObject);
        }

        private void Pulse(RectTransform hint)
        {
            StopPulse();
            if (hint == null) return;
            _pulsing = hint;
            hint.gameObject.SetActive(true);
            if (config == null) return;
            hint.DOScale(_hintScale * config.HintPulseScale, config.HintPulseSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void StopPulse()
        {
            if (_pulsing != null)
            {
                _pulsing.DOKill();
                _pulsing.localScale = _hintScale;
                _pulsing = null;
            }
            if (leftHint != null) leftHint.gameObject.SetActive(false);
            if (rightHint != null) rightHint.gameObject.SetActive(false);
        }
    }
}
