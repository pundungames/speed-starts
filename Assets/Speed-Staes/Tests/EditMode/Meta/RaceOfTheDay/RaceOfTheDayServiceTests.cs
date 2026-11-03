using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.DailyRace;
using SpeedStars.Meta.Leaderboard;
using SpeedStars.Tests.Meta.Tickets;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tests.Meta.RaceOfTheDay
{
    /// <summary>Race of the Day from a fake clock: one distance and one outfit per UTC day, the same for every launch (T-087).</summary>
    public sealed class RaceOfTheDayServiceTests
    {
        private static readonly float[] Distances = { 60f, 100f, 150f, 200f, 300f };

        private FakeClock _clock;
        private LeaderboardConfig _field;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock { UtcNow = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc) };
            _field = ScriptableObject.CreateInstance<LeaderboardConfig>();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_field);

        private static List<RaceOfTheDayLook> Looks()
        {
            var looks = new List<RaceOfTheDayLook>();
            string[] tops = { "tops.t01", "tops.t02", "tops.t03" };
            for (int i = 0; i < tops.Length; i++)
                looks.Add(new RaceOfTheDayLook("look " + i,
                    new[] { new CosmeticOutfitEntry(CosmeticCategory.Tops, tops[i], "E53935"), new CosmeticOutfitEntry(CosmeticCategory.Shoes, "shoes.sh01", "") },
                    "E0AC69"));
            return looks;
        }

        private RaceOfTheDayService Make(IReadOnlyList<float> distances = null, IReadOnlyList<RaceOfTheDayLook> looks = null, bool withField = true) =>
            new RaceOfTheDayService(_clock, "race_of_the_day", distances ?? Distances, looks ?? Looks(), withField ? _field : null);

        [Test]
        public void SameUtcDay_GivesTheSameDistanceAndOutfitAtEveryTime()
        {
            RaceOfTheDayService service = Make();
            _clock.UtcNow = new DateTime(2026, 11, 3, 0, 0, 5, DateTimeKind.Utc);
            RaceOfTheDayInfo morning = service.Today;
            _clock.UtcNow = new DateTime(2026, 11, 3, 23, 59, 55, DateTimeKind.Utc);
            RaceOfTheDayInfo night = service.Today;

            Assert.AreEqual(morning.DayKey, night.DayKey);
            Assert.AreEqual(morning.DistanceMeters, night.DistanceMeters);
            Assert.AreEqual(morning.LookLabel, night.LookLabel);
            Assert.IsTrue(morning.Outfit.Equals(night.Outfit));
        }

        [Test]
        public void TheNextDay_ChangesBothTheDistanceAndTheOutfit()
        {
            RaceOfTheDayService service = Make();
            for (int d = 0; d < 60; d++)
            {
                _clock.UtcNow = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc).AddDays(d);
                RaceOfTheDayInfo today = service.Today;
                _clock.UtcNow = _clock.UtcNow.AddDays(1);
                RaceOfTheDayInfo tomorrow = service.Today;

                Assert.AreNotEqual(today.DistanceMeters, tomorrow.DistanceMeters, $"distance, day +{d}");
                Assert.AreNotEqual(today.LookLabel, tomorrow.LookLabel, $"look, day +{d}");
                Assert.IsFalse(today.Outfit.Equals(tomorrow.Outfit), $"outfit, day +{d}");
            }
        }

        [Test]
        public void TwoServices_OnTheSameDay_Agree()
        {
            RaceOfTheDayInfo a = Make().Today;
            RaceOfTheDayInfo b = Make().Today;

            Assert.AreEqual(a.DistanceMeters, b.DistanceMeters);
            Assert.AreEqual(a.LookLabel, b.LookLabel);
        }

        [Test]
        public void TheDistance_IsOneOfTheListedOnes_AndEveryOneComesRound()
        {
            RaceOfTheDayService service = Make();
            var seen = new HashSet<float>();
            for (int d = 0; d < 30; d++)
            {
                _clock.UtcNow = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc).AddDays(d);
                float distance = service.Today.DistanceMeters;
                CollectionAssert.Contains(Distances, distance);
                seen.Add(distance);
            }
            Assert.AreEqual(Distances.Length, seen.Count);
        }

        [Test]
        public void TheOutfit_HoldsTheLooksItemsColoursAndSkin()
        {
            RaceOfTheDayService service = Make(looks: new List<RaceOfTheDayLook> { Looks()[1] });

            CosmeticOutfit outfit = service.Today.Outfit;

            Assert.AreEqual("tops.t02", outfit.Get(CosmeticCategory.Tops).CosmeticId);
            Assert.AreEqual("E53935", outfit.Get(CosmeticCategory.Tops).ColorHex);
            Assert.AreEqual("shoes.sh01", outfit.Get(CosmeticCategory.Shoes).CosmeticId);
            Assert.IsTrue(outfit.Get(CosmeticCategory.Headwear).IsEmpty, "a category the look leaves out stays bare");
            Assert.AreEqual("E0AC69", outfit.SkinColorHex);
        }

        [Test]
        public void EmptyLists_GiveNoDistanceAndAnEmptyOutfit()
        {
            RaceOfTheDayService service = Make(new float[0], new List<RaceOfTheDayLook>());

            RaceOfTheDayInfo today = service.Today;

            Assert.AreEqual(0f, today.DistanceMeters);
            Assert.IsTrue(today.Outfit.Equals(CosmeticOutfit.Empty));
            Assert.IsFalse(service.TryGetFieldRecord(out _), "no distance, no field");
        }

        [Test]
        public void IsRaceOfTheDay_OnlyForItsOwnEvent_AndTheRecordIdCarriesTheDistance()
        {
            RaceOfTheDayService service = Make();

            Assert.IsTrue(service.IsRaceOfTheDay(new RaceEventId("race_of_the_day")));
            Assert.IsFalse(service.IsRaceOfTheDay(new RaceEventId("100m")));
            Assert.AreEqual(new RaceEventId("race_of_the_day:150m"), service.RecordIdFor(150f));
            Assert.AreNotEqual(service.RecordIdFor(100f), service.RecordIdFor(150f));
        }

        [Test]
        public void FieldRecord_IsStableForTheDay_AndLongerRacesAreSlower()
        {
            bool a = Make(new[] { 60f }).TryGetFieldRecord(out DailyFieldRecord shortRace);
            bool b = Make(new[] { 60f }).TryGetFieldRecord(out DailyFieldRecord shortAgain);
            bool c = Make(new[] { 300f }).TryGetFieldRecord(out DailyFieldRecord longRace);

            Assert.IsTrue(a && b && c);
            Assert.AreEqual(shortRace.ElapsedMilliseconds, shortAgain.ElapsedMilliseconds);
            Assert.AreEqual(shortRace.Name, shortAgain.Name);
            Assert.Greater(longRace.ElapsedMilliseconds, shortRace.ElapsedMilliseconds);
            Assert.Greater(shortRace.ElapsedMilliseconds, 0L);
        }

        [Test]
        public void WithoutFieldNumbers_ThereIsNoFieldRecord()
        {
            Assert.IsFalse(Make(withField: false).TryGetFieldRecord(out _));
        }

        [Test]
        public void Config_KeepsOnlyUsableDistancesAndNamedSlots()
        {
            var config = ScriptableObject.CreateInstance<RaceOfTheDayConfig>();
            try
            {
                var so = new SerializedObject(config);
                SerializedProperty list = so.FindProperty("distancesMeters");
                float[] raw = { 100f, 0f, -5f, 150f, float.NaN };
                list.arraySize = raw.Length;
                for (int i = 0; i < raw.Length; i++) list.GetArrayElementAtIndex(i).floatValue = raw[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                CollectionAssert.AreEqual(new[] { 100f, 150f }, config.Distances);
                Assert.AreEqual("race_of_the_day", config.EventId, "an empty id falls back to the event's own");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void SeededAsset_UsesOnlyKnownCosmeticsOfTheRightCategory()
        {
            var config = AssetDatabase.LoadAssetAtPath<RaceOfTheDayConfig>("Assets/Speed-Staes/Data/Config/RaceOfTheDayConfig.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/Speed-Staes/Data/Config/CosmeticCatalog.asset");
            if (config == null || catalog == null) Assert.Inconclusive("The Race of the Day config is not seeded yet (ConfigAssetSeeder.ApplyRaceOfTheDay).");

            Assert.GreaterOrEqual(config.Distances.Count, 2, "one distance would never change");
            Assert.GreaterOrEqual(config.Looks.Count, 2, "one look would never change");
            foreach (RaceOfTheDayLook look in config.Looks)
            {
                CosmeticOutfit outfit = look.ToOutfit();
                foreach (CosmeticCategory category in CosmeticOutfit.Categories)
                {
                    CosmeticSlot slot = outfit.Get(category);
                    if (slot.IsEmpty) continue;
                    Assert.IsTrue(catalog.TryGet(slot.CosmeticId, out CosmeticDefinition item), $"{look.Label}: '{slot.CosmeticId}' is not in the catalog");
                    Assert.AreEqual(category, item.Category, $"{look.Label}: '{slot.CosmeticId}' is in the wrong slot");
                }
            }
        }
    }
}
