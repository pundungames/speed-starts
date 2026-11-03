using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Core.Race.Relay;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// The relay's own HUD, as in the reference game: a band at every leg line ("100m: 23.82s (+9.24)", then "2nd leg: 12.28"),
    /// the handoff banner ("PERFECT HANDOFF!" / "LATE HANDOFF!"), the split list under the race HUD's counters, and the distance
    /// marker over the player's next runner counting the metres down to the exchange. Dressed in the game's UI art (UI/Art: the
    /// start banner, the Results header strip, the HUD counter panel) and laid out on the race HUD's own reference resolution, so it
    /// reads as part of the HUD. Built at runtime on its own overlay canvas (no scene or prefab change): the sprites are the ones the
    /// Race scene already has loaded, found by name; shown only while a relay runs and hidden under Results.
    /// </summary>
    public sealed class RaceRelayHud : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0f), Tooltip("How long a leg line band stays on screen (s).")]
        float bandSeconds = 3f;
        [SerializeField, Min(0f), Tooltip("How long the handoff banner stays on screen (s).")]
        float bannerSeconds = 2.5f;

        [Header("Art (UI/Art sprite names, loaded by the Race scene)")]
        [SerializeField, Tooltip("Leg line band: the race start banner (RaceStart/Banner).")]
        string bandSprite = "start_banner_background";
        [SerializeField, Tooltip("Handoff banner: the dark header strip of the Results screen (Results).")]
        string bannerSprite = "daily_best_header";
        [SerializeField, Tooltip("Split rows and the distance marker: the race HUD's counter panel (RaceHud).")]
        string panelSprite = "counter_panel";

        [Header("Layout (race HUD reference pixels, 2400 x 1080)")]
        [SerializeField, Tooltip("Reference resolution of the race HUD canvas; this canvas uses the same so both line up.")]
        Vector2 referenceResolution = new Vector2(2400f, 1080f);
        [SerializeField, Tooltip("Leg line band: centre below the top edge, size.")]
        Vector2 bandPosition = new Vector2(0f, -250f);
        [SerializeField] Vector2 bandSize = new Vector2(780f, 166f);
        [SerializeField, Tooltip("Handoff banner: centre below the top edge, size.")]
        Vector2 bannerPosition = new Vector2(0f, -372f);
        [SerializeField] Vector2 bannerSize = new Vector2(560f, 66f);
        [SerializeField, Tooltip("First split row: top-left, from the HUD counters' anchor (5 % of the width, top), like the Cash / Place counters.")]
        Vector2 splitsPosition = new Vector2(52f, -218f);
        [SerializeField, Tooltip("Split row size and the step between rows.")]
        Vector2 splitRowSize = new Vector2(205f, 62f);
        [SerializeField] float splitRowStep = 68f;
        [SerializeField, Tooltip("Distance marker size.")]
        Vector2 markerSize = new Vector2(140f, 56f);
        [SerializeField, Min(0f), Tooltip("The distance marker keeps at least this far inside the screen edges (reference pixels).")]
        float markerEdgeMargin = 110f;
        [SerializeField, Tooltip("Sorting order of the relay HUD canvas: above the race HUD, below Results.")]
        int sortingOrder = 5;

        [Header("Colours (the HUD's own)")]
        [SerializeField, Tooltip("HUD text navy.")]
        Color navy = new Color32(0x0A, 0x1F, 0x5C, 0xFF);
        [SerializeField, Tooltip("Gap to the fastest team in the band: the banner's red.")]
        Color behindColor = new Color32(0xE8, 0x20, 0x2E, 0xFF);
        [SerializeField, Tooltip("'PERFECT HANDOFF!' on the dark strip.")]
        Color perfectColor = Color.white;
        [SerializeField, Tooltip("'LATE HANDOFF!' on the dark strip.")]
        Color lateColor = new Color32(0xFF, 0x5A, 0x64, 0xFF);
        [SerializeField, Range(0f, 1f), Tooltip("Distance marker panel: how much of the leg colour tints the cream panel.")]
        float markerTint = 0.55f;

        RaceRelay _relay;
        RaceSessionAdapter _session;
        Canvas _canvas;
        RectTransform _root;
        Font _font;
        GameObject _band, _banner, _marker;
        Image _markerPanel;
        Text _bandText, _bandLegText, _bannerText, _markerText;
        RectTransform _splits;
        readonly List<GameObject> _splitRows = new List<GameObject>();
        float _bandUntil, _bannerUntil;
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
            _bandText.text = $"{meters}: {seconds:0.00}s{gap}";
            _bandLegText.text = legTime.HasValue ? $"{Ordinal(line)} leg: {legTime.Value:0.00}" : string.Empty;
            _bandLegText.color = _relay.LegColor(line - 1);
            _bandUntil = Time.time + bandSeconds;
            AddSplitRow(meters, seconds, _relay.LegColor(line - 1));
        }

        void OnHandedOver(int leg, HandoffQuality quality)
        {
            EnsureBuilt();
            bool perfect = quality == HandoffQuality.Perfect;
            _bannerText.text = perfect ? "PERFECT HANDOFF!" : "LATE HANDOFF!";
            _bannerText.color = perfect ? perfectColor : lateColor;
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
                    ((RectTransform)_marker.transform).anchoredPosition = local;
                    _markerText.text = Mathf.FloorToInt(meters) + "m";
                    _markerPanel.color = Color.Lerp(Color.white, _relay.LegColor(leg), markerTint);
                }
            }
            _marker.SetActive(marker);
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
            foreach (GameObject row in _splitRows) Destroy(row);
            _splitRows.Clear();
            _bandUntil = _bannerUntil = 0f;
        }

        /// <summary>One HUD counter panel per leg line, stacked under the Cash counter: a leg-coloured tick, the line and its time.</summary>
        void AddSplitRow(string meters, double seconds, Color legColor)
        {
            var row = Panel("Split " + meters, _splits, panelSprite, splitRowSize);
            var rect = (RectTransform)row.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -_splitRows.Count * splitRowStep);

            var tick = new GameObject("Leg", typeof(RectTransform), typeof(Image));
            tick.transform.SetParent(row.transform, false);
            var tickRect = (RectTransform)tick.transform;
            tickRect.anchorMin = tickRect.anchorMax = tickRect.pivot = new Vector2(0f, 0.5f);
            tickRect.anchoredPosition = new Vector2(14f, 0f);
            tickRect.sizeDelta = new Vector2(8f, splitRowSize.y * 0.55f);
            Image tickImage = tick.GetComponent<Image>();
            tickImage.color = legColor;
            tickImage.raycastTarget = false;

            Text text = Label(row.transform, "Text", 26, navy, TextAnchor.MiddleCenter);
            text.rectTransform.offsetMin = new Vector2(20f, 0f); // clear of the leg tick
            text.rectTransform.offsetMax = new Vector2(-8f, 0f);
            text.text = $"{meters}  {seconds:0.00}";
            _splitRows.Add(row);
        }

        void EnsureBuilt()
        {
            if (_canvas) return;
            _font = HudFont();

            var go = new GameObject("Relay HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0f; // as the race HUD: by width
            _root = (RectTransform)go.transform;

            _band = Panel("Leg Band", _root, bandSprite, bandSize);
            Place(_band, new Vector2(0.5f, 1f), bandPosition);
            _bandText = Label(_band.transform, "Split", 58, navy, TextAnchor.MiddleCenter);
            _bandText.rectTransform.offsetMin = new Vector2(0f, bandSize.y * 0.36f);
            _bandText.rectTransform.offsetMax = new Vector2(0f, -bandSize.y * 0.06f);
            _bandLegText = Label(_band.transform, "Leg", 38, navy, TextAnchor.MiddleCenter);
            _bandLegText.rectTransform.offsetMin = new Vector2(0f, bandSize.y * 0.12f);
            _bandLegText.rectTransform.offsetMax = new Vector2(0f, -bandSize.y * 0.58f);
            Outline(_bandLegText, navy, 2f); // leg colours (yellow too) stay readable on the cream band
            _band.SetActive(false);

            _banner = Panel("Handoff Banner", _root, bannerSprite, bannerSize);
            Place(_banner, new Vector2(0.5f, 1f), bannerPosition);
            _bannerText = Label(_banner.transform, "Text", 38, perfectColor, TextAnchor.MiddleCenter);
            _banner.SetActive(false);

            var splits = new GameObject("Splits", typeof(RectTransform));
            splits.transform.SetParent(_root, false);
            _splits = (RectTransform)splits.transform;
            _splits.anchorMin = _splits.anchorMax = new Vector2(0.05f, 1f); // the HUD counters' anchor
            _splits.pivot = new Vector2(0f, 1f);
            _splits.anchoredPosition = splitsPosition;
            _splits.sizeDelta = splitRowSize;

            _marker = Panel("Distance Marker", _root, panelSprite, markerSize);
            var markerRect = (RectTransform)_marker.transform;
            markerRect.anchorMin = markerRect.anchorMax = markerRect.pivot = new Vector2(0.5f, 0.5f);
            _markerPanel = _marker.GetComponent<Image>();
            _markerText = Label(_marker.transform, "Text", 38, navy, TextAnchor.MiddleCenter);
            _marker.SetActive(false);
        }

        static void Place(GameObject go, Vector2 anchor, Vector2 position)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
        }

        /// <summary>An Image with a UI/Art sprite (stretched to the size, as the HUD draws its panels); plain cream without the sprite.</summary>
        GameObject Panel(string panelName, Transform parent, string spriteName, Vector2 size)
        {
            var go = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = FindSprite(spriteName);
            if (!image.sprite) image.color = new Color(1f, 0.98f, 0.93f);
            image.raycastTarget = false;
            return go;
        }

        Text Label(Transform parent, string labelName, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(labelName, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Text text = go.GetComponent<Text>();
            text.font = _font;
            text.fontStyle = FontStyle.BoldAndItalic; // the race HUD's style
            text.fontSize = size;
            text.alignment = anchor;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = color;
            return text;
        }

        static void Outline(Text text, Color color, float size)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(size, -size);
        }

        /// <summary>A sprite the scene already uses (UI/Art), by name; null if none is loaded.</summary>
        static Sprite FindSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                if (sprite.name == spriteName) return sprite;
            return null;
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
