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
        private const float TitleSize = 64f;
        private const float EventNameSize = 34f;
        private const float CountSize = 64f;
        private const float RefillSize = 28f;
        private const float RemainingSize = 30f;
        private const float TimedLabelSize = 36f;
        private const float TimedValueSize = 40f;
        private const float PrimaryLabelSize = 52f;
        private const float SecondaryLabelSize = 30f;
        private const float CashLabelSize = 24f;
        private const float CashPriceSize = 30f;
        private const float CaptionSize = 32f;

        public static void EntryGate(RectTransform root)
        {
            root.gameObject.AddComponent<EntryGateModalView>(); // its fields are bound by UiViewBinder.BindEntryGate below

            // The dimscreen covers the whole screen and catches every tap (the modal blocks what is under it).
            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dim"));
            UiArt.Paint(dim.gameObject, Art("modal_dimscreen"), raycast: true);

            RectTransform window = Kit.Node(root, "Window");
            Kit.Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, WindowCanvas * Scale);
            UiArt.Paint(Kit.Stretch(Kit.Node(window, "Panel")).gameObject, Art("daily_panel"), raycast: true); // blocks taps inside the window

            // Header: native place on the 620x600 canvas of the daily panel (it starts at the top, so y is unchanged).
            Vector4 header = UiArt.AlphaBounds(ArtFolder + "/daily_header.png");
            RectTransform headerBox = Box(window, "Header", WindowCanvas, header.x * 620f, (1f - header.w) * 600f, (header.z - header.x) * 620f, (header.w - header.y) * 600f);
            UiArt.CanvasLayer(headerBox, "Art", Art("daily_header"), header);
            Caption(window, WindowCanvas, "Title", "RACE ENTRY", TitleSize, 60f, 28f, 500f, 72f, Color.white);

            Caption(window, WindowCanvas, "EventName", "100m dash", EventNameSize, 60f, 114f, 500f, 38f, UiArt.Navy);

            // Ticket row: the red Entry Ticket and "Tickets 2/3".
            Fit(window, WindowCanvas, "TicketIcon", "daily_ticket_icon", 70f, 160f, 80f);
            Caption(window, WindowCanvas, "TicketCount", "Tickets 0/0", CountSize, 175f, 156f, 415f, 90f, UiArt.Navy, TextAnchor.MiddleLeft);
            Caption(window, WindowCanvas, "Refill", "Next ticket in 00:00", RefillSize, 60f, 250f, 500f, 30f, UiArt.Navy);
            Caption(window, WindowCanvas, "Remaining", "Left: 0", RemainingSize, 60f, 282f, 500f, 32f, UiArt.Navy);

            // Timed Ticket row (a status, not a button).
            RectTransform timed = Box(window, "TimedTicketStatus", WindowCanvas, 60f, 326f, TimedCanvas.x, TimedCanvas.y);
            Fit(timed, TimedCanvas, "Icon", "day_card_timed_ticket", 0f, 4f, 58f);
            Caption(timed, TimedCanvas, "Label", "Timed Ticket", TimedLabelSize, 84f, 0f, 236f, 66f, UiArt.Navy, TextAnchor.MiddleLeft);
            Caption(timed, TimedCanvas, "Value", "0", TimedValueSize, 330f, 0f, 170f, 66f, UiArt.Navy, TextAnchor.MiddleRight);

            PrimaryButton(window, "UseEntryTicket", "USE TICKET", 135f, 404f);

            // Starts the Timed Ticket window; the view shows it only with a ticket in stock and no window running.
            RectTransform useTimed = SecondaryButton(window, WindowCanvas, "UseTimedTicket", 170f, 494f, 280f, 66f);
            Caption(useTimed, new Vector2(280f, 66f), "Label", "USE TIMED TICKET", SecondaryLabelSize, 14f, 6f, 252f, 54f, UiArt.Navy);
            useTimed.gameObject.SetActive(false);

            RectTransform offers = Box(window, "GetTickets", WindowCanvas, 40f, 570f, GetTicketsCanvas.x, GetTicketsCanvas.y);
            Caption(offers, GetTicketsCanvas, "Caption", "GET TICKETS", CaptionSize, 0f, 0f, 540f, 36f, UiArt.Navy);

            RectTransform cash = SecondaryButton(offers, GetTicketsCanvas, "BuyWithCash", 0f, 42f, 266f, 62f);
            Vector2 cashCanvas = new Vector2(266f, 62f);
            Caption(cash, cashCanvas, "Label", "BUY WITH CASH", CashLabelSize, 12f, 3f, 242f, 30f, UiArt.Navy);
            Caption(cash, cashCanvas, "Price", "0", CashPriceSize, 12f, 31f, 242f, 28f, UiArt.Navy);

            RectTransform ad = SecondaryButton(offers, GetTicketsCanvas, "WatchAd", 274f, 42f, 266f, 62f);
            Caption(ad, cashCanvas, "Label", "WATCH AD", SecondaryLabelSize, 12f, 6f, 242f, 50f, UiArt.Navy);
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

        /// <summary>The big red button on the shared 620x600 canvas: tap area = the visible bounds of its art; pressed and disabled art swap in.</summary>
        private static void PrimaryButton(RectTransform window, string name, string label, float x, float y)
        {
            const string claimArt = "daily_button_claim";
            Vector4 bounds = UiArt.AlphaBounds(ArtFolder + "/" + claimArt + ".png");
            RectTransform action = Box(window, name, WindowCanvas, x, y, 350f, 82f);
            Kit.Fill(action.gameObject, new Color(1f, 1f, 1f, 0f), raycast: true);
            Image art = UiArt.CanvasLayer(action, "Art", Art(claimArt), bounds);
            Button button = Kit.Clickable(action.gameObject, art);
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Art("daily_button_pressed"), disabledSprite = Art("daily_button_disabled") };
            UiArt.Slot(action, "Label", label, PrimaryLabelSize * Scale, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), TextAnchor.MiddleCenter, Color.white);
        }

        /// <summary>A cream secondary button (own 350x82 canvas, so its whole picture is the tap area); the label is added by the caller.</summary>
        private static RectTransform SecondaryButton(RectTransform parent, Vector2 canvas, string name, float x, float y, float w, float h)
        {
            RectTransform node = Box(parent, name, canvas, x, y, w, h);
            Image background = UiArt.Paint(node.gameObject, Art("button_secondary"), raycast: true);
            UiArt.MakeButton(node.gameObject, background, interactable: true, alphaHit: false);
            return node;
        }

        /// <summary>A picture drawn at a given height, keeping the proportions of its visible pixels (its art canvas may be larger).</summary>
        private static RectTransform Fit(RectTransform parent, Vector2 canvas, string name, string file, float x, float y, float height)
        {
            Sprite sprite = Art(file);
            Vector4 bounds = UiArt.AlphaBounds(ArtFolder + "/" + file + ".png");
            float visibleWidth = (bounds.z - bounds.x) * sprite.rect.width;
            float visibleHeight = (bounds.w - bounds.y) * sprite.rect.height;
            RectTransform box = Box(parent, name, canvas, x, y, height * visibleWidth / visibleHeight, height);
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
