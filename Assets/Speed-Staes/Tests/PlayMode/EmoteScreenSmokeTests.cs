using System.Collections;
using NUnit.Framework;
using SpeedStars.UI.Emotes;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// T-040: Home's Emote button opens the emote screen, a real (raycast) tap on emote cards selects them and shows exactly
    /// one action (Equip or the price button), Back returns Home. Nothing is bought or equipped, so the save is untouched;
    /// it is restored byte for byte anyway. No price or emote count is asserted.
    /// </summary>
    public sealed class EmoteScreenSmokeTests
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
        public IEnumerator EmoteButton_OpensScreen_CardsSelect_AndBackReturnsHome()
        {
            HomeScreenView home = Require<HomeScreenView>();
            UiScreen screen = RequireField<UiScreen>(home, "emoteScreen");
            Assert.IsFalse(screen.gameObject.activeSelf, "emoteScreen must start closed");

            yield return Tap(RequireField<Button>(home, "emoteButton"), "emoteButton");
            yield return WaitOpen(screen, "emoteScreen");
            yield return WaitClosed(home, "HomeScreen to step aside");

            Transform content = RequireField<Transform>(screen, "itemsContent");
            var cards = new System.Collections.Generic.List<EmoteCardView>();
            yield return WaitUntil(() =>
            {
                cards.Clear();
                foreach (EmoteCardView card in content.GetComponentsInChildren<EmoteCardView>(false)) cards.Add(card);
                return cards.Count > 0;
            }, "emote cards to appear");

            Button equip = RequireField<Button>(screen, "equipButton");
            Button purchase = RequireField<Button>(screen, "purchaseButton");

            // Tap the first and the last card (a free and, usually, a locked one): each becomes the selected frame and exactly
            // one of Equip / Purchase is up.
            EmoteCardView first = cards[0];
            EmoteCardView last = cards[cards.Count - 1];
            foreach (EmoteCardView card in new[] { first, last })
            {
                yield return Tap(RequireField<Button>(card, "button"), $"emote card '{card.EmoteId}'");
                Assert.IsTrue(RequireField<Image>(card, "selectedBackground").gameObject.activeSelf, $"card '{card.EmoteId}' must show the selected frame");
                Assert.AreNotEqual(equip.gameObject.activeSelf, purchase.gameObject.activeSelf, "exactly one of Equip / Purchase is shown");
            }

            yield return Tap(CloseButtonOf(screen), "emoteScreen Back");
            yield return WaitClosed(screen, "emoteScreen");
            yield return WaitOpen(home, "HomeScreen to come back");
        }
    }
}
