using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Persistence;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// The first race's tutorial in the real Race scene: at GO the game waits, the wrong side is no step, the asked side is, it never holds
    /// the game speed once the prompts are done, and it is saved as seen, so the next race has none. The save is restored afterwards.
    /// </summary>
    public sealed class RaceTutorialSmokeTests
    {
        private const float TapSeconds = 0.13f;
        private const float SlowedDownSeconds = 0.4f;
        private const int Left = 0;
        private const int Right = 1;
        private const int PromptedSteps = 4;
        private const float HintsSeconds = 12f;

        private SaveFolderSnapshot _save;

        [SetUp]
        public void SetUp() => _save = SaveFolderSnapshot.Take();

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            _save.Restore();
        }

        private static IRaceSession Session()
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
                if (behaviour is IRaceSession session) return session;
            return null;
        }

        private static IEnumerator Tap(MethodInfo inject, object input, int leg)
        {
            inject.Invoke(input, new object[] { leg, true, Time.timeAsDouble });
            yield return new WaitForSecondsRealtime(TapSeconds);
            inject.Invoke(input, new object[] { leg, false, Time.timeAsDouble });
        }

        [UnityTest]
        public IEnumerator FirstRace_AsksForEachSide_ThenRunsFree_AndIsSavedAsSeen()
        {
            yield return LoadScene(RaceScene, tutorialSeen: false);
            PlayerProgress progress = ProjectContext.Instance.Container.Resolve<ProgressRepository>().Current;
            IRaceSession session = Session();
            Assert.IsNotNull(session, "No IRaceSession in the Race scene.");
            var signal = (IRaceStartSignal)session;
            yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "GO", 15f);

            object body = session.GetType().GetProperty("PlayerBody").GetValue(session);
            object input = body.GetType().GetField("input").GetValue(body);
            MethodInfo inject = input.GetType().GetMethod("Inject");
            object sim = body.GetType().GetProperty("Sim").GetValue(body);
            PropertyInfo presses = sim.GetType().GetProperty("Presses");

            yield return new WaitForSecondsRealtime(SlowedDownSeconds);
            Assert.Less(Time.timeScale, 0.5f, "at GO the game waits for the first tap");

            yield return Tap(inject, input, Right);
            yield return null;
            Assert.AreEqual(0, (int)presses.GetValue(sim), "the wrong side is no step");
            Assert.Less(Time.timeScale, 0.5f, "and the game goes on waiting");

            for (int i = 0; i < PromptedSteps; i++)
            {
                yield return Tap(inject, input, i % 2 == 0 ? Left : Right);
                Assert.AreEqual(i + 1, (int)presses.GetValue(sim), $"prompt {i}: the asked side is a step");
                yield return new WaitForSecondsRealtime(SlowedDownSeconds);
            }
            Assert.AreEqual(1f, Time.timeScale, 0.001f, "the prompts are over: normal speed");
            Assert.IsFalse(progress.RaceTutorialDone, "the hints are still to come");

            yield return WaitUntil(() => progress.RaceTutorialDone, "the tutorial to be saved as seen", HintsSeconds);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        [UnityTest]
        public IEnumerator Seen_NoPromptHoldsTheGame()
        {
            yield return LoadScene(RaceScene);
            IRaceSession session = Session();
            Assert.IsNotNull(session, "No IRaceSession in the Race scene.");
            var signal = (IRaceStartSignal)session;
            yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "GO", 15f);

            yield return new WaitForSecondsRealtime(SlowedDownSeconds);

            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }
    }
}
