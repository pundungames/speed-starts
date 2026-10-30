using System;
using SpeedStars.UI.Costume;
using SpeedStars.UI.Rewards;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The daily reward prefabs (T-043, Gunluk_Odul_UI_v2 art in UI/Art/Rewards): the daily gift popup (DailyGiftPopup path),
    /// the 7 day panel (SevenDayCheckInPanel path) with its two card prefabs (RewardDayCard, RewardPremiumCard) and the ticket
    /// flight widget (RewardFlight path). Every rect comes from the package's layout.json (reference 1678x937 scaled to the
    /// canvas height); layers of one component share its art canvas at 100% scale, untrimmed. Buttons that sit on a shared
    /// canvas take their tap area from the alpha bounds of their art. Text is legacy Text, written by the views; the
    /// day 7 hat picture is NOT built in: the panel view reads it from the wardrobe's cosmetic-id mapping. Dynamic content:
    /// none (the seven cards are fixed prefab instances, visible in the Inspector). Never runs in the game.
    /// </summary>
    internal static class UiRewardPrefabs
    {
        public const string ArtFolder = UiArt.ArtRoot + "/Rewards";
        public const string DayCardPath = UiPrefabPaths.Root + "/Rows/RewardDayCard.prefab";
        public const string PremiumCardPath = UiPrefabPaths.Root + "/Rows/RewardPremiumCard.prefab";
        public const string ConfigPath = ConfigAssetSeeder.Folder + "/RewardUiConfig.asset";

        private const string Source = nameof(UiRewardPrefabs);
        private const int DayCount = 7;

        // Font sizes in canvas pixels (legacy Text best-fits down to the box).
        private const float DailyTitleSize = 64f;
        private const float DailyQuantitySize = 100f;
        private const float DailyStateSize = 56f;
        private const float DailyCapacitySize = 38f;
        private const float ButtonLabelSize = 66f;
        private const float SevenDayTitleSize = 88f;
        private const float DayLabelSize = 36f;
        private const float AmountSize = 40f;
        private const float FooterSize = 30f;
        private const float PremiumDaySize = 64f;
        private const float PremiumCaptionSize = 34f;

        // Card text boxes in the card's art canvas, reference pixels: x, y (from top), w, h.
        private static readonly float[] DayTextBox = { 14f, 12f, 128f, 40f };
        private static readonly float[] AmountBox = { 20f, 150f, 150f, 40f };
        private static readonly float[] AvailableFooterBox = { 12f, 188f, 166f, 36f };
        private static readonly float[] ClaimedFooterBox = { 58f, 188f, 118f, 36f };
        private static readonly float[] PremiumDayBox = { 24f, 18f, 220f, 64f };
        private static readonly float[] PremiumCaptionBox = { 12f, 400f, 292f, 44f };
        private static readonly float[] PremiumClaimedBox = { 70f, 420f, 220f, 36f };

        // The 7 day modal's title and X are not in layout.json: measured on preview_layers.jpg, panel-relative reference pixels.
        private static readonly float[] SevenDayTitleBox = { 100f, 20f, 800f, 104f };
        private static readonly float[] CloseBox = { 950f, 2f, 80f, 80f };

        public static void BuildAll(Func<string, Action<RectTransform>, GameObject> build, UnityEngine.SceneManagement.Scene scene)
        {
            RewardLayout layout = ReadLayout();
            RewardUiConfig config = SeedConfig(overwrite: false);
            CostumeEditorConfig costume = CostumeConfigSeed.Seed(overwrite: false);

            GameObject dayCard = build(DayCardPath, root => DayCard(root, layout));
            GameObject premiumCard = build(PremiumCardPath, root => PremiumCard(root, layout));
            UiViewBinder.RowComponent<RewardDayCardView>(dayCard, DayCardPath);
            UiViewBinder.RowComponent<RewardDayCardView>(premiumCard, PremiumCardPath);

            build(UiPrefabPaths.DailyGiftPopup, root => DailyGift(root, layout));
            build(UiPrefabPaths.SevenDayCheckInPanel, root => SevenDay(root, layout, dayCard, premiumCard, costume, scene));
            build(UiPrefabPaths.RewardFlight, root => Flight(root, layout, config));
        }

        public static RewardUiConfig SeedConfig(bool overwrite)
        {
            var asset = CreateOrReset<RewardUiConfig>(ConfigPath, overwrite);
            if (asset != null)
            {
                Log(Source, $"Created {ConfigPath} with the field defaults.");
                return asset;
            }
            return LoadRequired<RewardUiConfig>(ConfigPath, Source);
        }

        // ---------------------------------------------------------------- cards

        /// <summary>Day 1-6: layers on the 190x236 canvas, text boxes over them; the view switches layers by state.</summary>
        private static void DayCard(RectTransform root, RewardLayout layout)
        {
            Vector2 canvas = layout.GroupSize("day_card");
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, canvas * layout.Scale);
            var view = root.gameObject.AddComponent<RewardDayCardView>();

            GameObject available = UiArt.Layer(root, "Available", Art("day_card_available")).gameObject;
            GameObject claimed = UiArt.Layer(root, "Claimed", Art("day_card_claimed")).gameObject;
            GameObject locked = UiArt.Layer(root, "Locked", Art("day_card_locked")).gameObject;
            GameObject diamond = UiArt.Layer(root, "Diamond", Art("day_card_diamond")).gameObject;
            GameObject timed = UiArt.Layer(root, "TimedTicket", Art("day_card_timed_ticket")).gameObject;
            GameObject lockIcon = UiArt.Layer(root, "Lock", Art("day_card_lock")).gameObject;

            RectTransform availableFooter = Kit.Stretch(Kit.Node(root, "AvailableFooter"));
            UiArt.Layer(availableFooter, "Art", Art("day_card_available_footer"));
            Slot(availableFooter, "Label", RewardTexts.Available, FooterSize, canvas, AvailableFooterBox, Color.white);

            RectTransform claimedFooter = Kit.Stretch(Kit.Node(root, "ClaimedFooter"));
            UiArt.Layer(claimedFooter, "Art", Art("day_card_claimed_footer"));
            UiArt.Layer(claimedFooter, "Check", Art("day_card_claimed_check"));
            Slot(claimedFooter, "Label", RewardTexts.Claimed, FooterSize, canvas, ClaimedFooterBox, Color.white);

            Text day = Slot(root, "Day", RewardTexts.DayLabel(1), DayLabelSize, canvas, DayTextBox, UiArt.Navy);
            Text amount = Slot(root, "Amount", RewardTexts.CashAmount(0), AmountSize, canvas, AmountBox, UiArt.Navy);

            var so = new SerializedObject(view);
            Ref(so, "availableLook", available);
            Ref(so, "claimedLook", claimed);
            Ref(so, "lockedLook", locked);
            Ref(so, "lockIcon", lockIcon);
            Ref(so, "claimedFooter", claimedFooter.gameObject);
            Ref(so, "availableFooter", availableFooter.gameObject);
            Ref(so, "diamondIcon", diamond);
            Ref(so, "timedTicketIcon", timed);
            Ref(so, "dayText", day);
            Ref(so, "amountText", amount);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Day 7: the 316x472 card. The picture slot (icon_rect_local) is an empty Image the view fills with the real hat.</summary>
        private static void PremiumCard(RectTransform root, RewardLayout layout)
        {
            Vector2 canvas = layout.GroupSize("premium_card");
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, canvas * layout.Scale);
            var view = root.gameObject.AddComponent<RewardDayCardView>();

            GameObject available = UiArt.Layer(root, "Available", Art("premium_card_available")).gameObject;
            GameObject claimed = UiArt.Layer(root, "Claimed", Art("premium_card_claimed")).gameObject;
            GameObject locked = UiArt.Layer(root, "Locked", Art("premium_card_locked")).gameObject;
            GameObject lockIcon = UiArt.Layer(root, "Lock", Art("premium_card_lock")).gameObject;

            RectTransform item = Kit.Node(root, "Item");
            PlaceInCanvas(item, canvas, layout.PremiumIconRect);
            Image itemImage = item.gameObject.AddComponent<Image>();
            itemImage.preserveAspect = true;
            itemImage.raycastTarget = false;
            itemImage.enabled = false; // no sprite of its own: the view shows the wardrobe's picture of the item

            RectTransform claimedFooter = Kit.Stretch(Kit.Node(root, "ClaimedFooter"));
            UiArt.Layer(claimedFooter, "Check", Art("premium_card_claimed_check"));
            Slot(claimedFooter, "Label", RewardTexts.Claimed, FooterSize * 1.2f, canvas, PremiumClaimedBox, UiArt.Navy);

            Text day = Slot(root, "Day", RewardTexts.DayLabel(DayCount), PremiumDaySize, canvas, PremiumDayBox, UiArt.Navy, TextAnchor.MiddleLeft);
            Text caption = Slot(root, "Caption", string.Empty, PremiumCaptionSize, canvas, PremiumCaptionBox, UiArt.Navy);

            var so = new SerializedObject(view);
            Ref(so, "availableLook", available);
            Ref(so, "claimedLook", claimed);
            Ref(so, "lockedLook", locked);
            Ref(so, "lockIcon", lockIcon);
            Ref(so, "claimedFooter", claimedFooter.gameObject);
            Ref(so, "itemIcon", itemImage);
            Ref(so, "dayText", day);
            Ref(so, "captionText", caption);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- daily gift popup

        private static void DailyGift(RectTransform root, RewardLayout layout)
        {
            var view = root.gameObject.AddComponent<DailyGiftPopupView>();

            // The dimscreen of the package covers the whole screen and catches every tap (the popup is modal).
            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dim"));
            UiArt.PaintDim(dim.gameObject, ArtFolder + "/modal_dimscreen.png");

            Vector2 canvas = layout.GroupSize("daily_gift");
            RectTransform window = Kit.Node(root, "Window");
            PlaceWindow(window, layout, layout.DailyPanelRect);
            UiArt.Layer(window, "Panel", Art("daily_panel"));
            UiArt.Layer(window, "Header", Art("daily_header"));
            UiArt.Layer(window, "TicketIcon", Art("daily_ticket_icon"));

            Text title = Centered(window, "Title", RewardTexts.DailyGiftTitle, DailyTitleSize, canvas, layout.TitleAt, 520f, 70f, Color.white);
            Text quantity = Centered(window, "Quantity", RewardTexts.Quantity(1), DailyQuantitySize, canvas, layout.QuantityAt, 200f, 110f, UiArt.Navy);
            Text state = Centered(window, "State", RewardTexts.EntryTicket, DailyStateSize, canvas, layout.StateAt, 540f, 56f, UiArt.Navy);
            Text capacity = Centered(window, "Capacity", RewardTexts.Capacity(0, 0), DailyCapacitySize, canvas, layout.CapacityAt, 400f, 40f, UiArt.Navy);

            // One button on the shared 620x600 canvas: tap area = the visible bounds of its art; CLAIM and OK art have the same bounds.
            string claimArt = "daily_button_claim";
            Vector4 bounds = UiArt.AlphaBounds(ArtFolder + "/" + claimArt + ".png");
            RectTransform action = Kit.Span(Kit.Node(window, "Action"), new Vector2(bounds.x, bounds.y), new Vector2(bounds.z, bounds.w));
            Kit.Fill(action.gameObject, new Color(1f, 1f, 1f, 0f), raycast: true);
            Image art = UiArt.CanvasLayer(action, "Art", Art(claimArt), bounds);
            Button button = Kit.Clickable(action.gameObject, art);
            SpriteSwap(button, "daily_button_pressed", "daily_button_disabled");
            Text label = UiArt.Slot(action, "Label", RewardTexts.Claim, ButtonLabelSize, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), TextAnchor.MiddleCenter, Color.white);

            var so = new SerializedObject(view);
            Ref(so, "titleText", title);
            Ref(so, "quantityText", quantity);
            Ref(so, "stateText", state);
            Ref(so, "capacityText", capacity);
            Ref(so, "actionButton", button);
            Ref(so, "actionArt", art);
            Ref(so, "actionLabel", label);
            Ref(so, "claimSprite", Art(claimArt));
            Ref(so, "okSprite", Art("daily_button_ok"));
            Ref(so, "claimPressedSprite", Art("daily_button_pressed"));
            so.ApplyModifiedPropertiesWithoutUndo();
            // "flight" is a scene object (the RewardFlight instance): SceneWiring binds it.
        }

        // ---------------------------------------------------------------- 7 day panel

        private static void SevenDay(RectTransform root, RewardLayout layout, GameObject dayCard, GameObject premiumCard,
            CostumeEditorConfig costume, UnityEngine.SceneManagement.Scene scene)
        {
            if (costume == null) throw new InvalidOperationException($"{Source}: CostumeEditorConfig asset is missing (CostumeConfigSeed).");
            var view = root.gameObject.AddComponent<SevenDayPanelView>();

            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dim"));
            UiArt.PaintDim(dim.gameObject, ArtFolder + "/modal_dimscreen.png");

            RectTransform window = Kit.Node(root, "Window");
            PlaceWindow(window, layout, layout.SevenDayPanelRect);
            UiArt.Layer(window, "Panel", Art("seven_day_panel"));

            Vector2 panelSize = new Vector2(layout.SevenDayPanelRect[2], layout.SevenDayPanelRect[3]);
            Text title = Slot(window, "Title", RewardTexts.SevenDayTitle, SevenDayTitleSize, panelSize, SevenDayTitleBox, UiArt.Navy);

            // Cards: fixed prefab instances at the layout.json positions (relative to the panel's top-left corner).
            var cards = new RewardDayCardView[DayCount];
            for (int i = 0; i < layout.Cards.Length && i < DayCount - 1; i++)
            {
                RewardLayout.Card card = layout.Cards[i];
                cards[card.day - 1] = NestCard(dayCard, window, "Day" + card.day, scene, layout,
                    card.position[0] - layout.SevenDayPanelRect[0], card.position[1] - layout.SevenDayPanelRect[1], layout.GroupSize("day_card"));
            }
            cards[DayCount - 1] = NestCard(premiumCard, window, "Day" + DayCount, scene, layout,
                layout.PremiumRect[0] - layout.SevenDayPanelRect[0], layout.PremiumRect[1] - layout.SevenDayPanelRect[1], layout.GroupSize("premium_card"));

            RectTransform close = Kit.Node(window, "Close");
            PlaceTopLeft(close, layout, CloseBox);
            Image closeBackground = UiArt.Paint(close.gameObject, Art("close_background"), raycast: true);
            UiArt.Layer(close, "Icon", Art("close_icon"));
            Kit.Clickable(close.gameObject, closeBackground);

            RectTransform claim = Kit.Node(window, "Claim");
            float[] claimBox =
            {
                layout.ClaimRect[0] - layout.SevenDayPanelRect[0], layout.ClaimRect[1] - layout.SevenDayPanelRect[1],
                layout.ClaimRect[2], layout.ClaimRect[3]
            };
            PlaceTopLeft(claim, layout, claimBox);
            Image claimArt = UiArt.Paint(claim.gameObject, Art("button_claim"), raycast: true);
            Button claimButton = Kit.Clickable(claim.gameObject, claimArt);
            SpriteSwap(claimButton, "button_pressed", "button_disabled");
            Text claimLabel = UiArt.Slot(claim, "Label", RewardTexts.Claim, ButtonLabelSize, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), TextAnchor.MiddleCenter, Color.white);

            var so = new SerializedObject(view);
            Ref(so, "titleText", title);
            Ref(so, "claimButton", claimButton);
            Ref(so, "claimLabel", claimLabel);
            Ref(so, "costumeConfig", costume);
            SerializedProperty array = Find(so, "dayCards");
            array.arraySize = DayCount;
            for (int i = 0; i < DayCount; i++)
            {
                if (cards[i] == null) throw new InvalidOperationException($"{Source}: day card {i + 1} was not built (layout.json lists {layout.Cards.Length} small cards).");
                array.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            UiViewBinder.BindClose(root, "Window/Close"); // base UiPanel.closeButton: the X closes the popup like every other
        }

        /// <summary>Instance of a card prefab at a panel-relative top-left position; the change is recorded so it survives the save.</summary>
        private static RewardDayCardView NestCard(GameObject prefab, RectTransform window, string name, UnityEngine.SceneManagement.Scene scene,
            RewardLayout layout, float x, float y, Vector2 size)
        {
            RectTransform card = Kit.Nest(prefab, window, name, scene);
            if (card == null) throw new InvalidOperationException($"{Source}: card prefab for '{name}' is missing.");
            PlaceTopLeft(card, layout, new[] { x, y, size.x, size.y });
            Kit.Record(card);
            return card.GetComponent<RewardDayCardView>();
        }

        // ---------------------------------------------------------------- ticket flight

        /// <summary>
        /// The flight widget: a full-screen invisible blocker (taps wait until the ticket has landed) and the flying ticket in
        /// the middle. The root is the overlay-canvas scene object that starts inactive; the view switches it on for a flight.
        /// </summary>
        private static void Flight(RectTransform root, RewardLayout layout, RewardUiConfig config)
        {
            var view = root.gameObject.AddComponent<TicketFlightView>();
            root.gameObject.AddComponent<CanvasGroup>(); // blocks raycasts while the widget is on

            RectTransform blocker = Kit.Stretch(Kit.Node(root, "Blocker"));
            Kit.Fill(blocker.gameObject, new Color(1f, 1f, 1f, 0f), raycast: true);

            RectTransform icon = Kit.Node(root, "Icon");
            Kit.Anchor(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, layout.GroupSize("flight_ticket") * layout.Scale);
            UiArt.Paint(icon.gameObject, Art("ticket_flight_icon"), raycast: false);
            icon.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            Ref(so, "config", config);
            Ref(so, "icon", icon);
            so.ApplyModifiedPropertiesWithoutUndo();
            // "resourceBar" is a scene object: SceneWiring binds it.
        }

        // ---------------------------------------------------------------- geometry helpers

        /// <summary>The window of a modal: centre-anchored, sized and offset from the layout rect (reference pixels, origin top-left).</summary>
        private static void PlaceWindow(RectTransform window, RewardLayout layout, int[] rect)
        {
            float s = layout.Scale;
            float centerX = rect[0] + rect[2] * 0.5f - layout.RefWidth * 0.5f;
            float centerY = rect[1] + rect[3] * 0.5f - layout.RefHeight * 0.5f;
            Kit.Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(centerX * s, -centerY * s), new Vector2(rect[2], rect[3]) * s);
        }

        /// <summary>Top-left anchored child at a reference-pixel rect (x, y from the parent's top-left corner, w, h).</summary>
        private static void PlaceTopLeft(RectTransform rt, RewardLayout layout, float[] box)
        {
            float s = layout.Scale;
            var corner = new Vector2(0f, 1f);
            Kit.Place(rt, corner, corner, corner, new Vector2(box[0] * s, -box[1] * s), new Vector2(box[2], box[3]) * s);
        }

        /// <summary>A child covering a rect of its parent's art canvas (fractions, so it follows the parent's size).</summary>
        private static void PlaceInCanvas(RectTransform rt, Vector2 canvas, int[] rect)
        {
            Kit.Span(rt, new Vector2(rect[0] / canvas.x, 1f - (rect[1] + rect[3]) / canvas.y), new Vector2((rect[0] + rect[2]) / canvas.x, 1f - rect[1] / canvas.y));
        }

        /// <summary>Navy (or given colour) text over a box of the parent's art canvas: x, y from the top-left, w, h in reference pixels.</summary>
        private static Text Slot(Transform parent, string name, string text, float size, Vector2 canvas, float[] box, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            Vector2 min = new Vector2(box[0] / canvas.x, 1f - (box[1] + box[3]) / canvas.y);
            Vector2 max = new Vector2((box[0] + box[2]) / canvas.x, 1f - box[1] / canvas.y);
            return UiArt.Slot(parent, name, text, size, min, max, alignment, color);
        }

        /// <summary>The same, for a box given by its centre (layout.json "texts") and a size.</summary>
        private static Text Centered(Transform parent, string name, string text, float size, Vector2 canvas, int[] center, float width, float height, Color color)
        {
            return Slot(parent, name, text, size, canvas, new[] { center[0] - width * 0.5f, center[1] - height * 0.5f, width, height }, color);
        }

        private static void SpriteSwap(Button button, string pressed, string disabled)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Art(pressed), disabledSprite = Art(disabled) };
        }

        // ---------------------------------------------------------------- layout.json

        [Serializable]
        private sealed class RewardLayout
        {
            [Serializable]
            public sealed class Group
            {
                public string name;
                public int[] size;
            }

            [Serializable]
            public sealed class Card
            {
                public int day;
                public int[] position;
            }

            [Serializable]
            public sealed class Premium
            {
                public int[] rect;
                public int[] icon_rect_local;
            }

            [Serializable]
            public sealed class SevenDayPart
            {
                public int[] panel_rect;
                public Card[] cards;
                public Premium premium;
                public int[] claim_rect;
            }

            [Serializable]
            public sealed class DailyTexts
            {
                public int[] title;
                public int[] quantity;
                public int[] state;
                public int[] capacity;
                public int[] button;
            }

            [Serializable]
            public sealed class DailyPart
            {
                public int[] panel_rect;
                public DailyTexts texts;
            }

            public int[] reference_size;
            public Group[] groups;
            public SevenDayPart seven_day;
            public DailyPart daily_gift;

            public float RefWidth => reference_size[0];
            public float RefHeight => reference_size[1];
            public float Scale => UiArt.CanvasHeight / RefHeight;
            public Card[] Cards => seven_day.cards;
            public int[] SevenDayPanelRect => seven_day.panel_rect;
            public int[] PremiumRect => seven_day.premium.rect;
            public int[] PremiumIconRect => seven_day.premium.icon_rect_local;
            public int[] ClaimRect => seven_day.claim_rect;
            public int[] DailyPanelRect => daily_gift.panel_rect;
            public int[] TitleAt => daily_gift.texts.title;
            public int[] QuantityAt => daily_gift.texts.quantity;
            public int[] StateAt => daily_gift.texts.state;
            public int[] CapacityAt => daily_gift.texts.capacity;

            public Vector2 GroupSize(string group)
            {
                foreach (Group g in groups)
                    if (g.name == group) return new Vector2(g.size[0], g.size[1]);
                throw new InvalidOperationException($"{Source}: layout.json has no group '{group}'.");
            }
        }

        private static RewardLayout ReadLayout()
        {
            string path = ArtFolder + "/layout.json";
            string full = Kit.FullPath(path);
            if (!System.IO.File.Exists(full)) throw new InvalidOperationException($"{Source}: '{path}' not found.");
            var layout = JsonUtility.FromJson<RewardLayout>(System.IO.File.ReadAllText(full));
            if (layout == null || layout.reference_size == null || layout.reference_size.Length < 2 || layout.groups == null
                || layout.seven_day == null || layout.seven_day.cards == null || layout.seven_day.premium == null || layout.daily_gift == null
                || layout.daily_gift.texts == null)
                throw new InvalidOperationException($"{Source}: '{path}' could not be read.");
            return layout;
        }

        // ---------------------------------------------------------------- assets and bindings

        private static Sprite Art(string file)
        {
            string path = ArtFolder + "/" + file + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"{Source}: sprite '{path}' not found or not imported as a Sprite.");
            return sprite;
        }

        private static SerializedProperty Find(SerializedObject so, string field)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null) throw new InvalidOperationException($"{Source}: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            return property;
        }

        /// <summary>Sets an object reference field; nothing silently stays unbound.</summary>
        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            if (value == null) throw new InvalidOperationException($"{Source}: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            Find(so, field).objectReferenceValue = value;
        }
    }
}
