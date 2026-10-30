using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Screen prefabs (UiScreen root, stretched full screen; layout in 2400×1080 reference pixels, CanvasScaler Expand: width varies per phone (never below the reference), so edge items are anchored to their corner and middle items are centered).
    /// Positions follow the original's reference screens (docs/gdd/referans-ekranlar): menu buttons on the right, racer
    /// controls bottom-left, the 3D racer in the middle stays free. Lists keep an empty Content: views instantiate rows.
    /// </summary>
    internal static class UiScreenPrefabs
    {
        /// <summary>UI v2 Home: art-dressed, positions from layout.json (see UiV2Prefabs).</summary>
        public static void Home(RectTransform root)
        {
            UiV2Prefabs.Home(root);
        }

        /// <summary>UI v2 race HUD: telemetry, cash, energy, cadence, exit and restart (see UiV2Prefabs). Only the buttons catch taps.</summary>
        public static void RaceHud(RectTransform root)
        {
            UiV2Prefabs.RaceHud(root);
        }
    }

    /// <summary>
    /// Popup prefabs (UiPopup root, stretched; a dim backdrop inside the prefab so it fades with the popup and blocks
    /// taps on what is below; the content sits on a centered "Window").
    /// </summary>
    internal static class UiPopupPrefabs
    {
        /// <summary>Entry gate (T-050): dressed with the daily reward art, see UiEntryGatePrefabs.</summary>
        public static void EntryGate(RectTransform root)
        {
            UiEntryGatePrefabs.EntryGate(root);
        }
    }
}
