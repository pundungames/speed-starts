using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// UI v2 art pipeline (editor time): texture import settings for UI/Art/**,
    /// sprite lookup and the reader of Art/Home/layout.json (reference-screen rects, converted to canvas anchors).
    /// Positions are never hard-coded: every component rect comes from layout.json.
    /// </summary>
    internal static class UiArt
    {
        public const string ArtRoot = UiPrefabPaths.Root + "/Art";
        public const string HomeArt = ArtRoot + "/Home";
        public const string RaceArt = ArtRoot + "/Race";
        public const string LayoutPath = HomeArt + "/layout.json";
        public const string RaceHudArt = ArtRoot + "/RaceHud";

        /// <summary>Canvas reference height of the scene scalers (match = height): reference-screen rects scale by this / reference height.</summary>
        public const float CanvasHeight = 1080f;

        // Home art on the 491x486 character-card canvas whose button backgrounds are hit-tested by alpha (they overlap in rect).
        private static readonly string[] AlphaHitSprites =
        {
            "character_previous_background", "character_next_background", "edit_racer_background", "emote_background"
        };

        /// <summary>Largest art side (px) that fits the Android/iOS texture cap; bigger art keeps 4096 so it is never downscaled.</summary>
        private const int MobileMaxSize = 2048;
        private const int MobileMaxSizeLarge = 4096;
        private static readonly string[] MobilePlatforms = { "Android", "iPhone" };

        /// <summary>
        /// 9-slice border (px) of the three premium card layers on their shared 736x807 canvas: covers the frame band (~8..77 px)
        /// and the bevelled corners (~100 px), up to the background's inner bevel (~140 px). All three layers must use the same
        /// value so they stay aligned when the card is stretched to the cell.
        /// </summary>
        public const float PremiumBorderPx = 150f;

        // Files that get a 9-slice border on import (file name without extension -> left, bottom, right, top in px).
        private static readonly (string stem, Vector4 border)[] SlicedBorders =
        {
            ("premium_background", new Vector4(PremiumBorderPx, PremiumBorderPx, PremiumBorderPx, PremiumBorderPx)),
            ("premium_frame", new Vector4(PremiumBorderPx, PremiumBorderPx, PremiumBorderPx, PremiumBorderPx)),
            ("premium_selected_frame", new Vector4(PremiumBorderPx, PremiumBorderPx, PremiumBorderPx, PremiumBorderPx)),
        };

        public static readonly Color Navy = new Color(0.04f, 0.12f, 0.36f, 1f);

        // ---------------------------------------------------------------- import

        /// <summary>Applies the UI sprite settings to every PNG under UI/Art (only re-imports when a setting differs).</summary>
        public static void ImportAll()
        {
            string full = Kit.FullPath(ArtRoot);
            if (!Directory.Exists(full)) throw new InvalidOperationException($"UiArt: art folder '{ArtRoot}' not found.");

            foreach (string file in Directory.GetFiles(full, "*.png", SearchOption.AllDirectories))
            {
                string rel = file.Replace('\\', '/');
                int at = rel.IndexOf("Assets/", StringComparison.Ordinal);
                if (at < 0) continue;
                ConfigureTexture(rel.Substring(at));
            }
        }

        private static void ConfigureTexture(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport); // not known yet: creates the .meta
                importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            }
            if (importer == null) throw new InvalidOperationException($"UiArt: '{assetPath}' has no TextureImporter.");

            bool home = assetPath.StartsWith(HomeArt, StringComparison.Ordinal);
            string spriteName = Path.GetFileNameWithoutExtension(assetPath);
            bool readable = Array.IndexOf(AlphaHitSprites, spriteName) >= 0;
            int mobileMax = MobileMaxFor(assetPath);
            TextureImporterCompression compression = home ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;

            bool sliced = TryGetBorder(spriteName, out Vector4 border);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            bool same = importer.textureType == TextureImporterType.Sprite
                && settings.spriteMode == (int)SpriteImportMode.Single
                && !importer.mipmapEnabled
                && importer.wrapMode == TextureWrapMode.Clamp
                && importer.alphaIsTransparency
                && importer.isReadable == readable
                && importer.textureCompression == compression
                && settings.spriteMeshType == SpriteMeshType.FullRect
                && MobileMatches(importer, mobileMax)
                && (!sliced || importer.spriteBorder == border); // art without a table entry keeps whatever border it has
            if (same) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.isReadable = readable;
            importer.textureCompression = compression;
            importer.maxTextureSize = 4096; // default platform: race art is up to 2172 px wide, never silently downscaled

            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            if (sliced) importer.spriteBorder = border;
            ApplyMobile(importer, mobileMax);
            importer.SaveAndReimport();
        }

        private static bool TryGetBorder(string stem, out Vector4 border)
        {
            foreach ((string name, Vector4 value) in SlicedBorders)
            {
                if (name != stem) continue;
                border = value;
                return true;
            }
            border = Vector4.zero;
            return false;
        }

        /// <summary>
        /// Image.pixelsPerUnitMultiplier that keeps a sliced border at the thickness the whole sprite would have when its
        /// height is fitted to <paramref name="cellHeight"/> (border units shrink by this factor): spriteHeight / cellHeight.
        /// </summary>
        internal static float SlicedBorderMultiplier(float spriteHeight, float cellHeight)
        {
            if (spriteHeight <= 0f || cellHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(cellHeight), "UiArt: sizes must be positive.");
            return spriteHeight / cellHeight;
        }

        /// <summary>
        /// Android/iOS get explicit ASTC 4x4: the default "Compressed" falls back to uncompressed RGBA32 on mobile for
        /// non-multiple-of-4 sizes (UI art is mostly such canvases), which cost 238 MB of an APK. ASTC has no size rule and
        /// keeps alpha edges sharp (4x4 is its highest quality). The default (editor) platform stays as it was.
        /// </summary>
        private static void ApplyMobile(TextureImporter importer, int maxSize)
        {
            foreach (string platform in MobilePlatforms)
            {
                var p = new TextureImporterPlatformSettings
                {
                    name = platform,
                    overridden = true,
                    maxTextureSize = maxSize,
                    format = TextureImporterFormat.ASTC_4x4,
                    textureCompression = TextureImporterCompression.Compressed,
                    compressionQuality = 50
                };
                importer.SetPlatformTextureSettings(p);
            }
        }

        private static bool MobileMatches(TextureImporter importer, int maxSize)
        {
            foreach (string platform in MobilePlatforms)
            {
                TextureImporterPlatformSettings p = importer.GetPlatformTextureSettings(platform);
                if (!p.overridden || p.format != TextureImporterFormat.ASTC_4x4 || p.maxTextureSize != maxSize) return false;
            }
            return true;
        }

        private static int MobileMaxFor(string assetPath)
        {
            ReadPngSize(assetPath, out int width, out int height);
            return Mathf.Max(width, height) > MobileMaxSize ? MobileMaxSizeLarge : MobileMaxSize;
        }

        /// <summary>Width and height from the PNG header (IHDR, big-endian at byte 16): no decode, no importer needed.</summary>
        internal static void ReadPngSize(string assetPath, out int width, out int height)
        {
            byte[] header = new byte[24];
            using (FileStream stream = File.OpenRead(Kit.FullPath(assetPath)))
            {
                if (stream.Read(header, 0, 24) < 24) throw new InvalidOperationException($"UiArt: '{assetPath}' is not a PNG.");
            }
            width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        }

        // ---------------------------------------------------------------- texture budget

        /// <summary>Pixel formats the Android size estimate knows.</summary>
        internal enum UiTextureFormat
        {
            Rgba32,
            Astc4x4
        }

        /// <summary>
        /// Expected GPU/APK size of one texture without mipmaps: RGBA32 = 4 bytes per pixel, ASTC 4x4 = one 16-byte block
        /// per 4x4 pixels (edges round up to whole blocks). Lets us compare art before building an APK.
        /// </summary>
        internal static long ExpectedTextureBytes(int width, int height, UiTextureFormat format)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "UiArt: texture size must be positive.");
            switch (format)
            {
                case UiTextureFormat.Rgba32:
                    return (long)width * height * 4L;
                case UiTextureFormat.Astc4x4:
                    return ((width + 3) / 4) * (long)((height + 3) / 4) * 16L;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        // ---------------------------------------------------------------- dim

        /// <summary>
        /// A full-screen dim as a plain tinted Image (no sprite, no texture): the dim PNGs are one flat RGBA each, so the
        /// colour is read from the art file (single source) and the 6 MB texture is not referenced at all.
        /// </summary>
        public static Image PaintDim(GameObject go, string artPath)
        {
            return Kit.Fill(go, ReadFlatColor(artPath), raycast: true);
        }

        /// <summary>The one colour of a flat PNG (corners and center sampled); throws if the art is not flat.</summary>
        internal static Color32 ReadFlatColor(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Kit.FullPath(assetPath))))
                    throw new InvalidOperationException($"UiArt: '{assetPath}' could not be read.");

                int w = texture.width, h = texture.height;
                Color32[] px = texture.GetPixels32();
                Color32 first = px[0];
                Vector2Int[] samples = { new Vector2Int(w - 1, 0), new Vector2Int(0, h - 1), new Vector2Int(w - 1, h - 1), new Vector2Int(w / 2, h / 2) };
                foreach (Vector2Int at in samples)
                {
                    Color32 c = px[at.y * w + at.x];
                    if (c.r != first.r || c.g != first.g || c.b != first.b || c.a != first.a)
                        throw new InvalidOperationException($"UiArt: dim art '{assetPath}' is not a flat colour; PaintDim cannot replace it.");
                }
                return first;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        // ---------------------------------------------------------------- sprites and images

        public static Sprite Home(string file) => Load(HomeArt + "/" + file + ".png");

        public static Sprite Race(string file) => Load(RaceArt + "/" + file + ".png");

        /// <summary>Race HUD part (UI/Art/RaceHud: cropped, native size = canvas units).</summary>
        public static Sprite RaceHud(string file) => Load(RaceHudArt + "/" + file + ".png");

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"UiArt: sprite '{path}' not found or not imported as a Sprite.");
            return sprite;
        }

        /// <summary>Puts a sprite on an existing object (no aspect forcing: the rect is the art canvas).</summary>
        public static Image Paint(GameObject go, Sprite sprite, bool raycast)
        {
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>
        /// Bounds of the visible (alpha &gt; 0) pixels of an art PNG as fractions of its canvas: x, y (from bottom), x2, y2.
        /// Shared by every builder whose buttons sit on a canvas shared with other art (hit rect = these bounds).
        /// </summary>
        public static Vector4 AlphaBounds(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Kit.FullPath(assetPath))))
                    throw new InvalidOperationException($"UiArt: '{assetPath}' could not be read.");

                int width = texture.width, height = texture.height;
                Color32[] pixels = texture.GetPixels32();
                int minX = width, minY = height, maxX = -1, maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[y * width + x].a == 0) continue;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
                if (maxX < 0) throw new InvalidOperationException($"UiArt: '{assetPath}' is fully transparent.");
                return new Vector4(minX / (float)width, minY / (float)height, (maxX + 1) / (float)width, (maxY + 1) / (float)height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// A layer drawn on the whole art canvas while its parent covers only <paramref name="bounds"/> (fractions of that
        /// canvas): anchors extend past the parent by the same fractions, offsets stay zero. Never catches taps.
        /// </summary>
        public static Image CanvasLayer(RectTransform parent, string name, Sprite sprite, Vector4 bounds)
        {
            float w = bounds.z - bounds.x;
            float h = bounds.w - bounds.y;
            RectTransform rt = Kit.Node(parent, name);
            Kit.Span(rt, new Vector2(-bounds.x / w, -bounds.y / h), new Vector2((1f - bounds.x) / w, (1f - bounds.y) / h));
            return Paint(rt.gameObject, sprite, raycast: false);
        }

        /// <summary>
        /// A stretched layer drawn 9-sliced (border set on the sprite's import): the rect can be any shape, the frame band and
        /// corners keep their look. <paramref name="borderMultiplier"/> scales the border thickness (see SlicedBorderMultiplier).
        /// </summary>
        public static Image SlicedLayer(Transform parent, string name, Sprite sprite, float borderMultiplier, bool fillCenter)
        {
            Image image = Layer(parent, name, sprite);
            image.type = Image.Type.Sliced;
            image.fillCenter = fillCenter;
            image.pixelsPerUnitMultiplier = borderMultiplier;
            return image;
        }

        /// <summary>A child stretched over its parent that shows one art layer. Never catches taps.</summary>
        public static Image Layer(Transform parent, string name, Sprite sprite)
        {
            RectTransform rt = Kit.Stretch(Kit.Node(parent, name));
            return Paint(rt.gameObject, sprite, raycast: false);
        }

        /// <summary>Button on an object that already carries its background Image: opaque tint when disabled (no see-through ghost).</summary>
        public static Button MakeButton(GameObject go, Graphic background, bool interactable, bool alphaHit)
        {
            Button button = Kit.Clickable(go, background);
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            button.colors = colors;
            button.interactable = interactable;
            if (alphaHit && background is Image image) image.alphaHitTestMinimumThreshold = 0.5f;
            return button;
        }

        /// <summary>Navy Text over an art slot (rect as fractions of its parent). Never catches taps.</summary>
        public static Text Slot(Transform parent, string name, string text, float size, Vector2 min, Vector2 max,
            TextAnchor alignment = TextAnchor.MiddleCenter, Color? color = null)
        {
            Text label = Kit.Label(parent, name, text, size, color ?? Navy, alignment);
            Kit.Span(label.rectTransform, min, max);
            return label;
        }

        // ---------------------------------------------------------------- layout.json

        [Serializable]
        internal sealed class LayoutFile
        {
            public int[] reference_size;
            public LayoutComponent[] components;
        }

        [Serializable]
        public sealed class LayoutComponent
        {
            public string name;
            public int[] rect; // x, y, w, h in reference pixels, origin top-left
            public string[] layers;
            public float[] pivot;
        }

        internal sealed class Layout
        {
            private readonly LayoutFile _file;

            internal Layout(LayoutFile file)
            {
                _file = file;
            }

            public float RefWidth => _file.reference_size[0];
            public float RefHeight => _file.reference_size[1];
            public float Scale => CanvasHeight / RefHeight;

            public LayoutComponent Get(string name)
            {
                foreach (LayoutComponent component in _file.components)
                    if (component.name == name) return component;
                throw new InvalidOperationException($"UiArt: layout.json has no component '{name}'.");
            }

            /// <summary>
            /// Anchors a rect by the screen edge it sits near: left third → left edge, right third → right edge, middle →
            /// center; bottom quarter → bottom edge, rest → top edge. Width varies per phone (Expand scaler), so edge items
            /// keep their distance to their own edge. Size and offsets are the reference pixels × Scale.
            /// </summary>
            public RectTransform PlaceOnScreen(RectTransform rt, LayoutComponent c)
            {
                float x = c.rect[0], y = c.rect[1], w = c.rect[2], h = c.rect[3];
                float s = Scale;
                float centerX = x + w * 0.5f;
                float centerY = y + h * 0.5f;

                float ax, px;
                if (centerX < RefWidth * 0.4f) { ax = 0f; px = x * s; }
                else if (centerX > RefWidth * 0.6f) { ax = 1f; px = -(RefWidth - (x + w)) * s; }
                else { ax = 0.5f; px = (centerX - RefWidth * 0.5f) * s; }

                float ay, py;
                if (centerY > RefHeight * 0.75f) { ay = 0f; py = (RefHeight - (y + h)) * s; }
                else { ay = 1f; py = -y * s; }

                var anchor = new Vector2(ax, ay);
                return Kit.Anchor(rt, anchor, anchor, new Vector2(px, py), new Vector2(w * s, h * s));
            }

            /// <summary>Top-left anchored rect relative to an origin (a container that was itself placed on screen).</summary>
            public RectTransform PlaceInside(RectTransform rt, LayoutComponent c, float originX, float originY)
            {
                float s = Scale;
                var corner = new Vector2(0f, 1f);
                return Kit.Anchor(rt, corner, corner, new Vector2((c.rect[0] - originX) * s, -(c.rect[1] - originY) * s),
                    new Vector2(c.rect[2] * s, c.rect[3] * s));
            }

            /// <summary>The rect (reference px) that contains all given components.</summary>
            public Vector4 Bounds(params string[] names)
            {
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (string name in names)
                {
                    LayoutComponent c = Get(name);
                    minX = Mathf.Min(minX, c.rect[0]);
                    minY = Mathf.Min(minY, c.rect[1]);
                    maxX = Mathf.Max(maxX, c.rect[0] + c.rect[2]);
                    maxY = Mathf.Max(maxY, c.rect[1] + c.rect[3]);
                }
                return new Vector4(minX, minY, maxX, maxY);
            }
        }

        internal static Layout ReadLayout()
        {
            string path = Kit.FullPath(LayoutPath);
            if (!File.Exists(path)) throw new InvalidOperationException($"UiArt: '{LayoutPath}' not found.");
            var file = JsonUtility.FromJson<LayoutFile>(File.ReadAllText(path));
            if (file == null || file.reference_size == null || file.reference_size.Length < 2 || file.components == null)
                throw new InvalidOperationException($"UiArt: '{LayoutPath}' could not be read.");
            return new Layout(file);
        }
    }
}
