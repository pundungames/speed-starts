using SpeedStars.Core.Race;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// The replay's controls, in the race HUD's look (<see cref="ReplayControlsSkin"/>): top left the followed runner's race clock and speed
    /// on the HUD's telemetry panel with his name below; top right runner &lt; camera &gt; and Exit as the HUD's square buttons; along the
    /// bottom play/pause, the timeline (the HUD's energy bar, drag to seek) and the speed. Built in code on its own overlay canvas, so
    /// no prefab or scene holds it; without a skin it falls back to plain panels. Closes itself when the replay ends.
    /// </summary>
    public sealed class ReplayControlsView : MonoBehaviour
    {
        static readonly Color PlainPanel = new Color(1f, 0.98f, 0.93f, 0.95f);

        IRaceReplay _replay;
        ReplayControlsSkin _skin;
        Font _font;
        Color _textColor = new Color32(10, 31, 92, 255);
        FontStyle _fontStyle = FontStyle.BoldAndItalic;
        Slider _timeline;
        Text _timeText, _speedText, _nameText, _playText, _rateText;

        /// <summary>Opens the controls over a replay that is already playing.</summary>
        public static ReplayControlsView Open(IRaceReplay replay, ReplayControlsSkin skin, Font fallbackFont)
        {
            var go = new GameObject("Replay Controls", typeof(RectTransform));
            var view = go.AddComponent<ReplayControlsView>();
            view.Build(replay, skin, fallbackFont);
            return view;
        }

        void Build(IRaceReplay replay, ReplayControlsSkin skin, Font fallbackFont)
        {
            _replay = replay;
            _skin = skin;
            _font = skin && skin.font ? skin.font : fallbackFont ? fallbackFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (skin)
            {
                _textColor = skin.textColor;
                _fontStyle = skin.fontStyle;
            }

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            Transform root = transform;

            // Top left: race clock and speed, as on the race HUD; the followed runner's name below.
            RectTransform clock = MakeImage(root, "Clock", Sprite(s => s.telemetryPanel), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(600f, 106f));
            MakeImage(clock, "Stopwatch", Sprite(s => s.stopwatchIcon), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(54f, 64f));
            _timeText = MakeText(clock, "Time", 50, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(88f, 0f), new Vector2(200f, 80f));
            MakeImage(clock, "Speedometer", Sprite(s => s.speedometerIcon), new Vector2(0f, 0.5f), new Vector2(300f, 0f), new Vector2(72f, 48f));
            _speedText = MakeText(clock, "Speed", 50, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(384f, 0f), new Vector2(210f, 80f));
            RectTransform plate = MakeImage(root, "Name", Sprite(s => s.namePanel), new Vector2(0f, 1f), new Vector2(40f, -162f), new Vector2(440f, 90f));
            _nameText = MakeText(plate, "Label", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(410f, 80f));

            // Top right: < camera > and Exit, the race HUD's square buttons.
            Vector2 square = new Vector2(110f, 105f);
            Button exit = MakeButton(root, "Exit", Sprite(s => s.squareButton), new Vector2(1f, 1f), new Vector2(-40f, -40f), square, () => _replay.Leave());
            MakeImage(exit.transform, "Icon", Sprite(s => s.exitIcon), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 67f));
            Button next = MakeButton(root, "NextRunner", Sprite(s => s.squareButton), new Vector2(1f, 1f), new Vector2(-180f, -40f), square, () => _replay.Follow(1));
            MakeCut(next.transform, "Icon", _skin ? _skin.arrowSheet : null, _skin ? _skin.arrowRect : default, new Vector2(30f, 52f), false, ">", true);
            Button camera = MakeButton(root, "Camera", Sprite(s => s.squareButton), new Vector2(1f, 1f), new Vector2(-300f, -40f), square, () => _replay.NextShot(1));
            MakeCut(camera.transform, "Icon", _skin ? _skin.cameraIconSheet : null, _skin ? _skin.cameraIconRect : default, new Vector2(78f, 62f), false, "CAM");
            Button prev = MakeButton(root, "PrevRunner", Sprite(s => s.squareButton), new Vector2(1f, 1f), new Vector2(-420f, -40f), square, () => _replay.Follow(-1));
            MakeCut(prev.transform, "Icon", _skin ? _skin.arrowSheet : null, _skin ? _skin.arrowRect : default, new Vector2(30f, 52f), true, "<", true);

            // Bottom: play/pause, the timeline, the speed.
            Vector2 bottom = new Vector2(0.5f, 0f);
            Button play = MakeButton(root, "Play", Sprite(s => s.squareButton), bottom, new Vector2(-795f, 40f), square, () => _replay.SetPlaying(!_replay.IsPlaying));
            _playText = MakeText(play.transform, "Label", 52, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, square);
            _timeline = MakeSlider(root, new Vector2(-40f, 92f), new Vector2(1340f, 30f));
            _timeline.onValueChanged.AddListener(v => _replay.Seek(v * _replay.Duration));
            Button rate = MakeButton(root, "Rate", Sprite(s => s.namePanel), bottom, new Vector2(755f, 40f), new Vector2(190f, 105f), () => _replay.NextSpeed());
            _rateText = MakeText(rate.transform, "Label", 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 100f));

            _replay.WatchEnded += OnWatchEnded;
            Refresh();
        }

        void OnDestroy()
        {
            if (_replay != null) _replay.WatchEnded -= OnWatchEnded;
        }

        void OnWatchEnded() => Destroy(gameObject);

        void Update() => Refresh();

        void Refresh()
        {
            if (_replay == null) return;
            float duration = _replay.Duration;
            _timeline.SetValueWithoutNotify(duration > 0f ? _replay.Time / duration : 0f);
            _timeText.text = _replay.Time.ToString("0.00");
            _speedText.text = _replay.FollowedSpeed.ToString("0.0") + " m/s";
            _nameText.text = _replay.FollowedName;
            _playText.text = _replay.IsPlaying ? "II" : "▶";
            _rateText.text = _replay.Speed.ToString("0.##") + "x";
        }

        Sprite Sprite(System.Func<ReplayControlsSkin, Sprite> pick) => _skin ? pick(_skin) : null;

        /// <summary>An image anchored (and pivoted) at one point. A sprite with borders is sliced, so panels keep their corners at any size.</summary>
        RectTransform MakeImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = Place(go, parent, anchor, position, size);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            if (sprite)
            {
                image.sprite = sprite;
                image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            }
            else image.color = name == "Icon" || name.EndsWith("ometer") || name == "Stopwatch" ? Color.clear : PlainPanel;
            return rt;
        }

        Button MakeButton(Transform parent, string name, Sprite background, Vector2 anchor, Vector2 position, Vector2 size, UnityAction onClick)
        {
            RectTransform rt = MakeImage(parent, name, background, anchor, position, size);
            var image = rt.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>
        /// An icon cut out of a wider sprite (rect from its top-left corner); a text stand-in without the sprite. A tinted icon takes the
        /// text colour (the Continue arrow is white and would vanish on the cream button).
        /// </summary>
        void MakeCut(Transform parent, string name, Sprite sheet, RectInt cut, Vector2 size, bool mirror, string fallback, bool tint = false)
        {
            if (!sheet)
            {
                MakeText(parent, name, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size * 1.6f).text = fallback;
                return;
            }
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            RectTransform rt = Place(go, parent, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            if (mirror) rt.localScale = new Vector3(-1f, 1f, 1f);
            var image = go.GetComponent<RawImage>();
            image.raycastTarget = false;
            if (tint) image.color = _textColor;
            Texture tex = sheet.texture;
            Rect tr = sheet.textureRect;
            float fromBottom = sheet.rect.height - cut.y - cut.height;
            image.texture = tex;
            image.uvRect = new Rect((tr.x + cut.x) / tex.width, (tr.y + fromBottom) / tex.height, (float)cut.width / tex.width, (float)cut.height / tex.height);
        }

        Text MakeText(Transform parent, string name, int size, TextAnchor align, Vector2 anchor, Vector2 position, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            Place(go, parent, anchor, position, box);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = _fontStyle;
            text.alignment = align;
            text.color = _textColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        static RectTransform Place(GameObject go, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>The race HUD's energy bar as a seek bar: track, yellow fill, the cadence pointer as handle.</summary>
        Slider MakeSlider(Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject("Timeline", typeof(RectTransform), typeof(Slider));
            RectTransform rt = Place(go, parent, new Vector2(0.5f, 0f), position, size);

            RectTransform track = MakeImage(rt, "Track", Sprite(s => s.timelineTrack), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            if (!_skin) track.GetComponent<Image>().color = new Color(_textColor.r, _textColor.g, _textColor.b, 0.8f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fa = (RectTransform)fillArea.transform;
            fa.SetParent(rt, false);
            fa.anchorMin = Vector2.zero;
            fa.anchorMax = Vector2.one;
            fa.offsetMin = new Vector2(4f, 4f);
            fa.offsetMax = new Vector2(-4f, -4f);
            RectTransform fill = MakeImage(fa, "Fill", Sprite(s => s.timelineFill), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.sizeDelta = Vector2.zero;
            if (!_skin) fill.GetComponent<Image>().color = new Color(0.98f, 0.78f, 0.1f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            var ha = (RectTransform)handleArea.transform;
            ha.SetParent(rt, false);
            ha.anchorMin = Vector2.zero;
            ha.anchorMax = Vector2.one;
            ha.offsetMin = ha.offsetMax = Vector2.zero;
            RectTransform handle = MakeImage(ha, "Handle", Sprite(s => s.timelineHandle), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 0f));
            handle.anchorMin = new Vector2(0f, -0.6f);
            handle.anchorMax = new Vector2(0f, 1.6f);
            handle.sizeDelta = new Vector2(26f, 0f);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.raycastTarget = true;
            if (!_skin) handleImage.color = Color.white;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }
    }
}
