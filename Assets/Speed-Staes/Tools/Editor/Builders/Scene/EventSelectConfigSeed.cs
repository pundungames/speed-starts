using SpeedStars.UI.Home;
using UnityEditor;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First fill of the event select content asset (T-024): which race event sits behind which category card and bottom
    /// button, plus the art's fixed texts. Event ids are the ones of ConfigAssetSeeder's catalog seed (strings, so this
    /// does not depend on seed order). An existing asset is the user's and is never touched unless <c>overwrite</c> is asked.
    /// </summary>
    public static class EventSelectConfigSeed
    {
        private const string Source = nameof(EventSelectConfigSeed);

        public const string Path = ConfigAssetSeeder.Folder + "/EventSelectLayout.asset";

        /// <returns>The asset (new, reset or the untouched existing one).</returns>
        public static EventSelectLayout Seed(bool overwrite)
        {
            var asset = CreateOrReset<EventSelectLayout>(Path, overwrite);
            if (asset == null)
            {
                Skipped(Source, Path);
                return LoadRequired<EventSelectLayout>(Path, Source);
            }

            var so = new SerializedObject(asset);
            Prop(so, "headingText").stringValue = "SELECT EVENT";
            Prop(so, "continueLabel").stringValue = "CONTINUE";

            var categories = Prop(so, "categories");
            categories.arraySize = 3;
            SetCategory(categories, 0, EventCategory.Sprints, "SPRINTS", "100m", "200m", "400m");
            SetCategory(categories, 1, EventCategory.Hurdles, "HURDLES", "60mh", "110mh", "400mh");
            SetCategory(categories, 2, EventCategory.Relays, "RELAYS", "4x100m", "medley");

            Prop(so, "raceOfTheDayId").stringValue = "race_of_the_day";
            Prop(so, "raceOfTheDayLabel").stringValue = "RACE OF THE DAY";
            Prop(so, "practiceId").stringValue = "training";
            Prop(so, "practiceLabel").stringValue = "PRACTICE";
            Apply(so);
            Log(Source, $"Seeded {Path}.");
            return asset;
        }

        private static void SetCategory(SerializedProperty categories, int index, EventCategory category, string title, params string[] ids)
        {
            SerializedProperty entry = categories.GetArrayElementAtIndex(index);
            Rel(entry, "category").intValue = (int)category;
            Rel(entry, "title").stringValue = title;
            SerializedProperty list = Rel(entry, "eventIds");
            list.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++) list.GetArrayElementAtIndex(i).stringValue = ids[i];
        }
    }
}
