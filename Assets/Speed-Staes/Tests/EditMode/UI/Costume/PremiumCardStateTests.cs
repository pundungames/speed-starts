using NUnit.Framework;
using SpeedStars.Meta.Config;
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

        [Test]
        public void PriceShowsOnlyOnALockedCardThatIsForSale_WithTheIconOfItsCurrency()
        {
            foreach (bool premium in new[] { false, true })
            foreach (bool selected in new[] { false, true })
            foreach (CosmeticCurrency currency in new[] { CosmeticCurrency.Cash, CosmeticCurrency.PurpleGem })
            {
                PremiumCardLayers locked = PremiumCardLayers.Resolve(premium, selected, true, false, true, currency);
                Assert.IsTrue(locked.PriceVisible);
                Assert.AreEqual(currency == CosmeticCurrency.Cash, locked.PriceCashIcon);
                Assert.AreEqual(currency == CosmeticCurrency.PurpleGem, locked.PriceGemIcon);

                PremiumCardLayers owned = PremiumCardLayers.Resolve(premium, selected, false, false, true, currency);
                Assert.IsFalse(owned.PriceVisible || owned.PriceCashIcon || owned.PriceGemIcon, "an owned card shows no price");

                PremiumCardLayers free = PremiumCardLayers.Resolve(premium, selected, true, false, false, currency);
                Assert.IsFalse(free.PriceVisible || free.PriceCashIcon || free.PriceGemIcon, "a card with no price shows none");
            }
        }

        [Test]
        public void ThePriceNeverChangesTheOtherLayers()
        {
            foreach (bool premium in new[] { false, true })
            foreach (bool selected in new[] { false, true })
            foreach (bool locked in new[] { false, true })
            {
                PremiumCardLayers without = PremiumCardLayers.Resolve(premium, selected, locked, false);
                PremiumCardLayers with = PremiumCardLayers.Resolve(premium, selected, locked, false, true, CosmeticCurrency.Cash);
                Assert.AreEqual(without.NormalLock, with.NormalLock);
                Assert.AreEqual(without.PremiumLock, with.PremiumLock);
                Assert.AreEqual(without.PremiumCheck, with.PremiumCheck);
                Assert.AreEqual(without.NormalCheck, with.NormalCheck);
            }
        }
    }
}
