using System;
using SpeedStars.UI.Home;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Binds the [SerializeField] fields of the view components to objects INSIDE the same prefab (editor time, through
    /// SerializedObject). Field names are the views' own; paths are the hierarchy names UiScreenPrefabs/UiRowPrefabs build.
    /// Fields that point at scene objects (other screens, modals) stay empty on purpose: the scene builder binds them.
    /// A missing child, a missing component or an unknown field name stops the build with a clear error: nothing stays
    /// silently unbound.
    /// </summary>
    internal static class UiViewBinder
    {
        // ---------------------------------------------------------------- screens and popups

        /// <summary>
        /// Left for the scene builder: eventSelectScreen, entryGateModal, cosmeticsScreen, highScoresScreen, sevenDayPopup.
        /// </summary>
        public static void BindHome(RectTransform root)
        {
            var b = new Binding(root, typeof(HomeScreenView));
            b.Set("sevenDayButton", b.Child<Button>("DailyReward"));
            b.Set("sevenDayRedDot", b.Child<Transform>("DailyReward/Notification").gameObject);
            b.Set("settingsButton", b.Child<Button>("Settings"));
            b.Set("soundButton", b.Child<Button>("Sound"));
            b.Set("environmentButton", b.Child<Button>("Environment"));
            b.Set("upgradeButton", b.Child<Button>("Upgrade"));
            b.Set("playersButton", b.Child<Button>("Players"));
            b.Set("highScoresButton", b.Child<Button>("HighScores"));
            b.Set("eventSelectButton", b.Child<Button>("RaceSelect"));
            b.Set("eventNameText", b.Child<Text>("RaceSelect/Label"));
            b.Set("playButton", b.Child<Button>("Play"));
            b.Set("racerNameText", b.Child<Text>("CharacterCard/Select/Name"));
            b.Set("previousRacerButton", b.Child<Button>("CharacterCard/Previous"));
            b.Set("nextRacerButton", b.Child<Button>("CharacterCard/Next"));
            b.Set("powerFill", b.Child<Image>("CharacterCard/PowerBar/Fill"));
            b.Set("staminaFill", b.Child<Image>("CharacterCard/StaminaBar/Fill"));
            b.Set("heightText", b.Child<Text>("CharacterCard/HeightField/Label"));
            b.Set("editRacerButton", b.Child<Button>("CharacterCard/EditRacer"));
            b.Set("emoteButton", b.Child<Button>("CharacterCard/Emote"));
            b.Apply();
        }

        public static void BindEntryGate(RectTransform root)
        {
            var b = new Binding(root, typeof(EntryGateModalView));
            b.Set("window", b.Child<RectTransform>("Window"));
            b.Set("titleText", b.Child<Text>("Window/Title"));
            b.Set("eventNameText", b.Child<Text>("Window/EventName"));
            b.Set("ticketCountText", b.Child<Text>("Window/TicketCount"));
            b.Set("ticketIcon", b.Child<RectTransform>("Window/TicketIcon"));
            b.Set("refillText", b.Child<Text>("Window/Refill"));
            b.Set("useTicketButton", b.Child<Button>("Window/UseEntryTicket"));
            b.Set("useTicketLabel", b.Child<Text>("Window/UseEntryTicket/Label"));
            b.Set("remainingText", b.Child<Text>("Window/Remaining"));
            b.Set("timedLabel", b.Child<Text>("Window/TimedTicketStatus/Label"));
            b.Set("timedValue", b.Child<Text>("Window/TimedTicketStatus/Value"));
            b.Set("useTimedButton", b.Child<Button>("Window/UseTimedTicket"));
            b.Set("useTimedLabel", b.Child<Text>("Window/UseTimedTicket/Label"));
            b.Set("timedStatus", b.Child<RectTransform>("Window/TimedTicketStatus"));
            b.Set("offersCaption", b.Child<Text>("Window/GetTickets/Caption"));
            b.Set("buyWithCashButton", b.Child<Button>("Window/GetTickets/BuyWithCash"));
            b.Set("buyWithCashLabel", b.Child<Text>("Window/GetTickets/BuyWithCash/Label"));
            b.Set("cashPriceText", b.Child<Text>("Window/GetTickets/BuyWithCash/Price"));
            b.Set("watchAdButton", b.Child<Button>("Window/GetTickets/WatchAd"));
            b.Set("watchAdLabel", b.Child<Text>("Window/GetTickets/WatchAd/Label"));
            b.Set("watchAdSpinner", b.Child<RectTransform>("Window/GetTickets/WatchAd/Spinner"));
            b.Apply();
            BindClose(root, "Window/Close"); // base UiPanel.closeButton (the view has no field of its own any more)
        }

        public static void BindRaceHud(RectTransform root)
        {
            var b = new Binding(root, typeof(RaceHudView));
            b.Set("timeText", b.Child<Text>("Telemetry/Time"));
            b.Set("speedText", b.Child<Text>("Telemetry/Speed"));
            b.Set("cashText", b.Child<Text>("Cash/Value"));
            b.Set("cashPlusButton", b.Child<Button>("Cash/Plus"));
            b.Set("energyFill", b.Child<Image>("Energy/Fill"));
            b.Set("cadenceTrack", b.Child<RectTransform>("Cadence/Track"));
            b.Set("cadenceMarker", b.Child<RectTransform>("Cadence/Marker"));
            b.Set("exitButton", b.Child<Button>("Exit"));
            b.Set("restartButton", b.Child<Button>("Restart"));
            b.Apply();
        }

        // ---------------------------------------------------------------- rows and widgets

        public static void BindResourceBar(RectTransform root)
        {
            var b = new Binding(root, typeof(ResourceBarView));
            b.Set("purpleGemText", b.Child<Text>("PurpleGem/Value"));
            b.Set("entryTicketText", b.Child<Text>("EntryTicket/Value"));
            b.Set("entryTimerRoot", b.Child<Transform>("TicketTimer").gameObject);
            b.Set("entryTimerText", b.Child<Text>("TicketTimer/Value"));
            b.Set("timedTicketText", b.Child<Text>("TimedTicket/Value"));
            b.Set("cashText", b.Child<Text>("Cash/Value"));
            b.Apply();
        }

        /// <summary>
        /// Wires UiPanel.closeButton to the first existing candidate (Back / Close / ItemBack). No candidate means the panel has no
        /// close button of its own (e.g. Results); a missing closeButton FIELD stops the build (the view layer must provide it).
        /// Views with their own close code (EntryGateModalView, EventSelectScreenView) are not bound here.
        /// </summary>
        public static void BindClose(RectTransform root, params string[] candidates)
        {
            var panel = root.GetComponent<SpeedStars.UI.Framework.UiPanel>();
            if (panel == null)
                throw new InvalidOperationException($"UiViewBinder: '{root.name}' has no UiPanel to bind a close button on.");

            foreach (string path in candidates)
            {
                Transform node = root.Find(path);
                if (node == null) continue;
                var button = node.GetComponent<Button>();
                if (button == null)
                    throw new InvalidOperationException($"UiViewBinder: '{path}' under '{root.name}' has no Button.");

                var view = new SerializedObject(panel);
                SerializedProperty property = view.FindProperty("closeButton");
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                    throw new InvalidOperationException("UiViewBinder: UiPanel has no serialized object field 'closeButton' (T-015 not merged?).");
                property.objectReferenceValue = button;
                view.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }

        /// <summary>
        /// The view component on a saved row prefab (screens keep it as their rowPrefab). Fails with the reason when the
        /// row prefab on disk predates the view (kept because overwrite was off).
        /// </summary>
        public static T RowComponent<T>(GameObject rowPrefab, string path) where T : Component
        {
            T view = rowPrefab != null ? rowPrefab.GetComponent<T>() : null;
            if (view == null)
                throw new InvalidOperationException(
                    $"UiViewBinder: '{path}' has no {typeof(T).Name} on its root. Rebuild with overwrite = true so the row prefab gets its view.");
            return view;
        }

        // ---------------------------------------------------------------- one view's binding

        private sealed class Binding
        {
            private readonly Transform _root;
            private readonly Type _viewType;
            private readonly SerializedObject _view;

            public Binding(Transform root, Type viewType)
            {
                _root = root;
                _viewType = viewType;
                Component component = root.GetComponent(viewType);
                if (component == null)
                    throw new InvalidOperationException($"UiViewBinder: '{root.name}' has no {viewType.Name} component to bind.");
                _view = new SerializedObject(component);
            }

            /// <summary>Component T on the child at <paramref name="path"/> ("" = the root itself); throws when absent.</summary>
            public T Child<T>(string path) where T : Component
            {
                Transform node = path.Length == 0 ? _root : _root.Find(path);
                if (node == null)
                    throw new InvalidOperationException($"UiViewBinder: {_viewType.Name}: no child '{path}' under '{_root.name}'.");
                T found = node.GetComponent<T>();
                if (found == null)
                    throw new InvalidOperationException($"UiViewBinder: {_viewType.Name}: '{path}' under '{_root.name}' has no {typeof(T).Name}.");
                return found;
            }

            public void Set(string field, UnityEngine.Object value)
            {
                SerializedProperty property = _view.FindProperty(field);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                    throw new InvalidOperationException($"UiViewBinder: {_viewType.Name} has no serialized object field '{field}'.");
                if (value == null)
                    throw new InvalidOperationException($"UiViewBinder: {_viewType.Name}.{field}: nothing to assign.");
                property.objectReferenceValue = value;
            }

            public void Apply()
            {
                _view.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
