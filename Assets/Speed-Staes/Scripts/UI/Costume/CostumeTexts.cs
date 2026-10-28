namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// Fixed English labels of the costume editor (the art has no baked text). The builder writes them into the Text
    /// objects of the prefab; views never hard-code strings.
    /// </summary>
    public static class CostumeTexts
    {
        public const string ColorTitle = "Color";
        public const string Power = "Power";
        public const string Stamina = "Stamina";
        public const string Random = "Random";
        public const string Save = "Save";

        public static string TabName(CostumeTab tab) => tab.ToString();
    }
}
