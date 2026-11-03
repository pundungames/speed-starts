using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Flow;
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
    /// Watch Replay (Malik, 03.11) inside our flows, pressed with real taps on the real Home and Race scenes (T-094): a 100 m race, the
    /// Race of the Day, and Practice. The finish is a REAL finish: the dev key (DevForceFinish) completes the session without the runner
    /// crossing the line, so the recorder never sees a finished player and Watch Replay would stay grey; the test shortens the scene's
    /// race length (the one public field of the RaceController, reached by reflection) and runs it with injected presses instead.
    /// No balance number is asserted: the length is only shortened, the Cash is compared with itself, the outfit with its own earlier look.
    /// Malik's types (Assembly-CSharp) are reached by reflection through the adapter's fields, the way the other smoke tests do.
    /// The save folder and the touched in-memory progress are put back.
    /// </summary>
    public sealed class WatchReplaySmokeTests
    {
        /// <summary>Fixture length of the shortened race (m): long enough that the replay lasts a few seconds, short enough to run quickly.</summary>
        private const float ShortRaceMeters = 15f;

        private SaveFolderSnapshot _saveFiles;
        private bool _captured;
        private int _cash;
        private RaceEventId _selected;

        private static DiContainer Container => ProjectContext.Instance.Container;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveFiles = SaveFolderSnapshot.Take();
            _captured = false;
            yield return LoadScene(HomeScene);

            _cash = Container.Resolve<ProgressRepository>().Current.Cash;
            _selected = Container.Resolve<IRaceSelection>().Selected;
            _captured = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance)
            {
                SceneFlow flow = Container.TryResolve<SceneFlow>();
                if (flow != null) yield return WaitUntil(() => !flow.IsTransitioning, "scene move to finish", 15f);

                Container.Resolve<IRaceEntryGate>().TryTakePending(out _);
                if (_captured)
                {
                    Container.Resolve<ProgressRepository>().Current.Cash = _cash;
                    if (!_selected.IsEmpty) Container.Resolve<IRaceSelection>().Select(_selected);
                }
            }
            _saveFiles.Restore();
        }

        // ---------------------------------------------------------------- 1. 100 m

        [UnityTest]
        public IEnumerator Race100m_WatchReplay_PlaysTheRace_ExitReturnsToResults_AndRaceAgainStartsAFreshCountdown()
        {
            yield return EnterRace(Pick100m());
            IRaceSession session = Session();
            yield return RunToTheLine(session);

            ResultsScreenView results = Require<ResultsScreenView>();
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results screen to open after the finish");
            IWallet wallet = Container.Resolve<IWallet>();
            int cashAfterFinish = wallet.Cash;

            IRaceReplay replay = RequireField<IRaceReplay>(results, "_replay");
            Button watch = RequireField<Button>(results, "watchReplayButton");
            yield return Tap(watch, "Results Watch Replay");
            yield return WaitUntil(() => replay.IsWatching, "the replay to start");

            // Controls are up and the Results screen steps aside.
            ReplayControlsView controls = Require<ReplayControlsView>();
            Assert.IsTrue(controls.gameObject.activeInHierarchy, "the replay controls must be visible");
            yield return WaitUntil(() => !results.gameObject.activeInHierarchy, "Results to step aside for the replay");

            // The race really plays: the clock runs, the followed runner is the player, runs, and its root moves forward.
            Assert.IsTrue(replay.FollowedIsPlayer, "the replay starts on the player's runner");
            Assert.GreaterOrEqual(replay.RunnerCount, 2, "a race with opponents is recorded with its opponents");
            Assert.Greater(replay.Duration, 0f, "the replay has a length");
            Transform runner = RequireField<Component>(session, "body").transform;
            float clock0 = replay.Time;
            Vector3 position0 = runner.position;
            yield return WaitUntil(() => replay.Time > clock0 + 0.5f, "the replay clock to advance");
            Assert.Greater(Vector3.Distance(runner.position, position0), 0.01f, "the followed runner must move while the replay plays");
            yield return WaitUntil(() => replay.FollowedSpeed > 0f, "the followed runner's speed to show on the replay");

            // Exit (the replay's own square button) puts everything back: runners live, controls gone, Results again.
            Transform exitTransform = controls.transform.Find("Exit");
            Assert.IsNotNull(exitTransform, "ReplayControlsView has no 'Exit' child any more.");
            yield return Tap(exitTransform.GetComponent<Button>(), "Replay Exit");
            yield return WaitUntil(() => !replay.IsWatching, "the replay to be left");
            yield return WaitUntil(() => Object.FindObjectOfType<ReplayControlsView>(true) == null, "the replay controls to go");
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results to come back after the replay");
            Assert.AreEqual(cashAfterFinish, wallet.Cash, "watching a replay must not pay the race again");

            // Race Again: a normal fresh start, READY / SET with the input closed, input open at GO, nothing paid for the old race again.
            yield return Tap(RequireField<Button>(results, "raceAgainButton"), "Results Race Again");
            yield return AssertFreshCountdown(session);
            Assert.AreEqual(cashAfterFinish, wallet.Cash, "Race Again must not pay the finished race a second time");
            yield return WaitUntil(() => !results.gameObject.activeInHierarchy, "Results to close for the new race");
        }

        [UnityTest]
        public IEnumerator Race100m_ReplayPlayedToItsEnd_ReturnsToResults_AndCanBeWatchedAgain()
        {
            yield return EnterRace(Pick100m());
            IRaceSession session = Session();
            yield return RunToTheLine(session);

            ResultsScreenView results = Require<ResultsScreenView>();
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results screen to open after the finish");
            int cashAfterFinish = Container.Resolve<IWallet>().Cash;

            IRaceReplay replay = RequireField<IRaceReplay>(results, "_replay");
            Button watch = RequireField<Button>(results, "watchReplayButton");
            yield return Tap(watch, "Results Watch Replay");
            yield return WaitUntil(() => replay.IsWatching, "the replay to start");
            Assert.IsNotNull(Require<ReplayControlsView>());

            // Jump to just before the end and let it play out: it ends by itself, leaves the runners live and brings Results back.
            replay.Seek(Mathf.Max(0f, replay.Duration - 0.3f));
            yield return WaitUntil(() => !replay.IsWatching, "the replay to play to its end", 10f);
            yield return WaitUntil(() => Object.FindObjectOfType<ReplayControlsView>(true) == null, "the replay controls to go");
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results to come back after the replay");
            Assert.AreEqual(cashAfterFinish, Container.Resolve<IWallet>().Cash, "a replay that played out must not pay the race again");

            // The recording is still there: Watch Replay works a second time and starts from the beginning.
            yield return WaitUntil(() => watch.interactable, "Watch Replay to be offered again");
            yield return Tap(watch, "Results Watch Replay (second time)");
            yield return WaitUntil(() => replay.IsWatching, "the second replay to start");
            Assert.Less(replay.Time, replay.Duration * 0.5f, "a new replay starts from the beginning");
            yield return WaitUntil(() => Object.FindObjectOfType<ReplayControlsView>(true) != null, "the replay controls to open again");

            replay.Leave(); // the adapter's own early exit (what the Exit button calls)
            yield return WaitUntil(() => !replay.IsWatching, "the second replay to be left");
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results to come back after the second replay");
        }

        // ---------------------------------------------------------------- 2. Race of the Day

        [UnityTest]
        public IEnumerator RaceOfTheDay_WatchReplay_KeepsTheDaysOutfit_AndTheDailyRowsOfTheResults()
        {
            IRaceOfTheDay daily = Container.TryResolve<IRaceOfTheDay>();
            if (daily == null) Assert.Ignore("WIRING: IRaceOfTheDay is not bound; there is no Race of the Day to play.");
            RaceEventDefinition definition = FindEvent(e => e.Id == daily.EventId);
            if (definition == null || !definition.IsPlayable) Assert.Ignore("The event catalog has no playable Race of the Day.");

            yield return EnterRace(definition);
            IRaceSession session = Session();
            Assert.IsTrue(daily.IsRaceOfTheDay(session.CurrentEventId()), "the race that started is the Race of the Day");
            var signal = (IRaceStartSignal)session;
            yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "GO", 15f);
            if (daily.Today.DistanceMeters > 0f)
                Assert.AreEqual(daily.Today.DistanceMeters, session.TargetDistance, 0.001f, "the day's rule sets the race length");

            // Look of every runner once the day's outfit is on (RaceOfTheDayOutfit dresses at the end of the READY frame).
            List<Transform> runners = RunnerRoots();
            Assert.IsNotEmpty(runners, "no runner on the field");
            string dressAtGo = Fingerprint(runners);
            Assert.IsNotEmpty(dressAtGo);

            yield return RunToTheLine(session, alreadyAtGo: true);
            ResultsScreenView results = Require<ResultsScreenView>();
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results screen to open after the finish");
            string[] rowFields = { "eventTitleText", "personalBestTitleText", "personalBestValueText", "personalBestEventText",
                "worldPositionTitleText", "worldPositionText", "dailyBestText", "cashText" };
            string rowsBefore = Snapshot(results, rowFields);
            Assert.IsNotEmpty(Field<TMP_Text>(results, "worldPositionText").text, "the Results show the day's record row");

            IRaceReplay replay = RequireField<IRaceReplay>(results, "_replay");
            yield return Tap(RequireField<Button>(results, "watchReplayButton"), "Results Watch Replay");
            yield return WaitUntil(() => replay.IsWatching, "the replay to start");
            float clock0 = replay.Time;
            yield return WaitUntil(() => replay.Time > clock0 + 0.5f, "the replay clock to advance");
            Assert.AreEqual(dressAtGo, Fingerprint(runners), "the runners must keep the day's outfit while the replay plays");

            replay.Seek(Mathf.Max(0f, replay.Duration - 0.3f));
            yield return WaitUntil(() => !replay.IsWatching, "the replay to play to its end", 10f);
            yield return WaitUntil(() => results.gameObject.activeInHierarchy, "Results to come back after the replay");
            yield return null;
            Assert.AreEqual(dressAtGo, Fingerprint(runners), "the runners must still wear the day's outfit after the replay");
            Assert.AreEqual(rowsBefore, Snapshot(results, rowFields), "the Results' Daily PB / world record rows must be the same after the replay");
        }

        // ---------------------------------------------------------------- 3. Practice (after T-092)

        [UnityTest]
        public IEnumerator Practice_FinishOpensNoResults_SoWatchReplayIsNotReachable()
        {
            RaceEventDefinition practice = FindEvent(e => e.IsPlayable && !e.RequiresTicket && !e.HasOpponents);
            if (practice == null) Assert.Ignore("The event catalog has no free playable event without opponents (Practice).");

            yield return EnterRace(practice);
            IRaceSession session = Session();
            ResultsScreenView results = Require<ResultsScreenView>();
            yield return RunToTheLine(session);

            // T-092: Practice keeps no Results; the finish is only the runner crossing the line. Wait well past the Results open time.
            float until = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < until)
            {
                Assert.IsFalse(results.gameObject.activeInHierarchy, "Practice must not open the Results screen (T-092), so Watch Replay is not reachable.");
                yield return null;
            }
            Assert.IsNull(Object.FindObjectOfType<ReplayControlsView>(true), "no replay controls without a Results screen");
        }

        // ---------------------------------------------------------------- helpers

        private static RaceEventDefinition FindEvent(System.Func<RaceEventDefinition, bool> match)
        {
            foreach (RaceEventDefinition definition in Container.Resolve<RaceEventCatalog>().GetEvents())
                if (match(definition)) return definition;
            return null;
        }

        /// <summary>A free, playable event with opponents and a length of its own that is not the Race of the Day: the 100 m of the catalog.</summary>
        private static RaceEventDefinition Pick100m()
        {
            IRaceOfTheDay daily = Container.TryResolve<IRaceOfTheDay>();
            RaceEventDefinition chosen = FindEvent(e => e.IsPlayable && !e.RequiresTicket && e.HasOpponents && e.DistanceMeters > 0f
                && (daily == null || !daily.IsRaceOfTheDay(e.Id)));
            if (chosen == null) Assert.Ignore("The event catalog has no free playable event with opponents.");
            return chosen;
        }

        private static IEnumerator EnterRace(RaceEventDefinition definition)
        {
            Container.Resolve<IRaceSelection>().Select(definition.Id);
            HomeScreenView home = Require<HomeScreenView>();
            yield return Tap(RequireField<Button>(home, "playButton"), "Play button");
            yield return WaitForScene(RaceScene);
            yield return WaitUntil(() => FindRaceSession() != null, "race session");
        }

        private static IRaceSession Session() => FindRaceSession();

        private static IRaceSession FindRaceSession()
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
                if (behaviour is IRaceSession session) return session;
            return null;
        }

        /// <summary>
        /// Waits for GO, shortens the scene's race length and runs it with injected presses until the runner crosses the line (the
        /// RaceController's LastRaceTime changes). Alternating legs at a steady rhythm; fails with the state if the line is never reached.
        /// </summary>
        private static IEnumerator RunToTheLine(IRaceSession session, bool alreadyAtGo = false)
        {
            var signal = (IRaceStartSignal)session;
            if (!alreadyAtGo) yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "GO", 15f);
            yield return WaitUntil(() => session.State == RaceSessionState.Running, "the race to run after GO", 10f);

            Component race = RequireField<Component>(session, "race");
            FieldInfo length = race.GetType().GetField("raceDistance");
            Assert.IsNotNull(length, "RaceController.raceDistance is gone; the race cannot be shortened for the test.");
            length.SetValue(race, Mathf.Min((float)length.GetValue(race), ShortRaceMeters));
            PropertyInfo lastTime = race.GetType().GetProperty("LastRaceTime");
            Assert.IsNotNull(lastTime, "RaceController.LastRaceTime is gone; the finish cannot be detected.");
            float before = (float)lastTime.GetValue(race);

            Component body = RequireField<Component>(session, "body");
            object input = body.GetType().GetField("input").GetValue(body);
            MethodInfo inject = input.GetType().GetMethod("Inject");
            Assert.IsNotNull(inject, "RunnerInput.Inject is gone; the test cannot press.");

            float start = Time.realtimeSinceStartup;
            int leg = 0;
            while (Mathf.Approximately((float)lastTime.GetValue(race), before))
            {
                if (Time.realtimeSinceStartup - start > 40f)
                    Assert.Fail($"the runner never crossed the shortened line ({session.Distance:0.0} of {session.TargetDistance:0.0} m, state {session.State}).");
                inject.Invoke(input, new object[] { leg, true, Time.timeAsDouble });
                yield return new WaitForSeconds(0.16f);
                inject.Invoke(input, new object[] { leg, false, Time.timeAsDouble });
                yield return new WaitForSeconds(0.16f);
                leg = 1 - leg;
            }
            // The recorder closes the player's track a frame after the line, Results follow the session's Finished event.
            yield return null;
            yield return null;
        }

        /// <summary>After Race Again: READY / SET keep the runner's input closed, GO opens it, the runner stands at the line again.</summary>
        private static IEnumerator AssertFreshCountdown(IRaceSession session)
        {
            var signal = (IRaceStartSignal)session;
            Component body = RequireField<Component>(session, "body");
            FieldInfo acceptInput = body.GetType().GetField("acceptInput");
            Assert.IsNotNull(acceptInput, "RunnerBody.acceptInput is gone; the input gate cannot be checked.");

            Assert.AreEqual(0f, session.Distance, 0.001f, "Race Again starts from the line");
            bool sawCountdown = false;
            float start = Time.realtimeSinceStartup;
            while (signal.StartPhase != RaceStartPhase.Go)
            {
                if (signal.StartPhase == RaceStartPhase.Ready || signal.StartPhase == RaceStartPhase.Set)
                {
                    sawCountdown = true;
                    Assert.IsFalse((bool)acceptInput.GetValue(body), $"input must be closed during {signal.StartPhase}");
                }
                if (Time.realtimeSinceStartup - start > 15f) Assert.Fail($"the new countdown never reached GO (phase {signal.StartPhase}).");
                yield return null;
            }
            Assert.IsTrue(sawCountdown, "Race Again must run READY / SET again before GO");
            Assert.IsTrue((bool)acceptInput.GetValue(body), "input must be open at GO");
        }

        /// <summary>Every runner of the field (player and opponents) while their bodies are live.</summary>
        private static List<Transform> RunnerRoots()
        {
            var roots = new List<Transform>();
            foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>())
                if (behaviour.GetType().Name == "RunnerBody" && behaviour.isActiveAndEnabled) roots.Add(behaviour.transform);
            roots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return roots;
        }

        /// <summary>The look of the runners: every renderer with its visibility, materials and colours. Equal strings = same outfit.</summary>
        private static string Fingerprint(List<Transform> roots)
        {
            var text = new StringBuilder();
            foreach (Transform root in roots)
            {
                text.Append('[').Append(root.name).Append(']');
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    text.Append(renderer.name).Append(renderer.enabled && renderer.gameObject.activeInHierarchy ? '+' : '-');
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null)
                        {
                            text.Append("|none");
                            continue;
                        }
                        text.Append('|').Append(material.GetInstanceID());
                        if (material.HasProperty("_Color")) text.Append(material.color);
                    }
                    text.Append(';');
                }
            }
            return text.ToString();
        }

        private static string Snapshot(object view, params string[] textFields)
        {
            var text = new StringBuilder();
            foreach (string name in textFields)
            {
                TMP_Text field = Field<TMP_Text>(view, name);
                text.Append(name).Append('=').Append(field != null ? field.text : "<unbound>").Append('\n');
            }
            return text.ToString();
        }
    }

    internal static class RaceSessionExtensions
    {
        /// <summary>The event of the race in progress (RaceSessionAdapter.CurrentEventId; not on the Core interface).</summary>
        public static RaceEventId CurrentEventId(this IRaceSession session)
        {
            PropertyInfo property = session.GetType().GetProperty("CurrentEventId");
            Assert.IsNotNull(property, "RaceSessionAdapter.CurrentEventId is gone.");
            return (RaceEventId)property.GetValue(session);
        }
    }
}
