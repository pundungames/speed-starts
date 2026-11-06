using System;
using SpeedStars.UI.Home;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// T-160: the relay leg row of the Home character card, added to the HomeScreen prefab. It is made from copies of the card's own
    /// name pill and arrow buttons (same art, same look), pushed down by one row, under <c>CharacterCard/Content/RelayLeg</c>, and
    /// bound on <see cref="HomeScreenView"/>. The row starts inactive (the view shows it while a relay event is selected).
    /// Patch only: it adds what is missing and never rebuilds, because the prefab is hand-edited (HandEditGuard). The designer moves
    /// and restyles the row in the Inspector afterwards; the tuned values live on the prefab, not here.
    /// </summary>
    public static class UiHomeRelayRow
    {
        public const string OkMarker = "HOME_RELAY_ROW_OK";
        private const string RowName = "RelayLeg";
        private const string CardPath = "CharacterCard/Content";

        /// <summary>The row sits this fraction of the card canvas height below the name pill (a starting place; the designer moves it).</summary>
        private const float DropFraction = 0.13f;

        [MenuItem("Tools/Speed Stars/Build/Home card - add the relay leg row to the HomeScreen prefab (patch)")]
        public static void PatchHomePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPaths.HomeScreen);
            try
            {
                if (root.transform.Find(CardPath + "/" + RowName) != null)
                    UnityEngine.Debug.Log($"UiHomeRelayRow: '{UiPrefabPaths.HomeScreen}' already has '{RowName}'; left as it is.");
                else
                {
                    AddRow(root);
                    HandEditGuard.SavePatchedPrefab(root, UiPrefabPaths.HomeScreen);
                }
                UnityEngine.Debug.Log(OkMarker);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            HandEditGuard.ConvertPending();
        }

        internal static void AddRow(GameObject homeRoot)
        {
            var view = homeRoot.GetComponent<HomeScreenView>()
                ?? throw new InvalidOperationException("UiHomeRelayRow: the HomeScreen root has no HomeScreenView.");
            Transform content = homeRoot.transform.Find(CardPath)
                ?? throw new InvalidOperationException($"UiHomeRelayRow: HomeScreen has no {CardPath}.");
            Transform select = Required(content, "Select");
            Transform previous = Required(content, "Previous");
            Transform next = Required(content, "Next");

            var row = new GameObject(RowName, typeof(RectTransform)) { layer = content.gameObject.layer };
            var rowRect = (RectTransform)row.transform;
            rowRect.SetParent(content, false);
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = rowRect.offsetMax = Vector2.zero;
            rowRect.localScale = Vector3.one;

            float drop = ((RectTransform)content).rect.height * DropFraction;
            Transform pill = Copy(select, row.transform, "Select", drop);
            Transform portrait = pill.Find("Portrait");
            if (portrait != null) UnityEngine.Object.DestroyImmediate(portrait.gameObject); // the name pill without the racer's face
            Transform back = Copy(previous, row.transform, "Previous", drop);
            Transform forward = Copy(next, row.transform, "Next", drop);

            TMP_Text label = pill.GetComponentInChildren<TMP_Text>(true)
                ?? throw new InvalidOperationException("UiHomeRelayRow: the card's name pill has no TMP_Text to copy.");
            label.text = HomeTexts.RelayLeg(0);

            var so = new SerializedObject(view);
            Bind(so, "relayLegRoot", row);
            Bind(so, "relayLegText", label);
            Bind(so, "previousLegButton", back.GetComponentInChildren<Button>(true)
                ?? throw new InvalidOperationException("UiHomeRelayRow: the card's Previous has no Button."));
            Bind(so, "nextLegButton", forward.GetComponentInChildren<Button>(true)
                ?? throw new InvalidOperationException("UiHomeRelayRow: the card's Next has no Button."));
            so.ApplyModifiedPropertiesWithoutUndo();

            row.SetActive(false);
        }

        private static Transform Required(Transform parent, string name) =>
            parent.Find(name) ?? throw new InvalidOperationException($"UiHomeRelayRow: {CardPath} has no '{name}'.");

        /// <summary>A copy of a card part under the row, moved down by <paramref name="drop"/> canvas units.</summary>
        private static Transform Copy(Transform source, Transform parent, string name, float drop)
        {
            GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
            copy.name = name;
            copy.SetActive(true);
            var rect = (RectTransform)copy.transform;
            rect.offsetMin += new Vector2(0f, -drop);
            rect.offsetMax += new Vector2(0f, -drop);
            return copy.transform;
        }

        private static void Bind(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty p = so.FindProperty(field)
                ?? throw new InvalidOperationException($"UiHomeRelayRow: HomeScreenView has no field '{field}'.");
            p.objectReferenceValue = value;
        }
    }
}
