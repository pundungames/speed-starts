using System;
using NUnit.Framework;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Cosmetics;
using SpeedStars.Meta.Rewards;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Economy;

namespace SpeedStars.Tests.Meta.Rewards
{
    public sealed class SevenDayRewardServiceTests
    {
        // Arbitrary test inputs; assertions are relations between them.
        private const int CashA = 30;
        private const int CashB = 70;
        private const int TimedAmount = 2;
        private const int HatPrice = 9;
        private const string HatId = "test-hat-premium";

        private EconomyTestKit _k;
        private SevenDayRewardConfig _config;
        private CosmeticDefinition _hat;
        private CosmeticService _cosmetics;
        private SevenDayRewardService _service;
        private DateTime _day0;

        [SetUp]
        public void SetUp()
        {
            _k = new EconomyTestKit();
            _day0 = new DateTime(2030, 5, 10, 8, 0, 0, DateTimeKind.Utc);
            _k.Kit.Clock.UtcNow = _day0;

            _hat = _k.Kit.Fixture.Create<CosmeticDefinition>();
            ConfigFixture.SetString(_hat, "cosmeticId", HatId);
            ConfigFixture.SetInt(_hat, "category", (int)CosmeticCategory.Headwear);
            ConfigFixture.SetBool(_hat, "isPremium", true);
            ConfigFixture.SetInt(_hat, "price", HatPrice);
            ConfigFixture.SetInt(_hat, "currency", (int)CosmeticCurrency.PurpleGem);
            ConfigFixture.SetStrings(_hat, "partNames", "TEST");

            var catalog = _k.Kit.Fixture.Create<CosmeticCatalog>();
            ConfigFixture.SetObjects(catalog, "cosmetics", _hat);
            _cosmetics = new CosmeticService(_k.Kit.Repository, _k.Ledger, catalog);

            _config = _k.Kit.Fixture.Create<SevenDayRewardConfig>();
            SetDays(
                (SevenDayRewardKind.Cash, CashA, null),
                (SevenDayRewardKind.Cash, CashB, null),
                (SevenDayRewardKind.TimedTicket, TimedAmount, null),
                (SevenDayRewardKind.Cash, CashA, null),
                (SevenDayRewardKind.TimedTicket, TimedAmount, null),
                (SevenDayRewardKind.Cash, CashB, null),
                (SevenDayRewardKind.Cosmetic, 0, _hat));
            Build();
        }

        [TearDown]
        public void TearDown() => _k.Dispose();

        private void Build() =>
            _service = new SevenDayRewardService(_k.Kit.Repository, _k.Kit.Clock, _config, _k.Ledger, _cosmetics);

        private void SetDays(params (SevenDayRewardKind kind, int amount, CosmeticDefinition item)[] days) =>
            ConfigFixture.Edit(_config, so =>
            {
                var list = ConfigFixture.Find(so, "days");
                list.arraySize = days.Length;
                for (int i = 0; i < days.Length; i++)
                {
                    var day = list.GetArrayElementAtIndex(i);
                    day.FindPropertyRelative("kind").intValue = (int)days[i].kind;
                    day.FindPropertyRelative("amount").intValue = days[i].amount;
                    day.FindPropertyRelative("cosmetic").objectReferenceValue = days[i].item;
                }
            });

        private void NextDay() => _k.Kit.Clock.UtcNow = _k.Kit.Clock.UtcNow.Date.AddDays(1).AddHours(1);

        private void ClaimDays(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(SevenDayClaimResult.Claimed, _service.Claim(), "claim " + (i + 1));
                NextDay();
            }
        }

        private int CountOf(SevenDayCardState state)
        {
            int n = 0;
            foreach (SevenDayCard card in _service.Cards) if (card.State == state) n++;
            return n;
        }

        [Test]
        public void FreshSave_FirstCardIsAvailable_RestAreLocked()
        {
            var cards = _service.Cards;

            Assert.AreEqual(SevenDayRewardConfig.DayCount, cards.Count);
            Assert.AreEqual(SevenDayCardState.Available, cards[0].State);
            Assert.AreEqual(SevenDayRewardConfig.DayCount - 1, CountOf(SevenDayCardState.Locked));
            Assert.IsTrue(_service.HasClaimable);
            for (int i = 0; i < cards.Count; i++) Assert.AreEqual(i + 1, cards[i].DayNumber);
        }

        [Test]
        public void Cards_ReflectTheConfig()
        {
            var cards = _service.Cards;

            Assert.AreEqual(SevenDayRewardKind.Cash, cards[0].Kind);
            Assert.AreEqual(CashA, cards[0].Amount);
            Assert.AreEqual(SevenDayRewardKind.TimedTicket, cards[2].Kind);
            Assert.AreEqual(SevenDayRewardKind.Cosmetic, cards[6].Kind);
            Assert.AreEqual(HatId, cards[6].CosmeticId);
            Assert.AreEqual(string.Empty, cards[0].CosmeticId);
        }

        [Test]
        public void Claim_PaysTheDay_MarksItClaimed_AndOnlyOnePerUtcDay()
        {
            Assert.AreEqual(SevenDayClaimResult.Claimed, _service.Claim());

            Assert.AreEqual(CashA, _k.Progress.Cash);
            Assert.AreEqual(SevenDayCardState.Claimed, _service.Cards[0].State);
            Assert.AreEqual(SevenDayCardState.Locked, _service.Cards[1].State);
            Assert.IsFalse(_service.HasClaimable);
            Assert.AreEqual(SevenDayClaimResult.NothingAvailable, _service.Claim());
            Assert.AreEqual(CashA, _k.Progress.Cash);
        }

        [Test]
        public void NextUtcDay_UnlocksTheNextCard_InConfigOrder()
        {
            _service.Claim();
            NextDay();

            Assert.AreEqual(SevenDayCardState.Available, _service.Cards[1].State);
            _service.Claim();
            Assert.AreEqual(CashA + CashB, _k.Progress.Cash);
        }

        [Test]
        public void MissedDays_DoNotResetOrSkip()
        {
            ClaimDays(2);
            _k.Kit.Clock.UtcNow = _k.Kit.Clock.UtcNow.AddDays(30);

            Assert.AreEqual(SevenDayCardState.Available, _service.Cards[2].State);
            Assert.AreEqual(2, CountOf(SevenDayCardState.Claimed));
            Assert.AreEqual(SevenDayClaimResult.Claimed, _service.Claim());
            Assert.AreEqual(TimedAmount, _k.Progress.TimedTickets);
        }

        [Test]
        public void TimedTicket_GoesToStock_AndIsNotActivated()
        {
            ClaimDays(2);

            _service.Claim();

            Assert.AreEqual(TimedAmount, _k.Progress.TimedTickets);
            Assert.AreEqual(0, _k.Progress.TimedAccessExpiresUtcTicks);
            Assert.IsFalse(_k.Wallet.TimedAccessActive);
        }

        [Test]
        public void DaySeven_GivesTheItem_AndPaysNothingElse()
        {
            ClaimDays(6);
            int gems = _k.Progress.PurpleGems;

            Assert.AreEqual(SevenDayClaimResult.Claimed, _service.Claim());

            Assert.IsTrue(_cosmetics.IsOwned(HatId));
            Assert.AreEqual(gems, _k.Progress.PurpleGems);
        }

        [Test]
        public void DaySeven_AlreadyOwned_PaysTheItemsPriceInstead()
        {
            _k.Progress.OwnedCosmeticIds.Add(HatId);
            ClaimDays(6);
            int gems = _k.Progress.PurpleGems;

            Assert.AreEqual(SevenDayClaimResult.Claimed, _service.Claim());

            Assert.AreEqual(gems + HatPrice, _k.Progress.PurpleGems);
            Assert.AreEqual(1, _k.Progress.OwnedCosmeticIds.FindAll(id => id == HatId).Count);
        }

        [Test]
        public void AfterDaySeven_TheCalendarRestartsAtDayOne_NextDay()
        {
            ClaimDays(6);
            _service.Claim();

            Assert.AreEqual(SevenDayRewardConfig.DayCount, CountOf(SevenDayCardState.Claimed), "the finished round stays complete today");
            Assert.IsFalse(_service.HasClaimable);

            NextDay();
            Assert.AreEqual(SevenDayCardState.Available, _service.Cards[0].State);
            Assert.AreEqual(0, CountOf(SevenDayCardState.Claimed));
            int cash = _k.Progress.Cash;
            _service.Claim();
            Assert.AreEqual(cash + CashA, _k.Progress.Cash);
        }

        [Test]
        public void EndPolicy_NothingIsClaimableAfterTheLastDay()
        {
            ConfigFixture.SetInt(_config, "cyclePolicy", (int)SevenDayCyclePolicy.EndAfterLastDay);
            ClaimDays(SevenDayRewardConfig.DayCount);

            Assert.IsFalse(_service.HasClaimable);
            Assert.AreEqual(SevenDayClaimResult.NothingAvailable, _service.Claim());
            Assert.AreEqual(SevenDayRewardConfig.DayCount, CountOf(SevenDayCardState.Claimed));
        }

        [Test]
        public void FailedGrant_ChangesNothing_AndTheClaimCanBeRetried()
        {
            // An unsupported kind cannot be paid: the claim fails and progress stays.
            SetDays((SevenDayRewardKind.EntryTicket, 1, null));
            Build();

            Assert.AreEqual(SevenDayClaimResult.Failed, _service.Claim());

            Assert.AreEqual(0, _k.Progress.SevenDay.ClaimedCount);
            Assert.AreEqual(string.Empty, _k.Progress.SevenDay.LastClaimDayKey);
            Assert.AreEqual(0, _k.Progress.Cash);
            Assert.IsTrue(_service.HasClaimable);
        }

        [Test]
        public void EmptyDay_AdvancesTheCalendarWithoutPaying()
        {
            SetDays((SevenDayRewardKind.None, 0, null), (SevenDayRewardKind.Cash, CashA, null));
            Build();

            Assert.AreEqual(SevenDayClaimResult.Claimed, _service.Claim());

            Assert.AreEqual(1, _k.Progress.SevenDay.ClaimedCount);
            Assert.AreEqual(0, _k.Progress.Cash);
        }

        [Test]
        public void Progress_SurvivesARestart_AndTodaysClaimStaysUsed()
        {
            _service.Claim();

            var reloaded = _k.Restart();
            var again = new SevenDayRewardService(reloaded, _k.Kit.Clock, _config, _k.Ledger, _cosmetics);

            Assert.AreEqual(1, reloaded.Current.SevenDay.ClaimedCount);
            Assert.IsFalse(again.HasClaimable);
            Assert.AreEqual(SevenDayCardState.Claimed, again.Cards[0].State);
        }

        [Test]
        public void Claim_RaisesChanged_OnlyWhenClaimed()
        {
            int raised = 0;
            _service.Changed += () => raised++;

            _service.Claim();
            _service.Claim();

            Assert.AreEqual(1, raised);
        }
    }
}
