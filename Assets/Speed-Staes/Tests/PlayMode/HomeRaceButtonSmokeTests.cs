using System.Collections;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using SpeedStars.UI.Race;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// Button-flow smoke tests on the real Home and Race scenes (Build Settings): panels open on the button, close on
    /// their Close/Back button, Play goes through the entry gate, the race scene is reached and left again. EditMode tests
    /// measure the logic; these press the buttons the way a player does (onClick on a button that is visible and
    /// interactable), so a Close button that is not wired or not pressable fails here.
    /// Each test leaves the player's save as it found it: the save folder is restored byte for byte and the few in-memory
    /// progress fields the tests touch are put back. No balance number is asserted; ticket counts are compared to each other.
    /// </summary>
    public sealed class HomeRaceButtonSmokeTests
    {
        private SaveFolderSnapshot _saveFiles;
        private int _entryTickets;
        private int _timedTickets;
        private long _regenDue;
        private long _timedExpires;
        private bool _progressCaptured;
        private RaceEventId _selected;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveFiles = SaveFolderSnapshot.Take();
            _progressCaptured = false;
            yield return LoadScene(HomeScene);

            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            _entryTickets = progress.EntryTickets;
            _timedTickets = progress.TimedTickets;
            _regenDue = progress.EntryRegenDueUtcTicks;
            _timedExpires = progress.TimedAccessExpiresUtcTicks;
            _selected = Container.Resolve<IRaceSelection>().Selected;
            _progressCaptured = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance)
            {
                SceneFlow flow = Container.TryResolve<SceneFlow>();
                if (flow != null) yield return WaitUntil(() => !flow.IsTransitioning, "scene move to finish", 15f);

                Container.Resolve<IRaceEntryGate>().TryTakePending(out _);
                if (_progressCaptured)
                {
                    PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
                    progress.EntryTickets = _entryTickets;
                    progress.TimedTickets = _timedTickets;
                    progress.EntryRegenDueUtcTicks = _regenDue;
                    progress.TimedAccessExpiresUtcTicks = _timedExpires;
                    if (!_selected.IsEmpty) Container.Resolve<IRaceSelection>().Select(_selected);
                }
            }
            _saveFiles.Restore();
        }

        private static DiContainer Container => ProjectContext.Instance.Container;

        // ---- 1. Home opens ----

        [UnityTest]
        public IEnumerator Home_Opens_WithHomeScreenVisible_AndNoErrorLogs()
        {
            HomeScreenView home = Require<HomeScreenView>();
            yield return WaitUntil(() => home.IsVisible, "HomeScreen to be visible");

            Assert.IsTrue(home.isActiveAndEnabled);
            Assert.IsNotNull(Field<Button>(home, "playButton"), "playButton not bound");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        // ---- 2. Seven-day popup ----

        [UnityTest]
        public IEnumerator SevenDay_Button_OpensPopup_AndCloseButtonClosesIt()
        {
            HomeScreenView home = Require<HomeScreenView>();
            UiPopup popup = RequireField<UiPopup>(home, "sevenDayPopup");
            Assert.IsFalse(popup.gameObject.activeSelf, "popup must start closed");

            yield return Tap(RequireField<Button>(home, "sevenDayButton"), "7-day button");
            yield return WaitOpen(popup, "7-day popup");

            yield return Tap(CloseButtonOf(popup), "7-day popup Close");
            yield return WaitClosed(popup, "7-day popup");
            Assert.IsTrue(home.gameObject.activeInHierarchy, "Home must stay up under a popup");
        }

        // ---- 3. High scores and Edit racer screens ----

        [UnityTest]
        public IEnumerator HighScores_Button_OpensScreen_AndBackReturnsHome()
        {
            yield return ScreenRoundTrip("highScoresButton", "highScoresScreen");
        }

        [UnityTest]
        public IEnumerator EditRacer_Button_OpensCosmeticsScreen_AndBackReturnsHome()
        {
            yield return ScreenRoundTrip("editRacerButton", "cosmeticsScreen");
        }

        private static IEnumerator ScreenRoundTrip(string buttonField, string screenField)
        {
            HomeScreenView home = Require<HomeScreenView>();
            UiScreen screen = RequireField<UiScreen>(home, screenField);
            Assert.IsFalse(screen.gameObject.activeSelf, $"{screenField} must start closed");

            yield return Tap(RequireField<Button>(home, buttonField), buttonField);
            yield return WaitOpen(screen, screenField);
            yield return WaitClosed(home, "HomeScreen to step aside");

            yield return Tap(CloseButtonOf(screen), $"{screenField} Back");
            yield return WaitClosed(screen, screenField);
            yield return WaitOpen(home, "HomeScreen to come back");
        }

        // ---- 4. Event select ----

        [UnityTest]
        public IEnumerator RaceSelect_Button_OpensEventSelect_AndBackClosesIt()
        {
            HomeScreenView home = Require<HomeScreenView>();
            EventSelectScreenView events = RequireField<EventSelectScreenView>(home, "eventSelectScreen");
            Assert.IsFalse(events.gameObject.activeSelf, "event select must start closed");

            yield return Tap(RequireField<Button>(home, "eventSelectButton"), "RaceSelect button");
            yield return WaitOpen(events, "event select screen");

            yield return Tap(CloseButtonOf(events), "event select Back");
            yield return WaitClosed(events, "event select screen");
            yield return WaitOpen(home, "HomeScreen to come back");
        }

        // ---- 5. Play without a ticket ----

        [UnityTest]
        public IEnumerator Play_WithoutTicket_OpensEntryGate_AndCloseClosesIt()
        {
            SelectTicketEvent();
            SetTickets(0);

            HomeScreenView home = Require<HomeScreenView>();
            EntryGateModalView gate = RequireField<EntryGateModalView>(home, "entryGateModal");
            Assert.IsFalse(gate.gameObject.activeSelf, "entry gate must start closed");

            yield return Tap(RequireField<Button>(home, "playButton"), "Play button");
            yield return WaitOpen(gate, "entry gate");
            Assert.AreEqual(HomeScene, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "no ticket: Play must not leave Home");

            yield return Tap(CloseButtonOf(gate), "entry gate Close");
            yield return WaitClosed(gate, "entry gate");
            Assert.IsFalse(Container.Resolve<IRaceEntryGate>().HasPendingEntry, "closing the gate must not leave an entry pending");
        }

        // ---- 6. Play with a ticket: Race scene, exit, results ----

        [UnityTest]
        public IEnumerator Play_WithTicket_UseEntryTicket_LoadsRaceScene_AndSpendsOneTicket()
        {
            int before = SetTickets(1);
            yield return EnterRaceWithTicket();

            Assert.Less(Container.Resolve<IEntryTickets>().Count, before, "entering a ticketed race must spend a ticket");
            Assert.IsNotNull(Require<RaceHudView>(), "Race scene has no HUD");
        }

        [UnityTest]
        public IEnumerator Race_ExitButton_ReturnsToHome()
        {
            SetTickets(1);
            yield return EnterRaceWithTicket();

            RaceHudView hud = Require<RaceHudView>();
            yield return Tap(RequireField<Button>(hud, "exitButton"), "HUD Exit");
            yield return WaitForScene(HomeScene);

            HomeScreenView home = Require<HomeScreenView>();
            yield return WaitUntil(() => home.IsVisible, "HomeScreen after Exit");
        }

        [UnityTest]
        public IEnumerator Race_Finish_OpensResults_AndMainMenuReturnsToHome()
        {
            SetTickets(1);
            yield return EnterRaceWithTicket();

            // The finish comes from the runner (Assembly-CSharp). The Dev adapter hook finishes with the current time;
            // it is reached by reflection because this assembly cannot reference the Bridge.
            yield return WaitUntil(() => FindRaceSession() != null, "race session");
            IRaceSession session = FindRaceSession();
            System.Reflection.MethodInfo finish = session.GetType().GetMethod("DevForceFinish");
            if (finish == null) Assert.Ignore("RaceSessionAdapter.DevForceFinish is not compiled in (non-dev build): no way to finish the race from a test.");
            finish.Invoke(session, null);

            ResultsScreenView results = Require<ResultsScreenView>();
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results screen to open after the finish");

            yield return Tap(RequireField<Button>(results, "mainMenuButton"), "Results Main Menu");
            yield return WaitForScene(HomeScene);
            HomeScreenView home = Require<HomeScreenView>();
            yield return WaitUntil(() => home.IsVisible, "HomeScreen after Main Menu");
        }

        // ---- helpers ----

        private IEnumerator EnterRaceWithTicket()
        {
            SelectTicketEvent();
            HomeScreenView home = Require<HomeScreenView>();
            EntryGateModalView gate = RequireField<EntryGateModalView>(home, "entryGateModal");

            yield return Tap(RequireField<Button>(home, "playButton"), "Play button");
            yield return WaitOpen(gate, "entry gate");
            yield return Tap(RequireField<Button>(gate, "useTicketButton"), "Use Entry Ticket");
            yield return WaitForScene(RaceScene);
        }

        private static IRaceSession FindRaceSession()
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
                if (behaviour is IRaceSession session) return session;
            return null;
        }

        /// <summary>Selects a playable event that costs a ticket (from the catalog asset, not a hard-coded id).</summary>
        private static void SelectTicketEvent()
        {
            RaceEventCatalog catalog = Container.Resolve<RaceEventCatalog>();
            foreach (RaceEventDefinition definition in catalog.GetEvents())
            {
                if (!definition.IsPlayable || !definition.RequiresTicket) continue;
                Container.Resolve<IRaceSelection>().Select(definition.Id);
                return;
            }
            Assert.Ignore("The event catalog has no playable event that requires a ticket; the ticket flow cannot be exercised.");
        }

        /// <summary>Gives the player exactly <paramref name="count"/> tickets and no running Timed access. Returns the count.</summary>
        private static int SetTickets(int count)
        {
            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            IEntryTickets tickets = Container.Resolve<IEntryTickets>();
            Assert.GreaterOrEqual(tickets.Cap, count, "test wants more tickets than the cap allows");
            progress.EntryTickets = count;
            progress.EntryRegenDueUtcTicks = 0;
            progress.TimedAccessExpiresUtcTicks = 0;
            tickets.Refresh();
            return tickets.Count;
        }
    }
}
