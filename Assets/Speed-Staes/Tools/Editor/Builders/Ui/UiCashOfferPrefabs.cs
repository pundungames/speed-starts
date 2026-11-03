using System;
using SpeedStars.UI.Home;
using SpeedStars.UI.Rewards;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The Cash offer window (CashOfferModal path, economy v2): opened by the "+" of the resource bar's Cash box. It uses the same
    /// window language as the entry gate and the daily gift popup (modal_dimscreen, daily_panel, daily_header, close X, red claim
    /// button, loading spinner) from UI/Art/Rewards, and the Cash diamond of the race HUD art. Positions are art pixels of the window
    /// (origin top-left) converted to anchors, so the window scales as one. There is no real-money row. Text is legacy Text written by
    /// <see cref="CashOfferModalView"/>. A flight diamond sits last in the prefab, inactive. Never runs in the game.
    /// </summary>
    internal static class UiCashOfferPrefabs
    {
        public const string PrefabPath = UiPrefabPaths.Root + "/Popups/CashOfferModal.prefab";

        private const string Source = nameof(UiCashOfferPrefabs);
        private const string ArtFolder = UiArt.ArtRoot + "/Rewards";

        // Same reference as the entry gate: art pixels are drawn at canvas height / this.
        private const float ReferenceHeight = 937f;
        private static readonly Vector2 WindowCanvas = new Vector2(620f, 560f);
        private static float Scale => UiArt.CanvasHeight / ReferenceHeight;

        // Font sizes in art pixels (legacy Text best-fits down to its box).
        private const float TitleSize = 50f;
        private const float SmallCaptionSize = 28f;
        private const float BalanceSize = 56f;
        private const float AmountSize = 80f;
        private const float OfferCaptionSize = 30f;
        private const float CounterSize = 44f;
        private const float ButtonLabelSize = 42f;
        private const float NoteSize = 26f;

        public static void BuildAll(Func<string, Action<RectTransform>, GameObject> build)
        {
            build(PrefabPath, CashOffer);
        }

        public static void CashOffer(RectTransform root)
        {
            var view = root.gameObject.AddComponent<CashOfferModalView>();

            // The dimscreen covers the whole screen and catches every tap (the modal blocks what is under it).
            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dim"));
            UiArt.PaintDim(dim.gameObject, ArtFolder + "/modal_dimscreen.png");

            RectTransform window = Kit.Node(root, "Window");
            Kit.Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, WindowCanvas * Scale);
            UiArt.Paint(Kit.Stretch(Kit.Node(window, "Panel")).gameObject, Art("daily_panel"), raycast: true); // blocks taps inside the window

            // Header, as on the entry gate (it starts at the top of the 620x600 art canvas, so y is unchanged).
            Vector4 header = UiArt.AlphaBounds(ArtFolder + "/daily_header.png");
            RectTransform headerBox = Box(window, "Header", header.x * 620f, (1f - header.w) * 600f, (header.z - header.x) * 620f, (header.w - header.y) * 600f);
            UiArt.CanvasLayer(headerBox, "Art", Art("daily_header"), header);
            Caption(window, "Title", "GET CASH", TitleSize, 100f, 30f, 420f, 68f, Color.white);

            // Balance row: the player's Cash. The flight diamond lands here.
            RectTransform balance = Box(window, "Balance", 130f, 128f, 360f, 70f);
            Caption(balance, new Vector2(360f, 70f), "Caption", "Your Cash", SmallCaptionSize, 0f, 0f, 360f, 26f, UiArt.Navy);
            CashIcon(balance, new Vector2(360f, 70f), "Icon", 70f, 28f, 42f);
            Caption(balance, new Vector2(360f, 70f), "Value", "0", BalanceSize, 130f, 26f, 230f, 44f, UiArt.Navy, TextAnchor.MiddleLeft);

            // Offer row: one ad, what it pays, how many are used today.
            RectTransform offer = Box(window, "Offer", 60f, 222f, 500f, 150f);
            Vector2 offerCanvas = new Vector2(500f, 150f);
            CashIcon(offer, offerCanvas, "Icon", 24f, 22f, 76f);
            Caption(offer, offerCanvas, "Amount", "+200", AmountSize, 120f, 10f, 230f, 100f, UiArt.Navy, TextAnchor.MiddleLeft);
            Caption(offer, offerCanvas, "Caption", "Watch an ad", OfferCaptionSize, 24f, 108f, 330f, 36f, UiArt.Navy, TextAnchor.MiddleLeft);
            Caption(offer, offerCanvas, "Counter", "0/3", CounterSize, 370f, 36f, 110f, 64f, UiArt.Navy, TextAnchor.MiddleRight);

            // The red claim button: WATCH AD, with the loading spinner on top while an ad runs.
            RectTransform watch = Box(window, "WatchAd", 135f, 392f, 350f, 82f);
            const string claimArt = "daily_button_claim";
            Vector4 claimBounds = UiArt.AlphaBounds(ArtFolder + "/" + claimArt + ".png");
            Kit.Fill(watch.gameObject, new Color(1f, 1f, 1f, 0f), raycast: true);
            Image watchArt = UiArt.CanvasLayer(watch, "Art", Art(claimArt), claimBounds);
            Button watchButton = Kit.Clickable(watch.gameObject, watchArt);
            watchButton.transition = Selectable.Transition.SpriteSwap;
            watchButton.spriteState = new SpriteState { pressedSprite = Art("daily_button_pressed"), disabledSprite = Art("daily_button_disabled") };
            Text watchLabel = UiArt.Slot(watch, "Label", "WATCH AD", ButtonLabelSize * Scale, new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.84f),
                TextAnchor.MiddleCenter, Color.white);
            RectTransform spinner = Kit.Node(watch, "Spinner");
            Kit.Anchor(spinner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f) * Scale);
            UiArt.Paint(spinner.gameObject, Art("loading_spinner"), raycast: false);
            spinner.gameObject.SetActive(false);

            // Why the button is closed (limit reached, no ad); the view keeps it hidden while the button works.
            Text note = Caption(window, "Note", "-", NoteSize, 40f, 490f, 540f, 50f, UiArt.Navy);
            note.gameObject.SetActive(false);

            // Close X: the corner of the window, drawn above everything else.
            RectTransform close = Box(window, "Close", 548f, -20f, 72f, 72f);
            Image closeBackground = UiArt.Paint(close.gameObject, Art("close_background"), raycast: true);
            UiArt.Layer(close, "Icon", Art("close_icon"));
            Kit.Clickable(close.gameObject, closeBackground);

            // The flying diamond goes last so it draws over the whole window; inactive until the view plays it.
            RectTransform flightNode = Kit.Node(root, "CashFlight");
            Sprite diamond = UiArt.RaceHud("cash_diamond_icon");
            Kit.Anchor(flightNode, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, diamond.rect.size * 1.4f);
            UiArt.Paint(flightNode.gameObject, diamond, raycast: false);
            var flight = flightNode.gameObject.AddComponent<CashFlightIcon>();
            flightNode.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            Ref(so, "window", window);
            Ref(so, "titleText", window.Find("Title").GetComponent<Text>());
            Ref(so, "balanceText", balance.Find("Value").GetComponent<Text>());
            Ref(so, "balanceBox", balance);
            Ref(so, "offerIcon", (RectTransform)offer.Find("Icon"));
            Ref(so, "amountText", offer.Find("Amount").GetComponent<Text>());
            Ref(so, "captionText", offer.Find("Caption").GetComponent<Text>());
            Ref(so, "counterText", offer.Find("Counter").GetComponent<Text>());
            Ref(so, "watchAdButton", watchButton);
            Ref(so, "watchAdLabel", watchLabel);
            Ref(so, "watchAdSpinner", spinner);
            Ref(so, "noteText", note);
            Ref(so, "flight", flight);
            so.ApplyModifiedPropertiesWithoutUndo();
            UiViewBinder.BindClose(root, "Window/Close"); // base UiPanel.closeButton
        }

        // ---------------------------------------------------------------- pieces

        /// <summary>The Cash diamond of the race HUD art at a given height, keeping the proportions of the picture.</summary>
        private static void CashIcon(RectTransform parent, Vector2 canvas, string name, float x, float y, float height)
        {
            Sprite sprite = UiArt.RaceHud("cash_diamond_icon");
            float width = height * sprite.rect.width / sprite.rect.height;
            RectTransform box = Box(parent, name, canvas, x, y, width, height);
            UiArt.Paint(box.gameObject, sprite, raycast: false).preserveAspect = true;
        }

        /// <summary>Text over a box of the window canvas (art pixels, origin top-left); the font size is given in art pixels too.</summary>
        private static Text Caption(RectTransform parent, string name, string text, float size, float x, float y, float w, float h,
            Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            return Caption(parent, WindowCanvas, name, text, size, x, y, w, h, color, alignment);
        }

        private static Text Caption(RectTransform parent, Vector2 canvas, string name, string text, float size, float x, float y, float w, float h,
            Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            Vector2 min = new Vector2(x / canvas.x, 1f - (y + h) / canvas.y);
            Vector2 max = new Vector2((x + w) / canvas.x, 1f - y / canvas.y);
            return UiArt.Slot(parent, name, text, size * Scale, min, max, alignment, color);
        }

        private static RectTransform Box(RectTransform parent, string name, float x, float y, float w, float h) =>
            Box(parent, name, WindowCanvas, x, y, w, h);

        /// <summary>An empty node covering a box of the parent canvas (fractions, so it follows the parent's size).</summary>
        private static RectTransform Box(RectTransform parent, string name, Vector2 canvas, float x, float y, float w, float h)
        {
            RectTransform node = Kit.Node(parent, name);
            return Kit.Span(node, new Vector2(x / canvas.x, 1f - (y + h) / canvas.y), new Vector2((x + w) / canvas.x, 1f - y / canvas.y));
        }

        private static Sprite Art(string file)
        {
            string path = ArtFolder + "/" + file + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"{Source}: sprite '{path}' not found or not imported as a Sprite.");
            return sprite;
        }

        /// <summary>Sets an object reference field; nothing silently stays unbound.</summary>
        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"{Source}: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            if (value == null)
                throw new InvalidOperationException($"{Source}: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            property.objectReferenceValue = value;
        }
    }
}
