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
    /// <summary>
    /// v2 catalog shape (T-089): "none" has no clip and is free, the others name a clip; a save from v1 (procedural emote ids that
    /// no longer exist) opens as None. Ids, names and prices here are test inputs, never the shipped catalog.
    /// </summary>
    public sealed class EmoteCatalogMigrationTests
    {
        private const int Price = 70;

        private TicketTestKit _kit;

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        private EmoteService Service(int cash, out EmoteCatalog catalog)
        {
            _kit = new TicketTestKit(0);
            _kit.Progress.Cash = cash;
            catalog = _kit.Fixture.Create<EmoteCatalog>();
            ConfigFixture.Edit(catalog, so =>
            {
                var list = ConfigFixture.Find(so, "emotes");
                string[] ids = { "none", "clip_a", "clip_b" };
                string[] clips = { "", "ClipA", "ClipB" };
                int[] prices = { 0, Price, Price * 2 };
                list.arraySize = ids.Length;
                for (int i = 0; i < ids.Length; i++)
                {
                    var e = list.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("emoteId").stringValue = ids[i];
                    e.FindPropertyRelative("displayName").stringValue = ids[i];
                    e.FindPropertyRelative("cashPrice").intValue = prices[i];
                    e.FindPropertyRelative("clipName").stringValue = clips[i];
                }
                ConfigFixture.Find(so, "defaultEmoteId").stringValue = "none";
            });
            var wallet = new WalletService(_kit.Repository, _kit.Clock);
            return new EmoteService(catalog, _kit.Repository, new EconomyLedger(_kit.Repository, wallet, _kit.Economy));
        }

        [Test]
        public void None_HasNoClip_AndIsTheFreeDefault()
        {
            var service = Service(0, out EmoteCatalog catalog);

            Assert.IsTrue(catalog.TryGet("none", out EmoteDefinition none));
            Assert.IsFalse(none.HasClip);
            Assert.IsTrue(catalog.TryGet("clip_a", out EmoteDefinition a));
            Assert.IsTrue(a.HasClip);
            Assert.AreEqual("ClipA", a.ClipName);

            Assert.AreEqual("none", catalog.DefaultEmote.Id);
            Assert.AreEqual("none", service.EquippedId, "a new player equips None");
            Assert.IsTrue(service.IsOwned("none"));
        }

        [Test]
        public void SaveWithAnOldProceduralEmote_OpensAsNone()
        {
            var service = Service(0, out _);
            _kit.Progress.EquippedEmoteId = "wave";   // v1 ids that are not in the catalog any more
            _kit.Progress.OwnedEmoteIds.Add("flex");

            Assert.AreEqual("none", service.EquippedId);
            Assert.IsFalse(service.IsOwned("wave"));
            Assert.IsFalse(service.IsOwned("flex"), "a stale owned id of a gone emote owns nothing");
            Assert.IsFalse(service.TryGet("wave", out _));
            Assert.AreEqual(3, service.Emotes.Count);

            var model = new EmoteScreenModel(service);
            Assert.AreEqual("none", model.SelectedId, "the screen opens on None");
            Assert.AreEqual(EmoteAction.Equipped, model.Action);
        }

        [Test]
        public void SaveWithABoughtClip_KeepsItAcrossTheMigration()
        {
            var service = Service(Price, out _);
            Assert.AreEqual(EmotePurchaseResult.Purchased, service.TryPurchase("clip_a"));
            Assert.IsTrue(service.Equip("clip_a"));
            _kit.Progress.OwnedEmoteIds.Add("flex");

            Assert.AreEqual("clip_a", service.EquippedId, "a valid choice is not reset because an old id sits next to it");
        }

        [Test]
        public void EquippingNone_IsAllowed_AndNeverCharged()
        {
            var service = Service(0, out _);
            Assert.IsFalse(service.Equip("clip_a"), "not owned");
            Assert.IsTrue(service.Equip("none"));
            Assert.AreEqual(0, _kit.Progress.Cash);
        }
    }
}
