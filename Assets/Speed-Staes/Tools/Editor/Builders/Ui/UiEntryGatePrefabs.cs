using System;
using SpeedStars.UI.Home;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The entry gate modal (EntryGateModal path, T-050), dressed with the Gunluk_Odul_UI_v2 art in UI/Art/Rewards: the same
    /// window language as the daily gift popup (modal_dimscreen, daily_panel, daily_header, close X, red ticket, claim button).
    /// The window is the daily panel art stretched taller (620x700 art pixels: the daily panel is 620x600 and has no room for
    /// the Timed Ticket row and the two get-tickets buttons); every other piece keeps its native proportions. Positions are
    /// measured on approved_daily_available.jpg (title centre 310,64; claim button 350x82) and written as art pixels of the
    /// window (origin top-left), converted to anchors so the window scales as one. Buttons that sit on a shared canvas take
    /// their tap area from the alpha bounds of their art. Text is legacy Text written by the view. Never runs in the game.
    /// </summary>
    internal static class UiEntryGatePrefabs
    {
        private const string Source = nameof(UiEntryGatePrefabs);
        private const string ArtFolder = UiArt.ArtRoot + "/Rewards";

        // layout.json reference screen height: art pixels are drawn at canvas height / this (same scale as the daily gift popup).
        private const float ReferenceHeight = 937f;
        private static readonly Vector2 WindowCanvas = new Vector2(620f, 700f);
        private static readonly Vector2 GetTicketsCanvas = new Vector2(540f, 104f);
        private static readonly Vector2 TimedCanvas = new Vector2(500f, 66f);
        private static float Scale => UiArt.CanvasHeight / ReferenceHeight;

        // Font sizes in art pixels (legacy Text best-fits down to its box).
        private const float TitleSize = 50f;
        private const float EventNameSize = 34f;
        private const float CountSize = 64f;
        private const float RefillSize = 28f;
        private const float RemainingSize = 28f;
        // Share of the ticket picture's visible height cut off at the bottom: the little navy tab under the red ticket.
        private const float TicketTabCrop = 0.10f;
        private const float ActionGap = 14f;
        private const float TimedLabelSize = 36f;
        private const float TimedValueSize = 40f;
        private const float PrimaryLabelSize = 42f;
        private const float SecondaryLabelSize = 28f;
        private const float OfferLabelSize = 26f; // BUY WITH CASH and WATCH AD: one style
        private const float CaptionSize = 32f;

        public static void EntryGate(RectTransform root)
        {
            root.gameObject.AddComponent<EntryGateModalView>(); // its fields are bound by UiViewBinder.BindEntryGate below

            // The dimscreen covers the whole screen and catches every tap (the modal blocks what is under it).
            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dim"));
            UiArt.PaintDim(dim.gameObject, ArtFolder + "/modal_dimscreen.png");

            RectTransform window = Kit.Node(root, "Window");
            Kit.Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, WindowCanvas * Scale);
            UiArt.Paint(Kit.Stretch(Kit.Node(window, "Panel")).gameObject, Art("daily_panel"), raycast: true); // blocks taps inside the window

            // Header: native place on the 620x600 canvas of the daily panel (it starts at the top, so y is unchanged).
            Vector4 header = UiArt.AlphaBounds(ArtFolder + "/daily_header.png");
            RectTransform headerBox = Box(window, "Header", WindowCanvas, header.x * 620f, (1f - header.w) * 600f, (header.z - header.x) * 620f, (header.w - header.y) * 600f);
            UiArt.CanvasLayer(headerBox, "Art", Art("daily_header"), header);
            Caption(window, WindowCanvas, "Title", "RACE ENTRY", TitleSize, 100f, 30f, 420f, 68f, Color.white); // inset: the text stays off both ends of the red band

            Caption(window, WindowCanvas, "EventName", "100m dash", EventNameSize, 60f, 114f, 500f, 38f, UiArt.Navy);

            // Ticket row: the red Entry Ticket and "Tickets 2/3".
            Fit(window, WindowCanvas, "TicketIcon", "daily_ticket_icon", 70f, 160f, 80f, TicketTabCrop);
            Caption(window, WindowCanvas, "TicketCount", "Tickets 0/0", CountSize, 175f, 156f, 415f, 90f, UiArt.Navy, TextAnchor.MiddleLeft);
            Caption(window, WindowCanvas, "Refill", "Next ticket in 00:00", RefillSize, 60f, 250f, 500f, 30f, UiArt.Navy);
            // Refusal warning only (shares the refill line's place); the view keeps it hidden otherwise.
            Caption(window, WindowCanvas, "Remaining", "-", RemainingSize, 60f, 250f, 500f, 30f, UiArt.Navy).gameObject.SetActive(false);

            // Timed Ticket row (a status, not a button).
            RectTransform timed = Box(window, "TimedTicketStatus", WindowCanvas, 60f, 292f, TimedCanvas.x, TimedCanvas.y);
            Fit(timed, TimedCanvas, "Icon", "day_card_timed_ticket", 0f, 4f, 58f);
            Caption(timed, TimedCanvas, "Label", "Timed Ticket", TimedLabelSize, 84f, 0f, 236f, 66f, UiArt.Navy, TextAnchor.MiddleLeft);
            Caption(timed, TimedCanvas, "Value", "0", TimedValueSize, 330f, 0f, 170f, 66f, UiArt.Navy, TextAnchor.MiddleRight);

            // The action buttons stack in a centred group: the hidden ones (ticket button while Timed access runs, Use Timed
            // Ticket without stock) take no room, so the window never shows a hole.
            RectTransform actions = Box(window, "Actions", WindowCanvas, 0f, 372f, WindowCanvas.x, 170f);
            var stack = actions.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.spacing = ActionGap * Scale;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = false;
            stack.childForceExpandHeight = false;

            PrimaryButton(actions, "UseEntryTicket", "USE TICKET", 350f, 82f);

            // Starts the Timed Ticket window; the view shows it only with a ticket in stock and no window running.
            RectTransform useTimed = SecondaryButton(actions, "UseTimedTicket", 280f, 66f);
            Caption(useTimed, new Vector2(280f, 66f), "Label", "USE TIMED TICKET", SecondaryLabelSize, 16f, 9f, 248f, 48f, UiArt.Navy);
            useTimed.gameObject.SetActive(false);

            RectTransform offers = Box(window, "GetTickets", WindowCanvas, 40f, 566f, GetTicketsCanvas.x, GetTicketsCanvas.y);
            Caption(offers, GetTicketsCanvas, "Caption", "GET TICKETS", CaptionSize, 0f, 0f, 540f, 36f, UiArt.Navy);

            // Both offer buttons: same label style, centred on the button's height. The cash price sits at the right end.
            Vector2 offerCanvas = new Vector2(266f, 62f);
            RectTransform cash = SecondaryButton(offers, GetTicketsCanvas, "BuyWithCash", 0f, 42f, offerCanvas.x, offerCanvas.y);
            Caption(cash, offerCanvas, "Label", "BUY WITH CASH", OfferLabelSize, 10f, 8f, 190f, 46f, UiArt.Navy);
            Caption(cash, offerCanvas, "Price", "0", OfferLabelSize, 202f, 8f, 54f, 46f, UiArt.Navy, TextAnchor.MiddleRight);

            RectTransform ad = SecondaryButton(offers, GetTicketsCanvas, "WatchAd", 274f, 42f, offerCanvas.x, offerCanvas.y);
            Caption(ad, offerCanvas, "Label", "WATCH AD", OfferLabelSize, 10f, 8f, 246f, 46f, UiArt.Navy);
            RectTransform spinner = Kit.Node(ad, "Spinner");
            Kit.Anchor(spinner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f) * Scale);
            UiArt.Paint(spinner.gameObject, Art("loading_spinner"), raycast: false);
            spinner.gameObject.SetActive(false);

            // Close X: the corner of the window, drawn above everything else.
            RectTransform close = Box(window, "Close", WindowCanvas, 548f, -20f, 72f, 72f);
            Image closeBackground = UiArt.Paint(close.gameObject, Art("close_background"), raycast: true);
            UiArt.Layer(close, "Icon", Art("close_icon"));
            Kit.Clickable(close.gameObject, closeBackground);

            UiViewBinder.BindEntryGate(root);
        }

        // ---------------------------------------------------------------- pieces

        /// <summary>
        /// The big red button inside the Actions stack (tap area = the visible bounds of its art; pressed and disabled art swap in).
        /// The label stays well inside the art's rim so it never touches the border, in any state.
        /// </summary>
        private static void PrimaryButton(RectTransform stack, string name, string label, float w, float h)
        {
            const string claimArt = "daily_button_claim";
            Vector4 bounds = UiArt.AlphaBounds(ArtFolder + "/" + claimArt + ".png");
            RectTransform action = StackItem(stack, name, w, h);
            Kit.Fill(action.gameObject, new Color(1f, 1f, 1f, 0f), raycast: true);
            Image art = UiArt.CanvasLayer(action, "Art", Art(claimArt), bounds);
            Button button = Kit.Clickable(action.gameObject, art);
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Art("daily_button_pressed"), disabledSprite = Art("daily_button_disabled") };
            UiArt.Slot(action, "Label", label, PrimaryLabelSize * Scale, new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.84f), TextAnchor.MiddleCenter, Color.white);
        }

        /// <summary>A cream secondary button inside the Actions stack (its whole picture is the tap area); the label is added by the caller.</summary>
        private static RectTransform SecondaryButton(RectTransform stack, string name, float w, float h)
        {
            RectTransform node = StackItem(stack, name, w, h);
            Image background = UiArt.Paint(node.gameObject, Art("button_secondary"), raycast: true);
            UiArt.MakeButton(node.gameObject, background, interactable: true, alphaHit: false);
            return node;
        }

        /// <summary>A cream secondary button on a shared canvas (the two get-tickets buttons).</summary>
        private static RectTransform SecondaryButton(RectTransform parent, Vector2 canvas, string name, float x, float y, float w, float h)
        {
            RectTransform node = Box(parent, name, canvas, x, y, w, h);
            Image background = UiArt.Paint(node.gameObject, Art("button_secondary"), raycast: true);
            UiArt.MakeButton(node.gameObject, background, interactable: true, alphaHit: false);
            return node;
        }

        /// <summary>A child of a layout stack with a fixed size in art pixels (canvas units after the scale).</summary>
        private static RectTransform StackItem(RectTransform stack, string name, float w, float h)
        {
            RectTransform node = Kit.Node(stack, name);
            var size = node.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = w * Scale;
            size.preferredHeight = h * Scale;
            return node;
        }

        /// <summary>A picture drawn at a given height, keeping the proportions of its visible pixels (its art canvas may be larger); cropBottom cuts that share of the visible height off the bottom.</summary>
        private static RectTransform Fit(RectTransform parent, Vector2 canvas, string name, string file, float x, float y, float height, float cropBottom = 0f)
        {
            Sprite sprite = Art(file);
            Vector4 bounds = UiArt.AlphaBounds(ArtFolder + "/" + file + ".png");
            float visibleWidth = (bounds.z - bounds.x) * sprite.rect.width;
            float visibleHeight = (bounds.w - bounds.y) * sprite.rect.height;
            float width = height * visibleWidth / visibleHeight;
            RectTransform box = Box(parent, name, canvas, x, y, width, height * (1f - cropBottom));
            if (cropBottom > 0f)
            {
                // The box ends above the art's lowest pixels and clips what hangs out below it (a stray tab in the picture).
                bounds.y += (bounds.w - bounds.y) * cropBottom;
                box.gameObject.AddComponent<RectMask2D>();
            }
            UiArt.CanvasLayer(box, "Art", sprite, bounds);
            return box;
        }

        /// <summary>Text over a box (art pixels of the parent canvas, origin top-left); the font size is given in art pixels too.</summary>
        private static Text Caption(RectTransform parent, Vector2 canvas, string name, string text, float size, float x, float y, float w, float h,
            Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            Vector2 min = new Vector2(x / canvas.x, 1f - (y + h) / canvas.y);
            Vector2 max = new Vector2((x + w) / canvas.x, 1f - y / canvas.y);
            return UiArt.Slot(parent, name, text, size * Scale, min, max, alignment, color);
        }

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
    }
}
