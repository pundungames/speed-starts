using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// Helpers of the PlayMode smoke tests. The test assembly cannot see Assembly-CSharp (Bridge, installers), and the
    /// view fields are private [SerializeField]s of the isim sozlesmesi (docs/tasks/UI-v2-ISIMLER.md), so scene objects
    /// are bound by field name through reflection: a renamed or unbound field fails the test with its name in the message.
    /// </summary>
    internal static class PlaySmokeSupport
    {
        public const string HomeScene = "Home";
        public const string RaceScene = "Race";
        public const float WaitSeconds = 6f;

        private const System.Reflection.BindingFlags Declared =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;

        /// <summary>First non-null value of the named field(s), looking at the most derived type first.</summary>
        public static T Field<T>(object owner, params string[] names) where T : class
        {
            for (System.Type type = owner.GetType(); type != null; type = type.BaseType)
            {
                foreach (string name in names)
                {
                    System.Reflection.FieldInfo field = type.GetField(name, Declared);
                    if (field == null) continue;
                    if (field.GetValue(owner) is T value && (!(value is UnityEngine.Object o) || o != null)) return value;
                }
            }
            return null;
        }

        public static T RequireField<T>(object owner, params string[] names) where T : class
        {
            T value = Field<T>(owner, names);
            Assert.IsNotNull(value, $"{owner.GetType().Name}.{string.Join("/", names)} is missing or not bound in the scene.");
            return value;
        }

        public static T Require<T>() where T : UnityEngine.Object
        {
            T found = UnityEngine.Object.FindObjectOfType<T>(true);
            Assert.IsNotNull(found, $"{typeof(T).Name} is not in the loaded scene.");
            return found;
        }

        /// <summary>Close/Back button of a panel: its own field first (backButton, closeButton), else the UiPanel.closeButton.</summary>
        public static Button CloseButtonOf(object panel)
        {
            return RequireField<Button>(panel, "closeButton", "backButton");
        }

        public static IEnumerator LoadScene(string sceneName)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.IsNotNull(load, $"Scene '{sceneName}' could not be loaded (is it in Build Settings?).");
            while (!load.isDone) yield return null;
            // Zenject installs the scene contexts, then Start/IInitializable run.
            yield return null;
            yield return null;
        }

        public static IEnumerator WaitUntil(System.Func<bool> condition, string what, float timeout = WaitSeconds)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeout) Assert.Fail($"Timed out after {timeout:0.#}s waiting for: {what}");
                yield return null;
            }
        }

        public static IEnumerator WaitForScene(string sceneName)
        {
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == sceneName && SceneManager.GetActiveScene().isLoaded,
                $"scene '{sceneName}' to be active", 15f);
            yield return null;
            yield return null;
        }

        /// <summary>Waits until the button can really be pressed (open animation done, group interactable), then presses it.</summary>
        public static IEnumerator Tap(Button button, string what)
        {
            Assert.IsNotNull(button, $"{what}: button is not bound.");
            yield return WaitUntil(() => button != null && button.gameObject.activeInHierarchy && button.IsInteractable(),
                $"'{what}' to be visible and interactable");
            // Open/stagger animations move and scale buttons: wait until the screen rect holds still for 3 frames and no parent
            // CanvasGroup blocks raycasts, otherwise a mid-animation raycast reports a false "covered by nothing".
            Vector2 lastCenter = ScreenCenterOf(button);
            Vector3 lastScale = button.transform.lossyScale;
            int still = 0;
            yield return WaitUntil(() =>
            {
                if (button == null) return true;
                Vector2 center = ScreenCenterOf(button);
                Vector3 scale = button.transform.lossyScale;
                bool same = (center - lastCenter).sqrMagnitude < 0.25f && (scale - lastScale).sqrMagnitude < 1e-6f;
                lastCenter = center;
                lastScale = scale;
                still = same && RaycastsAllowed(button) ? still + 1 : 0;
                return still >= 3;
            }, $"'{what}' to hold still and accept raycasts");
            AssertReallyHittable(button, what);

            // The same path a finger takes: the pointer click handler of the button (Button.OnPointerClick checks interactable).
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = ScreenCenterOf(button),
                clickCount = 1
            };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
        }

        /// <summary>
        /// A raycast from the button's screen centre must reach the button (or one of its children). Calling onClick.Invoke()
        /// hides art layers that cover a button and swallow the real tap.
        /// </summary>
        private static void AssertReallyHittable(Button button, string what)
        {
            Assert.IsNotNull(EventSystem.current, $"{what}: there is no EventSystem in the scene.");
            var data = new PointerEventData(EventSystem.current) { position = ScreenCenterOf(button) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);

            Transform self = button.transform;
            if (hits.Count > 0 && hits[0].gameObject != null && hits[0].gameObject.transform.IsChildOf(self)) return;

            string top = hits.Count == 0 || hits[0].gameObject == null ? "nothing" : PathOf(hits[0].gameObject.transform);
            Assert.Fail($"'{what}' is covered by '{top}' (a real tap at {data.position} does not reach {PathOf(self)}).");
        }

        private static bool RaycastsAllowed(Button button)
        {
            foreach (CanvasGroup group in button.GetComponentsInParent<CanvasGroup>(false))
            {
                if (!group.blocksRaycasts) return false;
                if (group.ignoreParentGroups) break;
            }
            return true;
        }

        private static Vector2 ScreenCenterOf(Button button)
        {
            var rect = (RectTransform)button.transform;
            Canvas canvas = button.GetComponentInParent<Canvas>();
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            Camera camera = null;
            if (root != null && root.renderMode != RenderMode.ScreenSpaceOverlay)
                camera = root.worldCamera != null ? root.worldCamera : Camera.main;
            return RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        public static IEnumerator WaitOpen(Component panel, string what)
        {
            yield return WaitUntil(() => panel.gameObject.activeInHierarchy, $"{what} to open");
        }

        public static IEnumerator WaitClosed(Component panel, string what)
        {
            yield return WaitUntil(() => !panel.gameObject.activeSelf, $"{what} to close");
        }
    }

    /// <summary>
    /// Files of the player's save folder, restored byte for byte after a test: the entry gate saves on a spent ticket and
    /// the tests must leave the real (or the copy's) save as they found it.
    /// </summary>
    internal sealed class SaveFolderSnapshot
    {
        private readonly string _directory;
        private readonly bool _existed;
        private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

        private SaveFolderSnapshot(string directory)
        {
            _directory = directory;
            _existed = Directory.Exists(directory);
            if (!_existed) return;
            foreach (string path in Directory.GetFiles(directory)) _files[path] = File.ReadAllBytes(path);
        }

        /// <summary>The folder FileSaveStore.CreateDefault uses.</summary>
        public static SaveFolderSnapshot Take()
        {
            return new SaveFolderSnapshot(Path.Combine(Application.persistentDataPath, "Save"));
        }

        public void Restore()
        {
            if (!_existed)
            {
                if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
                return;
            }

            Directory.CreateDirectory(_directory);
            foreach (string path in Directory.GetFiles(_directory))
                if (!_files.ContainsKey(path)) File.Delete(path);
            foreach (KeyValuePair<string, byte[]> file in _files) File.WriteAllBytes(file.Key, file.Value);
        }
    }
}
