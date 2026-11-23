using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// One ranking line of the Analysis screen (AnalysisRankingRow prefab): place, colour bar, name, time. The YOU
    /// highlight sits on the local player's line only; the colour bar is the runner's line colour on the chart and does not
    /// change when the order does. Pressing the line selects that runner on the chart. Binds ready-made objects and fills them.
    /// </summary>
    public sealed class AnalysisRowView : MonoBehaviour
    {
        [Tooltip("Pos: place number ('--' without one).")]
        [SerializeField] private Text rankText;

        [Tooltip("Name: runner name.")]
        [SerializeField] private Text nameText;

        [Tooltip("Time: finish time, or DNF.")]
        [SerializeField] private Text timeText;

        [Tooltip("YouHighlight: the outline shown only on the local player's line.")]
        [SerializeField] private GameObject youHighlight;

        [Tooltip("Swatches: one colour bar per runner slot, in palette order; only the runner's own is shown.")]
        [SerializeField] private GameObject[] swatches;

        [Tooltip("The line itself as a button (root): selects the runner.")]
        [SerializeField] private Button selectButton;

        private int _runnerId;
        private bool _bound;

        /// <summary>The line was pressed: the runner slot to select on the chart.</summary>
        public event Action<int> Selected;

        public int RunnerId => _runnerId;

        public void Fill(AnalysisRunner runner)
        {
            _runnerId = runner.RunnerId;
            if (rankText != null) rankText.text = runner.Rank > 0 ? runner.Rank.ToString(CultureInfo.InvariantCulture) : "--";
            if (nameText != null) nameText.text = runner.Name;
            if (timeText != null) timeText.text = runner.TimeText;
            if (youHighlight != null) youHighlight.SetActive(runner.IsPlayer);

            if (swatches != null && swatches.Length > 0)
            {
                int own = runner.RunnerId % swatches.Length;
                if (own < 0) own += swatches.Length;
                for (int i = 0; i < swatches.Length; i++)
                    if (swatches[i] != null) swatches[i].SetActive(i == own);
            }

            if (!_bound && selectButton != null)
            {
                selectButton.onClick.AddListener(OnClicked);
                _bound = true;
            }
        }

        private void OnDestroy()
        {
            if (_bound && selectButton != null) selectButton.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked() => Selected?.Invoke(_runnerId);
    }
}
