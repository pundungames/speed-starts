using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.UI.Home;

namespace SpeedStars.Tests.UI.Home
{
    public sealed class EventSelectModelTests
    {
        private static readonly RaceEventId PlayableA = new RaceEventId("test_a");
        private static readonly RaceEventId LockedB = new RaceEventId("test_b");
        private static readonly RaceEventId LockedC = new RaceEventId("test_c");
        private static readonly RaceEventId PlayableD = new RaceEventId("test_d");
        private static readonly RaceEventId DayId = new RaceEventId("test_day");
        private static readonly RaceEventId PracticeId = new RaceEventId("test_practice");

        private static EventSelectEntry Entry(RaceEventId id, bool playable) => new EventSelectEntry(id, playable);

        /// <summary>Card 0: A (playable), B (locked), C (locked). Card 1: D (playable) only. Card 2: empty. Day locked, practice playable.</summary>
        private static EventSelectModel Build(bool dayPlayable = false, bool practicePlayable = true)
        {
            var categories = new List<IReadOnlyList<EventSelectEntry>>
            {
                new List<EventSelectEntry> { Entry(PlayableA, true), Entry(LockedB, false), Entry(LockedC, false) },
                new List<EventSelectEntry> { Entry(PlayableD, true) },
                new List<EventSelectEntry>()
            };
            return new EventSelectModel(categories, Entry(DayId, dayPlayable), Entry(PracticeId, practicePlayable));
        }

        [Test]
        public void StartsOnFirstCardFirstDistance()
        {
            EventSelectModel model = Build();

            Assert.AreEqual(EventSelectTarget.Category, model.Target);
            Assert.AreEqual(0, model.CategoryIndex);
            Assert.AreEqual(0, model.DistanceIndex);
            Assert.IsTrue(model.TryGetCurrent(out EventSelectEntry current));
            Assert.AreEqual(PlayableA, current.Id);
        }

        [Test]
        public void SelectCategory_PicksItsFirstDistance_AndReportsChange()
        {
            EventSelectModel model = Build();
            model.SelectDistance(0, 2);

            Assert.IsTrue(model.SelectCategory(1));
            Assert.AreEqual(1, model.CategoryIndex);
            Assert.AreEqual(0, model.DistanceIndex);
            Assert.IsFalse(model.SelectCategory(1), "the same card again changes nothing");
        }

        [Test]
        public void SelectCategory_OutOfRange_ChangesNothing()
        {
            EventSelectModel model = Build();

            Assert.IsFalse(model.SelectCategory(-1));
            Assert.IsFalse(model.SelectCategory(model.CategoryCount));
            Assert.AreEqual(0, model.CategoryIndex);
        }

        [Test]
        public void SelectDistance_OnlyWorksOnTheActiveCard()
        {
            EventSelectModel model = Build();

            Assert.IsFalse(model.SelectDistance(1, 0), "card 1 is not the active card");
            Assert.IsTrue(model.SelectDistance(0, 1));
            Assert.AreEqual(1, model.DistanceIndex);
            Assert.IsFalse(model.SelectDistance(0, 1), "same distance again changes nothing");
        }

        [Test]
        public void SelectDistance_BeyondTheCardsRaces_IsRefused()
        {
            EventSelectModel model = Build();
            model.SelectCategory(1);

            Assert.IsFalse(model.SelectDistance(1, 1));
            Assert.IsFalse(model.SelectDistance(1, -1));
            Assert.AreEqual(0, model.DistanceIndex);
        }

        [Test]
        public void DistanceCount_IsTheNumberOfRacesOfThatCard()
        {
            EventSelectModel model = Build();

            Assert.AreEqual(3, model.DistanceCount(0));
            Assert.AreEqual(1, model.DistanceCount(1));
            Assert.AreEqual(0, model.DistanceCount(2));
            Assert.AreEqual(0, model.DistanceCount(99));
        }

        [Test]
        public void CanContinue_FollowsPlayableOfTheSelectedRace()
        {
            EventSelectModel model = Build();
            Assert.IsTrue(model.CanContinue);

            model.SelectDistance(0, 1);
            Assert.IsFalse(model.CanContinue, "a locked race can be selected but not continued");

            model.SelectCategory(1);
            Assert.IsTrue(model.CanContinue);
        }

        [Test]
        public void EmptyCard_CannotContinue()
        {
            EventSelectModel model = Build();
            model.SelectCategory(2);

            Assert.IsFalse(model.TryGetCurrent(out _));
            Assert.IsFalse(model.CanContinue);
        }

        [Test]
        public void BottomButtons_AreSelectedLikeCards_AndUseTheirOwnPlayableFlag()
        {
            EventSelectModel model = Build(dayPlayable: false, practicePlayable: true);

            Assert.IsTrue(model.SelectRaceOfTheDay());
            Assert.AreEqual(EventSelectTarget.RaceOfTheDay, model.Target);
            Assert.IsFalse(model.CanContinue);
            Assert.IsFalse(model.SelectRaceOfTheDay(), "the same button again changes nothing");

            Assert.IsTrue(model.SelectPractice());
            Assert.AreEqual(EventSelectTarget.Practice, model.Target);
            Assert.IsTrue(model.CanContinue);
            Assert.IsTrue(model.TryGetCurrent(out EventSelectEntry current));
            Assert.AreEqual(PracticeId, current.Id);
        }

        [Test]
        public void PickingACardAfterABottomButton_LeavesTheBottomButton()
        {
            EventSelectModel model = Build();
            model.SelectPractice();

            Assert.IsTrue(model.SelectCategory(0));
            Assert.AreEqual(EventSelectTarget.Category, model.Target);
            Assert.AreEqual(PlayableA, model.EntryAt(model.CategoryIndex, model.DistanceIndex).Id);
        }

        [Test]
        public void SelectDistance_WhileABottomButtonIsSelected_IsRefused()
        {
            EventSelectModel model = Build();
            model.SelectPractice();

            Assert.IsFalse(model.SelectDistance(0, 1));
        }

        [Test]
        public void Restore_FindsTheRaceInAnyCard()
        {
            EventSelectModel model = Build();

            model.Restore(LockedC);
            Assert.AreEqual(EventSelectTarget.Category, model.Target);
            Assert.AreEqual(0, model.CategoryIndex);
            Assert.AreEqual(2, model.DistanceIndex);

            model.Restore(PlayableD);
            Assert.AreEqual(1, model.CategoryIndex);
            Assert.AreEqual(0, model.DistanceIndex);
        }

        [Test]
        public void Restore_FindsTheBottomButtons()
        {
            EventSelectModel model = Build();

            model.Restore(DayId);
            Assert.AreEqual(EventSelectTarget.RaceOfTheDay, model.Target);

            model.Restore(PracticeId);
            Assert.AreEqual(EventSelectTarget.Practice, model.Target);
        }

        [Test]
        public void Restore_UnknownOrEmptyId_KeepsTheSelection()
        {
            EventSelectModel model = Build();
            model.SelectCategory(1);

            model.Restore(new RaceEventId("not_listed"));
            model.Restore(default);

            Assert.AreEqual(EventSelectTarget.Category, model.Target);
            Assert.AreEqual(1, model.CategoryIndex);
        }

        [Test]
        public void MissingBottomEvents_CannotContinue()
        {
            var model = new EventSelectModel(new List<IReadOnlyList<EventSelectEntry>>(), default, default);
            model.SelectRaceOfTheDay();

            Assert.IsFalse(model.CanContinue);
            model.SelectPractice();
            Assert.IsFalse(model.CanContinue);
        }
    }
}
