using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// The "Custom N" drop-down at the top of the wardrobe: a closed bar that shows the chosen preset and an open list
    /// with one row per preset. The rows come from the row prefab under the list's content parent (created once, on the
    /// first <see cref="Show"/>); the bar, chevron and list are prefab objects. Choosing a row reports its index and closes
    /// the list. With many presets (nine) the list is a short window onto the rows: it scrolls, and opens at the chosen one.
    /// </summary>
    public sealed class CostumePresetDropdownView : MonoBehaviour
    {
        [Tooltip("Bar: the closed drop-down; tapping it opens or closes the list.")]
        [SerializeField] private Button bar;

        [Tooltip("Bar/Label: name of the chosen preset.")]
        [SerializeField] private Text barLabel;

        [Tooltip("Bar/Chevron: turned upside down while the list is open.")]
        [SerializeField] private RectTransform chevron;

        [Tooltip("List: the open list (background + rows). Starts off.")]
        [SerializeField] private GameObject list;

        [Tooltip("List/Rows: the parent the row prefab is created under.")]
        [SerializeField] private Transform rowsParent;

        [Tooltip("List scroll rect (list > viewport > rows). Optional: empty = the list does not scroll (few presets).")]
        [SerializeField] private ScrollRect listScroll;

        [Tooltip("CostumePresetRow prefab with a CostumePresetRowView on its root.")]
        [SerializeField] private CostumePresetRowView rowPrefab;

        [Tooltip("Chevron turn while the list is open (degrees around Z).")]
        [SerializeField] private float openChevronAngle = 180f;

        private readonly List<CostumePresetRowView> _rows = new List<CostumePresetRowView>();
        private int _selected;
        private int _count;

        /// <summary>The player chose a preset (index).</summary>
        public event Action<int> PresetChosen;

        public bool IsOpen => list.activeSelf;

        private void Awake() => bar.onClick.AddListener(Toggle);

        private void OnDestroy()
        {
            if (bar != null) bar.onClick.RemoveListener(Toggle);
            foreach (CostumePresetRowView row in _rows)
                if (row != null) row.Chosen -= OnRowChosen;
        }

        private void OnDisable() => Close();

        /// <summary>Shows the preset names (the rows are created on first use) and marks the chosen one.</summary>
        public void Show(IInstantiator instantiator, IReadOnlyList<string> names, int selected)
        {
            while (_rows.Count < names.Count)
            {
                CostumePresetRowView row = instantiator.InstantiatePrefabForComponent<CostumePresetRowView>(rowPrefab.gameObject, rowsParent);
                row.Chosen += OnRowChosen;
                _rows.Add(row);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < names.Count;
                _rows[i].gameObject.SetActive(used);
                if (!used) continue;
                _rows[i].Bind(i, names[i]);
                _rows[i].SetSelected(i == selected);
            }
            _selected = selected;
            _count = names.Count;
            barLabel.text = selected >= 0 && selected < names.Count ? names[selected] : string.Empty;
        }

        public void Close()
        {
            list.SetActive(false);
            chevron.localEulerAngles = Vector3.zero;
        }

        private void Toggle()
        {
            bool open = !list.activeSelf;
            list.SetActive(open);
            chevron.localEulerAngles = new Vector3(0f, 0f, open ? openChevronAngle : 0f);
            if (open) ScrollToChosen();
        }

        /// <summary>The list opens with the chosen preset in view, however long the list is.</summary>
        private void ScrollToChosen()
        {
            if (listScroll == null || _count < 2) return;
            Canvas.ForceUpdateCanvases();
            listScroll.verticalNormalizedPosition = Mathf.Clamp01(1f - _selected / (float)(_count - 1));
        }

        private void OnRowChosen(int index)
        {
            Close();
            PresetChosen?.Invoke(index);
        }
    }
}
