using NUnit.Framework;
using SpeedStars.Meta.Emotes;

namespace SpeedStars.Tests.Meta.Emotes
{
    /// <summary>The pure maths of the baked emotes (clock, fade, frame lookup); every number here is a test input.</summary>
    public sealed class EmotePlaybackTests
    {
        private const float Tolerance = 0.001f;

        // ---- frame lookup ----

        [Test]
        public void Frame_PicksTheTwoFramesAroundTheTime_AndTheBlend()
        {
            EmoteSampler.Frame(0.5f, 10f, 20, out int a, out int b, out float blend);
            Assert.AreEqual(5, a);
            Assert.AreEqual(6, b);
            Assert.AreEqual(0f, blend, Tolerance);

            EmoteSampler.Frame(0.55f, 10f, 20, out a, out b, out blend);
            Assert.AreEqual(5, a);
            Assert.AreEqual(6, b);
            Assert.AreEqual(0.5f, blend, Tolerance);
        }

        [Test]
        public void Frame_HoldsTheEnds_AndSurvivesAnEmptyClip()
        {
            EmoteSampler.Frame(-3f, 10f, 20, out int a, out int b, out float blend);
            Assert.AreEqual(0, a);
            Assert.AreEqual(0f, blend, Tolerance);

            EmoteSampler.Frame(99f, 10f, 20, out a, out b, out blend);
            Assert.AreEqual(19, a);
            Assert.AreEqual(19, b);
            Assert.AreEqual(0f, blend, Tolerance);

            EmoteSampler.Frame(1f, 10f, 0, out a, out b, out blend);
            Assert.AreEqual(0, a);
            Assert.AreEqual(0, b);
            Assert.AreEqual(0f, blend, Tolerance);
        }

        // ---- weight envelope ----

        [Test]
        public void Weight_RisesFromTheCarry_AndFallsToZeroAtTheEnd()
        {
            Assert.AreEqual(0f, EmoteSampler.Weight(0f, 2f, 0.5f, 0.5f, 0f), Tolerance);
            Assert.AreEqual(0.4f, EmoteSampler.Weight(0f, 2f, 0.5f, 0.5f, 0.4f), Tolerance, "a replaced emote hands its weight over");
            Assert.AreEqual(1f, EmoteSampler.Weight(1f, 2f, 0.5f, 0.5f, 0f), Tolerance);
            Assert.AreEqual(0f, EmoteSampler.Weight(2f, 2f, 0.5f, 0.5f, 0f), Tolerance);
            float mid = EmoteSampler.Weight(1.75f, 2f, 0.5f, 0.5f, 0f);
            Assert.Greater(mid, 0f);
            Assert.Less(mid, 1f);
        }

        // ---- playback clock ----

        [Test]
        public void OneShot_EndsByItself_WithTheFadeOut()
        {
            var playback = new EmotePlayback();
            playback.Play(2f, 0.5f, 0.5f, loop: false);
            Assert.IsTrue(playback.IsPlaying);

            playback.Tick(1f);
            Assert.AreEqual(1f, playback.Weight, Tolerance);
            Assert.AreEqual(1f, playback.ClipTime, Tolerance);

            playback.Tick(0.9f);
            Assert.IsTrue(playback.IsPlaying);
            Assert.Less(playback.Weight, 1f, "fading out");

            playback.Tick(0.2f);
            Assert.IsFalse(playback.IsPlaying);
            Assert.AreEqual(0f, playback.Weight, Tolerance);
        }

        [Test]
        public void Loop_WrapsTheClipTime_AndNeverEndsOnItsOwn()
        {
            var playback = new EmotePlayback();
            playback.Play(2f, 0.2f, 0.2f, loop: true);
            playback.Tick(1.5f);
            playback.Tick(1.0f);
            Assert.IsTrue(playback.IsPlaying);
            Assert.AreEqual(0.5f, playback.ClipTime, Tolerance, "2.5 s into a 2 s loop");
            Assert.AreEqual(1f, playback.Weight, Tolerance);
        }

        [Test]
        public void Release_FadesALoopOutFromTheCurrentWeight_ThenStops()
        {
            var playback = new EmotePlayback();
            playback.Play(2f, 0.2f, 0.4f, loop: true);
            playback.Tick(1f);
            playback.Release();
            Assert.IsTrue(playback.IsPlaying, "still fading");

            playback.Tick(0.2f);
            Assert.Greater(playback.Weight, 0f);
            Assert.Less(playback.Weight, 1f);

            playback.Tick(0.3f);
            Assert.IsFalse(playback.IsPlaying);
            Assert.AreEqual(0f, playback.Weight, Tolerance);
        }

        [Test]
        public void Release_WithoutFadeOut_StopsAtOnce_AndWithoutAnEmoteDoesNothing()
        {
            var playback = new EmotePlayback();
            playback.Release();
            Assert.IsFalse(playback.IsPlaying);

            playback.Play(2f, 0.2f, 0f, loop: true);
            playback.Release();
            Assert.IsFalse(playback.IsPlaying);
        }

        [Test]
        public void PlayingANewEmote_StartsFromTheWeightOfTheOldOne()
        {
            var playback = new EmotePlayback();
            playback.Play(2f, 0.2f, 0.2f, loop: true);
            playback.Tick(1f);
            Assert.AreEqual(1f, playback.Weight, Tolerance);

            playback.Play(3f, 0.5f, 0.2f, loop: true);
            Assert.AreEqual(1f, playback.Weight, Tolerance, "no snap down to zero");
            Assert.AreEqual(0f, playback.Time, Tolerance);
        }

        [Test]
        public void PlayingAnEmptyClip_Stops()
        {
            var playback = new EmotePlayback();
            playback.Play(2f, 0.2f, 0.2f, loop: true);
            playback.Play(0f, 0.2f, 0.2f, loop: true);
            Assert.IsFalse(playback.IsPlaying);
            Assert.AreEqual(0f, playback.Weight, Tolerance);
        }
    }
}
