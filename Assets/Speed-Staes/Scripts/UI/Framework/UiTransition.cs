using System;
using DG.Tweening;
using UnityEngine;

namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// One direction of a panel transition (open or close). Scale is relative to the scale the designer built the
    /// panel with, never an absolute 1 (agent-kit/05 §3.1): open starts from it, close ends at it.
    /// </summary>
    [Serializable]
    public struct UiTransition
    {
        [Tooltip("Length in seconds (unscaled time, so it also plays while the game is paused).")]
        [Min(0f)]
        [SerializeField] private float duration;

        [Tooltip("Open: start scale. Close: end scale. Multiplies the panel's designed scale; 1 = no scale motion.")]
        [Min(0f)]
        [SerializeField] private float scale;

        [Tooltip("Easing of scale and alpha. Kit: OutBack when arriving, InQuad when leaving; avoid Linear.")]
        [SerializeField] private Ease ease;

        public UiTransition(float duration, float scale, Ease ease)
        {
            this.duration = duration;
            this.scale = scale;
            this.ease = ease;
        }

        public float Duration => duration;
        public float Scale => scale;
        public Ease Ease => ease;
    }
}
