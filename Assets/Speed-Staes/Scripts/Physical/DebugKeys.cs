using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpeedStars.Physical
{
    /// <summary>Debug shortcut keys that work with either input backend (Active Input Handling = Both).</summary>
    public static class DebugKeys
    {
        public static bool Down(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (key)
            {
                case KeyCode.Escape: return kb.escapeKey.wasPressedThisFrame;
                case KeyCode.Alpha1: return kb.digit1Key.wasPressedThisFrame;
                case KeyCode.Alpha2: return kb.digit2Key.wasPressedThisFrame;
                case KeyCode.Alpha3: return kb.digit3Key.wasPressedThisFrame;
                case KeyCode.Alpha4: return kb.digit4Key.wasPressedThisFrame;
                case KeyCode.H: return kb.hKey.wasPressedThisFrame;
                case KeyCode.P: return kb.pKey.wasPressedThisFrame;
                case KeyCode.O: return kb.oKey.wasPressedThisFrame;
                case KeyCode.K: return kb.kKey.wasPressedThisFrame;
                case KeyCode.C: return kb.cKey.wasPressedThisFrame;
                case KeyCode.Comma: return kb.commaKey.wasPressedThisFrame;
                case KeyCode.Period: return kb.periodKey.wasPressedThisFrame;
                case KeyCode.LeftBracket: return kb.leftBracketKey.wasPressedThisFrame;
                case KeyCode.RightBracket: return kb.rightBracketKey.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown(key);
#endif
        }
    }
}
