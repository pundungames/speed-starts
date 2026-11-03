using System;
using System.Collections.Generic;

namespace SpeedStars.Tools.Migration
{
    /// <summary>
    /// What the pure converter needs from Unity: the YAML references of the font assets (their sub-asset fileIDs are only known
    /// after the font asset is saved) and the set of nested-prefab targets that are text components.
    /// </summary>
    public sealed class TextConversionContext
    {
        /// <summary>YAML reference of the TMP font asset, e.g. "{fileID: 11400000, guid: ..., type: 2}".</summary>
        public string FontAssetRef = "{fileID: 11400000, guid: 00000000000000000000000000000000, type: 2}";

        /// <summary>YAML reference of the font asset's default material (sub-asset).</summary>
        public string DefaultMaterialRef = "{fileID: 2100000, guid: 00000000000000000000000000000000, type: 2}";

        /// <summary>YAML reference of the TTF (legacy Text font, used by the InputField texts that stay legacy).</summary>
        public string TtfFontRef = "{fileID: 12800000, guid: 00000000000000000000000000000000, type: 3}";

        /// <summary>Returns the YAML reference of the material preset for an outline look.</summary>
        public Func<OutlineSpec, string> PresetMaterialRef = spec => "{fileID: 2100000, guid: 00000000000000000000000000000000, type: 2}";

        /// <summary>"guid:fileID" of every text component inside a prefab (legacy Text or TMP): nested-prefab overrides on them are translated.</summary>
        public ISet<string> TextTargets = new HashSet<string>();
    }

    /// <summary>Which text components a file holds and which outline looks they need (pass 1 of the migration).</summary>
    public sealed class TextAnalysis
    {
        /// <summary>fileIDs of legacy Text (convertible) and TMP texts; InputField-owned texts are not in here.</summary>
        public readonly List<long> TextIds = new List<long>();

        /// <summary>Distinct outline looks the convertible texts need.</summary>
        public readonly List<OutlineSpec> Outlines = new List<OutlineSpec>();

        /// <summary>Legacy Text docs in the file that will be converted.</summary>
        public int LegacyCount;
    }

    /// <summary>Outcome of converting one file.</summary>
    public sealed class ConversionResult
    {
        public string Text;
        public bool Changed;
        public int Converted;
        public int InputFieldFonts;
        public int OutlinesRemoved;
        public int OverridesTranslated;
        public int OverridesDropped;
        public int ProxiesSwitched;
        public int DanglingRefsCleared;
        public readonly List<string> Warnings = new List<string>();

        /// <summary>Legacy Text docs left in the file (InputField texts) as "name (fileID)".</summary>
        public readonly List<string> RemainingLegacy = new List<string>();
    }
}
