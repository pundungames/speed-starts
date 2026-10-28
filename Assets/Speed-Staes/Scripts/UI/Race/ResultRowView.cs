using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Race
{
    /// <summary>One line of the Results list (HighScoreRow prefab). Binds its texts and fills them; creates nothing.</summary>
    public sealed class ResultRowView : MonoBehaviour
    {
        [Tooltip("Rank: place number.")]
        [SerializeField] private Text rankText;

        [Tooltip("Name: player name.")]
        [SerializeField] private Text nameText;

        [Tooltip("Time: race time.")]
        [SerializeField] private Text timeText;

        [Tooltip("Date: filled with a placeholder until records exist (Faz 6). Optional.")]
        [SerializeField] private Text dateText;

        [Tooltip("PbMark: shown only when the time is a personal best.")]
        [SerializeField] private GameObject personalBestMark;

        public void Fill(int rank, string playerName, string time, bool isPersonalBest)
        {
            if (rankText != null) rankText.text = rank.ToString(CultureInfo.InvariantCulture);
            if (nameText != null) nameText.text = playerName;
            if (timeText != null) timeText.text = time;
            if (dateText != null) dateText.text = "-";
            if (personalBestMark != null) personalBestMark.SetActive(isPersonalBest);
        }
    }
}
