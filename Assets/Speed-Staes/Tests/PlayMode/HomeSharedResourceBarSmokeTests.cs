using System.Collections;
using NUnit.Framework;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// T-067: Home has only the High Scores button in its bottom-left corner (Upgrade and Players are gone), and the one Home
    /// ResourceBar stays visible on the theme select and emote screens, which carry no resource boxes of their own.
    /// </summary>
    public sealed class HomeSharedResourceBarSmokeTests
    {
        private SaveFolderSnapshot _saveFiles;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveFiles = SaveFolderSnapshot.Take();
            yield return LoadScene(HomeScene);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _saveFiles.Restore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Home_HasNoUpgradeOrPlayersButton_AndHighScoresStillOpens()
        {
            HomeScreenView home = Require<HomeScreenView>();
            Assert.IsNull(home.transform.Find("Upgrade"), "Upgrade button must be gone");
            Assert.IsNull(home.transform.Find("Players"), "Players button must be gone");

            UiScreen screen = RequireField<UiScreen>(home, "highScoresScreen");
            yield return Tap(RequireField<Button>(home, "highScoresButton"), "highScoresButton");
            yield return WaitOpen(screen, "highScoresScreen");
        }

        [UnityTest]
        public IEnumerator ThemeSelect_KeepsTheHomeResourceBar_WithoutItsOwnBoxes()
        {
            yield return OpenAndCheck("environmentButton", "themeSelectScreen");
        }

        [UnityTest]
        public IEnumerator Emote_KeepsTheHomeResourceBar_WithoutItsOwnBoxes()
        {
            yield return OpenAndCheck("emoteButton", "emoteScreen");
        }

        private static IEnumerator OpenAndCheck(string buttonField, string screenField)
        {
            HomeScreenView home = Require<HomeScreenView>();
            ResourceBarView bar = Require<ResourceBarView>();
            CanvasGroup group = bar.GetComponent<CanvasGroup>();
            Assert.AreEqual(1f, group.alpha, 0.001f, "bar visible on Home");

            UiScreen screen = RequireField<UiScreen>(home, screenField);
            yield return Tap(RequireField<Button>(home, buttonField), buttonField);
            yield return WaitOpen(screen, screenField);
            yield return WaitClosed(home, "HomeScreen to step aside");

            Assert.AreEqual(1f, group.alpha, 0.001f, $"the same resource bar must stay visible on {screenField}");
            Assert.IsNull(screen.transform.Find("Cash"), $"{screenField} must not carry its own Cash box");
            Assert.IsNull(screen.transform.Find("PurpleGem"), $"{screenField} must not carry its own PurpleGem box");

            yield return Tap(CloseButtonOf(screen), $"{screenField} Back");
            yield return WaitClosed(screen, screenField);
            yield return WaitOpen(home, "HomeScreen to come back");
        }
    }
}
