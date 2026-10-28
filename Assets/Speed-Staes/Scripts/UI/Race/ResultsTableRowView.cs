using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// One line of the Results table (ResultsTableRow prefab): place, name, time, the cup of the first three places and
    /// the highlight of the player's own line. Binds ready-made objects and fills them; creates nothing.
    /// </summary>
    public sealed class ResultsTableRowView : MonoBehaviour
    {
        [Tooltip("Rank: place number.")]
        [SerializeField] private Text rankText;

        [Tooltip("Name: runner name.")]
        [SerializeField] private Text nameText;

        [Tooltip("Time: race time.")]
        [SerializeField] private Text timeText;

        [Tooltip("PlayerHighlight: the red outline, shown only on the player's own line.")]
        [SerializeField] private GameObject playerHighlight;

        [Tooltip("TrophyGold: cup of first place.")]
        [SerializeField] private GameObject goldTrophy;

        [Tooltip("TrophySilver: cup of second place.")]
        [SerializeField] private GameObject silverTrophy;

        [Tooltip("TrophyBronze: cup of third place.")]
        [SerializeField] private GameObject bronzeTrophy;

        [Tooltip("Name colour of the player's own line (the others keep the colour set on the Name text).")]
        [SerializeField] private Color playerNameColor = new Color(0.92f, 0.12f, 0.14f, 1f);

        private Color _otherNameColor;
        private bool _captured;

        public void Fill(ResultRowData row)
        {
            if (!_captured && nameText != null)
            {
                _otherNameColor = nameText.color;
                _captured = true;
            }

            if (rankText != null) rankText.text = row.Rank.ToString(CultureInfo.InvariantCulture);
            if (nameText != null)
            {
                nameText.text = row.Name;
                nameText.color = row.IsPlayer ? playerNameColor : _otherNameColor;
            }
            if (timeText != null) timeText.text = row.TimeText;
            if (playerHighlight != null) playerHighlight.SetActive(row.IsPlayer);
            if (goldTrophy != null) goldTrophy.SetActive(row.Trophy == ResultTrophy.Gold);
            if (silverTrophy != null) silverTrophy.SetActive(row.Trophy == ResultTrophy.Silver);
            if (bronzeTrophy != null) bronzeTrophy.SetActive(row.Trophy == ResultTrophy.Bronze);
        }
    }
}
