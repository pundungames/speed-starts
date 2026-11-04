using System.Collections;
using NUnit.Framework;
using SpeedStars.Core.Race;
using UnityEngine;
using UnityEngine.TestTools;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// A press before GO on the real Race scene: the game slows down and the runner falls under FAIL, then everybody is back on the
    /// line at normal speed and the countdown starts over; after GO a press moves the runner. The session and the runner are reached
    /// by reflection (this assembly cannot reference the Bridge).
    /// </summary>
    public sealed class FalseStartSmokeTests
    {
        [UnityTest]
        public IEnumerator PressBeforeGo_IsAFalseStart_AndOnlyGoOpensTheRace()
        {
            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            try
            {
                yield return LoadScene(RaceScene);

                MonoBehaviour session = null;
                IRaceStartSignal signal = null;
                foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
                    if (behaviour is IRaceStartSignal found) { session = behaviour; signal = found; break; }
                Assert.IsNotNull(signal, "No IRaceStartSignal in the Race scene.");

                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Ready || signal.StartPhase == RaceStartPhase.Set, "the countdown");
                InjectPress(session);
                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.FalseStart, "FAIL");
                Assert.Less(Time.timeScale, 1f, "the fall plays in slow motion");
                var state = (IRaceSession)session;
                yield return null;
                yield return null;
                Assert.AreEqual(RaceSessionState.Ready, state.State, "a false start never counts as the start of the race");

                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Ready, "the countdown to start over");
                Assert.AreEqual(1f, Time.timeScale, 1e-3f, "normal speed is back with the countdown");
                Assert.AreEqual(0f, DistanceOf(session), 1e-3f, "everybody is back on the line");
                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "GO");
                Assert.AreEqual(0f, DistanceOf(session), 1e-3f, "nothing moves the runner before a press after GO");

                InjectPress(session);
                yield return WaitUntil(() => DistanceOf(session) > 0f, "the runner to move after GO");
            }
            finally
            {
                saveFiles.Restore();
            }
        }

        private static void InjectPress(MonoBehaviour session)
        {
            object body = session.GetType().GetProperty("PlayerBody").GetValue(session);
            object input = body.GetType().GetField("input").GetValue(body);
            input.GetType().GetMethod("Inject").Invoke(input, new object[] { 0, true, Time.timeAsDouble });
        }

        private static float DistanceOf(MonoBehaviour session) => (float)session.GetType().GetProperty("Distance").GetValue(session);
    }
}
