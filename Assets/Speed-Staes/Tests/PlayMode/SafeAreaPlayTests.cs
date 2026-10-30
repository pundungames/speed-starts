using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using SpeedStars.UI.Race;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>Pretends a camera hole on the left edge by giving every SafeAreaFitter in the loaded scene a fixed safe area.</summary>
    internal static class SafeAreaTestSupport
    {
        /// <summary>Share of the screen width the fake notch takes on the left (120 px of the 2400 px reference width).</summary>
        public const float LeftFraction = 0.05f;
        public const float EdgeTolerancePx = 1f;

        public static FixedSafeAreaSource LeftNotch()
        {
            var screen = new Vector2(Screen.width, Screen.height);
            float left = Mathf.Round(screen.x * LeftFraction);
            return new FixedSafeAreaSource(screen, new Rect(left, 0f, screen.x - left, screen.y));
        }

        public static List<SafeAreaFitter> Install(ISafeAreaSource source)
        {
            var fitters = new List<SafeAreaFitter>(UnityEngine.Object.FindObjectsOfType<SafeAreaFitter>(true));
            Assert.IsNotEmpty(fitters, "No SafeAreaFitter in the scene: rebuild the Home/Race scenes (T-051 needs SCENE).");
            foreach (SafeAreaFitter fitter in fitters) fitter.SetSource(source);
            return fitters;
        }

        /// <summary>Screen-pixel x range of a rect on an overlay canvas (world corners are pixels there).</summary>
        public static void XRange(RectTransform rect, out float xMin, out float xMax)
        {
            Canvas root = rect.GetComponentInParent<Canvas>().rootCanvas;
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, root.renderMode, $"{rect.name}: the test reads pixels from an overlay canvas.");
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            xMin = Mathf.Min(corners[0].x, corners[2].x);
            xMax = Mathf.Max(corners[0].x, corners[2].x);
        }

        public static void AssertInside(RectTransform rect, FixedSafeAreaSource source, string what)
        {
            Assert.IsNotNull(rect, $"{what}: not bound.");
            XRange(rect, out float xMin, out float xMax);
            Assert.GreaterOrEqual(xMin, source.SafeArea.xMin - EdgeTolerancePx, $"{what} reaches into the notch (left edge {xMin}px).");
            Assert.LessOrEqual(xMax, source.SafeArea.xMax + EdgeTolerancePx, $"{what} reaches past the safe area (right edge {xMax}px).");
        }

        /// <summary>Backgrounds and dims (stretched, drawing) must still cover the whole screen, notch included.</summary>
        public static int AssertBleedsStayFull(IEnumerable<SafeAreaFitter> fitters, ISafeAreaSource source)
        {
            int checkedCount = 0;
            foreach (SafeAreaFitter fitter in fitters)
            {
                if (fitter.Mode != SafeAreaMode.Children) continue;
                foreach (Transform child in fitter.transform)
                {
                    var rect = child as RectTransform;
                    if (rect == null || rect.GetComponent<Graphic>() == null) continue;
                    if (!SafeAreaMath.IsFullStretch(rect.anchorMin, rect.anchorMax)) continue;
                    XRange(rect, out float xMin, out float xMax);
                    Assert.LessOrEqual(xMin, EdgeTolerancePx, $"{fitter.name}/{rect.name} no longer reaches the left screen edge.");
                    Assert.GreaterOrEqual(xMax, source.ScreenSize.x - EdgeTolerancePx, $"{fitter.name}/{rect.name} no longer reaches the right screen edge.");
                    checkedCount++;
                }
            }
            return checkedCount;
        }

        public static IEnumerator Settle(float seconds = 1.2f)
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < seconds) yield return null;
        }
    }

    /// <summary>
    /// T-051: with a fake camera hole on the left edge, edge-anchored UI of the real Home and Race scenes stays inside the safe
    /// area while dims and backgrounds still fill the whole screen. Button positions are compared with the injected rectangle,
    /// no layout number is asserted.
    /// </summary>
    public sealed class SafeAreaPlayTests
    {
        private SaveFolderSnapshot _saveFiles;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveFiles = SaveFolderSnapshot.Take();
            yield break;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _saveFiles.Restore();
            yield break;
        }

        [UnityTest]
        public IEnumerator Home_EdgeItemsStayInsideTheSafeArea_AndBackgroundsStayFullScreen()
        {
            yield return LoadScene(HomeScene);
            FixedSafeAreaSource notch = SafeAreaTestSupport.LeftNotch();
            List<SafeAreaFitter> fitters = SafeAreaTestSupport.Install(notch);
            yield return SafeAreaTestSupport.Settle();

            HomeScreenView home = Require<HomeScreenView>();
            ResourceBarView bar = Require<ResourceBarView>();
            SafeAreaTestSupport.AssertInside((RectTransform)bar.transform, notch, "Resource bar");
            foreach (string field in new[] { "playButton", "settingsButton", "soundButton", "environmentButton", "highScoresButton", "sevenDayButton", "editRacerButton" })
            {
                Button button = Field<Button>(home, field);
                if (button != null) SafeAreaTestSupport.AssertInside((RectTransform)button.transform, notch, "Home " + field);
            }

            Assert.Greater(SafeAreaTestSupport.AssertBleedsStayFull(fitters, notch), 0, "No full-screen dim/background found to check.");
        }

        [UnityTest]
        public IEnumerator Race_HudItemsStayInsideTheSafeArea()
        {
            yield return LoadScene(RaceScene);
            FixedSafeAreaSource notch = SafeAreaTestSupport.LeftNotch();
            List<SafeAreaFitter> fitters = SafeAreaTestSupport.Install(notch);
            yield return SafeAreaTestSupport.Settle();

            RaceHudView hud = Require<RaceHudView>();
            foreach (string field in new[] { "exitButton", "restartButton", "cashPlusButton" })
            {
                Button button = Field<Button>(hud, field);
                if (button != null) SafeAreaTestSupport.AssertInside((RectTransform)button.transform, notch, "Race HUD " + field);
            }
            // The left-most HUD block (telemetry, top-left) is a direct child of the stretched HUD root.
            Transform telemetry = hud.transform.Find("Telemetry");
            Assert.IsNotNull(telemetry, "RaceHud/Telemetry is missing.");
            SafeAreaTestSupport.AssertInside((RectTransform)telemetry, notch, "Race HUD telemetry");

            SafeAreaTestSupport.AssertBleedsStayFull(fitters, notch);
        }

        [UnityTest]
        public IEnumerator Home_WithoutANotch_KeepsTheAuthoredAnchors()
        {
            yield return LoadScene(HomeScene);
            HomeScreenView home = Require<HomeScreenView>();
            Button play = RequireField<Button>(home, "playButton");
            var rect = (RectTransform)play.transform;
            yield return SafeAreaTestSupport.Settle();
            Vector2 min = rect.anchorMin;
            Vector2 max = rect.anchorMax;

            var full = new FixedSafeAreaSource(new Vector2(Screen.width, Screen.height), new Rect(0f, 0f, Screen.width, Screen.height));
            SafeAreaTestSupport.Install(full);
            yield return null;

            Assert.AreEqual(min, rect.anchorMin, "A full-screen safe area must not move anything.");
            Assert.AreEqual(max, rect.anchorMax);
        }
    }
}
