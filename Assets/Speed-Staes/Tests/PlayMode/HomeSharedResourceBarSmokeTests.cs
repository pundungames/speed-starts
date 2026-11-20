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
        public IEnumerator ThemeSelect_PatrolContinuesAcrossThemes_AndReturnsToHomePose()
        {
            yield return new WaitForSecondsRealtime(SpeedStars.UI.Flow.UnitySceneLoader.DepartureDuration + 0.1f);
            Transform camera = Camera.main.transform;
            Vector3 menuPosition = camera.position;
            Quaternion menuRotation = camera.rotation;
            HomeScreenView home = Require<HomeScreenView>();
            var screen = RequireField<UiScreen>(home, "themeSelectScreen");
            yield return Tap(RequireField<Button>(home, "environmentButton"), "environment button");
            yield return WaitOpen(screen, "theme shop");
            yield return WaitUntil(() => menuPosition.z - camera.position.z >= 12f, "camera to move along the track");
            Assert.Less(camera.forward.z, -0.15f, "camera looks backward during the first leg");

            var themes = Zenject.ProjectContext.Instance.Container.Resolve<SpeedStars.Meta.Contracts.IThemeService>();
            Vector3 beforeSwitch = camera.position;
            Quaternion rotationBeforeSwitch = camera.rotation;
            Assert.IsTrue(themes.Step(themes.CanStep(1) ? 1 : -1));
            Assert.Less(Vector3.Distance(beforeSwitch, camera.position), 0.01f);
            Assert.Less(Quaternion.Angle(rotationBeforeSwitch, camera.rotation), 0.1f);
            yield return WaitUntil(() => beforeSwitch.z - camera.position.z >= 8f, "camera to continue in the same direction");

            yield return WaitUntil(() => menuPosition.z - camera.position.z >= 50f, "camera to approach the backward patrol limit", 16f);
            yield return WaitUntil(() => menuPosition.z - camera.position.z <= 45f, "camera to turn back", 12f);
            Assert.Greater(camera.forward.z, 0.1f, "camera looks forward after turning back");
            yield return WaitUntil(() => camera.position.z - menuPosition.z >= 20f, "camera to pass Home and approach the forward limit", 26f);
            Assert.Greater(Quaternion.Angle(menuRotation, camera.rotation), 20f, "camera must turn into the scenery");
            yield return Tap(CloseButtonOf(screen), "theme shop Back");
            yield return WaitUntil(() => !themes.IsBrowsing, "theme shop preview to end");
            Assert.Greater(Vector3.Distance(menuPosition, camera.position), 10f, "camera must not snap to Home");
            yield return new WaitForSecondsRealtime(0.45f);
            Assert.Greater(Vector3.Distance(menuPosition, camera.position), 2f, "camera is still easing back");
            yield return WaitUntil(() => Vector3.Distance(menuPosition, camera.position) < 0.01f, "camera to reach the menu pose", 1f);
            Assert.Less(Quaternion.Angle(menuRotation, camera.rotation), 0.1f);
        }

        [UnityTest]
        public IEnumerator Emote_KeepsTheHomeResourceBar_WithoutItsOwnBoxes()
        {
            yield return OpenAndCheck("emoteButton", "cosmeticsScreen");
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
