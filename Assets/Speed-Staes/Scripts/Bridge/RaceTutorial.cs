using SpeedStars.Core.Race;
using SpeedStars.Meta.Persistence;
using SpeedStars.Physical;
using SpeedStars.UI.Race;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// The first race teaches the two keys: WAIT FOR GO, then the first steps one by one, the game nearly stopped on each and only the asked
    /// side counting, then two hints while running. Once seen it is saved and never comes again; left halfway it starts over next time. The
    /// order is <see cref="RaceTutorialFlow"/> (pure, tested); this class feeds it the race, sets the game speed and hands the step to the
    /// view. It owns the game speed only while a prompt waits, so FAIL's slow motion and the finish sequence's stay theirs. Nothing in a relay:
    /// the player's own runner does not run there. On the RaceTutorial prefab over the HUD; Zenject injects it with the scene.
    /// </summary>
    public sealed class RaceTutorial : MonoBehaviour
    {
        [SerializeField, Tooltip("Numbers of the tutorial: how far the game slows down, how many steps are asked for, how long the hints stay.")]
        RaceTutorialConfig config;
        [SerializeField, Tooltip("The picture of the tutorial, on this prefab.")]
        RaceTutorialView view;

        RaceSessionAdapter _session;
        ProgressRepository _progress;
        RaceTutorialFlow _flow;
        RunnerBody _body;
        bool _holdsGameSpeed;

        [Inject]
        void Construct(IRaceSession session, [InjectOptional] ProgressRepository progress)
        {
            _session = session as RaceSessionAdapter;
            _progress = progress;
            bool seen = _progress == null || _progress.Current == null || _progress.Current.RaceTutorialDone;
            if (seen || !_session || !config) return;

            _flow = new RaceTutorialFlow(config);
            _flow.StepChanged += OnStepChanged;
            _session.StartPhaseChanged += OnStartPhase;
            _session.Restarting += GiveGameSpeedBack;
            _session.Completing += OnCompleting;
            _session.PressFilter = _flow.AcceptsPress;
            _body = _session.PlayerBody;
            if (_body) _body.Tapped += OnTapped;
        }

        void OnDestroy()
        {
            if (_flow == null) return;
            GiveGameSpeedBack();
            _flow.StepChanged -= OnStepChanged;
            if (_body) _body.Tapped -= OnTapped;
            if (!_session) return;
            _session.StartPhaseChanged -= OnStartPhase;
            _session.Restarting -= GiveGameSpeedBack;
            _session.Completing -= OnCompleting;
            _session.PressFilter = null;
        }

        void OnStartPhase(RaceStartPhase phase)
        {
            if (phase == RaceStartPhase.Ready) _flow.Begin();
            else if (phase == RaceStartPhase.Go)
            {
                if (_session.IsRelay) _flow.Stop();
                else _flow.OnGo();
            }
        }

        void OnTapped(int leg, bool pressed, double simTime)
        {
            if (pressed) _flow.OnPress(leg);
        }

        void OnStepChanged(RaceTutorialStep step)
        {
            if (view) view.Show(step, _flow.PromptIndex);
            if (step == RaceTutorialStep.Done) MarkSeen();
        }

        /// <summary>The race ended under the tutorial (a short race): seen if the keys were tapped, and the finish gets the normal speed.</summary>
        void OnCompleting()
        {
            GiveGameSpeedBack();
            if (_flow.KeysLearned) MarkSeen();
            _flow.Stop();
        }

        void Update()
        {
            if (_flow == null || !_flow.IsActive) return;
            _flow.Tick(Time.deltaTime, Mathf.Min(Time.unscaledDeltaTime, config.MaxFrameSeconds));

            if (_flow.Step == RaceTutorialStep.TapLeft || _flow.Step == RaceTutorialStep.TapRight)
            {
                _holdsGameSpeed = true;
                Time.timeScale = _flow.TimeScale;
            }
            else GiveGameSpeedBack();
        }

        void GiveGameSpeedBack()
        {
            if (!_holdsGameSpeed) return;
            _holdsGameSpeed = false;
            Time.timeScale = 1f;
        }

        void MarkSeen()
        {
            if (_progress == null || _progress.Current == null || _progress.Current.RaceTutorialDone) return;
            _progress.Current.RaceTutorialDone = true;
            _progress.Save();
        }
    }
}
