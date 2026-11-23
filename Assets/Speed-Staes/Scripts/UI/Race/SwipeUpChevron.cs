using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Race
{
    /// <summary>A white swipe chevron with a blue border drawn directly in the UI canvas.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SwipeUpChevron : MaskableGraphic
    {
        private static readonly Vector2[] Outline =
        {
            new Vector2(0f, 27f), new Vector2(46f, -12f), new Vector2(33f, -27f),
            new Vector2(0f, 1f), new Vector2(-33f, -27f), new Vector2(-46f, -12f)
        };

        private static readonly Vector2[] Fill =
        {
            new Vector2(0f, 21f), new Vector2(39f, -12f), new Vector2(33f, -20f),
            new Vector2(0f, 8f), new Vector2(-33f, -20f), new Vector2(-39f, -12f)
        };

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            byte alpha = ((Color32)color).a;
            var blue = new Color32(14, 47, 107, alpha);
            var white = new Color32(255, 255, 255, alpha);
            var transparentBlue = new Color32(blue.r, blue.g, blue.b, 0);
            Rect rect = GetPixelAdjustedRect();
            var scale = new Vector2(rect.width / 92f, rect.height / 54f);
            int count = Outline.Length;
            for (int i = 0; i < count; i++)
            {
                Vector2 outline = Vector2.Scale(Outline[i], scale) + rect.center;
                Vector2 fill = Vector2.Scale(Fill[i], scale) + rect.center;
                Vector2 fringe = Vector2.Scale(Outline[i] * 1.05f, scale) + rect.center;
                mesh.AddVert(fill, white, Vector2.zero);
                mesh.AddVert(fill, blue, Vector2.zero);
                mesh.AddVert(outline, blue, Vector2.zero);
                mesh.AddVert(fringe, transparentBlue, Vector2.zero);
            }

            for (int i = 0; i < count; i++)
            {
                int current = i * 4;
                int next = (i + 1) % count * 4;
                AddQuad(mesh, current + 1, current + 2, next + 2, next + 1);
                AddQuad(mesh, current + 2, current + 3, next + 3, next + 2);
            }

            AddQuad(mesh, 0, 4, 8, 12);
            AddQuad(mesh, 0, 12, 16, 20);
        }

        private static void AddQuad(VertexHelper mesh, int a, int b, int c, int d)
        {
            mesh.AddTriangle(a, b, c);
            mesh.AddTriangle(a, c, d);
        }
    }
}
