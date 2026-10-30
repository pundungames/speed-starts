using System.Collections.Generic;
using DG.Tweening;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Race Analysis screen (Analiz_UI_v1): cumulative time over distance for the last race, the ranking on the right.
    /// The prefab holds every object (chart art, tick label pool, cursor art, ranking row prefab under a scroll content);
    /// this binds and fills them. The lines are one UI mesh (AnalysisChartGraphic) over the plot rectangle; axes and
    /// ticks are computed from the data (AnalysisChartModel). Pressing or dragging on the chart picks the nearest sample
    /// of the selected runner (the player at first; pressing a ranking line picks that runner); the guide moves in X only,
    /// the marker sits on the sample, the balloon above it, kept inside the chart. Opening draws the line left to right.
    /// </summary>
    public sealed class AnalysisScreenView : UiScreen, IAnalysisSink
    {
        [Header("Config")]
        [Tooltip("Data/Config/AnalysisConfig: sampling step, axis rounding, line look.")]
        [SerializeField] private AnalysisConfig config;

        [Header("Texts")]
        [Tooltip("Diamond/Value: the player's Cash.")]
        [SerializeField] private Text cashText;

        [Tooltip("Rankings/Header/Title: event name.")]
        [SerializeField] private Text eventTitleText;

        [Tooltip("Chart/EmptyText: shown instead of the lines when the race has nothing to draw (e.g. ended at 0 m).")]
        [SerializeField] private Text emptyText;

        [Header("Chart")]
        [Tooltip("Chart: the art canvas (1038 x 502 art pixels); the cursor art moves inside it.")]
        [SerializeField] private RectTransform chartRoot;

        [Tooltip("Chart/Plot: exactly the plot rectangle of the art; tick labels hang from it.")]
        [SerializeField] private RectTransform plotRect;

        [Tooltip("Chart/Plot/Lines: the mesh graphic of grid, lines and dots (under a RectMask2D).")]
        [SerializeField] private AnalysisChartGraphic chartGraphic;

        [Tooltip("Chart/Touch: transparent surface over the plot that reports presses and drags.")]
        [SerializeField] private AnalysisChartInput chartInput;

        [Tooltip("Size of the chart art canvas in art pixels (1038 x 502).")]
        [SerializeField] private Vector2 chartArtSize = new Vector2(1038f, 502f);

        [Tooltip("Plot rectangle in art pixels, top-left origin: left, top, right, bottom (layout.json chart_plot_local).")]
        [SerializeField] private Vector4 plotArt = new Vector4(71f, 80f, 971f, 418f);

        [Header("Tick labels (fixed pool; unused ones are hidden)")]
        [Tooltip("Chart/Plot/XTicks/*: labels under the X axis (distance).")]
        [SerializeField] private Text[] xTickLabels;

        [Tooltip("Chart/Plot/YTicks/*: labels left of the Y axis (time).")]
        [SerializeField] private Text[] yTickLabels;

        [Header("Cursor")]
        [Tooltip("Chart/Guide: dashed vertical line, full art canvas; moves in X only.")]
        [SerializeField] private RectTransform guideNode;

        [Tooltip("Chart/Marker: the ring on the selected sample, full art canvas.")]
        [SerializeField] private RectTransform markerNode;

        [Tooltip("Chart/Balloon: the balloon and its text, full art canvas; sits on the marker.")]
        [SerializeField] private RectTransform balloonNode;

        [Tooltip("Chart/Balloon/Text: 'distance / time'.")]
        [SerializeField] private Text balloonText;

        [Tooltip("Marker centre in the art as drawn (art pixels, top-left origin): where it sits before it moves.")]
        [SerializeField] private Vector2 markerArt = new Vector2(607f, 249f);

        [Tooltip("Visible area of the balloon art in art pixels, top-left origin: left, top, right, bottom (from the PNG alpha).")]
        [SerializeField] private Vector4 balloonBodyArt = new Vector4(639f, 188f, 807f, 238f);

        [Header("Ranking")]
        [Tooltip("Rankings/Scroll/Viewport/Content: parent the row prefabs are created under.")]
        [SerializeField] private Transform rowsContent;

        [Tooltip("Rankings/Scroll: scrolls the list when there are more runners than the art has room for.")]
        [SerializeField] private ScrollRect rowsScroll;

        [Tooltip("AnalysisRankingRow prefab with an AnalysisRowView on its root.")]
        [SerializeField] private AnalysisRowView rowPrefab;

        private UiMotionConfig _analysisMotion;
        private readonly List<AnalysisRowView> _rows = new List<AnalysisRowView>();
        private AnalysisData _data;
        private ChartAxis _xAxis;
        private ChartAxis _yAxis;
        private int _selectedRunnerId = -1;
        private float _cursorDistance;
        private bool _hasCursor;
        private bool _revealing;
        private Vector2 _guideBase, _markerBase, _balloonBase;
        private Vector3 _balloonBaseScale = Vector3.one;
        private bool _capturedBases;
        private Tween _reveal;
        private Tween _punch;

        public float SplitStepMeters => config != null ? config.SplitStepMeters : 0f;

        public float FinishToleranceMeters => config != null ? config.FinishToleranceMeters : 0f;

        [Inject]
        public void InjectAnalysisMotion(UiMotionConfig motion)
        {
            _analysisMotion = motion;
        }

        protected override void Awake()
        {
            base.Awake();
            CaptureBases();
            if (config != null && chartGraphic != null)
                chartGraphic.Configure(config.GridColor, config.GridWidth, config.PointRadius, config.PointSpacing);
            if (chartInput != null) chartInput.Pointed += OnChartPointed;
            SetCursorVisible(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_data != null)
            {
                Canvas.ForceUpdateCanvases(); // the art rects are read below (cursor placement)
                RebuildChart();
            }
            PlayReveal();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            KillReveal();
            KillPunch();
            if (chartGraphic != null) chartGraphic.Reveal01 = 1f;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            KillReveal();
            KillPunch();
            if (chartInput != null) chartInput.Pointed -= OnChartPointed;
        }

        /// <summary>Takes the data of the race that just finished (kept until the next one) and draws it.</summary>
        public void PresentAnalysis(AnalysisData data)
        {
            if (data == null) return;
            _data = data;

            if (cashText != null) cashText.text = data.CashText;
            if (eventTitleText != null) eventTitleText.text = data.EventTitle;

            FillRows(data.Runners);

            int player = data.PlayerIndex;
            _selectedRunnerId = player >= 0 ? data.Runners[player].RunnerId : (data.Runners.Count > 0 ? data.Runners[0].RunnerId : -1);
            SplitSeries series = SelectedSeries();
            _hasCursor = series != null && series.Points.Count > 0;
            _cursorDistance = _hasCursor ? series.Last.Distance : 0f; // opens on the finish

            RebuildChart();
            if (emptyText != null) emptyText.gameObject.SetActive(!data.HasChartData);
            if (!_revealing && isActiveAndEnabled) ShowCursor(false);
        }

        // ---------------------------------------------------------------- chart

        private void RebuildChart()
        {
            if (_data == null || chartGraphic == null || config == null) return;

            var all = new List<SplitSeries>();
            foreach (AnalysisRunner runner in _data.Runners)
                if (runner.Series != null && runner.Series.HasLine) all.Add(runner.Series);

            _xAxis = AnalysisChartModel.DistanceAxis(_data.EventDistance, all, config.TargetTickCount, config.TickMultipliers);
            _yAxis = AnalysisChartModel.TimeAxis(all, config.TargetTickCount, config.TickMultipliers);

            List<double> xTicks = AnalysisChartModel.Ticks(_xAxis);
            List<double> yTicks = AnalysisChartModel.Ticks(_yAxis);
            var gridX = new List<float>();
            var gridY = new List<float>();
            foreach (double t in xTicks) if (t > 0d) gridX.Add(AnalysisChartModel.Fraction(t, _xAxis.Max));
            foreach (double t in yTicks) if (t > 0d) gridY.Add(AnalysisChartModel.Fraction(t, _yAxis.Max));
            FillTicks(xTickLabels, xTicks, _xAxis.Max, horizontal: true);
            FillTicks(yTickLabels, yTicks, _yAxis.Max, horizontal: false);

            // The selected runner is drawn last (on top) and thicker.
            var lines = new List<ChartLine>();
            ChartLine selected = null;
            foreach (AnalysisRunner runner in _data.Runners)
            {
                if (runner.Series == null || !runner.Series.HasLine) continue;
                bool isSelected = runner.RunnerId == _selectedRunnerId;
                ChartLine line = BuildLine(runner, isSelected);
                if (isSelected) selected = line;
                else lines.Add(line);
            }
            if (selected != null) lines.Add(selected);

            chartGraphic.SetData(gridX, gridY, lines);
        }

        private ChartLine BuildLine(AnalysisRunner runner, bool isSelected)
        {
            IReadOnlyList<SplitPoint> points = runner.Series.Points;
            var normalized = new Vector2[points.Count];
            for (int i = 0; i < points.Count; i++)
                normalized[i] = new Vector2(AnalysisChartModel.Fraction(points[i].Distance, _xAxis.Max), AnalysisChartModel.Fraction(points[i].Seconds, _yAxis.Max));
            return new ChartLine
            {
                Points = normalized,
                Color = config.LineColor(runner.RunnerId),
                Width = isSelected ? config.SelectedLineWidth : config.LineWidth
            };
        }

        private void FillTicks(Text[] labels, List<double> ticks, double axisMax, bool horizontal)
        {
            if (labels == null) return;
            for (int i = 0; i < labels.Length; i++)
            {
                Text label = labels[i];
                if (label == null) continue;
                bool used = i < ticks.Count;
                label.gameObject.SetActive(used);
                if (!used) continue;

                label.text = AnalysisChartModel.TickText(ticks[i]);
                float f = AnalysisChartModel.Fraction(ticks[i], axisMax);
                // The label hangs from a point of the plot edge (anchors, not pixels): no dependence on a laid-out rect.
                RectTransform rt = label.rectTransform;
                Vector2 anchor = rt.anchorMin;
                if (horizontal) anchor.x = f;
                else anchor.y = f;
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
            }
        }

        // ---------------------------------------------------------------- cursor

        private void OnChartPointed(Vector2 screenPoint, Camera eventCamera)
        {
            if (_revealing || _data == null || plotRect == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(plotRect, screenPoint, eventCamera, out Vector2 local)) return;

            Rect plot = plotRect.rect;
            if (plot.width <= 0f) return;
            float x01 = Mathf.Clamp01((local.x - plot.xMin) / plot.width);
            _cursorDistance = x01 * (float)_xAxis.Max;
            ShowCursor(true);
        }

        private void OnRowSelected(int runnerId)
        {
            if (_data == null || runnerId == _selectedRunnerId) return;
            _selectedRunnerId = runnerId;
            RebuildChart();
            if (!_revealing) ShowCursor(true);
        }

        private SplitSeries SelectedSeries()
        {
            if (_data == null) return null;
            foreach (AnalysisRunner runner in _data.Runners)
                if (runner.RunnerId == _selectedRunnerId) return runner.Series;
            return null;
        }

        /// <summary>Puts guide, marker and balloon on the sample nearest to the cursor distance of the selected runner.</summary>
        private void ShowCursor(bool punch)
        {
            SplitSeries series = SelectedSeries();
            int index = series != null && series.HasLine ? AnalysisChartModel.NearestIndex(series.Points, _cursorDistance) : -1;
            if (index < 0 || config == null || chartRoot == null)
            {
                SetCursorVisible(false);
                return;
            }

            SplitPoint point = series.Points[index];
            _cursorDistance = point.Distance;
            float x01 = AnalysisChartModel.Fraction(point.Distance, _xAxis.Max);
            float y01 = AnalysisChartModel.Fraction(point.Seconds, _yAxis.Max);

            // Art pixels (top-left origin): where the sample is on the chart art, and how far the art as drawn must move.
            float artX = plotArt.x + x01 * (plotArt.z - plotArt.x);
            float artY = plotArt.w - y01 * (plotArt.w - plotArt.y);
            Vector2 delta = new Vector2(artX - markerArt.x, artY - markerArt.y);

            // The balloon follows the marker but stays inside the chart (art edges minus a margin).
            float margin = config.BalloonEdgeMargin;
            Vector2 balloonDelta = delta;
            float left = balloonBodyArt.x + delta.x, right = balloonBodyArt.z + delta.x;
            float top = balloonBodyArt.y + delta.y, bottom = balloonBodyArt.w + delta.y;
            if (left < margin) balloonDelta.x += margin - left;
            else if (right > chartArtSize.x - margin) balloonDelta.x -= right - (chartArtSize.x - margin);
            if (top < margin) balloonDelta.y += margin - top;
            else if (bottom > chartArtSize.y - margin) balloonDelta.y -= bottom - (chartArtSize.y - margin);

            CaptureBases();
            SetCursorVisible(true);
            if (guideNode != null) guideNode.anchoredPosition = _guideBase + ArtToLocal(new Vector2(delta.x, 0f));
            if (markerNode != null) markerNode.anchoredPosition = _markerBase + ArtToLocal(delta);
            if (balloonNode != null) balloonNode.anchoredPosition = _balloonBase + ArtToLocal(balloonDelta);
            if (balloonText != null) balloonText.text = AnalysisChartModel.CursorText(point.Distance, point.Seconds);
            if (punch) PunchBalloon();
        }

        /// <summary>An offset in art pixels (y down) as a local offset of the chart root (y up).</summary>
        private Vector2 ArtToLocal(Vector2 art)
        {
            Rect rect = chartRoot.rect;
            return new Vector2(art.x * rect.width / chartArtSize.x, -art.y * rect.height / chartArtSize.y);
        }

        private void SetCursorVisible(bool visible)
        {
            if (guideNode != null) guideNode.gameObject.SetActive(visible);
            if (markerNode != null) markerNode.gameObject.SetActive(visible);
            if (balloonNode != null) balloonNode.gameObject.SetActive(visible);
        }

        private void CaptureBases()
        {
            if (_capturedBases) return;
            if (guideNode != null) _guideBase = guideNode.anchoredPosition;
            if (markerNode != null) _markerBase = markerNode.anchoredPosition;
            if (balloonNode != null)
            {
                _balloonBase = balloonNode.anchoredPosition;
                _balloonBaseScale = balloonNode.localScale;
            }
            _capturedBases = true;
        }

        private void PunchBalloon()
        {
            if (balloonNode == null || _analysisMotion == null || !isActiveAndEnabled) return;

            KillPunch();
            Transform target = balloonNode;
            Vector3 baseScale = _balloonBaseScale;
            Tween punch = target
                .DOPunchScale(baseScale * _analysisMotion.ButtonPunchScale, _analysisMotion.ButtonPunchDuration, _analysisMotion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
            punch.OnKill(() =>
            {
                if (_punch == punch) _punch = null;
                if (target != null) target.localScale = baseScale;
            });
            _punch = punch;
        }

        private void KillPunch()
        {
            if (_punch == null) return;
            Tween running = _punch;
            _punch = null;
            running.Kill();
        }

        // ---------------------------------------------------------------- open animation

        private void PlayReveal()
        {
            KillReveal();
            if (chartGraphic == null) return;

            float duration = _analysisMotion != null && config != null ? _analysisMotion.ScreenOpen.Duration * config.LineDrawDurationFactor : 0f;
            if (duration <= 0f)
            {
                chartGraphic.Reveal01 = 1f;
                if (_data != null) ShowCursor(false);
                return;
            }

            _revealing = true;
            chartGraphic.Reveal01 = 0f;
            SetCursorVisible(false);
            Tween tween = DOTween.To(() => chartGraphic.Reveal01, value => chartGraphic.Reveal01 = value, 1f, duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(gameObject);
            tween.OnComplete(() =>
            {
                _revealing = false;
                if (_data != null) ShowCursor(true); // the balloon lands on the finish with its punch
            });
            tween.OnKill(() =>
            {
                if (_reveal == tween) _reveal = null;
                _revealing = false;
            });
            _reveal = tween;
        }

        private void KillReveal()
        {
            if (_reveal == null)
            {
                _revealing = false;
                return;
            }
            Tween running = _reveal;
            _reveal = null;
            running.Kill();
        }

        // ---------------------------------------------------------------- ranking

        private void FillRows(IReadOnlyList<AnalysisRunner> runners)
        {
            foreach (AnalysisRowView old in _rows)
            {
                if (old == null) continue;
                old.Selected -= OnRowSelected;
                old.gameObject.SetActive(false); // out of the layout now, destroyed at the end of the frame
                Destroy(old.gameObject);
            }
            _rows.Clear();

            if (rowsContent == null || rowPrefab == null)
            {
                Debug.LogWarning($"{name}: ranking content or row prefab is not assigned; the ranking is empty.", this);
                return;
            }
            foreach (AnalysisRunner runner in runners)
            {
                AnalysisRowView view = Instantiate(rowPrefab, rowsContent);
                view.Fill(runner);
                view.Selected += OnRowSelected;
                _rows.Add(view);
            }
            if (rowsScroll != null) rowsScroll.verticalNormalizedPosition = 1f;
        }
    }
}
