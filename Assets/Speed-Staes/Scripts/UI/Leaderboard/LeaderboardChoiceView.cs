using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>
    /// One selectable entry of a leaderboard list or modal (prefab / prefab object): a country row, a period row, a
    /// category tab or a distance button. Background sprite = normal / selected / disabled, a check mark on the chosen one,
    /// an optional flag layer (country rows). Plain state holder: the screen view decides what is selected.
    /// </summary>
    public sealed class LeaderboardChoiceView : MonoBehaviour
    {
        [Tooltip("Whole-entry button.")]
        [SerializeField] private Button button;

        [Tooltip("Entry background (root Image); its sprite follows the state.")]
        [SerializeField] private Image background;

        [Tooltip("Label text.")]
        [SerializeField] private Text label;

        [Tooltip("Check mark layer, shown while selected. Empty on entries without one (category tabs).")]
        [SerializeField] private GameObject checkMark;

        [Tooltip("FlagLayer (country rows): full-row canvas Image whose sprite is swapped. Empty on other entries.")]
        [SerializeField] private Image flagLayer;

        [Tooltip("GlobeLayer (the GLOBAL row): shown instead of a flag. Empty on other entries.")]
        [SerializeField] private GameObject globeLayer;

        [Tooltip("Background while not chosen.")]
        [SerializeField] private Sprite normalSprite;

        [Tooltip("Background while chosen (red fill).")]
        [SerializeField] private Sprite selectedSprite;

        [Tooltip("Background while not available.")]
        [SerializeField] private Sprite disabledSprite;

        [Tooltip("Label colour while not chosen.")]
        [SerializeField] private Color normalTextColor = new Color(0.04f, 0.12f, 0.36f, 1f);

        [Tooltip("Label colour while chosen (on the red fill).")]
        [SerializeField] private Color selectedTextColor = Color.white;

        [Tooltip("Label colour while not available.")]
        [SerializeField] private Color disabledTextColor = new Color(0.45f, 0.5f, 0.6f, 1f);

        private Action _clicked;

        public string Label => label != null ? label.text : string.Empty;

        /// <summary>Sets text, state and the click action. <paramref name="flag"/> is the row art with the flag (null = none).</summary>
        public void Bind(string text, bool selected, bool enabled, Action clicked, Sprite flag = null, bool globe = false)
        {
            _clicked = clicked;
            label.text = text;
            if (flagLayer != null)
            {
                flagLayer.sprite = flag;
                flagLayer.enabled = flag != null && !globe;
            }
            if (globeLayer != null) globeLayer.SetActive(globe);
            SetState(selected, enabled);
        }

        public void SetState(bool selected, bool enabled)
        {
            button.interactable = enabled;
            Sprite sprite = !enabled ? disabledSprite : selected ? selectedSprite : normalSprite;
            if (sprite != null) background.sprite = sprite;
            label.color = !enabled ? disabledTextColor : selected ? selectedTextColor : normalTextColor;
            if (checkMark != null) checkMark.SetActive(selected && enabled);
        }

        private void Awake() => button.onClick.AddListener(OnClicked);

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked() => _clicked?.Invoke();
    }
}
