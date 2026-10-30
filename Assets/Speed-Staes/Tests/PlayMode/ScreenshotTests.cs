using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using SpeedStars.UI.Leaderboard;
using SpeedStars.UI.Race;
using SpeedStars.UI.Rewards;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// Evidence shots taken in Play mode: Home and Race with injected views, live numbers and the menu character posed,
    /// UI included. Only runs when the command line carries -shotDir <folder>; a normal test run ignores it and writes
    /// nothing. Overlay canvases do not reach a camera's RenderTexture, so for the shot they are switched to Screen Space -
    /// Camera and put back afterwards. The save is restored like in the button smoke tests.
    /// Run: Unity -batchmode (graphics on) -runTests -testPlatform PlayMode -testFilter Shots_HomeAndRace -shotDir &lt;folder&gt;
    /// </summary>
    public sealed class ScreenshotTests
    {
        private const int Width = 2400;
        private const int Height = 1080;
        private const float SettleSeconds = 1f;

        [UnityTest]
        public IEnumerator Shots_HomeAndRace()
        {
            string dir = ArgValue("-shotDir");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("No -shotDir argument: screenshot test only runs when asked for.");
            Directory.CreateDirectory(dir);

            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            bool captured = false;
            int entryTickets = 0, timedTickets = 0;
            long regenDue = 0, timedExpires = 0;
            string dailyKey = string.Empty;
            RaceEventId selected = default;
            try
            {
                yield return LoadScene(HomeScene);

                DiContainer container = ProjectContext.Instance.Container;
                PlayerProgress progress = container.Resolve<ProgressRepository>().Current;
                entryTickets = progress.EntryTickets;
                timedTickets = progress.TimedTickets;
                regenDue = progress.EntryRegenDueUtcTicks;
                timedExpires = progress.TimedAccessExpiresUtcTicks;
                dailyKey = progress.DailyGiftDayKey;
                selected = container.Resolve<IRaceSelection>().Selected;
                captured = true;

                // Daily gift: due today with room for a ticket, Home opens again and shows it by itself (taken at once, then put away unclaimed).
                progress.DailyGiftDayKey = string.Empty;
                progress.EntryTickets = 1;
                progress.EntryRegenDueUtcTicks = 0;
                container.Resolve<IEntryTickets>().Refresh();
                yield return LoadScene(HomeScene, dismissDailyGift: false);
                DailyGiftPopupView gift = Require<DailyGiftPopupView>();
                yield return WaitOpen(gift, "daily gift popup");
                yield return Settle();
                yield return Capture(dir, "Home_dailygift");
                yield return DismissDailyGift();

                HomeScreenView home = Require<HomeScreenView>();
                yield return WaitUntil(() => home.IsVisible, "HomeScreen to be visible");
                yield return Settle();
                yield return Capture(dir, "Home_play");

                // 7 day rewards: the calendar button opens the popup over Home.
                UiPopup sevenDay = RequireField<UiPopup>(home, "sevenDayPopup");
                yield return Tap(RequireField<Button>(home, "sevenDayButton"), "sevenDayButton");
                yield return WaitOpen(sevenDay, "7 day panel");
                yield return Settle();
                yield return Capture(dir, "Home_sevenday");
                yield return Tap(CloseButtonOf(sevenDay), "7 day panel X");
                yield return WaitClosed(sevenDay, "7 day panel");
                yield return Settle();

                // Home screens: open, shoot, close (same buttons and close path as the smoke tests).
                yield return ShootScreen(dir, home, "eventSelectButton", "eventSelectScreen", "Home_eventselect");
                yield return ShootScreen(dir, home, "environmentButton", "themeSelectScreen", "Home_themeselect");
                yield return ShootScreen(dir, home, "editRacerButton", "cosmeticsScreen", "Home_costume");
                yield return ShootScreen(dir, home, "highScoresButton", "highScoresScreen", "Home_leaderboard");
                yield return ShootScreen(dir, home, "emoteButton", "emoteScreen", "Home_emote");

                // Settings is a popup over Home (Home stays open behind its dimscreen).
                UiPopup settings = RequireField<UiPopup>(home, "settingsPopup");
                yield return Tap(RequireField<Button>(home, "settingsButton"), "settingsButton");
                yield return WaitOpen(settings, "settings popup");
                yield return Settle();
                yield return Capture(dir, "Home_settings");
                yield return Tap(CloseButtonOf(settings), "settings X");
                yield return WaitClosed(settings, "settings popup");
                yield return Settle();

                // Entry gate: Play with no ticket opens it.
                SelectTicketEvent(container);
                progress.EntryTickets = 0;
                progress.EntryRegenDueUtcTicks = 0;
                progress.TimedAccessExpiresUtcTicks = 0;
                container.Resolve<IEntryTickets>().Refresh();
                EntryGateModalView emptyGate = RequireField<EntryGateModalView>(home, "entryGateModal");
                yield return Tap(RequireField<Button>(home, "playButton"), "Play button (no ticket)");
                yield return WaitOpen(emptyGate, "entry gate");
                yield return Settle();
                yield return Capture(dir, "Home_entrygate");
                yield return Tap(CloseButtonOf(emptyGate), "entry gate Close");
                yield return WaitClosed(emptyGate, "entry gate");

                // Race: a ticketed event, exactly one ticket, then Use Entry Ticket.
                SelectTicketEvent(container);
                progress.EntryTickets = 1;
                progress.EntryRegenDueUtcTicks = 0;
                progress.TimedAccessExpiresUtcTicks = 0;
                container.Resolve<IEntryTickets>().Refresh();

                EntryGateModalView gate = RequireField<EntryGateModalView>(home, "entryGateModal");
                yield return Tap(RequireField<Button>(home, "playButton"), "Play button");
                yield return WaitOpen(gate, "entry gate");
                yield return Tap(RequireField<Button>(gate, "useTicketButton"), "Use Entry Ticket");
                yield return WaitForScene(RaceScene);

                // Countdown: the SET word (or, on a very slow load, whatever is up by then), taken before the race moves on.
                IRaceStartSignal startSignal = null;
                foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
                    if (behaviour is IRaceStartSignal found) { startSignal = found; break; }
                if (startSignal != null)
                {
                    yield return WaitUntil(() => startSignal.StartPhase == RaceStartPhase.Set || startSignal.StartPhase == RaceStartPhase.Go, "SET to show");
                    yield return null;
                    yield return null;
                    yield return Capture(dir, "Race_countdown");
                }

                yield return Settle();
                yield return Capture(dir, "Race_play");

                // Results: finish through the adapter's dev hook (dev builds only; reached by reflection, this assembly
                // cannot reference the Bridge), then shoot the results screen.
                IRaceSession session = null;
                foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
                    if (behaviour is IRaceSession found) { session = found; break; }
                System.Reflection.MethodInfo finish = session?.GetType().GetMethod("DevForceFinish");
                if (finish != null)
                {
                    finish.Invoke(session, null);
                    ResultsScreenView results = UnityEngine.Object.FindObjectOfType<ResultsScreenView>(true);
                    yield return WaitUntil(() => results != null && results.gameObject.activeInHierarchy, "results screen");
                    yield return Settle();
                    yield return Capture(dir, "Race_results");

                    // Analysis: Stats on the results screen opens it (a force-finish at ~0 m shows the empty state, not a crash).
                    AnalysisScreenView analysis = RequireField<AnalysisScreenView>(results, "analysisScreen");
                    yield return Tap(RequireField<Button>(results, "statsButton"), "Results Stats");
                    yield return WaitOpen(analysis, "analysis screen");
                    yield return Settle();
                    yield return Capture(dir, "Race_analysis");
                }
            }
            finally
            {
                if (ProjectContext.HasInstance)
                {
                    DiContainer container = ProjectContext.Instance.Container;
                    container.Resolve<IRaceEntryGate>().TryTakePending(out _);
                    if (captured)
                    {
                        PlayerProgress progress = container.Resolve<ProgressRepository>().Current;
                        progress.EntryTickets = entryTickets;
                        progress.TimedTickets = timedTickets;
                        progress.EntryRegenDueUtcTicks = regenDue;
                        progress.TimedAccessExpiresUtcTicks = timedExpires;
                        progress.DailyGiftDayKey = dailyKey;
                        if (!selected.IsEmpty) container.Resolve<IRaceSelection>().Select(selected);
                    }
                }
                saveFiles.Restore();
            }
        }

        /// <summary>
        /// T-059 evidence: the six frames of the design storyboard (IMPLEMENTATION.md) on the real Race scene: Race_start_01_impact,
        /// _02_rebound, _03_compress, _04_hold, _05_release, _06_clear. The countdown is stopped (time scale 0, so the session's clock
        /// stands) once READY is up, and the view shows each pose at an offset worked out from the RaceStartConfig (never fixed seconds).
        /// Also checks that READY is what the first frame of the countdown shows.
        /// </summary>
        [UnityTest]
        public IEnumerator Shots_RaceStartPhases()
        {
            string dir = ArgValue("-shotDir");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("No -shotDir argument: screenshot test only runs when asked for.");
            Directory.CreateDirectory(dir);

            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            float previousScale = Time.timeScale;
            try
            {
                yield return LoadScene(RaceScene);

                IRaceStartSignal startSignal = null;
                foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
                    if (behaviour is IRaceStartSignal found) { startSignal = found; break; }
                Assert.IsNotNull(startSignal, "No IRaceStartSignal in the Race scene.");
                Time.timeScale = 0f; // the session ticks on scaled time: the countdown stands still from here
                Assert.AreNotEqual(RaceStartPhase.Idle, startSignal.StartPhase, "The countdown has not begun after the Race scene loaded.");

                RaceStartView view = Require<RaceStartView>();
                RaceStartConfig config = RequireField<RaceStartConfig>(view, "config");
                Text word = RequireField<Text>(view, "wordText");
                if (startSignal.StartPhase == RaceStartPhase.Ready)
                {
                    Assert.IsTrue(word.transform.IsChildOf(view.transform) && word.gameObject.activeInHierarchy, "READY is not on screen in the first frames.");
                    Assert.AreEqual("READY", word.text);
                }

                float enter = config.ReadyEnterSeconds;
                float rebound = config.ReboundSeconds;
                float compress = config.SetCompressSeconds;
                float go = config.GoSeconds;
                yield return ShootStart(dir, view, "Race_start_01_impact", RaceStartPhase.Ready, enter * 0.67f);
                yield return ShootStart(dir, view, "Race_start_02_rebound", RaceStartPhase.Ready, enter + rebound * 0.5f);
                yield return ShootStart(dir, view, "Race_start_03_compress", RaceStartPhase.Set, compress);
                yield return ShootStart(dir, view, "Race_start_04_hold", RaceStartPhase.Set, compress + config.HoldCycleSeconds * 1.25f);
                yield return ShootStart(dir, view, "Race_start_05_release", RaceStartPhase.Go, go * 0.27f);
                yield return ShootStart(dir, view, "Race_start_06_clear", RaceStartPhase.Go, go * 0.93f);
            }
            finally
            {
                Time.timeScale = previousScale;
                saveFiles.Restore();
            }
        }

        private static IEnumerator ShootStart(string dir, RaceStartView view, string name, RaceStartPhase phase, float seconds)
        {
            view.PreviewAt(phase, seconds);
            yield return null;
            yield return null;
            yield return Capture(dir, name);
        }

        /// <summary>T-051 evidence: Home with a fake camera hole on the left edge (resource bar and corner buttons pulled in, art full-bleed).</summary>
        [UnityTest]
        public IEnumerator Shot_HomeSafeArea()
        {
            string dir = ArgValue("-shotDir");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("No -shotDir argument: screenshot test only runs when asked for.");
            Directory.CreateDirectory(dir);

            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            try
            {
                yield return LoadScene(HomeScene);
                SafeAreaTestSupport.Install(SafeAreaTestSupport.LeftNotch());
                HomeScreenView home = Require<HomeScreenView>();
                yield return WaitUntil(() => home.IsVisible, "HomeScreen to be visible");
                yield return Settle();
                yield return Capture(dir, "Home_safearea");
            }
            finally
            {
                saveFiles.Restore();
            }
        }

        /// <summary>
        /// T-054 evidence: the entry gate in every state it can show, opened straight from the view (Play skips the gate while
        /// Timed access runs): Home_entrygate_ready (tickets, USE TICKET works), _full (TICKETS FULL, offers closed), _timed
        /// (a Timed Ticket in stock: USE TIMED TICKET shows) and _timedactive (the window runs: ticket button hidden).
        /// Home_entrygate (no ticket) is taken by Shots_HomeAndRace.
        /// </summary>
        [UnityTest]
        public IEnumerator Shots_EntryGateStates()
        {
            string dir = ArgValue("-shotDir");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("No -shotDir argument: screenshot test only runs when asked for.");
            Directory.CreateDirectory(dir);

            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            bool captured = false;
            int entryTickets = 0, timedTickets = 0;
            long regenDue = 0, timedExpires = 0;
            RaceEventId selected = default;
            try
            {
                yield return LoadScene(HomeScene);

                DiContainer container = ProjectContext.Instance.Container;
                PlayerProgress progress = container.Resolve<ProgressRepository>().Current;
                IEntryTickets tickets = container.Resolve<IEntryTickets>();
                entryTickets = progress.EntryTickets;
                timedTickets = progress.TimedTickets;
                regenDue = progress.EntryRegenDueUtcTicks;
                timedExpires = progress.TimedAccessExpiresUtcTicks;
                selected = container.Resolve<IRaceSelection>().Selected;
                captured = true;

                RaceEventDefinition definition = SelectTicketEvent(container);
                HomeScreenView home = Require<HomeScreenView>();
                yield return WaitUntil(() => home.IsVisible, "HomeScreen to be visible");
                EntryGateModalView gate = RequireField<EntryGateModalView>(home, "entryGateModal");

                // 1. Tickets in hand, no Timed Ticket: USE TICKET works.
                SetGateState(progress, tickets, 1, 0, 0L);
                gate.Open(definition);
                yield return WaitOpen(gate, "entry gate");
                yield return Settle();
                yield return Capture(dir, "Home_entrygate_ready");
                yield return CloseGate(gate);

                // 2. Inventory full: TICKETS FULL, both offers closed.
                SetGateState(progress, tickets, tickets.Cap, 0, 0L);
                gate.Open(definition);
                yield return WaitOpen(gate, "entry gate");
                yield return Settle();
                yield return Capture(dir, "Home_entrygate_full");
                yield return CloseGate(gate);

                // 3. A Timed Ticket in stock: USE TIMED TICKET shows between the buttons.
                SetGateState(progress, tickets, 1, 2, 0L);
                gate.Open(definition);
                yield return WaitOpen(gate, "entry gate");
                yield return Settle();
                yield return Capture(dir, "Home_entrygate_timed");
                yield return CloseGate(gate);

                // 4. The Timed window runs: the ticket button is gone, the Timed row counts down.
                long end = container.Resolve<IClock>().UtcNow.Ticks + TimeSpan.FromMinutes(25).Ticks;
                SetGateState(progress, tickets, 1, 1, end);
                gate.Open(definition);
                yield return WaitOpen(gate, "entry gate");
                yield return Settle();
                yield return Capture(dir, "Home_entrygate_timedactive");
                yield return CloseGate(gate);
            }
            finally
            {
                if (ProjectContext.HasInstance)
                {
                    DiContainer container = ProjectContext.Instance.Container;
                    if (captured)
                    {
                        PlayerProgress progress = container.Resolve<ProgressRepository>().Current;
                        progress.EntryTickets = entryTickets;
                        progress.TimedTickets = timedTickets;
                        progress.EntryRegenDueUtcTicks = regenDue;
                        progress.TimedAccessExpiresUtcTicks = timedExpires;
                        container.Resolve<IEntryTickets>().Refresh();
                        if (!selected.IsEmpty) container.Resolve<IRaceSelection>().Select(selected);
                    }
                }
                saveFiles.Restore();
            }
        }

        /// <summary>
        /// T-060 evidence: the leaderboard's filter windows and table states. Home_lb_region, _region_search_empty, _period,
        /// _event_sprints / _hurdles / _relays come from the real local leaderboard with real taps (a category tab that is not
        /// on offer is skipped). _empty, _notranked, _loading and _error need a source answer the local one never gives, so a
        /// fake ILeaderboard is put in the screen's private field before its first opening (no product seam exists for it).
        /// </summary>
        [UnityTest]
        public IEnumerator Shots_LeaderboardStates()
        {
            string dir = ArgValue("-shotDir");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("No -shotDir argument: screenshot test only runs when asked for.");
            Directory.CreateDirectory(dir);

            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            try
            {
                // ---- real source: the three filter windows ----
                yield return LoadScene(HomeScene);
                HomeScreenView home = Require<HomeScreenView>();
                yield return WaitUntil(() => home.IsVisible, "HomeScreen to be visible");
                UiScreen screen = RequireField<UiScreen>(home, "highScoresScreen");
                yield return Tap(RequireField<Button>(home, "highScoresButton"), "highScoresButton");
                yield return WaitOpen(screen, "highScoresScreen");
                yield return WaitClosed(home, "HomeScreen to step aside");
                yield return Settle();
                ScreenNavigator navigator = SceneNavigator();

                // Region window, then the search with no match.
                UiPopup region = RequireField<UiPopup>(screen, "regionPopup");
                yield return Tap(RequireField<Button>(screen, "regionButton"), "region field");
                yield return WaitOpen(region, "region window");
                yield return Settle();
                yield return Capture(dir, "Home_lb_region");
                InputField search = RequireField<InputField>(screen, "searchInput");
                search.text = "xyz";
                yield return Settle();
                yield return Capture(dir, "Home_lb_region_search_empty");
                search.text = string.Empty;
                yield return CloseWindow(navigator, region, "region window");

                // Period window.
                UiPopup period = RequireField<UiPopup>(screen, "periodPopup");
                yield return Tap(RequireField<Button>(screen, "periodButton"), "period field");
                yield return WaitOpen(period, "period window");
                yield return Settle();
                yield return Capture(dir, "Home_lb_period");
                yield return CloseWindow(navigator, period, "period window");

                // Event modal: one shot per category tab, then Cancel (the active filter stays).
                UiPopup eventModal = RequireField<UiPopup>(screen, "eventModal");
                yield return Tap(RequireField<Button>(screen, "eventButton"), "event field");
                yield return WaitOpen(eventModal, "event modal");
                yield return Settle();
                LeaderboardChoiceView[] tabs = RequireField<LeaderboardChoiceView[]>(screen, "categoryTabs");
                string[] tabShots = { "Home_lb_event_sprints", "Home_lb_event_hurdles", "Home_lb_event_relays" };
                for (int i = 0; i < tabs.Length && i < tabShots.Length; i++)
                {
                    if (tabs[i] == null || !tabs[i].gameObject.activeSelf)
                    {
                        Debug.Log($"SHOT_SKIP {tabShots[i]}: category tab {i} is not on offer.");
                        continue;
                    }
                    yield return Tap(RequireField<Button>(tabs[i], "button"), $"category tab {i}");
                    yield return Settle();
                    yield return Capture(dir, tabShots[i]);
                }
                yield return Tap(RequireField<Button>(screen, "cancelButton"), "event modal Cancel");
                yield return WaitClosed(eventModal, "event modal");
                yield return Settle();

                yield return Tap(CloseButtonOf(screen), "highScoresScreen Back");
                yield return WaitClosed(screen, "highScoresScreen");
                yield return WaitOpen(home, "HomeScreen to come back");

                // ---- fake source: states the local leaderboard cannot produce ----
                yield return ShootFakeLeaderboard(dir, "Home_lb_empty", new FakeLeaderboard(FakeAnswer.Empty));
                yield return ShootFakeLeaderboard(dir, "Home_lb_notranked", new FakeLeaderboard(FakeAnswer.RowsWithoutOwn));
                yield return ShootFakeLeaderboard(dir, "Home_lb_loading", new FakeLeaderboard(FakeAnswer.Never));
                yield return ShootFakeLeaderboard(dir, "Home_lb_error", new FakeLeaderboard(FakeAnswer.Error));
            }
            finally
            {
                saveFiles.Restore();
            }
        }

        /// <summary>Fresh Home, the fake put in before the screen's first opening (the presenter is built then), open, shoot, Back.</summary>
        private static IEnumerator ShootFakeLeaderboard(string dir, string shotName, ILeaderboard fake)
        {
            yield return LoadScene(HomeScene);
            HomeScreenView home = Require<HomeScreenView>();
            yield return WaitUntil(() => home.IsVisible, "HomeScreen to be visible");
            UiScreen screen = RequireField<UiScreen>(home, "highScoresScreen");
            Assert.IsNull(GetPrivate(screen, "_presenter"), $"{shotName}: the leaderboard screen was opened before the fake was put in.");
            SetPrivate(screen, "_leaderboard", fake);

            yield return Tap(RequireField<Button>(home, "highScoresButton"), "highScoresButton");
            yield return WaitOpen(screen, "highScoresScreen");
            yield return WaitClosed(home, "HomeScreen to step aside");
            yield return Settle();
            yield return Capture(dir, shotName);

            yield return Tap(CloseButtonOf(screen), "highScoresScreen Back");
            yield return WaitClosed(screen, "highScoresScreen");
            yield return WaitOpen(home, "HomeScreen to come back");
        }

        /// <summary>Back / Escape path: the window's catcher sits under the window, so a centre tap on it would hit the window.</summary>
        private static IEnumerator CloseWindow(ScreenNavigator navigator, UiPopup window, string what)
        {
            Assert.IsTrue(navigator.Back(), $"{what}: device Back with the window open.");
            yield return WaitClosed(window, what);
            yield return Settle();
        }

        private static ScreenNavigator SceneNavigator()
        {
            SceneContext context = UnityEngine.Object.FindObjectOfType<SceneContext>();
            Assert.IsNotNull(context, "Home SceneContext");
            return context.Container.Resolve<ScreenNavigator>();
        }

        private const System.Reflection.BindingFlags AnyInstance =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;

        private static object GetPrivate(object owner, string name)
        {
            for (Type type = owner.GetType(); type != null; type = type.BaseType)
            {
                System.Reflection.FieldInfo field = type.GetField(name, AnyInstance);
                if (field != null) return field.GetValue(owner);
            }
            Assert.Fail($"{owner.GetType().Name} has no field '{name}'.");
            return null;
        }

        private static void SetPrivate(object owner, string name, object value)
        {
            for (Type type = owner.GetType(); type != null; type = type.BaseType)
            {
                System.Reflection.FieldInfo field = type.GetField(name, AnyInstance);
                if (field == null) continue;
                field.SetValue(owner, value);
                return;
            }
            Assert.Fail($"{owner.GetType().Name} has no field '{name}'.");
        }

        private enum FakeAnswer
        {
            Empty,
            RowsWithoutOwn,
            Never,
            Error
        }

        /// <summary>A leaderboard source with a fixed behaviour: no rows, rows with no own entry, no answer (loading) or an error.</summary>
        private sealed class FakeLeaderboard : ILeaderboard
        {
            private readonly FakeAnswer _answer;

            public FakeLeaderboard(FakeAnswer answer) { _answer = answer; }

            public event Action Changed { add { } remove { } }

            public void Query(LeaderboardQuery query, Action<LeaderboardPage> onDone, Action<string> onError)
            {
                var when = new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);
                switch (_answer)
                {
                    case FakeAnswer.Empty:
                        onDone(new LeaderboardPage(new List<LeaderboardRow>(), 0, -1, null, when));
                        break;
                    case FakeAnswer.RowsWithoutOwn:
                        var rows = new List<LeaderboardRow>();
                        for (int i = 0; i < 12; i++)
                            rows.Add(new LeaderboardRow("fake:" + i, i + 1, "RUNNER " + (i + 1), "US", 9500L + i * 37L, when, false));
                        onDone(new LeaderboardPage(rows, rows.Count, -1, null, when));
                        break;
                    case FakeAnswer.Error:
                        onError?.Invoke("fake error");
                        break;
                    default:
                        break; // never answers: the screen stays on Loading
                }
            }

            public bool TryGetStanding(RaceEventId eventId, out int rank, out int total)
            {
                rank = 0;
                total = 0;
                return false;
            }

            public bool TryGetBestOfDay(RaceEventId eventId, out LeaderboardRow best)
            {
                best = null;
                return false;
            }
        }

        private static void SetGateState(PlayerProgress progress, IEntryTickets tickets, int entry, int timedStock, long timedExpiresTicks)
        {
            progress.EntryTickets = entry;
            progress.TimedTickets = timedStock;
            progress.EntryRegenDueUtcTicks = 0;
            progress.TimedAccessExpiresUtcTicks = timedExpiresTicks;
            tickets.Refresh();
        }

        private static IEnumerator CloseGate(EntryGateModalView gate)
        {
            yield return Tap(CloseButtonOf(gate), "entry gate Close");
            yield return WaitClosed(gate, "entry gate");
        }

        private static IEnumerator ShootScreen(string dir, HomeScreenView home, string buttonField, string screenField, string shotName)
        {
            UiScreen screen = RequireField<UiScreen>(home, screenField);
            yield return Tap(RequireField<Button>(home, buttonField), buttonField);
            yield return WaitOpen(screen, screenField);
            yield return WaitClosed(home, "HomeScreen to step aside");
            yield return Settle();
            yield return Capture(dir, shotName);

            yield return Tap(CloseButtonOf(screen), $"{screenField} Back");
            yield return WaitClosed(screen, screenField);
            yield return WaitOpen(home, "HomeScreen to come back");
            yield return Settle();
        }

        private static IEnumerator Settle()
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < SettleSeconds) yield return null;
        }

        private static IEnumerator Capture(string dir, string name)
        {
            // Not WaitForEndOfFrame: it never fires in batchmode (no player loop render), and the camera is rendered by hand below.
            yield return null;
            Camera camera = Camera.main;
            Assert.IsNotNull(camera, $"{name}: no main camera in the scene.");

            var restore = new List<(Canvas canvas, RenderMode mode, Camera cam, float plane)>();
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                restore.Add((canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.1f + canvas.sortingOrder * 0.001f;
            }
            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(Width, Height, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                string file = Path.Combine(dir, name + ".png");
                File.WriteAllBytes(file, tex.EncodeToPNG());
                Assert.IsTrue(File.Exists(file) && new FileInfo(file).Length > 0, $"{name}: shot file was not written.");
                Debug.Log($"SHOT_OK {file}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                foreach (var entry in restore)
                {
                    if (entry.canvas == null) continue;
                    entry.canvas.renderMode = entry.mode;
                    entry.canvas.worldCamera = entry.cam;
                    entry.canvas.planeDistance = entry.plane;
                }
                rt.Release();
                UnityEngine.Object.Destroy(rt);
                UnityEngine.Object.Destroy(tex);
            }
        }

        private static RaceEventDefinition SelectTicketEvent(DiContainer container)
        {
            RaceEventCatalog catalog = container.Resolve<RaceEventCatalog>();
            foreach (RaceEventDefinition definition in catalog.GetEvents())
            {
                if (!definition.IsPlayable || !definition.RequiresTicket) continue;
                container.Resolve<IRaceSelection>().Select(definition.Id);
                return definition;
            }
            Assert.Ignore("The event catalog has no playable event that requires a ticket; the Race shot cannot be taken.");
            return null;
        }

        private static string ArgValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
