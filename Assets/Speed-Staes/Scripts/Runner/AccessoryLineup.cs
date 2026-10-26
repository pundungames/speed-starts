using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpeedStars.Runner
{
    public sealed class AccessoryLineup : MonoBehaviour
    {
        public AccessoryPhotoStudio[] characters;
        public Camera photoCamera;
        public float rotationSpeed = 60f;
        public bool showControls = true;
        const float Aspect = 3.2f;

        System.Collections.IEnumerator Start()
        {
            // Environment generators finish their startup before checking the wide camera's foreground.
            yield return null;
            ClearForeground();
        }

        public void ClearForeground()
        {
            if (characters == null || characters.Length == 0 || !photoCamera) return;
            Vector3 center = Vector3.zero;
            foreach (var member in characters) center += member.character.position / characters.Length;
            foreach (var renderer in FindObjectsOfType<MeshRenderer>())
            {
                var bounds = renderer.bounds;
                if (bounds.center.x > center.x + 0.4f && bounds.center.x < photoCamera.transform.position.x &&
                    bounds.max.z > center.z - 6 && bounds.min.z < center.z + 6 &&
                    bounds.min.y < 1.1f && bounds.max.y > 0.25f && bounds.max.y < 2)
                    renderer.enabled = false;
            }
        }

        void Update()
        {
            float rotation = 0;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            rotation = (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
            if (keyboard.vKey.wasPressedThisFrame) Capture();
            if (keyboard.rKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) RandomizeAll();
            if (keyboard.hKey.wasPressedThisFrame) showControls = !showControls;
#else
            rotation = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            if (Input.GetKeyDown(KeyCode.V)) Capture();
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space)) RandomizeAll();
            if (Input.GetKeyDown(KeyCode.H)) showControls = !showControls;
#endif
            foreach (var member in characters)
                if (member && member.character && rotation != 0)
                    member.character.Rotate(Vector3.up, rotation * rotationSpeed * Time.unscaledDeltaTime, Space.World);
        }

        void LateUpdate()
        {
            if (!photoCamera || Screen.height == 0) return;
            float screenAspect = (float)Screen.width / Screen.height;
            if (screenAspect < Aspect)
            {
                float height = screenAspect / Aspect;
                photoCamera.rect = new Rect(0, (1 - height) / 2, 1, height);
            }
            else
            {
                float width = Aspect / screenAspect;
                photoCamera.rect = new Rect((1 - width) / 2, 0, width, 1);
            }
            photoCamera.aspect = Aspect;
        }

        [ContextMenu("Randomize All Colors")]
        public void RandomizeAll()
        {
            if (characters == null) return;
            for (int i = 0; i < characters.Length; i++)
                if (characters[i]) characters[i].SelectSet(i + 1);
        }

        [ContextMenu("Capture Lineup PNG")]
        public void Capture()
        {
            if (characters != null && characters.Length > 0 && characters[0]) characters[0].Capture();
        }

        void OnGUI()
        {
            if (!showControls) return;
            GUILayout.BeginArea(new Rect(16, 16, 440, 100), GUI.skin.box);
            GUILayout.Label("SETS 1-9 / LEFT TO RIGHT / 5760 x 1800 PNG");
            GUILayout.Label("V: capture   R / Space: new colors   Left / Right: rotate   H: hide");
            if (GUILayout.Button("Capture all 9 (V)")) Capture();
            GUILayout.EndArea();
        }
    }
}
