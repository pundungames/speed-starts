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
            b.Set("playButton", b.Child<Button>("Play"));
            b.Set("eventSelectButton", b.Child<Button>("EventSelect"));
            b.Set("eventNameText", b.Child<Text>("EventSelect/EventName"));
            b.Set("editRacerButton", b.Child<Button>("EditRacer"));
            b.Set("highScoresButton", b.Child<Button>("HighScores"));
            b.Set("sevenDayButton", b.Child<Button>("SevenDayReward"));
            b.Apply();
        }

        public static void BindEventSelect(RectTransform root, EventRowView rowPrefab)
        {
            var b = new Binding(root, typeof(EventSelectScreenView));
            b.Set("content", b.Child<RectTransform>("Panel/List/Viewport/Content"));
            b.Set("rowPrefab", rowPrefab);
            b.Set("backButton", b.Child<Button>("Back"));
            b.Apply();
        }

        public static void BindEntryGate(RectTransform root)
        {
            var b = new Binding(root, typeof(EntryGateModalView));
            b.Set("window", b.Child<RectTransform>("Window"));
            b.Set("eventNameText", b.Child<Text>("Window/EventName"));
            b.Set("useTicketButton", b.Child<Button>("Window/UseEntryTicket"));
            b.Set("useTicketLabel", b.Child<Text>("Window/UseEntryTicket/Label"));
            b.Set("remainingText", b.Child<Text>("Window/UseEntryTicket/Remaining"));
            b.Set("timedLabel", b.Child<Text>("Window/TimedTicketStatus/Label"));
            b.Set("timedValue", b.Child<Text>("Window/TimedTicketStatus/Value"));
            b.Set("buyWithCashButton", b.Child<Button>("Window/GetTickets/BuyWithCash"));
            b.Set("watchAdButton", b.Child<Button>("Window/GetTickets/WatchAd"));
            b.Set("closeButton", b.Child<Button>("Window/Close"));
            b.Apply();
        }

        public static void BindRaceHud(RectTransform root)
        {
            var b = new Binding(root, typeof(RaceHudView));
            b.Set("timeText", b.Child<Text>("RaceTime/Value"));
            b.Set("progressFill", b.Child<Image>("Progress/Bar/Fill"));
            b.Set("distanceText", b.Child<Text>("Progress/Distance"));
            b.Apply();
        }

        /// <summary>
        /// resultsContent is the scroll list's Content (Results/Viewport/Content), not the Results ScrollRect itself: that is
        /// where the layout group lives, so the row gets its width and height there.
        /// </summary>
        public static void BindResults(RectTransform root, ResultRowView rowPrefab)
        {
            var b = new Binding(root, typeof(ResultsScreenView));
            b.Set("eventTitleText", b.Child<Text>("LeftPanel/EventTitle/Label"));
            b.Set("personalBestText", b.Child<Text>("LeftPanel/PersonalBest/Value"));
            b.Set("worldRecordText", b.Child<Text>("LeftPanel/WorldRecord/Value"));
            b.Set("resultsContent", b.Child<RectTransform>("LeftPanel/Results/Viewport/Content"));
            b.Set("rowPrefab", rowPrefab);
            b.Set("watchReplayButton", b.Child<Button>("Buttons/WatchReplay"));
            b.Set("raceAgainButton", b.Child<Button>("Buttons/RaceAgain"));
            b.Set("highScoresButton", b.Child<Button>("Buttons/HighScores"));
            b.Set("practiceButton", b.Child<Button>("Buttons/Practice"));
            b.Set("mainMenuButton", b.Child<Button>("Buttons/MainMenu"));
            b.Apply();
        }

        // ---------------------------------------------------------------- rows and widgets

        public static void BindEventRow(RectTransform root)
        {
            var b = new Binding(root, typeof(EventRowView));
            b.Set("button", b.Child<Button>(""));
            b.Set("nameText", b.Child<Text>("Name"));
            b.Set("distanceText", b.Child<Text>("Distance"));
            b.Set("ticketBadge", b.Child<Transform>("TicketBadge").gameObject);
            b.Set("lockOverlay", b.Child<Transform>("Lock").gameObject);
            b.Apply();
        }

        public static void BindResultRow(RectTransform root)
        {
            var b = new Binding(root, typeof(ResultRowView));
            b.Set("rankText", b.Child<Text>("Rank"));
            b.Set("nameText", b.Child<Text>("Name"));
            b.Set("timeText", b.Child<Text>("Time"));
            b.Set("dateText", b.Child<Text>("Date"));
            b.Set("personalBestMark", b.Child<Transform>("PbMark").gameObject);
            b.Apply();
        }

        public static void BindResourceBar(RectTransform root)
        {
            var b = new Binding(root, typeof(ResourceBarView));
            b.Set("gemAmount", b.Child<Text>("PurpleGem/Amount"));
            b.Set("ticketAmount", b.Child<Text>("EntryTicket/Amount"));
            b.Set("ticketCountdown", b.Child<Text>("EntryTicket/Countdown"));
            b.Set("timedAmount", b.Child<Text>("TimedTicket/Amount"));
            b.Set("cashAmount", b.Child<Text>("Cash/Amount"));
            b.Apply();
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
