using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Audio;
using SpeedStars.Core.Race;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// The race sounds of T-120 on the real Race scene, heard through a spy that replaces ISfx in the ProjectContext (the scene's
    /// views and the step listener inject it on load): READY / SET / GO with the pistol and the crowd, a step per press, the finish
    /// sound and the crowd at the line, and the false start with its own sound and no step. The session and the runner are reached
    /// by reflection (this assembly cannot reference the Bridge). No clip, loudness or length is asserted.
    /// </summary>
    public sealed class RaceSoundsPlayTests
    {
        private sealed class SpySfx : ISfx
        {
            public readonly List<SfxId> Played = new List<SfxId>();

            public void Play(SfxId id) => Played.Add(id);

            public int Count(SfxId id)
            {
                int n = 0;
                foreach (SfxId played in Played) if (played == id) n++;
                return n;
            }
        }

        [UnityTest]
        public IEnumerator Race_PlaysCountdownGoStepsAndFinishSounds()
        {
            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            DiContainer container = ProjectContext.Instance.Container;
            ISfx original = container.Resolve<ISfx>();
            var spy = new SpySfx();
            container.Rebind<ISfx>().FromInstance(spy);
            try
            {
                yield return LoadScene(RaceScene);
                MonoBehaviour session = FindSession(out IRaceStartSignal signal);

                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Go, "GO");
                Assert.GreaterOrEqual(spy.Count(SfxId.CountdownReady), 1, "READY beep");
                Assert.GreaterOrEqual(spy.Count(SfxId.CountdownSet), 1, "SET beep");
                int goBeeps = spy.Count(SfxId.CountdownGo);
                Assert.GreaterOrEqual(goBeeps, 1, "GO beep");
                Assert.AreEqual(goBeeps, spy.Count(SfxId.StartGun), "the pistol comes with every GO");
                Assert.AreEqual(goBeeps, spy.Count(SfxId.CrowdStart), "the crowd comes with every GO");
                Assert.AreEqual(0, spy.Count(SfxId.StepLeft) + spy.Count(SfxId.StepRight), "no step sound before the first press");

                InjectPress(session, 0, true);
                yield return WaitUntil(() => spy.Count(SfxId.StepLeft) == 1, "the left step sound");
                InjectPress(session, 0, false);
                InjectPress(session, 1, true);
                yield return WaitUntil(() => spy.Count(SfxId.StepRight) == 1, "the right step sound");
                yield return null;
                Assert.AreEqual(1, spy.Count(SfxId.StepLeft), "a release is not a step");

                System.Reflection.MethodInfo finish = session.GetType().GetMethod("DevForceFinish");
                if (finish == null) Assert.Ignore("RaceSessionAdapter.DevForceFinish is not compiled in (non-dev build): no way to finish the race from a test.");
                finish.Invoke(session, null);
                yield return WaitUntil(() => spy.Count(SfxId.CrowdEnd) == 1, "the crowd at the line");
                Assert.AreEqual(1, spy.Count(SfxId.FinishFirst) + spy.Count(SfxId.FinishRace), "exactly one finish sound");

                int stepsAtLine = spy.Count(SfxId.StepLeft) + spy.Count(SfxId.StepRight);
                for (int i = 0; i < 20; i++) yield return null;
                Assert.AreEqual(stepsAtLine, spy.Count(SfxId.StepLeft) + spy.Count(SfxId.StepRight), "the steps after the line are silent");
            }
            finally
            {
                container.Rebind<ISfx>().FromInstance(original);
                saveFiles.Restore();
            }
        }

        [UnityTest]
        public IEnumerator FalseStart_PlaysItsOwnSound_AndNoStep()
        {
            SaveFolderSnapshot saveFiles = SaveFolderSnapshot.Take();
            DiContainer container = ProjectContext.Instance.Container;
            ISfx original = container.Resolve<ISfx>();
            var spy = new SpySfx();
            container.Rebind<ISfx>().FromInstance(spy);
            try
            {
                yield return LoadScene(RaceScene);
                MonoBehaviour session = FindSession(out IRaceStartSignal signal);

                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.Ready || signal.StartPhase == RaceStartPhase.Set, "the countdown");
                InjectPress(session, 0, true);
                yield return WaitUntil(() => signal.StartPhase == RaceStartPhase.FalseStart, "FAIL");
                for (int i = 0; i < 10; i++) yield return null; // the fall presses both feet from the gate
                Assert.AreEqual(1, spy.Count(SfxId.FalseStart), "the false start sound");
                Assert.AreEqual(0, spy.Count(SfxId.Denied), "the old Denied sound is not used for it");
                Assert.AreEqual(0, spy.Count(SfxId.StepLeft) + spy.Count(SfxId.StepRight), "a false start and its fall make no step sound");
            }
            finally
            {
                container.Rebind<ISfx>().FromInstance(original);
                saveFiles.Restore();
            }
        }

        private static MonoBehaviour FindSession(out IRaceStartSignal signal)
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
            {
                if (!(behaviour is IRaceStartSignal found)) continue;
                signal = found;
                return behaviour;
            }
            Assert.Fail("No IRaceStartSignal in the Race scene.");
            signal = null;
            return null;
        }

        private static void InjectPress(MonoBehaviour session, int leg, bool pressed)
        {
            object body = session.GetType().GetProperty("PlayerBody").GetValue(session);
            object input = body.GetType().GetField("input").GetValue(body);
            input.GetType().GetMethod("Inject").Invoke(input, new object[] { leg, pressed, Time.timeAsDouble });
        }
    }
}
