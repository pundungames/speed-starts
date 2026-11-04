using DG.Tweening;
using UnityEngine;

namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// The one scale punch every tappable thing uses. Two owners can punch the same transform (ButtonFeedback on the
    /// press, the card's own Punch on the result), and DOTween reads a tween's start value on its FIRST UPDATE: the second
    /// punch would take the first one's swollen scale as its "start" and settle there. So the punch kills every tween on
    /// the target, writes the designed scale, reads the start value now (ForceInit) and writes the designed scale back
    /// when it ends. Only DOTween core is used: the SpeedStars.UI assembly cannot see the module extensions.
    /// </summary>
    public static class UiPunch
    {
        /// <summary>Punch from the designed scale with the numbers of the motion config. Null config = no punch (returns null).</summary>
        public static Tween Play(Transform target, Vector3 designedScale, UiMotionConfig motion, GameObject link)
        {
            if (motion == null) return null;
            return Play(target, designedScale, motion.ButtonPunchScale, motion.ButtonPunchDuration, motion.ButtonPunchVibrato, link);
        }

        /// <summary>Same with explicit numbers (for callers that get them from a popup).</summary>
        public static Tween Play(Transform target, Vector3 designedScale, float punchScale, float seconds, int vibrato, GameObject link)
        {
            target.DOKill();
            target.localScale = designedScale;
            Tween punch = target
                .DOPunchScale(designedScale * punchScale, seconds, vibrato)
                .SetUpdate(true)
                .SetLink(link);
            punch.OnComplete(() => target.localScale = designedScale);
            punch.ForceInit(); // start value is the designed scale now, not whatever another tween makes of it later
            return punch;
        }
    }
}
