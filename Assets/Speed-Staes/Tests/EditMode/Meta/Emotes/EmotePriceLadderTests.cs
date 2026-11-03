using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace SpeedStars.Tests.Meta.Emotes
{
    /// <summary>
    /// The seeder's emote price ladder (T-089): paid emotes cost more as the list goes on, in tiers of equal price, and "None"
    /// comes first and is not priced. The seeder lives in the editor assembly the tests cannot reference, so it is called by
    /// reflection. Prices are read, never written into the test.
    /// </summary>
    public sealed class EmotePriceLadderTests
    {
        private const int Tier = 3;      // the task's tier size: three emotes per price
        private const int Tiers = 3;

        private static Type Find(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName, false)).FirstOrDefault(t => t != null);

        private static int Price(int paidIndex)
        {
            Type seeder = Find("SpeedStars.Tools.Builders.ConfigAssetSeeder");
            Assert.IsNotNull(seeder, "ConfigAssetSeeder is in the editor assembly");
            MethodInfo method = seeder.GetMethod("EmotePrice", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ConfigAssetSeeder.EmotePrice");
            return (int)method.Invoke(null, new object[] { paidIndex });
        }

        [Test]
        public void Prices_AreTiersOfEqualPrice_RisingTowardsTheEndOfTheList()
        {
            var prices = new List<int>();
            for (int i = 0; i < Tier * Tiers; i++) prices.Add(Price(i));

            for (int tier = 0; tier < Tiers; tier++)
                for (int i = 1; i < Tier; i++)
                    Assert.AreEqual(prices[tier * Tier], prices[tier * Tier + i], "one price inside a tier");
            for (int tier = 1; tier < Tiers; tier++)
                Assert.Greater(prices[tier * Tier], prices[(tier - 1) * Tier], "each tier costs more than the one before");
            Assert.Greater(prices[0], 0, "a paid emote is never free");
        }

        [Test]
        public void MoreEmotesThanTiers_RepeatTheLastPrice()
        {
            Assert.AreEqual(Price(Tier * Tiers - 1), Price(Tier * Tiers + 5));
            Assert.AreEqual(Price(0), Price(-1), "a bad index reads the first tier");
        }

        [Test]
        public void SeedList_StartsWithNone_AndHasUniqueIds()
        {
            Type seed = Find("SpeedStars.Tools.Builders.EmoteCatalogSeed");
            Assert.IsNotNull(seed, "EmoteCatalogSeed is in the editor assembly");
            var ids = ((IEnumerable<string>)seed.GetMethod("Ids", BindingFlags.Public | BindingFlags.Static).Invoke(null, null)).ToList();

            Assert.AreEqual("none", ids[0]);
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "ids are unique");
            Assert.Greater(ids.Count - 1, Tier * (Tiers - 1), "enough clips to fill every tier");
        }
    }
}
