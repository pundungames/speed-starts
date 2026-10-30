using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Screen prefabs (UiScreen root, stretched full screen; layout in 2400×1080 reference pixels, CanvasScaler Expand: width varies per phone (never below the reference), so edge items are anchored to their corner and middle items are centered).
    /// Positions follow the original's reference screens (docs/gdd/referans-ekranlar): menu buttons on the right, racer
    /// controls bottom-left, the 3D racer in the middle stays free. Lists keep an empty Content: views instantiate rows.
    /// </summary>
    internal static class UiScreenPrefabs
    {
        /// <summary>UI v2 Home: art-dressed, positions from layout.json (see UiV2Prefabs).</summary>
        public static void Home(RectTransform root)
        {
            UiV2Prefabs.Home(root);
        }

        /// <summary>UI v2 race HUD: telemetry, cash, energy, cadence, exit and restart (see UiV2Prefabs). Only the buttons catch taps.</summary>
        public static void RaceHud(RectTransform root)
        {
            UiV2Prefabs.RaceHud(root);
        }
    }

    /// <summary>
    /// Popup prefabs (UiPopup root, stretched; a dim backdrop inside the prefab so it fades with the popup and blocks
    /// taps on what is below; the content sits on a centered "Window").
    /// </summary>
    internal static class UiPopupPrefabs
    {
        /// <summary>
        /// Entry gate: event name, "use an Entry Ticket" (+ remaining count), Timed Ticket status, ticket offers
        /// (Cash, rewarded ad; the view opens or greys them), "Use Timed Ticket", Close.
        /// </summary>
        public static void EntryGate(RectTransform root)
        {
            RectTransform window = PopupFrame<SpeedStars.UI.Home.EntryGateModalView>(root, new Vector2(960f, 960f));
            Kit.CloseButton(window);

            Text eventName = Kit.Label(window, "EventName", "100m dash", 76f, Kit.InkColor);
            Kit.TopBand(eventName.rectTransform, 30f, 100f, 140f);

            RectTransform useTicket = Kit.IconButton(window, "UseEntryTicket", "Use Entry Ticket", 60f, Kit.InkColor, Kit.TicketOrange, 96f,
                UiShape.Panel, out Text useLabel);
            Kit.Anchor(useTicket, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(780f, 140f));
            Kit.Stretch(useLabel.rectTransform, 128f, 16f, 4f, 52f); // icon inset + room for the Remaining line
            Text remaining = Kit.Label(useTicket, "Remaining", "Left: 0", 36f, Kit.BlueText);
            Kit.BottomBand(remaining.rectTransform, 8f, 44f);
            Kit.Margin(remaining, 128f, 0f, 16f, 0f); // same inset as the ticket label

            RectTransform timed = Kit.Node(window, "TimedTicketStatus");
            Kit.Anchor(timed, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(780f, 100f));
            Kit.Panel(timed.gameObject, Kit.SoftColor, frame: true, raycast: false);
            Image timedIcon = Kit.Icon(timed, "Icon", UiShape.Round, Kit.TimedCyan);
            Kit.Anchor(timedIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(70f, 70f));
            Text timedLabel = Kit.Label(timed, "Label", "Timed Ticket", 44f, Kit.InkColor, TextAnchor.MiddleLeft);
            Kit.Span(timedLabel.rectTransform, Vector2.zero, new Vector2(0.62f, 1f));
            Kit.Margin(timedLabel, 104f, 0f, 8f, 0f);
            Text timedValue = Kit.Label(timed, "Value", "-", 44f, Kit.BlueText, TextAnchor.MiddleRight);
            Kit.Span(timedValue.rectTransform, new Vector2(0.62f, 0f), Vector2.one);
            Kit.Margin(timedValue, 8f, 0f, 24f, 0f);

            // Starts the Timed Ticket window; the view shows it only with a ticket in stock and no window running.
            RectTransform useTimed = Kit.TextButton(window, "UseTimedTicket", "Use Timed Ticket", 52f, Kit.InkColor);
            Kit.Anchor(useTimed, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -440f), new Vector2(780f, 100f));
            useTimed.gameObject.SetActive(false);

            RectTransform offers = Kit.Node(window, "GetTickets");
            Kit.Anchor(offers, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(860f, 300f));
            Text offersCaption = Kit.Label(offers, "Caption", "Get tickets", 48f, Kit.InkColor);
            Kit.TopBand(offersCaption.rectTransform, 0f, 64f);

            RectTransform cash = Kit.IconButton(offers, "BuyWithCash", "Cash", 56f, Kit.InkColor, Kit.CashGreen, 80f, UiShape.Round);
            Kit.Anchor(cash, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(415f, 200f));
            Text cashPrice = Kit.Label(cash, "Price", "0", 44f, Kit.BlueText);
            Kit.BottomBand(cashPrice.rectTransform, 12f, 52f);

            RectTransform ad = Kit.IconButton(offers, "WatchAd", "Watch ad", 52f, Kit.InkColor, Kit.TicketOrange, 80f, UiShape.Round);
            Kit.Anchor(ad, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(415f, 200f));
            ad.GetComponent<Button>().interactable = false;

            UiViewBinder.BindEntryGate(root);
        }

        private static RectTransform PopupFrame<TPopup>(RectTransform root, Vector2 windowSize) where TPopup : UiPopup
        {
            root.gameObject.AddComponent<TPopup>();

            Kit.Fill(Kit.Stretch(Kit.Node(root, "Dim")).gameObject, Kit.DimColor, raycast: true);

            RectTransform window = Kit.Node(root, "Window");
            Kit.Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, windowSize);
            Kit.Panel(window.gameObject, Kit.PanelColor, frame: true, raycast: true);
            return window;
        }
    }
}
