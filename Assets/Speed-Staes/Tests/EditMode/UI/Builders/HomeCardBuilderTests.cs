using System.Linq;
using NUnit.Framework;
using SpeedStars.UI.Home;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tests.UI.Builders
{
    /// <summary>
    /// The Home character card of T-097, read back from the built HomeScreen prefab: it grew to the left only, the old parts sit in a
    /// fixed-size Content pinned to the right edge, the store's icon strip and the player name exist and are bound. Red until the
    /// builders have run once after T-097.
    /// </summary>
    public sealed class HomeCardBuilderTests
    {
        private const string HomeScreen = "Assets/Speed-Staes/UI/Screens/HomeScreen.prefab";
        private const string Layout = "Assets/Speed-Staes/UI/Art/Home/layout.json";
        private const float CanvasHeight = 1080f;
        private const float MaxGrowthShare = 0.45f;
        private const float Tolerance = 0.05f;

        [System.Serializable]
        private sealed class LayoutFile
        {
            public int[] reference_size;
            public LayoutComponent[] components;
        }

        [System.Serializable]
        private sealed class LayoutComponent
        {
            public string name;
            public int[] rect;
        }

        [Test]
        public void Card_GrowsToTheLeftOnly_RightEdgeAndHeightUnchanged()
        {
            RectTransform card = Find("CharacterCard");
            LayoutFile file = JsonUtility.FromJson<LayoutFile>(System.IO.File.ReadAllText(Layout));
            LayoutComponent spot = file.components.First(c => c.name == "character_card");
            float scale = CanvasHeight / file.reference_size[1];
            float oldWidth = spot.rect[2] * scale;

            Assert.AreEqual(1f, card.anchorMin.x, 0.0001f, "anchored to the right edge");
            Assert.AreEqual(1f, card.anchorMax.x, 0.0001f);
            Assert.AreEqual(1f, card.pivot.x, 0.0001f, "pivot on the right edge: growth goes left");
            float expectedRight = -(file.reference_size[0] - (spot.rect[0] + spot.rect[2])) * scale;
            Assert.AreEqual(expectedRight, card.anchoredPosition.x, Tolerance, "right edge distance to the screen edge");
            Assert.AreEqual(spot.rect[3] * scale, card.sizeDelta.y, Tolerance, "height");
            Assert.Greater(card.sizeDelta.x, oldWidth + 1f, "the card must be wider");
            Assert.LessOrEqual(card.sizeDelta.x, oldWidth * (1f + MaxGrowthShare) + Tolerance, "growth capped");

            RectTransform content = Find("CharacterCard/Content");
            Assert.AreEqual(1f, content.anchorMin.x, 0.0001f);
            Assert.AreEqual(1f, content.anchorMax.x, 0.0001f);
            Assert.AreEqual(1f, content.pivot.x, 0.0001f);
            Assert.AreEqual(oldWidth, content.sizeDelta.x, Tolerance, "old parts keep the old canvas width");
            Assert.AreEqual(0f, content.sizeDelta.y, Tolerance, "full card height");
        }

        [Test]
        public void Background_IsNineSliced_WithABorder()
        {
            Component image = Find("CharacterCard/Background").GetComponent("Image");
            Assert.IsNotNull(image, "Background has no Image");
            var so = new SerializedObject(image);
            Assert.AreEqual(1, so.FindProperty("m_Type").enumValueIndex, "Image.Type.Sliced");
            var sprite = (Sprite)so.FindProperty("m_Sprite").objectReferenceValue;
            Assert.IsNotNull(sprite);
            Assert.Greater(sprite.border.x, 0f, "the sprite needs its 9-slice border (import setting)");
        }

        [Test]
        public void OldParts_LiveInContent()
        {
            foreach (string part in new[] { "Select", "Previous", "Next", "PowerBar", "StaminaBar", "HeightField", "RacerOfTheDay" })
                Find("CharacterCard/Content/" + part);
        }

        [Test]
        public void StatsStrip_HasStoreIconsAndNames()
        {
            foreach (string part in new[] { "PowerIcon", "StaminaIcon", "HeightIcon", "PowerLabel", "StaminaLabel" })
                Find("CharacterCard/StatsStrip/" + part);
            AssertText("CharacterCard/StatsStrip/PowerLabel", "Power");
            AssertText("CharacterCard/StatsStrip/StaminaLabel", "Stamina");
        }

        [Test]
        public void ButtonsAndPlayerName_ExistAndAreBound()
        {
            Find("CharacterCard/EditRacer");
            Find("CharacterCard/Emote");
            Find("CharacterCard/PlayerName");

            var view = Load().GetComponent<HomeScreenView>();
            Assert.IsNotNull(view, "HomeScreenView missing");
            var so = new SerializedObject(view);
            Assert.AreEqual("PlayerName", ((Component)so.FindProperty("playerNameText").objectReferenceValue)?.name, "playerNameText");
            Assert.AreEqual("EditRacer", ((Component)so.FindProperty("editRacerButton").objectReferenceValue)?.name, "editRacerButton");
            Assert.AreEqual("Emote", ((Component)so.FindProperty("emoteButton").objectReferenceValue)?.name, "emoteButton");
            Assert.AreEqual("Name", ((Component)so.FindProperty("racerNameText").objectReferenceValue)?.name, "racerNameText");
        }

        [Test]
        public void RaceOfTheDay_HidesTheNewParts()
        {
            var so = new SerializedObject(Load().GetComponent<HomeScreenView>());
            SerializedProperty hidden = so.FindProperty("hiddenForRacerOfTheDay");
            var names = Enumerable.Range(0, hidden.arraySize)
                .Select(i => ((GameObject)hidden.GetArrayElementAtIndex(i).objectReferenceValue)?.name).ToList();
            foreach (string part in new[] { "StatsStrip", "PlayerName", "EditRacer", "Emote", "PowerBar", "StaminaBar", "HeightField" })
                Assert.Contains(part, names, "hidden list lacks " + part);
            Assert.IsFalse(names.Contains(null), "an entry of the hidden list is not bound");
        }

        private static GameObject Load()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(HomeScreen);
            Assert.IsNotNull(go, HomeScreen + " not found");
            return go;
        }

        private static RectTransform Find(string path)
        {
            Transform t = Load().transform.Find(path);
            Assert.IsNotNull(t, "HomeScreen has no " + path);
            return (RectTransform)t;
        }

        private static void AssertText(string path, string expected)
        {
            Component text = Find(path).GetComponent("Text");
            Assert.IsNotNull(text, path + " has no Text");
            Assert.AreEqual(expected, new SerializedObject(text).FindProperty("m_Text").stringValue, path);
        }
    }
}
