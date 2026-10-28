using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Settings;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Settings
{
    /// <summary>Config values here are test-chosen; assertions relate results to them, never to the shipped asset.</summary>
    public sealed class PlayerSettingsServiceTests
    {
        private const string LangA = "aa";
        private const string LangB = "bb";
        private const string FlagA = "FA";
        private const string FlagB = "FB";
        private const string DefaultName = "Anon";
        private const int NameMax = 5;

        private TicketTestKit _kit;
        private SettingsConfig _config;

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        private PlayerSettingsService Service(bool musicDefault = true, bool sfxDefault = true)
        {
            _kit = new TicketTestKit(0);
            _config = _kit.Fixture.Create<SettingsConfig>();
            ConfigFixture.Edit(_config, so =>
            {
                ConfigFixture.Find(so, "defaultMusicOn").boolValue = musicDefault;
                ConfigFixture.Find(so, "defaultSfxOn").boolValue = sfxDefault;
                ConfigFixture.Find(so, "defaultLanguageCode").stringValue = LangB;
                ConfigFixture.Find(so, "defaultFlagCode").stringValue = FlagB;
                ConfigFixture.Find(so, "defaultPlayerName").stringValue = DefaultName;
                ConfigFixture.Find(so, "nameMinLength").intValue = 2;
                ConfigFixture.Find(so, "nameMaxLength").intValue = NameMax;
                Fill(ConfigFixture.Find(so, "languages"), "languageCode", LangA, LangB);
                Fill(ConfigFixture.Find(so, "flags"), "flagCode", FlagA, FlagB);
            });
            return new PlayerSettingsService(_config, _kit.Repository);
        }

        private static void Fill(UnityEditor.SerializedProperty list, string codeField, params string[] codes)
        {
            list.arraySize = codes.Length;
            for (int i = 0; i < codes.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative(codeField).stringValue = codes[i];
                item.FindPropertyRelative("displayName").stringValue = codes[i] + " name";
            }
        }

        [Test]
        public void NewPlayer_ReadsDefaultsFromConfig()
        {
            var s = Service(musicDefault: false, sfxDefault: true);
            Assert.IsFalse(s.MusicOn);
            Assert.IsTrue(s.SfxOn);
            Assert.AreEqual(LangB, s.LanguageCode);
            Assert.AreEqual(FlagB, s.FlagCode);
            Assert.AreEqual(DefaultName, s.PlayerName);
            Assert.AreEqual(NameMax, s.MaxNameLength);
        }

        [Test]
        public void MusicAndSfx_AreSeparate_AndSavedAtOnce()
        {
            var s = Service();
            int writes = _kit.Store.Writes;
            s.SetMusic(false);
            Assert.IsFalse(s.MusicOn);
            Assert.IsTrue(s.SfxOn);
            Assert.AreEqual(writes + 1, _kit.Store.Writes);
            s.SetSfx(false);
            Assert.IsFalse(s.SfxOn);
            Assert.AreEqual(writes + 2, _kit.Store.Writes);
        }

        [Test]
        public void SettingTheSameValue_NeitherSavesNorRaisesChanged()
        {
            var s = Service();
            int changed = 0;
            s.Changed += () => changed++;
            int writes = _kit.Store.Writes;

            s.SetMusic(s.MusicOn);
            s.SetLanguage(s.LanguageCode);
            s.SetFlag(s.FlagCode);
            s.TrySetName(s.PlayerName);

            Assert.AreEqual(0, changed);
            Assert.AreEqual(writes, _kit.Store.Writes);
        }

        [Test]
        public void Changes_RaiseChanged_AndSurviveAReload()
        {
            var s = Service();
            int changed = 0;
            s.Changed += () => changed++;

            s.SetMusic(!s.MusicOn);
            Assert.IsTrue(s.SetLanguage(LangA));
            Assert.IsTrue(s.SetFlag(FlagA));
            Assert.AreEqual(PlayerNameResult.Valid, s.TrySetName("  Max "));
            Assert.AreEqual(4, changed);

            var reloaded = new ProgressRepository(_kit.Store, () => default);
            reloaded.Load();
            var again = new PlayerSettingsService(_config, reloaded);
            Assert.AreEqual(s.MusicOn, again.MusicOn);
            Assert.AreEqual(LangA, again.LanguageCode);
            Assert.AreEqual(FlagA, again.FlagCode);
            Assert.AreEqual("Max", again.PlayerName);
        }

        [Test]
        public void UnknownCodes_AreRefused_NothingChanges()
        {
            var s = Service();
            int writes = _kit.Store.Writes;
            Assert.IsFalse(s.SetLanguage("zz"));
            Assert.IsFalse(s.SetFlag("ZZ"));
            Assert.IsFalse(s.SetLanguage(string.Empty));
            Assert.AreEqual(LangB, s.LanguageCode);
            Assert.AreEqual(FlagB, s.FlagCode);
            Assert.AreEqual(writes, _kit.Store.Writes);
        }

        [Test]
        public void SavedCodeRemovedFromConfig_FallsBackToDefault()
        {
            var s = Service();
            s.SetLanguage(LangA);
            ConfigFixture.Edit(_config, so => Fill(ConfigFixture.Find(so, "languages"), "languageCode", LangB));
            Assert.AreEqual(LangB, s.LanguageCode);
        }

        [Test]
        public void BadName_IsRefused_WithTheReason_AndKeepsTheOldName()
        {
            var s = Service();
            Assert.AreEqual(PlayerNameResult.TooLong, s.TrySetName(new string('x', NameMax + 1)));
            Assert.AreEqual(PlayerNameResult.TooShort, s.TrySetName("x"));
            Assert.AreEqual(PlayerNameResult.InvalidCharacters, s.TrySetName("ab!"));
            Assert.AreEqual(DefaultName, s.PlayerName);
        }

        [Test]
        public void Lists_FollowTheConfigOrder()
        {
            var s = Service();
            var codes = new List<string>();
            foreach (FlagInfo f in s.Flags) codes.Add(f.Code);
            CollectionAssert.AreEqual(new[] { FlagA, FlagB }, codes);
            Assert.AreEqual(2, s.Languages.Count);
        }
    }
}
