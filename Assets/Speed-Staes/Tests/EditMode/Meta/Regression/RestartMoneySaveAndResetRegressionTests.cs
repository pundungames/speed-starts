using System;
using System.IO;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Tests.Meta.Regression
{
    /// <summary>
    /// Scenarios 5, 6 and 7 (T-053): transaction ids across a restart, a damaged save file (the contract of
    /// <c>ProgressRepository</c> + <c>FileSaveStore</c> as it is today), and the two resets of the settings window followed by a
    /// restart. The file tests use a temporary folder that TearDown deletes.
    /// </summary>
    public sealed class RestartMoneySaveAndResetRegressionTests
    {
        private RegressionHarness _h;
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "SpeedStarsTests", "regression-" + Guid.NewGuid().ToString("N"));
            _h = new RegressionHarness();
        }

        [TearDown]
        public void TearDown()
        {
            _h.Dispose();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private FileSaveStore UseFileStore()
        {
            _h.Dispose();
            var store = new FileSaveStore(_root);
            _h = new RegressionHarness(store);
            return store;
        }

        // ---- scenario 5: payment transaction ids ------------------------------------------------------------------

        [Test]
        public void RacePayout_IsPaidOnce_AlsoWhenTheSameRaceIsReportedAfterARestart()
        {
            var s = _h.Boot();
            int before = s.Wallet.PurpleGems;
            int paid = s.Rewards.Resolve(_h.Outcome(rank: 1), "race-1");
            Assert.AreEqual(_h.Gems.GetPurpleGems(1), paid);
            Assert.Greater(paid, 0, "the test needs a paying rank");
            Assert.AreEqual(before + paid, s.Wallet.PurpleGems);

            var r = _h.Boot();

            Assert.AreEqual(before + paid, r.Wallet.PurpleGems);
            Assert.AreEqual(0, r.Rewards.Resolve(_h.Outcome(rank: 1), "race-1"), "the saved transaction id blocks the second payout");
            Assert.AreEqual(before + paid, r.Wallet.PurpleGems);
        }

        [Test]
        public void RacePayout_OfADifferentRace_StillPays_AfterARestart()
        {
            var s = _h.Boot();
            int paid = s.Rewards.Resolve(_h.Outcome(rank: 1), "race-1");

            var r = _h.Boot();
            int before = r.Wallet.PurpleGems;
            int second = r.Rewards.Resolve(_h.Outcome(rank: 2), "race-2");

            Assert.AreEqual(_h.Gems.GetPurpleGems(2), second);
            Assert.AreEqual(before + second, r.Wallet.PurpleGems);
            Assert.Greater(paid, 0);
        }

        [Test]
        public void CosmeticPurchase_IsChargedOnce_AndStaysOwned_AfterARestart()
        {
            var s = _h.Boot();
            int gems = s.Wallet.PurpleGems;
            Assert.AreEqual(CosmeticPurchaseResult.Purchased, s.Cosmetics.TryPurchase(RegressionHarness.HatId));
            Assert.AreEqual(gems - RegressionHarness.HatPrice, s.Wallet.PurpleGems);

            var r = _h.Boot();

            Assert.IsTrue(r.Cosmetics.IsOwned(RegressionHarness.HatId));
            Assert.AreEqual(gems - RegressionHarness.HatPrice, r.Wallet.PurpleGems);
            Assert.AreEqual(CosmeticPurchaseResult.AlreadyOwned, r.Cosmetics.TryPurchase(RegressionHarness.HatId));
            Assert.AreEqual(gems - RegressionHarness.HatPrice, r.Wallet.PurpleGems);
            Assert.AreEqual(1, r.Progress.OwnedCosmeticIds.FindAll(id => id == RegressionHarness.HatId).Count);
        }

        [Test]
        public void ThemePurchase_IsChargedOnce_AndTheChoiceSurvivesARestart()
        {
            var s = _h.Boot();
            int cash = s.Wallet.Cash;
            Assert.AreEqual(ThemePurchaseResult.Purchased, s.Themes.TryPurchase(RegressionHarness.ThemePaid));
            Assert.AreEqual(ThemeSelectResult.Selected, s.Themes.Use(RegressionHarness.ThemePaid));

            var r = _h.Boot();

            Assert.AreEqual(cash - RegressionHarness.ThemePrice, r.Wallet.Cash);
            Assert.AreEqual(RegressionHarness.ThemePaid, r.Themes.ActiveId);
            Assert.AreEqual(ThemePurchaseResult.AlreadyOwned, r.Themes.TryPurchase(RegressionHarness.ThemePaid));
            Assert.AreEqual(cash - RegressionHarness.ThemePrice, r.Wallet.Cash);
        }

        // ---- scenario 6: damaged save file ------------------------------------------------------------------------

        private static void Truncate(string path)
        {
            byte[] all = File.ReadAllBytes(path);
            var half = new byte[all.Length / 2];
            Array.Copy(all, half, half.Length);
            File.WriteAllBytes(path, half);
        }

        [Test]
        public void TornMainFile_OpensFromTheBackup_WithThePreviousState_AndDoesNotThrow()
        {
            FileSaveStore store = UseFileStore();
            var s = _h.Boot();
            s.Progress.Cash = 111;
            Assert.IsTrue(s.Repository.Save());
            s.Progress.Cash = 222;
            Assert.IsTrue(s.Repository.Save());
            Truncate(store.GetMainPath(ProgressRepository.SaveKey));

            RegressionHarness.Session r = null;
            Assert.DoesNotThrow(() => r = _h.Boot());

            Assert.AreEqual(ProgressLoadStatus.Loaded, r.Repository.Status);
            Assert.AreEqual(111, r.Progress.Cash, "the previous good save, not the torn one and not a new player");
            Assert.IsTrue(r.Repository.CanSave);
        }

        [Test]
        public void MainAndBackupBothBroken_OpensAsANewPlayer_FromTheConfigStart_AndCanSaveAgain()
        {
            FileSaveStore store = UseFileStore();
            var s = _h.Boot();
            s.Progress.Cash = 111;
            s.Repository.Save();
            s.Progress.Cash = 222;
            s.Repository.Save();
            File.WriteAllText(store.GetMainPath(ProgressRepository.SaveKey), "garbage");
            File.WriteAllText(store.GetBackupPath(ProgressRepository.SaveKey), "garbage");

            RegressionHarness.Session r = null;
            Assert.DoesNotThrow(() => r = _h.Boot());

            Assert.AreEqual(ProgressLoadStatus.Created, r.Repository.Status);
            Assert.AreEqual(_h.Economy.StartingCash, r.Progress.Cash);
            Assert.AreEqual(_h.Economy.StartingPurpleGems, r.Wallet.PurpleGems);
            Assert.IsTrue(r.Repository.CanSave);

            r.Progress.Cash = 333;
            Assert.IsTrue(r.Repository.Save());
            var again = _h.Boot();
            Assert.AreEqual(ProgressLoadStatus.Loaded, again.Repository.Status);
            Assert.AreEqual(333, again.Progress.Cash);
        }

        [Test]
        public void LeftoverHalfWrittenTempFile_IsIgnored()
        {
            FileSaveStore store = UseFileStore();
            var s = _h.Boot();
            s.Progress.Cash = 111;
            s.Repository.Save();
            File.WriteAllText(store.GetTempPath(ProgressRepository.SaveKey), "{\"schemaVersion\":2,\"cash\":99");

            var r = _h.Boot();

            Assert.AreEqual(ProgressLoadStatus.Loaded, r.Repository.Status);
            Assert.AreEqual(111, r.Progress.Cash);
        }

        [Test]
        public void FirstEverSave_TornBeforeAnyBackupExists_OpensAsANewPlayer()
        {
            FileSaveStore store = UseFileStore();
            var s = _h.Boot();
            s.Progress.Cash = 111;
            s.Repository.Save();
            Truncate(store.GetMainPath(ProgressRepository.SaveKey));

            RegressionHarness.Session r = null;
            Assert.DoesNotThrow(() => r = _h.Boot());

            Assert.AreEqual(ProgressLoadStatus.Created, r.Repository.Status);
            Assert.AreEqual(_h.Economy.StartingCash, r.Progress.Cash);
        }

        [Test]
        public void UnparsableContent_RunsOnANewPlayer_ServicesWork_AndTheFileIsNeverOverwritten()
        {
            const string junk = "this is not json";
            _h.Store.Write(ProgressRepository.SaveKey, junk);

            RegressionHarness.Session r = null;
            Assert.DoesNotThrow(() => r = _h.Boot());

            Assert.AreEqual(ProgressLoadStatus.Unreadable, r.Repository.Status);
            Assert.AreEqual(_h.Economy.StartingCash, r.Progress.Cash);
            Assert.DoesNotThrow(() =>
            {
                Assert.IsTrue(r.Tickets.TrySpend());
                r.Gift.Claim();
                r.Rewards.Resolve(_h.Outcome(rank: 1), "race-1");
            });
            Assert.IsFalse(r.Repository.Save());
            Assert.IsTrue(_h.Store.TryRead(ProgressRepository.SaveKey, out string stored));
            Assert.AreEqual(junk, stored);

            // The next launch sees the same protected file.
            Assert.AreEqual(ProgressLoadStatus.Unreadable, _h.Boot().Repository.Status);
        }

        [Test]
        public void SaveFromANewerBuild_IsNotOverwritten_ByAnythingTheServicesDo()
        {
            string newer = "{\"schemaVersion\":" + (ProgressRepository.CurrentSchemaVersion + 1) + ",\"cash\":5}";
            _h.Store.Write(ProgressRepository.SaveKey, newer);

            var r = _h.Boot();
            r.Tickets.TrySpend();
            r.Gift.Claim();
            r.Settings.SetMusic(false);
            r.Bests.Record(RegressionHarness.GemEvent, 12.5, "race-1");

            Assert.AreEqual(ProgressLoadStatus.NewerSchema, r.Repository.Status);
            Assert.IsTrue(_h.Store.TryRead(ProgressRepository.SaveKey, out string stored));
            Assert.AreEqual(newer, stored);
        }

        // ---- scenario 7: resets -----------------------------------------------------------------------------------

        private RegressionHarness.Session PlayAFewThings()
        {
            var s = _h.Boot();
            Assert.IsTrue(s.Tickets.TrySpend());
            s.Repository.Save();
            Assert.AreEqual(DailyGiftResult.Granted, s.Gift.Claim());
            Assert.AreEqual(CosmeticPurchaseResult.Purchased, s.Cosmetics.TryPurchase(RegressionHarness.HatId));
            Assert.AreEqual(ThemePurchaseResult.Purchased, s.Themes.TryPurchase(RegressionHarness.ThemePaid));
            Assert.AreEqual(ThemeSelectResult.Selected, s.Themes.Use(RegressionHarness.ThemePaid));
            Assert.AreEqual(SevenDayClaimResult.Claimed, s.SevenDay.Claim());
            Assert.AreEqual(TimedAccessResult.Activated, s.Timed.TryActivate());
            s.Settings.SetMusic(false);
            s.Settings.SetSfx(false);
            Assert.AreEqual(PlayerNameResult.Valid, s.Settings.TrySetName("Ada"));
            Assert.IsTrue(s.Bests.Record(RegressionHarness.GemEvent, 12.5, "race-1").IsNewRecord);
            Assert.Greater(s.Rewards.Resolve(_h.Outcome(rank: 1), "race-1"), 0);
            return s;
        }

        [Test]
        public void ResetAll_ThenRestart_IsANewPlayer_FromTheConfigStart()
        {
            var s = PlayAFewThings();

            s.Reset.ResetAll();
            var r = _h.Boot();

            Assert.AreEqual(ProgressLoadStatus.Loaded, r.Repository.Status);
            Assert.AreEqual(_h.Economy.StartingCash, r.Wallet.Cash);
            Assert.AreEqual(_h.Economy.StartingPurpleGems, r.Wallet.PurpleGems);
            Assert.AreEqual(_h.Economy.StartingTimedTickets, r.Wallet.TimedTickets);
            Assert.AreEqual(Math.Min(_h.Economy.StartingEntryTickets, _h.Economy.EntryTicketCap), r.Tickets.Count);
            Assert.IsFalse(r.Wallet.TimedAccessActive);
            Assert.IsFalse(r.Cosmetics.IsOwned(RegressionHarness.HatId));
            Assert.IsFalse(r.Themes.IsOwned(RegressionHarness.ThemePaid));
            Assert.AreEqual(RegressionHarness.ThemeDefault, r.Themes.ActiveId);
            Assert.AreNotEqual(DailyGiftState.NotDue, r.Gift.State, "today's gift is available again");
            Assert.IsTrue(r.SevenDay.HasClaimable);
            Assert.AreEqual(0, r.Progress.SevenDay.ClaimedCount);
            CollectionAssert.IsEmpty(r.Progress.ProcessedTransactionIds);
            Assert.IsFalse(r.Bests.TryGetBest(RegressionHarness.GemEvent, out _));
            Assert.AreEqual(_h.Gems.GetPurpleGems(1), r.Rewards.Resolve(_h.Outcome(rank: 1), "race-1"),
                "a transaction id from before the reset is forgotten with the rest");
        }

        [Test]
        public void ResetAll_ThenRestart_KeepsTheSettings()
        {
            var s = PlayAFewThings();

            s.Reset.ResetAll();
            var r = _h.Boot();

            Assert.IsFalse(r.Settings.MusicOn, "the player's own switch is kept (the config default is on)");
            Assert.IsFalse(r.Settings.SfxOn);
            Assert.AreEqual("Ada", r.Settings.PlayerName);
        }

        [Test]
        public void ResetAll_ThenRestart_KeepsTheClockGuard_SoWindingTheClockBackStillEarnsNothing()
        {
            var s = PlayAFewThings();
            long guard = s.Progress.LastSeenUtcTicks;
            Assert.Greater(guard, 0L);

            s.Reset.ResetAll();
            _h.Device -= TimeSpan.FromDays(2);
            var r = _h.Boot();

            Assert.AreEqual(guard, r.Progress.LastSeenUtcTicks);
            Assert.AreEqual(guard, r.Clock.UtcNow.Ticks, "the clock still holds at the latest time seen before the reset");
            Assert.IsTrue(r.Clock.IsRolledBack);
        }

        private static LeaderboardPage Ask(RegressionHarness.Session s)
        {
            LeaderboardPage page = null;
            string error = null;
            s.Board.Query(new LeaderboardQuery(RegressionHarness.GemEvent, LeaderboardPeriod.AllTime, null, 0, 50),
                p => page = p, e => error = e);
            Assert.IsNull(error);
            Assert.IsNotNull(page);
            return page;
        }

        [Test]
        public void ResetPersonalBests_RemovesThePlayerFromTheLeaderboard_AlsoAfterARestart_AndTouchesNothingElse()
        {
            var s = _h.Boot();
            s.Bests.Record(RegressionHarness.GemEvent, 12.5, "race-1");
            _h.Device += TimeSpan.FromDays(1);
            s.Bests.Record(RegressionHarness.GemEvent, 11.5, "race-2");
            s.Settings.TrySetName("Ada");
            int cash = s.Wallet.Cash;

            LeaderboardPage before = Ask(s);
            Assert.IsNotNull(before.OwnEntry, "the player is ranked before the reset");
            Assert.IsTrue(s.Board.TryGetStanding(RegressionHarness.GemEvent, out _, out _));
            Assert.IsTrue(s.Bests.TryGetBest(RegressionHarness.GemEvent, out _));

            s.Reset.ResetPersonalBests();

            LeaderboardPage after = Ask(s);
            Assert.IsNull(after.OwnEntry);
            Assert.AreEqual(before.TotalCount - 1, after.TotalCount, "exactly the player's one row is gone");
            Assert.IsFalse(s.Board.TryGetStanding(RegressionHarness.GemEvent, out _, out _));
            Assert.IsFalse(s.Bests.TryGetBest(RegressionHarness.GemEvent, out _));

            var r = _h.Boot();
            Assert.IsNull(Ask(r).OwnEntry, "the reset was saved at once");
            Assert.IsFalse(r.Bests.TryGetBest(RegressionHarness.GemEvent, out _));
            Assert.AreEqual(cash, r.Wallet.Cash);
            Assert.AreEqual("Ada", r.Settings.PlayerName);
        }

        [Test]
        public void NewRaceAfterResetPersonalBests_IsANewRecord_AndRanksAgain()
        {
            var s = _h.Boot();
            s.Bests.Record(RegressionHarness.GemEvent, 9.5, "race-1");
            s.Reset.ResetPersonalBests();

            var r = _h.Boot();
            PersonalBestUpdate update = r.Bests.Record(RegressionHarness.GemEvent, 14.5, "race-2");

            Assert.IsTrue(update.IsNewRecord, "a slower time than the wiped record is a record again");
            Assert.IsNotNull(Ask(r).OwnEntry);
        }
    }
}
