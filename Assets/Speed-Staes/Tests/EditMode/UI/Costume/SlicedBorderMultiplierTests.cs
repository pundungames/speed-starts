using System;
using System.Reflection;
using NUnit.Framework;

namespace SpeedStars.Tests.UI.Costume
{
    /// <summary>
    /// UiArt.SlicedBorderMultiplier (premium card border thickness). The builder lives in Assembly-CSharp-Editor, so it is
    /// reached by reflection. Only relationships are asserted.
    /// </summary>
    public sealed class SlicedBorderMultiplierTests
    {
        private static float Multiplier(float spriteHeight, float cellHeight)
        {
            Type art = Type.GetType("SpeedStars.Tools.Builders.UiArt, Assembly-CSharp-Editor", throwOnError: true);
            MethodInfo method = art.GetMethod("SlicedBorderMultiplier", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(method, "UiArt.SlicedBorderMultiplier not found");
            try
            {
                return (float)method.Invoke(null, new object[] { spriteHeight, cellHeight });
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        [Test]
        public void Cell_as_tall_as_the_sprite_keeps_the_border_as_is()
        {
            Assert.AreEqual(1f, Multiplier(807f, 807f), 1e-5f);
        }

        [Test]
        public void Smaller_cell_gives_a_bigger_multiplier_so_the_border_shrinks_with_it()
        {
            Assert.Greater(Multiplier(807f, 200f), Multiplier(807f, 400f));
            Assert.AreEqual(807f / 200f, Multiplier(807f, 200f), 1e-4f);
        }

        [Test]
        public void Non_positive_sizes_throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Multiplier(807f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Multiplier(0f, 100f));
        }
    }
}
