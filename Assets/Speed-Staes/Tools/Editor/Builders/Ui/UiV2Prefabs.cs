using UnityEditor;
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

            // Bottom-left: High Scores only (Upgrade and Players were removed, T-067); it takes the far-left slot Upgrade had.
            UiArt.LayoutComponent highScores = layout.Get("high_scores");
            UiArt.LayoutComponent firstSlot = layout.Get("upgrade");
            var highScoresSpot = new UiArt.LayoutComponent
            {
                rect = new[] { firstSlot.rect[0], highScores.rect[1], highScores.rect[2], highScores.rect[3] }
            };
            ArtButton(root, layout, "HighScores", highScoresSpot, "high_scores_background", "high_scores_icon", true);

            RectTransform race = ArtButton(root, layout, "RaceSelect", "race_select", "race_select_background", "race_select_icon", true);
            UiArt.Slot(race, "Label", "100m", 56f, RaceLabelMin, RaceLabelMax, TextAnchor.MiddleLeft);
            UiArt.Layer(race, "Chevron", UiArt.Home("race_select_chevron"));

            RectTransform play = ArtButton(root, layout, "Play", "play", "play_background", "play_icon", true);
            UiArt.Slot(play, "Label", "Play", 72f, PlayLabelMin, PlayLabelMax, TextAnchor.MiddleCenter, Color.white);

            BuildCharacterCard(root, layout);
            UiViewBinder.BindHome(root);
            BindSoundIcons(root);
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
            CardButton(cardRoot, "Emote", "emote_background", "emote_icon", true);
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

        /// <summary>Sound button: the icon image and the muted sprite for HomeScreenView (the icon swaps sprite, same rect).</summary>
        private static void BindSoundIcons(RectTransform root)
        {
            var view = new SerializedObject(root.GetComponent<SpeedStars.UI.Home.HomeScreenView>());
            view.FindProperty("soundIcon").objectReferenceValue = root.Find("Sound/Icon").GetComponent<Image>();
            view.FindProperty("soundOffSprite").objectReferenceValue = UiArt.Home("sound_off_icon");
            view.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Screen-level button: background on the node, "Icon" child on the same rect, placed from layout.json.</summary>
        private static RectTransform ArtButton(Transform parent, UiArt.Layout layout, string name, string layoutName,
            string background, string icon, bool interactable)
            => ArtButton(parent, layout, name, layout.Get(layoutName), background, icon, interactable);

        private static RectTransform ArtButton(Transform parent, UiArt.Layout layout, string name, UiArt.LayoutComponent spot,
            string background, string icon, bool interactable)
        {
            RectTransform node = Kit.Node(parent, name);
            layout.PlaceOnScreen(node, spot);
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

        // Race HUD (T-061): the designer's cropped parts under UI/Art/RaceHud, drawn at their native size (art px = canvas units,
        // 1080 high). Offsets come from the reference screen (reference px x 1.5); only the containers' sizes are listed here.
        private static readonly Vector2 TelemetrySize = new Vector2(398f, 71f);
        private static readonly Vector2 CashSize = new Vector2(185f, 67f);
        private static readonly Vector2 EnergyGroupSize = new Vector2(428f, 39f);   // icon (left) + track
        private static readonly Vector2 CadenceGroupSize = new Vector2(542f, 39f);  // label + track + pointer
        private const float CadenceLabelWidth = 150f;
        private const float EnergyTrackX = 40f;   // track offset inside the Energy group (icon is at 0)
        private const float CadenceTrackX = 160f; // track offset inside the Cadence group (after the label)
        private const float BarTrackY = -6f;      // cadence track top inside its group (pointer is taller than the track)
        private const float EnergyTrackY = -5f;   // energy track top inside its group (icon is taller than the track)

        public static void RaceHud(RectTransform root)
        {
            root.gameObject.AddComponent<SpeedStars.UI.Race.RaceHudView>();
            var topLeft = new Vector2(0f, 1f);
            var topCenter = new Vector2(0.5f, 1f);
            var leftMiddle = new Vector2(0f, 0.5f);
            var rightMiddle = new Vector2(1f, 0.5f);

            // Telemetry (top-left): stopwatch + time | divider (in the art) | speedometer + speed.
            RectTransform telemetry = Kit.Node(root, "Telemetry");
            Kit.Anchor(telemetry, topLeft, topLeft, new Vector2(52f, -48f), TelemetrySize);
            UiArt.Paint(telemetry.gameObject, UiArt.RaceHud("telemetry_panel"), raycast: false);
            Native(telemetry, "Stopwatch", "stopwatch_icon", leftMiddle, leftMiddle, new Vector2(15f, 0f));
            TextBox(telemetry, "Time", "0.00", 40f, 66f, 126f, TextAnchor.MiddleCenter);
            Native(telemetry, "Speedometer", "speedometer_icon", leftMiddle, leftMiddle, new Vector2(TelemetrySize.x * 0.54f, 0f));
            TextBox(telemetry, "Speed", "0.0 m/s", 32f, 280f, 112f, TextAnchor.MiddleLeft);

            // Cash (under telemetry): diamond | value | plus.
            RectTransform cash = Kit.Node(root, "Cash");
            Kit.Anchor(cash, topLeft, topLeft, new Vector2(52f, -138f), CashSize);
            UiArt.Paint(cash.gameObject, UiArt.RaceHud("counter_panel"), raycast: false);
            Native(cash, "Icon", "cash_diamond_icon", leftMiddle, leftMiddle, new Vector2(15f, 0f));
            TextBox(cash, "Value", "0", 38f, 66f, 76f, TextAnchor.MiddleLeft);
            Image plus = Native(cash, "Plus", "plus_icon", rightMiddle, rightMiddle, new Vector2(-12f, 0f));
            plus.raycastTarget = true; // the visible plus is the hit area
            UiArt.MakeButton(plus.gameObject, plus, false, alphaHit: false); // no shop yet

            // Energy (top-center): icon | track with a Filled-type fill centered inside it.
            RectTransform energy = Kit.Node(root, "Energy");
            Kit.Anchor(energy, topCenter, topCenter, new Vector2(-21f, -50f), EnergyGroupSize);
            Native(energy, "Icon", "energy_icon", topLeft, topLeft, Vector2.zero);
            Image energyTrack = Native(energy, "Track", "energy_track", topLeft, topLeft, new Vector2(EnergyTrackX, EnergyTrackY));
            Image fill = Native(energy, "Fill", "energy_fill", topLeft, topLeft, CenteredIn(energyTrack, "energy_fill", EnergyTrackX, EnergyTrackY));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            // Cadence (under energy): label | track with the green-yellow-red scale | pointer. The view slides the pointer along
            // the Scale rect (the drawn range), so Scale is what RaceHudView.cadenceTrack binds to.
            RectTransform cadence = Kit.Node(root, "Cadence");
            Kit.Anchor(cadence, topCenter, topCenter, new Vector2(-79f, -115f), CadenceGroupSize);
            Text cadenceLabel = Kit.Label(cadence, "Label", "Cadence", 24f, UiArt.Navy, TextAnchor.MiddleRight);
            Kit.Place(cadenceLabel.rectTransform, topLeft, topLeft, topLeft, Vector2.zero, new Vector2(CadenceLabelWidth, CadenceGroupSize.y));
            Image cadenceTrack = Native(cadence, "Track", "cadence_track", topLeft, topLeft, new Vector2(CadenceTrackX, BarTrackY));
            Native(cadence, "Scale", "cadence_scale", topLeft, topLeft, CenteredIn(cadenceTrack, "cadence_scale", CadenceTrackX, BarTrackY));
            Vector2 trackSize = cadenceTrack.rectTransform.sizeDelta;
            // Starts centered on the track; the view moves it with the value.
            Native(cadence, "Marker", "cadence_pointer", topLeft, new Vector2(0.5f, 0.5f),
                new Vector2(CadenceTrackX + trackSize.x * 0.5f, BarTrackY - trackSize.y * 0.5f));

            // Exit and Restart (top-right): square background, native icon centered.
            SquareButton(root, "Restart", "restart_icon", -50f);
            SquareButton(root, "Exit", "exit_icon", -186f);

            UiViewBinder.BindRaceHud(root);
        }

        /// <summary>An Image at its sprite's native size (SetNativeSize equivalent: art px = canvas units).</summary>
        private static Image Native(Transform parent, string name, string sprite, Vector2 anchor, Vector2 pivot, Vector2 position)
        {
            RectTransform rt = Kit.Node(parent, name);
            Sprite art = UiArt.RaceHud(sprite);
            Kit.Anchor(rt, anchor, pivot, position, art.rect.size);
            return UiArt.Paint(rt.gameObject, art, raycast: false);
        }

        /// <summary>Top-left offset that centers a sprite inside a bar track placed at (trackX, trackY).</summary>
        private static Vector2 CenteredIn(Image track, string sprite, float trackX, float trackY)
        {
            Vector2 inner = UiArt.RaceHud(sprite).rect.size;
            Vector2 outer = track.rectTransform.sizeDelta;
            return new Vector2(trackX + (outer.x - inner.x) * 0.5f, trackY - (outer.y - inner.y) * 0.5f);
        }

        /// <summary>Navy Text at the left edge of its panel, full panel height: x and width in canvas units.</summary>
        private static void TextBox(Transform parent, string name, string text, float size, float x, float width, TextAnchor alignment)
        {
            Text label = Kit.Label(parent, name, text, size, UiArt.Navy, alignment);
            Kit.Place(label.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(width, 0f));
        }

        private static void SquareButton(Transform parent, string name, string icon, float rightOffset)
        {
            var topRight = new Vector2(1f, 1f);
            RectTransform node = Kit.Node(parent, name);
            Sprite background = UiArt.RaceHud("square_button_background");
            Kit.Anchor(node, topRight, topRight, new Vector2(rightOffset, -37f), background.rect.size);
            Image image = UiArt.Paint(node.gameObject, background, raycast: true);
            var center = new Vector2(0.5f, 0.5f);
            Native(node, "Icon", icon, center, center, Vector2.zero).preserveAspect = true;
            UiArt.MakeButton(node.gameObject, image, true, alphaHit: false);
        }
    }
}
