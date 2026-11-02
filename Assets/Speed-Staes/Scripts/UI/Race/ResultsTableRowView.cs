using System.Globalization;
using SpeedStars.UI.Framework;
using DG.Tweening;
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

        [Tooltip("PersonalBestBadge: small 'PB' Text after the time, shown only for a new personal record.")]
        [SerializeField] private Text personalBestBadge;

        [Tooltip("DayBestBadge: small 'DB' Text after the time, shown only for the player's best of today.")]
        [SerializeField] private Text dayBestBadge;

        [Tooltip("Flag: the runner's country flag between the place and the name. Hidden without a sprite.")]
        [SerializeField] private Image flagImage;

        [Tooltip("The row root's CanvasGroup: fades in with Reveal when a runner is added to a table that is already open.")]
        [SerializeField] private CanvasGroup revealGroup;

        private Color _otherNameColor;
        private bool _captured;
        private Tween _reveal;

        /// <param name="flag">Flag icon of the runner's country, or null for no flag.</param>
        public void Fill(ResultRowData row, UiMotionConfig motion = null, Sprite flag = null)
        {
            if (flagImage != null)
            {
                flagImage.sprite = flag;
                flagImage.enabled = flag != null; // an Image without a sprite would draw a white box
            }

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
            SetBadge(personalBestBadge, row.ShowPersonalBest, motion);
            SetBadge(dayBestBadge, row.ShowDayBest, motion);
        }

        /// <summary>A runner joining an open table: short fade and a small scale-in (UiMotionConfig stagger values).</summary>
        public void Reveal(UiMotionConfig motion)
        {
            KillReveal();
            if (motion == null || revealGroup == null) return;

            Vector3 scale = transform.localScale;
            revealGroup.alpha = 0f;
            transform.localScale = scale * motion.StaggerScaleFrom;
            Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOScale(scale, motion.StaggerItemDuration).SetEase(motion.StaggerEase));
            seq.Join(DOTween.To(() => revealGroup.alpha, a => revealGroup.alpha = a, 1f, motion.StaggerItemDuration).SetEase(Ease.OutQuad));
            seq.SetUpdate(true);
            seq.SetLink(gameObject);
            seq.OnKill(() =>
            {
                if (_reveal == seq) _reveal = null;
                if (revealGroup != null) revealGroup.alpha = 1f; // an interrupted fade must not leave the row see-through
                if (this != null) transform.localScale = scale;
            });
            _reveal = seq;
        }

        private void KillReveal()
        {
            if (_reveal == null) return;
            Tween running = _reveal;
            _reveal = null;
            running.Kill();
        }

        private void OnDisable() => KillReveal();

        /// <summary>Visible only when true; a small punch plays when it turns on.</summary>
        private static void SetBadge(Text badge, bool visible, UiMotionConfig motion)
        {
            if (badge == null) return;
            badge.gameObject.SetActive(visible);
            badge.transform.DOKill();
            badge.transform.localScale = Vector3.one;
            if (visible && motion != null)
                badge.transform.DOPunchScale(Vector3.one * motion.ButtonPunchScale * 3f, motion.ButtonPunchDuration, motion.ButtonPunchVibrato);
        }
    }
}
