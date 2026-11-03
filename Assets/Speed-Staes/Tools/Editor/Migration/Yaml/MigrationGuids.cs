namespace SpeedStars.Tools.Migration
{
    /// <summary>Script GUIDs the file-level converter recognises (YAML text has no types, only these).</summary>
    public static class MigrationGuids
    {
        /// <summary>UnityEngine.UI.Text (uGUI package).</summary>
        public const string LegacyText = "5f7201a12d95ffc409449d95f23cf332";

        /// <summary>TMPro.TextMeshProUGUI (TextMeshPro 3.0.x).</summary>
        public const string TextMeshProUgui = "f4688fdb7df04437aeb418b961361dc5";

        /// <summary>UnityEngine.UI.Outline.</summary>
        public const string Outline = "e19747de3f5aca642ab2be37e372fb86";

        /// <summary>SpeedStars.UI.Framework.ThickOutline (our own circle outline).</summary>
        public const string ThickOutline = "398b7a91f8301514f9a3b4bf7aba7f2f";
    }
}
