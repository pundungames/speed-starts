using UnityEngine;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// The look of the replay controls, taken from the race HUD and Results art (UI/Art/RaceHud, UI/Art/Results, UI/Art/EventSelect) so the
    /// replay reads like the rest of the game. Icons that only exist inside a wider sprite (the camera of the Watch Replay button, the
    /// Continue arrow) are cut out by a pixel rect, measured from the sprite's top-left corner.
    /// Builder input only: UiReplayPrefabs reads it and bakes the art into the ReplayControls prefab; nothing reads it in the game.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/UI/Replay Controls Skin", fileName = "ReplayControlsSkin")]
    public sealed class ReplayControlsSkin : ScriptableObject
    {
        [Tooltip("Text font (the one the race HUD and Results texts use).")]
        public Font font;
        [Tooltip("Text colour on the cream panels and buttons (the race HUD's navy).")]
        public Color textColor = new Color32(10, 31, 92, 255);
        [Tooltip("Text style (the race HUD's texts are bold italic).")]
        public FontStyle fontStyle = FontStyle.BoldAndItalic;

        [Header("Race clock panel (as the race HUD)")]
        [Tooltip("RaceHud/telemetry_panel: the panel behind time and speed.")]
        public Sprite telemetryPanel;
        [Tooltip("RaceHud/stopwatch_icon.")]
        public Sprite stopwatchIcon;
        [Tooltip("RaceHud/speedometer_icon.")]
        public Sprite speedometerIcon;
        [Tooltip("RaceHud/counter_panel: the plate behind the followed runner's name and the speed button.")]
        public Sprite namePanel;

        [Header("Buttons")]
        [Tooltip("RaceHud/square_button_background: every square button.")]
        public Sprite squareButton;
        [Tooltip("RaceHud/exit_icon: leaves the replay.")]
        public Sprite exitIcon;
        [Tooltip("Results/watch_replay_icon: the sprite the camera icon is cut from.")]
        public Sprite cameraIconSheet;
        [Tooltip("Where the camera icon sits in cameraIconSheet (px, x/y from the top-left, width, height).")]
        public RectInt cameraIconRect = new RectInt(33, 21, 78, 62);
        [Tooltip("EventSelect/continue_arrow: the sprite the runner arrows are cut from (the left one is mirrored).")]
        public Sprite arrowSheet;
        [Tooltip("Where the arrow sits in arrowSheet (px, x/y from the top-left, width, height).")]
        public RectInt arrowRect = new RectInt(286, 48, 27, 47);

        [Header("Timeline (as the race HUD's energy bar)")]
        [Tooltip("RaceHud/energy_track: the bar's background.")]
        public Sprite timelineTrack;
        [Tooltip("RaceHud/energy_fill: the played part.")]
        public Sprite timelineFill;
        [Tooltip("RaceHud/cadence_pointer: the handle dragged to seek.")]
        public Sprite timelineHandle;
    }
}
