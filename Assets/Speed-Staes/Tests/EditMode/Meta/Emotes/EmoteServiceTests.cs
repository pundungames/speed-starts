using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Economy;
using SpeedStars.Meta.Emotes;
using SpeedStars.Meta.Wallet;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;
using SpeedStars.UI.Emotes;

namespace SpeedStars.Tests.Meta.Emotes
{
    /// <summary>Prices and cash are test-chosen values; tests assert relations, never the shipped catalog.</summary>
    public sealed class EmoteServiceTests
    {
        private const int PriceB = 40;
        private const int PriceD = 90;

        private TicketTestKit _kit;

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        // Emotes in list order: a (free), b (PriceB), c (free), d (PriceD).
        private EmoteService Service(int cash)
        {
            _kit = new TicketTestKit(0);
            _kit.Progress.Cash = cash;
            EmoteCatalog catalog = _kit.Fixture.Create<EmoteCatalog>();
            ConfigFixture.Edit(catalog, so =>
            {
                var list = ConfigFixture.Find(so, "emotes");
                string[] ids = { "a", "b", "c", "d" };
                int[] prices = { 0, PriceB, 0, PriceD };
                list.arraySize = ids.Length;
                for (int i = 0; i < ids.Length; i++)
                {
                    var e = list.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("emoteId").stringValue = ids[i];
                    e.FindPropertyRelative("displayName").stringValue = ids[i].ToUpperInvariant();
                    e.FindPropertyRelative("cashPrice").intValue = prices[i];
                }
                ConfigFixture.Find(so, "defaultEmoteId").stringValue = "a";
            });
            var wallet = new WalletService(_kit.Repository, _kit.Clock);
            return new EmoteService(catalog, _kit.Repository, new EconomyLedger(_kit.Repository, wallet, _kit.Economy));
        }

        [Test]
        public void FreeEmotesAreOwned_PricedOnesAreNot()
        {
            var s = Service(0);
            Assert.IsTrue(s.IsOwned("a"));
            Assert.IsTrue(s.IsOwned("c"));
            Assert.IsFalse(s.IsOwned("b"));
            Assert.IsFalse(s.IsOwned("d"));
            Assert.IsFalse(s.IsOwned("nope"));
            Assert.AreEqual(4, s.Emotes.Count);
        }

        [Test]
        public void Purchase_SpendsThePriceOnce_AndOwnsTheEmote()
        {
            var s = Service(PriceB + 5);
            int before = _kit.Progress.Cash;

            Assert.AreEqual(EmotePurchaseResult.Purchased, s.TryPurchase("b"));
            Assert.AreEqual(PriceB, before - _kit.Progress.Cash);
            Assert.IsTrue(s.IsOwned("b"));

            int afterFirst = _kit.Progress.Cash;
            Assert.AreEqual(EmotePurchaseResult.AlreadyOwned, s.TryPurchase("b"));
            Assert.AreEqual(afterFirst, _kit.Progress.Cash, "an owned emote is never charged again");
        }

        [Test]
        public void Purchase_NotEnoughCash_ChangesNothing()
        {
            var s = Service(PriceB - 1);
            int before = _kit.Progress.Cash;
            int changed = 0;
            s.Changed += () => changed++;

            Assert.AreEqual(EmotePurchaseResult.NotEnoughCash, s.TryPurchase("b"));
            Assert.AreEqual(before, _kit.Progress.Cash);
            Assert.IsFalse(s.IsOwned("b"));
            Assert.AreEqual(0, changed);
        }

        [Test]
        public void Purchase_FreeOrUnknown_IsRefusedWithoutCharge()
        {
            var s = Service(1000);
            int before = _kit.Progress.Cash;
            Assert.AreEqual(EmotePurchaseResult.AlreadyOwned, s.TryPurchase("a"));
            Assert.AreEqual(EmotePurchaseResult.UnknownEmote, s.TryPurchase("nope"));
            Assert.AreEqual(before, _kit.Progress.Cash);
        }

        [Test]
        public void Equip_NeedsOwnership_AndSurvivesANewService()
        {
            var s = Service(PriceB);
            Assert.AreEqual("a", s.EquippedId, "default emote until the player chooses");
            Assert.IsFalse(s.Equip("b"), "not owned yet");
            Assert.AreEqual("a", s.EquippedId);

            s.TryPurchase("b");
            Assert.IsTrue(s.Equip("b"));
            Assert.AreEqual("b", s.EquippedId);
            Assert.IsTrue(s.Equip("c"));

            Assert.AreEqual("c", _kit.Progress.EquippedEmoteId, "the choice is in the save");
            Assert.IsFalse(s.Equip("nope"));
        }

        [Test]
        public void Changed_FiresOnPurchaseAndRealEquip_NotOnRepeat()
        {
            var s = Service(PriceB);
            int changed = 0;
            s.Changed += () => changed++;

            s.TryPurchase("b");
            Assert.AreEqual(1, changed);
            s.Equip("c");
            Assert.AreEqual(2, changed);
            s.Equip("c");
            Assert.AreEqual(2, changed, "equipping the equipped emote changes nothing");
        }

        // ---- the screen's model ----

        [Test]
        public void Model_BeginsOnTheEquippedEmote_AndFollowsOwnership()
        {
            var s = Service(PriceB);
            var model = new EmoteScreenModel(s);
            Assert.AreEqual(s.EquippedId, model.SelectedId);
            Assert.AreEqual(EmoteAction.Equipped, model.Action);

            Assert.IsTrue(model.Select("c"));
            Assert.AreEqual(EmoteAction.Equip, model.Action);
            Assert.IsTrue(model.Equip());
            Assert.AreEqual(EmoteAction.Equipped, model.Action);

            Assert.IsTrue(model.Select("b"), "a locked emote can be selected (previewed)");
            Assert.AreEqual(EmoteAction.Buy, model.Action);
            Assert.AreEqual(PriceB, model.SelectedPrice);
            Assert.IsFalse(model.Equip(), "a locked emote cannot be equipped");

            Assert.AreEqual(EmotePurchaseResult.Purchased, model.Purchase());
            Assert.AreEqual(EmoteAction.Equip, model.Action, "bought, not equipped yet");
        }

        [Test]
        public void Model_UnknownSelection_DoesNothing()
        {
            var model = new EmoteScreenModel(Service(0));
            string before = model.SelectedId;
            Assert.IsFalse(model.Select("nope"));
            Assert.AreEqual(before, model.SelectedId);
        }

        [Test]
        public void Model_NotEnoughCash_LeavesTheActionAsBuy()
        {
            var model = new EmoteScreenModel(Service(PriceD - 1));
            model.Select("d");
            Assert.AreEqual(EmotePurchaseResult.NotEnoughCash, model.Purchase());
            Assert.AreEqual(EmoteAction.Buy, model.Action);
        }
    }
}
