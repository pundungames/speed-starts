using System.Collections;
using NUnit.Framework;
using SpeedStars.UI.Emotes;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using SpeedStars.UI.Costume;
using TMPro;
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
            var wardrobe = RequireField<UiScreen>(home, "cosmeticsScreen") as CostumeEditorScreenView;
            Assert.IsNotNull(wardrobe);
            Assert.IsFalse(screen.gameObject.activeSelf, "emoteScreen must start closed");

            yield return Tap(RequireField<Button>(home, "emoteButton"), "emoteButton");
            yield return WaitOpen(wardrobe, "wardrobe Emote tab");
            Assert.IsFalse(screen.gameObject.activeSelf, "Standalone emote screen must stay inactive.");
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
                yield return ScrollIntoView(card);
                yield return Tap(RequireField<Button>(card, "button"), $"emote card '{card.EmoteId}'");
                Assert.IsTrue(RequireField<Image>(card, "selectedBackground").gameObject.activeSelf, $"card '{card.EmoteId}' must show the selected frame");
                Assert.AreNotEqual(equip.gameObject.activeSelf, purchase.gameObject.activeSelf, "exactly one of Equip / Purchase is shown");
            }

            yield return Tap(CloseButtonOf(wardrobe), "wardrobe Back");
            yield return WaitClosed(wardrobe, "wardrobe");
            yield return WaitOpen(home, "HomeScreen to come back");
        }

        /// <summary>
        /// T-105: ten quick taps hopping between two emote cards (ButtonFeedback punches on press, the card punches on select: two
        /// owners of one scale). Afterwards both cards sit at their designed scale and no frame went far past a single punch.
        /// The designed scale is read from the card before the taps; the punch size from UiMotionConfig.
        /// </summary>
        [UnityTest]
        public IEnumerator EmoteCards_TenQuickTaps_LeaveTheDesignedScale_AndNeverBalloon()
        {
            HomeScreenView home = Require<HomeScreenView>();
            UiScreen screen = RequireField<UiScreen>(home, "emoteScreen");
            var wardrobe = RequireField<UiScreen>(home, "cosmeticsScreen") as CostumeEditorScreenView;
            Assert.IsNotNull(wardrobe);

            yield return Tap(RequireField<Button>(home, "emoteButton"), "emoteButton");
            yield return WaitOpen(wardrobe, "wardrobe Emote tab");

            Transform content = RequireField<Transform>(screen, "itemsContent");
            var cards = new System.Collections.Generic.List<EmoteCardView>();
            yield return WaitUntil(() =>
            {
                cards.Clear();
                foreach (EmoteCardView card in content.GetComponentsInChildren<EmoteCardView>(false)) cards.Add(card);
                return cards.Count > 1;
            }, "emote cards to appear");

            EmoteCardView a = cards[0];
            EmoteCardView b = cards[1];
            yield return ScrollIntoView(a);
            yield return WaitScaleSettled(a.transform, "first card");
            yield return WaitScaleSettled(b.transform, "second card");
            Vector3 designedA = a.transform.localScale;
            Vector3 designedB = b.transform.localScale;

            ButtonFeedback feedback = a.GetComponent<ButtonFeedback>();
            Assert.IsNotNull(feedback, "the emote card must carry ButtonFeedback (press punch)");
            float limit = 1f + MotionOf(feedback).ButtonPunchScale;

            float peak = 0f;
            for (int i = 0; i < 10; i++)
            {
                QuickTap((i % 2 == 0 ? a : b).gameObject);
                yield return null;
                peak = Mathf.Max(peak, a.transform.localScale.x / designedA.x, b.transform.localScale.x / designedB.x);
            }
            Assert.LessOrEqual(peak, limit * 1.1f, "a card swelled past a single punch");

            yield return new WaitForSecondsRealtime(MotionOf(feedback).ButtonPunchDuration + 0.2f);
            Assert.Less((a.transform.localScale - designedA).magnitude, 1e-3f, "first card must end at its designed scale");
            Assert.Less((b.transform.localScale - designedB).magnitude, 1e-3f, "second card must end at its designed scale");

            yield return Tap(CloseButtonOf(wardrobe), "wardrobe Back");
            yield return WaitClosed(wardrobe, "wardrobe");
        }

        /// <summary>
        /// T-089: the list opens on "None" (free, equipped, no price), a real tap on a locked card shows the price button, and a real tap
        /// on it is handled either way: bought (lock gone, Equip offered) or refused for lack of Cash (still locked, still for sale).
        /// Which one depends on the save, so both are accepted; the card and the buttons must agree with each other.
        /// </summary>
        [UnityTest]
        public IEnumerator BaseCardIsNone_AndATapOnTheBuyButtonIsHandled()
        {
            HomeScreenView home = Require<HomeScreenView>();
            UiScreen screen = RequireField<UiScreen>(home, "emoteScreen");
            var wardrobe = RequireField<UiScreen>(home, "cosmeticsScreen") as CostumeEditorScreenView;
            Assert.IsNotNull(wardrobe);

            yield return Tap(RequireField<Button>(home, "emoteButton"), "emoteButton");
            yield return WaitOpen(wardrobe, "wardrobe Emote tab");

            Transform content = RequireField<Transform>(screen, "itemsContent");
            var cards = new System.Collections.Generic.List<EmoteCardView>();
            yield return WaitUntil(() =>
            {
                cards.Clear();
                foreach (EmoteCardView card in content.GetComponentsInChildren<EmoteCardView>(false)) cards.Add(card);
                return cards.Count > 1;
            }, "emote cards to appear");

            EmoteCardView none = cards[0];
            Assert.IsFalse(RequireField<GameObject>(none, "lockOverlay").activeSelf, "None is free: never locked");
            Assert.IsFalse(RequireField<TMP_Text>(none, "priceText").gameObject.activeSelf, "None shows no price");

            Button purchase = RequireField<Button>(screen, "purchaseButton");
            Button equip = RequireField<Button>(screen, "equipButton");
            EmoteCardView target = null;
            foreach (EmoteCardView card in cards)
                if (RequireField<GameObject>(card, "lockOverlay").activeSelf) target = card;
            if (target == null) yield break; // everything owned in this save: nothing to buy

            yield return ScrollIntoView(target);
            yield return Tap(RequireField<Button>(target, "button"), $"locked emote card '{target.EmoteId}'");
            Assert.IsTrue(purchase.gameObject.activeSelf, "a locked card offers the price button");
            Assert.IsFalse(equip.gameObject.activeSelf);

            yield return Tap(purchase, "emote purchase");
            bool locked = RequireField<GameObject>(target, "lockOverlay").activeSelf;
            Assert.AreEqual(locked, purchase.gameObject.activeSelf, "the card and the buttons agree (bought: Equip, refused: price button)");
            Assert.AreEqual(!locked, equip.gameObject.activeSelf);

            yield return Tap(CloseButtonOf(wardrobe), "wardrobe Back");
            yield return WaitClosed(wardrobe, "wardrobe");
        }
    }
}
