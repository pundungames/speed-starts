using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Race
{
    /// <summary>One line of the chart in plot space: x and y run 0..1 over the plot rectangle (y up).</summary>
    public sealed class ChartLine
    {
        public Vector2[] Points = new Vector2[0];
        public Color Color = Color.white;
        public float Width = 4f;
    }

    /// <summary>
    /// The chart's grid, lines and dots as one UI mesh over the plot rectangle (this object's rect). The prefab puts it
    /// under a RectMask2D, so nothing leaves the plot. <see cref="Reveal01"/> draws everything only up to that fraction of
    /// the width (the open animation moves it 0 to 1). Raycast off: the plot's own hit area takes the pointer.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AnalysisChartGraphic : MaskableGraphic
    {
        private const int CircleSegments = 10;

        private readonly List<ChartLine> _lines = new List<ChartLine>();
        private float[] _gridX = new float[0];
        private float[] _gridY = new float[0];
        private Color _gridColor = new Color(0.78f, 0.80f, 0.84f, 1f);
        private float _gridWidth = 2f;
        private float _pointRadius = 5f;
        private float _pointSpacing = 36f;
        private float _reveal = 1f;

        /// <summary>Fraction of the plot width drawn so far, 0..1.</summary>
        public float Reveal01
        {
            get => _reveal;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(clamped, _reveal)) return;
                _reveal = clamped;
                SetVerticesDirty();
            }
        }

        public void Configure(Color gridColor, float gridWidth, float pointRadius, float pointSpacing)
        {
            _gridColor = gridColor;
            _gridWidth = gridWidth;
            _pointRadius = pointRadius;
            _pointSpacing = Mathf.Max(1f, pointSpacing);
            SetVerticesDirty();
        }

        /// <summary>Grid positions (0..1 over the plot) and the lines, drawn in list order (the last one is on top).</summary>
        public void SetData(IEnumerable<float> gridX, IEnumerable<float> gridY, IEnumerable<ChartLine> lines)
        {
            _gridX = gridX != null ? new List<float>(gridX).ToArray() : new float[0];
            _gridY = gridY != null ? new List<float>(gridY).ToArray() : new float[0];
            _lines.Clear();
            if (lines != null) _lines.AddRange(lines);
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0f || r.height <= 0f) return;

            foreach (float x in _gridX)
            {
                float px = r.xMin + x * r.width;
                AddSegment(vh, new Vector2(px, r.yMin), new Vector2(px, r.yMax), _gridWidth, _gridColor);
            }
            foreach (float y in _gridY)
            {
                float py = r.yMin + y * r.height;
                AddSegment(vh, new Vector2(r.xMin, py), new Vector2(r.xMax, py), _gridWidth, _gridColor);
            }

            foreach (ChartLine line in _lines)
                if (line != null && line.Points.Length > 0) AddLine(vh, line, r);
        }

        private void AddLine(VertexHelper vh, ChartLine line, Rect r)
        {
            Vector2[] p = line.Points;
            float half = line.Width * 0.5f;
            Vector2 previous = ToRect(p[0], r);

            if (p[0].x <= _reveal) AddCircle(vh, previous, Mathf.Max(half, _pointRadius), line.Color);
            Vector2 lastDot = previous;

            for (int i = 1; i < p.Length; i++)
            {
                if (p[i - 1].x >= _reveal) break; // nothing of this segment is revealed yet

                Vector2 a = ToRect(p[i - 1], r);
                Vector2 b = ToRect(p[i], r);
                bool cut = p[i].x > _reveal;
                if (cut)
                {
                    float span = p[i].x - p[i - 1].x;
                    float t = span > 1e-6f ? Mathf.Clamp01((_reveal - p[i - 1].x) / span) : 0f;
                    b = Vector2.Lerp(a, b, t);
                }

                AddSegment(vh, a, b, line.Width, line.Color);
                AddCircle(vh, b, half, line.Color); // round joint: no gap between segments

                if (cut) break;
                bool isLast = i == p.Length - 1;
                if (isLast || (b - lastDot).sqrMagnitude >= _pointSpacing * _pointSpacing)
                {
                    AddCircle(vh, b, Mathf.Max(half, _pointRadius), line.Color);
                    lastDot = b;
                }
            }
        }

        private static Vector2 ToRect(Vector2 normalized, Rect r)
        {
            return new Vector2(r.xMin + normalized.x * r.width, r.yMin + normalized.y * r.height);
        }

        private static void AddSegment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            d.Normalize();
            Vector2 n = new Vector2(-d.y, d.x) * (width * 0.5f);

            int start = vh.currentVertCount;
            vh.AddVert(a - n, color, Vector2.zero);
            vh.AddVert(a + n, color, Vector2.zero);
            vh.AddVert(b + n, color, Vector2.zero);
            vh.AddVert(b - n, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddCircle(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            if (radius <= 0.01f) return;
            int start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleSegments;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
            }
            for (int i = 0; i < CircleSegments; i++)
                vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % CircleSegments);
        }
    }
}
