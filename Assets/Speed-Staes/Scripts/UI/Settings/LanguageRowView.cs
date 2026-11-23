using System;
using SpeedStars.Meta.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Settings
{
    /// <summary>One line of the language list (prefab LanguageRow): the language's own name, the chosen one marked.</summary>
    public sealed class LanguageRowView : MonoBehaviour
    {
        [Tooltip("Whole-row button.")]
        [SerializeField] private Button button;

        [Tooltip("Row background; swaps between the normal and selected sprite.")]
        [SerializeField] private Image background;

        [Tooltip("Language name.")]
        [SerializeField] private Text label;

        [Tooltip("Check mark, shown on the chosen language.")]
        [SerializeField] private GameObject checkMark;

        [Tooltip("Background of a row that is not chosen.")]
        [SerializeField] private Sprite normalSprite;

        [Tooltip("Background of the chosen row.")]
        [SerializeField] private Sprite selectedSprite;

        private string _code;
        private Action<string> _picked;

        public string Code => _code;

        public void Bind(LanguageInfo info, bool selected, Action<string> picked)
        {
            _code = info.Code;
            _picked = picked;
            label.text = info.DisplayName;
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            background.sprite = selected ? selectedSprite : normalSprite;
            checkMark.SetActive(selected);
        }

        private void Awake() => button.onClick.AddListener(OnClicked);

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked() => _picked?.Invoke(_code);
    }
}
