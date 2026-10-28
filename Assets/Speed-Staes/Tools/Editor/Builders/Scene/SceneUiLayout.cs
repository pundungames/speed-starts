using System;
using SpeedStars.Installers;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The scene objects both Home and Race need (contract §5): SceneContext with the scene installer, EventSystem,
    /// UI Root with one overlay canvas per layer, UI prefab instances, the scene-move input blocker. Everything is
    /// created in the given scene and wired through SerializedObject; the screens themselves are T-007's prefabs,
    /// placed as prefab instances so the user edits them in the Inspector.
    /// </summary>
    internal static class SceneUiLayout
    {
        // Canvas layers, bottom to top. The overlay canvas must be the top-most: the transition blocker lives there.
        public const int BaseCanvasOrder = 0;
        public const int PopupCanvasOrder = 10;
        public const int OverlayCanvasOrder = 20;

        // Landscape reference resolution (contract §5, T-007 layouts) and an even width/height match.
        private static readonly Vector2 ReferenceResolution = new Vector2(2400f, 1080f);
        private const float WidthHeightMatch = 1f;

        // The scene being built is the active one (Single mode), so new objects land in it; the explicit move only
        // guarantees it. new GameObject(name, types) is used because it swaps Transform for RectTransform cleanly.
        public static GameObject CreateRoot(Scene scene, string name, params Type[] components)
        {
            var go = new GameObject(name, components);
            if (go.scene != scene) SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        public static GameObject CreateChild(Transform parent, string name, params Type[] components)
        {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }

        /// <summary>Main camera that only clears to a flat colour (the Home scene has no 3D content yet).</summary>
        public static Camera CreateFlatCamera(Scene scene, Color background)
        {
            var go = CreateRoot(scene, "Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 1f, -10f);
            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            return camera;
        }

        public static GameObject CreateEventSystem(Scene scene)
        {
            var go = CreateRoot(scene, "EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            AddRequired<InputSystemUIInputModule>(go);
#else
            AddRequired<StandaloneInputModule>(go);
#endif
            return go;
        }

        /// <summary>UI Root with the SceneTransitionBlocker (an always-active object, as the component asks).</summary>
        public static GameObject CreateUiRoot(Scene scene)
        {
            var go = CreateRoot(scene, "UI Root");
            AddRequired<SceneTransitionBlocker>(go);
            return go;
        }

        public static Transform CreateCanvas(Transform uiRoot, string name, int sortingOrder)
        {
            var go = CreateChild(uiRoot, name, typeof(RectTransform));
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;

            var canvas = AddRequired<Canvas>(go);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = AddRequired<CanvasScaler>(go);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = WidthHeightMatch;

            AddRequired<GraphicRaycaster>(go);
            return go.transform;
        }

        /// <summary>A prefab instance under <paramref name="parent"/>, active or not; the active state is a recorded scene override.</summary>
        public static GameObject Place(GameObject prefab, Transform parent, bool active)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate '{AssetDatabase.GetAssetPath(prefab)}'.");
            instance.transform.SetParent(parent, false);
            if (instance.activeSelf != active) instance.SetActive(active);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            return instance;
        }

        /// <summary>
        /// Full-screen, invisible (alpha 0), raycast-target Image as the last child of the top-most canvas; inactive
        /// until a scene move switches it on (SceneTransitionBlocker). Wired into the blocker component on UI Root.
        /// </summary>
        public static GameObject CreateTransitionBlocker(GameObject uiRoot, Transform overlayCanvas)
        {
            var go = CreateChild(overlayCanvas, "TransitionBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            go.transform.SetAsLastSibling();
            go.SetActive(false);

            var blocker = uiRoot.GetComponent<SceneTransitionBlocker>();
            if (blocker == null) throw new InvalidOperationException($"'{uiRoot.name}' has no SceneTransitionBlocker.");
            var so = new SerializedObject(blocker);
            Prop(so, "blocker").objectReferenceValue = go;
            Apply(so);
            return go;
        }

        /// <summary>
        /// SceneContext + the scene installer on one object; the installer is in the context's installer list and its
        /// root screen slot points at <paramref name="rootScreen"/> (the only screen active at start).
        /// </summary>
        public static void CreateSceneContext<TInstaller>(Scene scene, UiScreen rootScreen) where TInstaller : SceneUiInstaller
        {
            if (rootScreen == null) throw new ArgumentNullException(nameof(rootScreen));

            var go = CreateRoot(scene, "SceneContext");
            go.transform.SetAsFirstSibling();
            var context = AddRequired<SceneContext>(go);
            var installer = AddRequired<TInstaller>(go);

            var installerSo = new SerializedObject(installer);
            Prop(installerSo, "rootScreen").objectReferenceValue = rootScreen;
            Apply(installerSo);

            var contextSo = new SerializedObject(context);
            SetObjectList(Prop(contextSo, "_monoInstallers"), new UnityEngine.Object[] { installer });
            Apply(contextSo);
        }

        /// <summary>The UiScreen on a screen instance's root; throws when the prefab (T-007) does not carry one.</summary>
        public static UiScreen RootScreenOf(GameObject screenInstance, string prefabPath)
        {
            var screen = screenInstance.GetComponent<UiScreen>();
            if (screen == null)
                throw new InvalidOperationException($"'{prefabPath}' has no UiScreen on its root (T-007); it cannot be the scene's root screen.");
            return screen;
        }
    }
}
