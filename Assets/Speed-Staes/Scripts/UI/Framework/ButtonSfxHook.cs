using SpeedStars.Core.Audio;
using Zenject;

namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// Sound and haptics of every button press (agent-kit/05 §5): ButtonFeedback calls it in the same frame as the punch.
    /// A press taps and buzzes lightly; a press on a button that cannot be used plays the denied sound (no buzz).
    /// Both services are optional, so a scene without the sound system keeps its buttons working.
    /// </summary>
    public sealed class ButtonSfxHook : IButtonFeedbackHook
    {
        private readonly ISfx _sfx;
        private readonly IHaptics _haptics;

        public ButtonSfxHook([InjectOptional] ISfx sfx, [InjectOptional] IHaptics haptics)
        {
            _sfx = sfx;
            _haptics = haptics;
        }

        public void Pressed(ButtonFeedback source)
        {
            _sfx?.Play(SfxId.Tap);
            _haptics?.Tap();
        }

        public void Denied(ButtonFeedback source)
        {
            _sfx?.Play(SfxId.Denied);
        }
    }
}
