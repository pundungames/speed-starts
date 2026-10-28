using System;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// The decisions of the Race scene, as pure C# (RaceFlow is the thin Zenject shell around it). Start: take the
    /// confirmed entry from the gate and start the race with the catalog distance; with no pending entry (Play pressed
    /// in the editor straight from the Race scene) start the selected event for free and warn once. Finish: open Results.
    /// Race again and the HUD Restart go through the entry gate again (a ticket may be spent again); no ticket opens the Entry Gate modal.
    /// </summary>
    public sealed class RaceFlowLogic : IDisposable
    {
        private readonly IRaceSession _session;
        private readonly IRaceEntryGate _gate;
        private readonly IRaceSelection _selection;
        private readonly IRaceEventLookup _events;
        private readonly ScreenNavigator _navigator;
        private readonly SceneFlow _sceneFlow;
        private readonly IResultsView _results;
        private readonly IEntryGatePrompt _prompt; // optional

        private RaceEventInfo _current;
        private bool _hasCurrent;
        private bool _started;
        private bool _resultsOpen;
        private bool _warnedFreeStart;

        public RaceFlowLogic(IRaceSession session, IRaceEntryGate gate, IRaceSelection selection, IRaceEventLookup events,
            ScreenNavigator navigator, SceneFlow sceneFlow, IResultsView results, IEntryGatePrompt prompt)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _prompt = prompt;
        }

        /// <summary>True while the Results screen is up (between a finish and the next start).</summary>
        public bool ResultsOpen => _resultsOpen;

        /// <summary>The race scene opened: take the entry and start. Safe to call once; repeats are ignored.</summary>
        public void Start()
        {
            if (_started) return;
            _started = true;

            _session.Finished += OnFinished;
            _results.RaceAgainRequested += OnRaceAgain;
            _results.MainMenuRequested += OnMainMenu;

            RaceEventId id;
            if (!_gate.TryTakePending(out id))
            {
                id = _selection.Selected;
                if (!_warnedFreeStart)
                {
                    _warnedFreeStart = true;
                    UnityEngine.Debug.LogWarning($"[RaceFlow] No pending entry (Race scene started directly); starting '{id}' for free.");
                }
            }
            BeginRace(id);
        }

        public void Dispose()
        {
            if (!_started) return;
            _started = false;
            _session.Finished -= OnFinished;
            _results.RaceAgainRequested -= OnRaceAgain;
            _results.MainMenuRequested -= OnMainMenu;
        }

        private bool BeginRace(RaceEventId id)
        {
            if (!_events.TryGet(id, out RaceEventInfo info))
            {
                UnityEngine.Debug.LogError($"[RaceFlow] Event '{id}' is not in the catalog; the race cannot start.");
                return false;
            }
            _current = info;
            _hasCurrent = true;
            _resultsOpen = false;
            _session.RequestStart(new RaceStartRequest(info.Id, info.DistanceMeters));
            return true;
        }

        private void OnFinished(RaceOutcome outcome)
        {
            if (_resultsOpen) return; // exactly one Results per race

            string title;
            if (_hasCurrent && _current.Id == outcome.EventId) title = _current.DisplayName;
            else if (_events.TryGet(outcome.EventId, out RaceEventInfo info)) title = info.DisplayName;
            else title = outcome.EventId.ToString();

            _resultsOpen = true;
            _results.Present(new RaceResult(title, RaceDisplayFormat.Time(outcome.TimeSeconds), "-", "-", false));
            _navigator.Push(_results);
        }

        private void OnRaceAgain()
        {
            if (!_resultsOpen) return;
            Restart();
        }

        /// <summary>
        /// Runs the current event again through the entry gate (a ticket may be spent again, none is refunded for the run
        /// being left). Works while the race runs and from Results alike; ignored before the first start or during a scene move.
        /// </summary>
        public void Restart()
        {
            if (!_started || !_hasCurrent || _sceneFlow.IsTransitioning) return;

            EntryResult result = _gate.Enter(_current.Id);
            switch (result)
            {
                case EntryResult.EnteredFree:
                case EntryResult.EnteredWithTicket:
                case EntryResult.EnteredWithTimedAccess:
                    _gate.TryTakePending(out _); // the scene takes the confirmed entry, so the next Enter pays anew
                    if (_resultsOpen)
                    {
                        _resultsOpen = false;
                        _navigator.Pop(); // back to the HUD at the root
                    }
                    BeginRace(_current.Id);
                    break;

                case EntryResult.NoTicket:
                    if (_resultsOpen) _results.NotifyDenied();
                    if (_prompt != null) _prompt.Open(_current.Id);
                    else UnityEngine.Debug.LogWarning($"[RaceFlow] No Entry Ticket for '{_current.Id}' and no Entry Gate modal is bound.");
                    break;

                default:
                    if (_resultsOpen) _results.NotifyDenied();
                    UnityEngine.Debug.LogWarning($"[RaceFlow] Entry to '{_current.Id}' refused: {result}.");
                    break;
            }
        }

        /// <summary>Leaves to the Home scene (no ticket refund); also from the running race. Ignored during a scene move.</summary>
        public void Exit()
        {
            if (_sceneFlow.IsTransitioning) return;
            _sceneFlow.GoTo(GameScene.Home);
        }

        private void OnMainMenu() => Exit();
    }
}
