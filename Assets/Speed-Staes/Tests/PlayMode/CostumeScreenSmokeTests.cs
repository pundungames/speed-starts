using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Costume;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// T-066: on the costume screen the Body tab shows skin tones and a real (raycast) tap on one recolours the menu runner
    /// (read from its renderer's MaterialPropertyBlock); the Emote tab opens the emote screen and Back returns with the unsaved
    /// skin still on the runner; the preset drop-down lists every preset and the last one can be reached and chosen. The save
    /// is restored byte for byte. No colour or count is hard-coded except the nine presets the task asks for.
    /// </summary>
    public sealed class CostumeScreenSmokeTests
    {
        private const string SkinRendererName = "LowPoly Male"; // the runner's skin mesh (T-065); the colour is on material slot 0
        private const string ColorProperty = "_Color";
        private const int WantedPresets = 9;

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
        public IEnumerator BodyTab_SkinTone_RecoloursRunner_AndEmoteTabRoundTrips()
        {
            HomeScreenView home = Require<HomeScreenView>();
            UiScreen screen = RequireField<UiScreen>(home, "cosmeticsScreen");
            yield return Tap(RequireField<Button>(home, "editRacerButton"), "editRacerButton");
            yield return WaitOpen(screen, "cosmeticsScreen");
            yield return WaitClosed(home, "HomeScreen to step aside");

            Renderer skin = FindRunnerSkin();
            Color before = SkinColorOf(skin);

            // Body tab: swatches appear in the item grid.
            yield return Tap(RequireField<Button>(TabOf(screen, CostumeTab.Body), "button"), "Body tab");
            Transform content = RequireField<Transform>(screen, "itemsContent");
            var swatches = new List<CostumeSkinSwatchView>();
            yield return WaitUntil(() =>
            {
                swatches.Clear();
                foreach (CostumeSkinSwatchView swatch in content.GetComponentsInChildren<CostumeSkinSwatchView>(false)) swatches.Add(swatch);
                return swatches.Count > 0;
            }, "skin swatches to appear");

            // A tone that differs from the runner's current skin, so the change is visible in the renderer.
            CostumeSkinSwatchView target = null;
            Color wanted = Color.clear;
            foreach (CostumeSkinSwatchView swatch in swatches)
            {
                Color tone = RequireField<Image>(swatch, "colorImage").color;
                if (Near(tone, before)) continue;
                target = swatch;
                wanted = tone;
                break;
            }
            Assert.IsNotNull(target, "no skin swatch differs from the runner's current skin colour");

            yield return Tap(RequireField<Button>(target, "button"), "skin swatch");
            yield return WaitUntil(() => Near(SkinColorOf(skin), wanted), "the runner's skin to take the tapped tone");
            Assert.IsTrue(RequireField<GameObject>(target, "check").activeSelf, "the tapped swatch shows its tick");

            // Emote tab: the emote screen opens over the costume screen; Back brings the costume screen back with the unsaved skin.
            UiScreen emote = RequireField<UiScreen>(screen, "emoteScreen");
            Assert.IsFalse(emote.gameObject.activeSelf, "emoteScreen must start closed");
            yield return Tap(RequireField<Button>(TabOf(screen, CostumeTab.Emote), "button"), "Emote tab");
            yield return WaitOpen(emote, "emoteScreen");
            yield return WaitClosed(screen, "cosmeticsScreen to step aside");

            yield return Tap(CloseButtonOf(emote), "emoteScreen Back");
            yield return WaitClosed(emote, "emoteScreen");
            yield return WaitOpen(screen, "cosmeticsScreen to come back");
            yield return WaitUntil(() => Near(SkinColorOf(skin), wanted), "the unsaved skin to be shown again after the emote screen");

            yield return Tap(CloseButtonOf(screen), "cosmeticsScreen Back");
            yield return WaitClosed(screen, "cosmeticsScreen");
            yield return WaitOpen(home, "HomeScreen to come back");
        }

        [UnityTest]
        public IEnumerator PresetDropdown_ListsEveryPreset_AndTheLastOneCanBeChosen()
        {
            ICosmeticService cosmetics = ProjectContext.Instance.Container.Resolve<ICosmeticService>();
            Assert.GreaterOrEqual(cosmetics.PresetCount, WantedPresets, "CosmeticCatalog.presetCount must be 9 (T-065 ASSET need)");

            HomeScreenView home = Require<HomeScreenView>();
            UiScreen screen = RequireField<UiScreen>(home, "cosmeticsScreen");
            yield return Tap(RequireField<Button>(home, "editRacerButton"), "editRacerButton");
            yield return WaitOpen(screen, "cosmeticsScreen");

            CostumePresetDropdownView dropdown = RequireField<CostumePresetDropdownView>(screen, "presetDropdown");
            yield return Tap(RequireField<Button>(dropdown, "bar"), "preset bar");

            Transform rowsParent = RequireField<Transform>(dropdown, "rowsParent");
            var rows = new List<CostumePresetRowView>();
            yield return WaitUntil(() =>
            {
                rows.Clear();
                foreach (CostumePresetRowView row in rowsParent.GetComponentsInChildren<CostumePresetRowView>(false)) rows.Add(row);
                return rows.Count > 0;
            }, "preset rows to appear");
            Assert.AreEqual(cosmetics.PresetCount, rows.Count, "one row per preset");

            // The list scrolls: bring the last row into the window, then press it for real.
            ScrollRect listScroll = RequireField<ScrollRect>(dropdown, "listScroll");
            listScroll.verticalNormalizedPosition = 0f;
            yield return null;
            CostumePresetRowView last = rows[rows.Count - 1];
            yield return Tap(RequireField<Button>(last, "button"), "last preset row");

            int lastIndex = cosmetics.PresetCount - 1;
            yield return WaitUntil(() => cosmetics.SelectedPreset == lastIndex, "the last preset to be chosen");
            Assert.AreEqual(cosmetics.GetPresetName(lastIndex), RequireField<Text>(dropdown, "barLabel").text);
            Assert.IsFalse(RequireField<GameObject>(dropdown, "list").activeSelf, "the list closes after a choice");

            yield return Tap(CloseButtonOf(screen), "cosmeticsScreen Back");
            yield return WaitClosed(screen, "cosmeticsScreen");
        }

        private static CostumeTabView TabOf(UiScreen screen, CostumeTab tab)
        {
            foreach (CostumeTabView view in RequireField<CostumeTabView[]>(screen, "tabs"))
                if (view != null && view.Tab == tab) return view;
            Assert.Fail($"The costume screen has no '{tab}' tab view bound.");
            return null;
        }

        /// <summary>The menu runner's skin mesh: the active renderer of that name (the race runner is in another scene).</summary>
        private static Renderer FindRunnerSkin()
        {
            foreach (Renderer renderer in Object.FindObjectsOfType<Renderer>(true))
                if (renderer.name == SkinRendererName && renderer.gameObject.activeInHierarchy) return renderer;
            Assert.Fail($"No active renderer named '{SkinRendererName}' (the menu runner's skin) in the Home scene.");
            return null;
        }

        /// <summary>The colour the renderer draws on its skin slot: the property block when there is one, else the shared material.</summary>
        private static Color SkinColorOf(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, 0);
            if (!block.isEmpty) return block.GetColor(ColorProperty);
            return renderer.sharedMaterials[0].GetColor(ColorProperty);
        }

        private static bool Near(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
        }
    }
}
