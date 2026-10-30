using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
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
    /// Settings window reset rows on the real Home scene, pressed the way a finger does (a real raycast through Tap):
    /// Reset Personal Bests opens the question, Cancel keeps the record, the yes deletes it and closes the question.
    /// The record the test plants and the records it finds are put back afterwards.
    /// </summary>
    public sealed class SettingsResetSmokeTests
    {
        private SaveFolderSnapshot _saveFiles;
        private List<EventBestTime> _bests;
        private List<DayBestEntry> _dayBests;
        private bool _captured;

        private static DiContainer Container => ProjectContext.Instance.Container;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveFiles = SaveFolderSnapshot.Take();
            _captured = false;
            yield return LoadScene(HomeScene);

            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            _bests = new List<EventBestTime>(progress.BestTimes);
            _dayBests = new List<DayBestEntry>(progress.DayBests);
            _captured = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance && _captured)
            {
                PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
                progress.BestTimes.Clear();
                progress.BestTimes.AddRange(_bests);
                progress.DayBests.Clear();
                progress.DayBests.AddRange(_dayBests);
            }
            _saveFiles.Restore();
            yield break;
        }

        [UnityTest]
        public IEnumerator ResetPersonalBests_CancelKeepsTheRecord_YesDeletesIt()
        {
            var plantedEvent = new RaceEventId("settings_reset_smoke");
            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            progress.BestTimes.Add(new EventBestTime(plantedEvent, 12.34));
            IPersonalBests bests = Container.Resolve<IPersonalBests>();
            Assert.IsTrue(bests.TryGetBest(plantedEvent, out _), "the planted record must be readable");

            HomeScreenView home = Require<HomeScreenView>();
            yield return WaitUntil(() => home.IsVisible, "HomeScreen to be visible");
            UiPopup settings = RequireField<UiPopup>(home, "settingsPopup");
            yield return Tap(RequireField<Button>(home, "settingsButton"), "settingsButton");
            yield return WaitOpen(settings, "settings popup");

            MonoBehaviour row = RequireField<MonoBehaviour>(settings, "resetBestsRow");
            MonoBehaviour dialog = RequireField<MonoBehaviour>(settings, "confirmDialog");
            Assert.IsFalse(dialog.gameObject.activeSelf, "the question starts closed");

            // Cancel: the question closes, the record stays.
            yield return ScrollIntoView(row);
            yield return Tap(RequireField<Button>(row, "button"), "Reset Personal Bests row");
            yield return WaitUntil(() => dialog.gameObject.activeSelf, "the question to open");
            yield return Tap(RequireField<Button>(dialog, "cancelButton"), "question Cancel");
            yield return WaitUntil(() => !dialog.gameObject.activeSelf, "the question to close after Cancel");
            Assert.IsTrue(bests.TryGetBest(plantedEvent, out _), "Cancel must keep the record");

            // Yes: the question closes, the record is gone, the settings window stays.
            yield return ScrollIntoView(row);
            yield return Tap(RequireField<Button>(row, "button"), "Reset Personal Bests row");
            yield return WaitUntil(() => dialog.gameObject.activeSelf, "the question to open again");
            yield return Tap(RequireField<Button>(dialog, "confirmButton"), "question RESET");
            yield return WaitUntil(() => !dialog.gameObject.activeSelf, "the question to close after RESET");
            Assert.IsFalse(bests.TryGetBest(plantedEvent, out _), "RESET must delete the record");
            Assert.IsTrue(settings.gameObject.activeInHierarchy, "the settings window stays open after the record reset");

            yield return Tap(CloseButtonOf(settings), "settings X");
            yield return WaitClosed(settings, "settings popup");
        }

        /// <summary>The reset rows sit below the fold of the settings ScrollRect, as for a player: scroll them into view first.</summary>
        private static System.Collections.IEnumerator ScrollIntoView(Component row)
        {
            ScrollRect scroll = row.GetComponentInParent<ScrollRect>();
            if (scroll != null)
            {
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 0f;
                scroll.velocity = Vector2.zero;
            }
            yield return null;
            yield return null;
        }
    }
}
