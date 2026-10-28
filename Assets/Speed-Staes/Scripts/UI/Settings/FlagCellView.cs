using System;
using DG.Tweening;
using SpeedStars.Meta.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Settings
{
    /// <summary>One cell of the flag grid (prefab FlagCell): frame, flag picture and a check mark on the chosen one.</summary>
    public sealed class FlagCellView : MonoBehaviour
    {
        [Tooltip("Whole-cell button.")]
        [SerializeField] private Button button;

        [Tooltip("Cell frame; swaps between the normal and selected sprite.")]
        [SerializeField] private Image frame;

        [Tooltip("Flag picture (same canvas as the frame).")]
        [SerializeField] private Image flag;

        [Tooltip("Check mark layer, shown on the chosen flag.")]
        [SerializeField] private GameObject checkMark;

        [Tooltip("Frame of a cell that is not chosen.")]
        [SerializeField] private Sprite normalSprite;

        [Tooltip("Frame of the chosen cell.")]
        [SerializeField] private Sprite selectedSprite;

        private string _code;
        private Action<string> _picked;
        private Tween _punch;
        private Vector3 _baseScale;
        private bool _scaleCaptured;

        public string Code => _code;

        public void Bind(FlagInfo info, bool selected, Action<string> picked)
        {
            _code = info.Code;
            _picked = picked;
            flag.sprite = info.Sprite;
            flag.enabled = info.Sprite != null; // an empty Image would draw a white box
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            frame.sprite = selected ? selectedSprite : normalSprite;
            checkMark.SetActive(selected);
        }

        /// <summary>The "you picked me" feedback: a punch scale (numbers from the UI motion config, given by the popup).</summary>
        public void Punch(float scale, float seconds, int vibrato)
        {
            CaptureScale();
            KillPunch();
            transform.localScale = _baseScale;
            _punch = transform.DOPunchScale(_baseScale * scale, seconds, vibrato).SetUpdate(true).SetLink(gameObject);
        }

        private void Awake()
        {
            CaptureScale();
            button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
            KillPunch();
        }

        private void OnDisable()
        {
            KillPunch();
            if (_scaleCaptured) transform.localScale = _baseScale;
        }

        private void OnClicked() => _picked?.Invoke(_code);

        private void CaptureScale()
        {
            if (_scaleCaptured) return;
            _scaleCaptured = true;
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
