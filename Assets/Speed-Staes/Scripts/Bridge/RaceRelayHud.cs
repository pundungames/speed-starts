using System.Text;
using SpeedStars.Core.Race;
using SpeedStars.Core.Race.Relay;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// The relay's own HUD, as in the reference game: a band at every leg line ("100m: 23.82s (+9.24)", then "2nd leg: 12.28"),
    /// the handoff banner ("Perfect handoff!" / "Late handoff!"), the split list under the race buttons, and the distance marker
    /// over the player's next runner counting the metres down to the exchange. Built at runtime on its own overlay canvas (no scene
    /// or prefab change), with the race HUD's font; shown only while a relay runs and hidden under Results.
    /// </summary>
    public sealed class RaceRelayHud : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("How long a leg line band stays on screen (s).")]
        float bandSeconds = 3f;
        [SerializeField, Min(0f), Tooltip("How long the handoff banner stays on screen (s).")]
        float bannerSeconds = 2.5f;
        [SerializeField, Min(0f), Tooltip("The distance marker keeps at least this far inside the screen edges (reference pixels).")]
        float markerEdgeMargin = 90f;
        [SerializeField, Tooltip("Sorting order of the relay HUD canvas: above the race HUD.")]
        int sortingOrder = 5;
        [SerializeField, Tooltip("Colour of the split time in the band.")]
        Color splitColor = new Color(0.10f, 0.78f, 0.85f);
        [SerializeField, Tooltip("Colour of the gap to the fastest team in the band.")]
        Color behindColor = new Color(0.95f, 0.30f, 0.35f);
        [SerializeField, Tooltip("Colour of the 'Perfect handoff!' banner text.")]
        Color perfectColor = new Color(0.62f, 0.15f, 0.72f);
        [SerializeField, Tooltip("Colour of the 'Late handoff!' banner text.")]
        Color lateColor = new Color(0.90f, 0.15f, 0.20f);

        RaceRelay _relay;
        RaceSessionAdapter _session;
        Canvas _canvas;
        RectTransform _root;
        GameObject _band, _banner;
        Text _bandText, _bandLegText, _bannerText, _splitsText, _markerText;
        float _bandUntil, _bannerUntil;
        readonly StringBuilder _splits = new StringBuilder();
        bool _finished;

        public void Bind(RaceRelay relay, RaceSessionAdapter session)
        {
            _relay = relay;
            _session = session;
            _relay.LineCrossed += OnLineCrossed;
            _relay.HandedOver += OnHandedOver;
            _relay.ActiveChanged += OnActiveChanged;
            if (_session)
            {
                _session.Finished += OnFinished;
                _session.StartPhaseChanged += OnStartPhase;
            }
        }

        void OnDestroy()
        {
            if (_relay)
            {
                _relay.LineCrossed -= OnLineCrossed;
                _relay.HandedOver -= OnHandedOver;
                _relay.ActiveChanged -= OnActiveChanged;
            }
            if (_session)
            {
                _session.Finished -= OnFinished;
                _session.StartPhaseChanged -= OnStartPhase;
            }
            if (_canvas) Destroy(_canvas.gameObject);
        }

        void OnActiveChanged()
        {
            EnsureBuilt();
            Clear();
            Refresh();
        }

        void OnStartPhase(RaceStartPhase phase)
        {
            if (phase != RaceStartPhase.Ready) return;
            _finished = false; // a new attempt: splits from zero
            Clear();
            Refresh();
        }

        void OnFinished(RaceOutcome outcome)
        {
            _finished = true;
            Refresh();
        }

        void OnLineCrossed(int line, double seconds, double? behind, double? legTime)
        {
            EnsureBuilt();
            string meters = Mathf.RoundToInt(line * _relay.LegMeters) + "m";
            string gap = behind.HasValue ? $" <color=#{ColorUtility.ToHtmlStringRGB(behindColor)}>(+{behind.Value:0.00})</color>" : string.Empty;
            _bandText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(splitColor)}>{meters}: {seconds:0.00}s</color>{gap}";
            _bandLegText.text = legTime.HasValue ? $"{Ordinal(line)} leg: {legTime.Value:0.00}" : string.Empty;
            _bandLegText.color = _relay.LegColor(line - 1);
            _bandUntil = Time.time + bandSeconds;

            if (_splits.Length > 0) _splits.Append('\n');
            _splits.Append(meters).Append(": ").Append(seconds.ToString("0.00"));
            _splitsText.text = _splits.ToString();
        }

        void OnHandedOver(int leg, HandoffQuality quality)
        {
            EnsureBuilt();
            _bannerText.text = quality == HandoffQuality.Perfect ? "Perfect handoff!" : "Late handoff!";
            _bannerText.color = quality == HandoffQuality.Perfect ? perfectColor : lateColor;
            _bannerUntil = Time.time + bannerSeconds;
        }

        void LateUpdate()
        {
            if (!_canvas) return;
            Refresh();
            if (!_canvas.enabled) return;

            Camera cam = Camera.main;
            Vector3 world = Vector3.zero;
            float meters = 0f;
            int leg = 0;
            bool marker = cam && !_finished && _relay.TryGetMarker(out world, out meters, out leg);
            if (marker)
            {
                Vector3 screen = cam.WorldToScreenPoint(world);
                marker = screen.z > 0f;
                if (marker)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local);
                    // The next runner can stand past the right edge of the frame: the marker stays on screen.
                    Vector2 half = _root.rect.size * 0.5f - new Vector2(markerEdgeMargin, markerEdgeMargin);
                    local.x = Mathf.Clamp(local.x, -half.x, half.x);
                    local.y = Mathf.Clamp(local.y, -half.y, half.y);
                    _markerText.rectTransform.anchoredPosition = local;
                    _markerText.text = Mathf.FloorToInt(meters) + "m";
                    _markerText.color = _relay.LegColor(leg);
                }
            }
            _markerText.gameObject.SetActive(marker);
        }

        void Refresh()
        {
            if (!_canvas) return;
            bool show = _relay && _relay.Active && !_finished;
            _canvas.enabled = show;
            _band.SetActive(show && Time.time < _bandUntil);
            _banner.SetActive(show && Time.time < _bannerUntil);
        }

        void Clear()
        {
            if (!_canvas) return;
            _splits.Clear();
            _splitsText.text = string.Empty;
            _bandUntil = _bannerUntil = 0f;
        }

        void EnsureBuilt()
        {
            if (_canvas) return;
            Font font = HudFont();

            var go = new GameObject("Relay HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            _root = (RectTransform)go.transform;

            _band = Strip("Leg Band", new Vector2(0f, -250f), 150f, 0.92f);
            _bandText = Label(_band.transform, "Split", new Vector2(0f, 22f), 72, font, TextAnchor.MiddleCenter);
            _bandLegText = Label(_band.transform, "Leg", new Vector2(0f, -42f), 44, font, TextAnchor.MiddleCenter);

            _banner = Strip("Handoff Banner", new Vector2(0f, -360f), 64f, 0.92f);
            _bannerText = Label(_banner.transform, "Text", Vector2.zero, 50, font, TextAnchor.MiddleCenter);

            _splitsText = Label(_root, "Splits", Vector2.zero, 36, font, TextAnchor.UpperLeft);
            RectTransform splits = _splitsText.rectTransform;
            splits.anchorMin = splits.anchorMax = splits.pivot = new Vector2(0f, 1f);
            splits.anchoredPosition = new Vector2(48f, -420f); // under the band and the banner
            splits.sizeDelta = new Vector2(420f, 240f);
            Outline(_splitsText);

            _markerText = Label(_root, "Distance Marker", Vector2.zero, 64, font, TextAnchor.MiddleCenter);
            RectTransform marker = _markerText.rectTransform;
            marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0.5f);
            Outline(_markerText);
            _markerText.gameObject.SetActive(false);
        }

        GameObject Strip(string stripName, Vector2 position, float height, float alpha)
        {
            var go = new GameObject(stripName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(0f, height);
            Image image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, alpha);
            image.raycastTarget = false;
            go.SetActive(false);
            return go;
        }

        static Text Label(Transform parent, string labelName, Vector2 position, int size, Font font, TextAnchor anchor)
        {
            var go = new GameObject(labelName, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(1600f, size * 1.4f);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontStyle = FontStyle.BoldAndItalic; // the race HUD's style
            text.fontSize = size;
            text.alignment = anchor;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;
            return text;
        }

        static void Outline(Text text)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3f, -3f);
        }

        /// <summary>The race HUD's own font, so the relay texts look like the rest; the built-in font without one.</summary>
        static Font HudFont()
        {
            var hud = FindObjectOfType<SpeedStars.UI.Race.RaceHudView>(true);
            Text any = hud ? hud.GetComponentInChildren<Text>(true) : null;
            if (any && any.font) return any.font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static string Ordinal(int n) => n + (n % 100 >= 11 && n % 100 <= 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" });
    }
}
