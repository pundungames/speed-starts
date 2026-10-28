using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// UI v2 art pipeline (editor time): texture import settings for UI/Art/**, the generated cadence gradient,
    /// sprite lookup and the reader of Art/Home/layout.json (reference-screen rects, converted to canvas anchors).
    /// Positions are never hard-coded: every component rect comes from layout.json.
    /// </summary>
    internal static class UiArt
    {
        public const string ArtRoot = UiPrefabPaths.Root + "/Art";
        public const string HomeArt = ArtRoot + "/Home";
        public const string RaceArt = ArtRoot + "/Race";
        public const string LayoutPath = HomeArt + "/layout.json";
        public const string CadenceGradientPath = RaceArt + "/cadence_gradient.png";

        /// <summary>Canvas reference height of the scene scalers (match = height): reference-screen rects scale by this / reference height.</summary>
        public const float CanvasHeight = 1080f;

        // Home art on the 491x486 character-card canvas whose button backgrounds are hit-tested by alpha (they overlap in rect).
        private static readonly string[] AlphaHitSprites =
        {
            "character_previous_background", "character_next_background", "edit_racer_background", "emote_background"
        };

        public static readonly Color Navy = new Color(0.04f, 0.12f, 0.36f, 1f);

        // ---------------------------------------------------------------- import

        /// <summary>Applies the UI sprite settings to every PNG under UI/Art (only re-imports when a setting differs).</summary>
        public static void ImportAll()
        {
            EnsureCadenceGradient();
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
            TextureImporterCompression compression = home ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            bool same = importer.textureType == TextureImporterType.Sprite
                && settings.spriteMode == (int)SpriteImportMode.Single
                && !importer.mipmapEnabled
                && importer.wrapMode == TextureWrapMode.Clamp
                && importer.alphaIsTransparency
                && importer.isReadable == readable
                && importer.textureCompression == compression
                && settings.spriteMeshType == SpriteMeshType.FullRect;
            if (same) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.isReadable = readable;
            importer.textureCompression = compression;
            importer.maxTextureSize = 4096; // race art is up to 2172 px wide: never silently downscaled to 2048

            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        /// <summary>Writes the green → yellow → red cadence track gradient once (builder-owned PNG, importer settings follow in ImportAll).</summary>
        private static void EnsureCadenceGradient()
        {
            if (File.Exists(Kit.FullPath(CadenceGradientPath))) return;

            const int width = 256;
            const int height = 8;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var green = new Color(0.1f, 0.85f, 0.2f, 1f);
            var yellow = new Color(1f, 0.9f, 0.1f, 1f);
            var orange = new Color(1f, 0.55f, 0.08f, 1f);
            var red = new Color(0.95f, 0.1f, 0.1f, 1f);
            var pixels = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                Color c = t < 0.4f ? Color.Lerp(green, yellow, t / 0.4f)
                    : t < 0.7f ? Color.Lerp(yellow, orange, (t - 0.4f) / 0.3f)
                    : Color.Lerp(orange, red, (t - 0.7f) / 0.3f);
                for (int y = 0; y < height; y++) pixels[y * width + x] = c;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            Kit.EnsureFolder(RaceArt);
            File.WriteAllBytes(Kit.FullPath(CadenceGradientPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(CadenceGradientPath, ImportAssetOptions.ForceSynchronousImport);
        }

        // ---------------------------------------------------------------- sprites and images

        public static Sprite Home(string file) => Load(HomeArt + "/" + file + ".png");

        public static Sprite Race(string file) => Load(RaceArt + "/" + file + ".png");

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
