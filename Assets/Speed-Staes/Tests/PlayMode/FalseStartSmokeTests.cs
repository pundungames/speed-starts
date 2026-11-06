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

        [UnityTest]
        public IEnumerator BlockStance_FollowsTheCountdown_ForThePlayerAndABot()
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

                FindDrivers(session, out MonoBehaviour player, out MonoBehaviour bot);
                Assert.IsNotNull(player, "No RunnerPoseDriver under the player body.");
                // The bots may not exist in every setup (or yet): their half is skipped, the player's is the test.
                if (bot == null) Debug.Log("FalseStartSmokeTests: no bot driver found, bot stance not checked.");

                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Ready, "READY");
                yield return WaitUntil(() => StanceOf(player) == "Marks", "the player on the blocks (Marks)");
                if (bot != null) Assert.AreEqual("Marks", StanceOf(bot), "the bot takes the same stance at READY");

                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Set, "SET");
                yield return null;
                Assert.AreEqual("Set", StanceOf(player), "SET puts the hips up");
                if (bot != null) Assert.AreEqual("Set", StanceOf(bot), "the bot takes the same stance at SET");

                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "GO");
                for (int i = 0; i < 5; i++) yield return null;
                Assert.AreEqual("Set", StanceOf(player), "GO leaves the stance to the first press");

                InjectPress(session);
                yield return WaitUntil(() => StanceOf(player) == "None", "the first press to end the stance");
            }
            finally
            {
                saveFiles.Restore();
            }
        }

        [UnityTest]
        public IEnumerator BlockStance_FalseStartEndsIt_AndTheNextReadyRestoresIt()
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

                FindDrivers(session, out MonoBehaviour player, out MonoBehaviour bot);
                Assert.IsNotNull(player, "No RunnerPoseDriver under the player body.");

                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Set, "SET");
                InjectPress(session);
                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.FalseStart, "FAIL");
                yield return null;
                Assert.AreEqual("None", StanceOf(player), "a false start takes the runner off the blocks");
                if (bot != null) Assert.AreEqual("None", StanceOf(bot), "and the bot too");

                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Ready, "the countdown to start over");
                yield return null;
                Assert.AreEqual("Marks", StanceOf(player), "READY puts the runner back on the blocks");
                if (bot != null) Assert.AreEqual("Marks", StanceOf(bot), "and the bot too");
            }
            finally
            {
                saveFiles.Restore();
            }
        }

        /// <summary>The player's driver is the one under the session's PlayerBody; a bot's is any other (RunnerPoseDriver lives in Assembly-CSharp).</summary>
        private static void FindDrivers(MonoBehaviour session, out MonoBehaviour player, out MonoBehaviour bot)
        {
            player = null;
            bot = null;
            var playerBody = (Component)session.GetType().GetProperty("PlayerBody").GetValue(session);
            foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
            {
                if (behaviour.GetType().Name != "RunnerPoseDriver") continue;
                if (playerBody != null && behaviour.transform.IsChildOf(playerBody.transform)) player = behaviour;
                else if (bot == null) bot = behaviour;
            }
        }

        private static string StanceOf(MonoBehaviour driver) => driver.GetType().GetField("startStance").GetValue(driver).ToString();

        private static void InjectPress(MonoBehaviour session)
        {
            object body = session.GetType().GetProperty("PlayerBody").GetValue(session);
            object input = body.GetType().GetField("input").GetValue(body);
            input.GetType().GetMethod("Inject").Invoke(input, new object[] { 0, true, Time.timeAsDouble });
        }

        private static float DistanceOf(MonoBehaviour session) => (float)session.GetType().GetProperty("Distance").GetValue(session);
    }
}
