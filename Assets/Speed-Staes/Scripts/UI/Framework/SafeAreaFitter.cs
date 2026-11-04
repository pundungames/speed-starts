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
    /// applied in Edit mode as DRIVEN values (DrivenRectTransformTracker). Unity does NOT skip driven anchors when it
    /// serializes: it writes (0,0) for them, to scenes and prefabs alike, so the inset must never reach a save. Therefore the
    /// authored anchors are put back before every editor save and before entering Play mode (SuspendPreviewForSave, wired in
    /// SafeAreaPreviewMenu.cs), when the fitter is disabled, and when the preview is switched off
    /// (menu Tools/Speed Stars/Notch Preview (Edit Mode)). While the preview is inset, those anchors are read-only
    /// (greyed) in the Inspector; switch the preview off to edit them. While nothing is driven the anchors are re-read before
    /// every refit, so hand edits are never lost. No preview in prefab mode or preview scenes. Play mode ignores the preview flag entirely.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField, Tooltip("Children: fit the direct children of this stretched root. Self: fit this rect itself. In Edit mode the fitted anchors are driven (greyed, read-only) while a notch preview is active (the authored values are restored before every save; no preview in prefab mode); turn it off with Tools > Speed Stars > Notch Preview (Edit Mode) to edit them.")]
        private SafeAreaMode mode = SafeAreaMode.Children;

        [SerializeField, Tooltip("Also keep the top/bottom edges inside the safe area (the game is landscape, so normally off).")]
        private bool includeVertical;

        [SerializeField, Tooltip("Move both left and right edges in by the larger of the two insets, so a notch on one side gives a balanced layout (and flipping the phone does not shift it).")]
        private bool mirrorSides = true;

        [SerializeField, Min(0f), Tooltip("Insets this small (px) count as none, so a notch-less phone keeps the authored layout.")]
        private float tolerancePx = 1f;

#if UNITY_EDITOR
        /// <summary>EditorUserSettings (per project) key of the Edit-mode preview switch (default on).</summary>
        public const string PreviewPrefKey = "SpeedStars.SafeAreaEditPreview";

        private static readonly List<SafeAreaFitter> ActiveFitters = new List<SafeAreaFitter>();
        private static int _previewCache = -1;
        private static bool _hold;

        /// <summary>Edit-mode notch preview switch (persisted per project in EditorUserSettings, default on). Never read in Play mode.</summary>
        public static bool EditorPreview
        {
            get
            {
                if (_previewCache < 0) _previewCache = UnityEditor.EditorUserSettings.GetConfigValue(PreviewPrefKey) == "0" ? 0 : 1;
                return _previewCache == 1;
            }
        }

        /// <summary>Switches the preview and refits every active fitter at once (off restores the authored anchors).</summary>
        public static void SetEditorPreview(bool on)
        {
            _previewCache = on ? 1 : 0;
            UnityEditor.EditorUserSettings.SetConfigValue(PreviewPrefKey, on ? "1" : "0");
            RefreshActive();
        }

        /// <summary>Before an editor save or Play-mode entry (both serialize the scene): drop driven anchors, because Unity writes (0,0) for them.</summary>
        public static void SuspendPreviewForSave()
        {
            if (Application.isPlaying) return;
            _hold = true;
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
            _hold = false;
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

        private bool InPreviewScene()
        {
            return UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene);
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

        // Keeps preview-driven changes out of the Undo list. Start/StopRecordingUndo are not public in every Unity version, hence reflection.
        private static System.Reflection.MethodInfo _startUndo;
        private static System.Reflection.MethodInfo _stopUndo;

        private static void RecordingUndo(bool on)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            if (_startUndo == null) _startUndo = typeof(DrivenRectTransformTracker).GetMethod("StartRecordingUndo", flags);
            if (_stopUndo == null) _stopUndo = typeof(DrivenRectTransformTracker).GetMethod("StopRecordingUndo", flags);
            System.Reflection.MethodInfo method = on ? _startUndo : _stopUndo;
            if (method != null) method.Invoke(null, null);
        }

        private bool PreviewOn
        {
            get
            {
#if UNITY_EDITOR
                return EditorPreview && !_hold && !InPreviewScene();
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

        // Runtime state only: never serialized, so a domain reload starts clean (OnDisable/OnEnable rebuild it).
        [System.NonSerialized] private readonly List<Entry> _entries = new List<Entry>();
        [System.NonSerialized] private ISafeAreaSource _source;
        [System.NonSerialized] private bool _collected;
        [System.NonSerialized] private bool _hasApplied;
        [System.NonSerialized] private bool _lastPreview;
        [System.NonSerialized] private bool _driven;
        [System.NonSerialized] private DrivenRectTransformTracker _tracker;
        [System.NonSerialized] private Vector2 _lastScreen;
        [System.NonSerialized] private Rect _lastSafe;
        private Canvas _canvas;
        private Vector2 _candidateScreen;
        private Rect _candidateSafe;
        private int _candidateFrame = -1;

        /// <summary>How many times anchors were actually written (tests: unchanged input must not write again).</summary>
        public int WriteCount { get; private set; }

        public SafeAreaMode Mode => mode;

        /// <summary>True while the Edit-mode preview holds the fitted anchors as driven (unsaved, read-only) values.</summary>
        public bool IsDriving => _driven && HasLiveEntries();

        private bool HasLiveEntries()
        {
            for (int i = 0; i < _entries.Count; i++) if (_entries[i].Rect != null) return true;
            return false;
        }

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
        private void LateUpdate()
        {
            Refresh();
        }

        /// <summary>Fits when the safe area or screen size differs from the last fit. Returns true when anchors were written.</summary>
        public bool Refresh()
        {
            ISafeAreaSource source = _source ?? (_source = new ScreenSafeAreaSource());
            Vector2 screen = source.ScreenSize;
            Rect safe = source.SafeArea;
            // A resize/simulator switch can publish the new Screen size before the
            // safe rectangle and Canvas have caught up. Keep the last good layout.
            if (!SafeAreaMath.IsValidSample(screen, safe, tolerancePx)) return false;
            if (mirrorSides && 2f * Mathf.Max(safe.xMin, screen.x - safe.xMax) >= screen.x) return false;
            if (source is ScreenSafeAreaSource && Application.isPlaying)
            {
                if (!_canvas) _canvas = GetComponentInParent<Canvas>();
                if (_canvas)
                {
                    Vector2 rendered = _canvas.rootCanvas.renderingDisplaySize;
                    if (Mathf.Abs(rendered.x - screen.x) > tolerancePx
                        || Mathf.Abs(rendered.y - screen.y) > tolerancePx) return false;
                }
                // Require a full frame of the same sample, not multiple calls in one frame.
                if (_candidateFrame < 0 || screen != _candidateScreen || safe != _candidateSafe)
                {
                    _candidateScreen = screen;
                    _candidateSafe = safe;
                    _candidateFrame = Time.frameCount;
                    return false;
                }
                if (Time.frameCount == _candidateFrame) return false;
            }
            bool edit = EditMode;
            bool preview = edit && PreviewOn;
            // A "driven" flag with nothing left to drive is stale (reload, destroyed rects): forget it and read the anchors again.
            if (_driven && !HasLiveEntries()) { _tracker.Clear(); _driven = false; _collected = false; _hasApplied = false; }
            if (_hasApplied && screen == _lastScreen && safe == _lastSafe && preview == _lastPreview) return false;

            // Edit mode, nothing driven: the anchors are the authored ones and may have been hand-edited since the last fit.
            if (edit && !_driven) _collected = false;
            Collect();
            SafeAreaMath.SafeAnchors(screen, safe, includeVertical, tolerancePx, out Vector2 safeMin, out Vector2 safeMax, mirrorSides);
            if (edit)
            {
                // Edit mode: preview off shows the authored layout; no notch drives nothing.
                if (!preview) { safeMin = Vector2.zero; safeMax = Vector2.one; }
                bool inset = safeMin != Vector2.zero || safeMax != Vector2.one;
                if (inset) BeginDrive(); else ReleaseDriven();
            }
            if (edit) RecordingUndo(false);
            try { for (int i = 0; i < _entries.Count; i++) Write(_entries[i], safeMin, safeMax); }
            finally { if (edit) RecordingUndo(true); }

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
            RecordingUndo(false);
            try
            {
                _tracker.Clear();
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].Rect == null) continue;
                    _tracker.Add(this, _entries[i].Rect, DrivenTransformProperties.AnchorMin | DrivenTransformProperties.AnchorMax);
                }
                _driven = true;
            }
            finally { RecordingUndo(true); }
        }

        /// <summary>Stops driving and puts the authored anchors back (offsets and sizes are preserved).</summary>
        private void ReleaseDriven()
        {
            if (!_driven) return;
            // Authored values go back while the anchors are still driven (the write dirties nothing), then the tracker lets go.
            RecordingUndo(false);
            try
            {
                for (int i = 0; i < _entries.Count; i++) Write(_entries[i], Vector2.zero, Vector2.one);
                _tracker.Clear();
                _driven = false;
            }
            finally { RecordingUndo(true); }
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
