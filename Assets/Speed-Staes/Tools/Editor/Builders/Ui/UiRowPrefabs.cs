using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Row / cell prefabs that views instantiate into list Content parents (contract §5 "satır prefab'ı + content parent").
    /// Built before the screens so screens can nest them. State markers (Lock, Claimed, PremiumGlow, PbMark) start
    /// inactive; the view switches them per item. All texts are placeholders the view overwrites.
    /// </summary>
    internal static class UiRowPrefabs
    {
        /// <summary>Event Select row: name, distance, "ticketed" badge, lock. The whole row is the tap target.</summary>
        public static void EventRow(RectTransform root)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Home.EventRowView>();
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 120f));
            Image background = Kit.Panel(root.gameObject, Kit.PanelColor, frame: true, raycast: true);
            Kit.Clickable(root.gameObject, background);
            Kit.PreferredHeight(root.gameObject, 120f);

            Text eventName = Kit.Label(root, "Name", "100m dash", 56f, Kit.InkColor, TextAnchor.MiddleLeft);
            Kit.Span(eventName.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.58f, 1f));

            Text distance = Kit.Label(root, "Distance", "100m", 44f, Kit.BlueText);
            Kit.Span(distance.rectTransform, new Vector2(0.58f, 0f), new Vector2(0.76f, 1f));

            RectTransform badge = Kit.Span(Kit.Node(root, "TicketBadge"), new Vector2(0.78f, 0.2f), new Vector2(0.96f, 0.8f));
            Kit.Panel(badge.gameObject, Kit.HighlightColor, frame: true, raycast: false);
            Image badgeIcon = Kit.Icon(badge, "Icon", UiShape.Round, Kit.TicketOrange);
            Kit.Anchor(badgeIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(48f, 48f));
            Text badgeLabel = Kit.Label(badge, "Label", "Ticket", 32f, Kit.InkColor);
            Kit.Margin(badgeLabel, 60f, 0f, 8f, 0f);

            RectTransform lockOverlay = Kit.Stretch(Kit.Node(root, "Lock"));
            Kit.Fill(lockOverlay.gameObject, Kit.ShadeColor, raycast: false);
            Image lockIcon = Kit.Icon(lockOverlay, "Icon", UiShape.Panel, Kit.IconGrey);
            Kit.Anchor(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 72f));
            lockOverlay.gameObject.SetActive(false);

            UiViewBinder.BindEventRow(root);
        }

        /// <summary>Edit racer item cell: icon, lock, price, premium glow panel (premium items sit on top, glowing).</summary>
        public static void CosmeticCell(RectTransform root)
        {
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 250f));
            Image background = Kit.Panel(root.gameObject, Kit.PanelColor, frame: true, raycast: true);
            Kit.Clickable(root.gameObject, background);

            RectTransform glow = Kit.Stretch(Kit.Node(root, "PremiumGlow"), 8f, 8f, 8f, 8f);
            Kit.Panel(glow.gameObject, Kit.HighlightColor, frame: false, raycast: false);
            glow.gameObject.SetActive(false);

            Image icon = Kit.Icon(root, "Icon", UiShape.Round, Kit.IconGrey);
            Kit.Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(150f, 150f));

            Image lockIcon = Kit.Icon(root, "Lock", UiShape.Panel, Kit.FrameColor);
            Kit.Anchor(lockIcon.rectTransform, Vector2.one, Vector2.one, new Vector2(-14f, -14f), new Vector2(56f, 56f));
            lockIcon.gameObject.SetActive(false);

            RectTransform price = Kit.BottomBand(Kit.Node(root, "Price"), 10f, 56f, 16f);
            Image currency = Kit.Icon(price, "Icon", UiShape.Round, Kit.CashGreen);
            Kit.Anchor(currency.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
            Text amount = Kit.Label(price, "Amount", "0", 40f, Kit.InkColor, TextAnchor.MiddleLeft);
            Kit.Margin(amount, 52f, 0f, 0f, 0f);
        }

        /// <summary>7-day check-in cell: day, reward icon, claimed mark. Tappable (today's cell claims).</summary>
        public static void SevenDayCell(RectTransform root)
        {
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 260f));
            Image background = Kit.Panel(root.gameObject, Kit.PanelColor, frame: true, raycast: true);
            Kit.Clickable(root.gameObject, background);

            Text day = Kit.Label(root, "Day", "Day 1", 40f, Kit.InkColor);
            Kit.TopBand(day.rectTransform, 12f, 56f, 10f);

            Image reward = Kit.Icon(root, "RewardIcon", UiShape.Round, Kit.TicketOrange);
            Kit.Anchor(reward.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(120f, 120f));

            RectTransform claimed = Kit.Stretch(Kit.Node(root, "Claimed"));
            Kit.Fill(claimed.gameObject, Kit.ShadeColor, raycast: false);
            Image check = Kit.Icon(claimed, "Check", UiShape.Check, Kit.GreenText);
            Kit.Anchor(check.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(130f, 130f));
            claimed.gameObject.SetActive(false);
        }

        /// <summary>High score / results row: rank, flag slot, name, time, date, PB mark. Columns follow the row width.</summary>
        public static void HighScoreRow(RectTransform root)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Race.ResultRowView>();
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 64f));
            Kit.Panel(root.gameObject, Kit.PanelColor, frame: false, raycast: false);
            Kit.PreferredHeight(root.gameObject, 64f);

            Text rank = Kit.Label(root, "Rank", "1", 40f, Kit.InkColor);
            Kit.Span(rank.rectTransform, new Vector2(0f, 0f), new Vector2(0.08f, 1f));

            Image flag = Kit.Icon(root, "Flag", UiShape.Panel, Kit.IconGrey);
            Kit.Span(flag.rectTransform, new Vector2(0.09f, 0.18f), new Vector2(0.15f, 0.82f));

            Text playerName = Kit.Label(root, "Name", "Player", 40f, Kit.InkColor, TextAnchor.MiddleLeft);
            Kit.Span(playerName.rectTransform, new Vector2(0.17f, 0f), new Vector2(0.55f, 1f));

            Text time = Kit.Label(root, "Time", "0.00", 40f, Kit.InkColor);
            Kit.Span(time.rectTransform, new Vector2(0.56f, 0f), new Vector2(0.72f, 1f));

            Text date = Kit.Label(root, "Date", "--/--/----", 36f, Kit.BlueText);
            Kit.Span(date.rectTransform, new Vector2(0.73f, 0f), new Vector2(0.92f, 1f));

            Text personalBest = Kit.Label(root, "PbMark", "PB", 36f, Kit.BlueText);
            Kit.Span(personalBest.rectTransform, new Vector2(0.92f, 0f), new Vector2(1f, 1f));
            personalBest.gameObject.SetActive(false);

            UiViewBinder.BindResultRow(root);
        }
    }

    /// <summary>
    /// Widget prefabs: the resource bar (four fixed indicators, not rows), the red dot and the reward flight card.
    /// The resource bar lives on the scene's overlay canvas; screens only keep an empty slot for it.
    /// </summary>
    internal static class UiWidgetPrefabs
    {
        /// <summary>Purple Gem · Entry Ticket (count + regen countdown) · Timed Ticket · Cash, each icon + amount + "+".</summary>
        public static void ResourceBar(RectTransform root)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Home.ResourceBarView>();
            Kit.Anchor(root, Vector2.one, Vector2.one, new Vector2(-24f, -20f), new Vector2(1260f, 100f));
            Kit.Row(root.gameObject, 16f, TextAnchor.MiddleRight);

            ResourceItem(root, "PurpleGem", Kit.GemPurple, "0", 280f, countdown: false);
            ResourceItem(root, "EntryTicket", Kit.TicketOrange, "0/0", 340f, countdown: true);
            ResourceItem(root, "TimedTicket", Kit.TimedCyan, "0", 280f, countdown: false);
            ResourceItem(root, "Cash", Kit.CashGreen, "0", 300f, countdown: false);

            UiViewBinder.BindResourceBar(root);
        }

        /// <summary>Small "there is something new" dot; sits at its parent's top-right corner.</summary>
        public static void RedDot(RectTransform root)
        {
            Kit.Anchor(root, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(38f, 38f));
            Image dot = root.gameObject.AddComponent<Image>();
            dot.sprite = Kit.SpriteOf(UiShape.Round);
            dot.color = Kit.MarkerRed;
            dot.raycastTarget = false;
            Kit.Frame(root.gameObject, 2f);
        }

        /// <summary>The ticket card that flies to the resource bar after the daily gift. Never blocks taps.</summary>
        public static void RewardFlight(RectTransform root)
        {
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            Image icon = Kit.Icon(root, "Icon", UiShape.Panel, Kit.TicketOrange);
            Kit.Stretch(icon.rectTransform);
            Kit.Frame(icon.gameObject);
        }

        private static void ResourceItem(Transform bar, string name, Color iconColor, string amount, float width, bool countdown)
        {
            RectTransform item = Kit.Node(bar, name);
            item.sizeDelta = new Vector2(width, 90f);
            Kit.Panel(item.gameObject, Kit.PanelColor, frame: true, raycast: false);

            RectTransform plus = Kit.TextButton(item, "Plus", "+", 56f, Kit.InkColor);
            Kit.Anchor(plus, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(74f, 74f));

            Image icon = Kit.Icon(item, "Icon", UiShape.Round, iconColor);
            Kit.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(92f, 0f), new Vector2(64f, 64f));

            Text value = Kit.Label(item, "Amount", amount, 48f, Kit.InkColor, TextAnchor.MiddleLeft);
            Kit.Stretch(value.rectTransform, 166f, 12f, 0f, countdown ? 34f : 0f);

            if (!countdown) return;
            Text timer = Kit.Label(item, "Countdown", "00:00", 28f, Kit.BlueText, TextAnchor.MiddleLeft);
            Kit.Stretch(timer.rectTransform, 166f, 12f, 54f, 4f);
        }
    }
}
