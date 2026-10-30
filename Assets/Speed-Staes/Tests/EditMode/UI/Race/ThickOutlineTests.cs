using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.UI.Framework;
using UnityEngine;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>The copy maths of ThickOutline (pure: a vertex stream in, a vertex stream out).</summary>
    public sealed class ThickOutlineTests
    {
        private static readonly Color32 Fill = new Color32(255, 252, 239, 255);
        private static readonly Color32 Line = new Color32(6, 26, 65, 255);

        /// <summary>One glyph quad as the triangle stream a Text writes (6 vertices).</summary>
        private static List<UIVertex> Quads(int count)
        {
            var stream = new List<UIVertex>();
            for (int q = 0; q < count; q++)
            {
                for (int v = 0; v < 6; v++)
                {
                    stream.Add(new UIVertex { position = new Vector3(q * 10f + v, v, 0f), color = Fill });
                }
            }
            return stream;
        }

        [Test]
        public void Expand_AddsOneCopyPerDirection_PlusTheFill()
        {
            List<UIVertex> stream = Quads(5);
            int before = stream.Count;

            int copies = ThickOutline.Expand(stream, 6f, 16, Line, new List<UIVertex>());

            Assert.AreEqual(16, copies);
            Assert.AreEqual(before * (16 + 1), stream.Count);
        }

        [Test]
        public void Expand_FillIsLast_AndUntouched()
        {
            List<UIVertex> original = Quads(2);
            List<UIVertex> stream = Quads(2);

            ThickOutline.Expand(stream, 6f, 12, Line, new List<UIVertex>());

            int tail = stream.Count - original.Count;
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i].position, stream[tail + i].position);
                Assert.AreEqual(Fill, (Color32)stream[tail + i].color);
            }
        }

        [Test]
        public void Expand_OutlineCopiesSitAtTheThicknessAndUseTheOutlineColour()
        {
            List<UIVertex> original = Quads(1);
            List<UIVertex> stream = Quads(1);
            const float thickness = 7f;

            ThickOutline.Expand(stream, thickness, 8, Line, new List<UIVertex>());

            for (int i = 0; i < stream.Count - original.Count; i++)
            {
                UIVertex copy = stream[i];
                Vector3 moved = copy.position - original[i % original.Count].position;
                Assert.AreEqual(thickness, moved.magnitude, 0.001f);
                Assert.AreEqual(Line, (Color32)copy.color);
            }
        }

        [Test]
        public void Expand_ZeroThickness_ChangesNothing()
        {
            List<UIVertex> stream = Quads(3);
            int before = stream.Count;

            Assert.AreEqual(0, ThickOutline.Expand(stream, 0f, 16, Line, new List<UIVertex>()));
            Assert.AreEqual(before, stream.Count);
        }

        [Test]
        public void Expand_EmptyStreamOrNoDirections_ChangesNothing()
        {
            var empty = new List<UIVertex>();
            Assert.AreEqual(0, ThickOutline.Expand(empty, 6f, 16, Line, new List<UIVertex>()));
            Assert.AreEqual(0, empty.Count);

            List<UIVertex> stream = Quads(1);
            Assert.AreEqual(0, ThickOutline.Expand(stream, 6f, 0, Line, new List<UIVertex>()));
            Assert.AreEqual(6, stream.Count);
        }
    }
}
