using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.Meta.Practice
{
    /// <summary>
    /// The numbers of the Practice panel (T-090): the start distances of its dropdown and the hurdle layout of each. The layouts
    /// follow the track-and-field hurdle races (110 m hurdles, 400 m hurdles ...) so a hurdle run feels like the real event.
    /// The code defaults below are only the first creation values; the asset is the source.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Practice", fileName = "PracticeConfig")]
    public sealed class PracticeConfig : ScriptableObject
    {
        /// <summary>Hurdles of one start distance.</summary>
        [Serializable]
        public sealed class HurdleLayout
        {
            [Tooltip("The start distance (m) this layout is for.")]
            [SerializeField, Min(0f)] private float distanceMeters = 100f;

            [Tooltip("Number of hurdles.")]
            [SerializeField, Min(0)] private int count = 10;

            [Tooltip("Distance of the first hurdle from the start (m).")]
            [SerializeField, Min(0f)] private float firstMeters = 13.72f;

            [Tooltip("Distance between hurdles (m).")]
            [SerializeField, Min(0f)] private float spacingMeters = 9.14f;

            public HurdleLayout() { }

            public HurdleLayout(float distanceMeters, int count, float firstMeters, float spacingMeters)
            {
                this.distanceMeters = distanceMeters;
                this.count = count;
                this.firstMeters = firstMeters;
                this.spacingMeters = spacingMeters;
            }

            public float DistanceMeters => distanceMeters;
            public int Count => count;
            public float FirstMeters => firstMeters;
            public float SpacingMeters => spacingMeters;

            /// <summary>Distance of the last hurdle from the start (m); 0 for an empty layout.</summary>
            public float LastMeters => count > 0 ? firstMeters + spacingMeters * (count - 1) : 0f;
        }

        [Tooltip("Start distances (m) of the Practice dropdown, in display order. The first is the default.")]
        [SerializeField] private float[] distances = { 100f, 200f, 300f, 400f };

        [Tooltip("Hurdle layout per start distance. A distance without an entry takes the nearest one.")]
        [SerializeField] private HurdleLayout[] hurdleLayouts =
        {
            new HurdleLayout(100f, 10, 13.72f, 9.14f),  // 110 m hurdles spacing
            new HurdleLayout(200f, 10, 18.29f, 18.29f), // 200 m hurdles
            new HurdleLayout(300f, 8, 50f, 35f),        // 300 m hurdles
            new HurdleLayout(400f, 10, 45f, 35f)        // 400 m hurdles
        };

        /// <summary>The configured start distances; never empty (a wrongly emptied asset reads as the default list).</summary>
        public IReadOnlyList<float> Distances => distances != null && distances.Length > 0 ? distances : DefaultDistances;

        public float DefaultDistance => Distances[0];

        /// <summary>
        /// The layout of the configured distance nearest to <paramref name="distanceMeters"/>; false when the asset lists none.
        /// </summary>
        public bool TryGetHurdles(float distanceMeters, out HurdleLayout layout)
        {
            layout = null;
            if (hurdleLayouts == null) return false;
            float best = float.MaxValue;
            foreach (HurdleLayout candidate in hurdleLayouts)
            {
                if (candidate == null) continue;
                float gap = Mathf.Abs(candidate.DistanceMeters - distanceMeters);
                if (gap >= best) continue;
                best = gap;
                layout = candidate;
            }
            return layout != null;
        }

        /// <summary>A runtime instance with the code defaults, for a scene whose asset is not wired yet (and for tests).</summary>
        public static PracticeConfig CreateDefault() => CreateInstance<PracticeConfig>();

        private static readonly float[] DefaultDistances = { 100f, 200f, 300f, 400f };
    }
}
