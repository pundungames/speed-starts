using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// UI v2 prefabs dressed with the designer's art: HomeScreen, ResourceBar, RaceHud. Hierarchy names and view fields follow
    /// docs/tasks/UI-v2-ISIMLER.md. Layers of one component sit on the same rect (the art canvas) at 100% scale; component
    /// rects come from Art/Home/layout.json. Text slots inside a layer (name, height, labels) are not in layout.json: they are
    /// fractions of the layer canvas, read off the design previews. Systems that do not exist yet leave their button
    /// non-interactable (settings, sound, ...), the sprite stays so the layout is complete.
    /// </summary>
    internal static class UiV2Prefabs
    {
        // Text slots as fractions of their art canvas (min, max; y from the bottom).
        private static readonly Vector2 PillValueMin = new Vector2(0.42f, 0.05f), PillValueMax = new Vector2(0.93f, 0.95f);
        private static readonly Vector2 TimerValueMin = new Vector2(0.08f, 0.05f), TimerValueMax = new Vector2(0.92f, 0.95f);
        private static readonly Vector2 RaceLabelMin = new Vector2(0.36f, 0.08f), RaceLabelMax = new Vector2(0.80f, 0.92f);
        private static readonly Vector2 PlayLabelMin = new Vector2(0.30f, 0.08f), PlayLabelMax = new Vector2(0.95f, 0.92f);
        private static readonly Vector2 RacerNameMin = new Vector2(0.45f, 0.78f), RacerNameMax = new Vector2(0.77f, 0.90f);
        private static readonly Vector2 HeightMin = new Vector2(0.09f, 0.34f), HeightMax = new Vector2(0.91f, 0.44f);

        private const float PowerDefaultFill = 0.75f;
        private const float StaminaDefaultFill = 0.7f;

        // ---------------------------------------------------------------- Home

        public static void Home(RectTransform root)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Home.HomeScreenView>();
            UiArt.Layout layout = UiArt.ReadLayout();

            // Top-left: daily reward with its notification dot.
            RectTransform daily = ArtButton(root, layout, "DailyReward", "daily_reward", "daily_reward_background", "daily_reward_icon", true);
            UiArt.Layer(daily, "Notification", UiArt.Home("daily_reward_notification"));

            // Top-right: settings, sound, environment.
            ArtButton(root, layout, "Settings", "settings", "settings_background", "settings_icon", true);
            ArtButton(root, layout, "Sound", "sound", "sound_background", "sound_icon", true);
            ArtButton(root, layout, "Environment", "environment", "environment_background", "environment_icon", true);

            // Bottom row.
            ArtButton(root, layout, "Upgrade", "upgrade", "upgrade_background", "upgrade_icon", false);
            ArtButton(root, layout, "Players", "players", "players_background", "players_icon", false);
            ArtButton(root, layout, "HighScores", "high_scores", "high_scores_background", "high_scores_icon", true);

            RectTransform race = ArtButton(root, layout, "RaceSelect", "race_select", "race_select_background", "race_select_icon", true);
            UiArt.Slot(race, "Label", "100m", 56f, RaceLabelMin, RaceLabelMax, TextAnchor.MiddleLeft);
            UiArt.Layer(race, "Chevron", UiArt.Home("race_select_chevron"));

            RectTransform play = ArtButton(root, layout, "Play", "play", "play_background", "play_icon", true);
            UiArt.Slot(play, "Label", "Play", 72f, PlayLabelMin, PlayLabelMax, TextAnchor.MiddleCenter, Color.white);

            BuildCharacterCard(root, layout);
            UiViewBinder.BindHome(root);
        }

        private static void BuildCharacterCard(RectTransform root, UiArt.Layout layout)
        {
            UiArt.LayoutComponent card = layout.Get("character_card");
            RectTransform cardRoot = Kit.Node(root, "CharacterCard");
            layout.PlaceOnScreen(cardRoot, card);

            // Background catches taps over the card so they never reach the 3D scene behind it.
            RectTransform background = Kit.Stretch(Kit.Node(cardRoot, "Background"));
            UiArt.Paint(background.gameObject, UiArt.Home("character_card_background"), raycast: true);

            RectTransform select = Kit.Stretch(Kit.Node(cardRoot, "Select"));
            UiArt.Paint(select.gameObject, UiArt.Home("character_select_background"), raycast: false);
            UiArt.Layer(select, "Portrait", UiArt.Home("character_select_icon"));
            UiArt.Slot(select, "Name", "Custom 1", 56f, RacerNameMin, RacerNameMax, TextAnchor.MiddleLeft);

            CardButton(cardRoot, "Previous", "character_previous_background", "character_previous_icon", true);
            CardButton(cardRoot, "Next", "character_next_background", "character_next_icon", true);

            StatBar(cardRoot, "PowerBar", "stats_power_empty", "stats_power_fill", PowerDefaultFill);
            StatBar(cardRoot, "StaminaBar", "stats_stamina_empty", "stats_stamina_fill", StaminaDefaultFill);

            RectTransform height = Kit.Stretch(Kit.Node(cardRoot, "HeightField"));
            UiArt.Paint(height.gameObject, UiArt.Home("stats_height_field"), raycast: false);
            UiArt.Slot(height, "Label", "Height: 5'9", 44f, HeightMin, HeightMax);

            CardButton(cardRoot, "EditRacer", "edit_racer_background", "edit_racer_icon", true);
            CardButton(cardRoot, "Emote", "emote_background", "emote_icon", false);
        }

        private static void StatBar(Transform card, string name, string empty, string fill, float amount)
        {
            RectTransform bar = Kit.Stretch(Kit.Node(card, name));
            UiArt.Layer(bar, "Empty", UiArt.Home(empty));
            Image fillImage = UiArt.Layer(bar, "Fill", UiArt.Home(fill));
            fillImage.type = Image.Type.Filled; // the art is delivered full; the view drives fillAmount
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = amount;
        }

        /// <summary>
        /// A button of the shared 491x486 card canvas: the node covers only the alpha bounds of its art (background + icon), so
        /// buttons never overlap the whole card; art layers are drawn on the full canvas from there and never catch taps.
        /// An invisible hit Image on the node catches them; the background Image is the tint target (disabled look).
        /// </summary>
        private static void CardButton(Transform card, string name, string background, string icon, bool interactable)
        {
            Vector4 b = UiArt.AlphaBounds(UiArt.HomeArt + "/" + background + ".png");
            Vector4 i = UiArt.AlphaBounds(UiArt.HomeArt + "/" + icon + ".png");
            var bounds = new Vector4(Mathf.Min(b.x, i.x), Mathf.Min(b.y, i.y), Mathf.Max(b.z, i.z), Mathf.Max(b.w, i.w));

            RectTransform node = Kit.Span(Kit.Node(card, name), new Vector2(bounds.x, bounds.y), new Vector2(bounds.z, bounds.w));
            Image hit = node.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            Image image = UiArt.CanvasLayer(node, "Background", UiArt.Home(background), bounds);
            UiArt.CanvasLayer(node, "Icon", UiArt.Home(icon), bounds);
            UiArt.MakeButton(node.gameObject, image, interactable, alphaHit: false);
        }

        /// <summary>Screen-level button: background on the node, "Icon" child on the same rect, placed from layout.json.</summary>
        private static RectTransform ArtButton(Transform parent, UiArt.Layout layout, string name, string layoutName,
            string background, string icon, bool interactable)
        {
            RectTransform node = Kit.Node(parent, name);
            layout.PlaceOnScreen(node, layout.Get(layoutName));
            Image image = UiArt.Paint(node.gameObject, UiArt.Home(background), raycast: true);
            UiArt.Layer(node, "Icon", UiArt.Home(icon));
            UiArt.MakeButton(node.gameObject, image, interactable, alphaHit: false);
            return node;
        }

        // ---------------------------------------------------------------- ResourceBar

        /// <summary>
        /// Left vertical column (layout.json rects). The root is only the column's bounding box, anchored top-left; items sit
        /// inside it at their reference offsets. No Image on the root: it never catches taps.
        /// </summary>
        public static void ResourceBar(RectTransform root)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Home.ResourceBarView>();
            UiArt.Layout layout = UiArt.ReadLayout();

            Vector4 bounds = layout.Bounds("purple_gem", "entry_ticket", "ticket_timer", "timed_ticket", "diamond");
            float s = layout.Scale;
            var corner = new Vector2(0f, 1f);
            Kit.Anchor(root, corner, corner, new Vector2(bounds.x * s, -bounds.y * s), new Vector2((bounds.z - bounds.x) * s, (bounds.w - bounds.y) * s));

            ResourceItem(root, layout, bounds, "PurpleGem", "purple_gem", "purple_gem_background", "purple_gem_icon", "0");
            ResourceItem(root, layout, bounds, "EntryTicket", "entry_ticket", "entry_ticket_background", "entry_ticket_icon", "0/0");
            ResourceItem(root, layout, bounds, "TicketTimer", "ticket_timer", "ticket_timer_background", null, "00:00");
            ResourceItem(root, layout, bounds, "TimedTicket", "timed_ticket", "timed_ticket_background", "timed_ticket_icon", "0");
            ResourceItem(root, layout, bounds, "Cash", "diamond", "diamond_background", "diamond_icon", "0");

            UiViewBinder.BindResourceBar(root);
        }

        private static void ResourceItem(Transform bar, UiArt.Layout layout, Vector4 bounds, string name, string layoutName,
            string background, string icon, string placeholder)
        {
            RectTransform node = Kit.Node(bar, name);
            layout.PlaceInside(node, layout.Get(layoutName), bounds.x, bounds.y);
            UiArt.Paint(node.gameObject, UiArt.Home(background), raycast: false);
            if (icon != null) UiArt.Layer(node, "Icon", UiArt.Home(icon));

            bool timer = icon == null;
            UiArt.Slot(node, "Value", placeholder, timer ? 30f : 48f, timer ? TimerValueMin : PillValueMin,
                timer ? TimerValueMax : PillValueMax, timer ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
        }

        // ---------------------------------------------------------------- RaceHud

        // Race HUD sizes in canvas units (design preview 1280x715 -> 1080 high: x1.51). Art canvases have transparent margins,
        // so rects are a little larger than the visible shapes.
        private static readonly Vector2 TelemetrySize = new Vector2(450f, 150f);
        private static readonly Vector2 CashSize = new Vector2(230f, 82f);
        private static readonly Vector2 BarGroupSize = new Vector2(600f, 50f);
        private const float BarX = 190f; // where the bar tracks start inside Energy/Cadence
        private const float BarWidth = 400f;
        private const float BarHeight = 28f;
        private static readonly Vector2 SquareButtonSize = new Vector2(110f, 108f);

        public static void RaceHud(RectTransform root)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Race.RaceHudView>();
            var topLeft = new Vector2(0f, 1f);
            var topCenter = new Vector2(0.5f, 1f);

            // Telemetry (top-left): stopwatch + time | speedometer | speed, split by the panel's two dividers.
            RectTransform telemetry = Kit.Node(root, "Telemetry");
            Kit.Anchor(telemetry, topLeft, topLeft, new Vector2(24f, -6f), TelemetrySize);
            UiArt.Paint(telemetry.gameObject, UiArt.Race("telemetry_panel"), raycast: false);
            Icon(telemetry, "Stopwatch", "stopwatch_icon", new Vector2(0.06f, 0.30f), new Vector2(0.15f, 0.70f));
            UiArt.Slot(telemetry, "Time", "0.00", 44f, new Vector2(0.15f, 0.30f), new Vector2(0.35f, 0.70f));
            Icon(telemetry, "Speedometer", "speedometer_icon", new Vector2(0.40f, 0.30f), new Vector2(0.60f, 0.70f));
            UiArt.Slot(telemetry, "Speed", "0.0 m/s", 36f, new Vector2(0.67f, 0.30f), new Vector2(0.95f, 0.70f));

            // Cash (under telemetry).
            RectTransform cash = Kit.Node(root, "Cash");
            Kit.Anchor(cash, topLeft, topLeft, new Vector2(24f, -150f), CashSize);
            UiArt.Paint(cash.gameObject, UiArt.Race("counter_panel"), raycast: false);
            Icon(cash, "Icon", "cash_diamond_icon", new Vector2(0.12f, 0.28f), new Vector2(0.34f, 0.76f));
            UiArt.Slot(cash, "Value", "0", 44f, new Vector2(0.36f, 0.25f), new Vector2(0.72f, 0.80f), TextAnchor.MiddleLeft);
            RectTransform plus = Kit.Span(Kit.Node(cash, "Plus"), new Vector2(0.72f, 0.2f), new Vector2(0.92f, 0.82f));
            Text plusLabel = plus.gameObject.AddComponent<Text>();
            StyleLabel(plusLabel, "+", 44f);
            plusLabel.raycastTarget = true;
            UiArt.MakeButton(plus.gameObject, plusLabel, false, alphaHit: false); // no shop yet

            // Energy (top-center): icon | track with fill.
            RectTransform energy = Kit.Node(root, "Energy");
            Kit.Anchor(energy, topCenter, topCenter, new Vector2(0f, -18f), BarGroupSize);
            RectTransform energyIcon = Kit.Anchor(Kit.Node(energy, "Icon"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(BarX - 70f, 0f), new Vector2(52f, 48f));
            UiArt.Paint(energyIcon.gameObject, UiArt.Race("energy_icon"), raycast: false);
            RectTransform energyTrack = BarRect(Kit.Node(energy, "Track"), 0f);
            Image trackImage = Kit.Panel(energyTrack.gameObject, UiArt.Navy, frame: false, raycast: false);
            trackImage.color = UiArt.Navy;
            RectTransform energyFill = BarRect(Kit.Node(energy, "Fill"), 3f);
            Image fill = Kit.Panel(energyFill.gameObject, new Color(1f, 0.78f, 0.1f, 1f), frame: false, raycast: false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            // Cadence (under energy): label | gradient track | marker above the track. Track and Marker are siblings anchored at
            // the group's left edge (pivot left / bottom-center), so the view moves the marker along track.anchoredPosition.x + t * width.
            RectTransform cadence = Kit.Node(root, "Cadence");
            Kit.Anchor(cadence, topCenter, topCenter, new Vector2(0f, -70f), BarGroupSize);
            Text cadenceLabel = Kit.Label(cadence, "Label", "Cadence", 30f, UiArt.Navy, TextAnchor.MiddleRight);
            Kit.Place(cadenceLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(BarX - 10f, 40f));
            RectTransform cadenceTrack = BarRect(Kit.Node(cadence, "Track"), 0f);
            Image gradient = UiArt.Paint(cadenceTrack.gameObject, Load(UiArt.CadenceGradientPath), raycast: false);
            Kit.Frame(cadenceTrack.gameObject, 3f).effectColor = UiArt.Navy;
            RectTransform marker = Kit.Node(cadence, "Marker");
            Kit.Anchor(marker, new Vector2(0f, 0.5f), new Vector2(0.5f, 0f), new Vector2(BarX + BarWidth * 0.5f, BarHeight * 0.5f - 2f), new Vector2(34f, 30f));
            UiArt.Paint(marker.gameObject, UiArt.Race("cadence_marker"), raycast: false);
            gradient.type = Image.Type.Simple;

            // Exit and Restart (top-right).
            SquareButton(root, "Restart", "restart_icon", new Vector2(-24f, -14f), new Vector2(66f, 64f));
            SquareButton(root, "Exit", "exit_icon", new Vector2(-24f - SquareButtonSize.x - 14f, -14f), new Vector2(70f, 61f));

            UiViewBinder.BindRaceHud(root);
        }

        private static Sprite Load(string path)
        {
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new System.InvalidOperationException($"UiV2Prefabs: sprite '{path}' not found.");
            return sprite;
        }

        /// <summary>Track rect in a bar group: left-anchored at BarX, fixed width, vertically centered; inset shrinks it all around.</summary>
        private static RectTransform BarRect(RectTransform rt, float inset)
        {
            return Kit.Anchor(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(BarX + inset, 0f),
                new Vector2(BarWidth - 2f * inset, BarHeight - 2f * inset));
        }

        private static void Icon(Transform parent, string name, string sprite, Vector2 min, Vector2 max)
        {
            RectTransform rt = Kit.Span(Kit.Node(parent, name), min, max);
            Image image = UiArt.Paint(rt.gameObject, UiArt.Race(sprite), raycast: false);
            image.preserveAspect = true; // race icons are not on a shared canvas: keep their shape inside the slot
        }

        private static void SquareButton(Transform parent, string name, string icon, Vector2 position, Vector2 iconSize)
        {
            RectTransform node = Kit.Node(parent, name);
            Kit.Anchor(node, Vector2.one, Vector2.one, position, SquareButtonSize);
            Image background = UiArt.Paint(node.gameObject, UiArt.Race("square_button_background"), raycast: true);
            RectTransform iconRect = Kit.Anchor(Kit.Node(node, "Icon"), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, iconSize);
            UiArt.Paint(iconRect.gameObject, UiArt.Race(icon), raycast: false).preserveAspect = true;
            UiArt.MakeButton(node.gameObject, background, true, alphaHit: false);
        }

        private static void StyleLabel(Text label, string text, float size)
        {
            // A Text added in place (the "+" is its own button graphic): same look as Kit.Label.
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = Mathf.RoundToInt(size);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UiArt.Navy;
        }
    }
}
