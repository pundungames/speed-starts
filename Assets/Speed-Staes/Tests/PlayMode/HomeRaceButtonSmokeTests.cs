using System.Collections;
using System.Linq;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Costume;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using SpeedStars.UI.Race;
using TMPro;
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
        public IEnumerator Home_FirstFrameEntry_CompletesAFullRunnerTurn()
        {
            // Load directly, as on launch, without a preceding SceneFlow request.
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(HomeScene);
            MonoBehaviour runner = Object.FindObjectsOfType<MonoBehaviour>()
                .First(behaviour => behaviour.GetType().Name == "MenuCharacter");
            float previousYaw = runner.transform.eulerAngles.y;
            float totalYaw = 0f;
            yield return WaitUntil(() =>
            {
                float yaw = runner.transform.eulerAngles.y;
                totalYaw += Mathf.DeltaAngle(previousYaw, yaw);
                previousYaw = yaw;
                return Mathf.Abs(totalYaw) >= 358f;
            }, "one complete runner rotation on direct Home entry");
        }

        [UnityTest]
        public IEnumerator Home_SavedRacerChange_RestartsTheFullTurn()
        {
            yield return new WaitForSecondsRealtime(UnitySceneLoader.DepartureDuration + 0.1f);
            HomeScreenView home = Require<HomeScreenView>();
            ICosmeticService cosmetics = Container.Resolve<ICosmeticService>();
            int originalPreset = cosmetics.SelectedPreset;
            Transform runner = Object.FindObjectsOfType<MonoBehaviour>()
                .First(behaviour => behaviour.GetType().Name == "MenuCharacter").transform;
            Transform camera = Camera.main.transform;
            Vector3 cameraPosition = camera.position;
            Quaternion cameraRotation = camera.rotation;
            float previousYaw = runner.eulerAngles.y;
            float totalYaw = 0f;

            try
            {
                Quaternion rest = runner.rotation;
                Button next = RequireField<Button>(home, "nextRacerButton");
                yield return Tap(next, "next saved racer");
                int firstPreset = cosmetics.SelectedPreset;
                Assert.AreNotEqual(originalPreset, firstPreset);
                yield return WaitUntil(() => Quaternion.Angle(rest, runner.rotation) >= 45f, "first racer to start turning");
                next.onClick.Invoke();
                Assert.AreNotEqual(firstPreset, cosmetics.SelectedPreset);
                Assert.Less(Quaternion.Angle(rest, runner.rotation), 1f, "each new racer starts from the rest heading");
                float restartedAt = Time.realtimeSinceStartup;
                float startedTurningAt = -1f;
                yield return WaitUntil(() =>
                {
                    float yaw = runner.eulerAngles.y;
                    totalYaw += Mathf.DeltaAngle(previousYaw, yaw);
                    previousYaw = yaw;
                    if (startedTurningAt < 0f && Mathf.Abs(totalYaw) >= 2f)
                        startedTurningAt = Time.realtimeSinceStartup;
                    Assert.Less(Vector3.Distance(cameraPosition, camera.position), 0.01f);
                    Assert.Less(Quaternion.Angle(cameraRotation, camera.rotation), 0.1f);
                    return Mathf.Abs(totalYaw) >= 358f;
                }, "one complete turn after switching saved racer");
                Assert.Less(startedTurningAt - restartedAt, 0.22f, "the new racer starts without the scene intro hold");
                Assert.Less(Time.realtimeSinceStartup - restartedAt, 2.15f, "the preset spin finishes within two seconds");
            }
            finally
            {
                cosmetics.SelectPreset(originalPreset);
            }
        }

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

        /// <summary>The leaderboard's filter windows are navigator popups: Back closes the open window first, the screen second.</summary>
        [UnityTest]
        public IEnumerator HighScores_Back_ClosesAnOpenFilterWindowBeforeLeavingTheScreen()
        {
            HomeScreenView home = Require<HomeScreenView>();
            UiScreen screen = RequireField<UiScreen>(home, "highScoresScreen");
            yield return Tap(RequireField<Button>(home, "highScoresButton"), "highScoresButton");
            yield return WaitOpen(screen, "highScoresScreen");
            yield return WaitClosed(home, "HomeScreen to step aside");

            UiPopup window = RequireField<UiPopup>(screen, "periodPopup");
            yield return Tap(RequireField<Button>(screen, "periodButton"), "period field");
            yield return WaitOpen(window, "period window");

            // The window's catcher covers the on-screen Back, so the first press is the device Back / Escape path
            // (BackKeyReader calls exactly ScreenNavigator.Back()).
            // ScreenNavigator is bound per scene (SceneUiInstaller), not in the ProjectContext.
            SceneContext sceneContext = UnityEngine.Object.FindObjectOfType<SceneContext>();
            Assert.IsNotNull(sceneContext, "Home SceneContext");
            Assert.IsTrue(sceneContext.Container.Resolve<ScreenNavigator>().Back(), "device Back with a window open");
            yield return WaitClosed(window, "period window");
            Assert.IsTrue(screen.gameObject.activeInHierarchy, "the first Back only closes the window");

            yield return Tap(CloseButtonOf(screen), "Back");
            yield return WaitClosed(screen, "highScoresScreen");
            yield return WaitOpen(home, "HomeScreen to come back");
        }

        [UnityTest]
        public IEnumerator EditRacer_Button_OpensCosmeticsScreen_AndBackReturnsHome()
        {
            yield return ScreenRoundTrip("editRacerButton", "cosmeticsScreen");
        }

        [UnityTest]
        public IEnumerator EditRacer_ColourBoxes_TapSelectsTheChannel_AndOthersLoseTheHighlight()
        {
            HomeScreenView home = Require<HomeScreenView>();
            UiScreen screen = RequireField<UiScreen>(home, "cosmeticsScreen");
            yield return Tap(RequireField<Button>(home, "editRacerButton"), "editRacerButton");
            yield return WaitOpen(screen, "cosmeticsScreen");

            // The boxes shown are the channels of the worn item of the first tab (1 to 3, whatever the catalog says).
            var picker = RequireField<CostumeColorPickerView>(screen, "colorPicker");
            var boxes = RequireField<CostumeChannelBoxView[]>(picker, "channelBoxes");
            var shown = new System.Collections.Generic.List<CostumeChannelBoxView>();
            foreach (CostumeChannelBoxView box in boxes)
                if (box.gameObject.activeInHierarchy) shown.Add(box);
            Assert.IsNotEmpty(shown, "the colour panel must show at least one colour box");

            foreach (CostumeChannelBoxView box in shown)
            {
                yield return Tap(RequireField<Button>(box, "button"), $"colour box {box.Index + 1}");
                Assert.IsTrue(RequireField<GameObject>(box, "selectedHighlight").activeSelf, $"box {box.Index + 1} must be highlighted after the tap");
                foreach (CostumeChannelBoxView other in shown)
                    if (other != box)
                        Assert.IsFalse(RequireField<GameObject>(other, "selectedHighlight").activeSelf, $"box {other.Index + 1} must lose the highlight");
            }

            // Pick a colour on box 1, switch to box 2: box 1 keeps the picked colour, box 2 shows its own.
            if (shown.Count > 1)
            {
                var sv = RequireField<ColorDragArea>(picker, "svArea");
                yield return Tap(RequireField<Button>(shown[0], "button"), "colour box 1");
                Color before2 = RequireField<Image>(shown[1], "colorImage").color;
                var pick = typeof(ColorDragArea).GetMethod("Report", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.IsNotNull(pick, "ColorDragArea.Report");
                pick.Invoke(sv, new object[] { new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                    { position = RectTransformUtility.WorldToScreenPoint(null, sv.transform.position), button = UnityEngine.EventSystems.PointerEventData.InputButton.Left } });
                Color picked = RequireField<Image>(shown[0], "colorImage").color;
                yield return Tap(RequireField<Button>(shown[1], "button"), "colour box 2");
                AssertColorClose(picked, RequireField<Image>(shown[0], "colorImage").color, "box 1 keeps the picked colour");
                AssertColorClose(before2, RequireField<Image>(shown[1], "colorImage").color, "box 2 shows its own colour");
            }

            yield return Tap(CloseButtonOf(screen), "cosmeticsScreen Back");
            yield return WaitClosed(screen, "cosmeticsScreen");
            yield return WaitOpen(home, "HomeScreen to come back");
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
        public IEnumerator Play_WithoutTicket_OpensTicketsStoreAndCloseClosesIt()
        {
            SelectTicketEvent();
            SetTickets(0);

            HomeScreenView home = Require<HomeScreenView>();
            EntryGateModalView gate = RequireField<EntryGateModalView>(home, "entryGateModal");
            Assert.IsFalse(gate.gameObject.activeSelf, "entry gate must start closed");

            yield return Tap(RequireField<Button>(home, "playButton"), "Play button");
            yield return WaitOpen(gate, "entry gate");
            Assert.AreEqual("TICKETS STORE", RequireField<TMP_Text>(gate, "titleText").text);
            Assert.IsFalse(RequireField<Button>(gate, "useTicketButton").gameObject.activeSelf);
            Assert.AreEqual(HomeScene, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "no ticket: Play must not leave Home");

            yield return Tap(CloseButtonOf(gate), "entry gate Close");
            yield return WaitClosed(gate, "entry gate");
            Assert.IsFalse(Container.Resolve<IRaceEntryGate>().HasPendingEntry, "closing the gate must not leave an entry pending");
        }

        // ---- 6. Play with a ticket: Race scene, exit, results ----

        [UnityTest]
        public IEnumerator Play_WithTimedAccess_ShowsVipAndStartsWithoutSpendingATicket()
        {
            SetTickets(0);
            SelectTicketEvent();
            Container.Resolve<ProgressRepository>().Current.TimedAccessExpiresUtcTicks =
                System.DateTime.UtcNow.AddMinutes(15).Ticks;
            HomeScreenView home = Require<HomeScreenView>();
            Button play = RequireField<Button>(home, "playButton");
            TMP_Text cost = play.transform.Find("Entry Cost").GetComponent<TMP_Text>();
            yield return WaitUntil(() => cost.text.StartsWith("VIP FREE"), "VIP Play style");

            yield return Tap(play, "VIP Play");
            Assert.IsFalse(RequireField<EntryGateModalView>(home, "entryGateModal").IsVisible);
            Assert.IsNull(Field<GameObject>(home, "_entryOverlay"), "VIP entry does not fly a one-use ticket");
            yield return WaitForScene(RaceScene);
            Assert.AreEqual(0, Container.Resolve<IEntryTickets>().Count);
        }

        [UnityTest]
        public IEnumerator Sprints100m_WithNoTickets_StartsDirectlyWithoutEntryGate()
        {
            SetTickets(0);
            var sprint = new RaceEventId("100m");
            Container.Resolve<IRaceSelection>().Select(sprint);
            HomeScreenView home = Require<HomeScreenView>();
            var gate = RequireField<EntryGateModalView>(home, "entryGateModal");
            yield return Tap(RequireField<Button>(home, "playButton"), "SPRINTS 100M Play");

            yield return WaitUntil(() =>
            {
                Assert.IsFalse(gate && gate.gameObject.activeInHierarchy, "free 100 m must bypass the entry popup");
                return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == RaceScene;
            }, "free 100 m to start", 15f);

            yield return WaitUntil(() => FindRaceSession()?.TargetDistance == 100f, "100 m sprint session");
            Assert.AreEqual(0, Container.Resolve<IEntryTickets>().Count, "no ticket is spent");
        }

        [UnityTest]
        public IEnumerator Play_WithTicket_FliesToPlayAndStartsWithoutConfirmation()
        {
            int before = SetTickets(1);
            yield return EnterRaceWithTicket(verifyDepartureMotion: true);

            Assert.Less(Container.Resolve<IEntryTickets>().Count, before, "entering a ticketed race must spend a ticket");
            Assert.IsNotNull(Require<RaceHudView>(), "Race scene has no HUD");
        }

        [UnityTest]
        public IEnumerator RaceOfTheDay_FromEventPicker_Starts100MetersAndLocksAfterEntry()
        {
            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            long before = progress.LastDailyRaceEntryUtcTicks;
            ICosmeticService cosmetics = Container.Resolve<ICosmeticService>();
            int originalPreset = cosmetics.SelectedPreset;
            progress.LastDailyRaceEntryUtcTicks = 0;
            try
            {
                HomeScreenView home = Require<HomeScreenView>();
                var picker = Require<EventSelectScreenView>();
                yield return Tap(RequireField<Button>(home, "eventSelectButton"), "event picker");
                yield return WaitOpen(picker, "event picker");
                yield return Tap(RequireField<Button>(picker, "raceOfTheDayButton"), "Race of the Day");
                yield return Tap(RequireField<Button>(picker, "continueButton"), "confirm daily race");
                yield return WaitOpen(home, "Home after daily race selection");
                Assert.IsTrue(RequireField<Button>(home, "editRacerButton").gameObject.activeInHierarchy);
                yield return Tap(RequireField<Button>(home, "nextRacerButton"), "change the daily race preset");
                Assert.AreNotEqual(originalPreset, cosmetics.SelectedPreset);
                Assert.IsFalse(Container.Resolve<ICosmeticPreview>().IsPreviewing, "the event must not preview a different outfit");
                Transform menuRunner = Object.FindObjectsOfType<MonoBehaviour>()
                    .First(behaviour => behaviour.GetType().Name == "MenuCharacter").transform;
                string[] expectedParts = ActiveOutfitParts(menuRunner);
                yield return Tap(RequireField<Button>(home, "playButton"), "daily race Play");
                yield return WaitForScene(RaceScene);
                yield return WaitUntil(() => FindRaceSession()?.TargetDistance == 100f, "100 m daily race");
                Assert.Greater(progress.LastDailyRaceEntryUtcTicks, 0L);
                var signal = (IRaceStartSignal)FindRaceSession();
                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "daily race GO", 12f);
                Transform raceRunner = Object.FindObjectsOfType<MonoBehaviour>()
                    .First(behaviour => behaviour.GetType().Name == "RaceRunnerOutfit").transform;
                CollectionAssert.AreEquivalent(expectedParts, ActiveOutfitParts(raceRunner));
                Assert.IsFalse(RequireField<Button>(Require<RaceHudView>(), "restartButton").interactable);
                yield return Tap(RequireField<Button>(Require<RaceHudView>(), "exitButton"), "exit daily race");
                yield return WaitForScene(HomeScene);
                home = Require<HomeScreenView>();
                Assert.IsFalse(RequireField<Button>(home, "playButton").interactable, "daily race stays locked after leaving");
            }
            finally
            {
                progress.LastDailyRaceEntryUtcTicks = before;
                cosmetics.SelectPreset(originalPreset);
            }
        }

        private static string[] ActiveOutfitParts(Transform runner)
        {
            Transform accessories = runner.Find("Accessories");
            Assert.IsNotNull(accessories, "runner has an Accessories root");
            return accessories.Cast<Transform>()
                .SelectMany(slot => slot.Cast<Transform>().Where(part => part.gameObject.activeSelf)
                    .Select(part => slot.name + "/" + part.name)).ToArray();
        }

        [UnityTest]
        public IEnumerator Race_ExitButton_ReturnsToHome()
        {
            SetTickets(1);
            yield return EnterRaceWithTicket();

            RaceHudView hud = Require<RaceHudView>();
            yield return Tap(RequireField<Button>(hud, "exitButton"), "HUD Exit");
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.AreEqual(RaceScene, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                "Race must stay on screen during the departure animation");
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

        [UnityTest]
        public IEnumerator Race_Results_StatsOpensAnalysis_BackReturnsToResults()
        {
            SetTickets(1);
            yield return EnterRaceWithTicket();

            yield return WaitUntil(() => FindRaceSession() != null, "race session");
            IRaceSession session = FindRaceSession();
            System.Reflection.MethodInfo finish = session.GetType().GetMethod("DevForceFinish");
            if (finish == null) Assert.Ignore("RaceSessionAdapter.DevForceFinish is not compiled in (non-dev build): no way to finish the race from a test.");
            finish.Invoke(session, null);

            ResultsScreenView results = Require<ResultsScreenView>();
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results screen to open after the finish");

            AnalysisScreenView analysis = RequireField<AnalysisScreenView>(results, "analysisScreen");
            Button stats = RequireField<Button>(results, "statsButton");
            Assert.IsTrue(stats.interactable, "Results Stats must be pressable once the Analysis screen is in the scene.");

            yield return Tap(stats, "Results Stats");
            yield return WaitOpen(analysis, "Analysis screen");

            yield return Tap(CloseButtonOf(analysis), "Analysis Back");
            yield return WaitClosed(analysis, "Analysis screen");
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results screen after Analysis Back");
        }

        // ---- 7. Race start countdown (T-039) ----

        [UnityTest]
        public IEnumerator Race_Countdown_KeepsRunnerInputClosedUntilGo_ThenOpens()
        {
            SetTickets(1);
            yield return EnterRaceWithTicket();

            yield return WaitUntil(() => FindRaceSession() != null, "race session");
            IRaceSession session = FindRaceSession();
            var signal = session as IRaceStartSignal;
            Assert.IsNotNull(signal, "the race session must expose the start phases (IRaceStartSignal)");

            // The runner body is the adapter's private field and lives in Assembly-CSharp: reached by reflection.
            Component body = RequireField<Component>(session, "body");
            System.Reflection.FieldInfo acceptInput = body.GetType().GetField("acceptInput");
            Assert.IsNotNull(acceptInput, "RunnerBody.acceptInput is gone; the input gate cannot be checked.");

            // Frame by frame, whatever the timing: input is closed while the countdown shows and open from GO on.
            float start = Time.realtimeSinceStartup;
            while (signal.StartPhase != RaceStartPhase.Go)
            {
                if (signal.StartPhase == RaceStartPhase.Ready || signal.StartPhase == RaceStartPhase.Set)
                    Assert.IsFalse((bool)acceptInput.GetValue(body), $"input must be closed during {signal.StartPhase}");
                if (Time.realtimeSinceStartup - start > 10f) Assert.Fail($"the countdown never reached GO (phase {signal.StartPhase})");
                yield return null;
            }
            Assert.IsTrue((bool)acceptInput.GetValue(body), "input must be open at GO");

            // The start words react and clean up: the view is in the scene and hides itself after the burst.
            RaceStartView words = Require<RaceStartView>();
            Transform group = RequireField<RectTransform>(words, "group");
            yield return WaitUntil(() => !group.gameObject.activeSelf, "the GO! words to leave the screen");
        }

        [UnityTest]
        public IEnumerator Race_ForceFinishDuringCountdown_StillOpensResults_AndInputStaysClosed()
        {
            SetTickets(1);
            yield return EnterRaceWithTicket();

            yield return WaitUntil(() => FindRaceSession() != null, "race session");
            IRaceSession session = FindRaceSession();
            System.Reflection.MethodInfo finish = session.GetType().GetMethod("DevForceFinish");
            if (finish == null) Assert.Ignore("RaceSessionAdapter.DevForceFinish is not compiled in (non-dev build).");
            finish.Invoke(session, null);

            ResultsScreenView results = Require<ResultsScreenView>();
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results screen to open after the finish");

            // A GO that comes after the finish must not reopen input under Results (and there must be no start words left).
            Component body = RequireField<Component>(session, "body");
            System.Reflection.FieldInfo acceptInput = body.GetType().GetField("acceptInput");
            float until = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < until)
            {
                Assert.IsFalse((bool)acceptInput.GetValue(body), "input must stay closed under Results");
                yield return null;
            }
            Assert.AreEqual(RaceStartPhase.Idle, ((IRaceStartSignal)session).StartPhase, "the finish cancels the countdown");
        }

        // ---- helpers ----

        [UnityTest]
        public IEnumerator Race_RestartButton_ResetsPlayerClockCameraAndCountdown()
        {
            SetTickets(1);
            yield return EnterRaceWithTicket();
            SetTickets(0); // HUD restart must work even when entry used the last ticket.
            IRaceSession session = FindRaceSession();
            var signal = (IRaceStartSignal)session;
            yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "initial GO", 12f);
            Component body = RequireField<Component>(session, "body");
            Vector3 startPosition = body.transform.position;
            var input = body.GetType().GetField("input").GetValue(body);
            var inject = input.GetType().GetMethod("Inject");
            inject.Invoke(input, new object[] { 0, true, Time.timeAsDouble });
            yield return null;
            inject.Invoke(input, new object[] { 0, false, Time.timeAsDouble });
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(session.Distance, 0);
            // Reproduce a late restart after the streaming world has retired the start sections.
            ((Behaviour)body).enabled = false;
            body.transform.position = startPosition + body.transform.forward * 240f;
            yield return null;
            yield return null;
            var hud = Require<RaceHudView>();
            yield return Tap(RequireField<Button>(hud, "restartButton"), "HUD Restart");
            Assert.AreEqual(RaceStartPhase.Ready, signal.StartPhase);
            Assert.AreEqual(0, session.Distance, 0.001f);
            Assert.AreEqual(0, session.RaceTime, 0.001);
            // Back on the start line; every start draws its lane, so only the way along the track is the same.
            Assert.AreEqual(Vector3.Dot(startPosition, body.transform.forward), Vector3.Dot(body.transform.position, body.transform.forward), 0.001f);
            Assert.IsFalse((bool)body.GetType().GetField("acceptInput").GetValue(body));
            yield return null;
            Assert.AreEqual("0.00", RequireField<TMP_Text>(hud, "timeText").text);
            var follow = Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(c => c.GetType().Name == "SideFollowCamera");
            Assert.IsNotNull(follow);
            // The opening camera frames the runner from close by: no flight back over the 240 m it was away.
            float nearby = 2f * CameraField(follow, "distance") * CameraField(follow, "distanceScale");
            Assert.Less(Vector3.Distance(body.transform.position, follow.transform.position), nearby, "Restart must snap the camera back immediately.");
            yield return null;
            var chunks = Object.FindObjectsOfType<MonoBehaviour>().Where(c => c.GetType().Name == "LoopWayChunk" || c.GetType().Name == "ThemeChunk")
                .Where(c => c.gameObject.activeInHierarchy).ToArray();
            Assert.Greater(chunks.Length, 0, "Restart must restore streamed track sections.");
            Assert.LessOrEqual(chunks.Min(c => (int)c.GetType().GetField("index").GetValue(c)), 1,
                "Start sections retired by the previous run must be rebuilt.");
            ((Behaviour)body).enabled = true;
            yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "restart GO", 12f);
            Assert.IsTrue((bool)body.GetType().GetField("acceptInput").GetValue(body));
            yield return Tap(RequireField<Button>(hud, "exitButton"), "HUD Main Menu");
            yield return WaitForScene(HomeScene);
        }

        /// <summary>Reads a public float field of the follow camera (Assembly-CSharp type, reached by reflection).</summary>
        private static float CameraField(Component camera, string name)
        {
            System.Reflection.FieldInfo field = camera.GetType().GetField(name);
            Assert.IsNotNull(field, $"SideFollowCamera.{name} is gone; the expected camera position cannot be computed.");
            return (float)field.GetValue(camera);
        }

        private IEnumerator EnterRaceWithTicket(bool verifyDepartureMotion = false)
        {
            SelectTicketEvent();
            HomeScreenView home = Require<HomeScreenView>();
            EntryGateModalView gate = RequireField<EntryGateModalView>(home, "entryGateModal");

            Camera camera = Camera.main;
            GameObject runner = null;
            Quaternion cameraStart = Quaternion.identity;
            Quaternion runnerStart = Quaternion.identity;
            if (verifyDepartureMotion)
            {
                yield return new WaitForSecondsRealtime(UnitySceneLoader.DepartureDuration);
                Assert.IsNotNull(camera);
                runner = Object.FindObjectsOfType<MonoBehaviour>()
                    .First(behaviour => behaviour.GetType().Name == "MenuCharacter").gameObject;
                cameraStart = camera.transform.rotation;
                runnerStart = runner.transform.rotation;
            }

            Button play = RequireField<Button>(home, "playButton");
            TMP_Text entryCost = play.transform.Find("Entry Cost").GetComponent<TMP_Text>();
            entryCost.ForceMeshUpdate();
            Assert.AreEqual("x1", entryCost.GetParsedText());
            Assert.Less(entryCost.textInfo.characterInfo[0].pointSize, entryCost.textInfo.characterInfo[1].pointSize);
            int ticketsBefore = Container.Resolve<IEntryTickets>().Count;
            yield return Tap(play, "Play button");
            Assert.IsFalse(gate.IsVisible, "the ticket store is only used when short of tickets");
            var overlay = RequireField<GameObject>(home, "_entryOverlay");
            Assert.IsTrue(overlay.transform.Find("Flying Entry Ticket").gameObject.activeInHierarchy);
            Assert.AreEqual(ticketsBefore, Container.Resolve<IEntryTickets>().Count);
            yield return WaitUntil(() => overlay.transform.Find("Loading") != null, "the ticket to land and loading to start", 1f);
            if (verifyDepartureMotion)
            {
                float totalYaw = 0f;
                float previousYaw = runnerStart.eulerAngles.y;
                float peakCameraAngle = 0f;
                yield return WaitUntil(() =>
                {
                    Assert.IsTrue(camera && runner, "Home must remain visible through a full runner turn");
                    float yaw = runner.transform.eulerAngles.y;
                    totalYaw += Mathf.DeltaAngle(previousYaw, yaw);
                    previousYaw = yaw;
                    peakCameraAngle = Mathf.Max(peakCameraAngle, Quaternion.Angle(cameraStart, camera.transform.rotation));
                    return Mathf.Abs(totalYaw) >= 358f;
                }, "one complete runner rotation before Home unloads");
                Assert.Greater(peakCameraAngle, 2f, "The camera must move during the turn");
            }
            yield return WaitForScene(RaceScene);
        }

        /// <summary>Colours round-trip through hex in the save (8 bits per channel): compare within one step.</summary>
        private static void AssertColorClose(Color expected, Color actual, string what)
        {
            const float step = 1f / 255f + 0.0001f;
            Assert.IsTrue(Mathf.Abs(expected.r - actual.r) <= step && Mathf.Abs(expected.g - actual.g) <= step
                && Mathf.Abs(expected.b - actual.b) <= step && Mathf.Abs(expected.a - actual.a) <= step, $"{what}: expected {expected}, was {actual}");
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
