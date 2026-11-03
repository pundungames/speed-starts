using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// The decisions of the Race scene, as pure C# (RaceFlow is the thin Zenject shell around it). Start: take the
    /// confirmed entry from the gate and start the race with the catalog distance; with no pending entry (Play pressed
    /// in the editor straight from the Race scene) start the selected event for free and warn once. Finish: pay the gem reward once and open Results (Practice: nothing is paid or recorded and no Results opens).
    /// Results Race Again enters a new race through the gate; HUD Restart resets the current attempt immediately.
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
        private readonly IRaceRewardResolver _rewards;
        private readonly IEntryGatePrompt _prompt; // optional
        private readonly IPersonalBests _records; // optional: without it no best time is kept or shown
        private readonly IWallet _wallet; // optional: the Cash card shows 0 without it
        private readonly IPlayerSettings _settings; // optional: the player row says YOU without it
        private readonly IDayBestSource _dayBests; // optional with the clock: without them the DB badge never shows
        private readonly IClock _clock;
        private readonly IRaceTraces _traces; // optional: without it Analysis shows the player alone
        private readonly IRaceCashRewardResolver _cashRewards; // optional: without it no Cash is paid for a race
        private readonly IRaceOfTheDay _daily; // optional: without it Race of the Day runs the catalog distance and reads like any event
        private readonly Func<RaceEventId, bool> _isPractice; // optional: without it every event ends with Results
        private readonly List<DayBestRecord> _dayScratch = new List<DayBestRecord>();

        private RaceEventInfo _current;
        private bool _hasCurrent;
        private bool _started;
        private bool _resultsOpen;
        private bool _practiceFinished; // a Practice run crossed the line: nothing more is recorded until the next start
        private bool _warnedFreeStart;
        private float _currentDistance; // the length of the race being run (a Race of the Day's changes by day)
        private string _payoutId = string.Empty;
        private RaceSplitRecorder _splits; // null until a race started and the results view can take analysis data
        private SplitSeries _analysisPlayerSeries; // the finished race's data, kept to redraw as bots cross the line
        private string _analysisTitle = string.Empty;
        private float _analysisDistance;
        private int _analysisRank;

        public RaceFlowLogic(IRaceSession session, IRaceEntryGate gate, IRaceSelection selection, IRaceEventLookup events,
            ScreenNavigator navigator, SceneFlow sceneFlow, IResultsView results, IRaceRewardResolver rewards, IEntryGatePrompt prompt,
            IPersonalBests records = null, IWallet wallet = null, IPlayerSettings settings = null,
            IDayBestSource dayBests = null, IClock clock = null, IRaceTraces traces = null,
            IRaceCashRewardResolver cashRewards = null, IRaceOfTheDay raceOfTheDay = null, Func<RaceEventId, bool> isPractice = null)
        {
            _isPractice = isPractice;
            _daily = raceOfTheDay;
            _cashRewards = cashRewards;
            _traces = traces;
            _dayBests = dayBests;
            _clock = clock;
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            _prompt = prompt;
            _records = records;
            _wallet = wallet;
            _settings = settings;
        }

        /// <summary>True while the Results screen is up (between a finish and the next start).</summary>
        public bool ResultsOpen => _resultsOpen;

        /// <summary>True after a Practice run crossed the line (no Results screen is shown for it).</summary>
        public bool PracticeFinished => _practiceFinished;

        /// <summary>The race scene opened: take the entry and start. Safe to call once; repeats are ignored.</summary>
        public void Start()
        {
            if (_started) return;
            _started = true;

            _session.Started += OnStarted;
            _session.Finished += OnFinished;
            _results.RaceAgainRequested += OnRaceAgain;
            _results.MainMenuRequested += OnMainMenu;
            if (_traces != null) _traces.TracesChanged += OnTracesChanged;

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
            _session.Started -= OnStarted;
            _session.Finished -= OnFinished;
            _results.RaceAgainRequested -= OnRaceAgain;
            _results.MainMenuRequested -= OnMainMenu;
            if (_traces != null) _traces.TracesChanged -= OnTracesChanged;
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
            _practiceFinished = false;
            _analysisPlayerSeries = null;
            _payoutId = Guid.NewGuid().ToString("N"); // one reward id per race start: a repeated finish cannot pay twice
            // The Race of the Day takes its length from the day's rule (the catalog has none); everything else from the catalog.
            _currentDistance = IsDaily(info.Id) && _daily.Today.DistanceMeters > 0f ? _daily.Today.DistanceMeters : info.DistanceMeters;
            _session.RequestStart(new RaceStartRequest(info.Id, _currentDistance));
            return true;
        }

        /// <summary>Per-frame sample of the player's run for the Analysis chart (the Zenject shell ticks this).</summary>
        public void Tick()
        {
            if (_splits == null || !_splits.IsActive || _session.State != RaceSessionState.Running) return;
            _splits.Sample(_session.Distance, _session.RaceTime);
        }

        private void OnStarted()
        {
            // The recording numbers live in the Analysis config, reached through the Results view (it owns the Analysis screen).
            if (!(_results is IAnalysisSink sink) || sink.SplitStepMeters <= 0f)
            {
                _splits = null; // no Analysis screen to feed: nothing is recorded
                return;
            }
            _splits = new RaceSplitRecorder(sink.SplitStepMeters, sink.FinishToleranceMeters);
            _splits.Begin(_session.TargetDistance);
        }

        private void OnFinished(RaceOutcome outcome)
        {
            if (_resultsOpen || _practiceFinished) return; // exactly one Results per race

            // Practice ends on the line: no Results, no reward, no personal / day best. The HUD stays up with Exit / Restart.
            if (_isPractice != null && _isPractice(outcome.EventId))
            {
                _practiceFinished = true;
                return;
            }

            string title;
            if (_hasCurrent && _current.Id == outcome.EventId) title = _current.DisplayName;
            else if (_events.TryGet(outcome.EventId, out RaceEventInfo info)) title = info.DisplayName;
            else title = outcome.EventId.ToString();

            _resultsOpen = true;
            if (string.IsNullOrEmpty(_payoutId)) _payoutId = Guid.NewGuid().ToString("N");
            int gems = _rewards.Resolve(outcome, _payoutId); // once per race (the guard above); the resolver pays once per id too
            RaceCashReward cash = _cashRewards != null ? _cashRewards.Resolve(outcome, _payoutId) : RaceCashReward.None; // ticketed events only; once per payout id
            ResultsData resultsData = BuildResults(title, outcome);
            _results.Present(resultsData);
            PresentAnalysis(title, outcome);
            if (_results is IGemRewardView gemView) gemView.ShowGemReward(gems); // 0 clears the line of a previous race
            if (_results is IRaceCashRewardView cashView) cashView.ShowCashReward(cash); // empty clears the block of a previous race
            _navigator.Push(_results);
        }

        /// <summary>Closes the player's split series on the finish and hands the Analysis data to the Results view.</summary>
        private void PresentAnalysis(string title, RaceOutcome outcome)
        {
            if (!(_results is IAnalysisSink sink) || sink.SplitStepMeters <= 0f) return;

            // A run that never reached the line (or a force-finish at ~0 m) stays a not-finished series: empty state, no invented time.
            if (_splits == null)
            {
                _splits = new RaceSplitRecorder(sink.SplitStepMeters, sink.FinishToleranceMeters);
                _splits.Begin(_session.TargetDistance);
            }
            _splits.Sample(_session.Distance, _session.RaceTime);
            float distance = outcome.DistanceMeters > 0f ? outcome.DistanceMeters : _session.TargetDistance;
            _analysisPlayerSeries = _splits.Finish(AnalysisModel.PlayerRunnerId, outcome.TimeSeconds);
            _analysisTitle = title;
            _analysisDistance = distance;
            _analysisRank = outcome.Rank;
            RedrawAnalysis(sink);
        }

        /// <summary>A bot crossed the line after the player: the open Analysis data gets its time instead of DNF.</summary>
        private void OnTracesChanged()
        {
            if (_resultsOpen && _analysisPlayerSeries != null && _results is IAnalysisSink sink) RedrawAnalysis(sink);
        }

        private void RedrawAnalysis(IAnalysisSink sink)
        {
            sink.PresentAnalysis(AnalysisModel.Build(_analysisTitle, _wallet != null ? _wallet.Cash : 0, _analysisDistance,
                _settings != null ? _settings.PlayerName : string.Empty, _analysisRank, _analysisPlayerSeries, BuildRivals(sink)));
        }

        /// <summary>The bots' runs as Analysis series, with the same step and tolerance as the player's; one still running is a DNF so far.</summary>
        private List<AnalysisRival> BuildRivals(IAnalysisSink sink)
        {
            if (_traces == null) return null;
            IReadOnlyList<RaceTrace> traces = _traces.Traces;
            var rivals = new List<AnalysisRival>(traces.Count);
            for (int i = 0; i < traces.Count; i++)
            {
                RaceTrace trace = traces[i];
                var recorder = new RaceSplitRecorder(sink.SplitStepMeters, sink.FinishToleranceMeters);
                recorder.Begin(_analysisDistance);
                foreach (RaceTraceSample sample in trace.Samples) recorder.Sample(sample.DistanceMeters, sample.Seconds);
                int runnerId = AnalysisModel.PlayerRunnerId + 1 + i;
                rivals.Add(new AnalysisRival(trace.Name, recorder.Finish(runnerId, trace.Finished ? trace.FinishSeconds : 0d)));
            }
            return rivals;
        }

        /// <summary>
        /// Records the time (once per race: the payout id is the race id) and turns the outcome into screen data.
        /// No other runners are known yet, so the table holds the player's row only.
        /// </summary>
        private ResultsData BuildResults(string title, RaceOutcome outcome)
        {
            // A Race of the Day stores its time under the event id plus the distance, so a best only ever compares equal lengths.
            bool daily = IsDaily(outcome.EventId);
            RaceEventId recordId = daily ? _daily.RecordIdFor(_currentDistance) : outcome.EventId;

            bool hasBest = false;
            bool isNewRecord = false;
            double best = 0d;
            if (_records != null)
            {
                PersonalBestUpdate update = _records.Record(recordId, outcome.TimeSeconds, _payoutId);
                hasBest = update.HasBest;
                isNewRecord = update.IsNewRecord;
                best = update.BestSeconds;
            }

            bool hasDayBest = TryGetTodaysBest(recordId, out long dayBestMilliseconds);
            bool dayBest = hasDayBest && ResultsModel.IsDayBest(true, dayBestMilliseconds, outcome.TimeSeconds);
            string playerName = _settings != null ? _settings.PlayerName : string.Empty;
            return ResultsModel.Build(new ResultsInput(title, outcome.Rank, outcome.TimeSeconds,
                playerName,
                _wallet != null ? _wallet.Cash : 0,
                hasBest, best, isNewRecord, null, dayBest,
                daily ? BuildDailyInput(hasDayBest, dayBestMilliseconds, playerName) : default));
        }

        private bool IsDaily(RaceEventId id) => _daily != null && _daily.IsRaceOfTheDay(id);

        /// <summary>The day's world record is the field's fastest result unless the player's own best of the day beats it.</summary>
        private DailyResultsInput BuildDailyInput(bool hasDayBest, long dayBestMilliseconds, string playerName)
        {
            bool hasField = _daily.TryGetFieldRecord(out DailyFieldRecord field);
            string ownName = string.IsNullOrWhiteSpace(playerName) ? ResultsTexts.DefaultPlayerName : playerName.Trim();
            bool hasRecord = ResultsModel.PickDailyRecord(hasField, field.Name, field.ElapsedMilliseconds,
                hasDayBest, ownName, dayBestMilliseconds, out string recordName, out long recordMilliseconds);
            return new DailyResultsInput(hasDayBest, dayBestMilliseconds, hasRecord, recordName, recordMilliseconds);
        }

        /// <summary>After Record: today's stored best of the id (a slower run leaves a lower stored value than this race's time).</summary>
        private bool TryGetTodaysBest(RaceEventId id, out long milliseconds)
        {
            milliseconds = 0L;
            if (_dayBests == null || _clock == null) return false;
            _dayScratch.Clear();
            _dayBests.CopyDayBests(id, _dayScratch);
            long today = _clock.UtcNow.Date.Ticks;
            foreach (DayBestRecord record in _dayScratch)
            {
                if (record.DayUtcTicks != today) continue;
                milliseconds = record.ElapsedMilliseconds;
                return true;
            }
            return false;
        }

        private void OnRaceAgain()
        {
            if (!_resultsOpen) return;
            Restart();
        }

        /// <summary>
        /// Resets an unfinished attempt directly. From Results, enters a new race through the entry gate.
        /// Ignored before the first start or during a scene move.
        /// </summary>
        public void Restart()
        {
            if (!_started || !_hasCurrent || _sceneFlow.IsTransitioning) return;

            // Restarting an unfinished attempt must not be blocked by the entry balance.
            // Results Race Again still enters a new race through the gate below.
            if (!_resultsOpen)
            {
                BeginRace(_current.Id);
                return;
            }

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
