using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SpeedStars.UI.Costume;
using SpeedStars.UI.Emotes;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    public sealed class WardrobeIntegrationTests
    {
        [UnityTest]
        public IEnumerator TabsStayInside_EmotesStayInline_BackgroundIsIsolated()
        {
            yield return LoadScene(HomeScene);
            var home = Require<HomeScreenView>();
            var resourceBar = Require<ResourceBarView>();
            var resourceRect = (RectTransform)resourceBar.transform;
            Vector2 homeResourcePosition = resourceRect.anchoredPosition;
            var wardrobe = RequireField<UiScreen>(home, "cosmeticsScreen") as CostumeEditorScreenView;
            var emotes = RequireField<UiScreen>(home, "emoteScreen") as EmoteScreenView;
            yield return Tap(RequireField<Button>(home, "editRacerButton"), "edit racer");
            yield return WaitOpen(wardrobe, "wardrobe");
            Assert.AreEqual(1f, resourceBar.GetComponent<CanvasGroup>().alpha);
            Assert.IsFalse(RequireField<CostumeResourceView>(wardrobe, "resources").gameObject.activeSelf,
                "Only the shared horizontal resource boxes should be shown.");
            var resourceCorners = new Vector3[4];
            resourceRect.GetWorldCorners(resourceCorners);
            var colorRect = (RectTransform)RequireField<CostumeColorPickerView>(wardrobe, "colorPicker").transform;
            var colorCorners = new Vector3[4];
            colorRect.GetWorldCorners(colorCorners);
            Assert.Less(resourceCorners[3].x, colorCorners[0].x, "Resources and color panel must have a clear gap.");
            var tabs = RequireField<CostumeTabView[]>(wardrobe, "tabs");
            var panel = wardrobe.transform.Find("Wardrobe/Panel") as RectTransform;
            var panelCorners = new Vector3[4];
            panel.GetWorldCorners(panelCorners);
            foreach (var tab in tabs)
            {
                var rect = (RectTransform)tab.transform;
                Assert.AreSame(panel, rect.parent, tab.Tab + " must be inside the panel.");
                Assert.GreaterOrEqual(rect.anchorMin.y, 0);
                Assert.LessOrEqual(rect.anchorMax.y, 1);
            }
            var dim = RequireField<GameObject>(wardrobe, "dimBackdrop");
            var isolation = dim.GetComponent<MenuBackdropIsolation>();
            Assert.IsNotNull(isolation);
            Assert.IsNotNull(isolation.ForegroundCamera);
            var dimCanvas = dim.GetComponent<Canvas>();
            Assert.Less(dimCanvas.planeDistance, dimCanvas.worldCamera.nearClipPlane + 0.1f);
            var platform = Object.FindObjectsOfType<Transform>().First(t => t.name == "Foreground Character Platform");
            foreach (var renderer in platform.GetComponentsInChildren<Renderer>())
                Assert.AreEqual(31, renderer.gameObject.layer);
            Assert.AreEqual(1 << 31, isolation.ForegroundCamera.cullingMask);
            var viewport = RequireField<Transform>(wardrobe, "itemsContent");
            var emoteTab = tabs.First(t => t.Tab == CostumeTab.Emote);
            yield return Tap(RequireField<Button>(emoteTab, "button"), "inline Emote");
            Assert.IsTrue(wardrobe.gameObject.activeInHierarchy);
            Assert.IsFalse(emotes.gameObject.activeSelf);
            Assert.AreSame(viewport, RequireField<Transform>(emotes, "itemsContent"));
            var cards = viewport.GetComponentsInChildren<EmoteCardView>();
            Assert.Greater(cards.Length, 0);
            yield return Tap(RequireField<Button>(cards[cards.Length - 1], "button"), "last emote");
            Assert.IsTrue(wardrobe.gameObject.activeInHierarchy);
            yield return Shoot("emote-inline");
            yield return Tap(RequireField<Button>(tabs.First(t => t.Tab == CostumeTab.Socks), "button"), "Socks");
            Assert.AreEqual(0, viewport.GetComponentsInChildren<EmoteCardView>().Length);
            Assert.Greater(viewport.GetComponentsInChildren<CostumeItemCardView>().Length, 0);
            yield return Shoot("socks");
            yield return Tap(CloseButtonOf(wardrobe), "Back");
            yield return WaitClosed(wardrobe, "wardrobe");
            Assert.AreEqual(homeResourcePosition, resourceRect.anchoredPosition, "Home resource layout must be restored.");
            Assert.AreEqual(1f, resourceBar.GetComponent<CanvasGroup>().alpha);
            Assert.IsFalse(dim.activeSelf);
            Assert.AreNotEqual(31, platform.gameObject.layer);
            yield return Tap(RequireField<Button>(home, "emoteButton"), "Home Emote");
            yield return WaitOpen(wardrobe, "wardrobe Emote");
            Assert.Greater(viewport.GetComponentsInChildren<EmoteCardView>().Length, 0);
            Assert.IsFalse(emotes.gameObject.activeSelf);
        }

        /// <summary>Review screenshot. Batchmode never reaches end of frame, so there it only lets a frame pass; the logic asserts stay.</summary>
        private static IEnumerator Shoot(string name)
        {
            if (Application.isBatchMode) { yield return null; yield break; }
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory("Temp/WardrobeReview");
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes("Temp/WardrobeReview/" + name + ".png", shot.EncodeToPNG());
            Object.Destroy(shot);
        }
    }
}
