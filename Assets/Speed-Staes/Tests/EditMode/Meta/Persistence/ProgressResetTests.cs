using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Tests.Meta.Persistence
{
    /// <summary>The two resets of the settings window on the repository. Balances are test values; tests assert they are used.</summary>
    public sealed class ProgressResetTests
    {
        private static readonly StartingBalances Start = new StartingBalances(1234, 56, 7, 8);

        private MemoryStore _store;
        private ProgressRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _store = new MemoryStore();
            _repo = new ProgressRepository(_store, () => Start);
            _repo.Load();
            Fill(_repo.Current);
        }

        private static void Fill(PlayerProgress p)
        {
            p.Cash = Start.Cash + 999;
            p.PurpleGems = Start.PurpleGems + 99;
            p.EntryTickets = 0;
            p.TimedTickets = 0;
            p.DailyGiftDayKey = "2026-10-30";
            p.SevenDay.ClaimedCount = 3;
            p.OwnedCosmeticIds.Add("hat_1");
            p.OwnedThemeIds.Add("theme_1");
            p.SelectedThemeId = "theme_1";
            p.OwnedEmoteIds.Add("emote_1");
            p.EquippedEmoteId = "emote_1";
            p.ProcessedTransactionIds.Add("tx1");
            p.BestTimes.Add(new EventBestTime(new RaceEventId("ev"), 12.3));
            p.DayBests.Add(new DayBestEntry(new RaceEventId("ev"), 10L, 12300L, 11L));
            p.MusicChoice = SettingChoice.Off;
            p.SfxChoice = SettingChoice.On;
            p.LanguageCode = "en";
            p.FlagCode = "US";
            p.PlayerName = "Ada";
            p.LastSeenUtcTicks = 777L;
        }

        [Test]
        public void ResetPersonalBests_ClearsBestTimesAndDayBests_AndNothingElse()
        {
            _repo.ResetPersonalBests();

            PlayerProgress p = _repo.Current;
            CollectionAssert.IsEmpty(p.BestTimes);
            CollectionAssert.IsEmpty(p.DayBests);
            Assert.AreEqual(Start.Cash + 999, p.Cash);
            CollectionAssert.AreEqual(new[] { "hat_1" }, p.OwnedCosmeticIds);
            Assert.AreEqual("Ada", p.PlayerName);
            Assert.AreEqual(3, p.SevenDay.ClaimedCount);
        }

        [Test]
        public void ResetPersonalBests_IsSavedAtOnce()
        {
            _repo.ResetPersonalBests();

            var reloaded = new ProgressRepository(_store, () => Start);
            reloaded.Load();
            CollectionAssert.IsEmpty(reloaded.Current.BestTimes);
            CollectionAssert.IsEmpty(reloaded.Current.DayBests);
            Assert.AreEqual(Start.Cash + 999, reloaded.Current.Cash);
        }

        [Test]
        public void ResetAll_BecomesANewPlayer_FromTheStartingBalances()
        {
            _repo.ResetAll();

            PlayerProgress p = _repo.Current;
            Assert.AreEqual(Start.Cash, p.Cash);
            Assert.AreEqual(Start.PurpleGems, p.PurpleGems);
            Assert.AreEqual(Start.EntryTickets, p.EntryTickets);
            Assert.AreEqual(Start.TimedTickets, p.TimedTickets);
            Assert.AreEqual(string.Empty, p.DailyGiftDayKey);
            Assert.AreEqual(0, p.SevenDay.ClaimedCount);
            CollectionAssert.IsEmpty(p.OwnedCosmeticIds);
            CollectionAssert.IsEmpty(p.OwnedThemeIds);
            CollectionAssert.IsEmpty(p.OwnedEmoteIds);
            CollectionAssert.IsEmpty(p.ProcessedTransactionIds);
            CollectionAssert.IsEmpty(p.BestTimes);
            CollectionAssert.IsEmpty(p.DayBests);
            Assert.AreEqual(string.Empty, p.SelectedThemeId);
            Assert.AreEqual(string.Empty, p.EquippedEmoteId);
        }

        [Test]
        public void ResetAll_KeepsTheSettings()
        {
            _repo.ResetAll();

            PlayerProgress p = _repo.Current;
            Assert.AreEqual(SettingChoice.Off, p.MusicChoice);
            Assert.AreEqual(SettingChoice.On, p.SfxChoice);
            Assert.AreEqual("en", p.LanguageCode);
            Assert.AreEqual("US", p.FlagCode);
            Assert.AreEqual("Ada", p.PlayerName);
            Assert.AreEqual(777L, p.LastSeenUtcTicks);
        }

        [Test]
        public void ResetAll_IsSavedAtOnce_AndSurvivesAReload()
        {
            _repo.ResetAll();

            var reloaded = new ProgressRepository(_store, () => Start);
            reloaded.Load();
            Assert.AreEqual(ProgressLoadStatus.Loaded, reloaded.Status);
            Assert.AreEqual(Start.Cash, reloaded.Current.Cash);
            CollectionAssert.IsEmpty(reloaded.Current.OwnedCosmeticIds);
            Assert.AreEqual("Ada", reloaded.Current.PlayerName);
        }

        [Test]
        public void ResetAll_ReplacesAProtectedSave_BecauseThePlayerAskedForIt()
        {
            _store.Seed(ProgressRepository.SaveKey, "this is not json");
            var repo = new ProgressRepository(_store, () => Start);
            repo.Load();
            Assert.IsFalse(repo.CanSave);

            repo.ResetAll();

            Assert.IsTrue(repo.CanSave);
            var reloaded = new ProgressRepository(_store, () => Start);
            reloaded.Load();
            Assert.AreEqual(ProgressLoadStatus.Loaded, reloaded.Status);
        }

        [Test]
        public void Resets_BeforeLoad_Throw()
        {
            var fresh = new ProgressRepository(_store, () => Start);
            Assert.Throws<System.InvalidOperationException>(() => fresh.ResetAll());
            Assert.Throws<System.InvalidOperationException>(() => fresh.ResetPersonalBests());
        }

        private sealed class MemoryStore : ISaveStore
        {
            private readonly Dictionary<string, string> _data = new Dictionary<string, string>();

            public void Seed(string key, string contents) => _data[key] = contents;
            public bool TryRead(string key, out string contents) => _data.TryGetValue(key, out contents);
            public void Write(string key, string contents) => _data[key] = contents;
            public void Delete(string key) => _data.Remove(key);
        }
    }
}
