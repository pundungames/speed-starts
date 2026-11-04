using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tools.Editor
{
    /// <summary>
    /// Deletes the dev save. The save is files, not PlayerPrefs, and its ".bak" would bring the progress back if only the main
    /// file went, so the whole folder goes. Off in Play mode: the running game holds the progress in memory and would write it back.
    /// </summary>
    public static class SaveMenu
    {
        private const string MenuPath = "Tools/Speed Stars/Delete Save";

        [MenuItem(MenuPath, false, 201)]
        private static void DeleteSave()
        {
            string folder = Path.Combine(Application.persistentDataPath, SpeedStars.Meta.Persistence.FileSaveStore.DefaultFolderName);
            if (!Directory.Exists(folder))
            {
                Debug.Log("Delete Save: there is no save at " + folder);
                return;
            }
            if (!EditorUtility.DisplayDialog("Delete Save", "Delete the progress in\n" + folder + "?", "Delete", "Cancel")) return;

            Directory.Delete(folder, true);
            Debug.Log("Delete Save: deleted " + folder);
        }

        [MenuItem(MenuPath, true)]
        private static bool DeleteSaveValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
