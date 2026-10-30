using NUnit.Framework;
using SpeedStars.Meta.Emotes;
using UnityEngine;

namespace SpeedStars.Tests.Meta.Emotes
{
    /// <summary>The pure maths of the procedural emotes; every number here is a test input.</summary>
    public sealed class EmotePlaybackTests
    {
        private const float Tolerance = 0.001f;

        private static EmoteKey K(float t, EmoteBone bone, float z, EmoteEase ease = EmoteEase.Linear) =>
            new EmoteKey(t, bone, new Vector3(0f, 0f, z), ease);

        // Duration 2, fade in 0.5, fade out 0.5; the arm goes 0 -> 100 between t=0.5 and t=1.5.
        private static EmoteDefinition Emote(float duration = 2f, float blendIn = 0.5f, float blendOut = 0.5f)
        {
            string json = "{\"emoteId\":\"t\",\"displayName\":\"T\",\"cashPrice\":0,\"duration\":" + duration.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ",\"blendIn\":" + blendIn.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ",\"blendOut\":" + blendOut.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ",\"keys\":[{\"time\":0.5,\"bone\":9,\"euler\":{\"x\":0,\"y\":0,\"z\":0},\"ease\":0},"
                + "{\"time\":1.5,\"bone\":9,\"euler\":{\"x\":0,\"y\":0,\"z\":100},\"ease\":0}]}";
            return JsonUtility.FromJson<EmoteDefinition>(json);
        }

        [Test]
        public void Evaluate_InterpolatesBetweenKeys_AndHoldsOutside()
        {
            EmoteKey[] keys = { K(1f, EmoteBone.Head, 10f), K(3f, EmoteBone.Head, 50f) };
            Assert.AreEqual(10f, EmoteSampler.Evaluate(keys, EmoteBone.Head, 0f).z, Tolerance, "before the first key it holds the first");
            Assert.AreEqual(30f, EmoteSampler.Evaluate(keys, EmoteBone.Head, 2f).z, Tolerance);
            Assert.AreEqual(50f, EmoteSampler.Evaluate(keys, EmoteBone.Head, 9f).z, Tolerance, "after the last key it holds the last");
        }

        [Test]
        public void Evaluate_IsIndependentOfKeyOrder_AndOfOtherBones()
        {
            EmoteKey[] keys = { K(3f, EmoteBone.Head, 50f), K(1f, EmoteBone.Head, 10f), K(2f, EmoteBone.Chest, 99f) };
            Assert.AreEqual(30f, EmoteSampler.Evaluate(keys, EmoteBone.Head, 2f).z, Tolerance);
            Assert.AreEqual(Vector3.zero, EmoteSampler.Evaluate(keys, EmoteBone.HandL, 2f), "a bone without keys is not moved");
            Assert.AreEqual(Vector3.zero, EmoteSampler.Evaluate(null, EmoteBone.Head, 2f));
        }

        [Test]
        public void Ease_EndsMeetTheLine_AndShapesTheMiddle()
        {
            foreach (EmoteEase ease in System.Enum.GetValues(typeof(EmoteEase)))
            {
                Assert.AreEqual(0f, EmoteSampler.Ease(ease, 0f), Tolerance, ease.ToString());
                Assert.AreEqual(1f, EmoteSampler.Ease(ease, 1f), Tolerance, ease.ToString());
            }
            Assert.Greater(EmoteSampler.Ease(EmoteEase.EaseOut, 0.5f), 0.5f, "ease-out is ahead of linear at the middle");
            Assert.Less(EmoteSampler.Ease(EmoteEase.EaseIn, 0.5f), 0.5f, "ease-in is behind linear at the middle");
        }

        [Test]
        public void Weight_FadesInAndOut_AndIsZeroAtTheEnd()
        {
            Assert.AreEqual(0f, EmoteSampler.Weight(0f, 2f, 0.5f, 0.5f, 0f), Tolerance);
            Assert.AreEqual(1f, EmoteSampler.Weight(1f, 2f, 0.5f, 0.5f, 0f), Tolerance);
            Assert.Greater(EmoteSampler.Weight(0.25f, 2f, 0.5f, 0.5f, 0f), 0f);
            Assert.Less(EmoteSampler.Weight(0.25f, 2f, 0.5f, 0.5f, 0f), 1f);
            Assert.Less(EmoteSampler.Weight(1.9f, 2f, 0.5f, 0.5f, 0f), EmoteSampler.Weight(1.6f, 2f, 0.5f, 0.5f, 0f), "fading out");
            Assert.AreEqual(0f, EmoteSampler.Weight(2f, 2f, 0.5f, 0.5f, 1f), Tolerance);
        }

        [Test]
        public void Weight_StartsFromTheCarriedWeight()
        {
            float fresh = EmoteSampler.Weight(0f, 2f, 0.5f, 0.5f, 0f);
            float carried = EmoteSampler.Weight(0f, 2f, 0.5f, 0.5f, 0.8f);
            Assert.Greater(carried, fresh, "a replaced emote hands its weight over, the pose does not snap to rest");
        }

        [Test]
        public void Playback_RunsToTheEnd_ThenStopsAtZero()
        {
            var playback = new EmotePlayback();
            Assert.IsFalse(playback.IsPlaying);
            Assert.AreEqual(Vector3.zero, playback.Euler(EmoteBone.UpperArmR));

            playback.Play(Emote());
            Assert.IsTrue(playback.IsPlaying);

            playback.Tick(1.0f);
            Assert.AreEqual(50f, playback.Euler(EmoteBone.UpperArmR).z, Tolerance);
            Assert.AreEqual(1f, playback.Weight, Tolerance);

            playback.Tick(1.5f);
            Assert.IsFalse(playback.IsPlaying);
            Assert.AreEqual(0f, playback.Weight, Tolerance);
            Assert.AreEqual(Vector3.zero, playback.Euler(EmoteBone.UpperArmR));
        }

        [Test]
        public void Playback_ReplayRestartsTheClock_AndStopIsImmediate()
        {
            var playback = new EmotePlayback();
            playback.Play(Emote());
            playback.Tick(1.0f);
            float weight = playback.Weight;

            playback.Play(Emote());
            Assert.AreEqual(0f, playback.Time, Tolerance);
            Assert.GreaterOrEqual(playback.Weight, 0f);
            Assert.LessOrEqual(playback.Weight, weight);

            playback.Stop();
            Assert.IsFalse(playback.IsPlaying);
            Assert.AreEqual(0f, playback.Weight, Tolerance);
        }

        [Test]
        public void Playback_NullEmote_Stops()
        {
            var playback = new EmotePlayback();
            playback.Play(Emote());
            playback.Play(null);
            Assert.IsFalse(playback.IsPlaying);
        }
    }
}
