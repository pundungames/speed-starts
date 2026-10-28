using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Persistence;
using UnityEngine;

namespace SpeedStars.Tests.Meta.Persistence
{
    /// <summary>
    /// ProgressRepository over an in-memory store (plus one pass over the real file store). Starting balances are
    /// arbitrary test values; tests assert that they are used, not what they are.
    /// </summary>
    public sealed class ProgressRepositoryTests
    {
        private static readonly StartingBalances Start = new StartingBalances(1234, 56, 7, 8);

        private MemorySaveStore _store;
        private int _startRequests;

        [SetUp]
        public void SetUp()
        {
            _store = new MemorySaveStore();
            _startRequests = 0;
        }

        private ProgressRepository NewRepository(ISaveStore store = null)
        {
            return new ProgressRepository(store ?? _store, () =>
            {
                _startRequests++;
                return Start;
            });
        }

        [Test]
        public void Load_NoSave_CreatesProgressFromStartingBalances()
        {
            var repo = NewRepository();

            PlayerProgress p = repo.Load();

            Assert.AreEqual(ProgressLoadStatus.Created, repo.Status);
            Assert.AreSame(p, repo.Current);
            Assert.AreEqual(Start.Cash, p.Cash);
            Assert.AreEqual(Start.PurpleGems, p.PurpleGems);
            Assert.AreEqual(Start.EntryTickets, p.EntryTickets);
            Assert.AreEqual(Start.TimedTickets, p.TimedTickets);
            Assert.AreEqual(ProgressRepository.CurrentSchemaVersion, p.SchemaVersion);
            Assert.IsTrue(repo.CanSave);
        }

        [Test]
        public void Load_NoSave_CollectionsAreEmptyNotNull()
        {
            PlayerProgress p = NewRepository().Load();

            Assert.IsNotNull(p.SevenDay);
            Assert.AreEqual(string.Empty, p.DailyGiftDayKey);
            Assert.AreEqual(string.Empty, p.SevenDay.LastClaimDayKey);
            CollectionAssert.IsEmpty(p.OwnedCosmeticIds);
            CollectionAssert.IsEmpty(p.SelectedCosmetics);
            CollectionAssert.IsEmpty(p.BestTimes);
            CollectionAssert.IsEmpty(p.ProcessedTransactionIds);
        }

        [Test]
        public void Load_NoSave_DoesNotWriteByItself()
        {
            NewRepository().Load();

            Assert.AreEqual(0, _store.Writes);
        }

        [Test]
        public void SaveThenLoad_RoundTripsEveryField()
        {
            var repo = NewRepository();
            Fill(repo.Load());
            Assert.IsTrue(repo.Save());

            var reloaded = NewRepository();
            PlayerProgress p = reloaded.Load();

            Assert.AreEqual(ProgressLoadStatus.Loaded, reloaded.Status);
            AssertFilled(p);
        }

        [Test]
        public void SaveThenLoad_ThroughFileStore_RoundTripsEveryField()
        {
            string root = Path.Combine(Path.GetTempPath(), "SpeedStarsTests", Guid.NewGuid().ToString("N"));
            try
            {
                var repo = NewRepository(new FileSaveStore(root));
                Fill(repo.Load());
                repo.Save();

                var reloaded = NewRepository(new FileSaveStore(root));
                PlayerProgress p = reloaded.Load();

                Assert.AreEqual(ProgressLoadStatus.Loaded, reloaded.Status);
                AssertFilled(p);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void Load_ExistingSave_IgnoresStartingBalances()
        {
            var repo = NewRepository();
            PlayerProgress first = repo.Load();
            first.Cash = Start.Cash + 1;
            repo.Save();
            _startRequests = 0;

            PlayerProgress p = NewRepository().Load();

            Assert.AreEqual(Start.Cash + 1, p.Cash);
            Assert.AreEqual(0, _startRequests);
        }

        [Test]
        public void Save_StampsCurrentSchemaVersion()
        {
            var repo = NewRepository();
            repo.Load().SchemaVersion = 0;

            repo.Save();

            PlayerProgress p = NewRepository().Load();
            Assert.AreEqual(ProgressRepository.CurrentSchemaVersion, p.SchemaVersion);
        }

        [Test]
        public void Save_BeforeLoad_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => NewRepository().Save());
        }

        [Test]
        public void Load_NewerSchema_RunsOnNewProgress_AndNeverOverwritesTheSave()
        {
            var newer = new PlayerProgress { SchemaVersion = ProgressRepository.CurrentSchemaVersion + 1, Cash = Start.Cash + 5 };
            string original = JsonUtility.ToJson(newer);
            _store.Seed(ProgressRepository.SaveKey, original);

            var repo = NewRepository();
            PlayerProgress p = repo.Load();

            Assert.AreEqual(ProgressLoadStatus.NewerSchema, repo.Status);
            Assert.AreEqual(Start.Cash, p.Cash);
            Assert.IsFalse(repo.CanSave);
            Assert.IsFalse(repo.Save());
            Assert.AreEqual(0, _store.Writes);
            Assert.AreEqual(original, _store.Peek(ProgressRepository.SaveKey));
        }

        [TestCase("this is not json")]
        [TestCase("{}")]
        public void Load_Unreadable_RunsOnNewProgress_AndNeverOverwritesTheSave(string stored)
        {
            _store.Seed(ProgressRepository.SaveKey, stored);

            var repo = NewRepository();
            PlayerProgress p = repo.Load();

            Assert.AreEqual(ProgressLoadStatus.Unreadable, repo.Status);
            Assert.AreEqual(Start.Cash, p.Cash);
            Assert.IsFalse(repo.Save());
            Assert.AreEqual(0, _store.Writes);
            Assert.AreEqual(stored, _store.Peek(ProgressRepository.SaveKey));
        }

        [Test]
        public void Load_SaveMissingNewerFields_FillsThemWithEmptyValues()
        {
            // An older build's save: only the version key. The key name is part of the save format and must not change.
            _store.Seed(ProgressRepository.SaveKey, "{\"schemaVersion\":" + ProgressRepository.CurrentSchemaVersion + "}");

            var repo = NewRepository();
            PlayerProgress p = repo.Load();

            Assert.AreEqual(ProgressLoadStatus.Loaded, repo.Status);
            Assert.IsNotNull(p.SevenDay);
            Assert.IsNotNull(p.OwnedCosmeticIds);
            Assert.IsNotNull(p.SelectedCosmetics);
            Assert.IsNotNull(p.BestTimes);
            Assert.IsNotNull(p.ProcessedTransactionIds);
            Assert.IsNotNull(p.DailyGiftDayKey);
        }

        [Test]
        public void Load_Twice_ReplacesCurrentWithStoredState()
        {
            var repo = NewRepository();
            repo.Load().Cash = Start.Cash + 7;
            repo.Save();
            repo.Current.Cash = Start.Cash + 9;

            PlayerProgress p = repo.Load();

            Assert.AreSame(p, repo.Current);
            Assert.AreEqual(Start.Cash + 7, p.Cash);
        }

        [Test]
        public void Constructor_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgressRepository(null, () => Start));
            Assert.Throws<ArgumentNullException>(() => new ProgressRepository(_store, null));
        }

        private static readonly DateTime SomeUtc = new DateTime(2026, 10, 28, 9, 15, 0, DateTimeKind.Utc);

        private static void Fill(PlayerProgress p)
        {
            p.Cash = 4321;
            p.PurpleGems = 65;
            p.EntryTickets = 9;
            p.TimedTickets = 13;
            p.TimedAccessExpiresUtcTicks = SomeUtc.AddHours(5).Ticks;
            p.EntryRegenDueUtcTicks = SomeUtc.AddMinutes(17).Ticks;
            p.DailyGiftDayKey = "2026-10-28";
            p.SevenDay.ClaimedCount = 3;
            p.SevenDay.LastClaimDayKey = "2026-10-27";
            p.OwnedCosmeticIds.Add("headwear.h01");
            p.OwnedCosmeticIds.Add("shoes.p02");
            p.SelectedCosmetics.Add(new SelectedCosmetic("Headwear", "headwear.h01", "FF8800"));
            p.SelectedCosmetics.Add(new SelectedCosmetic("Shoes", "shoes.p02", string.Empty));
            p.BestTimes.Add(new EventBestTime(new RaceEventId("100m"), 9.875));
            p.BestTimes.Add(new EventBestTime(new RaceEventId("400m"), 47.25));
            p.ProcessedTransactionIds.Add("entry-0001");
            p.ProcessedTransactionIds.Add("payout-0002");
            p.LastSeenUtcTicks = SomeUtc.Ticks;
        }

        private static void AssertFilled(PlayerProgress p)
        {
            Assert.AreEqual(ProgressRepository.CurrentSchemaVersion, p.SchemaVersion);
            Assert.AreEqual(4321, p.Cash);
            Assert.AreEqual(65, p.PurpleGems);
            Assert.AreEqual(9, p.EntryTickets);
            Assert.AreEqual(13, p.TimedTickets);
            Assert.AreEqual(SomeUtc.AddHours(5).Ticks, p.TimedAccessExpiresUtcTicks);
            Assert.AreEqual(SomeUtc.AddMinutes(17).Ticks, p.EntryRegenDueUtcTicks);
            Assert.AreEqual("2026-10-28", p.DailyGiftDayKey);
            Assert.AreEqual(3, p.SevenDay.ClaimedCount);
            Assert.AreEqual("2026-10-27", p.SevenDay.LastClaimDayKey);
            CollectionAssert.AreEqual(new[] { "headwear.h01", "shoes.p02" }, p.OwnedCosmeticIds);

            Assert.AreEqual(2, p.SelectedCosmetics.Count);
            Assert.AreEqual("Headwear", p.SelectedCosmetics[0].Category);
            Assert.AreEqual("headwear.h01", p.SelectedCosmetics[0].CosmeticId);
            Assert.AreEqual("FF8800", p.SelectedCosmetics[0].ColorHex);
            Assert.AreEqual("Shoes", p.SelectedCosmetics[1].Category);
            Assert.AreEqual(string.Empty, p.SelectedCosmetics[1].ColorHex);

            Assert.AreEqual(2, p.BestTimes.Count);
            Assert.AreEqual(new RaceEventId("100m"), p.BestTimes[0].EventId);
            Assert.AreEqual(9.875, p.BestTimes[0].BestSeconds);
            Assert.AreEqual(new RaceEventId("400m"), p.BestTimes[1].EventId);
            Assert.AreEqual(47.25, p.BestTimes[1].BestSeconds);

            CollectionAssert.AreEqual(new[] { "entry-0001", "payout-0002" }, p.ProcessedTransactionIds);
            Assert.AreEqual(SomeUtc.Ticks, p.LastSeenUtcTicks);
        }

        /// <summary>Dictionary-backed store that counts writes, so tests can prove a save was left alone.</summary>
        private sealed class MemorySaveStore : ISaveStore
        {
            private readonly Dictionary<string, string> _data = new Dictionary<string, string>();

            public int Writes { get; private set; }

            public void Seed(string key, string contents) => _data[key] = contents;
            public string Peek(string key) => _data[key];

            public bool TryRead(string key, out string contents) => _data.TryGetValue(key, out contents);

            public void Write(string key, string contents)
            {
                Writes++;
                _data[key] = contents;
            }

            public void Delete(string key) => _data.Remove(key);
        }
    }
}
