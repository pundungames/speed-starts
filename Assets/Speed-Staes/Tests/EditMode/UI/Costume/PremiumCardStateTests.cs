using NUnit.Framework;
using SpeedStars.UI.Costume;

namespace SpeedStars.Tests.UI.Costume
{
    public sealed class PremiumCardStateTests
    {
        [Test]
        public void NormalCardKeepsTheOldLayers()
        {
            PremiumCardLayers plain = PremiumCardLayers.Resolve(false, false, false, false);
            Assert.IsTrue(plain.NormalBackground);
            Assert.IsFalse(plain.NormalSelectedBackground || plain.NormalCheck || plain.NormalLock);
            Assert.IsFalse(plain.PremiumBackground || plain.PremiumFrame);

            PremiumCardLayers worn = PremiumCardLayers.Resolve(false, true, false, false);
            Assert.IsTrue(worn.NormalSelectedBackground && worn.NormalCheck);
            Assert.IsFalse(worn.NormalBackground);

            Assert.IsTrue(PremiumCardLayers.Resolve(false, false, true, false).NormalLock);
        }

        [Test]
        public void PremiumCardNeverShowsNormalLayers()
        {
            foreach (bool selected in new[] { false, true })
            foreach (bool locked in new[] { false, true })
            {
                PremiumCardLayers layers = PremiumCardLayers.Resolve(true, selected, locked, false);
                Assert.IsFalse(layers.NormalBackground || layers.NormalSelectedBackground || layers.NormalCheck || layers.NormalLock);
                Assert.IsTrue(layers.PremiumBackground);
                Assert.IsTrue(layers.PremiumFrame || layers.PremiumSelectedFrame);
            }
        }

        [Test]
        public void PremiumShowsExactlyOneFrame_RedWhenSelected_GoldOtherwise()
        {
            foreach (bool selected in new[] { false, true })
            foreach (bool locked in new[] { false, true })
            foreach (bool disabled in new[] { false, true })
            {
                PremiumCardLayers layers = PremiumCardLayers.Resolve(true, selected, locked, disabled);
                Assert.IsFalse(layers.PremiumFrame && layers.PremiumSelectedFrame, "gold and red must never show together");
                Assert.AreEqual(selected, layers.PremiumSelectedFrame);
                Assert.AreEqual(!selected, layers.PremiumFrame);
            }
        }

        [Test]
        public void NormalCardNeverShowsAPremiumFrame()
        {
            foreach (bool selected in new[] { false, true })
            {
                PremiumCardLayers layers = PremiumCardLayers.Resolve(false, selected, false, false);
                Assert.IsFalse(layers.PremiumFrame || layers.PremiumSelectedFrame);
            }
        }

        [Test]
        public void LockedPremiumShowsLockOnly()
        {
            PremiumCardLayers layers = PremiumCardLayers.Resolve(true, false, true, false);
            Assert.IsTrue(layers.PremiumLock);
            Assert.IsFalse(layers.PremiumCheck || layers.PremiumSelectedFrame || layers.PremiumDisabledOverlay);
        }

        [Test]
        public void OwnedPremiumIsPlain()
        {
            PremiumCardLayers layers = PremiumCardLayers.Resolve(true, false, false, false);
            Assert.IsFalse(layers.PremiumLock || layers.PremiumCheck || layers.PremiumSelectedFrame);
        }

        [Test]
        public void SelectedOwnedPremiumGetsFrameAndCheck()
        {
            PremiumCardLayers layers = PremiumCardLayers.Resolve(true, true, false, false);
            Assert.IsTrue(layers.PremiumSelectedFrame && layers.PremiumCheck);
            Assert.IsFalse(layers.PremiumLock);
        }

        [Test]
        public void PreviewedLockedPremiumIsNotMarkedWorn()
        {
            PremiumCardLayers layers = PremiumCardLayers.Resolve(true, true, true, false);
            Assert.IsTrue(layers.PremiumSelectedFrame && layers.PremiumLock);
            Assert.IsFalse(layers.PremiumCheck);
        }

        [Test]
        public void VeilOnlyForRealDisabled()
        {
            Assert.IsFalse(PremiumCardLayers.Resolve(true, false, true, false).PremiumDisabledOverlay);
            Assert.IsTrue(PremiumCardLayers.Resolve(true, false, false, true).PremiumDisabledOverlay);
            Assert.IsFalse(PremiumCardLayers.Resolve(false, false, false, true).PremiumDisabledOverlay);
        }
    }
}
