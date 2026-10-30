using System;
using DG.Tweening;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// One colour box under the colour square: the colour of one colour channel of the worn item, its frame, and a highlight
    /// behind the frame while it is the channel the picker edits. The box is a Button; the picker hears the tap through
    /// <see cref="Tapped"/> with the channel index. Binds existing objects only.
    /// </summary>
    public sealed class CostumeChannelBoxView : MonoBehaviour
    {
        [Tooltip("Box root button (tap area, the box's own rect).")]
        [SerializeField] private Button button;

        [Tooltip("Flat colour of the channel, under the frame art.")]
        [SerializeField] private Image colorImage;

        [Tooltip("Highlight behind the frame (a ring around the box): shown while the picker edits this channel.")]
        [SerializeField] private GameObject selectedHighlight;

        private Tween _punch;
        private Vector3 _baseScale = Vector3.one;
        private bool _baseKnown;
        private bool _wired;

        /// <summary>The channel this box stands for (0 = the item's primary channel).</summary>
        public int Index { get; private set; }

        /// <summary>The player tapped the box (channel index).</summary>
        public event Action<int> Tapped;

        private void Awake() => Wire();

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
            KillPunch();
        }

        private void OnDisable()
        {
            KillPunch();
            if (_baseKnown) transform.localScale = _baseScale;
        }

        /// <summary>Idempotent; the picker calls it before it reads the box (Awake order of siblings is not guaranteed).</summary>
        public void Initialize(int index)
        {
            Index = index;
            Wire();
        }

        public void SetColor(Color color) => colorImage.color = color;

        public void SetSelected(bool selected)
        {
            selectedHighlight.SetActive(selected);
        }

        /// <summary>A short scale punch on a tap: numbers from the motion config; no motion config = no punch.</summary>
        public void Punch(UiMotionConfig motion)
        {
            if (motion == null || !isActiveAndEnabled) return;
            RememberScale();
            KillPunch();
            transform.localScale = _baseScale;
            Tween punch = transform
                .DOPunchScale(_baseScale * motion.ButtonPunchScale, motion.ButtonPunchDuration, motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
            punch.OnKill(() => { if (_punch == punch) _punch = null; });
            _punch = punch;
        }

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            button.onClick.AddListener(OnClick);
            RememberScale();
        }

        private void OnClick() => Tapped?.Invoke(Index);

        private void RememberScale()
        {
            if (_baseKnown) return;
            _baseKnown = true;
            _baseScale = transform.localScale;
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
