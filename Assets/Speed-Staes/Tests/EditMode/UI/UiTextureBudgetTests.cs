using System;
using System.Reflection;
using NUnit.Framework;

namespace SpeedStars.Tests.UI
{
    /// <summary>
    /// UiArt's Android texture size estimate. The builder lives in Assembly-CSharp-Editor (no asmdef), which this
    /// assembly cannot reference, so the internal functions are reached by reflection. Only relationships are asserted.
    /// </summary>
    public sealed class UiTextureBudgetTests
    {
        private const string UiArtType = "SpeedStars.Tools.Builders.UiArt, Assembly-CSharp-Editor";
        private const string FormatType = "SpeedStars.Tools.Builders.UiArt+UiTextureFormat, Assembly-CSharp-Editor";

        private static long Bytes(int width, int height, string format)
        {
            Type art = Type.GetType(UiArtType, throwOnError: true);
            Type formatType = Type.GetType(FormatType, throwOnError: true);
            MethodInfo method = art.GetMethod("ExpectedTextureBytes", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(method, "UiArt.ExpectedTextureBytes not found");
            return (long)method.Invoke(null, new object[] { width, height, Enum.Parse(formatType, format) });
        }

        [Test]
        public void Astc_is_much_smaller_than_rgba32_for_a_full_canvas_texture()
        {
            Assert.Less(Bytes(1678, 937, "Astc4x4") * 3, Bytes(1678, 937, "Rgba32"));
        }

        [Test]
        public void Bigger_texture_never_costs_less_in_either_format()
        {
            foreach (string format in new[] { "Rgba32", "Astc4x4" })
            {
                Assert.Less(Bytes(256, 256, format), Bytes(512, 256, format), format);
                Assert.Less(Bytes(256, 256, format), Bytes(256, 512, format), format);
            }
        }

        [Test]
        public void Astc_rounds_edges_up_to_whole_blocks()
        {
            // 1 px over a block edge costs a whole extra block column, so it is not the same as the exact multiple.
            Assert.Greater(Bytes(1317, 1197, "Astc4x4"), Bytes(1316, 1196, "Astc4x4"));
            Assert.AreEqual(Bytes(1313, 1193, "Astc4x4"), Bytes(1316, 1196, "Astc4x4"));
        }

        [Test]
        public void Astc_cost_is_one_byte_per_pixel_on_block_multiples()
        {
            Assert.AreEqual(1024L * 512L, Bytes(1024, 512, "Astc4x4"));
        }
    }
}
