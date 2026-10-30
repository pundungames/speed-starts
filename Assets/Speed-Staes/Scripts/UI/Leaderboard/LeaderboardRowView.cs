using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>
    /// One table line (prefab LeaderboardRow, also the pinned YOU line): rank, flag, name, time and date. The flag is a
    /// full-row art layer whose sprite is swapped (the art has the flag already in its column). Binds objects of its
    /// prefab and fills them; it builds nothing.
    /// </summary>
    public sealed class LeaderboardRowView : MonoBehaviour
    {
        [Tooltip("Rank text (RANK column).")]
        [SerializeField] private Text rankText;

        [Tooltip("Player name text (PLAYER column, right of the flag).")]
        [SerializeField] private Text nameText;

        [Tooltip("Time text (TIME column).")]
        [SerializeField] private Text timeText;

        [Tooltip("Date text (SET ON column).")]
        [SerializeField] private Text dateText;

        [Tooltip("FlagLayer: full-row canvas Image; its sprite is the row art with this player's flag. Hidden when there is no flag.")]
        [SerializeField] private Image flagLayer;

        [Tooltip("YouHighlight: the red outline layer, on only for the local player's line.")]
        [SerializeField] private GameObject youHighlight;

        [Tooltip("Separator: thin line under the row; off on the pinned YOU line.")]
        [SerializeField] private GameObject separator;

        public RectTransform RectTransform => (RectTransform)transform;

        /// <param name="flag">Row art with the flag, or null for no flag.</param>
        /// <param name="highlight">Red outline (own line).</param>
        public void Bind(LeaderboardRowData data, Sprite flag, bool highlight)
        {
            rankText.text = data.RankText;
            nameText.text = data.Name;
            timeText.text = data.TimeText;
            dateText.text = data.DateText;
            flagLayer.sprite = flag;
            flagLayer.enabled = flag != null; // an Image without a sprite would draw a white box
            youHighlight.SetActive(highlight);
        }

        /// <summary>The pinned line has no separator under it.</summary>
        public void SetSeparator(bool visible)
        {
            if (separator != null) separator.SetActive(visible);
        }
    }
}
