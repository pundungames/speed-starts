using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// A solid outline for legacy Text / Image that stays smooth at any width. The built-in Outline copies the graphic four times,
    /// which shows corner blocks once the distance is more than a few pixels; this one copies it on a full circle (default 16
    /// directions) and draws every outline copy first, the original fill last, so neighbouring glyphs never paint their outline
    /// over each other's fill. The copy maths is a pure static (<see cref="Expand"/>) so it is tested without a scene.
    /// </summary>
    [AddComponentMenu("Speed Stars/UI/Thick Outline")]
    public sealed class ThickOutline : BaseMeshEffect
    {
        [Tooltip("How far the outline reaches around the fill (px in the graphic's own space, before canvas scaling). 0 = no outline.")]
        [Min(0f)]
        [SerializeField] private float thickness = 8f;

        [Tooltip("How many copies go around the circle. 12-16 is smooth for thick outlines; fewer shows corners.")]
        [Range(4, 32)]
        [SerializeField] private int directions = 16;

        [Tooltip("Outline colour (alpha included: the view fades the outline with the word through it).")]
        [SerializeField] private Color effectColor = new Color(0.024f, 0.102f, 0.255f, 1f);

        private readonly List<UIVertex> _stream = new List<UIVertex>(256);
        private readonly List<UIVertex> _scratch = new List<UIVertex>(256);

        public float Thickness => thickness;
        public int Directions => directions;
        public Color EffectColor => effectColor;

        /// <summary>Sets thickness, colour and (when given) direction count together, one rebuild; the view calls it from its config.</summary>
        public void Configure(float newThickness, Color newColor, int newDirections = 0)
        {
            thickness = Mathf.Max(0f, newThickness);
            effectColor = newColor;
            if (newDirections > 0) directions = newDirections;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public void SetColor(Color newColor)
        {
            if (effectColor == newColor) return;
            effectColor = newColor;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            _stream.Clear();
            vh.GetUIVertexStream(_stream);
            if (Expand(_stream, thickness, directions, effectColor, _scratch) == 0) return;

            vh.Clear();
            vh.AddUIVertexTriangleStream(_stream);
        }

        /// <summary>
        /// Rewrites a triangle vertex stream in place: <paramref name="directions"/> copies of it pushed out by
        /// <paramref name="thickness"/> on a circle and painted <paramref name="color"/>, then the untouched original on top.
        /// Returns the number of outline copies added (0 = stream left as it was: no thickness, no directions or an empty stream).
        /// </summary>
        public static int Expand(List<UIVertex> stream, float thickness, int directions, Color32 color, List<UIVertex> scratch)
        {
            if (stream == null || scratch == null || stream.Count == 0 || thickness <= 0f || directions < 1) return 0;

            scratch.Clear();
            scratch.AddRange(stream);
            stream.Clear();

            for (int d = 0; d < directions; d++)
            {
                float angle = 2f * Mathf.PI * d / directions;
                var offset = new Vector3(Mathf.Cos(angle) * thickness, Mathf.Sin(angle) * thickness, 0f);
                for (int i = 0; i < scratch.Count; i++)
                {
                    UIVertex vertex = scratch[i];
                    vertex.position += offset;
                    vertex.color = color;
                    stream.Add(vertex);
                }
            }
            stream.AddRange(scratch); // the fill goes last = on top
            return directions;
        }
    }
}
