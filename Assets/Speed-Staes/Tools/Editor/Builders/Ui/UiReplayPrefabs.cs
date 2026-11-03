using System;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The Watch Replay controls as a prefab (T-098; Malik's ReplayControlsView used to build all of this in code). Same nodes, rects,
    /// colours and canvas as that code made: an own overlay canvas (sorting 500, 1920x1080, match 0.5) with, as direct children,
    /// top left the race Clock (time + speed) and the runner Name plate, top right Exit / NextRunner / Camera / PrevRunner, along the
    /// bottom Play, the Timeline slider and Rate. Art comes from the ReplayControlsSkin asset (Malik's choice of sprites) at build time
    /// and is baked into the prefab; the view only binds and fills. Never runs in the game.
    /// </summary>
    internal static class UiReplayPrefabs
    {
        public const string SkinPath = "Assets/Speed-Staes/UI/Widgets/ReplayControlsSkin.asset";

        // Canvas, as the code set it up: above the HUD, the game's wide reference, width and height weighted evenly.
        private const int CanvasSortingOrder = 500;
        private static readonly Vector2 CanvasReference = new Vector2(1920f, 1080f);
        private const float CanvasMatch = 0.5f;

        // Look without a skin: plain cream panels (hidden icons) in the HUD's navy bold italic.
        private static readonly Color PlainPanel = new Color(1f, 0.98f, 0.93f, 0.95f);
        private static readonly Color DefaultTextColor = new Color32(10, 31, 92, 255);
        private static readonly Color PlainFill = new Color(0.98f, 0.78f, 0.1f, 1f);

        // Layout (canvas units at the reference resolution).
        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 BottomCentre = new Vector2(0.5f, 0f);
        private static readonly Vector2 SquareSize = new Vector2(110f, 105f);
        private static readonly Vector2 RateSize = new Vector2(190f, 105f);
        private const float TimelineHandleWidth = 26f; // the handle spans the bar's height plus 60% over and under
        private const float TimelineHandleOverhang = 0.6f;
        private const float TimelineFillInset = 4f;
        private const float CutFallbackTextSize = 40f;

        public static void BuildAll(Func<string, Action<RectTransform>, GameObject> build)
        {
            var skin = AssetDatabase.LoadAssetAtPath<ReplayControlsSkin>(SkinPath);
            if (skin == null)
                UnityEngine.Debug.LogWarning($"UiReplayPrefabs: {SkinPath} not found; the replay controls are built as plain panels.");
            build(UiPrefabPaths.ReplayControls, root => Build(root, skin));
        }

        private static void Build(RectTransform root, ReplayControlsSkin skin)
        {
            var look = new Look(skin);

            // Own overlay canvas on the prefab root (the view opens it over the screens, outside any other canvas).
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = CanvasReference;
            scaler.matchWidthOrHeight = CanvasMatch;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var view = root.gameObject.AddComponent<ReplayControlsView>();

            // Top left: race clock and speed, as on the race HUD; the followed runner's name below.
            RectTransform clock = look.MakeImage(root, "Clock", look.Pick(s => s.telemetryPanel), TopLeft, new Vector2(40f, -40f), new Vector2(600f, 106f));
            look.MakeImage(clock, "Stopwatch", look.Pick(s => s.stopwatchIcon), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(54f, 64f));
            Text time = look.MakeText(clock, "Time", 50, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(88f, 0f), new Vector2(200f, 80f));
            look.MakeImage(clock, "Speedometer", look.Pick(s => s.speedometerIcon), new Vector2(0f, 0.5f), new Vector2(300f, 0f), new Vector2(72f, 48f));
            Text speed = look.MakeText(clock, "Speed", 50, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(384f, 0f), new Vector2(210f, 80f));
            RectTransform plate = look.MakeImage(root, "Name", look.Pick(s => s.namePanel), TopLeft, new Vector2(40f, -162f), new Vector2(440f, 90f));
            Text nameText = look.MakeText(plate, "Label", 40, TextAnchor.MiddleCenter, Centre, Vector2.zero, new Vector2(410f, 80f));

            // Top right: < camera > and Exit, the race HUD's square buttons.
            Button exit = look.MakeButton(root, "Exit", look.Pick(s => s.squareButton), TopRight, new Vector2(-40f, -40f), SquareSize);
            look.MakeImage(exit.transform, "Icon", look.Pick(s => s.exitIcon), Centre, Vector2.zero, new Vector2(62f, 67f));
            Button next = look.MakeButton(root, "NextRunner", look.Pick(s => s.squareButton), TopRight, new Vector2(-180f, -40f), SquareSize);
            look.MakeCut(next.transform, "Icon", skin ? skin.arrowSheet : null, skin ? skin.arrowRect : default, new Vector2(30f, 52f), false, ">", true);
            Button camera = look.MakeButton(root, "Camera", look.Pick(s => s.squareButton), TopRight, new Vector2(-300f, -40f), SquareSize);
            look.MakeCut(camera.transform, "Icon", skin ? skin.cameraIconSheet : null, skin ? skin.cameraIconRect : default, new Vector2(78f, 62f), false, "CAM");
            Button prev = look.MakeButton(root, "PrevRunner", look.Pick(s => s.squareButton), TopRight, new Vector2(-420f, -40f), SquareSize);
            look.MakeCut(prev.transform, "Icon", skin ? skin.arrowSheet : null, skin ? skin.arrowRect : default, new Vector2(30f, 52f), true, "<", true);

            // Bottom: play/pause, the timeline, the speed.
            Button play = look.MakeButton(root, "Play", look.Pick(s => s.squareButton), BottomCentre, new Vector2(-795f, 40f), SquareSize);
            Text playText = look.MakeText(play.transform, "Label", 52, TextAnchor.MiddleCenter, Centre, Vector2.zero, SquareSize);
            Slider timeline = look.MakeSlider(root, new Vector2(-40f, 92f), new Vector2(1340f, 30f));
            Button rate = look.MakeButton(root, "Rate", look.Pick(s => s.namePanel), BottomCentre, new Vector2(755f, 40f), RateSize);
            Text rateText = look.MakeText(rate.transform, "Label", 48, TextAnchor.MiddleCenter, Centre, Vector2.zero, new Vector2(180f, 100f));

            // Every control is a direct child: the screens' own fitter moves them in from under a notch / camera hole.
            root.gameObject.AddComponent<SafeAreaFitter>();

            var so = new SerializedObject(view);
            Ref(so, "timeText", time);
            Ref(so, "speedText", speed);
            Ref(so, "nameText", nameText);
            Ref(so, "exitButton", exit);
            Ref(so, "nextRunnerButton", next);
            Ref(so, "cameraButton", camera);
            Ref(so, "prevRunnerButton", prev);
            Ref(so, "playButton", play);
            Ref(so, "playText", playText);
            Ref(so, "timeline", timeline);
            Ref(so, "rateButton", rate);
            Ref(so, "rateText", rateText);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"UiReplayPrefabs: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            if (value == null)
                throw new InvalidOperationException($"UiReplayPrefabs: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            property.objectReferenceValue = value;
        }

        /// <summary>The skin's sprites, font and text style (or the plain defaults) and the pieces made from them.</summary>
        private sealed class Look
        {
            private readonly ReplayControlsSkin _skin;
            private readonly Font _font;
            private readonly Color _textColor;
            private readonly FontStyle _fontStyle;

            public Look(ReplayControlsSkin skin)
            {
                _skin = skin;
                _font = skin && skin.font ? skin.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _textColor = skin ? skin.textColor : DefaultTextColor;
                _fontStyle = skin ? skin.fontStyle : FontStyle.BoldAndItalic;
            }

            public Sprite Pick(Func<ReplayControlsSkin, Sprite> pick) => _skin ? pick(_skin) : null;

            /// <summary>An image anchored (and pivoted) at one point. A sprite with borders is sliced, so panels keep their corners at any size.</summary>
            public RectTransform MakeImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
            {
                RectTransform rt = Place(Kit.Node(parent, name), anchor, position, size);
                var image = rt.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                if (sprite)
                {
                    image.sprite = sprite;
                    image.type = sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
                }
                else image.color = name == "Icon" || name.EndsWith("ometer") || name == "Stopwatch" ? Color.clear : PlainPanel;
                return rt;
            }

            /// <summary>Square / plate button: the image is the target graphic and takes taps (default colour tint, no extra feedback).</summary>
            public Button MakeButton(Transform parent, string name, Sprite background, Vector2 anchor, Vector2 position, Vector2 size)
            {
                RectTransform rt = MakeImage(parent, name, background, anchor, position, size);
                var image = rt.GetComponent<Image>();
                image.raycastTarget = true;
                var button = rt.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                return button;
            }

            /// <summary>
            /// An icon cut out of a wider sprite (rect from its top-left corner); a text stand-in without the sprite. A tinted icon takes the
            /// text colour (the Continue arrow is white and would vanish on the cream button).
            /// </summary>
            public void MakeCut(Transform parent, string name, Sprite sheet, RectInt cut, Vector2 size, bool mirror, string fallback, bool tint = false)
            {
                if (!sheet)
                {
                    MakeText(parent, name, Mathf.RoundToInt(CutFallbackTextSize), TextAnchor.MiddleCenter, Centre, Vector2.zero, size * 1.6f).text = fallback;
                    return;
                }
                RectTransform rt = Place(Kit.Node(parent, name), Centre, Vector2.zero, size);
                if (mirror) rt.localScale = new Vector3(-1f, 1f, 1f);
                var image = rt.gameObject.AddComponent<RawImage>();
                image.raycastTarget = false;
                if (tint) image.color = _textColor;
                Texture tex = sheet.texture;
                Rect tr = sheet.textureRect;
                float fromBottom = sheet.rect.height - cut.y - cut.height;
                image.texture = tex;
                image.uvRect = new Rect((tr.x + cut.x) / tex.width, (tr.y + fromBottom) / tex.height, (float)cut.width / tex.width, (float)cut.height / tex.height);
            }

            public Text MakeText(Transform parent, string name, int size, TextAnchor align, Vector2 anchor, Vector2 position, Vector2 box)
            {
                RectTransform rt = Place(Kit.Node(parent, name), anchor, position, box);
                var text = rt.gameObject.AddComponent<Text>();
                text.font = _font;
                text.fontSize = size;
                text.fontStyle = _fontStyle;
                text.alignment = align;
                text.color = _textColor;
                text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                return text;
            }

            /// <summary>The race HUD's energy bar as a seek bar: track, yellow fill, the cadence pointer as handle.</summary>
            public Slider MakeSlider(Transform parent, Vector2 position, Vector2 size)
            {
                RectTransform rt = Place(Kit.Node(parent, "Timeline"), BottomCentre, position, size);
                var slider = rt.gameObject.AddComponent<Slider>();

                RectTransform track = MakeImage(rt, "Track", Pick(s => s.timelineTrack), Centre, Vector2.zero, size);
                if (!_skin) track.GetComponent<Image>().color = new Color(_textColor.r, _textColor.g, _textColor.b, 0.8f);

                RectTransform fa = Kit.Node(rt, "Fill Area");
                fa.anchorMin = Vector2.zero;
                fa.anchorMax = Vector2.one;
                fa.offsetMin = new Vector2(TimelineFillInset, TimelineFillInset);
                fa.offsetMax = new Vector2(-TimelineFillInset, -TimelineFillInset);
                RectTransform fill = MakeImage(fa, "Fill", Pick(s => s.timelineFill), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(0f, 1f);
                fill.sizeDelta = Vector2.zero;
                if (!_skin) fill.GetComponent<Image>().color = PlainFill;

                RectTransform ha = Kit.Node(rt, "Handle Slide Area");
                ha.anchorMin = Vector2.zero;
                ha.anchorMax = Vector2.one;
                ha.offsetMin = ha.offsetMax = Vector2.zero;
                RectTransform handle = MakeImage(ha, "Handle", Pick(s => s.timelineHandle), Centre, Vector2.zero, new Vector2(TimelineHandleWidth, 0f));
                handle.anchorMin = new Vector2(0f, -TimelineHandleOverhang);
                handle.anchorMax = new Vector2(0f, 1f + TimelineHandleOverhang);
                handle.sizeDelta = new Vector2(TimelineHandleWidth, 0f);
                Image handleImage = handle.GetComponent<Image>();
                handleImage.raycastTarget = true;
                if (!_skin) handleImage.color = Color.white;

                slider.fillRect = fill;
                slider.handleRect = handle;
                slider.targetGraphic = handleImage;
                slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
                slider.minValue = 0f;
                slider.maxValue = 1f;
                return slider;
            }

            private static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
                rt.anchoredPosition = position;
                rt.sizeDelta = size;
                return rt;
            }
        }
    }
}
