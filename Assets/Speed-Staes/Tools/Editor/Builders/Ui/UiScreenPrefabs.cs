using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Screen prefabs (UiScreen root, stretched full screen; layout in 2400×1080 reference pixels, CanvasScaler height match = 1: width varies per phone, so edge items are anchored to their corner and middle items are centered).
    /// Positions follow the original's reference screens (docs/gdd/referans-ekranlar): menu buttons on the right, racer
    /// controls bottom-left, the 3D racer in the middle stays free. Lists keep an empty Content: views instantiate rows.
    /// Out of scope and therefore absent: Upgrade, Players, Emote, Stats, settings, banner ad, Random, Global, Find player.
    /// </summary>
    internal static class UiScreenPrefabs
    {
        public static void Home(RectTransform root, GameObject redDot, UnityEngine.SceneManagement.Scene scene)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Home.HomeScreenView>();

            // The ResourceBar widget lives on the overlay canvas; this empty slot keeps its corner free.
            Kit.Anchor(Kit.Node(root, "ResourceBarSlot"), Vector2.one, Vector2.one, new Vector2(-24f, -20f), new Vector2(1260f, 100f));

            RectTransform sevenDay = Kit.Node(root, "SevenDayReward");
            Kit.Anchor(sevenDay, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(160f, 160f));
            Image sevenDayBackground = Kit.Panel(sevenDay.gameObject, Kit.PanelColor, frame: true, raycast: true);
            Kit.Clickable(sevenDay.gameObject, sevenDayBackground);
            Image sevenDayIcon = Kit.Icon(sevenDay, "Icon", UiShape.Panel, Kit.TicketOrange);
            Kit.Anchor(sevenDayIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(90f, 90f));
            Text sevenDayLabel = Kit.Label(sevenDay, "Label", "7-Day Rewards", 26f, Kit.InkColor);
            Kit.BottomBand(sevenDayLabel.rectTransform, 8f, 40f, 6f);
            Kit.Nest(redDot, sevenDay, "RedDot", scene);

            RectTransform highScores = Kit.IconButton(root, "HighScores", "High scores", 72f, Kit.GoldText, Kit.GoldText, 90f, UiShape.Round);
            Kit.Anchor(highScores, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 470f), new Vector2(600f, 120f));

            RectTransform eventSelect = Kit.TextButton(root, "EventSelect", "100m", 76f, Kit.InkColor, out Text eventName);
            Kit.Anchor(eventSelect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 290f), new Vector2(600f, 150f));
            eventName.name = "EventName";
            Kit.Stretch(eventName.rectTransform, 16f, 16f, 54f, 10f);
            Text caption = Kit.Label(eventSelect, "Caption", "Event select:", 42f, Kit.InkColor);
            Kit.TopBand(caption.rectTransform, 6f, 48f, 16f);

            RectTransform play = Kit.IconButton(root, "Play", "Play", 150f, Kit.InkColor, Kit.FrameColor, 150f);
            Kit.Anchor(play, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(600f, 220f));

            RectTransform editRacer = Kit.IconButton(root, "EditRacer", "Edit racer", 60f, Kit.PinkText, Kit.PinkText, 70f);
            Kit.Anchor(editRacer, Vector2.zero, Vector2.zero, new Vector2(40f, 184f), new Vector2(470f, 100f));

            RectTransform preset = Kit.Node(root, "RacerPreset");
            Kit.Anchor(preset, Vector2.zero, Vector2.zero, new Vector2(40f, 40f), new Vector2(880f, 124f));
            RectTransform previous = Kit.TextButton(preset, "Previous", "<", 80f, Kit.InkColor);
            Kit.Anchor(previous, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(124f, 124f));
            RectTransform next = Kit.TextButton(preset, "Next", ">", 80f, Kit.InkColor);
            Kit.Anchor(next, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(124f, 124f));
            RectTransform presetName = Kit.Stretch(Kit.Node(preset, "Preset"), 140f, 140f, 0f, 0f);
            Kit.Panel(presetName.gameObject, Kit.PanelColor, frame: true, raycast: false);
            Image avatar = Kit.Icon(presetName, "Avatar", UiShape.Round, Kit.BlueText);
            Kit.Anchor(avatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(96f, 96f));
            Text presetLabel = Kit.Label(presetName, "Name", "Custom 1", 76f, Kit.BlueText);
            Kit.Margin(presetLabel, 120f, 0f, 16f, 0f);

            UiViewBinder.BindHome(root);
        }

        public static void EventSelect(RectTransform root, SpeedStars.UI.Home.EventRowView rowPrefab)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Home.EventSelectScreenView>();
            Kit.BackButton(root);

            RectTransform panel = Kit.Stretch(Kit.Node(root, "Panel"), 300f, 300f, 60f, 60f);
            Kit.Panel(panel.gameObject, Kit.PanelColor, frame: true, raycast: true);

            Text title = Kit.Label(panel, "Title", "Event select", 80f, Kit.InkColor);
            Kit.TopBand(title.rectTransform, 20f, 100f, 40f);

            RectTransform list = Kit.VerticalList(panel, "List", 12f);
            Kit.Stretch(list, 40f, 40f, 140f, 40f);

            UiViewBinder.BindEventSelect(root, rowPrefab);
        }

        /// <summary>
        /// Edit racer: CategoryPanel (racer name, 8 cosmetic categories + Body skin colour, Save) and ItemPanel (item grid,
        /// colour picker, 5 preset colours, price, Back / Choose). The view switches the two panels; ItemPanel starts off.
        /// </summary>
        public static void Cosmetics(RectTransform root)
        {
            root.gameObject.AddComponent<UiScreen>();
            Kit.BackButton(root);

            RectTransform categoryPanel = Kit.Node(root, "CategoryPanel");
            Kit.Anchor(categoryPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -40f), new Vector2(800f, 900f));

            RectTransform racerName = Kit.TopBand(Kit.Node(categoryPanel, "RacerName"), 0f, 120f);
            Kit.Panel(racerName.gameObject, Kit.PanelColor, frame: true, raycast: false);
            Image avatar = Kit.Icon(racerName, "Avatar", UiShape.Round, Kit.BlueText);
            Kit.Anchor(avatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(96f, 96f));
            Text nameLabel = Kit.Label(racerName, "Name", "Custom 1", 76f, Kit.BlueText);
            Kit.Margin(nameLabel, 120f, 0f, 16f, 0f);

            RectTransform categories = Kit.TopBand(Kit.Node(categoryPanel, "Categories"), 150f, 560f);
            GridLayoutGroup grid = categories.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(390f, 96f);
            grid.spacing = new Vector2(20f, 20f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            // Original order without Arms (no asset). Body = skin colour (no mesh); the rest map to the racer's
            // Accessories slots: Head=Headwear, Face=Glasses, Top=Tops, Hands=Gloves, Bottoms=Shorts.
            Kit.IconButton(categories, "Body", "Body", 60f, Kit.OrangeText, Kit.OrangeText, 70f);
            Kit.IconButton(categories, "Head", "Head", 60f, Kit.BlueText, Kit.BlueText, 70f);
            Kit.IconButton(categories, "Top", "Top", 60f, Kit.BlueText, Kit.BlueText, 70f);
            Kit.IconButton(categories, "Face", "Face", 60f, Kit.BlueText, Kit.BlueText, 70f);
            Kit.IconButton(categories, "Bottoms", "Bottoms", 60f, Kit.BlueText, Kit.BlueText, 70f);
            Kit.IconButton(categories, "Shoes", "Shoes", 60f, Kit.BlueText, Kit.BlueText, 70f);
            Kit.IconButton(categories, "Hands", "Hands", 60f, Kit.BlueText, Kit.BlueText, 70f);
            Kit.IconButton(categories, "Socks", "Socks", 60f, Kit.BlueText, Kit.BlueText, 70f);
            Kit.IconButton(categories, "Leggings", "Leggings", 60f, Kit.BlueText, Kit.BlueText, 70f);

            RectTransform save = Kit.IconButton(categoryPanel, "Save", "Save", 64f, Kit.GreenText, Kit.GreenText, 70f, UiShape.Check);
            Kit.Anchor(save, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(400f, 110f));

            RectTransform itemPanel = Kit.Stretch(Kit.Node(root, "ItemPanel"));

            RectTransform items = Kit.GridList(itemPanel, "ItemList", new Vector2(250f, 250f), new Vector2(16f, 16f), 2);
            Kit.Anchor(items, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -170f), new Vector2(548f, 640f));

            BuildColorPicker(itemPanel);

            RectTransform price = Kit.Node(itemPanel, "PriceTag");
            Kit.Anchor(price, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(20f, 170f), new Vector2(380f, 84f)); // above Choose
            Kit.Panel(price.gameObject, Kit.PanelColor, frame: true, raycast: false);
            Image currency = Kit.Icon(price, "Icon", UiShape.Round, Kit.CashGreen);
            Kit.Anchor(currency.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(56f, 56f));
            Text amount = Kit.Label(price, "Amount", "0", 52f, Kit.InkColor);
            Kit.Margin(amount, 80f, 0f, 16f, 0f);

            RectTransform itemBack = Kit.TextButton(itemPanel, "ItemBack", "Back", 60f, Kit.InkColor);
            Kit.Anchor(itemBack, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 40f), new Vector2(340f, 110f));
            RectTransform choose = Kit.IconButton(itemPanel, "Choose", "Choose", 64f, Kit.GreenText, Kit.GreenText, 70f, UiShape.Check);
            Kit.Anchor(choose, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(20f, 40f), new Vector2(380f, 110f));

            itemPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// High scores (local table): title, event selector, period (Daily / Weekly / All time), the player's own row
        /// (rank, name, time, percent) and the list. No "Global" region and no "Find player" (meaningless locally).
        /// </summary>
        public static void HighScores(RectTransform root)
        {
            root.gameObject.AddComponent<UiScreen>();
            Kit.BackButton(root);

            RectTransform panel = Kit.Stretch(Kit.Node(root, "Panel"), 300f, 300f, 40f, 40f);
            Kit.Panel(panel.gameObject, Kit.PanelColor, frame: true, raycast: true);

            Text title = Kit.Label(panel, "Title", "High scores", 84f, Kit.GoldText);
            Kit.TopBand(title.rectTransform, 20f, 100f, 140f);
            Image titleIcon = Kit.Icon(panel, "TitleIcon", UiShape.Round, Kit.GoldText);
            Kit.Anchor(titleIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -25f), new Vector2(90f, 90f));

            RectTransform filters = Kit.TopBand(Kit.Node(panel, "Filters"), 140f, 90f, 40f);

            RectTransform eventSelector = Kit.TextButton(filters, "EventSelector", "100m dash", 52f, Kit.InkColor, out Text eventLabel);
            Kit.Span(eventSelector, Vector2.zero, new Vector2(0.46f, 1f));
            eventLabel.alignment = TextAnchor.MiddleLeft;
            Kit.Stretch(eventLabel.rectTransform, 24f, 80f, 4f, 4f);
            Image arrow = Kit.Icon(eventSelector, "Arrow", UiShape.Arrow, Kit.InkColor);
            Kit.Anchor(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(52f, 52f));

            RectTransform period = Kit.Span(Kit.Node(filters, "Period"), new Vector2(0.48f, 0f), Vector2.one);
            HorizontalLayoutGroup periodRow = period.gameObject.AddComponent<HorizontalLayoutGroup>();
            periodRow.spacing = 12f;
            periodRow.childAlignment = TextAnchor.MiddleCenter;
            periodRow.childControlWidth = true;
            periodRow.childControlHeight = true;
            periodRow.childForceExpandWidth = true;
            periodRow.childForceExpandHeight = true;
            Kit.TextButton(period, "Daily", "Daily", 44f, Kit.InkColor);
            Kit.TextButton(period, "Weekly", "Weekly", 44f, Kit.InkColor);
            Kit.TextButton(period, "AllTime", "All time", 44f, Kit.InkColor);

            RectTransform playerRow = Kit.TopBand(Kit.Node(panel, "PlayerRow"), 250f, 84f, 40f);
            Kit.Panel(playerRow.gameObject, Kit.HighlightColor, frame: true, raycast: false);
            Image flag = Kit.Icon(playerRow, "Flag", UiShape.Panel, Kit.IconGrey);
            Kit.Span(flag.rectTransform, new Vector2(0.02f, 0.18f), new Vector2(0.08f, 0.82f));
            Kit.Span(Kit.Label(playerRow, "Name", "Player", 48f, Kit.InkColor, TextAnchor.MiddleLeft).rectTransform,
                new Vector2(0.1f, 0f), new Vector2(0.38f, 1f));
            Kit.Span(Kit.Label(playerRow, "Time", "0.00", 48f, Kit.InkColor).rectTransform, new Vector2(0.4f, 0f), new Vector2(0.56f, 1f));
            Kit.Span(Kit.Label(playerRow, "Rank", "#0", 48f, Kit.InkColor).rectTransform, new Vector2(0.58f, 0f), new Vector2(0.78f, 1f));
            Kit.Span(Kit.Label(playerRow, "Percent", "top 0%", 48f, Kit.InkColor).rectTransform, new Vector2(0.8f, 0f), new Vector2(0.98f, 1f));

            RectTransform list = Kit.VerticalList(panel, "List", 6f);
            Kit.Stretch(list, 40f, 40f, 360f, 30f);
        }

        /// <summary>Race HUD: race time and distance / progress bar. No pause (not in the PDF). Nothing catches taps.</summary>
        public static void RaceHud(RectTransform root)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Race.RaceHudView>();

            RectTransform raceTime = Kit.Node(root, "RaceTime");
            Kit.Anchor(raceTime, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(360f, 110f));
            Kit.Panel(raceTime.gameObject, Kit.PanelColor, frame: true, raycast: false);
            Kit.Label(raceTime, "Value", "0.00", 84f, Kit.InkColor);

            RectTransform progress = Kit.Node(root, "Progress");
            Kit.Anchor(progress, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(1100f, 64f));
            Kit.Panel(progress.gameObject, Kit.PanelColor, frame: true, raycast: false);

            RectTransform bar = Kit.Stretch(Kit.Node(progress, "Bar"), 12f, 200f, 14f, 14f);
            Kit.Fill(bar.gameObject, Kit.SoftColor, raycast: false);
            RectTransform fillNode = Kit.Stretch(Kit.Node(bar, "Fill"));
            Image fill = Kit.Fill(fillNode.gameObject, Kit.CashGreen, raycast: false);
            fill.sprite = Kit.SpriteOf(UiShape.Panel);
            fill.type = Image.Type.Filled; // the view drives fillAmount 0..1 from distance / target distance
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            Text distance = Kit.Label(progress, "Distance", "0 m", 44f, Kit.InkColor);
            Kit.Place(distance.rectTransform, new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(184f, 0f));

            UiViewBinder.BindRaceHud(root);
        }

        /// <summary>
        /// Results: left panel (event title, Personal Best, World Record, "Results" list of HighScoreRow) and the MD §2.1
        /// buttons on the right: Watch replay, Race again, High scores, Practice, Main menu.
        /// </summary>
        public static void Results(RectTransform root, SpeedStars.UI.Race.ResultRowView rowPrefab)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Race.ResultsScreenView>();

            RectTransform left = Kit.Node(root, "LeftPanel");
            Kit.Place(left, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(210f, 0f), new Vector2(760f, 0f));
            Kit.Panel(left.gameObject, Kit.PanelColor, frame: true, raycast: true);

            RectTransform eventTitle = Kit.TopBand(Kit.Node(left, "EventTitle"), 30f, 110f, 40f);
            Image eventIcon = Kit.Icon(eventTitle, "Icon", UiShape.Panel, Kit.FrameColor);
            Kit.Anchor(eventIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(80f, 80f));
            Text eventName = Kit.Label(eventTitle, "Label", "100m dash", 84f, Kit.InkColor);
            Kit.Margin(eventName, 130f, 0f, 16f, 0f);

            Kit.Fill(Kit.TopBand(Kit.Node(left, "Divider"), 160f, 6f, 50f).gameObject, Kit.FrameColor, raycast: false);
            RecordBlock(left, "PersonalBest", "Personal Best:", Kit.BlueText, 180f);
            RecordBlock(left, "WorldRecord", "World Record:", Kit.OrangeText, 300f);
            Kit.Fill(Kit.TopBand(Kit.Node(left, "Divider2"), 424f, 6f, 50f).gameObject, Kit.FrameColor, raycast: false);

            Text resultsCaption = Kit.Label(left, "ResultsCaption", "Results", 48f, Kit.InkColor, TextAnchor.MiddleLeft);
            Kit.TopBand(resultsCaption.rectTransform, 440f, 64f, 60f);

            RectTransform results = Kit.VerticalList(left, "Results", 4f);
            Kit.Stretch(results, 40f, 40f, 514f, 30f);

            RectTransform buttons = Kit.Node(root, "Buttons");
            Kit.Anchor(buttons, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-260f, 0f), new Vector2(660f, 760f));
            VerticalLayoutGroup column = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 24f;
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childControlWidth = true;
            column.childControlHeight = false;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            // All interactable here; in the vertical slice the view greys Watch replay and Practice out.
            ResultButton(buttons, "WatchReplay", "Watch replay", Kit.InkColor);
            ResultButton(buttons, "RaceAgain", "Race again", Kit.InkColor);
            ResultButton(buttons, "HighScores", "High scores", Kit.GoldText);
            ResultButton(buttons, "Practice", "Practice", Kit.InkColor);
            ResultButton(buttons, "MainMenu", "Main menu", Kit.PinkText);

            UiViewBinder.BindResults(root, rowPrefab);
        }

        private static void BuildColorPicker(RectTransform itemPanel)
        {
            RectTransform picker = Kit.Node(itemPanel, "ColorPicker");
            Kit.Anchor(picker, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, 60f), new Vector2(580f, 580f));

            // Placeholder square + hue bar: the view (or later art) supplies the real gradient textures.
            RectTransform area = Kit.Node(picker, "SvArea");
            Kit.Anchor(area, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(470f, 470f));
            Kit.Panel(area.gameObject, new Color(0.85f, 0.25f, 0.25f, 1f), frame: true, raycast: true);
            Image areaHandle = Kit.Icon(area, "Handle", UiShape.Round, Color.white);
            Kit.Anchor(areaHandle.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            Kit.Frame(areaHandle.gameObject, 3f);

            RectTransform hue = Kit.Node(picker, "HueBar");
            Kit.Anchor(hue, Vector2.one, Vector2.one, Vector2.zero, new Vector2(74f, 470f));
            Kit.Panel(hue.gameObject, new Color(0.55f, 0.35f, 0.9f, 1f), frame: true, raycast: true);
            Image hueHandle = Kit.Icon(hue, "Handle", UiShape.Round, Color.white);
            Kit.Anchor(hueHandle.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
            Kit.Frame(hueHandle.gameObject, 3f);

            RectTransform swatches = Kit.BottomBand(Kit.Node(picker, "Swatches"), 0f, 84f);
            Kit.Row(swatches.gameObject, 18f, TextAnchor.MiddleCenter);
            // Look-alike of the original's five quick colours; the real preset list comes from data in Faz 5.
            Swatch(swatches, "Swatch1", new Color(0.82f, 0.68f, 0.47f, 1f));
            Swatch(swatches, "Swatch2", new Color(0.9f, 0.1f, 0.1f, 1f));
            Swatch(swatches, "Swatch3", new Color(0.5f, 0.05f, 0.05f, 1f));
            Swatch(swatches, "Swatch4", Color.white);
            Swatch(swatches, "Swatch5", new Color(0.5f, 0.5f, 0.5f, 1f));
        }

        private static void Swatch(Transform parent, string name, Color color)
        {
            RectTransform swatch = Kit.Node(parent, name);
            swatch.sizeDelta = new Vector2(76f, 76f);
            Image image = swatch.gameObject.AddComponent<Image>();
            image.sprite = Kit.SpriteOf(UiShape.Round);
            image.color = color;
            Kit.Frame(swatch.gameObject, 3f);
            Kit.Clickable(swatch.gameObject, image);
        }

        private static void RecordBlock(RectTransform panel, string name, string caption, Color captionColor, float top)
        {
            RectTransform block = Kit.TopBand(Kit.Node(panel, name), top, 110f, 60f);
            Image icon = Kit.Icon(block, "Icon", UiShape.Round, Kit.GoldText);
            Kit.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
            Text captionLabel = Kit.Label(block, "Caption", caption, 46f, captionColor, TextAnchor.MiddleLeft);
            Kit.Place(captionLabel.rectTransform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(55f, 0f), new Vector2(-110f, 0f));
            Text value = Kit.Label(block, "Value", "-", 40f, Kit.InkColor, TextAnchor.MiddleLeft);
            Kit.Place(value.rectTransform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(55f, 0f), new Vector2(-110f, 0f));
        }

        private static void ResultButton(Transform column, string name, string caption, Color textColor)
        {
            RectTransform button = Kit.IconButton(column, name, caption, 68f, textColor, Kit.IconGrey, 90f);
            button.sizeDelta = new Vector2(660f, 128f);
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
        /// (Cash, rewarded ad; both start greyed out: not interactable), Close.
        /// </summary>
        public static void EntryGate(RectTransform root)
        {
            RectTransform window = PopupFrame<SpeedStars.UI.Home.EntryGateModalView>(root, new Vector2(960f, 820f));
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

            RectTransform offers = Kit.Node(window, "GetTickets");
            Kit.Anchor(offers, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(860f, 300f));
            Text offersCaption = Kit.Label(offers, "Caption", "Get tickets", 48f, Kit.InkColor);
            Kit.TopBand(offersCaption.rectTransform, 0f, 64f);

            RectTransform cash = Kit.IconButton(offers, "BuyWithCash", "Cash", 56f, Kit.InkColor, Kit.CashGreen, 80f, UiShape.Round);
            Kit.Anchor(cash, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(415f, 200f));
            Text cashPrice = Kit.Label(cash, "Price", "0", 44f, Kit.BlueText);
            Kit.BottomBand(cashPrice.rectTransform, 12f, 52f);
            cash.GetComponent<Button>().interactable = false;

            RectTransform ad = Kit.IconButton(offers, "WatchAd", "Watch ad", 52f, Kit.InkColor, Kit.TicketOrange, 80f, UiShape.Round);
            Kit.Anchor(ad, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(415f, 200f));
            ad.GetComponent<Button>().interactable = false;

            UiViewBinder.BindEntryGate(root);
        }

        /// <summary>Daily entry gift: "Welcome back", the ticket picture, CLAIM (claiming closes it; no Close button).</summary>
        public static void DailyGift(RectTransform root)
        {
            RectTransform window = PopupFrame<UiPopup>(root, new Vector2(900f, 700f));

            Text title = Kit.Label(window, "Title", "Welcome back! Claim it and go to start your challenge!", 60f, Kit.InkColor);
            Kit.TopBand(title.rectTransform, 40f, 190f, 50f);

            Image ticket = Kit.Icon(window, "TicketIcon", UiShape.Panel, Kit.TicketOrange);
            Kit.Anchor(ticket.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(220f, 220f));
            Kit.Frame(ticket.gameObject);

            RectTransform claim = Kit.TextButton(window, "Claim", "CLAIM", 72f, Kit.GreenText);
            Kit.Anchor(claim, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 124f));
        }

        /// <summary>7-day check-in: title, seven SevenDayCell instances (Day1..Day7, fixed count, not a list), Close.</summary>
        public static void SevenDayCheckIn(RectTransform root, GameObject sevenDayCell, UnityEngine.SceneManagement.Scene scene)
        {
            RectTransform window = PopupFrame<UiPopup>(root, new Vector2(1580f, 640f));
            Kit.CloseButton(window);

            Text title = Kit.Label(window, "Title", "7-Day Rewards", 80f, Kit.InkColor);
            Kit.TopBand(title.rectTransform, 30f, 100f, 140f);

            RectTransform days = Kit.Node(window, "Days");
            Kit.Anchor(days, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1500f, 280f));
            Kit.Row(days.gameObject, 16f, TextAnchor.MiddleCenter);

            for (int day = 1; day <= 7; day++)
            {
                RectTransform cell = Kit.Nest(sevenDayCell, days, "Day" + day, scene);
                if (cell == null) continue;
                Transform dayLabel = cell.Find("Day");
                Text text = dayLabel != null ? dayLabel.GetComponent<Text>() : null;
                if (text == null) continue;
                text.text = "Day " + day;
                Kit.Record(text);
            }
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
