using System.Globalization;
using DG.Tweening;
using SpeedStars.Core.Audio;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Rewards;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// The Cash breakdown of the Results screen, under the Cash counter (economy v2): "+30" and the lines "Finished race +10",
    /// "Difficulty bonus +10", "Rivals beaten +10" (a line that paid 0 is hidden, an empty reward hides the block). The prefab holds
    /// every object; this binds and fills them. <see cref="Present"/> only sets the content (the lines start faded out);
    /// <see cref="Play"/> reveals total and lines one after another, flies a diamond to the Cash counter, counts the counter
    /// up from what the wallet held before the race and punches its box. A screen that closes mid-way, or a run without a
    /// motion config, lands on the final look at once. What each line says is decided by <see cref="CashBreakdown"/>.
    /// </summary>
    public sealed class CashBreakdownView : MonoBehaviour
    {
        [Tooltip("CashBreakdown/Block: the visible block (a child, so this component stays active); shown only when the race paid Cash.")]
        [SerializeField] private GameObject blockRoot;

        [Tooltip("CashBreakdown/Block/Total: '+30'.")]
        [SerializeField] private CanvasGroup totalGroup;

        [Tooltip("CashBreakdown/Block/Total/Value.")]
        [SerializeField] private Text totalText;

        [Tooltip("The three line roots in screen order: finish, difficulty, rivals beaten (each needs a CanvasGroup).")]
        [SerializeField] private CanvasGroup[] lineGroups = new CanvasGroup[CashBreakdown.LineCount];

        [Tooltip("The three line texts, same order as the line roots.")]
        [SerializeField] private Text[] lineTexts = new Text[CashBreakdown.LineCount];

        [Tooltip("Diamond/Value of the Results screen: the Cash counter that counts up when the diamond lands.")]
        [SerializeField] private Text counterText;

        [Tooltip("Diamond (the whole Cash box): target of the flight; punches on arrival.")]
        [SerializeField] private RectTransform counterBox;

        [Tooltip("CashFlight: the diamond that flies from the total to the Cash box.")]
        [SerializeField] private CashFlightIcon flight;

        private UiMotionConfig _motion;
        private ISfx _sfx;
        private IHaptics _haptics;
        private CashBreakdown _current;
        private bool _hasContent;
        private bool _playedForContent;
        private Sequence _reveal;
        private Tween _count;
        private Tween _punch;
        private Vector3 _boxBaseScale = Vector3.one;
        private bool _boxScaleKnown;
        private int _finalCounter;

        [Inject]
        public void InjectCashBreakdown([InjectOptional] UiMotionConfig motion, [InjectOptional] ISfx sfx, [InjectOptional] IHaptics haptics)
        {
            _motion = motion;
            _sfx = sfx;
            _haptics = haptics;
        }

        /// <summary>The block shows (it has Cash to show).</summary>
        public bool IsShown => blockRoot != null && blockRoot.activeSelf;

        /// <summary>Sets the content for one race. An empty reward hides the block (and clears the previous race's).</summary>
        public void Present(RaceCashReward reward)
        {
            Stop();
            _current = CashBreakdown.Build(reward);
            _hasContent = _current.Visible;
            _playedForContent = false;

            if (blockRoot != null) blockRoot.SetActive(_current.Visible);
            if (!_current.Visible) return;

            if (totalText != null) totalText.text = _current.TotalText;
            for (int i = 0; i < CashBreakdown.LineCount; i++)
            {
                CashBreakdownLine line = _current.Line(i);
                if (lineTexts != null && i < lineTexts.Length && lineTexts[i] != null) lineTexts[i].text = line.Text;
                CanvasGroup group = LineGroup(i);
                if (group != null) group.gameObject.SetActive(line.Visible);
            }
            SetVisibleState(0f); // faded out until Play reveals it
        }

        /// <summary>
        /// Reveals the block and pays it into the counter. Once per presented reward; without a motion config it snaps to the final
        /// look. The Cash is already in the wallet: the counter text holds the final value from Present on.
        /// </summary>
        public void Play()
        {
            if (!_hasContent || _playedForContent) return;
            _playedForContent = true;
            StopAnimation();

            int.TryParse(counterText != null ? counterText.text : "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out _finalCounter);
            if (_motion == null)
            {
                SetVisibleState(1f);
                return;
            }

            int start = CashBreakdown.CounterStart(_finalCounter, _current.Total);
            if (counterText != null) counterText.text = start.ToString(CultureInfo.InvariantCulture);

            int shown = 1 + _current.VisibleLines;
            float interval = _motion.StaggerIntervalFor(shown);
            float duration = _motion.StaggerItemDuration;

            Sequence seq = DOTween.Sequence();
            int step = 0;
            if (totalGroup != null) AddPop(seq, totalGroup, step++ * interval, duration);
            for (int i = 0; i < CashBreakdown.LineCount; i++)
            {
                CanvasGroup group = LineGroup(i);
                if (group == null || !group.gameObject.activeSelf) continue;
                AddPop(seq, group, step++ * interval, duration);
            }

            float end = Mathf.Max(0f, (shown - 1) * interval) + duration;
            seq.InsertCallback(end, FlyToCounter);
            seq.SetUpdate(true);
            seq.SetLink(gameObject);
            _reveal = seq;
        }

        private void AddPop(Sequence seq, CanvasGroup group, float at, float duration)
        {
            Transform target = group.transform;
            Vector3 scale = Vector3.one;
            group.alpha = 0f;
            target.localScale = scale * _motion.StaggerScaleFrom;
            seq.Insert(at, target.DOScale(scale, duration).SetEase(_motion.StaggerEase));
            seq.Insert(at, DOTween.To(() => group.alpha, a => group.alpha = a, 1f, duration).SetEase(Ease.OutQuad));
        }

        private void FlyToCounter()
        {
            _reveal = null;
            RectTransform from = totalGroup != null ? (RectTransform)totalGroup.transform : null;
            if (flight == null) { Land(); return; }
            flight.Play(_motion, from, counterBox, Land);
        }

        /// <summary>The diamond arrived: the counter counts up to the final value, its box punches, the reward sounds.</summary>
        private void Land()
        {
            _sfx?.Play(SfxId.Reward);
            _haptics?.Tap();
            PunchBox();
            if (counterText == null || _motion == null) { SnapCounter(); return; }

            int start = CashBreakdown.CounterStart(_finalCounter, _current.Total);
            int shownValue = start;
            Tween count = DOTween.To(() => shownValue, v =>
            {
                shownValue = v;
                counterText.text = v.ToString(CultureInfo.InvariantCulture);
            }, _finalCounter, _motion.CounterDuration).SetEase(_motion.CounterEase).SetUpdate(true).SetLink(gameObject);
            count.OnComplete(() => { _count = null; SnapCounter(); });
            _count = count;
        }

        private void PunchBox()
        {
            if (counterBox == null || _motion == null) return;
            if (!_boxScaleKnown)
            {
                _boxBaseScale = counterBox.localScale;
                _boxScaleKnown = true;
            }
            KillPunch();
            counterBox.localScale = _boxBaseScale;
            _punch = counterBox
                .DOPunchScale(_boxBaseScale * _motion.ButtonPunchScale, _motion.ButtonPunchDuration, _motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(counterBox.gameObject);
        }

        private CanvasGroup LineGroup(int index) => lineGroups != null && index < lineGroups.Length ? lineGroups[index] : null;

        private void SetVisibleState(float alpha)
        {
            if (totalGroup != null)
            {
                totalGroup.alpha = alpha;
                totalGroup.transform.localScale = Vector3.one;
            }
            for (int i = 0; i < CashBreakdown.LineCount; i++)
            {
                CanvasGroup group = LineGroup(i);
                if (group == null) continue;
                group.alpha = alpha;
                group.transform.localScale = Vector3.one;
            }
        }

        private void SnapCounter()
        {
            if (counterText != null && _hasContent) counterText.text = _finalCounter.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Everything back to the final look: a closed or interrupted reveal must not leave faded lines or a half-counted Cash.</summary>
        private void Stop()
        {
            bool wasRunning = _reveal != null || _count != null || (flight != null && flight.IsFlying);
            StopAnimation();
            if (wasRunning && _hasContent) SetVisibleState(1f);
        }

        private void StopAnimation()
        {
            if (_reveal != null)
            {
                Sequence running = _reveal;
                _reveal = null;
                running.Kill();
            }
            if (flight != null) flight.Cancel();
            if (_count != null)
            {
                Tween running = _count;
                _count = null;
                running.Kill();
                SnapCounter();
            }
            KillPunch();
        }

        private void KillPunch()
        {
            if (_punch != null)
            {
                Tween running = _punch;
                _punch = null;
                running.Kill();
            }
            if (counterBox != null && _boxScaleKnown) counterBox.localScale = _boxBaseScale;
        }

        private void OnDisable()
        {
            bool unfinished = _reveal != null || _count != null || (flight != null && flight.IsFlying);
            StopAnimation();
            if (unfinished)
            {
                SnapCounter();
                if (_hasContent) SetVisibleState(1f);
            }
        }
    }
}
