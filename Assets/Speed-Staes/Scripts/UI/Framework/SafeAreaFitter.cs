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
    /// Edit-mode preview: so the notch shows in the Device Simulator while the UI is designed, the fitted anchors are also
    /// applied in Edit mode, but only as DRIVEN values (DrivenRectTransformTracker): they are not saved to the scene or prefab and
    /// are restored to the authored anchors the moment the fitter is disabled or the preview is switched off
    /// (menu Tools/Speed Stars/Notch Preview (Edit Mode)). While the preview is inset, those anchors are read-only
    /// (greyed) in the Inspector; switch the preview off to edit them. Play mode ignores the preview flag entirely.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField, Tooltip("Children: fit the direct children of this stretched root. Self: fit this rect itself. In Edit mode the fitted anchors are driven (greyed, read-only, never saved) while a notch preview is active; turn it off with Tools > Speed Stars > Notch Preview (Edit Mode) to edit them.")]
        private SafeAreaMode mode = SafeAreaMode.Children;

        [SerializeField, Tooltip("Also keep the top/bottom edges inside the safe area (the game is landscape, so normally off).")]
        private bool includeVertical;

        [SerializeField, Tooltip("Move both left and right edges in by the larger of the two insets, so a notch on one side gives a balanced layout (and flipping the phone does not shift it).")]
        private bool mirrorSides = true;

        [SerializeField, Min(0f), Tooltip("Insets this small (px) count as none, so a notch-less phone keeps the authored layout.")]
        private float tolerancePx = 1f;

#if UNITY_EDITOR
        /// <summary>EditorPrefs key of the Edit-mode preview switch (default on).</summary>
        public const string PreviewPrefKey = "SpeedStars.SafeAreaEditPreview";

        private static readonly List<SafeAreaFitter> ActiveFitters = new List<SafeAreaFitter>();
        private static int _previewCache = -1;

        /// <summary>Edit-mode notch preview switch (persisted in EditorPrefs, default on). Never read in Play mode.</summary>
        public static bool EditorPreview
        {
            get
            {
                if (_previewCache < 0) _previewCache = UnityEditor.EditorPrefs.GetBool(PreviewPrefKey, true) ? 1 : 0;
                return _previewCache == 1;
            }
        }

        /// <summary>Switches the preview and refits every active fitter at once (off restores the authored anchors).</summary>
        public static void SetEditorPreview(bool on)
        {
            _previewCache = on ? 1 : 0;
            UnityEditor.EditorPrefs.SetBool(PreviewPrefKey, on);
            RefreshActive();
        }

        /// <summary>Before an editor save: drop driven anchors so nothing inset can reach the file, even if Unity serialized them.</summary>
        public static void SuspendPreviewForSave()
        {
            if (Application.isPlaying) return;
            for (int i = 0; i < ActiveFitters.Count; i++)
            {
                if (ActiveFitters[i] == null) continue;
                ActiveFitters[i].ReleaseDriven();
                ActiveFitters[i]._hasApplied = false;
            }
        }

        /// <summary>After an editor save: put the preview back.</summary>
        public static void ResumePreviewAfterSave()
        {
            RefreshActive();
        }

        private static void RefreshActive()
        {
            for (int i = ActiveFitters.Count - 1; i >= 0; i--)
            {
                if (i >= ActiveFitters.Count) continue;
                SafeAreaFitter fitter = ActiveFitters[i];
                if (fitter == null) { ActiveFitters.RemoveAt(i); continue; }
                fitter.Refresh();
            }
        }

        // The Device Simulator changing device does not trigger Update in Edit mode, so poll the two cached values.
        private void EditorTick()
        {
            if (this == null || Application.isPlaying || !isActiveAndEnabled) return;
            Refresh();
        }
#endif

        private static bool EditMode
        {
            get
            {
#if UNITY_EDITOR
                return !Application.isPlaying;
#else
                return false;
#endif
            }
        }

        private static bool PreviewOn
        {
            get
            {
#if UNITY_EDITOR
                return EditorPreview;
#else
                return false;
#endif
            }
        }

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
        private bool _lastPreview;
        private bool _driven;
        private DrivenRectTransformTracker _tracker;
        private Vector2 _lastScreen;
        private Rect _lastSafe;

        /// <summary>How many times anchors were actually written (tests: unchanged input must not write again).</summary>
        public int WriteCount { get; private set; }

        public SafeAreaMode Mode => mode;

        /// <summary>True while the Edit-mode preview holds the fitted anchors as driven (unsaved, read-only) values.</summary>
        public bool IsDriving => _driven;

        /// <summary>Replaces the source (tests, or a future platform wrapper) and refits at once.</summary>
        public void SetSource(ISafeAreaSource source)
        {
            _source = source;
            Refresh();
        }

        /// <summary>Seam for code that builds the component (scene builder) without going through the serialized fields.</summary>
        public void Configure(SafeAreaMode newMode, bool vertical, bool mirror = true)
        {
            ReleaseDriven();
            mirrorSides = mirror;
            mode = newMode;
            includeVertical = vertical;
            _collected = false;
            _hasApplied = false;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            ActiveFitters.Add(this);
            if (!Application.isPlaying) UnityEditor.EditorApplication.update += EditorTick;
#endif
            Refresh();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            ActiveFitters.Remove(this);
            UnityEditor.EditorApplication.update -= EditorTick;
#endif
            if (_driven)
            {
                ReleaseDriven();
                _hasApplied = false;
            }
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
            bool edit = EditMode;
            bool preview = edit && PreviewOn;
            if (_hasApplied && screen == _lastScreen && safe == _lastSafe && preview == _lastPreview) return false;

            Collect();
            SafeAreaMath.SafeAnchors(screen, safe, includeVertical, tolerancePx, out Vector2 safeMin, out Vector2 safeMax, mirrorSides);
            if (edit)
            {
                // Edit mode: preview off shows the authored layout; no notch drives nothing.
                if (!preview) { safeMin = Vector2.zero; safeMax = Vector2.one; }
                bool inset = safeMin != Vector2.zero || safeMax != Vector2.one;
                if (inset) BeginDrive(); else ReleaseDriven();
            }
            for (int i = 0; i < _entries.Count; i++) Write(_entries[i], safeMin, safeMax);

            _hasApplied = true;
            _lastScreen = screen;
            _lastSafe = safe;
            _lastPreview = preview;
            WriteCount++;
            return true;
        }

        private void Collect()
        {
            if (_collected) return;
            // Authored anchors are only read while nothing is driven (a driven rect holds the inset value).
            ReleaseDriven();
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

        private void BeginDrive()
        {
            if (_driven) return;
            _tracker.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Rect == null) continue;
                _tracker.Add(this, _entries[i].Rect, DrivenTransformProperties.AnchorMin | DrivenTransformProperties.AnchorMax);
            }
            _driven = true;
        }

        /// <summary>Stops driving and puts the authored anchors back (offsets and sizes are preserved).</summary>
        private void ReleaseDriven()
        {
            if (!_driven) return;
            _tracker.Clear();
            _driven = false;
            for (int i = 0; i < _entries.Count; i++) Write(_entries[i], Vector2.zero, Vector2.one);
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
