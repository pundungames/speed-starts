using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Writes the 16 placeholder UI prefabs of contract §5 to the paths in UiPrefabPaths (editor time; nothing here runs
    /// in the game, so the "UI is never built at runtime" rule holds: views bind these prefabs with [SerializeField]).
    /// Order matters: widgets first, because screens nest them (RedDot in Home). Objects are built in a preview scene, so the open scene is never left with leftovers.
    /// overwrite == false: an existing prefab is kept as it is (the user may have edited it) and reused for nesting.
    /// overwrite == true: rebuilt and saved to the same path, which keeps its GUID and therefore every reference to it
    /// - but only with the explicit force flag (<see cref="ForceFlag"/>): the user hand-edits these prefabs (commit 4f38e016), so
    /// without it an existing prefab is kept even when overwrite is true. New prefabs are converted to TextMeshPro right after the build (T-100).
    /// </summary>
    public static class UiPrefabBuilder
    {
        /// <summary>Command-line flag (without the dash) that lets overwrite rebuild hand-edited prefabs / Home.unity.</summary>
        public const string ForceFlag = "forceOverwriteHandEdits";

        public static bool ForceOverwriteHandEdits => Array.IndexOf(Environment.GetCommandLineArgs(), "-" + ForceFlag) >= 0;

        public static void BuildAll(bool overwrite)
        {
            if (overwrite && !ForceOverwriteHandEdits)
            {
                UnityEngine.Debug.LogWarning($"UiPrefabBuilder: overwrite requested, but existing prefabs are hand-edited and are kept (missing ones are built). Pass -{ForceFlag} to rebuild them.");
                overwrite = false;
            }

            UiArt.ImportAll(); // sprite settings first: prefabs load the art as Sprites
            UiBuildKit.Begin();
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewPreviewScene();
            var run = new Run(scene, overwrite);
            try
            {
                run.Build(UiPrefabPaths.RedDot, UiWidgetPrefabs.RedDot);
                run.Build(UiPrefabPaths.ResourceBar, UiWidgetPrefabs.ResourceBar);

                run.Build(UiPrefabPaths.HomeScreen, UiScreenPrefabs.Home);
                // The screen reads the content asset (which race behind which card); seeded here when missing, never overwritten.
                SpeedStars.UI.Home.EventSelectLayout eventSelectContent = EventSelectConfigSeed.Seed(overwrite: false);
                run.Build(UiPrefabPaths.EventSelectScreen, root => UiEventSelectPrefabs.EventSelect(root, eventSelectContent));
                run.Build(UiPrefabPaths.ThemeSelectScreen, UiThemeSelectPrefabs.ThemeSelect);
                UiCostumePrefabs.BuildAll(run.Build); // T-026: item card + preset row + the costume editor screen (CosmeticsScreen path)
                UiLeaderboardPrefabs.BuildAll(run.Build, scene, eventSelectContent); // T-038: leaderboard screen (HighScoresScreen path) + its row prefabs
                UiEmotePrefabs.BuildAll(run.Build); // T-040: emote card + the emote screen (costume editor art)
                run.Build(UiPrefabPaths.RaceHud, UiScreenPrefabs.RaceHud);
                UiReplayPrefabs.BuildAll(run.Build); // T-098: Watch Replay controls; before Results, which binds the prefab
                UiResultsPrefabs.BuildAll(run.Build); // T-033: table row prefab + the results screen with the Oyun_Bitis_UI_v1 art
                UiAnalysisPrefabs.BuildAll(run.Build); // T-036: ranking row prefab + the Analysis screen (Analiz_UI_v1 art)
                UiRaceStartPrefabs.BuildAll(run.Build); // T-039: READY / SET / GO! overlay (Yaris_Baslangic_UI_v1 art)

                run.Build(UiPrefabPaths.EntryGateModal, UiPopupPrefabs.EntryGate);
                UiCashOfferPrefabs.BuildAll(run.Build); // T-085: the Cash offer window (resource bar "+": watch an ad for Cash)
                // T-043: daily gift popup, 7 day panel (+ its card prefabs) and the ticket flight widget, with the Gunluk_Odul_UI_v2 art.
                UiRewardPrefabs.BuildAll(run.Build, scene);
                // T-031: the settings window (its config asset, with the flag sprites, is seeded by ConfigAssetSeeder).
                UiSettingsPrefabs.BuildAll(run.Build, scene);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            // Freshly built prefabs carry legacy Text: convert them in place (existing, already converted ones are untouched).
            if (run.BuiltPaths.Count > 0) SpeedStars.Tools.Migration.TmpMigration.ConvertPaths(run.BuiltPaths);

            UnityEngine.Debug.Log($"UI_PREFABS_OK built={run.Built} kept={run.Kept} (overwrite={overwrite})");
        }

        /// <summary>State of one BuildAll call: where objects are built, the overwrite rule, counts for the log.</summary>
        private sealed class Run
        {
            private readonly UnityEngine.SceneManagement.Scene _scene;
            private readonly bool _overwrite;

            public Run(UnityEngine.SceneManagement.Scene scene, bool overwrite)
            {
                _scene = scene;
                _overwrite = overwrite;
            }

            public int Built { get; private set; }
            public int Kept { get; private set; }
            public System.Collections.Generic.List<string> BuiltPaths { get; } = new System.Collections.Generic.List<string>();

            /// <summary>
            /// Builds the prefab at <paramref name="path"/> (root named after the file, stretched; <paramref name="fill"/>
            /// adds the rest) and returns the saved asset, or returns the existing asset untouched when not overwriting.
            /// </summary>
            public GameObject Build(string path, Action<RectTransform> fill)
            {
                if (!_overwrite && File.Exists(UiBuildKit.FullPath(path)))
                {
                    Kept++;
                    return LoadExisting(path);
                }

                string folder = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(folder)) throw new InvalidOperationException($"UiPrefabBuilder: bad prefab path '{path}'.");
                UiBuildKit.EnsureFolder(folder.Replace('\\', '/'));

                RectTransform root = UiBuildKit.NewRoot(Path.GetFileNameWithoutExtension(path), _scene);
                try
                {
                    fill(root);
                    GameObject saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path, out bool success);
                    if (!success || saved == null) throw new InvalidOperationException($"UiPrefabBuilder: could not save '{path}'.");
                    Built++;
                    BuiltPaths.Add(path);
                    return saved;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root.gameObject);
                }
            }

            private static GameObject LoadExisting(string path)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;

                // On disk but not imported yet (copied in without a refresh): import it, never overwrite it.
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing == null)
                    UnityEngine.Debug.LogWarning($"UiPrefabBuilder: '{path}' exists but does not load as a prefab; kept, not nested anywhere.");
                return existing;
            }
        }
    }
}
