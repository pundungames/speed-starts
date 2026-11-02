using NUnit.Framework;
using SpeedStars.Core.Menu;

namespace SpeedStars.Tests.Menu
{
    /// <summary>Menu spin maths: pixels scale with screen width, the coast is frame-rate independent and ends at rest.</summary>
    public sealed class SpinMathTests
    {
        private const float PerScreen = 300f;
        private const float Decay = 3f;
        private const float Stop = 2f;

        private static SpinMotion Make() => new SpinMotion(PerScreen, Decay, Stop, 0f);

        [Test]
        public void SameFractionOfScreenGivesSameDegrees()
        {
            float narrow = SpinMath(100f, 1000f);
            float wide = SpinMath(200f, 2000f);
            Assert.AreEqual(narrow, wide, 1e-4f);
            Assert.AreEqual(2f * narrow, SpinMath(200f, 1000f), 1e-4f);
        }

        private static float SpinMath(float px, float width) => SpinMotion.PixelsToDegrees(px, width, PerScreen);

        [Test]
        public void ZeroScreenWidthGivesNoTurn() => Assert.AreEqual(0f, SpinMotion.PixelsToDegrees(50f, 0f, PerScreen));

        private static float CoastTravel(float dt)
        {
            var m = Make();
            m.BeginDrag();
            m.Drag(500f, 1000f, 0.1f); // fast flick
            float start = m.Yaw;
            m.EndDrag();
            for (float t = 0f; t < 10f; t += dt) m.Step(dt);
            return m.Yaw - start;
        }

        [Test]
        public void CoastDistanceDoesNotDependOnFrameRate()
        {
            float at60 = CoastTravel(1f / 60f);
            float at20 = CoastTravel(1f / 20f);
            Assert.Greater(at60, 0f);
            Assert.AreEqual(at60, at20, at60 * 0.02f);
        }

        [Test]
        public void CoastEndsAtRestAndAngleStays()
        {
            var m = Make();
            m.BeginDrag();
            m.Drag(300f, 1000f, 0.1f);
            m.EndDrag();
            for (int i = 0; i < 1000; i++) m.Step(1f / 60f);
            Assert.AreEqual(0f, m.Velocity);
            float yaw = m.Yaw;
            m.Step(1f);
            Assert.AreEqual(yaw, m.Yaw);
        }

        [Test]
        public void DraggingDoesNotCoastAndTouchStopsCoast()
        {
            var m = Make();
            m.BeginDrag();
            m.Drag(300f, 1000f, 0.1f);
            float held = m.Yaw;
            m.Step(0.5f);
            Assert.AreEqual(held, m.Yaw, "no coast while held");
            m.EndDrag();
            m.BeginDrag();
            Assert.AreEqual(0f, m.Velocity);
        }

        [Test]
        public void DragDirectionFollowsFingerAndFlickKeepsSign()
        {
            var m = Make();
            m.BeginDrag();
            m.Drag(-200f, 1000f, 0.05f);
            Assert.Less(m.Yaw, 0f);
            m.EndDrag();
            float before = m.Yaw;
            m.Step(0.1f);
            Assert.Less(m.Yaw, before);
        }
    }
}
