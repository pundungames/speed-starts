using System;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// T-089: the Emote button of the race HUD. It sits in the top-right row after Restart and Exit, on the same square button
    /// art (UI/Art/RaceHud) with the word "Emote"; under it a small name label the view shows for a moment after a press. Both are
    /// bound to <c>RaceHudView.emoteButton</c> / <c>emoteNameText</c>. <see cref="AddEmoteButton"/> is idempotent and works on any
    /// loaded RaceHud root, so the full RaceHud build can call it; <see cref="PatchRaceHudPrefab"/> adds the button to the
    /// EXISTING prefab without rebuilding the rest. Never runs in the game.
    /// </summary>
    public static class UiRaceHudPrefabs
    {
        private const string ButtonName = "Emote";
        private const string NameLabelName = "Name";
        private const float RightOffset = -322f;   // after Restart (-50) and Exit (-186): the same 136 px step
        private const float TopOffset = -37f;      // the row of Restart / Exit
        private const float LabelSize = 30f;
        private const float NameSize = 30f;
        private const float NameWidth = 320f;
        private const float NameHeight = 44f;
        private const float NameGap = 8f;

        [MenuItem("Tools/Speed Stars/Build/Race HUD - add Emote button to the prefab")]
        public static void PatchRaceHudPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPaths.RaceHud);
            try
            {
                // Add if missing, otherwise leave the prefab alone: the user hand-edits it (T-102).
                if (root.transform.Find(ButtonName) != null)
                    UnityEngine.Debug.Log($"UiRaceHudPrefabs: '{UiPrefabPaths.RaceHud}' already has '{ButtonName}'; left as it is.");
                else
                {
                    AddEmoteButton((RectTransform)root.transform);
                    HandEditGuard.SavePatchedPrefab(root, UiPrefabPaths.RaceHud);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            HandEditGuard.ConvertPending();
        }

        /// <summary>Adds the Emote button and its name label under the HUD root (when missing) and binds them on the view.</summary>
        internal static void AddEmoteButton(RectTransform hudRoot)
        {
            var view = hudRoot.GetComponent<RaceHudView>();
            if (view == null) throw new InvalidOperationException("UiRaceHudPrefabs: the root has no RaceHudView to bind the Emote button on.");

            Transform existing = hudRoot.Find(ButtonName);
            if (existing != null) return; // never delete and rebuild: it may be hand-edited (T-102)

            var topRight = new Vector2(1f, 1f);
            Sprite background = UiArt.RaceHud("square_button_background");
            RectTransform node = Kit.Node(hudRoot, ButtonName);
            Kit.Anchor(node, topRight, topRight, new Vector2(RightOffset, TopOffset), background.rect.size);
            Image image = UiArt.Paint(node.gameObject, background, raycast: true);
            Button button = UiArt.MakeButton(node.gameObject, image, true, alphaHit: false);

            Text caption = Kit.Label(node, "Label", "Emote", LabelSize, UiArt.Navy, TextAnchor.MiddleCenter);
            Kit.Margin(caption, 10f, 6f, 10f, 6f);

            // Under the button, right edge aligned with it; white so it reads on the track and the sky.
            Text nameLabel = Kit.Label(node, NameLabelName, string.Empty, NameSize, Color.white, TextAnchor.MiddleRight);
            Kit.Place(nameLabel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, -NameGap), new Vector2(NameWidth, NameHeight));

            var so = new SerializedObject(view);
            SerializedProperty buttonProp = so.FindProperty("emoteButton");
            SerializedProperty nameProp = so.FindProperty("emoteNameText");
            if (buttonProp == null || nameProp == null)
                throw new InvalidOperationException("UiRaceHudPrefabs: RaceHudView has no emoteButton / emoteNameText field.");
            buttonProp.objectReferenceValue = button;
            nameProp.objectReferenceValue = nameLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
