namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// Sound / haptic seam of ButtonFeedback, called in the same frame as the punch (agent-kit/05 §5: a late click
    /// sound feels detached). Optional: with nothing bound, buttons only punch. No implementation in this phase.
    /// </summary>
    public interface IButtonFeedbackHook
    {
        /// <summary>Pointer went down on an interactable button (the punch starts now).</summary>
        void Pressed(ButtonFeedback source);

        /// <summary>Pointer went down on a non-interactable button: no punch; room for a "not available" sound / buzz.</summary>
        void Denied(ButtonFeedback source);
    }
}
