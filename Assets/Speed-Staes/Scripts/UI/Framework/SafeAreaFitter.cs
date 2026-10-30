using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Framework
{
    /// <summary>What a SafeAreaFitter squeezes into the safe area.</summary>
    public enum SafeAreaMode
    {
        /// <summary>The direct children of this object (a stretched screen root): edge items move in, backgrounds stay full-bleed.</summary>
        Children,

        /// <summary>This object itself (an edge widget such as the resource bar).</summary>
        Self
    }

    /// <summary>
    /// Keeps edge-anchored UI out from under a camera hole / notch. It only rewrites the anchors of rects that already
    /// exist in the prefab (no hierarchy is built): each authored anchor is mapped into the safe rectangle, offsets and sizes
    /// are untouched, so an animation moving anchoredPosition keeps working. A screen background or dim (a stretched child
    /// that draws something, or one marked FullBleed) is left alone and still fills the whole screen.
    /// Written only when the safe area or the screen size changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField, Tooltip("Children: fit the direct children of this stretched root. Self: fit this rect itself.")]
        private SafeAreaMode mode = SafeAreaMode.Children;

        [SerializeField, Tooltip("Also keep the top/bottom edges inside the safe area (the game is landscape, so normally off).")]
        private bool includeVertical;

        [SerializeField, Min(0f), Tooltip("Insets this small (px) count as none, so a notch-less phone keeps the authored layout.")]
        private float tolerancePx = 1f;

        private struct Entry
        {
            public RectTransform Rect;
            public Vector2 AuthoredMin;
            public Vector2 AuthoredMax;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private ISafeAreaSource _source;
        private bool _collected;
        private bool _hasApplied;
        private Vector2 _lastScreen;
        private Rect _lastSafe;

        /// <summary>How many times anchors were actually written (tests: unchanged input must not write again).</summary>
        public int WriteCount { get; private set; }

        public SafeAreaMode Mode => mode;

        /// <summary>Replaces the source (tests, or a future platform wrapper) and refits at once.</summary>
        public void SetSource(ISafeAreaSource source)
        {
            _source = source;
            Refresh();
        }

        /// <summary>Seam for code that builds the component (scene builder) without going through the serialized fields.</summary>
        public void Configure(SafeAreaMode newMode, bool vertical)
        {
            mode = newMode;
            includeVertical = vertical;
            _collected = false;
            _hasApplied = false;
        }

        private void OnEnable()
        {
            Refresh();
        }

        // The safe area changes on rotation / fold / simulator switch; polling two cached values is cheaper than any event.
        private void Update()
        {
            Refresh();
        }

        /// <summary>Fits when the safe area or screen size differs from the last fit. Returns true when anchors were written.</summary>
        public bool Refresh()
        {
            ISafeAreaSource source = _source ?? (_source = new ScreenSafeAreaSource());
            Vector2 screen = source.ScreenSize;
            Rect safe = source.SafeArea;
            if (_hasApplied && screen == _lastScreen && safe == _lastSafe) return false;

            Collect();
            SafeAreaMath.SafeAnchors(screen, safe, includeVertical, tolerancePx, out Vector2 safeMin, out Vector2 safeMax);
            for (int i = 0; i < _entries.Count; i++) Write(_entries[i], safeMin, safeMax);

            _hasApplied = true;
            _lastScreen = screen;
            _lastSafe = safe;
            WriteCount++;
            return true;
        }

        private void Collect()
        {
            if (_collected) return;
            _collected = true;
            _entries.Clear();

            if (mode == SafeAreaMode.Self)
            {
                if (transform is RectTransform self) AddEntry(self);
                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                if (!(transform.GetChild(i) is RectTransform child)) continue;
                if (child.GetComponent<FullBleed>() != null) continue;
                bool graphic = child.GetComponent<Graphic>() != null;
                if (SafeAreaMath.LooksFullBleed(child.anchorMin, child.anchorMax, child.offsetMin, child.offsetMax, graphic, tolerancePx)) continue;
                AddEntry(child);
            }
        }

        private void AddEntry(RectTransform rect)
        {
            _entries.Add(new Entry { Rect = rect, AuthoredMin = rect.anchorMin, AuthoredMax = rect.anchorMax });
        }

        private static void Write(Entry entry, Vector2 safeMin, Vector2 safeMax)
        {
            if (entry.Rect == null) return;
            Vector2 min = SafeAreaMath.Remap(entry.AuthoredMin, safeMin, safeMax);
            Vector2 max = SafeAreaMath.Remap(entry.AuthoredMax, safeMin, safeMax);
            RectTransform rect = entry.Rect;
            if (rect.anchorMin == min && rect.anchorMax == max) return;

            // Setting anchors makes Unity keep the rect where it is by rewriting the position and size; the authored offsets
            // (and any animated position) must survive, so they are put back.
            Vector2 position = rect.anchoredPosition;
            Vector2 size = rect.sizeDelta;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
