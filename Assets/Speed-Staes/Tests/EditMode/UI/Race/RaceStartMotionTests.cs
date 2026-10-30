using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>
    /// The READY / SET / GO! motion (RaceStartMotion) against the config's own numbers: relations only (above rest, squashed, growing,
    /// faded), never a fixed value. Pure: no scene, no DOTween.
    /// </summary>
    public sealed class RaceStartMotionTests
    {
        private RaceStartConfig _config;
        private RaceStartMotion _motion;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<RaceStartConfig>();
            _motion = new RaceStartMotion(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Idle_ShowsNothing()
        {
            Assert.IsFalse(_motion.Sample(RaceStartPhase.Idle, 0f).Visible);
            Assert.IsTrue(_motion.IsFinished(RaceStartPhase.Idle, 0f));
        }

        [Test]
        public void Ready_StartsAboveItsPlace_AndLandsThere()
        {
            RaceStartPose first = _motion.Sample(RaceStartPhase.Ready, 0f);
            Assert.IsTrue(first.Visible);
            Assert.Greater(first.TextY, 0f, "READY comes in from above");
            Assert.AreEqual(0f, first.ImpactAlpha, "no impact lines before it lands");

            RaceStartPose landed = _motion.Sample(RaceStartPhase.Ready, _config.ReadyEnterSeconds);
            Assert.AreEqual(0f, landed.TextY, 0.001f);
            Assert.Less(landed.TextScale.y, 1f, "squashed at the impact");
            Assert.Greater(landed.TextScale.x, 1f);
            Assert.AreEqual(1f, landed.ImpactAlpha, 0.001f);
        }

        [Test]
        public void Ready_AfterTheRebound_RestsAndTheLinesAreGone()
        {
            RaceStartPose rest = _motion.Sample(RaceStartPhase.Ready, _config.ReadyEnterSeconds + _config.ReboundSeconds + 0.5f);

            Assert.IsTrue(rest.Visible, "READY stays until SET");
            Assert.AreEqual(0f, rest.TextY, 0.001f);
            Assert.AreEqual(1f, rest.TextScale.x, 0.001f);
            Assert.AreEqual(1f, rest.TextScale.y, 0.001f);
            Assert.AreEqual(0f, rest.ImpactAlpha);
            Assert.AreEqual(1f, rest.TextAlpha);
        }

        [Test]
        public void Ready_ReboundHopsUp()
        {
            float mid = _config.ReadyEnterSeconds + _config.ReboundSeconds * 0.5f;
            Assert.Greater(_motion.Sample(RaceStartPhase.Ready, mid).TextY, 0f);
        }

        [Test]
        public void Set_TensionFadesIn_ThenPulsesBetweenItsLimits()
        {
            Assert.AreEqual(0f, _motion.Sample(RaceStartPhase.Set, 0f).TensionAlpha, 0.001f);
            Assert.AreEqual(1f, _motion.Sample(RaceStartPhase.Set, _config.SetCompressSeconds).TensionAlpha, 0.001f);

            for (float t = _config.SetCompressSeconds; t < 5f; t += 0.013f)
            {
                float alpha = _motion.Sample(RaceStartPhase.Set, t).TensionAlpha;
                Assert.GreaterOrEqual(alpha, _config.HoldTensionMinAlpha - 0.001f, $"t={t}");
                Assert.LessOrEqual(alpha, 1.001f, $"t={t}");
            }
        }

        [Test]
        public void Set_SqueezesTheWord_AndTheShakeStaysTiny()
        {
            RaceStartPose squeezed = _motion.Sample(RaceStartPhase.Set, _config.SetCompressSeconds);
            Assert.AreEqual(_config.SetScale.x, squeezed.TextScale.x, 0.001f);
            Assert.AreEqual(_config.SetScale.y, squeezed.TextScale.y, 0.001f);
            Assert.AreEqual(0f, squeezed.TextY, "SET sits at the word's place");

            for (float t = 0f; t < 5f; t += 0.007f)
                Assert.LessOrEqual(Mathf.Abs(_motion.Sample(RaceStartPhase.Set, t).TextX), _config.HoldShakeAmplitude + 0.001f, $"t={t}");
        }

        [Test]
        public void Set_Waits_NeverEndsOnItsOwn()
        {
            Assert.IsFalse(_motion.IsFinished(RaceStartPhase.Set, 1000f), "SET holds until the start signal, whatever the time");
            Assert.IsTrue(_motion.Sample(RaceStartPhase.Set, 1000f).Visible);
        }

        [Test]
        public void Go_GrowsAndFades_AndEndsAtItsDuration()
        {
            RaceStartPose first = _motion.Sample(RaceStartPhase.Go, 0f);
            Assert.AreEqual(_config.GoScaleFrom, first.TextScale.x, 0.001f);
            Assert.AreEqual(1f, first.TextAlpha);
            Assert.AreEqual(1f, first.ReleaseAlpha);
            Assert.AreEqual(1f, first.EffectsScale);

            RaceStartPose inside = _motion.Sample(RaceStartPhase.Go, _config.GoSeconds * 0.99f);
            Assert.Greater(inside.TextScale.x, first.TextScale.x);
            Assert.Greater(inside.EffectsScale, 1f);
            Assert.Less(inside.TextAlpha, 0.2f);
            Assert.Less(inside.ReleaseAlpha, 0.2f);

            Assert.IsFalse(_motion.IsFinished(RaceStartPhase.Go, _config.GoSeconds * 0.99f));
            Assert.IsTrue(_motion.IsFinished(RaceStartPhase.Go, _config.GoSeconds));
            Assert.IsFalse(_motion.Sample(RaceStartPhase.Go, _config.GoSeconds).Visible, "the whole group closes after the burst");
        }

        [Test]
        public void Go_StaysOpaqueFirst_ThenFades()
        {
            float opaque = Mathf.Min(_config.GoOpaqueSeconds, _config.GoSeconds);
            Assert.AreEqual(1f, _motion.Sample(RaceStartPhase.Go, opaque * 0.99f).TextAlpha, 0.001f);

            float after = _motion.Sample(RaceStartPhase.Go, opaque + (_config.GoSeconds - opaque) * 0.5f).TextAlpha;
            Assert.Greater(after, 0f);
            Assert.Less(after, 1f);
        }

        [Test]
        public void SamePhaseSameTime_SameLook_NoHiddenState()
        {
            RaceStartPose a = _motion.Sample(RaceStartPhase.Set, 0.37f);
            _motion.Sample(RaceStartPhase.Go, 0.1f); // an unrelated phase in between: a restarted or early phase leaves nothing behind
            RaceStartPose b = _motion.Sample(RaceStartPhase.Set, 0.37f);

            Assert.AreEqual(a.TextX, b.TextX);
            Assert.AreEqual(a.TextScale, b.TextScale);
            Assert.AreEqual(a.TensionAlpha, b.TensionAlpha);
        }

        [Test]
        public void ReducedMotion_KeepsWordsAndTiming_DropsSlideSquashShakeAndLines()
        {
            var so = new SerializedObject(_config);
            so.FindProperty("reduceMotion").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            RaceStartPose ready = _motion.Sample(RaceStartPhase.Ready, 0f);
            Assert.IsTrue(ready.Visible);
            Assert.AreEqual(0f, ready.TextY);
            Assert.AreEqual(Vector2.one, ready.TextScale);
            Assert.AreEqual(0f, ready.TextAlpha, 0.001f, "a short fade-in instead");
            Assert.AreEqual(1f, _motion.Sample(RaceStartPhase.Ready, _config.ReducedFadeSeconds).TextAlpha, 0.001f);

            RaceStartPose set = _motion.Sample(RaceStartPhase.Set, 1f);
            Assert.AreEqual(0f, set.TextX);
            Assert.AreEqual(0f, set.TensionAlpha);
            Assert.AreEqual(Vector2.one, set.TextScale);

            RaceStartPose go = _motion.Sample(RaceStartPhase.Go, 0.05f);
            Assert.IsTrue(go.Visible, "GO! still shows at the start moment");
            Assert.AreEqual(Vector2.one, go.TextScale);
            Assert.AreEqual(0f, go.ReleaseAlpha);
            Assert.IsTrue(_motion.IsFinished(RaceStartPhase.Go, _config.GoSeconds), "same length as before");
        }

        [Test]
        public void Backdrop_ShowsOnlyWhileTheCountdownShows()
        {
            Assert.IsFalse(_motion.BackdropVisible(RaceStartPhase.Idle, 0f), "no countdown, no dim or banner");
            Assert.IsTrue(_motion.BackdropVisible(RaceStartPhase.Ready, 0f));
            Assert.IsTrue(_motion.BackdropVisible(RaceStartPhase.Ready, 5f), "READY stays until SET");
            Assert.IsTrue(_motion.BackdropVisible(RaceStartPhase.Set, 3f), "SET waits a random time");
            Assert.IsTrue(_motion.BackdropVisible(RaceStartPhase.Go, 0f));
            Assert.IsTrue(_motion.BackdropVisible(RaceStartPhase.Go, _config.GoSeconds * 0.5f));
            Assert.IsFalse(_motion.BackdropVisible(RaceStartPhase.Go, _config.GoSeconds), "closes with the GO! burst");
        }

        [Test]
        public void Config_BannerNumbers_AreUsable()
        {
            Assert.Greater(_config.BannerScale, 0f);
            Assert.Greater(_config.BannerWordFontSize, 0f);
            Assert.Greater(_config.DimColor.a, 0f, "the dim dims");
            Assert.Less(_config.DimColor.a, 1f, "the dim is see-through");
            Assert.IsFalse(_config.ReadySetOutline, "navy words on cream carry no outline by default");
        }
    }
}
