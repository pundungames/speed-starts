using SpeedStars.Core.Race;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Race HUD: time ("ss.cc"), distance and the progress bar. Binds objects that already exist in the RaceHud prefab
    /// and only fills them; text is rewritten only when the shown value changes (no per-frame string garbage).
    /// </summary>
    public sealed class RaceHudView : UiScreen
    {
        [Tooltip("RaceTime/Value: the race clock text.")]
        [SerializeField] private Text timeText;

        [Tooltip("Progress/Bar/Fill: Filled-type image, fillAmount = distance / target distance.")]
        [SerializeField] private Image progressFill;

        [Tooltip("Progress/Distance: meters run, e.g. '37 m'.")]
        [SerializeField] private Text distanceText;

        private IRaceSession _session;
        private long _shownCentiseconds = -1;
        private int _shownMeters = -1;
        private bool _warnedMissing;

        [Inject]
        public void InjectSession(IRaceSession session)
        {
            _session = session;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _shownCentiseconds = -1;
            _shownMeters = -1;
        }

        private void Update()
        {
            if (_session == null) return;
            if (timeText == null || progressFill == null || distanceText == null)
            {
                if (!_warnedMissing)
                {
                    _warnedMissing = true;
                    Debug.LogWarning($"{name}: a RaceHudView field is not assigned in the prefab.", this);
                }
                return;
            }

            long cc = RaceDisplayFormat.ToCentiseconds(_session.RaceTime);
            if (cc != _shownCentiseconds)
            {
                _shownCentiseconds = cc;
                timeText.text = RaceDisplayFormat.Time(cc);
            }

            float target = _session.TargetDistance;
            progressFill.fillAmount = RaceDisplayFormat.Progress(_session.Distance, target);

            int meters = RaceDisplayFormat.WholeMeters(_session.Distance, target);
            if (meters != _shownMeters)
            {
                _shownMeters = meters;
                distanceText.text = RaceDisplayFormat.Meters(meters);
            }
        }
    }
}
