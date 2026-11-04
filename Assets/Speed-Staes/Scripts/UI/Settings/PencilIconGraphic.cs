using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Settings
{
    /// <summary>A small UI-native pencil, so its edges remain crisp at every canvas scale.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PencilIconGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Polygon(vh, r, new Vector2(.12f,.12f), new Vector2(.2f,.43f), new Vector2(.72f,.95f), new Vector2(.95f,.72f), new Vector2(.43f,.2f));
        }

        void Polygon(VertexHelper vh, Rect r, params Vector2[] points)
        {
            for (int i = 0; i < points.Length; i++)
                vh.AddVert(new Vector3(r.xMin + points[i].x*r.width, r.yMin + points[i].y*r.height), color, Vector2.zero);
            for (int i = 1; i < points.Length-1; i++) vh.AddTriangle(0, i, i+1);
        }
    }
}
