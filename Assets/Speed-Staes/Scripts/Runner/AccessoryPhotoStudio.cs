using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpeedStars.Runner
{
    /// <summary>Isolated wardrobe preview. Set 1-7 = regular, 8-9 = premium.</summary>
    public sealed class AccessoryPhotoStudio : MonoBehaviour
    {
        public Transform character;
        public Camera photoCamera;
        [Range(0, 9)] public int selectedSet = 1;
        public int imageWidth = 1600;
        public int imageHeight = 2000;
        public bool showControls = true;
        public bool handleInput = true;
        public string capturePrefix;
        [Min(0f)] public float rotationSpeed = 90f;
        public string lastCapture;
        string status;
        bool capturing;
        readonly List<ColorSlot> colorSlots = new List<ColorSlot>();
        static readonly int BaseColor = Shader.PropertyToID("_Color");

        sealed class ColorSlot
        {
            public Renderer renderer;
            public int materialIndex, channel;
            public MaterialPropertyBlock original;
        }

        public static readonly string[] Categories =
            { "Headwear", "Glasses", "Tops", "Gloves", "Shorts", "Leggings", "Socks", "Shoes" };

        public string CaptureDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../Captures/Accessories"));

        void Start() => SelectSet(selectedSet);

        void Update()
        {
            if (!handleInput) return;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame) SelectSet(0);
            for (int i = 0; i < 9; i++)
                if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame ||
                    keyboard[(Key)((int)Key.Numpad1 + i)].wasPressedThisFrame) SelectSet(i + 1);
            RotateCharacter((keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f));
            if (keyboard.spaceKey.wasPressedThisFrame) NextSet();
            if (keyboard.vKey.wasPressedThisFrame) Capture();
            if (keyboard.hKey.wasPressedThisFrame) showControls = !showControls;
#else
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) SelectSet(0);
            for (int i = 0; i < 9; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) SelectSet(i + 1);
            RotateCharacter((Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f));
            if (Input.GetKeyDown(KeyCode.Space)) NextSet();
            if (Input.GetKeyDown(KeyCode.V)) Capture();
            if (Input.GetKeyDown(KeyCode.H)) showControls = !showControls;
#endif
        }

        void RotateCharacter(float direction)
        {
            if (character && direction != 0f)
                character.Rotate(Vector3.up, direction * rotationSpeed * Time.unscaledDeltaTime, Space.World);
        }

        public static int SetFor(string category, string itemName)
        {
            // The source model calls its first premium glove GLP01, unlike PGLP02.
            if (category == "Gloves" && itemName == "GLP01") return 8;
            var match = Regex.Match(itemName, @"\d+");
            if (!match.Success || !int.TryParse(match.Value, out int number)) return 0;
            bool premium = itemName.StartsWith("P", StringComparison.OrdinalIgnoreCase);
            return premium ? (number >= 1 && number <= 2 ? number + 7 : 0)
                : (number >= 1 && number <= 7 ? number : 0);
        }

        public void SelectSet(int number)
        {
            if (!character) return;
            var accessories = character.Find("Accessories");
            if (!accessories) { Debug.LogError("Accessories root missing.", this); return; }
            selectedSet = Mathf.Clamp(number, 0, 9);
            accessories.gameObject.SetActive(true);
            var report = new StringBuilder();
            foreach (string categoryName in Categories)
            {
                var category = accessories.Find(categoryName);
                if (!category) { report.Append(categoryName + ": missing; "); continue; }
                category.gameObject.SetActive(true);
                int count = 0;
                foreach (Transform item in category)
                {
                    // SMLE pieces share the same set as the corresponding main mesh.
                    bool active = selectedSet == 0
                        ? (categoryName == "Shorts" && item.name == "S01") ||
                          (categoryName == "Shoes" && item.name == "SH01")
                        : SetFor(categoryName, item.name) == selectedSet;
                    item.gameObject.SetActive(active);
                    if (active) count++;
                }
                if (count == 0 && selectedSet != 0) report.Append(categoryName + ": missing; ");
            }
            status = selectedSet == 0 ? "BASE / S01 shorts + SH01 shoes"
                : report.Length == 0 ? "All 8 categories matched" : report.ToString();
            if (Application.isPlaying) RandomizeColors();
        }

        void RandomizeColors()
        {
            if (colorSlots.Count == 0)
            {
                foreach (var renderer in character.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var material = materials[i];
                        if (!material || !material.HasProperty(BaseColor)) continue;
                        var match = Regex.Match(material.name, @"^Color([123])(?:_Toon)?(?: \(Instance\))?$");
                        bool skin = material.name == "ColorSkin_Toon" || material.name == "ColorSkin" ||
                            material.name == "ColorSkin_Toon (Instance)";
                        if (!match.Success && !skin) continue;
                        var original = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(original, i);
                        colorSlots.Add(new ColorSlot { renderer = renderer, materialIndex = i,
                            channel = skin ? 3 : int.Parse(match.Groups[1].Value) - 1, original = original });
                    }
                }
            }
            // Each accessory category gets independent channels; multipart items stay matched.
            var palettes = new Dictionary<Transform, Color[]>();
            var accessories = character.Find("Accessories");
            // Keep skin below the bright end: the scene lighting boosts these shader colors.
            // Use one tone for the body and every accessory skin slot.
            Color lightSkin = Color.Lerp(new Color(0.72f, 0.58f, 0.52f),
                new Color(0.72f, 0.54f, 0.52f), UnityEngine.Random.value);
            Color skinColor = Color.Lerp(new Color(0.16f, 0.075f, 0.045f),
                lightSkin, Mathf.Sqrt(UnityEngine.Random.value));
            foreach (var slot in colorSlots)
            {
                if (!slot.renderer) continue;
                var group = slot.renderer.transform;
                while (group.parent && group.parent != accessories && group.parent != character)
                    group = group.parent;
                if (slot.channel != 3 && !palettes.ContainsKey(group))
                {
                    float hue = UnityEngine.Random.value;
                    var palette = new Color[3];
                    for (int i = 0; i < 3; i++)
                        palette[i] = Color.HSVToRGB(Mathf.Repeat(hue + i / 3f + UnityEngine.Random.Range(-0.06f, 0.06f), 1),
                            UnityEngine.Random.Range(0.4f, 0.9f), UnityEngine.Random.Range(0.65f, 1f));
                    palettes.Add(group, palette);
                }
                var block = new MaterialPropertyBlock();
                slot.renderer.GetPropertyBlock(block, slot.materialIndex);
                block.SetColor(BaseColor, slot.channel == 3 ? skinColor : palettes[group][slot.channel]);
                slot.renderer.SetPropertyBlock(block, slot.materialIndex);
            }
        }

        void OnDisable()
        {
            foreach (var slot in colorSlots)
                if (slot.renderer) slot.renderer.SetPropertyBlock(slot.original, slot.materialIndex);
            colorSlots.Clear();
        }

        [ContextMenu("Next Set")]
        public void NextSet() => SelectSet((selectedSet + 1) % 10);
        [ContextMenu("Previous Set")]
        public void PreviousSet() => SelectSet((selectedSet + 9) % 10);

        [ContextMenu("Capture PNG")]
        public void Capture()
        {
            if (!photoCamera || capturing) return;
            capturing = true;
            RenderTexture target = null;
            Texture2D pixels = null;
            var previousTarget = photoCamera.targetTexture;
            var previousActive = RenderTexture.active;
            float previousAspect = photoCamera.aspect;
            Rect previousRect = photoCamera.rect;
            try
            {
                Directory.CreateDirectory(CaptureDirectory);
                int width = Mathf.Clamp(imageWidth, 64, 8192), height = Mathf.Clamp(imageHeight, 64, 8192);
                target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                photoCamera.targetTexture = target;
                photoCamera.rect = new Rect(0, 0, 1, 1);
                photoCamera.aspect = (float)width / height;
                photoCamera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                string tier = selectedSet == 0 ? "Base" : selectedSet <= 7 ? "Regular" : "Premium";
                string path = Path.Combine(CaptureDirectory,
                    $"{(string.IsNullOrEmpty(capturePrefix) ? $"Set_{selectedSet:00}_{tier}" : capturePrefix)}_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid().ToString("N").Substring(0, 6)}.png");
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                lastCapture = path;
                Debug.Log("Accessory photo saved: " + path, this);
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
            finally
            {
                photoCamera.targetTexture = previousTarget;
                photoCamera.aspect = previousAspect;
                photoCamera.rect = previousRect;
                RenderTexture.active = previousActive;
                if (target) RenderTexture.ReleaseTemporary(target);
                if (pixels)
                {
                    if (Application.isPlaying) Destroy(pixels); else DestroyImmediate(pixels);
                }
                capturing = false;
            }
        }

        void OnGUI()
        {
            if (!showControls) return;
            GUILayout.BeginArea(new Rect(16, 16, 370, 170), GUI.skin.box);
            GUILayout.Label($"ACCESSORY STUDIO  /  SET {selectedSet:00}  /  {(selectedSet == 0 ? "BASE" : selectedSet <= 7 ? "REGULAR" : "PREMIUM")}");
            GUILayout.Label("0: base  1-9: set + random colors  V: PNG  H: hide");
            GUILayout.Label("Hold Left / Right: rotate    Space: next set");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<")) PreviousSet();
            if (GUILayout.Button("Capture PNG (V)")) Capture();
            if (GUILayout.Button(">")) NextSet();
            GUILayout.EndHorizontal();
            GUILayout.Label(status);
            GUILayout.Label("1600 x 2000 PNG / Captures/Accessories");
            if (!string.IsNullOrEmpty(lastCapture)) GUILayout.Label(Path.GetFileName(lastCapture));
            GUILayout.EndArea();
        }
    }
}
