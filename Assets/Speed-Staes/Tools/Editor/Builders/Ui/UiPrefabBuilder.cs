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
    /// Order matters: rows and widgets first, because screens and panels nest them (RedDot in Home, SevenDayCell × 7
    /// in the check-in panel). Objects are built in a preview scene, so the open scene is never left with leftovers.
    /// overwrite == false: an existing prefab is kept as it is (the user may have edited it) and reused for nesting.
    /// overwrite == true: rebuilt and saved to the same path, which keeps its GUID and therefore every reference to it.
    /// </summary>
    public static class UiPrefabBuilder
    {
        public static void BuildAll(bool overwrite)
        {
            UiArt.ImportAll(); // sprite settings first: prefabs load the art as Sprites
            UiBuildKit.Begin();
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewPreviewScene();
            var run = new Run(scene, overwrite);
            try
            {
                // EventRow is no longer built: the event select screen (T-024) has cards, not a list.
                GameObject highScoreRow = run.Build(UiPrefabPaths.HighScoreRow, UiRowPrefabs.HighScoreRow);
                run.Build(UiPrefabPaths.CosmeticCell, UiRowPrefabs.CosmeticCell);
                GameObject sevenDayCell = run.Build(UiPrefabPaths.SevenDayCell, UiRowPrefabs.SevenDayCell);

                run.Build(UiPrefabPaths.RedDot, UiWidgetPrefabs.RedDot);
                run.Build(UiPrefabPaths.ResourceBar, UiWidgetPrefabs.ResourceBar);
                run.Build(UiPrefabPaths.RewardFlight, UiWidgetPrefabs.RewardFlight);

                run.Build(UiPrefabPaths.HomeScreen, UiScreenPrefabs.Home);
                // The screen reads the content asset (which race behind which card); seeded here when missing, never overwritten.
                SpeedStars.UI.Home.EventSelectLayout eventSelectContent = EventSelectConfigSeed.Seed(overwrite: false);
                run.Build(UiPrefabPaths.EventSelectScreen, root => UiEventSelectPrefabs.EventSelect(root, eventSelectContent));
                run.Build(UiPrefabPaths.ThemeSelectScreen, UiThemeSelectPrefabs.ThemeSelect);
                run.Build(UiPrefabPaths.CosmeticsScreen, UiScreenPrefabs.Cosmetics);
                run.Build(UiPrefabPaths.HighScoresScreen, UiScreenPrefabs.HighScores);
                run.Build(UiPrefabPaths.RaceHud, UiScreenPrefabs.RaceHud);
                run.Build(UiPrefabPaths.ResultsScreen,
                    root => UiScreenPrefabs.Results(root, UiViewBinder.RowComponent<SpeedStars.UI.Race.ResultRowView>(highScoreRow, UiPrefabPaths.HighScoreRow)));

                run.Build(UiPrefabPaths.EntryGateModal, UiPopupPrefabs.EntryGate);
                run.Build(UiPrefabPaths.DailyGiftPopup, UiPopupPrefabs.DailyGift);
                run.Build(UiPrefabPaths.SevenDayCheckInPanel, root => UiPopupPrefabs.SevenDayCheckIn(root, sevenDayCell, scene));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

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
