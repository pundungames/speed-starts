using System.Linq;
using SpeedStars.Runner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpeedStars.EditorTools
{
    public static class AccessoryLineupSetup
    {
        const string Path = "Assets/Speed-Staes/Scenes/Test/AccessoryLineup.unity";

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Open Nine Character Lineup")]
        public static void OpenLineup()
        {
            if (EditorApplication.isPlaying) return;
            if (System.IO.File.Exists(Path))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Path);
                return;
            }
            var studio = Object.FindObjectOfType<AccessoryPhotoStudio>();
            if (!studio) { Debug.LogError("Open AccessoryPhotoStudio first."); return; }
            var original = studio.gameObject.scene;
            if (original.isDirty && !EditorSceneManager.SaveScene(original)) return;
            if (!EditorSceneManager.SaveScene(original, Path, true)) return;
            var scene = EditorSceneManager.OpenScene(Path);
            studio = Object.FindObjectOfType<AccessoryPhotoStudio>();
            var model = studio.character;
            Vector3 center = model.position + Vector3.forward * 5 + Vector3.right * 2;
            var camera = studio.photoCamera;
            camera.transform.position = center + new Vector3(8, 1.5f, 0);
            camera.transform.LookAt(center + Vector3.up * 0.95f);
            camera.orthographic = false;
            camera.fieldOfView = 23;
            camera.orthographicSize = 1.65f;
            camera.aspect = 3.2f;
            var lineup = new GameObject("Nine Character Lineup").AddComponent<AccessoryLineup>();
            lineup.photoCamera = camera;
            lineup.characters = new AccessoryPhotoStudio[9];
            var shadow = Object.FindObjectsOfType<RunnerContactShadow>().FirstOrDefault(s => s.target == model);
            for (int i = 0; i < 9; i++)
            {
                var character = i == 0 ? model : Object.Instantiate(model);
                character.name = $"Character {i + 1:00} - {(i < 7 ? "Regular" : "Premium")}";
                character.position = center + camera.transform.right * ((i - 4) * 1.1f);
                character.rotation = Quaternion.Euler(0, 90, 0);
                var member = i == 0 ? studio : character.gameObject.AddComponent<AccessoryPhotoStudio>();
                member.character = character;
                member.photoCamera = camera;
                member.handleInput = false;
                member.showControls = false;
                member.imageWidth = 5760;
                member.imageHeight = 1800;
                member.capturePrefix = "Lineup_01-09";
                member.SelectSet(i + 1);
                lineup.characters[i] = member;
                if (shadow && i != 0)
                {
                    var copy = Object.Instantiate(shadow, shadow.transform.parent);
                    copy.name = $"Contact Shadow {i + 1:00}";
                    copy.target = character;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            FrameLineup();
            Selection.activeGameObject = lineup.gameObject;
            Debug.Log("Nine-character lineup ready: Play for independent random colors, V captures all nine, R rerolls colors.");
        }

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Capture Nine Character Lineup")]
        public static void Capture() => Object.FindObjectOfType<AccessoryLineup>()?.Capture();

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Frame Nine Character Lineup")]
        public static void FrameLineup()
        {
            var lineup = Object.FindObjectOfType<AccessoryLineup>();
            if (!lineup) return;
            Vector3 center = Vector3.zero;
            bool shift = lineup.characters[0].character.position.z < 0;
            bool moveForward = lineup.characters[0].character.position.x < 0;
            foreach (var member in lineup.characters)
            {
                if (shift) member.character.position += Vector3.forward * 5;
                if (moveForward) member.character.position += Vector3.right * 2;
                center += member.character.position / lineup.characters.Length;
            }
            var camera = lineup.photoCamera;
            camera.transform.position = center + new Vector3(8, 1.5f, 0);
            camera.transform.LookAt(center + Vector3.up * 0.95f);
            camera.orthographic = false;
            camera.fieldOfView = 23;
            camera.aspect = 3.2f;
            // Foreground maintenance barriers obscure the costumes in a wide group shot.
            lineup.ClearForeground();
            foreach (var root in lineup.gameObject.scene.GetRootGameObjects())
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                    if (item.name == "StreetMaintenanceBarrier" && item.position.x > center.x)
                        item.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(lineup.gameObject.scene);
            EditorSceneManager.SaveScene(lineup.gameObject.scene);
        }
    }
}
